// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

#if NETFRAMEWORK
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
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

        private static string? MonoPrefix;

        /// <summary>
        /// The path to the mono executable, if we are running on Mono.
        /// </summary>
        public static string MonoExePath => MonoPrefix is not null && Environment.OSVersion.Platform == PlatformID.Win32NT
                    ? Path.Combine(MonoPrefix, "bin/mono.exe")
                    : "mono";

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

            if (!RuntimeFramework.TryParse(name, out RuntimeFramework? requestedFramework))
                throw new NUnitEngineException("Invalid or unknown framework requested: " + name);

            var runtimes = needX86 ? _availableX86Runtimes : _availableRuntimes;
            foreach (var framework in runtimes)
                if (FrameworksMatch(requestedFramework, framework))
                    return true;

            return false;
        }

        private static readonly Version AnyVersion = new Version(0, 0);

        private static bool FrameworksMatch(RuntimeFramework requested, RuntimeFramework available)
        {
            if (!RuntimesMatch(requested.Runtime, available.Runtime))
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

        private static bool RuntimesMatch(Runtime requested, Runtime available)
        {
            if (requested == available)
                return true;

            if (requested == Runtime.Net && available == Runtime.Mono)
                return true;

            if (requested == Runtime.Mono && available == Runtime.Net)
                return true;

            return false;
        }

        /// <summary>
        /// Selects a target runtime framework for a TestPackage based on
        /// the settings in the package and the assemblies themselves.
        /// The package RuntimeFramework setting may be updated as a result
        /// and a string representing the selected runtime is returned.
        /// </summary>
        /// <param name="package">A TestPackage representing an assembly</param>
        /// <returns>A string representing the selected RuntimeFramework</returns>
        public void SelectRuntimeFramework(TestPackage package)
        {
            Guard.ArgumentValid(!package.HasSubPackages(),
                "SelectRuntimeFramework must be called with a package representing an assembly", nameof(package));

            // Evaluate package target framework
            if (package.IsAssemblyPackage())
                ApplyImageData(package);

            string frameworkSetting = package.Settings.GetValueOrDefault(SettingDefinitions.RequestedRuntimeFramework);
            bool runAsX86 = package.Settings.GetValueOrDefault(SettingDefinitions.RunAsX86);

            if (frameworkSetting.Length > 0)
            {
                if (!RuntimeFramework.TryParse(frameworkSetting, out RuntimeFramework? requestedFramework))
                    throw new NUnitEngineException("Invalid or unknown framework requested: " + frameworkSetting);

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
            Runtime targetRuntime;
            Version targetVersion;

            if (string.IsNullOrEmpty(imageTargetFrameworkNameSetting))
            {
                // Assume .NET Framework
                targetRuntime = Runtime.Net;
                var trialVersion = new Version(package.Settings.GetValueOrDefault(SettingDefinitions.ImageRuntimeVersion));
                targetVersion = new Version(trialVersion.Major, trialVersion.Minor);
            }
            else
            {
                FrameworkName frameworkName = new FrameworkName(imageTargetFrameworkNameSetting);

                switch (frameworkName.Identifier)
                {
                    case ".NETFramework":
                        targetRuntime = Runtime.Net;
                        targetVersion = frameworkName.Version;
                        break;
                    case ".NETCoreApp":
                        targetRuntime = Runtime.NetCore;
                        targetVersion = frameworkName.Version;
                        break;
                    case ".NETStandard":
                        targetRuntime = Runtime.NetCore;
                        targetVersion = new Version(3, 1);
                        break;
                    case "Unmanaged":
                    default:
                        throw new NUnitEngineException("Unsupported Target Framework: " + imageTargetFrameworkNameSetting);
                }
            }

            if (!IsAvailable(new RuntimeFramework(targetRuntime, targetVersion).Id, runAsX86))
            {
                log.Debug("Preferred version {0} is not installed or this NUnit installation does not support it", targetVersion);
                if (targetVersion < CurrentFramework.FrameworkVersion)
                    targetVersion = CurrentFramework.FrameworkVersion;
            }

            RuntimeFramework targetFramework = new RuntimeFramework(targetRuntime, targetVersion);
            package.Settings.Set(SettingDefinitions.TargetFrameworkName.WithValue(targetFramework.FrameworkName.ToString()));

            log.Debug($"Test will use {targetFramework} for {package.Name}");
        }

        public override void StartService()
        {
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

        private RuntimeFramework GetCurrentFramework()
        {
            Type? monoRuntimeType = Type.GetType("Mono.Runtime", throwOnError: false);

            Runtime runtime = monoRuntimeType is not null
                ? Runtime.Mono
                : Runtime.Net;

            int major = Environment.Version.Major;
            int minor = Environment.Version.Minor;

            if (monoRuntimeType is not null)
            {
                switch (major)
                {
                    case 1:
                        minor = 0;
                        break;
                    case 2:
                        major = 3;
                        minor = 5;
                        break;
                }
            }
            else /* It's windows */
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

            var currentFramework = new RuntimeFramework(runtime, new Version(major, minor));

            if (monoRuntimeType is not null)
            {
                MonoPrefix = GetMonoPrefixFromAssembly(monoRuntimeType.Assembly);

                MethodInfo? getDisplayNameMethod = monoRuntimeType.GetMethod(
                    "GetDisplayName", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.DeclaredOnly | BindingFlags.ExactBinding);
                if (getDisplayNameMethod is not null)
                {
                    string displayName = (string)getDisplayNameMethod.Invoke(null, Array.Empty<object>())!;

                    int space = displayName.IndexOf(' ');
                    if (space >= 3) // Minimum length of a version
                    {
                        string version = displayName.Substring(0, space);
                        displayName = "Mono " + version;
                    }
                    else
                        displayName = "Mono " + displayName;

                    currentFramework.DisplayName = displayName;
                }
            }

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

            if (Environment.OSVersion.Platform == PlatformID.Win32NT)
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
            string packageName = package.FullName ?? string.Empty;

            Version targetVersion = new Version(0, 0);
            string? frameworkName = null;
            bool requiresX86 = false;
            bool requiresAssemblyResolver = false;

            if (!File.Exists(packageName))
                log.Error($"Could not find {packageName}");
            else
                try
                {
                    using (var assembly = AssemblyDefinition.ReadAssembly(packageName))
                    {
                        targetVersion = assembly.GetRuntimeVersion();
                        log.Debug($"Assembly {packageName} uses version {targetVersion}");

                        frameworkName = assembly.GetFrameworkName();
                        log.Debug($"Assembly {packageName} targets {frameworkName}");

                        if (assembly.RequiresX86())
                        {
                            requiresX86 = true;
                            log.Debug($"Assembly {packageName} will be run x86");
                        }

                        if (assembly.HasAttribute("NUnit.Framework.TestAssemblyDirectoryResolveAttribute"))
                        {
                            requiresAssemblyResolver = true;
                            log.Debug($"Assembly {packageName} requires default app domain assembly resolver");
                        }
                    }
                }
                catch (BadImageFormatException)
                {
                    // "Unmanaged" is not a valid framework identifier but we handle it upstream
                    // using UnmanagedCodeTestRunner, which doesn't actually try to run it.
                    frameworkName = "Unmanaged,Version=0.0";
                }

            if (targetVersion.Major > 0)
                package.Settings.Set(SettingDefinitions.ImageRuntimeVersion.WithValue(targetVersion.ToString()));

            if (!string.IsNullOrEmpty(frameworkName))
                package.Settings.Set(SettingDefinitions.ImageTargetFrameworkName.WithValue(frameworkName));

            // If assembly requires X86, it MUST be run as X86, so we apply both settings
            if (requiresX86)
            {
                package.Settings.Set(SettingDefinitions.ImageRequiresX86.WithValue(true));
                package.Settings.Set(SettingDefinitions.RunAsX86.WithValue(true));
            }

            package.Settings.Set(SettingDefinitions.ImageRequiresDefaultAppDomainAssemblyResolver.WithValue(requiresAssemblyResolver));
        }
    }
}
#endif