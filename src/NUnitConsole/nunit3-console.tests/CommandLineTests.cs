// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using System;
using System.IO;
using System.Reflection;
using System.Collections.Generic;

using NUnit.Common;
using NUnit.ConsoleRunner.Options;
using NUnit.Framework;

namespace NUnit.ConsoleRunner.Tests
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

            var options = new ConsoleOptions(fileSystem);

            // When
            var expandedArgs = options.PreParse(commandline.Split(' '));

            // Then
            Assert.That(expandedArgs, Is.EqualTo(expectedArgs));
            Assert.That(options.ErrorMessages, Is.Empty);
        }

        [TestCase("--arg1 @file1.txt --arg2", "The file \"file1.txt\" was not found.")]
        [TestCase("--arg1 @ --arg2", "You must include a file name after @.")]
        public void GetArgsFromFiles_FailureTests(string args, string errorMessage)
        {
            var options = new ConsoleOptions(new VirtualFileSystem());

            options.PreParse(args.Split(' '));

            Assert.That(options.ErrorMessages, Is.EqualTo(new object[] { errorMessage }));
        }

        [Test]
        public void GetArgsFromFiles_NestingOverflow()
        {
            var fileSystem = new VirtualFileSystem();
            var lines = new string[] { "@file1.txt" };
            fileSystem.SetupFile("file1.txt", lines);
            var options = new ConsoleOptions(fileSystem);
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
        [TestCase("SkipNonTestAssemblies", "skipnontestassemblies")]
        [TestCase("NoResult", "noresult")]
#if NETFRAMEWORK
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
        [TestCase("DisplayTestLabels", "labels", new string[] { "Off", "OnOutputOnly", "Before", "After", "BeforeAndAfter" }, new string[] { "JUNK" })]
        [TestCase("InternalTraceLevel", "trace", new string[] { "Off", "Error", "Warning", "Info", "Debug", "Verbose" }, new string[] { "JUNK" })]
        [TestCase("DefaultTestNamePattern", "test-name-format", new string[] { "{m}{a}" }, new string[0])]
        [TestCase("ConsoleEncoding", "encoding", new string[] { "utf-8", "ascii", "unicode" }, new string[0])]
#if NETFRAMEWORK
        [TestCase("ProcessModel", "process", new string[] { "InProcess", "Separate", "Multiple" }, new string[] { "JUNK" })]
        [TestCase("DomainUsage", "domain", new string[] { "None", "Single", "Multiple" }, new string[] { "JUNK" })]
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

#if NETFRAMEWORK
        [Test]
        public void CanRecognizeInProcessOption()
        {
            ConsoleOptions options = ConsoleMocks.Options("--inprocess");
            Assert.That(options.Validate(), Is.True, "Should be valid: --inprocess");
            Assert.That(options.ProcessModel, Is.EqualTo("InProcess"), "Didn't recognize --inprocess");
        }
#endif

#if NETFRAMEWORK
        [TestCase("ProcessModel", "process", new string[] { "InProcess", "Separate", "Multiple" })]
        [TestCase("DomainUsage", "domain", new string[] { "None", "Single", "Multiple" })]
#endif
        [TestCase("DisplayTestLabels", "labels", new string[] { "Off", "OnOutputOnly", "Before", "After", "BeforeAndAfter" })]
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

        [TestCase("DefaultTimeout", "timeout")]
        [TestCase("RandomSeed", "seed")]
        [TestCase("NumberOfTestWorkers", "workers")]
#if NETFRAMEWORK
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
        [TestCase("--timeout")]
        [TestCase("--output")]
        [TestCase("--work")]
        [TestCase("--trace")]
        [TestCase("--test-name-format")]
        [TestCase("--params")]
        [TestCase("--encoding")]
        [TestCase("--extensionDirectory")]
        [TestCase("--enable")]
        [TestCase("--disable")]
#if NETFRAMEWORK
        [TestCase("--process")]
        [TestCase("--domain")]
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

#if NETFRAMEWORK
        [Test]
        public void X86AndInProcessAreCompatibleIn32BitProcess()
        {
            //Can be replaced with PlatformAttribute("32-Bit"), once NUnit Framework 3.12 can be used
            if (IntPtr.Size == 8)
            {
                Assert.Inconclusive("Test can only be run on 32-bit platform");
            }

            ConsoleOptions options = ConsoleMocks.Options("nunit.tests.dll", "--x86", "--inprocess");
            Assert.That(options.Validate(), Is.True);
            Assert.That(options.ErrorMessages.Count, Is.EqualTo(0), "command line should be valid");
        }

        [Test]
        public void X86AndInProcessAreNotCompatibleIn64BitProcess()
        {
            //Can be replaced with PlatformAttribute("64-Bit"), once NUnit Framework 3.12 can be used
            if (IntPtr.Size == 4)
            {
                Assert.Inconclusive("Test can only be run on 64-bit platform");
            }
            ConsoleOptions options = ConsoleMocks.Options("nunit.tests.dll", "--x86", "--inprocess");
            Assert.That(options.Validate(), Is.False, "Should be invalid");
            Assert.That(options.ErrorMessages[0], Is.EqualTo("The --x86 and --inprocess options are incompatible."));
        }
#endif

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
            Assert.That(options.DefaultTimeout, Is.EqualTo(-1));
        }

        [Test]
        public void TimeoutThrowsExceptionIfOptionHasNoValue()
        {
            Assert.Throws<OptionException>(() => ConsoleMocks.Options("tests.dll", "-timeout"));
        }

        [Test]
        public void TimeoutParsesIntValueCorrectly()
        {
            ConsoleOptions options = ConsoleMocks.Options("tests.dll", "-timeout:5000");
            Assert.That(options.Validate(), Is.True);
            Assert.That(options.DefaultTimeout, Is.EqualTo(5000));
        }

        [Test]
        public void TimeoutCausesErrorIfValueIsNotInteger()
        {
            ConsoleOptions options = ConsoleMocks.Options("tests.dll", "-timeout:abc");
            Assert.That(options.Validate(), Is.False);
            Assert.That(options.DefaultTimeout, Is.EqualTo(-1));
        }

        [Test]
        public void ResultOptionWithFilePath()
        {
            ConsoleOptions options = ConsoleMocks.Options("tests.dll", "-result:results.xml");
            Assert.That(options.Validate(), Is.True);
            Assert.That(options.InputFiles.Count, Is.EqualTo(1), "assembly should be set");
            Assert.That(options.InputFiles[0], Is.EqualTo("tests.dll"));

            var spec = options.ResultOutputSpecifications[0];
            Assert.That(spec.OutputPath, Is.EqualTo("results.xml"));
            Assert.That(spec.Format, Is.EqualTo("nunit3"));
            Assert.That(spec.Transform, Is.Null);
        }

        [Test]
        public void ResultOptionWithFilePathAndFormat()
        {
            ConsoleOptions options = ConsoleMocks.Options("tests.dll", "-result:results.xml;format=nunit2");
            Assert.That(options.Validate(), Is.True);
            Assert.That(options.InputFiles.Count, Is.EqualTo(1), "assembly should be set");
            Assert.That(options.InputFiles[0], Is.EqualTo("tests.dll"));

            var spec = options.ResultOutputSpecifications[0];
            Assert.That(spec.OutputPath, Is.EqualTo("results.xml"));
            Assert.That(spec.Format, Is.EqualTo("nunit2"));
            Assert.That(spec.Transform, Is.Null);
        }

        [Test]
        public void ResultOptionWithFilePathAndTransform()
        {
            string transformFile = Path.Combine(TestContext.CurrentContext.TestDirectory, "TextSummary.xslt");
            IFileSystem fileSystem = GetFileSystemContainingFile(transformFile);

            ConsoleOptions options = new ConsoleOptions(
                fileSystem,
                "tests.dll", $"-result:results.xml;transform={transformFile}");
            Assert.That(options.Validate(), Is.True);
            Assert.That(options.InputFiles.Count, Is.EqualTo(1), "assembly should be set");
            Assert.That(options.InputFiles[0], Is.EqualTo("tests.dll"));

            var spec = options.ResultOutputSpecifications[0];
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
                fileSystem,
                "tests.dll", "-result:results.xml", "-result:nunit2results.xml;format=nunit2", $"-result:myresult.xml;transform={transformFile}");
            Assert.That(options.Validate(), Is.True, "Should be valid");

            var specs = options.ResultOutputSpecifications;
            Assert.That(specs.Count, Is.EqualTo(3));

            var spec1 = specs[0];
            Assert.That(spec1.OutputPath, Is.EqualTo("results.xml"));
            Assert.That(spec1.Format, Is.EqualTo("nunit3"));
            Assert.That(spec1.Transform, Is.Null);

            var spec2 = specs[1];
            Assert.That(spec2.OutputPath, Is.EqualTo("nunit2results.xml"));
            Assert.That(spec2.Format, Is.EqualTo("nunit2"));
            Assert.That(spec2.Transform, Is.Null);

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
            Assert.That(spec.Transform, Is.Null);
        }

        [Test]
        public void InvalidResultSpecRecordsError()
        {
            var options = ConsoleMocks.Options("test.dll", "-result:userspecifed.xml;format=nunit2;format=nunit3");
            Assert.That(options.ResultOutputSpecifications, Has.Exactly(1).Items
                .And.Exactly(1).Property(nameof(Options.OutputSpecification.OutputPath)).EqualTo("TestResult.xml"));
            Assert.That(options.ErrorMessages, Has.Exactly(1).Contains("conflicting format options").IgnoreCase);
        }

        [Test]
        public void MissingXsltFileRecordsError()
        {
            const string missingXslt = "missing.xslt";

            var options = new ConsoleOptions(
                new VirtualFileSystem(),
                "test.dll", $"-result:userspecifed.xml;transform={missingXslt}");
            Assert.That(options.ResultOutputSpecifications, Has.Exactly(1).Items
                                                               .And.Exactly(1).Property(nameof(Options.OutputSpecification.Transform)).Null);
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

            var spec = options.ExploreOutputSpecifications[0];
            Assert.That(spec.OutputPath, Is.EqualTo("results.xml"));
            Assert.That(spec.Format, Is.EqualTo("nunit3"));
            Assert.That(spec.Transform, Is.Null);
        }

        [Test]
        public void ExploreOptionWithFilePathAndFormat()
        {
            ConsoleOptions options = ConsoleMocks.Options("tests.dll", "-explore:results.xml;format=cases");
            Assert.That(options.Validate(), Is.True);
            Assert.That(options.InputFiles.Count, Is.EqualTo(1), "assembly should be set");
            Assert.That(options.InputFiles[0], Is.EqualTo("tests.dll"));
            Assert.That(options.Explore, Is.True);

            var spec = options.ExploreOutputSpecifications[0];
            Assert.That(spec.OutputPath, Is.EqualTo("results.xml"));
            Assert.That(spec.Format, Is.EqualTo("cases"));
            Assert.That(spec.Transform, Is.Null);
        }

        [Test]
        public void ExploreOptionWithFilePathAndTransform()
        {
            string transformFile = Path.Combine(TestContext.CurrentContext.TestDirectory, "TextSummary.xslt");
            IFileSystem fileSystem = GetFileSystemContainingFile(transformFile);
            ConsoleOptions options = new ConsoleOptions(
                fileSystem,
                "tests.dll", $"-explore:results.xml;transform={transformFile}");
            Assert.That(options.Validate(), Is.True);
            Assert.That(options.InputFiles.Count, Is.EqualTo(1), "assembly should be set");
            Assert.That(options.InputFiles[0], Is.EqualTo("tests.dll"));
            Assert.That(options.Explore, Is.True);

            var spec = options.ExploreOutputSpecifications[0];
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
        public void SingleDeprecatedTestParameter()
        {
            var options = ConsoleMocks.Options("--params=X=5");
            Assert.That(options.ErrorMessages, Is.Empty);
            Assert.That(options.WarningMessages, Has.One.Contains("deprecated").IgnoreCase);
            Assert.That(options.TestParameters, Is.EqualTo(new Dictionary<string, string> { { "X", "5" } }));
        }

        [Test]
        public void TwoDeprecatedTestParametersInOneOption()
        {
            var options = ConsoleMocks.Options("--params:X=5;Y=7");
            Assert.That(options.ErrorMessages, Is.Empty);
            Assert.That(options.WarningMessages, Has.One.Contains("deprecated").IgnoreCase);
            Assert.That(options.TestParameters, Is.EqualTo(new Dictionary<string, string> { { "X", "5" }, { "Y", "7" } }));
        }

        [Test]
        public void TwoDeprecatedTestParametersInSeparateOptions()
        {
            var options = ConsoleMocks.Options("-p:X=5", "-p:Y=7");
            Assert.That(options.ErrorMessages, Is.Empty);
            Assert.That(options.WarningMessages, Has.One.Contains("deprecated").IgnoreCase);
            Assert.That(options.TestParameters, Is.EqualTo(new Dictionary<string, string> { { "X", "5" }, { "Y", "7" } }));
        }

        [Test]
        public void ThreeDeprecatedTestParametersInTwoOptions()
        {
            var options = ConsoleMocks.Options("--params:X=5;Y=7", "-p:Z=3");
            Assert.That(options.ErrorMessages, Is.Empty);
            Assert.That(options.WarningMessages, Has.One.Contains("deprecated").IgnoreCase);
            Assert.That(options.TestParameters, Is.EqualTo(new Dictionary<string, string> { { "X", "5" }, { "Y", "7" }, { "Z", "3" } }));
        }

        [Test]
        public void DeprecatedParameterWithoutEqualSignIsInvalid()
        {
            var options = ConsoleMocks.Options("--params=X5");
            Assert.That(options.WarningMessages, Has.One.Contains("deprecated").IgnoreCase);
            Assert.That(options.ErrorMessages.Count, Is.EqualTo(1));
        }

        [Test]
        public void SingleTestParameter()
        {
            var options = ConsoleMocks.Options("--testparam=X=5");
            Assert.That(options.ErrorMessages, Is.Empty);
            Assert.That(options.TestParameters, Is.EqualTo(new Dictionary<string, string> { { "X", "5" } }));
        }

        [Test]
        public void SemicolonsDoNotSplitTestParameters()
        {
            var options = ConsoleMocks.Options("--testparam:X=5;Y=7");
            Assert.That(options.ErrorMessages, Is.Empty);
            Assert.That(options.TestParameters, Is.EqualTo(new Dictionary<string, string> { { "X", "5;Y=7" } }));
        }

        [Test]
        public void TwoTestParametersInSeparateOptions()
        {
            var options = ConsoleMocks.Options("--testparam:X=5", "--testparam:Y=7");
            Assert.That(options.ErrorMessages, Is.Empty);
            Assert.That(options.TestParameters, Is.EqualTo(new Dictionary<string, string> { { "X", "5" }, { "Y", "7" } }));
        }

        [Test]
        public void ParameterWithoutEqualSignIsInvalid()
        {
            var options = ConsoleMocks.Options("--testparam=X5");
            Assert.That(options.ErrorMessages.Count, Is.EqualTo(1));
        }

        [Test]
        public void ParameterWithMissingNameIsInvalid()
        {
            var options = ConsoleMocks.Options("--testparam:=5");
            Assert.That(options.ErrorMessages.Count, Is.EqualTo(1));
        }

        [Test]
        public void ParameterWithMissingValueIsInvalid()
        {
            var options = ConsoleMocks.Options("--testparam:X=");
            Assert.That(options.ErrorMessages.Count, Is.EqualTo(1));
        }

        [Test]
        public void LeadingWhitespaceIsPreservedInParameterName()
        {
            // Command line examples to get in this scenario:
            // --testparams:"  X"=5
            // --testparams:"  X=5"
            // "--testparams:  X=5"

            var options = ConsoleMocks.Options("--testparam:  X=5");
            Assert.That(options.TestParameters, Is.EqualTo(new Dictionary<string, string> { ["  X"] = "5" }));
        }

        [Test]
        public void TrailingWhitespaceIsPreservedInParameterName()
        {
            // Command line examples to get in this scenario:
            // --testparams:"X  "=5
            // --testparams:"X  =5"
            // "--testparams:X  =5"

            var options = ConsoleMocks.Options("--testparam:X  =5");
            Assert.That(options.TestParameters, Is.EqualTo(new Dictionary<string, string> { ["X  "] = "5" }));
        }

        [Test]
        public void WhitespaceIsPermittedAsParameterName()
        {
            // Command line examples to get in this scenario:
            // --testparams:"  "=5
            // --testparams:"  =5"
            // "--testparams:  =5"

            var options = ConsoleMocks.Options("--testparam:  =5");
            Assert.That(options.TestParameters, Is.EqualTo(new Dictionary<string, string> { ["  "] = "5" }));
        }

        [Test]
        public void LeadingWhitespaceIsPreservedInParameterValue()
        {
            // Command line examples to get in this scenario:
            // --testparams:X="  5"
            // --testparams:"X=  5"
            // "--testparams:X=  5"

            var options = ConsoleMocks.Options("--testparam:X=  5");
            Assert.That(options.TestParameters, Is.EqualTo(new Dictionary<string, string> { ["X"] = "  5" }));
        }

        [Test]
        public void TrailingWhitespaceIsPreservedInParameterValue()
        {
            // Command line examples to get in this scenario:
            // --testparams:X="5  "
            // --testparams:"X=5  "
            // "--testparams:X=5  "

            var options = ConsoleMocks.Options("--testparam:X=5  ");
            Assert.That(options.TestParameters, Is.EqualTo(new Dictionary<string, string> { ["X"] = "5  " }));
        }

        [Test]
        public void WhitespaceIsPermittedAsParameterValue()
        {
            // Command line examples to get in this scenario:
            // --testparams:X="  "
            // --testparams:"X=  "
            // "--testparams:X=  "

            var options = ConsoleMocks.Options("--testparam:X=  ");
            Assert.That(options.TestParameters, Is.EqualTo(new Dictionary<string, string> { ["X"] = "  " }));
        }

        [Test]
        public void DisplayTestParameters()
        {
            if (TestContext.Parameters.Count == 0)
            {
                Console.WriteLine("No Test Parameters were passed");
                return;
            }

            Console.WriteLine("Test Parameters---");

            foreach (var name in TestContext.Parameters.Names)
                Console.WriteLine("   Name: {0} Value: {1}", name, TestContext.Parameters[name]);
        }

        [TestCase("On", true)]
        [TestCase("All", true)]
        [TestCase("Off", false)]
        [TestCase("OnOutputOnly", false)]
        [TestCase("Before", false)]
        [TestCase("After", false)]
        [TestCase("BeforeAndAfter", false)]
        public void DeprecatedLabelsOptionsWarnCorrectly(string labelOption, bool shouldWarn)
        {
            var options = ConsoleMocks.Options("--labels=" + labelOption);
            options.Validate();
            var countWarningsExpected = shouldWarn ? 1 : 0;
            Assert.Multiple(() => {
                Assert.That(options.ErrorMessages, Is.Empty);
                Assert.That(options.WarningMessages, Has.Exactly(countWarningsExpected).Contains("deprecated"));
            });
        }

        [TestCase("On", "OnOutputOnly")]
        [TestCase("All", "Before")]
        public void DeprecatedLabelsOptionsAreReplacedCorrectly(string oldOption, string newOption)
        {
            var options = ConsoleMocks.Options("--labels=" + oldOption);
            options.Validate();
            Assert.That(options.DisplayTestLabels, Is.EqualTo(newOption));
        }

        [Test]
        public void UserExtensionDirectoryTest()
        {
            ConsoleOptions options = ConsoleMocks.Options("--extensionDirectory=/a/b/c");
            Assert.That(options.Validate);
            Assert.That(options.ExtensionDirectories.Contains("/a/b/c"));
        }

        [Test]
        public void EnableExtensionTest()
        {
            ConsoleOptions options = ConsoleMocks.Options("--enable=NUnit.Engine.Listeners.TeamCityEventListener");
            Assert.That(options.Validate);
            Assert.That(options.EnableExtensions.Contains("NUnit.Engine.Listeners.TeamCityEventListener"));
        }

        [Test]
        public void DisableExtensionTest()
        {
            ConsoleOptions options = ConsoleMocks.Options("--disable=NUnit.Engine.Listeners.TeamCityEventListener");
            Assert.That(options.Validate);
            Assert.That(options.DisableExtensions.Contains("NUnit.Engine.Listeners.TeamCityEventListener"));
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
            Assert.That(field, Is.Not.Null, "The field '{0}' is not defined", fieldName);
            return field;
        }

        private static PropertyInfo GetPropertyInfo(string propertyName)
        {
            PropertyInfo property = typeof(ConsoleOptions).GetProperty(propertyName);
            Assert.That(property, Is.Not.Null, $"The property '{propertyName}' is not defined");
            return property;
        }
    }
}
