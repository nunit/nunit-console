// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

#if NETFRAMEWORK
using System;
using System.Collections.Generic;

namespace NUnit.Engine.Runners
{
    /// <summary>
    /// AggregatingTestRunner runs tests using multiple subordinate runners
    /// and combines the results. The individual runners may be run in parallel
    /// if a derived class sets the LevelOfParallelism
    /// property in its constructor.
    /// </summary>
    /// <remarks>
    /// AggregatingTestRunner may be called with a TestPackage that specifies a single
    /// assembly, multiple assemblies, a single project, multiple projects or any
    /// combination of projects and asemblies. In all cases, it extracts a list of the
    /// actual assemblies to be run and creates a separate runner for each of them.
    /// </remarks>
    public class AggregatingTestRunner : TestEngineRunner
    {
        private static readonly Logger log = InternalTrace.GetLogger(typeof(AggregatingTestRunner));

        // TODO: Determine whether AggregatingTestRunner needs to create an XML result
        // node for a project or if that responsibility can be delegated to the individual
        // runners it creates.
        private List<ITestEngineRunner>? _runners;

        // Exceptions from unloading individual runners are caught and rethrown
        // on AggregatingTestRunner disposal, to allow TestResults to be
        // written and execution of other runners to continue.
        private readonly List<Exception> _unloadExceptions = new List<Exception>();

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
                if (_runners is null)
                {
                    _runners = new List<ITestEngineRunner>();
                    foreach (var subPackage in TestPackage.Select(p => !p.HasSubPackages()))
                    {
                        var runner = CreateRunner(subPackage);
                        log.Debug($"Using {runner.GetType()} for {subPackage.Name}");
                        _runners.Add(runner);
                    }
                }

                return _runners;
            }
        }

        public AggregatingTestRunner(IServiceLocator services, TestPackage package) : base(services, package)
        {
            Guard.ArgumentValid(TestRunnerFactory is not null, "TestRunnerFactory service not available", nameof(services));
        }

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
            {
                try
                {
                    runner.Unload();
                }
                catch (Exception e)
                {
                    _unloadExceptions.Add(e);
                }
            }
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

            bool disposeRunners = TestPackage.GetSetting(PackageSettings.DisposeRunners.Name, false);

            if (LevelOfParallelism <= 1)
            {
                RunTestsSequentially(listener, filter, results, disposeRunners);
            }
            else
            {
                RunTestsInParallel(listener, filter, results, disposeRunners);
            }

            if (disposeRunners)
                Runners.Clear();

            return ResultHelper.Merge(results);
        }

        private void RunTestsSequentially(ITestEventListener listener, TestFilter filter, List<TestEngineResult> results, bool disposeRunners)
        {
            log.Debug("Running test assemblies sequentially.");

            foreach (ITestEngineRunner runner in Runners)
            {
                var task = new TestExecutionTask(runner, listener, filter, disposeRunners);
                task.Execute();
                LogResultsFromTask(task, results, _unloadExceptions);
            }
        }

        private void RunTestsInParallel(ITestEventListener listener, TestFilter filter, List<TestEngineResult> results, bool disposeRunners)
        {
            log.Debug("Running test assemblies in parallel.");

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
                LogResultsFromTask(task, results, _unloadExceptions);
        }

        /// <summary>
        /// Request the current test run to stop. If no tests are running,
        /// the call is ignored.
        /// </summary>
        public override void RequestStop()
        {
            foreach (var runner in Runners)
                runner.RequestStop();
        }

        /// <summary>
        /// Force the current test run to stop, killing threads or processes if necessary.
        /// If no tests are running, the call is ignored.
        /// </summary>
        public override void ForcedStop()
        {
            foreach (var runner in Runners)
                runner.ForcedStop();
        }
        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            foreach (var runner in Runners)
            {
                try
                {
                    runner.Dispose();
                }
                catch (Exception e)
                {
                    _unloadExceptions.Add(e);
                }
            }

            Runners.Clear();

            if (_unloadExceptions.Count > 0)
                throw new NUnitEngineUnloadException(_unloadExceptions);
        }

        // Use TestRunnerFactory to decide the type of runner to be used for
        // each individual assembly. This may be overridden by a derived class.
        protected virtual ITestEngineRunner CreateRunner(TestPackage package)
        {
            return TestRunnerFactory.MakeTestRunner(package);
        }

        private static void LogResultsFromTask(TestExecutionTask task, List<TestEngineResult> results, List<Exception> unloadExceptions)
        {
            var result = task.Result;
            if (result is not null)
            {
                results.Add(result);
            }

            if (task.UnloadException is not null)
            {
                unloadExceptions.Add(task.UnloadException);
            }
        }
    }
}
#endif