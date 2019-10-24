// ***********************************************************************
// Copyright (c) 2017–2019 Charlie Poole, Rob Prouse
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

#if !NETCOREAPP1_1 && !NETCOREAPP2_1
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
        private static readonly ITestAgent DummyAgent = new DummyTestAgent(Guid.NewGuid());

        [Test]
        public static void IdCannotBeReused()
        {
            var database = new AgentStore();

            database.Start(DummyAgent.Id, DummyProcess);
            Assert.That(() => database.Start(DummyAgent.Id, DummyProcess), Throws.ArgumentException.With.Property("ParamName").EqualTo("agentId"));

            database.Register(DummyAgent);
            Assert.That(() => database.Start(DummyAgent.Id, DummyProcess), Throws.ArgumentException.With.Property("ParamName").EqualTo("agentId"));

            database.MarkTerminated(DummyAgent.Id);
            Assert.That(() => database.Start(DummyAgent.Id, DummyProcess), Throws.ArgumentException.With.Property("ParamName").EqualTo("agentId"));
        }

        [Test]
        public static void AgentMustBeStartedBeforeRegistering()
        {
            var database = new AgentStore();

            Assert.That(() => database.Register(DummyAgent), Throws.ArgumentException.With.Property("ParamName").EqualTo("agent"));
        }

        [Test]
        public static void AgentMustNotRegisterTwice()
        {
            var database = new AgentStore();

            database.Start(DummyAgent.Id, DummyProcess);
            database.Register(DummyAgent);
            Assert.That(() => database.Register(DummyAgent), Throws.ArgumentException.With.Property("ParamName").EqualTo("agent"));
        }

        [Test]
        public static void AgentMustNotRegisterAfterTerminating()
        {
            var database = new AgentStore();

            database.Start(DummyAgent.Id, DummyProcess);
            database.MarkTerminated(DummyAgent.Id);
            Assert.That(() => database.Register(DummyAgent), Throws.ArgumentException.With.Property("ParamName").EqualTo("agent"));
        }

        [Test]
        public static void AgentMustBeStartedBeforeTerminating()
        {
            var database = new AgentStore();

            Assert.That(() => database.MarkTerminated(DummyAgent.Id), Throws.ArgumentException.With.Property("ParamName").EqualTo("agentId"));
        }

        [Test]
        public static void AgentIsNotReadyWhenNotStarted()
        {
            var database = new AgentStore();

            Assert.That(database.IsReady(DummyAgent.Id, out _), Is.False);
        }

        [Test]
        public static void AgentIsNotReadyWhenStarted()
        {
            var database = new AgentStore();

            database.Start(DummyAgent.Id, DummyProcess);
            Assert.That(database.IsReady(DummyAgent.Id, out _), Is.False);
        }

        [Test]
        public static void AgentIsReadyWhenRegistered()
        {
            var database = new AgentStore();

            database.Start(DummyAgent.Id, DummyProcess);
            database.Register(DummyAgent);
            Assert.That(database.IsReady(DummyAgent.Id, out var registeredAgent), Is.True);
            Assert.That(registeredAgent, Is.SameAs(DummyAgent));
        }

        [Test]
        public static void AgentIsNotReadyWhenTerminated()
        {
            var database = new AgentStore();

            database.Start(DummyAgent.Id, DummyProcess);
            database.Register(DummyAgent);
            database.MarkTerminated(DummyAgent.Id);
            Assert.That(database.IsReady(DummyAgent.Id, out _), Is.False);
        }

        [Test]
        public static void AgentIsNotRunningWhenNotStarted()
        {
            var database = new AgentStore();

            Assert.That(database.IsAgentProcessActive(DummyAgent.Id, out _), Is.False);
        }

        [Test]
        public static void AgentIsRunningWhenStarted()
        {
            var database = new AgentStore();

            database.Start(DummyAgent.Id, DummyProcess);
            Assert.That(database.IsAgentProcessActive(DummyAgent.Id, out var process), Is.True);
            Assert.That(process, Is.SameAs(DummyProcess));
        }

        [Test]
        public static void AgentIsRunningWhenRegistered()
        {
            var database = new AgentStore();

            database.Start(DummyAgent.Id, DummyProcess);
            database.Register(DummyAgent);
            Assert.That(database.IsAgentProcessActive(DummyAgent.Id, out var process), Is.True);
            Assert.That(process, Is.SameAs(DummyProcess));
        }

        [Test]
        public static void AgentIsNotRunningWhenTerminated()
        {
            var database = new AgentStore();

            database.Start(DummyAgent.Id, DummyProcess);
            database.Register(DummyAgent);
            database.MarkTerminated(DummyAgent.Id);
            Assert.That(database.IsAgentProcessActive(DummyAgent.Id, out _), Is.False);
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

                    database.Start(id, DummyProcess);
                    Assert.That(database.IsAgentProcessActive(id, out _), Is.True);
                    Assert.That(database.IsReady(id, out _), Is.False);

                    database.Register(new DummyTestAgent(id));
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
