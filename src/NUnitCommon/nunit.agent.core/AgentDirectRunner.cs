// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using System;
using System.IO;
using System.Xml;
using NUnit.TextDisplay;
using NUnit.Common;
using NUnit.Engine;
using NUnit.Engine.Runners;

namespace NUnit.Agents
{
    public class AgentDirectRunner
    {
        private static Logger log = InternalTrace.GetLogger(typeof(AgentDirectRunner));
        private AgentOptions _options;

        private ColorConsoleWriter OutWriter { get; } = new ColorConsoleWriter();

        public AgentDirectRunner(AgentOptions options)
        {
            _options = options;
        }

        public void ExecuteTestsDirectly()
        {
            try
            {
                var testFile = _options.Files[0];

                ResultReporter.WriteHeader(OutWriter);

                TestPackage package = new TestPackage(testFile).SubPackages[0];

#if NETFRAMEWORK
                var runner = new TestDomainRunner(package);
#else
                var runner = new LocalTestRunner(package);
#endif
                var xmlResult = runner.Run(null, TestFilter.Empty).Xml;
                var summary = new ResultSummary(xmlResult);

                ResultReporter.ReportResults(summary, OutWriter);

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
