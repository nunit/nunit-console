// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

#if NETFRAMEWORK
using System;
using System.Collections.Generic;

namespace NUnit.Engine.Services
{
    /// <summary>
    /// Service providing agents of all three types. Currently, only LocalProcess agents
    /// are implemented, so this class is not used and we call the TestAgency directly.
    /// </summary>
    public class TestAgentService : Service, ITestAgentInfo, ITestAgentProvider
    {
        private static readonly Logger log = InternalTrace.GetLogger(typeof(TestAgentService));

        private IList<ITestAgentProvider> _providers = new List<ITestAgentProvider>();

        #region ITestAgentInfo Implementation

        /// <summary>
        /// Gets a list containing <see cref="TestAgentInfo"/> for all available agents.
        /// </summary>
        public IList<TestAgentInfo> AvailableAgents
        {
            get
            {
                var agents = new List<TestAgentInfo>();

                foreach (var provider in _providers)
                    agents.AddRange(provider.AvailableAgents);

                return agents;
            }
        }

        /// <summary>
        /// Gets a list containing <see cref="TestAgentInfo"/> for any available agents,
        /// which are able to handle the specified package.
        /// </summary>
        /// <param name="package">A Testpackage</param>
        /// <returns>
        /// A list of suitable agents for running the package or an empty
        /// list if no agent is available for the package.
        /// </returns>
        public IList<TestAgentInfo> GetAgentsForPackage(TestPackage package)
        {
            var agents = new List<TestAgentInfo>();

            foreach (var provider in _providers)
                agents.AddRange(provider.GetAgentsForPackage(package));

            return agents;
        }

        #endregion

        #region ITestAgentProvider Implementation

        /// <summary>
        /// Returns true if an agent can be found, which is suitable
        /// for running the provided test package.
        /// </summary>
        /// <param name="package">A TestPackage</param>
        public bool IsAgentAvailable(TestPackage package)
        {
            foreach (var agentSource in _providers)
                if (agentSource.IsAgentAvailable(package))
                    return true;

            return false;
        }

        /// <summary>
        /// Return an agent, which best matches the criteria defined
        /// in a TestPackage.
        /// </summary>
        /// <param name="package">The test package to be run</param>
        /// <returns>An ITestAgent</returns>
        /// <exception cref="ArgumentException">If no agent is available.</exception>
        public ITestAgent GetAgent(TestPackage package)
        {
            foreach (var agentSource in _providers)
                if (agentSource.IsAgentAvailable(package))
                {
                    var agent = agentSource.GetAgent(package);
                    log.Debug($"Returning agent {agent.Id:B}");
                    return agent;
                }

            throw new InvalidOperationException("No available agent matches the TestPackage");
        }

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
        public void ReleaseAgent(ITestAgent agent)
        {
            // TODO: save the source rather than trying all sources
            foreach (var agentSource in _providers)
                agentSource.ReleaseAgent(agent);
        }

        #endregion

        #region Service Overrides

        public override void StartService()
        {
            base.StartService();

            ITestAgentProvider testAgency = ServiceContext.GetService<TestAgency>();

            if (testAgency is not null)
            {
                _providers.Add(testAgency);
                Status = ServiceStatus.Started;
            }
            else
                Status = ServiceStatus.Error;
        }

        #endregion
    }
}
#endif
