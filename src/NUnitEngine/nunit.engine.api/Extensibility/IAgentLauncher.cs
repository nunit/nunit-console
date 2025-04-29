// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using System;
using System.Diagnostics;
using NUnit.Extensibility;

namespace NUnit.Engine.Extensibility
{
    /// <summary>
    /// Interface implemented by an agent launcher, which is able to examine
    /// a test package, evaluate whether it can create an agent for it and
    /// create the agent itself on request.
    /// </summary>
    [TypeExtensionPoint(
        Description = "Launches an Agent Process for supported target runtimes")]
    public interface IAgentLauncher
    {
        /// <summary>
        /// Gets a TestAgentInfo describing this agent
        /// </summary>
        TestAgentInfo AgentInfo { get; }

        /// <summary>
        /// Returns true if the launcher can create an agent for the supplied package, otherwise false.
        /// </summary>
        /// <param name="package"></param>
        /// <returns></returns>
        bool CanCreateAgent(TestPackage package);

        /// <summary>
        /// Returns an agent capable of running the specified package.
        /// </summary>
        /// <param name="agentId"></param>
        /// <param name="agencyUrl"></param>
        /// <param name="package"></param>
        /// <returns></returns>
        Process CreateAgent(Guid agentId, string agencyUrl, TestPackage package);
    }
}
