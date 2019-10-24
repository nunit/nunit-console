// ***********************************************************************
// Copyright (c) 2011-2019 Charlie Poole, Rob Prouse
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

#if !NETSTANDARD1_6 && !NETSTANDARD2_0
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace NUnit.Engine.Services
{
    /// <summary>
    /// Defines the agent tracking operations that must be done atomically.
    /// </summary>
    internal sealed partial class AgentStore
    {
        private readonly Dictionary<Guid, AgentRecord> _agentsById = new Dictionary<Guid, AgentRecord>();

        public void Start(Guid agentId, Process process)
        {
            lock (_agentsById)
            {
                if (_agentsById.ContainsKey(agentId))
                {
                    throw new ArgumentException($"An agent has already been started with the ID '{agentId}'.", nameof(agentId));
                }

                _agentsById.Add(agentId, AgentRecord.Starting(process));
            }
        }

        public void Register(ITestAgent agent)
        {
            lock (_agentsById)
            {
                if (!_agentsById.TryGetValue(agent.Id, out var record)
                    || record.Status != AgentStatus.Starting)
                {
                    throw new ArgumentException($"Agent {agent.Id} must have a status of {AgentStatus.Starting} in order to register, but the status was {record.Status}.", nameof(agent));
                }

                _agentsById[agent.Id] = record.Ready(agent);
            }
        }

        public bool IsReady(Guid agentId, out ITestAgent agent)
        {
            lock (_agentsById)
            {
                if (_agentsById.TryGetValue(agentId, out var record)
                    && record.Status == AgentStatus.Ready)
                {
                    agent = record.Agent;
                    return true;
                }

                agent = null;
                return false;
            }
        }

        public bool IsAgentProcessActive(Guid agentId, out Process process)
        {
            lock (_agentsById)
            {
                if (_agentsById.TryGetValue(agentId, out var record)
                    && record.Status != AgentStatus.Terminated)
                {
                    process = record.Process;
                    return process != null;
                }

                process = null;
                return false;
            }
        }

        public void MarkTerminated(Guid agentId)
        {
            lock (_agentsById)
            {
                if (!_agentsById.TryGetValue(agentId, out var record))
                {
                    throw new ArgumentException($"An entry for agent {agentId} must exist in order to mark it as terminated.", nameof(agentId));
                }

                _agentsById[agentId] = record.Terminated();
            }
        }
    }
}
#endif
