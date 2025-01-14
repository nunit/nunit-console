﻿// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

#if NETCOREAPP
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using NUnit.Common;
using NUnit.Engine.Internal;

namespace NUnit.Engine.Drivers
{
    /// <summary>
    /// This is the revised API, designed for use with .NET Core. It first
    /// appears in our source code in 2018.
    /// </summary>
    public class NUnitFrameworkApi2018 : NUnitFrameworkApi
    {
        static readonly ILogger log = InternalTrace.GetLogger(nameof(NUnitFrameworkApi2018));

        const string LOAD_MESSAGE = "Method called without calling Load first. Possible error in runner.";
        const string INVALID_FRAMEWORK_MESSAGE = "Running tests against this version of the framework using this driver is not supported. Please update NUnit.Framework to the latest version.";
        const string FAILED_TO_LOAD_ASSEMBLY = "Failed to load assembly ";
        const string FAILED_TO_LOAD_NUNIT = "Failed to load the NUnit Framework in the test assembly";

        const string CONTROLLER_TYPE = "NUnit.Framework.Api.FrameworkController";

        NUnitFrameworkDriver _driver;

        AssemblyName _nunitRef;
        string? _testAssemblyPath;

        TestAssemblyLoadContext? _assemblyLoadContext;
        Assembly? _testAssembly;
        Assembly? _frameworkAssembly;

        object? _frameworkController;
        Type? _frameworkControllerType;

        public NUnitFrameworkApi2018(NUnitFrameworkDriver driver, AssemblyName nunitRef)
        {
            _driver = driver;
            _nunitRef = nunitRef;
        }

        public string Load(string testAssemblyPath, IDictionary<string, object> settings)
        {
            Guard.ArgumentValid(File.Exists(testAssemblyPath), "Framework driver called with a file name that doesn't exist.", "testAssemblyPath");
            log.Info($"Loading {testAssemblyPath} - see separate log file");

            _testAssemblyPath = Path.GetFullPath(testAssemblyPath);
            var idPrefix = string.IsNullOrEmpty(_driver.ID) ? "" : _driver.ID + "-";
            _assemblyLoadContext = new TestAssemblyLoadContext(testAssemblyPath);

            _testAssembly = LoadAssembly(testAssemblyPath);
            _frameworkAssembly = LoadAssembly(_nunitRef);

            _frameworkController = CreateInstance(CONTROLLER_TYPE, _testAssembly, idPrefix, settings);
            if (_frameworkController == null)
            {
                log.Error(INVALID_FRAMEWORK_MESSAGE);
                throw new NUnitEngineException(INVALID_FRAMEWORK_MESSAGE);
            }

            _frameworkControllerType = _frameworkController?.GetType();
            log.Debug($"Created FrameworkController {_frameworkControllerType?.Name}");

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
            log.Info("Running {0} - see separate log file", Path.GetFileName(_testAssemblyPath.ShouldNotBeNull()));
            Action<string>? callback = listener != null ? listener.OnTestEvent : (Action<string>?)null;
            return (string)ExecuteMethod(RUN_METHOD, new[] { typeof(Action<string>), typeof(string) }, callback, filter);
        }

        public void RunAsync(Action<string> callback, string filter)
        {
            CheckLoadWasCalled();
            log.Info("Running {0} - see separate log file", Path.GetFileName(_testAssemblyPath.ShouldNotBeNull()));
            ExecuteMethod(RUN_ASYNC_METHOD, new[] { typeof(Action<string>), typeof(string) }, callback, filter);
        }

        public void StopRun(bool force)
        {
            ExecuteMethod(STOP_RUN_METHOD, force);
        }

        public string Explore(string filter)
        {
            CheckLoadWasCalled();
            log.Info("Exploring {0} - see separate log file", Path.GetFileName(_testAssemblyPath.ShouldNotBeNull()));
            return (string)ExecuteMethod(EXPLORE_METHOD, filter);
        }

        private void CheckLoadWasCalled()
        {
            if (_frameworkController == null)
                throw new InvalidOperationException(LOAD_MESSAGE);
        }

        private object CreateInstance(string typeName, params object?[]? args)
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

        // API methods with no overloads
        private static readonly string LOAD_METHOD = "LoadTests";
        private static readonly string EXPLORE_METHOD = "ExploreTests";
        private static readonly string COUNT_METHOD = "CountTests";
        private static readonly string STOP_RUN_METHOD = "StopRun";

        // Execute methods with no overloads
        private object ExecuteMethod(string methodName, params object?[] args)
        {
            log.Debug($"Calling API method {methodName} with args {string.Join("+", args)}");
            var method = _frameworkControllerType.ShouldNotBeNull().GetMethod(methodName, BindingFlags.Public | BindingFlags.Instance);
            return ExecuteMethod(method, args);
        }

        // API methods with overloads
        private static readonly string RUN_METHOD = "RunTests";
        private static readonly string RUN_ASYNC_METHOD = "RunTests";

        // Execute overloaded methods specifying argument types
        private object ExecuteMethod(string methodName, Type[] ptypes, params object?[] args)
        {
            log.Debug($"Calling API method {methodName} with arg types {string.Join<Type>("+", ptypes)}");
            var method = _frameworkControllerType.ShouldNotBeNull().GetMethod(methodName, ptypes);
            return ExecuteMethod(method, args);
        }

        private object ExecuteMethod(MethodInfo? method, params object?[] args)
        {
            if (method == null)
                throw new NUnitEngineException(INVALID_FRAMEWORK_MESSAGE);

            log.Debug($"Executing {method.DeclaringType}.{method.Name}");
            using (_assemblyLoadContext.ShouldNotBeNull().EnterContextualReflection())
            {
                return method.Invoke(_frameworkController, args).ShouldNotBeNull();
            }
        }
    }
}
#endif
