﻿// ***********************************************************************
// Copyright (c) 2015—2018 Charlie Poole, Rob Prouse
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
// ***********************************************************************

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using NUnit.Engine.Extensibility;
using NUnit.Engine.Internal;
using NUnit.Engine.Internal.Metadata;

namespace NUnit.Engine.Services
{
    /// <summary>
    /// The ExtensionService discovers ExtensionPoints and Extensions and
    /// maintains them in a database. It can return extension nodes or
    /// actual extension objects on request.
    /// </summary>
    public class ExtensionService : Service, IExtensionService
    {
        static Logger log = InternalTrace.GetLogger(typeof(ExtensionService));
        static readonly Version ENGINE_VERSION = typeof(TestEngine).Assembly.GetName().Version;

        private readonly List<ExtensionPoint> _extensionPoints = new List<ExtensionPoint>();
        private readonly Dictionary<string, ExtensionPoint> _pathIndex = new Dictionary<string, ExtensionPoint>();

        private readonly List<ExtensionNode> _extensions = new List<ExtensionNode>();
        private readonly List<ExtensionAssembly> _assemblies = new List<ExtensionAssembly>();

        #region IExtensionService Members

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
            get { return _extensions.ToArray();  }
        }

        /// <summary>
        /// Get an ExtensionPoint based on its unique identifying path.
        /// </summary>
        IExtensionPoint IExtensionService.GetExtensionPoint(string path)
        {
            return this.GetExtensionPoint(path);
        }

        /// <summary>
        /// Get an enumeration of ExtensionNodes based on their identifying path.
        /// </summary>
        IEnumerable<IExtensionNode> IExtensionService.GetExtensionNodes(string path)
        {
            foreach (var node in this.GetExtensionNodes(path))
                yield return node;
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

        #region Public Methods - Extension Points

        /// <summary>
        /// Get an extension point based on its unique identifying path.
        /// </summary>
        public ExtensionPoint GetExtensionPoint(string path)
        {
            return _pathIndex.ContainsKey(path) ? _pathIndex[path] : null;
        }

        /// <summary>
        /// Gets the extension point with the specified full type name.
        /// </summary>
        public ExtensionPoint GetExtensionPointFromTypeName(string fullName)
        {
            foreach (var ep in _extensionPoints)
                if (ep.TypeName == fullName)
                    return ep;

            return null;
        }

        #endregion

        #region Public Methods - Extensions

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
            var ep = GetExtensionPoint(typeof(T).FullName);
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

        #endregion

        #region Service Overrides

        public override void StartService()
        {
            try
            {
                var thisAssembly = Assembly.GetExecutingAssembly();
                var apiAssembly = typeof(ITestEngine).Assembly;

                FindExtensionPoints(thisAssembly);
                FindExtensionPoints(apiAssembly);

                // Create the list of possible extension assemblies,
                // eliminating duplicates. Start in Engine directory.
                var startDir = new DirectoryInfo(AssemblyHelper.GetDirectoryName(thisAssembly));
                FindExtensionAssemblies(startDir);

                // Check each assembly to see if it contains extensions
                foreach (var candidate in _assemblies)
                    FindExtensionsInAssembly(candidate);

                Status = ServiceStatus.Started;
            }
            catch
            {
                Status = ServiceStatus.Error;
                throw;
            }
        }

        #endregion

        #region Helper Methods - Extension Points

        /// <summary>
        /// Find the extension points in a loaded assembly.
        /// Public for testing.
        /// </summary>
        public void FindExtensionPoints(Assembly assembly)
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

        /// <summary>
        /// Deduce the extension point based on the Type of an extension.
        /// Returns null if no extension point can be found that would
        /// be satisfied by the provided Type.
        /// </summary>
        private ExtensionPoint DeduceExtensionPointFromType(ITypeMetadataProvider typeDef, ExtensionAssembly extensionAssembly)
        {
            var ep = GetExtensionPointFromTypeName(typeDef.FullName);
            if (ep != null)
                return ep;

            foreach (var interfaceTypeRef in typeDef.Interfaces)
            {
                ep = DeduceExtensionPointFromType(interfaceTypeRef.Resolve(extensionAssembly.ResolveAssemblyPath), extensionAssembly);
                if (ep != null)
                    return ep;
            }

            var baseTypeRef = typeDef.BaseType;
            if (baseTypeRef == null) return null;

            return DeduceExtensionPointFromType(baseTypeRef.Resolve(extensionAssembly.ResolveAssemblyPath), extensionAssembly);
        }

        #endregion

        #region Helper Methods - Extensions

        /// <summary>
        /// Find candidate extension assemblies starting from a
        /// given base directory.
        /// </summary>
        /// <param name="startDir"></param>
        private void FindExtensionAssemblies(DirectoryInfo startDir)
        {
            // First check the directory itself
            ProcessAddinsFiles(startDir, false);
        }

        /// <summary>
        /// Scans a directory for candidate addin assemblies. Note that assemblies in
        /// the directory are only scanned if no file of type .addins is found. If such
        /// a file is found, then those assemblies it references are scanned.
        /// </summary>
        private void ProcessDirectory(DirectoryInfo startDir, bool fromWildCard)
        {
            log.Info("Scanning directory {0} for extensions", startDir.FullName);

            if (ProcessAddinsFiles(startDir, fromWildCard) == 0)
                foreach (var file in startDir.GetFiles("*.dll"))
                    ProcessCandidateAssembly(file.FullName, true);
        }

        /// <summary>
        /// Process all .addins files found in a directory
        /// </summary>
        private int ProcessAddinsFiles(DirectoryInfo startDir, bool fromWildCard)
        {
            var addinsFiles = startDir.GetFiles("*.addins");

            if (addinsFiles.Length > 0)
                foreach (var file in addinsFiles)
                    ProcessAddinsFile(startDir, file.FullName, fromWildCard);

            return addinsFiles.Length;
        }

        /// <summary>
        /// Process a .addins type file. The file contains one entry per
        /// line. Each entry may be a directory to scan, an assembly
        /// path or a wildcard pattern used to find assemblies. Blank
        /// lines and comments started by # are ignored.
        /// </summary>
        private void ProcessAddinsFile(DirectoryInfo baseDir, string fileName, bool fromWildCard)
        {
            log.Info("Processing file " + fileName);

            using (var rdr = new StreamReader(fileName))
            {
                while (!rdr.EndOfStream)
                {
                    var line = rdr.ReadLine();
                    if (line == null)
                        break;

                    line = line.Split(new char[] { '#' })[0].Trim();

                    if (line == string.Empty)
                        continue;

                    if (Path.DirectorySeparatorChar == '\\')
                        line = line.Replace(Path.DirectorySeparatorChar, '/');

                    bool isWild = fromWildCard || line.Contains("*");
                    if (line.EndsWith("/"))
                        foreach (var dir in DirectoryFinder.GetDirectories(baseDir, line))
                            ProcessDirectory(dir, isWild);
                    else
                        foreach (var file in DirectoryFinder.GetFiles(baseDir, line))
                            ProcessCandidateAssembly(file.FullName, isWild);
                }
            }
        }

        private void ProcessCandidateAssembly(string filePath, bool fromWildCard)
        {
            if (!Visited(filePath))
            {
                Visit(filePath);

                try
                {
                    var candidate = new ExtensionAssembly(filePath, fromWildCard);

                    for (int i = 0; i < _assemblies.Count; i++)
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
                        throw new NUnitEngineException(String.Format("Specified extension {0} could not be read", filePath), e);
                }
                catch (NUnitEngineException)
                {
                    if (!fromWildCard)
                        throw;
                }
            }
        }

        private Dictionary<string, object> _visited = new Dictionary<string, object>();

        private bool Visited(string filePath)
        {
            return _visited.ContainsKey(filePath);
        }

        private void Visit(string filePath)
        {
            _visited.Add(filePath, null);
        }

        /// <summary>
        /// Scan a single assembly for extensions marked by ExtensionAttribute.
        /// For each extension, create an ExtensionNode and link it to the
        /// correct ExtensionPoint. Public for testing.
        /// </summary>
        internal void FindExtensionsInAssembly(ExtensionAssembly assembly)
        {
            log.Info("Scanning {0} assembly for Extensions", assembly.FilePath);

            var currentFramework = RuntimeFramework.CurrentFramework;
            var assemblyTargetFramework = assembly.TargetFramework;
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

            foreach (var type in assembly.Metadata.Types)
            {
                var extensionAttributes = type.GetAttributes("NUnit.Engine.Extensibility.ExtensionAttribute");
                if (!extensionAttributes.TryFirst(IsCompatible, out var extensionAttr)) continue;

                var node = new ExtensionNode(assembly.FilePath, type.FullName, assemblyTargetFramework);
                node.Path = extensionAttr.GetNamedArgumentOrDefault("Path") as string;
                node.Description = extensionAttr.GetNamedArgumentOrDefault("Description") as string;

                node.Enabled = !false.Equals(extensionAttr.GetNamedArgumentOrDefault("Enabled"));

                log.Info("  Found ExtensionAttribute on Type " + type.FullName);

                foreach (var attr in type.GetAttributes("NUnit.Engine.Extensibility.ExtensionPropertyAttribute"))
                {
                    if (attr.PositionalArguments.Length != 2) continue;
                    if (!(attr.PositionalArguments[0] is string name)) continue;
                    var value = attr.PositionalArguments[1]?.ToString();

                    node.AddProperty(name, value);
                    log.Info("        ExtensionProperty {0} = {1}", name, value);
                }

                _extensions.Add(node);

                ExtensionPoint ep;
                if (node.Path == null)
                {
                    ep = DeduceExtensionPointFromType(type, assembly);
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

        private static bool IsCompatible(AttributeMetadata attribute)
        {
            return !(
                attribute.GetNamedArgumentOrDefault("EngineVersion") is string str
                && ENGINE_VERSION < new Version(str));
        }

        #endregion
    }
}
