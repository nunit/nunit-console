// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using System;

namespace NUnit.Engine
{
    internal class NullListener : MarshalByRefObject, ITestEventListener
    {
        public void OnTestEvent(string testEvent)
        {
            // No action
        }
    }
}
