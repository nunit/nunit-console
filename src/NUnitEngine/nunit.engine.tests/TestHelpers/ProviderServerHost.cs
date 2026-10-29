// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.Versioning;
using System.Threading;
using System.Xml.Linq;
using NUnit.Common;
using NUnit.Engine.Services;
using NSubstitute;

namespace NUnit.Engine.TestHelpers
{
    /// <summary>
    /// Starts an explicitly running test server in a separate process. Standard input
    /// owns its lifetime; the parent closes it only after releasing all acquired agents.
    /// No production service registration or public package pin changes are required.
    /// </summary>
    internal static class ProviderServerHost
    {
        internal static int Run(string[] args)
        {
            try
            {
                using var engine = new TestEngine { InternalTraceLevel = InternalTraceLevel.Off };
                ITestAgentProvider provider;
                if (args[2] == "fake" || args[2] == "fake-drop-acquisition")
                {
                    var fake = new ControlledProvider();
                    provider = fake;
                }
                else
                {
                    engine.Initialize();
                    var extensions = engine.Services.GetService<ExtensionService>();
                    string launcher = Directory.GetFiles(args[2], "nunit-agent-launcher-*.dll").Single();
                    File.WriteAllText(Path.Combine(Environment.CurrentDirectory, "test-server.addins"), launcher);
                    extensions.FindExtensionAssemblies(Environment.CurrentDirectory);
                    extensions.InstallExtensions();
                    var agency = engine.Services.GetService<TestAgentService>();
                    provider = new ObservedProvider(agency, Path.GetDirectoryName(args[1])!);
                }

                using var server = new TestAgentProviderServer(provider, args[2] == "fake-drop-acquisition");
                server.Start();
                using (var process = Process.GetCurrentProcess())
                    File.WriteAllText(args[1] + ".tmp", new XElement("ready", new XAttribute("port", server.EndPoint.Port),
                        new XAttribute("pid", process.Id)).ToString());
                File.Move(args[1] + ".tmp", args[1]);
                Console.ReadLine();
                return 0;
            }
            catch (Exception exception)
            {
                Console.Error.WriteLine(exception);
                return 1;
            }
        }

        /// <summary>
        /// Records native process identity and waits for exit after the agency releases
        /// a real agent. This is test evidence, not an additional provider API requirement.
        /// </summary>
        private sealed class ObservedProvider : ITestAgentProvider
        {
            private readonly TestAgentService _agency;
            private readonly string _directory;
            private readonly Dictionary<Guid, Process> _processes = new Dictionary<Guid, Process>();

            internal ObservedProvider(TestAgentService agency, string directory)
            {
                _agency = agency;
                _directory = directory;
            }

            public bool IsAgentAvailable(TestPackage package) => _agency.IsAgentAvailable(package);

            public IList<TestAgentInfo> AvailableAgents => _agency.AvailableAgents;

            public IList<TestAgentInfo> GetAgentsForPackage(TestPackage package) => _agency.GetAgentsForPackage(package);

            public ITestAgent GetAgent(TestPackage package)
            {
                var agent = _agency.GetAgent(package);
                if (!_agency._agentStore.IsAgentActive(agent.Id, out var process))
                    throw new InvalidOperationException("The acquired test agent has no active native process.");
                _processes.Add(agent.Id, process);
                var record = new XElement("agent", new XAttribute("id", agent.Id),
                    new XAttribute("pid", process.Id), new XAttribute("start", process.StartTime.ToUniversalTime().Ticks),
                    new XAttribute("exited", false));
                File.WriteAllText(RecordPath(agent), record.ToString());
                return agent;
            }

            public void ReturnAgent(ITestAgent agent)
            {
                var record = XElement.Load(RecordPath(agent));
                var process = _processes[agent.Id];
                _agency.ReturnAgent(agent);
                if (!process.WaitForExit(10000))
                    throw new TimeoutException("The released test agent process did not exit.");
                record.SetAttributeValue("exitCode", process.ExitCode);
                record.SetAttributeValue("exited", true);
                File.WriteAllText(RecordPath(agent), record.ToString());
                _processes.Remove(agent.Id);
            }

            private string RecordPath(ITestAgent agent) => Path.Combine(_directory, "agent-" + agent.Id + ".xml");
        }

        /// <summary>
        /// A deterministic provider for transport faults and cancellation, independent
        /// of the release cadence of pluggable agent packages. Real-agent cases use TestAgentService.
        /// </summary>
        private sealed class ControlledProvider : ITestAgentProvider
        {
            private readonly Dictionary<Guid, bool> _releaseFailures = new Dictionary<Guid, bool>();
            private readonly Dictionary<Guid, ManualResetEventSlim> _stopSignals = new Dictionary<Guid, ManualResetEventSlim>();

            public IList<TestAgentInfo> AvailableAgents => new List<TestAgentInfo>
            {
                new TestAgentInfo("Loopback.TestAgent", TestAgentType.LocalProcess,
                    new FrameworkName(".NETFramework,Version=v4.6.2"))
            };

            public IList<TestAgentInfo> GetAgentsForPackage(TestPackage package) =>
                IsAgentAvailable(package) ? AvailableAgents : new List<TestAgentInfo>();

            public bool IsAgentAvailable(TestPackage package)
            {
                string path = PackagePath(package);
                if (path.Contains("throw"))
                    throw new ArgumentException("Capability query rejected the supplied package.");
                return !path.Contains("unsupported");
            }

            public ITestAgent GetAgent(TestPackage package)
            {
                if (!IsAgentAvailable(package))
                    throw new InvalidOperationException("No compatible test agent.");
                var runner = Substitute.For<ITestEngineRunner>();
                var stopped = new ManualResetEventSlim();
                runner.Load().Returns(new TestEngineResult("<test-suite type='Assembly' result='Passed'/>"));
                runner.Explore(Arg.Any<TestFilter>()).Returns(new TestEngineResult("<test-suite type='Assembly'><test-case fullname='probe'/></test-suite>"));
                runner.CountTestCases(Arg.Any<TestFilter>()).Returns(1);
                runner.When(value => value.RequestStop()).Do(_ => stopped.Set());
                runner.When(value => value.ForcedStop()).Do(_ => stopped.Set());
                runner.Run(Arg.Any<ITestEventListener>(), Arg.Any<TestFilter>()).Returns(call =>
                {
                    var listener = call.Arg<ITestEventListener>();
                    var filter = call.Arg<TestFilter>();
                    listener.OnTestEvent("<start-test id='probe' fullname='probe'/>");
                    if (filter.Text.Contains("blocked") && !stopped.Wait(15000))
                        throw new TimeoutException("The client did not send a stop request.");
                    bool cancelled = stopped.IsSet;
                    listener.OnTestEvent("<test-output>Provider server event</test-output>");
                    listener.OnTestEvent("<test-case id='probe' fullname='probe' result='" + (cancelled ? "Failed" : "Passed") + "'/>");
                    return new TestEngineResult(new XElement("test-suite", new XAttribute("type", "Assembly"),
                        new XAttribute("result", cancelled ? "Failed" : "Passed"),
                        new XAttribute("label", cancelled ? "Cancelled" : string.Empty)).ToString());
                });

                var agent = Substitute.For<ITestAgent>();
                agent.Id.Returns(Guid.NewGuid());
                agent.Start().Returns(true);
                agent.CreateRunner(Arg.Any<TestPackage>()).Returns(runner);
                agent.When(value => value.Stop()).Do(_ => stopped.Set());
                _releaseFailures.Add(agent.Id, PackagePath(package).Contains("retry"));
                _stopSignals.Add(agent.Id, stopped);
                return agent;
            }

            private static string PackagePath(TestPackage package) => string.IsNullOrEmpty(package.FullName)
                ? string.Join(";", package.SubPackages.Select(PackagePath)) : package.FullName;

            public void ReturnAgent(ITestAgent agent)
            {
                if (!_releaseFailures.TryGetValue(agent.Id, out bool fail))
                    throw new InvalidOperationException("Unknown agent.");
                if (fail)
                {
                    _releaseFailures[agent.Id] = false;
                    throw new InvalidOperationException("Release failed once.");
                }
                _releaseFailures.Remove(agent.Id);
                _stopSignals[agent.Id].Dispose();
                _stopSignals.Remove(agent.Id);
            }
        }
    }
}
