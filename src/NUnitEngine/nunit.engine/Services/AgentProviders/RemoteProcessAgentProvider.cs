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
    internal class RemoteProcessAgentProvider : ITestAgentProvider
    {
        private static readonly Logger log = InternalTrace.GetLogger(typeof(RemoteProcessAgentProvider));

        // The TestAgency instance for which we are providing local process agents.
        private TestAgency _agency;

        public RemoteProcessAgentProvider(TestAgency agency)
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
                // Currently, no agents available
                return Array.Empty<TestAgentInfo>();
            }
        }

        public IList<TestAgentInfo> GetAgentsForPackage(TestPackage package)
        {
            // Currently, no agents available
            return Array.Empty<TestAgentInfo>();
        }

        #endregion

        #region ITestAgentProvider Implementation

        public bool IsAgentAvailable(TestPackage package)
        {
            // Currently, no agents available
            return false;
        }

        public ITestAgent GetAgent(TestPackage package)
        {
            throw new InvalidOperationException(
                $"No remote agents are available for package {package.Name}.\r\nGetAgent should not be called when IsAgentAvailable returns false.");
        }

        public void ReleaseAgent(ITestAgent agent)
        {
            // Currently does nothing
        }

        #endregion
    }
}
