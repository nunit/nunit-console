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

        private readonly AgentStore _agentStore = new AgentStore();

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

        public void Register(Guid agentId, ITestAgent agent)
        {
            _agentStore.Register(agentId, agent);
        }

        public ITestAgent GetAgent(TestPackage package, int waitTime)
        {
            // TODO: Decide if we should reuse agents
            return CreateRemoteAgent(package, waitTime);
        }

        internal bool IsAgentProcessActive(ITestAgent agent, out Process process)
        {
            return _agentStore.IsAgentProcessActive(((RemoteTestAgentProxy)agent).Id, out process);
        }

        private ITestAgent CreateRemoteAgent(TestPackage package, int waitTime)
        {
            var agentId = Guid.NewGuid();
            //var process = LaunchAgentProcess(package, agentId);
            var process = new AgentProcess(this, package, agentId);

            process.Exited += (sender, e) => OnAgentExit((Process)sender, agentId);

            process.Start();
            log.Debug("Launched Agent process {0} - see testcentric-agent_{0}.log", process.Id);
            log.Debug("Command line: \"{0}\" {1}", process.StartInfo.FileName, process.StartInfo.Arguments);

            _agentStore.AddAgent(agentId, process);

            log.Debug($"Waiting for agent {agentId:B} to register");

            const int pollTime = 200;

            // Wait for agent registration based on the agent actually getting processor time to avoid falling over
            // under process starvation.
            while (waitTime > process.TotalProcessorTime.TotalMilliseconds && !process.HasExited)
            {
                Thread.Sleep(pollTime);

                if (_agentStore.IsReady(agentId, out var agent))
                {
                    log.Debug($"Returning new agent {agentId:B}");
                    return new RemoteTestAgentProxy(agent, agentId);
                }
            }

            return null;
        }

        private void OnAgentExit(Process process, Guid agentId)
        {
            _agentStore.MarkTerminated(agentId);

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
                case AgentExitCodes.STACK_OVERFLOW_EXCEPTION:
                    if (Environment.OSVersion.Platform == PlatformID.Win32NT)
                    {
                        errorMsg = "Remote test agent was terminated due to a stack overflow.";
                    }
                    else
                    {
                        errorMsg = $"Remote test agent exited with non-zero exit code {process.ExitCode}";
                    }
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