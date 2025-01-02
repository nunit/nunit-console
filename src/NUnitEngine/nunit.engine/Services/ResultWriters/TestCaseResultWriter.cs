// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using System.IO;
using System.Text;
using System.Xml;
using NUnit.Engine.Extensibility;

namespace NUnit.Engine.Services
{
    public class TestCaseResultWriter : IResultWriter
    {
        public void CheckWritability(string outputPath)
        {
            using (new FileStream(outputPath, FileMode.Create, FileAccess.Write))
            {
                // Opening is enough to check
            }
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
            XmlNodeList? testCases = resultNode.SelectNodes("//test-case");
            if (testCases != null)
                foreach (XmlNode node in testCases)
                    writer.WriteLine(node.Attributes?["fullname"]?.Value);
        }
    }
}
