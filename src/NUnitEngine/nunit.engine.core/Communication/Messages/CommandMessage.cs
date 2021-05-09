// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using System;

namespace NUnit.Engine.Communication.Messages
{
    [Serializable]
    public class CommandMessage : TestEngineMessage
    {
        public CommandMessage(string commandName, params object[] arguments)
        {
            CommandName = commandName;
            Arguments = arguments;
        }

        public string CommandName { get; }

        public object[] Arguments { get; }
    }
}
