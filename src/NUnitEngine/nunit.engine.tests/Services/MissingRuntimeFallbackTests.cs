// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

#if !NETFRAMEWORK
using System.Linq;
using NUnit.Common;
using NUnit.Extensibility;
using NUnit.Framework;

namespace NUnit.Engine.Services
{
    /// <summary>
    /// Exercises runtime selection and launcher metadata together with a deliberately
    /// empty catalogue, independently of installed legacy runtimes or machine changes.
    /// </summary>
    public class MissingRuntimeFallbackTests
    {
        /// <summary>
        /// The launcher must support the selected runtime when the assembly's target is unavailable.
        /// </summary>
        [TestCase(6, false)]
        [TestCase(6, true)]
        [TestCase(7, false)]
        [TestCase(7, true)]
        [TestCase(8, false)]
        [TestCase(8, true)]
        public void SelectedRuntimeControlsLauncherCompatibility(int imageVersion, bool x86)
        {
            using var service = new RuntimeFrameworkService();
            Assert.That(service.AvailableRuntimes, Is.Empty);
            Assert.That(service.AvailableX86Runtimes, Is.Empty);
            var package = Package(imageVersion, x86);
            string imageTarget = package.Settings.GetValueOrDefault(SettingDefinitions.ImageTargetFrameworkName);

            service.SelectRuntimeFramework(package);

            string selected = service.CurrentFramework.FrameworkName.FullName;
            Assert.That(package.Settings.GetValueOrDefault(SettingDefinitions.TargetFrameworkName), Is.EqualTo(selected));
            Assert.That(package.Settings.GetValueOrDefault(SettingDefinitions.ImageTargetFrameworkName), Is.EqualTo(imageTarget));
            using var assembly = new ExtensionAssembly(typeof(MissingRuntimeFallbackTests).Assembly.Location, false);
            var type = assembly.Assembly.MainModule.Types.Single(value => value.FullName == typeof(MissingRuntimeFallbackTests).FullName);
            var older = new ExtensionNode(assembly, type);
            older.AddProperty("TargetFramework", ".NETCoreApp,Version=v8.0");
            var current = new ExtensionNode(assembly, type);
            current.AddProperty("TargetFramework", selected);
            Assert.That(LocalProcessAgentProvider.CanCreateAgent(older, package), Is.False);
            Assert.That(LocalProcessAgentProvider.CanCreateAgent(current, package), Is.True);
        }

        /// <summary>An explicit unavailable runtime remains an error rather than being silently substituted.</summary>
        [TestCase(false)]
        [TestCase(true)]
        public void ExplicitUnavailableRuntimeFailsBeforeLauncherSelection(bool x86)
        {
            using var service = new RuntimeFrameworkService();
            var package = Package(7, x86);
            package.AddSetting(SettingDefinitions.RequestedRuntimeFramework.WithValue("net7.0"), false);

            Assert.That(() => service.SelectRuntimeFramework(package),
                Throws.TypeOf<NUnitEngineException>().With.Message.Contains("Requested framework is not available"));
            Assert.That(package.Settings.GetValueOrDefault(SettingDefinitions.TargetFrameworkName), Is.Empty);
        }

        private static TestPackage Package(int version, bool x86)
        {
            // No assembly suffix: this isolates selection from assembly metadata reads.
            var package = new TestPackage("runtime-selection").SubPackages[0];
            package.AddSetting(SettingDefinitions.ImageTargetFrameworkName.WithValue($".NETCoreApp,Version=v{version}.0"), false);
            package.AddSetting(SettingDefinitions.RunAsX86.WithValue(x86), false);
            return package;
        }
    }
}
#endif
