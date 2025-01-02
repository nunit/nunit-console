// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using System;

namespace NUnit.Engine.Communication.Messages
{
    /// <summary>
    /// 
    /// </summary>
    [Serializable]
    public class CommandReturnMessage : TestEngineMessage
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="returnValue"></param>
        public CommandReturnMessage(object returnValue)
        {
            ReturnValue = returnValue;
        }

        /// <summary>
        /// 
        /// </summary>
        public object ReturnValue { get; }
    }
}
