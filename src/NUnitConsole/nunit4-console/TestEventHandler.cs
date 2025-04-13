// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using System;
using System.Xml;
using NUnit.Common;
using NUnit.Engine;
using NUnit.TextDisplay;

namespace NUnit.ConsoleRunner
{
    /// <summary>
    /// TestEventHandler processes events from the running
    /// test for the console runner.
    /// </summary>
    public class TestEventHandler : ITestEventListener
    {
        private readonly ExtendedTextWriter _outWriter;

        private readonly bool _displayBeforeTest;
        private readonly bool _displayAfterTest;
        private readonly bool _displayBeforeOutput;

        private string? _lastTestOutput;
        private bool _wantNewLine = false;

        public TestEventHandler(ExtendedTextWriter outWriter, string labelsOption)
        {
            _outWriter = outWriter;

            labelsOption = labelsOption.ToUpperInvariant();
            _displayBeforeTest = labelsOption == "BEFORE" || labelsOption == "BEFOREANDAFTER";
            _displayAfterTest = labelsOption == "AFTER" || labelsOption == "BEFOREANDAFTER";
            _displayBeforeOutput = _displayBeforeTest || _displayAfterTest || labelsOption == "ONOUTPUTONLY";
        }

        public void OnTestEvent(string report)
        {
            var doc = new XmlDocument();
            doc.LoadXml(report);

            var testEvent = doc.FirstChild;
            if (testEvent == null)
                return;

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
            var testName = testResult.Attributes?["fullname"]?.Value;

            if (_displayBeforeTest && testName != null)
                WriteLabelLine(testName);
        }

        private void TestFinished(XmlNode testResult)
        {
            var testName = testResult.Attributes?["fullname"]?.Value;

            if (testName == null)
                return;

            var status = testResult.GetAttribute("label") ?? testResult.GetAttribute("result") ?? "Unknown";
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
            var suiteName = testResult.Attributes?["fullname"]?.Value;
            var outputNode = testResult.SelectSingleNode("output");

            if (suiteName != null && outputNode != null)
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

            if (testName != null)
            {
                if (_displayBeforeOutput)
                    WriteLabelLine(testName);

                var stream = outputNode.GetAttribute("stream");
                WriteOutputLine(testName, outputNode.InnerText, stream == "Error" ? ColorStyle.Error : ColorStyle.Output);
            }
        }

        private string? _currentLabel;

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
            if (!text.EndsWith('\n'))
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
    }
}
