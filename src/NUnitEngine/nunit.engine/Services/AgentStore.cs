// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

#if !NETSTANDARD2_0
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

        public void AddAgent(Guid agentId, Process process)
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

        public void Register(Guid agentId, ITestAgent agent)
        {
            lock (_agentsById)
            {
                if (!_agentsById.TryGetValue(agentId, out var record)
                    || record.Status != AgentStatus.Starting)
                {
                    throw new ArgumentException($"Agent {agentId} must have a status of {AgentStatus.Starting} in order to register, but the status was {record.Status}.", nameof(agent));
                }

                _agentsById[agentId] = record.Ready(agent);
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
