// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

#if NETFRAMEWORK
using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.Versioning;
using System.Text;
using NUnit.Common;
using NUnit.Engine.Extensibility;

namespace NUnit.Engine.Services.AgentLaunchers
{
    public class Net80AgentLauncher : IAgentLauncher
    {
        public static bool Enabled = true;

        private const string TARGET_RUNTIME_IDENTIFIER = FrameworkIdentifiers.NetCoreApp;
        private static readonly Version TARGET_RUNTIME_VERSION = new Version(8, 0, 0);
        private static readonly FrameworkName TARGET_RUNTIME = new FrameworkName(TARGET_RUNTIME_IDENTIFIER, TARGET_RUNTIME_VERSION);

        private const string AGENT_NAME = "nunit-agent-net80.dll";

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

            var agentName = AGENT_NAME;
            var enginePath = AssemblyHelper.GetDirectoryName(Assembly.GetExecutingAssembly());
            var agentPath = System.IO.Path.Combine(enginePath, $"agents/net8.0/{agentName}");
            var agentArgs = sb.ToString();

            var process = new Process();
            process.EnableRaisingEvents = true;

            var startInfo = process.StartInfo;
            startInfo.UseShellExecute = false;
            startInfo.CreateNoWindow = true;
            startInfo.WorkingDirectory = Environment.CurrentDirectory;
            startInfo.LoadUserProfile = loadUserProfile;

            startInfo.FileName = "dotnet";
            startInfo.Arguments = $"\"{agentPath}\" {agentArgs}";

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

            return process;
        }
    }
}
#endif
