// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using System.Threading;

namespace NUnit.Engine
{
    public sealed class CurrentMessageCounter
    {
        private readonly ManualResetEvent _noMessages = new ManualResetEvent(true);
        private int _currentMessageCount;

        public void OnMessageStart()
        {
            if (Interlocked.Increment(ref _currentMessageCount) == 1)
                _noMessages.Reset();
        }

        public void OnMessageEnd()
        {
            if (Interlocked.Decrement(ref _currentMessageCount) == 0)
                _noMessages.Set();
        }

        public void WaitForAllCurrentMessages()
        {
            _noMessages.WaitOne();
        }
    }
}