// ***********************************************************************
// Copyright (c) Charlie Poole and TestCentric Engine contributors.
// Licensed under the MIT License. See LICENSE.txt in root directory.
// ***********************************************************************

#if NETFRAMEWORK
using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using NUnit.Engine;
using NUnit.Engine.Internal;

namespace NUnit.Engine.Services
{
    public class AgentProcess : Process
    {
        private static readonly Logger log = InternalTrace.GetLogger(typeof(AgentProcess));

        public AgentProcess(TestAgency agency, TestPackage package, Guid agentId)
        {
            // Get target runtime
            string runtimeSetting = package.GetSetting(EnginePackageSettings.TargetRuntimeFramework, "");
            TargetRuntime = RuntimeFramework.Parse(runtimeSetting);

            // Access other package settings
            bool runAsX86 = package.GetSetting(EnginePackageSettings.RunAsX86, false);
            bool debugTests = package.GetSetting(EnginePackageSettings.DebugTests, false);
            bool debugAgent = package.GetSetting(EnginePackageSettings.DebugAgent, false);
            string traceLevel = package.GetSetting(EnginePackageSettings.InternalTraceLevel, "Off");
            bool loadUserProfile = package.GetSetting(EnginePackageSettings.LoadUserProfile, false);
            string workDirectory = package.GetSetting(EnginePackageSettings.WorkDirectory, string.Empty);

            AgentArgs = new StringBuilder($"{agentId} {agency.ServerUrl} --pid={Process.GetCurrentProcess().Id}");

            // Set options that need to be in effect before the package
            // is loaded by using the command line.
            if (traceLevel != "Off")
                AgentArgs.Append(" --trace=").EscapeProcessArgument(traceLevel);
            if (debugAgent)
                AgentArgs.Append(" --debug-agent");
            if (workDirectory != string.Empty)
                AgentArgs.Append(" --work=").EscapeProcessArgument(workDirectory);

            AgentExePath = GetTestAgentExePath(TargetRuntime, runAsX86);

            log.Debug("Using nunit-agent at " + AgentExePath);

            StartInfo.UseShellExecute = false;
            StartInfo.CreateNoWindow = true;
            StartInfo.WorkingDirectory = Environment.CurrentDirectory;
            EnableRaisingEvents = true;

            if (TargetRuntime.Runtime == RuntimeType.Mono)
            {
                StartInfo.FileName = RuntimeFramework.MonoExePath;
                string monoOptions = "--runtime=v" + TargetRuntime.ClrVersion.ToString(3);
                if (debugTests || debugAgent) monoOptions += " --debug";
                StartInfo.Arguments = string.Format("{0} \"{1}\" {2}", monoOptions, AgentExePath, AgentArgs);
            }
            else if (TargetRuntime.Runtime == RuntimeType.Net)
            {
                StartInfo.FileName = AgentExePath;
                StartInfo.Arguments = AgentArgs.ToString();
                StartInfo.LoadUserProfile = loadUserProfile;
            }
            else
            {
                StartInfo.FileName = AgentExePath;
                StartInfo.Arguments = AgentArgs.ToString();
            }
        }

        // Internal properties exposed for testing

        internal RuntimeFramework TargetRuntime { get; }
        internal string AgentExePath { get; }
        internal StringBuilder AgentArgs { get; }

        public static string GetTestAgentExePath(RuntimeFramework targetRuntime, bool requires32Bit)
        {
            string engineDir = NUnitConfiguration.EngineDirectory;
            if (engineDir == null) return null;

            // If running out of a package "agents" is a subdirectory
            string agentsDir = Path.Combine(engineDir, "agents");
            log.Debug($"Checking for agents at {agentsDir}");

            if (!Directory.Exists(agentsDir))
            {
                // When developing and running in the output directory, "agents" is a 
                // sibling directory the one holding the agent (e.g. net20). This is a
                // bit of a kluge, but it's necessary unless we change the binary 
                // output directory to match the distribution structure.
                agentsDir = Path.Combine(Path.GetDirectoryName(engineDir), "agents");
                log.Debug($"Directory not found! Using {agentsDir}");
            }

            string runtimeDir = targetRuntime.FrameworkVersion.Major >= 4 ? "net40" : "net20";

            string agentName = requires32Bit
                ? "nunit-agent-x86.exe"
                : "nunit-agent.exe";

            return Path.Combine(Path.Combine(agentsDir, runtimeDir), agentName);
        }
    }
}
#endif
