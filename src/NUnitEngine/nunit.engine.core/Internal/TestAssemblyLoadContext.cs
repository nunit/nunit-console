// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

#if NETCOREAPP3_1_OR_GREATER

using System.Reflection;
using System.Runtime.Loader;
using System.IO;
using System;
using System.Linq;

namespace NUnit.Engine.Internal
{
    internal sealed class TestAssemblyLoadContext : AssemblyLoadContext
    {
        private readonly string _testAssemblyPath;
        private readonly string _basePath;
        private readonly TestAssemblyResolver _resolver;
        private readonly System.Runtime.Loader.AssemblyDependencyResolver _runtimeResolver;

        public TestAssemblyLoadContext(string testAssemblyPath)
        {
            _testAssemblyPath = testAssemblyPath;
            _resolver = new TestAssemblyResolver(this, testAssemblyPath);
            _basePath = Path.GetDirectoryName(testAssemblyPath);
            _runtimeResolver = new AssemblyDependencyResolver(testAssemblyPath);
        }

        protected override Assembly Load(AssemblyName name)
        {
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            var loadedAssembly = assemblies.FirstOrDefault(x => x.GetName().Name == name.Name);
            if (loadedAssembly != null)
            {
                return loadedAssembly;
            }

            loadedAssembly = base.Load(name);
            if (loadedAssembly != null)
            {
                return loadedAssembly;
            }

            var runtimeResolverPath = _runtimeResolver.ResolveAssemblyToPath(name);
            if (string.IsNullOrEmpty(runtimeResolverPath) == false &&
                File.Exists(runtimeResolverPath))
            {
                loadedAssembly = LoadFromAssemblyPath(runtimeResolverPath);
            }

            if (loadedAssembly != null)
            {
                return loadedAssembly;
            }

            loadedAssembly = _resolver.Resolve(this, name);
            if (loadedAssembly != null)
            {
                return loadedAssembly;
            }

            // Load assemblies that are dependencies, and in the same folder as the test assembly,
            // but are not fully specified in test assembly deps.json file. This happens when the
            // dependencies reference in the csproj file has CopyLocal=false, and for example, the
            // reference is a projectReference and has the same output directory as the parent.
            string assemblyPath = Path.Combine(_basePath, name.Name + ".dll");
            if (File.Exists(assemblyPath))
            {
                loadedAssembly = LoadFromAssemblyPath(assemblyPath);
            }

            return loadedAssembly;
        }
    }
}

#endif
