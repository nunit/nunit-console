// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using System;

namespace NUnit.Engine.Communication.Messages
{
#if !NETSTANDARD1_6
    [Serializable]
#endif
    public abstract class TestEngineMessage
    {
    }
}
