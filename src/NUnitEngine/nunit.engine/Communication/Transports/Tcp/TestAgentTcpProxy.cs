// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using System;
using System.Net.Sockets;
using NUnit.Engine.Communication.Messages;
using NUnit.Engine.Communication.Protocols;

namespace NUnit.Engine.Communication.Transports.Tcp
{
    /// <summary>
    /// TestAgentTcpProxy wraps a RemoteTestAgent so that certain
    /// of its properties may be accessed directly.
    /// </summary>
    internal class TestAgentTcpProxy : ITestAgent, ITestEngineRunner
    {
        private Socket _socket;
        private BinarySerializationProtocol _wireProtocol = new BinarySerializationProtocol();

        public TestAgentTcpProxy(Socket socket, Guid id)
        {
           _socket = socket;
            Id = id;
        }

        public Guid Id { get; }

        public ITestEngineRunner CreateRunner(TestPackage package)
        {
            SendCommandMessage("CreateRunner", package);

            // Agent also functions as the runner
            return this;
        }

        public bool Start()
        {
            SendCommandMessage("Start");
            return CommandResult<bool>();
        }

        public void Stop()
        {
            SendCommandMessage("Stop");
        }

        public TestEngineResult Load()
        {
            SendCommandMessage("Load");
            return CommandResult<TestEngineResult>();
        }

        public void Unload()
        {
            SendCommandMessage("Unload");
        }

        public TestEngineResult Reload()
        {
            SendCommandMessage("Reload");
            return CommandResult<TestEngineResult>();
        }

        public int CountTestCases(TestFilter filter)
        {
            SendCommandMessage("CountTestCases", filter);
            return CommandResult<int>();
        }

        public TestEngineResult Run(ITestEventListener listener, TestFilter filter)
        {
            SendCommandMessage("Run", filter);

            return TestRunResult(listener);
        }

        public AsyncTestEngineResult RunAsync(ITestEventListener listener, TestFilter filter)
        {
            SendCommandMessage("RunAsync", filter);
            // TODO: Should we get the async result from the agent or just use our own?
            return CommandResult<AsyncTestEngineResult>();
            //return new AsyncTestEngineResult();
        }

        public void RequestStop() => SendCommandMessage(MessageCode.RequestStopCommand);

        public void ForcedStop() => SendCommandMessage(MessageCode.ForcedStopCommand);

        public TestEngineResult Explore(TestFilter filter)
        {
            SendCommandMessage("Explore", filter);
            return CommandResult<TestEngineResult>();
        }

        public void Dispose()
        {
        }

        private void SendCommandMessage(string command, params object[] arguments)
        {
            _socket.Send(_wireProtocol.Encode(new CommandMessage(command, arguments)));
        }

        private T CommandResult<T>()
        {
            return (T)new SocketReader(_socket, _wireProtocol).GetNextMessage<CommandReturnMessage>().ReturnValue;
        }

        // Return the result of a test run as a TestEngineResult. ProgressMessages
        // preceding the final CommandReturnMessage are handled as well.
        private TestEngineResult TestRunResult(ITestEventListener listener)
        {
            var rdr = new SocketReader(_socket, _wireProtocol);
            while (true)
            {
                var receivedMessage = rdr.GetNextMessage();
                var receivedType = receivedMessage.GetType();

                var returnMessage = receivedMessage as CommandReturnMessage;
                if (returnMessage != null)
                    return (TestEngineResult)returnMessage.ReturnValue;

                var progressMessage = receivedMessage as ProgressMessage;
                if (progressMessage == null)
                    throw new InvalidOperationException($"Expected either a ProgressMessage or a CommandReturnMessage but received a {receivedType}");

                listener.OnTestEvent(progressMessage.Report);
            }
        }
    }
}
