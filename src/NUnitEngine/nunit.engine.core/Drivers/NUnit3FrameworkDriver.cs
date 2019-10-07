// ***********************************************************************
// Copyright (c) 2009-2014 Charlie Poole, Rob Prouse
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
// ***********************************************************************

#if !NETSTANDARD1_6 && !NETSTANDARD2_0
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
    /// tests using the NUnit framework assembly.
    /// </summary>
    public class NUnit3FrameworkDriver : IFrameworkDriver
    {
        private const string LOAD_MESSAGE = "Method called without calling Load first";

        private static readonly string CONTROLLER_TYPE = "NUnit.Framework.Api.FrameworkController";
        private static readonly string LOAD_ACTION = CONTROLLER_TYPE + "+LoadTestsAction";
        private static readonly string EXPLORE_ACTION = CONTROLLER_TYPE + "+ExploreTestsAction";
        private static readonly string COUNT_ACTION = CONTROLLER_TYPE + "+CountTestsAction";
        private static readonly string RUN_ACTION = CONTROLLER_TYPE + "+RunTestsAction";
        private static readonly string STOP_RUN_ACTION = CONTROLLER_TYPE + "+StopRunAction";

        static ILogger log = InternalTrace.GetLogger("NUnitFrameworkDriver");

        AppDomain _testDomain;
        AssemblyName _reference;
        string _testAssemblyPath;

        object _frameworkController;

        /// <summary>
        /// Construct an NUnit3FrameworkDriver
        /// </summary>
        /// <param name="testDomain">The application domain in which to create the FrameworkController</param>
        /// <param name="reference">An AssemblyName referring to the test framework.</param>
        public NUnit3FrameworkDriver(AppDomain testDomain, AssemblyName reference)
        {
            _testDomain = testDomain;
            _reference = reference;
        }

        public string ID { get; set; }

        /// <summary>
        /// Loads the tests in an assembly.
        /// </summary>
        /// <returns>An Xml string representing the loaded test</returns>
        public string Load(string testAssemblyPath, IDictionary<string, object> settings)
        {
            Guard.ArgumentValid(File.Exists(testAssemblyPath), "Framework driver constructor called with a file name that doesn't exist.", "testAssemblyPath");

            var idPrefix = string.IsNullOrEmpty(ID) ? "" : ID + "-";
            _testAssemblyPath = testAssemblyPath;
            try
            {
                _frameworkController = CreateObject(CONTROLLER_TYPE, testAssemblyPath, idPrefix, settings);
            }
            catch (SerializationException ex)
            {
                throw new NUnitEngineException("The NUnit 3 driver cannot support this test assembly. Use a platform specific runner.", ex);
            }

            CallbackHandler handler = new CallbackHandler();

            var fileName = Path.GetFileName(_testAssemblyPath);

            log.Info("Loading {0} - see separate log file", fileName);

            CreateObject(LOAD_ACTION, _frameworkController, handler);

            log.Info("Loaded {0}", fileName);

            return handler.Result;
        }

        public int CountTestCases(string filter)
        {
            CheckLoadWasCalled();

            CallbackHandler handler = new CallbackHandler();

            CreateObject(COUNT_ACTION, _frameworkController, filter, handler);

            return int.Parse(handler.Result);
        }

        /// <summary>
        /// Executes the tests in an assembly.
        /// </summary>
        /// <param name="listener">An ITestEventHandler that receives progress notices</param>
        /// <param name="filter">A filter that controls which tests are executed</param>
        /// <returns>An Xml string representing the result</returns>
        public string Run(ITestEventListener listener, string filter)
        {
            CheckLoadWasCalled();

            var handler = new RunTestsCallbackHandler(listener);

            log.Info("Running {0} - see separate log file", Path.GetFileName(_testAssemblyPath));
            CreateObject(RUN_ACTION, _frameworkController, filter, handler);

            return handler.Result;
        }

        /// <summary>
        /// Cancel the ongoing test run. If no  test is running, the call is ignored.
        /// </summary>
        /// <param name="force">If true, cancel any ongoing test threads, otherwise wait for them to complete.</param>
        public void StopRun(bool force)
        {
            CreateObject(STOP_RUN_ACTION, _frameworkController, force, new CallbackHandler());
        }

        /// <summary>
        /// Returns information about the tests in an assembly.
        /// </summary>
        /// <param name="filter">A filter indicating which tests to include</param>
        /// <returns>An Xml string representing the tests</returns>
        public string Explore(string filter)
        {
            CheckLoadWasCalled();

            CallbackHandler handler = new CallbackHandler();

            log.Info("Exploring {0} - see separate log file", Path.GetFileName(_testAssemblyPath));
            CreateObject(EXPLORE_ACTION, _frameworkController, filter, handler);

            return handler.Result;
        }

        private void CheckLoadWasCalled()
        {
            if (_frameworkController == null)
                throw new InvalidOperationException(LOAD_MESSAGE);
        }

        private object CreateObject(string typeName, params object[] args)
        {
            try
            {
                return _testDomain.CreateInstanceAndUnwrap(
                    _reference.FullName, typeName, false, 0,
#if !NET_4_0
                    null, args, null, null, null);
#else
                null, args, null, null );
#endif
            }
            catch (TargetInvocationException ex)
            {
                throw new NUnitEngineException("The NUnit 3 driver encountered an error while executing reflected code.", ex.InnerException);
            }
        }
    }
}
#endif