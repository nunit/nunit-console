// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using System;

namespace NUnit.Engine.Communication.Messages
{
    /// <summary>
    /// 
    /// </summary>
    [Serializable]
    public class CommandMessage : TestEngineMessage
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="commandName"></param>
        /// <param name="arguments"></param>
        public CommandMessage(string commandName, params object[] arguments)
        {
            CommandName = commandName;
            Arguments = arguments;
        }

        /// <summary>
        /// 
        /// </summary>
        public string CommandName { get; }
        /// <summary>
        /// 
        /// </summary>
        public object[] Arguments { get; }
    }
}
