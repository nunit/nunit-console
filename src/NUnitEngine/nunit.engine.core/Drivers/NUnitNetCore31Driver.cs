// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

#if NETCOREAPP
using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using NUnit.Common;
using NUnit.Engine.Internal;
using NUnit.Engine.Extensibility;

namespace NUnit.Engine.Drivers
{
    /// <summary>
    /// NUnitNetCore31Driver is used by the test-runner to load and run
    /// tests using the NUnit framework assembly. It contains functionality to
    /// correctly load assemblies from other directories, using APIs first available in
    /// .NET Core 3.1.
    /// </summary>
    public class NUnitNetCore31Driver : IFrameworkDriver
    {
        private const string LOAD_MESSAGE = "Method called without calling Load first";
        const string INVALID_FRAMEWORK_MESSAGE = "Running tests against this version of the framework using this driver is not supported. Please update NUnit.Framework to the latest version.";
        const string FAILED_TO_LOAD_ASSEMBLY = "Failed to load assembly ";
        const string FAILED_TO_LOAD_NUNIT = "Failed to load the NUnit Framework in the test assembly";

        private static readonly string CONTROLLER_TYPE = "NUnit.Framework.Api.FrameworkController";
        private static readonly string LOAD_METHOD = "LoadTests";
        private static readonly string EXPLORE_METHOD = "ExploreTests";
        private static readonly string COUNT_METHOD = "CountTests";
        private static readonly string RUN_METHOD = "RunTests";
        private static readonly string RUN_ASYNC_METHOD = "RunTests";
        private static readonly string STOP_RUN_METHOD = "StopRun";

        static readonly ILogger log = InternalTrace.GetLogger(nameof(NUnitNetCore31Driver));

        readonly AssemblyName _nunitRef;
        string? _testAssemblyPath;

        object? _frameworkController;
        Type? _frameworkControllerType;
        Assembly? _testAssembly;
        Assembly? _frameworkAssembly;
        TestAssemblyLoadContext? _assemblyLoadContext;

        /// <summary>
        /// Construct an NUnitNetCore31Driver
        /// </summary>
        /// <param name="reference">An AssemblyName referring to the test framework.</param>
        public NUnitNetCore31Driver(AssemblyName nunitRef)
        {
            Guard.ArgumentNotNull(nunitRef, nameof(nunitRef));
            _nunitRef = nunitRef;
        }

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
            _assemblyLoadContext = new TestAssemblyLoadContext(_testAssemblyPath);

            _testAssembly = LoadAssembly(_testAssemblyPath!);
            _frameworkAssembly = LoadAssembly(_nunitRef);

            _frameworkController = CreateObject(CONTROLLER_TYPE, _testAssembly, idPrefix, settings);
            if (_frameworkController == null)
            {
                log.Error(INVALID_FRAMEWORK_MESSAGE);
                throw new NUnitEngineException(INVALID_FRAMEWORK_MESSAGE);
            }

            _frameworkControllerType = _frameworkController.GetType();
            log.Debug($"Created FrameworkControler {_frameworkControllerType.Name}");

            log.Info("Loading {0} - see separate log file", _testAssembly.FullName!);
            return (string)ExecuteMethod(LOAD_METHOD);
        }

        /// <summary>
        /// Counts the number of test cases for the loaded test assembly
        /// </summary>
        /// <param name="filter">The XML test filter</param>
        /// <returns>The number of test cases</returns>
        public int CountTestCases(string filter)
        {
            CheckLoadWasCalled();
            object? count = ExecuteMethod(COUNT_METHOD, filter);
            return count != null ? (int)count : 0;
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
            log.Info("Running {0} - see separate log file", _testAssembly.ShouldNotBeNull().FullName!);
            Action<string>? callback = listener != null ? listener.OnTestEvent : (Action<string>?)null;
            return (string)ExecuteMethod(RUN_METHOD, new[] { typeof(Action<string>), typeof(string) }, callback, filter);
        }

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

        /// <summary>
        /// Cancel the ongoing test run. If no  test is running, the call is ignored.
        /// </summary>
        /// <param name="force">If true, cancel any ongoing test threads, otherwise wait for them to complete.</param>
        public void StopRun(bool force)
        {
            ExecuteMethod(STOP_RUN_METHOD, force);
        }

        /// <summary>
        /// Returns information about the tests in an assembly.
        /// </summary>
        /// <param name="filter">A filter indicating which tests to include</param>
        /// <returns>An Xml string representing the tests</returns>
        public string Explore(string filter)
        {
            CheckLoadWasCalled();
            log.Info("Exploring {0} - see separate log file", _testAssembly.ShouldNotBeNull().FullName!);
            return (string)ExecuteMethod(EXPLORE_METHOD, filter);
        }

        private void CheckLoadWasCalled()
        {
            if (_frameworkController == null)
                throw new InvalidOperationException(LOAD_MESSAGE);
        }

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

            using (_assemblyLoadContext.ShouldNotBeNull().EnterContextualReflection())
            {
                log.Debug($"Executing {method.DeclaringType}.{method.Name}");
                return method.Invoke(_frameworkController, args).ShouldNotBeNull();
            }
        }
    }
}
#endif