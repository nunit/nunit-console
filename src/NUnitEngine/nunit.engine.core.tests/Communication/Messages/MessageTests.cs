// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using System.Collections.Generic;
using NUnit.Framework;
using NUnit.Engine.Communication.Protocols;
using NUnit.Engine.Internal;

namespace NUnit.Engine.Communication.Messages
{
    public class MessageTests
    {
        const string EMPTY_FILTER = "</filter>";
        static readonly string TEST_PACKAGE = new TestPackage("mock-assembly.dll").ToXml();

        private BinarySerializationProtocol _wireProtocol = new BinarySerializationProtocol();

        static readonly TestCaseData[] MessageTestData = new TestCaseData[]
        {
            new TestCaseData(MessageCode.CreateRunner, TEST_PACKAGE),
            new TestCaseData(MessageCode.LoadCommand, null),
            new TestCaseData(MessageCode.ReloadCommand, null),
            new TestCaseData(MessageCode.UnloadCommand, null),
            new TestCaseData(MessageCode.ExploreCommand, EMPTY_FILTER),
            new TestCaseData(MessageCode.CountCasesCommand, EMPTY_FILTER),
            new TestCaseData(MessageCode.RunCommand, EMPTY_FILTER),
            new TestCaseData(MessageCode.RunAsyncCommand, EMPTY_FILTER),
            new TestCaseData(MessageCode.RequestStopCommand, null),
            new TestCaseData(MessageCode.ForcedStopCommand, null)
        };

        //[TestCaseSource(nameof(MessageTestData))]
        //public void CommandMessageConstructionTests(string commandName, string argument)
        //{
        //    var cmd = new TestEngineMessage(commandName, argument);
        //    Assert.That(cmd.Code, Is.EqualTo(commandName));
        //    Assert.That(cmd.CommandName, Is.EqualTo(commandName));
        //    Assert.That(cmd.Argument, Is.EqualTo(argument));
        //}

        //[TestCaseSource(nameof(MessageTestData))]
        //public void CommandMessageEncodingTests(string commandName, string argument)
        //{
        //    var cmd = new TestEngineMessage(commandName, argument);

        //    var bytes = _wireProtocol.Encode(cmd);
        //    var messages = new List<TestEngineMessage>(_wireProtocol.Decode(bytes));
        //    var decoded = messages[0];
        //    Assert.That(decoded.Code, Is.EqualTo(commandName));
        //    Assert.That(decoded.CommandName, Is.EqualTo(commandName));
        //    Assert.That(decoded.Argument, Is.EqualTo(argument));
        //}

        [Test]
        public void ProgressMessageTest()
        {
            const string REPORT = "Progress report";
            var msg = new TestEngineMessage(MessageCode.ProgressReport, REPORT);
            Assert.That(msg.Code, Is.EqualTo(MessageCode.ProgressReport));
            Assert.That(msg.Data, Is.EqualTo(REPORT));
            var bytes = _wireProtocol.Encode(msg);
            var messages = new List<TestEngineMessage>(_wireProtocol.Decode(bytes));
            var decoded = messages[0];
            Assert.That(decoded.Code, Is.EqualTo(MessageCode.ProgressReport));
            Assert.That(decoded.Data, Is.EqualTo(REPORT));
        }

        //[Test]
        //public void CommandReturnMessageTest()
        //{
        //    const string RESULT = "Result text";
        //    var msg = new TestEngineMessage(MessageCode.CommandResult, RESULT);
        //    Assert.That(msg.Code, Is.EqualTo(MessageCode.CommandResult));
        //    Assert.That(msg.Data, Is.EqualTo(RESULT));
        //    var bytes = _wireProtocol.Encode(msg);
        //    var messages = new List<TestEngineMessage>(_wireProtocol.Decode(bytes));
        //    var decoded = messages[0];
        //    Assert.That(decoded.Code, Is.EqualTo(MessageCode.CommandResult));
        //    Assert.That(decoded.Data, Is.EqualTo(RESULT));
        //}
    }
}
