﻿// ***********************************************************************
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
using System.Xml;
using NUnit.Common;
using NUnit.Engine;
using NUnit.Framework;
using NUnit.Framework.Api;
using NUnit.Tests.Assemblies;
using NUnit.Engine.Internal;

namespace NUnit.ConsoleRunner.Tests
{

    public class ResultReporterTests
    {
        private XmlNode _result;
        private ResultReporter _reporter;
        private StringBuilder _report;

        [OneTimeSetUp]
        public void CreateResult()
        {
            var mockAssembly = typeof(MockAssembly).Assembly;
            var frameworkSettings = new Dictionary<string, object>
            {
                { "TestParameters", "1=d;2=c" },
                { "TestParametersDictionary", new Dictionary<string, string>
                {
                    { "1", "d" },
                    { "2", "c" }
                }}
            };

            var controller = new FrameworkController(mockAssembly, "id", frameworkSettings);

            controller.LoadTests();
            var xmlText = controller.RunTests(null);
            var engineResult = AddMetadata(new TestEngineResult(xmlText));
            _result = engineResult.Xml;

            Assert.NotNull(_result, "Unable to create report result.");
        }

        [SetUp]
        public void CreateReporter()
        {
            _report = new StringBuilder();
            var writer = new ExtendedTextWrapper(new StringWriter(_report));
            _reporter = new ResultReporter(_result, writer, new ConsoleOptions());
        }

        [Test]
        public void ReportSequenceTest()
        {
            var report = GetReport(_reporter.ReportResults);

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
            var expected = new[] {
                "Test Run Summary",
                "  Overall result: Failed",
               $"  Test Count: {MockAssembly.Tests}, Passed: {MockAssembly.Passed}, Failed: 5, Warnings: 1, Inconclusive: 1, Skipped: 7",
                "    Failed Tests - Failures: 1, Errors: 1, Invalid: 3",
                "    Skipped Tests - Ignored: 4, Explicit: 3, Other: 0",
                "  Start time: 2015-10-19 02:12:28Z",
                "    End time: 2015-10-19 02:12:29Z",
                "    Duration: 0.349 seconds",
                ""
            };

            var actualSummary = GetReportLines(_reporter.WriteSummaryReport);
            Assert.That(actualSummary, Is.EqualTo(expected));
        }

        [Test]
        public void ErrorsAndFailuresReportTest()
        {
            var nl = Environment.NewLine;

            var expected = new[] {
                "Errors, Failures and Warnings",
                "1) Failed : NUnit.Tests.Assemblies.MockTestFixture.FailingTest" + nl +
                "Intentional failure",
                "2) Invalid : NUnit.Tests.Assemblies.MockTestFixture.NonPublicTest" + nl +
                "Method is not public",
                "3) Invalid : NUnit.Tests.Assemblies.MockTestFixture.NotRunnableTest" + nl +
                "No arguments were provided",
                "4) Error : NUnit.Tests.Assemblies.MockTestFixture.TestWithException" + nl +
                "System.Exception : Intentional Exception",
                "5) Warning : NUnit.Tests.Assemblies.MockTestFixture.WarningTest" + nl +
                "Warning Message",
                "6) Invalid : NUnit.Tests.BadFixture" + nl +
                "No suitable constructor was found"
            };

            var actualErrorFailuresReport = GetReport(_reporter.WriteErrorsFailuresAndWarningsReport);

            foreach (var ex in expected)
            {
                Assert.That(actualErrorFailuresReport, Does.Contain(ex));
            }
        }

        [Test]
        public void TestsNotRunTest()
        {
            var expected = new[] {
                "Tests Not Run",
                "",
                "1) Explicit : NUnit.Tests.Assemblies.MockTestFixture.ExplicitTest",
                "",
                "2) Ignored : NUnit.Tests.Assemblies.MockTestFixture.IgnoreTest",
                "Ignore Message",
                "",
                "3) Explicit : NUnit.Tests.ExplicitFixture.Test1",
                "OneTimeSetUp: ",
                "",
                "4) Explicit : NUnit.Tests.ExplicitFixture.Test2",
                "OneTimeSetUp: ",
                "",
                "5) Ignored : NUnit.Tests.IgnoredFixture.Test1",
                "OneTimeSetUp: BECAUSE",
                "",
                "6) Ignored : NUnit.Tests.IgnoredFixture.Test2",
                "OneTimeSetUp: BECAUSE",
                "",
                "7) Ignored : NUnit.Tests.IgnoredFixture.Test3",
                "OneTimeSetUp: BECAUSE",
                ""
            };

            var report = GetReportLines(_reporter.WriteNotRunReport);
            Assert.That(report, Is.EqualTo(expected));
        }

        [Test, Explicit("Displays failure behavior")]
        public void WarningsOnlyDisplayOnce()
        {
            Assert.Warn("Just a warning");
        }

        [Test]
        public void TestParameterSettingsWrittenCorrectly()
        {
            var expected = new[] {
                "    TestParameters: 1=d;2=c",
                "    TestParametersDictionary:",
                "        1 -> d",
                "        2 -> c"
            };

            var report = GetReportLines(_reporter.WriteRunSettingsReport);
            Assert.That(report, Is.SupersetOf(expected));
        }

        private static TestEngineResult AddMetadata(TestEngineResult input)
        {
            return input.Aggregate("test-run start-time=\"2015-10-19 02:12:28Z\" end-time=\"2015-10-19 02:12:29Z\" duration=\"0.348616\"", string.Empty, string.Empty);
        }

        private string GetReport(TestDelegate del)
        {
            del();
            return _report.ToString();
        }

        private IList<string> GetReportLines(TestDelegate del)
        {
            var rdr = new StringReader(GetReport(del));

            string line;
            var lines = new List<string>();
            while ((line = rdr.ReadLine()) != null)
                lines.Add(line);

            return lines;
        }
    }
}