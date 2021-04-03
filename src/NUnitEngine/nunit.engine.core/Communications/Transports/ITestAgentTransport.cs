// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using NUnit.Engine.Agents;

namespace NUnit.Engine.Communication.Transports
{
    public interface ITestAgentTransport : ITransport
    {
        TestAgent Agent { get; }
        ITestEngineRunner CreateRunner(TestPackage package);
    }
}
