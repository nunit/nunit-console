// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using NUnit.Engine.Communication.Messages;

namespace NUnit.Engine.Communication.Protocols
{
    /// <summary>
    /// BinarySerializationProtocol serializes messages in the following format:
    ///
    ///     [Message Length (4 bytes)][Serialized Message Content]
    ///     
    /// The message content is in binary form as produced by the .NET BinaryFormatter.
    /// Each message of length n is serialized as n + 4 bytes.
    /// </summary>
    public class BinarySerializationProtocol : ISerializationProtocol
    {
        /// <summary>
        /// Maximum length of a message.
        /// </summary>
        private const int MAX_MESSAGE_LENGTH = 128 * 1024 * 1024; //128 Megabytes.

        /// <summary>
        /// This MemoryStream object is used to collect receiving bytes to build messages.
        /// </summary>
        private MemoryStream _receiveMemoryStream = new MemoryStream();

        /// <summary>
        /// Serializes a message to a byte array to send to remote application.
        /// </summary>
        /// <param name="message">Message to be serialized</param>
        /// <returns>A byte[] containing the message, serialized as per the protocol.</returns>
        public byte[] Encode(object message)
        {
            //Serialize the message to a byte array
            var serializedMessage = SerializeMessage(message);

            //Check for message length
            var messageLength = serializedMessage.Length;
            if (messageLength > MAX_MESSAGE_LENGTH)
            {
                throw new Exception("Message is too big (" + messageLength + " bytes). Max allowed length is " + MAX_MESSAGE_LENGTH + " bytes.");
            }

            //Create a byte array including the length of the message (4 bytes) and serialized message content
            var bytes = new byte[messageLength + 4];
            WriteInt32(bytes, 0, messageLength);
            Array.Copy(serializedMessage, 0, bytes, 4, messageLength);

            //Return serialized message by this protocol
            return bytes;
        }

        /// <summary>
        /// Accept an array of bytes and deserialize the messages that are found.
        /// A single call may provide no messages, part of a message, a single
        /// message or multiple messages. Unused bytes are saved for use as
        /// further calls are made.
        /// </summary>
        /// <param name="receivedBytes">The byte array to be deserialized</param>
        /// <returns>An enumeration of TestEngineMessages</returns>
        public IEnumerable<TestEngineMessage> Decode(byte[] receivedBytes)
        {
            // Write all received bytes to the _receiveMemoryStream, which may
            // already contain part of a message from a previous call.
            _receiveMemoryStream.Write(receivedBytes, 0, receivedBytes.Length);

            //Create a list to collect messages
            var messages = new List<TestEngineMessage>();

            //Read all available messages and add to messages collection
            while (ReadSingleMessage(messages)) { }

            return messages;
        }

        public void Reset()
        {
            if (_receiveMemoryStream.Length > 0)
            {
                _receiveMemoryStream = new MemoryStream();
            }
        }

        /// <summary>
        /// Serializes a message to a byte array to send to remote application.
        /// </summary>
        /// <param name="message">Message to be serialized</param>
        /// <returns>
        /// A byte[] containing the message itself, without a prefixed
        /// length, serialized according to the protocol.
        /// </returns>
        internal static byte[] SerializeMessage(object message)
        {
            using (var memoryStream = new MemoryStream())
            {
                new BinaryFormatter().Serialize(memoryStream, message);
                return memoryStream.ToArray();
            }
        }

        /// <summary>
        /// Deserializes a message contained in a byte array.
        /// </summary>
        /// <param name="bytes">A byte[] containing just the message, without a length prefix</param>
        /// <returns>An object representing the message encoded in the byte array</returns>
        internal object DeserializeMessage(byte[] bytes)
        {
            using (var memoryStream = new MemoryStream(bytes))
            {
                try
                {
                    return new BinaryFormatter().Deserialize(memoryStream);
                }
                catch (Exception exception)
                {
                    Reset(); // reset the received memory stream before the exception is rethrown - otherwise the same erroneous message is received again and again
                    throw new SerializationException("error while deserializing message", exception);
                }
            }
        }

        /// <summary>
        /// This method tries to read a single message and add to the messages collection. 
        /// </summary>
        /// <param name="messages">Messages collection to collect messages</param>
        /// <returns>
        /// Returns a boolean value indicates that if there is a need to re-call this method.
        /// </returns>
        /// <exception cref="CommunicationException">Throws CommunicationException if message is bigger than maximum allowed message length.</exception>
        private bool ReadSingleMessage(ICollection<TestEngineMessage> messages)
        {
            //Go to the begining of the stream
            _receiveMemoryStream.Position = 0;

            //If stream has less than 4 bytes, that means we can not even read length of the message
            //So, return false to wait more for bytes from remorte application.
            if (_receiveMemoryStream.Length < 4)
            {
                return false;
            }

            //Read length of the message
            var messageLength = ReadInt32(_receiveMemoryStream);
            if (messageLength > MAX_MESSAGE_LENGTH)
            {
                throw new Exception("Message is too big (" + messageLength + " bytes). Max allowed length is " + MAX_MESSAGE_LENGTH + " bytes.");
            }

            //If message is zero-length (It must not be but good approach to check it)
            if (messageLength == 0)
            {
                //if no more bytes, return immediately
                if (_receiveMemoryStream.Length == 4)
                {
                    _receiveMemoryStream = new MemoryStream(); //Clear the stream
                    return false;
                }

                //Create a new memory stream from current except first 4-bytes.
                var bytes = _receiveMemoryStream.ToArray();
                _receiveMemoryStream = new MemoryStream();
                _receiveMemoryStream.Write(bytes, 4, bytes.Length - 4);
                return true;
            }

            //If all bytes of the message is not received yet, return to wait more bytes
            if (_receiveMemoryStream.Length < (4 + messageLength))
            {
                _receiveMemoryStream.Position = _receiveMemoryStream.Length;
                return false;
            }

            //Read bytes of serialized message and deserialize it
            var serializedMessageBytes = ReadByteArray(_receiveMemoryStream, messageLength);

            messages.Add((TestEngineMessage)DeserializeMessage(serializedMessageBytes));

            //Read remaining bytes to an array
            var remainingBytes = ReadByteArray(_receiveMemoryStream, (int)(_receiveMemoryStream.Length - (4 + messageLength)));

            //Re-create the receive memory stream and write remaining bytes
            _receiveMemoryStream = new MemoryStream();
            _receiveMemoryStream.Write(remainingBytes, 0, remainingBytes.Length);

            //Return true to re-call this method to try to read next message
            return (remainingBytes.Length > 4);
        }

        /// <summary>
        /// Writes a int value to a byte array from a starting index.
        /// </summary>
        /// <param name="buffer">Byte array to write int value</param>
        /// <param name="startIndex">Start index of byte array to write</param>
        /// <param name="number">An integer value to write</param>
        private static void WriteInt32(byte[] buffer, int startIndex, int number)
        {
            buffer[startIndex] = (byte)((number >> 24) & 0xFF);
            buffer[startIndex + 1] = (byte)((number >> 16) & 0xFF);
            buffer[startIndex + 2] = (byte)((number >> 8) & 0xFF);
            buffer[startIndex + 3] = (byte)((number) & 0xFF);
        }

        /// <summary>
        /// Deserializes and returns a serialized integer.
        /// </summary>
        /// <returns>Deserialized integer</returns>
        private static int ReadInt32(Stream stream)
        {
            var buffer = ReadByteArray(stream, 4);
            return ((buffer[0] << 24) |
                    (buffer[1] << 16) |
                    (buffer[2] << 8) |
                    (buffer[3])
                   );
        }

        /// <summary>
        /// Reads a byte array with specified length.
        /// </summary>
        /// <param name="stream">Stream to read from</param>
        /// <param name="length">Length of the byte array to read</param>
        /// <returns>Read byte array</returns>
        /// <exception cref="EndOfStreamException">Throws EndOfStreamException if can not read from stream.</exception>
        private static byte[] ReadByteArray(Stream stream, int length)
        {
            var buffer = new byte[length];
            var totalRead = 0;
            while (totalRead < length)
            {
                var read = stream.Read(buffer, totalRead, length - totalRead);
                if (read <= 0)
                {
                    throw new EndOfStreamException("Can not read from stream! Input stream is closed.");
                }

                totalRead += read;
            }

            return buffer;
        }
    }
}
