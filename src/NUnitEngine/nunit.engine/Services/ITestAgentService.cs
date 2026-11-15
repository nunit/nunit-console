// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using System;
using System.Threading;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Runtime.Versioning;
using NUnit.Common;
using NUnit.Engine.Communication.Transports.Tcp;
using NUnit.Engine.Extensibility;
using NUnit.Extensibility;
using System.Runtime.InteropServices;

namespace NUnit.Engine.Services
{
    // TODO: Move this to the engine API so it's available to extension creators.
    /// <summary>
    /// Provides operations for discovering available test agents, selecting an agent for a test package, and returning
    /// agents to the provider after use.
    /// </summary>
    /// <remarks>
    /// The interface duplicates the calls of <see cref="ITestAgentInfo"/> and <see cref="ITestAgentProvider"/>
    /// but avoids actually using those interfaces. This is so that TestAgentService does not appear
    /// to be one of its own providers. The interface is primarily defined for use in tests. Internal
    /// code will generally use the class name to retrieve this service from the service context.
    /// </remarks>
    public interface ITestAgentService
    {
        /// <summary>
        /// Gets a list containing <see cref="TestAgentInfo"/> for all available agents.
        /// </summary>
        public IList<TestAgentInfo> AvailableAgents { get; }

        /// <summary>
        /// Gets a list containing <see cref="TestAgentInfo"/> for any available agents,
        /// which are able to handle the specified package.
        /// </summary>
        /// <param name="package">A TestPackage</param>
        /// <returns>
        /// A list of suitable agents for running the package or an empty
        /// list if no agent is available for the package.
        /// </returns>
        public IList<TestAgentInfo> GetAgentsForPackage(TestPackage targetPackage);

        /// <summary>
        /// Returns true if an agent can be found, which is suitable
        /// for running the provided test package.
        /// </summary>
        /// <param name="package">A TestPackage</param>
        bool IsAgentAvailable(TestPackage package);

        /// <summary>
        /// Return an agent, which best matches the criteria defined
        /// in a TestPackage.
        /// </summary>
        /// <param name="package">The test package to be run</param>
        /// <returns>An ITestAgent</returns>
        ITestAgent GetAgent(TestPackage package);

        /// <summary>
        /// Releases the test agent back to the provider. Only the provider knows what to do
        /// with the agent, which may include terminating it or keeping it alive for future use.
        /// </summary>
        /// <param name="agent">An agent previously provided by a call to GetAgent.</param>
        void ReturnAgent(ITestAgent agent);

        /// <summary>
        /// Returns true if the specified agent is currently active,
        /// i.e. owned by one of the providers.
        /// </summary>
        /// <param name="agentId">The ID of the agent to check.</param>
        /// <returns>True if the agent is active; otherwise, false.</returns>
        bool IsAgentActive(Guid agentId);
    }
}
