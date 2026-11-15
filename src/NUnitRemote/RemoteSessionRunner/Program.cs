// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using System;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Engine;
using NUnit.Remote.Deployment;

namespace NUnit.Remote.Execution
{
    /// <summary>Runs one explicit local deployment plan through staging, execution and verified cleanup.</summary>
    internal static class Program
    {
        private static readonly JsonSerializerOptions Json = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true,
            UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow
        };

        private static int Main(string[] args)
        {
            try
            {
                if (args.Length is < 1 or > 2 || !Path.IsPathFullyQualified(args[0])
                    || (args.Length == 2 && args[1] != "--stdio" && args[1] != "--serve"))
                    throw new ArgumentException("Supply an absolute session-plan filename and optional --stdio or --serve mode.");
                bool stdio = args.Length == 2 && args[1] == "--stdio";
                bool serve = args.Length == 2 && args[1] == "--serve";
                var plan = JsonSerializer.Deserialize<RunPlan>(File.ReadAllText(args[0]), Json)
                    ?? throw new ArgumentException("The session plan is empty.");
                if (plan.RunTimeoutMilliseconds <= 0)
                    throw new ArgumentException("Supply a positive run timeout.");
                using var cancellation = new CancellationTokenSource();
                ConsoleCancelEventHandler handler = (_, signal) =>
                {
                    signal.Cancel = true;
                    cancellation.Cancel();
                };
                Console.CancelKeyPress += handler;
                if (stdio)
                    _ = Task.Run(() => ReadControlAsync(cancellation));
                try
                {
                    return serve ? Serve(plan, cancellation.Token) : Run(plan, stdio, cancellation.Token);
                }
                finally
                {
                    Console.CancelKeyPress -= handler;
                }
            }
            catch (Exception exception)
            {
                Console.Error.WriteLine(exception);
                return 2;
            }
        }

        /// <summary>
        /// Stages a provider for a frontend on another machine. Readiness publishes
        /// backend paths unchanged. The owning channel or bounded deadline ends the
        /// session; only confirmed host shutdown and a native audit permit cleanup.
        /// A zero exit confirms hosting/cleanup, not the frontend's test outcome.
        /// </summary>
        private static int Serve(RunPlan plan, CancellationToken cancellation)
        {
            using var session = SessionDeployment.Stage(plan.Deployment);
            var outcome = new ExecutionReceipt { Outcome = "HostedSessionCompleted", ExitCode = 0 };
            try
            {
                session.Start();
                var ready = System.Xml.Linq.XElement.Load(Path.Combine(plan.Deployment.EvidenceDirectory, "host.ready.xml"));
                Console.WriteLine(JsonSerializer.Serialize(new
                {
                    kind = "ready",
                    port = (int)ready.Attribute("port")!,
                    assemblyPath = session.AssemblyPath,
                    workDirectory = Path.Combine(session.Directory, "work"),
                    owner = session.Receipt.Owner,
                    manifestSha256 = session.Receipt.ManifestSha256
                }));
                try
                {
                    Task.Run(Console.ReadLine).WaitAsync(TimeSpan.FromMilliseconds(plan.RunTimeoutMilliseconds), cancellation)
                        .GetAwaiter().GetResult();
                }
                catch (TimeoutException)
                {
                    outcome.Outcome = "HostedSessionTimedOut";
                    outcome.ExitCode = 4;
                }
                catch (OperationCanceledException) when (cancellation.IsCancellationRequested)
                {
                    outcome.Outcome = "HostedSessionCancelled";
                    outcome.ExitCode = 3;
                }
            }
            catch (Exception exception)
            {
                outcome.Outcome = "InfrastructureError";
                outcome.ExitCode = 2;
                outcome.Error = exception.ToString();
                Console.Error.WriteLine(exception);
            }
            finally
            {
                outcome.Cleaned = session.CloseAndCleanup();
                if (!outcome.Cleaned)
                {
                    outcome.Outcome = "InfrastructureError";
                    outcome.ExitCode = 2;
                }
                File.WriteAllText(Path.Combine(plan.Deployment.EvidenceDirectory, "hosting.json"), JsonSerializer.Serialize(outcome, Json));
            }
            Console.WriteLine(JsonSerializer.Serialize(new { kind = "complete", outcome }));
            return outcome.ExitCode;
        }

        /// <summary>
        /// Accepts cooperative cancellation from an owning frontend. EOF also requests
        /// cancellation, so losing the frontend does not leave its coordinator running tests.
        /// This is a frontend control channel, separate from the NUnit agent transport.
        /// </summary>
        private static async Task ReadControlAsync(CancellationTokenSource cancellation)
        {
            try
            {
                while (true)
                {
                    string? command = await Console.In.ReadLineAsync().ConfigureAwait(false);
                    if (command is null || command == "cancel")
                    {
                        cancellation.Cancel();
                        return;
                    }
                    // Unknown commands cannot grant any additional capability.
                    cancellation.Cancel();
                    return;
                }
            }
            catch (ObjectDisposedException)
            {
                // The run may have completed while its owner closed the input stream.
            }
            catch (IOException)
            {
                try
                {
                    cancellation.Cancel();
                }
                catch (ObjectDisposedException)
                {
                    // Completion has already disposed the cancellation source.
                }
            }
        }

        private static int Run(RunPlan plan, bool stdio, CancellationToken cancellation)
        {
            SessionDeployment? session = null;
            var outcome = new ExecutionReceipt();
            try
            {
                session = SessionDeployment.Stage(plan.Deployment);
                session.Start();
                var package = new TestPackage(session.AssemblyPath);
                package.AddSetting(new SettingDefinition<string>("RemoteServerDefinition", string.Empty)
                    .WithValue(session.ServerDefinitionPath), true);
                using var engine = new TestEngine
                {
                    WorkDirectory = plan.Deployment.EvidenceDirectory,
                    InternalTraceLevel = InternalTraceLevel.Off
                };
                using var runner = engine.GetRunner(package);
                using var events = new EventJournal(Path.Combine(plan.Deployment.EvidenceDirectory, "events.jsonl"), stdio);
                var run = runner.RunAsync(events, new TestFilter(plan.FilterXml));
                var deadline = Stopwatch.StartNew();
                string? stopped = null;
                while (!run.Wait(50))
                {
                    if (!cancellation.IsCancellationRequested && deadline.ElapsedMilliseconds < plan.RunTimeoutMilliseconds)
                        continue;
                    stopped = cancellation.IsCancellationRequested ? "Cancelled" : "TimedOut";
                    runner.RequestStop();
                    if (!run.Wait(5000))
                    {
                        runner.ForcedStop();
                        if (!run.Wait(15000))
                            throw new TimeoutException("The requested stop did not complete; release remains unconfirmed.");
                    }
                    break;
                }
                string xml = run.Result.OuterXml;
                File.WriteAllText(Path.Combine(plan.Deployment.EvidenceDirectory, "result.xml"), xml);
                outcome.TestResult = run.Result.Attributes!["result"]!.Value;
                outcome.Outcome = stopped ?? (outcome.TestResult == "Failed" ? "TestFailures" : "Passed");
                outcome.ExitCode = stopped == "TimedOut" ? 4 : stopped == "Cancelled" ? 3 : outcome.TestResult == "Failed" ? 1 : 0;
                outcome.Events = events.Count;
            }
            catch (Exception exception)
            {
                outcome.Outcome = "InfrastructureError";
                outcome.ExitCode = 2;
                outcome.Error = exception.ToString();
                Console.Error.WriteLine(exception);
            }
            finally
            {
                if (session is not null)
                {
                    outcome.Cleaned = session.CloseAndCleanup();
                    if (!outcome.Cleaned)
                    {
                        outcome.Outcome = "InfrastructureError";
                        outcome.ExitCode = 2;
                    }
                    try
                    {
                        session.Dispose();
                    }
                    catch (Exception exception)
                    {
                        outcome.Error += "\n" + exception;
                        outcome.Outcome = "InfrastructureError";
                        outcome.ExitCode = 2;
                    }
                    File.WriteAllText(Path.Combine(plan.Deployment.EvidenceDirectory, "execution.json"),
                        JsonSerializer.Serialize(outcome, Json));
                }
            }
            Console.WriteLine(stdio
                ? JsonSerializer.Serialize(new { kind = "complete", outcome })
                : JsonSerializer.Serialize(outcome, Json));
            return outcome.ExitCode;
        }

        /// <summary>Provides explicit local inputs and a filter without changing NUnit's provider protocol.</summary>
        private sealed class RunPlan
        {
            public required DeploymentDefinition Deployment { get; init; }
            public string FilterXml { get; init; } = "<filter />";
            public int RunTimeoutMilliseconds { get; init; } = 30000;
        }

        /// <summary>Keeps test outcome separate from infrastructure cleanup status.</summary>
        private sealed class ExecutionReceipt
        {
            public string Outcome { get; set; } = "Preparing";
            public string? TestResult { get; set; }
            public int ExitCode { get; set; } = 2;
            public bool Cleaned { get; set; }
            public int Events { get; set; }
            public string? Error { get; set; }
        }

        /// <summary>Persists sequenced, escaped XML events outside the disposable deployment.</summary>
        private sealed class EventJournal : ITestEventListener, IDisposable
        {
            private readonly StreamWriter _writer;
            private readonly object _gate = new object();
            private readonly bool _stream;

            internal EventJournal(string path, bool stream)
            {
                _writer = new StreamWriter(path, append: false) { AutoFlush = true };
                _stream = stream;
            }
            internal int Count { get; private set; }

            public void OnTestEvent(string report)
            {
                lock (_gate)
                {
                    _writer.WriteLine(JsonSerializer.Serialize(new EventRecord(++Count, report)));
                    if (_stream)
                        Console.WriteLine(JsonSerializer.Serialize(new { kind = "event", sequence = Count, xml = report }));
                }
            }

            public void Dispose() => _writer.Dispose();

            private sealed record EventRecord(int Sequence, string Xml);
        }
    }
}
