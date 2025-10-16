// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

#if NETFRAMEWORK
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Versioning;
using NUnit.Common;
using NUnit.Engine.Internal;
using NUnit.Engine.Services.RuntimeLocators;
using TestCentric.Metadata;

namespace NUnit.Engine.Services
{
    public class RuntimeFrameworkService : Service, IRuntimeFrameworkService, IAvailableRuntimes
    {
        static readonly Logger log = InternalTrace.GetLogger(typeof(RuntimeFrameworkService));

        private List<RuntimeFramework> _availableRuntimes = new List<RuntimeFramework>();
        private List<RuntimeFramework> _availableX86Runtimes = new List<RuntimeFramework>();

        /// <summary>
        /// Gets a list of available runtimes.
        /// </summary>
        public IList<IRuntimeFramework> AvailableRuntimes => _availableRuntimes.ToArray();

        /// <summary>
        /// Gets a list of available runtimes.
        /// </summary>
        public IList<IRuntimeFramework> AvailableX86Runtimes => _availableX86Runtimes.ToArray();

        /// <summary>
        /// Returns true if the runtime framework represented by
        /// the string passed as an argument is available.
        /// </summary>
        /// <param name="name">A string representing a framework, like 'net-4.0'</param>
        /// <param name="x86">A flag indicating whether the X86 architecture is needed. Defaults to false.</param>
        /// <returns>True if the framework is available, false if unavailable or nonexistent</returns>
        public bool IsAvailable(string name, bool x86)
        {
            Guard.ArgumentNotNullOrEmpty(name, nameof(name));

            if (!RuntimeFramework.TryParse(name, out RuntimeFramework requestedFramework))
                throw new NUnitEngineException("Invalid or unknown framework requested: " + name);

            var runtimes = x86 ? _availableX86Runtimes : _availableRuntimes;
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

        private static bool RuntimesMatch(RuntimeType requested, RuntimeType available)
        {
            if (requested == available || requested == RuntimeType.Any)
                return true;

            if (requested == RuntimeType.Net && available == RuntimeType.Mono)
                return true;

            if (requested == RuntimeType.Mono && available == RuntimeType.Net)
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
        public void SelectRuntimeFramework(TestPackage package)
        {
            // Evaluate package target framework
            ApplyImageData(package);

            SelectRuntimeFrameworkInner(package);
        }

        private void SelectRuntimeFrameworkInner(TestPackage package)
        {
            foreach (var subPackage in package.SubPackages)
            {
                SelectRuntimeFrameworkInner(subPackage);
            }

            // Examine the provided settings
            RuntimeFramework currentFramework = RuntimeFramework.CurrentFramework;
            log.Debug("Current framework is " + currentFramework);

            string frameworkSetting = package.GetSetting(EnginePackageSettings.RequestedRuntimeFramework, "");
            bool runAsX86 = package.GetSetting(EnginePackageSettings.RunAsX86, false);

            if (frameworkSetting.Length > 0)
            {
                if (!RuntimeFramework.TryParse(frameworkSetting, out RuntimeFramework requestedFramework))
                    throw new NUnitEngineException("Invalid or unknown framework requested: " + frameworkSetting);

                log.Debug($"Requested framework for {package.Name} is {requestedFramework}");

                if (!IsAvailable(frameworkSetting, runAsX86))
                    throw new NUnitEngineException("Requested framework is not available: " + frameworkSetting);

                package.Settings[EnginePackageSettings.TargetRuntimeFramework] = frameworkSetting;
            }

            log.Debug($"No specific framework requested for {package.Name}");

            string imageTargetFrameworkNameSetting = package.GetSetting(InternalEnginePackageSettings.ImageTargetFrameworkName, "");
            RuntimeType targetRuntime = RuntimeType.Any;
            Version targetVersion = new Version(0,0);

            if (string.IsNullOrEmpty(imageTargetFrameworkNameSetting))
            {
                // Assume .NET Framework 2.0
                targetRuntime = currentFramework.Runtime;
                targetVersion = package.GetSetting(InternalEnginePackageSettings.ImageRuntimeVersion, new Version(2, 0));
            }
            else
            {
                FrameworkName frameworkName = new FrameworkName(imageTargetFrameworkNameSetting);

                switch (frameworkName.Identifier)
                {
                    case ".NETFramework":
                        targetRuntime = RuntimeType.Net;
                        targetVersion = frameworkName.Version;
                        break;
                    case ".NETCoreApp":
                        targetRuntime = RuntimeType.NetCore;
                        targetVersion = frameworkName.Version;
                        break;
                    case ".NETStandard":
                        targetRuntime = RuntimeType.NetCore;
                        targetVersion = new Version(3, 1);
                        break;
                    case "Unmanaged":
                        break;
                    default:
                        throw new NUnitEngineException("Unsupported Target Framework: " + imageTargetFrameworkNameSetting);
                }
            }

            // All cases except Unmanaged set the runtime type
            if (targetRuntime == RuntimeType.Any)
                package.Settings[InternalEnginePackageSettings.ImageTargetFrameworkName] = "Unmanaged,Version=0.0";
            else
            {
                if (!IsAvailable(new RuntimeFramework(targetRuntime, targetVersion).Id, runAsX86))
                {
                    log.Debug("Preferred version {0} is not installed or this NUnit installation does not support it", targetVersion);
                    if (targetVersion < currentFramework.FrameworkVersion)
                        targetVersion = currentFramework.FrameworkVersion;
                }

                RuntimeFramework targetFramework = new RuntimeFramework(targetRuntime, targetVersion);
                package.Settings[EnginePackageSettings.TargetRuntimeFramework] = targetFramework.ToString();

                log.Debug($"Test will use {targetFramework} for {package.Name}");
            }
        }

        public override void StartService()
        {
            try
            {
                FindAvailableRuntimes();
            }
            catch
            {
                Status = ServiceStatus.Error;
                throw;
            }

            Status = ServiceStatus.Started;
        }

        /// <summary>
        /// Returns the best available framework that matches a target framework.
        /// If the target framework has a build number specified, then an exact
        /// match is needed. Otherwise, the matching framework with the highest
        /// build number is used.
        /// </summary>
        public RuntimeFramework GetBestAvailableFramework(RuntimeFramework target)
        {
            RuntimeFramework result = target;

            foreach (RuntimeFramework framework in _availableRuntimes)
                if (framework.Supports(target))
                {
                    if (framework.ClrVersion.Build > result.ClrVersion.Build)
                        result = framework;
                }

            return result;
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

            var monoRuntimes = MonoRuntimeLocator.FindRuntimes();
            _availableRuntimes.AddRange(monoRuntimes);
            _availableX86Runtimes.AddRange(monoRuntimes);

            _availableRuntimes.AddRange(NetCoreRuntimeLocator.FindRuntimes(x86: false));
            _availableX86Runtimes.AddRange(NetCoreRuntimeLocator.FindRuntimes(x86: true));
        }

        /// <summary>
        /// Use TestCentric.Metadata to get information about all assemblies and
        /// apply it to the package using special internal keywords.
        /// </summary>
        /// <param name="package"></param>
        private static void ApplyImageData(TestPackage package)
        {
            string packageName = package.FullName;

            Version targetVersion = new Version(0, 0);
            string frameworkName = null;
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
                    Version v = subPackage.GetSetting(InternalEnginePackageSettings.ImageRuntimeVersion, new Version(0, 0));
                    if (v > targetVersion) targetVersion = v;

                    // Collect highest framework name
                    // TODO: This assumes lexical ordering is valid - check it
                    string fn = subPackage.GetSetting(InternalEnginePackageSettings.ImageTargetFrameworkName, "");
                    if (fn != "")
                    {
                        if (frameworkName == null || fn.CompareTo(frameworkName) < 0)
                            frameworkName = fn;
                    }

                    // If any assembly requires X86, then the aggregate package requires it
                    if (subPackage.GetSetting(InternalEnginePackageSettings.ImageRequiresX86, false))
                        requiresX86 = true;

                    if (subPackage.GetSetting(InternalEnginePackageSettings.ImageRequiresDefaultAppDomainAssemblyResolver, false))
                        requiresAssemblyResolver = true;
                }
            }
            else if (File.Exists(packageName) && PathUtils.IsAssemblyFileType(packageName))
            {
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
                package.Settings[InternalEnginePackageSettings.ImageRuntimeVersion] = targetVersion;

            if (!string.IsNullOrEmpty(frameworkName))
                package.Settings[InternalEnginePackageSettings.ImageTargetFrameworkName] = frameworkName;

            package.Settings[InternalEnginePackageSettings.ImageRequiresX86] = requiresX86;
            if (requiresX86)
                package.Settings[EnginePackageSettings.RunAsX86] = true;

            package.Settings[InternalEnginePackageSettings.ImageRequiresDefaultAppDomainAssemblyResolver] = requiresAssemblyResolver;
        }
    }
}
#endif