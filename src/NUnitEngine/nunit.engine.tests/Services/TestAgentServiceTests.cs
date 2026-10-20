// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using System;
using System.Collections.Generic;
using System.Runtime.Versioning;
using System.Threading.Tasks;
using NUnit.Framework;
using NSubstitute;

namespace NUnit.Engine.Services
{
    public class TestAgentServiceTests
    {
        private TestPackage _package;
        private ITestAgentProvider _first;
        private ITestAgentProvider _second;
        private TestAgentService _service;

        [SetUp]
        public void SetUp()
        {
            _package = new TestPackage("test-assembly.dll");
            _first = Substitute.For<ITestAgentProvider>();
            _second = Substitute.For<ITestAgentProvider>();
            _service = new TestAgentService(_first, _second);
            var context = new ServiceContext();
            context.Add(_service);
            _service.StartService();
        }

        [TearDown]
        public void TearDown()
        {
            _service.StopService();
            _service.Dispose();
        }

        [Test]
        public void AgentInformationIncludesEveryProvider()
        {
            var local = new TestAgentInfo("local", TestAgentType.LocalProcess, new FrameworkName(".NETCoreApp,Version=v10.0"));
            var remote = new TestAgentInfo("remote", TestAgentType.RemoteProcess, new FrameworkName(".NETCoreApp,Version=v10.0"));
            _first.AvailableAgents.Returns(new List<TestAgentInfo> { local });
            _second.AvailableAgents.Returns(new List<TestAgentInfo> { remote });
            _first.GetAgentsForPackage(_package).Returns(new List<TestAgentInfo> { local });
            _second.GetAgentsForPackage(_package).Returns(new List<TestAgentInfo> { remote });

            Assert.That(_service.AvailableAgents, Is.EqualTo(new[] { local, remote }));
            Assert.That(_service.GetAgentsForPackage(_package), Is.EqualTo(new[] { local, remote }));
        }

        [Test]
        public void CanFindAnAgentFromAnotherProvider()
        {
            _second.IsAgentAvailable(_package).Returns(true);

            Assert.That(_service.IsAgentAvailable(_package), Is.True);
        }

        [Test]
        public void ReportsWhenNoProviderSupportsPackage()
        {
            Assert.That(_service.IsAgentAvailable(_package), Is.False);
            Assert.That(() => _service.GetAgent(_package), Throws.TypeOf<InvalidOperationException>());
        }

        [Test]
        public void ReleasesOnlyToProviderThatCreatedAgent()
        {
            var agent = AcquireFrom(_second);

            _service.ReleaseAgent(agent);

            _second.Received(1).ReleaseAgent(agent);
            _first.DidNotReceiveWithAnyArgs().ReleaseAgent(default!);
        }

        [Test]
        public void KeepsOwnershipOfAgentsFromDifferentProviders()
        {
            var firstPackage = new TestPackage("first.dll");
            var secondPackage = new TestPackage("second.dll");
            var firstAgent = Substitute.For<ITestAgent>();
            var secondAgent = Substitute.For<ITestAgent>();
            _first.IsAgentAvailable(firstPackage).Returns(true);
            _second.IsAgentAvailable(secondPackage).Returns(true);
            _first.GetAgent(firstPackage).Returns(firstAgent);
            _second.GetAgent(secondPackage).Returns(secondAgent);

            _service.GetAgent(firstPackage);
            _service.GetAgent(secondPackage);
            _service.ReleaseAgent(secondAgent);
            _service.ReleaseAgent(firstAgent);

            _first.Received(1).ReleaseAgent(firstAgent);
            _first.DidNotReceive().ReleaseAgent(secondAgent);
            _second.Received(1).ReleaseAgent(secondAgent);
            _second.DidNotReceive().ReleaseAgent(firstAgent);
        }

        [Test]
        public void RejectsUnknownAgentEvenWhenItsIdMatches()
        {
            var owned = AcquireFrom(_second);
            var unknown = Substitute.For<ITestAgent>();
            unknown.Id.Returns(owned.Id);

            Assert.That(() => _service.ReleaseAgent(unknown), Throws.TypeOf<InvalidOperationException>());
            _service.ReleaseAgent(owned);
            _second.Received(1).ReleaseAgent(owned);
        }

        [Test]
        public void RejectsSecondRelease()
        {
            var agent = AcquireFrom(_first);
            _service.ReleaseAgent(agent);

            Assert.That(() => _service.ReleaseAgent(agent), Throws.TypeOf<InvalidOperationException>());
            _first.Received(1).ReleaseAgent(agent);
        }

        [Test]
        public void FailedReleasePreservesOwnershipForRetry()
        {
            var agent = AcquireFrom(_first);
            int attempts = 0;
            _first.When(provider => provider.ReleaseAgent(agent)).Do(_ =>
            {
                if (++attempts == 1)
                    throw new InvalidOperationException("release failed");
            });

            Assert.That(() => _service.ReleaseAgent(agent), Throws.TypeOf<InvalidOperationException>());
            Assert.DoesNotThrow(() => _service.ReleaseAgent(agent));
            _first.Received(2).ReleaseAgent(agent);
        }

        [Test]
        public void DoesNotFallBackAfterSelectedProviderFailsToAcquire()
        {
            _first.IsAgentAvailable(_package).Returns(true);
            _second.IsAgentAvailable(_package).Returns(true);
            _first.GetAgent(_package).Returns(_ => throw new InvalidOperationException("provider unavailable"));

            Assert.That(() => _service.GetAgent(_package), Throws.TypeOf<InvalidOperationException>());
            _second.DidNotReceiveWithAnyArgs().GetAgent(default!);
        }

        [Test]
        public void ReentrantReleaseDoesNotReleaseAgentTwice()
        {
            var agent = AcquireFrom(_first);
            _first.When(provider => provider.ReleaseAgent(agent)).Do(_ =>
                Assert.That(() => _service.ReleaseAgent(agent), Throws.TypeOf<InvalidOperationException>()));

            _service.ReleaseAgent(agent);

            _first.Received(1).ReleaseAgent(agent);
        }

        [Test]
        public void DefaultEngineResolvesProviderServiceForAcquisitionAndInformation()
        {
            using var engine = new TestEngine();
            engine.Initialize();

            var provider = engine.Services.GetService<ITestAgentProvider>();
            Assert.That(provider, Is.InstanceOf<TestAgentService>());
            Assert.That(engine.Services.GetService<ITestAgentInfo>(), Is.SameAs(provider));
            Assert.That(engine.Services.GetService<TestAgency>().Status, Is.EqualTo(ServiceStatus.Started));
        }

        [Test]
        public void ExplicitMissingProviderCannotFallBackToLocalExecution()
        {
            _first.IsAgentAvailable(_package).Returns(true);
            _package.AddSetting("RequestedAgentProvider", "Uninstalled.RemoteProvider", true);

            Assert.That(() => _service.GetAgent(_package), Throws.TypeOf<NUnitEngineException>());
            _first.DidNotReceiveWithAnyArgs().GetAgent(default!);
            _second.DidNotReceiveWithAnyArgs().GetAgent(default!);
        }

        [Test]
        public void EmptyProviderSelectionIsAnError()
        {
            _package.AddSetting("RequestedAgentProvider", string.Empty, true);
            Assert.That(() => _service.IsAgentAvailable(_package), Throws.TypeOf<NUnitEngineException>());
        }

        [Test]
        public void ExitDiagnosticComesOnlyFromOwningProvider()
        {
            var owner = Substitute.For<ITestAgentProvider, IAgentExitDiagnostics>();
            var other = Substitute.For<ITestAgentProvider, IAgentExitDiagnostics>();
            using var service = new TestAgentService(owner, other);
            var context = new ServiceContext();
            context.Add(service);
            service.StartService();
            var agent = Substitute.For<ITestAgent>();
            owner.IsAgentAvailable(_package).Returns(true);
            owner.GetAgent(_package).Returns(agent);
            ((IAgentExitDiagnostics)owner).GetAgentExitMessage(agent).Returns("exit code 42");
            service.GetAgent(_package);

            Assert.That(((IAgentExitDiagnostics)service).GetAgentExitMessage(agent), Is.EqualTo("exit code 42"));
            ((IAgentExitDiagnostics)other).DidNotReceiveWithAnyArgs().GetAgentExitMessage(default!);
        }

        [Test]
        public void ProviderWithoutDiagnosticsRemainsSupported()
        {
            ITestAgent agent = AcquireFrom(_first);

            Assert.That(((IAgentExitDiagnostics)_service).GetAgentExitMessage(agent), Is.Null);
        }

        [Test]
        public void ExitDiagnosticRejectsUnknownAndReleasedAgentReferences()
        {
            var owner = Substitute.For<ITestAgentProvider, IAgentExitDiagnostics>();
            using var service = new TestAgentService(owner);
            var context = new ServiceContext();
            context.Add(service);
            service.StartService();
            var agent = Substitute.For<ITestAgent>();
            var unknown = Substitute.For<ITestAgent>();
            unknown.Id.Returns(agent.Id);
            owner.IsAgentAvailable(_package).Returns(true);
            owner.GetAgent(_package).Returns(agent);
            service.GetAgent(_package);

            Assert.That(((IAgentExitDiagnostics)service).GetAgentExitMessage(unknown), Is.Null);
            service.ReleaseAgent(agent);
            Assert.That(((IAgentExitDiagnostics)service).GetAgentExitMessage(agent), Is.Null);
            ((IAgentExitDiagnostics)owner).DidNotReceiveWithAnyArgs().GetAgentExitMessage(default!);
        }

        [Test]
        public void ExitDiagnosticDoesNotHoldOwnershipLockDuringProviderCall()
        {
            var owner = Substitute.For<ITestAgentProvider, IAgentExitDiagnostics>();
            using var service = new TestAgentService(owner);
            var context = new ServiceContext();
            context.Add(service);
            service.StartService();
            var agent = Substitute.For<ITestAgent>();
            owner.IsAgentAvailable(_package).Returns(true);
            owner.GetAgent(_package).Returns(agent);
            service.GetAgent(_package);
            ((IAgentExitDiagnostics)owner).GetAgentExitMessage(agent).Returns(_ =>
            {
                Task release = Task.Run(() => service.ReleaseAgent(agent));
                Assert.That(release.Wait(3000), Is.True, "A diagnostic wait blocked ownership release");
                return "exit code 42";
            });

            Assert.That(((IAgentExitDiagnostics)service).GetAgentExitMessage(agent), Is.EqualTo("exit code 42"));
            owner.Received(1).ReleaseAgent(agent);
        }

        [Test]
        public void ExitDiagnosticIsUnavailableDuringRelease()
        {
            var owner = Substitute.For<ITestAgentProvider, IAgentExitDiagnostics>();
            using var service = new TestAgentService(owner);
            var context = new ServiceContext();
            context.Add(service);
            service.StartService();
            var agent = Substitute.For<ITestAgent>();
            owner.IsAgentAvailable(_package).Returns(true);
            owner.GetAgent(_package).Returns(agent);
            service.GetAgent(_package);
            owner.When(provider => provider.ReleaseAgent(agent)).Do(_ =>
                Assert.That(((IAgentExitDiagnostics)service).GetAgentExitMessage(agent), Is.Null));

            service.ReleaseAgent(agent);
            ((IAgentExitDiagnostics)owner).DidNotReceiveWithAnyArgs().GetAgentExitMessage(default!);
        }

        private ITestAgent AcquireFrom(ITestAgentProvider provider)
        {
            var agent = Substitute.For<ITestAgent>();
            provider.IsAgentAvailable(_package).Returns(true);
            provider.GetAgent(_package).Returns(agent);
            return _service.GetAgent(_package);
        }
    }
}
