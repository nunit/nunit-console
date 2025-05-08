// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace NUnit.Engine.Services
{
    /// <summary>
    /// Defines the agent tracking operations that must be done atomically.
    /// </summary>
    internal sealed class AgentStore
    {
        private static readonly Logger log = InternalTrace.GetLogger(typeof(AgentStore));

        private readonly object LOCK = new object();

        private readonly Dictionary<Guid, AgentRecord> _agentIndex = new Dictionary<Guid, AgentRecord>();

        public void AddAgent(Guid agentId, Process process)
        {
            Guard.ArgumentNotNull(process, nameof(process));

            lock (LOCK)
            {
                if (_agentIndex.ContainsKey(agentId))
                {
                    throw new ArgumentException($"An agent has already been started with the ID '{agentId}'.", nameof(agentId));
                }

                _agentIndex[agentId] = new AgentRecord(agentId, process);
            }
        }

        public void Register(ITestAgent agent)
        {
            lock (LOCK)
            {
                log.Debug($"Registering agent {agent.Id:B}");

                if (!_agentIndex.TryGetValue(agent.Id, out var record)
                    || record.Status != AgentStatus.Starting)
                {
                    string status = record?.Status.ToString() ?? "unknown";
                    throw new ArgumentException($"Agent {agent.Id} must have a status of {AgentStatus.Starting} in order to register, but the status was {status}.", nameof(agent));
                }

                record.Agent = agent;
                record.Status = AgentStatus.Ready;
            }
        }

        public bool IsReady(Guid agentId, [NotNullWhen(true)] out ITestAgent? agent)
        {
            lock (LOCK)
            {
                if (_agentIndex.TryGetValue(agentId, out var record)
                    && record.Status == AgentStatus.Ready)
                {
                    agent = record.Agent;
                    return agent is not null;
                }

                agent = null;
                return false;
            }
        }

        public bool IsAgentActive(Guid agentId, [NotNullWhen(true)] out Process? process)
        {
            lock (LOCK)
            {
                if (_agentIndex.TryGetValue(agentId, out var record)
                    && record.Status != AgentStatus.Terminated)
                {
                    process = record.Process;
                    return process is not null;
                }

                process = null;
                return false;
            }
        }

        // Internal for use by our tests, which may not actually create a process
        internal void MarkTerminated(Guid agentId)
        {
            lock (LOCK)
            {
                if (_agentIndex.TryGetValue(agentId, out var record))
                    record.MarkAsTerminated();
                else
                    throw new NUnitEngineException("Process terminated without registering an agent.");
            }
        }

        #region Nested AgentRecord Class

        private class AgentRecord
        {
            public AgentRecord(Guid agentId, Process process)
            {
                Guard.ArgumentNotNull(process, nameof(process));

                AgentId = agentId;
                Agent = null;
                Process = process;
                Status = AgentStatus.Starting;
            }

            // AgentId is a property because it is needed before agent registers
            // and after it terminates, i.e. while Agent itself is null.
            public Guid AgentId { get; }
            public Process Process { get; }
            public AgentStatus Status { get; set; }
            public ITestAgent? Agent { get; set; }
            // ExitCode is set when process terminates
            public int ExitCode { get; set; }

            public void MarkAsTerminated()
            {
                Status = AgentStatus.Terminated;
                if (Process is not null)
                    try
                    {
                        // Remote processes will throw
                        ExitCode = Process.ExitCode;
                    }
                    catch (Exception ex)
                    {
                        log.Error(ex.Message);
                        ExitCode = 0;
                    }
            }
        }

        #endregion
    }
}
