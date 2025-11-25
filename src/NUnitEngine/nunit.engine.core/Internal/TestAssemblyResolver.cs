// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

#if NETCOREAPP3_1_OR_GREATER

using Microsoft.Extensions.DependencyModel;
using Microsoft.Extensions.DependencyModel.Resolution;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Loader;
using TestCentric.Metadata;

namespace NUnit.Engine.Internal
{
    internal sealed class TestAssemblyResolver : IDisposable
    {
        private static readonly Logger log = InternalTrace.GetLogger(typeof(TestAssemblyResolver));

        private readonly AssemblyLoadContext _loadContext;

        // Our Strategies for resolving references
        List<ResolutionStrategy> ResolutionStrategies;

        public TestAssemblyResolver(AssemblyLoadContext loadContext, string testAssemblyPath)
        {
            _loadContext = loadContext;

            InitializeResolutionStrategies(loadContext, testAssemblyPath);

            _loadContext.Resolving += OnResolving;
        }

        private void InitializeResolutionStrategies(AssemblyLoadContext loadContext, string testAssemblyPath)
        {
            // Decide whether to try WindowsDeskTop and/or AspNetCore runtimes before any others.
            // We base this on direct references only, so we will eventually try each of them
            // later in case there are any indirect references.
            AssemblyDefinition assemblyDef = AssemblyDefinition.ReadAssembly(testAssemblyPath);
            bool tryWindowsDesktopFirst = false;
            bool tryAspNetCoreFirst = false;
            foreach (var reference in assemblyDef.MainModule.GetTypeReferences())
            {
                string fn = reference.FullName;
                if (fn.StartsWith("System.Windows.") || fn.StartsWith("PresentationFramework"))
                    tryWindowsDesktopFirst = true;
                if (fn.StartsWith("Microsoft.AspNetCore."))
                    tryAspNetCoreFirst = true;
            }

            // Initialize the list of ResolutionStrategies in the best order depending on
            // what we learned.
            ResolutionStrategies = new List<ResolutionStrategy>();
            
            if (tryWindowsDesktopFirst)
                ResolutionStrategies.Add(new WindowsDesktopStrategy());
            if (tryAspNetCoreFirst)
                ResolutionStrategies.Add(new AspNetCoreStrategy());

            ResolutionStrategies.Add(new TrustedPlatformAssembliesStrategy());
            ResolutionStrategies.Add(new RuntimeLibrariesStrategy(loadContext, testAssemblyPath));

            if (!tryWindowsDesktopFirst)
                ResolutionStrategies.Add(new WindowsDesktopStrategy());
            if (!tryAspNetCoreFirst)
                ResolutionStrategies.Add(new AspNetCoreStrategy());
        }

        public void Dispose()
        {
            _loadContext.Resolving -= OnResolving;
        }

        public Assembly Resolve(AssemblyLoadContext context, AssemblyName assemblyName)
        {
            return OnResolving(context, assemblyName);
        }

        private Assembly OnResolving(AssemblyLoadContext loadContext, AssemblyName assemblyName)
        {
            if (loadContext == null) throw new ArgumentNullException("context");

            Assembly loadedAssembly;
            foreach (var strategy in ResolutionStrategies)
                if (strategy.TryToResolve(loadContext, assemblyName, out loadedAssembly))
                    return loadedAssembly;

            log.Info("Cannot resolve assembly '{0}'", assemblyName);
            return null;
        }

        #region Nested ResolutionStrategy Classes

        public abstract class ResolutionStrategy
        {
            public abstract bool TryToResolve(
                AssemblyLoadContext loadContext, AssemblyName assemblyName, out Assembly loadedAssembly);
        }

        public class TrustedPlatformAssembliesStrategy : ResolutionStrategy
        {
            public override bool TryToResolve(
                AssemblyLoadContext loadContext, AssemblyName assemblyName, out Assembly loadedAssembly)
            {
                return TryLoadFromTrustedPlatformAssemblies(loadContext, assemblyName, out loadedAssembly);
            }

            private static bool TryLoadFromTrustedPlatformAssemblies(
                AssemblyLoadContext loadContext, AssemblyName assemblyName, out Assembly loadedAssembly)
            {
                // https://learn.microsoft.com/en-us/dotnet/core/dependency-loading/default-probing
                loadedAssembly = null;
                var trustedAssemblies = System.AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES") as string;
                if (string.IsNullOrEmpty(trustedAssemblies))
                {
                    return false;
                }

                var separator = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? ";" : ":";
                foreach (var assemblyPath in trustedAssemblies.Split(separator))
                {
                    var fileName = Path.GetFileNameWithoutExtension(assemblyPath);
                    if (FileMatchesAssembly(fileName) && File.Exists(assemblyPath))
                    {
                        loadedAssembly = loadContext.LoadFromAssemblyPath(assemblyPath);
                        log.Info("'{0}' assembly is loaded from trusted path '{1}'", assemblyPath, loadedAssembly.Location);

                        return true;
                    }
                }

                return false;

                bool FileMatchesAssembly(string fileName) =>
                    string.Equals(fileName, assemblyName.Name, StringComparison.InvariantCultureIgnoreCase);
            }
        }

        public class RuntimeLibrariesStrategy : ResolutionStrategy
        {
            private DependencyContext _dependencyContext;
            private readonly ICompilationAssemblyResolver _assemblyResolver;

            public RuntimeLibrariesStrategy(AssemblyLoadContext loadContext, string testAssemblyPath)
            {
                _dependencyContext = DependencyContext.Load(loadContext.LoadFromAssemblyPath(testAssemblyPath));

                _assemblyResolver = new CompositeCompilationAssemblyResolver(new ICompilationAssemblyResolver[]
                {
                    new AppBaseCompilationAssemblyResolver(Path.GetDirectoryName(testAssemblyPath)),
                    new ReferenceAssemblyPathResolver(),
                    new PackageCompilationAssemblyResolver()
                });
            }

            public override bool TryToResolve(
                AssemblyLoadContext loadContext, AssemblyName assemblyName, out Assembly loadedAssembly)
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
                        if (assemblyName.Name == Path.GetFileNameWithoutExtension(assemblyPath))
                        {
                            loadedAssembly = loadContext.LoadFromAssemblyPath(assemblyPath);
                            log.Info("'{0}' ({1}) assembly is loaded from runtime libraries {2} dependencies",
                                assemblyName,
                                loadedAssembly.Location,
                                library.Name);

                            return true;
                        }
                    }
                }

                loadedAssembly = null;
                return false;
            }
        }

        public class AdditionalRuntimesStrategy : ResolutionStrategy
        {
            private IEnumerable<DotNet.RuntimeInfo> _additionalRuntimes;

            public AdditionalRuntimesStrategy(string runtimeName)
            {
                _additionalRuntimes = DotNet.GetRuntimes(runtimeName, !Environment.Is64BitProcess);
            }

            public override bool TryToResolve(AssemblyLoadContext loadContext, AssemblyName assemblyName, out Assembly loadedAssembly)
            {
                loadedAssembly = null;

                DotNet.RuntimeInfo runtime;
                if (!FindBestRuntime(assemblyName, out runtime))
                    return false;

                string candidate = Path.Combine(runtime.Path, runtime.Version.ToString(), assemblyName.Name + ".dll");
                if (!File.Exists(candidate))
                    return false;

                loadedAssembly = loadContext.LoadFromAssemblyPath(candidate);
                return true;
            }

            private bool FindBestRuntime(AssemblyName assemblyName, out DotNet.RuntimeInfo bestRuntime)
            {
                bestRuntime = null;
                var targetVersion = new Version(assemblyName.Version.Major, assemblyName.Version.Minor, assemblyName.Version.Build);

                if (targetVersion is null)
                    return false;

                foreach (var candidate in _additionalRuntimes)
                {
                    if (candidate.Version >= targetVersion)
                        if (bestRuntime is null || bestRuntime.Version > candidate.Version)
                            bestRuntime = candidate;
                }

                return bestRuntime is not null;
            }
        }

        public class WindowsDesktopStrategy : AdditionalRuntimesStrategy
        {
            public WindowsDesktopStrategy() : base("Microsoft.WindowsDesktop.App") { }
        }

        public class AspNetCoreStrategy : AdditionalRuntimesStrategy
        {
            public AspNetCoreStrategy() : base("Microsoft.AspNetCore.App") { }
        }

        #endregion
    }
}
#endif
