// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

#if NETFRAMEWORK
using NUnit.Engine.Extensibility;
using System;
using System.Diagnostics;
using System.Runtime.Versioning;
using System.Text;
using NUnit.Common;
using System.IO;

namespace NUnit.Engine.Agents
{
    public abstract class LocalProcessAgentLauncher : IAgentLauncher
    {
        protected abstract string AgentName { get; }
        protected abstract TestAgentType AgentType { get; }
        protected abstract FrameworkName AgentRuntime { get; }

        protected abstract string AgentPath { get; }

        // Override if the agent path for X86 is different
        protected virtual string X86AgentPath => AgentPath;

        public TestAgentInfo AgentInfo => new TestAgentInfo(AgentName, TestAgentType.LocalProcess, AgentRuntime);

        public bool CanCreateAgent(TestPackage package)
        {
            // Get target runtime from package
            string runtimeSetting = package.GetSetting(EnginePackageSettings.TargetFrameworkName, string.Empty);
            var targetRuntime = new FrameworkName(runtimeSetting);
            bool runAsX86 = package.GetSetting(EnginePackageSettings.RunAsX86, false);

            // Running under X86 under .NET Core is currently only supported on Windows
            if (runAsX86 && targetRuntime.Identifier == FrameworkIdentifiers.NetCoreApp && Path.DirectorySeparatorChar != '\\')
                return false;

            return targetRuntime.Identifier == AgentRuntime.Identifier && targetRuntime.Version.Major <= AgentRuntime.Version.Major;
        }

        public Process CreateAgent(Guid agentId, string agencyUrl, TestPackage package)
        {
            // Should not be called unless we have previously checked CanCreateAgent
            Guard.ArgumentValid(CanCreateAgent(package), "Unable to create agent. Check result of CanCreateAgent before calling CreateAgent.", nameof(package));

            var process = new Process()
            {
                EnableRaisingEvents = true
            };

            // Access package settings
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
            if (debugTests)
                sb.Append(" --debug-tests");
            if (workDirectory != string.Empty)
                sb.Append(" --work=").EscapeProcessArgument(workDirectory);

            string arguments = sb.ToString();

            var startInfo = process.StartInfo;
            startInfo.UseShellExecute = false;
            startInfo.CreateNoWindow = true;
            startInfo.WorkingDirectory = Environment.CurrentDirectory;
            startInfo.LoadUserProfile = loadUserProfile;

            startInfo.FileName = runAsX86 ? X86AgentPath : AgentPath;
            startInfo.Arguments = arguments;

            if (AgentRuntime.Identifier == FrameworkIdentifiers.NetCoreApp)
            {
                startInfo.FileName = DotNet.GetDotNetExe(runAsX86);
                startInfo.Arguments = $"\"{AgentPath}\" {arguments}";
            }

            return process;
        }
    }
}
#endif
