﻿// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using System;
using System.Collections.Generic;
using NUnit.Engine.Internal;

using NUnit.Common;

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
        // AggregatingTestRunner combines the results from tests run by different
        // runners. It may be used as a base class or through one of it's derived
        // classes, MultipleTestDomainRunner and MultipleTestProcessRunner.
        //
        // The runners created by the derived class will (at least at the time
        // of writing this comment) be either DirectTestRunners, ProcessRunners or
        // AggregatingTestRunners.
        //
        // AggregatingTestRunner is used in the engine/runner process as well as in agent
        // processes. It may be called with a TestPackage that specifies a single 
        // assembly, multiple assemblies, a single project, multiple projects or
        // a mix of projects and assemblies. Each file passed is handled by
        // a single runner.
        //
        // TODO: Determine whether AggregatingTestRunner needs to create an XML result
        // node for a project or if that responsibility can be delegated to the individual
        // runners it creates.
        private List<ITestEngineRunner> _runners;

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
                if (_runners == null)
                {
                    _runners = new List<ITestEngineRunner>();
                    foreach (var subPackage in TestPackage.Select(p => !p.HasSubPackages()))
                    {
                        _runners.Add(CreateRunner(subPackage));
                    }
                }

                return _runners;
            }
        }

        public AggregatingTestRunner(IServiceLocator services, TestPackage package) : base(services, package)
        {
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

            bool disposeRunners = TestPackage.GetSetting(EnginePackageSettings.DisposeRunners, false);

            if (LevelOfParallelism <= 1)
            {
                RunTestsSequentially(listener, filter, results, disposeRunners);
            }
            else
            {
                RunTestsInParallel(listener, filter, results, disposeRunners);
            }

            if (disposeRunners) Runners.Clear();

            return ResultHelper.Merge(results);
        }

        private void RunTestsSequentially(ITestEventListener listener, TestFilter filter, List<TestEngineResult> results, bool disposeRunners)
        {
            foreach (ITestEngineRunner runner in Runners)
            {
                var task = new TestExecutionTask(runner, listener, filter, disposeRunners);
                task.Execute();
                LogResultsFromTask(task, results, _unloadExceptions);
            }
        }

        private void RunTestsInParallel(ITestEventListener listener, TestFilter filter, List<TestEngineResult> results, bool disposeRunners)
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

        protected virtual ITestEngineRunner CreateRunner(TestPackage package)
        {
            return TestRunnerFactory.MakeTestRunner(package);
        }

        private static void LogResultsFromTask(TestExecutionTask task, List<TestEngineResult> results, List<Exception> unloadExceptions)
        {
            var result = task.Result;
            if (result != null)
            {
                results.Add(result);
            }

            if (task.UnloadException != null)
            {
                unloadExceptions.Add(task.UnloadException);
            }
        }
    }
}
