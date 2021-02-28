// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using System;
using System.Reflection;

namespace NUnit.Engine.Extensibility
{
    /// <summary>
    /// Interface implemented by a Type that knows how to create a driver for a test assembly.
    /// </summary>
    [TypeExtensionPoint(
        Description = "Supplies a driver to run tests that use a specific test framework.")]
    public interface IDriverFactory
    {
        /// <summary>
        /// Gets a flag indicating whether a given AssemblyName
        /// represents a test framework supported by this factory.
        /// </summary>
        /// <param name="reference">An AssemblyName referring to the possible test framework.</param>
        bool IsSupportedTestFramework(AssemblyName reference);

#if NETSTANDARD2_0
        /// <summary>
        /// Gets a driver for a given test assembly and a framework
        /// which the assembly is already known to reference.
        /// </summary>
        /// <param name="reference">An AssemblyName referring to the test framework.</param>
        /// <returns></returns>
        IFrameworkDriver GetDriver(AssemblyName reference);
#else
        /// <summary>
        /// Gets a driver for a given test assembly and a framework
        /// which the assembly is already known to reference.
        /// </summary>
        /// <param name="domain">The domain in which the assembly will be loaded</param>
        /// <param name="reference">An AssemblyName referring to the test framework.</param>
        /// <returns></returns>
        IFrameworkDriver GetDriver(AppDomain domain, AssemblyName reference);
#endif
    }
}
