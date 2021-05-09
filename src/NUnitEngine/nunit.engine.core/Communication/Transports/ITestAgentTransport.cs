// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using NUnit.Engine.Agents;

namespace NUnit.Engine.Communication.Transports
{
    /// <summary>
    /// The ITestAgentTransport interface is implemented by a
    /// class providing communication for a TestAgent.
    /// </summary>
    public interface ITestAgentTransport : ITransport
    {
        TestAgent Agent { get; }
        ITestEngineRunner CreateRunner(ITestPackage package);
    }
}
