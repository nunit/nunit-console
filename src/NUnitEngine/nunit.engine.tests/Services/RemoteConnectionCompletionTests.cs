// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using System;
using System.Net.Sockets;
using System.Threading.Tasks;
using NUnit.Framework;

namespace NUnit.Engine.Services
{
    /// <summary>
    /// Checks connection completion ownership, original exceptions and bounded waiting
    /// independently of the timing of a real socket's synchronous completion path.
    /// </summary>
    public class RemoteConnectionCompletionTests
    {
        [Test]
        public void CompletionDoesNotDisposeTheTasksBorrowedWaitHandle()
        {
            var source = new TaskCompletionSource<int>();
            source.SetResult(0);
            using var task = source.Task;
            var handle = ((IAsyncResult)task).AsyncWaitHandle;

            RemoteAgentProviderClient.WaitForConnection(task, 1000);

            Assert.That(handle.SafeWaitHandle.IsClosed, Is.False);
        }

        [Test]
        public void ConnectionFailurePreservesTheOriginalException()
        {
            var failure = new SocketException((int)SocketError.ConnectionRefused);
            var source = new TaskCompletionSource<int>();
            source.SetException(failure);
            using var task = source.Task;

            Assert.That(Assert.Catch(() => RemoteAgentProviderClient.WaitForConnection(task, 1000)), Is.SameAs(failure));
        }

        [Test]
        public void DeadlineDoesNotRequireThePendingConnectionToComplete()
        {
            var source = new TaskCompletionSource<int>();

            Assert.That(() => RemoteAgentProviderClient.WaitForConnection(source.Task, 1), Throws.TypeOf<TimeoutException>());
            Assert.That(source.Task.IsCompleted, Is.False);

            // The socket owner may abort the outstanding operation after the deadline.
            source.SetException(new SocketException((int)SocketError.OperationAborted));
            source.Task.Dispose();
        }
    }
}
