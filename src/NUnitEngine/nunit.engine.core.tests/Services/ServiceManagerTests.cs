// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using System;
using NUnit.Framework;

namespace NUnit.Engine.Services.Tests
{
    using Fakes;

    public class ServiceManagerTests
    {
        private IService _settingsService;
        private ServiceManager _serviceManager;

        private IService _projectService;

        [SetUp]
        public void SetUp()
        {
            _serviceManager = new ServiceManager();

            _settingsService = new FakeSettingsService();
            _serviceManager.AddService(_settingsService);

            _projectService = new Fakes.FakeProjectService();
            _serviceManager.AddService(_projectService);
        }

        [Test]
        public void InitializeServices()
        {
            _serviceManager.StartServices();

            IService service = _serviceManager.GetService(typeof(ISettings));
            Assert.That(service.Status, Is.EqualTo(ServiceStatus.Started));
            service = _serviceManager.GetService(typeof(IProjectService));
            Assert.That(service.Status, Is.EqualTo(ServiceStatus.Started));
        }

        [Test]
        public void InitializationFailure()
        {
            ((FakeSettingsService)_settingsService).FailToStart = true;
            Assert.That(() => _serviceManager.StartServices(), 
                Throws.InstanceOf<InvalidOperationException>().And.Message.Contains("FakeSettingsService"));
        }

        [Test]
        public void TerminationFailure()
        {
            ((FakeSettingsService)_settingsService).FailedToStop = true;
            _settingsService.StartService();

            Assert.DoesNotThrow(() => _serviceManager.StopServices());
        }

        [Test]
        public void AccessServiceByClass()
        {
            IService service = _serviceManager.GetService(typeof(FakeSettingsService));
            Assert.That(service, Is.SameAs(_settingsService));
        }

        [Test]
        public void AccessServiceByInterface()
        {
            IService service = _serviceManager.GetService(typeof(ISettings));
            Assert.That(service, Is.SameAs(_settingsService));
        }
    }
}
