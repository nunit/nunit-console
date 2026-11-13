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

namespace NUnit.Engine.Services
{
    /// <summary>
    /// The TestAgency class provides RemoteTestAgents
    /// on request and tracks their status.
    /// </summary>
    internal class TestAgentService : Service, ITestAgentInfo, ITestAgentProvider
    {
        private static readonly Logger log = InternalTrace.GetLogger(typeof(TestAgentService));

        private readonly List<ITestAgentProvider> _providers = new List<ITestAgentProvider>();

        // Native-process registration remains in AgentStore. This index records
        // which provider owns each acquired agent, including agents without a local process.
        private readonly Dictionary<Guid, AgentOwnership> _agentOwners = new Dictionary<Guid, AgentOwnership>();

        public TestAgentService()
        {
        }

        #region ITestAgentInfo Implementation

        /// <summary>
        /// Gets a list containing <see cref="TestAgentInfo"/> for all available agents.
        /// </summary>
        public IList<TestAgentInfo> AvailableAgents
        {
            get
            {
                var agents = new List<TestAgentInfo>();

                foreach (var provider in _providers)
                    agents.AddRange(provider.AvailableAgents);

                return agents;
            }
        }

        /// <summary>
        /// Gets a list containing <see cref="TestAgentInfo"/> for any available agents,
        /// which are able to handle the specified package.
        /// </summary>
        /// <param name="package">A TestPackage</param>
        /// <returns>
        /// A list of suitable agents for running the package or an empty
        /// list if no agent is available for the package.
        /// </returns>
        public IList<TestAgentInfo> GetAgentsForPackage(TestPackage targetPackage)
        {
            var agents = new List<TestAgentInfo>();
            foreach (var agentSource in _providers)
                agents.AddRange(agentSource.GetAgentsForPackage(targetPackage));
            return agents;
        }

        #endregion

        #region ITestAgentProvider Implementation

        /// <summary>
        /// Returns true if an agent can be found, which is suitable
        /// for running the provided test package.
        /// </summary>
        /// <param name="package">A TestPackage</param>
        public bool IsAgentAvailable(TestPackage package)
        {
            foreach (var agentSource in _providers)
                if (agentSource.IsAgentAvailable(package))
                    return true;
            return false;
        }

        /// <summary>
        /// Return an agent, which best matches the criteria defined
        /// in a TestPackage.
        /// </summary>
        /// <param name="package">The test package to be run</param>
        /// <returns>An ITestAgent</returns>
        /// <exception cref="ArgumentException">If no agent is available.</exception>
        /// <remarks>Virtual to allow substitution of agents in testing.</remarks>
        public virtual ITestAgent GetAgent(TestPackage package)
        {
            // Target Runtime must be specified by this point
            string runtimeSetting = package.Settings.GetValueOrDefault(SettingDefinitions.TargetFrameworkName);
            Guard.OperationValid(runtimeSetting.Length > 0, "GetAgent called with no target runtime specified");

            foreach (var agentSource in _providers)
                if (agentSource.IsAgentAvailable(package))
                {
                    var agent = agentSource.GetAgent(package);
                    lock (_agentOwners)
                        _agentOwners.Add(agent.Id, new AgentOwnership(agent, agentSource));
                    log.Debug($"Returning agent {agent.Id:B}");
                    return agent;
                }

            throw new InvalidOperationException("No available agent matches the TestPackage");
        }

        /// <summary>
        /// Releases the test agent back to the provider. Only the provider knows what to do
        /// with the agent, which may include terminating it or keeping it alive for future use.
        /// </summary>
        /// <param name="agent">An agent previously provided by a call to GetAgent.</param>
        /// <exception cref="InvalidOperationException">
        /// If agent was never provided by the factory or was previously released.
        /// </exception>
        /// <remarks>
        /// Ownership is removed only after the provider confirms release. A failed
        /// release may be retried through the same agency. Requests for one agent
        /// are serialized without holding the ownership index lock during provider I/O.
        /// Virtual to allow substitution of agents and providers in runner tests.
        /// </remarks>
        public virtual void ReturnAgent(ITestAgent agent)
        {
            AgentOwnership ownership;
            lock (_agentOwners)
            {
                if (!_agentOwners.TryGetValue(agent.Id, out ownership!) || !ReferenceEquals(ownership.Agent, agent))
                    throw new InvalidOperationException("The agent was not acquired from this agency or was already released.");
            }

            lock (ownership)
            {
                if (ownership.Released)
                    throw new InvalidOperationException("The agent was already released.");

                ownership.Provider.ReturnAgent(agent);
                ownership.Released = true;
                lock (_agentOwners)
                    _agentOwners.Remove(agent.Id);
            }
        }

        #endregion

        #region Service Overrides

        public override void StopService()
        {
            base.StopService();
        }

        public override void StartService()
        {
            base.StartService();

            try
            {
                // Enumerate all ITestagentProvider services, excluding this instance.
                foreach (var provider in ServiceContext.GetServices<ITestAgentProvider>())
                    if (provider != this)
                        _providers.Add(provider);
            }
            catch
            {
                Status = ServiceStatus.Error;
                throw;
            }
        }

        #endregion

        internal bool IsAgentActive(Guid agentId)
        {
            lock (_agentOwners)
                return _agentOwners.ContainsKey(agentId);
        }

        /// <summary>
        /// Retains the exact acquired reference and its originating provider until release succeeds.
        /// A remote reference is active ownership, not proof that its server remains reachable.
        /// </summary>
        private sealed class AgentOwnership
        {
            internal AgentOwnership(ITestAgent agent, ITestAgentProvider provider)
            {
                Agent = agent;
                Provider = provider;
            }

            internal ITestAgent Agent { get; }
            internal ITestAgentProvider Provider { get; }
            internal bool Released { get; set; }
        }

        internal string? GetAgentExitMessage(Guid agentId)
        {
            if (_agentOwners.TryGetValue(agentId, out var ownership))
                if (ownership.Provider is LocalProcessAgentProvider localProvider)
                    return localProvider.GetAgentExitMessage(agentId);

            return null;
        }
    }
}
