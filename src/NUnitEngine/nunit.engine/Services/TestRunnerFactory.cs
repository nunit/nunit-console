// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using System;
using System.IO;
using NUnit.Common;
using NUnit.Engine.Runners;
using TestCentric.Metadata;

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

            // First get subRunners for each leaf package, i.e. any package without
            // subpackages, which will either be assemblies or unknown file types.
            var leafPackages = package.Select(p => !p.HasSubPackages);

#if NETFRAMEWORK
            // TODO: Currently, the .NET Core runner doesn't support multiple assemblies.
            // We therefore only properly deal with the situation where a single assembly
            // package is provided. This could change. :-)
            if (leafPackages.Count > 1)
                return new AggregatingTestRunner(ServiceContext, package);
#endif
            // Find a runner for the first or only leaf package
            package = leafPackages[0];

            string assemblyPath = package.FullName.ShouldNotBeNull();

            if (!File.Exists(assemblyPath))
                return new InvalidAssemblyTestRunner(assemblyPath, $"File not found: {assemblyPath}");
            if (!PathUtils.IsAssemblyFileType(assemblyPath))
                return new InvalidAssemblyTestRunner(assemblyPath, $"Not a valid assembly: {assemblyPath}");

            string targetFrameworkName = package.Settings.GetValueOrDefault(SettingDefinitions.ImageTargetFrameworkName);
            string platform = targetFrameworkName.Split(',')[0];
            if (!string.IsNullOrEmpty(targetFrameworkName))
                if (platform == "Silverlight" || platform == ".NETPortable" || platform == ".NETStandard" || platform == ".NETCompactFramework")
                    return new InvalidAssemblyTestRunner(assemblyPath, $"Platform {platform} is not supported");

            if (platform == "Unmanaged")
                return new UnmanagedExecutableTestRunner(assemblyPath);

            bool skipNonTestAssemblies = package.Settings.GetValueOrDefault(SettingDefinitions.SkipNonTestAssemblies);
            if (skipNonTestAssemblies)
                // TODO: It would be better to capture this when we first examine the assembly image
                using (var assemblyDef = AssemblyDefinition.ReadAssembly(assemblyPath))
                {
                    foreach (var attr in assemblyDef.CustomAttributes)
                        if (attr.AttributeType.FullName == "NUnit.Framework.NonTestAssemblyAttribute")
                            return new SkippedAssemblyTestRunner(assemblyPath);
                }

#if NETFRAMEWORK
            return new ProcessRunner(ServiceContext, package);
#else
            return new LocalTestRunner(package);
#endif
        }
    }
}
