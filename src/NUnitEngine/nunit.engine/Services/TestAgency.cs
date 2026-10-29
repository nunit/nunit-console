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
    internal class TestAgency : Service, ITestAgentInfo, ITestAgentProvider, ITestAgency
    {
        private static readonly Logger log = InternalTrace.GetLogger(typeof(TestAgency));

        private readonly List<ITestAgentProvider> _providers = new List<ITestAgentProvider>();

        // Native-process registration remains in AgentStore. This index records
        // which provider owns each acquired agent, including agents without a local process.
        private readonly Dictionary<Guid, AgentOwnership> _agentOwners = new Dictionary<Guid, AgentOwnership>();

        internal readonly AgentStore _agentStore = new AgentStore();

        internal ExtensionService? _extensionService;

        // Transports used for various target runtimes
        private TestAgencyTcpTransport _tcpTransport; // .NET Standard 2.0

        internal virtual string TcpEndPoint => _tcpTransport.ServerUrl;

        public TestAgency() : this("TestAgency", 0)
        {
        }

        public TestAgency(string uri, int port)
        {
            _tcpTransport = new TestAgencyTcpTransport(this, port);
        }

        /// <summary>
        /// Initializes an agency with explicitly supplied agent providers.
        /// The default providers are installed on service startup only when none were supplied.
        /// </summary>
        /// <param name="providers">The providers to consult, in selection order.</param>
        internal TestAgency(IEnumerable<ITestAgentProvider> providers) : this()
        {
            _providers.AddRange(providers);
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
        public virtual void ReleaseAgent(ITestAgent agent)
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

                ownership.Provider.ReleaseAgent(agent);
                ownership.Released = true;
                lock (_agentOwners)
                    _agentOwners.Remove(agent.Id);
            }
        }

        #endregion

        #region ITestAgency Implementation

        public void Register(ITestAgent agent)
        {
            log.Debug($"Registered agent {agent.Id:B}");
            _agentStore.Register(agent);
        }

        #endregion

        #region Service Overrides

        public override void StopService()
        {
            _tcpTransport.Stop();
            base.StopService();
        }

        public override void StartService()
        {
            base.StartService();

            try
            {
                _extensionService = ServiceContext.GetService<ExtensionService>().ShouldNotBeNull();
                if (_providers.Count == 0)
                {
                    _providers.Add(new LocalProcessAgentProvider(this));
                    _providers.Add(new RemoteProcessAgentProvider(this));
                    _providers.Add(new InProcessAgentProvider(this));
                }

                _tcpTransport.Start();

                base.StartService();
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
            // A known native exit must not be hidden by the outstanding ownership record.
            if (_agentStore.TryGetAgentProcess(agentId, out _))
                return _agentStore.IsAgentActive(agentId);

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
            if (!_agentStore.TryGetAgentProcess(agentId, out var process))
                return null;

            try
            {
                // A broken connection may be observed just before the native process exits.
                // Bound the diagnostic wait and do not hold the store lock while waiting.
                if (!process.WaitForExit(1000))
                    return null;

                int exitCode = process.ExitCode;
                string message = $"Remote test agent exited with exit code {exitCode}.";
                return exitCode == AgentExitCodes.OK ? message : message + " " + GetAgentExitMessage(exitCode);
            }
            catch (InvalidOperationException)
            {
                // A missing native association must not replace the original run failure.
                return null;
            }
        }

        internal static string GetAgentExitMessage(int exitCode)
        {
            switch (exitCode)
            {
                case AgentExitCodes.OK:
                    return "Remote test agent exited before registering.";
                case AgentExitCodes.PARENT_PROCESS_TERMINATED:
                    return "Remote test agent believes agency process has exited.";
                case AgentExitCodes.UNEXPECTED_EXCEPTION:
                    return "Unhandled exception on remote test agent. " +
                           "To debug, try using --trace=debug to output logs.";
                case AgentExitCodes.FAILED_TO_START_REMOTE_AGENT:
                    return "Failed to start remote test agent.";
                case AgentExitCodes.DEBUGGER_SECURITY_VIOLATION:
                    return "Debugger could not be started on remote agent due to System.Security.Permissions.UIPermission not being set.";
                case AgentExitCodes.DEBUGGER_NOT_IMPLEMENTED:
                    return "Debugger could not be started on remote agent as not available on platform.";
                case AgentExitCodes.UNABLE_TO_LOCATE_AGENCY:
                    return "Remote test agent unable to locate agency process.";
                default:
                    return $"Remote test agent exited with non-zero exit code {exitCode}";
            }
        }
    }
}
