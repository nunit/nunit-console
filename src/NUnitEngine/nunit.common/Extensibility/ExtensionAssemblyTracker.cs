// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using System.Collections.Generic;

namespace NUnit.Extensibility
{
    /// <summary>
    /// This is a simple utility class used by the ExtensionManager to keep track of ExtensionAssemblies.
    /// It is a List of ExtensionAssemblies and also provides indices by file path and assembly name.
    /// It allows writing tests to show that no duplicate extension assemblies are loaded.
    /// </summary>
    internal class ExtensionAssemblyTracker : List<ExtensionAssembly>
    {
        public Dictionary<string, ExtensionAssembly> ByPath = new Dictionary<string, ExtensionAssembly>();
        public Dictionary<string, ExtensionAssembly> ByName = new Dictionary<string, ExtensionAssembly>();

        public new void Add(ExtensionAssembly assembly)
        {
            base.Add(assembly);
            ByPath.Add(assembly.FilePath, assembly);
            ByName.Add(assembly.AssemblyName, assembly);
        }
    }
}
