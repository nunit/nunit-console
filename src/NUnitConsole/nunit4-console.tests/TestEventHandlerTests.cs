﻿// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using System;
using System.IO;
using System.Text;
using NUnit.Framework;

namespace NUnit.ConsoleRunner
{
    public class TestEventHandlerTests
    {
        private StringBuilder _output;
        private ExtendedTextWriter _writer;

        private string Output {  get { return _output.ToString(); } }

        [SetUp]
        public void CreateWriter()
        {
            _output = new StringBuilder();
            _writer = new ExtendedTextWrapper(new StringWriter(_output));
        }

        [TearDown]
        public void DisposeWriter()
        {
            _writer.Dispose();
        }

        [TestCase(char.MaxValue)]
        public void TestNameContainsInvalidChar(char c)
        {
            Console.WriteLine($"Test for char {c}");
        }

        [TestCaseSource(nameof(SingleEventData))]
        public void SingleEventsWriteExpectedOutput(string report, string labels, string expected)
        {
            var handler = new TestEventHandler(_writer, labels);

            handler.OnTestEvent(report);

            if (Environment.NewLine != "\r\n")
                expected = expected.Replace("\r\n", Environment.NewLine);
            Assert.That(Output, Is.EqualTo(expected));
        }

        [TestCaseSource(nameof(MultipleEventData))]
        public void MultipleEvents(string[] reports, string labels, string expected)
        {
            var handler = new TestEventHandler(_writer, labels);

            foreach (string report in reports)
                handler.OnTestEvent(report);

            // Make sure all newlines are the same
            expected = expected.Replace("\r\n", "\n");

            // Replace with current newline before comparing
            expected = expected.Replace("\n", Environment.NewLine);

            Assert.That(Output, Is.EqualTo(expected));
        }

        static readonly TestCaseData[] SingleEventData = new TestCaseData[]
        {
            // Start Events
            new TestCaseData("<start-test fullname='SomeName'/>", "Off", ""),
            new TestCaseData("<start-test fullname='SomeName'/>", "OnOutputOnly", ""),
            new TestCaseData("<start-test fullname='SomeName'/>", "Before", "=> SomeName\r\n"),
            new TestCaseData("<start-test fullname='SomeName'/>", "After", ""),
            new TestCaseData("<start-test fullname='SomeName'/>", "BeforeAndAfter", "=> SomeName\r\n"),
            new TestCaseData("<start-suite fullname='SomeName'/>", "Off", ""),
            new TestCaseData("<start-suite fullname='SomeName'/>", "OnOutputOnly", ""),
            new TestCaseData("<start-suite fullname='SomeName'/>", "Before", ""),
            new TestCaseData("<start-suite fullname='SomeName'/>", "After", ""),
            new TestCaseData("<start-suite fullname='SomeName'/>", "BeforeAndAfter", ""),
            // Finish Events - No Output
            new TestCaseData("<test-case fullname='SomeName' result='Passed'/>", "Off", ""),
            new TestCaseData("<test-case fullname='SomeName' result='Passed'/>", "OnOutputOnly", ""),
            new TestCaseData("<test-case fullname='SomeName' result='Passed'/>", "Before", ""),
            new TestCaseData("<test-case fullname='SomeName' result='Passed'/>", "After", "Passed => SomeName\r\n"),
            new TestCaseData("<test-case fullname='SomeName' result='Passed'/>", "BeforeAndAfter", "Passed => SomeName\r\n"),
            new TestCaseData("<test-case fullname='SomeName' result='Failed'/>", "After", "Failed => SomeName\r\n"),
            new TestCaseData("<test-case fullname='SomeName' result='Failed' label='Invalid'/>", "After", "Invalid => SomeName\r\n"),
            new TestCaseData("<test-case fullname='SomeName' result='Failed' label='Cancelled'/>", "After", "Cancelled => SomeName\r\n"),
            new TestCaseData("<test-case fullname='SomeName' result='Failed' label='Error'/>", "After", "Error => SomeName\r\n"),
            new TestCaseData("<test-case fullname='SomeName' result='Failed'/>", "BeforeAndAfter", "Failed => SomeName\r\n"),
            new TestCaseData("<test-case fullname='SomeName' result='Failed' label='Invalid'/>", "BeforeAndAfter", "Invalid => SomeName\r\n"),
            new TestCaseData("<test-case fullname='SomeName' result='Failed' label='Cancelled'/>", "BeforeAndAfter", "Cancelled => SomeName\r\n"),
            new TestCaseData("<test-case fullname='SomeName' result='Failed' label='Error'/>", "BeforeAndAfter", "Error => SomeName\r\n"),
            new TestCaseData("<test-case fullname='SomeName' result='Warning'/>", "After", "Warning => SomeName\r\n"),
            new TestCaseData("<test-case fullname='SomeName' result='Warning'/>", "BeforeAndAfter", "Warning => SomeName\r\n"),
            new TestCaseData("<test-case fullname='SomeName' result='Inconclusive'/>", "After", "Inconclusive => SomeName\r\n"),
            new TestCaseData("<test-case fullname='SomeName' result='Inconclusive'/>", "BeforeAndAfter", "Inconclusive => SomeName\r\n"),
            new TestCaseData("<test-case fullname='SomeName' result='Skipped'/>", "After", "Skipped => SomeName\r\n"),
            new TestCaseData("<test-case fullname='SomeName' result='Skipped'/>", "BeforeAndAfter", "Skipped => SomeName\r\n"),
            new TestCaseData("<test-case fullname='SomeName' result='Skipped' label='Ignored'/>", "After", "Ignored => SomeName\r\n"),
            new TestCaseData("<test-case fullname='SomeName' result='Skipped' label='Ignored'/>", "BeforeAndAfter", "Ignored => SomeName\r\n"),
            new TestCaseData("<test-suite fullname='SomeName' result='Passed'/>", "Off", ""),
            new TestCaseData("<test-suite fullname='SomeName' result='Passed'/>", "OnOutputOnly", ""),
            new TestCaseData("<test-suite fullname='SomeName' result='Passed'/>", "Before", ""),
            new TestCaseData("<test-suite fullname='SomeName' result='Passed'/>", "After", ""),
            new TestCaseData("<test-suite fullname='SomeName' result='Passed'/>", "BeforeAndAfter", ""),
            // Finish Events - With Output
            new TestCaseData(
                "<test-case fullname='SomeName' result='Passed'><output>OUTPUT</output></test-case>",
                "Off",
                "OUTPUT"),
            new TestCaseData(
                "<test-case fullname='SomeName' result='Passed'><output>OUTPUT</output></test-case>",
                "OnOutputOnly",
                "=> SomeName\r\nOUTPUT"),
            new TestCaseData(
                "<test-case fullname='SomeName' result='Passed'><output>OUTPUT</output></test-case>",
                "Before",
                "=> SomeName\r\nOUTPUT"),
            new TestCaseData(
                "<test-case fullname='SomeName' result='Passed'><output>OUTPUT</output></test-case>",
                "After",
                "=> SomeName\r\nOUTPUT\r\nPassed => SomeName\r\n"),
            new TestCaseData(
                "<test-case fullname='SomeName' result='Passed'><output>OUTPUT</output></test-case>",
                "BeforeAndAfter",
                "=> SomeName\r\nOUTPUT\r\nPassed => SomeName\r\n"),
            new TestCaseData(
                "<test-case fullname='SomeName' result='Failed'><output>OUTPUT</output></test-case>",
                "After",
                "=> SomeName\r\nOUTPUT\r\nFailed => SomeName\r\n"),
            new TestCaseData(
                "<test-case fullname='SomeName' result='Failed' label='Error'><output>OUTPUT</output></test-case>",
                "After",
                "=> SomeName\r\nOUTPUT\r\nError => SomeName\r\n"),
            new TestCaseData(
                "<test-case fullname='SomeName' result='Failed' label='Invalid'><output>OUTPUT</output></test-case>",
                "After",
                "=> SomeName\r\nOUTPUT\r\nInvalid => SomeName\r\n"),
            new TestCaseData(
                "<test-case fullname='SomeName' result='Failed' label='Cancelled'><output>OUTPUT</output></test-case>",
                "After",
                "=> SomeName\r\nOUTPUT\r\nCancelled => SomeName\r\n"),
            new TestCaseData(
                "<test-case fullname='SomeName' result='Warning'><output>OUTPUT</output></test-case>",
                "After",
                "=> SomeName\r\nOUTPUT\r\nWarning => SomeName\r\n"),
            new TestCaseData(
                "<test-case fullname='SomeName' result='Inconclusive'><output>OUTPUT</output></test-case>",
                "After",
                "=> SomeName\r\nOUTPUT\r\nInconclusive => SomeName\r\n"),
            new TestCaseData(
                "<test-case fullname='SomeName' result='Skipped'><output>OUTPUT</output></test-case>",
                "After",
                "=> SomeName\r\nOUTPUT\r\nSkipped => SomeName\r\n"),
            new TestCaseData(
                "<test-case fullname='SomeName' result='Skipped' label='Ignored'><output>OUTPUT</output></test-case>",
                "After",
                "=> SomeName\r\nOUTPUT\r\nIgnored => SomeName\r\n"),
            new TestCaseData(
                "<test-case fullname='SomeName' result='Failed' label='Error'><output>OUTPUT</output></test-case>",
                "BeforeAndAfter",
                "=> SomeName\r\nOUTPUT\r\nError => SomeName\r\n"),
            new TestCaseData(
                "<test-case fullname='SomeName' result='Failed' label='Invalid'><output>OUTPUT</output></test-case>",
                "BeforeAndAfter",
                "=> SomeName\r\nOUTPUT\r\nInvalid => SomeName\r\n"),
            new TestCaseData(
                "<test-case fullname='SomeName' result='Failed' label='Cancelled'><output>OUTPUT</output></test-case>",
                "BeforeAndAfter",
                "=> SomeName\r\nOUTPUT\r\nCancelled => SomeName\r\n"),
            new TestCaseData(
                "<test-case fullname='SomeName' result='Warning'><output>OUTPUT</output></test-case>",
                "BeforeAndAfter",
                "=> SomeName\r\nOUTPUT\r\nWarning => SomeName\r\n"),
            new TestCaseData(
                "<test-case fullname='SomeName' result='Inconclusive'><output>OUTPUT</output></test-case>",
                "BeforeAndAfter",
                "=> SomeName\r\nOUTPUT\r\nInconclusive => SomeName\r\n"),
            new TestCaseData(
                "<test-case fullname='SomeName' result='Skipped'><output>OUTPUT</output></test-case>",
                "BeforeAndAfter",
                "=> SomeName\r\nOUTPUT\r\nSkipped => SomeName\r\n"),
            new TestCaseData(
                "<test-case fullname='SomeName' result='Skipped' label='Ignored'><output>OUTPUT</output></test-case>",
                "BeforeAndAfter",
                "=> SomeName\r\nOUTPUT\r\nIgnored => SomeName\r\n"),
            new TestCaseData(
                "<test-suite fullname='SomeName' result='Passed'><output>OUTPUT</output></test-suite>",
                "Off",
                "OUTPUT"),
            new TestCaseData(
                "<test-suite fullname='SomeName' result='Passed'><output>OUTPUT</output></test-suite>",
                "OnOutputOnly",
                "=> SomeName\r\nOUTPUT"),
            new TestCaseData(
                "<test-suite fullname='SomeName' result='Passed'><output>OUTPUT</output></test-suite>",
                "Before",
                "=> SomeName\r\nOUTPUT"),
            new TestCaseData(
                "<test-suite fullname='SomeName' result='Passed'><output>OUTPUT</output></test-suite>",
                "After",
                "=> SomeName\r\nOUTPUT"),
            new TestCaseData(
                "<test-suite fullname='SomeName' result='Passed'><output>OUTPUT</output></test-suite>",
                "BeforeAndAfter",
                "=> SomeName\r\nOUTPUT"),
            // Output Events
            new TestCaseData(
                "<test-output testname='SomeName'>OUTPUT</test-output>",
                "Off",
                "OUTPUT"),
            new TestCaseData(
                "<test-output testname='SomeName'>OUTPUT</test-output>",
                "OnOutputOnly",
                "=> SomeName\r\nOUTPUT"),
            new TestCaseData(
                "<test-output testname='SomeName'>OUTPUT</test-output>",
                "Before",
                "=> SomeName\r\nOUTPUT"),
            new TestCaseData(
                "<test-output testname='SomeName'>OUTPUT</test-output>",
                "After",
                "=> SomeName\r\nOUTPUT"),
            new TestCaseData(
                "<test-output testname='SomeName'>OUTPUT</test-output>",
                "BeforeAndAfter",
                "=> SomeName\r\nOUTPUT")
        };

        static string[] SingleTest_StartAndFinish = new string[]
        {
            "<start-test fullname='TestName'/>",
            "<test-case fullname='TestName' result='Passed'><output>OUTPUT</output></test-case>"
        };

        static string[] SingleTest_ImmediateOutput = new string[]
        {
            "<start-test fullname='TEST1'/>",
            "<test-output testname='TEST1'>Immediate output from TEST1</test-output>",
            string.Format("<test-case fullname='TEST1' result='Passed'><output>Output{0}from{0}TEST1</output></test-case>", Environment.NewLine)
        };

        static string[] SingleTest_SuiteFinish = new string[]
        {
            "<test-output testname='TEST1'>Immediate output from TEST1</test-output>",
            string.Format("<test-suite fullname='TEST1' result='Passed'><output>Output from Suite TEST1</output></test-suite>")
        };

        static string[] TwoTests_SequentialExecution = new string[]
        {
            "<start-test fullname='TEST1'/>",
            "<test-case fullname='TEST1' result='Failed'><output>Output from first test</output></test-case>",
            "<start-test fullname='TEST2'/>",
            "<test-case fullname='TEST2' result='Passed'><output>Output from second test</output></test-case>"
        };

        static string[] TwoTests_InterleavedExecution = new string[]
        {
            "<start-test fullname='TEST1'/>",
            "<test-output testname='TEST1'>Immediate output from first test</test-output>",
            "<start-test fullname='TEST2'/>",
            "<test-output testname='TEST1'>Another immediate output from first test</test-output>",
            "<test-output testname='TEST2'>Immediate output from second test</test-output>",
            "<test-case fullname='TEST1' result='Failed'><output>Output from first test</output></test-case>",
            "<test-case fullname='TEST2' result='Passed'><output>Output from second test</output></test-case>"
        };

        static string[] TwoTests_NestedExecution = new string[]
        {
            "<start-test fullname='TEST1'/>",
            "<test-output testname='TEST1'>Immediate output from first test</test-output>",
            "<start-test fullname='TEST2'/>",
            "<test-output testname='TEST2'>Immediate output from second test</test-output>",
            "<test-case fullname='TEST2' result='Passed'><output>Output from second test</output></test-case>",
            "<test-case fullname='TEST1' result='Failed'><output>Output from first test</output></test-case>"
        };

        static readonly TestCaseData[] MultipleEventData = new TestCaseData[]
        {
            new TestCaseData(
                SingleTest_StartAndFinish,
                "Off",
                "OUTPUT"),
            new TestCaseData(
                SingleTest_StartAndFinish,
                "OnOutputOnly",
                "=> TestName\r\nOUTPUT"),
            new TestCaseData(
                SingleTest_StartAndFinish,
                "Before",
                "=> TestName\r\nOUTPUT"),
            new TestCaseData(
                SingleTest_StartAndFinish,
                "After",
                "=> TestName\r\nOUTPUT\r\nPassed => TestName\r\n"),
            new TestCaseData(
                SingleTest_StartAndFinish,
                "BeforeAndAfter",
                "=> TestName\r\nOUTPUT\r\nPassed => TestName\r\n"),
            new TestCaseData(
                SingleTest_ImmediateOutput,
                "Off",
@"Immediate output from TEST1
Output
from
TEST1"),
            new TestCaseData(
                SingleTest_ImmediateOutput,
                "OnOutputOnly",
@"=> TEST1
Immediate output from TEST1
Output
from
TEST1"),
            new TestCaseData(
                SingleTest_ImmediateOutput,
                "Before",
@"=> TEST1
Immediate output from TEST1
Output
from
TEST1"),
            new TestCaseData(
                SingleTest_ImmediateOutput,
                "After",
@"=> TEST1
Immediate output from TEST1
Output
from
TEST1
Passed => TEST1
"),
            new TestCaseData(
                SingleTest_ImmediateOutput,
                "BeforeAndAfter",
                @"=> TEST1
Immediate output from TEST1
Output
from
TEST1
Passed => TEST1
"),
            new TestCaseData(
                SingleTest_SuiteFinish,
                "Off",
@"Immediate output from TEST1
Output from Suite TEST1"),
            new TestCaseData(
                SingleTest_SuiteFinish,
                "OnOutputOnly",
@"=> TEST1
Immediate output from TEST1
Output from Suite TEST1"),
            new TestCaseData(
                SingleTest_SuiteFinish,
                "Before",
@"=> TEST1
Immediate output from TEST1
Output from Suite TEST1"),
            new TestCaseData(
                SingleTest_SuiteFinish,
                "After",
@"=> TEST1
Immediate output from TEST1
Output from Suite TEST1"),
            new TestCaseData(
                SingleTest_SuiteFinish,
                "BeforeAndAfter",
                @"=> TEST1
Immediate output from TEST1
Output from Suite TEST1"),
            new TestCaseData(
                TwoTests_SequentialExecution,
                "Off",
@"Output from first test
Output from second test"),
            new TestCaseData(
                TwoTests_SequentialExecution,
                "OnOutputOnly",
@"=> TEST1
Output from first test
=> TEST2
Output from second test"),
            new TestCaseData(
                TwoTests_SequentialExecution,
                "Before",
@"=> TEST1
Output from first test
=> TEST2
Output from second test"),
            new TestCaseData(
                TwoTests_SequentialExecution,
                "After",
@"=> TEST1
Output from first test
Failed => TEST1
=> TEST2
Output from second test
Passed => TEST2
"),
            new TestCaseData(
                TwoTests_SequentialExecution,
                "BeforeAndAfter",
                @"=> TEST1
Output from first test
Failed => TEST1
=> TEST2
Output from second test
Passed => TEST2
"),
            new TestCaseData(
                TwoTests_InterleavedExecution,
                "Off",
@"Immediate output from first testAnother immediate output from first test
Immediate output from second test
Output from first test
Output from second test"),
            new TestCaseData(
                TwoTests_InterleavedExecution,
                "OnOutputOnly",
@"=> TEST1
Immediate output from first testAnother immediate output from first test
=> TEST2
Immediate output from second test
=> TEST1
Output from first test
=> TEST2
Output from second test"),
            new TestCaseData(
                TwoTests_InterleavedExecution,
                "Before",
@"=> TEST1
Immediate output from first test
=> TEST2
=> TEST1
Another immediate output from first test
=> TEST2
Immediate output from second test
=> TEST1
Output from first test
=> TEST2
Output from second test"),
            new TestCaseData(
                TwoTests_InterleavedExecution,
                "After",
@"=> TEST1
Immediate output from first testAnother immediate output from first test
=> TEST2
Immediate output from second test
=> TEST1
Output from first test
Failed => TEST1
=> TEST2
Output from second test
Passed => TEST2
"),
            new TestCaseData(
                TwoTests_InterleavedExecution,
                "BeforeAndAfter",
                @"=> TEST1
Immediate output from first test
=> TEST2
=> TEST1
Another immediate output from first test
=> TEST2
Immediate output from second test
=> TEST1
Output from first test
Failed => TEST1
=> TEST2
Output from second test
Passed => TEST2
"),
            new TestCaseData(
                TwoTests_NestedExecution,
                "Off",
@"Immediate output from first test
Immediate output from second test
Output from second test
Output from first test"),
            new TestCaseData(
                TwoTests_NestedExecution,
                "OnOutputOnly",
@"=> TEST1
Immediate output from first test
=> TEST2
Immediate output from second test
Output from second test
=> TEST1
Output from first test"),
            new TestCaseData(
                TwoTests_NestedExecution,
                "Before",
@"=> TEST1
Immediate output from first test
=> TEST2
Immediate output from second test
Output from second test
=> TEST1
Output from first test"),
            new TestCaseData(
                TwoTests_NestedExecution,
                "After",
@"=> TEST1
Immediate output from first test
=> TEST2
Immediate output from second test
Output from second test
Passed => TEST2
=> TEST1
Output from first test
Failed => TEST1
"),
            new TestCaseData(
                TwoTests_NestedExecution,
                "BeforeAndAfter",
                @"=> TEST1
Immediate output from first test
=> TEST2
Immediate output from second test
Output from second test
Passed => TEST2
=> TEST1
Output from first test
Failed => TEST1
")
        };
#pragma warning restore 414
    }
}
