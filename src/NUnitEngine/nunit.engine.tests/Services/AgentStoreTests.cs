// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

#if NETFRAMEWORK
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using NUnit.Framework;

namespace NUnit.Engine.Services.Tests
{
    public static partial class AgentStoreTests
    {
        private static readonly Process DummyProcess = new Process();
        private static readonly Guid DummyAgentId = Guid.NewGuid();
        private static readonly ITestAgent DummyAgent = new DummyTestAgent();

        [Test]
        public static void IdCannotBeReused()
        {
            var database = new AgentStore();

            database.AddAgent(DummyAgentId, DummyProcess);
            Assert.That(() => database.AddAgent(DummyAgentId, DummyProcess), Throws.ArgumentException.With.Property("ParamName").EqualTo("agentId"));

            database.Register(DummyAgentId, DummyAgent);
            Assert.That(() => database.AddAgent(DummyAgentId, DummyProcess), Throws.ArgumentException.With.Property("ParamName").EqualTo("agentId"));

            database.MarkTerminated(DummyAgentId);
            Assert.That(() => database.AddAgent(DummyAgentId, DummyProcess), Throws.ArgumentException.With.Property("ParamName").EqualTo("agentId"));
        }

        [Test]
        public static void AgentMustBeStartedBeforeRegistering()
        {
            var database = new AgentStore();

            Assert.That(() => database.Register(DummyAgentId, DummyAgent), Throws.ArgumentException.With.Property("ParamName").EqualTo("agent"));
        }

        [Test]
        public static void AgentMustNotRegisterTwice()
        {
            var database = new AgentStore();

            database.AddAgent(DummyAgentId, DummyProcess);
            database.Register(DummyAgentId, DummyAgent);
            Assert.That(() => database.Register(DummyAgentId, DummyAgent), Throws.ArgumentException.With.Property("ParamName").EqualTo("agent"));
        }

        [Test]
        public static void AgentMustNotRegisterAfterTerminating()
        {
            var database = new AgentStore();

            database.AddAgent(DummyAgentId, DummyProcess);
            database.MarkTerminated(DummyAgentId);
            Assert.That(() => database.Register(DummyAgentId, DummyAgent), Throws.ArgumentException.With.Property("ParamName").EqualTo("agent"));
        }

        [Test]
        public static void AgentMustBeStartedBeforeTerminating()
        {
            var database = new AgentStore();

            Assert.That(() => database.MarkTerminated(DummyAgentId), Throws.ArgumentException.With.Property("ParamName").EqualTo("agentId"));
        }

        [Test]
        public static void AgentIsNotReadyWhenNotStarted()
        {
            var database = new AgentStore();

            Assert.That(database.IsReady(DummyAgentId, out _), Is.False);
        }

        [Test]
        public static void AgentIsNotReadyWhenStarted()
        {
            var database = new AgentStore();

            database.AddAgent(DummyAgentId, DummyProcess);
            Assert.That(database.IsReady(DummyAgentId, out _), Is.False);
        }

        [Test]
        public static void AgentIsReadyWhenRegistered()
        {
            var database = new AgentStore();

            database.AddAgent(DummyAgentId, DummyProcess);
            database.Register(DummyAgentId, DummyAgent);
            Assert.That(database.IsReady(DummyAgentId, out var registeredAgent), Is.True);
            Assert.That(registeredAgent, Is.SameAs(DummyAgent));
        }

        [Test]
        public static void AgentIsNotReadyWhenTerminated()
        {
            var database = new AgentStore();

            database.AddAgent(DummyAgentId, DummyProcess);
            database.Register(DummyAgentId, DummyAgent);
            database.MarkTerminated(DummyAgentId);
            Assert.That(database.IsReady(DummyAgentId, out _), Is.False);
        }

        [Test]
        public static void AgentIsNotRunningWhenNotStarted()
        {
            var database = new AgentStore();

            Assert.That(database.IsAgentProcessActive(DummyAgentId, out _), Is.False);
        }

        [Test]
        public static void AgentIsRunningWhenStarted()
        {
            var database = new AgentStore();

            database.AddAgent(DummyAgentId, DummyProcess);
            Assert.That(database.IsAgentProcessActive(DummyAgentId, out var process), Is.True);
            Assert.That(process, Is.SameAs(DummyProcess));
        }

        [Test]
        public static void AgentIsRunningWhenRegistered()
        {
            var database = new AgentStore();

            database.AddAgent(DummyAgentId, DummyProcess);
            database.Register(DummyAgentId, DummyAgent);
            Assert.That(database.IsAgentProcessActive(DummyAgentId, out var process), Is.True);
            Assert.That(process, Is.SameAs(DummyProcess));
        }

        [Test]
        public static void AgentIsNotRunningWhenTerminated()
        {
            var database = new AgentStore();

            database.AddAgent(DummyAgentId, DummyProcess);
            database.Register(DummyAgentId, DummyAgent);
            database.MarkTerminated(DummyAgentId);
            Assert.That(database.IsAgentProcessActive(DummyAgentId, out _), Is.False);
        }

        [Test]
        public static void ConcurrentOperationsDoNotCorruptState()
        {
            var database = new AgentStore();

            RunActionConcurrently(() =>
            {
                for (var i = 0; i < 1000; i++)
                {
                    var id = Guid.NewGuid();

                    Assert.That(database.IsAgentProcessActive(id, out _), Is.False);
                    Assert.That(database.IsReady(id, out _), Is.False);

                    database.AddAgent(id, DummyProcess);
                    Assert.That(database.IsAgentProcessActive(id, out _), Is.True);
                    Assert.That(database.IsReady(id, out _), Is.False);

                    database.Register(id, new DummyTestAgent());
                    Assert.That(database.IsAgentProcessActive(id, out _), Is.True);
                    Assert.That(database.IsReady(id, out _), Is.True);

                    database.MarkTerminated(id);
                    Assert.That(database.IsAgentProcessActive(id, out _), Is.False);
                    Assert.That(database.IsReady(id, out _), Is.False);
                }
            }, threadCount: Environment.ProcessorCount);
        }

        private static void RunActionConcurrently(Action action, int threadCount)
        {
            var threads = new List<Thread>();
            var exceptions = new List<Exception>();

            for (var i = 0; i < threadCount; i++)
            {
                threads.Add(new Thread(() =>
                {
                    try
                    {
                        action.Invoke();
                    }
                    catch (Exception ex)
                    {
                        lock (exceptions)
                            exceptions.Add(ex);
                    }
                }));
            }

            foreach (var thread in threads)
                thread.Start();

            foreach (var thread in threads)
                thread.Join();

            if (exceptions.Count != 0) throw exceptions[0];
        }
    }
}
#endif
