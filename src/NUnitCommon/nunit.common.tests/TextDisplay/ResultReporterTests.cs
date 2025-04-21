// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;
using NUnit.Engine;
using NUnit.Framework;
using NUnit.Framework.Api;
using NUnit.TestData.Assemblies;

namespace NUnit.TextDisplay
{
    public class ResultReporterTests
    {
        private XmlNode _result;
        private StringBuilder _report;

        [OneTimeSetUp]
        public void CreateResult()
        {
            var mockAssembly = typeof(MockAssembly).Assembly;
            var frameworkSettings = new Dictionary<string, object>
            {
                { "TestParameters", "1=d;2=c" },
                {
                    "TestParametersDictionary", new Dictionary<string, string>
                {
                    { "1", "d" },
                    { "2", "c" }
                }
                }
            };

            var controller = new FrameworkController(mockAssembly, "id", frameworkSettings);

            controller.LoadTests();
            var xmlText = controller.RunTests(null);
            var engineResult = AddMetadata(new TestEngineResult(xmlText));
            _result = engineResult.Xml;

            Assert.That(_result, Is.Not.Null, "Unable to create report result.");
        }

        [SetUp]
        public void CreateReporter()
        {
            _report = new StringBuilder();
            var writer = new ExtendedTextWrapper(new StringWriter(_report));
        }

        [Test]
        public void ReportSequenceTest()
        {
            var sb = new StringBuilder();
            var writer = new ExtendedTextWrapper(new StringWriter(sb));
            ResultReporter.ReportResults(new ResultSummary(_result), writer);
            var report = sb.ToString();

            var reportSequence = new[]
            {
                "Tests Not Run",
                "Errors, Failures and Warnings",
                "Test Run Summary"
            };

            int last = -1;

            foreach (string title in reportSequence)
            {
                var index = report.IndexOf(title);
                Assert.That(index > 0, "Report not found: " + title);
                Assert.That(index > last, "Report out of sequence: " + title);
                last = index;
            }
        }

        [Test]
        public void SummaryReportTest()
        {
#pragma warning disable SA1137 // Elements should have the same indentation
            var expected = new[]
            {
                "Test Run Summary",
                "    Overall result: Failed",
               $"    Test Count: {MockAssembly.Tests}, Pass: {MockAssembly.Passed}, Fail: 11, Warn: 1, Inconclusive: 1, Skip: 7",
                "        Failed Tests - Failures: 1, Errors: 7, Invalid: 3",
                "        Skipped Tests - Ignored: 4, Explicit: 3, Other: 0",
                "    Start time: 2015-10-19 02:12:28Z",
                "    End time: 2015-10-19 02:12:29Z",
                "    Duration: 0.349 seconds",
                string.Empty
            };
#pragma warning restore SA1137 // Elements should have the same indentation

            var report = new StringBuilder();
            var writer = new ExtendedTextWrapper(new StringWriter(report));
            ResultReporter.WriteSummaryReport(_result, new ResultSummary(_result), writer);
            var lines = GetReportLines(report.ToString());
            Assert.That(lines, Is.EqualTo(expected));
        }

        [Test]
        public void ErrorsFailuresAndWarningsReportTest()
        {
            var nl = Environment.NewLine;

            var expected = new[]
            {
                "Errors, Failures and Warnings",
                "1) Failed : NUnit.TestData.Assemblies.MockTestFixture.FailingTest" + nl +
                "Intentional failure",
                "2) Invalid : NUnit.TestData.Assemblies.MockTestFixture.NonPublicTest" + nl +
                "Method is not public",
                "3) Invalid : NUnit.TestData.Assemblies.MockTestFixture.NotRunnableTest" + nl +
                "No arguments were provided",
                "4) Error : NUnit.TestData.Assemblies.MockTestFixture.TestWithException" + nl +
                "System.Exception : Intentional Exception",
                "5) Warning : NUnit.TestData.Assemblies.MockTestFixture.WarningTest" + nl +
                "Warning Message",
                "6) Invalid : NUnit.TestData.BadFixture" + nl +
                "No suitable constructor was found"
            };

            var sb = new StringBuilder();
            var writer = new ExtendedTextWrapper(new StringWriter(sb));
            ResultReporter.WriteErrorsFailuresAndWarningsReport(_result, writer);

            var report = sb.ToString();

            foreach (var item in expected)
                Assert.That(report.Contains(item));
        }

        [Test]
        public void TestsNotRunTest()
        {
            var expected = new[]
            {
                "Tests Not Run",
                string.Empty,
                "1) Explicit : NUnit.TestData.Assemblies.MockTestFixture.ExplicitTest",
                string.Empty,
                "2) Ignored : NUnit.TestData.Assemblies.MockTestFixture.IgnoreTest",
                "Ignore Message",
                string.Empty,
                "3) Explicit : NUnit.TestData.ExplicitFixture.Test1",
                "OneTimeSetUp: ",
                string.Empty,
                "4) Explicit : NUnit.TestData.ExplicitFixture.Test2",
                "OneTimeSetUp: ",
                string.Empty,
                "5) Ignored : NUnit.TestData.IgnoredFixture.Test1",
                "OneTimeSetUp: BECAUSE",
                string.Empty,
                "6) Ignored : NUnit.TestData.IgnoredFixture.Test2",
                "OneTimeSetUp: BECAUSE",
                string.Empty,
                "7) Ignored : NUnit.TestData.IgnoredFixture.Test3",
                "OneTimeSetUp: BECAUSE",
                string.Empty
            };

            var report = new StringBuilder();
            var writer = new ExtendedTextWrapper(new StringWriter(report));
            ResultReporter.WriteNotRunReport(_result, writer);
            var lines = GetReportLines(report.ToString());
            Assert.That(lines, Is.EqualTo(expected));
        }

        [Test, Explicit("Displays failure behavior")]
        public void WarningsOnlyDisplayOnce()
        {
            Assert.Warn("Just a warning");
        }

        [Test]
        public void TestParameterSettingsWrittenCorrectly()
        {
            var expected = new[]
            {
                "    TestParameters: |1=d;2=c|",
                "    TestParametersDictionary:",
                "        1 -> |d|",
                "        2 -> |c|"
            };

            var report = new StringBuilder();
            var writer = new ExtendedTextWrapper(new StringWriter(report));
            ResultReporter.WriteRunSettingsReport(_result, writer);
            var lines = GetReportLines(report.ToString());
            Assert.That(lines, Is.SupersetOf(expected));
        }

        private static TestEngineResult AddMetadata(TestEngineResult input)
        {
            return input.Aggregate("test-run start-time=\"2015-10-19 02:12:28Z\" end-time=\"2015-10-19 02:12:29Z\" duration=\"0.348616\"", string.Empty, string.Empty, string.Empty);
        }

        private static List<string> GetReportLines(string report)
        {
            var rdr = new StringReader(report.ToString());

            string? line;
            var lines = new List<string>();
            while ((line = rdr.ReadLine()) is not null)
                lines.Add(line);

            return lines;
        }
    }
}