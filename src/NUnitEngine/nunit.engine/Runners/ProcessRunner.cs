// ***********************************************************************
// Copyright (c) 2011-2014 Charlie Poole
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

using System;
using System.Net.Sockets;
using NUnit.Engine.Internal;
using NUnit.Engine.Services;

namespace NUnit.Engine.Runners
{
    /// <summary>
    /// Summary description for ProcessRunner.
    /// </summary>
    public class ProcessRunner : AbstractTestRunner
    {
        private const int NORMAL_TIMEOUT = 30000;               // 30 seconds
        private const int DEBUG_TIMEOUT = NORMAL_TIMEOUT * 10;  // 5 minutes

        private static readonly Logger log = InternalTrace.GetLogger(typeof(ProcessRunner));

        private ITestAgent _agent;
        private ITestEngineRunner _remoteRunner;
        private TestAgency _agency;

        public ProcessRunner(IServiceLocator services, TestPackage package) : base(services, package) 
        {
            _agency = Services.GetService<TestAgency>();
        }

        #region Properties

        public RuntimeFramework RuntimeFramework { get; private set; }

        #endregion

        #region AbstractTestRunner Overrides

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
                log.Error("Failed to run remote tests {0}", e.Message);
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
                log.Warning("Failed to unload the remote runner. {0}", e.Message);
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
                log.Error("Failed to count remote tests {0}", e.Message);
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
                log.Error("Failed to run remote tests {0}", e.Message);
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
                log.Error("Failed to run remote tests {0}", e.Message);
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
                    log.Error("Failed to stop the remote run. {0}", e.Message);
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

                string unloadError = null;

                try
                {
                    Unload();
                }
                catch(Exception ex)
                {
                    // Save and log the unload error
                    unloadError = ex.Message;
                    log.Error(unloadError);
                }

                if (_agent != null)
                {
                    try
                    {
                        log.Debug("Pinging remote agent");

                        // Ping the agent to check that it is still alive.
                        _agent.Ping();
                    }
                    catch (Exception e)
                    {
                        string stopError = string.Format("Failed to ping the remote agent. {0}", e.Message);
                        log.Error(stopError);
                        _agent = null;

                        // Stop error with no unload error, just rethrow
                        if (unloadError == null)
                            throw;

                        // Both kinds of errors, throw exception with combined message
                        throw new NUnitEngineException(unloadError + Environment.NewLine + stopError);
                    }

                    try
                    {
                        log.Debug("Stopping remote agent");
                        _agent.Stop();
                        _agent = null;
                    }
                    catch (SocketException)
                    {
                        // Sometimes the TCP Channel Proxy to the Agent throws a 
                        // SocketException (0x80004005) when calling 'Stop', although 
                        // the Agent receives the stop signal and terminates successfully. 
                        // We assume that the Agent is still present and responding because 
                        // we called 'Ping' and that therefore the Socket Exception can be ignored.
                    }
                    catch (Exception e)
                    {
                        string stopError = string.Format("Failed to stop the remote agent. {0}", e.Message);
                        log.Error(stopError);
                        _agent = null;

                        // Stop error with no unload error, just rethrow
                        if (unloadError == null)
                            throw;

                        // Both kinds of errors, throw exception with combined message
                        throw new NUnitEngineException(unloadError + Environment.NewLine + stopError);
                    }
                }

                if (unloadError != null) // Add message line indicating we managed to stop agent anyway
                    throw (new NUnitEngineException(unloadError + "\nAgent Process was terminated successfully after error."));
            }
        }

#endregion

#region Helper Methods

        private void CreateAgentAndRunner()
        {
            if (_agent == null)
            {
                // Increase the timeout to give time to attach a debugger
                bool debug = TestPackage.GetSetting(EnginePackageSettings.DebugAgent, false) ||
                             TestPackage.GetSetting(EnginePackageSettings.PauseBeforeRun, false);

                _agent = _agency.GetAgent(TestPackage, debug ? DEBUG_TIMEOUT : NORMAL_TIMEOUT);

                if (_agent == null)
                    throw new Exception("Unable to acquire remote process agent");
            }

            if (_remoteRunner == null)
                _remoteRunner = _agent.CreateRunner(TestPackage);
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
            failure.AddElementWithCDataSection("message", e.Message);
            failure.AddElementWithCDataSection("stack-trace", e.StackTrace);

            return new TestEngineResult(suite);
        }

#endregion
    }
}
