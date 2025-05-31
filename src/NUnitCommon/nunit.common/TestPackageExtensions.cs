// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;
using System.Xml;
using NUnit.Common;
using System.Runtime;

namespace NUnit.Engine
{
    public delegate bool TestPackageSelectorDelegate(TestPackage p);

    /// <summary>
    /// Extension methods for use with TestPackages
    /// </summary>
    public static class TestPackageExtensions
    {
        public static bool IsAssemblyPackage(this TestPackage package)
        {
            return package.FullName is not null && PathUtils.IsAssemblyFileType(package.FullName);
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

            WriteXml(package, xmlWriter);

            xmlWriter.Flush();
            xmlWriter.Close();

            return writer.ToString();
        }

        private static void WriteXml(TestPackage package, XmlWriter xmlWriter)
        {
            xmlWriter.WriteStartElement("TestPackage");

            // Write ID and FullName
            xmlWriter.WriteAttributeString("id", package.ID);
            if (package.FullName is not null)
                xmlWriter.WriteAttributeString("fullname", package.FullName);

            // Write Settings
            if (package.Settings.Count != 0)
            {
                xmlWriter.WriteStartElement("Settings");

                foreach (PackageSetting setting in package.Settings)
                {
                    var type = setting.Value.GetType();
                    string? val;
                    if (type.IsPrimitive)
                        val = Convert.ToString(setting.Value);
                    xmlWriter.WriteAttributeString(setting.Name, setting.Value.ToString());
                }

                xmlWriter.WriteEndElement();
            }

            // Write any SubPackages recursively
            foreach (TestPackage subPackage in package.SubPackages)
                WriteXml(subPackage, xmlWriter);

            xmlWriter.WriteEndElement();
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

            // The first element must be TestPackage
            if (!ReadTestPackageElement())
                throw new InvalidOperationException("Invalid TestPackage XML");

            return package.Populate(xmlReader);

            bool ReadTestPackageElement()
            {
                while (xmlReader.Read())
                    if (xmlReader.NodeType == XmlNodeType.Element)
                        return xmlReader.Name == "TestPackage";
                return false;
            }
        }

        private static TestPackage Populate(this TestPackage package, XmlReader xmlReader)
        {
            package.ID = xmlReader.GetAttribute("id").ShouldNotBeNull();
            package.FullName = xmlReader.GetAttribute("fullname");

            if (!xmlReader.IsEmptyElement)
            {
                while (xmlReader.Read())
                {
                    switch (xmlReader.NodeType)
                    {
                        case XmlNodeType.Element:
                            switch (xmlReader.Name)
                            {
                                case "Settings":
                                    // We don't use AddSettings, which copies settings downward.
                                    // Instead, each package handles it's own settings.
                                    while (xmlReader.MoveToNextAttribute())
                                        package.Settings.Add(new PackageSetting<string>(xmlReader.Name, xmlReader.Value));
                                    xmlReader.MoveToElement();
                                    break;

                                case "TestPackage":
                                    package.SubPackages.Add(new TestPackage().Populate(xmlReader));
                                    break;
                            }
                            break;

                        case XmlNodeType.EndElement:
                            if (xmlReader.Name == "TestPackage")
                                return package;
                            else
                                throw new Exception("Unexpected EndElement: " + xmlReader.Name);
                    }
                }

                throw new Exception("Invalid XML: TestPackage Element not terminated.");
            }

            return package;
        }
    }
}
