// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using System.Collections.Generic;
using System.IO;
using NUnit.Extensibility;

namespace NUnit.Engine
{
    /// <summary>
    /// The IExtensionService interface allows a runner to manage extensions.
    /// </summary>
    public interface IExtensionService
    {
        /// <summary>
        /// Gets an enumeration of all extension points in the engine.
        /// </summary>
        /// <returns>An enumeration of IExtensionPoints. </returns>
        IEnumerable<IExtensionPoint> ExtensionPoints { get; }

        /// <summary>
        /// Gets an enumeration of all installed Extensions.
        /// </summary>
        /// <returns>An enumeration of IExtensionNodes</returns>
        IEnumerable<IExtensionNode> Extensions { get; }

        /// <summary>
        /// Find candidate extension assemblies starting from a given base directory,
        /// and using the contained '.addins' files to direct the search.
        /// </summary>
        /// <param name="initialDirectory">Path to the initial directory.</param>
        void FindExtensionAssemblies(string initialDirectory);

        /// <summary>
        /// Get an IExtensionPoint based on its unique identifying path.
        /// </summary>
        /// <returns>A single IExtensionPoint with the specified path if it exists. Otherwise null.</returns>
        IExtensionPoint? GetExtensionPoint(string path);

        /// <summary>
        /// Get an enumeration of IExtensionNodes based on their identifying path.
        /// </summary>
        /// <returns>An enumeration of IExtensionNodes.</returns>
        IEnumerable<IExtensionNode> GetExtensionNodes(string path);

        /// <summary>
        /// Enable or disable an extension
        /// </summary>
        void EnableExtension(string typeName, bool enabled);

        /// <summary>
        /// If extensions have not yet been loaded, examine all candidate assemblies
        /// and load them. Subsequent calls are ignored.
        /// </summary>
        /// <remarks>
        /// We can only load extensions after all candidate assemblies are identified.
        /// This method may be called by the user after all "Find" calls are complete.
        /// If the user fails to call it and subsequently tries to examine extensions
        /// using other ExtensionManager properties or methods, it will be called
        /// but calls not going through ExtensionManager may fail.
        /// </remarks>
        void LoadExtensions();

        /// <summary>
        /// Get extension objects for all nodes of a given type
        /// </summary>
        /// <returns>An enumeration of T</returns>
        IEnumerable<T> GetExtensions<T>();

        /// <summary>
        /// Get all extension nodes of a given Type.
        /// </summary>
        /// An enumeration of IExtensionNodes for Type T.
        IEnumerable<IExtensionNode> GetExtensionNodes<T>();
    }
}
