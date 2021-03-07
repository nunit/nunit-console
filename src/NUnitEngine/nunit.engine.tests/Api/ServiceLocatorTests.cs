// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using System;
using System.Collections.Generic;
using System.Text;
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

        [TestCase(typeof(ISettings))]
        [TestCase(typeof(IDriverService))]
        public void CanAccessService(Type serviceType)
        {
            IService service = _testEngine.Services.GetService(serviceType) as IService;
            Assert.NotNull(service, "GetService(Type) returned null");
            Assert.That(service, Is.InstanceOf(serviceType));
            Assert.That(service.Status, Is.EqualTo(ServiceStatus.Started));
        }
    }
}
