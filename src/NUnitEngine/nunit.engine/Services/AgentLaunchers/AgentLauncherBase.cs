// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

#if NETFRAMEWORK
using NUnit.Engine.Extensibility;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.Versioning;
using System.Text;
using System.Threading.Tasks;
using NUnit.Common;
using TestCentric.Metadata;
using System.IO;

namespace NUnit.Engine.Services.AgentLaunchers
{
    public abstract class AgentLauncherBase : IAgentLauncher
    {
        protected abstract string AgentName { get; }
        protected abstract TestAgentType AgentType { get; }
        protected abstract FrameworkName TargetRuntime { get; }

        protected abstract string AgentPath { get; }
        protected virtual string X86AgentPath => throw new NotImplementedException(".NET Framework agents must override X86AgentPath");

        public TestAgentInfo AgentInfo => new TestAgentInfo(AgentName, TestAgentType.LocalProcess, TargetRuntime);

        public bool CanCreateAgent(TestPackage package)
        {
            // Get target runtime from package
            string runtimeSetting = package.GetSetting(EnginePackageSettings.TargetFrameworkName, string.Empty);
            var targetRuntime = new FrameworkName(runtimeSetting);

            return targetRuntime.Identifier == TargetRuntime.Identifier && targetRuntime.Version.Major <= TargetRuntime.Version.Major;
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

            switch (TargetRuntime.Identifier)
            {
                case FrameworkIdentifiers.NetFramework:
                    startInfo.FileName = runAsX86 ? X86AgentPath : AgentPath;
                    startInfo.Arguments = arguments;
                    startInfo.LoadUserProfile = loadUserProfile;
                    // TODO: Re-integrate mono
                    //if (TargetRuntime.Runtime == Runtime.Mono)
                    //{
                    //    StartInfo.FileName = RuntimeFrameworkService.MonoExePath;
                    //    string monoOptions = "--runtime=v" + TargetRuntime.FrameworkVersion.ToString(2);
                    //    monoOptions += " --debug";
                    //    StartInfo.Arguments = $"{monoOptions} \"{agentPath}\" {AgentArgs}";
                    //}
                    break;
                case FrameworkIdentifiers.NetCoreApp:
                    startInfo.FileName = "dotnet";
                    startInfo.Arguments = $"\"{AgentPath}\" {arguments}";
                    startInfo.LoadUserProfile = loadUserProfile;

                    // TODO: Remove the windows limitation and the use of a hard-coded path.
                    if (runAsX86)
                    {
                        if (Path.DirectorySeparatorChar != '\\')
                            throw new Exception("Running .NET Core as X86 is currently only supported on Windows");

                        string? installDirectory = DotNet.GetX86InstallDirectory();
                        if (installDirectory is null)
                            throw new Exception("The X86 version of dotnet.exe is not installed");

                        var x86_dotnet_exe = Path.Combine(installDirectory, "dotnet.exe");
                        if (!File.Exists(x86_dotnet_exe))
                            throw new Exception("The X86 version of dotnet.exe is not installed");

                        startInfo.FileName = x86_dotnet_exe;
                    }
                    break;
                default:
                    startInfo.FileName = AgentPath;
                    startInfo.Arguments = arguments;
                    break;
            }

            return process;
        }
    }
}
#endif
