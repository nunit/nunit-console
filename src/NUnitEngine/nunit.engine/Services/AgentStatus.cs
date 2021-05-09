// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

#if !NETSTANDARD2_0
namespace NUnit.Engine.Services
{
    /// <summary>
    /// Enumeration used to report AgentStatus
    /// </summary>
    public enum AgentStatus
    {
        /// <summary>
        /// Agent is in the process of starting and we are waiting for it to be ready.
        /// This is generally used for process agents launched by the TestAgency,
        /// which must call back using the agency's Register method.
        /// </summary>
        Starting,

        /// <summary>
        /// Agent is ready to receive commands. Once in this state, it remains there until
        /// it is told to terminate using the Stop method.
        /// </summary>
        Ready,

        /// <summary>
        /// The agent has stopped completely. In the case of a process agent, this means
        /// that the process has terminated.
        /// </summary>
        Terminated
    }
}
#endif
