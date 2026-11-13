// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using System;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Common;
using NUnit.Engine.Runners;
using NUnit.Framework;
using NSubstitute;

namespace NUnit.Engine.Services
{
    /// <summary>
    /// Exercises provider ownership without requiring a native process for an acquired agent.
    /// These tests apply to remote and in-process providers as well as local-process selection.
    /// </summary>
    public class TestAgentServiceOwnershipTests
    {
        private TestPackage _package;
        private ITestAgent _agent;
        private ITestAgentProvider _owner;
        private ITestAgentProvider _other;
        private TestAgentService _agency => _services.GetService<TestAgentService>();
        private ServiceContext _services;

        [SetUp]
        public void SetUp()
        {
            _package = new TestPackage("test-assembly.dll").SubPackages[0];
            _package.AddSetting(SettingDefinitions.TargetFrameworkName.WithValue(".NETCoreApp,Version=v10.0"), false);
            _agent = Substitute.For<ITestAgent>();
            _agent.Id.Returns(Guid.NewGuid());
            _owner = Substitute.For<ITestAgentProvider, IService>();
            _owner.IsAgentAvailable(_package).Returns(true);
            _owner.GetAgent(_package).Returns(_agent);
            _other = Substitute.For<ITestAgentProvider, IService>();
            _services = new ServiceContext();
            _services.Add((IService)Substitute.For<ITestRunnerFactory, IService>());
            _services.Add((IService)_other);
            _services.Add((IService)_owner);
            _services.Add(new TestAgentService());
            _services.ServiceManager.StartServices();
        }

        [TearDown]
        public void TearDown()
        {
            _services.ServiceManager.Dispose();
        }

        [Test]
        public void AcquiredAgentIsActiveWithoutLocalProcess()
        {
            Assert.That(_agency.GetAgent(_package), Is.SameAs(_agent));
            Assert.That(_agency.IsAgentActive(_agent.Id), Is.True);
            Assert.That(_agency.GetAgentExitMessage(_agent.Id), Is.Null);
        }

        [Test]
        public void ReleaseReturnsAgentOnlyToOriginatingProvider()
        {
            _agency.GetAgent(_package);

            _agency.ReturnAgent(_agent);

            _owner.Received(1).ReturnAgent(_agent);
            _other.DidNotReceiveWithAnyArgs().ReturnAgent(default!);
            Assert.That(_agency.IsAgentActive(_agent.Id), Is.False);
        }

        [Test]
        public void UnknownAgentIsNotSentToAnyProvider()
        {
            Assert.That(() => _agency.ReturnAgent(_agent), Throws.InvalidOperationException);
            _owner.DidNotReceiveWithAnyArgs().ReturnAgent(default!);
            _other.DidNotReceiveWithAnyArgs().ReturnAgent(default!);
        }

        [Test]
        public void DifferentReferenceWithSameIdCannotReleaseAcquiredAgent()
        {
            _agency.GetAgent(_package);
            var foreign = Substitute.For<ITestAgent>();
            foreign.Id.Returns(_agent.Id);

            Assert.That(() => _agency.ReturnAgent(foreign), Throws.InvalidOperationException);
            Assert.That(_agency.IsAgentActive(_agent.Id), Is.True);
            _owner.DidNotReceiveWithAnyArgs().ReturnAgent(default!);
        }

        [Test]
        public void DuplicateReleaseDoesNotReachProviderTwice()
        {
            _agency.GetAgent(_package);
            _agency.ReturnAgent(_agent);

            Assert.That(() => _agency.ReturnAgent(_agent), Throws.InvalidOperationException);
            _owner.Received(1).ReturnAgent(_agent);
        }

        [Test]
        public void FailedReleaseRetainsOwnerForRetry()
        {
            _agency.GetAgent(_package);
            int attempts = 0;
            _owner.When(provider => provider.ReturnAgent(_agent)).Do(_ =>
            {
                if (++attempts == 1)
                    throw new InvalidOperationException("Release acknowledgement was lost.");
            });

            Assert.That(() => _agency.ReturnAgent(_agent), Throws.InvalidOperationException);
            Assert.That(_agency.IsAgentActive(_agent.Id), Is.True);

            _agency.ReturnAgent(_agent);

            Assert.That(attempts, Is.EqualTo(2));
            Assert.That(_agency.IsAgentActive(_agent.Id), Is.False);
            _other.DidNotReceiveWithAnyArgs().ReturnAgent(default!);
        }

        [Test]
        public void ConcurrentReleaseReturnsAgentOnce()
        {
            _agency.GetAgent(_package);
            using var entered = new ManualResetEventSlim();
            using var finish = new ManualResetEventSlim();
            _owner.When(provider => provider.ReturnAgent(_agent)).Do(_ =>
            {
                entered.Set();
                if (!finish.Wait(TimeSpan.FromSeconds(5)))
                    throw new TimeoutException("The release test did not permit completion.");
            });

            var first = Task.Run(() => TryRelease());
            Task<Exception?>? second = null;
            try
            {
                Assert.That(entered.Wait(TimeSpan.FromSeconds(5)), Is.True);
                second = Task.Run(() => TryRelease());
            }
            finally
            {
                finish.Set();
            }

            Assert.That(Task.WaitAll(new Task[] { first, second! }, TimeSpan.FromSeconds(5)), Is.True);
            Assert.That(first.Result, Is.Null);
            Assert.That(second!.Result, Is.TypeOf<InvalidOperationException>());
            _owner.Received(1).ReturnAgent(_agent);
        }

        [Test]
        public void SuitabilityQueryDoesNotAcquireAgent()
        {
            Assert.That(_agency.IsAgentAvailable(_package), Is.True);
            _owner.DidNotReceiveWithAnyArgs().GetAgent(default!);
            _other.DidNotReceiveWithAnyArgs().GetAgent(default!);
        }

        [Test]
        public void FirstCompatibleProviderStillSuppliesAgent()
        {
            _other.IsAgentAvailable(_package).Returns(true);
            var firstAgent = Substitute.For<ITestAgent>();
            firstAgent.Id.Returns(Guid.NewGuid());
            _other.GetAgent(_package).Returns(firstAgent);

            Assert.That(_agency.GetAgent(_package), Is.SameAs(firstAgent));
            _owner.DidNotReceiveWithAnyArgs().GetAgent(default!);
        }

        [Test]
        [Category("Integration")]
        public void RunnerDisposalReturnsAgentToProviderWithoutStoppingItDirectly()
        {
            var remoteRunner = Substitute.For<ITestEngineRunner>();
            remoteRunner.CountTestCases(TestFilter.Empty).Returns(42);
            _agent.CreateRunner(_package).Returns(remoteRunner);
            using (var runner = new ProcessRunner(_services, _package))
                Assert.That(runner.CountTestCases(TestFilter.Empty), Is.EqualTo(42));

            _owner.Received(1).ReturnAgent(_agent);
            _agent.DidNotReceive().Stop();
            Assert.That(_agency.IsAgentActive(_agent.Id), Is.False);
        }

        private Exception? TryRelease()
        {
            try
            {
                _agency.ReturnAgent(_agent);
                return null;
            }
            catch (Exception exception)
            {
                return exception;
            }
        }
    }
}
