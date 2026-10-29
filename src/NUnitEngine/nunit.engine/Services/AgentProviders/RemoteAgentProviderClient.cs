// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Versioning;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using NUnit.Common;
using NUnit.Engine.Communication.Messages;

namespace NUnit.Engine.Services
{
    /// <summary>
    /// Forwards provider and runner operations through the provisional #1922 protocol.
    /// This implementation candidate is restricted to an explicitly trusted loopback server.
    /// Each request has its own connection so cancellation can overtake a running test.
    /// </summary>
    internal sealed class RemoteAgentProviderClient : ITestAgentProvider
    {
        private readonly IPEndPoint _endpoint;
        private readonly RemoteServerDefinition _definition;
        private readonly Guid _owner = Guid.NewGuid();
        private readonly AcquisitionState _acquisition;

        internal RemoteAgentProviderClient(RemoteServerDefinition definition, AcquisitionState acquisition)
        {
            _definition = definition;
            _endpoint = definition.Endpoint;
            _acquisition = acquisition;
        }

        /// <inheritdoc />
        public IList<TestAgentInfo> AvailableAgents => ReadAgents(Call(RemoteAgentConnection.ListAgents));

        /// <inheritdoc />
        public IList<TestAgentInfo> GetAgentsForPackage(TestPackage package) =>
            ReadAgents(Call(RemoteAgentConnection.FindAgents, data: PreparePackage(package)));

        /// <inheritdoc />
        public bool IsAgentAvailable(TestPackage package) =>
            bool.Parse(Call(RemoteAgentConnection.IsAvailable, data: PreparePackage(package)));

        /// <summary>Acquires a server-owned agent; the returned ID identifies a lease, not a local process.</summary>
        public ITestAgent GetAgent(TestPackage package)
        {
            // A lost acquisition reply may hide a server-side lease. Do not send another
            // acquisition to this endpoint until a separate recovery policy resolves it.
            // Changing a definition's name or deadlines must not reset that fence.
            lock (_acquisition)
            {
                if (_acquisition.Uncertain)
                    throw new InvalidOperationException("A previous acquisition is unresolved; recover the server before acquiring another agent.");
                string xml = PreparePackage(package);
                try
                {
                    return new Agent(this, Guid.Parse(Call(RemoteAgentConnection.GetAgent, data: xml)));
                }
                catch
                {
                    _acquisition.Uncertain = true;
                    throw;
                }
            }
        }

        /// <summary>Confirms release through the originating server before marking the client reference released.</summary>
        public void ReturnAgent(ITestAgent agent)
        {
            if (agent is not Agent remote || !ReferenceEquals(remote.Provider, this) || remote.Released)
                throw new InvalidOperationException("The agent was not acquired from this provider or was already released.");
            lock (remote)
            {
                if (remote.Released)
                    throw new InvalidOperationException("The agent was already released.");
                Call(RemoteAgentConnection.ReleaseAgent, agent.Id);
                remote.Released = true;
            }
        }

        /// <summary>Drains progress and the final response through one reader, preserving coalesced messages.</summary>
        private string Call(string code, Guid agentId = default, string? data = null, ITestEventListener? listener = null)
        {
            using var socket = new Socket(_endpoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            var connecting = Task.Factory.FromAsync(
                (callback, state) => socket.BeginConnect(_endpoint, callback, state),
                socket.EndConnect, null);
            WaitForConnection(connecting, _definition.ConnectTimeout);
            using var connection = new RemoteAgentConnection(socket,
                code == MessageCode.RunCommand ? _definition.RunReadTimeout : _definition.OperationTimeout,
                _definition.OperationTimeout);
            connection.Send(code, RemoteAgentConnection.Request(_owner, agentId, data));
            while (true)
            {
                var message = connection.Receive();
                if (message.Code == MessageCode.CommandResult)
                    return message.Data ?? string.Empty;
                if (message.Code == RemoteAgentConnection.Error)
                    throw new NUnitEngineException("Remote provider: " + XElement.Parse(message.Data!).Value);
                if (message.Code != MessageCode.ProgressReport || listener is null)
                    throw new InvalidOperationException("Unexpected provider response: " + message.Code);
                listener.OnTestEvent(message.Data!);
            }
        }

        /// <summary>
        /// Waits for connection completion without taking ownership of a task's wait handle.
        /// The caller owns the socket and disposes it when the deadline expires.
        /// </summary>
        /// <param name="connecting">The connection operation, including its EndConnect callback.</param>
        /// <param name="millisecondsTimeout">The maximum time to wait for connection completion.</param>
        /// <remarks>
        /// Observe a late fault after timeout without blocking the caller. Cancel the
        /// deadline timer on completion and preserve the original connection exception.
        /// </remarks>
        internal static void WaitForConnection(Task connecting, int millisecondsTimeout)
        {
            using var deadline = new CancellationTokenSource();
            var expired = Task.Delay(millisecondsTimeout, deadline.Token);
            if (!ReferenceEquals(Task.WhenAny(connecting, expired).GetAwaiter().GetResult(), connecting))
            {
                _ = connecting.ContinueWith(task => { _ = task.Exception; },
                    CancellationToken.None,
                    TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously,
                    TaskScheduler.Default);
                throw new TimeoutException("The provider connection deadline expired.");
            }

            deadline.Cancel();
            connecting.GetAwaiter().GetResult();
        }

        private static List<TestAgentInfo> ReadAgents(string xml)
        {
            return XElement.Parse(xml).Elements("agent").Select(agent => new TestAgentInfo(
                (string)agent.Attribute("name")!, (TestAgentType)(int)agent.Attribute("type")!,
                new FrameworkName((string)agent.Attribute("runtime")!))).ToList();
        }

        /// <summary>
        /// Removes coordinator routing settings from a cloned package. The backend chooses
        /// its own local launcher; the caller's package and server selection remain unchanged.
        /// </summary>
        private string PreparePackage(TestPackage package)
        {
            TestPackage remote = PackageHelper.FromXml(package.ToXml());
            remote.RemoveSetting(RemoteAgentSettings.ServerDefinition, true);
            remote.RemoveSetting(RemoteAgentSettings.AgentType, true);
            remote.RemoveSetting(SettingDefinitions.SelectedAgentName, true);
            PrepareNames(remote);
            return remote.ToXml();
        }

        private void PrepareNames(TestPackage package)
        {
            string requested = package.Settings.GetValueOrDefault(SettingDefinitions.RequestedAgentName);
            string prefix = _definition.Name + "::";
            if (requested.StartsWith(prefix, StringComparison.Ordinal))
                package.ApplySetting(SettingDefinitions.RequestedAgentName.WithValue(requested.Substring(prefix.Length)), false);
            else if (requested.Contains("::"))
                throw new ArgumentException("The requested agent belongs to another server.", nameof(package));
            foreach (var child in package.SubPackages)
                PrepareNames(child);
        }

        /// <summary>
        /// Shares acquisition uncertainty across definitions addressing one endpoint. This
        /// conservative fence is not recovery or a server-side idempotency guarantee.
        /// </summary>
        internal sealed class AcquisitionState
        {
            internal bool Uncertain { get; set; }
        }

        /// <summary>Forwards agent and runner operations without loading the test package in the client.</summary>
        private sealed class Agent : ITestAgent, ITestEngineRunner
        {
            internal Agent(RemoteAgentProviderClient provider, Guid id)
            {
                Provider = provider;
                Id = id;
            }

            internal RemoteAgentProviderClient Provider { get; }
            internal bool Released { get; set; }
            /// <inheritdoc />
            public Guid Id { get; }

            private string Call(string code, string? data = null, ITestEventListener? listener = null)
            {
                if (Released)
                    throw new InvalidOperationException("The agent has been released.");
                return Provider.Call(code, Id, data, listener);
            }

            /// <inheritdoc />
            public bool Start() => bool.Parse(Call(MessageCode.StartAgent));
            /// <inheritdoc />
            public void Stop() => Call(MessageCode.StopAgent);

            /// <inheritdoc />
            public ITestEngineRunner CreateRunner(TestPackage package)
            {
                Call(MessageCode.CreateRunner, Provider.PreparePackage(package));
                return this;
            }

            /// <inheritdoc />
            public TestEngineResult Load() => new TestEngineResult(Call(MessageCode.LoadCommand));
            /// <inheritdoc />
            public void Unload() => Call(MessageCode.UnloadCommand);
            /// <inheritdoc />
            public TestEngineResult Reload() => new TestEngineResult(Call(MessageCode.ReloadCommand));
            /// <inheritdoc />
            public int CountTestCases(TestFilter filter) => RemoteAgentConnection.Integer(Call(MessageCode.CountCasesCommand, filter.Text));
            /// <inheritdoc />
            public TestEngineResult Explore(TestFilter filter) => new TestEngineResult(Call(MessageCode.ExploreCommand, filter.Text));
            /// <inheritdoc />
            public TestEngineResult Run(ITestEventListener listener, TestFilter filter) =>
                new TestEngineResult(Call(MessageCode.RunCommand, filter.Text, listener));

            /// <inheritdoc />
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

            /// <inheritdoc />
            public void RequestStop() => Call(MessageCode.RequestStopCommand);
            /// <inheritdoc />
            public void ForcedStop() => Call(MessageCode.ForcedStopCommand);
            /// <inheritdoc />
            public void Dispose()
            {
                if (!Released)
                    Call(RemoteAgentConnection.DisposeRunner);
            }
        }
    }
}
