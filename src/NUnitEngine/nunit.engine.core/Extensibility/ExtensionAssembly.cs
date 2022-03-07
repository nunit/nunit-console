// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Versioning;
using Mono.Cecil;
#if NETFRAMEWORK
using NUnit.Engine.Compatibility;
#endif

namespace NUnit.Engine.Extensibility
{
    internal class ExtensionAssembly : IExtensionAssembly, IDisposable
    {
        private AssemblyDefinition _assemblyDefinition;

        public ExtensionAssembly(string filePath, bool fromWildCard)
        {
            FilePath = filePath;
            FromWildCard = fromWildCard;

            var resolver = new DefaultAssemblyResolver();
            resolver.AddSearchDirectory(Path.GetDirectoryName(FilePath));
            resolver.AddSearchDirectory(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location));
            var parameters = new ReaderParameters { AssemblyResolver = resolver };

            _assemblyDefinition = AssemblyDefinition.ReadAssembly(FilePath, parameters);
        }

        public string FilePath { get; }
        public bool FromWildCard { get; }
        public string AssemblyName => _assemblyDefinition.Name.Name;
        public Version AssemblyVersion => _assemblyDefinition.Name.Version;
        public IEnumerable<TypeDefinition> GetTypes() => _assemblyDefinition.MainModule.GetTypes();

        public FrameworkName TargetRuntime
        {
            get
            {
                string frameworkName = _assemblyDefinition.GetFrameworkName();

                if (frameworkName != null)
                    return new FrameworkName(frameworkName);

                // We rely on TargetRuntime being available for all assemblies later than .NET 2.0
                var runtimeVersion = _assemblyDefinition.GetRuntimeVersion();
                var frameworkVersion = new Version(runtimeVersion.Major, runtimeVersion.Minor);
                return new FrameworkName(FrameworkIdentifiers.NetFramework, frameworkVersion);
            }
        }

        

        public void Dispose() => _assemblyDefinition?.Dispose();
    }
}
