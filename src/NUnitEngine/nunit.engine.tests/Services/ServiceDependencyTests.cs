// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using System;
using NUnit.Framework;

namespace NUnit.Engine.Services.Tests
{
    using Fakes;

    public class ServiceDependencyTests
    {
        private ServiceContext _services;

        [SetUp]
        public void CreateServiceContext()
        {
            _services = new ServiceContext();
        }

        [Test]
        public void RecentFilesService_SettingsServiceError()
        {
            var fake = new FakeSettingsService();
            fake.FailToStart = true;
            _services.Add(fake);
            var service = new RecentFilesService();
            _services.Add(service);
            ((IService)fake).StartService();
            service.StartService();
            Assert.That(service.Status, Is.EqualTo(ServiceStatus.Error));
        }

        [Test]
        public void RecentFilesService_SettingsServiceMissing()
        {
            var service = new RecentFilesService();
            _services.Add(service);
            service.StartService();
            Assert.That(service.Status, Is.EqualTo(ServiceStatus.Error));
        }

        [Test]
        public void DefaultTestRunnerFactory_ProjectServiceError()
        {
            var fake = new FakeProjectService();
            fake.FailToStart = true;
            _services.Add(fake);
            var service = new RecentFilesService();
            _services.Add(service);
            ((IService)fake).StartService();
            service.StartService();
            Assert.That(service.Status, Is.EqualTo(ServiceStatus.Error));
        }

        [Test]
        public void DefaultTestRunnerFactory_ProjectServiceMissing()
        {
            var service = new DefaultTestRunnerFactory();
            _services.Add(service);
            service.StartService();
            Assert.That(service.Status, Is.EqualTo(ServiceStatus.Error));
        }
    }
}
