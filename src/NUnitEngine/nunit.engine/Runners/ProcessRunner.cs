// ***********************************************************************
// Copyright (c) 2011–2019 Charlie Poole, Rob Prouse
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
using NUnit.Common;
using NUnit.Engine.AgentProtocol;
using NUnit.Engine.Internal;
using NUnit.Engine.Services;

namespace NUnit.Engine.Runners
{
    /// <summary>
    /// ProcessRunner loads and runs a set of tests in a single agent process.
    /// </summary>
    public partial class ProcessRunner : AbstractTestRunner
    {
        // ProcessRunner is given a TestPackage containing a single assembly
        // multiple assemblies, a project, multiple projects or a mix. It loads
        // and runs all tests in a single remote agent process.
        //
        // If the input contains projects, which are not summarized at a lower
        // level, the ProcessRunner should create an XML node for the entire
        // project, aggregating the assembly results.

        private static readonly Logger log = InternalTrace.GetLogger(typeof(ProcessRunner));

        private AgentClient _agentClient;

        public ProcessRunner(IServiceLocator services, TestPackage package) : base(services, package)
        {
        }

        private AgentClient GetAgentClient()
        {
            if (_agentClient == null)
            {
                var startInfo = CreateAgentProcessStartInfo(TestPackage, Services.GetService<RuntimeFrameworkService>());

                _agentClient = new AgentClient(new DuplexStandardProcessStream(startInfo));

                _agentClient.Connect(TestPackage);
            }

            return _agentClient;
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
                return GetAgentClient().Explore(filter);
            }
            catch (Exception ex)
            {
                log.Error("Failed to run remote tests. {0}", ExceptionHelper.BuildMessageAndStackTrace(ex));
                return CreateFailedResult(TestPackage, ex);
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

            return GetAgentClient().Load();
        }

        /// <summary>
        /// Unload any loaded TestPackage and clear
        /// the reference to the remote runner.
        /// </summary>
        public override void UnloadPackage()
        {
            try
            {
                log.Info("Unloading " + TestPackage.Name);
                GetAgentClient().Unload();
            }
            catch (Exception ex)
            {
                log.Warning("Failed to unload the remote runner. {0}", ExceptionHelper.BuildMessageAndStackTrace(ex));
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
                return GetAgentClient().CountTestCases(filter);
            }
            catch (Exception ex)
            {
                log.Error("Failed to count remote tests. {0}", ExceptionHelper.BuildMessageAndStackTrace(ex));
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
                var result = GetAgentClient().Run(listener, filter);
                log.Info("Done running " + TestPackage.Name);
                return result;
            }
            catch (Exception ex)
            {
                log.Error("Failed to run remote tests. {0}", ExceptionHelper.BuildMessageAndStackTrace(ex));
                return CreateFailedResult(TestPackage, ex);
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
                return GetAgentClient().RunAsync(listener, filter);
            }
            catch (Exception ex)
            {
                log.Error("Failed to run remote tests. {0}", ExceptionHelper.BuildMessageAndStackTrace(ex));
                var result = new AsyncTestEngineResult();
                result.SetResult(CreateFailedResult(TestPackage, ex));
                return result;
            }
        }

        /// <summary>
        /// Cancel the ongoing test run. If no  test is running, the call is ignored.
        /// </summary>
        /// <param name="force">If true, cancel any ongoing test threads, otherwise wait for them to complete.</param>
        public override void StopRun(bool force)
        {
            if (_agentClient == null) return;

            try
            {
                GetAgentClient().StopRun(force);
            }
            catch (Exception ex)
            {
                log.Error("Failed to stop the remote run. " + ExceptionHelper.BuildMessageAndStackTrace(ex));
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (_disposed || !disposing) return;
            _disposed = true;

            // Disposal has to perform two actions, unloading the runner and
            // stopping the agent. Both must be tried even if one fails so
            // there can be up to two independent errors to be reported
            // through an NUnitEngineException. We do that by combining messages.
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

            if (_agentClient != null)
            {
                log.Debug("Stopping remote agent");
                try
                {
                    _agentClient.Dispose();
                }
                catch (Exception ex)
                {
                    var stopError = "Failed to stop the remote agent. " + ExceptionHelper.BuildMessage(ex) + Environment.NewLine + ExceptionHelper.BuildMessageAndStackTrace(ex);
                    log.Error(stopError);

                    // Stop error with no unload error, just rethrow
                    if (unloadException == null)
                        throw;

                    // Both kinds of errors, throw exception with combined message
                    throw new NUnitEngineUnloadException(ExceptionHelper.BuildMessage(unloadException) + Environment.NewLine + stopError);
                }
            }

            if (unloadException != null) // Add message line indicating we managed to stop agent anyway
                throw new NUnitEngineUnloadException("Agent Process was terminated successfully after error.", unloadException);
        }

        private static TestEngineResult CreateFailedResult(TestPackage testPackage, Exception ex)
        {
            var suite = XmlHelper.CreateTopLevelElement("test-suite");
            XmlHelper.AddAttribute(suite, "type", "Assembly");
            XmlHelper.AddAttribute(suite, "id", testPackage.ID);
            XmlHelper.AddAttribute(suite, "name", testPackage.Name);
            XmlHelper.AddAttribute(suite, "fullname", testPackage.FullName);
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
            failure.AddElementWithCDataSection("message", ExceptionHelper.BuildMessage(ex));
            failure.AddElementWithCDataSection("stack-trace", ExceptionHelper.BuildMessageAndStackTrace(ex));

            return new TestEngineResult(suite);
        }
    }
}
#endif
