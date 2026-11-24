// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Versioning;
using System.Text;
using System.Threading.Tasks;

using NUnit.Framework;
using NUnit.Common;

namespace NUnit.Engine.Services
{
    [TestFixture]
    public class InProcessAgentProviderTests
    {
#if NETFRAMEWORK
        private const bool IS_NETFX_BUILD = true;
#else
        private const bool IS_NETFX_BUILD = false;
#endif

        private static readonly FrameworkName AGENT_RUNTIME = IS_NETFX_BUILD
            ? new FrameworkName(".NETFramework", new Version(4, 6, 2))
            : new FrameworkName(".NETCoreApp", new Version(10, 0));

        private static readonly TestCaseData[] TargetRuntimes =
        [
            // These are all valid for the net462 console runner
            new TestCaseData(".NETFramework,Version=2.0", IS_NETFX_BUILD),
            new TestCaseData(".NETFramework,Version=3.5", IS_NETFX_BUILD),
            new TestCaseData(".NETFramework,Version=4.0", IS_NETFX_BUILD),
            new TestCaseData(".NETFramework,Version=4.6.2", IS_NETFX_BUILD),
            new TestCaseData(".NETFramework,Version=4.8.1", IS_NETFX_BUILD),
            // These are all valid for the net10.0 console runner
            new TestCaseData(".NETCoreApp,Version=2.1", !IS_NETFX_BUILD),
            new TestCaseData(".NETCoreApp,Version=3.1", !IS_NETFX_BUILD),
            new TestCaseData(".NETCoreApp,Version=5.0", !IS_NETFX_BUILD),
            new TestCaseData(".NETCoreApp,Version=6.0", !IS_NETFX_BUILD),
            new TestCaseData(".NETCoreApp,Version=7.0", !IS_NETFX_BUILD),
            new TestCaseData(".NETCoreApp,Version=8.0", !IS_NETFX_BUILD),
            new TestCaseData(".NETCoreApp,Version=9.0", !IS_NETFX_BUILD),
            new TestCaseData(".NETCoreApp,Version=10.0", !IS_NETFX_BUILD),
            // These are not valid for either console runner.
            new TestCaseData(".NETCoreApp,Version=25.0", false),
        ];

        private InProcessAgentProvider _provider;
        [SetUp]
        public void Setup()
        {
            var context = new ServiceContext();
            _provider = new InProcessAgentProvider();

            var extensionService = new ExtensionService();
            context.Add(extensionService);
            context.Add(_provider);
            context.ServiceManager.StartServices();

            // As in ConsoleRunner: StartServices only locates candidate assemblies;
            // extensions are not attached to their extension points until installed.
            extensionService.InstallExtensions();
        }

        [TearDown]
        public void TearDown()
        {
            _provider.Dispose();
        }

        [Test]
        public void StartService_WithoutExtensionService_ThrowsAndSetsErrorStatus()
        {
            var context = new ServiceContext();
            var provider = new InProcessAgentProvider();
            context.Add(provider);

            Assert.That(() => provider.StartService(), Throws.TypeOf<NUnitEngineException>());
            Assert.That(provider.Status, Is.EqualTo(ServiceStatus.Error));
        }

        [Test, Order(1)]
        public void AvailableAgents_ReturnsSingleAgent()
        {
            var agents = _provider.AvailableAgents;
            Assert.That(agents, Has.Count.EqualTo(1));
            Assert.That(agents[0].AgentType, Is.EqualTo(TestAgentType.InProcess));
            Assert.That(agents[0].TargetRuntime, Is.EqualTo(AGENT_RUNTIME));
        }

        [TestCaseSource(nameof(TargetRuntimes))]
        public void GetAgentsForPackage_ReturnsAgentInfoForCompatiblePackage(string targetRuntime, bool isCompatible)
        {
            var package = new TestPackage("dummy.dll");
            package.AddSetting(SettingDefinitions.ImageTargetFrameworkName.WithValue(targetRuntime), true);
            package.AddSetting(SettingDefinitions.RequestedAgentName.WithValue("InProcessAgent"), true);
            package.AddSetting(SettingDefinitions.TargetFrameworkName.WithValue(AGENT_RUNTIME.FullName), true);

            var agents = _provider.GetAgentsForPackage(package);
            if (isCompatible)
            {
                Assert.That(agents, Has.Count.EqualTo(1));
                Assert.That(agents[0].AgentType, Is.EqualTo(TestAgentType.InProcess));
                Assert.That(agents[0].TargetRuntime, Is.EqualTo(AGENT_RUNTIME));
            }
            else
            {
                Assert.That(agents, Is.Empty);
            }
        }

        [TestCaseSource(nameof(TargetRuntimes))]
        public void IsAgentAvailable_ReturnsTrueForCompatiblePackage(string targetRuntime, bool isCompatible)
        {
            var package = new TestPackage("dummy.dll");
            package.AddSetting(SettingDefinitions.ImageTargetFrameworkName.WithValue(targetRuntime), true);
            package.AddSetting(SettingDefinitions.RequestedAgentName.WithValue("InProcessAgent"), true);
            package.AddSetting(SettingDefinitions.TargetFrameworkName.WithValue(AGENT_RUNTIME.FullName), true);
            Assert.That(_provider.IsAgentAvailable(package), Is.EqualTo(isCompatible));
        }

        [TestCaseSource(nameof(TargetRuntimes))]
        public void GetAgent_ReturnsAgentForCompatiblePackage(string targetRuntime, bool isCompatible)
        {
            var package = new TestPackage("dummy.dll");
            package.AddSetting(SettingDefinitions.ImageTargetFrameworkName.WithValue(targetRuntime), true);
            package.AddSetting(SettingDefinitions.RequestedAgentName.WithValue("InProcessAgent"), true);

            if (isCompatible)
            {
                package.AddSetting(SettingDefinitions.TargetFrameworkName.WithValue(AGENT_RUNTIME.FullName), true);
                Assert.That(_provider.GetAgent(package), Is.Not.Null);
            }
            else
                Assert.That(() => _provider.GetAgent(package), Throws.InvalidOperationException);
        }

        [TestCase("InProcessAgent", true)]
        [TestCase("SomeOtherAgent", false)]
        public void IsAgentAvailable_RespectsRequestedAgentName(string requestedAgent, bool expected)
        {
            var package = new TestPackage("dummy.dll").SubPackages[0];
            package.AddSetting(SettingDefinitions.ImageTargetFrameworkName.WithValue(AGENT_RUNTIME.FullName), true);
            package.AddSetting(SettingDefinitions.TargetFrameworkName.WithValue(AGENT_RUNTIME.FullName), true);
            package.AddSetting(SettingDefinitions.RequestedAgentName.WithValue(requestedAgent), true);

            Assert.That(_provider.IsAgentAvailable(package), Is.EqualTo(expected));
        }

        // Running in-process changes the process boundary, so it must be requested explicitly.
        // A compatible package that doesn't name the agent must not get it.
        [Test]
        public void IsAgentAvailable_ReturnsFalseWhenNotRequested()
        {
            var package = new TestPackage("dummy.dll").SubPackages[0];
            package.AddSetting(SettingDefinitions.ImageTargetFrameworkName.WithValue(AGENT_RUNTIME.FullName), true);
            package.AddSetting(SettingDefinitions.TargetFrameworkName.WithValue(AGENT_RUNTIME.FullName), true);

            Assert.That(_provider.IsAgentAvailable(package), Is.False);
        }

        // Each assembly in a multi-assembly run gets its own agent, so agent ids must not repeat.
        [Test]
        public void GetAgent_ReturnsUniqueAgentEachTime()
        {
            var agent1 = _provider.GetAgent(CreateCompatiblePackage());
            var agent2 = _provider.GetAgent(CreateCompatiblePackage());

            Assert.That(agent2, Is.Not.SameAs(agent1));
            Assert.That(agent2.Id, Is.Not.EqualTo(agent1.Id));
        }

        [Test]
        public void GetAgent_SetsSelectedAgentName()
        {
            var package = CreateCompatiblePackage();

            _provider.GetAgent(package);

            Assert.That(package.Settings.GetValueOrDefault(SettingDefinitions.SelectedAgentName), Is.EqualTo("InProcessAgent"));
        }

        // Runs a real assembly through the in-process agent, as the console does with --inprocess.
        // Creating the agent isn't enough: on .NET Framework the driver also crosses an AppDomain boundary.
        [Test]
        public void GetAgent_RunnerLoadsAndRunsMockAssembly()
        {
            string tfm = IS_NETFX_BUILD ? "net462" : "net10.0";
            string assemblyPath = System.IO.Path.GetFullPath($"testdata/{tfm}/mock-assembly.dll");
            var package = new TestPackage(assemblyPath).SubPackages[0];
            package.AddSetting(SettingDefinitions.RequestedAgentName.WithValue("InProcessAgent"), true);
            package.AddSetting(SettingDefinitions.ImageTargetFrameworkName.WithValue(AGENT_RUNTIME.FullName), true);
            package.AddSetting(SettingDefinitions.TargetFrameworkName.WithValue(AGENT_RUNTIME.FullName), true);

            var agent = _provider.GetAgent(package);
            using var runner = agent.CreateRunner(package);

            var loadResult = runner.Load();
            Assert.That(loadResult.Xml.Name, Is.EqualTo("test-suite").Or.EqualTo("test-run"), loadResult.Xml.OuterXml);

            var runResult = runner.Run(new NullListener(), TestFilter.Empty);
            Assert.That(runResult.Xml.GetAttribute("total"), Is.Not.Null.And.Not.EqualTo("0"), runResult.Xml.OuterXml);
        }

        private sealed class NullListener : ITestEventListener
        {
            public void OnTestEvent(string report)
            {
            }
        }

        private static TestPackage CreateCompatiblePackage()
        {
            var package = new TestPackage("dummy.dll").SubPackages[0];
            package.AddSetting(SettingDefinitions.ImageTargetFrameworkName.WithValue(AGENT_RUNTIME.FullName), true);
            package.AddSetting(SettingDefinitions.TargetFrameworkName.WithValue(AGENT_RUNTIME.FullName), true);
            package.AddSetting(SettingDefinitions.RequestedAgentName.WithValue("InProcessAgent"), true);
            return package;
        }

        //[TestCaseSource(nameof(TargetRuntimes))]
        public void ReturnAgent_DoesNotThrow(string targetRuntime, bool isCompatible)
        {
            var package = new TestPackage("dummy.dll");
            package.AddSetting(SettingDefinitions.TargetFrameworkName.WithValue(targetRuntime), true);
            Assert.That(_provider.IsAgentAvailable(package), Is.EqualTo(isCompatible));

            //if (isCompatible)
            //{
            //    var agent = _provider.GetAgent(package);
            //    Assert.That(() => _provider.ReturnAgent(agent), Throws.Nothing);
            //}
            //else
            //{
            //    Assert.That(() => _provider.GetAgent(package), Throws.InvalidOperationException);
            //}
        }
    }
}
