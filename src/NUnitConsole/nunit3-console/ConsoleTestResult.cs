// ***********************************************************************
// Copyright (c) 2016 Charlie Poole, Rob Prouse
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
using System.Text;
using System.Xml;

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

            Status = Label ?? Result;
            if (Status == "Failed" || Status == "Error")
                if (Site == "SetUp" || Site == "TearDown")
                    Status = Site + " " + Status;

            FullName = resultNode.GetAttribute("fullname");
        }

        #region Properties

        public string Result { get; private set; }
        public string Label { get; private set; }
        public string Site { get; private set; }
        public string FullName { get; private set; }

        public string ReportID { get; private set; }
        public string Status { get; private set; }

        public string Message
        {
            get
            {
                return GetTrimmedInnerText(_resultNode.SelectSingleNode("failure/message")) ??
                       GetTrimmedInnerText(_resultNode.SelectSingleNode("reason/message"));
            }
        }

        public string StackTrace
        {
            get { return GetTrimmedInnerText(_resultNode.SelectSingleNode("failure/stack-trace")); }
        }

        private List<AssertionResult> _assertions;
        public List<AssertionResult> Assertions
        {
            get
            {
                if (_assertions == null)
                {
                    _assertions = new List<AssertionResult>();
                    foreach (XmlNode assertion in _resultNode.SelectNodes("assertions/assertion"))
                        Assertions.Add(new ConsoleTestResult.AssertionResult(assertion));
                }

                return _assertions;
            }
        }

        #endregion

        #region Public Methods

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

        #endregion

        #region Helper Methods

        private void WriteResult(ExtendedTextWriter writer, string reportID, string status, string fullName, string message, string stackTrace)
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

        private static string GetTrimmedInnerText(XmlNode node)
        {
            // In order to control the format, we trim any line-end chars
            // from end of the strings we write and supply them via calls
            // to WriteLine(). Newlines within the strings are retained.
            return node != null
                ? node.InnerText.TrimEnd(EOL_CHARS)
                : null;
        }

        #endregion

        #region Nested AssertionResult Class

        public struct AssertionResult
        {
            public AssertionResult(XmlNode assertion)
            {
                Message = GetTrimmedInnerText(assertion.SelectSingleNode("message"));
                StackTrace = GetTrimmedInnerText(assertion.SelectSingleNode("stack-trace"));
            }

            public string Message { get; private set; }
            public string StackTrace { get; private set; }
        }

        #endregion
    }
}
