// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt
using NUnit.Engine.Internal;
using NUnit.Engine.Runners;

namespace NUnit.Engine.Services
{
    /// <summary>
    /// The InProcessTestRunnerFactory static class handles creation
    /// of a suitable test runner for a given package to be loaded
    /// and run within the current process.
    /// </summary>
    /// <remarks>
    /// This class was originally a non-static Service and was used as
    /// the base class for DefaultTestRunnerFactory. The static version
    /// is a temporary measure for use while we are in the process of
    /// removing all services from the nunit.engine.core assembly.
    /// </remarks>
    public static class InProcessTestRunnerFactory
    {
        /// <summary>
        /// Returns a test runner based on the settings in a TestPackage.
        /// Any setting that is "consumed" by the factory is removed, so
        /// that downstream runners using the factory will not repeatedly
        /// create the same type of runner.
        /// </summary>
        /// <param name="package">The TestPackage to be loaded and run</param>
        /// <returns>An ITestEngineRunner</returns>
        public static ITestEngineRunner MakeTestRunner(IServiceLocator context, TestPackage package)
        {
#if !NETFRAMEWORK
            return new LocalTestRunner(context, package);
#else
            DomainUsage domainUsage = (DomainUsage)System.Enum.Parse(
                typeof(DomainUsage),
                package.GetSetting(EnginePackageSettings.DomainUsage, "Default"));

            switch (domainUsage)
            {
                default:
                case DomainUsage.Default:
                case DomainUsage.Multiple:
                    if (package.SubPackages.Count > 1)
                        return new MultipleTestDomainRunner(context, package);
                    else
                        return new TestDomainRunner(context, package);

                case DomainUsage.None:
                    return new LocalTestRunner(context, package);

                case DomainUsage.Single:
                    return new TestDomainRunner(context, package);
            }
#endif
        }
    }
}
