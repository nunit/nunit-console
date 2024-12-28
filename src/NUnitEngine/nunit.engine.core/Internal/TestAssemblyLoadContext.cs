// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

#if NETCOREAPP3_1_OR_GREATER

using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Loader;
using System.IO;
using System;
using System.Linq;

namespace NUnit.Engine.Internal
{
    internal sealed class TestAssemblyLoadContext : AssemblyLoadContext
    {
        private static readonly Logger log = InternalTrace.GetLogger(typeof(TestAssemblyLoadContext));

        private readonly string _basePath;
        private readonly TestAssemblyResolver _resolver;
        private readonly System.Runtime.Loader.AssemblyDependencyResolver _runtimeResolver;

        public TestAssemblyLoadContext(string testAssemblyPath)
        {
            _resolver = new TestAssemblyResolver(this, testAssemblyPath);
            _basePath = Path.GetDirectoryName(testAssemblyPath);
            _runtimeResolver = new AssemblyDependencyResolver(testAssemblyPath);
#if NET8_0_OR_GREATER
            AppContext.SetData("APP_CONTEXT_BASE_DIRECTORY", _basePath);
#endif
        }

        protected override Assembly Load(AssemblyName name)
        {
            log.Debug("Loading {0} assembly", name);

            var loadedAssembly = base.Load(name);
            if (loadedAssembly != null)
            {
                log.Info("Assembly {0} ({1}) is loaded using default base.Load()", name, GetAssemblyLocationInfo(loadedAssembly));
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
                log.Info("Assembly {0} ({1}) is loaded using the deps.json info", name, GetAssemblyLocationInfo(loadedAssembly));
                return loadedAssembly;
            }

            loadedAssembly = _resolver.Resolve(this, name);
            if (loadedAssembly != null)
            {
                log.Info("Assembly {0} ({1}) is loaded using the TestAssembliesResolver", name, GetAssemblyLocationInfo(loadedAssembly));

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

            if (loadedAssembly != null)
            {
                log.Info("Assembly {0} ({1}) is loaded using base path", name, GetAssemblyLocationInfo(loadedAssembly));
                return loadedAssembly;
            }

            return loadedAssembly;
        }

        protected override IntPtr LoadUnmanagedDll(string name)
        {
            log.Debug("Loading {0} unmanaged dll", name);

            IntPtr loadedDllHandle = base.LoadUnmanagedDll(name);
            if (loadedDllHandle != IntPtr.Zero)
            {
                log.Info("Unmanaged DLL {0} is loaded using default base.LoadUnmanagedDll()", name);
                return loadedDllHandle;
            }

            string runtimeResolverPath = _runtimeResolver.ResolveUnmanagedDllToPath(name);
            if (string.IsNullOrEmpty(runtimeResolverPath) == false &&
                File.Exists(runtimeResolverPath))
            {
                loadedDllHandle = LoadUnmanagedDllFromPath(runtimeResolverPath);
            }

            if (loadedDllHandle != IntPtr.Zero)
            {
                log.Info("Unmanaged DLL {0} ({1}) is loaded using the deps.json info", name, runtimeResolverPath);
                return loadedDllHandle;
            }

            string unmanagedDllPath = Path.Combine(_basePath, name + ".dll");
            if (File.Exists(unmanagedDllPath))
            {
                loadedDllHandle = LoadUnmanagedDllFromPath(unmanagedDllPath);
            }

            if (loadedDllHandle != IntPtr.Zero)
            {
                log.Info("Unmanaged DLL {0} ({1}) is loaded using base path", name, unmanagedDllPath);
                return loadedDllHandle;
            }

            return IntPtr.Zero;
        }

        private static string GetAssemblyLocationInfo(Assembly assembly)
        {
            if (assembly.IsDynamic)
            {
                return $"Dynamic {assembly.FullName}";
            }

            if (string.IsNullOrEmpty(assembly.Location))
            {
                return $"No location for {assembly.FullName}";
            }

            return $"{assembly.FullName} from {assembly.Location}";
        }
    }
}

#endif
