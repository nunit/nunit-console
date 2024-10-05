// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

#if NETCOREAPP3_1_OR_GREATER

using Microsoft.Extensions.DependencyModel;
using Microsoft.Extensions.DependencyModel.Resolution;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
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

        private static readonly string INSTALL_DIR;
        private static readonly string WINDOWS_DESKTOP_DIR;
        private static readonly string ASP_NET_CORE_DIR;

        // Our Strategies for resolving references
        List<ResolutionStrategy> ResolutionStrategies;

        static TestAssemblyResolver()
        {
            INSTALL_DIR = DotNet.GetInstallDirectory();
            WINDOWS_DESKTOP_DIR = Path.Combine(INSTALL_DIR, "shared", "Microsoft.WindowsDesktop.App");
            ASP_NET_CORE_DIR = Path.Combine(INSTALL_DIR, "shared", "Microsoft.AspNetCore.App");
        }

        public TestAssemblyResolver(AssemblyLoadContext loadContext, string testAssemblyPath)
        {
            _loadContext = loadContext;

            InitializeResolutionStrategies(loadContext, testAssemblyPath);

            _loadContext.Resolving += OnResolving;
        }

        private void InitializeResolutionStrategies(AssemblyLoadContext loadContext, string testAssemblyPath)
        {
            // First, looking only at direct references by the test assembly, try to determine if
            // this assembly is using WindowsDesktop (either SWF or WPF) and/or AspNetCore.
            AssemblyDefinition assemblyDef = AssemblyDefinition.ReadAssembly(testAssemblyPath);
            bool isWindowsDesktop = false;
            bool isAspNetCore = false;
            foreach (var reference in assemblyDef.MainModule.GetTypeReferences())
            {
                string fn = reference.FullName;
                if (fn.StartsWith("System.Windows.") || fn.StartsWith("PresentationFramework"))
                    isWindowsDesktop = true;
                if (fn.StartsWith("Microsoft.AspNetCore."))
                    isAspNetCore = true;
            }

            // Initialize the list of ResolutionStrategies in the best order depending on
            // what we learned.
            ResolutionStrategies = new List<ResolutionStrategy>();
            
            if (isWindowsDesktop && Directory.Exists(WINDOWS_DESKTOP_DIR))
                ResolutionStrategies.Add(new AdditionalDirectoryStrategy(WINDOWS_DESKTOP_DIR));
            if (isAspNetCore && Directory.Exists(ASP_NET_CORE_DIR))
                ResolutionStrategies.Add(new AdditionalDirectoryStrategy(ASP_NET_CORE_DIR));
            ResolutionStrategies.Add(new TrustedPlatformAssembliesStrategy());
            ResolutionStrategies.Add(new RuntimeLibrariesStrategy(loadContext, testAssemblyPath));
            if (!isWindowsDesktop && Directory.Exists(WINDOWS_DESKTOP_DIR))
                ResolutionStrategies.Add(new AdditionalDirectoryStrategy(WINDOWS_DESKTOP_DIR));
            if (!isAspNetCore && Directory.Exists(ASP_NET_CORE_DIR))
                ResolutionStrategies.Add(new AdditionalDirectoryStrategy(ASP_NET_CORE_DIR));
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

        public class AdditionalDirectoryStrategy : ResolutionStrategy
        {
            private string _frameworkDirectory;

            public AdditionalDirectoryStrategy(string frameworkDirectory)
            {
                _frameworkDirectory = frameworkDirectory;
            }

            public override bool TryToResolve(
                AssemblyLoadContext loadContext, AssemblyName assemblyName, out Assembly loadedAssembly)
            {
                loadedAssembly = null;
                if (assemblyName.Version == null)
                    return false;

                var versionDir = FindBestVersionDir(_frameworkDirectory, assemblyName.Version);

                if (versionDir != null)
                {
                    string candidate = Path.Combine(_frameworkDirectory, versionDir, assemblyName.Name + ".dll");
                    if (File.Exists(candidate))
                    {
                        loadedAssembly = loadContext.LoadFromAssemblyPath(candidate);
                        log.Info("'{0}' ({1}) assembly is loaded from AdditionalFrameworkDirectory {2} dependencies with best candidate version {3}",
                            assemblyName,
                            loadedAssembly.Location,
                            _frameworkDirectory,
                            versionDir);

                        return true;
                    }
                    else
                    {
                        log.Debug("Best version dir for {0} is {1}, but there is no {2} file", _frameworkDirectory, versionDir, candidate);
                        return false;
                    }
                }

                return false;
            }
        }

        #endregion

        #region HelperMethods

        private static string FindBestVersionDir(string libraryDir, Version targetVersion)
        {
            string target = targetVersion.ToString();
            Version bestVersion = new Version(0, 0);
            foreach (var subdir in Directory.GetDirectories(libraryDir))
            {
                Version version;
                if (TryGetVersionFromString(Path.GetFileName(subdir), out version))
                {
                    if (version >= targetVersion)
                        if (bestVersion.Major == 0 || bestVersion > version)
                            bestVersion = version;
                }
            }

            return bestVersion.Major > 0
                ? bestVersion.ToString()
                : null;
        }

        private static bool TryGetVersionFromString(string text, out Version newVersion)
        {
            const string VERSION_CHARS = ".0123456789";

            int len = 0;
            foreach (char c in text)
            {
                if (VERSION_CHARS.IndexOf(c) >= 0)
                    len++;
                else
                    break;
            }

            try
            {
                newVersion = new Version(text.Substring(0, len));
                return true;
            }
            catch
            {
                newVersion = new Version();
                return false;
            }
        }

        #endregion
    }
}
#endif
