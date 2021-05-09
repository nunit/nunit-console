// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using System;
using System.Threading;

namespace NUnit.Engine.Agents
{
    /// <summary>
    /// Abstract base for all types of TestAgents.
    /// A TestAgent provides services of locating,
    /// loading and running tests in a particular
    /// context such as an application domain or process.
    /// </summary>
    public abstract class TestAgent : ITestAgent, IDisposable
    {
        internal readonly ManualResetEvent StopSignal = new ManualResetEvent(false);

        /// <summary>
        /// Initializes a new instance of the <see cref="TestAgent"/> class.
        /// </summary>
        /// <param name="agentId">The identifier of the agent.</param>
        /// <param name="services">The services available to the agent.</param>
        public TestAgent(Guid agentId, IServiceLocator services)
        {
            Id = agentId;
            Services = services;
        }

        /// <summary>
        /// The services available to the agent
        /// </summary>
        protected IServiceLocator Services { get; }

        /// <summary>
        /// Gets a Guid that uniquely identifies this agent.
        /// </summary>
        public Guid Id { get; }

        /// <summary>
        /// Starts the agent, performing any required initialization
        /// </summary>
        /// <returns><c>true</c> if the agent was started successfully.</returns>
        public abstract bool Start();

        /// <summary>
        /// Stops the agent, releasing any resources
        /// </summary>
        public abstract void Stop();

        /// <summary>
        ///  Creates a test runner
        /// </summary>
        public abstract ITestEngineRunner CreateRunner(ITestPackage package);

        public bool WaitForStop(int timeout)
        {
            return StopSignal.WaitOne(timeout);
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);
            Dispose(true);
        }

        private bool _disposed = false;

        /// <summary>
        /// Dispose is overridden to stop the agent
        /// </summary>
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                    Stop();

                _disposed = true;
            }
        }
    }
}
