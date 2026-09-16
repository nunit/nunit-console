// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.Versioning;
using Microsoft.Win32;
using NUnit.Common;
using NUnit.Engine.Services.RuntimeLocators;
using TestCentric.Metadata;

namespace NUnit.Engine.Services
{
    public class RuntimeFrameworkService : Service, IRuntimeFrameworkService, IAvailableRuntimes
    {
        private static readonly Logger log = InternalTrace.GetLogger(typeof(RuntimeFrameworkService));

        private List<RuntimeFramework> _availableRuntimes = new List<RuntimeFramework>();
        private List<RuntimeFramework> _availableX86Runtimes = new List<RuntimeFramework>();

        public RuntimeFrameworkService()
        {
            CurrentFramework = GetCurrentFramework();
        }

        /// <summary>
        /// Gets a RuntimeFramework instance representing the runtime under
        /// which the code is currently running.
        /// </summary>
        public IRuntimeFramework CurrentFramework { get; private set; }

        /// <summary>
        /// Gets a list of available X64 runtimes.
        /// </summary>
        public IList<IRuntimeFramework> AvailableRuntimes => _availableRuntimes.ToArray();

        /// <summary>
        /// Gets a list of available X86 runtimes.
        /// </summary>
        public IList<IRuntimeFramework> AvailableX86Runtimes => _availableX86Runtimes.ToArray();

        /// <summary>
        /// Returns true if the runtime framework represented by
        /// the string passed as an argument is available.
        /// </summary>
        /// <param name="name">A string representing a framework, like 'net-4.0'</param>
        /// <returns>True if the framework is available, false if unavailable or nonexistent</returns>
        public bool IsAvailable(string name, bool needX86)
        {
            Guard.ArgumentNotNullOrEmpty(name);

            RuntimeFramework requestedFramework = RuntimeFramework.FromTFM(name);

            var runtimes = needX86 ? _availableX86Runtimes : _availableRuntimes;
            foreach (var framework in runtimes)
                if (FrameworksMatch(requestedFramework, framework))
                    return true;

            return false;
        }

        private static readonly Version AnyVersion = new Version(0, 0);

        private static bool FrameworksMatch(RuntimeFramework requested, RuntimeFramework available)
        {
            var requestedIdentifier = requested.FrameworkName.Identifier;
            var availableIdentifier = available.FrameworkName.Identifier;
            if (requestedIdentifier != availableIdentifier)
                return false;

            var requestedVersion = requested.FrameworkVersion;
            var availableVersion = available.FrameworkVersion;

            if (requestedVersion == AnyVersion)
                return true;

            return requestedVersion.Major == availableVersion.Major &&
                   requestedVersion.Minor == availableVersion.Minor &&
                   (requestedVersion.Build < 0 || availableVersion.Build < 0 || requestedVersion.Build == availableVersion.Build) &&
                   (requestedVersion.Revision < 0 || availableVersion.Revision < 0 || requestedVersion.Revision == availableVersion.Revision);
        }

        /// <summary>
        /// Selects a target runtime framework for a TestPackage based on
        /// the settings in the package and the assemblies themselves.
        /// The package RuntimeFramework setting may be updated as a result
        /// and a string representing the selected runtime is returned.
        /// </summary>
        /// <param name="package">A TestPackage representing an assembly</param>
        public void SelectRuntimeFramework(TestPackage package)
        {
            Guard.ArgumentValid(!package.HasSubPackages,
                "SelectRuntimeFramework must be called with a package representing an assembly", nameof(package));

            // Evaluate package target framework
            if (package.IsAssemblyPackage)
                ApplyImageData(package);

            string frameworkSetting = package.Settings.GetValueOrDefault(SettingDefinitions.RequestedRuntimeFramework);
            bool runAsX86 = package.Settings.GetValueOrDefault(SettingDefinitions.RunAsX86);

            if (frameworkSetting.Length > 0)
            {
                RuntimeFramework requestedFramework = RuntimeFramework.FromTFM(frameworkSetting);

                log.Debug($"Requested framework for {package.Name} is {requestedFramework}");

                if (!IsAvailable(frameworkSetting, runAsX86))
                    throw new NUnitEngineException("Requested framework is not available: " + frameworkSetting);

                var frameworkName = requestedFramework.FrameworkName.ToString();
                package.Settings.Set(SettingDefinitions.RequestedFrameworkName.WithValue(frameworkName));
                package.Settings.Set(SettingDefinitions.TargetFrameworkName.WithValue(frameworkName));
            }

            log.Debug($"No specific framework requested for {package.Name}");

            string imageTargetFrameworkNameSetting =
                package.Settings.GetValueOrDefault(SettingDefinitions.ImageTargetFrameworkName);
            string targetIdentifier;
            Version targetVersion;

            if (string.IsNullOrEmpty(imageTargetFrameworkNameSetting))
            {
                // Assume .NET Framework
                targetIdentifier = FrameworkIdentifiers.NetFramework;
                var trialVersion = new Version(package.Settings.GetValueOrDefault(SettingDefinitions.ImageRuntimeVersion));
                targetVersion = new Version(trialVersion.Major, trialVersion.Minor);
            }
            else
            {
                FrameworkName frameworkName = new FrameworkName(imageTargetFrameworkNameSetting);

                switch (frameworkName.Identifier)
                {
                    case ".NETFramework":
                        targetIdentifier = FrameworkIdentifiers.NetFramework;
                        targetVersion = frameworkName.Version;
                        break;
                    case ".NETCoreApp":
                        targetIdentifier = FrameworkIdentifiers.NetCoreApp;
                        targetVersion = frameworkName.Version;
                        break;
                    case ".NETStandard":
                        targetIdentifier = FrameworkIdentifiers.NetCoreApp;
                        targetVersion = new Version(3, 1);
                        break;
                    case "Unmanaged":
                        package.Settings.Set(SettingDefinitions.ImageTargetFrameworkName.WithValue("Unmanaged,Version=0.0"));
                        return;
                    default:
                        throw new NUnitEngineException("Unsupported Target Framework: " + imageTargetFrameworkNameSetting);
                }
            }

            if (!IsAvailable(new RuntimeFramework(targetIdentifier, targetVersion).TFM, runAsX86))
            {
                log.Debug("Preferred version {0} is not installed or this NUnit installation does not support it", targetVersion);
                if (targetVersion < CurrentFramework.FrameworkName.Version)
                    targetVersion = CurrentFramework.FrameworkName.Version;
            }

            RuntimeFramework targetFramework = new RuntimeFramework(targetIdentifier, targetVersion);
            package.Settings.Set(SettingDefinitions.TargetFrameworkName.WithValue(targetFramework.FrameworkName.ToString()));

            log.Debug($"Test will use {targetFramework} for {package.Name}");
        }

        public override void StartService()
        {
            base.StartService();

            try
            {
                FindAvailableRuntimes();

                Status = ServiceStatus.Started;
            }
            catch
            {
                Status = ServiceStatus.Error;
                throw;
            }
        }

        private static RuntimeFramework GetCurrentFramework()
        {
            string identifier = FrameworkIdentifiers.NetFramework;

            int major = Environment.Version.Major;
            int minor = Environment.Version.Minor;

            if (Platform.IsWindows)
            {
                if (major == 2)
                {
                    using RegistryKey? key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\.NETFramework");
                    if (key is not null)
                    {
                        string? installRoot = key.GetValue("InstallRoot") as string;
                        if (installRoot is not null)
                        {
                            if (Directory.Exists(Path.Combine(installRoot, "v3.5")))
                            {
                                major = 3;
                                minor = 5;
                            }
                            else if (Directory.Exists(Path.Combine(installRoot, "v3.0")))
                            {
                                major = 3;
                                minor = 0;
                            }
                        }
                    }
                }
                else if (major == 4 && Type.GetType("System.Reflection.AssemblyMetadataAttribute") is not null)
                {
                    minor = 5;
                }
                else if (major > 4)
                {
                    identifier = FrameworkIdentifiers.NetCoreApp;
                }
            }
            else
                throw new NotSupportedException("Platform is not recognized");

            var currentFramework = new RuntimeFramework(identifier, new Version(major, minor));

            return currentFramework;
        }

        private static string GetMonoPrefixFromAssembly(Assembly assembly)
        {
            string prefix = assembly.Location;

            // In all normal mono installations, there will be sufficient
            // levels to complete the four iterations. But just in case
            // files have been copied to some non-standard place, we check.
            for (int i = 0; i < 4; i++)
            {
                string? dir = Path.GetDirectoryName(prefix);
                if (string.IsNullOrEmpty(dir))
                    break;

                prefix = dir;
            }

            return prefix;
        }

        private void FindAvailableRuntimes()
        {
            _availableRuntimes = new List<RuntimeFramework>();
            _availableX86Runtimes = new List<RuntimeFramework>();

            if (Platform.IsWindows)
            {
                var netFxRuntimes = NetFxRuntimeLocator.FindRuntimes();
                _availableRuntimes.AddRange(netFxRuntimes);
                _availableX86Runtimes.AddRange(netFxRuntimes);
            }

            //FindDefaultMonoFramework();
            _availableRuntimes.AddRange(NetCoreRuntimeLocator.FindRuntimes(forX86: false));
            _availableX86Runtimes.AddRange(NetCoreRuntimeLocator.FindRuntimes(forX86: true));
        }

        /// <summary>
        /// Use Mono.Cecil to get information about all assemblies and
        /// apply it to the package using special internal keywords.
        /// </summary>
        private static void ApplyImageData(TestPackage package)
        {
            Guard.ArgumentNotNull(package, nameof(package));
            Guard.ArgumentValid(package.IsAssemblyPackage, "ApplyImageSettings called for non-assembly", nameof(package));

            string assemblyPath = package.FullName.ShouldNotBeNull();
            if (!File.Exists(assemblyPath))
            {
                log.Error($"Could not find {assemblyPath}");
                return;
            }

            try
            {
                using (var assembly = AssemblyDefinition.ReadAssembly(assemblyPath))
                {
                    var targetVersion = assembly.GetRuntimeVersion();
                    if (targetVersion.Major > 0)
                    {
                        log.Debug($"Assembly {assemblyPath} uses version {targetVersion}");
                        package.Settings.Set(SettingDefinitions.ImageRuntimeVersion.WithValue(targetVersion.ToString()));
                    }

                    if (assembly.TryGetFrameworkName(out string frameworkName))
                    {
                        log.Debug($"Assembly {assemblyPath} targets {frameworkName}");
                        package.Settings.Set(SettingDefinitions.ImageTargetFrameworkName.WithValue(frameworkName));
                    }

                    if (assembly.RequiresX86())
                    {
                        // If assembly requires X86, it MUST be run as X86, so we apply both settings
                        package.Settings.Set(SettingDefinitions.ImageRequiresX86.WithValue(true));
                        package.Settings.Set(SettingDefinitions.RunAsX86.WithValue(true));
                        log.Debug($"Assembly {assemblyPath} will be run x86");
                    }

                    if (assembly.HasAttribute("NUnit.Framework.TestAssemblyDirectoryResolveAttribute"))
                    {
                        package.Settings.Set(SettingDefinitions.ImageRequiresDefaultAppDomainAssemblyResolver.WithValue(true));
                        log.Debug($"Assembly {assemblyPath} requires default app domain assembly resolver");
                    }
                }
            }
            catch (BadImageFormatException)
            {
                // "Unmanaged" is not a valid framework identifier but we handle it upstream
                // using UnmanagedCodeTestRunner, which doesn't actually try to run it.
                package.Settings.Set(SettingDefinitions.ImageTargetFrameworkName.WithValue("Unmanaged,Version=0.0"));
            }
        }
    }
}
