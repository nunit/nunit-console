// ***********************************************************************
// Copyright (c) 2015 Charlie Poole, Rob Prouse
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
// ***********************************************************************

#if !NETCOREAPP1_1
using System;
using System.Threading;
using NUnit.Framework;

namespace NUnit.Engine.Runners.Tests
{
    public class ParallelTaskWorkerPoolTests
    {
        [TestCase(-1)]
        [TestCase(0)]
        public void RequiresAtLeastOneThread(int numThreads)
        {
            Assert.Throws<ArgumentException>(() => new ParallelTaskWorkerPool(numThreads));
        }

        [Test]
        public void EnqueueCannotBeCalledAfterWorkHasStarted()
        {
            var workerPool = new ParallelTaskWorkerPool(1);
            workerPool.Start();

            Assert.Throws<InvalidOperationException>(() => workerPool.Enqueue(new NoOpTask()));
        }

        [Test]
        public void WaitAll_SingleTask()
        {
            var workerPool = new ParallelTaskWorkerPool(1);
            var task = new BusyTask();
            workerPool.Enqueue(task);
            workerPool.Start();

            Assert.That(workerPool.WaitAll(10), Is.False, "Threads should not have exited, work is in progress");

            task.MarkTaskAsCompleted();

            Assert.That(workerPool.WaitAll(100), Is.True, "Threads should have exited, all work is complete");
        }

        [Test]
        public void WaitAll_SingleThread_MultipleTasks()
        {
            var workerPool = new ParallelTaskWorkerPool(1);
            var task1 = new BusyTask();
            var task2 = new BusyTask();
            workerPool.Enqueue(task1);
            workerPool.Enqueue(task2);
            workerPool.Start();

            Assert.That(workerPool.WaitAll(10), Is.False, "Threads should not have exited, 2 tasks are in progress");

            Assert.That(task1.State, Is.EqualTo(BusyTaskState.Executing));
            Assert.That(task2.State, Is.EqualTo(BusyTaskState.Queued));

            task1.MarkTaskAsCompleted();

            Assert.That(workerPool.WaitAll(10), Is.False, "Threads should not have exited, 1 task is in progress");

            Assert.That(task1.State, Is.EqualTo(BusyTaskState.Completed));
            Assert.That(task2.State, Is.EqualTo(BusyTaskState.Executing));

            task2.MarkTaskAsCompleted();

            Assert.That(workerPool.WaitAll(100), Is.True, "Threads should have exited, all work is complete");

            Assert.That(task1.State, Is.EqualTo(BusyTaskState.Completed));
            Assert.That(task2.State, Is.EqualTo(BusyTaskState.Completed));
        }

        [Test]
        public void WaitAll_TwoThreads_MultipleTasks()
        {
            var workerPool = new ParallelTaskWorkerPool(2);
            var task1 = new BusyTask();
            var task2 = new BusyTask();
            workerPool.Enqueue(task1);
            workerPool.Enqueue(task2);
            workerPool.Start();

            Assert.That(workerPool.WaitAll(10), Is.False, "Threads should not have exited, 2 tasks are in progress");

            Assert.That(task1.State, Is.EqualTo(BusyTaskState.Executing));
            Assert.That(task2.State, Is.EqualTo(BusyTaskState.Executing));

            task1.MarkTaskAsCompleted();

            Assert.That(workerPool.WaitAll(10), Is.False, "Threads should not have exited, 1 task is in progress");

            Assert.That(task1.State, Is.EqualTo(BusyTaskState.Completed));
            Assert.That(task2.State, Is.EqualTo(BusyTaskState.Executing));

            task2.MarkTaskAsCompleted();

            Assert.That(workerPool.WaitAll(100), Is.True, "Threads should have exited, all work is complete");

            Assert.That(task1.State, Is.EqualTo(BusyTaskState.Completed));
            Assert.That(task2.State, Is.EqualTo(BusyTaskState.Completed));
        }

        private class NoOpTask : ITestExecutionTask
        {
            public void Execute() { }
        }

        private enum BusyTaskState
        {
            Queued,
            Executing,
            Completed
        }

        private class BusyTask : ITestExecutionTask
        {
            private readonly Semaphore _semaphore;
            public BusyTaskState State { get; private set; }

            public BusyTask()
            {
                _semaphore = new Semaphore(0, 1);
                State = BusyTaskState.Queued;
            }

            public void Execute()
            {
                State = BusyTaskState.Executing;
                _semaphore.WaitOne();
                State = BusyTaskState.Completed;
            }

            public void MarkTaskAsCompleted()
            {
                _semaphore.Release(1);
            }
        }
    }
}
#endif