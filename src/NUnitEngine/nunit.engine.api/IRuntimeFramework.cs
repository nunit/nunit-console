// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using System;

namespace NUnit.Engine
{
    /// <summary>
    /// Interface implemented by objects representing a runtime framework.
    /// </summary>
    public interface IRuntimeFramework
    {
        /// <summary>
        /// Gets the inique Id for this runtime, such as "net-4.5"
        /// </summary>
        string Id { get;  }

        /// <summary>
        /// Gets the display name of the framework, such as ".NET 4.5"
        /// </summary>
        string DisplayName { get; }

        /// <summary>
        /// Gets the framework version: usually contains two components, Major
        /// and Minor, which match the corresponding CLR components, but not always.
        /// </summary>
        Version FrameworkVersion { get; }

        /// <summary>
        /// Gets the Version of the CLR for this framework
        /// </summary>
        Version ClrVersion { get; }

        /// <summary>
        /// Gets a string representing the particular profile installed,
        /// or null if there is no profile. Currently. the only defined 
        /// values are Full and Client.
        /// </summary>
        string Profile { get; }
    }
}
