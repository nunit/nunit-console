// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Engine.Runners;
using NUnit.Framework;
using NUnitLite;

namespace NUnit.Engine.Services.TestRunnerFactoryTests
{
    /// <summary>
    /// Tests of ITestRunner tree structure for different combinations
    /// of TestPackage and PackageSettings. Tests are currently written
    /// to protect existing behaviour, rather than define desired behaviour.
    /// </summary>
    public class RunnerSelectionTests
    {
        private TestRunnerFactory _factory;
        private ServiceContext _services;

        [OneTimeSetUp]
        public void SetUp()
        {
            _services = new ServiceContext();
            _services.Add(new ExtensionService());
            var projectService = new FakeProjectService();
            ((IService)projectService).StartService();
            projectService.Add("xy.nunit", "x.dll", "y.dll");
            projectService.Add("z.nunit", "z.dll");
            _services.Add(projectService);
            Assert.That(((IService)projectService).Status, Is.EqualTo(ServiceStatus.Started));
            _factory = new TestRunnerFactory();
            _services.Add(_factory);
            _factory.StartService();
            Assert.That(_factory.Status, Is.EqualTo(ServiceStatus.Started));

            var fakeRuntimeService = new FakeRuntimeService();
            ((IService)fakeRuntimeService).StartService();
            _services.Add(fakeRuntimeService);
            Assert.That(((IService)fakeRuntimeService).Status, Is.EqualTo(ServiceStatus.Started));

#if NETFRAMEWORK
            var testAgency = new TestAgency();
            _services.Add(testAgency);
            ((IService)testAgency).StartService();
            Assert.That(testAgency.Status, Is.EqualTo(ServiceStatus.Started));
#endif
        }

        [OneTimeTearDown]
        public void TearDown()
        {
            _factory.Dispose();
        }

        [TestCaseSource(typeof(TestRunnerFactoryData), nameof(TestRunnerFactoryData.TestCases))]
        public void RunnerSelectionTest(TestPackage package, RunnerResult expected)
        {
            var masterRunner = new MasterTestRunner(_services, package);
            var runner = masterRunner.GetEngineRunner();
            var result = GetRunnerResult(runner);
            Assert.That(result, Is.EqualTo(expected).Using(RunnerResultComparer.Instance));
        }

        private static RunnerResult GetRunnerResult(ITestEngineRunner runner)
        {
            var runnerType = runner.GetType();

            if (runner is AggregatingTestRunner aggRunner)
                return new RunnerResult(runnerType, aggRunner.Runners.Select(GetRunnerResult).ToArray());

            return new RunnerResult(runnerType);
        }
    }
}
