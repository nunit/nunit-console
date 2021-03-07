// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

#if NETFRAMEWORK
using System;
using System.Threading;
using System.Diagnostics;
using NUnit.Common;
using NUnit.Engine.Internal;
using System.Net.Sockets;

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
    public partial class TestAgency : ServerBase, ITestAgency, IService
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

        public IAgentLease GetAgent(TestPackage package, int waitTime)
        {
            // TODO: Decide if we should reuse agents
            return CreateRemoteAgent(package, waitTime);
        }

        private IAgentLease CreateRemoteAgent(TestPackage package, int waitTime)
        {
            var agentId = Guid.NewGuid();
            var process = new AgentProcess(this, package, agentId);

            process.Exited += (sender, e) => OnAgentExit((Process)sender, agentId);

            process.Start();
            log.Debug("Launched Agent process {0} - see nunit-agent_{0}.log", process.Id);
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
                    return new AgentLease(this, agentId, agent);
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

        private void Release(Guid agentId, ITestAgent agent)
        {
            if (_agentStore.IsAgentProcessActive(agentId, out var process))
            {
                try
                {
                    log.Debug("Stopping remote agent");
                    agent.Stop();
                }
                catch (SocketException ex)
                {
                    int? exitCode;
                    try
                    {
                        exitCode = process.ExitCode;
                    }
                    catch (NotSupportedException)
                    {
                        exitCode = null;
                    }

                    if (exitCode == 0)
                    {
                        log.Warning("Agent connection was forcibly closed. Exit code was 0, so agent shutdown OK");
                    }
                    else
                    {
                        var stopError = $"Agent connection was forcibly closed. Exit code was {exitCode?.ToString() ?? "unknown"}. {Environment.NewLine}{ExceptionHelper.BuildMessageAndStackTrace(ex)}";
                        log.Error(stopError);
                        throw new NUnitEngineUnloadException(stopError, ex);
                    }
                }
                catch (Exception ex)
                {
                    var stopError = "Failed to stop the remote agent." + Environment.NewLine + ExceptionHelper.BuildMessageAndStackTrace(ex);
                    log.Error(stopError);
                    throw new NUnitEngineUnloadException(stopError, ex);
                }
            }
        }
    }
}
#endif