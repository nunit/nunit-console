// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using System;
using NUnit.Framework;

namespace NUnit.Engine.Api.Tests
{
    public class ServiceLocatorTests
    {
        private ITestEngine _testEngine;

        [OneTimeSetUp]
        public void CreateEngine()
        {
            _testEngine = new TestEngine();
            _testEngine.InternalTraceLevel = InternalTraceLevel.Off;
        }

        [TestCase(typeof(ITestFilterService))]
        [TestCase(typeof(IExtensionService))]
        [TestCase(typeof(Services.ProjectService))]
#if NETFRAMEWORK
        [TestCase(typeof(Services.DomainManager))]
        [TestCase(typeof(IRuntimeFrameworkService))]
        [TestCase(typeof(ITestAgency))]
#endif
        [TestCase(typeof(IDriverService))]
        [TestCase(typeof(IResultService))]
        [TestCase(typeof(ITestRunnerFactory))]
        public void CanAccessService(Type serviceType)
        {
            IService service = _testEngine.Services.GetService(serviceType) as IService;
            Assert.NotNull(service, "GetService(Type) returned null");
            Assert.That(service, Is.InstanceOf(serviceType));
            Assert.That(service.Status, Is.EqualTo(ServiceStatus.Started));
        }
    }
}
