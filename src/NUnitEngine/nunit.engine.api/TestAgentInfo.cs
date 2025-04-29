// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using System.Runtime.Versioning;

namespace NUnit.Engine
{
    /// <summary>
    /// The TestAgentInfo struct provides information about an
    /// available agent for use by a runner.
    /// </summary>
    public struct TestAgentInfo
    {
        /// <summary>
        /// The name of this agent
        /// </summary>
        public string AgentName;

        /// <summary>
        /// The agent type: InProcess, LocalProcess or RemoteProcess
        /// </summary>
        public TestAgentType AgentType;

        /// <summary>
        /// The target runtime used by this agent
        /// </summary>
        public FrameworkName TargetRuntime;

        /// <summary>
        /// Construct a TestAgent Info
        /// </summary>
        /// <param name="agentName">The agent name</param>
        /// <param name="agentType">The AgentType</param>
        /// <param name="targetRuntime">The target runtime</param>
        public TestAgentInfo(string agentName, TestAgentType agentType, FrameworkName targetRuntime)
        {
            AgentName = agentName;
            AgentType = agentType;
            TargetRuntime = targetRuntime;
        }
    }
}
