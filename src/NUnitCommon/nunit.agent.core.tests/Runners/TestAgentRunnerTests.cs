// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using NUnit.Framework;
using NUnit.Framework.Internal;
using NUnit.TestData;
using NUnit.TestData.Assemblies;

namespace NUnit.Engine.Runners
{
    [TestFixture(typeof(LocalTestRunner))]
#if NETFRAMEWORK
    [TestFixture(typeof(TestDomainRunner))]
#endif
    public class TestAgentRunnerTests<TRunner> : ITestEventListener
        where TRunner : TestAgentRunner
    {
        protected TestPackage _package;
        protected TRunner _runner;

        [SetUp]
        public void Initialize()
        {
            var mockAssemblyPath = Path.Combine(TestContext.CurrentContext.TestDirectory, "mock-assembly.dll");

            _package = new TestPackage(mockAssemblyPath).SubPackages[0];

            _runner = (TRunner)Activator.CreateInstance(typeof(TRunner), _package)!;
        }

        [TearDown]
        public void Cleanup()
        {
            if (_runner is not null)
                _runner.Dispose();
        }

        [Test]
        public void Load()
        {
            var result = _runner.Load();
            CheckLoadResult(result);
        }

        [Test]
        public void CountTestCases()
        {
            int count = _runner.CountTestCases(TestFilter.Empty);
            Assert.That(_runner.IsPackageLoaded, "Package was not loaded automatically");
            Assert.That(count, Is.EqualTo(MockAssembly.Tests));
        }

        [Test]
        public void Explore()
        {
            var result = _runner.Explore(TestFilter.Empty);
            CheckLoadResult(result);
        }

        [Test]
        public void Run()
        {
            var result = _runner.Run(null, TestFilter.Empty);
            CheckRunResult(result);
        }

        [Test]
        public void RunAsync()
        {
            var asyncResult = _runner.RunAsync(this, TestFilter.Empty);
            asyncResult.Wait(-1);
            Assert.That(asyncResult.IsComplete, "Async result is not complete");

            CheckRunResult(asyncResult.EngineResult);
        }

        private void CheckLoadResult(TestEngineResult result)
        {
            Assert.That(_runner.IsPackageLoaded, "Package was not loaded automatically");
            Assert.That(result.IsSingle);
            var node = result.XmlNodes[0];
            Assert.That(node.Name, Is.EqualTo("test-suite"));
            Assert.That(node.GetAttribute("testcasecount", 0), Is.EqualTo(MockAssembly.Tests));
            Assert.That(node.GetAttribute("runstate"), Is.EqualTo("Runnable"));
        }

        private void CheckRunResult(TestEngineResult result)
        {
            CheckLoadResult(result);
            var node = result.XmlNodes[0];
            Assert.That(node.GetAttribute("passed", 0), Is.EqualTo(MockAssembly.Passed_Raw));
            Assert.That(node.GetAttribute("failed", 0), Is.EqualTo(MockAssembly.Failed_Raw));
            Assert.That(node.GetAttribute("skipped", 0), Is.EqualTo(MockAssembly.Skipped));
            Assert.That(node.GetAttribute("inconclusive", 0), Is.EqualTo(MockAssembly.Inconclusive));
        }

        public void OnTestEvent(string report)
        {
            // Do nothing
        }
    }
}
