// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace NUnit.Remote.Deployment
{
    /// <summary>
    /// Owns one local staged host, launcher bundles and test package. Cleanup requires
    /// the original marker, a successful host exit and an independent native process audit.
    /// Failed startup, shutdown or audit retains the payload and external evidence.
    /// </summary>
    internal sealed class SessionDeployment : IDisposable
    {
        private static readonly JsonSerializerOptions Json = new JsonSerializerOptions { WriteIndented = true };
        private readonly DeploymentDefinition _definition;
        private readonly DeploymentPayload _payload;
        private readonly string _evidence;
        private Process? _host;
        private Task<string>? _stdout;
        private Task<string>? _stderr;
        private bool _startAttempted;
        private bool _startupConfirmed;
        private bool _disposed;

        private SessionDeployment(DeploymentDefinition definition, string directory, string owner)
        {
            _definition = definition;
            _evidence = definition.EvidenceDirectory;
            Directory = directory;
            Receipt = new DeploymentReceipt { Owner = owner, Directory = directory };
            _payload = new DeploymentPayload(PayloadDirectory);
        }

        internal string Directory { get; }
        internal string PayloadDirectory => Path.Combine(Directory, "payload");
        internal string AssemblyPath => DeploymentPaths.Child(PayloadDirectory, "tests/" + _definition.AssemblyRelativePath);
        internal string ServerDefinitionPath => Path.Combine(_evidence, "server.xml");
        internal DeploymentReceipt Receipt { get; }
        internal IReadOnlyList<DeploymentFile> Files => _payload.Files;

        /// <summary>
        /// Builds a fresh verified snapshot. Generated addins refer only to staged
        /// launchers, and all evidence is kept outside the disposable session root.
        /// </summary>
        internal static SessionDeployment Stage(DeploymentDefinition definition)
        {
            definition = Validate(definition);
            string owner = Guid.NewGuid().ToString("N");
            string directory = Path.Combine(definition.DeploymentRoot, "nunit-" + owner);
            if (System.IO.Directory.Exists(directory) || File.Exists(directory))
                throw new IOException("The generated session identity already exists.");
            System.IO.Directory.CreateDirectory(definition.EvidenceDirectory);
            System.IO.Directory.CreateDirectory(directory);
            using (var marker = new FileStream(Path.Combine(directory, "owner.txt"), FileMode.CreateNew, FileAccess.Write, FileShare.None))
                marker.Write(Encoding.UTF8.GetBytes(owner));
            var session = new SessionDeployment(definition, directory, owner);
            session.Save();
            try
            {
                session._payload.CopyTree(definition.HostDirectory, "host", regenerateAddins: true);
                var addins = new List<string>();
                foreach (var launcher in definition.Launchers)
                {
                    session._payload.CopyTree(launcher.ToolsDirectory, "agents/" + launcher.Name, regenerateAddins: true);
                    addins.Add("../agents/" + launcher.Name + "/" + launcher.LauncherRelativePath);
                }
                session._payload.Write("host/session.addins", string.Join("\n", addins) + "\n");
                session._payload.CopyTree(definition.PackageDirectory, "tests");
                session._payload.Verify();
                session.Receipt.ManifestSha256 = session._payload.Digest;
                File.WriteAllText(Path.Combine(session._evidence, "manifest.json"), JsonSerializer.Serialize(session.Files, Json));
                session.Receipt.State = "Staged";
                session.Save();
                return session;
            }
            catch (Exception exception)
            {
                session.Receipt.State = "StagingFailed";
                session.Receipt.Errors.Add(exception.ToString());
                session.Save();
                throw;
            }
        }

        /// <summary>
        /// Starts only the staged host and checks the readiness PID against its
        /// owned process handle. The backend uses the same provisional definition
        /// already understood by the engine's remote provider.
        /// </summary>
        internal void Start()
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            if (_startAttempted)
                throw new InvalidOperationException("A deployment session can start only once.");
            _startAttempted = true;
            try
            {
                DeploymentPaths.EnsureOwned(_definition.DeploymentRoot, Directory, Receipt.Owner);
                _payload.Verify();
                string control = Path.Combine(Directory, "control");
                string work = Path.Combine(Directory, "work");
                System.IO.Directory.CreateDirectory(control);
                System.IO.Directory.CreateDirectory(work);
                string ready = Path.Combine(control, "ready.xml");
                var start = new ProcessStartInfo(_definition.DotnetExecutable)
                {
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardInput = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    WorkingDirectory = work
                };
                start.ArgumentList.Add(DeploymentPaths.Child(PayloadDirectory, "host/" + _definition.HostAssemblyRelativePath));
                start.ArgumentList.Add(ready);
                start.ArgumentList.Add(DeploymentPaths.Child(PayloadDirectory, "agents"));
                // Package dependencies are copied into the payload; the child has no need
                // to restore or resolve them from the coordinator's package cache.
                start.Environment["NUGET_PACKAGES"] = Path.Combine(control, "empty-package-cache");
                _host = Process.Start(start) ?? throw new IOException("The staged host did not start.");
                Receipt.HostPid = _host.Id;
                Receipt.HostStartUtc = _host.StartTime.ToUniversalTime();
                _stdout = _host.StandardOutput.ReadToEndAsync();
                _stderr = _host.StandardError.ReadToEndAsync();
                Receipt.State = "Starting";
                Save();
                var deadline = Stopwatch.StartNew();
                while (!File.Exists(ready) && !_host.HasExited && deadline.Elapsed < _definition.StartupTimeout)
                    Thread.Sleep(20);
                if (!File.Exists(ready) || _host.HasExited)
                    throw new IOException("The staged host did not become ready before its startup deadline.");
                var response = XElement.Load(ready);
                if ((int?)response.Attribute("pid") != _host.Id || (int?)response.Attribute("port") is not int port
                    || port < 1 || port > 65535 || (int?)response.Attribute("agents") is not int agents || agents < 1)
                    throw new IOException("The readiness receipt does not identify the expected live provider host.");
                new XElement("nunit-remote-server", new XAttribute("name", "session-" + Receipt.Owner),
                    new XAttribute("endpoint", "tcp://127.0.0.1:" + port)).Save(ServerDefinitionPath);
                File.Copy(ready, Path.Combine(_evidence, "host.ready.xml"), false);
                _startupConfirmed = true;
                Receipt.StartupConfirmed = true;
                Receipt.State = "Running";
                Save();
            }
            catch (Exception exception)
            {
                Receipt.State = "StartupFailed";
                Receipt.Errors.Add(exception.ToString());
                Save();
                throw;
            }
        }

        /// <summary>
        /// Stops the host without escalating to arbitrary PID termination. The owner
        /// and native reference checks are repeated immediately before recursive removal.
        /// A failure is recorded and the directory is retained for recovery.
        /// </summary>
        internal bool CloseAndCleanup()
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            if (Receipt.Cleaned)
                return true;
            Receipt.NativeAuditPassed = false;
            try
            {
                DeploymentPaths.EnsureOwned(_definition.DeploymentRoot, Directory, Receipt.Owner);
                if (_host is not null)
                {
                    if (!_host.HasExited)
                        _host.StandardInput.Close();
                    if (!_host.WaitForExit((int)_definition.ShutdownTimeout.TotalMilliseconds))
                        throw new TimeoutException("The staged host has not exited; retain the deployment.");
                    Receipt.HostExitCode = _host.ExitCode;
                    SaveOutput();
                    if (_host.ExitCode != 0 || !_startupConfirmed)
                        throw new IOException("Host startup or shutdown failed; retain the deployment.");
                }
                else if (_startAttempted)
                    throw new IOException("Startup did not produce a confirmed host; retain the deployment.");
                DeploymentProcessAudit.AssertNoReferences(Directory);
                Receipt.NativeAuditPassed = true;
                // Enumerating the entire tree rejects links before Directory.Delete.
                _ = DeploymentPaths.Files(Directory).Count();
                DeploymentPaths.EnsureOwned(_definition.DeploymentRoot, Directory, Receipt.Owner);
                System.IO.Directory.Delete(Directory, recursive: true);
                Receipt.Cleaned = !System.IO.Directory.Exists(Directory);
                Receipt.State = "Cleaned";
                Save();
                return Receipt.Cleaned;
            }
            catch (Exception exception)
            {
                Receipt.Errors.Add(exception.ToString());
                Receipt.State = "CleanupRetained";
                Receipt.Cleaned = false;
                Save();
                return false;
            }
        }

        /// <summary>
        /// Closes the lifetime channel and records a confirmed exit even when the caller
        /// did not reach explicit cleanup. A stopped host does not establish a native
        /// process audit or permit deletion. Earlier startup and cleanup failures remain
        /// failures, and disposal errors must not replace an exception from the caller.
        /// </summary>
        public void Dispose()
        {
            if (_disposed)
                return;
            try
            {
                if (_host is null || Receipt.Cleaned)
                    return;
                if (!_host.HasExited)
                {
                    _host.StandardInput.Close();
                    if (!_host.WaitForExit((int)_definition.ShutdownTimeout.TotalMilliseconds))
                        throw new TimeoutException("Dispose could not confirm host shutdown; deployment retained.");
                }
                Receipt.HostExitCode = _host.ExitCode;
                SaveOutput();
                if (_host.ExitCode != 0)
                {
                    Receipt.Errors.Add("The disposed host exited unsuccessfully; deployment retained.");
                    Receipt.State = "CleanupRetained";
                }
                else if (Receipt.State is "Running" or "Starting")
                    Receipt.State = "StoppedRetained";
            }
            catch (Exception exception)
            {
                Receipt.Errors.Add(exception.ToString());
                Receipt.State = "CleanupRetained";
            }
            finally
            {
                if (_host is not null && !Receipt.Cleaned)
                {
                    try
                    {
                        Save();
                    }
                    catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
                    {
                        // The live object retains this failure when its external receipt
                        // cannot be written. Do not mask the caller's original exception.
                        Receipt.Errors.Add("Could not save disposal evidence: " + exception);
                        Receipt.State = "CleanupRetained";
                    }
                }
                _host?.Dispose();
                _disposed = true;
            }
        }

        private void SaveOutput()
        {
            if (_stdout is not null && _stderr is not null)
            {
                if (!Task.WhenAll(_stdout, _stderr).Wait(TimeSpan.FromSeconds(5)))
                    throw new TimeoutException("Host output is incomplete; retain the deployment.");
                File.WriteAllText(Path.Combine(_evidence, "host.stdout.txt"), _stdout.Result);
                File.WriteAllText(Path.Combine(_evidence, "host.stderr.txt"), _stderr.Result);
            }
        }

        private void Save() => File.WriteAllText(Path.Combine(_evidence, "session.json"),
            JsonSerializer.Serialize(Receipt, Json));

        private static DeploymentDefinition Validate(DeploymentDefinition definition)
        {
            string deployment = DeploymentPaths.Root(definition.DeploymentRoot);
            string evidence = DeploymentPaths.Root(definition.EvidenceDirectory);
            string host = DeploymentPaths.Root(definition.HostDirectory);
            string package = DeploymentPaths.Root(definition.PackageDirectory);
            if (Path.GetPathRoot(deployment) == deployment || System.IO.Directory.Exists(evidence) || File.Exists(evidence))
                throw new IOException("Use a dedicated deployment root and a fresh evidence directory.");
            var inputs = new List<string> { host, package };
            if (definition.Launchers.Count == 0 || definition.Launchers.Count > 16
                || definition.StartupTimeout <= TimeSpan.Zero || definition.StartupTimeout > TimeSpan.FromMinutes(5)
                || definition.ShutdownTimeout <= TimeSpan.Zero || definition.ShutdownTimeout > TimeSpan.FromMinutes(5))
                throw new ArgumentException("Supply explicit launchers and bounded lifecycle deadlines.", nameof(definition));
            var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var launcher in definition.Launchers)
            {
                if (launcher.Name.Length == 0 || launcher.Name.Any(character => !char.IsAsciiLetterOrDigit(character) && character != '-')
                    || !names.Add(launcher.Name))
                    throw new IOException("Launcher names must be unique portable identifiers.");
                string tools = DeploymentPaths.Root(launcher.ToolsDirectory);
                if (!File.Exists(DeploymentPaths.Child(tools, launcher.LauncherRelativePath)))
                    throw new FileNotFoundException("The explicit standard launcher does not exist.");
                inputs.Add(tools);
            }
            string hostAssembly = DeploymentPaths.Child(host, definition.HostAssemblyRelativePath);
            if (!Path.GetExtension(hostAssembly).Equals(".dll", StringComparison.OrdinalIgnoreCase)
                || !File.Exists(hostAssembly)
                || !File.Exists(DeploymentPaths.Child(package, definition.AssemblyRelativePath)))
                throw new FileNotFoundException("The host entry point and test assembly must be supplied.");
            if (DeploymentPaths.Contains(deployment, evidence) || DeploymentPaths.Contains(evidence, deployment))
                throw new IOException("Evidence and disposable deployments must not overlap.");
            foreach (string input in inputs)
                if (!System.IO.Directory.Exists(input) || DeploymentPaths.Contains(input, deployment)
                    || DeploymentPaths.Contains(deployment, input) || DeploymentPaths.Contains(input, evidence)
                    || DeploymentPaths.Contains(evidence, input))
                    throw new IOException("Source, deployment and evidence roots must be separate.");
            return definition with { DeploymentRoot = deployment, EvidenceDirectory = evidence, HostDirectory = host, PackageDirectory = package };
        }
    }
}
