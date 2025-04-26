// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using System;

namespace NUnit.Engine.Communication.Messages
{
    [Serializable]
    public class TestEngineMessage
    {
        public TestEngineMessage(string code, string? data = null)
        {
            Guard.ArgumentNotNull(code, nameof(code));
            Guard.ArgumentValid(code.Length == 4, "All message codes must be length 4", nameof(code));

            Code = code;
            Data = data;
        }

        public string Code { get; }
        public string? Data { get; }

        // Alias properties for convenience
        //public string CommandName => Code;
        //public string Argument => Data;    }
    }
}
