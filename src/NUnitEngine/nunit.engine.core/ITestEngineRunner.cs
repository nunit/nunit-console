// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using System;

namespace NUnit.Engine
{
    /// <summary>
    /// Interface implemented by all internal test runners in
    /// the engine, allowing them to pass back TestEngineResults
    /// to any higher-level runner that calls them.
    /// </summary>
    public interface ITestEngineRunner : IDisposable
    {
        /// <summary>
        /// Load a TestPackage for possible execution
        /// </summary>
        /// <returns>A TestEngineResult.</returns>
        TestEngineResult Load();

        /// <summary>
        /// Unload any loaded TestPackage. If none is loaded,
        /// the call is ignored.
        /// </summary>
        void Unload();

        /// <summary>
        /// Reload the loaded test package.
        /// </summary>
        /// <returns>A TestEngineResult.</returns>
        /// <exception cref="InvalidOperationException">If no package is loaded.</exception>
        TestEngineResult Reload();

        /// <summary>
        /// Count the test cases that would be run under
        /// the specified filter.
        /// </summary>
        /// <param name="filter">A TestFilter</param>
        /// <returns>The count of test cases</returns>
        int CountTestCases(TestFilter filter);

        /// <summary>
        /// Run the tests in the loaded TestPackage and return a test result. The tests
        /// are run synchronously and the listener interface is notified as it progresses.
        /// </summary>
        /// <param name="listener">An ITestEventHandler to receive events</param>
        /// <param name="filter">A TestFilter used to select tests</param>
        /// <returns>A TestEngineResult giving the result of the test execution</returns>
        TestEngineResult Run(ITestEventListener listener, TestFilter filter);

        /// <summary>
        /// Start a run of the tests in the loaded TestPackage. The tests are run
        /// asynchronously and the listener interface is notified as it progresses.
        /// </summary>
        /// <param name="listener">An ITestEventHandler to receive events</param>
        /// <param name="filter">A TestFilter used to select tests</param>
        /// <returns>An <see cref="AsyncTestEngineResult"/> that will provide the result of the test execution</returns>
        AsyncTestEngineResult RunAsync(ITestEventListener listener, TestFilter filter);

        /// <summary>
        /// Cancel the current test run. If no test is running,
        /// the call is ignored.
        /// </summary>
        /// <param name="force">If true, force a stop by cancelling threads if necessary.</param>
        void StopRun(bool force);

        /// <summary>
        /// Explore a loaded TestPackage and return information about
        /// the tests found.
        /// </summary>
        /// <param name="filter">Criteria used to filter the search results</param>
        /// <returns>A TestEngineResult.</returns>
        TestEngineResult Explore(TestFilter filter);
    }
}
