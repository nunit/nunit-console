// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using System;

namespace NUnit.Engine.Communication.Messages
{
    [Serializable]
    public sealed class TestEngineMessage
    {
        public TestEngineMessage(string code, string data)
        {
            Code = code;
            Data = data;
        }

        public string Code { get; }
        public string Data { get; }
    }
}
