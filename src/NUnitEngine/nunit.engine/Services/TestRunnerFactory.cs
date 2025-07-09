// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using System;
using System.Collections;
using System.IO;
using NUnit.Common;
using NUnit.Engine.Runners;

namespace NUnit.Engine.Services
{
    /// <summary>
    /// TestRunnerFactory handles creation of a suitable test
    /// runner for a given package to be loaded and run either in a
    /// separate process or within the same process.
    /// </summary>
    public class TestRunnerFactory : Service, ITestRunnerFactory
    {
        private IProjectService? _projectService;

        public override void StartService()
        {
            try
            {
                if (ServiceContext is null)
                    throw new InvalidOperationException("Only services that have a ServiceContext can be started.");

                // TestRunnerFactory requires the ProjectService
                _projectService = ServiceContext.GetService<IProjectService>();

                // Anything returned from ServiceContext is known to be an IService
                Status = ((IService)_projectService).Status == ServiceStatus.Started
                    ? ServiceStatus.Started
                    : ServiceStatus.Error;
            }
            catch
            {
                Status = ServiceStatus.Error;
                throw;
            }
        }

        /// <summary>
        /// Returns a test runner based on the settings in a TestPackage.
        /// Any setting that is "consumed" by the factory is removed, so
        /// that downstream runners using the factory will not repeatedly
        /// create the same type of runner.
        /// </summary>
        /// <param name="package">The TestPackage to be loaded and run</param>
        /// <returns>A TestRunner</returns>
        public ITestEngineRunner MakeTestRunner(TestPackage package)
        {
            if (ServiceContext is null)
                throw new InvalidOperationException("ServiceContext not set.");

            // Any package without subpackages is either an assembly or unknown.
            // If it's unknown, that will be found out we try to load it.
            var leafPackages = package.Select(p => !p.HasSubPackages());
            var firstOrOnlyPackage = leafPackages[0];

#if NETFRAMEWORK
            if (leafPackages.Count > 1)
                return new MultipleTestProcessRunner(ServiceContext, package);

            return MakePseudoRunnerIfNeeded(firstOrOnlyPackage) ?? new ProcessRunner(ServiceContext, firstOrOnlyPackage);
#else
            // TODO: Currently, the .NET Core runner doesn't support multiple assemblies.
            // We therefore only properly deal with the situation where a single assembly
            // package is provided. This could change. :-)
            return MakePseudoRunnerIfNeeded(firstOrOnlyPackage) ?? new LocalTestRunner(firstOrOnlyPackage);
#endif
        }

        /// <summary>
        /// Check for various package errors and return a pseudo-runner if needed, otherwise null
        /// </summary>
        private static ITestEngineRunner? MakePseudoRunnerIfNeeded(TestPackage package)
        {
            string assemblyPath = package.FullName.ShouldNotBeNull();
            if (!File.Exists(assemblyPath))
                return new InvalidAssemblyTestRunner(assemblyPath, $"File not found: {assemblyPath}");

            if (package.Settings.GetValueOrDefault(SettingDefinitions.ImageTargetFrameworkName).StartsWith("Unmanaged,"))
                return new UnmanagedExecutableTestRunner(package.FullName ?? "Package Suite");

            return null;
        }
    }
}
