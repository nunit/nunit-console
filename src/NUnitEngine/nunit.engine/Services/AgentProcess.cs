// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

#if NETFRAMEWORK
using System;
using System.Diagnostics;
using System.IO;
using System.Text;
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
            log.Debug($"TargetRuntime = {runtimeSetting}");
            TargetRuntime = RuntimeFramework.Parse(runtimeSetting);

            // Access other package settings
            bool runAsX86 = package.GetSetting(EnginePackageSettings.RunAsX86, false);
            bool debugAgent = package.GetSetting(EnginePackageSettings.DebugAgent, false);
            string traceLevel = package.GetSetting(EnginePackageSettings.InternalTraceLevel, "Off");
            bool loadUserProfile = package.GetSetting(EnginePackageSettings.LoadUserProfile, false);
            string workDirectory = package.GetSetting(EnginePackageSettings.WorkDirectory, string.Empty);

            string agencyUrl = TargetRuntime.Runtime == RuntimeType.NetCore ? agency.TcpEndPoint : agency.RemotingUrl;
            AgentArgs = new StringBuilder($"{agentId} {agencyUrl} --pid={Process.GetCurrentProcess().Id}");

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
                monoOptions += " --debug";
                StartInfo.Arguments = string.Format("{0} \"{1}\" {2}", monoOptions, AgentExePath, AgentArgs);
            }
            else if (TargetRuntime.Runtime == RuntimeType.Net)
            {
                StartInfo.FileName = AgentExePath;
                StartInfo.Arguments = AgentArgs.ToString();
                StartInfo.LoadUserProfile = loadUserProfile;
            }
            else if (TargetRuntime.Runtime == RuntimeType.NetCore)
            {
                StartInfo.FileName = "dotnet";
                StartInfo.Arguments = $"{AgentExePath} {AgentArgs}";
                StartInfo.LoadUserProfile = loadUserProfile;

                // TODO: Remove the windows limitation and the use of a hard-coded path.
                if (runAsX86)
                {
                    if (Path.DirectorySeparatorChar != '\\')
                        throw new Exception("Running .NET Core as X86 is currently only supported on Windows");

                    var x86_dotnet_exe = @"C:\Program Files (x86)\dotnet\dotnet.exe";
                    if (!File.Exists(x86_dotnet_exe))
                        throw new Exception("The X86 version of dotnet.exe is not installed");

                    StartInfo.FileName = x86_dotnet_exe;
                }
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
            log.Debug($"GetTestAgentExePath({targetRuntime}, {requires32Bit})");
            string engineDir = AssemblyHelper.GetDirectoryName(typeof(AgentProcess).Assembly);

            // If running out of a package "agents" is a subdirectory
            string agentsDir = Path.Combine(engineDir, "agents");

            if (!Directory.Exists(agentsDir))
            {
                // When developing and running in the output directory, "agents" is a 
                // sibling directory the one holding the agent (e.g. net20). This is a
                // bit of a kluge, but it's necessary unless we change the binary 
                // output directory to match the distribution structure.
                agentsDir = Path.Combine(Path.GetDirectoryName(engineDir), "agents");
                log.Debug("Assuming test run in project output directory");
            }

            log.Debug($"Checking for agents at {agentsDir}");

            string runtimeDir;
            string agentName;
            string agentExtension;
            int major = targetRuntime.FrameworkVersion.Major;
            switch (targetRuntime.Runtime)
            {
                case RuntimeType.Net:
                case RuntimeType.Mono:
                    runtimeDir = major >= 4 ? "net40" : "net20";
                    agentName = requires32Bit ? "nunit-agent-x86" : "nunit-agent";
                    agentExtension = ".exe";
                    break;
                case RuntimeType.NetCore:
                    runtimeDir = major >= 6 ? "net6.0" : major == 5 ? "net5.0" : "netcoreapp3.1";
                    agentName = "nunit-agent";
                    agentExtension = ".dll";
                    break;
                default:
                    log.Error($"Unknown runtime type: {targetRuntime.Runtime}");
                    return null;
            }

            return Path.Combine(Path.Combine(agentsDir, runtimeDir), agentName + agentExtension);
        }
    }
}
#endif
