// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.Serialization;
using NUnit.Common;
using NUnit.Engine.Internal;

namespace NUnit.Engine.Drivers
{
#if NETFRAMEWORK
        /// <summary>
        /// This is the original NUnit 3 API, which only works for .NET Framework.
        /// As far as I can discover, it first appeared in pre-release 2.9.1,
        /// on launchpad in 2009, hence the name.
        /// </summary>
        class NUnitFrameworkApi2009 : NUnitFrameworkApi
        {
            static readonly ILogger log = InternalTrace.GetLogger(nameof(NUnitFrameworkApi2009));

            const string LOAD_MESSAGE = "Method called without calling Load first. Possible error in runner.";
            const string INVALID_FRAMEWORK_MESSAGE = "Running tests against this version of the framework using this driver is not supported. Please update NUnit.Framework to the latest version.";
            const string FAILED_TO_LOAD_ASSEMBLY = "Failed to load assembly ";
            const string FAILED_TO_LOAD_NUNIT = "Failed to load the NUnit Framework in the test assembly";

            const string CONTROLLER_TYPE = "NUnit.Framework.Api.FrameworkController";

            NUnitFrameworkDriver _driver;

            AppDomain _testDomain;
            AssemblyName _nunitRef;

            string? _testAssemblyPath;
            
            object? _frameworkController;
            Type? _frameworkControllerType;

            public NUnitFrameworkApi2009(NUnitFrameworkDriver driver, AppDomain testDomain, AssemblyName nunitRef)
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
            }

            public int CountTestCases(string filter)
            {
                CheckLoadWasCalled();
                return int.Parse(ExecuteAction(COUNT_ACTION, filter));
            }

            public string Run(ITestEventListener? listener, string filter)
            {
                CheckLoadWasCalled();
                log.Info("Running {0} - see separate log file", Path.GetFileName(_testAssemblyPath.ShouldNotBeNull()));
                return ExecuteAction(RUN_ACTION, listener, filter);
            }

            public void RunAsync(Action<string> callback, string filter) => throw new NotImplementedException();

            public void StopRun(bool force) => ExecuteAction(STOP_RUN_ACTION, force);

            public string Explore(string filter)
            {
                CheckLoadWasCalled();
                log.Info("Exploring {0} - see separate log file", Path.GetFileName(_testAssemblyPath.ShouldNotBeNull()));
                return ExecuteAction(EXPLORE_ACTION, filter);
            }

            private void CheckLoadWasCalled()
            {
                if (_frameworkController == null)
                    throw new InvalidOperationException(LOAD_MESSAGE);
            }

            // Actions with no extra arguments beyond controller and handler
            const string LOAD_ACTION = CONTROLLER_TYPE + "+LoadTestsAction";
            private string ExecuteAction(string action)
            {
                CallbackHandler handler = new CallbackHandler();
                CreateObject(action, _frameworkController, handler);
                return handler.Result.ShouldNotBeNull();
            }

            // Actions with one extra argument
            const string EXPLORE_ACTION = CONTROLLER_TYPE + "+ExploreTestsAction";
            const string COUNT_ACTION = CONTROLLER_TYPE + "+CountTestsAction";
            const string STOP_RUN_ACTION = CONTROLLER_TYPE + "+StopRunAction";
            private string ExecuteAction(string action, object arg1)
            {
                CallbackHandler handler = new CallbackHandler();
                CreateObject(action, _frameworkController, arg1, handler);
                return handler.Result.ShouldNotBeNull();
            }

            // Run action has two extra arguments and uses a special handler
            const string RUN_ACTION = CONTROLLER_TYPE + "+RunTestsAction";
            private string ExecuteAction(string action, ITestEventListener? listener, string filter)
            {
                RunTestsCallbackHandler handler = new RunTestsCallbackHandler(listener);
                CreateObject(action, _frameworkController, filter, handler);
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
        }
#endif
}
