// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

#if NETFRAMEWORK
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Reflection;
using System.Runtime.Versioning;
using Microsoft.Win32;
using NUnit.Engine.Services.RuntimeLocators;
using TestCentric.Metadata;

namespace NUnit.Engine.Services
{
    public class RuntimeFrameworkService : Service, IRuntimeFrameworkService, IAvailableRuntimes
    {
        private static readonly Logger log = InternalTrace.GetLogger(typeof(RuntimeFrameworkService));

        private List<RuntimeFramework> _availableRuntimes = new List<RuntimeFramework>();
        private List<RuntimeFramework> _availableX86Runtimes = new List<RuntimeFramework>();

        /// <summary>
        /// Gets a RuntimeFramework instance representing the runtime under
        /// which the code is currently running.
        /// </summary>
        public IRuntimeFramework? CurrentFramework { get; private set; }

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
        /// <param name="package">A TestPackage</param>
        /// <returns>A string representing the selected RuntimeFramework</returns>
        public string SelectRuntimeFramework(TestPackage package)
        {
            // Evaluate package target framework
            ApplyImageData(package);

            var targetFramework = SelectRuntimeFrameworkInner(package);
            return targetFramework.ToString();
        }

        private RuntimeFramework SelectRuntimeFrameworkInner(TestPackage package)
        {
            if (CurrentFramework is null)
                throw new InvalidOperationException("Service not Started");

            foreach (var subPackage in package.SubPackages)
            {
                SelectRuntimeFrameworkInner(subPackage);
            }

            // Examine the provided settings
            IRuntimeFramework currentFramework = CurrentFramework;
            log.Debug("Current framework is " + currentFramework);

            string frameworkSetting = package.GetSetting(EnginePackageSettings.RequestedRuntimeFramework, string.Empty);
            bool runAsX86 = package.GetSetting(EnginePackageSettings.RunAsX86, false);

            if (frameworkSetting.Length > 0)
            {
                if (!RuntimeFramework.TryParse(frameworkSetting, out RuntimeFramework? requestedFramework))
                    throw new NUnitEngineException("Invalid or unknown framework requested: " + frameworkSetting);

                log.Debug($"Requested framework for {package.Name} is {requestedFramework}");

                if (!IsAvailable(frameworkSetting, runAsX86))
                    throw new NUnitEngineException("Requested framework is not available: " + frameworkSetting);

                package.Settings[EnginePackageSettings.TargetRuntimeFramework] = frameworkSetting;

                return requestedFramework;
            }

            log.Debug($"No specific framework requested for {package.Name}");

            string imageTargetFrameworkNameSetting = package.GetSetting(EnginePackageSettings.ImageTargetFrameworkName, string.Empty);
            Runtime targetRuntime;
            Version targetVersion;

            if (string.IsNullOrEmpty(imageTargetFrameworkNameSetting))
            {
                // Assume .NET Framework
                targetRuntime = Runtime.Net;
                var trialVersion = package.GetSetting(EnginePackageSettings.ImageRuntimeVersion, new Version(2, 0));
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
                if (targetVersion < currentFramework.FrameworkVersion)
                    targetVersion = currentFramework.FrameworkVersion;
            }

            RuntimeFramework targetFramework = new RuntimeFramework(targetRuntime, targetVersion);
            package.Settings[EnginePackageSettings.TargetRuntimeFramework] = targetFramework.ToString();

            log.Debug($"Test will use {targetFramework} for {package.Name}");
            return targetFramework;
        }

        public override void StartService()
        {
            try
            {
                SetCurrentFramework();
                FindAvailableRuntimes();

                Status = ServiceStatus.Started;
            }
            catch
            {
                Status = ServiceStatus.Error;
                throw;
            }
        }

        [MemberNotNull(nameof(CurrentFramework))]
        private void SetCurrentFramework()
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

            CurrentFramework = currentFramework;
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
        /// <param name="package"></param>
        private static void ApplyImageData(TestPackage package)
        {
            string packageName = package.FullName ?? string.Empty;

            Version targetVersion = new Version(0, 0);
            string? frameworkName = null;
            bool requiresX86 = false;
            bool requiresAssemblyResolver = false;

            // We are doing two jobs here: (1) in the else clause (below)
            // we get information about a single assembly and record it,
            // (2) in the if clause, we recursively examine all subpackages
            // and then apply policies for promulgating each setting to
            // a containing package. We could implement the policy part at
            // a higher level, but it seems simplest to do it right here.
            if (package.SubPackages.Count > 0)
            {
                foreach (var subPackage in package.SubPackages)
                {
                    ApplyImageData(subPackage);

                    // Collect the highest version required
                    Version v = subPackage.GetSetting(EnginePackageSettings.ImageRuntimeVersion, new Version(0, 0));
                    if (v > targetVersion)
                        targetVersion = v;

                    // Collect highest framework name
                    // TODO: This assumes lexical ordering is valid - check it
                    string fn = subPackage.GetSetting(EnginePackageSettings.ImageTargetFrameworkName, string.Empty);
                    if (fn != string.Empty)
                    {
                        if (frameworkName is null || fn.CompareTo(frameworkName) < 0)
                            frameworkName = fn;
                    }

                    // If any assembly requires X86, then the aggregate package requires it
                    if (subPackage.GetSetting(EnginePackageSettings.ImageRequiresX86, false))
                        requiresX86 = true;

                    if (subPackage.GetSetting(EnginePackageSettings.ImageRequiresDefaultAppDomainAssemblyResolver, false))
                        requiresAssemblyResolver = true;
                }
            }
            else if (PathUtils.IsAssemblyFileType(packageName))
            {
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
            }

            if (targetVersion.Major > 0)
                package.Settings[EnginePackageSettings.ImageRuntimeVersion] = targetVersion;

            if (!string.IsNullOrEmpty(frameworkName))
                package.Settings[EnginePackageSettings.ImageTargetFrameworkName] = frameworkName;

            package.Settings[EnginePackageSettings.ImageRequiresX86] = requiresX86;
            if (requiresX86)
                package.Settings[EnginePackageSettings.RunAsX86] = true;

            package.Settings[EnginePackageSettings.ImageRequiresDefaultAppDomainAssemblyResolver] = requiresAssemblyResolver;
        }
    }
}
#endif