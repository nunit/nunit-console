// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using System;
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
        /// <summary>
        /// Construct a RemoteTestAgent
        /// </summary>
        /// <remarks>
        /// The first argument is a temporary measure and will be removed
        /// once services are all moved from nunit.engine.core to nunit.engine.
        /// </remarks>
        public RemoteTestAgent(Guid agentId) : base(agentId)
        {
        }

        public ITestAgentTransport? Transport;

        public override bool Start()
        {
            Guard.OperationValid(Transport is not null, "Transport must be set before calling Start().");
            return Transport.Start();
        }

        public override void Stop()
        {
            Guard.OperationValid(Transport is not null, "Transport must be set before calling Stop().");
            Transport.Stop();
        }

        public override ITestEngineRunner CreateRunner(TestPackage package)
        {
#if NETFRAMEWORK
            return new TestDomainRunner(package);
#else
            return new LocalTestRunner(package);
#endif
        }
    }
}
