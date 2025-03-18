// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using System.Collections.Generic;
using NUnit.Engine.Communication.Messages;

namespace NUnit.Engine.Communication.Protocols
{
    public interface ISerializationProtocol
    {
        /// <summary>
        /// Serializes a message to a byte array to send to remote application.
        /// </summary>
        /// <param name="message">Message to be serialized</param>
        /// <returns>A byte[] containing the message, serialized as per the protocol.</returns>
        byte[] Encode(TestEngineMessage message);

        /// <summary>
        /// Accept an array of bytes and deserialize the messages that are found.
        /// A single call may provide no messages, part of a message, a single
        /// message or multiple messages. Unused bytes are saved for use as
        /// further calls are made.
        /// </summary>
        /// <param name="receivedBytes">The byte array to be deserialized</param>
        /// <returns>An enumeration of TestEngineMessages</returns>
        IEnumerable<TestEngineMessage> Decode(byte[] receivedBytes);
    }
}
