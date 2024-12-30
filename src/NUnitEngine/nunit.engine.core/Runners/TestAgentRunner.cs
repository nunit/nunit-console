// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using System;
using System.Collections.Generic;
using System.ComponentModel;
using NUnit.Common;
using NUnit.Engine.Drivers;
using NUnit.Engine.Extensibility;
using NUnit.Engine.Internal;

namespace NUnit.Engine.Runners
{
    /// <summary>
    /// TestAgentRunner is the abstract base for runners used by agents, which
    /// deal directly with a framework driver. It loads and runs tests in a single 
    /// assembly, creating an <see cref="IFrameworkDriver"/> to do so.
    /// </summary>
    public abstract class TestAgentRunner : ITestEngineRunner
    {
        private readonly List<IFrameworkDriver> _drivers = new List<IFrameworkDriver>();

        private readonly ProvidedPathsAssemblyResolver? _assemblyResolver;

        protected AppDomain? TestDomain { get; set; }

        // Used to inject DriverService for testing
        internal IDriverService? DriverService { get; set; }

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
            get { return LoadResult != null; }
        }

        public TestAgentRunner(TestPackage package)
        {
            Guard.ArgumentNotNull(package, nameof(package));
            var assemblyPackages = package.Select(p => !p.HasSubPackages());
            Guard.ArgumentValid(assemblyPackages.Count == 1, "TestAgentRunner requires a package with a single assembly", nameof(package));

            TestPackage = assemblyPackages[0];

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
            EnsurePackageIsLoaded();

            var result = new TestEngineResult();

            foreach (IFrameworkDriver driver in _drivers)
            {
                string driverResult;

                try
                {
                    driverResult = driver.Explore(filter.Text);
                }
                catch (Exception ex) when (!(ex is NUnitEngineException))
                {
                    throw new NUnitEngineException("An exception occurred in the driver while exploring tests.", ex);
                }

                result.Add(driverResult);
            }

            return result;
        }

        /// <summary>
        /// Load a TestPackage for exploration or execution
        /// </summary>
        /// <returns>A TestEngineResult.</returns>
        public virtual TestEngineResult Load()
        {
            Guard.OperationValid(TestDomain != null, "TestDomain is not set");
            AppDomain testDomain = TestDomain!;

            var result = new TestEngineResult();

            // DirectRunner may be called with a single-assembly package,
            // a set of assemblies as subpackages or even an arbitrary
            // hierarchy of packages and subpackages with assemblies
            // found in the terminal nodes.
            var packagesToLoad = TestPackage.Select(p => !p.HasSubPackages());

            if (DriverService == null)
                DriverService = new DriverService();

            _drivers.Clear();

            foreach (var subPackage in packagesToLoad)
            {
                var testFile = subPackage.FullName!; // We know it's an assembly

                string? targetFramework = subPackage.GetSetting(InternalEnginePackageSettings.ImageTargetFrameworkName, (string?)null);
                bool skipNonTestAssemblies = subPackage.GetSetting(EnginePackageSettings.SkipNonTestAssemblies, false);

                if (_assemblyResolver != null && !testDomain.IsDefaultAppDomain()
                    && subPackage.GetSetting(InternalEnginePackageSettings.ImageRequiresDefaultAppDomainAssemblyResolver, false))
                {
                    // It's OK to do this in the loop because the Add method
                    // checks to see if the path is already present.
                    _assemblyResolver.AddPathFromFile(testFile);
                }

                IFrameworkDriver driver = DriverService.GetDriver(testDomain, testFile, targetFramework, skipNonTestAssemblies);

                driver.ID = subPackage.ID;
                result.Add(LoadDriver(driver, testFile, subPackage));
                _drivers.Add(driver);
            }
            return result;
        }

        public virtual void Unload() { }
        public TestEngineResult Reload() => Load();

        private static string LoadDriver(IFrameworkDriver driver, string testFile, TestPackage subPackage)
        {
            try
            {
                return driver.Load(testFile, subPackage.Settings);
            }
            catch (Exception ex) when (ex is not NUnitEngineException)
            {
                throw new NUnitEngineException("An exception occurred in the driver while loading tests.", ex);
            }
        }

        /// <summary>
        /// Count the test cases that would be run under
        /// the specified filter.
        /// </summary>
        /// <param name="filter">A TestFilter</param>
        /// <returns>The count of test cases</returns>
        public int CountTestCases(TestFilter filter)
        {
            EnsurePackageIsLoaded();

            int count = 0;

            foreach (IFrameworkDriver driver in _drivers)
            {
                try
                {
                    count += driver.CountTestCases(filter.Text);
                }
                catch (Exception ex) when (!(ex is NUnitEngineException))
                {
                    throw new NUnitEngineException("An exception occurred in the driver while counting test cases.", ex);
                }
            }

            return count;
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
            EnsurePackageIsLoaded();

            var result = new TestEngineResult();

            foreach (IFrameworkDriver driver in _drivers)
            {
                string driverResult;

                try
                {
                    driverResult = driver.Run(listener, filter.Text);
                }
                catch (Exception ex) when (!(ex is NUnitEngineException))
                {
                    throw new NUnitEngineException("An exception occurred in the driver while running tests.", ex);
                }

                result.Add(driverResult);
            }

            if (_assemblyResolver != null)
            {
                foreach (var package in TestPackage.Select(p => p.IsAssemblyPackage()))
                    _assemblyResolver.RemovePathFromFile(package.FullName!); // IsAssemblyPackage guarantees FullName is not null
            }

            return result;
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
        /// Cancel the ongoing test run. If no  test is running, the call is ignored.
        /// </summary>
        /// <param name="force">If true, cancel any ongoing test threads, otherwise wait for them to complete.</param>
        public void StopRun(bool force)
        {
            EnsurePackageIsLoaded();

            foreach (IFrameworkDriver driver in _drivers)
            {
                try
                {
                    driver.StopRun(force);
                }
                catch (Exception ex) when (!(ex is NUnitEngineException))
                {
                    throw new NUnitEngineException("An exception occurred in the driver while stopping the run.", ex);
                }
            }
        }

        private void EnsurePackageIsLoaded()
        {
            if (!IsPackageLoaded)
                LoadResult = Load();
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