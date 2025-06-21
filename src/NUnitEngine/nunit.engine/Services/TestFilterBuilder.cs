// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;

// Missing XML Docs
#pragma warning disable 1591

namespace NUnit.Engine
{
    public class TestFilterBuilder : ITestFilterBuilder
    {
        private List<string> _testList = new List<string>();
        private string? _whereClause;

        /// <summary>
        /// Add a test to be selected
        /// </summary>
        /// <param name="fullName">The full name of the test, as created by NUnit</param>
        public void AddTest(string fullName)
        {
            _testList.Add(fullName);
        }

        /// <summary>
        /// Specify what is to be included by the filter using a where clause.
        /// </summary>
        /// <param name="whereClause">A where clause that will be parsed by NUnit to create the filter.</param>
        public void SelectWhere(string whereClause)
        {
            _whereClause = whereClause;
        }

        /// <summary>
        /// Get a TestFilter constructed according to the criteria specified by the other calls.
        /// </summary>
        /// <returns>A TestFilter.</returns>
        public TestFilter GetFilter()
        {
            var filter = new StringBuilder();
            var xmlWriter = XmlWriter.Create(filter, new XmlWriterSettings { OmitXmlDeclaration = true });

            xmlWriter.WriteStartElement("filter");

            switch (_testList.Count)
            {
                case 0:
                    break;
                case 1:
                    WriteTestElement(_testList[0]);
                    break;
                default:
                    xmlWriter.WriteStartElement("or");
                    foreach (string test in _testList)
                        WriteTestElement(test);
                    xmlWriter.WriteEndElement();
                    break;
            }

            // TODO: We use the result of the where clause directly,
            // as a raw string. This assumes that TestSelectionParser
            // produces valid XML. This is not actually the case and
            // TestSelectionParser needs to use similar techniques
            // for encoding test and category names as are used here.
            if (_whereClause is not null)
                xmlWriter.WriteRaw(TestSelectionParser.Parse(_whereClause));

            xmlWriter.WriteEndElement();
            xmlWriter.Close();

            return new TestFilter(filter.ToString());

            void WriteTestElement(string test)
            {
                xmlWriter.WriteStartElement("test");
                xmlWriter.WriteCData(test);
                xmlWriter.WriteEndElement();
            }
        }
    }
}
