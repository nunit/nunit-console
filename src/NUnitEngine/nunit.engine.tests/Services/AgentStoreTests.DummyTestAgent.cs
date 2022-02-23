// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using System;

namespace NUnit.Engine.Services
{
    public partial class AgentStoreTests
    {
        private sealed class DummyTestAgent : ITestAgent
        {
            public DummyTestAgent(Guid id)
            {
                Id = id;
            }

            public Guid Id { get; }

            public ITestEngineRunner CreateRunner(TestPackage package)
            {
                throw new NotImplementedException();
            }

            public bool Start()
            {
                throw new NotImplementedException();
            }

            public void Stop()
            {
                throw new NotImplementedException();
            }
        }
    }
}
