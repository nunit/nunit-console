// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using System.Collections.Generic;
using System.Reflection;
using NUnit.Extensibility;
using NUnit.FileSystemAccess;

namespace NUnit.Engine.Services
{
    /// <summary>
    /// The ExtensionService discovers ExtensionPoints and Extensions and
    /// maintains them in a database. It can return extension nodes or
    /// actual extension objects on request.
    /// </summary>
    public class ExtensionService : Service, IExtensionService
    {
        private const string ENGINE_TYPE_EXTENSION_PATH = "/NUnit/Engine/TypeExtensions/";

        // The Extension Manager is available internally to allow direct
        // access to ExtensionPoints and ExtensionNodes.
        internal readonly ExtensionManager _extensionManager;

        public ExtensionService()
        {
            _extensionManager = new ExtensionManager(ENGINE_TYPE_EXTENSION_PATH);
        }

        internal ExtensionService(IFileSystem fileSystem)
            : this(fileSystem, new DirectoryFinder(fileSystem))
        {
            _extensionManager = new ExtensionManager(ENGINE_TYPE_EXTENSION_PATH, fileSystem);
        }

        internal ExtensionService(IFileSystem fileSystem, IDirectoryFinder directoryFinder)
        {
            _extensionManager = new ExtensionManager(ENGINE_TYPE_EXTENSION_PATH, fileSystem, directoryFinder);
        }

        #region IExtensionService Implementation

        /// <summary>
        /// Gets an enumeration of all extension points in the engine.
        /// </summary>
        /// <returns>An enumeration of IExtensionPoints. </returns>
        IEnumerable<IExtensionPoint> IExtensionService.ExtensionPoints => this.ExtensionPoints;

        /// <summary>
        /// Gets an enumeration of all installed Extensions.
        /// </summary>
        /// <returns>An enumeration of IExtensionNodes</returns>
        IEnumerable<IExtensionNode> IExtensionService.Extensions => this.Extensions;

        /// <summary>
        /// Find candidate extension assemblies starting from a given base directory,
        /// and using the contained '.addins' files to direct the search.
        /// </summary>
        /// <param name="initialDirectory">Path to the initial directory.</param>
        public void FindExtensionAssemblies(string initialDirectory)
        {
            _extensionManager.FindExtensionAssemblies(initialDirectory);
        }

        /// <summary>
        /// Get an ExtensionPoint based on its unique identifying path.
        /// </summary>
        IExtensionPoint? IExtensionService.GetExtensionPoint(string path)
        {
            return _extensionManager.GetExtensionPoint(path);
        }

        /// <summary>
        /// Get an enumeration of ExtensionNodes based on their identifying path.
        /// </summary>
        IEnumerable<IExtensionNode> IExtensionService.GetExtensionNodes(string path) => this.GetExtensionNodes(path);

        /// <summary>
        /// Enable or disable an extension
        /// </summary>
        public void EnableExtension(string typeName, bool enabled)
        {
            _extensionManager.EnableExtension(typeName, enabled);
        }

        /// <summary>
        /// If extensions have not yet been loaded, examine all candidate assemblies
        /// and load them. Subsequent calls are ignored.
        /// </summary>
        public void LoadExtensions() => _extensionManager.LoadExtensions();

        /// <summary>
        /// Get extension objects for all nodes of a given type
        /// </summary>
        /// <returns>An enumeration of T</returns>
        public IEnumerable<T> GetExtensions<T>() => _extensionManager.GetExtensions<T>();

        /// <summary>
        /// Get all extension nodes of a given Type.
        /// </summary>
        /// An enumeration of IExtensionNodes for Type T.
        IEnumerable<IExtensionNode> IExtensionService.GetExtensionNodes<T>() => this.GetExtensionNodes<T>();

        #endregion

        #region Class Properties and Methods

        /// <summary>
        /// Gets an enumeration of all extension points in the engine.
        /// </summary>
        /// <returns>An enumeration of ExtensionPoints. </returns>
        /// <remarks>This class property returns actual ExtensionPoints rather than the IExtensionPoint interface.</remarks>
        public IEnumerable<ExtensionPoint> ExtensionPoints => _extensionManager.ExtensionPoints;

        /// <summary>
        /// Gets an enumeration of all installed Extensions.
        /// </summary>
        /// <returns>An enumeration of ExtensionNodes</returns>
        /// <remarks>This class property returns actual ExtensionNodes rather than the IExtensionNode interface.</remarks>
        public IEnumerable<ExtensionNode> Extensions => _extensionManager.Extensions;

        /// <summary>
        /// Get an ExtensionPoint based on its unique identifying path.
        /// </summary>
        /// <remarks>This class method returns an actual ExtensionPoint rather than the IExtensionPoint interface.</remarks>
        public ExtensionPoint? GetExtensionPoint(string path)
        {
            return _extensionManager.GetExtensionPoint(path);
        }

        /// <summary>
        /// Get an enumeration of ExtensionNodes based on their identifying path.
        /// </summary>
        /// <remarks>This class method returns actual ExtensionNodes rather than the IExtensionNode interface.</remarks>
        public IEnumerable<ExtensionNode> GetExtensionNodes(string path)
        {
            foreach (var node in _extensionManager.GetExtensionNodes(path))
                yield return node;
        }

        /// <summary>
        /// Get all extension nodes of a given Type.
        /// </summary>
        /// <returns>An enumeration of ExtensionNodes for Type T.</returns>
        /// <remarks>This class method returns actual ExtensionNodes rather than the IExtensionNode interface.</remarks>
        public IEnumerable<ExtensionNode> GetExtensionNodes<T>() => _extensionManager.GetExtensionNodes<T>();

        #endregion

        #region IService Implementation

        public override void StartService()
        {
            try
            {
                _extensionManager.FindExtensionPoints(
                    Assembly.GetExecutingAssembly(),
                    typeof(ITestEngine).Assembly);

                var thisAssembly = Assembly.GetExecutingAssembly();
                _extensionManager.FindExtensionAssemblies(thisAssembly);

                Status = ServiceStatus.Started;
            }
            catch
            {
                Status = ServiceStatus.Error;
                throw;
            }
        }

        public override void StopService()
        {
            _extensionManager.Dispose();

            Status = ServiceStatus.Stopped;
        }

        #endregion
    }
}
