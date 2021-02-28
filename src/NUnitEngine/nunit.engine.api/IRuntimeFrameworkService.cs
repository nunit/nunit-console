// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using System;
using System.Collections.Generic;

namespace NUnit.Engine
{
    /// <summary>
    /// Implemented by a type that provides information about the
    /// current and other available runtimes.
    /// </summary>
    public interface IRuntimeFrameworkService
    {
        /// <summary>
        /// Returns true if the runtime framework represented by
        /// the string passed as an argument is available.
        /// </summary>
        /// <param name="framework">A string representing a framework, like 'net-4.0'</param>
        /// <returns>True if the framework is available, false if unavailable or nonexistent</returns>
        bool IsAvailable(string framework);

        /// <summary>
        /// Selects a target runtime framework for a TestPackage based on
        /// the settings in the package and the assemblies themselves.
        /// The package RuntimeFramework setting may be updated as a 
        /// result and the selected runtime is returned.
        ///
        /// Note that if a package has subpackages, the subpackages may run on a different
        /// framework to the top-level package. In future, this method should
        /// probably not return a simple string, and instead require runners
        /// to inspect the test package structure, to find all desired frameworks.
        /// </summary>
        /// <param name="package">A TestPackage</param>
        /// <returns>The selected RuntimeFramework</returns>
        string SelectRuntimeFramework(TestPackage package);
    }
}
