// ***********************************************************************
// Copyright (c) 2011–2019 Charlie Poole, Rob Prouse
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
// ***********************************************************************

#if !NETSTANDARD1_6 && !NETSTANDARD2_0
using NUnit.Engine.Internal;
using NUnit.Engine.Services;
using System;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace NUnit.Engine.Runners
{
    public partial class ProcessRunner
    {
        private static ProcessStartInfo CreateAgentProcessStartInfo(TestPackage package, RuntimeFrameworkService runtimeFrameworkService)
        {
            var targetRuntime = GetTargetRuntime(package);

            bool useX86Agent = package.GetSetting(EnginePackageSettings.RunAsX86, false);
            bool debugTests = package.GetSetting(EnginePackageSettings.DebugTests, false);
            bool debugAgent = package.GetSetting(EnginePackageSettings.DebugAgent, false);
            string traceLevel = package.GetSetting(EnginePackageSettings.InternalTraceLevel, "Off");
            bool loadUserProfile = package.GetSetting(EnginePackageSettings.LoadUserProfile, false);
            string workDirectory = package.GetSetting(EnginePackageSettings.WorkDirectory, string.Empty);

            log.Info("Getting {0} agent for use under {1}", useX86Agent ? "x86" : "standard", targetRuntime);

            if (!targetRuntime.IsAvailable)
                throw new ArgumentException($"The {targetRuntime} framework is not available.", nameof(package));

            string agentExePath = GetTestAgentExePath(useX86Agent);

            if (!File.Exists(agentExePath))
                throw new FileNotFoundException(Path.GetFileName(agentExePath) + " could not be found.", agentExePath);

            log.Debug("Using nunit-agent at " + agentExePath);

            var startInfo = new ProcessStartInfo
            {
                UseShellExecute = false,
                FileName = agentExePath
            };

            targetRuntime = runtimeFrameworkService.GetBestAvailableFramework(targetRuntime);

            var arguments = new StringBuilder();

            var clrVersion = "v" + targetRuntime.ClrVersion.ToString(3);

            switch (targetRuntime.Runtime)
            {
                case RuntimeType.Mono:
                    startInfo.FileName = RuntimeFramework.MonoExePath;

                    arguments.Append("--runtime=").Append(clrVersion).Append(' ');

                    if (debugTests || debugAgent)
                        arguments.Append("--debug ");

                    arguments.EscapeProcessArgument(agentExePath);
                    arguments.Append(' ');
                    break;

                case RuntimeType.Net:
                    // Override the COMPLUS_Version env variable. This causes the CLR meta host to run a CLR of the specific version.
                    startInfo.EnvironmentVariables["COMPLUS_Version"] = clrVersion;

                    // Leave a marker that we have changed this variable,so that the agent can restore it for any code or child processes running within the agent.
                    string cpvOriginal = Environment.GetEnvironmentVariable("COMPLUS_Version");
                    startInfo.EnvironmentVariables["TestAgency_COMPLUS_Version_Original"] = string.IsNullOrEmpty(cpvOriginal) ? "NULL" : cpvOriginal;
                    startInfo.LoadUserProfile = loadUserProfile;
                    break;
            }

            // Set options that need to be in effect before the package
            // is loaded by using the command line.
            arguments.Append("--pid=").Append(Process.GetCurrentProcess().Id);

            if (traceLevel != "Off")
                arguments.Append(" --trace:").EscapeProcessArgument(traceLevel);
            if (debugAgent)
                arguments.Append(" --debug-agent");
            if (workDirectory != string.Empty)
                arguments.Append(" --work=").EscapeProcessArgument(workDirectory);

            startInfo.Arguments = arguments.ToString();

            return startInfo;
        }

        private static RuntimeFramework GetTargetRuntime(TestPackage package)
        {
            RuntimeFramework targetRuntime;
            string runtimeSetting = package.GetSetting(EnginePackageSettings.RuntimeFramework, "");
            if (runtimeSetting.Length > 0)
            {
                if (!RuntimeFramework.TryParse(runtimeSetting, out targetRuntime))
                    throw new NUnitEngineException("Invalid or unknown framework requested: " + runtimeSetting);
            }
            else
            {
                targetRuntime = RuntimeFramework.CurrentFramework;
            }

            return targetRuntime.Runtime != RuntimeType.Any ? targetRuntime :
                new RuntimeFramework(RuntimeFramework.CurrentFramework.Runtime, targetRuntime.ClrVersion);
        }

        private static string GetTestAgentExePath(bool requires32Bit)
        {
            string engineDir = NUnitConfiguration.EngineDirectory;
            if (engineDir == null) return null;

            string agentName = requires32Bit
                ? "nunit-agent-x86.exe"
                : "nunit-agent.exe";

            return Path.Combine(engineDir, agentName);
        }
    }
}
#endif
