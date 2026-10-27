// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using System;
using System.Globalization;
using System.IO;
using System.Net.Sockets;
using System.Xml.Linq;
using NUnit.Engine.Communication.Messages;
using NUnit.Engine.Communication.Protocols;
using NUnit.Engine.Communication.Transports.Tcp;

namespace NUnit.Engine.TestHelpers
{
    /// <summary>
    /// Exchanges messages using NUnit's existing TCP framing. Provider commands
    /// and their XML payloads are provisional test infrastructure, not a public protocol.
    /// </summary>
    internal sealed class ProviderConnection : IDisposable
    {
        internal const string ListAgents = "LIST";
        internal const string FindAgents = "FIND";
        internal const string IsAvailable = "AVAL";
        internal const string GetAgent = "GETA";
        internal const string ReleaseAgent = "RELA";
        internal const string ActiveAgents = "ACTV";
        internal const string Error = "ERRO";
        internal const string DisposeRunner = "DISP";

        private readonly Socket _socket;
        private readonly BinarySerializationProtocol _protocol = new BinarySerializationProtocol();
        private readonly SocketReader _reader;
        private readonly object _sendLock = new object();

        internal ProviderConnection(Socket socket)
        {
            _socket = socket;
            _socket.SendTimeout = 30000;
            _socket.ReceiveTimeout = 30000;
            _reader = new SocketReader(socket, _protocol);
        }

        internal TestEngineMessage Receive() => _reader.GetNextMessage();

        internal void Send(string code, string? data = null)
        {
            byte[] bytes = _protocol.Encode(new TestEngineMessage(code, data));
            lock (_sendLock)
            {
                int offset = 0;
                while (offset < bytes.Length)
                {
                    int sent = _socket.Send(bytes, offset, bytes.Length - offset, SocketFlags.None);
                    if (sent == 0)
                        throw new EndOfStreamException("The provider connection closed while sending a message.");
                    offset += sent;
                }
            }
        }

        internal static string Request(Guid owner, Guid agentId, string? data)
        {
            return new XElement("request", new XAttribute("owner", owner),
                new XAttribute("agent", agentId), data ?? string.Empty).ToString(SaveOptions.DisableFormatting);
        }

        internal static string Failure(Exception exception)
        {
            return new XElement("error", new XAttribute("type", exception.GetType().FullName!),
                exception.Message).ToString(SaveOptions.DisableFormatting);
        }

        internal static int Integer(string value) => int.Parse(value, CultureInfo.InvariantCulture);

        public void Dispose() => _socket.Dispose();
    }
}
