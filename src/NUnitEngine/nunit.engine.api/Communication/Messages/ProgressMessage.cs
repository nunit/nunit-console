// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using System;

namespace NUnit.Engine.Communication.Messages
{
    /// <summary>
    /// 
    /// </summary>
    [Serializable]
    public class ProgressMessage : TestEngineMessage
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="report"></param>
        public ProgressMessage(string report)
        {
            Report = report;
        }

        /// <summary>
        /// 
        /// </summary>
        public string Report { get; }
    }
}
