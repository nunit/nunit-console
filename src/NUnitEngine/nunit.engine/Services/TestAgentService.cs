// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

namespace NUnit.Engine.Services
{
    /// <summary>
    /// Coordinates agent providers and returns each acquired agent to its owning provider.
    /// </summary>
    internal class TestAgentService : Service, ITestAgentInfo, ITestAgentProvider, IAgentExitDiagnostics
    {
        private static readonly Logger log = InternalTrace.GetLogger(typeof(TestAgentService));

        private const string PROVIDERS_PATH = "/NUnit/Engine/AgentProviders";
        private const string REQUESTED_PROVIDER = "RequestedAgentProvider";
        private readonly List<ITestAgentProvider> _providers;
        private ExtensionService? _extensionService;
        private bool _extensionsLoaded;
        private readonly Dictionary<ITestAgent, AgentLease> _agents =
            new Dictionary<ITestAgent, AgentLease>(new AgentReferenceComparer());

        public TestAgentService() : this(Array.Empty<ITestAgentProvider>())
        {
        }

        internal TestAgentService(params ITestAgentProvider[] providers)
        {
            _providers = new List<ITestAgentProvider>(providers);
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

                foreach (var provider in Providers)
                    agents.AddRange(provider.AvailableAgents);

                return agents;
            }
        }

        /// <summary>
        /// Gets a list containing <see cref="TestAgentInfo"/> for any available agents,
        /// which are able to handle the specified package.
        /// </summary>
        /// <param name="package">A Testpackage</param>
        /// <returns>
        /// A list of suitable agents for running the package or an empty
        /// list if no agent is available for the package.
        /// </returns>
        public IList<TestAgentInfo> GetAgentsForPackage(TestPackage package)
        {
            var agents = new List<TestAgentInfo>();

            foreach (var provider in ProvidersFor(package))
                agents.AddRange(provider.GetAgentsForPackage(package));

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
            foreach (var agentSource in ProvidersFor(package))
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
        /// <exception cref="InvalidOperationException">If no compatible agent is available.</exception>
        public ITestAgent GetAgent(TestPackage package)
        {
            foreach (var agentSource in ProvidersFor(package))
                if (agentSource.IsAgentAvailable(package))
                {
                    var agent = agentSource.GetAgent(package);
                    lock (_agents)
                        _agents.Add(agent, new AgentLease(agentSource));
                    log.Debug($"Returning agent {agent.Id:B}");
                    return agent;
                }

            throw new InvalidOperationException("No available agent matches the TestPackage");
        }

        /// <summary>
        /// Releases the test agent back to the supplier, which provided it.
        /// </summary>
        /// <param name="agent">An agent previously provided by a call to GetAgent.</param>
        /// <exception cref="InvalidOperationException">
        /// If agent was never provided by the factory or was previously released.
        /// </exception>
        /// <remarks>
        /// The ownership record is removed only when the owning provider returns
        /// successfully. If the provider throws, the record remains for retry.
        /// Execution and deployment cleanup guarantees remain provider responsibilities.
        /// </remarks>
        public void ReleaseAgent(ITestAgent agent)
        {
            AgentLease lease;
            lock (_agents)
            {
                if (!_agents.TryGetValue(agent, out lease!) || lease.Releasing)
                    throw new InvalidOperationException("Agent was not acquired from this service or is already being released");

                lease.Releasing = true;
            }

            try
            {
                lease.Provider.ReleaseAgent(agent);
            }
            catch
            {
                lock (_agents)
                    lease.Releasing = false;
                throw;
            }

            lock (_agents)
                _agents.Remove(agent);
        }

        #endregion

        string? IAgentExitDiagnostics.GetAgentExitMessage(ITestAgent agent)
        {
            ITestAgentProvider provider;
            lock (_agents)
            {
                if (!_agents.TryGetValue(agent, out var lease) || lease.Releasing)
                    return null;

                provider = lease.Provider;
            }

            // The provider may wait for its agent to exit. Do not hold the ownership lock.
            return (provider as IAgentExitDiagnostics)?.GetAgentExitMessage(agent);
        }

        #region Service Overrides

        public override void StartService()
        {
            base.StartService();

            if (_providers.Count == 0)
            {
                _providers.Add(ServiceContext.GetService<TestAgency>());
                _extensionService = ServiceContext.GetService<ExtensionService>();
            }
            else
                _extensionsLoaded = true;
        }

        // ConsoleRunner installs extensions after engine service startup. Delay provider
        // discovery until agents are actually queried, as TestAgency does for launchers.
        private IList<ITestAgentProvider> Providers
        {
            get
            {
                lock (_providers)
                {
                    if (!_extensionsLoaded)
                    {
                        var extensions = _extensionService ?? throw new InvalidOperationException("Agent provider service has not started");
                        var discovered = new List<ITestAgentProvider>();
                        foreach (var node in extensions.GetExtensionNodes(PROVIDERS_PATH))
                        {
                            if (node.ExtensionObject is not ITestAgentProvider provider)
                                throw new NUnitEngineException($"Invalid agent provider extension: {node.TypeName}");
                            discovered.Add(provider);
                        }
                        _providers.InsertRange(0, discovered);
                        _extensionsLoaded = true;
                    }
                    return _providers.ToArray();
                }
            }
        }

        private IEnumerable<ITestAgentProvider> ProvidersFor(TestPackage package)
        {
            var setting = package.Settings.HasSetting(REQUESTED_PROVIDER) ? package.Settings.GetSetting(REQUESTED_PROVIDER) : null;
            string? requested = setting as string;
            if (package.Settings.HasSetting(REQUESTED_PROVIDER) && string.IsNullOrWhiteSpace(requested))
                throw new NUnitEngineException("RequestedAgentProvider must name an installed provider type");
            var providers = Providers;
            if (requested is null)
                return providers;
            var matches = providers.Where(provider => provider.GetType().FullName == requested).ToArray();
            if (matches.Length != 1)
                throw new NUnitEngineException($"Requested agent provider is unavailable or ambiguous: {requested}");
            return matches;
        }

        private sealed class AgentLease
        {
            public AgentLease(ITestAgentProvider provider)
            {
                Provider = provider;
            }

            public ITestAgentProvider Provider { get; }
            public bool Releasing { get; set; }
        }

        private sealed class AgentReferenceComparer : IEqualityComparer<ITestAgent>
        {
            public bool Equals(ITestAgent? left, ITestAgent? right) => ReferenceEquals(left, right);

            public int GetHashCode(ITestAgent agent) => RuntimeHelpers.GetHashCode(agent);
        }

        #endregion
    }
}
