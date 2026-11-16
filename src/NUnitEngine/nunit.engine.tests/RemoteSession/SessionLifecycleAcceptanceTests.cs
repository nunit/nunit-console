// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using NUnit.Framework;
using NUnit.Remote.Deployment;

namespace NUnit.Engine.Tests.RemoteSession
{
    /// <summary>
    /// Checks durable lifecycle outcomes with a source-built, bounded fault host.
    /// No installed launcher, real agent, network endpoint or private fixture root is needed.
    /// </summary>
    [TestFixture, Category("Acceptance"), NonParallelizable]
    public class SessionLifecycleAcceptanceTests
    {
        private static readonly string[] ProbeFiles =
        {
            "SessionHostProbe.dll", "SessionHostProbe.deps.json", "SessionHostProbe.runtimeconfig.json"
        };

        private string _case = null!;
        private DeploymentDefinition _definition = null!;
        private SessionDeployment? _session;
        private Process? _host;

        /// <summary>Creates a fresh case root; retained payloads remain available as evidence.</summary>
        [SetUp]
        public void CreateCase()
        {
            _case = Path.Combine(TestContext.CurrentContext.WorkDirectory, "lifecycle-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_case);
            _session = null;
            _host = null;
        }

        /// <summary>Releases only the retained test child and independently checks for native references.</summary>
        [TearDown]
        public void ConfirmOwnedChildStopped()
        {
            try
            {
                if (_host is not null && !_host.HasExited)
                    ReleaseHost();
                _session?.Dispose();
                if (_session is not null)
                    DeploymentProcessAudit.AssertNoReferences(_session.Directory);
            }
            finally
            {
                _host?.Dispose();
            }
        }

        /// <summary>Neither zero exit nor a later orderly exit can invent a missing readiness confirmation.</summary>
        [TestCase("exit-without-ready")]
        [TestCase("wait-before-ready")]
        public void MissingReadinessRemainsAStartupFailure(string mode)
        {
            var session = Stage(mode);
            Assert.That(session.Start, Throws.TypeOf<IOException>());
            string[] errors = session.Receipt.Errors.ToArray();
            session.Dispose();
            using var receipt = ReadReceipt();
            Assert.That(receipt.RootElement.GetProperty("State").GetString(), Is.EqualTo("StartupFailed"));
            Assert.That(receipt.RootElement.GetProperty("HostExitCode").GetInt32(), Is.Zero);
            Assert.That(receipt.RootElement.GetProperty("StartupConfirmed").GetBoolean(), Is.False);
            Assert.That(session.Receipt.Errors, Is.EqualTo(errors));
            AssertRetained(receipt);
        }

        /// <summary>Preserves an unsuccessful host exit rather than replacing it with a stopped success.</summary>
        [Test]
        public void NonzeroExitRetainsDeploymentAndFailureEvidence()
        {
            var session = Start("nonzero-on-eof");
            Assert.That(session.CloseAndCleanup(), Is.False);
            session.Dispose();
            using var receipt = ReadReceipt();
            Assert.That(receipt.RootElement.GetProperty("State").GetString(), Is.EqualTo("CleanupRetained"));
            Assert.That(receipt.RootElement.GetProperty("HostExitCode").GetInt32(), Is.EqualTo(17));
            Assert.That(File.ReadAllText(Path.Combine(_definition.EvidenceDirectory, "host.stderr.txt")),
                Does.Contain("Controlled lifecycle probe exit: 17"));
            AssertRetained(receipt);
        }

        /// <summary>Requires confirmed exit before an explicit retry and preserves the original failed receipt.</summary>
        [Test]
        public void ShutdownTimeoutRequiresExplicitRecoveryBeforeDeletion()
        {
            var session = Start("ignore-eof");
            Assert.That(session.CloseAndCleanup(), Is.False);
            byte[] firstFailure = File.ReadAllBytes(ReceiptPath);
            string snapshot = Path.Combine(_case, "first-cleanup-failure.json");
            File.WriteAllBytes(snapshot, firstFailure);
            string[] errors = session.Receipt.Errors.ToArray();
            using (var failed = ReadReceipt())
            {
                Assert.That(failed.RootElement.GetProperty("HostExitCode").ValueKind, Is.EqualTo(JsonValueKind.Null));
                AssertRetained(failed);
            }
            Assert.That(_host!.HasExited, Is.False);
            ReleaseHost();
            Assert.That(session.CloseAndCleanup(), Is.True, string.Join("\n", session.Receipt.Errors));
            using var recovered = ReadReceipt();
            Assert.That(recovered.RootElement.GetProperty("State").GetString(), Is.EqualTo("Cleaned"));
            Assert.That(recovered.RootElement.GetProperty("HostExitCode").GetInt32(), Is.Zero);
            Assert.That(recovered.RootElement.GetProperty("NativeAuditPassed").GetBoolean(), Is.True);
            Assert.That(Directory.Exists(session.Directory), Is.False);
            Assert.That(session.Receipt.Errors, Is.EqualTo(errors));
            Assert.That(File.ReadAllBytes(snapshot), Is.EqualTo(firstFailure));
        }

        /// <summary>Distinguishes disposal after readiness from verified deployment removal.</summary>
        [Test]
        public void DisposalAfterReadinessRetainsPayloadWithoutClaimingAudit()
        {
            var session = Start("ready-success");
            session.Dispose();
            using var receipt = ReadReceipt();
            Assert.That(receipt.RootElement.GetProperty("State").GetString(), Is.EqualTo("StoppedRetained"));
            Assert.That(receipt.RootElement.GetProperty("HostExitCode").GetInt32(), Is.Zero);
            Assert.That(receipt.RootElement.GetProperty("StartupConfirmed").GetBoolean(), Is.True);
            AssertRetained(receipt);
        }

        /// <summary>Never replaces the caller's primary exception when disposal cannot save its receipt.</summary>
        [Test]
        public void EvidenceWriteFailurePreservesTheCallerException()
        {
            var session = Start("ready-success");
            File.Move(ReceiptPath, Path.Combine(_case, "receipt-before-write-failure.json"));
            Directory.CreateDirectory(ReceiptPath);
            var failure = new InvalidOperationException("Controlled caller failure.");
            Action action = () =>
            {
                using (session)
                    throw failure;
            };
            Assert.That(Assert.Throws<InvalidOperationException>(action), Is.SameAs(failure));
            Assert.That(session.Receipt.State, Is.EqualTo("CleanupRetained"));
            Assert.That(session.Receipt.Errors, Has.Some.StartsWith("Could not save disposal evidence:"));
            Assert.That(session.Receipt.HostExitCode, Is.Zero);
            Assert.That(session.Receipt.Cleaned, Is.False);
            Assert.That(Directory.Exists(session.Directory), Is.True);
            File.WriteAllText(Path.Combine(_case, "disposal-live-observation.json"), JsonSerializer.Serialize(session.Receipt));
        }

        /// <summary>Successful cleanup remains confirmed after repeated cleanup and disposal.</summary>
        [Test]
        public void SuccessfulCleanupIsIdempotentAndDurable()
        {
            var session = Start("ready-success");
            Assert.That(session.CloseAndCleanup(), Is.True);
            byte[] completed = File.ReadAllBytes(ReceiptPath);
            Assert.That(session.CloseAndCleanup(), Is.True);
            session.Dispose();
            Assert.That(File.ReadAllBytes(ReceiptPath), Is.EqualTo(completed));
            using var receipt = ReadReceipt();
            Assert.That(receipt.RootElement.GetProperty("Cleaned").GetBoolean(), Is.True);
            Assert.That(receipt.RootElement.GetProperty("NativeAuditPassed").GetBoolean(), Is.True);
            Assert.That(Directory.Exists(session.Directory), Is.False);
        }

        /// <summary>Disposal timeout cannot claim a confirmed host exit or rewrite evidence after release.</summary>
        [Test]
        public void DisposalTimeoutRetainsAnUnconfirmedOutcome()
        {
            var session = Start("ignore-eof");
            session.Dispose();
            byte[] retained = File.ReadAllBytes(ReceiptPath);
            using (var receipt = ReadReceipt())
            {
                Assert.That(receipt.RootElement.GetProperty("HostExitCode").ValueKind, Is.EqualTo(JsonValueKind.Null));
                Assert.That(receipt.RootElement.GetProperty("State").GetString(), Is.EqualTo("CleanupRetained"));
                AssertRetained(receipt);
            }
            Assert.That(_host!.HasExited, Is.False);
            ReleaseHost();
            session.Dispose();
            Assert.That(File.ReadAllBytes(ReceiptPath), Is.EqualTo(retained));
        }

        private string ReceiptPath => Path.Combine(_definition.EvidenceDirectory, "session.json");

        private SessionDeployment Stage(string mode)
        {
            string host = Path.Combine(_case, "host");
            string package = Path.Combine(_case, "tests");
            string tools = Path.Combine(_case, "tools");
            Directory.CreateDirectory(host);
            Directory.CreateDirectory(package);
            Directory.CreateDirectory(tools);
            foreach (string file in ProbeFiles)
                File.Copy(Path.Combine(AppContext.BaseDirectory, "session-host-probe", file), Path.Combine(host, file));
            File.WriteAllText(Path.Combine(host, "probe-mode.txt"), mode);
            File.WriteAllText(Path.Combine(package, "fixture.dll"), "synthetic package, never executed");
            File.WriteAllText(Path.Combine(tools, "launcher.dll"), "synthetic launcher, never loaded");
            _definition = new DeploymentDefinition
            {
                DeploymentRoot = Path.Combine(_case, "deployed"),
                EvidenceDirectory = Path.Combine(_case, "receipt"),
                HostDirectory = host,
                HostAssemblyRelativePath = "SessionHostProbe.dll",
                PackageDirectory = package,
                AssemblyRelativePath = "fixture.dll",
                Launchers = new[] { new LauncherBundle("probe", tools, "launcher.dll") },
                StartupTimeout = TimeSpan.FromSeconds(mode == "wait-before-ready" ? 2 : 10),
                ShutdownTimeout = mode == "ignore-eof" ? TimeSpan.FromMilliseconds(200) : TimeSpan.FromSeconds(5)
            };
            return _session = SessionDeployment.Stage(_definition);
        }

        private SessionDeployment Start(string mode)
        {
            var session = Stage(mode);
            session.Start();
            _host = Process.GetProcessById(session.Receipt.HostPid!.Value);
            Assert.That(_host.StartTime.ToUniversalTime(), Is.EqualTo(session.Receipt.HostStartUtc));
            return session;
        }

        private JsonDocument ReadReceipt() => JsonDocument.Parse(File.ReadAllText(ReceiptPath));

        private void AssertRetained(JsonDocument receipt)
        {
            Assert.That(receipt.RootElement.GetProperty("Cleaned").GetBoolean(), Is.False);
            Assert.That(receipt.RootElement.GetProperty("NativeAuditPassed").GetBoolean(), Is.False);
            Assert.That(Directory.Exists(_session!.Directory), Is.True);
        }

        private void ReleaseHost()
        {
            DeploymentPaths.EnsureOwned(_definition.DeploymentRoot, _session!.Directory, _session.Receipt.Owner);
            File.WriteAllText(Path.Combine(_session.Directory, "control", "release.txt"), "release the test-owned probe");
            if (!_host!.WaitForExit(5000))
            {
                _host.Kill();
                Assert.That(_host.WaitForExit(5000), Is.True, "The owned fault host did not exit after termination.");
                Assert.Fail("The owned fault host did not honour its release signal.");
            }
        }
    }
}
