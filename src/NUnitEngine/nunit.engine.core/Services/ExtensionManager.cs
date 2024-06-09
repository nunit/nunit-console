// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using System;
using System.Collections.Generic;
using System.Reflection;
using TestCentric.Metadata;
using NUnit.Engine.Extensibility;
using NUnit.Engine.Internal;
using NUnit.Engine.Internal.Backports;
using NUnit.Engine.Internal.FileSystemAccess;
using NUnit.Engine.Internal.FileSystemAccess.Default;

using Backports = NUnit.Engine.Internal.Backports;
#if NET20 || NETSTANDARD2_0
using Path = NUnit.Engine.Internal.Backports.Path;
#else
using Path = System.IO.Path;
#endif

namespace NUnit.Engine.Services
{
    public sealed class ExtensionManager : IDisposable
    {
        static readonly Logger log = InternalTrace.GetLogger(typeof(ExtensionService));
        static readonly Version ENGINE_VERSION = typeof(ExtensionService).Assembly.GetName().Version;

        private readonly IFileSystem _fileSystem;
        private readonly IAddinsFileReader _addinsReader;
        private readonly IDirectoryFinder _directoryFinder;

        private readonly List<ExtensionPoint> _extensionPoints = new List<ExtensionPoint>();
        private readonly Dictionary<string, ExtensionPoint> _pathIndex = new Dictionary<string, ExtensionPoint>();

        private readonly List<ExtensionNode> _extensions = new List<ExtensionNode>();
        private readonly List<ExtensionAssembly> _assemblies = new List<ExtensionAssembly>();

        public ExtensionManager()
            : this(new AddinsFileReader(), new FileSystem())
        {
        }

        internal ExtensionManager(IAddinsFileReader addinsReader, IFileSystem fileSystem)
            : this(addinsReader, fileSystem, new DirectoryFinder(fileSystem))
        {
        }

        internal ExtensionManager(IAddinsFileReader addinsReader, IFileSystem fileSystem, IDirectoryFinder directoryFinder)
        {
            _addinsReader = addinsReader;
            _fileSystem = fileSystem;
            _directoryFinder = directoryFinder;
        }

        internal void FindExtensions(string startDir)
        {
            // Create the list of possible extension assemblies,
            // eliminating duplicates, start in the provided directory.
            FindExtensionAssemblies(_fileSystem.GetDirectory(startDir));

            // Check each assembly to see if it contains extensions
            foreach (var candidate in _assemblies)
                FindExtensionsInAssembly(candidate);
        }

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
            get { return _extensions.ToArray(); }
        }

        /// <summary>
        /// Enable or disable an extension
        /// </summary>
        public void EnableExtension(string typeName, bool enabled)
        {
            foreach (var node in _extensions)
                if (node.TypeName == typeName)
                    node.Enabled = enabled;
        }

        /// <summary>
        /// Get an ExtensionPoint based on its unique identifying path.
        /// </summary>
        public ExtensionPoint GetExtensionPoint(string path)
        {
            return _pathIndex.ContainsKey(path) ? _pathIndex[path] : null;
        }

        /// <summary>
        /// Get an ExtensionPoint based on the required Type for extensions.
        /// </summary>
        public ExtensionPoint GetExtensionPoint(Type type)
        {
            foreach (var ep in _extensionPoints)
                if (ep.TypeName == type.FullName)
                    return ep;

            return null;
        }

        /// <summary>
        /// Get an ExtensionPoint based on a Cecil TypeReference.
        /// </summary>
        public ExtensionPoint GetExtensionPoint(TypeReference type)
        {
            foreach (var ep in _extensionPoints)
                if (ep.TypeName == type.FullName)
                    return ep;

            return null;
        }

        public IEnumerable<ExtensionNode> GetExtensionNodes(string path)
        {
            var ep = GetExtensionPoint(path);
            if (ep != null)
                foreach (var node in ep.Extensions)
                    yield return node;
        }

        public ExtensionNode GetExtensionNode(string path)
        {
            var ep = GetExtensionPoint(path);

            return ep != null && ep.Extensions.Count > 0 ? ep.Extensions[0] : null;
        }

        public IEnumerable<ExtensionNode> GetExtensionNodes<T>(bool includeDisabled = false)
        {
            var ep = GetExtensionPoint(typeof(T));
            if (ep != null)
                foreach (var node in ep.Extensions)
                    if (includeDisabled || node.Enabled)
                        yield return node;
        }

        public IEnumerable<T> GetExtensions<T>()
        {
            foreach (var node in GetExtensionNodes<T>())
                yield return (T)node.ExtensionObject;
        }

        /// <summary>
        /// Find the extension points in a loaded assembly.
        /// </summary>
        public void FindExtensionPoints(params Assembly[] targetAssemblies)
        {
            foreach (var assembly in targetAssemblies)
            {
                log.Info("Scanning {0} assembly for extension points", assembly.GetName().Name);

                foreach (ExtensionPointAttribute attr in assembly.GetCustomAttributes(typeof(ExtensionPointAttribute), false))
                {
                    if (_pathIndex.ContainsKey(attr.Path))
                    {
                        string msg = string.Format(
                            "The Path {0} is already in use for another extension point.",
                            attr.Path);
                        throw new NUnitEngineException(msg);
                    }

                    var ep = new ExtensionPoint(attr.Path, attr.Type)
                    {
                        Description = attr.Description,
                    };

                    _extensionPoints.Add(ep);
                    _pathIndex.Add(ep.Path, ep);

                    log.Info("  Found Path={0}, Type={1}", ep.Path, ep.TypeName);
                }

                foreach (Type type in assembly.GetExportedTypes())
                {
                    foreach (TypeExtensionPointAttribute attr in type.GetCustomAttributes(typeof(TypeExtensionPointAttribute), false))
                    {
                        string path = attr.Path ?? "/NUnit/Engine/TypeExtensions/" + type.Name;

                        if (_pathIndex.ContainsKey(path))
                        {
                            string msg = string.Format(
                                "The Path {0} is already in use for another extension point.",
                                attr.Path);
                            throw new NUnitEngineException(msg);
                        }

                        var ep = new ExtensionPoint(path, type)
                        {
                            Description = attr.Description,
                        };

                        _extensionPoints.Add(ep);
                        _pathIndex.Add(path, ep);

                        log.Info("  Found Path={0}, Type={1}", ep.Path, ep.TypeName);
                    }
                }
            }
        }

        /// <summary>
        /// Deduce the extension point based on the Type of an extension.
        /// Returns null if no extension point can be found that would
        /// be satisfied by the provided Type.
        /// </summary>
        private ExtensionPoint DeduceExtensionPointFromType(TypeReference typeRef)
        {
            var ep = GetExtensionPoint(typeRef);
            if (ep != null)
                return ep;

            TypeDefinition typeDef = typeRef.Resolve();

            foreach (InterfaceImplementation iface in typeDef.Interfaces)
            {
                ep = DeduceExtensionPointFromType(iface.InterfaceType);
                if (ep != null)
                    return ep;
            }

            TypeReference baseType = typeDef.BaseType;
            return baseType != null && baseType.FullName != "System.Object"
                ? DeduceExtensionPointFromType(baseType)
                : null;
        }

        /// <summary>
        /// Find candidate extension assemblies starting from a
        /// given base directory.
        /// </summary>
        /// <param name="startDir"></param>
        private void FindExtensionAssemblies(IDirectory startDir)
        {
            // First check the directory itself
            ProcessAddinsFiles(startDir, false);
        }

        /// <summary>
        /// Scans a directory for candidate addin assemblies. Note that assemblies in
        /// the directory are only scanned if no file of type .addins is found. If such
        /// a file is found, then those assemblies it references are scanned.
        /// </summary>
        private void ProcessDirectory(IDirectory startDir, bool fromWildCard)
        {
            var directoryName = startDir.FullName;
            if (WasVisited(startDir.FullName, fromWildCard))
            {
                log.Warning($"Skipping directory '{directoryName}' because it was already visited.");
            }
            else
            {
                log.Info($"Scanning directory '{directoryName}' for extensions.");
                MarkAsVisited(directoryName, fromWildCard);

                if (ProcessAddinsFiles(startDir, fromWildCard) == 0)
                {
                    foreach (var file in startDir.GetFiles("*.dll"))
                    {
                        ProcessCandidateAssembly(file.FullName, true);
                    }
                }
            }
        }

        /// <summary>
        /// Process all .addins files found in a directory
        /// </summary>
        private int ProcessAddinsFiles(IDirectory startDir, bool fromWildCard)
        {
            var addinsFiles = startDir.GetFiles("*.addins");
            var addinsFileCount = 0;

            if (addinsFiles.Any())
            {
                foreach (var file in addinsFiles)
                {
                    ProcessAddinsFile(startDir, file, fromWildCard);
                    addinsFileCount += 1;
                }
            }
            return addinsFileCount;
        }

        /// <summary>
        /// Process a .addins type file. The file contains one entry per
        /// line. Each entry may be a directory to scan, an assembly
        /// path or a wildcard pattern used to find assemblies. Blank
        /// lines and comments started by # are ignored.
        /// </summary>
        private void ProcessAddinsFile(IDirectory baseDir, IFile addinsFile, bool fromWildCard)
        {
            log.Info("Processing file " + addinsFile.FullName);

            foreach (var entry in _addinsReader.Read(addinsFile))
            {
                bool isWild = fromWildCard || entry.Contains("*");
                var args = GetBaseDirAndPattern(baseDir, entry);
                // TODO: See if we can handle '/tools/*/' efficiently by examining every
                // assembly in the directory. Otherwise try this approach:
                // 1. Check entry for ending with '/tools/*/'
                // 2. If so, examine the directory name to see if it matches a tfm.
                // 3. If it does, check to see if the implied runtime would be loadable.
                // 4. If so, process it, if not, skip it.
                if (entry.EndsWith("/"))
                {
                    foreach (var dir in _directoryFinder.GetDirectories(args.Item1, args.Item2))
                    {
                        ProcessDirectory(dir, isWild);
                    }
                }
                else
                {
                    foreach (var file in _directoryFinder.GetFiles(args.Item1, args.Item2))
                    {
                        ProcessCandidateAssembly(file.FullName, isWild);
                    }
                }
            }
        }

        private Backports.Tuple<IDirectory, string> GetBaseDirAndPattern(IDirectory baseDir, string path)
        {
            if (Path.IsPathFullyQualified(path))
            {
                if (path.EndsWith("/"))
                {
                    return new Backports.Tuple<IDirectory, string>(_fileSystem.GetDirectory(path), string.Empty);
                }
                else
                {
                    return new Backports.Tuple<IDirectory, string>(_fileSystem.GetDirectory(System.IO.Path.GetDirectoryName(path)), System.IO.Path.GetFileName(path));
                }
            }
            else
            {
                return new Backports.Tuple<IDirectory, string>(baseDir, path);
            }
        }

        private void ProcessCandidateAssembly(string filePath, bool fromWildCard)
        {
            if (WasVisited(filePath, fromWildCard)) 
                return;

            MarkAsVisited(filePath, fromWildCard);

            try
            {
                var candidate = new ExtensionAssembly(filePath, fromWildCard);

                if (!CanLoadTargetFramework(Assembly.GetEntryAssembly(), candidate))
                    return;

                for (var i = 0; i < _assemblies.Count; i++)
                {
                    var assembly = _assemblies[i];

                    if (candidate.IsDuplicateOf(assembly))
                    {
                        if (candidate.IsBetterVersionOf(assembly))
                            _assemblies[i] = candidate;

                        return;
                    }
                }

                _assemblies.Add(candidate);
            }
            catch (BadImageFormatException e)
            {
                if (!fromWildCard)
                    throw new NUnitEngineException($"Specified extension {filePath} could not be read", e);
            }
            catch (NUnitEngineException)
            {
                if (!fromWildCard)
                    throw;
            }
        }

        private readonly Dictionary<string, object> _visited = new Dictionary<string, object>();

        private bool WasVisited(string filePath, bool fromWildcard)
        {
            return _visited.ContainsKey($"path={ filePath }_visited={fromWildcard}");
        }

        private void MarkAsVisited(string filePath, bool fromWildcard)
        {
            _visited.Add($"path={ filePath }_visited={fromWildcard}", null);
        }

        /// <summary>
        /// Scan a single assembly for extensions marked by ExtensionAttribute.
        /// For each extension, create an ExtensionNode and link it to the
        /// correct ExtensionPoint. Public for testing.
        /// </summary>
        internal void FindExtensionsInAssembly(ExtensionAssembly assembly)
        {
            log.Info($"Scanning {assembly.FilePath} for Extensions");

            if (!CanLoadTargetFramework(Assembly.GetEntryAssembly(), assembly))
            {
                log.Info($"{assembly.FilePath} cannot be loaded on this runtime");
                return;
            }

            IRuntimeFramework assemblyTargetFramework = null;
#if NETFRAMEWORK
            var currentFramework = RuntimeFramework.CurrentFramework;
            assemblyTargetFramework = assembly.TargetFramework;
            if (!currentFramework.CanLoad(assemblyTargetFramework))
            {
                if (!assembly.FromWildCard)
                {
                    throw new NUnitEngineException($"Extension {assembly.FilePath} targets {assemblyTargetFramework.DisplayName}, which is not available.");
                }
                else
                {
                    log.Info($"Assembly {assembly.FilePath} targets {assemblyTargetFramework.DisplayName}, which is not available. Assembly found via wildcard.");
                    return;
                }
            }
#endif

            foreach (var type in assembly.MainModule.GetTypes())
            {
                CustomAttribute extensionAttr = type.GetAttribute("NUnit.Engine.Extensibility.ExtensionAttribute");

                if (extensionAttr == null)
                    continue;

                object versionArg = extensionAttr.GetNamedArgument("EngineVersion");
                if (versionArg != null && new Version((string)versionArg) > ENGINE_VERSION)
                    continue;

                var node = new ExtensionNode(assembly.FilePath, assembly.AssemblyVersion, type.FullName, assemblyTargetFramework)
                {
                    Path = extensionAttr.GetNamedArgument("Path") as string,
                    Description = extensionAttr.GetNamedArgument("Description") as string
                };

                object enabledArg = extensionAttr.GetNamedArgument("Enabled");
                node.Enabled = enabledArg == null || (bool)enabledArg;

                log.Info("  Found ExtensionAttribute on Type " + type.Name);

                foreach (var attr in type.GetAttributes("NUnit.Engine.Extensibility.ExtensionPropertyAttribute"))
                {
                    string name = attr.ConstructorArguments[0].Value as string;
                    string value = attr.ConstructorArguments[1].Value as string;

                    if (name != null && value != null)
                    {
                        node.AddProperty(name, value);
                        log.Info("        ExtensionProperty {0} = {1}", name, value);
                    }
                }

                _extensions.Add(node);

                ExtensionPoint ep;
                if (node.Path == null)
                {
                    ep = DeduceExtensionPointFromType(type);
                    if (ep == null)
                    {
                        string msg = string.Format(
                            "Unable to deduce ExtensionPoint for Type {0}. Specify Path on ExtensionAttribute to resolve.",
                            type.FullName);
                        throw new NUnitEngineException(msg);
                    }

                    node.Path = ep.Path;
                }
                else
                {
                    ep = GetExtensionPoint(node.Path);
                    if (ep == null)
                    {
                        string msg = string.Format(
                            "Unable to locate ExtensionPoint for Type {0}. The Path {1} cannot be found.",
                            type.FullName,
                            node.Path);
                        throw new NUnitEngineException(msg);
                    }
                }

                ep.Install(node);
            }
        }

        /// <summary>
        /// Checks that the target framework of the current runner can load the extension assembly. For example, .NET Core
        /// cannot load .NET Framework assemblies and vice-versa.
        /// </summary>
        /// <param name="runnerAsm">The executing runner</param>
        /// <param name="extensionAsm">The extension we are attempting to load</param>
        internal static bool CanLoadTargetFramework(Assembly runnerAsm, ExtensionAssembly extensionAsm)
        {
            if (runnerAsm == null)
                return true;

            string extensionFrameworkName = AssemblyDefinition.ReadAssembly(extensionAsm.FilePath).GetFrameworkName();
            string runnerFrameworkName = AssemblyDefinition.ReadAssembly(runnerAsm.Location).GetFrameworkName();
            if (runnerFrameworkName?.StartsWith(".NETStandard") == true)
            {
                throw new NUnitEngineException($"{runnerAsm.FullName} test runner must target .NET Core or .NET Framework, not .NET Standard");
            }
            else if (runnerFrameworkName?.StartsWith(".NETCoreApp") == true)
            {
                if (extensionFrameworkName?.StartsWith(".NETStandard") != true && extensionFrameworkName?.StartsWith(".NETCoreApp") != true)
                {
                    log.Info($".NET Core runners require .NET Core or .NET Standard extension for {extensionAsm.FilePath}");
                    return false;
                }
            }
            else if (extensionFrameworkName?.StartsWith(".NETCoreApp") == true)
            {
                log.Info($".NET Framework runners cannot load .NET Core extension {extensionAsm.FilePath}");
                return false;
            }

            return true;
        }

        public void Dispose()
        {
            // Make sure all assemblies release the underlying file streams. 
            foreach (var assembly in _assemblies)
            {
                assembly.Dispose();
            }
        }
    }
}
