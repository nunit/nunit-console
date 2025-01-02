// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

#if NETFRAMEWORK
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.Serialization;
using NUnit.Common;
using NUnit.Engine.Internal;
using NUnit.Engine.Extensibility;
using System.Diagnostics.CodeAnalysis;

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

        static readonly ILogger log = InternalTrace.GetLogger("NUnitFrameworkDriver");

        readonly AppDomain _testDomain;
        readonly AssemblyName _reference;
        string? _testAssemblyPath;

        object? _frameworkController;

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

        public string ID { get; set; } = string.Empty;

        /// <summary>
        /// Loads the tests in an assembly.
        /// </summary>
        /// <returns>An Xml string representing the loaded test</returns>
        public string Load(string testAssemblyPath, IDictionary<string, object> settings)
        {
            Guard.ArgumentValid(File.Exists(testAssemblyPath), "Framework driver constructor called with a file name that doesn't exist.", "testAssemblyPath");

            var idPrefix = string.IsNullOrEmpty(ID) ? "" : ID + "-";

            // Normally, the runner should check for an invalid requested runtime, but we make sure here
            var requestedRuntime = settings.ContainsKey(EnginePackageSettings.RequestedRuntimeFramework)
                ? settings[EnginePackageSettings.RequestedRuntimeFramework] : null;
            
            _testAssemblyPath = testAssemblyPath;

            try
            {
                _frameworkController = CreateObject(CONTROLLER_TYPE, testAssemblyPath, idPrefix, settings);
            }
            catch (BadImageFormatException ex) when (requestedRuntime != null)
            {
                throw new NUnitEngineException($"Requested runtime {requestedRuntime} is not suitable for use with test assembly {testAssemblyPath}", ex);
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

            return handler.Result.ShouldNotBeNull();
        }

        public int CountTestCases(string filter)
        {
            CheckLoadWasCalled();

            CallbackHandler handler = new CallbackHandler();

            CreateObject(COUNT_ACTION, _frameworkController.ShouldNotBeNull(), filter, handler);

            return int.Parse(handler.Result.ShouldNotBeNull());
        }

        /// <summary>
        /// Executes the tests in an assembly.
        /// </summary>
        /// <param name="listener">An ITestEventHandler that receives progress notices</param>
        /// <param name="filter">A filter that controls which tests are executed</param>
        /// <returns>An Xml string representing the result</returns>
        public string Run(ITestEventListener? listener, string filter)
        {
            CheckLoadWasCalled();

            var handler = new RunTestsCallbackHandler(listener);

            log.Info("Running {0} - see separate log file", Path.GetFileName(_testAssemblyPath.ShouldNotBeNull()));
            CreateObject(RUN_ACTION, _frameworkController.ShouldNotBeNull(), filter, handler);

            return handler.Result.ShouldNotBeNull();
        }

        /// <summary>
        /// Cancel the ongoing test run. If no  test is running, the call is ignored.
        /// </summary>
        /// <param name="force">If true, cancel any ongoing test threads, otherwise wait for them to complete.</param>
        public void StopRun(bool force)
        {
            CreateObject(STOP_RUN_ACTION, _frameworkController.ShouldNotBeNull(), force, new CallbackHandler());
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

            log.Info("Exploring {0} - see separate log file", Path.GetFileName(_testAssemblyPath.ShouldNotBeNull()));
            CreateObject(EXPLORE_ACTION, _frameworkController.ShouldNotBeNull(), filter, handler);

            return handler.Result.ShouldNotBeNull();
        }

        private void CheckLoadWasCalled()
        {
            if (_frameworkController == null)
                throw new InvalidOperationException(LOAD_MESSAGE);
        }

        private object CreateObject(string typeName, params object?[]? args)
        {
            try
            {
                return _testDomain.CreateInstanceAndUnwrap(
                    _reference.FullName, typeName, false, 0, null, args, null, null )!;
            }
            catch (TargetInvocationException ex)
            {
                throw new NUnitEngineException("The NUnit 3 driver encountered an error while executing reflected code.", ex.InnerException);
            }
        }
    }
}
#endif