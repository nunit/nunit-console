// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using System;
using System.ComponentModel;
using System.Linq;
using NUnit.Engine.Drivers;
using NUnit.Engine.Extensibility;

namespace NUnit.Engine.Runners
{
    /// <summary>
    /// TestAgentRunner is the abstract base for runners used by agents, which
    /// deal directly with a framework driver. It loads and runs tests in a single
    /// assembly, creating an <see cref="IFrameworkDriver"/> to do so.
    /// </summary>
    public abstract class TestAgentRunner : ITestEngineRunner
    {
        private readonly Logger log = InternalTrace.GetLogger(typeof(TestAgentRunner));

        private readonly ProvidedPathsAssemblyResolver? _assemblyResolver;

        private IFrameworkDriver? _driver;

        protected AppDomain? TestDomain { get; set; }

        // Used to inject DriverService for testing
        protected IDriverService? DriverService { get; set; }

        /// <summary>
        /// The TestPackage for which this is the runner
        /// </summary>
        protected TestPackage TestPackage { get; }

        /// <summary>
        /// The result of the last call to Load
        /// </summary>
        protected TestEngineResult? LoadResult { get; set; }

        /// <summary>
        /// Gets an indicator of whether the package has been loaded.
        /// </summary>
        public bool IsPackageLoaded
        {
            get { return LoadResult is not null; }
        }

        public TestAgentRunner(TestPackage package)
        {
            Guard.ArgumentNotNull(package);
            var assemblyPackages = package.Select(p => !p.HasSubPackages());
            Guard.ArgumentValid(assemblyPackages.Count == 1, "TestAgentRunner requires a package with a single assembly", nameof(package));

            TestPackage = package;

            // Bypass the resolver if not in the default AppDomain. This prevents trying to use the resolver within
            // NUnit's own automated tests (in a test AppDomain) which does not make sense anyway.
            if (AppDomain.CurrentDomain.IsDefaultAppDomain())
            {
                _assemblyResolver = new ProvidedPathsAssemblyResolver();
                _assemblyResolver.Install();
            }
        }

        /// <summary>
        /// Explores a previously loaded TestPackage and returns information
        /// about the tests found.
        /// </summary>
        /// <param name="filter">The TestFilter to be used to select tests</param>
        /// <returns>
        /// A TestEngineResult.
        /// </returns>
        public TestEngineResult Explore(TestFilter filter)
        {
            try
            {
                return new TestEngineResult(GetLoadedDriver().Explore(filter.Text));
            }
            catch (Exception ex) when (!(ex is NUnitEngineException))
            {
                throw new NUnitEngineException("An exception occurred in the driver while exploring tests.", ex);
            }
        }

        /// <summary>
        /// Load a TestPackage for exploration or execution
        /// </summary>
        /// <returns>A TestEngineResult.</returns>
        public virtual TestEngineResult Load()
        {
            Guard.OperationValid(TestDomain is not null, "TestDomain is not set");

            var result = new TestEngineResult();

            // The TestAgentRunner constructor guarantees that TestPackage has
            // only a single assembly.
            var assemblyPackage = TestPackage.Select(p => !p.HasSubPackages()).First();

            if (DriverService is null)
                DriverService = new DriverService();

            var testFile = assemblyPackage.FullName!; // We know it's an assembly

            string? targetFramework = assemblyPackage.GetSetting(EnginePackageSettings.ImageTargetFrameworkName, string.Empty);
            bool skipNonTestAssemblies = assemblyPackage.GetSetting(EnginePackageSettings.SkipNonTestAssemblies, false);

            // TODO: Restore this code after changes to PackageSettings implementation
            //if (_assemblyResolver is not null && !TestDomain.IsDefaultAppDomain()
            //    && assemblyPackage.GetSetting(EnginePackageSettings.ImageRequiresDefaultAppDomainAssemblyResolver, false))
            //{
            //    // It's OK to do this in the loop because the Add method
            //    // checks to see if the path is already present.
            //    _assemblyResolver.AddPathFromFile(testFile);
            //}

            _driver = DriverService.GetDriver(TestDomain, assemblyPackage, testFile, targetFramework, skipNonTestAssemblies);

            try
            {
                return new TestEngineResult(_driver.Load(testFile, assemblyPackage.Settings));
            }
            catch (Exception ex) when (ex is not NUnitEngineException)
            {
                throw new NUnitEngineException("An exception occurred in the driver while loading tests.", ex);
            }
        }

        public virtual void Unload()
        {
        }
        public TestEngineResult Reload() => Load();

        /// <summary>
        /// Count the test cases that would be run under
        /// the specified filter.
        /// </summary>
        /// <param name="filter">A TestFilter</param>
        /// <returns>The count of test cases</returns>
        public int CountTestCases(TestFilter filter)
        {
            try
            {
                return GetLoadedDriver().CountTestCases(filter.Text);
            }
            catch (Exception ex) when (!(ex is NUnitEngineException))
            {
                throw new NUnitEngineException("An exception occurred in the driver while counting test cases.", ex);
            }
        }

        /// <summary>
        /// Run the tests in the loaded TestPackage.
        /// </summary>
        /// <param name="listener">An ITestEventHandler to receive events</param>
        /// <param name="filter">A TestFilter used to select tests</param>
        /// <returns>
        /// A TestEngineResult giving the result of the test execution
        /// </returns>
        public TestEngineResult Run(ITestEventListener? listener, TestFilter filter)
        {
            try
            {
                log.Debug($"Running");
                return new TestEngineResult(GetLoadedDriver().Run(listener, filter.Text));
            }
            catch (Exception ex) when (!(ex is NUnitEngineException))
            {
                string msg = "An exception occurred in the driver while running tests.";
                log.Error(msg, ex);
                throw new NUnitEngineException(msg, ex);
            }
        }

        public AsyncTestEngineResult RunAsync(ITestEventListener? listener, TestFilter filter)
        {
            var testRun = new AsyncTestEngineResult();

            using (var worker = new BackgroundWorker())
            {
                worker.DoWork += (s, ea) =>
                {
                    var result = Run(listener, filter);
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
        public void RequestStop() => StopRun(false);

        /// <summary>
        /// Force the current test run to stop, killing threads or processes if necessary.
        /// If no tests are running, the call is ignored.
        /// </summary>
        public void ForcedStop() => StopRun(true);

        private void StopRun(bool force)
        {
            try
            {
                GetLoadedDriver().StopRun(force);
            }
            catch (Exception ex) when (!(ex is NUnitEngineException))
            {
                throw new NUnitEngineException("An exception occurred in the driver while stopping the run.", ex);
            }
        }

        private IFrameworkDriver GetLoadedDriver()
        {
            if (!IsPackageLoaded)
                LoadResult = Load();

            return _driver.ShouldNotBeNull();
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