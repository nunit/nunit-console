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
using System.IO;
using System.Net;
using System.Security;
using NUnit.Common;
using NUnit.Engine;
using NUnit.Engine.Agent;
using NUnit.Engine.Internal;
using NUnit.Engine.Services;

namespace NUnit.Agent
{
    public static class Program
    {
        private static Logger log;

        [STAThread]
        public static void Main(string[] args)
        {
            var traceLevel = InternalTraceLevel.Off;
            var pid = Process.GetCurrentProcess().Id;
            var debugArgPassed = false;
            var workDirectory = string.Empty;
            var parentPid = string.Empty;

            for (int i = 0; i < args.Length; i++)
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
                    parentPid = arg.Substring(6);
                }
                else if (arg.StartsWith("--work="))
                {
                    workDirectory = arg.Substring(7);
                }
            }

            var logName = $"nunit-agent_{pid}.log";
            InternalTrace.Initialize(Path.Combine(workDirectory, logName), traceLevel);
            log = InternalTrace.GetLogger(typeof(Program));

            if (debugArgPassed)
                TryLaunchDebugger();

            var parentProcess = LocateParentProcess(int.Parse(parentPid));
            if (parentProcess == null)
                Environment.Exit(AgentExitCodes.UNABLE_TO_LOCATE_PARENT_PROCESS);

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
            // can access its internals as needed.
            var engine = new TestEngine
            {
                InternalTraceLevel = traceLevel
            };

            // Custom Service Initialization
            engine.Services.Add(new ExtensionService());
            engine.Services.Add(new DomainManager());
            engine.Services.Add(new InProcessTestRunnerFactory());
            engine.Services.Add(new DriverService());

            // Initialize Services
            log.Info("Initializing Services");
            engine.Initialize();

            log.Info("Starting AgentServer");
            try
            {
                using (var server = AgentServer.Start(IPAddress.Loopback, engine.Services.GetService<ITestRunnerFactory>()))
                {
                    // Write to the console to let the parent process know which port Windows assigned
                    Console.WriteLine("Listening on " + server.ListeningOn);
                    log.Info("Listening on " + server.ListeningOn);

                    Console.WriteLine("Type 'stop' to exit.");

                    while (true)
                    {
                        var line = Console.ReadLine();
                        if (line is null)
                        {
                            if (parentProcess.HasExited)
                            {
                                log.Error("Parent process has been terminated.");
                                Environment.Exit(AgentExitCodes.PARENT_PROCESS_TERMINATED);
                            }

                            log.Info("Stopping AgentServer in response to standard input being closed.");
                            break;
                        }

                        if ("stop".Equals(line, StringComparison.OrdinalIgnoreCase))
                        {
                            log.Info("Stopping AgentServer in response to 'stop' command from standard input.");
                            break;
                        }

                        log.Info($"Ignoring unrecognized command '{line}' from standard input.");
                        Console.WriteLine($"Unrecognized command '{line}'.");
                    }
                }
            }
            catch (Exception ex)
            {
                log.Error("Unexpected exception. {0}", ExceptionHelper.BuildMessageAndStackTrace(ex));
                Environment.Exit(AgentExitCodes.UNEXPECTED_EXCEPTION);
            }
            log.Info("Agent process {0} exiting cleanly", pid);

            Environment.Exit(AgentExitCodes.OK);
        }

        private static Process LocateParentProcess(int parentPid)
        {
            try
            {
                return Process.GetProcessById(parentPid);
            }
            catch (Exception ex)
            {
                log.Error($"Unable to find parent process with PID: {parentPid}");
                log.Error($"Failed with exception: {ex.Message} {ex.StackTrace}");
                return null;
            }
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
