// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Common;
using NUnit.Framework;
using NSubstitute;

namespace NUnit.Engine.Services
{
    public class TestAgencyExitTests
    {
        [TestCase(42, false, -1)]
        [TestCase(42, true, -1)]
        [TestCase(42, false, -2)]
        [TestCase(42, true, -2)]
        [TestCase(0, false, 0)]
        [TestCase(0, true, 0)]
        [TestCase(42, false, 0)]
        [TestCase(42, true, 0)]
        [TestCase(0, false, 250)]
        [TestCase(0, true, 250)]
        [TestCase(42, false, 250)]
        [TestCase(42, true, 250)]
        public void AgentExitIsReportedWithoutThrowingFromCallback(int exitCode, bool enableEvents, int delay)
        {
            using var probe = new Process
            {
                StartInfo = CreateStartInfo("--agent-exit-probe", exitCode, enableEvents, delay)
            };
            probe.StartInfo.RedirectStandardOutput = true;
            probe.StartInfo.RedirectStandardError = true;
            probe.Start();
            var output = probe.StandardOutput.ReadToEndAsync();
            var error = probe.StandardError.ReadToEndAsync();

            if (!probe.WaitForExit(30000))
            {
                probe.Kill();
                Assert.Fail("The isolated acquisition probe did not finish within 30 seconds.");
            }

            string diagnostic = output.GetAwaiter().GetResult() + error.GetAwaiter().GetResult();
            Assert.That(probe.ExitCode, Is.Zero, diagnostic);
            Assert.That(diagnostic, Does.Contain("Agent exit observed: " + exitCode));
        }

        [TestCase(AgentExitCodes.OK, "before registering")]
        [TestCase(AgentExitCodes.PARENT_PROCESS_TERMINATED, "believes agency process has exited")]
        [TestCase(AgentExitCodes.FAILED_TO_START_REMOTE_AGENT, "Failed to start")]
        [TestCase(AgentExitCodes.DEBUGGER_SECURITY_VIOLATION, "System.Security.Permissions.UIPermission")]
        [TestCase(AgentExitCodes.DEBUGGER_NOT_IMPLEMENTED, "not available on platform")]
        [TestCase(AgentExitCodes.UNABLE_TO_LOCATE_AGENCY, "unable to locate agency")]
        [TestCase(AgentExitCodes.UNEXPECTED_EXCEPTION, "--trace=debug")]
        [TestCase(42, "exit code 42")]
        public void PreservesAgentExitDiagnostic(int exitCode, string diagnostic)
        {
            Assert.That(TestAgency.GetAgentExitMessage(exitCode), Does.Contain(diagnostic));
        }

        [Test]
        public void UnknownAgentHasNoExitDiagnostic()
        {
            using var agency = new TestAgency();
            Assert.That(agency.GetAgentExitMessage(Guid.NewGuid()), Is.Null);
        }

        internal static int RunProbe(string[] args)
        {
            int exitCode = int.Parse(args[1], CultureInfo.InvariantCulture);
            bool enableEvents = bool.Parse(args[2]);
            int delay = int.Parse(args[3], CultureInfo.InvariantCulture);
            using var process = new Process
            {
                StartInfo = CreateStartInfo("--agent-exit-child", exitCode, enableEvents, delay),
                EnableRaisingEvents = enableEvents
            };
            process.StartInfo.RedirectStandardInput = delay < 0;
            using var agency = new ExitTestAgency(process, delay < 0);
            var package = new TestPackage("test-assembly.dll");
            package.AddSetting(SettingDefinitions.TargetFrameworkName.WithValue(".NETCoreApp,Version=v10.0"), true);

            try
            {
                agency.GetAgent(package);
                if (delay < 0)
                {
                    agency.Registration!.GetAwaiter().GetResult();
                    if (delay == -2)
                        process.Kill();
                    else
                        process.StandardInput.WriteLine();

                    if (!process.WaitForExit(5000))
                    {
                        process.Kill();
                        Console.Error.WriteLine("Registered agent did not exit.");
                        return 3;
                    }

                    // HasExited also raises Exited when automatic events are disabled.
                    if (!process.HasExited)
                        return 5;

                    Thread.Sleep(250);
                    if (agency.IsAgentActive(agency.AgentId, out _))
                    {
                        Console.Error.WriteLine("Exited agent is still active.");
                        return 4;
                    }

                    string? message = agency.GetAgentExitMessage(agency.AgentId);
                    if (message is null || !message.Contains("exit code " + process.ExitCode))
                    {
                        Console.Error.WriteLine("Registered agent exit diagnostic was lost.");
                        return 6;
                    }

                    process.Dispose();
                    if (agency.GetAgentExitMessage(agency.AgentId) is not null)
                    {
                        Console.Error.WriteLine("Disposed process produced an exit diagnostic.");
                        return 7;
                    }

                    Console.WriteLine("Agent exit observed: " + exitCode);
                    return 0;
                }

                Console.Error.WriteLine("Acquisition unexpectedly returned an agent.");
                return 1;
            }
            catch (NUnitEngineException ex)
            {
                // An exit callback may run on the thread pool or inside HasExited/WaitForExit.
                // Keep the probe separate so the broken callback cannot kill the test runner.
                if (delay < 0 || ex.StackTrace?.Contains("OnAgentExit") == true ||
                    !ex.Message.Contains("exit code " + exitCode))
                {
                    Console.Error.WriteLine(ex);
                    return 2;
                }

                process.WaitForExit();
                Thread.Sleep(250);
                Console.WriteLine("Agent exit observed: " + exitCode);
                return 0;
            }
        }

        internal static int RunChild(string[] args)
        {
            int delay = int.Parse(args[3], CultureInfo.InvariantCulture);
            if (delay < 0)
                Console.ReadLine();
            else
                Thread.Sleep(delay);
            return int.Parse(args[1], CultureInfo.InvariantCulture);
        }

        private static ProcessStartInfo CreateStartInfo(string mode, int exitCode, bool enableEvents, int delay)
        {
            string assemblyPath = typeof(TestAgencyExitTests).Assembly.Location;
            var arguments = new StringBuilder();
#if NETFRAMEWORK
            bool mono = Type.GetType("Mono.Runtime") is not null;
            string executable = mono ? Process.GetCurrentProcess().MainModule!.FileName : assemblyPath;
            if (mono)
                arguments.EscapeProcessArgument(assemblyPath);
#else
            string executable = Environment.GetEnvironmentVariable("DOTNET_HOST_PATH") ?? "dotnet";
            arguments.EscapeProcessArgument(assemblyPath);
#endif
            foreach (string argument in new[]
            {
                mode, exitCode.ToString(CultureInfo.InvariantCulture),
                enableEvents.ToString(), delay.ToString(CultureInfo.InvariantCulture)
            })
            {
                if (arguments.Length > 0)
                    arguments.Append(' ');

                arguments.EscapeProcessArgument(argument);
            }

            return new ProcessStartInfo(executable, arguments.ToString())
            {
                UseShellExecute = false,
                CreateNoWindow = true,
                WorkingDirectory = Path.GetDirectoryName(assemblyPath)!
            };
        }

        private sealed class ExitTestAgency : TestAgency
        {
            private readonly Process _process;
            private readonly bool _register;

            public ExitTestAgency(Process process, bool register)
            {
                _process = process;
                _register = register;
            }

            public Task? Registration { get; private set; }

            public Guid AgentId { get; private set; }

            internal override string TcpEndPoint => "tcp://unused";

            internal override Process CreateAgentProcess(Guid agentId, string agencyUrl, TestPackage package)
            {
                AgentId = agentId;
                if (_register)
                {
                    Registration = Task.Run(() =>
                    {
                        var agent = Substitute.For<ITestAgent>();
                        agent.Id.Returns(agentId);
                        // CreateAgentProcess returns before GetAgent adds the store entry.
                        for (int attempt = 0; attempt < 500; attempt++)
                        {
                            try
                            {
                                Register(agent);
                                return;
                            }
                            catch (ArgumentException)
                            {
                                Thread.Sleep(10);
                            }
                        }

                        throw new InvalidOperationException("Test agent could not register.");
                    });
                }

                return _process;
            }
        }
    }
}
