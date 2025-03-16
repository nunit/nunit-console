// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using System.Net;
using System.Net.Sockets;
using System.Threading;
using NUnit.Common;
using NUnit.Engine.Agents;
using NUnit.Engine.Internal;
using NUnit.Engine.Communication.Messages;
using NUnit.Engine.Communication.Protocols;
using System.Xml.Serialization;
using System.IO;

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

        private string _agencyUrl;
        private Socket _clientSocket;
        private ITestEngineRunner _runner;
        private XmlSerializer _testPackageSerializer = new XmlSerializer(typeof(TestPackage));

        public TestAgentTcpTransport(RemoteTestAgent agent, string serverUrl)
        {
            Guard.ArgumentNotNull(agent, nameof(agent));
            Agent = agent;

            Guard.ArgumentNotNullOrEmpty(serverUrl, nameof(serverUrl));
            _agencyUrl = serverUrl;

            var parts = serverUrl.Split(new char[] { ':' });
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
            var socketReader = new SocketReader(_clientSocket, new BinarySerializationProtocol());

            while (keepRunning)
            {
                var command = socketReader.GetNextMessage();

                switch (command.Code)
                {
                    case MessageCode.CreateRunner:
                        var package = new TestPackage().FromXml(command.Data);
                        _runner = CreateRunner(package);
                        break;
                    case MessageCode.LoadCommand:
                        SendResult(_runner.Load().Xml.OuterXml);
                        break;
                    case MessageCode.ReloadCommand:
                        SendResult(_runner.Reload().Xml.OuterXml);
                        break;
                    case MessageCode.UnloadCommand:
                        _runner.Unload();
                        break;
                    case MessageCode.ExploreCommand:
                        var filter = new TestFilter(command.Data);
                        SendResult(_runner.Explore(filter).Xml.OuterXml);
                        break;
                    case MessageCode.CountCasesCommand:
                        filter = new TestFilter(command.Data);
                        SendResult(_runner.CountTestCases(filter).ToString());
                        break;
                    case MessageCode.RunCommand:
                        filter = new TestFilter(command.Data);
                        SendResult(_runner.Run(this, filter).Xml.OuterXml);
                        break;

                    case MessageCode.RunAsyncCommand:
                        filter = new TestFilter(command.Data);
                        _runner.RunAsync(this, filter);
                        break;

                    case MessageCode.StopAgent:
                        keepRunning = false;
                        break;
                }
            }

            Stop();
        }

        private void SendResult(string result)
        {
            var resultMessage = new TestEngineMessage(MessageCode.CommandResult, result);
            var bytes = new BinarySerializationProtocol().Encode(resultMessage);
            _clientSocket.Send(bytes);
        }

        public void OnTestEvent(string report)
        {
            var progressMessage = new TestEngineMessage(MessageCode.ProgressReport, report);
            var bytes = new BinarySerializationProtocol().Encode(progressMessage);
            _clientSocket.Send(bytes);
        }
    }
}
