// ***********************************************************************
// Copyright (c) 2018 Charlie Poole, Rob Prouse
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
// ***********************************************************************

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
#if !NETCOREAPP1_1
            _services.Add(new ExtensionService());
            var projectService = new FakeProjectService();
            ((IService)projectService).StartService();
            projectService.Add(TestPackageFactory.FakeProject, "a.dll", "b.dll");
            _services.Add(projectService);
            Assert.That(((IService)projectService).Status, Is.EqualTo(ServiceStatus.Started));
#endif
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
            var result = new RunnerResult { TestRunner = runner.GetType() };

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
