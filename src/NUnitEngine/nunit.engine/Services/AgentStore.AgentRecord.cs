// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace NUnit.Engine.Services
{
    internal sealed partial class AgentStore
    {
        private readonly struct AgentRecord
        {
            private AgentRecord(Process? process, ITestAgent? agent)
            {
                Process = process;
                Agent = agent;
            }

            public Process? Process { get; }
            public ITestAgent? Agent { get; }

            public AgentStatus Status =>
                Process is null ? AgentStatus.Terminated :
                Agent is null ? AgentStatus.Starting :
                AgentStatus.Ready;

            [MemberNotNullWhen(true, nameof(Process), nameof(Agent))]
            public bool IsReady => Process is not null && Agent is not null;

            public static AgentRecord Starting(Process process)
            {
                if (process is null) throw new ArgumentNullException(nameof(process));

                return new AgentRecord(process, agent: null);
            }

            public AgentRecord Ready(ITestAgent agent)
            {
                if (agent is null) throw new ArgumentNullException(nameof(agent));

                return new AgentRecord(Process, agent);
            }

            public AgentRecord Terminated()
            {
                return new AgentRecord(process: null, agent: null);
            }
        }
    }
}
