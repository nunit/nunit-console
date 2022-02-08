// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using System;
using NUnit.Common;
using NUnit.Engine.Communication.Transports;
using NUnit.Engine.Runners;

namespace NUnit.Engine.Agents
{
    /// <summary>
    /// RemoteTestAgent represents a remote agent executing in another process
    /// and communicating with NUnit by TCP. Although it is similar to a
    /// TestServer, it does not publish a Uri at which clients may connect
    /// to it. Rather, it reports back to the sponsoring TestAgency upon
    /// startup so that the agency may in turn provide it to clients for use.
    /// </summary>
    public class RemoteTestAgent : TestAgent
    {
        private IServiceLocator _services;

        /// <summary>
        /// Construct a RemoteTestAgent
        /// </summary>
        /// <remarks>
        /// The first argument is a temporary measure and will be removed
        /// once services are all moved from nunit.engine.core to nunit.engine.
        /// </remarks>
        public RemoteTestAgent(IServiceLocator services, Guid agentId)
            : base(agentId)
        {
            _services = services;
        }

        public ITestAgentTransport Transport;

        public int ProcessId => System.Diagnostics.Process.GetCurrentProcess().Id;

        public override bool Start()
        {
            Guard.OperationValid(Transport != null, "Transport must be set before calling Start().");
            return Transport.Start();
        }

        public override void Stop()
        {
            Transport.Stop();
        }

        public override ITestEngineRunner CreateRunner(TestPackage package)
        {
#if NETFRAMEWORK
            if (package.SubPackages.Count > 1)
                return new MultipleTestDomainRunner(_services, package);
            else
                return new TestDomainRunner(_services, package);
#else
            return new LocalTestRunner(_services, package);
#endif
        }
    }
}
