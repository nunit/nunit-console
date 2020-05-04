// ***********************************************************************
// Copyright (c) Charlie Poole and TestCentric Engine contributors.
// Licensed under the MIT License. See LICENSE.txt in root directory.
// ***********************************************************************

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
        private readonly string REQUIRED_ARGS = $"{AGENT_ID} {REMOTING_URL} --pid={Process.GetCurrentProcess().Id}";

        [SetUp]
        public void SetUp()
        {
            _agency = Substitute.For<TestAgency>();
            _agency.ServerUrl.ReturnsForAnyArgs(REMOTING_URL);
            _package = new TestPackage("junk.dll");
            // Only required setting, some tests may change this
            _package.Settings[EnginePackageSettings.TargetRuntimeFramework] = "net-4.5";
        }

        [TestCase("net-4.5", false, "../agents/net40/nunit-agent.exe")]
        [TestCase("net-4.5", true, "../agents/net40/nunit-agent-x86.exe")]
        [TestCase("net-4.0", false, "../agents/net40/nunit-agent.exe")]
        [TestCase("net-4.0", true, "../agents/net40/nunit-agent-x86.exe")]
        [TestCase("net-3.5", false, "../agents/net20/nunit-agent.exe")]
        [TestCase("net-3.5", true, "../agents/net20/nunit-agent-x86.exe")]
        [TestCase("net-2.0", false, "../agents/net20/nunit-agent.exe")]
        [TestCase("net-2.0", true, "../agents/net20/nunit-agent-x86.exe")]
        //[TestCase("netcore-2.1", false, "agents/netcoreapp2.1/testcentric-agent.dll")]
        //[TestCase("netcore-2.1", true, "agents/netcoreapp2.1/testcentric-agent-x86.dll")]
        //[TestCase("netcore-1.1", false, "agents/netcoreapp1.1/testcentric-agent.dll")]
        //[TestCase("netcore-1.1", true, "agents/netcoreapp1.1/testcentric-agent-x86.dll")]
        public void AgentSelection(string runtime, bool x86, string agentPath)
        {
            _package.Settings[EnginePackageSettings.TargetRuntimeFramework] = runtime;
            _package.Settings[EnginePackageSettings.RunAsX86] = x86;

            var agentProcess = GetAgentProcess();
            agentPath = Path.Combine(TestContext.CurrentContext.TestDirectory, agentPath);

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

            Assert.True(process.EnableRaisingEvents, "EnableRaisingEvents");

            var startInfo = process.StartInfo;
            Assert.False(startInfo.UseShellExecute, "UseShellExecute");
            Assert.True(startInfo.CreateNoWindow, "CreateNoWindow");
            Assert.False(startInfo.LoadUserProfile, "LoadUserProfile");

            var targetRuntime = RuntimeFramework.Parse(framework);
            if (targetRuntime.Runtime == RuntimeType.Mono)
            {
                string monoOptions = "--runtime=v" + targetRuntime.ClrVersion.ToString(3);
                Assert.That(startInfo.FileName, Is.EqualTo(RuntimeFramework.MonoExePath));
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
