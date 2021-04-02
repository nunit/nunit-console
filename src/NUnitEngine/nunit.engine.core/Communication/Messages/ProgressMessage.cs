// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using System;

namespace NUnit.Engine.Communication.Messages
{
#if !NETSTANDARD1_6
    [Serializable]
#endif
    public class ProgressMessage : TestEngineMessage
    {
        public ProgressMessage(string report)
        {
            Report = report;
        }

        public string Report { get; }
    }
}
