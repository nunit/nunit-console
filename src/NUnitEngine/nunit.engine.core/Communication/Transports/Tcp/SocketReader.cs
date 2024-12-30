// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using System;
using System.Collections.Generic;
using System.Net.Sockets;
using NUnit.Engine.Communication.Messages;
using NUnit.Engine.Communication.Protocols;

namespace NUnit.Engine.Communication.Transports.Tcp
{
    /// <summary>
    /// SocketReader reads a socket and composes Messages
    /// for consumption using a specific wire protocol.
    /// </summary>
    public class SocketReader
    {
        private const int BUFFER_SIZE = 1024;

        private readonly Socket _socket;
        private readonly ISerializationProtocol _wireProtocol;

        private readonly Queue<TestEngineMessage> _msgQueue;
        private readonly byte[] _buffer;

        public SocketReader(Socket socket, ISerializationProtocol protocol)
        {
            _socket = socket;
            _wireProtocol = protocol;

            _msgQueue = new Queue<TestEngineMessage>();
            _buffer = new byte[BUFFER_SIZE];
        }

        /// <summary>
        /// Get the next TestEngineMessage to arrive
        /// </summary>
        /// <returns>The message</returns>
        public TestEngineMessage GetNextMessage()
        {
            while (_msgQueue.Count == 0)
            {
                int n = _socket.Receive(_buffer);
                var bytes = new byte[n];
                Array.Copy(_buffer, 0, bytes, 0, n);
                foreach (var message in _wireProtocol.Decode(bytes))
                    _msgQueue.Enqueue(message);
            }

            return _msgQueue.Dequeue();
        }

        /// <summary>
        /// Get the next message to arrive, which must be of the
        /// specified message type.
        /// </summary>
        /// <typeparam name="TMessage">The expected message type</typeparam>
        /// <returns>A message of type TMessage</returns>
        /// <exception cref="InvalidOperationException">A message of a different type was received</exception>
        public TMessage GetNextMessage<TMessage>() where TMessage : TestEngineMessage
        {
            var receivedMessage = GetNextMessage();
            var expectedMessage = receivedMessage as TMessage;

            if (expectedMessage == null)
                throw new InvalidOperationException($"Expected a {typeof(TMessage)} but received a {receivedMessage.GetType()}");

            return expectedMessage;
        }
    }
}
