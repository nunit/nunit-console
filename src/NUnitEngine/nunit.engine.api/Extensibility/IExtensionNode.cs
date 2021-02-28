// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using System;
using System.Collections.Generic;

namespace NUnit.Engine.Extensibility
{
    /// <summary>
    /// The IExtensionNode interface is implemented by a class that represents a
    /// single extension being installed on a particular extension point.
    /// </summary>
    public interface IExtensionNode
    {
        /// <summary>
        /// Gets the full name of the Type of the extension object.
        /// </summary>
        string TypeName { get; }

        /// <summary>
        /// Gets a value indicating whether this <see cref="NUnit.Engine.Extensibility.IExtensionNode"/> is enabled.
        /// </summary>
        /// <value><c>true</c> if enabled; otherwise, <c>false</c>.</value>
        bool Enabled { get; }

        /// <summary>
        /// Gets the unique string identifying the ExtensionPoint for which
        /// this Extension is intended. This identifier may be supplied by the attribute
        /// marking the extension or deduced by NUnit from the Type of the extension class.
        /// </summary>
        string Path { get; }

        /// <summary>
        /// Gets an optional description of what the extension does.
        /// </summary>
        string Description { get; }

        /// <summary>
        /// The TargetFramework of the extension assembly.
        /// </summary>
        IRuntimeFramework TargetFramework { get; }

        /// <summary>
        /// Gets a collection of the names of all this extension's properties
        /// </summary>
        IEnumerable<string> PropertyNames { get; }

        /// <summary>
        /// Gets a collection of the values of a particular named property
        /// If none are present, returns an empty enumerator.
        /// </summary>
        /// <param name="name">The property name</param>
        /// <returns>A collection of values</returns>
        IEnumerable<string> GetValues(string name);

        /// <summary>
        /// The path to the assembly implementing this extension.
        /// </summary>
        string AssemblyPath { get; }

        /// <summary>
        /// The version of the assembly implementing this extension.
        /// </summary>
        Version AssemblyVersion { get; }
    }
}
