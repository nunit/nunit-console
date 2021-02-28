// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using System.Collections.Generic;
using System.Linq;
using NUnit.Engine.Runners;
using NUnit.Engine.Services;
using NUnit.Engine.Services.Tests.Fakes;
using NUnit.Engine.Tests.Services.TestRunnerFactoryTests.TestCases;
using NUnit.Framework;

namespace NUnit.Engine.Tests.Services.TestRunnerFactoryTests
{
    /// <summary>
    /// Tests of ITestRunner tree structure for different combinations
    /// of TestPackage and PackageSettings. Tests are currently written
    /// to protect existing behaviour, rather than define desired behaviour.
    /// </summary>
    public class RunnerSelectionTests
    {
        private DefaultTestRunnerFactory _factory;
        private ServiceContext _services;

        [OneTimeSetUp]
        public void SetUp()
        {
            _services = new ServiceContext();
            _services.Add(new ExtensionService());
            var projectService = new FakeProjectService();
            ((IService)projectService).StartService();
            projectService.Add(TestPackageFactory.FakeProject, "a.dll", "b.dll");
            _services.Add(projectService);
            Assert.That(((IService)projectService).Status, Is.EqualTo(ServiceStatus.Started));
            _factory = new DefaultTestRunnerFactory();
            _services.Add(_factory);
            _factory.StartService();
            Assert.That(_factory.Status, Is.EqualTo(ServiceStatus.Started));

            var fakeRuntimeService = new FakeRuntimeService();
            ((IService)fakeRuntimeService).StartService();
            _services.Add(fakeRuntimeService);
            Assert.That(((IService)fakeRuntimeService).Status, Is.EqualTo(ServiceStatus.Started));
        }

        [TestCaseSource(nameof(TestCases))]
        public void RunnerSelectionTest(TestPackage package, RunnerResult expected)
        {
            var masterRunner = new MasterTestRunner(_services, package);
            var runner = masterRunner.GetEngineRunner();
            var result = GetRunnerResult(runner);
            Assert.That(result, Is.EqualTo(expected).Using(RunnerResultComparer.Instance));
        }

        private static RunnerResult GetRunnerResult(ITestEngineRunner runner)
        {
            var result = new RunnerResult(runner.GetType());

            if (runner is AggregatingTestRunner aggRunner)
                result.SubRunners = aggRunner.Runners.Select(GetRunnerResult).ToList();

            return result;
        }

#if NETCOREAPP
        private static IEnumerable<TestCaseData> TestCases => NetStandardTestCases.TestCases;
#else
        private static IEnumerable<TestCaseData> TestCases => Net20AssemblyTestCases.TestCases
            .Concat(Net20ProjectTestCases.TestCases)
            .Concat(Net20MixedProjectAndAssemblyTestCases.TestCases);
#endif
    }
}

