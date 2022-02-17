// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using System;
using System.Collections.Generic;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using NUnit.Engine.Extensibility;
using NUnit.Framework;

namespace NUnit.Engine.Runners
{
    public class TestAgentRunnerExceptionTests
    {
        private IFrameworkDriver _driver;
        private FakeTestAgentRunner _runner;
        private readonly TestFilter _testFilter = new TestFilter(string.Empty);

        [SetUp]
        public void Initialize()
        {
            _driver = Substitute.For<IFrameworkDriver>();
            var driverService = Substitute.For<Drivers.IDriverService>();
            driverService.GetDriver(
                AppDomain.CurrentDomain,
                string.Empty,
                string.Empty, 
                false).ReturnsForAnyArgs(_driver);

            _runner = new FakeTestAgentRunner(new TestPackage("mock-assembly.dll").SubPackages[0])
            {
                DriverService = driverService
            };
        }

        [Test]
        public void Explore_Passes_Along_NUnitEngineException()
        {
            _driver.Explore(Arg.Any<string>()).Throws(new NUnitEngineException("Message"));
            var ex = Assert.Throws<NUnitEngineException>(() => _runner.Explore(new TestFilter(string.Empty)));
            Assert.That(ex.Message, Is.EqualTo("Message"));
        }

        [Test]
        public void Explore_Throws_NUnitEngineException()
        {
            _driver.Explore(Arg.Any<string>()).Throws(new ArgumentException("Message"));
            var ex = Assert.Throws<NUnitEngineException>(() => _runner.Explore(new TestFilter(string.Empty)));
            Assert.That(ex.InnerException is ArgumentException);
            Assert.That(ex.InnerException.Message, Is.EqualTo("Message"));
        }

        [Test]
        public void Load_Passes_Along_NUnitEngineException()
        {
            _driver.Load(Arg.Any<string>(), Arg.Any<Dictionary<string, object>>()).Throws(new NUnitEngineException("Message"));
            var ex = Assert.Throws<NUnitEngineException>(() => _runner.Load());
            Assert.That(ex.Message, Is.EqualTo("Message"));
        }

        [Test]
        public void Load_Throws_NUnitEngineException()
        {
            _driver.Load(Arg.Any<string>(), Arg.Any<Dictionary<string, object>>()).Throws(new ArgumentException("Message"));
            var ex = Assert.Throws<NUnitEngineException>(() => _runner.Load());
            Assert.That(ex.InnerException is ArgumentException);
            Assert.That(ex.InnerException.Message, Is.EqualTo("Message"));
        }

        [Test]
        public void CountTestCases_Passes_Along_NUnitEngineException()
        {
            _driver.CountTestCases(Arg.Any<string>()).Throws(new NUnitEngineException("Message"));
            var ex = Assert.Throws<NUnitEngineException>(() => _runner.CountTestCases(_testFilter));
            Assert.That(ex.Message, Is.EqualTo("Message"));
        }

        [Test]
        public void CountTestCases_Throws_NUnitEngineException()
        {
            _driver.CountTestCases(Arg.Any<string>()).Throws(new ArgumentException("Message"));
            var ex = Assert.Throws<NUnitEngineException>(() => _runner.CountTestCases(_testFilter));
            Assert.That(ex.InnerException is ArgumentException);
            Assert.That(ex.InnerException.Message, Is.EqualTo("Message"));
        }

        [Test]
        public void Run_Passes_Along_NUnitEngineException()
        {
            _driver.Run(Arg.Any<ITestEventListener>(), Arg.Any<string>()).Throws(new NUnitEngineException("Message"));
            var ex = Assert.Throws<NUnitEngineException>(() => _runner.Run(Substitute.For<ITestEventListener>(), _testFilter));
            Assert.That(ex.Message, Is.EqualTo("Message"));
        }

        [Test]
        public void Run_Throws_NUnitEngineException()
        {
            _driver.Run(Arg.Any<ITestEventListener>(), Arg.Any<string>()).Throws(new ArgumentException("Message"));
            var ex = Assert.Throws<NUnitEngineException>(() => _runner.Run(Substitute.For<ITestEventListener>(), _testFilter));
            Assert.That(ex.InnerException is ArgumentException);
            Assert.That(ex.InnerException.Message, Is.EqualTo("Message"));
        }

        [Test]
        public void StopRun_Passes_Along_NUnitEngineException()
        {
            _driver.When(x => x.StopRun(Arg.Any<bool>()))
                .Do(x => { throw new NUnitEngineException("Message"); });

            var ex = Assert.Throws<NUnitEngineException>(() => _runner.StopRun(true));
            Assert.That(ex.Message, Is.EqualTo("Message"));
        }

        [Test]
        public void StopRun_Throws_NUnitEngineException()
        {
            _driver.When(x => x.StopRun(Arg.Any<bool>()))
                .Do(x => { throw new ArgumentException("Message"); });

            var ex = Assert.Throws<NUnitEngineException>(() => _runner.StopRun(true));
            Assert.That(ex.InnerException is ArgumentException);
            Assert.That(ex.InnerException.Message, Is.EqualTo("Message"));
        }
    }
}
