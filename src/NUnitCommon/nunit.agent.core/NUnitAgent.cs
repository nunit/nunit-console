// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using System;
using System.Diagnostics;
using System.IO;
using System.Security;
using System.Reflection;
using NUnit.Engine.Communication.Transports.Tcp;

namespace NUnit.Agents
{
    public class NUnitAgent<TAgent>
    {
        static readonly int _pid = Process.GetCurrentProcess().Id;
        static readonly Logger log = InternalTrace.GetLogger(typeof(NUnitAgent<TAgent>));

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        public static void Execute(string[] args)
        {
            var options = new AgentOptions(args);
            var logName = $"nunit-agent_{_pid}.log";

            InternalTrace.Initialize(Path.Combine(options.WorkDirectory, logName), options.TraceLevel);
            log.Info($"{typeof(TAgent).Name} process {_pid} starting");
            log.Info($"  Agent Path: {Assembly.GetExecutingAssembly().Location}");

            if (options.DebugAgent || options.DebugTests)
                TryLaunchDebugger();

            if (!string.IsNullOrEmpty(options.AgencyUrl))
                RegisterAndWaitForCommands(options);
            else if (options.Files.Count != 0)
                new AgentDirectRunner(options).ExecuteTestsDirectly();
            else
                throw new ArgumentException("No file specified for direct execution");
        }

        private static void RegisterAndWaitForCommands(AgentOptions options)
        {
            log.Info($"  AgentId:   {options.AgentId}");
            log.Info($"  AgencyUrl: {options.AgencyUrl}");
            log.Info($"  AgencyPid: {options.AgencyPid}");

            log.Info("Starting RemoteTestAgent");
            TestAgentTcpTransport transport = new(options.AgentId, options.AgencyUrl);

            try
            {
                if (transport.Start())
                {
                    WaitForStop(transport, options.AgencyPid);
                }
                else
                {
                    log.Error("Failed to start RemoteTestAgent");
                    Environment.Exit(AgentExitCodes.FAILED_TO_START_REMOTE_AGENT);
                }
            }
            catch (Exception ex)
            {
                log.Error("Exception in RemoteTestAgent. {0}", ExceptionHelper.BuildMessageAndStackTrace(ex));
                Environment.Exit(AgentExitCodes.UNEXPECTED_EXCEPTION);
            }
            log.Info("Agent process {0} exiting cleanly", _pid);

            Environment.Exit(AgentExitCodes.OK);
        }

        private static Process LocateAgencyProcess(string agencyPid)
        {
            try
            {
                var agencyProcessId = int.Parse(agencyPid);
                return Process.GetProcessById(agencyProcessId);
            }
            catch (Exception e)
            {
                log.Error($"Unable to connect to agency process with PID: {agencyPid}");
                log.Error($"Failed with exception: {e}");
                Environment.Exit(AgentExitCodes.UNABLE_TO_LOCATE_AGENCY);

                throw;
            }
        }

        private static void WaitForStop(TestAgentTcpTransport transport, string agencyPid)
        {
            Process agencyProcess = LocateAgencyProcess(agencyPid);

            log.Debug("Waiting for stopSignal");

            while (!transport.WaitForStop(500))
            {
                if (agencyProcess.HasExited)
                {
                    log.Error("Parent process has been terminated.");
                    transport.Stop();
                    Environment.Exit(AgentExitCodes.PARENT_PROCESS_TERMINATED);
                }
            }

            log.Debug("Stop signal received");
        }

        private static void TryLaunchDebugger()
        {
            if (Debugger.IsAttached)
                return;

            try
            {
                Debugger.Launch();
            }
            catch (SecurityException se)
            {
                if (InternalTrace.Initialized)
                {
                    log.Error($"System.Security.Permissions.UIPermission is not set to start the debugger. {se} {se.StackTrace}");
                }
                Environment.Exit(AgentExitCodes.DEBUGGER_SECURITY_VIOLATION);
            }
            catch (NotImplementedException nie) //Debugger is not implemented on mono
            {
                if (InternalTrace.Initialized)
                {
                    log.Error($"Debugger is not available on all platforms. {nie} {nie.StackTrace}");
                }
                Environment.Exit(AgentExitCodes.DEBUGGER_NOT_IMPLEMENTED);
            }
        }
    }
}
