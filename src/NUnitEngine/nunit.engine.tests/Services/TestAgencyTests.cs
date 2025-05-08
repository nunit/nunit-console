// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

#if NETFRAMEWORK
using NUnit.Common;
using NUnit.Engine.Services;
using NUnit.Framework;
using System;
using System.Linq;
using System.Runtime.Versioning;

namespace NUnit.Engine.Services
{
    public class TestAgencyTests
    {
        private TestAgency _testAgency;
        private ServiceContext _services;

        private const string NET462AGENT = "Net462Agent";
        private const string NET80AGENT = "Net80Agent";
        private static readonly string[] AVAILABLE_AGENTS = new[] { NET462AGENT, NET80AGENT };

        [SetUp]
        public void CreateServiceContext()
        {
            _services = new ServiceContext();
            _services.Add(new FakeRuntimeService());
            _testAgency = new TestAgency();
            _services.Add(_testAgency);
            _services.ServiceManager.StartServices();
        }

        [TearDown]
        public void TearDown()
        {
            _services.ServiceManager.Dispose();
        }

        [Test]
        public void ServiceIsStarted()
        {
            Assert.That(_testAgency.Status, Is.EqualTo(ServiceStatus.Started));
        }

        [Test]
        public void GetAvailableAgents()
        {
            Assert.That(_testAgency.AvailableAgents.Select(a => a.AgentName), Is.EquivalentTo(AVAILABLE_AGENTS));
        }

        [Test]
        public void GetAgentsForPackage_Net462()
        {
            var package = new TestPackage("test-assembly.dll");
            package.AddSetting(
                EnginePackageSettings.TargetFrameworkName,
                new FrameworkName(FrameworkIdentifiers.NetFramework, new Version(4, 6, 2)).ToString());
            Assert.That(_testAgency.GetAgentsForPackage(package).Select(a => a.AgentName), Is.EquivalentTo(new[] { NET462AGENT }));
        }

        [Test]
        public void GetAgentsForPackage_Net80()
        {
            var package = new TestPackage("test-assembly.dll");
            package.AddSetting(
                EnginePackageSettings.TargetFrameworkName,
                new FrameworkName(FrameworkIdentifiers.NetCoreApp, new Version(8, 0, 0)).ToString());
            Assert.That(_testAgency.GetAgentsForPackage(package).Select(a => a.AgentName), Is.EquivalentTo(new[] { NET80AGENT }));
        }

        [Test]
        public void GetAgentsForPackage_ConflictingAssemblies()
        {
            var package = new TestPackage("test1.dll", "test2.dll");
            package.SubPackages[0].AddSetting(
                EnginePackageSettings.TargetFrameworkName,
                new FrameworkName(FrameworkIdentifiers.NetFramework, new Version(4, 6, 2)).ToString());
            package.SubPackages[1].AddSetting(
                EnginePackageSettings.TargetFrameworkName,
                new FrameworkName(FrameworkIdentifiers.NetCoreApp, new Version(8, 0, 0)).ToString());
            Assert.That(_testAgency.GetAgentsForPackage(package), Is.Empty);
        }
    }
}
#endif
