// ***********************************************************************
// Copyright (c) 2016 Charlie Poole
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
using System.Collections.Generic;
using NUnit.Framework;

namespace NUnit.Engine.Agents.Tests
{
    [TestFixture]
    public sealed class RemoteTestAgentTests
    {
        [Test]
        public void Stop_ShouldUnregisterWithAgency()
        {
            List<Guid> unregisteredAgents = new List<Guid>();

            ITestAgency agency = new MockAgency(
                register: agent => { Assert.Fail("Should not be invoked"); },
                unregister: unregisteredAgents.Add
            );

            Guid agentId = Guid.NewGuid();

            using (RemoteTestAgent testAgent = new RemoteTestAgent(agentId, agency, services: null))
            {
                testAgent.Stop();
                CollectionAssert.AreEqual(new[] { agentId }, unregisteredAgents, "Agent should be unregistered");
            }

            CollectionAssert.AreEqual(new[] { agentId }, unregisteredAgents, "Agent should only unregistered once");
        }

        [Test]
        public void Stop_WhenAgencyThrows_ShouldNotThrow()
        {
            ITestAgency agency = new MockAgency(
                register: agent => { Assert.Fail("Should not be invoked"); },
                unregister: agentId => { throw new Exception("Boom"); }
            );

            using (RemoteTestAgent testAgent = new RemoteTestAgent(Guid.NewGuid(), agency, services: null))
            {
                Assert.DoesNotThrow(() => testAgent.Stop(), "Agent should catch and log exception");
            }
        }

        #region MockAgency Definition

        private sealed class MockAgency : ITestAgency
        {
            private readonly Action<ITestAgent> _register;
            private readonly Action<Guid> _unregister;

            public MockAgency(Action<ITestAgent> register, Action<Guid> unregister)
            {
                _register = register;
                _unregister = unregister;
            }

            void ITestAgency.Register(ITestAgent agent)
            {
                _register(agent);
            }

            void ITestAgency.Unregister(Guid agentId)
            {
                _unregister(agentId);
            }
        }

        #endregion

    }
}
