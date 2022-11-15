// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

#if NETCOREAPP3_1_OR_GREATER

using System.Reflection;
using System.Runtime.Loader;
using System.IO;
using System.Linq;
using System;

namespace NUnit.Engine.Internal
{
    internal class CustomAssemblyLoadContext : AssemblyLoadContext
    {
        private readonly AssemblyDependencyResolver _resolver;
        private readonly string _basePath;

        public CustomAssemblyLoadContext(string mainAssemblyToLoadPath)
        {
            _resolver = new AssemblyDependencyResolver(mainAssemblyToLoadPath);
            _basePath = Path.GetDirectoryName(mainAssemblyToLoadPath);
        }

        protected override Assembly Load(AssemblyName name)
        {
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            var loaded = assemblies.FirstOrDefault(x => x.GetName().Name == name.Name);
            if (loaded != null)
                return loaded;

            var assemblyPath = _resolver.ResolveAssemblyToPath(name);
            return assemblyPath != null ? LoadFromAssemblyPath(assemblyPath) : null;
        }

        /// <summary>
        /// Loads assemblies that are dependencies, and in the same folder as the parent assembly,
        /// but are not fully specified in parent assembly deps.json file. This happens when the
        /// dependencies reference in the csproj file has CopyLocal=false, and for example, the
        /// reference is a projectReference and has the same output directory as the parent.
        ///
        /// LoadFallback should be called via the CustomAssemblyLoadContext.Resolving callback when
        /// a dependent assembly of that referred to in a previous 'CustomAssemblyLoadContext.Load' call
        /// could not be loaded by CustomAssemblyLoadContext.Load nor by the default ALC; to which the
        /// runtime will fallback when CustomAssemblyLoadContext.Load fails (to let the default ALC
        /// load system assemblies).
        /// </summary>
        /// <param name="assemblyName"></param>
        /// <returns></returns>
        public Assembly LoadFallback(AssemblyName name)
        {
            string assemblyPath = Path.Combine(_basePath, name.Name + ".dll");
            if (File.Exists(assemblyPath))
                return LoadFromAssemblyPath(assemblyPath);
            return null;
        }
    }
}

#endif
