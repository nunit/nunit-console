// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using System.Collections.Generic;

namespace NUnit.Engine.Extensibility
{
    /// <summary>
    /// Interface for the various project types that the engine can load.
    /// </summary>
    public interface IProject
    {
        /// <summary>
        /// Gets the path to the file storing this project, if any.
        /// If the project has not been saved, this is null.
        /// </summary>
        string ProjectPath { get; }

        /// <summary>
        /// Gets the active configuration, as defined
        /// by the particular project.
        /// </summary>
        string ActiveConfigName { get; }

        /// <summary>
        /// Gets a list of the configs for this project
        /// </summary>
        IList<string> ConfigNames { get; }

        /// <summary>
        /// Gets a test package for the primary or active
        /// configuration within the project. The package 
        /// includes all the assemblies and any settings
        /// specified in the project format.
        /// </summary>
        /// <returns>A TestPackage</returns>
        TestPackage GetTestPackage();

        /// <summary>
        /// Gets a TestPackage for a specific configuration
        /// within the project. The package includes all the
        /// assemblies and any settings specified in the 
        /// project format.
        /// </summary>
        /// <param name="configName">The name of the config to use</param>
        /// <returns>A TestPackage for the named configuration.</returns>
        TestPackage GetTestPackage(string configName);
    }
}
