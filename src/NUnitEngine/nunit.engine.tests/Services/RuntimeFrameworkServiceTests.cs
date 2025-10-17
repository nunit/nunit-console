// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

#if NETFRAMEWORK
using System;
using System.IO;
using NUnit.Framework;

namespace NUnit.Engine.Services
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

        [TestCase("mock-assembly.dll", "net-4.6.2", false)]
        [TestCase("../agents/net462/nunit-agent.exe", "net-4.6.2", false)]
        [TestCase("../agents/net462/nunit-agent-x86.exe", "net-4.6.2", true)]
        [TestCase("../netstandard2.0/nunit.engine.api.dll", "netcore-3.1", false)]
        public void SelectRuntimeFramework(string assemblyName, string expectedRuntime, bool runAsX86)
        {
            var package = new TestPackage(Path.Combine(TestContext.CurrentContext.TestDirectory, assemblyName));
            
            _runtimeService.SelectRuntimeFramework(package);

            Assert.That(package.GetSetting("TargetRuntimeFramework", ""), Is.EqualTo(expectedRuntime));
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

        //[TestCase("mono", 2, 0, "net-4.0")]
        //[TestCase("net", 2, 0, "net-4.0")]
        //[TestCase("net", 3, 5, "net-4.0")]

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