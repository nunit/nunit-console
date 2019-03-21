// ***********************************************************************
// Copyright (c) 2016 Charlie Poole, Rob Prouse
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

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Xml;
using NUnit.Framework;
using NUnit.Tests.Assemblies;
using NUnit.Engine.Services;
using NUnit.Engine.Services.Tests.Fakes;

namespace NUnit.Engine.Runners.Tests
{
    [TestFixtureSource(nameof(FixtureData))]
    public class MasterTestRunnerTests : ITestEventListener
    {
        private List<AssemblyData> _data = new List<AssemblyData>();
        private List<string> _testFiles = new List<string>();
        private List<string> _assemblies = new List<string>();

        private int _numAssemblies = 0;
        private int _totalTests = 0;
        private int _totalPassed = 0;
        private int _totalFailed = 0;
        private int _totalSkipped = 0;
        private int _totalInconclusive = 0;

        private Type _expectedRunner;

        private TestPackage _package;
        private ServiceContext _services;
        private MasterTestRunner _runner;
        private List<XmlNode> _events;

        public MasterTestRunnerTests(TestRunData testRunData)
        {
            _expectedRunner = testRunData.ExpectedRunner;

            string testDirectory = TestContext.CurrentContext.TestDirectory;

            foreach (string file in testRunData.CommandLine.Split(new char[] { ',' }))
                _testFiles.Add(Path.Combine(testDirectory, file));

            foreach (var data in testRunData.AssemblyData)
            {
                _data.Add(data);
                _numAssemblies++;
                _assemblies.Add(Path.Combine(testDirectory, data.Name));
                _totalTests += data.Tests;
                _totalPassed += data.Passed;
                _totalFailed += data.Failed;
                _totalSkipped += data.Skipped;
                _totalInconclusive += data.Inconclusive;
            }
        }

        static AssemblyData MockAssemblyData =
            new AssemblyData("mock-assembly.dll", "Runnable", MockAssembly.Tests, MockAssembly.PassedInAttribute, MockAssembly.Failed, MockAssembly.Skipped, MockAssembly.Inconclusive);

        static AssemblyData NoTestsAssemblyData =
            new AssemblyData("empty-assembly.dll", "NotRunnable", 0, 0, 0, 0, 0);

        static AssemblyData Project1Data =
            new AssemblyData("project1.nunit", "Runnable", MockAssembly.Tests, MockAssembly.PassedInAttribute, MockAssembly.Failed, MockAssembly.Skipped, MockAssembly.Inconclusive);

        static AssemblyData Project2Data =
            new AssemblyData("project2.nunit", "Runnable", 2*MockAssembly.Tests, 2*MockAssembly.PassedInAttribute, 2*MockAssembly.Failed, 2*MockAssembly.Skipped, 2*MockAssembly.Inconclusive);

        static AssemblyData Project3Data =
            new AssemblyData("project3.nunit", "Runnable", 0, 0, 0, 0, 0);

        static AssemblyData Project4Data =
            new AssemblyData("project4.nunit", "Runnable", 0, 0, 0, 0, 0);

        static TestRunData[] FixtureData = new TestRunData[]
        {
            // NOTE: These tests document current behavior. In some cases
            // we may want to change that behavior.
#if NETCOREAPP1_1
            new TestRunData( "mock-assembly.dll", typeof(LocalTestRunner), MockAssemblyData ),
            new TestRunData( "mock-assembly.dll,mock-assembly.dll", typeof(AggregatingTestRunner), MockAssemblyData, MockAssemblyData ),
            new TestRunData( "empty-assembly.dll", typeof(LocalTestRunner), NoTestsAssemblyData ),
            new TestRunData( "empty-assembly.dll,empty-assembly.dll", typeof(AggregatingTestRunner), NoTestsAssemblyData, NoTestsAssemblyData ),
            new TestRunData( "mock-assembly.dll,empty-assembly.dll", typeof(AggregatingTestRunner), MockAssemblyData, NoTestsAssemblyData )
            // .NET Standard 1.6 doesn't support projects.
#elif NETCOREAPP2_0
            new TestRunData( "mock-assembly.dll", typeof(LocalTestRunner), MockAssemblyData ),
            new TestRunData( "mock-assembly.dll,mock-assembly.dll", typeof(AggregatingTestRunner), MockAssemblyData, MockAssemblyData ),
            new TestRunData( "empty-assembly.dll", typeof(LocalTestRunner), NoTestsAssemblyData ),
            new TestRunData( "empty-assembly.dll,empty-assembly.dll", typeof(AggregatingTestRunner), NoTestsAssemblyData, NoTestsAssemblyData ),
            new TestRunData( "mock-assembly.dll,empty-assembly.dll", typeof(AggregatingTestRunner), MockAssemblyData, NoTestsAssemblyData ),
            new TestRunData( "project1.nunit", typeof(AggregatingTestRunner), Project1Data ),
            new TestRunData( "project2.nunit", typeof(AggregatingTestRunner), Project2Data ),
            new TestRunData( "project3.nunit", typeof(AggregatingTestRunner), Project3Data ),
            new TestRunData( "project4.nunit", typeof(AggregatingTestRunner), Project4Data ),
            new TestRunData( "project1.nunit,empty-assembly.dll,project2.nunit", typeof(AggregatingTestRunner), Project1Data, NoTestsAssemblyData, Project2Data)
#else
            new TestRunData( "mock-assembly.dll", typeof(TestDomainRunner), MockAssemblyData ),
            new TestRunData( "mock-assembly.dll,mock-assembly.dll", typeof(MultipleTestDomainRunner), MockAssemblyData, MockAssemblyData ),
            new TestRunData( "empty-assembly.dll", typeof(TestDomainRunner), NoTestsAssemblyData ),
            new TestRunData( "empty-assembly.dll,empty-assembly.dll", typeof(MultipleTestDomainRunner), NoTestsAssemblyData, NoTestsAssemblyData ),
            new TestRunData( "mock-assembly.dll,empty-assembly.dll", typeof(MultipleTestDomainRunner), MockAssemblyData, NoTestsAssemblyData ),
            new TestRunData( "project1.nunit", typeof(AggregatingTestRunner), Project1Data ),
            new TestRunData( "project2.nunit", typeof(AggregatingTestRunner), Project2Data ),
            new TestRunData( "project3.nunit", typeof(AggregatingTestRunner), Project3Data ),
            new TestRunData( "project4.nunit", typeof(AggregatingTestRunner), Project4Data ),
            new TestRunData( "project1.nunit,empty-assembly.dll,project2.nunit", typeof(AggregatingTestRunner), Project1Data, NoTestsAssemblyData, Project2Data)
#endif
        };

        [SetUp]
        public void Initialize()
        {
            _package = new TestPackage(_testFiles);
            _package.AddSetting(EnginePackageSettings.ProcessModel, "InProcess");

            // Add all services needed
            _services = new ServiceContext();
#if !NETCOREAPP1_1
            _services.Add(new ExtensionService());
            var projectService = new FakeProjectService();
            var mockPath = Path.Combine(TestContext.CurrentContext.TestDirectory, "mock-assembly.dll");
            var notestsPath = Path.Combine(TestContext.CurrentContext.TestDirectory, "empty-assembly.dll");
            projectService.Add("project1.nunit", mockPath);
            projectService.Add("project2.nunit", mockPath, mockPath);
            projectService.Add("project3.nunit", notestsPath);
            projectService.Add("project4.nunit", notestsPath, notestsPath);
            _services.Add(projectService);
#if !NETCOREAPP2_0
            _services.Add(new DomainManager());
            _services.Add(new RuntimeFrameworkService());
#endif
#endif
            _services.Add(new DriverService());
            _services.Add(new DefaultTestRunnerFactory());
            _services.ServiceManager.StartServices();

            _runner = new MasterTestRunner(_services, _package);
            _events = new List<XmlNode>();
        }

        [TearDown]
        public void CleanUp()
        {
            if (_runner != null)
                _runner.Dispose();

            if (_services != null)
                _services.ServiceManager.Dispose();
        }

        [Test]
        public void CheckInternalRunner()
        {
            var prop = typeof(MasterTestRunner).GetField("_engineRunner", BindingFlags.NonPublic | BindingFlags.Instance);
            var runner = prop.GetValue(_runner);
            Assert.That(runner, Is.TypeOf(_expectedRunner));
        }

        [Test]
        public void Load()
        {
            var result = _runner.Load();

            Assert.That(result.Name, Is.EqualTo("test-run"));
            Assert.That(result.GetAttribute("testcasecount", -1), Is.EqualTo(_totalTests));

            var suites = result.SelectNodes("test-suite");
            Assert.That(suites.Count, Is.EqualTo(_numAssemblies));

            int i = 0;
            foreach (XmlNode child in suites)
            {
                var data = _data[i++];
                Assert.That(child.GetAttribute("name"), Is.EqualTo(data.Name));
                Assert.That(child.GetAttribute("testcasecount", -1), Is.EqualTo(data.Tests));
                Assert.That(child.GetAttribute("runstate"), Is.EqualTo(data.RunState));
            }

            AssertThatIdsAreUnique(result);
        }

        [Test]
        public void Reload()
        {
            _runner.Load();
            var result = _runner.Reload();

            Assert.That(result.Name, Is.EqualTo("test-run"));
            Assert.That(result.GetAttribute("testcasecount", -1), Is.EqualTo(_totalTests));

            var suites = result.SelectNodes("test-suite");
            Assert.That(suites.Count, Is.EqualTo(_numAssemblies));

            int i = 0;
            foreach (XmlNode child in result.SelectNodes("test-suite"))
            {
                var data = _data[i++];
                Assert.That(child.GetAttribute("name"), Is.EqualTo(data.Name));
                Assert.That(child.GetAttribute("testcasecount", -1), Is.EqualTo(data.Tests));
                Assert.That(child.GetAttribute("runstate"), Is.EqualTo(data.RunState));
            }

            AssertThatIdsAreUnique(result);
        }

        [Test]
        public void CountTestCases()
        {
            int count = _runner.CountTestCases(TestFilter.Empty);
            Assert.That(count, Is.EqualTo(_totalTests));
        }

        [Test]
        public void Explore()
        {
            var result = _runner.Explore(TestFilter.Empty);

            Assert.That(result.Name, Is.EqualTo("test-run"));
            Assert.That(result.GetAttribute("testcasecount", -1), Is.EqualTo(_totalTests));

            var suites = result.SelectNodes("test-suite");
            Assert.That(suites.Count, Is.EqualTo(_numAssemblies));

            int i = 0;
            foreach (XmlNode suite in suites)
            {
                var data = _data[i++];
                Assert.That(suite.GetAttribute("name"), Is.EqualTo(data.Name));
                Assert.That(suite.GetAttribute("testcasecount", -1), Is.EqualTo(data.Tests));
                Assert.That(suite.GetAttribute("runstate"), Is.EqualTo(data.RunState));
            }

            AssertThatIdsAreUnique(result);
        }

        [Test]
        public void Run()
        {
            _runner.Load(); // Make sure it's pre-loaded so we get count in start-run
            var result = _runner.Run(this, TestFilter.Empty);
            CheckTestRunResult(result);
        }

#if !NETCOREAPP1_1
        [Test]
        public void RunAsync()
        {
            _runner.Load(); // Make sure it's pre-loaded so we get count in start-run
            var testRun = _runner.RunAsync(this, TestFilter.Empty);
            testRun.Wait(-1);
            CheckTestRunResult(testRun.Result);
        }
#endif

#region Helper Methods

        private void CheckTestRunResult(XmlNode result)
        {
            Assert.That(result.Name, Is.EqualTo("test-run"));
            Assert.That(result.GetAttribute("testcasecount", -1), Is.EqualTo(_totalTests));
            Assert.That(result.GetAttribute("result"), Is.EqualTo("Failed"));
            Assert.That(result.GetAttribute("passed", -1), Is.EqualTo(_totalPassed));
            Assert.That(result.GetAttribute("failed", -1), Is.EqualTo(_totalFailed));
            Assert.That(result.GetAttribute("skipped", -1), Is.EqualTo(_totalSkipped));
            Assert.That(result.GetAttribute("inconclusive", -1), Is.EqualTo(_totalInconclusive));

            var suites = result.SelectNodes("test-suite");
            Assert.That(suites.Count, Is.EqualTo(_numAssemblies));

            int i = 0;
            foreach (XmlNode suite in suites)
            {
                var data = _data[i++];
                Assert.That(suite.GetAttribute("name"), Is.EqualTo(data.Name));
                Assert.That(suite.GetAttribute("testcasecount", -1), Is.EqualTo(data.Tests));
                Assert.That(suite.GetAttribute("result"), Is.EqualTo("Failed"));
                Assert.That(suite.GetAttribute("passed", 0), Is.EqualTo(data.Passed));
                Assert.That(suite.GetAttribute("failed", 0), Is.EqualTo(data.Failed));
                Assert.That(suite.GetAttribute("skipped", 0), Is.EqualTo(data.Skipped));
                Assert.That(suite.GetAttribute("inconclusive", 0), Is.EqualTo(data.Inconclusive));
            }

            AssertThatIdsAreUnique(result);

            Assert.That(_events[0].Name, Is.EqualTo("start-run"));
            Assert.That(_events[0].GetAttribute("count", -1), Is.EqualTo(_totalTests), "Start-run count value");
            Assert.That(_events[_events.Count - 1].Name, Is.EqualTo("test-run"));
            Assert.That(_events.Count(x => x.Name == "test-case"), Is.EqualTo(_totalTests));
        }

        private void AssertThatIdsAreUnique(XmlNode test)
        {
            AssertThatIdsAreUnique(test, new Dictionary<string, bool>());
        }

        private void AssertThatIdsAreUnique(XmlNode test, Dictionary<string, bool> dict)
        {
            foreach (XmlNode child in test.SelectNodes("test-suite"))
                AssertThatIdsAreUnique(child, dict);

            string id = test.GetAttribute("id");
            Assert.That(dict, Does.Not.ContainKey(id));

            dict.Add(id, true);
        }

        void ITestEventListener.OnTestEvent(string report)
        {
            _events.Add(XmlHelper.CreateXmlNode(report));
        }

        #endregion

        #region Nested Helper Classes

        public class TestRunData
        {
            public string CommandLine;
            public Type ExpectedRunner;
            public AssemblyData[] AssemblyData;

            public TestRunData(string commandLine, Type expectedRunner, params AssemblyData[] assemblyData)
            {
                CommandLine = commandLine;
                ExpectedRunner = expectedRunner;
                AssemblyData = assemblyData;
            }

            public override string ToString()
            {
                var sb = new StringBuilder(ExpectedRunner.Name);
                foreach (var assembly in AssemblyData)
                    sb.Append($", {assembly.Name}");
                return sb.ToString();
            }
        }

        public class AssemblyData
        {
            public string Name;
            public string RunState;
            public int Tests;
            public int Passed;
            public int Failed;
            public int Skipped;
            public int Inconclusive;

            public AssemblyData(string name, string runstate, int tests, int passing, int failed, int skipped, int inconclusive)
            {
                Name = name;
                RunState = runstate;
                Tests = tests;
                Passed = passing;
                Failed = failed;
                Skipped = skipped;
                Inconclusive = inconclusive;
            }
        }
        #endregion
    }
}
