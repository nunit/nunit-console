// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using System;
using System.Text;
using System.Xml;
using System.IO;
using NUnit.Engine.Extensibility;

namespace NUnit.Engine.Services
{
    using Internal;

    /// <summary>
    /// NUnit3XmlResultWriter is responsible for writing the results
    /// of a test to a file in NUnit 3 format.
    /// </summary>
    public class NUnit3XmlResultWriter : IResultWriter
    {
        /// <summary>
        /// Checks if the output is writable by creating a stub result file. If the output is not
        /// writable, this method should throw an exception.
        /// </summary>
        /// <param name="outputPath"></param>
        public void CheckWritability(string outputPath)
        {
            XmlNode stub = GetStubResult();
            WriteResultFile(stub, outputPath);
        }

        public void WriteResultFile(XmlNode resultNode, string outputPath)
        {
            using (var stream = new FileStream(outputPath, FileMode.Create, FileAccess.Write))
            using (var writer = new StreamWriter(stream))
            {
                WriteResultFile(resultNode, writer);
            }
        }

        public void WriteResultFile(XmlNode resultNode, TextWriter writer)
        {
            var settings = new XmlWriterSettings();
            settings.Indent = true;

            using (XmlWriter xmlWriter = XmlWriter.Create(writer, settings))
            {
                xmlWriter.WriteStartDocument(false);
                resultNode.WriteTo(xmlWriter);
            }
        }

        private XmlNode GetStubResult()
        {
            var doc = new XmlDocument();
            var test = doc.CreateElement("test-run");
            test.AddAttribute("start-time", DateTime.UtcNow.ToString("u"));
            doc.AppendChild(test);

            var cmd = doc.CreateElement("command-line");
            var cdata = doc.CreateCDataSection(Environment.CommandLine);
            cmd.AppendChild(cdata);
            test.AppendChild(cmd);

            return doc;
        }
    }
}
