// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;

namespace NUnit.Engine.Services
{
    /// <summary>
    /// Provides agents from an explicitly selected server, retaining each originating client
    /// until its agent has been returned. No endpoint is contacted without configuration.
    /// </summary>
    /// <remarks>
    /// This integration candidate uses the provisional #1922 protocol over trusted loopback.
    /// A selected server definition is re-read for new queries/acquisitions; existing agents
    /// retain their original endpoint even when the file changes.
    /// </remarks>
    internal sealed class RemoteProcessAgentProvider : ITestAgentProvider
    {
        private readonly Dictionary<string, ServerEntry> _servers = new Dictionary<string, ServerEntry>(StringComparer.Ordinal);
        private readonly Dictionary<IPEndPoint, RemoteAgentProviderClient.AcquisitionState> _acquisitions =
            new Dictionary<IPEndPoint, RemoteAgentProviderClient.AcquisitionState>();
        private readonly Dictionary<ITestAgent, RemoteAgentProviderClient> _owners = new Dictionary<ITestAgent, RemoteAgentProviderClient>();

        /// <summary>Lists agents from definitions explicitly used by this provider; listing never acquires an agent.</summary>
        public IList<TestAgentInfo> AvailableAgents
        {
            get
            {
                ServerEntry[] entries;
                lock (_servers)
                    entries = _servers.Values.ToArray();
                return entries.SelectMany(entry => Describe(entry, entry.Client.AvailableAgents)).ToList();
            }
        }

        /// <summary>Queries the selected server for package compatibility and qualifies its agent identities.</summary>
        public IList<TestAgentInfo> GetAgentsForPackage(TestPackage package)
        {
            if (RemoteAgentSettings.RequestedType(package) != TestAgentType.RemoteProcess)
                return Array.Empty<TestAgentInfo>();
            var server = GetServer(package);
            return Describe(server, server.Client.GetAgentsForPackage(package));
        }

        /// <summary>
        /// Queries only an explicitly requested remote endpoint. Configuration and connection
        /// failures propagate so TestAgentService cannot silently substitute a local process.
        /// </summary>
        public bool IsAgentAvailable(TestPackage package) =>
            RemoteAgentSettings.RequestedType(package) == TestAgentType.RemoteProcess
                && GetServer(package).Client.IsAgentAvailable(package);

        /// <summary>Acquires an agent without creating a process in the caller.</summary>
        public ITestAgent GetAgent(TestPackage package)
        {
            if (RemoteAgentSettings.RequestedType(package) != TestAgentType.RemoteProcess)
                throw new InvalidOperationException("Remote execution was not selected for this package.");
            var client = GetServer(package).Client;
            var agent = client.GetAgent(package);
            lock (_owners)
                _owners.Add(agent, client);
            return agent;
        }

        /// <summary>Returns the exact reference to its original server, preserving ownership after failure.</summary>
        public void ReturnAgent(ITestAgent agent)
        {
            RemoteAgentProviderClient client;
            lock (_owners)
            {
                if (!_owners.TryGetValue(agent, out client!))
                    throw new InvalidOperationException("The agent does not belong to this provider or was already released.");
            }
            client.ReturnAgent(agent);
            lock (_owners)
                _owners.Remove(agent);
        }

        private ServerEntry GetServer(TestPackage package)
        {
            string? path = package.Settings.HasSetting(RemoteAgentSettings.ServerDefinition)
                ? package.Settings.GetSetting(RemoteAgentSettings.ServerDefinition.Name) as string : null;
            if (string.IsNullOrWhiteSpace(path))
                throw new InvalidOperationException("Remote execution requires a RemoteServerDefinition file.");
            RemoteServerDefinition definition = RemoteServerDefinition.Load(path!);
            path = Path.GetFullPath(path!);
            lock (_servers)
            {
                if (_servers.Any(server => server.Key != path && server.Value.Definition.Name == definition.Name))
                    throw new InvalidOperationException("Server names must be unique within an engine instance.");
                if (!_servers.TryGetValue(path, out var entry) || !entry.Definition.Matches(definition))
                {
                    if (!_acquisitions.TryGetValue(definition.Endpoint, out var acquisition))
                    {
                        acquisition = new RemoteAgentProviderClient.AcquisitionState();
                        _acquisitions.Add(definition.Endpoint, acquisition);
                    }
                    entry = new ServerEntry(definition, acquisition);
                    _servers[path] = entry;
                }
                return entry;
            }
        }

        private static List<TestAgentInfo> Describe(ServerEntry server, IList<TestAgentInfo> agents) =>
            agents.Select(agent => new TestAgentInfo(server.Definition.Name + "::" + agent.AgentName,
                TestAgentType.RemoteProcess, agent.TargetRuntime)).ToList();

        private sealed class ServerEntry
        {
            internal ServerEntry(RemoteServerDefinition definition, RemoteAgentProviderClient.AcquisitionState acquisition)
            {
                Definition = definition;
                Client = new RemoteAgentProviderClient(definition, acquisition);
            }
            internal RemoteServerDefinition Definition { get; }
            internal RemoteAgentProviderClient Client { get; }
        }
    }
}
