// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using System;
using System.Threading;
using System.Xml;
using NUnit.Common;

namespace NUnit.Engine
{
    /// <summary>
    /// The TestRun class encapsulates an ongoing test run.
    /// </summary>
    [Serializable]
    public class AsyncTestEngineResult : ITestRun
    {
        private volatile TestEngineResult _result;
        private readonly ManualResetEvent _waitHandle = new ManualResetEvent(false);

        /// <summary>
        /// Get the result of this run.
        /// </summary>
        /// <exception cref="InvalidOperationException">Cannot retrieve Result from an incomplete or cancelled TestRun.</exception>
        public TestEngineResult EngineResult
        {
            get
            {
                Guard.OperationValid(_result != null, "Cannot retrieve Result from an incomplete or cancelled TestRun.");

                return _result;
            }
        }

        public EventWaitHandle WaitHandle
        {
            get { return _waitHandle; }
        }
        
        public void SetResult(TestEngineResult result)
        {
            Guard.ArgumentNotNull(result, "result");
            Guard.OperationValid(_result == null, "Cannot set the Result of an TestRun more than once");
            
            _result = result;
            _waitHandle.Set();
        }

        /// <summary>
        /// Blocks the current thread until the current test run completes
        /// or the timeout is reached
        /// </summary>
        /// <param name="timeout">A <see cref="T:System.Int32"/> that represents the number of milliseconds to wait, or -1 milliseconds to wait indefinitely. </param>
        /// <returns>True if the run completed</returns>
        public bool Wait(int timeout)
        {
            return _waitHandle.WaitOne(timeout);
        }

        /// <summary>
        /// True if the test run has completed
        /// </summary>
        public bool IsComplete { get { return _result != null; } }

        XmlNode ITestRun.Result
        {
            get { return EngineResult.Xml; }
        }

        bool ITestRun.Wait(int timeout)
        {
            return Wait(timeout);
        }
    }
}
