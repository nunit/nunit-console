// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using NUnit.Common;
using NUnit.ConsoleRunner.Options;
using NUnit.ConsoleRunner.Utilities;
using NUnit.Engine;
using NUnit.Engine.Extensibility;
using NUnit.Extensibility;
using NUnit.TextDisplay;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace NUnit.ConsoleRunner
{
    /// <summary>
    /// ConsoleRunner provides the nunit4-console text-based
    /// user interface, running the tests and reporting the results.
    /// </summary>
    public class ConsoleRunner
    {
        private static readonly Logger log = InternalTrace.GetLogger(typeof(ConsoleRunner));

        private static readonly char[] PathSeparator = [Path.PathSeparator];

        // Some operating systems truncate the return code to 8 bits, which
        // only allows us a maximum of 127 in the positive range. We limit
        // ourselves so as to stay in that range.
        private const int MAXIMUM_RETURN_CODE_ALLOWED = 100; // In case we are running on Unix

        private const string EVENT_LISTENER_EXTENSION_PATH = "/NUnit/Engine/TypeExtensions/ITestEventListener";
        private const string TEAMCITY_EVENT_LISTENER = "NUnit.Engine.Listeners.TeamCityEventListener";

        private const string INDENT4 = "    ";
        private const string INDENT6 = "      ";
        private const string INDENT8 = "        ";
        private const string INDENT10 = "          ";

        private const string NUNIT_EXTENSION_DIRECTORIES = "NUNIT_EXTENSION_DIRECTORIES";

        public static readonly int OK = 0;
        public static readonly int INVALID_ARG = -1;
        public static readonly int INVALID_ASSEMBLY = -2;
        //public static readonly int FIXTURE_NOT_FOUND = -3;    //No longer in use
        public static readonly int INVALID_TEST_FIXTURE = -4;
        //public static readonly int UNLOAD_ERROR = -5;         //No longer in use
        public static readonly int UNEXPECTED_ERROR = -100;

        private readonly ITestEngine _engine;
        private readonly ConsoleOptions _options;
        private readonly IResultService _resultService;
        private readonly ITestFilterService _filterService;
        private readonly IExtensionService _extensionService;

        private readonly ExtendedTextWriter _outWriter;

        private readonly string _workDirectory;

        public ConsoleRunner(ITestEngine engine, ConsoleOptions options, ExtendedTextWriter writer)
        {
            Guard.ArgumentNotNull(_engine = engine);
            Guard.ArgumentNotNull(_options = options);
            Guard.ArgumentNotNull(_outWriter = writer);

            // NOTE: Accessing Services triggers the engine to initialize all services
            _resultService = _engine.Services.GetService<IResultService>();
            Guard.OperationValid(_resultService is not null, "Internal Error: ResultService was not found");

            _filterService = _engine.Services.GetService<ITestFilterService>();
            Guard.OperationValid(_filterService is not null, "Internal Error: TestFilterService was not found");

            _extensionService = _engine.Services.GetService<IExtensionService>();
            Guard.OperationValid(_extensionService is not null, "Internal Error: ExtensionService was not found");

            var extensionPath = Environment.GetEnvironmentVariable(NUNIT_EXTENSION_DIRECTORIES);
            if (!string.IsNullOrEmpty(extensionPath))
                foreach (string extensionDirectory in extensionPath.Split(PathSeparator, StringSplitOptions.RemoveEmptyEntries))
                    _extensionService.FindExtensionAssemblies(extensionDirectory);

            foreach (string extensionDirectory in _options.ExtensionDirectories)
                _extensionService.FindExtensionAssemblies(extensionDirectory);

            _extensionService.InstallExtensions();

            _workDirectory = options.WorkDirectory ?? Directory.GetCurrentDirectory();

            if (!Directory.Exists(_workDirectory))
                Directory.CreateDirectory(_workDirectory);

            // Attempt to enable extensions as requested by the user
            foreach (string typeName in options.EnableExtensions)
            {
                // Throw if requested extension is not installed
                if (!IsExtensionInstalled(typeName))
                    throw new RequiredExtensionException(typeName);

                EnableExtension(typeName);
                Console.WriteLine($"Enabled extension {typeName}");
            }

            // Also enable TeamCity extension under TeamCity, if it is installed
            if (RunningUnderTeamCity && IsExtensionInstalled(TEAMCITY_EVENT_LISTENER))
                EnableExtension(TEAMCITY_EVENT_LISTENER);

            // Disable extensions as requested by the user, ignoring any not installed
            foreach (string typeName in options.DisableExtensions)
                if (IsExtensionInstalled(typeName))
                    DisableExtension(typeName);
        }

        private static bool RunningUnderTeamCity =>
            !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("TEAMCITY_PROJECT_NAME"));

        private bool IsExtensionInstalled(string typeName)
        {
            foreach (var node in _extensionService.Extensions)
                if (node.TypeName == typeName)
                    return true;

            return false;
        }

        private void EnableExtension(string name) => _extensionService.EnableExtension(name, true);

        private void DisableExtension(string name) => _extensionService.EnableExtension(name, false);

        /// <summary>
        /// Executes tests according to the provided command-line options.
        /// </summary>
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
                _outWriter.WriteLine(ColorStyle.Default, INDENT4 + file);
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
                    _resultService.GetResultWriter(spec.Format, spec.Transform).WriteResultFile(result, spec.OutputPath);
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
                    log.Debug($"Got ResultWriter {resultWriter}");
                }
                catch (Exception ex)
                {
                    throw new NUnitEngineException($"Error encountered in resolving output specification: {spec}", ex);
                }

                try
                {
                    var outputDirectory = Path.GetDirectoryName(outputPath)!;
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

            var labels = _options.DisplayTestLabels is not null
                ? _options.DisplayTestLabels.ToUpperInvariant()
                : "ON";

            XmlNode? result = null;
            NUnitEngineUnloadException? unloadException = null;
            NUnitEngineException? engineException = null;

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

            if (result is not null)
            {
                var summary = new ResultSummary(result);
                var settings = _options.ResultReporterSettings;

                new ResultReporter(settings).ReportResults(summary, writer);

                foreach (var spec in _options.ResultOutputSpecifications)
                {
                    var outputPath = Path.Combine(_workDirectory, spec.OutputPath);
                    GetResultWriter(spec).WriteResultFile(result, outputPath);
                    writer.WriteLine("Results ({0}) saved as {1}", spec.Format, spec.OutputPath);
                }

                if (engineException is not null)
                {
                    writer.WriteLine(ColorStyle.Error, Environment.NewLine + ExceptionHelper.BuildMessage(engineException));
                    return ConsoleRunner.UNEXPECTED_ERROR;
                }

                if (unloadException is not null)
                {
                    writer.WriteLine(ColorStyle.Warning, Environment.NewLine + ExceptionHelper.BuildMessage(unloadException));
                }

                if (summary.UnexpectedError)
                    return ConsoleRunner.UNEXPECTED_ERROR;

                if (summary.InvalidAssemblies > 0)
                    return ConsoleRunner.INVALID_ASSEMBLY;

                if (summary.InvalidTestFixtures > 0)
                    return ConsoleRunner.INVALID_TEST_FIXTURE;

                var failureCount = summary.FailureCount + summary.ErrorCount + summary.InvalidCount;
                return Math.Min(failureCount, MAXIMUM_RETURN_CODE_ALLOWED);
            }

            // If we got here, it's because we had an exception, but check anyway
            if (engineException is not null)
            {
                writer.WriteLine(ColorStyle.Error, ExceptionHelper.BuildMessage(engineException));
                writer.WriteLine();
                writer.WriteLine(ColorStyle.Error, ExceptionHelper.BuildMessageAndStackTrace(engineException));
            }

            return ConsoleRunner.UNEXPECTED_ERROR;
        }

        private static void DisplayRuntimeEnvironment(ExtendedTextWriter OutWriter)
        {
            OutWriter.WriteLine(ColorStyle.SectionHeader, "Runtime Environment");
            OutWriter.WriteLabelLine(INDENT4 + "OS Version: ", GetOSVersion());
#if NETFRAMEWORK
            OutWriter.WriteLabelLine(INDENT4 + "Runtime: ", ".NET Framework CLR v" + Environment.Version.ToString());
#else
            OutWriter.WriteLabelLine(INDENT4 + "Runtime: ", RuntimeInformation.FrameworkDescription);
#endif

            OutWriter.WriteLine();
        }

#if NETFRAMEWORK
        [DllImport("libc")]
        private static extern int uname(IntPtr buf);

        private static string GetOSVersion()
        {
            OperatingSystem os = Environment.OSVersion;
            string osString = os.ToString();
            if (os.Platform == PlatformID.Unix)
            {
                IntPtr buf = Marshal.AllocHGlobal(8192);
                if (uname(buf) == 0)
                {
                    var unixVariant = Marshal.PtrToStringAnsi(buf);
                    if (string.Equals(unixVariant, "Darwin"))
                        unixVariant = "MacOSX";

                    osString = string.Format("{0} {1} {2}", unixVariant, os.Version, os.ServicePack);
                }
                Marshal.FreeHGlobal(buf);
            }
            return osString;
        }
#else
        private static string GetOSVersion()
        {
            return RuntimeInformation.OSDescription;
        }
#endif

        private void DisplayExtensionList()
        {
            if (_options.ExtensionDirectories.Count > 0)
            {
                _outWriter.WriteLine(ColorStyle.SectionHeader, "User Extension Directories");
                foreach (var dir in _options.ExtensionDirectories)
                    _outWriter.WriteLine($"  {Path.GetFullPath(dir)}");
                _outWriter.WriteLine();
            }

            _outWriter.WriteLine(ColorStyle.SectionHeader, "Installed Extensions");

            if (_extensionService.ExtensionPoints is not null)
                foreach (var ep in _extensionService.ExtensionPoints)
                {
                    _outWriter.WriteLabelLine(INDENT4 + "Extension Point: ", ep.Path);
                    foreach (var node in ep.Extensions)
                        DisplayExtension(node);
                }

            var unknownExtensions = _extensionService.Extensions.Where(n => n.Status == ExtensionStatus.Unknown);
            if (unknownExtensions.Any())
            {
                _outWriter.WriteLine();
                _outWriter.WriteLine(ColorStyle.Label, "Unknown Extensions");
                _outWriter.WriteLine(INDENT4 + "Extensions not matching any known ExtensionPoint");
                foreach (var node in unknownExtensions)
                    DisplayExtension(node);
            }

            _outWriter.WriteLine();
        }

        private void DisplayExtension(IExtensionNode node)
        {
            _outWriter.Write(INDENT6 + "Extension: ");
            _outWriter.Write(ColorStyle.Value, $"{node.TypeName}");
            _outWriter.WriteLine(node.Enabled ? string.Empty : " (Disabled)");

            _outWriter.Write(INDENT8 + "Version: ");
            _outWriter.WriteLine(ColorStyle.Value, node.AssemblyVersion.ToString());

            _outWriter.Write(INDENT8 + "Status: ");
            _outWriter.WriteLine(ColorStyle.Value, node.Status.ToString());

            _outWriter.Write(INDENT8 + "Enabled: ");
            _outWriter.WriteLine(ColorStyle.Value, node.Enabled.ToString());

            _outWriter.Write(INDENT8 + "Path: ");
            _outWriter.WriteLine(ColorStyle.Value, node.AssemblyPath);

            if (node.PropertyNames.Any())
            {
                _outWriter.WriteLine(INDENT8 + "Properties -");

                foreach (var prop in node.PropertyNames)
                {
                    _outWriter.Write(INDENT10 + prop + ":");
                    foreach (var val in node.GetValues(prop))
                        _outWriter.Write(ColorStyle.Value, " " + val);
                    _outWriter.WriteLine();
                }
            }
        }

        private void DisplayTestFilters()
        {
            if (_options.TestList.Count > 0 || _options.WhereClauseSpecified)
            {
                _outWriter.WriteLine(ColorStyle.SectionHeader, "Test Filters");

                if (_options.TestList.Count > 0)
                    foreach (string testName in _options.TestList)
                        _outWriter.WriteLabelLine(INDENT4 + "Test: ", testName);

                if (_options.WhereClauseSpecified)
                    _outWriter.WriteLabelLine(INDENT4 + "Where: ", _options.WhereClause.Trim());

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
            return _resultService.GetResultWriter(spec.Format, spec.Transform);
        }

        // This is public static for ease of testing
        public static TestPackage MakeTestPackage(ConsoleOptions options)
        {
            TestPackage package = new TestPackage(options.InputFiles);

            if (options.RuntimeFrameworkSpecified)
            {
                // Temporarily use both settings until RuntimeFramework is retired
                package.AddSetting(SettingDefinitions.RequestedRuntimeFramework.WithValue(options.RuntimeFramework));
                package.AddSetting(SettingDefinitions.RequestedFrameworkName.WithValue(options.RuntimeFramework));
            }

            if (options.RunAsX86)
                package.AddSetting(SettingDefinitions.RunAsX86.WithValue(true));

            // Console runner always sets DisposeRunners
            //if (options.DisposeRunners)
            package.AddSetting(SettingDefinitions.DisposeRunners.WithValue(true));

            if (options.ShadowCopyFiles)
                package.AddSetting(SettingDefinitions.ShadowCopyFiles.WithValue(true));

            if (options.LoadUserProfile)
                package.AddSetting(SettingDefinitions.LoadUserProfile.WithValue(true));

            if (options.SkipNonTestAssemblies)
                package.AddSetting(SettingDefinitions.SkipNonTestAssemblies.WithValue(true));

            if (options.TestRunTimeout > 0)
                package.AddSetting(SettingDefinitions.TestRunTimeout.WithValue(options.TestRunTimeout));
            if (options.DefaultTestCaseTimeout >= 0)
                package.AddSetting(SettingDefinitions.DefaultTimeout.WithValue(options.DefaultTestCaseTimeout));

            if (options.InternalTraceLevelSpecified)
                package.AddSetting(SettingDefinitions.InternalTraceLevel.WithValue(options.InternalTraceLevel));

            if (options.ActiveConfigSpecified)
                package.AddSetting(SettingDefinitions.ActiveConfig.WithValue(options.ActiveConfig));

            // Always add work directory, in case current directory is changed
            var workDirectory = options.WorkDirectory ?? Directory.GetCurrentDirectory();
            package.AddSetting(SettingDefinitions.WorkDirectory.WithValue(workDirectory));

            if (options.StopOnError)
                package.AddSetting(SettingDefinitions.StopOnError.WithValue(true));

            if (options.MaxAgentsSpecified)
                package.AddSetting(SettingDefinitions.MaxAgents.WithValue(options.MaxAgents));

            if (options.NumberOfTestWorkersSpecified)
                package.AddSetting(SettingDefinitions.NumberOfTestWorkers.WithValue(options.NumberOfTestWorkers));

            if (options.RandomSeedSpecified)
                package.AddSetting(SettingDefinitions.RandomSeed.WithValue(options.RandomSeed));

            if (options.DebugTests)
            {
                package.AddSetting(SettingDefinitions.DebugTests.WithValue(true));

                if (!options.NumberOfTestWorkersSpecified)
                    package.AddSetting(SettingDefinitions.NumberOfTestWorkers.WithValue(0));
            }

            if (options.PauseBeforeRun)
                package.AddSetting(SettingDefinitions.PauseBeforeRun.WithValue(true));

            if (options.PrincipalPolicy is not null)
                package.AddSetting(SettingDefinitions.PrincipalPolicy.WithValue(options.PrincipalPolicy));

#if DEBUG
            if (options.DebugAgent)
                package.AddSetting(SettingDefinitions.DebugAgent.WithValue(true));
            if (options.DebugConsole)
                package.AddSetting(SettingDefinitions.DebugConsole.WithValue(true));

            //foreach (KeyValuePair<string, object> entry in package.Settings)
            //    if (!(entry.Value is string || entry.Value is int || entry.Value is bool))
            //        throw new Exception(string.Format("Package setting {0} is not a valid type", entry.Key));
#endif

            if (options.DefaultTestNamePattern is not null)
                package.AddSetting(SettingDefinitions.DefaultTestNamePattern.WithValue(options.DefaultTestNamePattern));

            if (options.TestParameters.Count != 0)
                AddTestParametersSetting(package, options.TestParameters);

            if (options.ConfigurationFile is not null)
                package.AddSetting(SettingDefinitions.ConfigurationFile.WithValue(options.ConfigurationFile));

            return package;
        }

        private static string MakeFrameworkName(string runtimeFramework)
        {
            var parts = runtimeFramework.Split('-');

            if (parts.Length == 2)
            {
                string runtime = parts[0];
                Version version = new Version(parts[1]);
                string? identifier = null;

                switch (runtime)
                {
                    case "netcore":
                        identifier = FrameworkIdentifiers.NetCoreApp;
                        break;
                    case "net":
                        identifier = version.Major > 4 ? FrameworkIdentifiers.NetCoreApp : FrameworkIdentifiers.NetFramework;
                        break;
                }

                if (identifier is not null)
                    return $"{identifier},Version=v{version}";
            }

            throw new ArgumentException($"Invalid RuntimeFramework: {runtimeFramework}", nameof(runtimeFramework));
        }

        /// <summary>
        /// Sets test parameters, handling backwards compatibility.
        /// </summary>
        private static void AddTestParametersSetting(TestPackage testPackage, IDictionary<string, string> testParameters)
        {
            testPackage.AddSetting(SettingDefinitions.TestParametersDictionary.WithValue(testParameters));

            if (testParameters.Count != 0)
            {
                // This cannot be changed without breaking backwards compatibility with old frameworks.
                // Reserializes the way old frameworks understand, even if this runner's parsing is changed.

                var oldFrameworkSerializedParameters = new StringBuilder();
                foreach (var parameter in testParameters)
                    oldFrameworkSerializedParameters.Append(parameter.Key).Append('=').Append(parameter.Value).Append(';');

                testPackage.AddSetting(SettingDefinitions.TestParameters.WithValue(oldFrameworkSerializedParameters.ToString(0, oldFrameworkSerializedParameters.Length - 1)));
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
