// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

#if NET8_0_OR_GREATER
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using NUnit.Common;
using NUnit.Engine.Communication.Messages;
using NUnit.Engine.Services;
using NUnit.Framework;

namespace NUnit.Engine.Integration
{
    /// <summary>
    /// Exercises the separate server executable, public engine API and published
    /// launcher. The tests do not reference or replace the server implementation.
    /// </summary>
    [NonParallelizable]
    public sealed class StandaloneProviderServerTests : IntegrationTests
    {
        private string _directory = null!;
        private string _hostDirectory = null!;
        private string _tools = null!;
        private string _receipt = null!;
        private Process? _host;
        private Task<string>? _stdout;
        private Task<string>? _stderr;
        private int _port;
        private bool _expectedStartupFailure;

        /// <summary>Stages only this test's host and unchanged published launcher files.</summary>
        [SetUp]
        public void StageHost()
        {
            _expectedStartupFailure = false;
            _host = null;
            // The normal engine search walks ancestor directories for extension packages.
            // Stage outside the source tree so installed development packages cannot win discovery.
            _directory = Path.Combine(Path.GetTempPath(), "NUnitTestServerTests", "standalone-" + Guid.NewGuid().ToString("N"));
            TestContext.Out.WriteLine("Standalone staging: " + _directory);
            _hostDirectory = Path.Combine(_directory, "server");
            _tools = Path.Combine(_directory, "NUnit.Extension.Net10PluggableAgent", "tools");
            _receipt = Path.Combine(_directory, "ready.xml");
            CopyDirectory(Path.Combine(TestContext.CurrentContext.TestDirectory, "..", "server", "net10.0"), _hostDirectory);
            CopyDirectory(Path.Combine(TestContext.CurrentContext.TestDirectory, "provider-test-tools", "net10"), _tools);
            string launcher = Path.Combine(_tools, "net8.0", "nunit-agent-launcher-net10.dll");
            Assert.That(File.Exists(launcher), Is.True, "The published modern launcher is required.");
            File.WriteAllText(Path.Combine(_hostDirectory, "server.addins"), launcher);
        }

        /// <summary>
        /// Requires orderly host exit and retains logs and staged files for independent
        /// process auditing. A failed deadline is never treated as successful cleanup.
        /// </summary>
        [TearDown]
        public void StopHost()
        {
            if (_host is null)
                return;
            try
            {
                if (!_host.HasExited)
                    _host.StandardInput.Close();
                if (!_host.WaitForExit(35000))
                {
                    _host.Kill(entireProcessTree: true);
                    _host.WaitForExit();
                    Assert.Fail("The owned server exceeded its shutdown deadline; retain its files.");
                }
                File.WriteAllText(Path.Combine(_directory, "stdout.txt"), _stdout!.GetAwaiter().GetResult());
                string error = _stderr!.GetAwaiter().GetResult();
                File.WriteAllText(Path.Combine(_directory, "stderr.txt"), error);
                Assert.That(_host.ExitCode, Is.EqualTo(_expectedStartupFailure ? 1 : 0), error);
            }
            finally
            {
                if (_stdout?.IsCompleted == true)
                    _stdout.Dispose();
                if (_stderr?.IsCompleted == true)
                    _stderr.Dispose();
                _host.Dispose();
                _host = null;
            }
        }

        /// <summary>Invalid startup arguments fail without claiming readiness.</summary>
        [TestCase(false)]
        [TestCase(true)]
        public void InvalidArgumentsDoNotPublishReadiness(bool missing)
        {
            _expectedStartupFailure = true;
            if (missing)
                Start();
            else
                Start("relative.xml");
            Assert.That(_host!.WaitForExit(15000), Is.True);
            Assert.That(File.Exists(_receipt), Is.False);
        }

        /// <summary>A caller's existing readiness evidence is not overwritten.</summary>
        [Test]
        public void ExistingReadinessReceiptIsPreserved()
        {
            File.WriteAllText(_receipt, "existing evidence");
            _expectedStartupFailure = true;
            Start(_receipt, _tools);
            Assert.That(_host!.WaitForExit(15000), Is.True);
            Assert.That(File.ReadAllText(_receipt), Is.EqualTo("existing evidence"));
        }

        /// <summary>The launcher-root check rejects an inconsistent deployment.</summary>
        [Test]
        public void ExtensionOutsideDeclaredRootDoesNotPublishReadiness()
        {
            string other = Path.Combine(_directory, "other");
            Directory.CreateDirectory(other);
            _expectedStartupFailure = true;
            Start(_receipt, other);
            Assert.That(_host!.WaitForExit(15000), Is.True);
            Assert.That(File.Exists(_receipt), Is.False);
        }

        /// <summary>Ordinary input and an invalid command do not terminate the server.</summary>
        [Test]
        public void ServerRemainsAvailableUntilInputCloses()
        {
            StartReady();
            _host!.StandardInput.WriteLine("keep running");
            _host.StandardInput.Flush();
            Assert.That(_host.WaitForExit(250), Is.False, "Only EOF requests shutdown.");
            Assert.That(Call("????", Guid.NewGuid(), Guid.Empty).Code, Is.EqualTo(RemoteAgentConnection.Error));
            var response = Call(RemoteAgentConnection.ListAgents, Guid.NewGuid(), Guid.Empty);
            Assert.That(response.Code, Is.EqualTo(MessageCode.CommandResult));
            Assert.That(XElement.Parse(response.Data!).Elements("agent").Count(), Is.EqualTo(1));
        }

        /// <summary>Unmodified public engine calls discover and execute a staged test exactly.</summary>
        [TestCase("NUnit.TestData.Singletons.OneTestCase.TestCase", "Passed")]
        [TestCase("NUnit.TestData.Assemblies.MockTestFixture.FailingTest", "Failed")]
        public void PublicEngineDiscoversAndRunsExactTest(string name, string expected)
        {
            StartReady();
            AssertEngineRunsExactTest(name, expected);
        }

        /// <summary>Windows' extended runtime path must not reject a correctly staged long-path launcher.</summary>
        [TestCase(false)]
        [TestCase(true)]
        [Platform("Win")]
        public void ServerAcceptsExtendedExtensionPathsFromALongDeploymentPath(bool extendedAllowedRoot)
        {
            string originalHost = _hostDirectory;
            string originalTools = _tools;
            // CreateProcess requires a short working directory. Only the host and
            // launcher locations need long paths to reproduce the runtime spelling.
            string longRoot = Path.Combine(_directory + "-long", new string('a', 80), new string('b', 80), new string('c', 80));
            _hostDirectory = Path.Combine(longRoot, "server");
            _tools = Path.Combine(longRoot, "agents");
            TestContext.Out.WriteLine("Long-path staging: " + longRoot);
            CopyDirectory(originalHost, _hostDirectory);
            CopyDirectory(originalTools, _tools);
            // Session deployments use relative addins. An absolute entry would
            // hide the extended prefix introduced by AppContext.BaseDirectory.
            File.WriteAllText(Path.Combine(_hostDirectory, "server.addins"),
                "../agents/net8.0/nunit-agent-launcher-net10.dll");
            Assert.That(_hostDirectory.Length, Is.GreaterThan(260), "The actual process must exercise a long deployment path.");

            StartReady(extendedAllowedRoot ? @"\\?\" + _tools : _tools);
            var ready = XElement.Load(_receipt);
            Assert.That(ready.Elements("extension").Any(extension =>
                ((string)extension.Attribute("assembly")!).StartsWith(@"\\?\", StringComparison.Ordinal)), Is.True,
                "The test must exercise runtime-generated extended paths, not just a long string.");
            var response = Call(RemoteAgentConnection.ListAgents, Guid.NewGuid(), Guid.Empty);
            Assert.That(response.Code, Is.EqualTo(MessageCode.CommandResult));
            Assert.That(XElement.Parse(response.Data!).Elements("agent").Count(), Is.EqualTo(1));
        }

        private void AssertEngineRunsExactTest(string name, string expected)
        {
            var package = Package();
            string definition = Path.Combine(_directory, "server.xml");
            new XElement("nunit-remote-server", new XAttribute("name", "standalone"),
                new XAttribute("endpoint", "tcp://127.0.0.1:" + _port)).Save(definition);
            package.AddSetting(RemoteAgentSettings.ServerDefinition.WithValue(definition), true);
            package.AddSetting(RemoteAgentSettings.AgentType.WithValue("RemoteProcess"), true);
            var filter = new TestFilter(new XElement("filter", new XElement("test", name)).ToString());
            using var engine = new TestEngine { InternalTraceLevel = InternalTraceLevel.Off, WorkDirectory = _directory };
            using var runner = engine.GetRunner(package);
            var discovery = runner.Explore(filter);
            Assert.That(discovery.SelectSingleNode("//failure"), Is.Null, discovery.OuterXml);
            Assert.That(discovery.SelectNodes("//test-case"), Has.Count.EqualTo(1), discovery.OuterXml);
            Assert.That(runner.CountTestCases(filter), Is.EqualTo(1));
            var events = new Events();
            var result = runner.Run(events, filter);
            File.WriteAllText(Path.Combine(_directory, "result.xml"), result.OuterXml);
            Assert.That(result.SelectNodes("//test-case"), Has.Count.EqualTo(1), result.OuterXml);
            Assert.That(result.SelectSingleNode("//test-case")!.Attributes!["result"]!.Value, Is.EqualTo(expected));
            Assert.That(events.Reports, Has.Some.Contains("<start-test"));
            Assert.That(events.Reports, Has.Some.Contains("<test-case"));
        }

        /// <summary>A foreign client cannot return a lease, and return succeeds only once.</summary>
        [Test]
        public void LeaseReturnRequiresOriginalOwner()
        {
            StartReady();
            Guid owner = Guid.NewGuid();
            var acquired = Call(RemoteAgentConnection.GetAgent, owner, Guid.Empty, Package().ToXml());
            Assert.That(acquired.Code, Is.EqualTo(MessageCode.CommandResult), acquired.Data);
            Guid agent = Guid.Parse(acquired.Data!);
            Assert.That(Call(RemoteAgentConnection.ReleaseAgent, Guid.NewGuid(), agent).Code, Is.EqualTo(RemoteAgentConnection.Error));
            Assert.That(Call(RemoteAgentConnection.ReleaseAgent, owner, agent).Code, Is.EqualTo(MessageCode.CommandResult));
            Assert.That(Call(RemoteAgentConnection.ReleaseAgent, owner, agent).Code, Is.EqualTo(RemoteAgentConnection.Error));
        }

        /// <summary>Closing the parent's input returns outstanding agents before exit.</summary>
        [Test]
        public void ShutdownReturnsOutstandingAgent()
        {
            StartReady();
            var acquired = Call(RemoteAgentConnection.GetAgent, Guid.NewGuid(), Guid.Empty, Package().ToXml());
            Assert.That(acquired.Code, Is.EqualTo(MessageCode.CommandResult), acquired.Data);
            _host!.StandardInput.Close();
            Assert.That(_host.WaitForExit(15000), Is.True, "Outstanding agent shutdown did not finish.");
            Assert.That(_host.ExitCode, Is.Zero, _stderr!.GetAwaiter().GetResult());
        }

        private void Start(params string[] args)
        {
            var start = new ProcessStartInfo("dotnet")
            {
                WorkingDirectory = _directory,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false
            };
            start.ArgumentList.Add(Path.Combine(_hostDirectory, "nunit-test-server.dll"));
            foreach (string arg in args)
                start.ArgumentList.Add(arg);
            _host = Process.Start(start) ?? throw new InvalidOperationException("The server could not be started.");
            _stdout = _host.StandardOutput.ReadToEndAsync();
            _stderr = _host.StandardError.ReadToEndAsync();
        }

        private void StartReady(string? allowedRoot = null)
        {
            Start(_receipt, allowedRoot ?? _tools);
            var deadline = Stopwatch.StartNew();
            while (!File.Exists(_receipt) && !_host!.HasExited && deadline.ElapsedMilliseconds < 15000)
                Thread.Sleep(20);
            Assert.That(File.Exists(_receipt), Is.True, "No readiness receipt. " + (_host!.HasExited ? _stderr!.GetAwaiter().GetResult() : string.Empty));
            var ready = XElement.Load(_receipt);
            Assert.That((int)ready.Attribute("pid")!, Is.EqualTo(_host.Id));
            Assert.That((int)ready.Attribute("agents")!, Is.EqualTo(1));
            // macOS may report /private/var for files staged through /var. Compare
            // the loaded engine's bytes rather than assuming one spelling of that alias.
            Assert.That(SHA256.HashData(File.ReadAllBytes((string)ready.Attribute("engine")!)),
                Is.EqualTo(SHA256.HashData(File.ReadAllBytes(Path.Combine(_hostDirectory, "nunit.engine.dll")))));
            _port = (int)ready.Attribute("port")!;
        }

        private TestPackage Package()
        {
            string stage = Path.Combine(_directory, "tests");
            Directory.CreateDirectory(stage);
            string assembly = typeof(NUnit.TestData.Singletons.OneTestCase).Assembly.Location;
            string framework = typeof(TestAttribute).Assembly.Location;
            string target = Path.Combine(stage, Path.GetFileName(assembly));
            File.Copy(assembly, target, true);
            File.Copy(framework, Path.Combine(stage, Path.GetFileName(framework)), true);
            var package = new TestPackage(target).SubPackages[0];
            package.AddSetting(SettingDefinitions.TargetFrameworkName.WithValue(".NETCoreApp,Version=v10.0"), true);
            package.AddSetting(SettingDefinitions.ImageTargetFrameworkName.WithValue(".NETCoreApp,Version=v10.0"), true);
            return package;
        }

        private TestEngineMessage Call(string code, Guid owner, Guid agent, string? data = null)
        {
            using var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            socket.Connect(IPAddress.Loopback, _port);
            using var connection = new RemoteAgentConnection(socket, 10000, 10000);
            connection.Send(code, RemoteAgentConnection.Request(owner, agent, data));
            return connection.Receive();
        }

        private static void CopyDirectory(string source, string destination)
        {
            Assert.That(Directory.Exists(source), Is.True, "Build the solution before running standalone integration tests: " + source);
            Directory.CreateDirectory(destination);
            foreach (string file in Directory.GetFiles(source, "*", SearchOption.AllDirectories))
            {
                string target = Path.Combine(destination, Path.GetRelativePath(source, file));
                Directory.CreateDirectory(Path.GetDirectoryName(target)!);
                File.Copy(file, target);
            }
        }

        private sealed class Events : ITestEventListener
        {
            internal ConcurrentQueue<string> Reports { get; } = new ConcurrentQueue<string>();

            public void OnTestEvent(string report) => Reports.Enqueue(report);
        }
    }
}
#endif
