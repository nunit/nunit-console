// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using System;
using System.Collections.Generic;
using System.Xml;

namespace NUnit.Engine
{
    /// <summary>
    /// Interface implemented by all test runners.
    /// </summary>
    public interface ITestRunner : IDisposable
    {
        /// <summary>
        /// Get a flag indicating whether a test is running
        /// </summary>
        bool IsTestRunning { get; }

        /// <summary>
        /// Load a TestPackage for possible execution
        /// </summary>
        /// <returns>An XmlNode representing the loaded package.</returns>
        /// <remarks>
        /// This method is normally optional, since Explore and Run call
        /// it automatically when necessary. The method is kept in order
        /// to make it easier to convert older programs that use it.
        /// </remarks>
        XmlNode Load();

        /// <summary>
        /// Unload any loaded TestPackage. If none is loaded,
        /// the call is ignored.
        /// </summary>
        void Unload();

        /// <summary>
        /// Reload the current TestPackage
        /// </summary>
        /// <returns>An XmlNode representing the loaded package.</returns>
        XmlNode Reload();

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
        /// <param name="listener">The listener that is notified as the run progresses</param>
        /// <param name="filter">A TestFilter used to select tests</param>
        /// <returns>An XmlNode giving the result of the test execution</returns>
        XmlNode Run(ITestEventListener listener, TestFilter filter);

        /// <summary>
        /// Start a run of the tests in the loaded TestPackage. The tests are run
        /// asynchronously and the listener interface is notified as it progresses.
        /// </summary>
        /// <param name="listener">The listener that is notified as the run progresses</param>
        /// <param name="filter">A TestFilter used to select tests</param>
        /// <returns></returns>
        ITestRun RunAsync(ITestEventListener listener, TestFilter filter);

        /// <summary>
        /// Cancel the ongoing test run. If no  test is running, the call is ignored.
        /// </summary>
        /// <param name="force">If true, cancel any ongoing test threads, otherwise wait for them to complete.</param>
        void StopRun(bool force);

        /// <summary>
        /// Explore a loaded TestPackage and return information about the tests found.
        /// </summary>
        /// <param name="filter">The TestFilter to be used in selecting tests to explore.</param>
        /// <returns>An XmlNode representing the tests found.</returns>
        XmlNode Explore(TestFilter filter);
    }
}
