// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

#if NET8_0_OR_GREATER
using System;
using System.Threading;
using NUnit.Framework;
using NUnit.Remote.Hosting;

namespace NUnit.Engine.Services
{
    /// <summary>Protects the listener from inline execution of a registered synchronous request.</summary>
    public sealed class StandaloneServerRequestGateTests
    {
        [Test]
        public void ReleasingRegistrationDoesNotWaitForTheRequestToComplete()
        {
            var gate = RemoteProviderServer.CreateRequestRegistrationGate();
            using var entered = new ManualResetEventSlim();
            using var release = new ManualResetEventSlim();
            using var published = new ManualResetEventSlim();
            using var completed = new ManualResetEventSlim();
            int requestThread = 0;
            int listenerThread = 0;
            gate.Task.ConfigureAwait(false).GetAwaiter().UnsafeOnCompleted(() =>
            {
                requestThread = Environment.CurrentManagedThreadId;
                entered.Set();
                release.Wait(TimeSpan.FromSeconds(10));
                completed.Set();
            });
            var listener = new Thread(() =>
            {
                listenerThread = Environment.CurrentManagedThreadId;
                gate.SetResult(true);
                published.Set();
            });
            bool returned;
            bool started;
            listener.Start();
            try
            {
                started = entered.Wait(TimeSpan.FromSeconds(5));
                returned = published.Wait(TimeSpan.FromSeconds(2));
            }
            finally
            {
                release.Set();
                Assert.That(completed.Wait(5000), Is.True, "The request callback must finish.");
                Assert.That(listener.Join(5000), Is.True, "The registration publisher must finish.");
            }

            Assert.That(started, Is.True, "The registered request must start.");
            Assert.That(returned, Is.True, "A running request must not hold the listener inside gate release.");
            Assert.That(requestThread, Is.Not.EqualTo(listenerThread));
        }
    }
}
#endif
