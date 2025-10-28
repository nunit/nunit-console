// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

// Normally, these tests are run as unit tests. No process is actually launched
// but we verify that the proper calls are made. When making changes to the code,
// it may be convenient to actually use TestAgency and launch agent processes,
// running the tests. To do so, uncomment the following #define.
//#define TESTAGENCY_INTEGRATION

#if NETFRAMEWORK
using System;
using System.IO;
using NUnit.Engine.Services;
using NUnit.Framework;
using NSubstitute;
using System.Collections.Generic;
using NUnit.Framework.Constraints;

namespace NUnit.Engine.Runners
{
#if TESTAGENCY_INTEGRATION
    [TestFixture("net462")]
    [TestFixture("net35")]
    [TestFixture("net8.0")]
    [TestFixture("net7.0")]
    [TestFixture("net6.0")]
    [TestFixture("netcoreapp3.1")]
#endif
    public class ProcessRunnerTests : ITestEventListener
    {
        private TestPackage _package;
        private ProcessRunner _processRunner;
        private ITestEngineRunner? _remoteRunner;

        private string _runtimeDir = "net462";

#if TESTAGENCY_INTEGRATION
        public ProcessRunnerTests(string runtimeDir)
        {
            _runtimeDir = runtimeDir;
        }
#else
#endif

        [SetUp]
        public void Initialize()
        {
            var assemblyPath = Path.Combine(TestContext.CurrentContext.TestDirectory, $"../testdata/{_runtimeDir}/mock-assembly.dll");
            Assert.That(File.Exists(assemblyPath), Is.True);
            _package = new TestPackage(assemblyPath).SubPackages[0];
            _package.Settings.Add(
                SettingDefinitions.TargetFrameworkName.WithValue(
                    _runtimeDir.StartsWith("net4") || _runtimeDir.StartsWith("net3")
                        ? ".NETFramework,Version=v4.6.2"
                        : ".NETCoreApp,Version=v8.0.0"));

            var context = Substitute.For<IServiceLocator>();

#if TESTAGENCY_INTEGRATION
            // We actually run the test
            var agency = new TestAgency() { ServiceContext = context };
            context.GetService<TestAgency>().Returns(agency);
            agency.StartService();
#else
            // Create Substitutes for All Components
            _remoteRunner = Substitute.For<ITestEngineRunner>();
            _remoteRunner.Load().Returns(new TestEngineResult("<LOAD_RESULT />"));
            _remoteRunner.CountTestCases(TestFilter.Empty).Returns(42);
            _remoteRunner.Explore(TestFilter.Empty).Returns(new TestEngineResult("<EXPLORE_RESULT />"));
            _remoteRunner.Run(this, TestFilter.Empty).Returns(new TestEngineResult("<RUN_RESULT />"));
            var asyncResult = new AsyncTestEngineResult();
            asyncResult.SetResult(new TestEngineResult("<RUN_ASYNC_RESULT />"));
            _remoteRunner.RunAsync(this, TestFilter.Empty).Returns(asyncResult);

            var agent = Substitute.For<ITestAgent>();
            agent.CreateRunner(_package).ReturnsForAnyArgs(_remoteRunner);

            var agency = Substitute.For<TestAgency>();
            agency.GetAgent(_package).ReturnsForAnyArgs(agent);

            context.GetService<TestAgency>().Returns(agency);
#endif

            _processRunner = new ProcessRunner(context, _package);
        }

        [TearDown]
        public void Cleanup()
        {
            _remoteRunner?.Dispose();
            _processRunner?.Dispose();
        }

        [Test]
        public void Load()
        {
            TestEngineResult result = _processRunner.Load();
#if TESTAGENCY_INTEGRATION
            CheckBasicResult(result);
#else
            _remoteRunner?.Received().Load();
            Assert.That(result.Xml.Name, Is.EqualTo("LOAD_RESULT"));
#endif
        }

        [Test]
        public void CountTestCases()
        {
            int count = _processRunner.CountTestCases(TestFilter.Empty);
#if TESTAGENCY_INTEGRATION
            Assert.That(count, Is.EqualTo(MockAssembly.Tests));
#else
            _remoteRunner?.Received().CountTestCases(TestFilter.Empty);
            Assert.That(count, Is.EqualTo(42));
#endif
        }

        [Test]
        public void Explore()
        {
            var result = _processRunner.Explore(TestFilter.Empty);
#if TESTAGENCY_INTEGRATION
            CheckBasicResult(result);
#else
            _remoteRunner?.Received().Explore(TestFilter.Empty);
            Assert.That(result.Xml.Name, Is.EqualTo("EXPLORE_RESULT"));
#endif
        }

        [Test]
        public void Run()
        {
            var result = _processRunner.Run(this, TestFilter.Empty);
#if TESTAGENCY_INTEGRATION
            CheckRunResult(result);
#else
            _remoteRunner?.Received().Run(this, TestFilter.Empty);
            Assert.That(result.Xml.Name, Is.EqualTo("RUN_RESULT"));
#endif
        }

        [Test]
        public void RunAsync()
        {
            var asyncResult = _processRunner.RunAsync(this, TestFilter.Empty);
            Assert.That(asyncResult.Wait(-1), Is.True);
            Assert.That(asyncResult.IsComplete, Is.True);
#if TESTAGENCY_INTEGRATION
            CheckRunResult(asyncResult.EngineResult);
#else
            _remoteRunner?.Received().RunAsync(this, TestFilter.Empty);
            Assert.That(asyncResult.EngineResult.Xml.Name, Is.EqualTo("RUN_ASYNC_RESULT"));
#endif
        }

#if TESTAGENCY_INTEGRATION
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
            Assert.That(result.GetAttribute("passed", 0), Is.EqualTo(MockAssembly.Passed_Raw));
            Assert.That(result.GetAttribute("failed", 0), Is.EqualTo(MockAssembly.Failed_Raw));
            Assert.That(result.GetAttribute("skipped", 0), Is.EqualTo(MockAssembly.Skipped));
            Assert.That(result.GetAttribute("inconclusive", 0), Is.EqualTo(MockAssembly.Inconclusive));
        }
#else
        [Test]
        public void RequestStop()
        {
            _processRunner.ForceRemoteRunnerCreation();
            _processRunner.RequestStop();
            _remoteRunner?.Received().RequestStop();
        }

        [Test]
        public void ForcedStop()
        {
            _processRunner.ForceRemoteRunnerCreation();
            _processRunner.ForcedStop();
            _remoteRunner?.Received().ForcedStop();
        }
#endif

        public void OnTestEvent(string report)
        {
             // Do nothing
        }
    }
}
#endif
