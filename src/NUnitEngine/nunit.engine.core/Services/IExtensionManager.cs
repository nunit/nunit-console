// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using System;
using System.Collections.Generic;
using System.Reflection;

using NUnit.Engine.Extensibility;

namespace NUnit.Engine.Services
{
    public interface IExtensionManager : IDisposable
    {
        IEnumerable<IExtensionPoint> ExtensionPoints { get; }
        IEnumerable<IExtensionNode> Extensions { get; }

        void FindExtensionPoints(params Assembly[] targetAssemblies);

        /// <summary>
        /// Find and install extensions starting from a given base directory,
        /// and using the contained '.addins' files to direct the search.
        /// </summary>
        /// <param name="initialDirectory">Path to the initial directory.</param>
        void FindExtensions(string initialDirectory);

        /// <summary>
        /// Find and install extensions starting from a given base directory,
        /// and using the provided list of patterns to direct the search using
        /// a built-in algorithm.
        /// </summary>
        /// <param name="initialDirectory">Path to the initial directory.</param>
        /// <param name="patterns">A list of patterns used to identify potential candidates.</param>
        void FindExtensions(string initialDirectory, string[] patterns);

        IExtensionPoint GetExtensionPoint(string path);
        
        IEnumerable<T> GetExtensions<T>();

        IEnumerable<IExtensionNode> GetExtensionNodes(string path);
        IExtensionNode GetExtensionNode(string path);
        /// <summary>
        /// Returns all extension nodes for a given Type.
        /// </summary>
        /// <typeparam name="T">The Type of the node</typeparam>
        /// <param name="includeDisabled">If true, disabled nodes are included</param>
        /// <returns>An enumeration of ExtensionNodes</returns>
        /// <remarks>
        /// Unlike other methods, this method returns an actual ExtensionNode rather 
        /// than an IExtensionNode. It is required in order for classes that support
        /// extensions to create the actual extension object.
        /// 
        /// </remarks>
        // NOTE: 
        IEnumerable<ExtensionNode> GetExtensionNodes<T>(bool includeDisabled = false);

        void EnableExtension(string typeName, bool enabled);
    }

}
