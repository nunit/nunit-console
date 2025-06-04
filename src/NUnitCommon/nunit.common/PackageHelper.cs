// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Xml;
using NUnit.Common;

namespace NUnit.Engine
{
    public delegate bool TestPackageSelectorDelegate(TestPackage p);

    /// <summary>
    /// Extension methods for use with TestPackages
    /// </summary>
    public static class PackageHelper
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
                WriteSettings(package.Settings, xmlWriter);

            // Write any SubPackages recursively
            foreach (TestPackage subPackage in package.SubPackages)
                WriteXml(subPackage, xmlWriter);

            xmlWriter.WriteEndElement();
        }

        private static void WriteSettings(PackageSettingsList packageSettings, XmlWriter xmlWriter)
        {
            xmlWriter.WriteStartElement("Settings");

            foreach (PackageSetting setting in packageSettings)
            {
                switch (setting.Name)
                {
                    // Some settings require special handling
                    case "InternalTraceWriter":
                        // NYI
                        break;
                    case "LOAD":
                        var filters = setting.Value as string[];
                        if (filters is not null)
                            xmlWriter.WriteAttributeString("LOAD", $"{string.Join(";", filters)}");
                        break;
                    case "TestParametersDictionary":
                        var dict = setting.Value as IDictionary<string, string>;
                        if (dict is not null)
                        {
                            StringBuilder sb = new StringBuilder();
                            xmlWriter.WriteStartAttribute(setting.Name);

                            // Quick and Dirty code for attribute value containing XML.
                            xmlWriter.WriteRaw("&lt;parms>");
                            foreach (var entry in dict)
                            {
                                xmlWriter.WriteRaw("&lt;parm ");
                                // NOTE: Non-XML chars in value will be replaced
                                xmlWriter.WriteString($"key='{entry.Key}' value='{entry.Value}'");
                                xmlWriter.WriteRaw(" />");
                            }
                            xmlWriter.WriteRaw("&lt;/parms>");

                            xmlWriter.WriteEndAttribute();
                        }
                        break;
                    default:
                        var type = setting.Value.GetType();
                        if (type.IsPrimitive || type == typeof(string))
                            xmlWriter.WriteAttributeString(setting.Name, Convert.ToString(setting.Value));
                        break;
                }
            }

            xmlWriter.WriteEndElement();
        }

        /// <summary>
        /// Create a new TestPackage from its XML representation
        /// </summary>
        /// <param name="xml">String holding the XML representation of the package</param>
        /// <returns>A TestPackage</returns>
        public static TestPackage FromXml(string xml)
        {
            var doc = new XmlDocument();
            doc.LoadXml(xml);

            var reader = new StringReader(doc.OuterXml);
            var xmlReader = XmlReader.Create(reader);

            // The first element must be TestPackage
            if (!ReadTestPackageElement())
                throw new InvalidOperationException("Invalid TestPackage XML");

            return ReadXml(xmlReader);

            bool ReadTestPackageElement()
            {
                while (xmlReader.Read())
                    if (xmlReader.NodeType == XmlNodeType.Element)
                        return xmlReader.Name == "TestPackage";
                return false;
            }
        }

        private static TestPackage ReadXml(XmlReader xmlReader)
        {
            TestPackage package = new TestPackage();
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
                                    package.Settings.ReadSettings(xmlReader);
                                    break;

                                case "TestPackage":
                                    package.SubPackages.Add(ReadXml(xmlReader));
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

        private static void ReadSettings(this PackageSettingsList packageSettings, XmlReader xmlReader)
        {
            while (xmlReader.MoveToNextAttribute())
            {
                var name = xmlReader.Name;
                var value = xmlReader.Value;
                var prop = typeof(PackageSettings).GetProperty(name, BindingFlags.Public | BindingFlags.Static);

                if (prop is not null) // Standard SettingDefinition
                {
                    var settingDefinition = prop.GetValue(null) as SettingDefinition;

                    if (settingDefinition is not null)
                        switch (name)
                        {
                            case "InternalTraceWriter":
                                // NYI
                                break;
                            case "LOAD":
                                packageSettings.Add(settingDefinition.WithValue(value.Split(new[] { ';' })));
                                break;
                            case "TestParametersDictionary":
                                XmlDocument doc = new XmlDocument();
                                doc.LoadXml(value);
                                var dict = new Dictionary<string, string>();
                                foreach (XmlNode node in doc.SelectNodes("parms/parm").ShouldNotBeNull())
                                    dict.Add(node.GetAttribute("key").ShouldNotBeNull(), node.GetAttribute("value").ShouldNotBeNull());
                                packageSettings.Add(settingDefinition.WithValue(dict));
                                break;
                            default:
                                switch (settingDefinition.ValueType)
                                {
                                    case Type t when t.IsPrimitive | t.IsAssignableFrom(typeof(string)):
                                        var data = Convert.ChangeType(value, t);
                                        packageSettings.Add(settingDefinition.WithValue(data));
                                        break;
                                    default:
                                        packageSettings.Add(new PackageSetting<string>(name, value));
                                        break;
                                }
                                break;
                    }
                }
                else // Non-standard Setting created by user
                {
                    // We assume that a setting that parses as a bool or an int
                    // is intended as that type, otherwise we use string.
                    if (bool.TryParse(value, out var flag))
                        packageSettings.Add(new PackageSetting<bool>(name, flag));
                    else if (int.TryParse(value, out var num))
                        packageSettings.Add(new PackageSetting<int>(name, num));
                    else
                        packageSettings.Add(new PackageSetting<string>(name, value));
                }
            }

            xmlReader.MoveToElement();
        }
    }
}
