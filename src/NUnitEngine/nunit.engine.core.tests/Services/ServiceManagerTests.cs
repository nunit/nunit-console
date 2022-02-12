// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using System;
using NUnit.Framework;

namespace NUnit.Engine.Services.Tests
{
    using Fakes;

    public class ServiceManagerTests
    {
        private IService _fakeService;
        private ServiceManager _serviceManager;

        [SetUp]
        public void SetUp()
        {
            _serviceManager = new ServiceManager();

            _fakeService = new FakeService();
            _serviceManager.AddService(_fakeService);
        }

        [Test]
        public void InitializeServices()
        {
            _serviceManager.StartServices();

            IService service = _serviceManager.GetService(typeof(IFakeService));
            Assert.That(service.Status, Is.EqualTo(ServiceStatus.Started));
            // TODO: Reinstate when this is moved to engine tests
            //service = _serviceManager.GetService(typeof(IExtensionService));
            //Assert.That(service.Status, Is.EqualTo(ServiceStatus.Started));
        }

        [Test]
        public void InitializationFailure()
        {
            ((FakeService)_fakeService).FailToStart = true;
            Assert.That(() => _serviceManager.StartServices(), 
                Throws.InstanceOf<InvalidOperationException>().And.Message.Contains("FakeService"));
        }

        [Test]
        public void TerminationFailure()
        {
            ((FakeService)_fakeService).FailedToStop = true;
            _fakeService.StartService();

            Assert.DoesNotThrow(() => _serviceManager.StopServices());
        }

        [Test]
        public void AccessServiceByClass()
        {
            IService service = _serviceManager.GetService(typeof(FakeService));
            Assert.That(service, Is.SameAs(_fakeService));
        }

        [Test]
        public void AccessServiceByInterface()
        {
            IService service = _serviceManager.GetService(typeof(IFakeService));
            Assert.That(service, Is.SameAs(_fakeService));
        }
    }
}
