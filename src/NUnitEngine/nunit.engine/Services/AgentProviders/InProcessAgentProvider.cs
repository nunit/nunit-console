// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Sockets;
using System.Reflection;
using System.Runtime.Versioning;
using System.Text;
using System.Threading.Tasks;

using NUnit.Common;
using NUnit.Engine.Extensibility;
using NUnit.Extensibility;

namespace NUnit.Engine.Services
{
    /// <summary>
    /// InProcessAgentProvider provides a single in-process agent targeting
    /// either net462 or net10.0. These two target versions match the two targets
    /// used by the NUnit engine and console runner. More flexible targeting is
    /// a desirable feature, which may be introduced at a later time.
    /// </summary>
    internal class InProcessAgentProvider : Service, ITestAgentProvider
    {
#if NETFRAMEWORK
        private static bool IS_NETFX_BUILD = true;
#else
        private static bool IS_NETFX_BUILD = false;
#endif

        private static readonly FrameworkName AGENT_RUNTIME = IS_NETFX_BUILD
            ? new FrameworkName(".NETFramework", new Version(4, 6, 2))
            : new FrameworkName(".NETCoreApp", new Version(10, 0));

        private static readonly Logger log = InternalTrace.GetLogger(typeof(InProcessAgentProvider));

        private const string INPROCESS_AGENTS_PATH = "/NUnit/Engine/InProcessAgents";
        private static readonly Guid INPROCESS_AGENT_ID = Guid.NewGuid();

        private ExtensionService? _extensionService;
        private TestAgentInfo _agentInfo = new TestAgentInfo(
            "InProcessAgent", TestAgentType.InProcess, AGENT_RUNTIME);

        #region ITestAgentInfo Implementation

        /// <summary>
        /// Gets a list containing <see cref="TestAgentInfo"/> for all available agents.
        /// </summary>
        public IList<TestAgentInfo> AvailableAgents => new List<TestAgentInfo> { _agentInfo };

        public IList<TestAgentInfo> GetAgentsForPackage(TestPackage package)
        {
            return IsAgentAvailable(package)
                ? new List<TestAgentInfo> { _agentInfo }
                : Array.Empty<TestAgentInfo>();
        }

        #endregion

        #region ITestAgentProvider Implementation

#pragma warning disable CS0067 // The event is required by the interface but is not used by this provider.
        public event AgentExitedEventHandler? AgentExited;
#pragma warning restore CS0067

        public bool IsAgentAvailable(TestPackage package)
        {
            var targetFramework = new FrameworkName((string)package.Settings.GetSetting(SettingDefinitions.TargetFrameworkName.Name));
            return targetFramework.Identifier.Equals(AGENT_RUNTIME.Identifier);
        }

        public ITestAgent GetAgent(TestPackage package)
        {
            throw new InvalidOperationException(
                $"No agents are available for package {package.Name}.\r\nGetAgent should not be called when IsAgentAvailable returns false.");
        }

        public void ReturnAgent(ITestAgent agent)
        {
            // TODO: Close any open tests, dispose resources, keep the agent available for reuse.
        }

        #endregion

        #region Service Overrides

        public override void StartService()
        {
            base.StartService();
            try
            {
                _extensionService = ServiceContext.GetService<ExtensionService>().ShouldNotBeNull();
            }
            catch
            {
                log.Error("Failed to start InProcessAgentProvider");
                throw;
            }
        }

        public override void StopService()
        {
            base.StopService();
        }

        #endregion
    }
}
