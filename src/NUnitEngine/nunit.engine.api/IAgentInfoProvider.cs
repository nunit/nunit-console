// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using System.Collections.Generic;

namespace NUnit.Engine.Services
{
    /// <summary>
    /// Objects implementing IAgentInfoProvider can return information about
    /// available agents. The interface is used by runners in order to provide
    /// to the user or to allow selecting agents.
    /// </summary>
    public interface IAgentInfoProvider
    {
        /// <summary>
        /// Gets a list containing <see cref="TestAgentInfo"/> for all available agents.
        /// </summary>
        IList<TestAgentInfo> AvailableAgents { get; }

        /// <summary>
        /// Gets a list containing <see cref="TestAgentInfo"/> for any available agents,
        /// which are able to handle the specified package.
        /// </summary>
        /// <param name="package">A TestPackage</param>
        /// <returns>
        /// A list of suitable agents for running the package or an empty
        /// list if no agent is available for the package.
        /// </returns>
        IList<TestAgentInfo> GetAgentsForPackage(TestPackage package);
    }
}
