// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using System.Collections;
using System.Collections.Generic;

namespace NUnit.Extensibility
{
    /// <summary>
    /// This is a simple utility class used by the ExtensionManager to keep track of ExtensionAssemblies.
    /// It maps assemblies by there name nad keeps track of evealuated assembly paths.
    /// It allows writing tests to show that no duplicate extension assemblies are loaded.
    /// </summary>
    internal class ExtensionAssemblyTracker : IEnumerable<ExtensionAssembly>
    {
        private static readonly Logger log = InternalTrace.GetLogger(typeof(ExtensionAssemblyTracker));

        private readonly HashSet<string> _evaluatedPaths = new HashSet<string>();
        private readonly Dictionary<string, ExtensionAssembly> _byName = new Dictionary<string, ExtensionAssembly>();

        public int Count => +_byName.Count;

        public IEnumerator<ExtensionAssembly> GetEnumerator() => _byName.Values.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public bool ContainsPath(string path) => _evaluatedPaths.Contains(path);

        public void AddOrUpdate(ExtensionAssembly candidateAssembly)
        {
            string assemblyName = candidateAssembly.AssemblyName;
            _evaluatedPaths.Add(candidateAssembly.FilePath);

            // Do we already have a copy of the same assembly at a different path?
            if (_byName.TryGetValue(assemblyName, out var existing))
            {
                if (candidateAssembly.IsBetterVersionOf(existing))
                {
                    _byName[assemblyName] = candidateAssembly;
                    log.Debug($"Newer version added for assembly: {assemblyName}");
                }
            }
            else
            {
                _byName[assemblyName] = candidateAssembly;
                log.Debug($"Assembly added: {assemblyName}");
            }
        }
    }
}
