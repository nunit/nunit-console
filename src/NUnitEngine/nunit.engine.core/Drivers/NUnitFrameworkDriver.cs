﻿// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.Serialization;
using NUnit.Common;
using NUnit.Engine.Internal;
using NUnit.Engine.Extensibility;

namespace NUnit.Engine.Drivers
{
    /// <summary>
    /// NUnitFrameworkDriver is used by the test-runner to load and run
    /// tests using the NUnit framework assembly, versions 3 and up.
    /// </summary>
    public class NUnitFrameworkDriver : IFrameworkDriver
    {
        static readonly ILogger log = InternalTrace.GetLogger(nameof(NUnitFrameworkDriver));

        readonly NUnitFrameworkApi _api;

#if NETFRAMEWORK
        /// <summary>
        /// Construct an NUnitFrameworkDriver
        /// </summary>
        /// <param name="testDomain">The application domain in which to create the FrameworkController</param>
        /// <param name="nunitRef">An AssemblyName referring to the test framework.</param>
        public NUnitFrameworkDriver(AppDomain testDomain, AssemblyName nunitRef)
        {
            Guard.ArgumentNotNull(testDomain, nameof(testDomain));
            Guard.ArgumentNotNull(nunitRef, nameof(nunitRef));

           _api = new NUnitFrameworkApi2009(this, testDomain, nunitRef);
        }
#else
        /// <summary>
        /// Construct an NUnitFrameworkDriver
        /// </summary>
        /// <param name="reference">An AssemblyName referring to the test framework.</param>
        public NUnitFrameworkDriver(AssemblyName nunitRef)
        {
            Guard.ArgumentNotNull(nunitRef, nameof(nunitRef));

            _api = new NUnitFrameworkApi2018(this, nunitRef);
        }
#endif

        /// <summary>
        /// An id prefix that will be passed to the test framework and used as part of the
        /// test ids created.
        /// </summary>
        public string ID { get; set; } = string.Empty;

        /// <summary>
        /// Loads the tests in an assembly.
        /// </summary>
        /// <param name="testAssemblyPath">The path to the test assembly</param>
        /// <param name="settings">The test settings</param>
        /// <returns>An XML string representing the loaded test</returns>
        public string Load(string testAssemblyPath, IDictionary<string, object> settings)
            => _api.Load(testAssemblyPath, settings);

        /// <summary>
        /// Counts the number of test cases for the loaded test assembly
        /// </summary>
        /// <param name="filter">The XML test filter</param>
        /// <returns>The number of test cases</returns>
        public int CountTestCases(string filter) => _api.CountTestCases(filter);

        /// <summary>
        /// Executes the tests in an assembly.
        /// </summary>
        /// <param name="listener">An ITestEventHandler that receives progress notices</param>
        /// <param name="filter">A filter that controls which tests are executed</param>
        /// <returns>An Xml string representing the result</returns>
        public string Run(ITestEventListener? listener, string filter) => _api.Run(listener, filter);

        /// <summary>
        /// Executes the tests in an assembly asynchronously.
        /// </summary>
        /// <param name="callback">A callback that receives XML progress notices</param>
        /// <param name="filter">A filter that controls which tests are executed</param>
        public void RunAsync(Action<string> callback, string filter) => _api.RunAsync(callback, filter);

        /// <summary>
        /// Cancel the ongoing test run. If no  test is running, the call is ignored.
        /// </summary>
        /// <param name="force">If true, cancel any ongoing test threads, otherwise wait for them to complete.</param>
        public void StopRun(bool force) => _api.StopRun(force);

        /// <summary>
        /// Returns information about the tests in an assembly.
        /// </summary>
        /// <param name="filter">A filter indicating which tests to include</param>
        /// <returns>An Xml string representing the tests</returns>
        public string Explore(string filter) => _api.Explore(filter);
    }
}
