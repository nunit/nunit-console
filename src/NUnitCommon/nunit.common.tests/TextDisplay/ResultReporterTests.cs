﻿// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using NUnit.Common;
using NUnit.Engine;
using NUnit.Framework;
using NUnit.Framework.Api;
using NUnit.TestData.Assemblies;

namespace NUnit.TextDisplay
{
    public class ResultReporterTests
    {
        private XmlNode _result;

        private StringBuilder _reportBuilder;
        private ExtendedTextWrapper _writer;

        private List<string> ReportLines
        {
            get
            {
                var rdr = new StringReader(_reportBuilder.ToString());

                string? line;
                var lines = new List<string>();
                while ((line = rdr.ReadLine()) is not null)
                    lines.Add(line);

                return lines;
            }
        }

        [OneTimeSetUp]
        public void CreateResult()
        {
            var mockAssembly = typeof(MockAssembly).Assembly;
            var frameworkSettings = new Dictionary<string, object>
            {
                { FrameworkPackageSettings.TestParameters, "1=d;2=c" },
                {
                    FrameworkPackageSettings.TestParametersDictionary, new Dictionary<string, string>
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
        public void SetUp()
        {
            _reportBuilder = new StringBuilder();
            _writer = new ExtendedTextWrapper(new StringWriter(_reportBuilder));
        }

        [TearDown]
        public void TearDown() => _writer.Dispose();

        [TestCase(false, "Tests Not Run", "Errors, Failures and Warnings", "Test Run Summary")]
        [TestCase(true, "Errors, Failures and Warnings", "Test Run Summary")]
        public void ReportSequenceTest(bool omitNotRunReport, params string[] reportSequence)
        {
            var settings = new ResultReporterSettings() { OmitNotRunReport = omitNotRunReport };
            new ResultReporter(settings).ReportResults(new ResultSummary(_result), _writer);

            int last = -1;

            string reportOutput = _reportBuilder.ToString();
            foreach (string title in reportSequence)
            {
                var index = reportOutput.IndexOf(title);
                Assert.That(index >= 0, "Report not found: " + title);
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
               $"    Test Count: {MockAssembly.Tests}, Pass: {MockAssembly.Passed}, Fail: {MockAssembly.Failed}, Warn: {MockAssembly.Warnings}, Inconclusive: {MockAssembly.Inconclusive}, Skip: {MockAssembly.Skipped}",
               $"        Failed Tests - Failures: {MockAssembly.Failures}, Errors: {MockAssembly.Errors}, Invalid: {MockAssembly.NotRunnable}",
               $"        Skipped Tests - Ignored: {MockAssembly.Ignored}, Explicit: {MockAssembly.Explicit}, Other: 0",
                "    Start time: 2015-10-19 02:12:28Z",
                "    End time: 2015-10-19 02:12:29Z",
                "    Duration: 0.349 seconds",
                string.Empty
            };
#pragma warning restore SA1137 // Elements should have the same indentation

            ResultReporter.WriteSummaryReport(new ResultSummary(_result), _writer);
            Assert.That(ReportLines, Is.EqualTo(expected));
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

            new ResultReporter().WriteErrorsFailuresAndWarningsReport(_result, _writer);

            string reportOutput = _reportBuilder.ToString();
            foreach (var item in expected)
                Assert.That(reportOutput.Contains(item));
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

            new ResultReporter().WriteNotRunReport(_result, _writer);
            Assert.That(ReportLines, Is.EqualTo(expected));
        }

        [Test]
        public void TestsNotRun_OmitExplicit()
        {
            var settings = new ResultReporterSettings() { OmitExplicitTests = true };
            new ResultReporter(settings).WriteNotRunReport(_result, _writer);
            Assert.That(ReportLines.Where(line => line.Contains("Explicit")), Is.Empty);
        }

        [Test]
        public void TestsNotRun_OmitIgnored()
        {
            var settings = new ResultReporterSettings() { OmitIgnoredTests = true };
            new ResultReporter(settings).WriteNotRunReport(_result, _writer);
            Assert.That(ReportLines.Where(line => line.Contains("Ignored")), Is.Empty);
        }

        [Test]
        public void TestsNotRun_OmitExplicitAndIgnored()
        {
            var settings = new ResultReporterSettings() { OmitExplicitTests = true, OmitIgnoredTests = true };
            new ResultReporter(settings).WriteNotRunReport(_result, _writer);
            Assert.That(ReportLines.Where(line => line.Contains("Explicit") || line.Contains("Ignored")), Is.Empty);
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

            ResultReporter.WriteRunSettingsReport(_result, _writer);
            Assert.That(ReportLines, Is.SupersetOf(expected));
        }

        private static TestEngineResult AddMetadata(TestEngineResult input)
        {
            return input.Aggregate("test-run start-time=\"2015-10-19 02:12:28Z\" end-time=\"2015-10-19 02:12:29Z\" duration=\"0.348616\"", string.Empty, string.Empty, string.Empty);
        }
    }
}