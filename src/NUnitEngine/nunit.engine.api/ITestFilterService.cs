// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

namespace NUnit.Engine
{
    /// <summary>
    /// The TestFilterService provides builders that can create TestFilters
    /// </summary>
    public interface ITestFilterService
    {
        /// <summary>
        /// Get an uninitialized TestFilterBuilder
        /// </summary>
        ITestFilterBuilder GetTestFilterBuilder();
    }
}
