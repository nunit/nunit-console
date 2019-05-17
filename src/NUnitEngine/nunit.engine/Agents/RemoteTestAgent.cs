// ***********************************************************************
// Copyright (c) 2008-2014 Charlie Poole, Rob Prouse
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
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Tcp;
using System.Threading;
using NUnit.Common;
using NUnit.Engine.Internal;

namespace NUnit.Engine.Agents
{
    /// <summary>
    /// RemoteTestAgent represents a remote agent executing in another process
    /// and communicating with NUnit by TCP. Although it is similar to a
    /// TestServer, it does not publish a Uri at which clients may connect 
    /// to it. Rather, it reports back to the sponsoring TestAgency upon 
    /// startup so that the agency may in turn provide it to clients for use.
    /// </summary>
    public class RemoteTestAgent : TestAgent, ITestEngineRunner
    {
        private static readonly Logger log = InternalTrace.GetLogger(typeof(RemoteTestAgent));

        #region Fields

        private readonly string _agencyUrl;

        private ITestEngineRunner _runner;
        private TestPackage _package;

        private readonly ManualResetEvent stopSignal = new ManualResetEvent(false);
        private readonly CurrentMessageCounter _currentMessageCounter = new CurrentMessageCounter();
        private TcpChannel _channel;
        private ITestAgency _agency;

        #endregion

        #region Constructor

        /// <summary>
        /// Construct a RemoteTestAgent
        /// </summary>
        public RemoteTestAgent(Guid agentId, string agencyUrl, IServiceLocator services)
            : base(agentId, services)
        {
            _agencyUrl = agencyUrl;
        }

        #endregion

        #region Properties

        public int ProcessId
        {
            get { return System.Diagnostics.Process.GetCurrentProcess().Id; }
        }

        #endregion

        #region Public Methods

        public override ITestEngineRunner CreateRunner(TestPackage package)
        {
            _package = package;
            _runner = Services.GetService<ITestRunnerFactory>().MakeTestRunner(_package);
            return this;
        }

        public override bool Start()
        {
            log.Info("Agent starting");

            // Open the TCP channel so we can activate an ITestAgency instance from _agencyUrl
            _channel = TcpChannelUtils.GetTcpChannel(_currentMessageCounter);

            log.Info("Connecting to TestAgency at {0}", _agencyUrl);
            try
            {
                // Direct cast, not safe cast. If the cast fails we need a clear InvalidCastException message, not a null _agency.
                _agency = (ITestAgency)Activator.GetObject(typeof(ITestAgency), _agencyUrl);
            }
            catch (Exception ex)
            {
                log.Error("Unable to connect: {0}", ExceptionHelper.BuildMessageAndStackTrace(ex));
            }

            try
            {
                _agency.Register(this);
                log.Debug("Registered with TestAgency");
            }
            catch (Exception ex)
            {
                log.Error("RemoteTestAgent: Failed to register with TestAgency. {0}", ExceptionHelper.BuildMessageAndStackTrace(ex));
                return false;
            }

            return true;
        }

        public override void Stop()
        {
            log.Info("Stopping");
            // This causes an error in the client because the agent 
            // database is not thread-safe.
            //if (agency != null)
            //    agency.ReportStatus(this.ProcessId, AgentStatus.Stopping);

            // Do this on a different thread since we need to wait until all messages are through,
            // including the message which is waiting for this method to return so it can report back.
            ThreadPool.QueueUserWorkItem(_ =>
            {
                log.Info("Waiting for messages to complete");

                // Wait till all messages are finished
                _currentMessageCounter.WaitForAllCurrentMessages();

                log.Info("Attempting shut down channel");

                // Shut down nicely
                _channel.StopListening(null);
                ChannelServices.UnregisterChannel(_channel);

                // Signal to other threads that it's okay to exit the process or start a new channel, etc.
                log.Info("Set stop signal");
                stopSignal.Set();
            });
        }

        public bool WaitForStop(int timeout)
        {
            return stopSignal.WaitOne(timeout);
        }

        #endregion

        #region ITestEngineRunner Members

        /// <summary>
        /// Explore a loaded TestPackage and return information about
        /// the tests found.
        /// </summary>
        /// <param name="filter">Criteria used to filter the search results</param>
        /// <returns>A TestEngineResult.</returns>
        public TestEngineResult Explore(TestFilter filter)
        {
            return _runner.Explore(filter);
        }

        public TestEngineResult Load()
        {
            return _runner.Load();
        }

        public void Unload()
        {
            if (_runner != null)
                _runner.Unload();
        }

        public TestEngineResult Reload()
        {
            return _runner.Reload();
        }

        /// <summary>
        /// Count the test cases that would be run under
        /// the specified filter.
        /// </summary>
        /// <param name="filter">A TestFilter</param>
        /// <returns>The count of test cases</returns>
        public int CountTestCases(TestFilter filter)
        {
            return _runner.CountTestCases(filter);
        }

        /// <summary>
        /// Run the tests in the loaded TestPackage and return a test result. The tests
        /// are run synchronously and the listener interface is notified as it progresses.
        /// </summary>
        /// <param name="listener">An ITestEventHandler to receive events</param>
        /// <param name="filter">A TestFilter used to select tests</param>
        /// <returns>A TestEngineResult giving the result of the test execution</returns>
        public TestEngineResult Run(ITestEventListener listener, TestFilter filter)
        {
            return _runner.Run(listener, filter);
        }

        /// <summary>
        /// Start a run of the tests in the loaded TestPackage. The tests are run
        /// asynchronously and the listener interface is notified as it progresses.
        /// </summary>
        /// <param name="listener">An ITestEventHandler to receive events</param>
        /// <param name="filter">A TestFilter used to select tests</param>
        /// <returns>A <see cref="AsyncTestEngineResult"/> that will provide the result of the test execution</returns>
        public AsyncTestEngineResult RunAsync(ITestEventListener listener, TestFilter filter)
        {
            return _runner.RunAsync(listener, filter);
        }

        /// <summary>
        /// Cancel the ongoing test run. If no  test is running, the call is ignored.
        /// </summary>
        /// <param name="force">If true, cancel any ongoing test threads, otherwise wait for them to complete.</param>
        public void StopRun(bool force)
        {
            if (_runner != null)
                _runner.StopRun(force);
        }

        #endregion
    }
}
#endif