// ***********************************************************************
// Copyright (c) 2011-2014 Charlie Poole
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

using System.Collections.Generic;
using NUnit.Engine.Internal;

namespace NUnit.Engine.Runners
{
    /// <summary>
    /// AggregatingTestRunner runs tests using multiple
    /// subordinate runners and combines the results.
    /// The individual runners may be run in parallel
    /// if a derived class sets the LevelOfParallelism
    /// property in its constructor.
    /// </summary>
    public class AggregatingTestRunner : AbstractTestRunner
    {
        // The runners created by the derived class will (at least at the time
        // of writing this comment) be either TestDomainRunners or ProcessRunners.
        private List<ITestEngineRunner> _runners;

        // Public for testing purposes
        public virtual int LevelOfParallelism
        {
            get { return 1; }
        }

        // Exposed for use by tests
        public IList<ITestEngineRunner> Runners
        {
            get
            {
                if (_runners == null)
                {
                    _runners = new List<ITestEngineRunner>();
                    foreach (var subPackage in TestPackage.SubPackages)
                    {
                        _runners.Add(CreateRunner(subPackage));
                    }
                }

                return _runners;
            }
        }

#if NETSTANDARD1_3
        public AggregatingTestRunner(TestPackage package) : base(package)
        {
        }
#else
        public AggregatingTestRunner(IServiceLocator services, TestPackage package) : base(services, package)
        {
        }
#endif

        #region AbstractTestRunner Overrides

        /// <summary>
        /// Explore a TestPackage and return information about
        /// the tests found.
        /// </summary>
        /// <param name="filter">A TestFilter used to select tests</param>
        /// <returns>A TestEngineResult.</returns>
        public override TestEngineResult Explore(TestFilter filter)
        {
            var results = new List<TestEngineResult>();

            foreach (ITestEngineRunner runner in Runners)
                results.Add(runner.Explore(filter));

            return ResultHelper.Merge(results);
        }

        /// <summary>
        /// Load a TestPackage for possible execution
        /// </summary>
        /// <returns>A TestEngineResult.</returns>
        protected override TestEngineResult LoadPackage()
        {
            var results = new List<TestEngineResult>();

            var packages = new List<TestPackage>(TestPackage.SubPackages);
            if (packages.Count == 0)
                packages.Add(TestPackage);

            foreach (var runner in Runners)
                results.Add(runner.Load());

            return ResultHelper.Merge(results);
        }

        /// <summary>
        /// Unload any loaded TestPackages.
        /// </summary>
        public override void UnloadPackage()
        {
            foreach (ITestEngineRunner runner in Runners)
                runner.Unload();
        }

        /// <summary>
        /// Count the test cases that would be run under
        /// the specified filter.
        /// </summary>
        /// <param name="filter">A TestFilter</param>
        /// <returns>The count of test cases</returns>
        public override int CountTestCases(TestFilter filter)
        {
            int count = 0;

            foreach (ITestEngineRunner runner in Runners)
                count += runner.CountTestCases(filter);

            return count;
        }

        /// <summary>
        /// Run the tests in a loaded TestPackage
        /// </summary>
        /// <param name="listener">An ITestEventHandler to receive events</param>
        /// <param name="filter">A TestFilter used to select tests</param>
        /// <returns>
        /// A TestEngineResult giving the result of the test execution.
        /// </returns>
        protected override TestEngineResult RunTests(ITestEventListener listener, TestFilter filter)
        {
            var results = new List<TestEngineResult>();

            bool disposeRunners = TestPackage.GetSetting(EnginePackageSettings.DisposeRunners, false);

            if (LevelOfParallelism <= 1)
            {
                foreach (ITestEngineRunner runner in Runners)
                {
                    results.Add(runner.Run(listener, filter));
                    if (disposeRunners) runner.Dispose();
                }
            }
            else
            {
                var workerPool = new ParallelTaskWorkerPool(LevelOfParallelism);
                var tasks = new List<TestExecutionTask>();

                foreach (ITestEngineRunner runner in Runners)
                {
                    var task = new TestExecutionTask(runner, listener, filter, disposeRunners);
                    tasks.Add(task);
                    workerPool.Enqueue(task);
                }

                workerPool.Start();
                workerPool.WaitAll();

                foreach (var task in tasks)
                    results.Add(task.Result());
            }

            if (disposeRunners) Runners.Clear();

            return ResultHelper.Merge(results);
        }

        /// <summary>
        /// Cancel the ongoing test run. If no  test is running, the call is ignored.
        /// </summary>
        /// <param name="force">If true, cancel any ongoing test threads, otherwise wait for them to complete.</param>
        public override void StopRun(bool force)
        {
            foreach (var runner in Runners)
                runner.StopRun(force);
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            foreach (var runner in Runners)
                runner.Dispose();

            Runners.Clear();
        }

        #endregion

        protected virtual ITestEngineRunner CreateRunner(TestPackage package)
        {
            return TestRunnerFactory.MakeTestRunner(package);
        }
    }
}
