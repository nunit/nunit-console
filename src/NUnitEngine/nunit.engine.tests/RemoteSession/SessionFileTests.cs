// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using NUnit.Framework.Interfaces;
using NUnit.Remote.Deployment;

namespace NUnit.Engine.Tests.RemoteSession
{
    /// <summary>
    /// Exercises staging invariants without an installed host, launcher, agent or network.
    /// Synthetic entry-point files are never executed by these tests.
    /// </summary>
    [TestFixture]
    public class SessionFileTests
    {
        private static readonly string[] ManifestNames = { "a.dll", "b.dll" };
        private string _case = null!;

        /// <summary>Creates an independently owned filesystem sandbox for each case.</summary>
        [SetUp]
        public void CreateCase()
        {
            _case = Path.Combine(TestContext.CurrentContext.WorkDirectory, "session-files-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_case);
        }

        /// <summary>
        /// Removes only a successful case's ordinary tree. Failed cases retain their
        /// input and payload evidence; filesystem links must never be followed.
        /// </summary>
        [TearDown]
        public void RemoveSuccessfulCase()
        {
            if (TestContext.CurrentContext.Result.Outcome.Status != TestStatus.Passed)
                return;
            string parent = Path.GetFullPath(TestContext.CurrentContext.WorkDirectory);
            Assert.That(Path.GetDirectoryName(_case), Is.EqualTo(parent));
            _ = DeploymentPaths.Files(_case).Count();
            Directory.Delete(_case, recursive: true);
        }

        /// <summary>Rejects portable manifest names which could escape or alias another file.</summary>
        [TestCase("")]
        [TestCase("../escape.dll")]
        [TestCase("/absolute.dll")]
        [TestCase("a/../escape.dll")]
        [TestCase("a\\escape.dll")]
        [TestCase("a//escape.dll")]
        [TestCase("a./escape.dll")]
        [TestCase("a /escape.dll")]
        [TestCase("a:stream.dll")]
        [TestCase("a/./b.dll")]
        [TestCase("a/line\nbreak.dll")]
        public void AmbiguousPayloadPathIsRejected(string relative)
        {
            Assert.That(() => DeploymentPaths.Child(_case, relative), Throws.TypeOf<IOException>());
            Assert.That(Directory.EnumerateFileSystemEntries(_case), Is.Empty);
        }

        /// <summary>Distinguishes a child from an unrelated directory with a shared prefix.</summary>
        [Test]
        public void ContainmentRequiresADirectoryBoundary()
        {
            string child = Path.Combine(_case, "child");
            Assert.That(DeploymentPaths.Contains(_case, child), Is.True);
            Assert.That(DeploymentPaths.Contains(_case, _case + "-other"), Is.False);
            Assert.That(DeploymentPaths.Child(_case, "nested/file.dll"),
                Is.EqualTo(Path.Combine(_case, "nested", "file.dll")));
        }

        /// <summary>Requires a matching marker even when the generated directory name is correct.</summary>
        [Test]
        public void ForeignMarkerCannotAuthorizeCleanup()
        {
            const string owner = "0123456789abcdef0123456789abcdef";
            string directory = Path.Combine(_case, "nunit-" + owner);
            Directory.CreateDirectory(directory);
            string marker = Path.Combine(directory, "owner.txt");
            File.WriteAllText(marker, "another-owner");
            Assert.That(() => DeploymentPaths.EnsureOwned(_case, directory, owner), Throws.TypeOf<IOException>());
            Assert.That(File.ReadAllText(marker), Is.EqualTo("another-owner"));
        }

        /// <summary>Rejects a matching marker outside the exact generated session directory.</summary>
        [Test]
        public void MarkerAloneCannotAuthorizeAnotherDirectory()
        {
            string directory = Path.Combine(_case, "not-a-generated-session");
            Directory.CreateDirectory(directory);
            File.WriteAllText(Path.Combine(directory, "owner.txt"), "owner");
            Assert.That(() => DeploymentPaths.EnsureOwned(_case, directory, "owner"), Throws.TypeOf<IOException>());
        }

        /// <summary>Detects additions, missing assets and same-length changes before launch.</summary>
        [TestCase("additional")]
        [TestCase("missing")]
        [TestCase("same-length-change")]
        public void PayloadMutationFailsVerification(string mutation)
        {
            string root = Path.Combine(_case, "payload");
            var payload = new DeploymentPayload(root);
            payload.Write("tests/fixture.dll", "original");
            if (mutation == "additional")
                File.WriteAllText(Path.Combine(root, "unlisted.dll"), "extra");
            else if (mutation == "missing")
                File.Delete(Path.Combine(root, "tests", "fixture.dll"));
            else
                File.WriteAllText(Path.Combine(root, "tests", "fixture.dll"), "modified");
            Assert.That(payload.Verify, Throws.TypeOf<IOException>());
            Assert.That(Directory.Exists(root), Is.True);
        }

        /// <summary>Defines a stable manifest independent of filesystem enumeration or copy order.</summary>
        [Test]
        public void ManifestDigestIsIndependentOfInsertionOrder()
        {
            var first = new DeploymentPayload(Path.Combine(_case, "first"));
            var second = new DeploymentPayload(Path.Combine(_case, "second"));
            first.Write("a.dll", "alpha");
            first.Write("b.dll", "beta");
            second.Write("b.dll", "beta");
            second.Write("a.dll", "alpha");
            first.Verify();
            second.Verify();
            Assert.That(first.Digest, Is.EqualTo(second.Digest));
            Assert.That(first.Files.Select(file => file.Name), Is.EquivalentTo(ManifestNames));
        }

        /// <summary>Rejects case aliases even on filesystems which would permit two distinct files.</summary>
        [Test]
        public void CaseAliasCannotReplaceAnInventoriedFile()
        {
            var payload = new DeploymentPayload(Path.Combine(_case, "payload"));
            payload.Write("tests/Fixture.dll", "original");
            Assert.That(() => payload.Write("tests/fixture.dll", "replacement"), Throws.TypeOf<IOException>());
            Assert.That(File.ReadAllText(Path.Combine(_case, "payload", "tests", "Fixture.dll")), Is.EqualTo("original"));
        }

        /// <summary>Stops source key material from becoming a deployment asset.</summary>
        [TestCase(".snk")]
        [TestCase(".pfx")]
        [TestCase(".key")]
        [TestCase(".private")]
        [TestCase(".clixml")]
        public void PrivateExportsAreRejected(string extension)
        {
            string source = Path.Combine(_case, "source");
            Directory.CreateDirectory(source);
            File.WriteAllText(Path.Combine(source, "private" + extension), "synthetic-not-a-secret");
            var payload = new DeploymentPayload(Path.Combine(_case, "payload"));
            Assert.That(() => payload.CopyTree(source, "tests"), Throws.TypeOf<IOException>());
            Assert.That(Directory.Exists(Path.Combine(_case, "payload")), Is.False);
        }

        /// <summary>Leaves original source bytes and addins intact when regenerating staged configuration.</summary>
        [Test]
        public void StagingRegeneratesAddinsWithoutChangingSource()
        {
            var definition = Definition();
            string original = Path.Combine(definition.HostDirectory, "original.addins");
            File.WriteAllText(original, "must-not-be-copied");
            using var session = SessionDeployment.Stage(definition);
            Assert.That(session.Files.Any(file => file.Name == "host/original.addins"), Is.False);
            Assert.That(session.Files.Any(file => file.Name == "host/session.addins"), Is.True);
            Assert.That(File.ReadAllText(original), Is.EqualTo("must-not-be-copied"));
            Assert.That(session.Receipt.State, Is.EqualTo("Staged"));
            Assert.That(session.Receipt.HostPid, Is.Null);
            Assert.That(File.Exists(Path.Combine(definition.EvidenceDirectory, "manifest.json")), Is.True);
        }

        /// <summary>Rejects ambiguous assembly paths without creating a session or evidence directory.</summary>
        [TestCase("../escape.dll")]
        [TestCase("/absolute.dll")]
        [TestCase("a\\escape.dll")]
        [TestCase("a//escape.dll")]
        [TestCase("a:stream.dll")]
        public void InvalidAssemblyPathDoesNotStage(string relative)
        {
            var definition = Definition() with { AssemblyRelativePath = relative };
            Assert.That(() => SessionDeployment.Stage(definition), Throws.TypeOf<IOException>());
            Assert.That(Directory.Exists(definition.DeploymentRoot), Is.False);
            Assert.That(Directory.Exists(definition.EvidenceDirectory), Is.False);
        }

        /// <summary>Keeps evidence outside disposable files, regardless of which root contains the other.</summary>
        [TestCase(true)]
        [TestCase(false)]
        public void OverlappingEvidenceIsRejected(bool evidenceInside)
        {
            var definition = Definition();
            definition = evidenceInside
                ? definition with { EvidenceDirectory = Path.Combine(definition.DeploymentRoot, "evidence") }
                : definition with { DeploymentRoot = Path.Combine(definition.EvidenceDirectory, "deployed") };
            Assert.That(() => SessionDeployment.Stage(definition), Throws.TypeOf<IOException>());
            Assert.That(Directory.Exists(definition.DeploymentRoot), Is.False);
            Assert.That(Directory.Exists(definition.EvidenceDirectory), Is.False);
        }

        /// <summary>Never rewrites an existing evidence directory while starting another session.</summary>
        [Test]
        public void ExistingEvidenceIsPreserved()
        {
            var definition = Definition();
            Directory.CreateDirectory(definition.EvidenceDirectory);
            string receipt = Path.Combine(definition.EvidenceDirectory, "session.json");
            File.WriteAllText(receipt, "earlier-receipt");
            Assert.That(() => SessionDeployment.Stage(definition), Throws.TypeOf<IOException>());
            Assert.That(File.ReadAllText(receipt), Is.EqualTo("earlier-receipt"));
            Assert.That(Directory.Exists(definition.DeploymentRoot), Is.False);
        }

        /// <summary>Disposal alone does not claim execution, process auditing or payload deletion.</summary>
        [Test]
        public void DisposalBeforeStartRetainsStagedEvidence()
        {
            var session = SessionDeployment.Stage(Definition());
            session.Dispose();
            Assert.That(session.Receipt.State, Is.EqualTo("Staged"));
            Assert.That(session.Receipt.StartupConfirmed, Is.False);
            Assert.That(session.Receipt.NativeAuditPassed, Is.False);
            Assert.That(session.Receipt.Cleaned, Is.False);
            Assert.That(Directory.Exists(session.Directory), Is.True);
            Assert.That(session.Start, Throws.TypeOf<ObjectDisposedException>());
        }

        private DeploymentDefinition Definition()
        {
            string host = Path.Combine(_case, "host");
            string package = Path.Combine(_case, "tests");
            string tools = Path.Combine(_case, "tools");
            Directory.CreateDirectory(host);
            Directory.CreateDirectory(package);
            Directory.CreateDirectory(tools);
            File.WriteAllText(Path.Combine(host, "host.dll"), "synthetic-host-never-executed");
            File.WriteAllText(Path.Combine(package, "fixture.dll"), "synthetic-fixture-never-executed");
            File.WriteAllText(Path.Combine(tools, "launcher.dll"), "synthetic-launcher-never-executed");
            return new DeploymentDefinition
            {
                DeploymentRoot = Path.Combine(_case, "deployed"),
                EvidenceDirectory = Path.Combine(_case, "receipt"),
                HostDirectory = host,
                HostAssemblyRelativePath = "host.dll",
                PackageDirectory = package,
                AssemblyRelativePath = "fixture.dll",
                Launchers = new[] { new LauncherBundle("synthetic", tools, "launcher.dll") }
            };
        }
    }
}
