// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using NUnit.Engine.Internal;
using NUnit.Framework;
using NUnit.Tests;
using NUnit.Tests.Assemblies;

namespace NUnit.Engine.Runners.Tests
{
    // Temporarily commenting out Process tests due to
    // intermittent errors, probably due to the test
    // fixture rather than the engine.
    [TestFixture(typeof(LocalTestRunner))]
#if NETFRAMEWORK
    [TestFixture(typeof(TestDomainRunner))]
    //[TestFixture(typeof(ProcessRunner))]
    [TestFixture(typeof(MultipleTestDomainRunner), 1)]
    [TestFixture(typeof(MultipleTestDomainRunner), 3)]
#endif
    //[TestFixture(typeof(MultipleTestProcessRunner), 1)]
    //[TestFixture(typeof(MultipleTestProcessRunner), 3)]
    //[Platform(Exclude = "Mono", Reason = "Currently causing long delays or hangs under Mono")]
    public class TestEngineRunnerTests<TRunner> where TRunner : AbstractTestRunner
    {
        protected TestPackage _package;
        protected ServiceContext _services;
        protected TRunner _runner;

        // Number of copies of mock-assembly to use in package
        protected int _numAssemblies;

        public TestEngineRunnerTests() : this(1) { }

        public TestEngineRunnerTests(int numAssemblies)
        {
            _numAssemblies = numAssemblies;
        }

        [SetUp]
        public void Initialize()
        {
            // Add all services needed by any of our TestEngineRunners
            _services = new ServiceContext();
            _services.Add(new Services.ExtensionService());
            _services.Add(new Services.ProjectService());
#if NETFRAMEWORK
            _services.Add(new Services.DomainManager());
            _services.Add(new Services.RuntimeFrameworkService());
            _services.Add(new Services.TestAgency("ProcessRunnerTests", 0));
#endif
            _services.Add(new Services.DriverService());
            _services.Add(new Services.DefaultTestRunnerFactory());
            _services.ServiceManager.StartServices();

            var mockAssemblyPath = Path.Combine(TestContext.CurrentContext.TestDirectory, "mock-assembly.dll");

            var assemblies = new List<string>();
            for (int i = 0; i < _numAssemblies; i++)
            {
                assemblies.Add(mockAssemblyPath);
            }

            _package = new TestPackage(assemblies);

            // HACK: Depends on the fact that all TestEngineRunners support this constructor
            _runner = (TRunner)Activator.CreateInstance(typeof(TRunner), _services, _package);
        }

        [TearDown]
        public void Cleanup()
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
            CheckBasicResult(result);
        }

        [Test]
        public void CountTestCases()
        {
            int count = _runner.CountTestCases(TestFilter.Empty);
            Assert.That(count, Is.EqualTo(MockAssembly.Tests * _numAssemblies));
            CheckPackageLoading();
        }

        [Test]
        public void Explore()
        {
            var result = _runner.Explore(TestFilter.Empty);
            CheckBasicResult(result);
            CheckPackageLoading();
        }

        [Test]
        public void Run()
        {
            var result = _runner.Run(null, TestFilter.Empty);
            CheckRunResult(result);
            CheckPackageLoading();
        }

        [Test]
        public void RunAsync()
        {
#if NETFRAMEWORK
            if (_runner is ProcessRunner || _runner is MultipleTestProcessRunner)
                Assert.Ignore("RunAsync is not working for ProcessRunner");
#endif

            var asyncResult = _runner.RunAsync(null, TestFilter.Empty);
            asyncResult.Wait(-1);
            Assert.That(asyncResult.IsComplete, "Async result is not complete");

            CheckRunResult(asyncResult.EngineResult);
            CheckPackageLoading();
        }

        private void CheckPackageLoading()
        {
            // Runners that derive from DirectTestRunner should automatically load the package
            // on calls to CountTestCases, Explore, Run and RunAsync. Other runners should
            // defer the loading to subpackages.
            if (_runner is DirectTestRunner)
                Assert.That(_runner.IsPackageLoaded, "Package was not loaded automatically");
            else
                Assert.That(_runner.IsPackageLoaded, Is.False, "Package should not be loaded automatically");
        }

        private void CheckBasicResult(TestEngineResult result)
        {
            foreach (var node in result.XmlNodes)
                CheckBasicResult(node);
        }

        private void CheckBasicResult(XmlNode node)
        {
            Assert.That(node.Name, Is.EqualTo("test-suite"));
            Assert.That(node.GetAttribute("testcasecount", 0), Is.EqualTo(MockAssembly.Tests));
            Assert.That(node.GetAttribute("runstate"), Is.EqualTo("Runnable"));
        }

        private void CheckRunResult(TestEngineResult result)
        {
            foreach (var node in result.XmlNodes)
                CheckRunResult(node);
        }

        private void CheckRunResult(XmlNode result)
        {
            CheckBasicResult(result);
            Assert.That(result.GetAttribute("passed", 0), Is.EqualTo(MockAssembly.PassedInAttribute));
            Assert.That(result.GetAttribute("failed", 0), Is.EqualTo(MockAssembly.Failed));
            Assert.That(result.GetAttribute("skipped", 0), Is.EqualTo(MockAssembly.Skipped));
            Assert.That(result.GetAttribute("inconclusive", 0), Is.EqualTo(MockAssembly.Inconclusive));
        }
    }
}
