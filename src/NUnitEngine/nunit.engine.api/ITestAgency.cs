// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

namespace NUnit.Engine
{
    /// <summary>
    /// The ITestAgency interface is implemented by a TestAgency in
    /// order to allow TestAgents to register with it.
    /// </summary>
    public interface ITestAgency
    {
        /// <summary>
        /// Registers an agent with an agency
        /// </summary>
        void Register(ITestAgent agent);
    }
}
