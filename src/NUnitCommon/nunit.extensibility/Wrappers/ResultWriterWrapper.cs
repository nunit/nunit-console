// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using System.IO;
using System.Xml;
using NUnit.Engine.Extensibility;

namespace NUnit.Extensibility.Wrappers
{
    /// <summary>
    /// Wrapper class for result writers based on the NUnit3 API
    /// </summary>
    public class ResultWriterWrapper : ExtensionWrapper, IResultWriter
    {
        public ResultWriterWrapper(object writer) : base(writer) { }

        public void CheckWritability(string outputPath)
        {
            Invoke(nameof(CheckWritability), outputPath);
        }

        public void WriteResultFile(XmlNode resultNode, string outputPath)
        {
            Invoke(nameof(WriteResultFile), resultNode, outputPath);
        }

        public void WriteResultFile(XmlNode resultNode, TextWriter writer)
        {
            Invoke(nameof(WriteResultFile), resultNode, writer);
        }
    }
}
