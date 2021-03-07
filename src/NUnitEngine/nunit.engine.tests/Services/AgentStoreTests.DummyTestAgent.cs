// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

#if !NETCOREAPP2_0
using System;

namespace NUnit.Engine.Services.Tests
{
    public partial class AgentStoreTests
    {
        private sealed class DummyTestAgent : ITestAgent
        {
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
#endif
