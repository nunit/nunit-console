// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using System;
using System.Diagnostics.CodeAnalysis;
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

            if (package.GetSetting(PackageSettings.ImageTargetFrameworkName.Name, string.Empty).StartsWith("Unmanaged,"))
                return new UnmanagedExecutableTestRunner(package.FullName ?? "Package Suite");

#if NETFRAMEWORK
            bool isNested = false;
            foreach (TestPackage subPackage in package.SubPackages)
            {
                if (subPackage.SubPackages.Count > 0)
                {
                    isNested = true;
                    break;
                }
            }

            // TODO: A package is "nested" if any subpackages also have subpackages.
            // In some cases, the maxagents setting could vary in different subpackages.
            // In that case, we are currently running the individual subpackages
            // sequentially but running agents in parallel across each subpackage.
            // We should decide whether we really need to support maxagents on
            // subpackages and exactly how we want it to work if we do support it.
            // An "ideal" solution may be to require subordinate maxagents to
            // add up to the top level maxagents and to enforce the value at all levels
            // simultaneously. But this is rather complicated and may not be worth it.
            if (isNested)
                return new AggregatingTestRunner(ServiceContext, package);
            else if (package.SubPackages.Count > 1)
                return new MultipleTestProcessRunner(ServiceContext, package);
            else
                return new ProcessRunner(ServiceContext, package);
#else
            // TODO: Currently, the .NET Core runner doesn't support multiple assemblies.
            // We therefore only properly deal with the situation where a single assembly
            // package is provided. This could change. :-)
            // Zero case included but should never occur
            var assemblyPackages = package.Select(p => p.IsAssemblyPackage());

            switch (assemblyPackages.Count)
            {
                case 1:
                    return new LocalTestRunner(assemblyPackages[0]);
                case 0:
                    return new LocalTestRunner(package);
                default:
                    throw new InvalidOperationException("Multi-assembly packages are not supported under .NET Core");
            }
#endif
        }
    }
}
