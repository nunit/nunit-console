// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using System;
using System.Reflection;
using NUnit.Engine.Extensibility;

namespace NUnit.Engine.Drivers
{
    public class NUnit3DriverFactory : IDriverFactory
    {
        internal const string NUNIT_FRAMEWORK = "nunit.framework";
        private static readonly Logger log = InternalTrace.GetLogger(typeof(NUnit3DriverFactory));

        /// <summary>
        /// Gets a flag indicating whether a given assembly name and version
        /// represent a test framework supported by this factory.
        /// </summary>
        /// <param name="reference">An AssemblyName referring to the possible test framework.</param>
        public bool IsSupportedTestFramework(AssemblyName reference)
        {
            return NUNIT_FRAMEWORK.Equals(reference.Name, StringComparison.OrdinalIgnoreCase) && reference.Version?.Major >= 3;
        }

#if NETFRAMEWORK
        /// <summary>
        /// Gets a driver for a given test framework.
        /// </summary>
        /// <param name="domain">The domain in which the assembly will be loaded</param>
        /// <param name="reference">An AssemblyName referring to the test framework.</param>
        /// <returns>An IFrameworkDriver</returns>
        public IFrameworkDriver GetDriver(AppDomain domain, string id, AssemblyName reference)
        {
            Guard.ArgumentNotNullOrEmpty(id);
            Guard.ArgumentValid(IsSupportedTestFramework(reference), "Invalid framework", nameof(reference));

            log.Info("Using NUnitFrameworkDriver");
            return new NUnitFrameworkDriver(domain, id, reference);
        }
#else
        /// <summary>
        /// Gets a driver for a given test framework.
        /// </summary>
        /// <param name="reference">An AssemblyName referring to the test framework.</param>
        /// <returns></returns>
        public IFrameworkDriver GetDriver(string id, AssemblyName reference)
        {
            Guard.ArgumentNotNullOrEmpty(id);
            Guard.ArgumentValid(IsSupportedTestFramework(reference), "Invalid framework", nameof(reference));
            log.Info("Using NUnitFrameworkDriver");
            return new NUnitFrameworkDriver(id, reference);
        }
#endif
    }
}
