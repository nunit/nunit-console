// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using NUnit.Engine.Extensibility;
using NUnit.Engine.Internal;
using NUnit.Engine.Internal.FileSystemAccess;
using System.Collections.Generic;
using System.Reflection;

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

        public ExtensionService(ExtensionManager extensionManager)
        {
            _extensionManager = extensionManager;
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

        #region IExtensionService Implementation

        /// <inheritdoc/>
        public IEnumerable<IExtensionPoint> ExtensionPoints => _extensionManager.ExtensionPoints;

        /// <inheritdoc/>
        public IEnumerable<IExtensionNode> Extensions => _extensionManager.Extensions;

        /// <inheritdoc/>
        public void FindExtensionAssemblies(string initialDirectory)
        {
            _extensionManager.FindExtensionAssemblies(initialDirectory);
        }

        /// <inheritdoc/>
        IExtensionPoint IExtensionService.GetExtensionPoint(string path)
        {
            return _extensionManager.GetExtensionPoint(path);
        }

        /// <inheritdoc/>
        IEnumerable<IExtensionNode> IExtensionService.GetExtensionNodes(string path)
        {
            foreach (var node in _extensionManager.GetExtensionNodes(path))
                yield return node;
        }

        /// <inheritdoc/>
        public void EnableExtension(string typeName, bool enabled)
        {
            _extensionManager.EnableExtension(typeName, enabled);
        }

        #endregion

        public IEnumerable<T> GetExtensions<T>() => _extensionManager.GetExtensions<T>();

        public IExtensionNode GetExtensionNode(string path) => _extensionManager.GetExtensionNode(path);

        public IEnumerable<ExtensionNode> GetExtensionNodes<T>() => _extensionManager.GetExtensionNodes<T>();

        public override void StartService()
        {
            Assembly thisAssembly = Assembly.GetExecutingAssembly();
            Assembly apiAssembly = typeof(ITestEngine).Assembly;

            try
            {
                _extensionManager.FindExtensionPoints(thisAssembly, apiAssembly);
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
