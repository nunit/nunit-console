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
        /// <param name="x86">A flag indicating whether the X86 architecture is needed. Defaults to false.</param>
        /// <returns>True if the framework is available, false if unavailable or nonexistent</returns>
        bool IsAvailable(string framework, bool x86);

        /// <summary>
        /// Selects a target runtime framework for a TestPackage based on
        /// the settings in the package and the assemblies themselves.
        /// The package RuntimeFramework setting may be updated as a result.
        /// </summary>
        /// <param name="package">A TestPackage</param>
        void SelectRuntimeFramework(TestPackage package);
    }
}
