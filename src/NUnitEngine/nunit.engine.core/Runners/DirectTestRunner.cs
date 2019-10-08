// ***********************************************************************
// Copyright (c) 2011-2014 Charlie Poole, Rob Prouse
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

using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Engine.Extensibility;
using NUnit.Engine.Internal;

namespace NUnit.Engine.Runners
{
    /// <summary>
    /// DirectTestRunner is the abstract base for runners
    /// that deal directly with a framework driver.
    /// </summary>
    public abstract class DirectTestRunner : AbstractTestRunner
    {
        // DirectTestRunner loads and runs tests in a particular AppDomain using
        // one driver per assembly. All test assemblies are ultimately executed by
        // one of the derived classes of DirectTestRunner, either LocalTestRunner
        // or TestDomainRunner.
        //
        // DirectTestRunner creates an appropriate framework driver for each assembly
        // included in the TestPackage. All frameworks loaded by the same DirectRunner
        // must be compatible, i.e. runnable within the same AppDomain.
        // 
        // DirectTestRunner is used in the engine/runner process as well as in agent
        // processes. It may be called with a TestPackage that specifies a single 
        // assembly, multiple assemblies, a single project, multiple projects or
        // a mix of projects and assemblies. This variety of potential package
        // inputs complicates things. It arises from the fact that NUnit permits 
        // the caller to specify that all projects and assemblies should be loaded 
        // in the same AppDomain.
        //
        // TODO: When there are projects included in the TestPackage, DirectTestRUnner
        // should create intermediate result nodes for each project.
        //
        // TODO: We really should detect and give a meaningful message if the user 
        // tries to load incopatible frameworks in the same AppDomain.

        private readonly List<IFrameworkDriver> _drivers = new List<IFrameworkDriver>();

#if !NETSTANDARD1_6
        private ProvidedPathsAssemblyResolver _assemblyResolver;

        protected AppDomain TestDomain { get; set; }
#endif

        public DirectTestRunner(IServiceLocator services, TestPackage package) : base(services, package)
        {
#if !NETSTANDARD1_6
            // Bypass the resolver if not in the default AppDomain. This prevents trying to use the resolver within
            // NUnit's own automated tests (in a test AppDomain) which does not make sense anyway.
            if (AppDomain.CurrentDomain.IsDefaultAppDomain())
            {
                _assemblyResolver = new ProvidedPathsAssemblyResolver();
                _assemblyResolver.Install();
            }
#endif
        }

        /// <summary>
        /// Explores a previously loaded TestPackage and returns information
        /// about the tests found.
        /// </summary>
        /// <param name="filter">The TestFilter to be used to select tests</param>
        /// <returns>
        /// A TestEngineResult.
        /// </returns>
        public override TestEngineResult Explore(TestFilter filter)
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
        protected override TestEngineResult LoadPackage()
        {
            var result = new TestEngineResult();

            // DirectRunner may be called with a single-assembly package,
            // a set of assemblies as subpackages or even an arbitrary
            // hierarchy of packages and subpackages with assemblies
            // found in the terminal nodes.
            var packagesToLoad = TestPackage.Select(p => !p.HasSubPackages());

            var driverService = Services.GetService<IDriverService>();

            foreach (var subPackage in packagesToLoad)
            {
                var testFile = subPackage.FullName;

                string targetFramework = subPackage.GetSetting(InternalEnginePackageSettings.ImageTargetFrameworkName, (string)null);
                bool skipNonTestAssemblies = subPackage.GetSetting(EnginePackageSettings.SkipNonTestAssemblies, false);

#if !NETSTANDARD1_6
                if (_assemblyResolver != null && !TestDomain.IsDefaultAppDomain()
                    && subPackage.GetSetting(InternalEnginePackageSettings.ImageRequiresDefaultAppDomainAssemblyResolver, false))
                {
                    // It's OK to do this in the loop because the Add method
                    // checks to see if the path is already present.
                    _assemblyResolver.AddPathFromFile(testFile);
                }

                IFrameworkDriver driver = driverService.GetDriver(TestDomain, testFile, targetFramework, skipNonTestAssemblies);
#else
                IFrameworkDriver driver = driverService.GetDriver(testFile, skipNonTestAssemblies);
#endif
                driver.ID = TestPackage.ID;
                result.Add(LoadDriver(driver, testFile, subPackage));
                _drivers.Add(driver);
            }
            return result;
        }

        private static string LoadDriver(IFrameworkDriver driver, string testFile, TestPackage subPackage)
        {
            try
            {
                return driver.Load(testFile, subPackage.Settings);
            }
            catch (Exception ex) when (!(ex is NUnitEngineException))
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
        public override int CountTestCases(TestFilter filter)
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
        protected override TestEngineResult RunTests(ITestEventListener listener, TestFilter filter)
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

#if !NETSTANDARD1_6
            if (_assemblyResolver != null)
            {
                foreach (var package in TestPackage.Select(p => p.IsAssemblyPackage()))
                    _assemblyResolver.RemovePathFromFile(package.FullName);
            }
#endif
            return result;
        }

        /// <summary>
        /// Cancel the ongoing test run. If no  test is running, the call is ignored.
        /// </summary>
        /// <param name="force">If true, cancel any ongoing test threads, otherwise wait for them to complete.</param>
        public override void StopRun(bool force)
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
                LoadResult = LoadPackage();
        }
    }
}