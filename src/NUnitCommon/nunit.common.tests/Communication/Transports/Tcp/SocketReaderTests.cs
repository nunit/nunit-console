// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Engine.Communication.Messages;
using NUnit.Engine.Communication.Protocols;
using NUnit.Framework;

namespace NUnit.Engine.Communication.Transports.Tcp
{
    public class SocketReaderTests
    {
        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        public void ClosedConnectionThrowsAfterQueuedMessagesAreRead(int messageState)
        {
            var listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            try
            {
                using var client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                client.Connect(listener.LocalEndpoint);
                using Socket server = listener.AcceptSocket();
                var protocol = new BinarySerializationProtocol();
                var reader = new SocketReader(client, protocol);
                if (messageState != 0)
                {
                    byte[] frame = protocol.Encode(new TestEngineMessage(MessageCode.ProgressReport, "completed"));
                    int length = messageState == 1 ? frame.Length : frame.Length - 1;
                    int sent = 0;
                    while (sent < length)
                        sent += server.Send(frame, sent, length - sent, SocketFlags.None);
                }

                server.Shutdown(SocketShutdown.Send);
                if (messageState == 1)
                    Assert.That(reader.GetNextMessage().Data, Is.EqualTo("completed"));

                Task<TestEngineMessage> read = Task.Run(() => reader.GetNextMessage());
                try
                {
                    Assert.That(Task.WaitAny(new Task[] { read }, 3000), Is.Zero,
                        "The socket reader kept waiting after the remote endpoint closed.");
                    Assert.That(() => read.GetAwaiter().GetResult(), Throws.TypeOf<EndOfStreamException>());
                }
                finally
                {
                    // On the broken reader, closing the local socket releases the spinning worker.
                    client.Dispose();
                    Assert.That(SpinWait.SpinUntil(() => read.IsCompleted, 3000), Is.True);
                    _ = read.Exception;
                }
            }
            finally
            {
                listener.Stop();
            }
        }
    }
}
