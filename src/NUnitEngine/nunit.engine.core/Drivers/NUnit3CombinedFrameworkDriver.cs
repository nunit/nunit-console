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
    /// NUnit3CombinedFrameworkDriver is used by the test-runner to load and run
    /// tests using the NUnit framework assembly.
    /// </summary>
    public class NUnit3CombinedFrameworkDriver : IFrameworkDriver
    {
        const string LOAD_MESSAGE = "Method called without calling Load first. Possible error in runner.";
        const string INVALID_FRAMEWORK_MESSAGE = "Running tests against this version of the framework using this driver is not supported. Please update NUnit.Framework to the latest version.";
        const string FAILED_TO_LOAD_ASSEMBLY = "Failed to load assembly ";
        const string FAILED_TO_LOAD_NUNIT = "Failed to load the NUnit Framework in the test assembly";

        const string CONTROLLER_TYPE = "NUnit.Framework.Api.FrameworkController";

        static readonly ILogger log = InternalTrace.GetLogger(nameof(NUnit3CombinedFrameworkDriver));

        readonly FrameworkApi _api;

#if NETFRAMEWORK
        /// <summary>
        /// Construct an NUnit3CombinedFrameworkDriver
        /// </summary>
        /// <param name="testDomain">The application domain in which to create the FrameworkController</param>
        /// <param name="nunitRef">An AssemblyName referring to the test framework.</param>
        public NUnit3CombinedFrameworkDriver(AppDomain testDomain, AssemblyName nunitRef)
        {
            Guard.ArgumentNotNull(nunitRef, nameof(testDomain));
            Guard.ArgumentNotNull(nunitRef, nameof(nunitRef));
            _api = new Api2009(this, testDomain, nunitRef);
        }
#else
        /// <summary>
        /// Construct an NUnitNetCore31Driver
        /// </summary>
        /// <param name="reference">An AssemblyName referring to the test framework.</param>
        public NUnit3CombinedFrameworkDriver(AssemblyName nunitRef)
        {
            Guard.ArgumentNotNull(nunitRef, nameof(nunitRef));
            _api = new Api2018(this, nunitRef);
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

#if NETFRAMEWORK
        /// <summary>
        /// This is the original NUnit 3 API, which only works for .NET Framework.
        /// As far as I can discover, it first appeared in pre-release 2.9.1,
        /// on launchpad in 2009, hence the name.
        /// </summary>
        class Api2009 : FrameworkApi
        {
            NUnit3CombinedFrameworkDriver _driver;

            AppDomain _testDomain;
            AssemblyName _nunitRef;

            string? _testAssemblyPath;
            
            object? _frameworkController;
            Type? _frameworkControllerType;

            public Api2009(NUnit3CombinedFrameworkDriver driver, AppDomain testDomain, AssemblyName nunitRef) 
            { 
                _driver = driver;
                _testDomain = testDomain;
                _nunitRef = nunitRef;
            }

            public string Load(string testAssemblyPath, IDictionary<string, object> settings)
            {
                Guard.ArgumentValid(File.Exists(testAssemblyPath), "Framework driver called with a file name that doesn't exist.", "testAssemblyPath");

                log.Info($"Loading {testAssemblyPath} - see separate log file");

                _testAssemblyPath = testAssemblyPath;

                // Normally, the caller should check for an invalid requested runtime, but we make sure here
                var requestedRuntime = settings.ContainsKey(EnginePackageSettings.RequestedRuntimeFramework)
                    ? settings[EnginePackageSettings.RequestedRuntimeFramework] : null;

                var idPrefix = string.IsNullOrEmpty(_driver.ID) ? "" : _driver.ID + "-";
                try
                {
                    _frameworkController = CreateObject(CONTROLLER_TYPE, _testAssemblyPath, idPrefix, settings);
                }
                catch (BadImageFormatException ex) when (requestedRuntime != null)
                {
                    throw new NUnitEngineException($"Requested runtime {requestedRuntime} is not suitable for use with test assembly {_testAssemblyPath}", ex);
                }
                catch (SerializationException ex)
                {
                    throw new NUnitEngineException("The NUnit 3 driver cannot support this test assembly. Use a platform specific runner.", ex);
                }

                _frameworkControllerType = _frameworkController.GetType();
                log.Debug($"Created FrameworkController {_frameworkControllerType.Name}");

                return ExecuteAction(LOAD_ACTION);
                //log.Debug($"Loaded {_testAssemblyPath}");
            }

            public int CountTestCases(string filter)
            {
                CheckLoadWasCalled();
                CallbackHandler handler = new CallbackHandler();
                CreateObject(COUNT_ACTION, _frameworkController.ShouldNotBeNull(), filter, handler);
                return int.Parse(handler.Result.ShouldNotBeNull());
            }

            public string Run(ITestEventListener? listener, string filter)
            {
                CheckLoadWasCalled();
                log.Info("Running {0} - see separate log file", Path.GetFileName(_testAssemblyPath.ShouldNotBeNull()));
                var handler = new RunTestsCallbackHandler(listener);
                CreateObject(RUN_ACTION, _frameworkController.ShouldNotBeNull(), filter, handler);
                return handler.Result.ShouldNotBeNull();
            }

            public void RunAsync(Action<string> callback, string filter) => throw new NotImplementedException();

            public void StopRun(bool force)
            {
                CreateObject(STOP_RUN_ACTION, _frameworkController.ShouldNotBeNull(), force, new CallbackHandler());
            }

            public string Explore(string filter)
            {
                CheckLoadWasCalled();
                log.Info("Exploring {0} - see separate log file", Path.GetFileName(_testAssemblyPath.ShouldNotBeNull()));
                CallbackHandler handler = new CallbackHandler();
                CreateObject(EXPLORE_ACTION, _frameworkController.ShouldNotBeNull(), filter, handler);
                return handler.Result.ShouldNotBeNull();
            }

            public void CheckLoadWasCalled()
            {
                if (_frameworkController == null)
                    throw new InvalidOperationException(LOAD_MESSAGE);
            }

            const string LOAD_ACTION = CONTROLLER_TYPE + "+LoadTestsAction";
            const string EXPLORE_ACTION = CONTROLLER_TYPE + "+ExploreTestsAction";
            const string COUNT_ACTION = CONTROLLER_TYPE + "+CountTestsAction";
            const string RUN_ACTION = CONTROLLER_TYPE + "+RunTestsAction";
            const string STOP_RUN_ACTION = CONTROLLER_TYPE + "+StopRunAction";

            public string ExecuteAction(string action, params object?[] args)
            {
                CallbackHandler handler = new CallbackHandler();
                CreateObject(action, _frameworkController, handler);
                return handler.Result.ShouldNotBeNull();
            }

            public object CreateObject(string typeName, params object?[]? args)
            {
                try
                {
                    return _testDomain.CreateInstanceAndUnwrap(
                        _nunitRef.FullName, typeName, false, 0, null, args, null, null)!;
                }
                catch (TargetInvocationException ex)
                {
                    throw new NUnitEngineException("The NUnit 3 driver encountered an error while executing reflected code.", ex.InnerException);
                }
            }
        }
#else
        /// <summary>
        /// This is the revised API, designed for use with .NET Core. It first
        /// appears in our source code in 2018.
        /// </summary>
        class Api2018 : FrameworkApi
        {
            NUnit3CombinedFrameworkDriver _driver;
            AssemblyName _nunitRef;

            string? _testAssemblyPath;
            TestAssemblyLoadContext? _assemblyLoadContext;
            Assembly? _testAssembly;
            Assembly? _frameworkAssembly;

            object? _frameworkController;
            Type? _frameworkControllerType;

            public Api2018(NUnit3CombinedFrameworkDriver driver, AssemblyName nunitRef) 
            {
                _driver = driver;
                _nunitRef = nunitRef;
            }

            public string Load(string testAssemblyPath, IDictionary<string, object> settings)
            {
                Guard.ArgumentValid(File.Exists(testAssemblyPath), "Framework driver called with a file name that doesn't exist.", "testAssemblyPath");

                log.Info($"Loading {testAssemblyPath} - see separate log file");

                _testAssemblyPath = Path.GetFullPath(testAssemblyPath);

                _assemblyLoadContext = new TestAssemblyLoadContext(testAssemblyPath);

                _testAssembly = LoadAssembly(testAssemblyPath);
                _frameworkAssembly = LoadAssembly(_nunitRef);

                var idPrefix = string.IsNullOrEmpty(_driver.ID) ? "" : _driver.ID + "-";
                _frameworkController = CreateObject(CONTROLLER_TYPE, _testAssembly, idPrefix, settings);
                if (_frameworkController == null)
                {
                    log.Error(INVALID_FRAMEWORK_MESSAGE);
                    throw new NUnitEngineException(INVALID_FRAMEWORK_MESSAGE);
                }

                _frameworkControllerType = _frameworkController.GetType();
                log.Debug($"Created FrameworkController {_frameworkControllerType.Name}");

                log.Debug($"Loaded {testAssemblyPath}");
                return (string)ExecuteMethod(LOAD_METHOD);
            }

            public int CountTestCases(string filter)
            {
                CheckLoadWasCalled();
                object? count = ExecuteMethod(COUNT_METHOD, filter);
                return count != null ? (int)count : 0;
            }

            public string Run(ITestEventListener? listener, string filter)
            {
                CheckLoadWasCalled();
                log.Info("Running {0} - see separate log file", _testAssembly.ShouldNotBeNull().FullName!);
                Action<string>? callback = listener != null ? listener.OnTestEvent : (Action<string>?)null;
                return (string)ExecuteMethod(RUN_METHOD, new[] { typeof(Action<string>), typeof(string) }, callback, filter);
            }

            public void RunAsync(Action<string> callback, string filter)
            {
                CheckLoadWasCalled();
                log.Info("Running {0} - see separate log file", _testAssembly.ShouldNotBeNull().FullName!);
                ExecuteMethod(RUN_ASYNC_METHOD, new[] { typeof(Action<string>), typeof(string) }, callback, filter);
            }

            public void StopRun(bool force)
            {
                ExecuteMethod(STOP_RUN_METHOD, force);
            }

            public string Explore(string filter)
            {
                CheckLoadWasCalled();
                log.Info("Exploring {0} - see separate log file", _testAssembly.ShouldNotBeNull().FullName!);
                return (string)ExecuteMethod(EXPLORE_METHOD, filter);
            }

            public void CheckLoadWasCalled()
            {
                if (_frameworkController == null)
                    throw new InvalidOperationException(LOAD_MESSAGE);
            }


            public object CreateObject(string typeName, params object?[]? args)
            {
                var type = _frameworkAssembly.ShouldNotBeNull().GetType(typeName, throwOnError: true)!;
                return Activator.CreateInstance(type, args)!;
            }

            public Assembly LoadAssembly(string assemblyPath)
            {
                Assembly assembly;

                try
                {
                    assembly = _assemblyLoadContext?.LoadFromAssemblyPath(assemblyPath)!;
                    if (assembly == null)
                        throw new Exception("LoadFromAssemblyPath returned null");
                }
                catch (Exception e)
                {
                    var msg = string.Format(FAILED_TO_LOAD_ASSEMBLY + assemblyPath);
                    log.Error(msg);
                    throw new NUnitEngineException(msg, e);
                }

                log.Debug($"Loaded {assemblyPath}");
                return assembly;
            }

            public Assembly LoadAssembly(AssemblyName assemblyName)
            {
                Assembly assembly;

                try
                {
                    assembly = _assemblyLoadContext?.LoadFromAssemblyName(assemblyName)!;
                    if (assembly == null)
                        throw new Exception("LoadFromAssemblyName returned null");
                }
                catch (Exception e)
                {
                    var msg = string.Format(FAILED_TO_LOAD_ASSEMBLY + assemblyName.FullName);
                    log.Error($"{FAILED_TO_LOAD_ASSEMBLY}\r\n{e}");
                    throw new NUnitEngineException(FAILED_TO_LOAD_NUNIT, e);
                }

                log.Debug($"Loaded {assemblyName.FullName}");
                return assembly;
            }

            private static readonly string LOAD_METHOD = "LoadTests";
            private static readonly string EXPLORE_METHOD = "ExploreTests";
            private static readonly string COUNT_METHOD = "CountTests";
            private static readonly string RUN_METHOD = "RunTests";
            private static readonly string RUN_ASYNC_METHOD = "RunTests";
            private static readonly string STOP_RUN_METHOD = "StopRun";

            public object ExecuteMethod(string methodName, params object?[] args)
            {
                var method = _frameworkControllerType.ShouldNotBeNull().GetMethod(methodName, BindingFlags.Public | BindingFlags.Instance);
                return ExecuteMethod(method, args);
            }

            public object ExecuteMethod(string methodName, Type[] ptypes, params object?[] args)
            {
                var method = _frameworkControllerType.ShouldNotBeNull().GetMethod(methodName, ptypes);
                return ExecuteMethod(method, args);
            }

            public object ExecuteMethod(MethodInfo? method, params object?[] args)
            {
                if (method == null)
                {
                    throw new NUnitEngineException(INVALID_FRAMEWORK_MESSAGE);
                }

                //using (_assemblyLoadContext.ShouldNotBeNull().EnterContextualReflection())
                //{
                log.Debug($"Executing {method.DeclaringType}.{method.Name}");
                return method.Invoke(_frameworkController, args).ShouldNotBeNull();
                //}
            }
        }
#endif
        interface FrameworkApi
        {
            string Load(string testAssemblyPath, IDictionary<string, object> settings);
            int CountTestCases(string filter);
            string Run(ITestEventListener? listener, string filter);
            void RunAsync(Action<string> callback, string filter);
            void StopRun(bool force);
            string Explore(string filter);
        }
    }
}
