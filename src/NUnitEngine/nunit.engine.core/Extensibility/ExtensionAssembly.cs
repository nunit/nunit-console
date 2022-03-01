// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using System;
using System.IO;
using Mono.Cecil;
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
        }

        public string FilePath { get; }
        public bool FromWildCard { get; }
        public AssemblyDefinition Assembly { get; }

        public string AssemblyName
        {
            get { return Assembly.Name.Name; }
        }

        public Version AssemblyVersion
        {
            get { return Assembly.Name.Version; }
        }

        public ModuleDefinition MainModule
        {
            get { return Assembly.MainModule; }
        }

#if NETFRAMEWORK
        public RuntimeFramework TargetFramework
        {
            get
            {
                var frameworkName = Assembly.GetFrameworkName();
                if (frameworkName != null)
                    return RuntimeFramework.FromFrameworkName(frameworkName);

                // No TargetFrameworkAttribute - Assume .NET Framework
                var runtimeVersion = Assembly.GetRuntimeVersion();
                return new RuntimeFramework(Runtime.Net, new Version(runtimeVersion.Major, runtimeVersion.Minor));
            }
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
