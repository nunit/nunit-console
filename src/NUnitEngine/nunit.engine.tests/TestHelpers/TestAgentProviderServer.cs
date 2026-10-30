// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.Xml.Linq;
using NUnit.Engine.Communication.Messages;
using NUnit.Engine.Services;

namespace NUnit.Engine.TestHelpers
{
    /// <summary>
    /// Hosts a provider on loopback for contract/integration tests. It neither
    /// discovers machines nor deploys packages, and must not be exposed on a network.
    /// Agent references belong to the originating client/provider until release succeeds.
    /// </summary>
    internal sealed class TestAgentProviderServer : ITestAgentProvider, IDisposable
    {
        private readonly ITestAgentProvider _provider;
        private readonly object _providerLock = new object();
        private readonly TcpListener _listener = new TcpListener(IPAddress.Loopback, 0);
        private readonly ConcurrentDictionary<Guid, Lease> _agents = new ConcurrentDictionary<Guid, Lease>();
        private readonly ConcurrentDictionary<Socket, Task> _requests = new ConcurrentDictionary<Socket, Task>();
        private Task? _acceptLoop;
        private volatile bool _stopping;
        private readonly bool _dropAcquisitionResponse;

        internal TestAgentProviderServer(ITestAgentProvider provider, bool dropAcquisitionResponse = false)
        {
            _provider = provider;
            _dropAcquisitionResponse = dropAcquisitionResponse;
        }

        internal IPEndPoint EndPoint => (IPEndPoint)_listener.LocalEndpoint;

        internal void Start()
        {
            _listener.Start();
            _acceptLoop = Task.Run(AcceptConnections);
        }

        public IList<TestAgentInfo> AvailableAgents
        {
            get
            {
                lock (_providerLock)
                    return _provider.AvailableAgents;
            }
        }

        public IList<TestAgentInfo> GetAgentsForPackage(TestPackage package)
        {
            lock (_providerLock)
                return _provider.GetAgentsForPackage(package);
        }

        public bool IsAgentAvailable(TestPackage package)
        {
            lock (_providerLock)
                return _provider.IsAgentAvailable(package);
        }

        public ITestAgent GetAgent(TestPackage package)
        {
            lock (_providerLock)
                return _provider.GetAgent(package);
        }

        public void ReturnAgent(ITestAgent agent)
        {
            lock (_providerLock)
                _provider.ReturnAgent(agent);
        }

        private void AcceptConnections()
        {
            while (!_stopping)
            {
                Socket socket;
                try
                {
                    socket = _listener.AcceptSocket();
                }
                catch (SocketException) when (_stopping)
                {
                    return;
                }
                catch (ObjectDisposedException) when (_stopping)
                {
                    return;
                }

                // Register before starting so disposal cannot miss an accepted request.
                var start = new TaskCompletionSource<bool>();
                var task = Task.Run(async () =>
                {
                    await start.Task;
                    try
                    {
                        Handle(socket);
                    }
                    finally
                    {
                        _requests.TryRemove(socket, out _);
                    }
                });
                _requests.TryAdd(socket, task);
                start.SetResult(true);
            }
        }

        private void Handle(Socket socket)
        {
            using var connection = new ProviderConnection(socket);
            try
            {
                var message = connection.Receive();
                var request = XElement.Parse(message.Data!);
                Guid owner = Guid.Parse((string)request.Attribute("owner")!);
                Guid id = Guid.Parse((string)request.Attribute("agent")!);
                string data = request.Value;
                string result;
                switch (message.Code)
                {
                    case ProviderConnection.ListAgents:
                        result = WriteAgents(AvailableAgents);
                        break;
                    case ProviderConnection.FindAgents:
                        result = WriteAgents(GetAgentsForPackage(PackageHelper.FromXml(data)));
                        break;
                    case ProviderConnection.IsAvailable:
                        result = IsAgentAvailable(PackageHelper.FromXml(data)).ToString();
                        break;
                    case ProviderConnection.ActiveAgents:
                        result = _agents.Count.ToString(CultureInfo.InvariantCulture);
                        break;
                    case ProviderConnection.GetAgent:
                        var agent = GetAgent(PackageHelper.FromXml(data));
                        if (!_agents.TryAdd(agent.Id, new Lease(owner, agent)))
                            throw new InvalidOperationException("The provider returned an agent that is already leased.");
                        // Deterministic transport fault: retain the lease but lose its reply.
                        // Only the explicitly selected controlled-provider test mode enables it.
                        if (_dropAcquisitionResponse)
                            return;
                        result = agent.Id.ToString();
                        break;
                    default:
                        if (!_agents.TryGetValue(id, out var lease) || lease.Owner != owner)
                            throw new InvalidOperationException("Unknown agent or incorrect owner.");
                        result = Dispatch(message.Code, data, lease, connection);
                        if (message.Code == ProviderConnection.ReleaseAgent)
                            _agents.TryRemove(id, out _);
                        break;
                }
                connection.Send(MessageCode.CommandResult, result);
            }
            catch (Exception exception)
            {
                try
                {
                    connection.Send(ProviderConnection.Error, ProviderConnection.Failure(exception));
                }
                catch (SocketException)
                {
                }
                catch (ObjectDisposedException)
                {
                }
            }
        }

        private string Dispatch(string code, string data, Lease lease, ProviderConnection connection)
        {
            // Stop requests must be able to overtake a synchronous Run request.
            if (code == MessageCode.RequestStopCommand || code == MessageCode.ForcedStopCommand)
            {
                if (lease.Released)
                    throw new InvalidOperationException("The agent has been released.");
                var runner = lease.Runner ?? throw new InvalidOperationException("No runner has been created.");
                if (code == MessageCode.RequestStopCommand)
                    runner.RequestStop();
                else
                    runner.ForcedStop();
                return string.Empty;
            }

            lock (lease.CommandLock)
            {
                if (lease.Released)
                    throw new InvalidOperationException("The agent has been released.");
                switch (code)
                {
                    case ProviderConnection.ReleaseAgent:
                        ReturnAgent(lease.Agent);
                        lease.Released = true;
                        return string.Empty;
                    case MessageCode.StartAgent:
                        return lease.Agent.Start().ToString();
                    case MessageCode.StopAgent:
                        lease.Agent.Stop();
                        return string.Empty;
                    case MessageCode.CreateRunner:
                        if (lease.Runner is not null)
                            throw new InvalidOperationException("A runner has already been created.");
                        lease.Runner = lease.Agent.CreateRunner(PackageHelper.FromXml(data));
                        return string.Empty;
                }

                var runner = lease.Runner ?? throw new InvalidOperationException("No runner has been created.");
                switch (code)
                {
                    case MessageCode.LoadCommand:
                        return runner.Load().Xml.OuterXml;
                    case MessageCode.ReloadCommand:
                        return runner.Reload().Xml.OuterXml;
                    case MessageCode.UnloadCommand:
                        runner.Unload();
                        return string.Empty;
                    case MessageCode.ExploreCommand:
                        return runner.Explore(new TestFilter(data)).Xml.OuterXml;
                    case MessageCode.CountCasesCommand:
                        return runner.CountTestCases(new TestFilter(data)).ToString(CultureInfo.InvariantCulture);
                    case MessageCode.RunCommand:
                        return runner.Run(new Listener(connection), new TestFilter(data)).Xml.OuterXml;
                    case ProviderConnection.DisposeRunner:
                        runner.Dispose();
                        return string.Empty;
                    default:
                        throw new InvalidOperationException("Unknown provider command: " + code);
                }
            }
        }

        private static string WriteAgents(IList<TestAgentInfo> agents)
        {
            return new XElement("agents", agents.Select(agent => new XElement("agent",
                new XAttribute("name", agent.AgentName), new XAttribute("type", (int)agent.AgentType),
                new XAttribute("runtime", agent.TargetRuntime.FullName)))).ToString(SaveOptions.DisableFormatting);
        }

        /// <summary>
        /// Stops accepting requests, requests cancellation, drains request workers and
        /// returns remaining agents to their provider. Failed cleanup is reported, not
        /// treated as confirmation that agent processes or deployments were removed.
        /// </summary>
        public void Dispose()
        {
            _stopping = true;
            _listener.Stop();
            _acceptLoop?.GetAwaiter().GetResult();
            foreach (var lease in _agents.Values)
                lease.Runner?.RequestStop();
            foreach (var request in _requests.Keys)
                request.Dispose();
            if (!Task.WaitAll(_requests.Values.ToArray(), 30000))
                throw new TimeoutException("Provider requests did not stop; agent ownership is retained.");
            foreach (var pair in _agents.ToArray())
            {
                ReturnAgent(pair.Value.Agent);
                _agents.TryRemove(pair.Key, out _);
            }
        }

        private sealed class Lease
        {
            internal Lease(Guid owner, ITestAgent agent)
            {
                Owner = owner;
                Agent = agent;
            }
            internal Guid Owner { get; }
            internal ITestAgent Agent { get; }
            internal ITestEngineRunner? Runner { get; set; }
            internal object CommandLock { get; } = new object();
            internal volatile bool Released;
        }

        private sealed class Listener : ITestEventListener
        {
            private readonly ProviderConnection _connection;
            internal Listener(ProviderConnection connection)
            {
                _connection = connection;
            }
            public void OnTestEvent(string report) => _connection.Send(MessageCode.ProgressReport, report);
        }
    }
}
