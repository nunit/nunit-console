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

using System.Collections.Generic;
using System.Linq;
using System.Xml;
using NUnit.Framework;
using NUnit.Tests.Assemblies;
using System.Reflection;
using System.IO;
using NUnit.Tests;

namespace NUnit.Engine.Runners.Tests
{
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

    [TestFixtureSource("FixtureData")]
    public class MasterTestRunnerTests : ITestEventListener
    {
        private List<AssemblyData> _data = new List<AssemblyData>();
        private List<string> _assemblies = new List<string>();

        private int _numAssemblies = 0;
        private int _totalTests = 0;
        private int _totalPassed = 0;
        private int _totalFailed = 0;
        private int _totalSkipped = 0;
        private int _totalInconclusive = 0;

        private TestPackage _package;
        private ServiceContext _services;
        private MasterTestRunner _runner;
        private List<XmlNode> _events;

        // Note: Unable to get a constructor taking an AssemblyData[]
        // to work with TestFixtureSource!
        public MasterTestRunnerTests(AssemblyData data)
        {
            System.Console.WriteLine("Constructor 1");
            Initialize(new AssemblyData[] { data });
        }

        public MasterTestRunnerTests(AssemblyData data1, AssemblyData data2)
        {
            System.Console.WriteLine("Constructor 2");
            Initialize(new AssemblyData[] { data1, data2 });
        }

        private void Initialize(AssemblyData[] assemblyData)
        {
            string testDirectory = TestContext.CurrentContext.TestDirectory;

            foreach (var data in assemblyData)
            {
                _data.Add(data);
                _assemblies.Add(Path.Combine(testDirectory, data.Name));
                _numAssemblies++;
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
            new AssemblyData("notests-assembly.dll", "NotRunnable", 0, 0, 0, 0, 0);

        static TestFixtureData[] FixtureData = new TestFixtureData[]
        {
            new TestFixtureData( MockAssemblyData ),
            new TestFixtureData( MockAssemblyData, MockAssemblyData ),
            // NOTE: The empty tests are excluded for .NET Standard because of 
            // errors and different behavior which either needs to be accounted 
            // for in the tests or corrected.
#if !NETSTANDARD1_6 && !NETSTANDARD2_0
            new TestFixtureData( NoTestsAssemblyData ),
            new TestFixtureData( NoTestsAssemblyData, NoTestsAssemblyData ),
            new TestFixtureData( MockAssemblyData, NoTestsAssemblyData )
#endif
        };

        [SetUp]
        public void Initialize()
        {
            _package = new TestPackage(_assemblies);

            // Add all services needed
            _services = new ServiceContext();
#if !NETCOREAPP1_1
            _services.Add(new Services.ExtensionService());
            _services.Add(new Services.ProjectService());
#if !NETCOREAPP2_0
            _services.Add(new Services.DomainManager());
            _services.Add(new Services.RuntimeFrameworkService());
#endif
#endif
            _services.Add(new Services.DriverService());
            _services.Add(new Services.InProcessTestRunnerFactory());
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
#if !NETCOREAPP1_1 && !NETCOREAPP2_0
            if (_numAssemblies == 1)
                Assert.That(runner, Is.TypeOf<TestDomainRunner>());
            else
                Assert.That(runner, Is.TypeOf<MultipleTestDomainRunner>());
#else
            Assert.That(runner, Is.TypeOf<LocalTestRunner>());
#endif
        }

        [Test]
        public void Load()
        {
            var result = _runner.Load();

            Assert.That(result.Name, Is.EqualTo("test-run"));
            Assert.That(result.GetAttribute("testcasecount", 0), Is.EqualTo(_totalTests));

            var suites = result.SelectNodes("test-suite");
            Assert.That(suites.Count, Is.EqualTo(_numAssemblies));

            int i = 0;
            foreach (XmlNode child in suites)
            {
                var data = _data[i++];
                Assert.That(child.GetAttribute("testcasecount", 0), Is.EqualTo(data.Tests));
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
            Assert.That(result.GetAttribute("testcasecount", 0), Is.EqualTo(_totalTests));

            var suites = result.SelectNodes("test-suite");
            Assert.That(suites.Count, Is.EqualTo(_numAssemblies));

            int i = 0;
            foreach (XmlNode child in result.SelectNodes("test-suite"))
            {
                var data = _data[i++];
                Assert.That(child.GetAttribute("testcasecount", 0), Is.EqualTo(data.Tests));
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
            Assert.That(result.GetAttribute("testcasecount", 0), Is.EqualTo(_totalTests));

            var suites = result.SelectNodes("test-suite");
            Assert.That(suites.Count, Is.EqualTo(_numAssemblies));

            int i = 0;
            foreach (XmlNode suite in suites)
            {
                var data = _data[i++];
                Assert.That(suite.GetAttribute("testcasecount", 0), Is.EqualTo(data.Tests));
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
            Assert.That(result.GetAttribute("testcasecount", 0), Is.EqualTo(_totalTests));
            Assert.That(result.GetAttribute("result"), Is.EqualTo("Failed"));
            Assert.That(result.GetAttribute("passed", 0), Is.EqualTo(_totalPassed));
            Assert.That(result.GetAttribute("failed", 0), Is.EqualTo(_totalFailed));
            Assert.That(result.GetAttribute("skipped", 0), Is.EqualTo(_totalSkipped));
            Assert.That(result.GetAttribute("inconclusive", 0), Is.EqualTo(_totalInconclusive));

            var suites = result.SelectNodes("test-suite");
            Assert.That(suites.Count, Is.EqualTo(_numAssemblies));

            int i = 0;
            foreach (XmlNode suite in suites)
            {
                var data = _data[i++];
                Assert.That(suite.GetAttribute("testcasecount", 0), Is.EqualTo(data.Tests));
                Assert.That(suite.GetAttribute("result"), Is.EqualTo("Failed"));
                Assert.That(suite.GetAttribute("passed", 0), Is.EqualTo(data.Passed));
                Assert.That(suite.GetAttribute("failed", 0), Is.EqualTo(data.Failed));
                Assert.That(suite.GetAttribute("skipped", 0), Is.EqualTo(data.Skipped));
                Assert.That(suite.GetAttribute("inconclusive", 0), Is.EqualTo(data.Inconclusive));
            }

            AssertThatIdsAreUnique(result);

            Assert.That(_events[0].Name, Is.EqualTo("start-run"));
            Assert.That(_events[0].GetAttribute("count", -1), Is.EqualTo(_totalTests), "Start-run count value");
            //Assert.That(_events[1].Name, Is.EqualTo("start-suite"));
            //Assert.That(_events[_events.Count - 2].Name, Is.EqualTo("test-suite"));
            Assert.That(_events[_events.Count - 1].Name, Is.EqualTo("test-run"));
            Assert.That(_events.Count(x => x.Name == "test-case"), Is.EqualTo(_totalTests));
        }

        private void AssertThatIdsAreUnique(XmlNode test)
        {
            AssertThatIdsAreUnique(test, new Dictionary<string, bool>());
        }

        private void AssertThatIdsAreUnique(XmlNode test, Dictionary<string, bool> dict)
        {
            Assert.That(dict, Does.Not.ContainKey(test.GetAttribute("id")));

            foreach (XmlNode child in test.SelectNodes("test-suite"))
                AssertThatIdsAreUnique(child, dict);
        }

        void ITestEventListener.OnTestEvent(string report)
        {
            _events.Add(XmlHelper.CreateXmlNode(report));
        }

        #endregion
    }
}
