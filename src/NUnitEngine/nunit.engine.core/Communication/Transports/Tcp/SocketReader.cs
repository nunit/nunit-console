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

        private Socket _socket;
        private ISerializationProtocol _wireProtocol;

        private Queue<TestEngineMessage> _msgQueue;
        private byte[] _buffer;

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
    }
}
