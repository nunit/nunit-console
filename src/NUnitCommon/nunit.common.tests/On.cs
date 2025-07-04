﻿// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using System;
using System.Threading;

namespace NUnit.Engine.Tests.Helpers
{
    public static class On
    {
        public static IDisposable Dispose(Action action) => new OnDisposeAction(action);

        private sealed class OnDisposeAction : IDisposable
        {
            private Action? action;

            public OnDisposeAction(Action action)
            {
                this.action = action;
            }

            public void Dispose() => Interlocked.Exchange(ref action, null)?.Invoke();
        }
    }
}
