﻿// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using NUnit.Engine.Internal;
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
        private IProjectService _projectService;

        public override void StartService()
        {
            // TestRunnerFactory requires the ProjectService
            _projectService = ServiceContext.GetService<IProjectService>();

            // Anything returned from ServiceContext is known to be an IService
            Status = _projectService != null && ((IService)_projectService).Status == ServiceStatus.Started
                ? ServiceStatus.Started
                : ServiceStatus.Error;
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
#if !NETFRAMEWORK
            if (package.SubPackages.Count > 1)
                return new AggregatingTestRunner(ServiceContext, package);
            else
            {
                var assemblyPackages = package.Select(p => p.IsAssemblyPackage());
                switch (assemblyPackages.Count)
                {
                    default:
                        return new AggregatingTestRunner(ServiceContext, package);
                    case 1:
                        return new LocalTestRunner(assemblyPackages[0]);
                    case 0:
                        return new LocalTestRunner(package);
                }
            }
#else
            if (package.GetSetting(InternalEnginePackageSettings.ImageTargetFrameworkName, "").StartsWith("Unmanaged,"))
                return new UnmanagedExecutableTestRunner(package.FullName);

            bool isNested = false;
            foreach (TestPackage subPackage in package.SubPackages)
            {
                if (subPackage.SubPackages.Count > 0)
                {
                    isNested = true;
                    break;
                }
            }
            if (isNested)
                return new AggregatingTestRunner(ServiceContext, package);
            else if (package.SubPackages.Count > 1)
                return new MultipleTestProcessRunner(ServiceContext, package);
            else
                return new ProcessRunner(ServiceContext, package);
#endif
        }
    }
}
