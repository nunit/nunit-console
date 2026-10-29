// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using System;
using System.Diagnostics;
using System.Net.Sockets;
using NUnit.Common;
using NUnit.Engine.Runners;
using NUnit.Framework;
using NSubstitute;

namespace NUnit.Engine.Services
{
    /// <summary>
    /// Exercises local shutdown exceptions through provider ownership and runner disposal,
    /// using controlled child processes to distinguish confirmed exit from lost communication.
    /// </summary>
    public class LocalProcessAgentReleaseTests
    {
        private TestPackage _package;
        private ITestAgent _agent;
        private ITestEngineRunner _remoteRunner;
        private ITestAgentProvider _owner;
        private TestAgentService _agency;
        private Process? _process;
        private bool _processStarted;
        private Exception? _stopFailure;

        [SetUp]
        public void SetUp()
        {
            _process = null;
            _processStarted = false;
            _stopFailure = null;
            _package = new TestPackage("test-assembly.dll").SubPackages[0];
            _package.AddSetting(SettingDefinitions.TargetFrameworkName.WithValue(".NETCoreApp,Version=v10.0"), false);
            _agent = Substitute.For<ITestAgent>();
            _agent.Id.Returns(Guid.NewGuid());
            _remoteRunner = Substitute.For<ITestEngineRunner>();
            _remoteRunner.CountTestCases(TestFilter.Empty).Returns(42);
            _agent.CreateRunner(_package).Returns(_remoteRunner);
            _owner = Substitute.For<ITestAgentProvider>();
            _owner.IsAgentAvailable(_package).Returns(true);
            _owner.GetAgent(_package).Returns(_agent);
            _agency = new TestAgentService(new[] { _owner });
            var localProvider = new LocalProcessAgentProvider(_agency);
            _owner.When(provider => provider.ReturnAgent(_agent)).Do(_ => localProvider.ReturnAgent(_agent));
            _agent.When(agent => agent.Stop()).Do(_ =>
            {
                if (_stopFailure is not null)
                    throw _stopFailure;
                FinishChild();
            });
        }

        [TearDown]
        public void TearDown()
        {
            try
            {
                FinishChild();
            }
            finally
            {
                if (_processStarted && !_process!.HasExited)
                {
                    _process.Kill();
                    _process.WaitForExit(10000);
                }
                _process?.Dispose();
                _remoteRunner.Dispose();
                _agency.Dispose();
            }
        }

        [TestCase(false)]
        [TestCase(true)]
        public void SocketFailureAfterSuccessfulExitReleasesOwnership(bool throughRunner)
        {
            StartChild(0, false);
            _stopFailure = new SocketException((int)SocketError.ConnectionReset);

            Assert.That(() => Release(throughRunner), Throws.Nothing);

            _agent.Received(1).Stop();
            Assert.That(() => _agency.ReturnAgent(_agent), Throws.InvalidOperationException);
            _owner.Received(1).ReturnAgent(_agent);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void SocketFailureAfterNonzeroExitRetainsOwnership(bool throughRunner)
        {
            StartChild(7, false);
            _stopFailure = new SocketException((int)SocketError.ConnectionReset);

            AssertFailureAndRetry(throughRunner);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void SocketFailureWhileProcessIsRunningRetainsOwnership(bool throughRunner)
        {
            StartChild(0, true);
            _stopFailure = new SocketException((int)SocketError.ConnectionReset);

            AssertFailureAndRetry(throughRunner);
        }

        [TestCase(false, false)]
        [TestCase(false, true)]
        [TestCase(true, false)]
        [TestCase(true, true)]
        public void OtherStopFailureAfterSuccessfulExitRetainsOwnership(bool throughRunner, bool unloadFailure)
        {
            StartChild(0, false);
            _stopFailure = unloadFailure
                ? new NUnitEngineUnloadException("The agent could not stop.")
                : new InvalidOperationException("The agent could not stop.");

            AssertFailureAndRetry(throughRunner);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void UnavailableProcessStateDoesNotReplaceStopFailure(bool throughRunner)
        {
            _process = new Process();
            _agency._agentStore.AddAgent(_agent.Id, _process);
            _stopFailure = new SocketException((int)SocketError.ConnectionReset);

            AssertFailureAndRetry(throughRunner);
        }

        [Test]
        public void UnloadFailureIsPreservedAfterSuccessfulLocalShutdown()
        {
            StartChild(0, false);
            _stopFailure = new SocketException((int)SocketError.ConnectionReset);
            var unloadFailure = new NUnitEngineUnloadException("The runner could not unload.");
            _remoteRunner.When(runner => runner.Unload()).Do(_ => throw unloadFailure);
            using var runner = CreateRunner();

            var failure = Assert.Throws<NUnitEngineUnloadException>(() => runner.Dispose());

            Assert.That(failure!.InnerException, Is.SameAs(unloadFailure));
            Assert.That(() => _agency.ReturnAgent(_agent), Throws.InvalidOperationException);
        }

        [Test]
        public void UnloadAndStopFailuresAreBothReportedAndOwnershipIsRetained()
        {
            StartChild(0, true);
            _stopFailure = new NUnitEngineUnloadException("The agent could not stop.");
            var unloadFailure = new NUnitEngineUnloadException("The runner could not unload.");
            _remoteRunner.When(runner => runner.Unload()).Do(_ => throw unloadFailure);
            using var runner = CreateRunner();

            var failure = Assert.Throws<NUnitEngineUnloadException>(() => runner.Dispose());

            Assert.That(failure!.Message, Does.Contain(unloadFailure.Message).And.Contain(_stopFailure.Message));
            _stopFailure = null;
            _agency.ReturnAgent(_agent);
            _owner.Received(2).ReturnAgent(_agent);
        }

        private void AssertFailureAndRetry(bool throughRunner)
        {
            Assert.That(Assert.Catch(() => Release(throughRunner)), Is.SameAs(_stopFailure));

            _stopFailure = null;
            _agency.ReturnAgent(_agent);

            _owner.Received(2).ReturnAgent(_agent);
            Assert.That(() => _agency.ReturnAgent(_agent), Throws.InvalidOperationException);
        }

        private void Release(bool throughRunner)
        {
            if (throughRunner)
            {
                using var runner = CreateRunner();
                runner.Dispose();
            }
            else
            {
                _agency.GetAgent(_package);
                _agency.ReturnAgent(_agent);
            }
        }

        private ProcessRunner CreateRunner()
        {
            var services = Substitute.For<IServiceLocator>();
            services.GetService<TestAgentService>().Returns(_agency);
            var runner = new ProcessRunner(services, _package);
            Assert.That(runner.CountTestCases(TestFilter.Empty), Is.EqualTo(42));
            return runner;
        }

        private void StartChild(int exitCode, bool keepRunning)
        {
            var startInfo = TestAgencyExitTests.CreateStartInfo("--agent-exit-child", exitCode, false, -1);
            startInfo.RedirectStandardInput = true;
            _process = new Process { StartInfo = startInfo };
            Assert.That(_process.Start(), Is.True);
            _processStarted = true;
            _agency._agentStore.AddAgent(_agent.Id, _process);
            // Retain the store's starting state to exercise shutdown before its exit callback updates the record.
            if (!keepRunning)
                FinishChild();
        }

        private void FinishChild()
        {
            if (_processStarted && !_process!.HasExited)
            {
                _process.StandardInput.WriteLine();
                Assert.That(_process.WaitForExit(10000), Is.True, "The controlled child did not exit.");
            }
        }
    }
}
