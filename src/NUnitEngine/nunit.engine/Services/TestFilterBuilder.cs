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

            // NOTE: Top level "filter" element works like "or"
            foreach (string test in _testList)
            {
                xmlWriter.WriteStartElement("test");
                xmlWriter.WriteCData(test);
                xmlWriter.WriteEndElement();
            }

            // We pass our XmlWriter to TestSelectionParser so it can parse
            // the where clause and leave the result where we will find it.
            if (_whereClause is not null)
                TestSelectionParser.Parse(_whereClause, xmlWriter);

            xmlWriter.WriteEndElement();
            xmlWriter.Close();

            return new TestFilter(filter.ToString());
        }
    }
}
