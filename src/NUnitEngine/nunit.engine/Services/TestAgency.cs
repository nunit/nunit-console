// ***********************************************************************
// Copyright (c) 2011-2016 Charlie Poole, Rob Prouse
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
using System;
using System.IO;
using System.Threading;
using System.Diagnostics;
using System.Text;
using NUnit.Common;
using NUnit.Engine.Agents;
using NUnit.Engine.Internal;

namespace NUnit.Engine.Services
{
    /// <summary>
    /// The TestAgency class provides RemoteTestAgents
    /// on request and tracks their status. Agents
    /// are wrapped in an instance of the TestAgent
    /// class. Multiple agent types are supported
    /// but only one, ProcessAgent is implemented
    /// at this time.
    /// </summary>
    public class TestAgency : ServerBase, ITestAgency, IService
    {
        private static readonly Logger log = InternalTrace.GetLogger(typeof(TestAgency));

        private readonly AgentStore _agents = new AgentStore();

        public TestAgency() : this( "TestAgency", 0 ) { }

        public TestAgency( string uri, int port ) : base( uri, port ) { }

        //public override void Stop()
        //{
        //    foreach( KeyValuePair<Guid,AgentRecord> pair in agentData )
        //    {
        //        AgentRecord r = pair.Value;

        //        if ( !r.Process.HasExited )
        //        {
        //            if ( r.Agent != null )
        //            {
        //                r.Agent.Stop();
        //                r.Process.WaitForExit(10000);
        //            }

        //            if ( !r.Process.HasExited )
        //                r.Process.Kill();
        //        }
        //    }

        //    agentData.Clear();

        //    base.Stop ();
        //}

        public void Register(ITestAgent agent)
        {
            _agents.Register(agent);
        }

        public ITestAgent GetAgent(TestPackage package, int waitTime)
        {
            // TODO: Decide if we should reuse agents
            return CreateRemoteAgent(package, waitTime);
        }

        internal bool IsAgentProcessActive(Guid agentId, out Process process)
        {
            return _agents.IsAgentProcessActive(agentId, out process);
        }

        private Process LaunchAgentProcess(TestPackage package, Guid agentId)
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

            if (targetRuntime.Runtime == RuntimeType.Any)
                targetRuntime = new RuntimeFramework(RuntimeFramework.CurrentFramework.Runtime, targetRuntime.ClrVersion);

            bool useX86Agent = package.GetSetting(EnginePackageSettings.RunAsX86, false);
            bool debugTests = package.GetSetting(EnginePackageSettings.DebugTests, false);
            bool debugAgent = package.GetSetting(EnginePackageSettings.DebugAgent, false);
            string traceLevel = package.GetSetting(EnginePackageSettings.InternalTraceLevel, "Off");
            bool loadUserProfile = package.GetSetting(EnginePackageSettings.LoadUserProfile, false);
            string workDirectory = package.GetSetting(EnginePackageSettings.WorkDirectory, string.Empty);

            var agentArgs = new StringBuilder();

            // Set options that need to be in effect before the package
            // is loaded by using the command line.
            agentArgs.Append("--pid=").Append(Process.GetCurrentProcess().Id);
            if (traceLevel != "Off")
                agentArgs.Append(" --trace:").EscapeProcessArgument(traceLevel);
            if (debugAgent)
                agentArgs.Append(" --debug-agent");
            if (workDirectory != string.Empty)
                agentArgs.Append(" --work=").EscapeProcessArgument(workDirectory);

            log.Info("Getting {0} agent for use under {1}", useX86Agent ? "x86" : "standard", targetRuntime);

            if (!targetRuntime.IsAvailable)
                throw new ArgumentException(
                    string.Format("The {0} framework is not available", targetRuntime),
                    "framework");

            string agentExePath = GetTestAgentExePath(targetRuntime, useX86Agent);
            log.Debug("Using nunit-agent at " + agentExePath);

            if (!File.Exists(agentExePath))
                throw new FileNotFoundException($"Agent {agentExePath} could not be found.");

            Process p = new Process();
            p.StartInfo.UseShellExecute = false;
            p.StartInfo.CreateNoWindow = true;
            p.EnableRaisingEvents = true;
            p.Exited += (sender, e) => OnAgentExit((Process)sender, agentId);
            string arglist = agentId.ToString() + " " + ServerUrl + " " + agentArgs;

            switch( targetRuntime.Runtime )
            {
                case RuntimeType.Mono:
                    p.StartInfo.FileName = RuntimeFramework.MonoExePath;
                    string monoOptions = "--runtime=v" + targetRuntime.ClrVersion.ToString(3);
                    if (debugTests || debugAgent) monoOptions += " --debug";
                    p.StartInfo.Arguments = string.Format("{0} \"{1}\" {2}", monoOptions, agentExePath, arglist);
                    break;

                case RuntimeType.Net:
                    p.StartInfo.FileName = agentExePath;
                    p.StartInfo.Arguments = arglist;
                    p.StartInfo.LoadUserProfile = loadUserProfile;
                    break;

                default:
                    p.StartInfo.FileName = agentExePath;
                    p.StartInfo.Arguments = arglist;
                    break;
            }

            p.Start();
            log.Debug("Launched Agent process {0} - see nunit-agent_{0}.log", p.Id);
            log.Debug("Command line: \"{0}\" {1}", p.StartInfo.FileName, p.StartInfo.Arguments);

            _agents.Start(agentId, p);
            return p;
        }

        private ITestAgent CreateRemoteAgent(TestPackage package, int waitTime)
        {
            var agentId = Guid.NewGuid();
            var process = LaunchAgentProcess(package, agentId);

            log.Debug($"Waiting for agent {agentId:B} to register");

            const int pollTime = 200;

            // Wait for agent registration based on the agent actually getting processor time to avoid falling over
            // under process starvation.
            while (waitTime > process.TotalProcessorTime.TotalMilliseconds && !process.HasExited)
            {
                Thread.Sleep(pollTime);

                if (_agents.IsReady(agentId, out var agent))
                {
                    log.Debug($"Returning new agent {agentId:B}");
                    return new RemoteTestAgentProxy(agent, agentId);
                }
            }

            return null;
        }

        private static string GetTestAgentExePath(RuntimeFramework targetRuntime, bool requires32Bit)
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

        private void OnAgentExit(Process process, Guid agentId)
        {
            _agents.MarkTerminated(agentId);

            string errorMsg;

            switch (process.ExitCode)
            {
                case AgentExitCodes.OK:
                    return;
                case AgentExitCodes.PARENT_PROCESS_TERMINATED:
                    errorMsg = "Remote test agent believes agency process has exited.";
                    break;
                case AgentExitCodes.UNEXPECTED_EXCEPTION:
                    errorMsg = "Unhandled exception on remote test agent. " +
                               "To debug, try running with the --inprocess flag, or using --trace=debug to output logs.";
                    break;
                case AgentExitCodes.FAILED_TO_START_REMOTE_AGENT:
                    errorMsg = "Failed to start remote test agent.";
                    break;
                case AgentExitCodes.DEBUGGER_SECURITY_VIOLATION:
                    errorMsg = "Debugger could not be started on remote agent due to System.Security.Permissions.UIPermission not being set.";
                    break;
                case AgentExitCodes.DEBUGGER_NOT_IMPLEMENTED:
                    errorMsg = "Debugger could not be started on remote agent as not available on platform.";
                    break;
                case AgentExitCodes.UNABLE_TO_LOCATE_AGENCY:
                    errorMsg = "Remote test agent unable to locate agency process.";
                    break;
                default:
                    errorMsg = $"Remote test agent exited with non-zero exit code {process.ExitCode}";
                    break;
            }

            throw new NUnitEngineException(errorMsg);
        }

        public IServiceLocator ServiceContext { get; set; }

        public ServiceStatus Status { get; private set; }

        public void StopService()
        {
            try
            {
                Stop();
            }
            finally
            {
                Status = ServiceStatus.Stopped;
            }
        }

        public void StartService()
        {
            try
            {
                Start();
                Status = ServiceStatus.Started;
            }
            catch
            {
                Status = ServiceStatus.Error;
                throw;
            }
        }
    }
}
#endif