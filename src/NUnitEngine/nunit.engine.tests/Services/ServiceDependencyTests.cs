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
        public void TestRunnerFactory_ProjectServiceMissing()
        {
            var service = new TestRunnerFactory();
            _services.Add(service);
            service.StartService();
            Assert.That(service.Status, Is.EqualTo(ServiceStatus.Error));
        }
    }
}
