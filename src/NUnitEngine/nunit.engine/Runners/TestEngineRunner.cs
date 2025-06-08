﻿// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace NUnit.Engine.Runners
{
    /// <summary>
    /// TestEngineRunner is the base class for all TestEngineRunners
    /// within the NUnit Engine itself.
    /// </summary>
    public abstract class TestEngineRunner : ITestEngineRunner
    {
        public TestEngineRunner(IServiceLocator services, TestPackage package)
        {
            Guard.ArgumentNotNull(services, nameof(package));
            Guard.ArgumentNotNull(package, nameof(package));

            TestPackages = package.Select(p => !p.HasSubPackages());
            TestPackage = TestPackages.Count == 1
                ? TestPackages[0]
                : package;
            Services = services;
            TestRunnerFactory = Services.GetService<ITestRunnerFactory>();
        }

        /// <summary>
        /// The top-level TestPackage for which this is the runner
        /// </summary>
        protected TestPackage TestPackage { get; }

        /// <summary>
        /// A list of leaf packages, expected to be assemblies.
        /// </summary>
        protected IList<TestPackage> TestPackages { get; }

        /// <summary>
        /// Our Service Context
        /// </summary>
        protected IServiceLocator Services { get; }

        protected ITestRunnerFactory TestRunnerFactory { get; }

        /// <summary>
        /// The result of the last call to LoadPackage
        /// </summary>
        protected TestEngineResult? LoadResult { get; set; }

        /// <summary>
        /// Gets an indicator of whether the package has been loaded.
        /// </summary>
        public bool IsPackageLoaded => LoadResult is not null;

        /// <summary>
        /// Loads the TestPackage for exploration or execution.
        /// </summary>
        /// <returns>A TestEngineResult.</returns>
        protected abstract TestEngineResult LoadPackage();

        /// <summary>
        /// Reload the currently loaded test package. Overridden
        /// in derived classes to take any additional action.
        /// </summary>
        /// <returns>A TestEngineResult.</returns>
        protected virtual TestEngineResult ReloadPackage()
        {
            return LoadPackage();
        }

        /// <summary>
        /// Unload any loaded TestPackage. Overridden in
        /// derived classes to take any necessary action.
        /// </summary>
        public virtual void UnloadPackage()
        {
        }

        /// <summary>
        /// Run the tests in the loaded TestPackage.
        /// </summary>
        /// <param name="listener">An ITestEventHandler to receive events</param>
        /// <param name="filter">A TestFilter used to select tests</param>
        /// <returns>A TestEngineResult giving the result of the test execution</returns>
        protected abstract TestEngineResult RunTests(ITestEventListener listener, TestFilter filter);

        /// <summary>
        /// Start a run of the tests in the loaded TestPackage, returning immediately.
        /// The tests are run asynchronously and the listener interface is notified
        /// as it progresses.
        /// </summary>
        /// <param name="listener">An ITestEventHandler to receive events</param>
        /// <param name="filter">A TestFilter used to select tests</param>
        /// <returns>An <see cref="AsyncTestEngineResult"/> that will provide the result of the test execution</returns>
        protected virtual AsyncTestEngineResult RunTestsAsync(ITestEventListener listener, TestFilter filter)
        {
            var testRun = new AsyncTestEngineResult();

            using (var worker = new BackgroundWorker())
            {
                worker.DoWork += (s, ea) =>
                {
                    var result = RunTests(listener, filter);
                    testRun.SetResult(result);
                };
                worker.RunWorkerAsync();
            }

            return testRun;
        }

        /// <summary>
        /// Request the current test run to stop. If no tests are running,
        /// the call is ignored.
        /// </summary>
        public abstract void RequestStop();

        /// <summary>
        /// Force the current test run to stop, killing threads or processes if necessary.
        /// If no tests are running, the call is ignored.
        /// </summary>
        public abstract void ForcedStop();

        /// <summary>
        /// Explores the TestPackage and returns information about
        /// the tests found. Loads the package if not done previously.
        /// </summary>
        /// <param name="filter">The TestFilter to be used to select tests</param>
        /// <returns>A TestEngineResult.</returns>
        public abstract TestEngineResult Explore(TestFilter filter);

        /// <summary>
        /// Loads the TestPackage for exploration or execution, saving the result.
        /// </summary>
        /// <returns>A TestEngineResult.</returns>
        public TestEngineResult Load()
        {
            return LoadResult = LoadPackage();
        }

        /// <summary>
        /// Reload the currently loaded test package, saving the result.
        /// </summary>
        /// <returns>A TestEngineResult.</returns>
        /// <exception cref="InvalidOperationException">If no package has been loaded</exception>
        public TestEngineResult Reload()
        {
            if (TestPackage is null)
                throw new InvalidOperationException("MasterTestRunner: Reload called before Load");

            return LoadResult = ReloadPackage();
        }

        /// <summary>
        /// Unload any loaded TestPackage.
        /// </summary>
        public void Unload()
        {
            UnloadPackage();
            LoadResult = null;
        }

        /// <summary>
        /// Count the test cases that would be run under the specified
        /// filter, loading the TestPackage if it is not already loaded.
        /// </summary>
        /// <param name="filter">A TestFilter</param>
        /// <returns>The count of test cases.</returns>
        public abstract int CountTestCases(TestFilter filter);

        /// <summary>
        /// Run the tests in the TestPackage, loading the package
        /// if this has not already been done.
        /// </summary>
        /// <param name="listener">An ITestEventHandler to receive events</param>
        /// <param name="filter">A TestFilter used to select tests</param>
        /// <returns>A TestEngineResult giving the result of the test execution</returns>
        public TestEngineResult Run(ITestEventListener listener, TestFilter filter)
        {
            return RunTests(listener, filter);
        }

        /// <summary>
        /// Start a run of the tests in the loaded TestPackage. The tests are run
        /// asynchronously and the listener interface is notified as it progresses.
        /// </summary>
        /// <param name="listener">An ITestEventHandler to receive events</param>
        /// <param name="filter">A TestFilter used to select tests</param>
        /// <returns>An <see cref="AsyncTestEngineResult"/> that will provide the result of the test execution</returns>
        public AsyncTestEngineResult RunAsync(ITestEventListener listener, TestFilter filter)
        {
            return RunTestsAsync(listener, filter);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected bool _disposed = false;

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                    Unload();

                _disposed = true;
            }
        }
    }
}
