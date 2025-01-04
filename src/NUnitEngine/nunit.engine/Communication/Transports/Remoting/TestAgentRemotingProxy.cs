// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using System;
using NUnit.Engine;

namespace NUnit.Engine.Communication.Transports.Remoting
{
    /// <summary>
    /// RemoteTestAgentProxy wraps a RemoteTestAgent so that certain
    /// of its properties may be accessed without remoting.
    /// </summary>
    internal class TestAgentRemotingProxy : ITestAgent
    {
        private ITestAgent _remoteAgent;

        public TestAgentRemotingProxy(ITestAgent remoteAgent, Guid id)
        {
            _remoteAgent = remoteAgent;

            Id = id;
        }

        public Guid Id { get; private set; }

        public ITestEngineRunner CreateRunner(TestPackage package)
        {
            return _remoteAgent.CreateRunner(package);
        }

        public bool Start()
        {
            return _remoteAgent.Start();
        }

        public void Stop()
        {
            _remoteAgent.Stop();
        }
    }
}
