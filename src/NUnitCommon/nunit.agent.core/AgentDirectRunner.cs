// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using System;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Xml;
using NUnit.TextDisplay;
using NUnit.Engine;
using NUnit.Engine.Runners;

namespace NUnit.Agents
{
    public class AgentDirectRunner
    {
        private static Logger log = InternalTrace.GetLogger(typeof(AgentDirectRunner));
        private AgentOptions _options;

        private ColorConsoleWriter OutWriter { get; } = new ColorConsoleWriter();
        private int ReportIndex { get; set; }

        public AgentDirectRunner(AgentOptions options)
        {
            _options = options;
        }

        public void ExecuteTestsDirectly()
        {
            try
            {
                var testFile = _options.Files[0];

                WriteHeader();

                TestPackage package = new TestPackage(testFile).SubPackages[0];

#if NETFRAMEWORK
                var runner = new TestDomainRunner(package);
#else
                var runner = new LocalTestRunner(package);
#endif
                var xmlResult = runner.Run(null, TestFilter.Empty).Xml;

                WriteRunSettingsReport(xmlResult);

                var result = xmlResult.GetAttribute("result");
                if (result == "Failed" || result == "Warning")
                    WriteErrorsFailuresAndWarningsReport(xmlResult);

                int notrunCount = int.Parse(xmlResult.GetAttribute("skipped") ?? "0");
                if (notrunCount > 0)
                    WriteNotRunReport(xmlResult);

                WriteSummaryReport(xmlResult);

                var pathToResultFile = Path.Combine(_options.WorkDirectory, "TestResult.xml");
                WriteResultFile(xmlResult, pathToResultFile);
                OutWriter.WriteLine($"Saved result file as {pathToResultFile}");
            }
            catch (Exception ex)
            {
                log.Error(ex.ToString());
                Environment.Exit(AgentExitCodes.UNEXPECTED_EXCEPTION);
            }

            Environment.Exit(AgentExitCodes.OK);
        }

        private void WriteHeader()
        {
            var ea = Assembly.GetEntryAssembly();
            if (ea is not null)
            {
                var title = GetAttribute<AssemblyTitleAttribute>(ea)?.Title;
                var version = GetAttribute<AssemblyFileVersionAttribute>(ea)?.Version;
                var copyright = GetAttribute<AssemblyCopyrightAttribute>(ea)?.Copyright;

                OutWriter.WriteLine(ColorStyle.Header, $"{title} {version}");
                if (copyright is not null)
                    OutWriter.WriteLine(ColorStyle.SubHeader, copyright);
                OutWriter.WriteLine(ColorStyle.SubHeader, DateTime.Now.ToString(CultureInfo.CurrentCulture.DateTimeFormat.FullDateTimePattern));
                OutWriter.WriteLine();
            }
        }

        private static TAttr? GetAttribute<TAttr>(Assembly assembly)
            where TAttr : Attribute
        {
            return assembly?.GetCustomAttribute<TAttr>();
        }

        internal void WriteRunSettingsReport(XmlNode resultNode)
        {
            var settings = resultNode.SelectNodes("settings/setting");

            if (settings is not null && settings.Count > 0)
            {
                OutWriter.WriteLine(ColorStyle.SectionHeader, "Run Settings");
                settings.ForEachNode(WriteSettingsNode);
                OutWriter.WriteLine();
            }
        }

        private void WriteSettingsNode(XmlNode node)
        {
            var items = node.SelectNodes("item");
            var name = node.GetAttribute("name");
            var val = node.GetAttribute("value") ?? string.Empty;

            if (items is not null)
            {
                OutWriter.WriteLabelLine($"    {name}:", items.Count > 0 ? string.Empty : $" {val}");

                items.ForEachNode((item) =>
                {
                    var key = item.GetAttribute("key");
                    var value = item.GetAttribute("value");
                    OutWriter.WriteLine(ColorStyle.Value, $"        {key} -> {value}");
                });
            }
        }

        public void WriteSummaryReport(XmlNode resultNode)
        {
            var overallResult = resultNode.GetAttribute("result") ?? "Unknown";
            if (overallResult == "Skipped")
                overallResult = "Warning";

            ColorStyle resultColor = overallResult == "Passed"
                ? ColorStyle.Pass
                : overallResult == "Failed" || overallResult == "Unknown"
                    ? ColorStyle.Failure
                    : overallResult == "Warning"
                        ? ColorStyle.Warning
                        : ColorStyle.Output;

            OutWriter.WriteLine(ColorStyle.SectionHeader, "Test Run Summary");
            OutWriter.WriteLabelLine(overallResult, resultColor);

            int cases = resultNode.GetAttribute("testcasecount", 0);
            int passed = resultNode.GetAttribute("passed", 0);
            int failed = resultNode.GetAttribute("failed", 0);
            int warnings = resultNode.GetAttribute("warnings", 0);
            int inconclusive = resultNode.GetAttribute("inconclusive", 0);
            int skipped = resultNode.GetAttribute("skipped", 0);

            WriteSummaryCount("  Test Cases: ", cases);
            WriteSummaryCount(", Passed: ", passed);
            WriteSummaryCount(", Failed: ", failed, ColorStyle.Failure);
            WriteSummaryCount(", Warnings: ", warnings, ColorStyle.Warning);
            WriteSummaryCount(", Inconclusive: ", inconclusive);
            WriteSummaryCount(", Skipped: ", skipped);
            OutWriter.WriteLine();

            var duration = resultNode.GetAttribute("duration", 0.0);
            var startTime = resultNode.GetAttribute("start-time", DateTime.MinValue);
            var endTime = resultNode.GetAttribute("end-time", DateTime.MaxValue);

            OutWriter.WriteLabelLine("  Start time: ", startTime.ToString("u"));
            OutWriter.WriteLabelLine("    End time: ", endTime.ToString("u"));
            OutWriter.WriteLabelLine("    Duration: ", string.Format(NumberFormatInfo.InvariantInfo, "{0:0.000} seconds", duration));
            OutWriter.WriteLine();
        }

        public void WriteErrorsFailuresAndWarningsReport(XmlNode resultNode)
        {
            ReportIndex = 0;
            OutWriter.WriteLine(ColorStyle.SectionHeader, "Errors, Failures and Warnings");
            OutWriter.WriteLine();

            WriteErrorsFailuresAndWarnings(resultNode);
        }

        private void WriteErrorsFailuresAndWarnings(XmlNode resultNode)
        {
            const string OLD_NUNIT_CHILD_HAD_ERRORS_MESSAGE = "One or more child tests had errors";
            const string OLD_NUNIT_CHILD_HAD_WARNINGS_MESSAGE = "One or more child tests had errors";

            string resultState = resultNode.GetAttribute("result") ?? string.Empty;

            switch (resultNode.Name)
            {
                case "test-case":
                    if (resultState == "Failed" || resultState == "Warning")
                        DisplayResultItem(resultNode);
                    return;

                // Not present in this agent but retain for use with other agents
                case "test-run":
                    resultNode.ForEachChildNode(WriteErrorsFailuresAndWarnings);
                    break;

                case "test-suite":
                    if (resultState == "Failed" || resultState == "Warning")
                    {
                        var suiteType = resultNode.GetAttribute("type");
                        if (suiteType == "Theory")
                        {
                            // Report failure of the entire theory and then go on
                            // to list the individual cases that failed
                            DisplayResultItem(resultNode);
                        }
                        else
                        {
                            // Where did this happen? Default is in the current test.
                            var site = resultNode.GetAttribute("site");

                            // Correct a problem in some framework versions, whereby warnings and some failures
                            // are promulgated to the containing suite without setting the FailureSite.
                            if (site is null)
                            {
                                if (resultNode.SelectSingleNode("reason/message")?.InnerText == OLD_NUNIT_CHILD_HAD_WARNINGS_MESSAGE ||
                                    resultNode.SelectSingleNode("failure/message")?.InnerText == OLD_NUNIT_CHILD_HAD_ERRORS_MESSAGE)
                                {
                                    site = "Child";
                                }
                                else
                                    site = "Test";
                            }

                            // Only report errors in the current test method, setup or teardown
                            if (site == "SetUp" || site == "TearDown" || site == "Test")
                            {
                                DisplayResultItem(resultNode);
                            }

                            // Do not list individual "failed" tests after a one-time setup failure
                            if (site == "SetUp")
                                return;
                        }
                    }

                    resultNode.ForEachChildNode(WriteErrorsFailuresAndWarnings);

                    break;
            }
        }

        public void WriteNotRunReport(XmlNode resultNode)
        {
            ReportIndex = 0;
            OutWriter.WriteLine(ColorStyle.SectionHeader, "Tests Not Run");
            OutWriter.WriteLine();
            WriteNotRunResults(resultNode);
        }

        private void WriteNotRunResults(XmlNode resultNode)
        {
            switch (resultNode.Name)
            {
                case "test-case":
                    string? status = resultNode.GetAttribute("result");

                    if (status == "Skipped")
                        DisplayResultItem(resultNode);

                    break;

                case "test-suite":
                case "test-run":
                    resultNode.ForEachChildNode(WriteNotRunResults);

                    break;
            }
        }

        private static readonly char[] EOL_CHARS = new char[] { '\r', '\n' };

        private void DisplayResultItem(XmlNode resultNode)
        {
            string? resultState = resultNode.GetAttribute("result");
            string? fullName = resultNode.GetAttribute("fullname");
            string? message = (resultNode.SelectSingleNode("failure/message") ?? resultNode.SelectSingleNode("reason/message"))?.InnerText.Trim(EOL_CHARS);

            OutWriter.WriteLine(GetColorStyle(),
                string.Format($"{++ReportIndex}) {resultState} : {fullName}"));
            if (!string.IsNullOrEmpty(message))
                OutWriter.WriteLine(ColorStyle.Output, message);
            var stackTrace = resultNode.SelectSingleNode("failure/stack-trace")?.InnerText;
            if (!string.IsNullOrEmpty(stackTrace))
                OutWriter.WriteLine(stackTrace);
            OutWriter.WriteLine();

            ColorStyle GetColorStyle()
            {
                return resultState == "Failed"
                    ? ColorStyle.Failure
                    : resultState == "Warning"
                        ? ColorStyle.Warning
                        : ColorStyle.Output;
            }
        }

        private void WriteSummaryCount(string label, int count)
        {
            OutWriter.WriteLabel(label, count.ToString(CultureInfo.CurrentUICulture));
        }

        private void WriteSummaryCount(string label, int count, ColorStyle color)
        {
            OutWriter.WriteLabel(label, count.ToString(CultureInfo.CurrentUICulture), count > 0 ? color : ColorStyle.Value);
        }

        public static void WriteResultFile(XmlNode resultNode, string outputPath)
        {
            using (var stream = new FileStream(outputPath, FileMode.Create, FileAccess.Write))
            using (var writer = new StreamWriter(stream))
            {
                WriteResultFile(resultNode, writer);
            }
        }

        public static void WriteResultFile(XmlNode resultNode, TextWriter writer)
        {
            var settings = new XmlWriterSettings();
            settings.Indent = true;

            using (XmlWriter xmlWriter = XmlWriter.Create(writer, settings))
            {
                xmlWriter.WriteStartDocument(false);
                resultNode.WriteTo(xmlWriter);
            }
        }
    }
}
