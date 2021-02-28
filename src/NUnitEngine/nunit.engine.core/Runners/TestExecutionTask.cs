// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using System;
using NUnit.Common;

namespace NUnit.Engine.Runners
{
    public class TestExecutionTask : ITestExecutionTask
    {
        private readonly ITestEngineRunner _runner;
        private readonly ITestEventListener _listener;
        private readonly TestFilter _filter;
        private volatile TestEngineResult _result;
        private readonly bool _disposeRunner;
        private bool _hasExecuted = false;
        private Exception _unloadException;

        public TestExecutionTask(ITestEngineRunner runner, ITestEventListener listener, TestFilter filter, bool disposeRunner)
        {
            _disposeRunner = disposeRunner;
            _filter = filter;
            _listener = listener;
            _runner = runner;
        }

        public void Execute()
        {
            _hasExecuted = true;
            try
            {
                _result = _runner.Run(_listener, _filter);
            }
            finally
            {
                try
                {
                    if (_disposeRunner)
                        _runner.Dispose();
                }
                catch (Exception e)
                {
                    _unloadException = e;
                }
            }
        }

        public TestEngineResult Result
        {
            get
            {
                Guard.OperationValid(_hasExecuted, "Can not access result until task has been executed");
                return _result;
            }
        }

        /// <summary>
        /// Stored exception thrown during test assembly unload.
        /// </summary>
        public Exception UnloadException
        {
            get
            {
                Guard.OperationValid(_hasExecuted, "Can not access thrown exceptions until task has been executed");
                return _unloadException;
            }
        }
    }
}
