// ***********************************************************************
// Copyright (c) 2007 Charlie Poole, Rob Prouse
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
using System.Xml;
using NUnit.Engine;

namespace NUnit.ConsoleRunner
{
    /// <summary>
    /// TestEventHandler processes events from the running
    /// test for the console runner.
    /// </summary>
#if NET20
    public class TestEventHandler : MarshalByRefObject, ITestEventListener
#else
    public class TestEventHandler : ITestEventListener
#endif
    {
        private readonly ExtendedTextWriter _outWriter;

        private readonly bool _displayBeforeTest;
        private readonly bool _displayAfterTest;
        private readonly bool _displayBeforeOutput;

        private string _lastTestOutput;
        private bool _wantNewLine = false;

        public TestEventHandler(ExtendedTextWriter outWriter, string labelsOption)
        {
            _outWriter = outWriter;

            labelsOption = labelsOption.ToUpperInvariant();
            _displayBeforeTest = labelsOption == "ALL" || labelsOption == "BEFORE" || labelsOption == "BEFOREANDAFTER";
            _displayAfterTest = labelsOption == "AFTER" || labelsOption == "BEFOREANDAFTER";
            _displayBeforeOutput = _displayBeforeTest || _displayAfterTest || labelsOption == "ON";
        }

        public void OnTestEvent(string report)
        {
            var doc = new XmlDocument();
            doc.LoadXml(report);

            var testEvent = doc.FirstChild;
            switch (testEvent.Name)
            {
                case "start-test":
                    TestStarted(testEvent);
                    break;

                case "test-case":
                    TestFinished(testEvent);
                    break;

                case "test-suite":
                    SuiteFinished(testEvent);
                    break;

                case "test-output":
                    TestOutput(testEvent);
                    break;
            }
        }

        private void TestStarted(XmlNode testResult)
        {
            var testName = testResult.Attributes["fullname"].Value;

            if (_displayBeforeTest)
                WriteLabelLine(testName);
        }

        private void TestFinished(XmlNode testResult)
        {
            var testName = testResult.Attributes["fullname"].Value;
            var status = testResult.GetAttribute("label") ?? testResult.GetAttribute("result");
            var outputNode = testResult.SelectSingleNode("output");

            if (outputNode != null)
            {
                if (_displayBeforeOutput)
                    WriteLabelLine(testName);

                FlushNewLineIfNeeded();
                WriteOutputLine(testName, outputNode.InnerText);
            }

            if (_displayAfterTest)
                WriteLabelLineAfterTest(testName, status);
        }

        private void SuiteFinished(XmlNode testResult)
        {
            var suiteName = testResult.Attributes["fullname"].Value;
            var outputNode = testResult.SelectSingleNode("output");

            if (outputNode != null)
            {
                if (_displayBeforeOutput)
                    WriteLabelLine(suiteName);

                FlushNewLineIfNeeded();
                WriteOutputLine(suiteName, outputNode.InnerText);
            }
        }

        private void TestOutput(XmlNode outputNode)
        {
            var testName = outputNode.GetAttribute("testname");
            var stream = outputNode.GetAttribute("stream");

            if (_displayBeforeOutput && testName != null)
                WriteLabelLine(testName);

            WriteOutputLine(testName, outputNode.InnerText, stream == "Error" ? ColorStyle.Error : ColorStyle.Output);
        }

        private string _currentLabel;

        private void WriteLabelLine(string label)
        {
            if (label != _currentLabel)
            {
                FlushNewLineIfNeeded();
                _lastTestOutput = label;

                _outWriter.WriteLine(ColorStyle.SectionHeader, $"=> {label}");

                _currentLabel = label;
            }
        }

        private void WriteLabelLineAfterTest(string label, string status)
        {
            FlushNewLineIfNeeded();
            _lastTestOutput = label;

            if (status != null)
            {
                _outWriter.Write(GetColorForResultStatus(status), $"{status} ");
            }

            _outWriter.WriteLine(ColorStyle.SectionHeader, $"=> {label}");

            _currentLabel = label;
        }

        private void WriteOutputLine(string testName, string text)
        {
            WriteOutputLine(testName, text, ColorStyle.Output);
        }

        private void WriteOutputLine(string testName, string text, ColorStyle color)
        {
            if (_lastTestOutput != testName)
            {
                FlushNewLineIfNeeded();
                _lastTestOutput = testName;
            }

            _outWriter.Write(color, text);

            // If the text we just wrote did not have a new line, flag that we should eventually emit one.
            if (!text.EndsWith("\n"))
            {
                _wantNewLine = true;
            }
        }

        private void FlushNewLineIfNeeded()
        {
            if (_wantNewLine)
            {
                _outWriter.WriteLine();
                _wantNewLine = false;
            }
        }

        private static ColorStyle GetColorForResultStatus(string status)
        {
            switch (status)
            {
                case "Passed":
                    return ColorStyle.Pass;
                case "Failed":
                    return ColorStyle.Failure;
                case "Error":
                case "Invalid":
                case "Cancelled":
                    return ColorStyle.Error;
                case "Warning":
                case "Ignored":
                    return ColorStyle.Warning;
                default:
                    return ColorStyle.Output;
            }
        }

#if NET20
        public override object InitializeLifetimeService()
        {
            return null;
        }
#endif
    }
}
