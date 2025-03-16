// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;
using NUnit.Framework;
using NUnit.Engine.Communication.Messages;
using NUnit.Engine.Internal;
using System.Xml;

namespace NUnit.Engine.Communication.Protocols
{
    public class BinarySerializationProtocolTests
    {
        private BinarySerializationProtocol _wireProtocol = new BinarySerializationProtocol();

        [Test]
        public void WriteAndReadBytes()
        {
            int SIZE = 1024;

            var bytes = new byte[SIZE];
            new Random().NextBytes(bytes);

            var stream = new MemoryStream();
            stream.Write(bytes, 0, SIZE);

            Assert.That(stream.Length, Is.EqualTo(SIZE));
            var copy = new byte[SIZE];

            stream.Position = 0;
            Assert.That(stream.Read(copy, 0, SIZE), Is.EqualTo(SIZE));

            Assert.That(copy, Is.EqualTo(bytes));
        }

        [Test]
        public void DecodeSingleMessage()
        {
            var originalPackage = new TestPackage(new[] { "mock-assembly.dll", "notest-assembly.dll" });
            var originalMessage = new TestEngineMessage(MessageCode.CommandResult, originalPackage.ToXml());

            var bytes = _wireProtocol.Encode(originalMessage);
            Console.WriteLine($"Serialized {bytes.Length} bytes.");

            var messages = new List<TestEngineMessage>(_wireProtocol.Decode(bytes));
            Assert.That(messages.Count, Is.EqualTo(1));
            var message = messages[0];

            Assert.That(message.Code, Is.EqualTo(MessageCode.CommandResult));
            Assert.That(message.Data, Is.EqualTo(originalPackage.ToXml()));
            var newPackage = new TestPackage().FromXml(message.Data);
            ComparePackages(newPackage, originalPackage);
        }

        [TestCase(1)]
        [TestCase(2)]
        [TestCase(5)]
        public void DecodeSplitMessages(int numMessages)
        {
            const int SPLIT_SIZE = 1000;

            var originalPackage = new TestPackage(new[] { "mock-assembly.dll", "notest-assembly.dll" });
            var originalMessage = new TestEngineMessage(MessageCode.CommandResult, originalPackage.ToXml());

            var msgBytes = _wireProtocol.Encode(originalMessage);
            var msgLength = msgBytes.Length;
            var allBytes = new byte[msgLength * numMessages];
            for (int i = 0; i < numMessages; i++)
                Array.Copy(msgBytes, 0, allBytes, i * msgLength, msgLength);

            Console.WriteLine($"Serialized {numMessages} messages in {allBytes.Length} bytes.");

            var messages = new List<TestEngineMessage>();

            for (int index = 0; index < allBytes.Length; index += SPLIT_SIZE)
            {
                var bytesToSend = Math.Min(allBytes.Length - index, SPLIT_SIZE);
                var buffer = new byte[bytesToSend];
                Array.Copy(allBytes, index, buffer, 0, bytesToSend);
                messages.AddRange(_wireProtocol.Decode(buffer));
                Console.WriteLine($"Decoded {bytesToSend} bytes, message count is now {messages.Count}");
                var expectedCount = (index + bytesToSend) / msgLength;
                Assert.That(messages.Count, Is.EqualTo(expectedCount));
            }

            foreach (TestEngineMessage message in messages)
            {
                Assert.That(message.Code, Is.EqualTo(MessageCode.CommandResult));
                var newPackage = new TestPackage().FromXml(message.Data);
                ComparePackages(newPackage, originalPackage);
            }
        }

        [Test]
        public void DecodeMultipleMessages()
        {
            var commands = new string[] { "CMD1", "CMD2", "CMD3", "CMD4", "CMD5", "CMD6" };

            var stream = new MemoryStream();

            foreach (var command in commands)
            {
                var buffer = _wireProtocol.Encode(new TestEngineMessage(command, null));
                stream.Write(buffer, 0, buffer.Length);
            }

            var received = new List<TestEngineMessage>(_wireProtocol.Decode(stream.ToArray()));
            Assert.That(received.Count, Is.EqualTo(commands.Length));

            for (int i = 0; i < commands.Length; i++)
                Assert.That(received[i].Code, Is.EqualTo(commands[i]));
        }

        private void ComparePackages(TestPackage newPackage, TestPackage oldPackage)
        {
            Assert.Multiple(() =>
            {
                Assert.That(newPackage.Name, Is.EqualTo(oldPackage.Name));
                Assert.That(newPackage.FullName, Is.EqualTo(oldPackage.FullName));
                Assert.That(newPackage.Settings.Count, Is.EqualTo(oldPackage.Settings.Count));
                Assert.That(newPackage.SubPackages.Count, Is.EqualTo(oldPackage.SubPackages.Count));

                foreach (var key in oldPackage.Settings.Keys)
                {
                    Assert.That(newPackage.Settings.ContainsKey(key));
                    Assert.That(newPackage.Settings[key], Is.EqualTo(oldPackage.Settings[key]));
                }

                for (int i = 0; i < oldPackage.SubPackages.Count; i++)
                    ComparePackages(newPackage.SubPackages[i], oldPackage.SubPackages[i]);
            });
        }
    }
}
