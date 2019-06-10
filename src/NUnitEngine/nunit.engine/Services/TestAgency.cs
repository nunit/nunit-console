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
    /// Enumeration used to report AgentStatus
    /// </summary>
    public enum AgentStatus
    {
        Unknown,
        Starting,
        Ready,
        Busy,
        Stopping,
        Terminated
    }

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

        private readonly AgentDataBase _agentData = new AgentDataBase();

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

        public void Register( ITestAgent agent )
        {
            AgentRecord r = _agentData[agent.Id];
            if ( r == null )
                throw new ArgumentException(
                    string.Format("Agent {0} is not in the agency database", agent.Id),
                    "agentId");
            r.Agent = agent;
        }

        public ITestAgent GetAgent(TestPackage package, int waitTime)
        {
            // TODO: Decide if we should reuse agents
            return CreateRemoteAgent(package, waitTime);
        }

        public void ReleaseAgent( ITestAgent agent )
        {
            AgentRecord r = _agentData[agent.Id];
            if (r == null)
                log.Error(string.Format("Unable to release agent {0} - not in database", agent.Id));
            else
            {
                r.Status = AgentStatus.Ready;
                log.Debug("Releasing agent " + agent.Id.ToString());
            }
        }

        internal bool IsAgentRunning(Guid id)
        {
            var agentRecord = _agentData[id];
            return agentRecord != null && agentRecord.Status != AgentStatus.Terminated;
        }

        internal int? GetAgentExitCode(Guid id)
        {
            var agentRecord = _agentData[id];
            if (agentRecord?.Process != null && agentRecord.Process.HasExited)
            {
                try
                {
                    return agentRecord.Process.ExitCode;
                }
                catch (NotSupportedException)
                {
                    return null;
                }
            }
            return null;
        }

        private Guid LaunchAgentProcess(TestPackage package)
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

            string agentExePath = GetTestAgentExePath(useX86Agent);

            if (!File.Exists(agentExePath))
                throw new FileNotFoundException(
                    $"{Path.GetFileName(agentExePath)} could not be found.", agentExePath);

            log.Debug("Using nunit-agent at " + agentExePath);

            Process p = new Process();
            p.StartInfo.UseShellExecute = false;
            p.StartInfo.CreateNoWindow = true;
            p.EnableRaisingEvents = true;
            p.Exited += OnAgentExit;
            Guid agentId = Guid.NewGuid();
            string arglist = agentId.ToString() + " " + ServerUrl + " " + agentArgs;

            targetRuntime = ServiceContext.GetService<RuntimeFrameworkService>().GetBestAvailableFramework(targetRuntime);

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
                    // Override the COMPLUS_Version env variable, this would cause CLR meta host to run a CLR of the specific version
                    string envVar = "v" + targetRuntime.ClrVersion.ToString(3);
                    p.StartInfo.EnvironmentVariables["COMPLUS_Version"] = envVar;
                    // Leave a marker that we have changed this variable, so that the agent could restore it for any code or child processes running within the agent
                    string cpvOriginal = Environment.GetEnvironmentVariable("COMPLUS_Version");
                    p.StartInfo.EnvironmentVariables["TestAgency_COMPLUS_Version_Original"] = string.IsNullOrEmpty(cpvOriginal) ? "NULL" : cpvOriginal;
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

            _agentData.Add( new AgentRecord( agentId, p, null, AgentStatus.Starting ) );
            return agentId;
        }

        private ITestAgent CreateRemoteAgent(TestPackage package, int waitTime)
        {
            var agentId = LaunchAgentProcess(package);

            log.Debug($"Waiting for agent {agentId:B} to register");

            const int pollTime = 200;

            var agentRecord = _agentData[agentId];
            var agentProcess = agentRecord.Process;

            //Wait for agent registration based on the agent actually getting processor time - to avoid falling over under process starvation
            while(waitTime > agentProcess.TotalProcessorTime.TotalMilliseconds && !agentProcess.HasExited)
            {
                Thread.Sleep(pollTime);

                if (agentRecord.Agent != null)
                {
                    log.Debug($"Returning new agent {agentId:B}");
                    return new RemoteTestAgentProxy(agentRecord.Agent, agentRecord.Id);
                }
            }

            return null;
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

        private void OnAgentExit(object sender, EventArgs e)
        {
            var process = sender as Process;
            if (process == null)
                return;

            var agentRecord = _agentData.GetDataForProcess(process);
            agentRecord.Status = AgentStatus.Terminated;

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