// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using System;

namespace NUnit.Engine.Extensibility
{
    /// <summary>
    /// The IProjectLoader interface is implemented by any class
    /// that knows how to load projects in a specific format.
    /// </summary>
    [TypeExtensionPoint(
        Description = "Recognizes and loads assemblies from various types of project formats.")]
    public interface IProjectLoader
    {
        /// <summary>
        /// Returns true if the file indicated is one that this
        /// loader knows how to load.
        /// </summary>
        /// <param name="path">The path of the project file</param>
        /// <returns>True if the loader knows how to load this file, otherwise false</returns>
        bool CanLoadFrom( string path );

        /// <summary>
        /// Loads a project of a known format.
        /// </summary>
        /// <param name="path">The path of the project file</param>
        /// <returns>An IProject interface to the loaded project or null if the project cannot be loaded</returns>
        IProject LoadFrom(string path);
    }
}
