// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using NUnit.Engine.Extensibility;
using NUnit.Extensibility;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Sockets;
using System.Runtime.Versioning;
using System.Text;
using System.Threading.Tasks;

namespace NUnit.Engine.Services
{
    internal class InProcessAgentProvider : ITestAgentProvider
    {
        private static readonly Logger log = InternalTrace.GetLogger(typeof(InProcessAgentProvider));

        // The TestAgency instance for which we are providing local process agents.
        private TestAgentService _agency;

        public InProcessAgentProvider(TestAgentService agency)
        {
            _agency = agency;
        }

        #region ITestAgentInfo Implementation

        //private List<TestAgentInfo>? _availableAgents;

        /// <summary>
        /// Gets a list containing <see cref="TestAgentInfo"/> for all available agents.
        /// </summary>
        public IList<TestAgentInfo> AvailableAgents
        {
            get
            {
                // Pseudocode
                // if the current runtime is eiher .NET Framework or .NET Core
                //     return a list containing a single TestAgentInfo for the current runtime

                // Currently, no agents available
                return Array.Empty<TestAgentInfo>();
            }
        }

        public IList<TestAgentInfo> GetAgentsForPackage(TestPackage package)
        {
            // Pseudocode
            // if package is a leaf assembly package and is compatible with the current runtime
            //     return a list containing a single TestAgentInfo for an InProcessTestAgent

            // Currently, no agents available
            return Array.Empty<TestAgentInfo>();
        }

        #endregion

        #region ITestAgentProvider Implementation

        public bool IsAgentAvailable(TestPackage package)
        {
            // TODO: Return true if an InProcessTestAgent is available for the package.
            return false;
        }

        public ITestAgent GetAgent(TestPackage package)
        {
            // TODO: Return a new InProcessTestAgent instance for the package.
            throw new InvalidOperationException(
                $"No agents are available for package {package.Name}.\r\nGetAgent should not be called when IsAgentAvailable returns false.");
        }

        public void ReturnAgent(ITestAgent agent)
        {
            // TODO: Close any open tests, dispose resources, keep the agent available for reuse.
        }

        #endregion
    }
}
