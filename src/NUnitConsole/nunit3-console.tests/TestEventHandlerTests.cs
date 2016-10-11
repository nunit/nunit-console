// ***********************************************************************
// Copyright (c) 2016 Charlie Poole
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
using System.IO;
using System.Text;
using NUnit.Framework;

namespace NUnit.ConsoleRunner.Tests
{
    public class TestEventHandlerTests
    {
        private StringBuilder _output;
        private TextWriter _writer;

        private string Output {  get { return _output.ToString(); } }

        [SetUp]
        public void CreateWriter()
        {
            _output = new StringBuilder();
            _writer = new StringWriter(_output);
        }

        [TestCaseSource("EventData")]
        public void EventsWriteExpectedOutput(string report, string labels, string expected)
        {
            var handler = new TestEventHandler(_writer, labels);

            handler.OnTestEvent(report);

            if (Environment.NewLine != "\r\n")
                expected = expected.Replace("\r\n", Environment.NewLine);
            Assert.That(Output, Is.EqualTo(expected));
        }

#pragma warning disable 414
        static TestCaseData[] EventData = new TestCaseData[]
        {
            // Start Events
            new TestCaseData("<start-test fullname='SomeName'/>", "Off", ""),
            new TestCaseData("<start-test fullname='SomeName'/>", "On", ""),
            new TestCaseData("<start-test fullname='SomeName'/>", "Before", "=> SomeName\r\n"),
            new TestCaseData("<start-test fullname='SomeName'/>", "After", ""),
            new TestCaseData("<start-test fullname='SomeName'/>", "All", "=> SomeName\r\n"),
            new TestCaseData("<start-suite fullname='SomeName'/>", "Off", ""),
            new TestCaseData("<start-suite fullname='SomeName'/>", "On", ""),
            new TestCaseData("<start-suite fullname='SomeName'/>", "Before", ""),
            new TestCaseData("<start-suite fullname='SomeName'/>", "After", ""),
            new TestCaseData("<start-suite fullname='SomeName'/>", "All", ""),
            // Finish Events - No Output
            new TestCaseData("<test-case fullname='SomeName'/>", "Off", ""),
            new TestCaseData("<test-case fullname='SomeName'/>", "On", ""),
            new TestCaseData("<test-case fullname='SomeName'/>", "Before", ""),
            new TestCaseData("<test-case fullname='SomeName'/>", "After", "=> SomeName\r\n"),
            new TestCaseData("<test-case fullname='SomeName'/>", "All", ""),
            new TestCaseData("<test-suite fullname='SomeName'/>", "Off", ""),
            new TestCaseData("<test-suite fullname='SomeName'/>", "On", ""),
            new TestCaseData("<test-suite fullname='SomeName' result='Passed'/>", "Before", ""),
            new TestCaseData("<test-suite fullname='SomeName' result='Passed'/>", "After", ""),

            // Finish Events - With Output
            new TestCaseData(
                "<test-case fullname='SomeName' result='Passed'><output>OUTPUT</output></test-case>",
                "Off",
                "OUTPUT\r\n"),
            new TestCaseData(
                "<test-case fullname='SomeName' result='Passed'><output>OUTPUT</output></test-case>",
                "On",
                "=> SomeName\r\nOUTPUT\r\n"),
            new TestCaseData(
                "<test-case fullname='SomeName' result='Passed'><output>OUTPUT</output></test-case>",
                "After", 
                "PASSED => SomeName\r\nOUTPUT\r\n"),
            new TestCaseData(
                "<test-case fullname='SomeName' result='Passed' />", 
                "After", 
                "PASSED => SomeName\r\n"),
            new TestCaseData(
                "<test-case fullname='SomeName' result='Failed' />", 
                "After", 
                "FAILED => SomeName\r\n"),
            new TestCaseData(
                "<test-case fullname='SomeName' result='Failed'><output>OUTPUT</output></test-case>", 
                "After", 
                "FAILED => SomeName\r\nOUTPUT\r\n"),
            new TestCaseData(
                "<test-case fullname='SomeName' result='Failed' label='Invalid'><output>OUTPUT</output></test-case>", 
                "After", 
                "INVALID => SomeName\r\nOUTPUT\r\n"),
            new TestCaseData(
                "<test-case fullname='SomeName' result='Failed' label='Error'><output>OUTPUT</output></test-case>", 
                "After", 
                "ERROR => SomeName\r\nOUTPUT\r\n"),
            new TestCaseData(
                "<test-case fullname='SomeName' result='Failed' label='Cancelled'><output>OUTPUT</output></test-case>", 
                "After", 
                "CANCELLED => SomeName\r\nOUTPUT\r\n"),
            new TestCaseData(
                "<test-case fullname='SomeName' result='Skipped'><output>OUTPUT</output></test-case>", 
                "After", 
                "SKIPPED => SomeName\r\nOUTPUT\r\n"),
            new TestCaseData(
                "<test-case fullname='SomeName' result='Skipped' label='Ignored'><output>OUTPUT</output></test-case>", 
                "After", 
                "IGNORED => SomeName\r\nOUTPUT\r\n"),
            new TestCaseData(
                "<test-case fullname='SomeName' result='Inconclusive'><output>OUTPUT</output></test-case>", 
                "After", 
                "INCONCLUSIVE => SomeName\r\nOUTPUT\r\n"),
            new TestCaseData(
                "<test-case fullname='SomeName'><output>OUTPUT</output></test-case>",
                "All", 
                "OUTPUT\r\n"),
            new TestCaseData(
                "<test-suite fullname='SomeName' result='Passed'><output>OUTPUT</output></test-suite>",
                "Off", 
                "OUTPUT\r\n"),
            new TestCaseData(
                "<test-suite fullname='SomeName'><output>OUTPUT</output></test-suite>",
                "On",
                "=> SomeName\r\nOUTPUT\r\n"),
            new TestCaseData(
                "<test-suite fullname='SomeName'><output>OUTPUT</output></test-suite>",
                "After",
                "=> SomeName\r\nOUTPUT\r\n"),
            new TestCaseData(
                "<test-suite fullname='SomeName'><output>OUTPUT</output></test-suite>",
                "All",
                "OUTPUT\r\n"),
            // Output Events
            new TestCaseData(
                "<test-output>OUTPUT</test-output>",
                "Off",
                "OUTPUT\r\n"),
            new TestCaseData(
                "<test-output>OUTPUT</test-output>",
                "On",
                "OUTPUT\r\n"),
            new TestCaseData(
                "<test-output>OUTPUT</test-output>",
                "After",
                "OUTPUT\r\n")
        };
#pragma warning restore 414
    }
}
