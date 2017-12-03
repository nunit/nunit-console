// ***********************************************************************
// Copyright (c) 2008 Charlie Poole, Rob Prouse
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
using System.Diagnostics;
using System.Security;
using NUnit.Common;
using NUnit.Engine;
using NUnit.Engine.Agents;
using NUnit.Engine.Internal;
using NUnit.Engine.Services;

namespace NUnit.Agent
{
    public class NUnitTestAgent
    {
        static Guid AgentId;
        static string AgencyUrl;
        static Process AgencyProcess;
        static RemoteTestAgent Agent;
        private static Logger log;

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        public static void Main(string[] args)
        {
            AgentId = new Guid(args[0]);
            AgencyUrl = args[1];

            InternalTraceLevel traceLevel = InternalTraceLevel.Off;
            int pid = Process.GetCurrentProcess().Id;
            bool debugArgPassed = false;

            for (int i = 2; i < args.Length; i++)
            {
                string arg = args[i];

                // NOTE: we can test these strings exactly since
                // they originate from the engine itself.
                if (arg == "--debug-agent")
                {
                    debugArgPassed = true;
                }
                else if (arg.StartsWith("--trace:"))
                {
                    traceLevel = (InternalTraceLevel)Enum.Parse(typeof(InternalTraceLevel), arg.Substring(8));
                }
                else if (arg.StartsWith("--pid="))
                {
                    int agencyProcessId = int.Parse(arg.Substring(6));
                    AgencyProcess = Process.GetProcessById(agencyProcessId);
                }
            }

            InternalTrace.Initialize($"nunit-agent_{pid}.log", traceLevel);
            log = InternalTrace.GetLogger(typeof(NUnitTestAgent));

            if (debugArgPassed)
                TryLaunchDebugger();

            log.Info("Agent process {0} starting", pid);
            log.Info("Running under version {0}, {1}",
                Environment.Version,
                RuntimeFramework.CurrentFramework.DisplayName);

            // Restore the COMPLUS_Version env variable if it's been overridden by TestAgency::LaunchAgentProcess
            try
            {
              string cpvOriginal = Environment.GetEnvironmentVariable("TestAgency_COMPLUS_Version_Original");
              if(!string.IsNullOrEmpty(cpvOriginal))
              {
                log.Debug("Agent process has the COMPLUS_Version environment variable value \"{0}\" overridden with \"{1}\", restoring the original value.", cpvOriginal, Environment.GetEnvironmentVariable("COMPLUS_Version"));
                Environment.SetEnvironmentVariable("TestAgency_COMPLUS_Version_Original", null, EnvironmentVariableTarget.Process); // Erase marker
                Environment.SetEnvironmentVariable("COMPLUS_Version", (cpvOriginal == "NULL" ? null : cpvOriginal), EnvironmentVariableTarget.Process); // Restore original (which might be n/a)
              }
            }
            catch(Exception ex)
            {
              log.Warning("Failed to restore the COMPLUS_Version variable. " + ex.Message); // Proceed with running tests anyway
            }

            // Create TestEngine - this program is
            // conceptually part of  the engine and
            // can access it's internals as needed.
            TestEngine engine = new TestEngine();

            // TODO: We need to get this from somewhere. Argument?
            engine.InternalTraceLevel = InternalTraceLevel.Debug;

            // Custom Service Initialization
            //log.Info("Adding Services");
            engine.Services.Add(new SettingsService(false));
            engine.Services.Add(new ExtensionService());
            engine.Services.Add(new ProjectService());
            engine.Services.Add(new DomainManager());
            engine.Services.Add(new InProcessTestRunnerFactory());
            engine.Services.Add(new DriverService());

            // Initialize Services
            log.Info("Initializing Services");
            engine.Initialize();

            log.Info("Starting RemoteTestAgent");
            Agent = new RemoteTestAgent(AgentId, AgencyUrl, engine.Services);

            try
            {
                if (Agent.Start())
                    WaitForStop();
                else
                {
                    log.Error("Failed to start RemoteTestAgent");
                    Environment.Exit(AgentExitCodes.FAILED_TO_START_REMOTE_AGENT);
                }
            }
            catch (Exception ex)
            {
                log.Error("Exception in RemoteTestAgent", ex);
                Environment.Exit(AgentExitCodes.UNEXPECTED_EXCEPTION);
            }
            log.Info("Agent process {0} exiting cleanly", pid);

            Environment.Exit(AgentExitCodes.OK);
        }

        private static void WaitForStop()
        {
            log.Debug("Waiting for stopSignal");

            while (!Agent.WaitForStop(500))
            {
                if (AgencyProcess.HasExited)
                {
                    log.Error("Parent process has been terminated.");
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
