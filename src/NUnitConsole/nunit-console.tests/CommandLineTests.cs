// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using System;
using System.IO;
using System.Reflection;
using NUnit.Common;
using System.Collections.Generic;
using NUnit.Framework;

using NUnit.ConsoleRunner.Options;

namespace NUnit.ConsoleRunner
{
    [TestFixture]
    public class CommandLineTests
    {
        [TestCase("--arg", "--arg")]
        [TestCase("--ArG", "--ArG")]
        [TestCase("--arg1 --arg2", "--arg1", "--arg2")]
        [TestCase("--arg1 data --arg2", "--arg1", "data", "--arg2")]
        [TestCase("")]
        [TestCase("   ")]
        [TestCase("\"--arg 1\" --arg2", "--arg 1", "--arg2")]
        [TestCase("--arg1 \"--arg 2\"", "--arg1", "--arg 2")]
        [TestCase("\"--arg 1\" \"--arg 2\"", "--arg 1", "--arg 2")]
        [TestCase("\"--arg 1\" \"--arg 2\" arg3 \"arg 4\"", "--arg 1", "--arg 2", "arg3", "arg 4")]
        [TestCase("--arg1 \"--arg 2\" arg3 \"arg 4\"", "--arg1", "--arg 2", "arg3", "arg 4")]
        [TestCase("\"--arg 1\" \"--arg 2\" arg3 \"arg 4\" \"--arg 1\" \"--arg 2\" arg3 \"arg 4\"",
            "--arg 1", "--arg 2", "arg3", "arg 4", "--arg 1", "--arg 2", "arg3", "arg 4")]
        [TestCase("\"--arg\"", "--arg")]
        [TestCase("\"--arg 1\"", "--arg 1")]
        [TestCase("\"--arg abc\"", "--arg abc")]
        [TestCase("\"--arg   abc\"", "--arg   abc")]
        [TestCase("\" --arg   abc \"", " --arg   abc ")]
        [TestCase("\"--arg=abc\"", "--arg=abc")]
        [TestCase("\"--arg=aBc\"", "--arg=aBc")]
        [TestCase("\"--arg = abc\"", "--arg = abc")]
        [TestCase("\"--arg=abc,xyz\"", "--arg=abc,xyz")]
        [TestCase("\"--arg=abc, xyz\"", "--arg=abc, xyz")]
        [TestCase("\"@arg = ~ ` ! @ # $ % ^ & * ( ) _ - : ; + ' ' { } [ ] | \\ ? / . , , xYz\"",
            "@arg = ~ ` ! @ # $ % ^ & * ( ) _ - : ; + ' ' { } [ ] | \\ ? / . , , xYz")]
        public void GetArgsFromCommandLine(string cmdline, params string[] expectedArgs)
        {
            var actualArgs = ConsoleOptions.GetArgs(cmdline);

            Assert.That(actualArgs, Is.EqualTo(expectedArgs));
        }

        [TestCase("--arg1 @file1.txt --arg2", "file1.txt:--filearg1 --filearg2", "--arg1", "--filearg1", "--filearg2", "--arg2")]
        [TestCase("--arg1 @file1.txt --arg2", "file1.txt:--fileArg1\n--fileArg2", "--arg1", "--fileArg1", "--fileArg2", "--arg2")]
        [TestCase("--arg1 @file1.txt --arg2", "file1.txt:--filearg1 data", "--arg1", "--filearg1", "data", "--arg2")]
        [TestCase("--arg1 @file1.txt --arg2", "file1.txt:--filearg1 \"data in quotes\"", "--arg1", "--filearg1", "data in quotes", "--arg2")]
        [TestCase("--arg1 @file1.txt --arg2", "file1.txt:--filearg1 \"data in quotes with 'single' quotes\"", "--arg1", "--filearg1", "data in quotes with 'single' quotes", "--arg2")]
        [TestCase("--arg1 @file1.txt --arg2", "file1.txt:--filearg1 \"data in quotes with /slashes/\"", "--arg1", "--filearg1", "data in quotes with /slashes/", "--arg2")]
        [TestCase("--arg1 @file1.txt --arg2 @file2.txt", "file1.txt:--fileArg1\n--fileArg2,file2.txt:--fileArg3", "--arg1", "--fileArg1", "--fileArg2", "--arg2", "--fileArg3")]
        [TestCase("--arg1 @file1.txt --arg2", "file1.txt:", "--arg1", "--arg2")]
        // Blank lines
        [TestCase("--arg1 @file1.txt --arg2", "file1.txt:--fileArg1\n\n\n--fileArg2", "--arg1", "--fileArg1", "--fileArg2", "--arg2")]
        [TestCase("--arg1 @file1.txt --arg2", "file1.txt:--fileArg1\n    \n\t\t\n--fileArg2", "--arg1", "--fileArg1", "--fileArg2", "--arg2")]
        [TestCase("--arg1 @file1.txt --arg2", "file1.txt:--fileArg1\r\n\r\n\r\n--fileArg2", "--arg1", "--fileArg1", "--fileArg2", "--arg2")]
        [TestCase("--arg1 @file1.txt --arg2", "file1.txt:--fileArg1\r\n    \r\n\t\t\r\n--fileArg2", "--arg1", "--fileArg1", "--fileArg2", "--arg2")]
        [TestCase("--arg1 @file1.txt --arg2", "file1.txt:--filearg1 --filearg2\r\n\n--filearg3 --filearg4", "--arg1", "--filearg1", "--filearg2", "--filearg3", "--filearg4", "--arg2")]
        // Comments
        [TestCase("--arg1 @file1.txt --arg2", "file1.txt:--fileArg1\nThis is NOT treated as a COMMENT\n--fileArg2", "--arg1", "--fileArg1", "This", "is", "NOT", "treated", "as", "a", "COMMENT", "--fileArg2", "--arg2")]
        [TestCase("--arg1 @file1.txt --arg2", "file1.txt:--fileArg1\n#This is treated as a COMMENT\n--fileArg2", "--arg1", "--fileArg1", "--fileArg2", "--arg2")]
        // Nesting of files
        [TestCase("--arg1 @file1.txt --arg2 @file2.txt", "file1.txt:--filearg1 --filearg2,file2.txt:--filearg3 @file3.txt,file3.txt:--filearg4", "--arg1", "--filearg1", "--filearg2", "--arg2", "--filearg3", "--filearg4")]
        // Where clauses
        [TestCase("testfile.dll @file1.txt --arg2", "file1.txt:--where test==somelongname", "testfile.dll", "--where", "test==somelongname", "--arg2")]
        // NOTE: The next is not valid. Where clause is spread over several args and therefore won't parse. Quotes are required.
        [TestCase("testfile.dll @file1.txt --arg2",
            "file1.txt:--where test == somelongname",
            "testfile.dll", "--where", "test", "==", "somelongname", "--arg2")]
        [TestCase("testfile.dll @file1.txt --arg2",
            "file1.txt:--where \"test == somelongname\"",
            "testfile.dll", "--where", "test == somelongname", "--arg2")]
        [TestCase("testfile.dll @file1.txt --arg2",
            "file1.txt:--where\n    \"test == somelongname\"",
            "testfile.dll", "--where", "test == somelongname", "--arg2")]
        [TestCase("testfile.dll @file1.txt --arg2",
            "file1.txt:--where\n    \"test == somelongname or test == /another long name/ or cat == SomeCategory\"",
            "testfile.dll", "--where", "test == somelongname or test == /another long name/ or cat == SomeCategory", "--arg2")]
        [TestCase("testfile.dll @file1.txt --arg2",
            "file1.txt:--where\n    \"test == somelongname or\ntest == /another long name/ or\ncat == SomeCategory\"",
            "testfile.dll", "--where", "test == somelongname or test == /another long name/ or cat == SomeCategory", "--arg2")]
        [TestCase("testfile.dll @file1.txt --arg2",
            "file1.txt:--where\n    \"test == somelongname ||\ntest == /another long name/ ||\ncat == SomeCategory\"",
            "testfile.dll", "--where", "test == somelongname || test == /another long name/ || cat == SomeCategory", "--arg2")]
        public void GetArgsFromFiles(string commandline, string files, params string[] expectedArgs)
        {
            // Given
            var fileSystem = new VirtualFileSystem();
            fileSystem.SetupFiles(files);

            var options = new ConsoleOptions(new DefaultOptionsProviderStub(false), fileSystem);

            // When
            var expandedArgs = options.PreParse(commandline.Split(' '));

            // Then
            Assert.That(expandedArgs, Is.EqualTo(expectedArgs));
            Assert.IsEmpty(options.ErrorMessages);
        }

        [TestCase("--arg1 @file1.txt --arg2", "The file \"file1.txt\" was not found.")]
        [TestCase("--arg1 @ --arg2", "You must include a file name after @.")]
        public void GetArgsFromFiles_FailureTests(string args, string errorMessage)
        {
            var options = new ConsoleOptions(new DefaultOptionsProviderStub(false), new VirtualFileSystem());

            options.PreParse(args.Split(' '));

            Assert.That(options.ErrorMessages, Is.EqualTo(new object[] { errorMessage }));
        }

        [Test]
        public void GetArgsFromFiles_NestingOverflow()
        {
            var fileSystem = new VirtualFileSystem();
            var lines = new string[] { "@file1.txt" };
            fileSystem.SetupFile("file1.txt", lines);
            var options = new ConsoleOptions(new DefaultOptionsProviderStub(false), fileSystem);
            var expectedErrors = new string[] { "Arguments file nesting exceeds maximum depth of 3." };

            var arglist = options.PreParse(lines);

            Assert.That(arglist, Is.EqualTo(lines));
            Assert.That(options.ErrorMessages, Is.EqualTo(expectedErrors));
        }

        [Test]
        public void NoInputFiles()
        {
            ConsoleOptions options = ConsoleMocks.Options();
            Assert.That(options.Validate(), Is.True);
            Assert.That(options.InputFiles.Count, Is.EqualTo(0));
        }

        [TestCase("ShowHelp", "help|h")]
        [TestCase("ShowVersion", "version|V")]
        [TestCase("StopOnError", "stoponerror")]
        [TestCase("WaitBeforeExit", "wait")]
        [TestCase("NoHeader", "noheader|noh")]
        [TestCase("DisposeRunners", "dispose-runners")]
        [TestCase("TeamCity", "teamcity")]
        [TestCase("SkipNonTestAssemblies", "skipnontestassemblies")]
#if NET35
        [TestCase("RunAsX86", "x86")]
        [TestCase("ShadowCopyFiles", "shadowcopy")]
        [TestCase("DebugTests", "debug")]
        [TestCase("PauseBeforeRun", "pause")]
        [TestCase("LoadUserProfile", "loaduserprofile")]
#if DEBUG
        [TestCase("DebugAgent", "debug-agent")]
#endif
#endif
        public void CanRecognizeBooleanOptions(string propertyName, string pattern)
        {
            string[] prototypes = pattern.Split('|');

            PropertyInfo property = GetPropertyInfo(propertyName);
            Assert.That(property.PropertyType, Is.EqualTo(typeof(bool)), "Property '{0}' is wrong type", propertyName);

            foreach (string option in prototypes)
            {
                ConsoleOptions options;

                if (option.Length == 1)
                {
                    options = ConsoleMocks.Options("-" + option);
                    Assert.That((bool)property.GetValue(options, null), Is.EqualTo(true), "Didn't recognize -" + option);

                    options = ConsoleMocks.Options("-" + option + "+");
                    Assert.That((bool)property.GetValue(options, null), Is.EqualTo(true), "Didn't recognize -" + option + "+");

                    options = ConsoleMocks.Options("-" + option + "-");
                    Assert.That((bool)property.GetValue(options, null), Is.EqualTo(false), "Didn't recognize -" + option + "-");
                }
                else
                {
                    options = ConsoleMocks.Options("--" + option);
                    Assert.That((bool)property.GetValue(options, null), Is.EqualTo(true), "Didn't recognize --" + option);
                }

                options = ConsoleMocks.Options("/" + option);
                Assert.That((bool)property.GetValue(options, null), Is.EqualTo(true), "Didn't recognize /" + option);
            }
        }

        [TestCase("WhereClause", "where", new string[] { "cat==Fast" }, new string[0])]
        [TestCase("ActiveConfig", "config", new string[] { "Debug" }, new string[0])]
        [TestCase("OutFile", "output|out", new string[] { "output.txt" }, new string[0])]
        [TestCase("WorkDirectory", "work", new string[] { "results" }, new string[0])]
        [TestCase("DisplayTestLabels", "labels", new string[] { "Off", "On", "OnOutput", "Before", "After", "BeforeAndAfter" }, new string[] { "JUNK" })]
        [TestCase("InternalTraceLevel", "trace", new string[] { "Off", "Error", "Warning", "Info", "Debug", "Verbose" }, new string[] { "JUNK" })]
        [TestCase("DefaultTestNamePattern", "test-name-format", new string[] { "{m}{a}" }, new string[0])]
        [TestCase("ConsoleEncoding", "encoding", new string[] { "utf-8", "ascii", "unicode" }, new string[0])]
#if NET35
        [TestCase("Framework", "framework", new string[] { "net-4.0" }, new string[0])]
        [TestCase("ConfigurationFile", "configfile", new string[] { "mytest.config" }, new string[0] )]
        [TestCase("PrincipalPolicy", "set-principal-policy", new string[] { "UnauthenticatedPrincipal", "NoPrincipal", "WindowsPrincipal" }, new string[] { "JUNK" })]
#endif
        public void CanRecognizeStringOptions(string propertyName, string pattern, string[] goodValues, string[] badValues)
        {
            string[] prototypes = pattern.Split('|');

            PropertyInfo property = GetPropertyInfo(propertyName);
            Assert.That(property.PropertyType, Is.EqualTo(typeof(string)));

            foreach (string option in prototypes)
            {
                foreach (string value in goodValues)
                {
                    string optionPlusValue = string.Format("--{0}:{1}", option, value);
                    ConsoleOptions options = ConsoleMocks.Options(optionPlusValue);
                    Assert.That(options.Validate(), Is.True, "Should be valid: " + optionPlusValue);
                    Assert.That((string)property.GetValue(options, null), Is.EqualTo(value), "Didn't recognize " + optionPlusValue);
                }

                foreach (string value in badValues)
                {
                    string optionPlusValue = string.Format("--{0}:{1}", option, value);
                    ConsoleOptions options = ConsoleMocks.Options(optionPlusValue);
                    Assert.That(options.Validate(), Is.False, "Should not be valid: " + optionPlusValue);
                }
            }
        }

        [TestCase("DisplayTestLabels", "labels", new string[] { "Off", "On", "OnOutput", "Before", "After", "BeforeAndAfter" })]
        [TestCase("InternalTraceLevel", "trace", new string[] { "Off", "Error", "Warning", "Info", "Debug", "Verbose" })]
        public void CanRecognizeLowerCaseOptionValues(string propertyName, string optionName, string[] canonicalValues)
        {
            PropertyInfo property = GetPropertyInfo(propertyName);
            Assert.That(property.PropertyType, Is.EqualTo(typeof(string)));

            foreach (string canonicalValue in canonicalValues)
            {
                string lowercaseValue = canonicalValue.ToLowerInvariant();
                string optionPlusValue = string.Format("--{0}:{1}", optionName, lowercaseValue);
                ConsoleOptions options = ConsoleMocks.Options(optionPlusValue);
                Assert.That(options.Validate(), Is.True, "Should be valid: " + optionPlusValue);
                Assert.That((string)property.GetValue(options, null), Is.EqualTo(canonicalValue), "Didn't recognize " + optionPlusValue);
            }
        }

        [TestCase("DefaultTestCaseTimeout", "testCaseTimeout")]
        [TestCase("RandomSeed", "seed")]
        [TestCase("NumberOfTestWorkers", "workers")]
#if NET35
        [TestCase("MaxAgents", "agents")]
#endif
        public void CanRecognizeIntOptions(string propertyName, string pattern)
        {
            string[] prototypes = pattern.Split('|');

            PropertyInfo property = GetPropertyInfo(propertyName);
            Assert.That(property.PropertyType, Is.EqualTo(typeof(int)));

            foreach (string option in prototypes)
            {
                ConsoleOptions options = ConsoleMocks.Options("--" + option + ":42");
                Assert.That((int)property.GetValue(options, null), Is.EqualTo(42), "Didn't recognize --" + option + ":42");
            }
        }

        [TestCase("--where")]
        [TestCase("--config")]
        [TestCase("--testCaseTimeout")]
        [TestCase("--output")]
        [TestCase("--work")]
        [TestCase("--trace")]
        [TestCase("--test-name-format")]
        [TestCase("--param")]
        [TestCase("--encoding")]
#if NET35
        [TestCase("--framework")]
#endif
        public void MissingValuesAreReported(string option)
        {
            ConsoleOptions options = ConsoleMocks.Options(option + "=");
            Assert.That(options.Validate(), Is.False, "Missing value should not be valid");
            Assert.That(options.ErrorMessages[0], Is.EqualTo("Missing required value for option '" + option + "'."));
        }

        [Test]
        public void AssemblyName()
        {
            ConsoleOptions options = ConsoleMocks.Options("nunit.tests.dll");
            Assert.That(options.Validate(), Is.True);
            Assert.That(options.InputFiles.Count, Is.EqualTo(1));
            Assert.That(options.InputFiles[0], Is.EqualTo("nunit.tests.dll"));
        }

        [Test]
        public void AssemblyAloneIsValid()
        {
            ConsoleOptions options = ConsoleMocks.Options("nunit.tests.dll");
            Assert.That(options.Validate(), Is.True);
            Assert.That(options.ErrorMessages.Count, Is.EqualTo(0), "command line should be valid");
        }

        [Test]
        public void InvalidOption()
        {
            ConsoleOptions options = ConsoleMocks.Options("-assembly:nunit.tests.dll");
            Assert.That(options.Validate(), Is.False);
            Assert.That(options.ErrorMessages.Count, Is.EqualTo(1));
            Assert.That(options.ErrorMessages[0], Is.EqualTo("Invalid argument: -assembly:nunit.tests.dll"));
        }

        [Test]
        public void InvalidCommandLineParms()
        {
            ConsoleOptions options = ConsoleMocks.Options("-garbage:TestFixture", "-assembly:Tests.dll");
            Assert.That(options.Validate(), Is.False);
            Assert.That(options.ErrorMessages.Count, Is.EqualTo(2));
            Assert.That(options.ErrorMessages[0], Is.EqualTo("Invalid argument: -garbage:TestFixture"));
            Assert.That(options.ErrorMessages[1], Is.EqualTo("Invalid argument: -assembly:Tests.dll"));
        }

        [Test]
        public void TimeoutIsMinusOneIfNoOptionIsProvided()
        {
            ConsoleOptions options = ConsoleMocks.Options("tests.dll");
            Assert.That(options.Validate(), Is.True);
            Assert.That(options.DefaultTestCaseTimeout, Is.EqualTo(-1));
        }

        [Test]
        public void TimeoutThrowsExceptionIfOptionHasNoValue()
        {
            Assert.Throws<OptionException>(() => ConsoleMocks.Options("tests.dll", "-testCaseTimeout"));
        }

        [Test]
        public void TimeoutParsesIntValueCorrectly()
        {
            ConsoleOptions options = ConsoleMocks.Options("tests.dll", "-testCaseTimeout:5000");
            Assert.That(options.Validate(), Is.True);
            Assert.That(options.DefaultTestCaseTimeout, Is.EqualTo(5000));
        }

        [Test]
        public void TimeoutCausesErrorIfValueIsNotInteger()
        {
            ConsoleOptions options = ConsoleMocks.Options("tests.dll", "-testCaseTimeout:abc");
            Assert.That(options.Validate(), Is.False);
            Assert.That(options.DefaultTestCaseTimeout, Is.EqualTo(-1));
        }

        [Test]
        public void ResultOptionWithFilePath()
        {
            ConsoleOptions options = ConsoleMocks.Options("tests.dll", "-result:results.xml");
            Assert.That(options.Validate(), Is.True);
            Assert.That(options.InputFiles.Count, Is.EqualTo(1), "assembly should be set");
            Assert.That(options.InputFiles[0], Is.EqualTo("tests.dll"));

            OutputSpecification spec = options.ResultOutputSpecifications[0];
            Assert.That(spec.OutputPath, Is.EqualTo("results.xml"));
            Assert.That(spec.Format, Is.EqualTo("nunit3"));
            Assert.Null(spec.Transform);
        }

        [Test]
        public void ResultOptionWithFilePathAndFormat()
        {
            ConsoleOptions options = ConsoleMocks.Options("tests.dll", "-result:results.xml;format=nunit2");
            Assert.That(options.Validate(), Is.True);
            Assert.That(options.InputFiles.Count, Is.EqualTo(1), "assembly should be set");
            Assert.That(options.InputFiles[0], Is.EqualTo("tests.dll"));

            OutputSpecification spec = options.ResultOutputSpecifications[0];
            Assert.That(spec.OutputPath, Is.EqualTo("results.xml"));
            Assert.That(spec.Format, Is.EqualTo("nunit2"));
            Assert.Null(spec.Transform);
        }

        [Test]
        public void ResultOptionWithFilePathAndTransform()
        {
            string transformFile = Path.Combine(TestContext.CurrentContext.TestDirectory, "TextSummary.xslt");
            IFileSystem fileSystem = GetFileSystemContainingFile(transformFile);

            ConsoleOptions options = new ConsoleOptions(
                new DefaultOptionsProviderStub(false),
                fileSystem,
                "tests.dll", $"-result:results.xml;transform={transformFile}");
            Assert.That(options.Validate(), Is.True);
            Assert.That(options.InputFiles.Count, Is.EqualTo(1), "assembly should be set");
            Assert.That(options.InputFiles[0], Is.EqualTo("tests.dll"));

            OutputSpecification spec = options.ResultOutputSpecifications[0];
            Assert.That(spec.OutputPath, Is.EqualTo("results.xml"));
            Assert.That(spec.Format, Is.EqualTo("user"));
            var fullFilePath = Path.Combine(TestContext.CurrentContext.TestDirectory, transformFile);
            Assert.That(spec.Transform, Is.EqualTo(fullFilePath));
        }

        [Test]
        public void FileNameWithoutResultOptionLooksLikeParameter()
        {
            ConsoleOptions options = ConsoleMocks.Options("tests.dll", "results.xml");
            Assert.That(options.Validate(), Is.True);
            Assert.That(options.ErrorMessages.Count, Is.EqualTo(0));
            Assert.That(options.InputFiles.Count, Is.EqualTo(2));
        }

        [Test]
        public void ResultOptionWithoutFileNameIsInvalid()
        {
            ConsoleOptions options = ConsoleMocks.Options("tests.dll", "-result:");
            Assert.That(options.Validate(), Is.False, "Should not be valid");
            Assert.That(options.ErrorMessages.Count, Is.EqualTo(1), "An error was expected");
        }

        [Test]
        public void ResultOptionMayBeRepeated()
        {
            string transformFile = Path.Combine(TestContext.CurrentContext.TestDirectory, "TextSummary.xslt");
            IFileSystem fileSystem = GetFileSystemContainingFile(transformFile);

            ConsoleOptions options = new ConsoleOptions(
                new DefaultOptionsProviderStub(false),
                fileSystem,
                "tests.dll", "-result:results.xml", "-result:nunit2results.xml;format=nunit2", $"-result:myresult.xml;transform={transformFile}");
            Assert.That(options.Validate(), Is.True, "Should be valid");

            var specs = options.ResultOutputSpecifications;
            Assert.That(specs.Count, Is.EqualTo(3));

            var spec1 = specs[0];
            Assert.That(spec1.OutputPath, Is.EqualTo("results.xml"));
            Assert.That(spec1.Format, Is.EqualTo("nunit3"));
            Assert.Null(spec1.Transform);

            var spec2 = specs[1];
            Assert.That(spec2.OutputPath, Is.EqualTo("nunit2results.xml"));
            Assert.That(spec2.Format, Is.EqualTo("nunit2"));
            Assert.Null(spec2.Transform);

            var spec3 = specs[2];
            Assert.That(spec3.OutputPath, Is.EqualTo("myresult.xml"));
            Assert.That(spec3.Format, Is.EqualTo("user"));
            var fullFilePath = Path.Combine(TestContext.CurrentContext.TestDirectory, transformFile);
            Assert.That(spec3.Transform, Is.EqualTo(fullFilePath));
        }

        [Test]
        public void DefaultResultSpecification()
        {
            var options = ConsoleMocks.Options("test.dll");
            Assert.That(options.ResultOutputSpecifications.Count, Is.EqualTo(1));

            var spec = options.ResultOutputSpecifications[0];
            Assert.That(spec.OutputPath, Is.EqualTo("TestResult.xml"));
            Assert.That(spec.Format, Is.EqualTo("nunit3"));
            Assert.Null(spec.Transform);
        }

        [Test]
        public void NoResultSuppressesDefaultResultSpecification()
        {
            var options = ConsoleMocks.Options("test.dll", "-noresult");
            Assert.That(options.ResultOutputSpecifications.Count, Is.EqualTo(0));
        }

        [Test]
        public void NoResultSuppressesAllResultSpecifications()
        {
            var options = ConsoleMocks.Options("test.dll", "-result:results.xml", "-noresult", "-result:nunit2results.xml;format=nunit2");
            Assert.That(options.ResultOutputSpecifications.Count, Is.EqualTo(0));
        }

        [Test]
        public void InvalidResultSpecRecordsError()
        {
            var options = ConsoleMocks.Options("test.dll", "-result:userspecifed.xml;format=nunit2;format=nunit3");
            Assert.That(options.ResultOutputSpecifications, Has.Exactly(1).Items
                .And.Exactly(1).Property(nameof(OutputSpecification.OutputPath)).EqualTo("TestResult.xml"));
            Assert.That(options.ErrorMessages, Has.Exactly(1).Contains("conflicting format options").IgnoreCase);
        }

        [Test]
        public void MissingXsltFileRecordsError()
        {
            const string missingXslt = "missing.xslt";

            var options = new ConsoleOptions(
                new DefaultOptionsProviderStub(false),
                new VirtualFileSystem(),
                "test.dll", $"-result:userspecifed.xml;transform={missingXslt}");
            Assert.That(options.ResultOutputSpecifications, Has.Exactly(1).Items
                                                               .And.Exactly(1).Property(nameof(OutputSpecification.Transform)).Null);
            Assert.That(options.ErrorMessages, Has.Exactly(1).Contains($"{missingXslt} could not be found").IgnoreCase);
        }

        [Test]
        public void ExploreOptionWithoutPath()
        {
            ConsoleOptions options = ConsoleMocks.Options("tests.dll", "-explore");
            Assert.That(options.Validate(), Is.True);
            Assert.That(options.Explore, Is.True);
        }

        [Test]
        public void ExploreOptionWithFilePath()
        {
            ConsoleOptions options = ConsoleMocks.Options("tests.dll", "-explore:results.xml");
            Assert.That(options.Validate(), Is.True);
            Assert.That(options.InputFiles.Count, Is.EqualTo(1), "assembly should be set");
            Assert.That(options.InputFiles[0], Is.EqualTo("tests.dll"));
            Assert.That(options.Explore, Is.True);

            OutputSpecification spec = options.ExploreOutputSpecifications[0];
            Assert.That(spec.OutputPath, Is.EqualTo("results.xml"));
            Assert.That(spec.Format, Is.EqualTo("nunit3"));
            Assert.Null(spec.Transform);
        }

        [Test]
        public void ExploreOptionWithFilePathAndFormat()
        {
            ConsoleOptions options = ConsoleMocks.Options("tests.dll", "-explore:results.xml;format=cases");
            Assert.That(options.Validate(), Is.True);
            Assert.That(options.InputFiles.Count, Is.EqualTo(1), "assembly should be set");
            Assert.That(options.InputFiles[0], Is.EqualTo("tests.dll"));
            Assert.That(options.Explore, Is.True);

            OutputSpecification spec = options.ExploreOutputSpecifications[0];
            Assert.That(spec.OutputPath, Is.EqualTo("results.xml"));
            Assert.That(spec.Format, Is.EqualTo("cases"));
            Assert.Null(spec.Transform);
        }

        [Test]
        public void ExploreOptionWithFilePathAndTransform()
        {
            string transformFile = Path.Combine(TestContext.CurrentContext.TestDirectory, "TextSummary.xslt");
            IFileSystem fileSystem = GetFileSystemContainingFile(transformFile);
            ConsoleOptions options = new ConsoleOptions(
                new DefaultOptionsProviderStub(false),
                fileSystem,
                "tests.dll", $"-explore:results.xml;transform={transformFile}");
            Assert.That(options.Validate(), Is.True);
            Assert.That(options.InputFiles.Count, Is.EqualTo(1), "assembly should be set");
            Assert.That(options.InputFiles[0], Is.EqualTo("tests.dll"));
            Assert.That(options.Explore, Is.True);

            OutputSpecification spec = options.ExploreOutputSpecifications[0];
            Assert.That(spec.OutputPath, Is.EqualTo("results.xml"));
            Assert.That(spec.Format, Is.EqualTo("user"));
            var fullFilePath = Path.Combine(TestContext.CurrentContext.TestDirectory, transformFile);
            Assert.That(spec.Transform, Is.EqualTo(fullFilePath));
        }

        [Test]
        public void ExploreOptionWithFilePathUsingEqualSign()
        {
            ConsoleOptions options = ConsoleMocks.Options("tests.dll", "-explore=C:/nunit/tests/bin/Debug/console-test.xml");
            Assert.That(options.Validate(), Is.True);
            Assert.That(options.Explore, Is.True);
            Assert.That(options.InputFiles.Count, Is.EqualTo(1), "assembly should be set");
            Assert.That(options.InputFiles[0], Is.EqualTo("tests.dll"));
            Assert.That(options.ExploreOutputSpecifications[0].OutputPath, Is.EqualTo("C:/nunit/tests/bin/Debug/console-test.xml"));
        }

        [Test]
        [TestCase(true, null, true)]
        [TestCase(false, null, false)]
        [TestCase(true, false, true)]
        [TestCase(false, false, false)]
        [TestCase(true, true, true)]
        [TestCase(false, true, true)]
        public void ShouldSetTeamCityFlagAccordingToArgsAndDefaults(bool hasTeamcityInCmd, bool? defaultTeamcity, bool expectedTeamCity)
        {
            // Given
            List<string> args = new List<string> { "tests.dll" };
            if (hasTeamcityInCmd)
            {
                args.Add("--teamcity");
            }

            ConsoleOptions options;
            if (defaultTeamcity.HasValue)
            {
                options = new ConsoleOptions(new DefaultOptionsProviderStub(defaultTeamcity.Value), new VirtualFileSystem(), args.ToArray());
            }
            else
            {
                options = ConsoleMocks.Options(args.ToArray());
            }

            // When
            var actualTeamCity = options.TeamCity;

            // Then
            Assert.That(expectedTeamCity, Is.EqualTo(actualTeamCity));
        }

        [Test]
        public void ShouldNotFailOnEmptyLine()
        {
            var testListPath = Path.Combine(TestContext.CurrentContext.TestDirectory, "TestListWithEmptyLine.tst");
            // Not copying this test file into releases
            Assume.That(testListPath, Does.Exist);
            var options = ConsoleMocks.Options("--testlist=" + testListPath);
            Assert.That(options.ErrorMessages, Is.Empty);
            Assert.That(options.TestList, Is.EqualTo(new[] {"AmazingTest"}));
        }

        [Test]
        public void SingleTestParameter()
        {
            var options = ConsoleMocks.Options("--param=X=5");
            Assert.That(options.ErrorMessages, Is.Empty);
            Assert.That(options.TestParameters, Is.EqualTo(new Dictionary<string, string> { { "X", "5" } }));
        }

        [Test]
        public void SemicolonsDoNotSplitTestParameters()
        {
            var options = ConsoleMocks.Options("--param:X=5;Y=7");
            Assert.That(options.ErrorMessages, Is.Empty);
            Assert.That(options.TestParameters, Is.EqualTo(new Dictionary<string, string> { { "X", "5;Y=7" } }));
        }

        [Test]
        public void TwoTestParametersInSeparateOptions()
        {
            var options = ConsoleMocks.Options("--param:X=5", "--param:Y=7");
            Assert.That(options.ErrorMessages, Is.Empty);
            Assert.That(options.TestParameters, Is.EqualTo(new Dictionary<string, string> { { "X", "5" }, { "Y", "7" } }));
        }

        [Test]
        public void ParameterWithoutEqualSignIsInvalid()
        {
            var options = ConsoleMocks.Options("--param=X5");
            Assert.That(options.ErrorMessages.Count, Is.EqualTo(1));
        }

        [Test]
        public void ParameterWithMissingNameIsInvalid()
        {
            var options = ConsoleMocks.Options("--param:=5");
            Assert.That(options.ErrorMessages.Count, Is.EqualTo(1));
        }

        [Test]
        public void ParameterWithMissingValueIsInvalid()
        {
            var options = ConsoleMocks.Options("--param:X=");
            Assert.That(options.ErrorMessages.Count, Is.EqualTo(1));
        }

        [Test]
        public void LeadingWhitespaceIsRemovedInParameterName()
        {
            // Command line examples to get in this scenario:
            // --param:"  X"=5
            // --param:"  X=5"
            // "--param:  X=5"

            var options = ConsoleMocks.Options("--param:  X=5");
            Assert.That(options.TestParameters, Is.EqualTo(new Dictionary<string, string> { ["X"] = "5" }));
        }

        [Test]
        public void TrailingWhitespaceIsRemovedInParameterName()
        {
            // Command line examples to get in this scenario:
            // --param:"X  "=5
            // --param:"X  =5"
            // "--param:X  =5"

            var options = ConsoleMocks.Options("--param:X  =5");
            Assert.That(options.TestParameters, Is.EqualTo(new Dictionary<string, string> { ["X"] = "5" }));
        }

        [Test]
        public void WhitespaceIsNotPermittedAsParameterName()
        {
            // Command line examples to get in this scenario:
            // --testparams:"  "=5
            // --testparams:"  =5"
            // "--testparams:  =5"

            var options = ConsoleMocks.Options("--param:  =5");
            Assert.That(options.ErrorMessages.Count, Is.EqualTo(1));
        }

        [Test]
        public void LeadingWhitespaceIsRemovedInParameterValue()
        {
            // Command line examples to get in this scenario:
            // --param:X="  5"
            // --param:"X=  5"
            // "--param:X=  5"

            var options = ConsoleMocks.Options("--param:X=  5");
            Assert.That(options.TestParameters, Is.EqualTo(new Dictionary<string, string> { ["X"] = "5" }));
        }

        [Test]
        public void TrailingWhitespaceIsRemovedInParameterValue()
        {
            // Command line examples to get in this scenario:
            // --param:X="5  "
            // --param:"X=5  "
            // "--param:X=5  "

            var options = ConsoleMocks.Options("--param:X=5  ");
            Assert.That(options.TestParameters, Is.EqualTo(new Dictionary<string, string> { ["X"] = "5" }));
        }

        [Test]
        public void WhitespaceIsNotPermittedForNonQuotedParameterValue()
        {
            // Command line examples to get in this scenario:
            // --param:X="  "
            // --param:"X=  "
            // "--param:X=  "

            var options = ConsoleMocks.Options("--param:X=  ");
            Assert.That(options.ErrorMessages.Count, Is.EqualTo(1));
        }

        [TestCase('"', TestName = "{m}_DoubleQuote")]
        [TestCase('\'', TestName = "{m}_SingleQuote")]
        public void WhitespaceIsPermittedForQuotedParameterValue(char quoteChar)
        {
            // Command line examples to get in this scenario:
            // --param:X=\"  \"
            // --param:X='  '

            var options = ConsoleMocks.Options($"--param:X={quoteChar}  {quoteChar}");
            Console.WriteLine(options.TestParameters["X"]);
            Assert.That(options.TestParameters, Is.EqualTo(new Dictionary<string, string> { ["X"] = "  " }));
        }

        private static IFileSystem GetFileSystemContainingFile(string fileName)
        {
            var fileSystem = new VirtualFileSystem();
            fileSystem.SetupFile(Path.Combine(Environment.CurrentDirectory, fileName), new List<string>());
            return fileSystem;
        }

        private static FieldInfo GetFieldInfo(string fieldName)
        {
            FieldInfo field = typeof(ConsoleOptions).GetField(fieldName);
            Assert.IsNotNull(field, "The field '{0}' is not defined", fieldName);
            return field;
        }

        private static PropertyInfo GetPropertyInfo(string propertyName)
        {
            PropertyInfo property = typeof(ConsoleOptions).GetProperty(propertyName);
            Assert.IsNotNull(property, "The property '{0}' is not defined", propertyName);
            return property;
        }

        internal sealed class DefaultOptionsProviderStub : IDefaultOptionsProvider
        {
            public DefaultOptionsProviderStub(bool teamCity)
            {
                TeamCity = teamCity;
            }

            public bool TeamCity { get; private set; }
        }
    }
}
