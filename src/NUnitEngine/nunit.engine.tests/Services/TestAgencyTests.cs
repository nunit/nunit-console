// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

#if NETFRAMEWORK
using NUnit.Framework;

namespace NUnit.Engine.Services.Tests
{
    using Fakes;

    public class TestAgencyTests
    {
        private TestAgency _testAgency;

        [SetUp]
        public void CreateServiceContext()
        {
            var services = new ServiceContext();
            services.Add(new FakeRuntimeService());
            // Use a different URI to avoid conflicting with the "real" TestAgency
            _testAgency = new TestAgency("TestAgencyTest", 0);
            services.Add(_testAgency);
            services.ServiceManager.StartServices();
        }

        [Test]
        public void ServiceIsStarted()
        {
            Assert.That(_testAgency.Status, Is.EqualTo(ServiceStatus.Started));
        }
    }
}
#endif