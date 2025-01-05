// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

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
        private const string LOAD_MESSAGE = "Method called without calling Load first";
        const string INVALID_FRAMEWORK_MESSAGE = "Running tests against this version of the framework using this driver is not supported. Please update NUnit.Framework to the latest version.";
        const string FAILED_TO_LOAD_ASSEMBLY = "Failed to load assembly ";
        const string FAILED_TO_LOAD_NUNIT = "Failed to load the NUnit Framework in the test assembly";

        private static readonly string CONTROLLER_TYPE = "NUnit.Framework.Api.FrameworkController";
#if NETFRAMEWORK
        private static readonly string LOAD_ACTION = CONTROLLER_TYPE + "+LoadTestsAction";
        private static readonly string EXPLORE_ACTION = CONTROLLER_TYPE + "+ExploreTestsAction";
        private static readonly string COUNT_ACTION = CONTROLLER_TYPE + "+CountTestsAction";
        private static readonly string RUN_ACTION = CONTROLLER_TYPE + "+RunTestsAction";
        private static readonly string STOP_RUN_ACTION = CONTROLLER_TYPE + "+StopRunAction";
#else
        private static readonly string LOAD_METHOD = "LoadTests";
        private static readonly string EXPLORE_METHOD = "ExploreTests";
        private static readonly string COUNT_METHOD = "CountTests";
        private static readonly string RUN_METHOD = "RunTests";
        private static readonly string RUN_ASYNC_METHOD = "RunTests";
        private static readonly string STOP_RUN_METHOD = "StopRun";
#endif

        static readonly ILogger log = InternalTrace.GetLogger(nameof(NUnit3CombinedFrameworkDriver));

        readonly AssemblyName _nunitRef;
        string? _testAssemblyPath;

        object? _frameworkController;
        Type? _frameworkControllerType;

#if NETFRAMEWORK
        readonly AppDomain _testDomain;
#else
        Assembly? _testAssembly;
        Assembly? _frameworkAssembly;
        TestAssemblyLoadContext? _assemblyLoadContext;
#endif

#if NETFRAMEWORK
        /// <summary>
        /// Construct an NUnit3CombinedFrameworkDriver
        /// </summary>
        /// <param name="testDomain">The application domain in which to create the FrameworkController</param>
        /// <param name="nunitRef">An AssemblyName referring to the test framework.</param>
        public NUnit3CombinedFrameworkDriver(AppDomain testDomain, AssemblyName nunitRef)
        {
            _testDomain = testDomain;
            _nunitRef = nunitRef;
        }
#else
        /// <summary>
        /// Construct an NUnitNetCore31Driver
        /// </summary>
        /// <param name="reference">An AssemblyName referring to the test framework.</param>
        public NUnit3CombinedFrameworkDriver(AssemblyName nunitRef)
        {
            Guard.ArgumentNotNull(nunitRef, nameof(nunitRef));
            _nunitRef = nunitRef;
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
        {
            Guard.ArgumentValid(File.Exists(testAssemblyPath), "Framework driver called with a file name that doesn't exist.", "testAssemblyPath");
            log.Debug($"Loading {testAssemblyPath}");
            var idPrefix = string.IsNullOrEmpty(ID) ? "" : ID + "-";

            // Normally, the caller should check for an invalid requested runtime, but we make sure here
            var requestedRuntime = settings.ContainsKey(EnginePackageSettings.RequestedRuntimeFramework)
                ? settings[EnginePackageSettings.RequestedRuntimeFramework] : null;

            _testAssemblyPath = Path.GetFullPath(testAssemblyPath);
#if NETFRAMEWORK
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
#else
            _assemblyLoadContext = new TestAssemblyLoadContext(_testAssemblyPath);

            _testAssembly = LoadAssembly(_testAssemblyPath!);
            _frameworkAssembly = LoadAssembly(_nunitRef);

            _frameworkController = CreateObject(CONTROLLER_TYPE, _testAssembly, idPrefix, settings);
            if (_frameworkController == null)
            {
                log.Error(INVALID_FRAMEWORK_MESSAGE);
                throw new NUnitEngineException(INVALID_FRAMEWORK_MESSAGE);
            }
#endif

            _frameworkControllerType = _frameworkController.GetType();
            log.Debug($"Created FrameworkController {_frameworkControllerType.Name}");

            var fileName = Path.GetFileName(_testAssemblyPath);
            log.Info("Loading {0} - see separate log file", fileName);

#if NETFRAMEWORK
            return ExecuteAction(LOAD_ACTION);
            //log.Debug($"Loaded {_testAssemblyPath}");
#else
            log.Debug($"Loaded {_testAssemblyPath}");
            return (string)ExecuteMethod(LOAD_METHOD);
#endif
        }

        /// <summary>
        /// Counts the number of test cases for the loaded test assembly
        /// </summary>
        /// <param name="filter">The XML test filter</param>
        /// <returns>The number of test cases</returns>
        public int CountTestCases(string filter)
        {
            CheckLoadWasCalled();
            return PerformCountTestCases(filter);
        }

        public int PerformCountTestCases(string filter)
        { 
#if NETFRAMEWORK
            CallbackHandler handler = new CallbackHandler();
            CreateObject(COUNT_ACTION, _frameworkController.ShouldNotBeNull(), filter, handler);
            return int.Parse(handler.Result.ShouldNotBeNull());
#else
            object? count = ExecuteMethod(COUNT_METHOD, filter);
            return count != null ? (int)count : 0;
#endif
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
#if NETFRAMEWORK
            log.Info("Running {0} - see separate log file", Path.GetFileName(_testAssemblyPath.ShouldNotBeNull()));
            var handler = new RunTestsCallbackHandler(listener);
            CreateObject(RUN_ACTION, _frameworkController.ShouldNotBeNull(), filter, handler);
            return handler.Result.ShouldNotBeNull();
#else
            log.Info("Running {0} - see separate log file", _testAssembly.ShouldNotBeNull().FullName!);
            Action<string>? callback = listener != null ? listener.OnTestEvent : (Action<string>?)null;
            return (string)ExecuteMethod(RUN_METHOD, new[] { typeof(Action<string>), typeof(string) }, callback, filter);
#endif
        }

#if NETCOREAPP
        /// <summary>
        /// Executes the tests in an assembly asynchronously.
        /// </summary>
        /// <param name="callback">A callback that receives XML progress notices</param>
        /// <param name="filter">A filter that controls which tests are executed</param>
        public void RunAsync(Action<string> callback, string filter)
        {
            CheckLoadWasCalled();
            log.Info("Running {0} - see separate log file", _testAssembly.ShouldNotBeNull().FullName!);
            ExecuteMethod(RUN_ASYNC_METHOD, new[] { typeof(Action<string>), typeof(string) }, callback, filter);
        }
#endif

        /// <summary>
        /// Cancel the ongoing test run. If no  test is running, the call is ignored.
        /// </summary>
        /// <param name="force">If true, cancel any ongoing test threads, otherwise wait for them to complete.</param>
        public void StopRun(bool force)
        {
#if NETFRAMEWORK
            CreateObject(STOP_RUN_ACTION, _frameworkController.ShouldNotBeNull(), force, new CallbackHandler());
#else
            ExecuteMethod(STOP_RUN_METHOD, force);
#endif
        }

        /// <summary>
        /// Returns information about the tests in an assembly.
        /// </summary>
        /// <param name="filter">A filter indicating which tests to include</param>
        /// <returns>An Xml string representing the tests</returns>
        public string Explore(string filter)
        {
            CheckLoadWasCalled();
#if NETFRAMEWORK
            log.Info("Exploring {0} - see separate log file", Path.GetFileName(_testAssemblyPath.ShouldNotBeNull()));
            CallbackHandler handler = new CallbackHandler();
            CreateObject(EXPLORE_ACTION, _frameworkController.ShouldNotBeNull(), filter, handler);
            return handler.Result.ShouldNotBeNull();
#else
            log.Info("Exploring {0} - see separate log file", _testAssembly.ShouldNotBeNull().FullName!);
            return (string)ExecuteMethod(EXPLORE_METHOD, filter);
#endif
        }

        private void CheckLoadWasCalled()
        {
            if (_frameworkController == null)
                throw new InvalidOperationException(LOAD_MESSAGE);
        }

#if NETFRAMEWORK
        private string ExecuteAction(string action, params object?[] args)
        {
            CallbackHandler handler = new CallbackHandler();
            CreateObject(action, _frameworkController, handler);
            return handler.Result.ShouldNotBeNull();
        }

        private object CreateObject(string typeName, params object?[]? args)
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
#else
       private object CreateObject(string typeName, params object?[]? args)
        {
            var type = _frameworkAssembly.ShouldNotBeNull().GetType(typeName, throwOnError: true)!;
            return Activator.CreateInstance(type, args)!;
        }
        
        private Assembly LoadAssembly(string assemblyPath)
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

        private Assembly LoadAssembly(AssemblyName assemblyName)
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

        object ExecuteMethod(string methodName, params object?[] args)
        {
            var method = _frameworkControllerType.ShouldNotBeNull().GetMethod(methodName, BindingFlags.Public | BindingFlags.Instance);
            return ExecuteMethod(method, args);
        }

        object ExecuteMethod(string methodName, Type[] ptypes, params object?[] args)
        {
            var method = _frameworkControllerType.ShouldNotBeNull().GetMethod(methodName, ptypes);
            return ExecuteMethod(method, args);
        }

        object ExecuteMethod(MethodInfo? method, params object?[] args)
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
#endif
    }
}
