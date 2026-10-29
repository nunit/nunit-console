// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using System;
using NUnit.Common;
using NUnit.Engine.Runners;
using NUnit.Framework;
using NSubstitute;

namespace NUnit.Engine.Services
{
    /// <summary>
    /// Uses an unstarted service's empty local catalog to distinguish portable remote
    /// runtime constraints from local installation checks, without changing the machine.
    /// </summary>
    public sealed class RemoteRuntimeSelectionTests
    {
        private static readonly string[] RemoteAssemblyPaths = { "remote1.dll", "remote2.dll" };
        private static readonly string[] MixedAssemblyPaths = { "remote.dll", "local.dll" };

        [TestCase("net462", ".NETFramework,Version=v4.6.2", false)]
        [TestCase("net462", ".NETFramework,Version=v4.6.2", true)]
        [TestCase("net10.0", ".NETCoreApp,Version=v10.0", false)]
        [TestCase("netcoreapp6.0", ".NETCoreApp,Version=v6.0", false)]
        public void RemoteRequestDoesNotRequireAnInstalledCoordinatorRuntime(string tfm, string expected, bool x86)
        {
            using var service = new RuntimeFrameworkService();
            var package = Package(".NETCoreApp,Version=v2.0", true);
            package.AddSetting(SettingDefinitions.RequestedRuntimeFramework.WithValue(tfm), false);
            package.AddSetting(SettingDefinitions.RunAsX86.WithValue(x86), false);
            Assert.That(service.IsAvailable(tfm, x86), Is.False);

            service.SelectRuntimeFramework(package);

            Assert.That(package.Settings.GetValueOrDefault(SettingDefinitions.RequestedFrameworkName), Is.EqualTo(expected));
            Assert.That(package.Settings.GetValueOrDefault(SettingDefinitions.TargetFrameworkName), Is.EqualTo(expected));
            Assert.That(package.Settings.GetValueOrDefault(SettingDefinitions.RequestedRuntimeFramework), Is.EqualTo(tfm));
            Assert.That(package.Settings.GetValueOrDefault(SettingDefinitions.RunAsX86), Is.EqualTo(x86));
        }

        [TestCase("Any")]
        [TestCase("LocalProcess")]
        [TestCase("InProcess")]
        public void OtherAgentTypesRetainLocalAvailabilityChecks(string type)
        {
            using var service = new RuntimeFrameworkService();
            var package = Package(".NETCoreApp,Version=v2.0", false);
            package.AddSetting(RemoteAgentSettings.AgentType.WithValue(type), false);
            package.AddSetting(SettingDefinitions.RequestedRuntimeFramework.WithValue("net10.0"), false);

            Assert.That(() => service.SelectRuntimeFramework(package),
                Throws.TypeOf<NUnitEngineException>().With.Message.Contains("Requested framework is not available"));
        }

        [TestCase(".NETFramework,Version=v2.0", ".NETFramework,Version=v2.0")]
        [TestCase(".NETCoreApp,Version=v2.0", ".NETCoreApp,Version=v2.0")]
        [TestCase(".NETStandard,Version=v2.0", ".NETCoreApp,Version=v3.1")]
        public void RemoteImageTargetIsNotUpgradedToTheCoordinatorRuntime(string image, string expected)
        {
            using var service = new RuntimeFrameworkService();
            var package = Package(image, true);

            service.SelectRuntimeFramework(package);

            Assert.That(package.Settings.GetValueOrDefault(SettingDefinitions.TargetFrameworkName), Is.EqualTo(expected));
            Assert.That(package.Settings.GetValueOrDefault(SettingDefinitions.ImageTargetFrameworkName), Is.EqualTo(image));
            Assert.That(package.Settings.HasSetting(SettingDefinitions.RequestedFrameworkName), Is.False);
        }

        [TestCase(".NETFramework")]
        [TestCase(".NETCoreApp")]
        public void LocalImageTargetRetainsCoordinatorFallback(string identifier)
        {
            using var service = new RuntimeFrameworkService();
            var package = Package(identifier + ",Version=v2.0", false);

            service.SelectRuntimeFramework(package);

            string expected = new RuntimeFramework(identifier, service.CurrentFramework.FrameworkName.Version).FrameworkName.ToString();
            Assert.That(package.Settings.GetValueOrDefault(SettingDefinitions.TargetFrameworkName), Is.EqualTo(expected));
        }

        [Test]
        public void RemoteLegacyImageRetainsItsRuntimeConstraint()
        {
            using var service = new RuntimeFrameworkService();
            var package = Package(string.Empty, true);
            package.AddSetting(SettingDefinitions.ImageRuntimeVersion.WithValue("2.0.50727"), false);

            service.SelectRuntimeFramework(package);

            Assert.That(package.Settings.GetValueOrDefault(SettingDefinitions.TargetFrameworkName), Is.EqualTo(".NETFramework,Version=v2.0"));
        }

        [Test]
        public void ExplicitRemoteAgentTypeUsesTheSameSelectionPolicy()
        {
            using var service = new RuntimeFrameworkService();
            var package = Package(".NETCoreApp,Version=v2.0", false);
            package.AddSetting(RemoteAgentSettings.AgentType.WithValue("RemoteProcess"), false);

            service.SelectRuntimeFramework(package);

            Assert.That(package.Settings.GetValueOrDefault(SettingDefinitions.TargetFrameworkName), Is.EqualTo(".NETCoreApp,Version=v2.0"));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void InvalidRuntimeRequestIsStillRejected(bool remote)
        {
            using var service = new RuntimeFrameworkService();
            var package = Package(".NETCoreApp,Version=v2.0", remote);
            package.AddSetting(SettingDefinitions.RequestedRuntimeFramework.WithValue("invalid"), false);

            Assert.That(() => service.SelectRuntimeFramework(package), Throws.ArgumentException);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void RemoteUnmanagedImageCannotFallThroughToLocalExecution(bool explicitRuntime)
        {
            using var service = new RuntimeFrameworkService();
            var package = Package("Unmanaged,Version=0.0", true);
            if (explicitRuntime)
                package.AddSetting(SettingDefinitions.RequestedRuntimeFramework.WithValue("net10.0"), false);

            Assert.That(() => service.SelectRuntimeFramework(package),
                Throws.TypeOf<NUnitEngineException>().With.Message.Contains("unmanaged"));
        }

        [Test]
        public void LocalUnmanagedImageHandlingIsUnchanged()
        {
            using var service = new RuntimeFrameworkService();
            var package = Package("Unmanaged,Version=0.0", false);

            Assert.That(() => service.SelectRuntimeFramework(package), Throws.Nothing);
            Assert.That(package.Settings.GetValueOrDefault(SettingDefinitions.ImageTargetFrameworkName), Is.EqualTo("Unmanaged,Version=0.0"));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void MasterRunnerValidatesRemoteOnlyPackagesWithoutLocalAvailability(bool rootProfile)
        {
            var services = new FakeServiceContext();
            using var manager = services.ServiceManager;
            services.Initialize();
            var package = new TestPackage(RemoteAssemblyPaths);
            package.AddSetting(SettingDefinitions.RequestedRuntimeFramework.WithValue("net10.0"), true);
            foreach (var child in package.SubPackages)
                child.AddSetting(RemoteAgentSettings.ServerDefinition.WithValue("unused.xml"), false);
            if (rootProfile)
                package.AddSetting(RemoteAgentSettings.ServerDefinition.WithValue("unused.xml"), false);

            using var runner = new MasterTestRunner(services, package);

            services.RuntimeFrameworkService.DidNotReceive().IsAvailable(Arg.Any<string>(), Arg.Any<bool>());
        }

        [Test]
        public void MasterRunnerRetainsLocalValidationForMixedPackages()
        {
            var services = new FakeServiceContext();
            using var manager = services.ServiceManager;
            services.Initialize();
            var package = new TestPackage(MixedAssemblyPaths);
            package.AddSetting(SettingDefinitions.RequestedRuntimeFramework.WithValue("net10.0"), true);
            package.SubPackages[0].AddSetting(RemoteAgentSettings.ServerDefinition.WithValue("unused.xml"), false);

            Assert.That(() => new MasterTestRunner(services, package),
                Throws.TypeOf<NUnitEngineException>().With.Message.Contains("unknown or not available"));
            services.RuntimeFrameworkService.Received(1).IsAvailable("net10.0", false);
        }

        [TestCase("invalid")]
        [TestCase("unsupported4.0")]
        public void MasterRunnerStillRejectsInvalidRemoteRuntimeSyntax(string tfm)
        {
            var services = new FakeServiceContext();
            using var manager = services.ServiceManager;
            services.Initialize();
            var package = new TestPackage("remote.dll");
            package.AddSetting(RemoteAgentSettings.ServerDefinition.WithValue("unused.xml"), true);
            package.AddSetting(SettingDefinitions.RequestedRuntimeFramework.WithValue(tfm), true);

            Assert.That(() => new MasterTestRunner(services, package), Throws.ArgumentException);
        }

        private static TestPackage Package(string imageFramework, bool remote)
        {
            // No assembly path or endpoint is read. These are deliberate metadata-only packages.
            var package = new TestPackage("runtime-selection-input").SubPackages[0];
            package.AddSetting(SettingDefinitions.ImageTargetFrameworkName.WithValue(imageFramework), false);
            if (remote)
                package.AddSetting(RemoteAgentSettings.ServerDefinition.WithValue("not-opened-in-this-test.xml"), false);
            return package;
        }
    }
}
