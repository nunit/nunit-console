// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using System;

namespace NUnit.Engine.Services
{
    /// <summary>
    /// An object implementing ITestAgentProvider is able to provide
    /// test agents, which satisfy the criteria specified in a TestPackage.
    /// The engine loads implementations through /NUnit/Engine/AgentProviders.
    /// Capability queries do not acquire execution ownership. An acquired agent
    /// remains associated with its provider until ReleaseAgent succeeds.
    /// </summary>
    public interface ITestAgentProvider : ITestAgentInfo
    {
        /// <summary>
        /// Returns true if an agent can be found, which is suitable
        /// for running the provided test package.
        /// </summary>
        /// <param name="package">A TestPackage</param>
        /// <returns>True if the provider can supply a compatible agent.</returns>
        /// <remarks>Capability queries must not acquire execution ownership.</remarks>
        bool IsAgentAvailable(TestPackage package);

        /// <summary>
        /// Return an agent, which best matches the criteria defined
        /// in a TestPackage.
        /// </summary>
        /// <param name="package">The test package to be run</param>
        /// <returns>An ITestAgent</returns>
        /// <exception cref="ArgumentException">If the package is invalid.</exception>
        /// <exception cref="InvalidOperationException">If no compatible agent is available.</exception>
        ITestAgent GetAgent(TestPackage package);

        /// <summary>
        /// Releases the test agent back to the supplier, which provided it.
        /// </summary>
        /// <param name="agent">An agent previously provided by a call to GetAgent.</param>
        /// <exception cref="InvalidOperationException">
        /// If agent was never provided by the factory or was previously released.
        /// </exception>
        /// <remarks>
        /// Return the agent through the same provider which acquired it so that
        /// ownership bookkeeping remains consistent. A stop request, a closed
        /// connection or disposal of a runner is not confirmation of release.
        /// If release fails, retain ownership until the provider can reconcile it.
        /// The provider defines how it stops its agent. Successful release alone
        /// does not prove that external deployment files can be deleted safely;
        /// remote providers must confirm execution has stopped before allowing cleanup.
        /// </remarks>
        void ReleaseAgent(ITestAgent agent);
    }
}
