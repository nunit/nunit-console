// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;
using System.Xml;

namespace NUnit.Engine.Internal
{
    public delegate bool TestPackageSelectorDelegate(TestPackage p);

    /// <summary>
    /// Extension methods for use with TestPackages
    /// </summary>
    public static class TestPackageExtensions
    {
        public static bool IsAssemblyPackage(this TestPackage package)
        {
            return package.FullName != null && PathUtils.IsAssemblyFileType(package.FullName);
        }

        public static bool HasSubPackages(this TestPackage package)
        {
            return package.SubPackages.Count > 0;
        }

        public static IList<TestPackage> Select(this TestPackage package, TestPackageSelectorDelegate selector)
        {
            var selection = new List<TestPackage>();

            AccumulatePackages(package, selection, selector);

            return selection;
        }

        private static void AccumulatePackages(TestPackage package, IList<TestPackage> selection, TestPackageSelectorDelegate selector)
        {
            if (selector(package))
                selection.Add(package);

            foreach (var subPackage in package.SubPackages)
                AccumulatePackages(subPackage, selection, selector);
        }

        public static string ToXml(this TestPackage package)
        {
            var writer = new StringWriter();
            var xmlWriter = XmlWriter.Create(writer, new XmlWriterSettings() { OmitXmlDeclaration = true });
            var serializer = new XmlSerializer(typeof(TestPackage));
            serializer.Serialize(xmlWriter, package);
            xmlWriter.Flush();
            xmlWriter.Close();

            return writer.ToString();
        }

        /// <summary>
        /// Populate an empty TestPackage using its XML representation
        /// </summary>
        /// <param name="xml">String holding the XML representation of the package</param>
        /// <returns>A TestPackage</returns>
        public static TestPackage FromXml(this TestPackage package, string xml)
        {
            var doc = new XmlDocument();
            doc.LoadXml(xml);

            var reader = new StringReader(doc.OuterXml);
            var xmlReader = XmlReader.Create(reader);

            if (!ReadTestPackageElement())
                throw new InvalidOperationException("Invalid TestPackage XML");

            package.ReadXml(xmlReader);
            return package;

            // The reader must be positioned on the top-level TestPackgae element
            // before calling ReadXml.
            bool ReadTestPackageElement()
            {
                while (xmlReader.Read())
                    if (xmlReader.NodeType == XmlNodeType.Element)
                        return xmlReader.Name == "TestPackage";
                return false;
            }
        }
    }
}
