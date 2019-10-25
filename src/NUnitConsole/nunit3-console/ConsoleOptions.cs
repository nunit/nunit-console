// ***********************************************************************
// Copyright (c) 2015 Charlie Poole, Rob Prouse
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
using System.Text;
using System.Text.RegularExpressions;
using NUnit.Options;

namespace NUnit.Common
{
    /// <summary>
    /// <c>ConsoleOptions</c> encapsulates the option settings for
    /// the nunit3-console program. The class inherits from the Mono
    /// Options <see cref="OptionSet"/> class and provides a central location
    /// for defining and parsing options.
    /// </summary>
    internal class ConsoleOptions : OptionSet
    {
        private static readonly string CURRENT_DIRECTORY_ON_ENTRY = Directory.GetCurrentDirectory();

        private bool validated;
        private bool noresult;

        /// <summary>
        /// An abstraction of the file system
        /// </summary>
        protected readonly IFileSystem _fileSystem;

        internal ConsoleOptions(IDefaultOptionsProvider defaultOptionsProvider, IFileSystem fileSystem, params string[] args)
        {
            // Apply default options
            if (defaultOptionsProvider == null) throw new ArgumentNullException(nameof(defaultOptionsProvider));
            TeamCity = defaultOptionsProvider.TeamCity;
            _fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));

            ConfigureOptions();
            if (args != null)
                Parse(args);
        }

        public ConsoleOptions(params string[] args)
        {
            ConfigureOptions();
            if (args != null)
                Parse(args);
        }

        // Action to Perform

        public bool Explore { get; private set; }

        public bool ShowHelp { get; private set; }

        public bool ShowVersion { get; private set; }

        // Select tests

        public IList<string> InputFiles { get; } = new List<string>();

        public IList<string> TestList { get; } = new List<string>();

        public IDictionary<string, string> TestParameters { get; } = new Dictionary<string, string>();

        public string WhereClause { get; private set; }
        public bool WhereClauseSpecified { get { return WhereClause != null; } }

        public int DefaultTimeout { get; private set; } = -1;
        public bool DefaultTimeoutSpecified { get { return DefaultTimeout >= 0; } }

        public int RandomSeed { get; private set; } = -1;
        public bool RandomSeedSpecified { get { return RandomSeed >= 0; } }

        public string DefaultTestNamePattern { get; private set; }
        public int NumberOfTestWorkers { get; private set; } = -1;
        public bool NumberOfTestWorkersSpecified { get { return NumberOfTestWorkers >= 0; } }

        public bool StopOnError { get; private set; }

        public bool WaitBeforeExit { get; private set; }

        // Output Control

        public string ConsoleEncoding { get; private set; }

        public bool NoHeader { get; private set; }

        public bool NoColor { get; private set; }

        public bool TeamCity { get; private set; }

        public string OutFile { get; private set; }
        public bool OutFileSpecified { get { return OutFile != null; } }

        public string DisplayTestLabels { get; private set; }

        private string workDirectory = null;
        public string WorkDirectory
        {
            get { return workDirectory ?? CURRENT_DIRECTORY_ON_ENTRY; }
        }
        public bool WorkDirectorySpecified { get { return workDirectory != null; } }

        public string InternalTraceLevel { get; private set; }
        public bool InternalTraceLevelSpecified { get { return InternalTraceLevel != null; } }

        private readonly List<OutputSpecification> resultOutputSpecifications = new List<OutputSpecification>();
        public IList<OutputSpecification> ResultOutputSpecifications
        {
            get
            {
                if (noresult)
                    return new OutputSpecification[0];

                if (resultOutputSpecifications.Count == 0)
                    resultOutputSpecifications.Add(
                        new OutputSpecification("TestResult.xml", CURRENT_DIRECTORY_ON_ENTRY));

                return resultOutputSpecifications;
            }
        }

        public IList<OutputSpecification> ExploreOutputSpecifications { get; } = new List<OutputSpecification>();

        public string ActiveConfig { get; private set; }
        public bool ActiveConfigSpecified { get { return ActiveConfig != null; } }

        // Where to Run Tests

        public string ProcessModel { get; private set; }
        public bool ProcessModelSpecified { get { return ProcessModel != null; } }

        public string DomainUsage { get; private set; }
        public bool DomainUsageSpecified { get { return DomainUsage != null; } }

        // How to Run Tests

        public string Framework { get; private set; }
        public bool FrameworkSpecified { get { return Framework != null; } }

        public string ConfigurationFile { get; private set; }

        public bool RunAsX86 { get; private set; }

        public bool DisposeRunners { get; private set; }

        public bool ShadowCopyFiles { get; private set; }

        public bool LoadUserProfile { get; private set; }

        public bool SkipNonTestAssemblies { get; private set; }

        private int _maxAgents = -1;
        public int MaxAgents { get { return _maxAgents; } }
        public bool MaxAgentsSpecified { get { return _maxAgents >= 0; } }

        public bool DebugTests { get; private set; }

        public bool DebugAgent { get; private set; }

        public bool ListExtensions { get; private set; }

        public bool PauseBeforeRun { get; private set; }

        public string PrincipalPolicy { get; private set; }

        private void CheckOptionCombinations()
        {
            // Normally, console is run in a 64-bit process on a 64-bit machine
            // but this might vary if the process is started by some other program.
            if (IntPtr.Size == 8 && RunAsX86 && ProcessModel == "InProcess")
                ErrorMessages.Add("The --x86 and --inprocess options are incompatible.");
        }

        public IList<string> WarningMessages { get; } = new List<string>();

        public IList<string> ErrorMessages { get; } = new List<string>();

        public bool Validate()
        {
            if (!validated)
            {
                CheckOptionCombinations();

                validated = true;
            }

            return ErrorMessages.Count == 0;
        }

        private void ConfigureOptions()
        {
            // NOTE: The order in which patterns are added
            // determines the display order for the help.

            this.Add("test=", "Comma-separated list of {NAMES} of tests to run or explore. This option may be repeated.",
                v => ((List<string>)TestList).AddRange(TestNameParser.Parse(RequiredValue(v, "--test"))));

            this.Add("testlist=", "File {PATH} containing a list of tests to run, one per line. This option may be repeated.",
                v =>
                {
                    string testListFile = RequiredValue(v, "--testlist");

                    var fullTestListPath = ExpandToFullPath(testListFile);

                    if (!File.Exists(fullTestListPath))
                        ErrorMessages.Add("Unable to locate file: " + testListFile);
                    else
                    {
                        try
                        {
                            using (var rdr = new StreamReader(fullTestListPath))
                            {
                                while (!rdr.EndOfStream)
                                {
                                    var line = rdr.ReadLine().Trim();

                                    if (!string.IsNullOrEmpty(line) && line[0] != '#')
                                        ((List<string>)TestList).Add(line);
                                }
                            }
                        }
                        catch (IOException)
                        {
                            ErrorMessages.Add("Unable to read file: " + testListFile);
                        }
                    }
                });

            this.Add("where=", "Test selection {EXPRESSION} indicating what tests will be run. See description below.",
                v => WhereClause = RequiredValue(v, "--where"));

            this.Add("params|p=", "Deprecated and will be removed in a future release. Please use --testparam instead.",
                v =>
                {
                    const string deprecationWarning = "--params is deprecated and will be removed in a future release. Please use --testparam instead.";

                    if (!WarningMessages.Contains(deprecationWarning))
                        WarningMessages.Add(deprecationWarning);

                    string parameters = RequiredValue(v, "--params");

                    foreach (string param in parameters.Split(new[] { ';' }))
                    {
                        ApplyTestParameter(param);
                    }
                });

            this.Add("testparam|tp=", "Followed by a key-value pair separated by an equals sign. Test code can access the value by name.",
                v => ApplyTestParameter(RequiredValue(v, "--testparam")));

            this.Add("timeout=", "Set timeout for each test case in {MILLISECONDS}.",
                v => DefaultTimeout = RequiredInt(v, "--timeout"));

            this.Add("seed=", "Set the random {SEED} used to generate test cases.",
                v => RandomSeed = RequiredInt(v, "--seed"));

            this.Add("workers=", "Specify the {NUMBER} of worker threads to be used in running tests. If not specified, defaults to 2 or the number of processors, whichever is greater.",
                v => NumberOfTestWorkers = RequiredInt(v, "--workers"));

            this.Add("stoponerror", "Stop run immediately upon any test failure or error.",
                v => StopOnError = v != null);

            this.Add("wait", "Wait for input before closing console window.",
                v => WaitBeforeExit = v != null);

            // Output Control
            this.Add("work=", "{PATH} of the directory to use for output files. If not specified, defaults to the current directory.",
                v => workDirectory = RequiredValue(v, "--work"));

            this.Add("output|out=", "File {PATH} to contain text output from the tests.",
                v => OutFile = RequiredValue(v, "--output"));

            this.Add("result=", "An output {SPEC} for saving the test results.\nThis option may be repeated.",
                v => ResolveOutputSpecification(RequiredValue(v, "--resultxml"), resultOutputSpecifications));

            this.Add("explore:", "Display or save test info rather than running tests. Optionally provide an output {SPEC} for saving the test info. This option may be repeated.", v =>
            {
                Explore = true;
                ResolveOutputSpecification(v, ExploreOutputSpecifications);
            });

            this.Add("noresult", "Don't save any test results.",
                v => noresult = v != null);

            this.Add("labels=", "Specify whether to write test case names to the output. Values: Off, On, Before, After, BeforeAndAfter, All",
                v => DisplayTestLabels = RequiredValue(v, "--labels", "Off", "On", "Before", "After", "BeforeAndAfter", "All"));

            this.Add("test-name-format=", "Non-standard naming pattern to use in generating test names.",
                v => DefaultTestNamePattern = RequiredValue(v, "--test-name-format"));

            this.Add("trace=", "Set internal trace {LEVEL}.\nValues: Off, Error, Warning, Info, Verbose (Debug)",
                v => InternalTraceLevel = RequiredValue(v, "--trace", "Off", "Error", "Warning", "Info", "Verbose", "Debug"));

            this.Add("teamcity", "Turns on use of TeamCity service messages. TeamCity engine extension is required.",
                v => TeamCity = v != null);

            this.Add("noheader|noh", "Suppress display of program information at start of run.",
                v => NoHeader = v != null);

            this.Add("nocolor|noc", "Displays console output without color.",
                v => NoColor = v != null);

            this.Add("help|h", "Display this message and exit.",
                v => ShowHelp = v != null);

            this.Add("version|V", "Display the header and exit.",
                v => ShowVersion = v != null);

            this.Add("encoding=", "Specifies the encoding to use for Console standard output, for example utf-8, ascii, unicode.",
                v => ConsoleEncoding = RequiredValue(v, "--encoding"));

            // Default
            this.Add("<>", v =>
            {
                if (v.StartsWith("-") || v.StartsWith("/") && Path.DirectorySeparatorChar != '/')
                    ErrorMessages.Add("Invalid argument: " + v);
                else
                    InputFiles.Add(v);
            });

            this.Add("config=", "{NAME} of a project configuration to load (e.g.: Debug).",
                v => ActiveConfig = RequiredValue(v, "--config"));

            this.Add("configfile=", "{NAME} of configuration file to use for this run.",
                v => ConfigurationFile = RequiredValue(v, "--configfile"));

            // Where to Run Tests
            this.Add("process=", "{PROCESS} isolation for test assemblies.\nValues: InProcess, Separate, Multiple. If not specified, defaults to Separate for a single assembly or Multiple for more than one.",
                v =>
                {
                    ProcessModel = RequiredValue(v, "--process", "Single", "InProcess", "Separate", "Multiple");
                    // Change so it displays correctly even though it isn't absolutely needed
                    if (ProcessModel.ToLower() == "single")
                        ProcessModel = "InProcess";
                });

            this.Add("inprocess", "Synonym for --process:InProcess",
                v => ProcessModel = "InProcess");

            this.Add("domain=", "{DOMAIN} isolation for test assemblies.\nValues: None, Single, Multiple. If not specified, defaults to Single for a single assembly or Multiple for more than one.",
                v => DomainUsage = RequiredValue(v, "--domain", "None", "Single", "Multiple"));

            // How to Run Tests
            this.Add("framework=", "{FRAMEWORK} type/version to use for tests.\nExamples: mono, net-3.5, v4.0, 2.0, mono-4.0. If not specified, tests will run under the framework they are compiled with.",
                v => Framework = RequiredValue(v, "--framework"));

            this.Add("x86", "Run tests in an x86 process on 64 bit systems",
                v => RunAsX86 = v != null);

            this.Add("dispose-runners", "Dispose each test runner after it has finished running its tests.",
                v => DisposeRunners = v != null);

            this.Add("shadowcopy", "Shadow copy test files",
                v => ShadowCopyFiles = v != null);

            this.Add("loaduserprofile", "Load user profile in test runner processes",
                v => LoadUserProfile = v != null);

            this.Add("skipnontestassemblies", "Skip any non-test assemblies specified, without error.",
                v => SkipNonTestAssemblies = v != null);

            this.Add("agents=", "Specify the maximum {NUMBER} of test assembly agents to run at one time. If not specified, there is no limit.",
                v => _maxAgents = RequiredInt(v, "--agents"));

            this.Add("debug", "Launch debugger to debug tests.",
                v => DebugTests = v != null);

            this.Add("pause", "Pause before running to allow attaching a debugger.",
                v => PauseBeforeRun = v != null);

            this.Add("list-extensions", "List all extension points and the extensions for each.",
                v => ListExtensions = v != null);

            this.Add("set-principal-policy=", "Set PrincipalPolicy for the test domain.",
                v => PrincipalPolicy = RequiredValue(v, "--set-principal-policy", "UnauthenticatedPrincipal", "NoPrincipal", "WindowsPrincipal"));

#if DEBUG
            this.Add("debug-agent", "Launch debugger in nunit-agent when it starts.",
                v => DebugAgent = v != null);
#endif
        }

        private int _nesting = 0;

        public IEnumerable<string> PreParse(IEnumerable<string> args)
        {
            if (args == null) throw new ArgumentNullException("args");

            if (++_nesting > 3)
            {
                ErrorMessages.Add("Arguments file nesting exceeds maximum depth of 3.");
                --_nesting;
                return args;
            }

            var listArgs = new List<string>();

            foreach (var arg in args)
            {
                if (arg.Length == 0 || arg[0] != '@')
                {
                    listArgs.Add(arg);
                    continue;
                }

                var filename = arg.Substring(1, arg.Length - 1);
                if (string.IsNullOrEmpty(filename))
                {
                    ErrorMessages.Add("You must include a file name after @.");
                    continue;
                }

                if (!_fileSystem.FileExists(filename))
                {
                    ErrorMessages.Add("The file \"" + filename + "\" was not found.");
                    continue;
                }

                try
                {
                    listArgs.AddRange(PreParse(GetArgsFromFile(filename)));
                }
                catch (IOException ex)
                {
                    ErrorMessages.Add("Error reading \"" + filename + "\": " + ex.Message);
                }
            }

            --_nesting;
            return listArgs;
        }

        private static readonly Regex ArgsRegex = new Regex(@"\G(""((""""|[^""])+)""|(\S+)) *", RegexOptions.Compiled | RegexOptions.CultureInvariant);

        // Get args from a string of args
        internal static IEnumerable<string> GetArgs(string commandLine)
        {
            var ms = ArgsRegex.Matches(commandLine);
            foreach (Match m in ms)
                yield return Regex.Replace(m.Groups[2].Success ? m.Groups[2].Value : m.Groups[4].Value, @"""""", @"""");
        }

        // Get args from an included file
        private IEnumerable<string> GetArgsFromFile(string filename)
        {
            var sb = new StringBuilder();

            foreach (var line in _fileSystem.ReadLines(filename))
            {
                if (!string.IsNullOrEmpty(line) && line[0] != '#' && line.Trim().Length > 0)
                {
                    if (sb.Length > 0)
                        sb.Append(' ');
                    sb.Append(line);
                }
            }

            return GetArgs(sb.ToString());
        }

        private void ApplyTestParameter(string testParameterSpecification)
        {
            var equalsIndex = testParameterSpecification.IndexOf("=");

            if (equalsIndex <= 0 || equalsIndex == testParameterSpecification.Length - 1)
            {
                ErrorMessages.Add("Invalid format for test parameter. Use NAME=VALUE.");
            }
            else
            {
                string name = testParameterSpecification.Substring(0, equalsIndex);
                string value = testParameterSpecification.Substring(equalsIndex + 1);

                TestParameters[name] = value;
            }
        }

        private void ResolveOutputSpecification(string value, IList<OutputSpecification> outputSpecifications)
        {
            if (value == null)
                return;

            OutputSpecification spec;

            try
            {
                spec = new OutputSpecification(value, CURRENT_DIRECTORY_ON_ENTRY);
            }
            catch (ArgumentException e)
            {
                ErrorMessages.Add(e.Message);
                return;
            }

            if (spec.Transform != null)
            {
                if (!_fileSystem.FileExists(spec.Transform))
                {
                    ErrorMessages.Add($"Transform {spec.Transform} could not be found.");
                    return;
                }
            }

            outputSpecifications.Add(spec);
        }

        /// <summary>
        /// Case is ignored when val is compared to validValues. When a match is found, the
        /// returned value will be in the canonical case from validValues.
        /// </summary>
        private string RequiredValue(string val, string option, params string[] validValues)
        {
            if (string.IsNullOrEmpty(val))
                ErrorMessages.Add("Missing required value for option '" + option + "'.");

            bool isValid = true;

            if (validValues != null && validValues.Length > 0)
            {
                isValid = false;

                foreach (string valid in validValues)
                    if (string.Compare(valid, val, StringComparison.OrdinalIgnoreCase) == 0)
                        return valid;

            }

            if (!isValid)
                ErrorMessages.Add(string.Format("The value '{0}' is not valid for option '{1}'.", val, option));

            return val;
        }

        private int RequiredInt(string val, string option)
        {
            // We have to return something even though the value will
            // be ignored if an error is reported. The -1 value seems
            // like a safe bet in case it isn't ignored due to a bug.
            int result = -1;

            if (string.IsNullOrEmpty(val))
                ErrorMessages.Add("Missing required value for option '" + option + "'.");
            else
            {
                // NOTE: Don't replace this with TryParse or you'll break the CF build!
                try
                {
                    result = int.Parse(val);
                }
                catch (Exception)
                {
                    ErrorMessages.Add(String.Format("An int value was expected for option '{0}' but a value of '{1}' was used", option, val));
                }
            }

            return result;
        }

        private string ExpandToFullPath(string path)
        {
            if (path == null) return null;

            return Path.GetFullPath(path);
        }

    }
}
