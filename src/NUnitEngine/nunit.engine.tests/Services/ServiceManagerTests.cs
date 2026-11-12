// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using System;
using System.Linq;
using NUnit.Framework;

namespace NUnit.Engine.Services
{
    public class ServiceManagerTests
    {
        private IService _fakeService;
        private ServiceManager _serviceManager;
        private ServiceContext _context;

        [SetUp]
        public void SetUp()
        {
            _context = new ServiceContext();
            _serviceManager = _context.ServiceManager;

            _fakeService = new FakeService();
            _context.Add(_fakeService);
            _context.Add(new ExtensionService());
        }

        [TearDown]
        public void TearDown()
        {
            _serviceManager.StopServices();
            _serviceManager.Dispose();
        }

        [Test]
        public void InitializeServices()
        {
            _serviceManager.StartServices();

            IService? service = _serviceManager.GetServiceOrNull(typeof(IFakeService));
            Assert.That(service, Is.Not.Null);
            Assert.That(service.Status, Is.EqualTo(ServiceStatus.Started));
            service = _serviceManager.GetServiceOrNull(typeof(IExtensionService));
            Assert.That(service, Is.Not.Null);
            Assert.That(service.Status, Is.EqualTo(ServiceStatus.Started));
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
            IService? service = _serviceManager.GetServiceOrNull(typeof(FakeService));
            Assert.That(service, Is.SameAs(_fakeService));
        }

        [Test]
        public void AccessServiceByInterface()
        {
            IService? service = _serviceManager.GetServiceOrNull(typeof(IFakeService));
            Assert.That(service, Is.SameAs(_fakeService));
        }

        [Test]
        public void GetServicesReturnsAllCompatibleRegistrations()
        {
            var second = new FakeService();
            _context.Add(second);
            Assert.That(_context.GetServices<IFakeService>(), Is.EquivalentTo(new[] { _fakeService, second }));
            Assert.That(_context.GetServices<IExtensionService>(), Has.Exactly(1).TypeOf<ExtensionService>());
            Assert.That(_context.GetServices<IService>().ToArray(), Has.Length.EqualTo(3));
        }

        [Test]
        public void GetServicesDoesNotReplaceCachedSingleService()
        {
            IFakeService first = _context.GetService<IFakeService>();
            _context.Add(new FakeService());
            Assert.That(_context.GetServices<IFakeService>().ToArray(), Has.Length.EqualTo(2));
            Assert.That(_context.GetService<IFakeService>(), Is.SameAs(first));
            Assert.That(_context.TryGetService<IFakeService>(out var service), Is.True);
            Assert.That(service, Is.SameAs(first));
        }
    }
}
