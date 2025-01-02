// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security;
using NUnit.Common;
using NUnit.Engine;
using NUnit.Engine.Agents;
using NUnit.Engine.Internal;

#if NETFRAMEWORK
using RuntimeInformation = NUnit.Engine.Internal.Backports.RuntimeInformation;
#endif

namespace NUnit.Agent
{
    public class NUnitTestAgent
    {
        static readonly string CURRENT_RUNTIME = RuntimeInformation.FrameworkDescription;
        const string AGENT_RUNTIME =
#if NET8_0
                ".NET 8.0";
#elif NET7_0
                ".NET 7.0";
#elif NET6_0
                ".NET 6.0";
#elif NET5_0
                ".NET 5.0";
#elif NETCOREAPP3_1
                ".NET Core 3.1";
#elif NET462
                ".NET 4.6.2";
#elif NET20
                ".NET 2.0";
#endif

        static Guid AgentId;
        static string? AgencyUrl;
        static Process? AgencyProcess;
        static RemoteTestAgent? Agent;

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        public static void Main(string[] args)
        {
            AgentId = new Guid(args[0]);
            AgencyUrl = args[1];

            var traceLevel = InternalTraceLevel.Off;
            var pid = Process.GetCurrentProcess().Id;
            var debugArgPassed = false;
            var workDirectory = string.Empty;
            var agencyPid = string.Empty;

            for (int i = 2; i < args.Length; i++)
            {
                string arg = args[i];

                // NOTE: we can test these strings exactly since
                // they originate from the engine itself.
                if (arg == "--debug-agent")
                {
                    debugArgPassed = true;
                }
                else if (arg.StartsWith("--trace="))
                {
                    traceLevel = (InternalTraceLevel)Enum.Parse(typeof(InternalTraceLevel), arg.Substring(8));
                }
                else if (arg.StartsWith("--pid="))
                {
                    agencyPid = arg.Substring(6);
                }
                else if (arg.StartsWith("--work="))
                {
                    workDirectory = arg.Substring(7);
                }
            }

            var logName = $"nunit-agent_{pid}.log";
            //InternalTrace.Initialize(Path.Combine(workDirectory, logName), traceLevel);
            Logger log = InternalTrace.GetLogger(typeof(NUnitTestAgent));

            log.Info($"Agent process {pid} starting");
            log.Info($"Running {AGENT_RUNTIME} agent under {CURRENT_RUNTIME}");

            if (debugArgPassed)
                TryLaunchDebugger(log);

            AgencyProcess = LocateAgencyProcess(log, agencyPid);

            log.Info("Starting RemoteTestAgent");
            Agent = new RemoteTestAgent(AgentId);
            Agent.Transport =
#if NETFRAMEWORK
                new Engine.Communication.Transports.Remoting.TestAgentRemotingTransport(Agent, AgencyUrl);
#else
                new Engine.Communication.Transports.Tcp.TestAgentTcpTransport(Agent, AgencyUrl);
#endif

            try
            {
                if (Agent.Start())
                    WaitForStop(log, Agent, AgencyProcess);
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
            log.Info("Agent process {0} exiting cleanly", pid);

            Environment.Exit(AgentExitCodes.OK);
        }

        private static Process LocateAgencyProcess(Logger log, string agencyPid)
        {
            var agencyProcessId = int.Parse(agencyPid);
            try
            {
                return Process.GetProcessById(agencyProcessId);
            }
            catch (Exception e)
            {
                log.Error($"Unable to connect to agency process with PID: {agencyProcessId}");
                log.Error($"Failed with exception: {e.Message} {e.StackTrace}");
                Environment.Exit(AgentExitCodes.UNABLE_TO_LOCATE_AGENCY);
                return null; // Will never reach here
            }
        }

        private static void WaitForStop(Logger log, RemoteTestAgent agent, Process agencyProcess)
        {
            log.Debug("Waiting for stopSignal");

            while (!agent.WaitForStop(500))
            {
                if (agencyProcess.HasExited)
                {
                    log.Error("Parent process has been terminated.");
                    Environment.Exit(AgentExitCodes.PARENT_PROCESS_TERMINATED);
                }
            }

            log.Debug("Stop signal received");
        }

        private static void TryLaunchDebugger(Logger log)
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
