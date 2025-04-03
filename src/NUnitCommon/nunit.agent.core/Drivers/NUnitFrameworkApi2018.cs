// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

#if NETCOREAPP
using NUnit.Engine.Internal;
#endif

namespace NUnit.Engine.Drivers
{
    /// <summary>
    /// This is the revised API, designed for use with .NET Core. It first
    /// appears in our source code in 2018. This implementation is modified
    /// to make it work under the .NET Framework as well as .NET Core. It
    /// may be used for NUnit 3.10 or higher.
    /// </summary>
#if NETFRAMEWORK
    public class NUnitFrameworkApi2018 : MarshalByRefObject, NUnitFrameworkApi
#else
    public class NUnitFrameworkApi2018 : NUnitFrameworkApi
#endif
    {
        static readonly Logger log = InternalTrace.GetLogger(nameof(NUnitFrameworkApi2018));

        const string LOAD_MESSAGE = "Method called without calling Load first. Possible error in runner.";
        const string INVALID_FRAMEWORK_MESSAGE = "Running tests against this version of the framework using this driver is not supported. Please update NUnit.Framework to the latest version.";
        const string FAILED_TO_LOAD_ASSEMBLY = "Failed to load assembly ";
        const string FAILED_TO_LOAD_NUNIT = "Failed to load the NUnit Framework in the test assembly";

        const string CONTROLLER_TYPE = "NUnit.Framework.Api.FrameworkController";

        private readonly string _driverId;

        private readonly AssemblyName _nunitRef;

        private object? _frameworkController;
        private Type? _frameworkControllerType;

#if NETCOREAPP
        private TestAssemblyLoadContext? _assemblyLoadContext;
        private Assembly? _frameworkAssembly;
#endif

        private string? _testAssemblyPath;

        public NUnitFrameworkApi2018(string driverId, AssemblyName nunitRef)
        {
            Guard.ArgumentNotNull(driverId, nameof(driverId));
            Guard.ArgumentNotNull(nunitRef, nameof(nunitRef));

            _driverId = driverId;
            _nunitRef = nunitRef;
        }

        public string Load(string testAssemblyPath, IDictionary<string, object> settings)
        {
            Guard.ArgumentNotNull(testAssemblyPath, nameof(testAssemblyPath));
            Guard.ArgumentNotNull(settings, nameof(settings));
            Guard.ArgumentValid(File.Exists(testAssemblyPath), "Framework driver called with a file name that doesn't exist.", nameof(testAssemblyPath));
            log.Info($"Loading {testAssemblyPath} - see separate log file");

            _testAssemblyPath = Path.GetFullPath(testAssemblyPath);
            var idPrefix = _driverId + "-";

#if NETFRAMEWORK
            try
            {
                _frameworkController = AppDomain.CurrentDomain.CreateInstanceAndUnwrap(
                    _nunitRef.FullName,
                    CONTROLLER_TYPE,
                    false,
                    0,
                    null,
                    new object[] { _testAssemblyPath, idPrefix, settings },
                    null,
                    null).ShouldNotBeNull();
            }
            catch (Exception ex)
            {
                string msg = $"Failed to load {_nunitRef.FullName}\r\n  Codebase: {_nunitRef.CodeBase}";
                throw new Exception(msg, ex);
            }

            _frameworkControllerType = _frameworkController?.GetType();
            log.Debug($"Created FrameworkController {_frameworkControllerType?.Name}");

            var controllerAssembly = _frameworkControllerType?.Assembly?.GetName();
            log.Debug($"Controller assembly is {controllerAssembly}");
#else
            _assemblyLoadContext = new TestAssemblyLoadContext(testAssemblyPath);

            var testAssembly = LoadAssembly(testAssemblyPath);
            _frameworkAssembly = LoadAssembly(_nunitRef);

            _frameworkController = CreateInstance(CONTROLLER_TYPE, testAssembly, idPrefix, settings);
            if (_frameworkController == null)
            {
                log.Error(INVALID_FRAMEWORK_MESSAGE);
                throw new NUnitEngineException(INVALID_FRAMEWORK_MESSAGE);
            }
#endif

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
#if NETFRAMEWORK
            Action<string>? callback = listener != null ? listener.OnTestEvent : null;
#else
            Action<string>? callback = listener != null ? listener.OnTestEvent : null;
#endif
            return (string)ExecuteMethod(RUN_METHOD, [typeof(Action<string>), typeof(string)], callback, filter);
        }

        public void RunAsync(Action<string> callback, string filter)
        {
            CheckLoadWasCalled();
            log.Info("Running {0} - see separate log file", Path.GetFileName(_testAssemblyPath.ShouldNotBeNull()));
            ExecuteMethod(RUN_ASYNC_METHOD, [typeof(Action<string>), typeof(string)], callback, filter);
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

#if NETCOREAPP
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
                log.Error($"{FAILED_TO_LOAD_ASSEMBLY}\r\n{e}");
                throw new NUnitEngineException(FAILED_TO_LOAD_NUNIT, e);
            }

            log.Debug($"Loaded {assemblyName.FullName}");
            return assembly;
        }
#endif

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

#if NETFRAMEWORK
            return method.Invoke(_frameworkController, args).ShouldNotBeNull();
#else
            //using (_assemblyLoadContext.ShouldNotBeNull().EnterContextualReflection())
            //{
            return method.Invoke(_frameworkController, args).ShouldNotBeNull();
            //}
#endif
        }

#if NETFRAMEWORK
        public override object InitializeLifetimeService()
        {
            return null!;
        }
#endif
    }
}
