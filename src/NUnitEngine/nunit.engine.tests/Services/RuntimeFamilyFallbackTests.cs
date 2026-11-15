// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using System.Runtime.Versioning;
using NUnit.Common;
using NUnit.Framework;

namespace NUnit.Engine.Services
{
    /// <summary>Checks runtime-family boundaries without requiring installed runtimes or an operating system.</summary>
    public sealed class RuntimeFamilyFallbackTests
    {
        /// <summary>A CoreCLR host version is not a replacement Framework version.</summary>
        [TestCase(false), TestCase(true)]
        public void MissingFrameworkTargetKeepsItsFamilyAndVersionOnModernHost(bool x86)
        {
            Assert.That(Select(".NETCoreApp,Version=v10.0", ".NETFramework,Version=v4.6.2", x86),
                Is.EqualTo(".NETFramework,Version=v4.6.2"));
        }

        /// <summary>A Framework host version is not a replacement CoreCLR version.</summary>
        [TestCase(false), TestCase(true)]
        public void MissingModernTargetKeepsItsFamilyAndVersionOnFrameworkHost(bool x86)
        {
            Assert.That(Select(".NETFramework,Version=v4.8", ".NETCoreApp,Version=v3.1", x86),
                Is.EqualTo(".NETCoreApp,Version=v3.1"));
        }

        /// <summary>The existing fallback to a newer current CoreCLR version is retained.</summary>
        [TestCase(false), TestCase(true)]
        public void OlderModernTargetFallsBackWithinItsFamily(bool x86)
        {
            Assert.That(Select(".NETCoreApp,Version=v10.0", ".NETCoreApp,Version=v8.0", x86),
                Is.EqualTo(".NETCoreApp,Version=v10.0"));
        }

        /// <summary>The existing fallback within Framework profiles is retained.</summary>
        [TestCase(false), TestCase(true)]
        public void OlderFrameworkTargetFallsBackWithinItsFamily(bool x86)
        {
            Assert.That(Select(".NETFramework,Version=v4.8", ".NETFramework,Version=v4.6.2", x86),
                Is.EqualTo(".NETFramework,Version=v4.8"));
        }

        /// <summary>A newer target is not downgraded to the current host version.</summary>
        [TestCase(false), TestCase(true)]
        public void NewerTargetKeepsItsVersion(bool x86)
        {
            Assert.That(Select(".NETCoreApp,Version=v10.0", ".NETCoreApp,Version=v11.0", x86),
                Is.EqualTo(".NETCoreApp,Version=v11.0"));
        }

        /// <summary>An explicit unavailable runtime still fails rather than silently choosing another.</summary>
        [TestCase(false), TestCase(true)]
        public void ExplicitUnavailableRuntimeIsStillRejected(bool x86)
        {
            using var service = Service(".NETCoreApp,Version=v10.0");
            var package = Package(".NETFramework,Version=v4.6.2", x86);
            package.AddSetting(SettingDefinitions.RequestedRuntimeFramework.WithValue("net462"), false);
            Assert.That(() => service.SelectRuntimeFramework(package),
                Throws.TypeOf<NUnitEngineException>().With.Message.Contains("Requested framework is not available"));
            Assert.That(package.Settings.GetValueOrDefault(SettingDefinitions.TargetFrameworkName), Is.Empty);
        }

        private static string Select(string current, string target, bool x86)
        {
            using var service = Service(current);
            var package = Package(target, x86);
            service.SelectRuntimeFramework(package);
            Assert.That(package.Settings.GetValueOrDefault(SettingDefinitions.ImageTargetFrameworkName), Is.EqualTo(target));
            Assert.That(package.Settings.GetValueOrDefault(SettingDefinitions.RunAsX86), Is.EqualTo(x86));
            return package.Settings.GetValueOrDefault(SettingDefinitions.TargetFrameworkName);
        }

        /// <summary>
        /// Controls only metadata: an unstarted service has empty catalogues, and its
        /// reported host family is set explicitly. No simulated runtime is installed or loaded.
        /// </summary>
        private static RuntimeFrameworkService Service(string current)
        {
            var service = new RuntimeFrameworkService();
            Assert.That(service.AvailableRuntimes, Is.Empty);
            Assert.That(service.AvailableX86Runtimes, Is.Empty);
            var name = new FrameworkName(current);
            typeof(RuntimeFrameworkService).GetProperty(nameof(RuntimeFrameworkService.CurrentFramework))!
                .SetValue(service, new RuntimeFramework(name.Identifier, name.Version));
            return service;
        }

        private static TestPackage Package(string target, bool x86)
        {
            var package = new TestPackage("runtime-family-selection").SubPackages[0];
            package.AddSetting(SettingDefinitions.ImageTargetFrameworkName.WithValue(target), false);
            package.AddSetting(SettingDefinitions.RunAsX86.WithValue(x86), false);
            return package;
        }
    }
}
