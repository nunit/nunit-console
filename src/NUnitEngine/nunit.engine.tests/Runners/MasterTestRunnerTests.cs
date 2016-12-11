// ***********************************************************************
// Copyright (c) 2016 Charlie Poole
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

namespace NUnit.Engine.Runners.Tests
{
    public class MasterTestRunnerTests : ITestEventListener
    {
        private TestPackage _package;
        private ServiceContext _services;
        private MasterTestRunner _runner;
        private List<XmlNode> _events;

        [SetUp]
        public void Initialize()
        {
            // Add all services needed
            _services = new ServiceContext();
            _services.Add(new Services.DomainManager());
            _services.Add(new Services.ExtensionService());
            _services.Add(new Services.DriverService());
            _services.Add(new Services.ProjectService());
            _services.Add(new Services.DefaultTestRunnerFactory());
            _services.Add(new Services.RuntimeFrameworkService());
            _services.Add(new Services.TestAgency("ProcessRunnerTests", 0));
            _services.ServiceManager.StartServices();

            _package = new TestPackage("mock-assembly.dll");

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

            Assert.That(result.Name, Is.EqualTo("test-suite"));
            Assert.That(result.GetAttribute("testcasecount", 0), Is.EqualTo(MockAssembly.Tests));
            Assert.That(result.GetAttribute("runstate"), Is.EqualTo("Runnable"));
        }

        [Test]
        public void CountTestCases()
        {
            int count = _runner.CountTestCases(TestFilter.Empty);
            Assert.That(count, Is.EqualTo(MockAssembly.Tests));
        }

        [Test]
        public void Explore()
        {
            var result = _runner.Explore(TestFilter.Empty);

            Assert.That(result.Name, Is.EqualTo("test-run"));
            Assert.That(result.GetAttribute("testcasecount", 0), Is.EqualTo(MockAssembly.Tests));

            var suite = result.SelectSingleNode("test-suite");
            Assert.NotNull(suite, "No suite found");
            Assert.That(suite.GetAttribute("testcasecount", 0), Is.EqualTo(MockAssembly.Tests));
            Assert.That(suite.GetAttribute("runstate"), Is.EqualTo("Runnable"));
        }

        [Test]
        public void Run()
        {
            _runner.Load(); // Make sure it's pre-loaded so we get count in start-run
            var result = _runner.Run(this, TestFilter.Empty);
            CheckTestRunResult(result);
        }

        [Test]
        public void RunAsync()
        {
            _runner.Load(); // Make sure it's pre-loaded so we get count in start-run
            var testRun = _runner.RunAsync(this, TestFilter.Empty);
            testRun.Wait(-1);
            CheckTestRunResult(testRun.Result);
        }

        private void CheckTestRunResult(XmlNode result)
        {
            Assert.That(result.Name, Is.EqualTo("test-run"));
            Assert.That(result.GetAttribute("testcasecount", 0), Is.EqualTo(MockAssembly.Tests));
            Assert.That(result.GetAttribute("result"), Is.EqualTo("Failed"));
            Assert.That(result.GetAttribute("passed", 0), Is.EqualTo(MockAssembly.Success));
            Assert.That(result.GetAttribute("failed", 0), Is.EqualTo(MockAssembly.ErrorsAndFailures));
            Assert.That(result.GetAttribute("skipped", 0), Is.EqualTo(MockAssembly.Skipped));
            Assert.That(result.GetAttribute("inconclusive", 0), Is.EqualTo(MockAssembly.Inconclusive));

            var suite = result.SelectSingleNode("test-suite");
            Assert.NotNull("No suite found");
            Assert.That(suite.GetAttribute("testcasecount", 0), Is.EqualTo(MockAssembly.Tests));
            Assert.That(suite.GetAttribute("result"), Is.EqualTo("Failed"));
            Assert.That(suite.GetAttribute("passed", 0), Is.EqualTo(MockAssembly.Success));
            Assert.That(suite.GetAttribute("failed", 0), Is.EqualTo(MockAssembly.ErrorsAndFailures));
            Assert.That(suite.GetAttribute("skipped", 0), Is.EqualTo(MockAssembly.Skipped));
            Assert.That(suite.GetAttribute("inconclusive", 0), Is.EqualTo(MockAssembly.Inconclusive));

            Assert.That(_events[0].Name, Is.EqualTo("start-run"));
            Assert.That(_events[0].GetAttribute("count", -1), Is.EqualTo(MockAssembly.Tests), "Start-run count value");
            Assert.That(_events[1].Name, Is.EqualTo("start-suite"));
            Assert.That(_events[_events.Count - 2].Name, Is.EqualTo("test-suite"));
            Assert.That(_events[_events.Count - 1].Name, Is.EqualTo("test-run"));
            Assert.That(_events.Count(x => x.Name == "test-case"), Is.EqualTo(MockAssembly.Tests));
        }

        void ITestEventListener.OnTestEvent(string report)
        {
            _events.Add(XmlHelper.CreateXmlNode(report));
        }
    }
}
