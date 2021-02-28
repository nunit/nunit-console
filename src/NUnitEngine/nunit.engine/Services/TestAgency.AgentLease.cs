// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

#if NETFRAMEWORK
using System;

namespace NUnit.Engine.Services
{
    public partial class TestAgency
    {
        private sealed class AgentLease : IAgentLease
        {
            private readonly TestAgency _agency;
            private readonly ITestAgent _remoteAgent;

            public AgentLease(TestAgency agency, Guid id, ITestAgent remoteAgent)
            {
                _agency = agency;
                Id = id;
                _remoteAgent = remoteAgent;
            }

            public Guid Id { get; }

            public ITestEngineRunner CreateRunner(TestPackage package)
            {
                return _remoteAgent.CreateRunner(package);
            }

            public void Dispose()
            {
                _agency.Release(Id, _remoteAgent);
            }
        }
    }
}
#endif