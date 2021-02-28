// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

#if !NETSTANDARD2_0
using System;

namespace NUnit.Engine.Agents
{
    /// <summary>
    /// Abstract base for all types of TestAgents.
    /// A TestAgent provides services of locating,
    /// loading and running tests in a particular
    /// context such as an application domain or process.
    /// </summary>
    public abstract class TestAgent : MarshalByRefObject, ITestAgent, IDisposable
    {
        private readonly IServiceLocator services;

        /// <summary>
        /// Initializes a new instance of the <see cref="TestAgent"/> class.
        /// </summary>
        /// <param name="agentId">The identifier of the agent.</param>
        /// <param name="services">The services available to the agent.</param>
        public TestAgent(IServiceLocator services)
        {
            this.services = services;
        }

        /// <summary>
        /// The services available to the agent
        /// </summary>
        protected IServiceLocator Services
        {
            get { return services; }
        }

        /// <summary>
        /// Stops the agent, releasing any resources
        /// </summary>
        public abstract void Stop();

        /// <summary>
        ///  Creates a test runner
        /// </summary>
        public abstract ITestEngineRunner CreateRunner(TestPackage package);

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
