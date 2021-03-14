// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using System;
using System.Collections.Generic;
using System.Text;
using NUnit.Engine.Runners;
using NUnit.Framework;

namespace NUnit.Engine.Services.Tests
{
    using Fakes;

    public class InProcessTestRunnerFactoryTests
    {
        private InProcessTestRunnerFactory _factory;

        [SetUp]
        public void CreateServiceContext()
        {
            var services = new ServiceContext();
            _factory = new InProcessTestRunnerFactory();
            services.Add(_factory);
            services.ServiceManager.StartServices();
        }

        [Test]
        public void ServiceIsStarted()
        {
            Assert.That(_factory.Status, Is.EqualTo(ServiceStatus.Started), "Failed to start service");
        }

#if NETCOREAPP
        [TestCase("x.dll", typeof(LocalTestRunner))]
        [TestCase("x.dll y.dll", typeof(LocalTestRunner))]
        [TestCase("x.dll y.dll z.dll", typeof(LocalTestRunner))]
#else
        [TestCase("x.dll",  typeof(TestDomainRunner))]
        [TestCase("x.dll y.dll",  typeof(MultipleTestDomainRunner))]
        [TestCase("x.dll y.dll z.dll", typeof(MultipleTestDomainRunner))]
#endif
        public void CorrectRunnerIsUsed(string files, Type expectedType)
        {
            var package = new TestPackage(files.Split(new char[] { ' ' }));
            Assert.That(_factory.MakeTestRunner(package), Is.TypeOf(expectedType));
        }
    }
}
