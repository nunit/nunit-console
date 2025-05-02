// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using NUnit.Engine.Communication.Messages;
using NUnit.Engine.Communication.Protocols;
using NUnit.Engine.Runners;

namespace NUnit.Engine.Communication.Transports.Tcp
{
    /// <summary>
    /// TestAgentRemotingTransport uses TCP to support
    /// a TestAgent in communicating with a TestAgency and
    /// with the runners that make use of it.
    /// </summary>
    public class TestAgentTcpTransport : ITestEventListener
    {
        internal readonly ManualResetEvent StopSignal = new ManualResetEvent(false);

        private static readonly Logger log = InternalTrace.GetLogger(typeof(TestAgentTcpTransport));
        private readonly Guid id;
        private readonly string _agencyUrl;
        private Socket? _clientSocket;
        private ITestEngineRunner? _runner;

        public TestAgentTcpTransport(Guid id, string serverUrl)
        {
            Guard.ArgumentNotNullOrEmpty(serverUrl, nameof(serverUrl));
            this.id = id;
            _agencyUrl = serverUrl;

            var parts = serverUrl.Split(new char[] { ':' });
            Guard.ArgumentValid(parts.Length == 2, "Invalid server address specified. Must be a valid endpoint including the port number", nameof(serverUrl));
            ServerEndPoint = new IPEndPoint(IPAddress.Parse(parts[0]), int.Parse(parts[1]));
        }

        public IPEndPoint ServerEndPoint { get; }

        public bool Start()
        {
            log.Info("Connecting to TestAgency at {0}", _agencyUrl);

            // Connect to the server
            _clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            _clientSocket.Connect(ServerEndPoint);

            // Immediately upon connection send the agent Id as a raw byte array
            _clientSocket.Send(id.ToByteArray());

            // Start the loop that reads and executes commands
            Thread commandLoop = new Thread(CommandLoop);
            commandLoop.Start();

            return true;
        }

        public void Stop()
        {
            StopSignal.Set();
        }

        public bool WaitForStop(int timeout)
        {
            return StopSignal.WaitOne(timeout);
        }

        ITestEngineRunner CreateRunner(TestPackage package)
        {
#if NETFRAMEWORK
            return new TestDomainRunner(package);
#else
            return new LocalTestRunner(package);
#endif
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
