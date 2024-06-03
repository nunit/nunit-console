// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

#if NETCOREAPP3_1_OR_GREATER

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Loader;
using Microsoft.Extensions.DependencyModel;
using Microsoft.Extensions.DependencyModel.Resolution;
using Microsoft.Win32;

namespace NUnit.Engine.Internal
{
    internal sealed class TestAssemblyResolver : IDisposable
    {
        private static readonly Logger log = InternalTrace.GetLogger(typeof(TestAssemblyResolver));

        private readonly ICompilationAssemblyResolver _assemblyResolver;
        private readonly DependencyContext _dependencyContext;
        private readonly AssemblyLoadContext _loadContext;

        private static readonly string INSTALL_DIR = GetDotNetInstallDirectory();
        private static readonly string WINDOWS_DESKTOP_DIR = Path.Combine(INSTALL_DIR, "shared", "Microsoft.WindowsDesktop.App");
        private static readonly string ASP_NET_CORE_DIR = Path.Combine(INSTALL_DIR, "shared", "Microsoft.AspNetCore.App");
        private static readonly List<string> AdditionalFrameworkDirectories;

        static TestAssemblyResolver()
        {
            AdditionalFrameworkDirectories = new List<string>();
            if (Directory.Exists(WINDOWS_DESKTOP_DIR))
                AdditionalFrameworkDirectories.Add(WINDOWS_DESKTOP_DIR);
            if (Directory.Exists(ASP_NET_CORE_DIR))
                AdditionalFrameworkDirectories.Add(ASP_NET_CORE_DIR);
        }

        public TestAssemblyResolver(AssemblyLoadContext loadContext, string assemblyPath)
        {
            _loadContext = loadContext;
            _dependencyContext = DependencyContext.Load(loadContext.LoadFromAssemblyPath(assemblyPath));

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

        public Assembly Resolve(AssemblyLoadContext context, AssemblyName name)
        {
            return OnResolving(context, name);
        }

        private Assembly OnResolving(AssemblyLoadContext context, AssemblyName name)
        {
            context = context ?? _loadContext;
            
            if (TryLoadFromTrustedPlatformAssemblies(context, name, out var loadedAssembly))
            {
                log.Info("'{0}' assembly is loaded from trusted path '{1}'", name, loadedAssembly.Location);
                return loadedAssembly;
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
                    if (name.Name == Path.GetFileNameWithoutExtension(assemblyPath))
                    {
                        loadedAssembly = context.LoadFromAssemblyPath(assemblyPath);
                        log.Info("'{0}' ({1}) assembly is loaded from runtime libraries {2} dependencies",
                            name,
                            loadedAssembly.Location,
                            library.Name);

                        return loadedAssembly;
                    }
                }
            }

            if (name.Version == null)
            {
                return null;
            }

            foreach (string frameworkDirectory in AdditionalFrameworkDirectories)
            {
                var versionDir = FindBestVersionDir(frameworkDirectory, name.Version);

                if (versionDir != null)
                {
                    string candidate = Path.Combine(frameworkDirectory, versionDir, name.Name + ".dll");
                    if (File.Exists(candidate))
                    {
                        loadedAssembly = context.LoadFromAssemblyPath(candidate);
                        log.Info("'{0}' ({1}) assembly is loaded from AdditionalFrameworkDirectory {2} dependencies with best candidate version {3}",
                            name,
                            loadedAssembly.Location,
                            frameworkDirectory,
                            versionDir);

                        return loadedAssembly;
                    }
                    else
                    {
                        log.Debug("Best version dir for {0} is {1}, but there is no {2} file", frameworkDirectory, versionDir, candidate);
                    }
                }
            }

            log.Info("Cannot resolve assembly '{0}'", name);
            return null;
        }

        private static bool TryLoadFromTrustedPlatformAssemblies(AssemblyLoadContext context, AssemblyName assemblyName, out Assembly loadedAssembly)
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
                if (string.Equals(fileName, assemblyName.Name, StringComparison.InvariantCultureIgnoreCase) == false)
                {
                    continue;
                }

                if (File.Exists(assemblyPath))
                {
                    loadedAssembly = context.LoadFromAssemblyPath(assemblyPath);
                    return true;
                }
            }

            return false;
        }

        private static string GetDotNetInstallDirectory()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                // Running on Windows so use registry
                //RegistryKey key = Environment.Is64BitProcess
                //    ? Registry.LocalMachine.OpenSubKey(@"Software\dotnet\SetUp\InstalledVersions\x64\sharedHost\")
                //    : Registry.LocalMachine.OpenSubKey(@"Software\dotnet\SetUp\InstalledVersions\x86\sharedHost\");
                if (Environment.Is64BitProcess)
                {
                    RegistryKey key =
                        Registry.LocalMachine.OpenSubKey(@"Software\dotnet\SetUp\InstalledVersions\x64\sharedHost\");
                    return (string)key?.GetValue("Path");
                }
                else
                    return (@"C:\Program Files (x86)\dotnet\");
            }
            else
                return "/usr/shared/dotnet/";
        }

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
    }
}
#endif
