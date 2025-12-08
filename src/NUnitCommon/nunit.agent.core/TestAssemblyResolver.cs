// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

#if NETCOREAPP3_1_OR_GREATER

using Microsoft.Extensions.DependencyModel;
using Microsoft.Extensions.DependencyModel.Resolution;
using Microsoft.Win32;
using NUnit.Common;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Loader;
using System.Xml.Linq;
using TestCentric.Metadata;

namespace NUnit.Engine.Internal
{
    internal sealed class TestAssemblyResolver : IDisposable
    {
        private static readonly Logger log = InternalTrace.GetLogger(typeof(TestAssemblyResolver));

        private readonly AssemblyLoadContext _loadContext;

        // Our Strategies for resolving references
        internal List<ResolutionStrategy> ResolutionStrategies = new List<ResolutionStrategy>();

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
            // what we learned from examining direct references.
            if (tryWindowsDesktopFirst)
                ResolutionStrategies.Add(new WindowsDesktopStrategy(false));
            if (tryAspNetCoreFirst)
                ResolutionStrategies.Add(new AspNetCoreStrategy(false));

            ResolutionStrategies.Add(new TrustedPlatformAssembliesStrategy());
            ResolutionStrategies.Add(new RuntimeLibrariesStrategy(loadContext, testAssemblyPath));

            if (!tryWindowsDesktopFirst)
                ResolutionStrategies.Add(new WindowsDesktopStrategy(false));
            if (!tryAspNetCoreFirst)
                ResolutionStrategies.Add(new AspNetCoreStrategy(false));
        }

        public void Dispose()
        {
            _loadContext.Resolving -= OnResolving;
        }

        public Assembly? Resolve(AssemblyLoadContext context, AssemblyName assemblyName)
        {
            return OnResolving(context, assemblyName);
        }

        private Assembly? OnResolving(AssemblyLoadContext loadContext, AssemblyName assemblyName)
        {
            Guard.ArgumentNotNull(loadContext);

            foreach (var strategy in ResolutionStrategies)
            {
                strategy.Calls++;
                if (strategy.TryToResolve(loadContext, assemblyName, out Assembly? loadedAssembly))
                {
                    log.Info($"Assembly {assemblyName} ({GetAssemblyLocationInfo(loadedAssembly)}) is loaded using strategy {strategy.Name}");
                    strategy.Resolved++;
                    return loadedAssembly;
                }
            }

            log.Info("Cannot resolve assembly '{0}'", assemblyName);
            return null;
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

    #region ResolutionStrategy Classes

    public abstract class ResolutionStrategy
    {
        public string Name => GetType().Name;
        public int Calls { get; set; }
        public int Resolved { get; set; }

        public abstract bool TryToResolve(
            AssemblyLoadContext loadContext, AssemblyName assemblyName, [NotNullWhen(true)] out Assembly? loadedAssembly);
    }

    public class TrustedPlatformAssembliesStrategy : ResolutionStrategy
    {
        private static readonly Logger log = InternalTrace.GetLogger(typeof(TrustedPlatformAssembliesStrategy));
        public override bool TryToResolve(
            AssemblyLoadContext loadContext, AssemblyName assemblyName, [NotNullWhen(true)] out Assembly? loadedAssembly)
        {
            return TryLoadFromTrustedPlatformAssemblies(loadContext, assemblyName, out loadedAssembly);
        }

        private static bool TryLoadFromTrustedPlatformAssemblies(
            AssemblyLoadContext loadContext, AssemblyName assemblyName, [NotNullWhen(true)] out Assembly? loadedAssembly)
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
        private static readonly Logger log = InternalTrace.GetLogger(typeof(RuntimeLibrariesStrategy));

        private DependencyContext? _dependencyContext;
        private readonly CompositeCompilationAssemblyResolver _assemblyResolver;

        public RuntimeLibrariesStrategy(AssemblyLoadContext loadContext, string testAssemblyPath)
        {
            _dependencyContext = DependencyContext.Load(loadContext.LoadFromAssemblyPath(testAssemblyPath));

            _assemblyResolver = new CompositeCompilationAssemblyResolver(
            [
                new AppBaseCompilationAssemblyResolver(Path.GetDirectoryName(testAssemblyPath)!),
                new ReferenceAssemblyPathResolver(),
                new PackageCompilationAssemblyResolver()
            ]);
        }

        public override bool TryToResolve(
            AssemblyLoadContext loadContext, AssemblyName assemblyName, [NotNullWhen(true)] out Assembly? loadedAssembly)
        {
            if (_dependencyContext is null)
            {
                // TODO: Is this the intended behavior?
                loadedAssembly = null;
                return false;
            }

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
        private string _runtimeName;
        private bool _x86;

        public AdditionalRuntimesStrategy(string runtimeName, bool x86)
        {
            _runtimeName = runtimeName;
            _x86 = x86;
        }

        public override bool TryToResolve(AssemblyLoadContext loadContext, AssemblyName assemblyName, [NotNullWhen(true)] out Assembly? loadedAssembly)
        {
            loadedAssembly = null;
            if (assemblyName.Version is null)
                return false;

            if (!DotNet.FindBestRuntime(assemblyName.Version, _runtimeName, _x86, out DotNet.RuntimeInfo? runtime))
                return false;

            string candidate = Path.Combine(runtime.Path, runtime.Version.ToString(), assemblyName.Name + ".dll");
            if (!File.Exists(candidate))
                return false;

            loadedAssembly = loadContext.LoadFromAssemblyPath(candidate);
            return true;
        }
    }

    public class WindowsDesktopStrategy : AdditionalRuntimesStrategy
    {
        public WindowsDesktopStrategy(bool x86) : base("Microsoft.WindowsDesktop.App", x86)
        {
        }
    }

    public class AspNetCoreStrategy : AdditionalRuntimesStrategy
    {
        public AspNetCoreStrategy(bool x86) : base("Microsoft.AspNetCore.App", x86)
        {
        }
    }

        #endregion
}
#endif
