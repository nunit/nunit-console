// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

namespace NUnit.Engine
{
    /// <summary>
    /// <para>TestAgentType is an enumeration of the types
    /// of agents, which may be available. Currently, three
    /// types are defined, of which one is implemented.</para>
    /// </summary>
    /// <remarks>
    /// A user requests a particular agent type by including a
    /// "TestAgentType" setting in the test package. The setting
    /// is optional and the runner will select an agent of any
    /// type available if it isn't specified.
    /// </remarks>
    public enum TestAgentType
    {
        /// <summary>
        /// Any agent type is acceptable. This is the default value,
        /// so it never needs to be specified by the user. This
        /// setting is not valid in the TestAgentInfo struct, since
        /// each agent must be of some particular type.
        /// </summary>
        Any = 0,

        /// <summary>
        /// An in-process agent. This type is not directly supported
        /// by the engine but may be provided by an extension.
        /// </summary>
        InProcess = 1,

        /// <summary>
        /// An agent running as a separate local process.
        /// A supplier for this type is built into the engine.
        /// </summary>
        LocalProcess = 2,

        /// <summary>
        /// An agent running on a server, that is a separate
        /// machine, which may be specified in the request or
        /// left up to the agent supplier to determine. A supplier
        /// for this type may be developed in the future.
        /// </summary>
        RemoteProcess = 3
    }
}
