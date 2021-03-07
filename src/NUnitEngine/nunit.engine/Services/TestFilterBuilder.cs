// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using System.Collections.Generic;
using System.Text;

// Missing XML Docs
#pragma warning disable 1591

namespace NUnit.Engine
{
    public class TestFilterBuilder : ITestFilterBuilder
    {
        private List<string> _testList = new List<string>();
        private string _whereClause;

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
            var filter = new StringBuilder("<filter>");

            if (_testList.Count > 0)
            {
                if (_testList.Count > 1)
                    filter.Append("<or>");
                foreach (string test in _testList)
                    filter.AppendFormat("<test>{0}</test>", XmlEscape(test));
                if (_testList.Count > 1)
                    filter.Append("</or>");
            }


            if (_whereClause != null)
                filter.Append(new TestSelectionParser().Parse(_whereClause));

            filter.Append("</filter>");

            return new TestFilter(filter.ToString());
        }

        private static string XmlEscape(string text)
        {
            return text
                .Replace("&", "&amp;")
                .Replace("\"", "&quot;")
                .Replace("<", "&lt;")
                .Replace(">", "&gt;")
                .Replace("'", "&apos;");
        }
    }
}
