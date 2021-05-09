// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

#if NETFRAMEWORK
using System;
using System.Threading;
using System.Diagnostics;
using NUnit.Common;
using NUnit.Engine.Internal;
using NUnit.Engine.Communication.Transports.Remoting;
using NUnit.Engine.Communication.Transports.Tcp;

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
    public partial class TestAgency : ITestAgency, IService
    {
        private static readonly Logger log = InternalTrace.GetLogger(typeof(TestAgency));

        private const int NORMAL_TIMEOUT = 30000;               // 30 seconds
        private const int DEBUG_TIMEOUT = NORMAL_TIMEOUT * 10;  // 5 minutes

        private readonly AgentStore _agentStore = new AgentStore();

        private IRuntimeFrameworkService _runtimeService;

        // Transports used for various target runtimes
        private TestAgencyRemotingTransport _remotingTransport; // .NET Framework
        private TestAgencyTcpTransport _tcpTransport; // .NET Standard 2.0

        internal virtual string RemotingUrl => _remotingTransport.ServerUrl;
        internal virtual string TcpEndPoint => _tcpTransport.ServerUrl;

        public TestAgency() : this( "TestAgency", 0 ) { }

        public TestAgency( string uri, int port )
        {
            _remotingTransport = new TestAgencyRemotingTransport(this, uri, port);
            _tcpTransport = new TestAgencyTcpTransport(this, port);
        }

        public void Register(ITestAgent agent)
        {
            _agentStore.Register(agent);
        }

        public ITestAgent GetAgent(ITestPackage package)
        {
            // Target Runtime must be specified by this point
            string runtimeSetting = package.GetSetting(EnginePackageSettings.TargetRuntimeFramework, "");
            Guard.OperationValid(runtimeSetting.Length > 0, "LaunchAgentProcess called with no runtime specified");

            // If target runtime is not available, something went wrong earlier.
            // We list all available frameworks to use in debugging.
            var targetRuntime = RuntimeFramework.Parse(runtimeSetting);
            if (!_runtimeService.IsAvailable(targetRuntime.Id))
            {
                string msg = $"The {targetRuntime} framework is not available.\r\nAvailable frameworks:";
                foreach (var runtime in RuntimeFramework.AvailableFrameworks)
                    msg += $" {runtime}";
                throw new ArgumentException(msg);
            }

            var agentId = Guid.NewGuid();
            var agentProcess = new AgentProcess(this, package, agentId);

            agentProcess.Exited += (sender, e) => OnAgentExit((Process)sender, agentId);

            agentProcess.Start();
            log.Debug("Launched Agent process {0} - see nunit-agent_{0}.log", agentProcess.Id);
            log.Debug("Command line: \"{0}\" {1}", agentProcess.StartInfo.FileName, agentProcess.StartInfo.Arguments);

            _agentStore.AddAgent(agentId, agentProcess);

            log.Debug($"Waiting for agent {agentId:B} to register");

            const int pollTime = 200;

            // Increase the timeout to give time to attach a debugger
            bool debug = package.GetSetting(EnginePackageSettings.DebugAgent, false) ||
                         package.GetSetting(EnginePackageSettings.PauseBeforeRun, false);

            int waitTime = debug ? DEBUG_TIMEOUT : NORMAL_TIMEOUT;

            // Wait for agent registration based on the agent actually getting processor time to avoid falling over
            // under process starvation.
            while (waitTime > agentProcess.TotalProcessorTime.TotalMilliseconds && !agentProcess.HasExited)
            {
                Thread.Sleep(pollTime);

                if (_agentStore.IsReady(agentId, out var agent))
                {
                    log.Debug($"Returning new agent {agentId:B}");

                    return new TestAgentRemotingProxy(agent, agentId);
                }
            }

            return null;
        }

        internal bool IsAgentProcessActive(Guid agentId, out Process process)
        {
            return _agentStore.IsAgentProcessActive(agentId, out process);
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

        // TODO: it would be better if we had a list of transports to start and stop!

        public void StopService()
        {
            try
            {
                _remotingTransport.Stop();
                _tcpTransport.Stop();
            }
            finally
            {
                Status = ServiceStatus.Stopped;
            }
        }

        public void StartService()
        {
            _runtimeService = ServiceContext.GetService<IRuntimeFrameworkService>();
            if (_runtimeService == null)
                Status = ServiceStatus.Error;
            else
                try
                {
                _remotingTransport.Start();
                    _tcpTransport.Start();
                    Status = ServiceStatus.Started;
            }
            catch
            {
                Status = ServiceStatus.Error;
                throw;
            }
        }

        // TODO: Need to figure out how to incorporate this in the
        // new structure, if possible. Originally, Release was only
        // called when the nested AgentLease class was disposed.
        //public void Release(Guid agentId, ITestAgent agent)
        //{
        //    if (_agentStore.IsAgentProcessActive(agentId, out var process))
        //    {
        //        try
        //        {
        //            log.Debug("Stopping remote agent");
        //            agent.Stop();
        //        }
        //        catch (SocketException ex)
        //        {
        //            int? exitCode;
        //            try
        //            {
        //                exitCode = process.ExitCode;
        //            }
        //            catch (NotSupportedException)
        //            {
        //                exitCode = null;
        //            }

        //            if (exitCode == 0)
        //            {
        //                log.Warning("Agent connection was forcibly closed. Exit code was 0, so agent shutdown OK");
        //            }
        //            else
        //            {
        //                var stopError = $"Agent connection was forcibly closed. Exit code was {exitCode?.ToString() ?? "unknown"}. {Environment.NewLine}{ExceptionHelper.BuildMessageAndStackTrace(ex)}";
        //                log.Error(stopError);
        //                throw new NUnitEngineUnloadException(stopError, ex);
        //            }
        //        }
        //        catch (Exception ex)
        //        {
        //            var stopError = "Failed to stop the remote agent." + Environment.NewLine + ExceptionHelper.BuildMessageAndStackTrace(ex);
        //            log.Error(stopError);
        //            throw new NUnitEngineUnloadException(stopError, ex);
        //        }
        //    }
        //}
    }
}
#endif