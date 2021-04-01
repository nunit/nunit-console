// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

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
        private TestRunData _testRunData;

        private List<string> _testFiles = new List<string>();

        private TestPackage _package;
        private ServiceContext _services;
        private MasterTestRunner _runner;
        private List<XmlNode> _events;

        public MasterTestRunnerTests(TestRunData testRunData)
        {
            _testRunData = testRunData;

            string testDirectory = TestContext.CurrentContext.TestDirectory;

            foreach (string file in testRunData.CommandLine.Split(new char[] { ',' }))
                _testFiles.Add(Path.Combine(testDirectory, file));
        }

        static ResultData MockAssemblyData =
            new ResultData("mock-assembly.dll", "Runnable", MockAssembly.Tests, MockAssembly.PassedInAttribute, MockAssembly.Failed, MockAssembly.Skipped, MockAssembly.Inconclusive);

        static ResultData NoTestAssemblyData =
            new ResultData("notest-assembly.dll", "NotRunnable", 0, 0, 0, 0, 0);

        static ResultData Project1Data =
            new ResultData("project1.nunit", "Runnable", MockAssemblyData);

        static ResultData Project2Data =
            new ResultData("project2.nunit", "Runnable", MockAssemblyData, MockAssemblyData);

        static TestRunData[] FixtureData = new TestRunData[]
        {
            // NOTES: 
            // 1. These tests document current behavior. In some cases we may want to change that behavior.
            // 2. The .NET Standard builds don't seem to handle notest-assembly correctly, so those entries are commented out.
            // 3. The .NET Standard 1.6 build is not intended to handle projects.
#if NETCOREAPP2_1 || NETCOREAPP3_1
            new TestRunData( "mock-assembly.dll", MockAssemblyData ),
            new TestRunData( "mock-assembly.dll,mock-assembly.dll", MockAssemblyData, MockAssemblyData ),
            //new TestRunData( "notest-assembly.dll", NoTestAssemblyData ),
            //new TestRunData( "notest-assembly.dll,notest-assembly.dll, NoTestAssemblyData, NoTestAssemblyData ),
            //new TestRunData( "mock-assembly.dll,notest-assembly.dll", MockAssemblyData, NoTestAssemblyData ),
            new TestRunData( "project1.nunit", Project1Data ),
            new TestRunData( "project2.nunit", Project2Data ),
            new TestRunData( "project1.nunit,project2.nunit", Project1Data, Project2Data ),
            new TestRunData( "project1.nunit,mock-assembly.dll,project2.nunit", Project1Data, MockAssemblyData, Project2Data)
#else
            new TestRunData( "mock-assembly.dll", MockAssemblyData ),
            new TestRunData( "mock-assembly.dll,mock-assembly.dll", MockAssemblyData, MockAssemblyData ),
            new TestRunData( "notest-assembly.dll", NoTestAssemblyData ),
            new TestRunData( "notest-assembly.dll,notest-assembly.dll", NoTestAssemblyData, NoTestAssemblyData ),
            new TestRunData( "mock-assembly.dll,notest-assembly.dll", MockAssemblyData, NoTestAssemblyData ),
            new TestRunData( "project1.nunit", Project1Data ),
            new TestRunData( "project2.nunit", Project2Data ),
            new TestRunData( "project1.nunit,project2.nunit", Project1Data, Project2Data ),
            new TestRunData( "project1.nunit,mock-assembly.dll,project2.nunit", Project1Data, MockAssemblyData, Project2Data)
#endif
        };

        [SetUp]
        public void Initialize()
        {
            _package = new TestPackage(_testFiles);
            _package.AddSetting(EnginePackageSettings.ProcessModel, "InProcess");

            // Add all services needed
            _services = new ServiceContext();
            _services.Add(new ExtensionService());
            var projectService = new FakeProjectService();
            var mockPath = Path.Combine(TestContext.CurrentContext.TestDirectory, "mock-assembly.dll");
            var notestsPath = Path.Combine(TestContext.CurrentContext.TestDirectory, "empty-assembly.dll");
            projectService.Add("project1.nunit", mockPath);
            projectService.Add("project2.nunit", mockPath, mockPath);
            projectService.Add("project3.nunit", notestsPath);
            projectService.Add("project4.nunit", notestsPath, notestsPath);
            _services.Add(projectService);
#if NETFRAMEWORK
            _services.Add(new DomainManager());
            _services.Add(new RuntimeFrameworkService());
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
        public void Load()
        {
            var result = _runner.Load();

            Assert.That(result.Name, Is.EqualTo("test-run"));
            CheckResult(result, _testRunData);

            CheckThatIdsAreUnique(result);
        }

        [Test]
        public void Reload()
        {
            _runner.Load();
            var result = _runner.Reload();

            Assert.That(result.Name, Is.EqualTo("test-run"));
            CheckResult(result, _testRunData);

            CheckThatIdsAreUnique(result);
        }

        [Test]
        public void CountTestCases()
        {
            int count = _runner.CountTestCases(TestFilter.Empty);
            Assert.That(count, Is.EqualTo(_testRunData.Tests));
        }

        [Test]
        public void Explore()
        {
            var result = _runner.Explore(TestFilter.Empty);

            Assert.That(result.Name, Is.EqualTo("test-run"));
            CheckResult(result, _testRunData);

            CheckThatIdsAreUnique(result);
        }

        [Test]
        public void Run()
        {
            _runner.Load(); // Make sure it's pre-loaded so we get count in start-run
            var result = _runner.Run(this, TestFilter.Empty);

            Assert.That(result.Name, Is.EqualTo("test-run"));
            CheckTestRunResult(result, _testRunData);

            CheckThatIdsAreUnique(result);

            CheckTestRunEvents();
        }

        [Test]
        public void RunAsync()
        {
            _runner.Load(); // Make sure it's pre-loaded so we get count in start-run
            var testRun = _runner.RunAsync(this, TestFilter.Empty);

            testRun.Wait(-1);
            Assert.That(testRun.Result.Name, Is.EqualTo("test-run"));
            CheckTestRunResult(testRun.Result, _testRunData);

            CheckThatIdsAreUnique(testRun.Result);

            CheckTestRunEvents();
        }

        private void CheckResult(XmlNode result, ResultData expected)
        {
            if (expected.Name != null)
                Assert.That(result.GetAttribute("name"), Is.EqualTo(expected.Name));
            Assert.That(result.GetAttribute("testcasecount", -1), Is.EqualTo(expected.Tests));
            Assert.That(result.GetAttribute("runstate"), Is.EqualTo(expected.RunState));

            if (expected.ContainedResults.Length > 0)
            {
                var suites = result.SelectNodes("test-suite");
                Assert.That(suites.Count, Is.EqualTo(expected.ContainedResults.Length));

                int i = 0;
                foreach (XmlNode suite in suites)
                {
                    var data = expected.ContainedResults[i++];
                    CheckResult(suite, data);
                }
            }
        }

        private void CheckTestRunResult(XmlNode result, ResultData expected)
        {
            if (expected.Name != null)
                Assert.That(result.GetAttribute("name"), Is.EqualTo(expected.Name));
            Assert.That(result.GetAttribute("testcasecount", -1), Is.EqualTo(expected.Tests));
            Assert.That(result.GetAttribute("result"), Is.EqualTo("Failed"));
            Assert.That(result.GetAttribute("passed", -1), Is.EqualTo(expected.Passed));
            Assert.That(result.GetAttribute("failed", -1), Is.EqualTo(expected.Failed));
            Assert.That(result.GetAttribute("skipped", -1), Is.EqualTo(expected.Skipped));
            Assert.That(result.GetAttribute("inconclusive", -1), Is.EqualTo(expected.Inconclusive));

            if (expected.ContainedResults.Length > 0)
            {
                var suites = result.SelectNodes("test-suite");
                Assert.That(suites.Count, Is.EqualTo(expected.ContainedResults.Length));

                int i = 0;
                foreach (XmlNode suite in suites)
                {
                    var data = expected.ContainedResults[i++];
                    CheckTestRunResult(suite, data);
                }
            }
        }

        private void CheckThatIdsAreUnique(XmlNode test)
        {
            CheckTestIdsAreUnique(test, new HashSet<string>(), new HashSet<string>());
        }

        /// Tests both that individual test IDs are unique, 
        private void CheckTestIdsAreUnique(XmlNode test, HashSet<string> testIDs, HashSet<string> testAssemblyPrefixes)
        {
            foreach (XmlNode child in test.SelectNodes("test-suite"))
                CheckTestIdsAreUnique(child, testIDs, testAssemblyPrefixes);

            var id = test.GetAttribute("id");
            Assert.That(testIDs, Does.Not.Contain(id));
            testIDs.Add(id);

            var testType = test.GetAttribute("type");
            if (testType != null && testType.Equals("assembly", StringComparison.OrdinalIgnoreCase))
            {
                CheckTestAssemblyPrefixIsUnique(test, testAssemblyPrefixes);
            }
        }

        private static void CheckTestAssemblyPrefixIsUnique(XmlNode test, HashSet<string> testAssemblyPrefixes)
        {
            var prefix = test.GetAttribute("id").Split('-')[0];
            Assert.That(testAssemblyPrefixes, Does.Not.Contain(prefix));
            testAssemblyPrefixes.Add(prefix);
        }

        private void CheckTestRunEvents()
        {
            var startRun = _events[0];
            Assert.That(startRun.Name, Is.EqualTo("start-run"), "First event should be start-run");
            Assert.That(startRun.GetAttribute("engine-version"), Is.Not.Null, "Incorrect engine-version in start-run event");
            Assert.That(startRun.GetAttribute("clr-version"), Is.Not.Null, "Incorrect clr-version in start-run event");
            Assert.That(startRun.GetAttribute("start-time", DateTime.Now.AddDays(-2)), Is.GreaterThan(DateTime.Now.AddDays(-1)), "Incorrect start-time in start-run event");
            Assert.That(startRun.GetAttribute("count", -1), Is.EqualTo(_testRunData.Tests), "Incorrect count in start-run event");
            Assert.That(startRun.FirstChild.Name, Is.EqualTo("command-line"), "First child of start-run should be command-line");
            Assert.That(_events[_events.Count - 1].Name, Is.EqualTo("test-run"), "Last event should be test-run");

            Assert.That(_events.Count(x => x.Name == "start-run"), Is.EqualTo(1), "More than one start-run event");
            Assert.That(_events.Count(x => x.Name == "test-run"), Is.EqualTo(1), "More than one test-run event");

            Assert.That(_events.Count(x => x.Name == "test-suite" && x.GetAttribute("type") == "Assembly"), Is.EqualTo(_testRunData.Assemblies), "Incorrect number of test-suite type='Assembly' events");
            Assert.That(_events.Count(x => x.Name == "test-case"), Is.EqualTo(_testRunData.Tests), "Incorrect number of test-case events");
        }

        void ITestEventListener.OnTestEvent(string report)
        {
            _events.Add(XmlHelper.CreateXmlNode(report));
        }

        public class TestRunData : ResultData
        {
            private const string Q = "\"";

            public string CommandLine;
            public ResultData[] ResultData;

            public TestRunData(string commandLine, params ResultData[] containedResults)
                : base(containedResults)
            {
                CommandLine = commandLine;
            }

            public override string ToString()
            {
                return Q + CommandLine + Q;
            }
        }

        public class ResultData
        {
            public string Name;
            public string RunState;
            public ResultData[] ContainedResults = new ResultData[0];

            public int Assemblies = 0;
            public int Tests = 0;
            public int Passed = 0;
            public int Failed = 0;
            public int Skipped = 0;
            public int Inconclusive = 0;

            public ResultData(string name, string runstate, int tests, int passing, int failed, int skipped, int inconclusive)
            {
                Name = name;
                RunState = runstate;
                Assemblies = 1;
                Tests = tests;
                Passed = passing;
                Failed = failed;
                Skipped = skipped;
                Inconclusive = inconclusive;
            }

            // Constructor used for fixture data - top level only
            public ResultData(params ResultData[] containedResults)
                : this(null, "Runnable", containedResults)
            {
            }

            public ResultData(string name, string runstate, params ResultData[] containedResults)
            {
                Name = name;
                RunState = runstate;
                ContainedResults = containedResults;

                foreach (var result in containedResults)
                {
                    Assemblies += result.Assemblies;
                    Tests += result.Tests;
                    Passed += result.Passed;
                    Failed += result.Failed;
                    Skipped += result.Skipped;
                    Inconclusive += result.Inconclusive;
                }
            }
        }
    }
}
