// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using System;
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
        private static readonly Type[] XmlNodeStringTypes = [typeof(XmlNode), typeof(string)];
        private static readonly Type[] XmlNodeTextWriterTypes = [typeof(XmlNode), typeof(TextWriter)];

        public ResultWriterWrapper(object writer) : base(writer) { }

        public void CheckWritability(string outputPath)
        {
            Invoke(nameof(CheckWritability), StringType, outputPath);
        }

        public void WriteResultFile(XmlNode resultNode, string outputPath)
        {
            Invoke(nameof(WriteResultFile), XmlNodeStringTypes, resultNode, outputPath);
        }

        public void WriteResultFile(XmlNode resultNode, TextWriter writer)
        {
            Invoke(nameof(WriteResultFile), XmlNodeTextWriterTypes, resultNode, writer);
        }
    }
}
