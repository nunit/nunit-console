// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

#if NETFRAMEWORK
using NUnit.Engine.Services;
using NUnit.Engine.Services.Tests.Fakes;
using NUnit.Framework;

namespace NUnit.Engine.Tests.Services
{
    public class TestAgencyTests
    {
        private TestAgency _testAgency;
        private ServiceContext _services;

        [SetUp]
        public void CreateServiceContext()
        {
            var services = new ServiceContext();
            services.Add(new FakeRuntimeService());
            _testAgency = new TestAgency();
            services.Add(_testAgency);
            services.ServiceManager.StartServices();
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
    }
}
#endif