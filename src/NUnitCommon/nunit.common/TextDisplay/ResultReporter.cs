// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using System;
using System.Globalization;
using System.Xml;

namespace NUnit.TextDisplay
{
    public static class ResultReporter
    {
        /// <summary>
        /// Reports the results to the console
        /// </summary>
        public static void ReportResults(ResultSummary summary, ExtendedTextWriter writer, bool stopOnError = false)
        {
            writer.WriteLine();

            var topLevelResult = summary.ResultNode;

            if (summary.ExplicitCount + summary.SkipCount + summary.IgnoreCount > 0)
                WriteNotRunReport(topLevelResult, writer);

            if (summary.OverallResult == "Failed" || summary.WarningCount > 0)
                WriteErrorsFailuresAndWarningsReport(topLevelResult, writer, stopOnError);

            WriteRunSettingsReport(topLevelResult, writer);

            WriteSummaryReport(topLevelResult, summary, writer);
        }

        public static void WriteRunSettingsReport(XmlNode topLevelResult, ExtendedTextWriter writer)
        {
            var firstSuite = topLevelResult.SelectSingleNode("test-suite");
            if (firstSuite is not null)
            {
                var settings = firstSuite.SelectNodes("settings/setting");

                if (settings is not null && settings.Count > 0)
                {
                    writer.WriteLine(ColorStyle.SectionHeader, "Run Settings");

                    foreach (XmlNode node in settings)
                        WriteSettingsNode(node, writer);

                    writer.WriteLine();
                }
            }
        }

        private static void WriteSettingsNode(XmlNode node, ExtendedTextWriter writer)
        {
            var items = node.SelectNodes("item");
            var name = node.GetAttribute("name");
            var val = node.GetAttribute("value") ?? string.Empty;

            if (items is null || items.Count == 0)
                writer.WriteLabelLine($"    {name}:", $" |{val}|");
            else
            {
                writer.WriteLabelLine($"    {name}:", string.Empty);

                foreach (XmlNode item in items)
                {
                    var key = item.GetAttribute("key");
                    var value = item.GetAttribute("value");
                    writer.WriteLine(ColorStyle.Value, $"        {key} -> |{value}|");
                }
            }
        }

        public static void WriteSummaryReport(XmlNode topLevelResult, ResultSummary summary, ExtendedTextWriter writer)
        {
            const string INDENT4 = "    ";
            const string INDENT8 = "        ";

            ColorStyle resultColor = summary.OverallResult == "Passed"
                ? ColorStyle.Pass
                : summary.OverallResult == "Failed" || summary.OverallResult == "Unknown"
                    ? ColorStyle.Failure
                    : summary.OverallResult == "Warning"
                        ? ColorStyle.Warning
                        : ColorStyle.Output;

            writer.WriteLine(ColorStyle.SectionHeader, "Test Run Summary");
            writer.WriteLabelLine(INDENT4 + "Overall result: ", summary.OverallResult, resultColor);

            WriteSummaryCount(writer, INDENT4 + "Test Count: ", summary.TestCount);
            WriteSummaryCount(writer, ", Pass: ", summary.PassCount);
            WriteSummaryCount(writer, ", Fail: ", summary.FailedCount, ColorStyle.Failure);
            WriteSummaryCount(writer, ", Warn: ", summary.WarningCount, ColorStyle.Warning);
            WriteSummaryCount(writer, ", Inconclusive: ", summary.InconclusiveCount);
            WriteSummaryCount(writer, ", Skip: ", summary.TotalSkipCount);
            writer.WriteLine();

            if (summary.FailedCount > 0)
            {
                WriteSummaryCount(writer, INDENT8 + "Failed Tests - Failures: ", summary.FailureCount);
                WriteSummaryCount(writer, ", Errors: ", summary.ErrorCount, ColorStyle.Error);
                WriteSummaryCount(writer, ", Invalid: ", summary.InvalidCount);
                writer.WriteLine();
            }
            if (summary.TotalSkipCount > 0)
            {
                WriteSummaryCount(writer, INDENT8 + "Skipped Tests - Ignored: ", summary.IgnoreCount);
                WriteSummaryCount(writer, ", Explicit: ", summary.ExplicitCount);
                WriteSummaryCount(writer, ", Other: ", summary.SkipCount);
                writer.WriteLine();
            }

            var duration = topLevelResult.GetAttribute("duration", 0.0);
            var startTime = topLevelResult.GetAttribute("start-time", DateTime.MinValue);
            var endTime = topLevelResult.GetAttribute("end-time", DateTime.MaxValue);

            writer.WriteLabelLine(INDENT4 + "Start time: ", startTime.ToString("u"));
            writer.WriteLabelLine(INDENT4 + "End time: ", endTime.ToString("u"));
            writer.WriteLabelLine(INDENT4 + "Duration: ", string.Format(NumberFormatInfo.InvariantInfo, "{0:0.000} seconds", duration));
            writer.WriteLine();
        }

        public static void WriteErrorsFailuresAndWarningsReport(XmlNode resultNode, ExtendedTextWriter writer, bool stopOnError = false)
        {
            int reportIndex = 0;
            writer.WriteLine(ColorStyle.SectionHeader, "Errors, Failures and Warnings");
            writer.WriteLine();

            WriteErrorsFailuresAndWarnings(resultNode, writer, ref reportIndex);

            if (stopOnError)
            {
                writer.WriteLine(ColorStyle.Failure, "Execution terminated after first error");
                writer.WriteLine();
            }
        }

        private static void WriteErrorsFailuresAndWarnings(XmlNode resultNode, ExtendedTextWriter writer, ref int reportIndex)
        {
            string? resultState = resultNode.GetAttribute("result");

            switch (resultNode.Name)
            {
                case "test-case":
                    if (resultState == "Failed" || resultState == "Warning")
                        new ClientTestResult(resultNode, ++reportIndex).WriteResult(writer);
                    return;

                case "test-run":
                    foreach (XmlNode childResult in resultNode.ChildNodes)
                        WriteErrorsFailuresAndWarnings(childResult, writer, ref reportIndex);
                    break;

                case "test-suite":
                    if (resultState == "Failed" || resultState == "Warning")
                    {
                        var suiteType = resultNode.GetAttribute("type");
                        if (suiteType == "Theory")
                        {
                            // Report failure of the entire theory and then go on
                            // to list the individual cases that failed
                            new ClientTestResult(resultNode, ++reportIndex).WriteResult(writer);
                        }
                        else
                        {
                            // Where did this happen? Default is in the current test.
                            var site = resultNode.GetAttribute("site");

                            // Correct a problem in some framework versions, whereby warnings and some failures
                            // are promulgated to the containing suite without setting the FailureSite.
                            if (site is null)
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
                                new ClientTestResult(resultNode, ++reportIndex).WriteResult(writer);

                            // Do not list individual "failed" tests after a one-time setup failure
                            if (site == "SetUp")
                                return;
                        }
                    }

                    foreach (XmlNode childResult in resultNode.ChildNodes)
                        WriteErrorsFailuresAndWarnings(childResult, writer, ref reportIndex);

                    break;
            }
        }

        public static void WriteNotRunReport(XmlNode resultNode, ExtendedTextWriter writer)
        {
            int reportIndex = 0;
            writer.WriteLine(ColorStyle.SectionHeader, "Tests Not Run");
            writer.WriteLine();
            WriteNotRunResults(resultNode, writer, ref reportIndex);
        }

        private static void WriteNotRunResults(XmlNode resultNode, ExtendedTextWriter writer, ref int reportIndex)
        {
            switch (resultNode.Name)
            {
                case "test-case":
                    string? status = resultNode.GetAttribute("result");

                    if (status == "Skipped")
                        new ClientTestResult(resultNode, ++reportIndex).WriteResult(writer);

                    break;

                case "test-suite":
                case "test-run":
                    foreach (XmlNode childResult in resultNode.ChildNodes)
                        WriteNotRunResults(childResult, writer, ref reportIndex);

                    break;
            }
        }

        private static void WriteSummaryCount(ExtendedTextWriter writer, string label, int count)
        {
            writer.WriteLabel(label, count.ToString(CultureInfo.CurrentUICulture));
        }

        private static void WriteSummaryCount(ExtendedTextWriter writer, string label, int count, ColorStyle color)
        {
            writer.WriteLabel(label, count.ToString(CultureInfo.CurrentUICulture), count > 0 ? color : ColorStyle.Value);
        }
    }
}
