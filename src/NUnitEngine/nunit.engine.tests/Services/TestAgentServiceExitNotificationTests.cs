// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using System;
using System.IO;
using NSubstitute;
using NUnit.Common;
using NUnit.Engine.Runners;
using NUnit.Framework;

namespace NUnit.Engine.Services
{
    /// <summary>
    /// Checks that provider exit notifications preserve runner diagnostics and the
    /// acquired reference's return path without throwing on the notifying thread.
    /// </summary>
    public class TestAgentServiceExitNotificationTests
    {
        private ServiceContext _services;
        private TestAgentService _agency;
        private ITestAgentProvider _provider;
        private ITestAgent _agent;
        private TestPackage _package;

        [SetUp]
        public void SetUp()
        {
            _agent = Substitute.For<ITestAgent>();
            _agent.Id.Returns(Guid.NewGuid());
            _provider = Substitute.For<ITestAgentProvider, IService>();
            _provider.IsAgentAvailable(Arg.Any<TestPackage>()).Returns(true);
            _provider.GetAgent(Arg.Any<TestPackage>()).Returns(_agent);
            _services = new ServiceContext();
            _services.Add((IService)_provider);
            _agency = new TestAgentService();
            _services.Add(_agency);
            _services.ServiceManager.StartServices();
            _package = new TestPackage("offline.dll").SubPackages[0];
            _package.AddSetting(SettingDefinitions.TargetFrameworkName.WithValue(".NETCoreApp,Version=v10.0"), false);
        }

        [TearDown]
        public void TearDown()
        {
            _agency.Dispose();
            _services.ServiceManager.Dispose();
        }

        [TestCase(42)]
        [TestCase(null)]
        public void ExitNotificationRetainsDiagnosticAndReturnPath(int? exitCode)
        {
            _agency.GetAgent(_package);

            Assert.That(() => NotifyExit(_provider, _agent, exitCode), Throws.Nothing);
            Assert.That(_agency.IsAgentActive(_agent.Id), Is.True, "The acquired reference still needs to be returned.");
            var message = _agency.GetAgentExitMessage(_agent.Id);
            Assert.That(message, Does.Contain(_agent.Id.ToString("B")).And.Contain("Controlled agent exit"));
            if (exitCode is not null)
                Assert.That(message, Does.Contain($"Native process exit code: {exitCode}."));
            else
                Assert.That(message, Does.Not.Contain("Native process exit code"));

            _agency.ReturnAgent(_agent);

            _provider.Received(1).ReturnAgent(_agent);
            Assert.That(_agency.IsAgentActive(_agent.Id), Is.False);
        }

        [Test]
        public void ReturnFailureAfterExitRetainsOwnershipForRetry()
        {
            _agency.GetAgent(_package);
            NotifyExit(_provider, _agent, null);
            bool failReturn = true;
            _provider.When(provider => provider.ReturnAgent(_agent)).Do(_ =>
            {
                if (failReturn)
                    throw new IOException("Controlled return failure");
            });

            Assert.That(() => _agency.ReturnAgent(_agent), Throws.TypeOf<IOException>());
            Assert.That(_agency.IsAgentActive(_agent.Id), Is.True);

            failReturn = false;
            _agency.ReturnAgent(_agent);

            _provider.Received(2).ReturnAgent(_agent);
            Assert.That(_agency.IsAgentActive(_agent.Id), Is.False);
        }

        [Test]
        public void ProcessRunnerReportsRecordedExitWhenTransportFails()
        {
            var remoteRunner = Substitute.For<ITestEngineRunner>();
            _agent.CreateRunner(_package).Returns(remoteRunner);
            remoteRunner.Explore(TestFilter.Empty).Returns(_ =>
            {
                NotifyExit(_provider, _agent, 42);
                throw new IOException("Controlled transport failure");
            });
            var runnerServices = Substitute.For<IServiceLocator>();
            runnerServices.GetService<TestAgentService>().Returns(_agency);
            runnerServices.GetService<ITestRunnerFactory>().Returns(Substitute.For<ITestRunnerFactory>());
            using (var runner = new ProcessRunner(runnerServices, _package))
            {
                var result = runner.Explore(TestFilter.Empty);

                Assert.That(result.Xml.GetAttribute("result"), Is.EqualTo("Failed"));
                var message = result.Xml.SelectSingleNode("failure/message")!.InnerText;
                Assert.That(message, Does.Contain("Controlled agent exit")
                    .And.Contain("Native process exit code: 42.").And.Contain("Controlled transport failure"));
                Assert.That(_agency.IsAgentActive(_agent.Id), Is.True);
            }

            _provider.Received(1).ReturnAgent(_agent);
            Assert.That(_agency.IsAgentActive(_agent.Id), Is.False);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void NotificationFromAnotherProviderOrReferenceDoesNotChangeOwnership(bool differentReference)
        {
            var otherProvider = Substitute.For<ITestAgentProvider, IService>();
            var services = new ServiceContext();
            services.Add((IService)_provider);
            services.Add((IService)otherProvider);
            var agency = new TestAgentService();
            services.Add(agency);
            services.ServiceManager.StartServices();
            try
            {
                agency.GetAgent(_package);
                var id = _agent.Id;
                var notificationAgent = Substitute.For<ITestAgent>();
                notificationAgent.Id.Returns(id);

                Assert.That(() => NotifyExit(differentReference ? _provider : otherProvider,
                    differentReference ? notificationAgent : _agent, 42), Throws.Nothing);

                Assert.That(agency.GetAgentExitMessage(id), Is.Null);
                Assert.That(agency.IsAgentActive(id), Is.True);
                agency.ReturnAgent(_agent);
                _provider.Received(1).ReturnAgent(_agent);
            }
            finally
            {
                services.ServiceManager.Dispose();
            }
        }

        private static void NotifyExit(ITestAgentProvider provider, ITestAgent agent, int? exitCode)
        {
            provider.AgentExited += Raise.Event<AgentExitedEventHandler>(provider,
                new AgentExitedEventArgs(agent, "Controlled agent exit", exitCode));
        }
    }
}