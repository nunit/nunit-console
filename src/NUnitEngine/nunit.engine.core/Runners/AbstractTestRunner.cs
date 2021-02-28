// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using System;
using System.ComponentModel;
using NUnit.Engine.Services;

namespace NUnit.Engine.Runners
{
    /// <summary>
    /// AbstractTestRunner is the base class for all internal runners
    /// within the NUnit Engine. It implements the ITestEngineRunner
    /// interface, which uses TestEngineResults to communicate back
    /// to higher level runners.
    /// </summary>
    public abstract class AbstractTestRunner : ITestEngineRunner
    {
        public AbstractTestRunner(IServiceLocator services, TestPackage package)
        {
            Services = services;
            TestRunnerFactory = Services.GetService<ITestRunnerFactory>();
            TestPackage = package;
        }

        /// <summary>
        /// Our Service Context
        /// </summary>
        protected IServiceLocator Services { get; private set; }

        protected ITestRunnerFactory TestRunnerFactory { get; private set; }

        /// <summary>
        /// The TestPackage for which this is the runner
        /// </summary>
        protected TestPackage TestPackage { get; set; }

        /// <summary>
        /// The result of the last call to LoadPackage
        /// </summary>
        protected TestEngineResult LoadResult { get; set; }

        /// <summary>
        /// Gets an indicator of whether the package has been loaded.
        /// </summary>
        public bool IsPackageLoaded
        {
            get { return LoadResult != null;  }
        }

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
        /// Cancel the ongoing test run. If no  test is running, the call is ignored.
        /// </summary>
        /// <param name="force">If true, cancel any ongoing test threads, otherwise wait for them to complete.</param>
        public abstract void StopRun(bool force);

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
            if (this.TestPackage == null)
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
