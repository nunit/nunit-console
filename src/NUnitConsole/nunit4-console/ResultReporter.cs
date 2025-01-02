﻿// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Xml;
using NUnit.ConsoleRunner.Options;
using NUnit.ConsoleRunner.Utilities;

namespace NUnit.ConsoleRunner
{
    public class ResultReporter
    {
        public ResultReporter(XmlNode resultNode, ExtendedTextWriter writer, ConsoleOptions options)
        {
            ResultNode = resultNode;
            Writer = writer;
            Options = options;

            string? overallResult = resultNode.GetAttribute("result");
            if (overallResult == "Skipped")
                OverallResult = "Warning";
            if (overallResult == null)
                OverallResult = "Unknown";
            else
                OverallResult = overallResult;
            Summary = new ResultSummary(resultNode);
        }

        public ResultSummary Summary { get; private set; }

        private int ReportIndex { get; set; }
        private XmlNode ResultNode { get; set; }
        private ExtendedTextWriter Writer { get; set; }
        private ConsoleOptions Options { get; set; }
        private string OverallResult { get; set; }

        /// <summary>
        /// Reports the results to the console
        /// </summary>
        public void ReportResults()
        {
            Writer.WriteLine();

            if (Summary.ExplicitCount + Summary.SkipCount + Summary.IgnoreCount > 0)
                WriteNotRunReport();

            if (OverallResult == "Failed" || Summary.WarningCount > 0)
                WriteErrorsFailuresAndWarningsReport();

            WriteRunSettingsReport();

            WriteSummaryReport();
        }

        internal void WriteRunSettingsReport()
        {
            var firstSuite = ResultNode.SelectSingleNode("test-suite");
            if (firstSuite != null)
            {
                var settings = firstSuite.SelectNodes("settings/setting");

                if (settings is not null && settings.Count > 0)
                {
                    Writer.WriteLine(ColorStyle.SectionHeader, "Run Settings");

                    foreach (XmlNode node in settings)
                        WriteSettingsNode(node);

                    Writer.WriteLine();
                }
            }
        }

        private void WriteSettingsNode(XmlNode node)
        {
            var items = node.SelectNodes("item");
            var name = node.GetAttribute("name");
            var val = node.GetAttribute("value") ?? string.Empty;

            if (items is null || items.Count == 0)
                Writer.WriteLabelLine($"    {name}:", $" |{val}|");
            else
            {
                Writer.WriteLabelLine($"    {name}:", string.Empty);

                foreach (XmlNode item in items)
                {
                    var key = item.GetAttribute("key");
                    var value = item.GetAttribute("value");
                    Writer.WriteLine(ColorStyle.Value, $"        {key} -> |{value}|");
                }
            }
        }

        public void WriteSummaryReport()
        {
            const string INDENT4 = "    ";
            const string INDENT8 = "        ";

            ColorStyle overall = OverallResult == "Passed"
                ? ColorStyle.Pass
                : OverallResult == "Failed"  || OverallResult == "Unknown"
                    ? ColorStyle.Failure
                    : OverallResult == "Warning"
                        ? ColorStyle.Warning
                        : ColorStyle.Output;
            
            Writer.WriteLine(ColorStyle.SectionHeader, "Test Run Summary");
            Writer.WriteLabelLine(INDENT4 + "Overall result: ", OverallResult, overall);

            WriteSummaryCount(INDENT4 + "Test Count: ", Summary.TestCount);
            WriteSummaryCount(", Pass: ", Summary.PassCount);
            WriteSummaryCount(", Fail: ", Summary.FailedCount, ColorStyle.Failure);
            WriteSummaryCount(", Warn: ", Summary.WarningCount, ColorStyle.Warning);
            WriteSummaryCount(", Inconclusive: ", Summary.InconclusiveCount);
            WriteSummaryCount(", Skip: ", Summary.TotalSkipCount);
            Writer.WriteLine();

            if (Summary.FailedCount > 0)
            {
                WriteSummaryCount(INDENT8 + "Failed Tests - Failures: ", Summary.FailureCount);
                WriteSummaryCount(", Errors: ", Summary.ErrorCount, ColorStyle.Error);
                WriteSummaryCount(", Invalid: ", Summary.InvalidCount);
                Writer.WriteLine();
            }
            if (Summary.TotalSkipCount > 0)
            {
                WriteSummaryCount(INDENT8 + "Skipped Tests - Ignored: ", Summary.IgnoreCount);
                WriteSummaryCount(", Explicit: ", Summary.ExplicitCount);
                WriteSummaryCount(", Other: ", Summary.SkipCount);
                Writer.WriteLine();
            }

            var duration = ResultNode.GetAttribute("duration", 0.0);
            var startTime = ResultNode.GetAttribute("start-time", DateTime.MinValue);
            var endTime = ResultNode.GetAttribute("end-time", DateTime.MaxValue);

            Writer.WriteLabelLine(INDENT4 + "Start time: ", startTime.ToString("u"));
            Writer.WriteLabelLine(INDENT4 + "End time: ", endTime.ToString("u"));
            Writer.WriteLabelLine(INDENT4 + "Duration: ", string.Format(NumberFormatInfo.InvariantInfo, "{0:0.000} seconds", duration));
            Writer.WriteLine();
        }

        public void WriteErrorsFailuresAndWarningsReport()
        {
            ReportIndex = 0;
            Writer.WriteLine(ColorStyle.SectionHeader, "Errors, Failures and Warnings");
            Writer.WriteLine();

            WriteErrorsFailuresAndWarnings(ResultNode);

            if (Options.StopOnError)
            {
                Writer.WriteLine(ColorStyle.Failure, "Execution terminated after first error");
                Writer.WriteLine();
            }
        }

        private void WriteErrorsFailuresAndWarnings(XmlNode resultNode)
        {
            string? resultState = resultNode.GetAttribute("result");

            switch (resultNode.Name)
            {
                case "test-case":
                    if (resultState == "Failed" || resultState == "Warning")
                        new ConsoleTestResult(resultNode, ++ReportIndex).WriteResult(Writer);
                    return;

                case "test-run":
                    foreach (XmlNode childResult in resultNode.ChildNodes)
                        WriteErrorsFailuresAndWarnings(childResult);
                    break;

                case "test-suite":
                    if (resultState == "Failed" || resultState == "Warning")
                    {
                        var suiteType = resultNode.GetAttribute("type");
                        if (suiteType == "Theory")
                        {
                            // Report failure of the entire theory and then go on
                            // to list the individual cases that failed
                            new ConsoleTestResult(resultNode, ++ReportIndex).WriteResult(Writer);
                        }
                        else
                        {
                            // Where did this happen? Default is in the current test.
                            var site = resultNode.GetAttribute("site");

                            // Correct a problem in some framework versions, whereby warnings and some failures 
                            // are promulgated to the containing suite without setting the FailureSite.
                            if (site == null)
                            {
                                if (resultNode.SelectSingleNode("reason/message")?.InnerText == "One or more child tests had warnings" ||
                                    resultNode.SelectSingleNode("failure/message")?.InnerText == "One or more child tests had errors")
                                {
                                    site = "Child";
                                }
                                else
                                    site = "Test";
                            }

                            // Only report errors in the current test method, setup or teardown
                            if (site == "SetUp" || site == "TearDown" || site == "Test")
                                new ConsoleTestResult(resultNode, ++ReportIndex).WriteResult(Writer);

                            // Do not list individual "failed" tests after a one-time setup failure
                            if (site == "SetUp") return;
                        }
                    }
                    
                    foreach (XmlNode childResult in resultNode.ChildNodes)
                        WriteErrorsFailuresAndWarnings(childResult);

                    break;
            }
        }

        public void WriteNotRunReport()
        {
            ReportIndex = 0;
            Writer.WriteLine(ColorStyle.SectionHeader, "Tests Not Run");
            Writer.WriteLine();
            WriteNotRunResults(ResultNode);
        }

        private void WriteNotRunResults(XmlNode resultNode)
        {
            switch (resultNode.Name)
            {
                case "test-case":
                    string? status = resultNode.GetAttribute("result");

                    if (status == "Skipped")
                        new ConsoleTestResult(resultNode, ++ReportIndex).WriteResult(Writer);

                    break;

                case "test-suite":
                case "test-run":
                    foreach (XmlNode childResult in resultNode.ChildNodes)
                        WriteNotRunResults(childResult);

                    break;
            }
        }

        private void WriteSummaryCount(string label, int count)
        {
            Writer.WriteLabel(label, count.ToString(CultureInfo.CurrentUICulture));
        }

        private void WriteSummaryCount(string label, int count, ColorStyle color)
        {
            Writer.WriteLabel(label, count.ToString(CultureInfo.CurrentUICulture), count > 0 ? color : ColorStyle.Value);
        }
    }
}
