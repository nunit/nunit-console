// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using System;
using NUnit.Framework;

namespace NUnit.Engine.Api
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

        [OneTimeTearDown]
        public void TearDown()
        {
            _testEngine.Dispose();
        }

        [TestCase(TypeArgs = new[] { typeof(IResultService) })]
        [TestCase(TypeArgs = new[] { typeof(ITestRunnerFactory) })]
        public void GetService<T>()
            where T : class
        {
            T service = _testEngine.Services.GetService<T>();
            Assert.That(service, Is.Not.Null, "GetService returned null");
            Assert.That(((IService)service).Status, Is.EqualTo(ServiceStatus.Started));
        }

        [TestCase(TypeArgs = new[] { typeof(IResultService) })]
        [TestCase(TypeArgs = new[] { typeof(ITestRunnerFactory) })]
        public void TryGetService<T>()
            where T : class
        {
            T? service;
            Assert.That(_testEngine.Services.TryGetService<T>(out service), Is.True);
            Assert.That(service, Is.Not.Null);
        }

        [Test]
        public void GetService_ThrowsWhenServiceIsNotFound()
        {
            var ex = Assert.Throws(typeof(NUnitEngineException), () => _testEngine.Services.GetService<InvalidService>());
            Assert.That(ex.Message, Contains.Substring("Unable to acquire InvalidService"));
        }

        [Test]
        public void TryGetService_FalseWhenServiceIsNotFound()
        {
            InvalidService? service;
            Assert.That(_testEngine.Services.TryGetService<InvalidService>(out service), Is.False);
            Assert.That(service, Is.Null);
        }

        internal class InvalidService
        {
        }
    }
}
