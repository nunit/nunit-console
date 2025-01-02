// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using NUnit.Engine;

namespace NUnit.ConsoleRunner.Options
{
    /// <summary>
    /// <c>ConsoleOptions</c> encapsulates the option settings for
    /// the nunit4-console program. The class inherits from the Mono
    /// Options <see cref="OptionSet"/> class and provides a central location
    /// for defining and parsing options.
    /// </summary>
    public class ConsoleOptions : OptionSet
    {
        private static readonly string CURRENT_DIRECTORY_ON_ENTRY = Directory.GetCurrentDirectory();

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

        // Action to Perform

        public bool Explore { get; private set; }

        public bool ShowHelp { get; private set; }

        public bool ShowVersion { get; private set; }

        // Select tests

        public IList<string> InputFiles { get; } = new List<string>();

        public IList<string> TestList { get; } = new List<string>();

        public IDictionary<string, string> TestParameters { get; } = new Dictionary<string, string>();

        public string? WhereClause { get; private set; }

        [MemberNotNullWhen(true, nameof(WhereClause))]
        public bool WhereClauseSpecified { get { return WhereClause != null; } }

        public int DefaultTestCaseTimeout { get; private set; } = -1;
        public bool DefaultTestCaseTimeoutSpecified { get { return DefaultTestCaseTimeout >= 0; } }

        public int RandomSeed { get; private set; } = -1;
        public bool RandomSeedSpecified { get { return RandomSeed >= 0; } }

        public string? DefaultTestNamePattern { get; private set; }
        public int NumberOfTestWorkers { get; private set; } = -1;
        public bool NumberOfTestWorkersSpecified { get { return NumberOfTestWorkers >= 0; } }

        public bool StopOnError { get; private set; }

        public bool WaitBeforeExit { get; private set; }

        // Output Control

        public string? ConsoleEncoding { get; private set; }

        public bool NoHeader { get; private set; }

        public bool NoColor { get; private set; }

        public bool TeamCity { get; private set; }

        public string? OutFile { get; private set; }

        [MemberNotNullWhen(true, nameof(OutFile))]
        public bool OutFileSpecified { get { return OutFile != null; } }

        public string? DisplayTestLabels { get; private set; }

        private string? workDirectory = null;
        public string WorkDirectory
        {
            get { return workDirectory ?? CURRENT_DIRECTORY_ON_ENTRY; }
        }
        public bool WorkDirectorySpecified { get { return workDirectory != null; } }

        public string? InternalTraceLevel { get; private set; }

        [MemberNotNullWhen(true, nameof(InternalTraceLevel))]
        public bool InternalTraceLevelSpecified { get { return InternalTraceLevel != null; } }

        private readonly List<OutputSpecification> resultOutputSpecifications = new List<OutputSpecification>();
        public IList<OutputSpecification> ResultOutputSpecifications
        {
            get
            {
                if (NoResultSpecified)
                    return new OutputSpecification[0];

                if (resultOutputSpecifications.Count == 0)
                    resultOutputSpecifications.Add(
                        new OutputSpecification("TestResult.xml", CURRENT_DIRECTORY_ON_ENTRY));

                return resultOutputSpecifications;
            }
        }

        public bool NoResultSpecified { get; private set; }

        public IList<OutputSpecification> ExploreOutputSpecifications { get; } = new List<OutputSpecification>();

        public string? ActiveConfig { get; private set; }

        [MemberNotNullWhen(true, nameof(ActiveConfig))]
        public bool ActiveConfigSpecified { get { return ActiveConfig != null; } }

        // How to Run Tests

        public string? RuntimeFramework { get; private set; }

        [MemberNotNullWhen(true, nameof(RuntimeFramework))]
        public bool RuntimeFrameworkSpecified { get { return RuntimeFramework != null; } }

        public string? ConfigurationFile { get; private set; }

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

        public string? PrincipalPolicy { get; private set; }

        public IList<string> WarningMessages { get; } = new List<string>();

        public IList<string> ErrorMessages { get; } = new List<string>();

        private void ConfigureOptions()
        {
            var parser = new OptionParser(s => ErrorMessages.Add(s));

            // NOTE: The order in which patterns are added
            // determines the display order for the help.

            this.Add("test=", "Comma-separated list of {NAMES} of tests to run or explore. This option may be repeated.",
                v => ((List<string>)TestList).AddRange(TestNameParser.Parse(parser.RequiredValue(v, "--test"))));

            this.Add("testlist=", "File {PATH} containing a list of tests to run, one per line. This option may be repeated.",
                v =>
                {
                    string testListFile = parser.RequiredValue(v, "--testlist");

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
                                    var line = rdr.ReadLine()?.Trim();

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
                v => WhereClause = parser.RequiredValue(v, "--where"));

            this.Add("param|p=", "Followed by a key-value pair separated by an equals sign. Test code can access the value by name. This option may be repeated.",
                v =>
                {
                    var valuePair = parser.RequiredKeyValue(parser.RequiredValue(v, "--param"));
                    if (valuePair.HasValue)
                    {
                        TestParameters[valuePair.Value.Key] = valuePair.Value.Value;
                    }
                });

            this.Add("testCaseTimeout=", "Set timeout for each test case in {MILLISECONDS}. May be overridden with TimeoutAttribute.",
                v => DefaultTestCaseTimeout = parser.RequiredInt(v, "--testCaseTimeout"));

            this.Add("seed=", "Set the random {SEED} used to generate test cases.",
                v => RandomSeed = parser.RequiredInt(v, "--seed"));

            this.Add("workers=", "Specify the {NUMBER} of worker threads to be used in running tests. If not specified, defaults to 2 or the number of processors, whichever is greater.",
                v => NumberOfTestWorkers = parser.RequiredInt(v, "--workers"));

            this.Add("stoponerror", "Stop run immediately upon any test failure or error.",
                v => StopOnError = !string.IsNullOrEmpty(v));

            this.Add("wait", "Wait for input before closing console window.",
                v => WaitBeforeExit = !string.IsNullOrEmpty(v));

            // Output Control
            this.Add("work=", "{PATH} of the directory to use for output files. If not specified, defaults to the current directory.",
                v => workDirectory = parser.RequiredValue(v, "--work"));

            this.Add("output|out=", "File {PATH} to contain text output from the tests.",
                v => OutFile = parser.RequiredValue(v, "--output"));

            this.Add("result=", "An output {SPEC} for saving the test results.\nThis option may be repeated.",
                v =>
                {
                    var spec = parser.ResolveOutputSpecification(parser.RequiredValue(v, "--resultxml"), resultOutputSpecifications, _fileSystem, CURRENT_DIRECTORY_ON_ENTRY);
                    if (spec != null) resultOutputSpecifications.Add(spec);
                });

            this.Add("explore:", "Display or save test info rather than running tests. Optionally provide an output {SPEC} for saving the test info. This option may be repeated.", v =>
            {
                Explore = true;
                var spec = parser.ResolveOutputSpecification(v, ExploreOutputSpecifications, _fileSystem, CURRENT_DIRECTORY_ON_ENTRY);
                if (spec != null) ExploreOutputSpecifications.Add(spec);
            });

            this.Add("noresult", "Don't save any test results.",
                v => NoResultSpecified = !string.IsNullOrEmpty(v));

            this.Add("labels=", "Specify whether to write test case names to the output. Values: Off (Default), On, OnOutput, Before, After, BeforeAndAfter. On is currently an alias for OnOutput, but is subject to change.",
                v => {
                        DisplayTestLabels = parser.RequiredValue(v, "--labels", "Off", "On", "OnOutput", "Before", "After", "BeforeAndAfter");
                });

            this.Add("test-name-format=", "Non-standard naming pattern to use in generating test names.",
                v => DefaultTestNamePattern = parser.RequiredValue(v, "--test-name-format"));

            this.Add("trace=", "Set internal trace {LEVEL}.\nValues: Off, Error, Warning, Info, Verbose (Debug)",
                v => InternalTraceLevel = parser.RequiredValue(v, "--trace", "Off", "Error", "Warning", "Info", "Verbose", "Debug"));

            this.Add("teamcity", "Turns on use of TeamCity service messages. TeamCity engine extension is required.",
                v => TeamCity = !string.IsNullOrEmpty(v));

            this.Add("noheader|noh", "Suppress display of program information at start of run.",
                v => NoHeader = !string.IsNullOrEmpty(v));

            this.Add("nocolor|noc", "Displays console output without color.",
                v => NoColor = !string.IsNullOrEmpty(v));

            this.Add("help|h", "Display this message and exit.",
                v => ShowHelp = !string.IsNullOrEmpty(v));

            this.Add("version|V", "Display the header and exit.",
                v => ShowVersion = !string.IsNullOrEmpty(v));

            this.Add("encoding=", "Specifies the encoding to use for Console standard output, for example utf-8, ascii, unicode.",
                v => ConsoleEncoding = parser.RequiredValue(v, "--encoding"));

            // Default
            this.Add("<>", v =>
            {
                if (v.StartsWith("-") || v.StartsWith("/") && Path.DirectorySeparatorChar != '/')
                    ErrorMessages.Add("Invalid argument: " + v);
                else
                    InputFiles.Add(v);
            });

            this.Add("config=", "{NAME} of a project configuration to load (e.g.: Debug).",
                v => ActiveConfig = parser.RequiredValue(v, "--config"));

            this.AddNetFxOnlyOption("configfile=", "{NAME} of configuration file to use for this run.",
                NetFxOnlyOption("configfile=", v => ConfigurationFile = parser.RequiredValue(v, "--configfile")));

            // How to Run Tests
            this.AddNetFxOnlyOption("framework=", "{FRAMEWORK} type/version to use for tests.\nExamples: mono, net-3.5, v4.0, 2.0, mono-4.0. If not specified, tests will run under the framework they are compiled with.",
                NetFxOnlyOption("framework=", v => RuntimeFramework = parser.RequiredValue(v, "--framework")));

            this.AddNetFxOnlyOption("x86", "Run tests in an x86 process on 64 bit systems",
                NetFxOnlyOption("x86", v => RunAsX86 = !string.IsNullOrEmpty(v)));

            this.Add("dispose-runners", "Dispose each test runner after it has finished running its tests.",
                v => DisposeRunners = !string.IsNullOrEmpty(v));

            this.AddNetFxOnlyOption("shadowcopy", "Shadow copy test files",
                NetFxOnlyOption("shadowcopy", v => ShadowCopyFiles = !string.IsNullOrEmpty(v)));

            this.AddNetFxOnlyOption("loaduserprofile", "Load user profile in test runner processes",
                NetFxOnlyOption("loaduserprofile", v => LoadUserProfile = !string.IsNullOrEmpty(v)));

            this.Add("skipnontestassemblies", "Skip any non-test assemblies specified, without error.",
                v => SkipNonTestAssemblies = !string.IsNullOrEmpty(v));

            this.AddNetFxOnlyOption("agents=", "Specify the maximum {NUMBER} of test assembly agents to run at one time. If not specified, there is no limit.",
                NetFxOnlyOption("agents=", v => _maxAgents = parser.RequiredInt(v, "--agents")));

            this.AddNetFxOnlyOption("debug", "Launch debugger to debug tests.",
                NetFxOnlyOption("debug", v => DebugTests = !string.IsNullOrEmpty(v)));

            this.AddNetFxOnlyOption("pause", "Pause before running to allow attaching a debugger.",
                NetFxOnlyOption("pause", v => PauseBeforeRun = !string.IsNullOrEmpty(v)));

            this.Add("list-extensions", "List all extension points and the extensions for each.",
                v => ListExtensions = !string.IsNullOrEmpty(v));

            this.AddNetFxOnlyOption("set-principal-policy=", "Set PrincipalPolicy for the test domain.",
                NetFxOnlyOption("set-principal-policy=", v => PrincipalPolicy = parser.RequiredValue(v, "--set-principal-policy", "UnauthenticatedPrincipal", "NoPrincipal", "WindowsPrincipal")));

#if DEBUG
            this.AddNetFxOnlyOption("debug-agent", "Launch debugger in nunit-agent when it starts.",
                NetFxOnlyOption("debug-agent", v => DebugAgent = !string.IsNullOrEmpty(v)));
#endif
        }

        private void AddNetFxOnlyOption(string prototype, string description, Action<string> action)
        {
#if NETFRAMEWORK
            var isHidden = false;
#else
            var isHidden = true;
#endif
            this.Add(prototype, description, action, isHidden);
        }

        private Action<string> NetFxOnlyOption(string optionName, Action<string> action)
        {
#if NETFRAMEWORK
            return action;
#else
            return s => ErrorMessages.Add($"The {optionName} option is not available on this platform.");
#endif
        }

        private void CheckOptionCombinations()
        {
            // Add any validation of option combinations here
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

        private string? ExpandToFullPath(string path)
        {
            if (path == null) return null;

            return Path.GetFullPath(path);
        }

    }
}
