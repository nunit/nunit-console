// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using System.Collections.Generic;

namespace NUnit.Engine
{
    /// <summary>
    /// Interface that returns a list of available runtime frameworks.
    /// </summary>
    public interface IAvailableRuntimes
    {
        /// <summary>
        /// Gets a list of available runtime frameworks for X64.
        /// </summary>
        IList<IRuntimeFramework> AvailableRuntimes { get; }

        /// <summary>
        /// Gets a list of available runtime frameworks for X86.
        /// </summary>
        IList<IRuntimeFramework> AvailableX86Runtimes { get; }
    }
}
