// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using TestCentric.Metadata;
using NUnit.Engine.Extensibility;
using NUnit.Engine.Internal;
using NUnit.Engine.Internal.FileSystemAccess;
using NUnit.Engine.Internal.FileSystemAccess.Default;
using System.Linq;

namespace NUnit.Engine.Services
{
    public class ExtensionManager : IExtensionManager
    {
        static readonly Logger log = InternalTrace.GetLogger(typeof(ExtensionManager));

        private readonly IFileSystem _fileSystem;
        //private readonly IAddinsFileReader _addinsReader;
        private readonly IDirectoryFinder _directoryFinder;

        private readonly List<ExtensionPoint> _extensionPoints = new List<ExtensionPoint>();
        private readonly Dictionary<string, ExtensionPoint> _pathIndex = new Dictionary<string, ExtensionPoint>();

        private readonly List<ExtensionNode> _extensions = new List<ExtensionNode>();
        private readonly List<ExtensionAssembly> _assemblies = new List<ExtensionAssembly>();

        public ExtensionManager()
            : this(new FileSystem())
        {
        }

        internal ExtensionManager(IFileSystem fileSystem)
            : this(fileSystem, new DirectoryFinder(fileSystem))
        {
        }

        internal ExtensionManager(IFileSystem fileSystem, IDirectoryFinder directoryFinder)
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
            get { return _extensions.ToArray(); }
        }

        /// <summary>
        /// Find the extension points in a loaded assembly.
        /// </summary>
        public virtual void FindExtensionPoints(params Assembly[] targetAssemblies)
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
        /// Find and install extensions starting from a given base directory,
        /// and using the contained '.addins' files to direct the search.
        /// </summary>
        /// <param name="startDir"></param>
        public void FindExtensions(string startDir)
        {
            // Create the list of possible extension assemblies,
            // eliminating duplicates, start in the provided directory.
            FindExtensionAssemblies(_fileSystem.GetDirectory(startDir));

            // Check each assembly to see if it contains extensions
            foreach (var candidate in _assemblies)
                FindExtensionsInAssembly(candidate);
        }

        /// <summary>
        /// Find and install standard extensions for a host assembly using
        /// a built-in algorithm that searches in certain known locations.
        /// </summary>
        /// <param name="hostAssembly">An assembly that supports extensions.</param>
        public void FindStandardExtensions(Assembly hostAssembly)
        {
            bool isChocolateyPackage = System.IO.File.Exists(Path.Combine(hostAssembly.Location, "VERIFICATION.txt"));
            string[] extensionPatterns = isChocolateyPackage
                ? new[] { "nunit-extension-*/**/tools/", "nunit-extension-*/**/tools/*/" }
                : new[] { "NUnit.Extension.*/**/tools/", "NUnit.Extension.*/**/tools/*/" };

            FindExtensionAssemblies(hostAssembly, extensionPatterns);

            foreach (var candidate in _assemblies)
                FindExtensionsInAssembly(candidate);
        }

        /// <summary>
        /// Get an ExtensionPoint based on its unique identifying path.
        /// </summary>
        public IExtensionPoint GetExtensionPoint(string path)
        {
            return _pathIndex.ContainsKey(path) ? _pathIndex[path] : null;
        }

        public IEnumerable<IExtensionNode> GetExtensionNodes(string path)
        {
            var ep = GetExtensionPoint(path);
            if (ep != null)
                foreach (var node in ep.Extensions)
                    yield return node;
        }

        public IExtensionNode GetExtensionNode(string path)
        {
            // TODO: Remove need for the cast
            var ep = GetExtensionPoint(path) as ExtensionPoint;

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
                yield return (T)((ExtensionNode)node).ExtensionObject; // HACK
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

        #endregion

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
        /// Find candidate extension assemblies starting from a given base directory
        /// and using the contained '.addins' files to direct the search.
        /// </summary>
        /// <param name="initialDirectory">Path to the initial directory.</param>
        private void FindExtensionAssemblies(IDirectory initialDirectory)
        {
            // Since no patterns are provided, look for addins files
            ProcessAddinsFiles(initialDirectory, false);
        }

        /// <summary>
        /// Find candidate extension assemblies for a given host assembly,
        /// using the provided list of patterns to direct the search using
        /// a built-in algorithm.
        /// </summary>
        /// <param name="hostAssembly">An assembly that supports extensions</param>
        /// <param name="patterns">A list of patterns used to identify potential candidates.</param>
        private void FindExtensionAssemblies(Assembly hostAssembly, string[] patterns)
        {
            IDirectory startDir = _fileSystem.GetDirectory(AssemblyHelper.GetDirectoryName(hostAssembly));

            while (startDir != null)
            {               
                foreach (var pattern in patterns)
                    foreach (var dir in _directoryFinder.GetDirectories(startDir, pattern))
                        ProcessDirectory(dir, true);

                startDir = startDir.Parent;
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

                if (entry.IsDirectory)
                {
                    if (entry.IsFullyQualified)
                    {
                        baseDir = _fileSystem.GetDirectory(entry.Text);
                        foreach (var dir in _directoryFinder.GetDirectories(_fileSystem.GetDirectory(entryDir), ""))
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

            foreach (var extensionType in assembly.MainModule.GetTypes())
            {
                CustomAttribute extensionAttr = extensionType.GetAttribute("NUnit.Engine.Extensibility.ExtensionAttribute");

                if (extensionAttr == null)
                    continue;

                // TODO: This is a remnant of older code. In principle, this should be generalized
                // to something like "HostVersion". However, this can safely remain until
                // we separate ExtensionManager into its own assembly.
                string versionArg = extensionAttr.GetNamedArgument("EngineVersion") as string;
                if (versionArg != null)
                {
                    Assembly THIS_ASSEMBLY = Assembly.GetExecutingAssembly();
                    Version ENGINE_VERSION = THIS_ASSEMBLY.GetName().Version;
                    if (new Version(versionArg) > ENGINE_VERSION)
                    {
                        log.Warning($"  Ignoring {extensionType.Name}. It requires version {versionArg}.");
                        continue;
                    }
                }

                var node = new ExtensionNode(assembly.FilePath, assembly.AssemblyVersion, extensionType.FullName, assemblyTargetFramework)
                {
                    Path = extensionAttr.GetNamedArgument("Path") as string,
                    Description = extensionAttr.GetNamedArgument("Description") as string
                };

                object enabledArg = extensionAttr.GetNamedArgument("Enabled");
                node.Enabled = enabledArg == null || (bool)enabledArg;

                log.Info("  Found ExtensionAttribute on Type " + extensionType.Name);

                foreach (var attr in extensionType.GetAttributes("NUnit.Engine.Extensibility.ExtensionPropertyAttribute"))
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
                    ep = DeduceExtensionPointFromType(extensionType);
                    if (ep == null)
                    {
                        string msg = string.Format(
                            "Unable to deduce ExtensionPoint for Type {0}. Specify Path on ExtensionAttribute to resolve.",
                            extensionType.FullName);
                        throw new NUnitEngineException(msg);
                    }

                    node.Path = ep.Path;
                }
                else
                {
                    // TODO: Remove need for the cast
                    ep = GetExtensionPoint(node.Path) as ExtensionPoint;
                    if (ep == null)
                    {
                        string msg = string.Format(
                            "Unable to locate ExtensionPoint for Type {0}. The Path {1} cannot be found.",
                            extensionType.FullName,
                            node.Path);
                        throw new NUnitEngineException(msg);
                    }
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
        internal bool CanLoadTargetFramework(Assembly runnerAsm, ExtensionAssembly extensionAsm)
        {
            if (runnerAsm == null)
                return true;

            var runnerFrameworkName = GetTargetRuntime(runnerAsm.Location);
            var extensionFrameworkName = GetTargetRuntime(extensionAsm.FilePath);

            switch (runnerFrameworkName.Identifier)
            {
                case NETSTANDARD:
                    throw new Exception($"{runnerAsm.FullName} test runner must target .NET Core or .NET Framework, not .NET Standard");

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
            foreach (var assembly in _assemblies)
            {
                assembly.Dispose();
            }
        }
    }
}
