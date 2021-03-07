// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

#if !NETSTANDARD2_0
using System;

namespace NUnit.Engine.Internal
{
    /// <summary>
    /// Represents the manner in which test assemblies are
    /// distributed across processes.
    /// </summary>
    public enum ProcessModel
    {
        /// <summary>
        /// Use the default setting, depending on the runner
        /// and the nature of the tests to be loaded.
        /// </summary>
        Default,
        /// <summary>
        /// Run tests directly in the NUnit process
        /// </summary>
        InProcess,
        /// <summary>
        /// Run tests directly in the NUnit process
        /// </summary>
        [Obsolete("Use InProcess instead")]
        Single = InProcess,
        /// <summary>
        /// Run tests in a single separate process
        /// </summary>
        Separate,
        /// <summary>
        /// Run tests in a separate process per assembly
        /// </summary>
        Multiple
    }
}
#endif