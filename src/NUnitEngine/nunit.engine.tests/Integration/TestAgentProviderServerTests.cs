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
using NUnit.Engine.Runners;
using NUnit.Engine.Services;
using NUnit.Engine.TestHelpers;
using NUnit.Framework;
using NSubstitute;

namespace NUnit.Engine.Integration
{
    /// <summary>
    /// Runs controlled and standard-agent servers in child processes, including the
    /// configured provider/public engine path. No remote machine, hardware or private package is used.
    /// </summary>
    [NonParallelizable]
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
                _stdout?.GetAwaiter().GetResult();
                _stderr?.GetAwaiter().GetResult();
                _stdout?.Dispose();
                _stderr?.Dispose();
                _stdout = null;
                _stderr = null;
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

        [Test]
        public void RepeatedCapabilityQueriesKeepConnectionLifetimesIndependent()
        {
            var probe = StartServer();
            var package = ConfiguredPackage("supported.dll");
            var provider = new RemoteProcessAgentProvider();

            // Fast loopback connects can complete synchronously. Their runtime-owned
            // completion state must remain usable by subsequent requests.
            for (int i = 0; i < 256; i++)
                Assert.That(provider.IsAgentAvailable(package), Is.True);

            Assert.That(probe.ActiveAgentCount, Is.Zero);
        }

        [Test]
        public void ConfiguredProviderQualifiesMetadataWithoutAcquisition()
        {
            var probe = StartServer();
            var package = ConfiguredPackage("supported.dll");
            var provider = new RemoteProcessAgentProvider();

            Assert.That(provider.IsAgentAvailable(package), Is.True);
            var agents = provider.GetAgentsForPackage(package);
            Assert.That(agents, Has.Count.EqualTo(1));
            Assert.That(agents[0].AgentName, Is.EqualTo("office::Loopback.TestAgent"));
            Assert.That(agents[0].AgentType, Is.EqualTo(TestAgentType.RemoteProcess));
            Assert.That(provider.AvailableAgents[0].AgentName, Is.EqualTo(agents[0].AgentName));
            Assert.That(probe.ActiveAgentCount, Is.Zero);
        }

        [Test]
        public void ConfiguredProviderRetainsOriginalEndpointUntilRelease()
        {
            var probe = StartServer();
            var package = ConfiguredPackage("supported.dll");
            var provider = new RemoteProcessAgentProvider();
            var agent = provider.GetAgent(package);
            var unresponsive = new TcpListener(IPAddress.Loopback, 0);
            unresponsive.Start();
            try
            {
                WriteDefinition((IPEndPoint)unresponsive.LocalEndpoint, 100);
                Assert.That(() => provider.IsAgentAvailable(package), Throws.Exception);
                provider.ReturnAgent(agent);
                Assert.That(probe.ActiveAgentCount, Is.Zero);
            }
            finally
            {
                unresponsive.Stop();
            }
        }

        [Test]
        public void ConfiguredProviderRetainsOwnershipAfterFailedRelease()
        {
            var probe = StartServer();
            var package = ConfiguredPackage("retry.dll");
            var provider = new RemoteProcessAgentProvider();
            var agent = provider.GetAgent(package);

            Assert.That(() => provider.ReturnAgent(agent), Throws.TypeOf<NUnitEngineException>());
            Assert.That(probe.ActiveAgentCount, Is.EqualTo(1));
            provider.ReturnAgent(agent);
            Assert.That(probe.ActiveAgentCount, Is.Zero);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void ConfiguredProviderStopOvertakesExecution(bool forced)
        {
            var probe = StartServer();
            var package = ConfiguredPackage("supported.dll");
            var provider = new RemoteProcessAgentProvider();
            var agent = provider.GetAgent(package);
            using (var runner = agent.CreateRunner(package))
            {
                using var started = new ManualResetEventSlim();
                var result = runner.RunAsync(new Events(started), new TestFilter("<filter><test>blocked</test></filter>"));
                Assert.That(started.Wait(10000), Is.True);
                if (forced)
                    runner.ForcedStop();
                else
                    runner.RequestStop();
                Assert.That(result.Wait(10000), Is.True);
                Assert.That(result.EngineResult.Xml.Attributes!["label"]!.Value, Is.EqualTo("Cancelled"));
            }
            provider.ReturnAgent(agent);
            Assert.That(probe.ActiveAgentCount, Is.Zero);
        }

        [Test]
        public void FailedAcquisitionCannotAutomaticallyAllocateAnotherAgent()
        {
            var probe = StartServer();
            var package = ConfiguredPackage("unsupported.dll");
            var provider = new RemoteProcessAgentProvider();
            Assert.That(() => provider.GetAgent(package), Throws.TypeOf<NUnitEngineException>());
            package = ConfiguredPackage("supported.dll");
            Assert.That(() => provider.GetAgent(package), Throws.InvalidOperationException.With.Message.Contains("unresolved"));
            Assert.That(probe.ActiveAgentCount, Is.Zero);
        }

        [TestCase("unchanged")]
        [TestCase("timeout")]
        [TestCase("alias")]
        [TestCase("file")]
        public void LostAcquisitionReplyCannotBeRetriedByChangingDefinition(string change)
        {
            var probe = StartServer("fake-drop-acquisition");
            var package = ConfiguredPackage("supported.dll");
            var provider = new RemoteProcessAgentProvider();
            Assert.That(() => provider.GetAgent(package), Throws.Exception);
            Assert.That(probe.ActiveAgentCount, Is.EqualTo(1), "The server allocated a lease before losing the reply.");

            string path = (string)package.Settings.GetSetting(RemoteAgentSettings.ServerDefinition.Name)!;
            var definition = XElement.Load(path);
            if (change == "timeout")
                definition.SetAttributeValue("operationTimeout", 2000);
            if (change == "alias" || change == "file")
                definition.SetAttributeValue("name", "renamed");
            if (change == "file")
                path = Path.Combine(_directory, "other-server.xml");
            File.WriteAllText(path, definition.ToString());
            package.ApplySetting(RemoteAgentSettings.ServerDefinition.WithValue(path), true);

            Assert.That(() => provider.GetAgent(package), Throws.InvalidOperationException.With.Message.Contains("unresolved"));
            Assert.That(probe.ActiveAgentCount, Is.EqualTo(1), "No second allocation may hide the unresolved lease.");
            // The controlled server owns and releases its unresolved lease during teardown.
        }

        [Test]
        public void RejectsAmbiguousServerNamesBeforeContactingAnotherEndpoint()
        {
            var probe = StartServer();
            var package = ConfiguredPackage("supported.dll");
            var provider = new RemoteProcessAgentProvider();
            Assert.That(provider.IsAgentAvailable(package), Is.True);
            string path = (string)package.Settings.GetSetting(RemoteAgentSettings.ServerDefinition.Name)!;
            string other = Path.Combine(_directory, "other-server.xml");
            File.Copy(path, other);
            package.ApplySetting(RemoteAgentSettings.ServerDefinition.WithValue(other), true);

            Assert.That(() => provider.IsAgentAvailable(package), Throws.InvalidOperationException.With.Message.Contains("unique"));
            Assert.That(probe.ActiveAgentCount, Is.Zero);
        }

        [Test]
        public void RejectsForeignAgentNameWithoutFencingValidAcquisition()
        {
            var probe = StartServer();
            var package = ConfiguredPackage("supported.dll");
            var provider = new RemoteProcessAgentProvider();
            package.AddSetting(SettingDefinitions.RequestedAgentName.WithValue("elsewhere::Loopback.TestAgent"), true);
            Assert.That(() => provider.GetAgent(package), Throws.ArgumentException.With.Message.Contains("another server"));
            Assert.That(probe.ActiveAgentCount, Is.Zero);
            package.ApplySetting(SettingDefinitions.RequestedAgentName.WithValue("office::Loopback.TestAgent"), true);
            var agent = provider.GetAgent(package);
            provider.ReturnAgent(agent);
            Assert.That(probe.ActiveAgentCount, Is.Zero);
        }

        [Test]
        public void ConfiguredProviderCompletesAsyncResultAfterServerLoss()
        {
            StartServer();
            var package = ConfiguredPackage("supported.dll");
            var provider = new RemoteProcessAgentProvider();
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
            Assert.That(() => provider.ReturnAgent(agent), Throws.Exception);
            // The controlled provider has no child agent process. Loss is not a release acknowledgement.
        }

        [TestCase("NUnit.TestData.Singletons.OneTestCase.TestCase", "Passed")]
        [TestCase("NUnit.TestData.Assemblies.MockTestFixture.FailingTest", "Failed")]
        public void PublicEngineUsesConfiguredRemoteProvider(string name, string expected)
        {
#if NETFRAMEWORK
            string tools = Path.Combine(TestContext.CurrentContext.TestDirectory, "provider-test-tools", "net462", "net462");
            string runtime = ".NETFramework,Version=v4.6.2";
#else
            string tools = Path.Combine(TestContext.CurrentContext.TestDirectory, "provider-test-tools", "net10", "net8.0");
            string runtime = ".NETCoreApp,Version=v10.0";
#endif
            var probe = StartServer(tools);
            TestPackage package = StagePackage(runtime);
            package.AddSetting(RemoteAgentSettings.ServerDefinition.WithValue(WriteDefinition(_endpoint)), true);
            package.AddSetting(RemoteAgentSettings.AgentType.WithValue("RemoteProcess"), true);
            string agentName = new RemoteProcessAgentProvider().GetAgentsForPackage(package)[0].AgentName;
            package.AddSetting(SettingDefinitions.RequestedAgentName.WithValue(agentName), true);
            string definition = (string)package.Settings.GetSetting(RemoteAgentSettings.ServerDefinition.Name)!;
            var filter = new TestFilter(new XElement("filter", new XElement("test", name)).ToString());
            using var engine = new TestEngine { WorkDirectory = _directory, InternalTraceLevel = InternalTraceLevel.Off };
            using (var runner = engine.GetRunner(package))
            {
                // MasterTestRunner caches Load/Explore results. Explore first so this assertion
                // exercises remote discovery rather than the cached summary returned by Load.
                var discovery = runner.Explore(filter);
                Assert.That(discovery.SelectSingleNode("//failure"), Is.Null, discovery.OuterXml);
                Assert.That(discovery.SelectNodes("//test-case"), Has.Count.EqualTo(1), discovery.OuterXml);
                Assert.That(runner.CountTestCases(filter), Is.EqualTo(1));
                var events = new Events();
                var result = runner.Run(events, filter);
                Assert.That(result.SelectSingleNode("//test-case")!.Attributes!["result"]!.Value, Is.EqualTo(expected), result.OuterXml);
                Assert.That(events.Reports, Has.Some.Contains("<start-test"));
                Assert.That(events.Reports, Has.Some.Contains("<test-case"));
                Assert.That(package.Settings.GetSetting(RemoteAgentSettings.ServerDefinition.Name), Is.EqualTo(definition));
                Assert.That(package.Settings.GetValueOrDefault(SettingDefinitions.RequestedAgentName), Is.EqualTo(agentName));
                Assert.That(package.Settings.GetSetting(RemoteAgentSettings.AgentType.Name), Is.EqualTo("RemoteProcess"));
            }
            Assert.That(probe.ActiveAgentCount, Is.Zero);
            var receipts = Directory.GetFiles(_directory, "agent-*.xml");
            Assert.That(receipts, Has.Length.EqualTo(1));
            var receipt = XElement.Load(receipts[0]);
            Assert.That((bool)receipt.Attribute("exited")!, Is.True);
            Assert.That((int)receipt.Attribute("exitCode")!, Is.Zero);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void PublicEngineDistinguishesRemoteFromLocalRuntimeAvailability(bool remote)
        {
#if NETFRAMEWORK
            string tools = Path.Combine(TestContext.CurrentContext.TestDirectory, "provider-test-tools", "net462", "net462");
            string runtime = ".NETFramework,Version=v4.6.2";
            string tfm = "net462";
#else
            string tools = Path.Combine(TestContext.CurrentContext.TestDirectory, "provider-test-tools", "net10", "net8.0");
            string runtime = ".NETCoreApp,Version=v10.0";
            string tfm = "net10.0";
#endif
            var probe = StartServer(tools);
            var package = StagePackage(runtime);
            package.AddSetting(SettingDefinitions.RequestedRuntimeFramework.WithValue(tfm), true);
            if (remote)
                package.AddSetting(RemoteAgentSettings.ServerDefinition.WithValue(WriteDefinition(_endpoint)), true);
            var filter = new TestFilter("<filter><test>NUnit.TestData.Singletons.OneTestCase.TestCase</test></filter>");
            using var engine = CreateEngineWithEmptyRuntimeCatalog(out var coordinatorRuntime);
            Assert.That(coordinatorRuntime.AvailableRuntimes, Is.Empty);
            Assert.That(coordinatorRuntime.IsAvailable(tfm, false), Is.False);
            if (!remote)
            {
                Assert.That(() => engine.GetRunner(package),
                    Throws.TypeOf<NUnitEngineException>().With.Message.Contains("unknown or not available"));
                Assert.That(probe.ActiveAgentCount, Is.Zero);
                Assert.That(Directory.GetFiles(_directory, "agent-*.xml"), Is.Empty);
                return;
            }
            using (var runner = engine.GetRunner(package))
            {
                var discovery = runner.Explore(filter);
                Assert.That(discovery.SelectNodes("//test-case"), Has.Count.EqualTo(1), discovery.OuterXml);
                var result = runner.Run(new Events(), filter);
                Assert.That(result.SelectSingleNode("//test-case")!.Attributes!["result"]!.Value, Is.EqualTo("Passed"), result.OuterXml);
            }
            Assert.That(probe.ActiveAgentCount, Is.Zero);
            var receipts = Directory.GetFiles(_directory, "agent-*.xml");
            Assert.That(receipts, Has.Length.EqualTo(remote ? 1 : 0));
            if (remote)
            {
                var receipt = XElement.Load(receipts[0]);
                Assert.That((bool)receipt.Attribute("exited")!, Is.True);
                Assert.That((int)receipt.Attribute("exitCode")!, Is.Zero);
            }
        }

        [Test]
        public void BackendRejectsUnavailableRequestedRuntimeWithoutFallbackOrAllocation()
        {
#if NETFRAMEWORK
            string tools = Path.Combine(TestContext.CurrentContext.TestDirectory, "provider-test-tools", "net462", "net462");
            string runtime = ".NETFramework,Version=v4.6.2";
#else
            string tools = Path.Combine(TestContext.CurrentContext.TestDirectory, "provider-test-tools", "net10", "net8.0");
            string runtime = ".NETCoreApp,Version=v10.0";
#endif
            var probe = StartServer(tools);
            var package = StagePackage(runtime);
            package.AddSetting(RemoteAgentSettings.ServerDefinition.WithValue(WriteDefinition(_endpoint)), true);
            package.AddSetting(SettingDefinitions.RequestedRuntimeFramework.WithValue("net99.0"), true);
            using var engine = CreateEngineWithEmptyRuntimeCatalog(out _);
            using var runner = engine.GetRunner(package);

            // ProcessRunner reports infrastructure errors as failed discovery XML.
            var discovery = runner.Explore(TestFilter.Empty);
            Assert.That(discovery.SelectSingleNode("//failure/message")?.InnerText, Does.Contain("No agent available"), discovery.OuterXml);
            Assert.That(discovery.SelectNodes("//test-case"), Is.Empty, discovery.OuterXml);
            Assert.That(package.Settings.GetValueOrDefault(SettingDefinitions.RequestedFrameworkName), Is.EqualTo(".NETCoreApp,Version=v99.0"));
            Assert.That(probe.ActiveAgentCount, Is.Zero);
            Assert.That(Directory.GetFiles(_directory, "agent-*.xml"), Is.Empty);
        }

        private TestEngine CreateEngineWithEmptyRuntimeCatalog(out EmptyRuntimeCatalog runtime)
        {
            var engine = new TestEngine { WorkDirectory = _directory, InternalTraceLevel = InternalTraceLevel.Off };
            runtime = new EmptyRuntimeCatalog();
            engine.Services.Add(new TestFilterService());
            engine.Services.Add(new ExtensionService());
            engine.Services.Add(new ProjectService());
            engine.Services.Add(runtime);
            engine.Services.Add(new TestAgentService());
            engine.Services.Add(new ResultService());
            engine.Services.Add(new TestRunnerFactory());
            engine.Initialize();
            return engine;
        }

        /// <summary>Models a coordinator with no installed target runtimes without altering the host.</summary>
        private sealed class EmptyRuntimeCatalog : RuntimeFrameworkService
        {
            public override void StartService()
            {
                if (ServiceContext is null)
                    throw new InvalidOperationException("The test runtime service requires a service context.");
                Status = ServiceStatus.Started;
            }
        }

        private TestPackage ConfiguredPackage(string path)
        {
            var package = new TestPackage(path);
            package.AddSetting(RemoteAgentSettings.ServerDefinition.WithValue(WriteDefinition(_endpoint)), true);
            return package;
        }

        private string WriteDefinition(IPEndPoint endpoint, int operationTimeout = 30000)
        {
            string path = Path.Combine(_directory, "server.xml");
            File.WriteAllText(path, new XElement("nunit-remote-server", new XAttribute("name", "office"),
                new XAttribute("endpoint", "tcp://" + endpoint), new XAttribute("operationTimeout", operationTimeout)).ToString());
            return path;
        }

        private TestPackage StagePackage(string runtime)
        {
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
            return package;
        }

        [TestCase("NUnit.TestData.Singletons.OneTestCase.TestCase", "Passed", false)]
        [TestCase("NUnit.TestData.Assemblies.MockTestFixture.FailingTest", "Failed", false)]
        [TestCase("NUnit.TestData.Singletons.OneTestCase.TestCase", "Passed", true)]
        [TestCase("NUnit.TestData.Assemblies.MockTestFixture.FailingTest", "Failed", true)]
        public void StandardAgentDiscoversAndRunsAStagedPackage(string name, string expected, bool throughAgency)
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
            using var agency = new TestAgentService(new ITestAgentProvider[] { provider });
            var services = Substitute.For<IServiceLocator>();
            services.GetService<TestAgentService>().Returns(agency);
            var agent = throughAgency ? null : provider.GetAgent(package);
            Guid agentId = default;
            try
            {
                using var runner = throughAgency
                    ? (ITestEngineRunner)new ProcessRunner(services, package)
                    : agent!.CreateRunner(package);
                runner.Load();
                var receipts = Directory.GetFiles(_directory, "agent-*.xml");
                Assert.That(receipts, Has.Length.EqualTo(1));
                agentId = (Guid)XElement.Load(receipts[0]).Attribute("id")!;
                if (throughAgency)
                {
                    Assert.That(agency.IsAgentActive(agentId), Is.True);
                    Assert.That(agency._agentStore.TryGetAgentProcess(agentId, out _), Is.False);
                }
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
                if (agent is not null)
                    provider.ReturnAgent(agent);
            }
            Assert.That(provider.ActiveAgentCount, Is.Zero);
            if (throughAgency)
                Assert.That(agency.IsAgentActive(agentId), Is.False);
            var receipt = XElement.Load(Path.Combine(_directory, "agent-" + agentId + ".xml"));
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
