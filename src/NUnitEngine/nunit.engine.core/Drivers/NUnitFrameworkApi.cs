// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using System;
using System.Collections.Generic;

namespace NUnit.Engine.Drivers
{
    public interface NUnitFrameworkApi
    {
        /// <summary>
        /// Loads the tests in an assembly.
        /// </summary>
        /// <returns>An Xml string representing the loaded test</returns>
        string Load(string testAssemblyPath, IDictionary<string, object> settings);

        /// <summary>
        /// Count the test cases that would be executed.
        /// </summary>
        /// <param name="filter">An XML string representing the TestFilter to use in counting the tests</param>
        /// <returns>The number of test cases counted</returns>
        int CountTestCases(string filter);

        /// <summary>
        /// Executes the tests in an assembly synchronously.
        /// </summary>
        /// <param name="listener">An ITestEventHandler that receives progress notices</param>
        /// <param name="filter">A XML string representing the filter that controls which tests are executed</param>
        /// <returns>An Xml string representing the result</returns>
        string Run(ITestEventListener? listener, string filter);

        /// <summary>
        /// Executes the tests in an assembly asynchronously.
        /// </summary>
        /// <param name="callback">A callback that receives XML progress notices</param>
        /// <param name="filter">A filter that controls which tests are executed</param>
        void RunAsync(Action<string> callback, string filter);

        /// <summary>
        /// Returns information about the tests in an assembly.
        /// </summary>
        /// <param name="filter">An XML string representing the filter that controls which tests are included</param>
        /// <returns>An Xml string representing the tests</returns>
        string Explore(string filter);

        /// <summary>
        /// Cancel the ongoing test run. If no  test is running, the call is ignored.
        /// </summary>
        /// <param name="force">If true, cancel any ongoing test threads, otherwise wait for them to complete.</param>
        void StopRun(bool force);
    }
}
