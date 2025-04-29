// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

#if NETFRAMEWORK
using System;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.Versioning;
using System.Security.Permissions;
using System.Text;
using NUnit.Common;
using NUnit.Engine.Extensibility;

namespace NUnit.Engine.Services.AgentLaunchers
{
    public class Net462AgentLauncher : IAgentLauncher
    {
        public static Logger log = InternalTrace.GetLogger(typeof(Net462AgentLauncher));

        public static bool Enabled = true;

        private const string TARGET_RUNTIME_IDENTIFIER = FrameworkIdentifiers.NetFramework;
        private static readonly Version TARGET_RUNTIME_VERSION = new Version(4, 6, 2);
        private static readonly FrameworkName TARGET_RUNTIME = new FrameworkName(TARGET_RUNTIME_IDENTIFIER, TARGET_RUNTIME_VERSION);

        private const string AGENT_NAME = "nunit-agent-net462.exe";
        private const string AGENT_NAME_X86 = "nunit-agent-net462-x86.exe";

        public TestAgentInfo AgentInfo => new TestAgentInfo(GetType().Name, TestAgentType.LocalProcess, TARGET_RUNTIME);

        public bool CanCreateAgent(TestPackage package)
        {
            // Get target runtime
            string runtimeSetting = package.GetSetting(EnginePackageSettings.TargetRuntimeFramework, string.Empty);
            var framework = RuntimeFramework.Parse(runtimeSetting).FrameworkName;
            return framework.Identifier == TARGET_RUNTIME_IDENTIFIER && framework.Version.Major <= TARGET_RUNTIME_VERSION.Major;
        }

        public Process CreateAgent(Guid agentId, string agencyUrl, TestPackage package)
        {
            // Should not be called unless we have previously checked CanCreateAgent
            Guard.ArgumentValid(CanCreateAgent(package), "Unable to create agent. Check result of CanCreateAgent before calling CreateAgent.", nameof(package));

            log.Debug($"CreateAgent: id={agentId} url={agencyUrl} package ={package.FullName}");

            // Access other package settings
            bool runAsX86 = package.GetSetting(EnginePackageSettings.RunAsX86, false);
            bool debugTests = package.GetSetting(EnginePackageSettings.DebugTests, false);
            bool debugAgent = package.GetSetting(EnginePackageSettings.DebugAgent, false);
            string traceLevel = package.GetSetting(EnginePackageSettings.InternalTraceLevel, "Off");
            bool loadUserProfile = package.GetSetting(EnginePackageSettings.LoadUserProfile, false);
            string workDirectory = package.GetSetting(EnginePackageSettings.WorkDirectory, string.Empty);

            var sb = new StringBuilder($"--agentId={agentId} --agencyUrl={agencyUrl} --pid={Process.GetCurrentProcess().Id}");

            // Set options that need to be in effect before the package
            // is loaded by using the command line.
            if (traceLevel != "Off")
                sb.Append(" --trace=").EscapeProcessArgument(traceLevel);
            if (debugAgent)
                sb.Append(" --debug-agent");
            if (workDirectory != string.Empty)
                sb.Append(" --work=").EscapeProcessArgument(workDirectory);

            var agentName = runAsX86 ? AGENT_NAME_X86 : AGENT_NAME;
            var enginePath = AssemblyHelper.GetDirectoryName(Assembly.GetExecutingAssembly());
            var agentPath = System.IO.Path.Combine(enginePath, $"agents/net462/{agentName}");
            var agentArgs = sb.ToString();

            var process = new Process();
            process.EnableRaisingEvents = true;

            var startInfo = process.StartInfo;
            startInfo.UseShellExecute = false;
            startInfo.CreateNoWindow = true;
            startInfo.WorkingDirectory = Environment.CurrentDirectory;
            startInfo.LoadUserProfile = loadUserProfile;

            startInfo.FileName = agentPath;
            startInfo.Arguments = agentArgs;

            return process;
        }
    }
}
#endif
