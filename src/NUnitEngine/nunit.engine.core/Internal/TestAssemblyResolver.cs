// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

#if NETCOREAPP3_1_OR_GREATER

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using Microsoft.Extensions.DependencyModel;
using Microsoft.Extensions.DependencyModel.Resolution;

namespace NUnit.Engine.Internal
{
    internal sealed class TestAssemblyResolver : IDisposable
    {
        private readonly ICompilationAssemblyResolver _assemblyResolver;
        private readonly DependencyContext _dependencyContext;
        private readonly AssemblyLoadContext _loadContext;
        private readonly string _basePath;

        public TestAssemblyResolver(AssemblyLoadContext loadContext, string assemblyPath)
        {
            _loadContext = loadContext;
            _dependencyContext = DependencyContext.Load(loadContext.LoadFromAssemblyPath(assemblyPath));
            _basePath = Path.GetDirectoryName(assemblyPath);

            _assemblyResolver = new CompositeCompilationAssemblyResolver(new ICompilationAssemblyResolver[]
            {
                new AppBaseCompilationAssemblyResolver(Path.GetDirectoryName(assemblyPath)),
                new ReferenceAssemblyPathResolver(),
                new PackageCompilationAssemblyResolver()
            });

            _loadContext.Resolving += OnResolving;
        }

        public void Dispose()
        {
            _loadContext.Resolving -= OnResolving;
        }

        private Assembly OnResolving(AssemblyLoadContext context, AssemblyName name)
        {
            foreach (var library in _dependencyContext.RuntimeLibraries)
            {
                var wrapper = new CompilationLibrary(
                    library.Type,
                    library.Name,
                    library.Version,
                    library.Hash,
                    library.RuntimeAssemblyGroups.SelectMany(g => g.AssetPaths),
                    library.Dependencies,
                    library.Serviceable);

                var assemblies = new List<string>();
                _assemblyResolver.TryResolveAssemblyPaths(wrapper, assemblies);

                foreach (var assemblyPath in assemblies)
                {
                    if (name.Name == Path.GetFileNameWithoutExtension(assemblyPath))
                        return _loadContext.LoadFromAssemblyPath(assemblyPath);
                }
            }

            return null;
        }
    }
}
#endif
