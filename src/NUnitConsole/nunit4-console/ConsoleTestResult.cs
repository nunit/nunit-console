﻿// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using NUnit.TextDisplay;

namespace NUnit.ConsoleRunner
{
    /// <summary>
    /// ConsoleTestResult represents the result of one test being
    /// displayed in the console report.
    /// </summary>
    public class ConsoleTestResult
    {
        private static readonly char[] EOL_CHARS = new char[] { '\r', '\n' };

        private readonly XmlNode _resultNode;

        /// <summary>
        /// ConsoleTestResult represents the result of a test in the console runner.
        /// </summary>
        /// <param name="resultNode">An XmlNode representing the result</param>
        /// <param name="reportIndex">Integer index used in the report listing</param>
        public ConsoleTestResult(XmlNode resultNode, int reportIndex)
        {
            _resultNode = resultNode;
            ReportID = reportIndex.ToString();

            Result = resultNode.GetAttribute("result");
            Label = resultNode.GetAttribute("label");
            Site = resultNode.GetAttribute("site");

            Status = Label ?? Result ?? "Unkown";
            if (Status == "Failed" || Status == "Error")
                if (Site == "SetUp" || Site == "TearDown")
                    Status = Site + " " + Status;

            FullName = resultNode.GetAttribute("fullname") ?? "Unknown";
        }

        public string? Result { get; private set; }
        public string? Label { get; private set; }
        public string? Site { get; private set; }
        public string FullName { get; private set; }

        public string ReportID { get; private set; }
        public string Status { get; private set; }

        public string? Message
        {
            get
            {
                return GetTrimmedInnerText(_resultNode.SelectSingleNode("failure/message")) ??
                       GetTrimmedInnerText(_resultNode.SelectSingleNode("reason/message"));
            }
        }

        public string? StackTrace
        {
            get { return GetTrimmedInnerText(_resultNode.SelectSingleNode("failure/stack-trace")); }
        }

        private List<AssertionResult>? _assertions;
        public List<AssertionResult> Assertions
        {
            get
            {
                if (_assertions == null)
                {
                    _assertions = new List<AssertionResult>();
                    XmlNodeList? assertions = _resultNode.SelectNodes("assertions/assertion");
                    if (assertions is not null)
                        foreach (XmlNode assertion in assertions)
                            Assertions.Add(new ConsoleTestResult.AssertionResult(assertion));
                }

                return _assertions;
            }
        }

        public void WriteResult(ExtendedTextWriter writer)
        {
            int numAsserts = Assertions.Count;

            if (numAsserts > 0)
            {
                int assertionCounter = 0;
                string assertID = ReportID;

                foreach (var assertion in Assertions)
                {
                    if (numAsserts > 1)
                        assertID = string.Format("{0}-{1}", ReportID, ++assertionCounter);

                    WriteResult(writer, assertID, Status, FullName, assertion.Message, assertion.StackTrace);
                }
            }
            else
                WriteResult(writer, ReportID, Status, FullName, Message, StackTrace);
        }

        private void WriteResult(ExtendedTextWriter writer, string reportID, string status, string fullName, string? message, string? stackTrace)
        {
            ColorStyle style = GetColorStyle();

            writer.WriteLine(style,
                string.Format("{0}) {1} : {2}", reportID, status, fullName));

            if (!string.IsNullOrEmpty(message))
                writer.WriteLine(ColorStyle.Output, message);

            if (!string.IsNullOrEmpty(stackTrace))
                writer.WriteLine(ColorStyle.Output, stackTrace);

            writer.WriteLine(); // Skip after each item
        }

        private ColorStyle GetColorStyle()
        {
            return Result == "Failed"
                ? ColorStyle.Failure
                : Result == "Warning" || Status == "Ignored"
                    ? ColorStyle.Warning
                    : ColorStyle.Output;
        }

        private static string? GetTrimmedInnerText(XmlNode? node)
        {
            // In order to control the format, we trim any line-end chars
            // from end of the strings we write and supply them via calls
            // to WriteLine(). Newlines within the strings are retained.
            return node != null
                ? node.InnerText.TrimEnd(EOL_CHARS)
                : null;
        }

        public struct AssertionResult
        {
            public AssertionResult(XmlNode assertion)
            {
                Message = GetTrimmedInnerText(assertion.SelectSingleNode("message"));
                StackTrace = GetTrimmedInnerText(assertion.SelectSingleNode("stack-trace"));
            }

            public string? Message { get; private set; }
            public string? StackTrace { get; private set; }
        }
    }
}
