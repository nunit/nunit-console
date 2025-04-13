// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using System.Net;
using System.Net.Sockets;
using System.Threading;
using NUnit.Engine.Agents;
using NUnit.Engine.Communication.Messages;
using NUnit.Engine.Communication.Protocols;

namespace NUnit.Engine.Communication.Transports.Tcp
{
    /// <summary>
    /// TestAgentRemotingTransport uses TCP to support
    /// a TestAgent in communicating with a TestAgency and
    /// with the runners that make use of it.
    /// </summary>
    public class TestAgentTcpTransport : ITestAgentTransport, ITestEventListener
    {
        private static readonly Logger log = InternalTrace.GetLogger(typeof(TestAgentTcpTransport));
        private static readonly char[] PortSeparator = [':'];

        private readonly string _agencyUrl;
        private Socket? _clientSocket;
        private ITestEngineRunner? _runner;

        public TestAgentTcpTransport(RemoteTestAgent agent, string serverUrl)
        {
            Guard.ArgumentNotNull(agent, nameof(agent));
            Agent = agent;

            Guard.ArgumentNotNullOrEmpty(serverUrl, nameof(serverUrl));
            _agencyUrl = serverUrl;

            var parts = serverUrl.Split(PortSeparator);
            Guard.ArgumentValid(parts.Length == 2, "Invalid server address specified. Must be a valid endpoint including the port number", nameof(serverUrl));
            ServerEndPoint = new IPEndPoint(IPAddress.Parse(parts[0]), int.Parse(parts[1]));
        }

        public TestAgent Agent { get; }

        public IPEndPoint ServerEndPoint { get; }

        public bool Start()
        {
            log.Info("Connecting to TestAgency at {0}", _agencyUrl);

            // Connect to the server
            _clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            _clientSocket.Connect(ServerEndPoint);

            // Immediately upon connection send the agent Id as a raw byte array
            _clientSocket.Send(Agent.Id.ToByteArray());

            // Start the loop that reads and executes commands
            Thread commandLoop = new Thread(CommandLoop);
            commandLoop.Start();

            return true;
        }

        public void Stop()
        {
            Agent.StopSignal.Set();
        }

        public ITestEngineRunner CreateRunner(TestPackage package)
        {
            return Agent.CreateRunner(package);
        }

        private void CommandLoop()
        {
            bool keepRunning = true;
            var socketReader = new SocketReader(_clientSocket.ShouldNotBeNull(), new BinarySerializationProtocol());

            while (keepRunning)
            {
                var command = socketReader.GetNextMessage<CommandMessage>();

                switch (command.CommandName)
                {
                    case "CreateRunner":
                        var package = (TestPackage)command.Arguments[0];
                        _runner?.Unload();
                        _runner = CreateRunner(package);
                        break;
                    case "Load":
                        SendResult(_runner.ShouldNotBeNull().Load());
                        break;
                    case "Reload":
                        SendResult(_runner.ShouldNotBeNull().Reload());
                        break;
                    case "Unload":
                        _runner.ShouldNotBeNull().Unload();
                        break;
                    case "Explore":
                        var filter = (TestFilter)command.Arguments[0];
                        SendResult(_runner.ShouldNotBeNull().Explore(filter));
                        break;
                    case "CountTestCases":
                        filter = (TestFilter)command.Arguments[0];
                        SendResult(_runner.ShouldNotBeNull().CountTestCases(filter));
                        break;
                    case "Run":
                        filter = (TestFilter)command.Arguments[0];
                        SendResult(_runner.ShouldNotBeNull().Run(this, filter));
                        break;

                    case "RunAsync":
                        filter = (TestFilter)command.Arguments[0];
                        _runner.ShouldNotBeNull().RunAsync(this, filter);
                        break;

                    case "Stop":
                        keepRunning = false;
                        break;
                }
            }

            Stop();
        }

        private void SendResult(object result)
        {
            var resultMessage = new CommandReturnMessage(result);
            var bytes = new BinarySerializationProtocol().Encode(resultMessage);
            _clientSocket.ShouldNotBeNull().Send(bytes);
        }

        public void OnTestEvent(string report)
        {
            var progressMessage = new ProgressMessage(report);
            var bytes = new BinarySerializationProtocol().Encode(progressMessage);
            _clientSocket.ShouldNotBeNull().Send(bytes);
        }
    }
}
