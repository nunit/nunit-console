// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

#if NETFRAMEWORK
using System;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Tcp;
using System.Threading;
using NUnit.Common;
using NUnit.Engine.Agents;
using NUnit.Engine.Internal;

namespace NUnit.Engine.Communication.Transports.Remoting
{

    /// <summary>
    /// TestAgentRemotingTransport uses remoting to support
    /// a TestAgent in communicating with a TestAgency and
    /// with the runners that make use of it.
    /// </summary>
    public class TestAgentRemotingTransport : MarshalByRefObject, ITestAgentTransport, ITestAgent, ITestEngineRunner
    {
        private static readonly Logger log = InternalTrace.GetLogger(typeof(TestAgentRemotingTransport));

        private readonly string _agencyUrl;
        private ITestEngineRunner _runner;

        private TcpChannel _channel;
        private ITestAgency _agency;
        private readonly CurrentMessageCounter _currentMessageCounter = new CurrentMessageCounter();

        public TestAgentRemotingTransport(RemoteTestAgent agent, string agencyUrl)
        {
            Agent = agent;
            _agencyUrl = agencyUrl;
        }

        public TestAgent Agent { get; }

        public Guid Id => Agent.Id;

        public bool Start()
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
                log.Debug("Registered agent with TestAgency");
            }
            catch (Exception ex)
            {
                log.Error("RemoteTestAgent: Failed to register with TestAgency. {0}", ExceptionHelper.BuildMessageAndStackTrace(ex));
                return false;
            }

            return true;
        }

        public void Stop()
        {
            log.Info("Stopping");

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
                Agent.StopSignal.Set();
            });
        }

        public ITestEngineRunner CreateRunner(TestPackage package)
        {
            _runner = Agent.CreateRunner(package);
            return this;
        }

        public void Dispose()
        {
            Agent.Dispose();
            _runner.Dispose();
        }

        #region ITestEngineRunner Implementation

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

        /// <summary>
        /// Overridden to cause object to live indefinitely
        /// </summary>
        public override object InitializeLifetimeService()
        {
            return null;
        }
    }
}
#endif
