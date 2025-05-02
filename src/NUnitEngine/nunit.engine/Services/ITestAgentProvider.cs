// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using System;

namespace NUnit.Engine.Services
{
    /// <summary>
    /// An object implementing ITestAgentProvider is able to provide
    /// test agents, which satisfy the criteria specified in a TestPackage.
    /// The interface is used by the engine itself to locate agents.
    /// </summary>
    internal interface ITestAgentProvider : IAgentInfoProvider
    {
        /// <summary>
        /// Returns true if an agent can be found, which is suitable
        /// for running the provided test package.
        /// </summary>
        /// <param name="package">A TestPackage</param>
        bool IsAgentAvailable(TestPackage package);

        /// <summary>
        /// Return an agent, which best matches the criteria defined
        /// in a TestPackage.
        /// </summary>
        /// <param name="package">The test package to be run</param>
        /// <returns>An ITestAgent</returns>
        /// <exception cref="ArgumentException">If no agent is available.</exception>
        ITestAgent GetAgent(TestPackage package);

        /// <summary>
        /// Releases the test agent back to the supplier, which provided it.
        /// </summary>
        /// <param name="agent">An agent previously provided by a call to GetAgent.</param>
        /// <exception cref="InvalidOperationException">
        /// If agent was never provided by the factory or was previously released.
        /// </exception>
        /// <remarks>
        /// Disposing an agent also releases it. However, this should not
        /// normally be done by the client, but by the source that created
        /// the agent in the first place.
        /// </remarks>
        void ReleaseAgent(ITestAgent agent);
    }
}
