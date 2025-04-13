// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

#if NETFRAMEWORK
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using NUnit.Framework;

namespace NUnit.Engine.Services
{
    public class RuntimeFrameworkServiceTests
    {
        private RuntimeFrameworkService _runtimeService;

        // We can do this because we currently only build under NETFRAMEWORK
        private static Runtime _currentRuntime =
            Type.GetType("Mono.Runtime", false) != null
                ? Runtime.Mono
                : Runtime.Net;

        // TODO: We cast IRuntimeFramework to RuntimeFramework in several
        // places here. Ideally, we should deal with the interfaces but
        // they would need to be changed to do that. For now, the casts are
        // used since we may end up eliminating the RuntimeFramework class.
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
            _runtimeService.Dispose();
        }

        [Test]
        public void ServiceIsStarted()
        {
            Assert.That(_runtimeService.Status, Is.EqualTo(ServiceStatus.Started));
        }

#if DEBUG
        private const string AGENTS_DIR = "../../../../nunit.engine/bin/Debug/agents/";
#else
        private const string AGENTS_DIR = "../../../../nunit.engine/bin/Release/agents/";
#endif

        [TestCase("net35", false)]
        [TestCase("net462", false)]
        [TestCase("net462", true)]
        public void SelectRuntimeFramework(string runtime, bool runAsX86)
        {
            var assemblyPath = Path.GetFullPath($"testdata/{runtime}/{(runAsX86 ? "mock-assembly-x86.dll" : "mock-assembly.dll")}");

            Assert.That(File.Exists(assemblyPath), $"File does not exist: {assemblyPath}");
            var package = new TestPackage(assemblyPath);

            var returnValue = _runtimeService.SelectRuntimeFramework(package);

            Assert.That(package.GetSetting("TargetRuntimeFramework", string.Empty), Is.EqualTo(returnValue));
            Assert.That(package.GetSetting("RunAsX86", false), Is.EqualTo(runAsX86));
        }

        [Test]
        public void CanGetCurrentFramework()
        {
            var framework = _runtimeService.CurrentFramework as RuntimeFramework;
            Assert.That(framework, Is.Not.Null);
            Assert.That(framework.Runtime, Is.EqualTo(_currentRuntime));
        }

        [Test]
        public void AvailableFrameworks()
        {
            var available = _runtimeService.AvailableRuntimes;
            Assert.That(available.Count, Is.GreaterThan(0));
            foreach (var framework in available)
                Console.WriteLine("Available: {0}", framework.DisplayName);
        }

        [Test]
        public void CurrentFrameworkMustBeAvailable()
        {
            var current = _runtimeService.CurrentFramework;
            Assert.That(current, Is.Not.Null);
            Console.WriteLine("Current framework is {0} ({1})", current.DisplayName, current.Id);
            Assert.That(_runtimeService.IsAvailable(current.Id, false), "{current} not available");
        }

        [Test]
        public void AvailableFrameworksList_IncludesCurrentFramework()
        {
            var current = _runtimeService.CurrentFramework as RuntimeFramework;
            Assert.That(current, Is.Not.Null);

            foreach (var framework in _runtimeService.AvailableRuntimes)
            {
                RuntimeFramework? runtimeFramework = framework as RuntimeFramework;
                Assert.That(runtimeFramework, Is.Not.Null);
                if (current.Supports(runtimeFramework))
                    return;
            }

            Assert.Fail("CurrentFramework not listed as available");
        }

        [Test]
        public void AvailableFrameworksList_ContainsNoDuplicates()
        {
            var names = new List<string>();
            foreach (var framework in _runtimeService.AvailableRuntimes)
                names.Add(framework.DisplayName);
            Assert.That(names, Is.Unique);
        }

        [TestCase("mono", 2, 0, "net-4.0")]
        [TestCase("net", 2, 0, "net-4.0")]
        [TestCase("net", 3, 5, "net-4.0")]

        public void EngineOptionPreferredOverImageTarget(string framework, int majorVersion, int minorVersion, string requested)
        {
            var package = new TestPackage("test");
            package.AddSetting(EnginePackageSettings.ImageTargetFrameworkName, framework);
            package.AddSetting(EnginePackageSettings.ImageRuntimeVersion, new Version(majorVersion, minorVersion));
            package.AddSetting(EnginePackageSettings.RequestedRuntimeFramework, requested);

            _runtimeService.SelectRuntimeFramework(package);
            Assert.That(package.GetSetting<string>(EnginePackageSettings.RequestedRuntimeFramework, string.Empty), Is.EqualTo(requested));
        }

        [Test]
        public void RuntimeFrameworkIsSetForSubpackages()
        {
            //Runtime Service verifies that requested frameworks are available, therefore this test can only currently be run on platforms with both CLR v2 and v4 available
            Assume.That(_runtimeService.IsAvailable("net-2.0", false));
            Assume.That(_runtimeService.IsAvailable("net-2.0", true));
            Assume.That(_runtimeService.IsAvailable("net-4.0", false));
            Assume.That(_runtimeService.IsAvailable("net-4.0", true));

            var topLevelPackage = new TestPackage(new string[] { "a.dll", "b.dll" });

            var net20Package = topLevelPackage.SubPackages[0];
            net20Package.Settings.Add(EnginePackageSettings.ImageRuntimeVersion, new Version("2.0"));
            var net40Package = topLevelPackage.SubPackages[1];
            net40Package.Settings.Add(EnginePackageSettings.ImageRuntimeVersion, new Version("4.0"));

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