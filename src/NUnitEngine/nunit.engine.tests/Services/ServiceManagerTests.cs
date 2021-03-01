// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using System;
using NUnit.Framework;

namespace NUnit.Engine.Services.Tests
{
    using Fakes;

    public class ServiceManagerTests
    {
        private ServiceManager _serviceManager;

        private IService1 _service1;
        private IService2 _service2;

        [SetUp]
        public void SetUp()
        {
            _serviceManager = new ServiceManager();

            _service1 = new FakeService1();
            _serviceManager.AddService(_service1);
            _service2 = new FakeService2();
            _serviceManager.AddService(_service2);
        }

        [Test]
        public void InitializeServices()
        {
            _serviceManager.StartServices();

            IService service = _serviceManager.GetService(typeof(IService1));
            Assert.That(service.Status, Is.EqualTo(ServiceStatus.Started));
            Assert.That(service, Is.SameAs(_service1));
            service = _serviceManager.GetService(typeof(IService2));
            Assert.That(service.Status, Is.EqualTo(ServiceStatus.Started));
            Assert.That(service, Is.SameAs(_service2));
        }

        [Test]
        public void InitializationFailure()
        {
            ((FakeService)_service1).FailToStart = true;
            Assert.That(() => _serviceManager.StartServices(),
                Throws.InstanceOf<InvalidOperationException>().And.Message.Contains("FakeService1"));
        }

        [Test]
        public void TerminationFailure()
        {
            ((FakeService)_service1).FailToStop = true;
            _service1.StartService();

            Assert.DoesNotThrow(() => _serviceManager.StopServices());
        }

        [Test]
        public void AccessServiceByClass()
        {
            IService service = _serviceManager.GetService(typeof(FakeService2));
            Assert.That(service, Is.SameAs(_service2));
        }

        public interface IService1 : IService { }
        public interface IService2 : IService { }

        public class FakeService1 : FakeService, IService1 { }
        public class FakeService2 : FakeService, IService2 { }
    }
}
