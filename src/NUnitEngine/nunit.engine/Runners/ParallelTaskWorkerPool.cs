// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using NUnit.Common;

namespace NUnit.Engine.Runners
{
    public class ParallelTaskWorkerPool
    {
        private readonly object _taskLock = new object();
        private readonly Queue<ITestExecutionTask> _tasks;
        private readonly List<Thread> _threads;
        private readonly int _maxThreads;

        private bool _isRunning = false;

        public ParallelTaskWorkerPool(int maxThreads)
        {
            Guard.ArgumentValid(maxThreads >= 1, "Number of threads must be greater than zero.", "maxThreads");

            _maxThreads = maxThreads;
            _tasks = new Queue<ITestExecutionTask>();
            _threads = new List<Thread>();
        }

        public void Enqueue(ITestExecutionTask task)
        {
            Guard.OperationValid(!_isRunning, "Can only enqueue tasks before starting the queue");

            _tasks.Enqueue(task);
        }

        public void Start()
        {
            _isRunning = true;

            var numThreads = Math.Min(_tasks.Count, _maxThreads);
            for (var i = 0; i < numThreads; i++)
                _threads.Add(new Thread(ProcessTasksProc));

            foreach (var thread in _threads)
                thread.Start();
        }

        private void ProcessTasksProc()
        {
            while (true)
            {
                ITestExecutionTask task = null;
                lock (_taskLock)
                {
                    if (_tasks.Count > 0)
                        task = _tasks.Dequeue();
                    else
                        return;
                }

                task.Execute();
            }
        }

        public bool WaitAll(int timeout)
        {
            var sw = Stopwatch.StartNew();
            var remainingTimeout = timeout;

            foreach (var thread in _threads)
            {
                if (!thread.Join(remainingTimeout))
                    return false;

                if (timeout != Timeout.Infinite)
                {
                    remainingTimeout = timeout - (int)sw.ElapsedMilliseconds;
                    if (remainingTimeout <= 0)
                        return false;
                }
            }

            return true;
        }

        public void WaitAll()
        {
            foreach (var thread in _threads)
                thread.Join();
        }
    }
}
