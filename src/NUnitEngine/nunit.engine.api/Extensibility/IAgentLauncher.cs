// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using System;
using System.Diagnostics;

namespace NUnit.Engine.Extensibility
{
    /// <summary>
    /// Interface implemented by an agent launcher, which is able to examine
    /// a test package, evaluate whether it can create an agent for it and
    /// create the agent itself on request.
    /// </summary>
    public interface IAgentLauncher
    {
        /// <summary>
        /// Gets a TestAgentInfo describing this agent
        /// </summary>
        TestAgentInfo AgentInfo { get; }

        /// <summary>
        /// Returns true if the launcher can create an agent for the supplied package, otherwise false.
        /// </summary>
        bool CanCreateAgent(TestPackage package);

        /// <summary>
        /// Returns an agent capable of running the specified package.
        /// </summary>
        Process CreateAgent(Guid agentId, string agencyUrl, TestPackage package);
    }
}
