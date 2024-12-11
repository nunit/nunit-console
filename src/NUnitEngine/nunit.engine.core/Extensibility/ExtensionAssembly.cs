// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using System;
using System.Collections.Generic;
using System.IO;
using TestCentric.Metadata;
using NUnit.Engine.Internal;

namespace NUnit.Engine.Extensibility
{
    internal class ExtensionAssembly : IExtensionAssembly, IDisposable
    {
        public ExtensionAssembly(string filePath, bool fromWildCard)
        {
            FilePath = filePath;
            FromWildCard = fromWildCard;
            Assembly = GetAssemblyDefinition();
            AssemblyName = Assembly.Name.Name;
            AssemblyVersion = Assembly.Name.Version;
        }

        // Internal constructor used for certain tests. AssemblyDefinition is not initialized.
        internal ExtensionAssembly(string filePath, bool fromWildCard, string assemblyName, Version version)
        {
            FilePath = filePath;
            FromWildCard = fromWildCard;
            AssemblyName = assemblyName;
            AssemblyVersion = version;
        }

        public string FilePath { get; }
        public bool FromWildCard { get; }
        public AssemblyDefinition Assembly { get; }

        public string AssemblyName { get; }

        public Version AssemblyVersion { get; }

#if NETFRAMEWORK
        public RuntimeFramework TargetFramework
        {
            get { return new RuntimeFramework(RuntimeType.Any, Assembly.GetRuntimeVersion()); }
        }
#endif

        private AssemblyDefinition GetAssemblyDefinition()
        {
            var resolver = new DefaultAssemblyResolver();
            resolver.AddSearchDirectory(Path.GetDirectoryName(FilePath));
            resolver.AddSearchDirectory(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location));
            var parameters = new ReaderParameters { AssemblyResolver = resolver };

            return AssemblyDefinition.ReadAssembly(FilePath, parameters);
        }

        public void Dispose()
        {
            Assembly?.Dispose();
        }
    }
}
