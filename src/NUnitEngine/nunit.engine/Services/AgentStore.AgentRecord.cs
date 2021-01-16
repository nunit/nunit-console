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
using System.Diagnostics;

namespace NUnit.Engine.Services
{
    internal sealed partial class AgentStore
    {
        private struct AgentRecord
        {
            private AgentRecord(Process process, ITestAgent agent)
            {
                Process = process;
                Agent = agent;
            }

            public Process Process { get; }
            public ITestAgent Agent { get; }

            public AgentStatus Status =>
                Process is null ? AgentStatus.Terminated :
                Agent is null ? AgentStatus.Starting :
                AgentStatus.Ready;

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
#endif
