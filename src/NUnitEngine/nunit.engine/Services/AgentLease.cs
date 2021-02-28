// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using System;

namespace NUnit.Engine.Services
{
    /// <summary>
    /// Disposing releases the agent back to the agency, allowing the agency it to shut it down or pool it.
    /// </summary>
    public interface IAgentLease : IDisposable
    {
        /// <summary>
        /// Creates a test runner on the acquired agent.
        /// </summary>
        ITestEngineRunner CreateRunner(TestPackage package);
    }
}
