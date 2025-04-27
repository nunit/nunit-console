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
            SendCommandMessage(MessageCode.CreateRunner, package.ToXml());

            // Agent also functions as the runner
            return this;
        }

        public bool Start()
        {
            SendCommandMessage(MessageCode.StartAgent);
            return bool.Parse(CommandResult());
        }

        public void Stop()
        {
            SendCommandMessage(MessageCode.StopAgent);
        }

        public TestEngineResult Load()
        {
            SendCommandMessage(MessageCode.LoadCommand);
            return new TestEngineResult(CommandResult());
        }

        public void Unload()
        {
            SendCommandMessage(MessageCode.UnloadCommand);
        }

        public TestEngineResult Reload()
        {
            SendCommandMessage(MessageCode.ReloadCommand);
            return new TestEngineResult(CommandResult());
        }

        public int CountTestCases(TestFilter filter)
        {
            SendCommandMessage(MessageCode.CountCasesCommand, filter.Text);
            return int.Parse(CommandResult());
        }

        public TestEngineResult Run(ITestEventListener listener, TestFilter filter)
        {
            SendCommandMessage(MessageCode.RunCommand, filter.Text);

            return TestRunResult(listener);
        }

        public AsyncTestEngineResult RunAsync(ITestEventListener listener, TestFilter filter)
        {
            SendCommandMessage(MessageCode.RunAsyncCommand, filter.Text);
            // TODO: Should we get the async result from the agent or just use our own?
            //return CommandResult<AsyncTestEngineResult>();
            return new AsyncTestEngineResult();
        }

        public void RequestStop() => SendCommandMessage(MessageCode.RequestStopCommand);

        public void ForcedStop() => SendCommandMessage(MessageCode.ForcedStopCommand);

        public TestEngineResult Explore(TestFilter filter)
        {
            SendCommandMessage(MessageCode.ExploreCommand, filter.Text);
            return new TestEngineResult(CommandResult());
        }

        public void Dispose()
        {
        }

        private void SendCommandMessage(string command, string? data = null)
        {
            _socket.Send(_wireProtocol.Encode(new TestEngineMessage(command, data)));
        }

        private string CommandResult()
        {
            return new SocketReader(_socket, _wireProtocol).GetNextMessage().Data!;
        }

        // Return the result of a test run as a TestEngineResult. ProgressMessages
        // preceding the final CommandReturnMessage are handled as well.
        private TestEngineResult TestRunResult(ITestEventListener listener)
        {
            var rdr = new SocketReader(_socket, _wireProtocol);
            while (true)
            {
                var receivedMessage = rdr.GetNextMessage();

                if (receivedMessage.Code == MessageCode.CommandResult)
                    return new TestEngineResult(receivedMessage.Data!);

                if (receivedMessage.Code == MessageCode.ProgressReport)
                    listener.OnTestEvent(receivedMessage.Data!);
                else
                    throw new InvalidOperationException($"Expected either a ProgressMessage or a CommandReturnMessage but received a {receivedMessage.Code}");
            }
        }
    }
}
