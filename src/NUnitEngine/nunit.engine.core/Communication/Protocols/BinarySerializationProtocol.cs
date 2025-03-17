// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using NUnit.Engine.Communication.Messages;

namespace NUnit.Engine.Communication.Protocols
{
    /// <summary>
    /// BinarySerializationProtocol serializes messages as an array of bytes in the following format:
    ///
    ///     <Message Length> <Message Code> [<Additional Message Data>]
    ///
    /// The length of the message is encoded as four bytes, from lowest to highest order.
    /// 
    /// The message code is always four bytes and indicates the type of message.
    /// 
    /// Messages taking an additional data argument encode it in the remaining bytes. The
    /// argument length may therefore be calculated as overall length - 8 bytes. Messages
    /// without an argument are serialized as 8 bytes.
    /// </summary>
    public class BinarySerializationProtocol : ISerializationProtocol
    {
        private const int MSG_LENGTH_SIZE = 4;
        private const int MSG_CODE_SIZE = 4;

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
        public byte[] Encode(TestEngineMessage message)
        {
            string code = message.Code;
            string data = message.Data;
            Encoding utf8 = Encoding.UTF8;

            // TODO: Compile time check
            if (utf8.GetByteCount(code) != MSG_CODE_SIZE)
                throw new ArgumentException($"Invalid message code {code}");

            int dataLength = message.Data != null ? utf8.GetByteCount(message.Data) : 0;
            int messageLength = dataLength + MSG_CODE_SIZE;

            //Check message length
            if (messageLength > MAX_MESSAGE_LENGTH)
                throw new Exception("Message is too big (" + messageLength + " bytes). Max allowed length is " + MAX_MESSAGE_LENGTH + " bytes.");

            //Create a byte array including the length of the message (4 bytes) and serialized message content
            var bytes = new byte[messageLength + MSG_LENGTH_SIZE];
            WriteInt32(bytes, 0, messageLength);
            utf8.GetBytes(code, 0, code.Length, bytes, MSG_LENGTH_SIZE);
            if (dataLength > 0)
                utf8.GetBytes(data, 0, data.Length, bytes, MSG_LENGTH_SIZE + MSG_CODE_SIZE);

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
            if (_receiveMemoryStream.Length < MSG_LENGTH_SIZE)
            {
                return false;
            }

            //Read length of the message
            var messageLength = ReadInt32(_receiveMemoryStream);
            if (messageLength > MAX_MESSAGE_LENGTH)
                throw new Exception("Message is too big (" + messageLength + " bytes). Max allowed length is " + MAX_MESSAGE_LENGTH + " bytes.");

            //If message is zero-length (It must not be but good approach to check it)
            if (messageLength == 0)
            {
                //if no more bytes, return immediately
                if (_receiveMemoryStream.Length == MSG_LENGTH_SIZE)
                {
                    _receiveMemoryStream = new MemoryStream(); //Clear the stream
                    return false;
                }

                //Create a new memory stream from current except first 4-bytes.
                var bytes = _receiveMemoryStream.ToArray();
                _receiveMemoryStream = new MemoryStream();
                _receiveMemoryStream.Write(bytes, MSG_LENGTH_SIZE, bytes.Length - MSG_LENGTH_SIZE);
                return true;
            }

            //If all bytes of the message is not received yet, return to wait more bytes
            if (_receiveMemoryStream.Length < (MSG_LENGTH_SIZE + messageLength))
            {
                _receiveMemoryStream.Position = _receiveMemoryStream.Length;
                return false;
            }

            //Read bytes of serialized message and deserialize it
            var serializedMessageBytes = ReadByteArray(_receiveMemoryStream, messageLength);

            Encoding utf8 = Encoding.UTF8;

            string code = utf8.GetString(serializedMessageBytes, 0, MSG_CODE_SIZE);

            string data = messageLength > MSG_CODE_SIZE
                ? utf8.GetString(serializedMessageBytes, MSG_CODE_SIZE, messageLength - MSG_CODE_SIZE)
                : null;

            messages.Add(new TestEngineMessage(code, data));

            //Read remaining bytes to an array
            var remainingBytes = ReadByteArray(_receiveMemoryStream, (int)(_receiveMemoryStream.Length - (4 + messageLength)));

            //Re-create the receive memory stream and write remaining bytes
            _receiveMemoryStream = new MemoryStream();
            _receiveMemoryStream.Write(remainingBytes, 0, remainingBytes.Length);

            //Return true to re-call this method to try to read next message
            return (remainingBytes.Length > MSG_LENGTH_SIZE);
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
            return ReadInt32(buffer, 0);
        }

        private static int ReadInt32(byte[] buffer, int index)
        {
            return ((buffer[index] << 24) |
                    (buffer[index + 1] << 16) |
                    (buffer[index + 2] << 8) |
                    (buffer[index + 3])
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
