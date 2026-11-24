// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using System;
using System.Runtime.Versioning;
using NUnit.Common;
using NUnit.Engine.Runners;
using NUnit.Framework;
using NSubstitute;

namespace NUnit.Engine.Services
{
    /// <summary>
    /// Tests selection of an agent by name, using the RequestedAgentName setting.
    /// When a name is requested, only the provider that publishes an agent with
    /// that name is consulted.
    /// </summary>
    public class TestAgentServiceAgentSelectionTests
    {
        private static readonly FrameworkName NET10 = new FrameworkName(".NETCoreApp", new Version(10, 0));

        private TestPackage _package;
        private ITestAgent _agentA;
        private ITestAgent _agentB;
        private ITestAgentProvider _providerA;
        private ITestAgentProvider _providerB;
        private ServiceContext _services;

        private TestAgentService _agency => _services.GetService<TestAgentService>();

        [SetUp]
        public void SetUp()
        {
            _package = new TestPackage("test-assembly.dll").SubPackages[0];
            _package.AddSetting(SettingDefinitions.TargetFrameworkName.WithValue(NET10.FullName), false);

            _agentA = CreateAgent();
            _agentB = CreateAgent();
            _providerA = CreateProvider("AgentA", _agentA);
            _providerB = CreateProvider("AgentB", _agentB);

            // Provider A is registered first, so it wins when no name is requested.
            _services = new ServiceContext();
            _services.Add((IService)Substitute.For<ITestRunnerFactory, IService>());
            _services.Add((IService)_providerA);
            _services.Add((IService)_providerB);
            _services.Add(new TestAgentService());
            _services.ServiceManager.StartServices();
        }

        [TearDown]
        public void TearDown()
        {
            _services.ServiceManager.Dispose();
        }

        [Test]
        public void GetAgent_NameRequested_UsesOwningProviderOnly()
        {
            RequestAgent("AgentB");

            Assert.That(_agency.GetAgent(_package), Is.SameAs(_agentB));
            _providerA.DidNotReceiveWithAnyArgs().IsAgentAvailable(default!);
            _providerA.DidNotReceiveWithAnyArgs().GetAgent(default!);
        }

        [Test]
        public void GetAgent_UnknownNameRequested_Throws()
        {
            RequestAgent("NoSuchAgent");

            Assert.That(() => _agency.GetAgent(_package),
                Throws.InvalidOperationException.With.Message.Contains("NoSuchAgent"));
            _providerA.DidNotReceiveWithAnyArgs().GetAgent(default!);
            _providerB.DidNotReceiveWithAnyArgs().GetAgent(default!);
        }

        [Test]
        public void GetAgent_OwningProviderCannotRunPackage_ThrowsWithoutFallback()
        {
            RequestAgent("AgentB");
            _providerB.IsAgentAvailable(_package).Returns(false);

            Assert.That(() => _agency.GetAgent(_package),
                Throws.InvalidOperationException.With.Message.Contains("AgentB"));
            _providerA.DidNotReceiveWithAnyArgs().GetAgent(default!);
            _providerB.DidNotReceiveWithAnyArgs().GetAgent(default!);
        }

        [Test]
        public void GetAgent_NoNameRequested_UsesFirstAvailableProvider()
        {
            Assert.That(_agency.GetAgent(_package), Is.SameAs(_agentA));
            _providerB.DidNotReceiveWithAnyArgs().GetAgent(default!);
        }

        [TestCase("AgentB", true, true, ExpectedResult = true)]
        [TestCase("AgentB", true, false, ExpectedResult = false)]
        [TestCase("NoSuchAgent", true, true, ExpectedResult = false)]
        public bool IsAgentAvailable_NameRequested_ReflectsOwningProviderOnly(
            string requestedAgent, bool providerAAvailable, bool providerBAvailable)
        {
            RequestAgent(requestedAgent);
            _providerA.IsAgentAvailable(_package).Returns(providerAAvailable);
            _providerB.IsAgentAvailable(_package).Returns(providerBAvailable);

            return _agency.IsAgentAvailable(_package);
        }

        [Test]
        public void ReturnAgent_AfterNamedSelection_GoesToOwningProvider()
        {
            RequestAgent("AgentB");
            _agency.GetAgent(_package);

            _agency.ReturnAgent(_agentB);

            _providerB.Received(1).ReturnAgent(_agentB);
            _providerA.DidNotReceiveWithAnyArgs().ReturnAgent(default!);
            Assert.That(_agency.IsAgentActive(_agentB.Id), Is.False);
        }

        private void RequestAgent(string agentName)
        {
            _package.AddSetting(SettingDefinitions.RequestedAgentName.WithValue(agentName), false);
        }

        private static ITestAgent CreateAgent()
        {
            var agent = Substitute.For<ITestAgent>();
            agent.Id.Returns(Guid.NewGuid());
            return agent;
        }

        private ITestAgentProvider CreateProvider(string agentName, ITestAgent agent)
        {
            var provider = Substitute.For<ITestAgentProvider, IService>();
            provider.AvailableAgents.Returns(new[] { new TestAgentInfo(agentName, TestAgentType.LocalProcess, NET10) });
            provider.IsAgentAvailable(_package).Returns(true);
            provider.GetAgent(_package).Returns(agent);
            return provider;
        }
    }
}
