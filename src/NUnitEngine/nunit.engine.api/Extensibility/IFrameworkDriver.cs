// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Xml;

namespace NUnit.Engine.Extensibility
{
    /// <summary>
    /// The IFrameworkDriver interface is implemented by a class that
    /// is able to use an external framework to explore or run tests
    /// under the engine.
    /// </summary>
    public interface IFrameworkDriver
    {
        /// <summary>
        /// Gets and sets the unique identifier for this driver,
        /// used to ensure that test ids are unique across drivers.
        /// </summary>
        string ID { get; set; }

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
        /// Executes the tests in an assembly.
        /// </summary>
        /// <param name="listener">An ITestEventHandler that receives progress notices</param>
        /// <param name="filter">A XML string representing the filter that controls which tests are executed</param>
        /// <returns>An Xml string representing the result</returns>
        string Run(ITestEventListener listener, string filter);

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
