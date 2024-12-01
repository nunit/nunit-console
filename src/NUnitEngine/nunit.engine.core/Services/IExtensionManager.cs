// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using System;
using System.Collections.Generic;
using System.Reflection;

using NUnit.Engine.Extensibility;

namespace NUnit.Engine.Services
{
    public interface IExtensionManager : IDisposable
    {
        /// <summary>
        /// Gets an enumeration of all ExtensionPoints in the engine.
        /// </summary>
        IEnumerable<IExtensionPoint> ExtensionPoints { get; }

        /// <summary>
        /// Gets an enumeration of all installed Extensions.
        /// </summary>
        IEnumerable<IExtensionNode> Extensions { get; }

        /// <summary>
        /// Find the extension points in a loaded assembly.
        /// </summary>
        void FindExtensionPoints(params Assembly[] targetAssemblies);

        /// <summary>
        /// Find and install extensions starting from a given base directory,
        /// and using the contained '.addins' files to direct the search.
        /// </summary>
        /// <param name="initialDirectory">Path to the initial directory.</param>
        void FindExtensions(string initialDirectory);

        /// <summary>
        /// Find and install standard extensions for a host assembly using
        /// a built-in algorithm that searches in certain known locations.
        /// </summary>
        /// <param name="hostAssembly">An assembly that supports NUnit extensions.</param>
        void FindStandardExtensions(Assembly hostAssembly);

        /// <summary>
        /// Get an ExtensionPoint based on its unique identifying path.
        /// </summary>
        IExtensionPoint GetExtensionPoint(string path);
        
        /// <summary>
        /// Get extension objects for all nodes of a given type
        /// </summary>
        IEnumerable<T> GetExtensions<T>();

        /// <summary>
        /// Get all ExtensionNodes for a give path
        /// </summary>
        IEnumerable<IExtensionNode> GetExtensionNodes(string path);

        /// <summary>
        /// Get the first or only ExtensionNode for a given ExtensionPoint
        /// </summary>
        /// <param name="path">The identifying path for an ExtensionPoint</param>
        /// <returns></returns>
        IExtensionNode GetExtensionNode(string path);

        /// <summary>
        /// Get all extension nodes of a given Type.
        /// </summary>
        /// <param name="includeDisabled">If true, disabled nodes are included</param>
        IEnumerable<ExtensionNode> GetExtensionNodes<T>(bool includeDisabled = false);

        /// <summary>
        /// Enable or disable an extension
        /// </summary>
        void EnableExtension(string typeName, bool enabled);
    }

}
