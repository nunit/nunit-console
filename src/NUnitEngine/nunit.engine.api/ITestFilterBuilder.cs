// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

namespace NUnit.Engine
{
    /// <summary>
    /// Interface to a TestFilterBuilder, which is used to create TestFilters
    /// </summary>
    public interface ITestFilterBuilder
    {
        /// <summary>
        /// Add a test to be selected
        /// </summary>
        /// <param name="fullName">The full name of the test, as created by NUnit</param>
        void AddTest(string fullName);

        /// <summary>
        /// Specify what is to be included by the filter using a where clause.
        /// </summary>
        /// <param name="whereClause">A where clause that will be parsed by NUnit to create the filter.</param>
        void SelectWhere(string whereClause);

        /// <summary>
        /// Get a TestFilter constructed according to the criteria specified by the other calls.
        /// </summary>
        /// <returns>A TestFilter.</returns>
        TestFilter GetFilter();
    }
}
