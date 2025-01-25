// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using System;
using System.Collections.Generic;
using System.IO;
using TestCentric.Metadata;
using System.Runtime.Versioning;

namespace NUnit.Extensibility
{
    public class ExtensionAssembly : IExtensionAssembly, IDisposable
    {
        public ExtensionAssembly(string filePath, bool fromWildCard)
        {
            FilePath = filePath;
            FromWildCard = fromWildCard;
            Assembly = GetAssemblyDefinition();
            AssemblyName = Assembly.Name.Name;
            AssemblyVersion = Assembly.Name.Version;
        }

#if ACTUALLY_USED
        // Internal constructor used for certain tests. AssemblyDefinition is not initialized.
        internal ExtensionAssembly(string filePath, bool fromWildCard, string assemblyName, Version version)
        {
            FilePath = filePath;
            FromWildCard = fromWildCard;
            AssemblyName = assemblyName;
            AssemblyVersion = version;
        }
#endif

        public string FilePath { get; }
        public bool FromWildCard { get; }
        public AssemblyDefinition Assembly { get; }

        public string AssemblyName { get; }

        public Version AssemblyVersion { get; }

//#if NETFRAMEWORK
//        public RuntimeFramework TargetFramework
//        {
//            get { return new RuntimeFramework(RuntimeType.Any, Assembly.GetRuntimeVersion()); }
//        }
//#endif

        public FrameworkName FrameworkName
        {
            get
            {
                var framework = Assembly.GetFrameworkName();
                if (framework != null)
                    return new FrameworkName(framework);

                // No TargetFrameworkAttribute - Assume .NET Framework
                var runtimeVersion = Assembly.GetRuntimeVersion();
                return new FrameworkName(".NETFramework", new Version(runtimeVersion.Major, runtimeVersion.Minor));
            }
        }

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
