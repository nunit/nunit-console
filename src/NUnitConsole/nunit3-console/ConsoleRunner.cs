// ***********************************************************************
// Copyright (c) 2014 Charlie Poole, Rob Prouse
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
// ***********************************************************************

using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using NUnit.Common;
using NUnit.ConsoleRunner.Utilities;
using NUnit.Engine;
using NUnit.Engine.Extensibility;
using System.Runtime.InteropServices;
using System.Text;

namespace NUnit.ConsoleRunner
{
    /// <summary>
    /// ConsoleRunner provides the nunit3-console text-based
    /// user interface, running the tests and reporting the results.
    /// </summary>
    public class ConsoleRunner
    {
        public static readonly int OK = 0;
        public static readonly int INVALID_ARG = -1;
        public static readonly int INVALID_ASSEMBLY = -2;
        //public static readonly int FIXTURE_NOT_FOUND = -3;    //No longer in use
        public static readonly int INVALID_TEST_FIXTURE = -4;
        //public static readonly int UNLOAD_ERROR = -5;         //No longer in use
        public static readonly int UNEXPECTED_ERROR = -100;

        private ITestEngine _engine;
        private ConsoleOptions _options;
        private IResultService _resultService;
        private ITestFilterService _filterService;
        private IExtensionService _extensionService;

        private ExtendedTextWriter _outWriter;

        private string _workDirectory;

        public ConsoleRunner(ITestEngine engine, ConsoleOptions options, ExtendedTextWriter writer)
        {
            _engine = engine;
            _options = options;
            _outWriter = writer;

            _workDirectory = options.WorkDirectory ?? Directory.GetCurrentDirectory();

            if (!Directory.Exists(_workDirectory))
                Directory.CreateDirectory(_workDirectory);

            _resultService = _engine.Services.GetService<IResultService>();
            _filterService = _engine.Services.GetService<ITestFilterService>();
            _extensionService = _engine.Services.GetService<IExtensionService>();

            // Enable TeamCityEventListener immediately, before the console is redirected
            _extensionService?.EnableExtension("NUnit.Engine.Listeners.TeamCityEventListener", _options.TeamCity);
        }

        /// <summary>
        /// Executes tests according to the provided commandline options.
        /// </summary>
        /// <returns></returns>
        public int Execute()
        {
            if (!VerifyEngineSupport(_options))
                return INVALID_ARG;

            DisplayRuntimeEnvironment(_outWriter);

            if (_options.ListExtensions)
                DisplayExtensionList();

            if (_options.InputFiles.Count == 0)
            {
                if (!_options.ListExtensions)
                    using (new ColorConsole(ColorStyle.Error))
                        Console.Error.WriteLine("Error: no inputs specified");
                return ConsoleRunner.OK;
            }

            DisplayTestFiles();

            TestPackage package = MakeTestPackage(_options);

            // We display the filters at this point so  that any exception message
            // thrown by CreateTestFilter will be understandable.
            DisplayTestFilters();

            TestFilter filter = CreateTestFilter(_options);

            if (_options.Explore)
                return ExploreTests(package, filter);
            else
                return RunTests(package, filter);
        }

        private void DisplayTestFiles()
        {
            _outWriter.WriteLine(ColorStyle.SectionHeader, "Test Files");
            foreach (string file in _options.InputFiles)
                _outWriter.WriteLine(ColorStyle.Default, "    " + file);
            _outWriter.WriteLine();
        }

        private int ExploreTests(TestPackage package, TestFilter filter)
        {
            XmlNode result;

            using (var runner = _engine.GetRunner(package))
                result = runner.Explore(filter);

            if (_options.ExploreOutputSpecifications.Count == 0)
            {
                _resultService.GetResultWriter("cases", null).WriteResultFile(result, Console.Out);
            }
            else
            {
                foreach (OutputSpecification spec in _options.ExploreOutputSpecifications)
                {
                    _resultService.GetResultWriter(spec.Format, new object[] {spec.Transform}).WriteResultFile(result, spec.OutputPath);
                    _outWriter.WriteLine("Results ({0}) saved as {1}", spec.Format, spec.OutputPath);
                }
            }

            return ConsoleRunner.OK;
        }

        private int RunTests(TestPackage package, TestFilter filter)
        {

            var writer = new ColorConsoleWriter(!_options.NoColor);

            foreach (var spec in _options.ResultOutputSpecifications)
            {
                var outputPath = Path.Combine(_workDirectory, spec.OutputPath);
                
                IResultWriter resultWriter;
                
                try
                {
                    resultWriter = GetResultWriter(spec);
                }
                catch (Exception ex)
                {
                    throw new NUnitEngineException($"Error encountered in resolving output specification: {spec}", ex);
                }

                try
                {
                    var outputDirectory = Path.GetDirectoryName(outputPath);
                    Directory.CreateDirectory(outputDirectory);
                }
                catch (Exception ex)
                {
                    writer.WriteLine(ColorStyle.Error, String.Format(
                        "The directory in --result {0} could not be created",
                        spec.OutputPath));
                    writer.WriteLine(ColorStyle.Error, ExceptionHelper.BuildMessage(ex));
                    return ConsoleRunner.UNEXPECTED_ERROR;
                }

                try
                {
                    resultWriter.CheckWritability(outputPath);
                }
                catch (Exception ex)
                {
                    throw new NUnitEngineException(
                        String.Format(
                            "The path specified in --result {0} could not be written to",
                            spec.OutputPath), ex);
                }

            }

            var labels = _options.DisplayTestLabels != null
                ? _options.DisplayTestLabels.ToUpperInvariant()
                : "ON";

            XmlNode result = null;
            NUnitEngineUnloadException unloadException = null;
            NUnitEngineException engineException = null;

            try
            {
                using (new SaveConsoleOutput())
                using (ITestRunner runner = _engine.GetRunner(package))
                using (var output = CreateOutputWriter())
                {
                    var eventHandler = new TestEventHandler(output, labels);

                    result = runner.Run(eventHandler, filter);
                }
            }
            catch (NUnitEngineUnloadException ex)
            {
                unloadException = ex;
            }
            catch (NUnitEngineException ex)
            {
                engineException = ex;
            }

            if (result != null)
            {
                var reporter = new ResultReporter(result, writer, _options);
                reporter.ReportResults();

                foreach (var spec in _options.ResultOutputSpecifications)
                {
                    var outputPath = Path.Combine(_workDirectory, spec.OutputPath);
                    GetResultWriter(spec).WriteResultFile(result, outputPath);
                    writer.WriteLine("Results ({0}) saved as {1}", spec.Format, spec.OutputPath);
                }

                if (engineException != null)
                {
                    writer.WriteLine(ColorStyle.Error, Environment.NewLine + ExceptionHelper.BuildMessage(engineException));
                    return ConsoleRunner.UNEXPECTED_ERROR;
                }

                if (unloadException != null)
                {
                    writer.WriteLine(ColorStyle.Warning, Environment.NewLine + ExceptionHelper.BuildMessage(unloadException));
                }

                if (reporter.Summary.UnexpectedError)
                    return ConsoleRunner.UNEXPECTED_ERROR;

                if (reporter.Summary.InvalidAssemblies > 0)
                    return ConsoleRunner.INVALID_ASSEMBLY;

                return reporter.Summary.InvalidTestFixtures > 0
                    ? ConsoleRunner.INVALID_TEST_FIXTURE
                    : reporter.Summary.FailureCount + reporter.Summary.ErrorCount + reporter.Summary.InvalidCount;
            }

            // If we got here, it's because we had an exception, but check anyway
            if (engineException != null)
            {
                writer.WriteLine(ColorStyle.Error, ExceptionHelper.BuildMessage(engineException));
                writer.WriteLine();
                writer.WriteLine(ColorStyle.Error, ExceptionHelper.BuildMessageAndStackTrace(engineException));
            }

            return ConsoleRunner.UNEXPECTED_ERROR;
        }

        private void DisplayRuntimeEnvironment(ExtendedTextWriter OutWriter)
        {
            OutWriter.WriteLine(ColorStyle.SectionHeader, "Runtime Environment");
            OutWriter.WriteLabelLine("   OS Version: ", GetOSVersion());
#if NET20
            OutWriter.WriteLabelLine("   Runtime: ", ".NET Framework CLR v" + Environment.Version.ToString());
#else
            OutWriter.WriteLabelLine("  Runtime: ", RuntimeInformation.FrameworkDescription);
#endif


            OutWriter.WriteLine();
        }

        private static string GetOSVersion()
        {
#if NET20
            OperatingSystem os = Environment.OSVersion;
            string osString = os.ToString();
            if (os.Platform == PlatformID.Unix)
            {
                IntPtr buf = Marshal.AllocHGlobal(8192);
                if (uname(buf) == 0)
                {
                    var unixVariant = Marshal.PtrToStringAnsi(buf);
                    if (unixVariant.Equals("Darwin"))
                        unixVariant = "MacOSX";

                    osString = string.Format("{0} {1} {2}", unixVariant, os.Version, os.ServicePack);
                }
                Marshal.FreeHGlobal(buf);
            }
            return osString;
#else
            return RuntimeInformation.OSDescription;
#endif
        }

        [DllImport("libc")]
        static extern int uname(IntPtr buf);

        private void DisplayExtensionList()
        {
            _outWriter.WriteLine(ColorStyle.SectionHeader, "Installed Extensions");

            foreach (var ep in _extensionService?.ExtensionPoints ?? new IExtensionPoint[0])
            {
                _outWriter.WriteLabelLine("  Extension Point: ", ep.Path);
                foreach (var node in ep.Extensions)
                {
                    _outWriter.Write("    Extension: ");
                    _outWriter.Write(ColorStyle.Value, $"{node.TypeName}");
                    if(node.TargetFramework != null)
                        _outWriter.Write(ColorStyle.Value, $"(.NET {node.TargetFramework?.FrameworkVersion})");
                    _outWriter.WriteLine(node.Enabled ? "" : " (Disabled)");

                    _outWriter.Write("      Version: ");
                    _outWriter.WriteLine(ColorStyle.Value, node.AssemblyVersion.ToString());

                    _outWriter.Write("      Path: ");
                    _outWriter.WriteLine(ColorStyle.Value, node.AssemblyPath);

                    foreach (var prop in node.PropertyNames)
                    {
                        _outWriter.Write("      " + prop + ":");
                        foreach (var val in node.GetValues(prop))
                            _outWriter.Write(ColorStyle.Value, " " + val);
                        _outWriter.WriteLine();
                    }
                }
            }

            _outWriter.WriteLine();
        }

        private void DisplayTestFilters()
        {
            if (_options.TestList.Count > 0 || _options.WhereClauseSpecified)
            {
                _outWriter.WriteLine(ColorStyle.SectionHeader, "Test Filters");

                if (_options.TestList.Count > 0)
                    foreach (string testName in _options.TestList)
                        _outWriter.WriteLabelLine("    Test: ", testName);

                if (_options.WhereClauseSpecified)
                    _outWriter.WriteLabelLine("    Where: ", _options.WhereClause.Trim());

                _outWriter.WriteLine();
            }
        }

        private ExtendedTextWriter CreateOutputWriter()
        {
            if (_options.OutFileSpecified)
            {
                var outStreamWriter = new StreamWriter(Path.Combine(_workDirectory, _options.OutFile));
                outStreamWriter.AutoFlush = true;

                return new ExtendedTextWrapper(outStreamWriter);
            }

            return _outWriter;
        }

        private IResultWriter GetResultWriter(OutputSpecification spec)
        {
            return _resultService.GetResultWriter(spec.Format, new object[] {spec.Transform});
        }

        // This is public static for ease of testing
        public static TestPackage MakeTestPackage(ConsoleOptions options)
        {
            TestPackage package = new TestPackage(options.InputFiles);

            if (options.ProcessModelSpecified)
                package.AddSetting(EnginePackageSettings.ProcessModel, options.ProcessModel);

            if (options.DomainUsageSpecified)
                package.AddSetting(EnginePackageSettings.DomainUsage, options.DomainUsage);

            if (options.FrameworkSpecified)
                package.AddSetting(EnginePackageSettings.RuntimeFramework, options.Framework);

            if (options.RunAsX86)
                package.AddSetting(EnginePackageSettings.RunAsX86, true);

            // Console runner always sets DisposeRunners
            //if (options.DisposeRunners)
                package.AddSetting(EnginePackageSettings.DisposeRunners, true);

            if (options.ShadowCopyFiles)
                package.AddSetting(EnginePackageSettings.ShadowCopyFiles, true);

            if (options.LoadUserProfile)
                package.AddSetting(EnginePackageSettings.LoadUserProfile, true);

            if (options.SkipNonTestAssemblies)
                package.AddSetting(EnginePackageSettings.SkipNonTestAssemblies, true);

            if (options.DefaultTimeout >= 0)
                package.AddSetting(FrameworkPackageSettings.DefaultTimeout, options.DefaultTimeout);

            if (options.InternalTraceLevelSpecified)
                package.AddSetting(FrameworkPackageSettings.InternalTraceLevel, options.InternalTraceLevel);

            if (options.ActiveConfigSpecified)
                package.AddSetting(EnginePackageSettings.ActiveConfig, options.ActiveConfig);

            // Always add work directory, in case current directory is changed
            var workDirectory = options.WorkDirectory ?? Directory.GetCurrentDirectory();
            package.AddSetting(FrameworkPackageSettings.WorkDirectory, workDirectory);

            if (options.StopOnError)
                package.AddSetting(FrameworkPackageSettings.StopOnError, true);

            if (options.MaxAgentsSpecified)
                package.AddSetting(EnginePackageSettings.MaxAgents, options.MaxAgents);

            if (options.NumberOfTestWorkersSpecified)
                package.AddSetting(FrameworkPackageSettings.NumberOfTestWorkers, options.NumberOfTestWorkers);

            if (options.RandomSeedSpecified)
                package.AddSetting(FrameworkPackageSettings.RandomSeed, options.RandomSeed);

            if (options.DebugTests)
            {
                package.AddSetting(FrameworkPackageSettings.DebugTests, true);

                if (!options.NumberOfTestWorkersSpecified)
                    package.AddSetting(FrameworkPackageSettings.NumberOfTestWorkers, 0);
            }

            if (options.PauseBeforeRun)
                package.AddSetting(FrameworkPackageSettings.PauseBeforeRun, true);

            if (options.PrincipalPolicy != null)
                package.AddSetting(EnginePackageSettings.PrincipalPolicy, options.PrincipalPolicy);

#if DEBUG
            if (options.DebugAgent)
                package.AddSetting(EnginePackageSettings.DebugAgent, true);

            //foreach (KeyValuePair<string, object> entry in package.Settings)
            //    if (!(entry.Value is string || entry.Value is int || entry.Value is bool))
            //        throw new Exception(string.Format("Package setting {0} is not a valid type", entry.Key));
#endif

            if (options.DefaultTestNamePattern != null)
                package.AddSetting(FrameworkPackageSettings.DefaultTestNamePattern, options.DefaultTestNamePattern);

            if (options.TestParameters.Count != 0)
                AddTestParametersSetting(package, options.TestParameters);

            if (options.ConfigurationFile != null)
                package.AddSetting(EnginePackageSettings.ConfigurationFile, options.ConfigurationFile);

            return package;
        }

        /// <summary>
        /// Sets test parameters, handling backwards compatibility.
        /// </summary>
        private static void AddTestParametersSetting(TestPackage testPackage, IDictionary<string, string> testParameters)
        {
            testPackage.AddSetting(FrameworkPackageSettings.TestParametersDictionary, testParameters);

            if (testParameters.Count != 0)
            {
                // This cannot be changed without breaking backwards compatibility with old frameworks.
                // Reserializes the way old frameworks understand, even if this runner's parsing is changed.

                var oldFrameworkSerializedParameters = new StringBuilder();
                foreach (var parameter in testParameters)
                    oldFrameworkSerializedParameters.Append(parameter.Key).Append('=').Append(parameter.Value).Append(';');

                testPackage.AddSetting(FrameworkPackageSettings.TestParameters, oldFrameworkSerializedParameters.ToString(0, oldFrameworkSerializedParameters.Length - 1));
            }
        }

        private TestFilter CreateTestFilter(ConsoleOptions options)
        {
            ITestFilterBuilder builder = _filterService.GetTestFilterBuilder();

            foreach (string testName in options.TestList)
                builder.AddTest(testName);

            if (options.WhereClauseSpecified)
                builder.SelectWhere(options.WhereClause);

            return builder.GetFilter();
        }

        private bool VerifyEngineSupport(ConsoleOptions options)
        {
            foreach (var spec in options.ResultOutputSpecifications)
            {
                bool available = false;

                foreach (var format in _resultService.Formats)
                {
                    if (spec.Format == format)
                    {
                        available = true;
                        break;
                    }
                }

                if (!available)
                {
                    Console.WriteLine("Unknown result format: {0}", spec.Format);
                    return false;
                }
            }

            return true;
        }
    }
}

