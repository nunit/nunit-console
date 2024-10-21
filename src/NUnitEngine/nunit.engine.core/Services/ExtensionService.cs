// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using System;
using System.Collections.Generic;
using System.Reflection;
using TestCentric.Metadata;
using NUnit.Engine.Extensibility;
using NUnit.Engine.Internal;
using NUnit.Engine.Internal.FileSystemAccess;
using NUnit.Engine.Internal.FileSystemAccess.Default;
using System.IO;

namespace NUnit.Engine.Services
{
    /// <summary>
    /// The ExtensionService discovers ExtensionPoints and Extensions and
    /// maintains them in a database. It can return extension nodes or
    /// actual extension objects on request.
    /// </summary>
    public class ExtensionService : Service, IExtensionService
    {
        private static readonly Assembly THIS_ASSEMBLY = typeof(ExtensionService).Assembly;

        private static readonly string[] CHOCO_PATTERNS = new[] {
            "nunit-extension-*/**/tools/", "nunit-extension-*/**/tools/*/" };
        private static readonly string[] NUGET_PATTERNS = new[] {
            "NUnit.Extension.*/**/tools/", "NUnit.Extension.*/**/tools/*/" };

        private readonly IExtensionManager _extensionManager;

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

        public IEnumerable<IExtensionPoint> ExtensionPoints => _extensionManager.ExtensionPoints;

        public IEnumerable<IExtensionNode> Extensions => _extensionManager.Extensions;

        /// <summary>
        /// Get an ExtensionPoint based on its unique identifying path.
        /// </summary>
        IExtensionPoint IExtensionService.GetExtensionPoint(string path)
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

        public IExtensionNode GetExtensionNode(string path) => _extensionManager.GetExtensionNode(path);

        public IEnumerable<ExtensionNode> GetExtensionNodes<T>() => _extensionManager.GetExtensionNodes<T>();

        public override void StartService()
        {
            try
            {
                _extensionManager.FindExtensionPoints(
                    Assembly.GetExecutingAssembly(),
                    typeof(ITestEngine).Assembly);

                var initialDirectory = AssemblyHelper.GetDirectoryName(THIS_ASSEMBLY);
                bool isChocolateyPackage = System.IO.File.Exists(Path.Combine(THIS_ASSEMBLY.Location, "VERIFICATION.txt"));

                _extensionManager.FindExtensions(initialDirectory, isChocolateyPackage ? CHOCO_PATTERNS : NUGET_PATTERNS);

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
