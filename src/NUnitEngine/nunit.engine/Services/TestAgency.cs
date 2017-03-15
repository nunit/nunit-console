// ***********************************************************************
// Copyright (c) 2011-2016 Charlie Poole
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

using System;
using System.IO;
using System.Threading;
using System.Diagnostics;
using System.Collections.Generic;
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
        Stopping
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
        static Logger log = InternalTrace.GetLogger(typeof(TestAgency));

        #region Private Fields

        private AgentDataBase _agentData = new AgentDataBase();

        #endregion

        #region Constructors
        public TestAgency() : this( "TestAgency", 0 ) { }

        public TestAgency( string uri, int port ) : base( uri, port ) { }
        #endregion

        #region ServerBase Overrides
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
        #endregion

        #region Public Methods - Called by Agents
        public void Register( ITestAgent agent )
        {
            AgentRecord r = _agentData[agent.Id];
            if ( r == null )
                throw new ArgumentException(
                    string.Format("Agent {0} is not in the agency database", agent.Id),
                    "agentId");
            r.Agent = agent;
        }

        public void Unregister(Guid agentId)
        {
            _agentData.Remove(agentId);
        }

        #endregion

        #region Public Methods - Called by Clients

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

        #endregion

        #region Helper Methods
        private Guid LaunchAgentProcess(TestPackage package)
        {
            RuntimeFramework targetRuntime = RuntimeFramework.CurrentFramework;
            string runtimeSetting = package.GetSetting(EnginePackageSettings.RuntimeFramework, "");
            if (runtimeSetting != "")
                targetRuntime = RuntimeFramework.Parse(runtimeSetting);

            if (targetRuntime.Runtime == RuntimeType.Any)
                targetRuntime = new RuntimeFramework(RuntimeFramework.CurrentFramework.Runtime, targetRuntime.ClrVersion);

            bool useX86Agent = package.GetSetting(EnginePackageSettings.RunAsX86, false);
            bool debugTests = package.GetSetting(EnginePackageSettings.DebugTests, false);
            bool debugAgent = package.GetSetting(EnginePackageSettings.DebugAgent, false);
            string traceLevel = package.GetSetting(EnginePackageSettings.InternalTraceLevel, "Off");
            bool loadUserProfile = package.GetSetting(EnginePackageSettings.LoadUserProfile, false);
            
            // Set options that need to be in effect before the package
            // is loaded by using the command line.
            string agentArgs = "--pid=" + Process.GetCurrentProcess().Id.ToString();
            if (debugAgent)
                agentArgs += " --debug-agent";
            if (traceLevel != "Off")
                agentArgs += " --trace:" + traceLevel;

            log.Info("Getting {0} agent for use under {1}", useX86Agent ? "x86" : "standard", targetRuntime);

            if (!targetRuntime.IsAvailable)
                throw new ArgumentException(
                    string.Format("The {0} framework is not available", targetRuntime),
                    "framework");

            string agentExePath = GetTestAgentExePath(useX86Agent);

            if (agentExePath == null)
                throw new ArgumentException(
                    string.Format("NUnit components for version {0} of the CLR are not installed",
                    targetRuntime.ClrVersion.ToString()), "targetRuntime");

            log.Debug("Using nunit-agent at " + agentExePath);

            Process p = new Process();
            p.StartInfo.UseShellExecute = false;
            p.StartInfo.CreateNoWindow = true;
            Guid agentId = Guid.NewGuid();
            string arglist = agentId.ToString() + " " + ServerUrl + " " + agentArgs;

            targetRuntime = RuntimeFramework.GetBestAvailableFramework(targetRuntime);

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
            Guid agentId = LaunchAgentProcess(package);

            log.Debug( "Waiting for agent {0} to register", agentId.ToString("B") );

            int pollTime = 200;
            bool infinite = waitTime == Timeout.Infinite;

            while( infinite || waitTime > 0 )
            {
                Thread.Sleep( pollTime );
                if ( !infinite ) waitTime -= pollTime;
                ITestAgent agent = _agentData[agentId].Agent;
                if ( agent != null )
                {
                    log.Debug( "Returning new agent {0}", agentId.ToString("B") );
                    return agent;
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

            string agentExePath = Path.Combine(engineDir, agentName);
            return File.Exists(agentExePath) ? agentExePath : null;
        }

        #endregion

        #region IService Members

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

        #endregion
    }
}
