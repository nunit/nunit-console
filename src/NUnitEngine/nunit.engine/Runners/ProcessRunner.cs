// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

#if NETFRAMEWORK
using System;
using NUnit.Common;
using NUnit.Engine.Internal;
using NUnit.Engine.Services;

namespace NUnit.Engine.Runners
{
    /// <summary>
    /// ProcessRunner loads and runs a set of tests in a single agent process.
    /// </summary>
    public class ProcessRunner : AbstractTestRunner
    {
        // ProcessRunner is given a TestPackage containing a single assembly
        // multiple assemblies, a project, multiple projects or a mix. It loads
        // and runs all tests in a single remote agent process.
        //
        // If the input contains projects, which are not summarized at a lower
        // level, the ProcessRunner should create an XML node for the entire
        // project, aggregating the assembly results.

        private const int NORMAL_TIMEOUT = 30000;               // 30 seconds
        private const int DEBUG_TIMEOUT = NORMAL_TIMEOUT * 10;  // 5 minutes

        private static readonly Logger log = InternalTrace.GetLogger(typeof(ProcessRunner));

        private IAgentLease _agentLease;
        private ITestEngineRunner _remoteRunner;
        private TestAgency _agency;

        public ProcessRunner(IServiceLocator services, TestPackage package) : base(services, package)
        {
            _agency = Services.GetService<TestAgency>();
        }

        /// <summary>
        /// Explore a TestPackage and return information about
        /// the tests found.
        /// </summary>
        /// <param name="filter">A TestFilter used to select tests</param>
        /// <returns>A TestEngineResult.</returns>
        public override TestEngineResult Explore(TestFilter filter)
        {
            try
            {
                CreateAgentAndRunner();
                return _remoteRunner.Explore(filter);
            }
            catch (Exception e)
            {
                log.Error("Failed to run remote tests {0}", ExceptionHelper.BuildMessageAndStackTrace(e));
                return CreateFailedResult(e);
            }
        }

        /// <summary>
        /// Load a TestPackage for possible execution
        /// </summary>
        /// <returns>A TestEngineResult.</returns>
        protected override TestEngineResult LoadPackage()
        {
            log.Info("Loading " + TestPackage.Name);
            Unload();

            try
            {
                CreateAgentAndRunner();

                return _remoteRunner.Load();
            }
            catch (Exception)
            {
                // TODO: Check if this is really needed
                // Clean up if the load failed
                Unload();
                throw;
            }
        }

        /// <summary>
        /// Unload any loaded TestPackage and clear
        /// the reference to the remote runner.
        /// </summary>
        public override void UnloadPackage()
        {
            try
            {
                if (_remoteRunner != null)
                {
                    log.Info("Unloading " + TestPackage.Name);
                    _remoteRunner.Unload();
                    _remoteRunner = null;
                }
            }
            catch (Exception e)
            {
                log.Warning("Failed to unload the remote runner. {0}", ExceptionHelper.BuildMessageAndStackTrace(e));
                _remoteRunner = null;
                throw;
            }
        }

        /// <summary>
        /// Count the test cases that would be run under
        /// the specified filter.
        /// </summary>
        /// <param name="filter">A TestFilter</param>
        /// <returns>The count of test cases</returns>
        public override int CountTestCases(TestFilter filter)
        {
            try
            {
                CreateAgentAndRunner();

                return _remoteRunner.CountTestCases(filter);
            }
            catch (Exception e)
            {
                log.Error("Failed to count remote tests {0}", ExceptionHelper.BuildMessageAndStackTrace(e));
                return 0;
            }
        }

        /// <summary>
        /// Run the tests in a loaded TestPackage
        /// </summary>
        /// <param name="listener">An ITestEventHandler to receive events</param>
        /// <param name="filter">A TestFilter used to select tests</param>
        /// <returns>A TestResult giving the result of the test execution</returns>
        protected override TestEngineResult RunTests(ITestEventListener listener, TestFilter filter)
        {
            log.Info("Running " + TestPackage.Name);

            try
            {
                CreateAgentAndRunner();

                var result = _remoteRunner.Run(listener, filter);
                log.Info("Done running " + TestPackage.Name);
                return result;
            }
            catch (Exception e)
            {
                log.Error("Failed to run remote tests {0}", ExceptionHelper.BuildMessageAndStackTrace(e));
                return CreateFailedResult(e);
            }
        }

        /// <summary>
        /// Start a run of the tests in the loaded TestPackage, returning immediately.
        /// The tests are run asynchronously and the listener interface is notified
        /// as it progresses.
        /// </summary>
        /// <param name="listener">An ITestEventHandler to receive events</param>
        /// <param name="filter">A TestFilter used to select tests</param>
        /// <returns>An AsyncTestRun that will provide the result of the test execution</returns>
        protected override AsyncTestEngineResult RunTestsAsync(ITestEventListener listener, TestFilter filter)
        {
            log.Info("Running " + TestPackage.Name + " (async)");

            try
            {
                CreateAgentAndRunner();

                return _remoteRunner.RunAsync(listener, filter);
            }
            catch (Exception e)
            {
                log.Error("Failed to run remote tests {0}", ExceptionHelper.BuildMessageAndStackTrace(e));
                var result = new AsyncTestEngineResult();
                result.SetResult(CreateFailedResult(e));
                return result;
            }
        }

        /// <summary>
        /// Cancel the ongoing test run. If no  test is running, the call is ignored.
        /// </summary>
        /// <param name="force">If true, cancel any ongoing test threads, otherwise wait for them to complete.</param>
        public override void StopRun(bool force)
        {
            if (_remoteRunner != null)
            {
                try
                {
                    _remoteRunner.StopRun(force);
                }
                catch (Exception e)
                {
                    log.Error("Failed to stop the remote run. {0}", ExceptionHelper.BuildMessageAndStackTrace(e));
                }
            }
        }

        protected override void Dispose(bool disposing)
        {
            // Disposal has to perform two actions, unloading the runner and
            // stopping the agent. Both must be tried even if one fails so
            // there can be up to two independent errors to be reported
            // through an NUnitEngineException. We do that by combining messages.
            if (!_disposed && disposing)
            {
                _disposed = true;

                Exception unloadException = null;

                try
                {
                    Unload();
                }
                catch (Exception ex)
                {
                    // Save and log the unload error
                    unloadException = ex;
                    log.Error(ExceptionHelper.BuildMessage(ex));
                    log.Error(ExceptionHelper.BuildMessageAndStackTrace(ex));
                }

                if (_agentLease != null)
                {
                    try
                    {
                        _agentLease.Dispose();
                    }
                    catch (NUnitEngineUnloadException ex) when (unloadException != null)
                    {
                        // Both kinds of errors, throw exception with combined message
                        throw new NUnitEngineUnloadException(ExceptionHelper.BuildMessage(unloadException) + Environment.NewLine + ex.Message);
                    }
                    finally
                    {
                        _agentLease = null;
                    }
                }

                if (unloadException != null) // Add message line indicating we managed to stop agent anyway
                    throw new NUnitEngineUnloadException("Agent Process was terminated successfully after error.", unloadException);
            }
        }

        private void CreateAgentAndRunner()
        {
            if (_agentLease == null)
            {
                // Increase the timeout to give time to attach a debugger
                bool debug = TestPackage.GetSetting(EnginePackageSettings.DebugAgent, false) ||
                             TestPackage.GetSetting(EnginePackageSettings.PauseBeforeRun, false);

                _agentLease = _agency.GetAgent(TestPackage, debug ? DEBUG_TIMEOUT : NORMAL_TIMEOUT);

                if (_agentLease == null)
                    throw new NUnitEngineException("Unable to acquire remote process agent");
            }

            if (_remoteRunner == null)
                _remoteRunner = _agentLease.CreateRunner(TestPackage);
        }

        TestEngineResult CreateFailedResult(Exception e)
        {
            var suite = XmlHelper.CreateTopLevelElement("test-suite");
            XmlHelper.AddAttribute(suite, "type", "Assembly");
            XmlHelper.AddAttribute(suite, "id", TestPackage.ID);
            XmlHelper.AddAttribute(suite, "name", TestPackage.Name);
            XmlHelper.AddAttribute(suite, "fullname", TestPackage.FullName);
            XmlHelper.AddAttribute(suite, "runstate", "NotRunnable");
            XmlHelper.AddAttribute(suite, "testcasecount", "1");
            XmlHelper.AddAttribute(suite, "result", "Failed");
            XmlHelper.AddAttribute(suite, "label", "Error");
            XmlHelper.AddAttribute(suite, "start-time", DateTime.UtcNow.ToString("u"));
            XmlHelper.AddAttribute(suite, "end-time", DateTime.UtcNow.ToString("u"));
            XmlHelper.AddAttribute(suite, "duration", "0.001");
            XmlHelper.AddAttribute(suite, "total", "1");
            XmlHelper.AddAttribute(suite, "passed", "0");
            XmlHelper.AddAttribute(suite, "failed", "1");
            XmlHelper.AddAttribute(suite, "inconclusive", "0");
            XmlHelper.AddAttribute(suite, "skipped", "0");
            XmlHelper.AddAttribute(suite, "asserts", "0");

            var failure = suite.AddElement("failure");
            failure.AddElementWithCDataSection("message", ExceptionHelper.BuildMessage(e));
            failure.AddElementWithCDataSection("stack-trace", ExceptionHelper.BuildMessageAndStackTrace(e));

            return new TestEngineResult(suite);
        }
    }
}
#endif
