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
using NUnit.Options;

namespace NUnit.Common
{
    /// <summary>
    /// CommandLineOptions is the base class the specific option classes
    /// used for nunit3-console and nunitlite. It encapsulates all common
    /// settings and features of both. This is done to ensure that common
    /// features remain common and for the convenience of having the code
    /// in a common location. The class inherits from the Mono
    /// Options OptionSet class and provides a central location
    /// for defining and parsing options.
    /// </summary>
    public class CommandLineOptions : OptionSet
    {
        private static readonly string CURRENT_DIRECTORY_ON_ENTRY = Environment.CurrentDirectory;

        private bool validated;
        private bool noresult;

        /// <summary>
        /// An abstraction of the file system
        /// </summary>
        protected readonly IFileSystem _fileSystem;

        #region Constructor

        internal CommandLineOptions(IDefaultOptionsProvider defaultOptionsProvider, IFileSystem fileSystem, params string[] args)
        {
            // Apply default options
            if (defaultOptionsProvider == null) throw new ArgumentNullException(nameof(defaultOptionsProvider));
            TeamCity = defaultOptionsProvider.TeamCity;

            if (fileSystem == null) throw new ArgumentNullException(nameof(fileSystem));
            _fileSystem = fileSystem;

            ConfigureOptions();
            if (args != null)
                Parse(args);
        }

        public CommandLineOptions(params string[] args)
        {
            ConfigureOptions();
            if (args != null)
                Parse(args);
        }

        #endregion

        #region Properties

        // Action to Perform

        public bool Explore { get; private set; }

        public bool ShowHelp { get; private set; }

        public bool ShowVersion { get; private set; }

        // Select tests

        public IList<string> InputFiles { get; } = new List<string>();

        public IList<string> TestList { get; } =  new List<string>();

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

        // Error Processing

        public IList<string> WarningMessages { get; } = new List<string>();

        public IList<string> ErrorMessages { get; } = new List<string>();

        #endregion

        #region Public Methods

        public bool Validate()
        {
            if (!validated)
            {
                CheckOptionCombinations();

                validated = true;
            }

            return ErrorMessages.Count == 0;
        }

        #endregion

        #region Helper Methods

        protected virtual void CheckOptionCombinations()
        {

        }

        /// <summary>
        /// Case is ignored when val is compared to validValues. When a match is found, the
        /// returned value will be in the canonical case from validValues.
        /// </summary>
        protected string RequiredValue(string val, string option, params string[] validValues)
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

        protected int RequiredInt(string val, string option)
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

        protected virtual void ConfigureOptions()
        {
            // NOTE: The order in which patterns are added
            // determines the display order for the help.

            // Select Tests
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

                    string parameters = RequiredValue( v, "--params");

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

        #endregion
    }
}
