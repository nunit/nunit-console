// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

#if NETFRAMEWORK
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using NUnit.Framework;
using NUnit.Tests;

namespace NUnit.Engine.Services.Tests
{
    public class RuntimeFrameworkServiceTests
    {
        private RuntimeFrameworkService _runtimeService;

        [SetUp]
        public void CreateServiceContext()
        {
            var services = new ServiceContext();
            _runtimeService = new RuntimeFrameworkService();
            services.Add(_runtimeService);
            services.ServiceManager.StartServices();
        }

        [TearDown]
        public void StopService()
        {
            _runtimeService.StopService();
        }

        [Test]
        public void ServiceIsStarted()
        {
            Assert.That(_runtimeService.Status, Is.EqualTo(ServiceStatus.Started));
        }

        [TestCase("mock-assembly.dll", false)]
        [TestCase("../agents/net20/nunit-agent.exe", false)]
        [TestCase("../agents/net20/nunit-agent-x86.exe", true)]
        public void SelectRuntimeFramework(string assemblyName, bool runAsX86)
        {
            var package = new TestPackage(Path.Combine(TestContext.CurrentContext.TestDirectory, assemblyName));

            var returnValue = _runtimeService.SelectRuntimeFramework(package);

            Assert.That(package.GetSetting("TargetRuntimeFramework", ""), Is.EqualTo(returnValue));
            Assert.That(package.GetSetting("RunAsX86", false), Is.EqualTo(runAsX86));
        }

        [Test]
        public void AvailableFrameworks()
        {
            var available = _runtimeService.AvailableRuntimes;
            Assert.That(available.Count, Is.GreaterThan(0));
            foreach (var framework in available)
                Console.WriteLine("Available: {0}", framework.DisplayName);
        }

        [TestCase("mono", 4, 5, "net-4.5")]
        [TestCase("net", 4, 0, "net-4.5")]
        [TestCase("net", 4, 5, "net-4.5")]

        public void EngineOptionPreferredOverImageTarget(string framework, int majorVersion, int minorVersion, string requested)
        {
            var package = new TestPackage("test");
            package.AddSetting(InternalEnginePackageSettings.ImageTargetFrameworkName, framework);
            package.AddSetting(InternalEnginePackageSettings.ImageRuntimeVersion, new Version(majorVersion, minorVersion));
            package.AddSetting(EnginePackageSettings.RequestedRuntimeFramework, requested);

            _runtimeService.SelectRuntimeFramework(package);
            Assert.That(package.GetSetting<string>(EnginePackageSettings.RequestedRuntimeFramework, null), Is.EqualTo(requested));
        }

        [Test]
        public void RuntimeFrameworkIsSetForSubpackages()
        {
            //Runtime Service verifies that requested frameworks are available, therefore this test can only currently be run on platforms with both CLR v2 and v4 available
            Assume.That(new RuntimeFramework(RuntimeType.Net, new Version("2.0.50727")), Has.Property(nameof(RuntimeFramework.IsAvailable)).True);
            Assume.That(new RuntimeFramework(RuntimeType.Net, new Version("4.0.30319")), Has.Property(nameof(RuntimeFramework.IsAvailable)).True);

            var topLevelPackage = new TestPackage(new [] {"a.dll", "b.dll"});

            var net20Package = topLevelPackage.SubPackages[0];
            net20Package.Settings.Add(InternalEnginePackageSettings.ImageRuntimeVersion, new Version("2.0.50727"));
            var net40Package = topLevelPackage.SubPackages[1];
            net40Package.Settings.Add(InternalEnginePackageSettings.ImageRuntimeVersion, new Version("4.0.30319"));

            _runtimeService.SelectRuntimeFramework(topLevelPackage);

            Assert.Multiple(() =>
            {
                Assert.That(net20Package.Settings[EnginePackageSettings.TargetRuntimeFramework], Is.EqualTo("net-2.0"));
                Assert.That(net40Package.Settings[EnginePackageSettings.TargetRuntimeFramework], Is.EqualTo("net-4.0"));
                Assert.That(topLevelPackage.Settings[EnginePackageSettings.TargetRuntimeFramework], Is.EqualTo("net-4.0"));
            });
        }
    }
}
#endif