// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Engine;
using NUnit.FileSystemAccess;
using TestCentric.Metadata;

namespace NUnit.Extensibility
{
    public class ExtensionManager
    {
        private static readonly Version CURRENT_ENGINE_VERSION = Assembly.GetExecutingAssembly().GetName().Version ?? new Version();
        private static readonly string EXTENSION_ATTRIBUTE = typeof(ExtensionAttribute).FullName.ShouldNotBeNull();
        private static readonly string EXTENSION_PROPERTY_ATTRIBUTE = typeof(ExtensionPropertyAttribute).FullName.ShouldNotBeNull();
        private static readonly string V3_EXTENSION_ATTRIBUTE = "NUnit.Engine.Extensibility.ExtensionAttribute";
        private static readonly string V3_EXTENSION_PROPERTY_ATTRIBUTE = "NUnit.Engine.Extensibility.ExtensionPropertyAttribute";

        private static readonly Logger log = InternalTrace.GetLogger(typeof(ExtensionManager));

        private readonly IFileSystem _fileSystem;
        //private readonly IAddinsFileReader _addinsReader;
        private readonly IDirectoryFinder _directoryFinder;

        // List of all ExtensionPoints discovered
        private readonly List<ExtensionPoint> _extensionPoints = new List<ExtensionPoint>();

        // Index to ExtensionPoints based on the Path
        private readonly Dictionary<string, ExtensionPoint> _extensionPointIndex = new Dictionary<string, ExtensionPoint>();

        // List of ExtensionNodes for all extensions discovered.
        private readonly List<ExtensionNode> _extensions = new List<ExtensionNode>();

        private bool _extensionsAreLoaded;

        // AssemblyTracker is a List of candidate ExtensionAssemblies, with built-in indexing
        // by file path and assembly name, eliminating the need to update indices separately.
        private readonly ExtensionAssemblyTracker _assemblies = new ExtensionAssemblyTracker();

        // List of all extensionDirectories specified on command-line or in environment,
        // used to ignore duplicate calls to FindExtensionAssemblies.
        private readonly List<string> _extensionDirectories = new List<string>();

        public ExtensionManager()
            : this(new FileSystem())
        {
        }

        public ExtensionManager(IFileSystem fileSystem)
            : this(fileSystem, new DirectoryFinder(fileSystem))
        {
        }

        public ExtensionManager(IFileSystem fileSystem, IDirectoryFinder directoryFinder)
        {
            _fileSystem = fileSystem;
            _directoryFinder = directoryFinder;
        }

        #region IExtensionManager Implementation

        /// <summary>
        /// Gets an enumeration of all ExtensionPoints in the engine.
        /// </summary>
        public IEnumerable<IExtensionPoint> ExtensionPoints
        {
            get { return _extensionPoints.ToArray(); }
        }

        /// <summary>
        /// Gets an enumeration of all installed Extensions.
        /// </summary>
        public IEnumerable<IExtensionNode> Extensions
        {
            get
            {
                EnsureExtensionsAreLoaded();

                return _extensions.ToArray();
            }
        }

        /// <summary>
        /// Find the extension points in a loaded assembly.
        /// </summary>
        public virtual void FindExtensionPoints(params Assembly[] targetAssemblies)
        {
            foreach (var assembly in targetAssemblies)
            {
                log.Info("FindExtensionPoints scanning {0} assembly", assembly.GetName().Name!);

                foreach (ExtensionPointAttribute attr in assembly.GetCustomAttributes(typeof(ExtensionPointAttribute), false))
                {
                    if (_extensionPointIndex.ContainsKey(attr.Path))
                        throw new NUnitExtensibilityException($"The Path {attr.Path} is already in use for another extension point.");

                    var ep = new ExtensionPoint(attr.Path, attr.Type)
                    {
                        Description = attr.Description,
                    };

                    _extensionPoints.Add(ep);
                    _extensionPointIndex.Add(ep.Path, ep);

                    log.Info("  Found ExtensionPoint: Path={0}, Type={1}", ep.Path, ep.TypeName);
                }

                foreach (Type type in assembly.GetExportedTypes())
                {
                    foreach (TypeExtensionPointAttribute attr in type.GetCustomAttributes(typeof(TypeExtensionPointAttribute), false))
                    {
                        string path = attr.Path ?? "/NUnit/Engine/TypeExtensions/" + type.Name;

                        if (_extensionPointIndex.ContainsKey(path))
                            throw new NUnitExtensibilityException($"The Path {attr.Path} is already in use for another extension point.");

                        var ep = new ExtensionPoint(path, type)
                        {
                            Description = attr.Description,
                        };

                        _extensionPoints.Add(ep);
                        _extensionPointIndex.Add(path, ep);

                        log.Info("  Found ExtensionPoint: Path={0}, Type={1}", ep.Path, ep.TypeName);
                    }
                }
            }
        }

        /// <summary>
        /// Find extension assemblies starting from a given base directory,
        /// and using the contained '.addins' files to direct the search.
        /// </summary>
        /// <param name="initialDirectory">Path to the initial directory.</param>
        public void FindExtensionAssemblies(string startDir)
        {
            // Ignore a call for a directory we have already used
            if (!_extensionDirectories.Contains(startDir))
            {
                _extensionDirectories.Add(startDir);

                log.Info($"FindExtensionAssemblies examining extension directory {startDir}");

                // Create the list of possible extension assemblies,
                // eliminating duplicates, start in the provided directory.
                // In this top level directory, we only look at .addins files.
                ProcessAddinsFiles(_fileSystem.GetDirectory(startDir), false);
            }
        }

        /// <summary>
        /// Find ExtensionAssemblies for a host assembly using
        /// a built-in algorithm that searches in certain known locations.
        /// </summary>
        /// <param name="hostAssembly">An assembly that supports NUnit extensions.</param>
        public void FindExtensionAssemblies(Assembly hostAssembly)
        {
            log.Info($"FindExtensionAssemblies called for host {hostAssembly.FullName}");

            bool isChocolateyPackage = System.IO.File.Exists(Path.Combine(Path.GetDirectoryName(hostAssembly.Location)!, "VERIFICATION.txt"));
            string[] extensionPatterns = isChocolateyPackage
                ? new[] { "nunit-extension-*/**/tools/", "nunit-extension-*/**/tools/*/" }
                : new[] { "NUnit.Extension.*/**/tools/", "NUnit.Extension.*/**/tools/*/" };

            IDirectory? startDir = _fileSystem.GetDirectory(AssemblyHelper.GetDirectoryName(hostAssembly));

            while (startDir is not null)
            {
                foreach (var pattern in extensionPatterns)
                    foreach (var dir in _directoryFinder.GetDirectories(startDir, pattern))
                        ProcessDirectory(dir, true);

                startDir = startDir.Parent;
            }
        }

        /// <summary>
        /// Get an ExtensionPoint based on its unique identifying path.
        /// </summary>
        public IExtensionPoint? GetExtensionPoint(string path)
        {
            return _extensionPointIndex.TryGetValue(path, out ExtensionPoint? ep) ? ep : null;
        }

        /// <summary>
        /// Get extension objects for all nodes of a given type
        /// </summary>
        public IEnumerable<T> GetExtensions<T>()
        {
            foreach (var node in GetExtensionNodes<T>())
                yield return (T)node.ExtensionObject;
        }

        /// <summary>
        /// Get all ExtensionNodes for a path
        /// </summary>
        public IEnumerable<IExtensionNode> GetExtensionNodes(string path)
        {
            EnsureExtensionsAreLoaded();

            var ep = GetExtensionPoint(path);
            if (ep is not null)
                foreach (var node in ep.Extensions)
                    yield return node;
        }

        /// <summary>
        /// Get the first or only ExtensionNode for a given ExtensionPoint
        /// </summary>
        /// <param name="path">The identifying path for an ExtensionPoint</param>
        /// <returns></returns>
        public IExtensionNode? GetExtensionNode(string path)
        {
            EnsureExtensionsAreLoaded();

            // TODO: Remove need for the cast
            var ep = GetExtensionPoint(path) as ExtensionPoint;

            return ep is not null && ep.Extensions.Count > 0 ? ep.Extensions[0] : null;
        }

        /// <summary>
        /// Get all extension nodes of a given Type.
        /// </summary>
        /// <param name="includeDisabled">If true, disabled nodes are included</param>
        public IEnumerable<ExtensionNode> GetExtensionNodes<T>(bool includeDisabled = false)
        {
            EnsureExtensionsAreLoaded();

            var ep = GetExtensionPoint(typeof(T));
            if (ep is not null)
                foreach (var node in ep.Extensions)
                    if (includeDisabled || node.Enabled)
                        yield return node;
        }

        /// <summary>
        /// Enable or disable an extension
        /// </summary>
        public void EnableExtension(string typeName, bool enabled)
        {
            EnsureExtensionsAreLoaded();

            foreach (var node in _extensions)
                if (node.TypeName == typeName)
                    node.Enabled = enabled;
        }

        #endregion

        /// <summary>
        /// Get an ExtensionPoint based on the required Type for extensions.
        /// </summary>
        public ExtensionPoint? GetExtensionPoint(Type type)
        {
            foreach (var ep in _extensionPoints)
                if (ep.TypeName == type.FullName)
                    return ep;

            return null;
        }

        /// <summary>
        /// Get an ExtensionPoint based on a Cecil TypeReference.
        /// </summary>
        public ExtensionPoint? GetExtensionPoint(TypeReference type)
        {
            foreach (var ep in _extensionPoints)
                if (ep.TypeName == type.FullName)
                    return ep;

            return null;
        }

        /// <summary>
        /// Deduce the extension point based on the Type of an extension.
        /// Returns null if no extension point can be found that would
        /// be satisfied by the provided Type.
        /// </summary>
        private ExtensionPoint? DeduceExtensionPointFromType(TypeReference typeRef)
        {
            var ep = GetExtensionPoint(typeRef);
            if (ep is not null)
                return ep;

            TypeDefinition typeDef = typeRef.Resolve();

            foreach (InterfaceImplementation iface in typeDef.Interfaces)
            {
                ep = DeduceExtensionPointFromType(iface.InterfaceType);
                if (ep is not null)
                    return ep;
            }

            TypeReference? baseType = typeDef.BaseType;
            return baseType is not null && baseType.FullName != "System.Object"
                ? DeduceExtensionPointFromType(baseType)
                : null;
        }

        /// <summary>
        /// We can only load extensions after all candidate assemblies are identified.
        /// This method is called internally to ensure the load happens before any
        /// extensions are used.
        /// </summary>
        private void EnsureExtensionsAreLoaded()
        {
            if (!_extensionsAreLoaded)
            {
                _extensionsAreLoaded = true;

                foreach (var candidate in _assemblies)
                    FindExtensionsInAssembly(candidate);
            }
        }

        /// <summary>
        /// Scans a directory for candidate addin assemblies. Note that assemblies in
        /// the directory are only scanned if no file of type .addins is found. If such
        /// a file is found, then those assemblies it references are scanned.
        /// </summary>
        private void ProcessDirectory(IDirectory startDir, bool fromWildCard)
        {
            var directoryName = startDir.FullName;
            if (WasVisited(directoryName, fromWildCard))
            {
                log.Warning($"Skipping directory '{directoryName}' because it was already visited.");
                return;
            }

            log.Info($"Scanning directory '{directoryName}' for extensions.");
            Visit(directoryName, fromWildCard);

            if (ProcessAddinsFiles(startDir, fromWildCard) == 0)
                foreach (var file in startDir.GetFiles("*.dll"))
                    ProcessCandidateAssembly(file.FullName, true);
        }

        /// <summary>
        /// Process all .addins files found in a directory
        /// </summary>
        private int ProcessAddinsFiles(IDirectory startDir, bool fromWildCard)
        {
            var addinsFiles = startDir.GetFiles("*.addins");
            var addinsFileCount = 0;

            foreach (var file in addinsFiles)
            {
                ProcessAddinsFile(file, fromWildCard);
                addinsFileCount++;
            }

            return addinsFileCount;
        }

        /// <summary>
        /// Process a .addins type file. The file contains one entry per
        /// line. Each entry may be a directory to scan, an assembly
        /// path or a wildcard pattern used to find assemblies. Blank
        /// lines and comments started by # are ignored.
        /// </summary>
        private void ProcessAddinsFile(IFile addinsFile, bool fromWildCard)
        {
            log.Info("Processing file " + addinsFile.FullName);

            foreach (var entry in AddinsFile.Read(addinsFile).Where(e => e.Text != string.Empty))
            {
                bool isWild = fromWildCard || entry.IsPattern;
                IDirectory baseDir = addinsFile.Parent;
                string entryDir = entry.DirectoryName;
                string entryFile = entry.FileName;

                log.Debug($"Processing entry {entry.Text}");
                if (entry.IsDirectory)
                {
                    if (entry.IsFullyQualified)
                    {
                        baseDir = _fileSystem.GetDirectory(entry.Text);
                        foreach (var dir in _directoryFinder.GetDirectories(_fileSystem.GetDirectory(entryDir), string.Empty))
                            ProcessDirectory(dir, isWild);
                    }
                    else
                        foreach (var dir in _directoryFinder.GetDirectories(baseDir, entry.Text))
                            ProcessDirectory(dir, isWild);
                }
                else
                {
                    if (entry.IsFullyQualified)
                    {
                        foreach (var file in _directoryFinder.GetFiles(_fileSystem.GetDirectory(entryDir), entryFile))
                            ProcessCandidateAssembly(file.FullName, isWild);
                    }
                    else
                        foreach (var file in _directoryFinder.GetFiles(baseDir, entry.Text))
                            ProcessCandidateAssembly(file.FullName, isWild);
                }
            }
        }

        private void ProcessCandidateAssembly(string filePath, bool fromWildCard)
        {
            log.Debug($"Processing candidate assembly {filePath}");
            // Did we already process this file?
            if (_assemblies.ByPath.ContainsKey(filePath))
            {
                log.Debug("  Skipping assembly already processed");
                return;
            }

            try
            {
                var candidateAssembly = new ExtensionAssembly(filePath, fromWildCard);
                string assemblyName = candidateAssembly.AssemblyName;

                // We never add assemblies unless the host can load them
                if (!CanLoadTargetFramework(Assembly.GetEntryAssembly(), candidateAssembly))
                {
                    log.Debug("  Unable to load this assembly");
                    return;
                }

                // Do we already have a copy of the same assembly at a different path?
                //if (_assemblies.ByName.ContainsKey(assemblyName))
                if (_assemblies.ByName.TryGetValue(assemblyName, out ExtensionAssembly? existing))
                {
                    if (candidateAssembly.IsBetterVersionOf(existing))
                        _assemblies.ByName[assemblyName] = candidateAssembly;

                    return;
                }

                log.Debug("  Adding this assembly");
                _assemblies.Add(candidateAssembly);
            }
            catch (Exception) when (fromWildCard)
            {
                // When we are using wildcards, we may reach assemblies built for some other
                // runtime, which we cannot load. We ignore those assemblies.
            }
        }

        // Dictionary containing all directory paths already visited.
        private readonly Dictionary<string, object?> _visited = new Dictionary<string, object?>();

        private bool WasVisited(string path, bool fromWildcard)
        {
            return _visited.ContainsKey($"path={path}_visited={fromWildcard}");
        }

        private void Visit(string path, bool fromWildcard)
        {
            _visited.Add($"path={path}_visited={fromWildcard}", null);
        }

        /// <summary>
        /// Scan a single assembly for extensions marked by ExtensionAttribute.
        /// For each extension, create an ExtensionNode and link it to the
        /// correct ExtensionPoint. Public for testing.
        /// </summary>
        public void FindExtensionsInAssembly(ExtensionAssembly extensionAssembly)
        {
            log.Info($"Scanning {extensionAssembly.FilePath} for Extensions");

            if (!CanLoadTargetFramework(Assembly.GetEntryAssembly(), extensionAssembly))
            {
                log.Info($"{extensionAssembly.FilePath} cannot be loaded on this runtime");
                return;
            }

#if NETFRAMEWORK
            //// Use special properties provided by our backport of RuntimeInformation
            //Version currentVersion = RuntimeInformation.FrameworkVersion;
            //var frameworkName = extensionAssembly.FrameworkName;

            //if (frameworkName.Identifier != FrameworkIdentifiers.NetFramework || frameworkName.Version > currentVersion)
            //{
            //    if (!extensionAssembly.FromWildCard)
            //    {
            //        throw new NUnitEngineException($"Extension {extensionAssembly.FilePath} targets {assemblyTargetFramework.DisplayName}, which is not available.");
            //    }
            //    else
            //    {
            //        log.Info($"Assembly {extensionAssembly.FilePath} targets {assemblyTargetFramework.DisplayName}, which is not available. Assembly found via wildcard.");
            //        return;
            //    }
            //}
#endif

            foreach (TypeDefinition extensionType in extensionAssembly.Assembly.MainModule.GetTypes())
            {
                bool isV3Extension = false;
                CustomAttribute extensionAttr = extensionType.GetAttribute(EXTENSION_ATTRIBUTE);
                if (extensionAttr is null)
                {
                    extensionAttr = extensionType.GetAttribute(V3_EXTENSION_ATTRIBUTE);
                    isV3Extension = true;
                }

                if (extensionAttr is null)
                    continue;

                // TODO: This is a remnant of older code. In principle, this should be generalized
                // to something like "HostVersion". However, this can safely remain until
                // we separate ExtensionManager into its own assembly.
                string? versionArg = extensionAttr.GetNamedArgument(nameof(ExtensionAttribute.EngineVersion)) as string;
                if (versionArg is not null)
                {
                    if (new Version(versionArg) > CURRENT_ENGINE_VERSION)
                    {
                        log.Warning($"  Ignoring {extensionType.Name}. It requires version {versionArg}.");
                        continue;
                    }
                }

                string? extensionAttrPath = (string?)extensionAttr.GetNamedArgument(nameof(ExtensionAttribute.Path));
                var node = new ExtensionNode(extensionAssembly, extensionType)
                {
                    IsV3Extension = isV3Extension,
                    Description = (string?)extensionAttr.GetNamedArgument(nameof(ExtensionAttribute.Description)),
                };

                object? enabledArg = extensionAttr.GetNamedArgument(nameof(ExtensionAttribute.Enabled));
                node.Enabled = enabledArg is null || (bool)enabledArg;

                log.Info("  Found ExtensionAttribute on Type " + extensionType.Name);

                var propertyAttributes = isV3Extension
                    ? extensionType.GetAttributes(V3_EXTENSION_PROPERTY_ATTRIBUTE)
                    : extensionType.GetAttributes(EXTENSION_PROPERTY_ATTRIBUTE);

                foreach (var attr in propertyAttributes)
                {
                    string? name = attr.ConstructorArguments[0].Value as string;
                    string? value = attr.ConstructorArguments[1].Value as string;

                    if (name is not null && value is not null)
                    {
                        node.AddProperty(name, value);
                        log.Info("        ExtensionProperty {0} = {1}", name, value);
                    }
                }

                _extensions.Add(node);

                ExtensionPoint? ep;
                if (extensionAttrPath is null)
                {
                    ep = DeduceExtensionPointFromType(extensionType);
                    if (ep is null)
                        throw new NUnitExtensibilityException($"Unable to deduce ExtensionPoint for Type {extensionType.FullName}. Specify Path on ExtensionAttribute to resolve.");

                    node.Path = ep.Path;
                }
                else
                {
                    node.Path = extensionAttrPath;

                    // TODO: Remove need for the cast
                    ep = GetExtensionPoint(node.Path) as ExtensionPoint;
                    if (ep is null)
                        throw new NUnitExtensibilityException($"Unable to locate ExtensionPoint for Type {extensionType.FullName}. The Path {node.Path} cannot be found.");
                }

                ep.Install(node);
            }
        }

        private const string NETFRAMEWORK = ".NETFramework";
        private const string NETCOREAPP = ".NETCoreApp";
        private const string NETSTANDARD = ".NETStandard";

        /// <summary>
        /// Checks that the target framework of the current runner can load the extension assembly. For example, .NET Core
        /// cannot load .NET Framework assemblies and vice-versa.
        /// </summary>
        /// <param name="runnerAsm">The executing runner</param>
        /// <param name="extensionAsm">The extension we are attempting to load</param>
        public bool CanLoadTargetFramework(Assembly? runnerAsm, ExtensionAssembly extensionAsm)
        {
            if (runnerAsm is null)
                return true;

            var runnerFrameworkName = GetTargetRuntime(runnerAsm.Location);
            var extensionFrameworkName = GetTargetRuntime(extensionAsm.FilePath);

            switch (runnerFrameworkName.Identifier)
            {
                case NETSTANDARD:
                    throw new NUnit.Engine.NUnitEngineException($"{runnerAsm.FullName} test runner must target .NET Core or .NET Framework, not .NET Standard");

                case NETCOREAPP:
                    switch (extensionFrameworkName.Identifier)
                    {
                        case NETSTANDARD:
                        case NETCOREAPP:
                            return true;
                        case NETFRAMEWORK:
                        default:
                            log.Info($".NET Core runners require .NET Core or .NET Standard extension for {extensionAsm.FilePath}");
                            return false;
                    }
                case NETFRAMEWORK:
                default:
                    switch (extensionFrameworkName.Identifier)
                    {
                        case NETFRAMEWORK:
                            return runnerFrameworkName.Version.Major == 4 || extensionFrameworkName.Version.Major < 4;
                        // For .NET Framework calling .NET Standard, we only support if framework is 4.7.2 or higher
                        case NETSTANDARD:
                            return extensionFrameworkName.Version >= new Version(4, 7, 2);
                        case NETCOREAPP:
                        default:
                            log.Info($".NET Framework runners cannot load .NET Core extension {extensionAsm.FilePath}");
                            return false;
                    }
            }

            //string extensionFrameworkName = AssemblyDefinition.ReadAssembly(extensionAsm.FilePath).GetFrameworkName();
            //string runnerFrameworkName = AssemblyDefinition.ReadAssembly(runnerAsm.Location).GetFrameworkName();
            //if (runnerFrameworkName?.StartsWith(".NETStandard") == true)
            //{
            //    throw new NUnitEngineException($"{runnerAsm.FullName} test runner must target .NET Core or .NET Framework, not .NET Standard");
            //}
            //else if (runnerFrameworkName?.StartsWith(".NETCoreApp") == true)
            //{
            //    if (extensionFrameworkName?.StartsWith(".NETStandard") != true && extensionFrameworkName?.StartsWith(".NETCoreApp") != true)
            //    {
            //        log.Info($".NET Core runners require .NET Core or .NET Standard extension for {extensionAsm.FilePath}");
            //        return false;
            //    }
            //}
            //else if (extensionFrameworkName?.StartsWith(".NETCoreApp") == true)
            //{
            //    log.Info($".NET Framework runners cannot load .NET Core extension {extensionAsm.FilePath}");
            //    return false;
            //}

            //return true;
        }

        private System.Runtime.Versioning.FrameworkName GetTargetRuntime(string filePath)
        {
            var assemblyDef = AssemblyDefinition.ReadAssembly(filePath);
            var frameworkName = assemblyDef.GetFrameworkName();
            if (string.IsNullOrEmpty(frameworkName))
            {
                var runtimeVersion = assemblyDef.GetRuntimeVersion();
                frameworkName = $".NETFramework,Version=v{runtimeVersion.ToString(3)}";
            }
            return new System.Runtime.Versioning.FrameworkName(frameworkName);
        }

        public void Dispose()
        {
            // Make sure all assemblies release the underlying file streams.
            foreach (var candidate in _assemblies)
            {
                candidate.Dispose();
            }
        }
    }
}
