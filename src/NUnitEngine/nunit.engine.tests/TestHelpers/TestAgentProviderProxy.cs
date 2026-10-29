// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Versioning;
using System.Threading.Tasks;
using System.Xml.Linq;
using NUnit.Engine.Communication.Messages;
using NUnit.Engine.Services;

namespace NUnit.Engine.TestHelpers
{
    /// <summary>
    /// Exercises the current provider contract across a real loopback connection.
    /// Kept in the test assembly until the production remote agency/API is agreed.
    /// Each request has its own connection so cancellation can overtake a running test.
    /// </summary>
    internal sealed class TestAgentProviderProxy : ITestAgentProvider
    {
        private readonly IPEndPoint _endpoint;
        private readonly Guid _owner = Guid.NewGuid();

        internal TestAgentProviderProxy(IPEndPoint endpoint)
        {
            if (!IPAddress.IsLoopback(endpoint.Address))
                throw new ArgumentException("The test provider only supports loopback endpoints.", nameof(endpoint));
            _endpoint = endpoint;
        }

        public IList<TestAgentInfo> AvailableAgents => ReadAgents(Call(ProviderConnection.ListAgents));

        public IList<TestAgentInfo> GetAgentsForPackage(TestPackage package) =>
            ReadAgents(Call(ProviderConnection.FindAgents, data: package.ToXml()));

        public bool IsAgentAvailable(TestPackage package) =>
            bool.Parse(Call(ProviderConnection.IsAvailable, data: package.ToXml()));

        /// <summary>Acquires a server-owned agent; the returned ID identifies a lease, not a local process.</summary>
        public ITestAgent GetAgent(TestPackage package) =>
            new Agent(this, Guid.Parse(Call(ProviderConnection.GetAgent, data: package.ToXml())));

        /// <summary>Confirms release through the originating server before marking the client reference released.</summary>
        public void ReturnAgent(ITestAgent agent)
        {
            if (agent is not Agent remote || !ReferenceEquals(remote.Provider, this) || remote.Released)
                throw new InvalidOperationException("The agent was not acquired from this provider or was already released.");
            Call(ProviderConnection.ReleaseAgent, agent.Id);
            remote.Released = true;
        }

        internal int ActiveAgentCount => ProviderConnection.Integer(Call(ProviderConnection.ActiveAgents));

        /// <summary>Drains progress and the final response through one reader, preserving coalesced messages.</summary>
        private string Call(string code, Guid agentId = default, string? data = null, ITestEventListener? listener = null)
        {
            using var socket = new Socket(_endpoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            socket.Connect(_endpoint);
            using var connection = new ProviderConnection(socket);
            connection.Send(code, ProviderConnection.Request(_owner, agentId, data));
            while (true)
            {
                var message = connection.Receive();
                if (message.Code == MessageCode.CommandResult)
                    return message.Data ?? string.Empty;
                if (message.Code == ProviderConnection.Error)
                    throw new NUnitEngineException("Remote provider: " + XElement.Parse(message.Data!).Value);
                if (message.Code != MessageCode.ProgressReport || listener is null)
                    throw new InvalidOperationException("Unexpected provider response: " + message.Code);
                listener.OnTestEvent(message.Data!);
            }
        }

        private static List<TestAgentInfo> ReadAgents(string xml)
        {
            return XElement.Parse(xml).Elements("agent").Select(agent => new TestAgentInfo(
                (string)agent.Attribute("name")!, (TestAgentType)(int)agent.Attribute("type")!,
                new FrameworkName((string)agent.Attribute("runtime")!))).ToList();
        }

        /// <summary>Forwards agent and runner operations without loading the test package in the client.</summary>
        private sealed class Agent : ITestAgent, ITestEngineRunner
        {
            internal Agent(TestAgentProviderProxy provider, Guid id)
            {
                Provider = provider;
                Id = id;
            }

            internal TestAgentProviderProxy Provider { get; }
            internal bool Released { get; set; }
            public Guid Id { get; }

            private string Call(string code, string? data = null, ITestEventListener? listener = null)
            {
                if (Released)
                    throw new InvalidOperationException("The agent has been released.");
                return Provider.Call(code, Id, data, listener);
            }

            public bool Start() => bool.Parse(Call(MessageCode.StartAgent));
            public void Stop() => Call(MessageCode.StopAgent);

            public ITestEngineRunner CreateRunner(TestPackage package)
            {
                Call(MessageCode.CreateRunner, package.ToXml());
                return this;
            }

            public TestEngineResult Load() => new TestEngineResult(Call(MessageCode.LoadCommand));
            public void Unload() => Call(MessageCode.UnloadCommand);
            public TestEngineResult Reload() => new TestEngineResult(Call(MessageCode.ReloadCommand));
            public int CountTestCases(TestFilter filter) => ProviderConnection.Integer(Call(MessageCode.CountCasesCommand, filter.Text));
            public TestEngineResult Explore(TestFilter filter) => new TestEngineResult(Call(MessageCode.ExploreCommand, filter.Text));
            public TestEngineResult Run(ITestEventListener listener, TestFilter filter) =>
                new TestEngineResult(Call(MessageCode.RunCommand, filter.Text, listener));

            public AsyncTestEngineResult RunAsync(ITestEventListener listener, TestFilter filter)
            {
                var result = new AsyncTestEngineResult();
                Task.Run(() =>
                {
                    try
                    {
                        result.SetResult(Run(listener, filter));
                    }
                    catch (Exception exception)
                    {
                        // AsyncTestEngineResult has no exception channel. Complete with an explicit
                        // infrastructure failure rather than leave a waiting caller blocked forever.
                        result.SetResult(new TestEngineResult(new XElement("test-suite",
                            new XAttribute("type", "Assembly"), new XAttribute("result", "Failed"),
                            new XAttribute("label", "Error"), new XElement("failure",
                                new XElement("message", exception.ToString()))).ToString()));
                    }
                });
                return result;
            }

            public void RequestStop() => Call(MessageCode.RequestStopCommand);
            public void ForcedStop() => Call(MessageCode.ForcedStopCommand);
            public void Dispose()
            {
                if (!Released)
                    Call(ProviderConnection.DisposeRunner);
            }
        }
    }
}
