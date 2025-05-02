// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

#if NETFRAMEWORK
using System;
using System.IO;
using System.Diagnostics;
using NUnit.Framework;
using NSubstitute;

namespace NUnit.Engine.Services
{
    public class AgentProcessTests
    {
        private TestAgency _agency;
        private TestPackage _package;
        private readonly static Guid AGENT_ID = Guid.NewGuid();
        private const string REMOTING_URL = "tcp://127.0.0.1:1234/TestAgency";
        private readonly string REQUIRED_ARGS = $"--agentId={AGENT_ID} --agencyUrl={REMOTING_URL} --pid={Process.GetCurrentProcess().Id}";

        [SetUp]
        public void SetUp()
        {
            _agency = Substitute.For<TestAgency>();
            _agency.TcpEndPoint.ReturnsForAnyArgs(REMOTING_URL);
            _package = new TestPackage("junk.dll");
            // Only required setting, some tests may change this
            _package.Settings[EnginePackageSettings.TargetRuntimeFramework] = "net-4.5";
        }

#if DEBUG
        const string AGENTS_DIR = "../../../bin/Debug/agents/";
#else
        const string AGENTS_DIR = "../../../bin/Release/agents/";
#endif

        [TestCase("net-4.8", false, "net462/nunit-agent-net462.exe")]
        [TestCase("net-4.8", true, "net462/nunit-agent-net462-x86.exe")]
        [TestCase("net-4.6.2", false, "net462/nunit-agent-net462.exe")]
        [TestCase("net-4.6.2", true, "net462/nunit-agent-net462-x86.exe")]
        [TestCase("net-4.0", false, "net462/nunit-agent-net462.exe")]
        [TestCase("net-4.0", true, "net462/nunit-agent-net462-x86.exe")]
        [TestCase("net-3.5", false, "net462/nunit-agent-net462.exe")]
        [TestCase("net-3.5", true, "net462/nunit-agent-net462-x86.exe")]
        [TestCase("net-2.0", false, "net462/nunit-agent-net462.exe")]
        [TestCase("net-2.0", true, "net462/nunit-agent-net462-x86.exe")]
        [TestCase("netcore-2.1", false, "net8.0/nunit-agent-net80.dll")]
        [TestCase("netcore-2.1", true, "net8.0/nunit-agent-net80.dll")]
        [TestCase("netcore-3.1", false, "net8.0/nunit-agent-net80.dll")]
        [TestCase("netcore-3.1", true, "net8.0/nunit-agent-net80.dll")]
        [TestCase("netcore-5.0", false, "net8.0/nunit-agent-net80.dll")]
        [TestCase("netcore-5.0", true, "net8.0/nunit-agent-net80.dll")]
        [TestCase("netcore-6.0", false, "net8.0/nunit-agent-net80.dll")]
        [TestCase("netcore-6.0", true, "net8.0/nunit-agent-net80.dll")]
        [TestCase("netcore-7.0", false, "net8.0/nunit-agent-net80.dll")]
        [TestCase("netcore-7.0", true, "net8.0/nunit-agent-net80.dll")]
        [TestCase("netcore-8.0", false, "net8.0/nunit-agent-net80.dll")]
        [TestCase("netcore-8.0", true, "net8.0/nunit-agent-net80.dll")]
        [TestCase("netcore-9.0", false, "net8.0/nunit-agent-net80.dll")]
        [TestCase("netcore-9.0", true, "net8.0/nunit-agent-net80.dll")]
        public void AgentSelection(string runtime, bool x86, string agentPath)
        {
            _package.Settings[EnginePackageSettings.TargetRuntimeFramework] = runtime;
            _package.Settings[EnginePackageSettings.RunAsX86] = x86;

            var agentProcess = GetAgentProcess();
            agentPath = Path.GetFullPath(AGENTS_DIR + agentPath);

            // NOTE: the file doesn't actually exist at this location during unit
            // testing, but it's where it will be found once the app is installed.
            Assert.That(agentProcess.AgentExePath, Is.SamePath(agentPath));
        }

        [TestCase("net-4.5")]
        [TestCase("net-4.0")]
        [TestCase("net-3.5")]
        [TestCase("net-2.0")]
        [TestCase("mono-4.5")]
        [TestCase("mono-4.0")]
        [TestCase("mono-2.0")]
        public void DefaultValues(string framework)
        {
            _package.Settings[EnginePackageSettings.TargetRuntimeFramework] = framework;
            var process = GetAgentProcess();

            Assert.That(process.AgentArgs.ToString(), Is.EqualTo(REQUIRED_ARGS));

            Assert.That(process.EnableRaisingEvents, Is.True, "EnableRaisingEvents");

            var startInfo = process.StartInfo;
            Assert.That(startInfo.UseShellExecute, Is.False, "UseShellExecute");
            Assert.That(startInfo.CreateNoWindow, Is.True, "CreateNoWindow");
            Assert.That(startInfo.LoadUserProfile, Is.False, "LoadUserProfile");

            var targetRuntime = RuntimeFramework.Parse(framework);
            if (targetRuntime.Runtime == Runtime.Mono)
            {
                string monoOptions = "--runtime=v" + targetRuntime.FrameworkVersion.ToString(2);
                monoOptions += " --debug";
                Assert.That(startInfo.FileName, Is.EqualTo(RuntimeFrameworkService.MonoExePath));
                Assert.That(startInfo.Arguments, Is.EqualTo(
                    $"{monoOptions} \"{process.AgentExePath}\" {process.AgentArgs}"));
            }
            else
            {
                Assert.That(startInfo.FileName, Is.EqualTo(process.AgentExePath));
                Assert.That(startInfo.Arguments, Is.EqualTo(process.AgentArgs.ToString()));
            }
        }

        [Test]
        public void DebugTests()
        {
            _package.Settings[EnginePackageSettings.DebugTests] = true;
            var agentProcess = GetAgentProcess();

            // Not reflected in args because framework handles it
            Assert.That(agentProcess.AgentArgs.ToString(), Is.EqualTo(REQUIRED_ARGS));
        }

        [Test]
        public void DebugAgent()
        {
            _package.Settings[EnginePackageSettings.DebugAgent] = true;
            var agentProcess = GetAgentProcess();
            Assert.That(agentProcess.AgentArgs.ToString(), Is.EqualTo(REQUIRED_ARGS + " --debug-agent"));
        }


        [Test]
        public void LoadUserProfile()
        {
            _package.Settings[EnginePackageSettings.LoadUserProfile] = true;
            var agentProcess = GetAgentProcess();
            Assert.That(agentProcess.AgentArgs.ToString(), Is.EqualTo(REQUIRED_ARGS));
        }

        [Test]
        public void TraceLevel()
        {
            _package.Settings[EnginePackageSettings.InternalTraceLevel] = "Debug";
            var agentProcess = GetAgentProcess();
            Assert.That(agentProcess.AgentArgs.ToString(), Is.EqualTo(REQUIRED_ARGS + " --trace=Debug"));
        }

        [Test]
        public void WorkDirectory()
        {
            _package.Settings[EnginePackageSettings.WorkDirectory] = "WORKDIRECTORY";
            var agentProcess = GetAgentProcess();
            Assert.That(agentProcess.AgentArgs.ToString(), Is.EqualTo(REQUIRED_ARGS + " --work=WORKDIRECTORY"));
        }

        [Test]
        public void WorkingDirectory()
        {
            Assert.That(GetAgentProcess().StartInfo.WorkingDirectory, Is.EqualTo(Environment.CurrentDirectory));
        }

        private AgentProcess GetAgentProcess()
        {
            return new AgentProcess(_agency, _package, AGENT_ID);
        }
    }
}
#endif
