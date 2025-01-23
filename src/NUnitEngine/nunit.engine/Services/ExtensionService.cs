// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Engine.Extensibility;
using NUnit.Engine.Internal;
using NUnit.Engine.Internal.FileSystemAccess;

namespace NUnit.Engine.Services
{
    /// <summary>
    /// The ExtensionService discovers ExtensionPoints and Extensions and
    /// maintains them in a database. It can return extension nodes or
    /// actual extension objects on request.
    /// </summary>
    public class ExtensionService : Service, IExtensionService
    {
        private readonly ExtensionManager _extensionManager;

        public ExtensionService()
        {
            _extensionManager = new ExtensionManager();
        }

        internal ExtensionService(IFileSystem fileSystem)
            : this(fileSystem, new DirectoryFinder(fileSystem))
        {
            _extensionManager = new ExtensionManager(fileSystem);
        }

        internal ExtensionService(IFileSystem fileSystem, IDirectoryFinder directoryFinder)
        {
            _extensionManager = new ExtensionManager(fileSystem, directoryFinder);
        }

        public IEnumerable<IExtensionPoint> ExtensionPoints => _extensionManager.ExtensionPoints;

        public IEnumerable<IExtensionNode> Extensions => _extensionManager.Extensions;

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
        IEnumerable<IExtensionNode> IExtensionService.GetExtensionNodes(string path)
        {
            foreach (var node in _extensionManager.GetExtensionNodes(path))
                yield return node;
        }

        public void EnableExtension(string typeName, bool enabled)
        {
            _extensionManager.EnableExtension(typeName, enabled);
        }

        public IEnumerable<T> GetExtensions<T>() => _extensionManager.GetExtensions<T>();

        public IExtensionNode? GetExtensionNode(string path) => _extensionManager.GetExtensionNode(path);

        public IEnumerable<ExtensionNode> GetExtensionNodes<T>() => _extensionManager.GetExtensionNodes<T>();

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
    }
}
