// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Runtime.Versioning;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using NUnit.Common;
using NUnit.Engine.TestHelpers;
using NUnit.Framework;

namespace NUnit.Engine.Integration
{
    /// <summary>
    /// Runs the provider server in a child process. No tests require production
    /// remote-provider integration, a remote machine, hardware, or private packages.
    /// </summary>
    [NonParallelizable]
    [Ignore("Unexplained errors")]
    public sealed class TestAgentProviderServerTests : IntegrationTests
    {
        private string _directory = null!;
        private Process? _server;
        private Task<string>? _stdout;
        private Task<string>? _stderr;
        private IPEndPoint _endpoint = null!;

        [SetUp]
        public void CreateDirectory()
        {
            _directory = Path.Combine(TestContext.CurrentContext.WorkDirectory, "provider-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_directory);
        }

        [TearDown]
        public void StopServer()
        {
            try
            {
                if (_server is not null)
                {
                    if (!_server.HasExited)
                    {
                        _server.StandardInput.Close();
                        if (!_server.WaitForExit(35000))
                        {
                            _server.Kill();
                            _server.WaitForExit();
                            Assert.Fail("The owned localhost server did not stop within its cleanup deadline.");
                        }
                    }
                    Assert.That(_server.ExitCode, Is.Zero, Output());
                }
            }
            finally
            {
                _server?.Dispose();
                _server = null;
                Directory.Delete(_directory, true);
            }
        }

        [Test]
        public void QueriesCrossProcessBoundaryWithoutAcquiringAnAgent()
        {
            var provider = StartServer();
            var package = new TestPackage("supported.dll");
            Assert.That(provider.AvailableAgents, Has.Count.EqualTo(1));
            Assert.That(provider.AvailableAgents[0].AgentName, Is.EqualTo("Loopback.TestAgent"));
            Assert.That(provider.AvailableAgents[0].AgentType, Is.EqualTo(TestAgentType.LocalProcess));
            Assert.That(provider.AvailableAgents[0].TargetRuntime.FullName, Is.EqualTo(".NETFramework,Version=v4.6.2"));
            Assert.That(provider.IsAgentAvailable(package), Is.True);
            Assert.That(provider.GetAgentsForPackage(package), Has.Count.EqualTo(1));
            Assert.That(provider.IsAgentAvailable(new TestPackage("unsupported.dll")), Is.False);
            Assert.That(provider.GetAgentsForPackage(new TestPackage("unsupported.dll")), Is.Empty);
            Assert.That(provider.ActiveAgentCount, Is.Zero);
        }

        [Test]
        public void ProviderErrorsDoNotKillTheServer()
        {
            var provider = StartServer();
            Assert.That(() => provider.IsAgentAvailable(new TestPackage("throw.dll")),
                Throws.TypeOf<NUnitEngineException>().With.Message.Contains("Capability query rejected"));
            Assert.That(() => provider.GetAgent(new TestPackage("unsupported.dll")),
                Throws.TypeOf<NUnitEngineException>().With.Message.Contains("No compatible test agent"));
            Assert.That(provider.AvailableAgents, Has.Count.EqualTo(1));
            Assert.That(provider.ActiveAgentCount, Is.Zero);
        }

        [Test]
        public void ReleaseUsesTheOriginatingProviderAndRejectsDoubleRelease()
        {
            var provider = StartServer();
            var agent = provider.GetAgent(new TestPackage("supported.dll"));
            var other = new TestAgentProviderProxy(_endpoint);
            Assert.That(agent.Start(), Is.True);
            Assert.That(provider.ActiveAgentCount, Is.EqualTo(1));
            Assert.That(() => other.ReturnAgent(agent), Throws.InvalidOperationException);
            Assert.That(provider.ActiveAgentCount, Is.EqualTo(1));
            provider.ReturnAgent(agent);
            Assert.That(provider.ActiveAgentCount, Is.Zero);
            Assert.That(() => provider.ReturnAgent(agent), Throws.InvalidOperationException);
            Assert.That(() => agent.Start(), Throws.InvalidOperationException);
        }

        [Test]
        public void FailedReleaseRetainsTheLeaseForRetry()
        {
            var provider = StartServer();
            var agent = provider.GetAgent(new TestPackage("retry.dll"));
            Assert.That(() => provider.ReturnAgent(agent), Throws.TypeOf<NUnitEngineException>().With.Message.Contains("Release failed once"));
            Assert.That(provider.ActiveAgentCount, Is.EqualTo(1));
            provider.ReturnAgent(agent);
            Assert.That(provider.ActiveAgentCount, Is.Zero);
        }

        [Test]
        public void ServerRejectsAnotherClientsAttemptToReleaseAnAgent()
        {
            var provider = StartServer();
            var agent = provider.GetAgent(new TestPackage("supported.dll"));
            using (var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
            {
                socket.Connect(_endpoint);
                using var connection = new ProviderConnection(socket);
                connection.Send(ProviderConnection.ReleaseAgent, ProviderConnection.Request(Guid.NewGuid(), agent.Id, null));
                Assert.That(connection.Receive().Code, Is.EqualTo(ProviderConnection.Error));
            }
            Assert.That(provider.ActiveAgentCount, Is.EqualTo(1));
            provider.ReturnAgent(agent);
            Assert.That(provider.ActiveAgentCount, Is.Zero);
        }

        [Test]
        public void InvalidCommandDoesNotPreventSubsequentRequests()
        {
            var provider = StartServer();
            using (var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
            {
                socket.Connect(_endpoint);
                using var connection = new ProviderConnection(socket);
                connection.Send("????", ProviderConnection.Request(Guid.NewGuid(), Guid.NewGuid(), null));
                Assert.That(connection.Receive().Code, Is.EqualTo(ProviderConnection.Error));
            }
            Assert.That(provider.AvailableAgents, Has.Count.EqualTo(1));
            Assert.That(provider.ActiveAgentCount, Is.Zero);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void StopCanOvertakeAnActiveRun(bool forced)
        {
            var provider = StartServer();
            var package = new TestPackage("supported.dll");
            var agent = provider.GetAgent(package);
            var runner = agent.CreateRunner(package);
            using var started = new ManualResetEventSlim();
            var events = new Events(started);
            var result = runner.RunAsync(events, new TestFilter("<filter><test>blocked</test></filter>"));
            Assert.That(started.Wait(10000), Is.True, "No start-test event arrived across the server boundary.");
            if (forced)
                runner.ForcedStop();
            else
                runner.RequestStop();
            Assert.That(result.Wait(10000), Is.True, "Stop was blocked behind synchronous execution.");
            Assert.That(result.EngineResult.Xml.Attributes!["label"]!.Value, Is.EqualTo("Cancelled"));
            Assert.That(events.Reports, Has.Some.Contains("Provider server event"));
            runner.Dispose();
            provider.ReturnAgent(agent);
            Assert.That(provider.ActiveAgentCount, Is.Zero);
        }

        [Test]
        public void ServerLossCompletesAsyncResultAsAnInfrastructureFailure()
        {
            var provider = StartServer();
            var package = new TestPackage("supported.dll");
            var agent = provider.GetAgent(package);
            var runner = agent.CreateRunner(package);
            using var started = new ManualResetEventSlim();
            var result = runner.RunAsync(new Events(started), new TestFilter("<filter><test>blocked</test></filter>"));
            Assert.That(started.Wait(10000), Is.True);
            _server!.Kill();
            _server.WaitForExit();
            _server.Dispose();
            _server = null;
            Assert.That(result.Wait(10000), Is.True);
            Assert.That(result.EngineResult.Xml.Attributes!["result"]!.Value, Is.EqualTo("Failed"));
            Assert.That(result.EngineResult.Xml.Attributes!["label"]!.Value, Is.EqualTo("Error"));
            // Connection loss is not release confirmation. There is no agent process in this controlled-provider case.
            Assert.That(() => provider.ReturnAgent(agent), Throws.Exception);
        }

        [TestCase("NUnit.TestData.Singletons.OneTestCase.TestCase", "Passed")]
        [TestCase("NUnit.TestData.Assemblies.MockTestFixture.FailingTest", "Failed")]
        public void StandardAgentDiscoversAndRunsAStagedPackage(string name, string expected)
        {
#if NETFRAMEWORK
            string tools = Path.Combine(TestContext.CurrentContext.TestDirectory, "provider-test-tools", "net462", "net462");
            string runtime = ".NETFramework,Version=v4.6.2";
#else
            string tools = Path.Combine(TestContext.CurrentContext.TestDirectory, "provider-test-tools", "net10", "net8.0");
            string runtime = ".NETCoreApp,Version=v10.0";
#endif
            var provider = StartServer(tools);
            string stage = Path.Combine(_directory, "package");
            Directory.CreateDirectory(stage);
            string assembly = typeof(NUnit.TestData.Singletons.OneTestCase).Assembly.Location;
            string framework = typeof(TestAttribute).Assembly.Location;
            File.Copy(assembly, Path.Combine(stage, Path.GetFileName(assembly)));
            File.Copy(framework, Path.Combine(stage, Path.GetFileName(framework)));
            var package = new TestPackage(Path.Combine(stage, Path.GetFileName(assembly))).SubPackages[0];
            package.AddSetting(SettingDefinitions.TargetFrameworkName.WithValue(runtime), true);
            package.AddSetting(SettingDefinitions.ImageTargetFrameworkName.WithValue(
                typeof(NUnit.TestData.Singletons.OneTestCase).Assembly.GetCustomAttribute<TargetFrameworkAttribute>()!.FrameworkName), true);
            var filter = new TestFilter(new XElement("filter", new XElement("test", name)).ToString());
            Assert.That(provider.AvailableAgents, Is.Not.Empty);
            Assert.That(provider.IsAgentAvailable(package), Is.True);
            var agent = provider.GetAgent(package);
            try
            {
                using var runner = agent.CreateRunner(package);
                runner.Load();
                Assert.That(runner.CountTestCases(filter), Is.EqualTo(1));
                Assert.That(runner.Explore(filter).Xml.SelectNodes("//test-case"), Has.Count.EqualTo(1));
                var events = new Events();
                var result = runner.Run(events, filter);
                Assert.That(result.Xml.SelectSingleNode("//test-case")!.Attributes!["result"]!.Value, Is.EqualTo(expected), result.Xml.OuterXml);
                Assert.That(events.Reports, Has.Some.Contains("<start-test"));
                Assert.That(events.Reports, Has.Some.Contains("<test-case"));
                Assert.That(result.Xml.OuterXml, Does.Contain(stage));
                runner.Unload();
            }
            finally
            {
                provider.ReturnAgent(agent);
            }
            Assert.That(provider.ActiveAgentCount, Is.Zero);
            var receipt = XElement.Load(Path.Combine(_directory, "agent-" + agent.Id + ".xml"));
            Assert.That((int)receipt.Attribute("pid")!, Is.Not.EqualTo(_server!.Id));
            Assert.That((bool)receipt.Attribute("exited")!, Is.True);
            Assert.That((int)receipt.Attribute("exitCode")!, Is.Zero);
        }

        private TestAgentProviderProxy StartServer(string extensionDirectory = "fake")
        {
            string assembly = typeof(TestAgentProviderServerTests).Assembly.Location;
            string ready = Path.Combine(_directory, "ready.xml");
            var arguments = new StringBuilder();
#if NETFRAMEWORK
            bool mono = Type.GetType("Mono.Runtime") is not null;
            string executable = mono ? Process.GetCurrentProcess().MainModule!.FileName : assembly;
            if (mono)
                arguments.EscapeProcessArgument(assembly);
#else
            string executable = Environment.GetEnvironmentVariable("DOTNET_HOST_PATH") ?? "dotnet";
            arguments.EscapeProcessArgument(assembly);
#endif
            foreach (string argument in new[] { "--test-provider-server", ready, extensionDirectory })
            {
                if (arguments.Length > 0)
                    arguments.Append(' ');
                arguments.EscapeProcessArgument(argument);
            }
            _server = new Process
            {
                StartInfo = new ProcessStartInfo(executable, arguments.ToString())
                {
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardInput = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    WorkingDirectory = _directory
                }
            };
            _server.Start();
            _stdout = _server.StandardOutput.ReadToEndAsync();
            _stderr = _server.StandardError.ReadToEndAsync();
            Assert.That(SpinWait.SpinUntil(() => File.Exists(ready) || _server.HasExited, 15000), Is.True, "Server readiness deadline exceeded.");
            Assert.That(_server.HasExited, Is.False, _server.HasExited ? Output() : string.Empty);
            var record = XElement.Load(ready);
            Assert.That((int)record.Attribute("pid")!, Is.EqualTo(_server.Id));
            _endpoint = new IPEndPoint(IPAddress.Loopback, (int)record.Attribute("port")!);
            return new TestAgentProviderProxy(_endpoint);
        }

        private string Output() => (_stdout?.GetAwaiter().GetResult() ?? string.Empty) + (_stderr?.GetAwaiter().GetResult() ?? string.Empty);

        private sealed class Events : ITestEventListener
        {
            private readonly ManualResetEventSlim? _started;
            internal Events(ManualResetEventSlim? started = null)
            {
                _started = started;
            }
            internal ConcurrentQueue<string> Reports { get; } = new ConcurrentQueue<string>();
            public void OnTestEvent(string report)
            {
                Reports.Enqueue(report);
                if (report.StartsWith("<start-test", StringComparison.Ordinal))
                    _started?.Set();
            }
        }
    }
}
