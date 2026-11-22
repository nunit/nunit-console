// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Versioning;

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
        private static readonly FrameworkName AGENT_RUNTIME = new FrameworkName(".NETFramework", new Version(4, 6, 2));
#else
        private static readonly FrameworkName AGENT_RUNTIME = new FrameworkName(".NETCoreApp", new Version(10, 0));
#endif

        private static readonly Logger log = InternalTrace.GetLogger(typeof(InProcessAgentProvider));

        private const string INPROCESS_AGENTS_PATH = "/NUnit/Engine/InProcessAgents";

        // Runners request this agent by name via the RequestedAgentName setting.
        // The console's --inprocess option depends on it, so don't change it lightly.
        private const string AGENT_NAME = "InProcessAgent";
        private static readonly Guid INPROCESS_AGENT_ID = Guid.NewGuid();

        private static readonly IList<TestAgentInfo> NO_AGENTS = new List<TestAgentInfo>().AsReadOnly();

        private readonly TestAgentInfo _agentInfo;
        private readonly IList<TestAgentInfo> _availableAgents;

        private ExtensionService? _extensionService;
        private ITestAgent? _agent;

        public InProcessAgentProvider()
        {
            _agentInfo = new TestAgentInfo(AGENT_NAME, TestAgentType.InProcess, AGENT_RUNTIME);
            _availableAgents = new List<TestAgentInfo> { _agentInfo }.AsReadOnly();
        }

        #region ITestAgentProvider Implementation

        /// <summary>
        /// Gets a read-only list containing <see cref="TestAgentInfo"/> for all available agents.
        /// </summary>
        public IList<TestAgentInfo> AvailableAgents => _availableAgents;

        public IList<TestAgentInfo> GetAgentsForPackage(TestPackage package)
        {
            return IsAgentAvailable(package) ? _availableAgents : NO_AGENTS;
        }

        // Required by the interface, but in-process agents never exit independently.
        public event AgentExitedEventHandler? AgentExited
        {
            add { }
            remove { }
        }

        public bool IsAgentAvailable(TestPackage package)
        {
            string requestedAgent = package.Settings.GetValueOrDefault(SettingDefinitions.RequestedAgentName);
            if (!string.IsNullOrEmpty(requestedAgent) && requestedAgent != AGENT_NAME)
                return false;

            if (package.Settings.GetSetting(SettingDefinitions.ImageTargetFrameworkName.Name) is not string name)
                return false;

            var targetFramework = new FrameworkName(name);
            if (!targetFramework.Identifier.Equals(AGENT_RUNTIME.Identifier))
                return false;

#if NETFRAMEWORK
            // .NET Framework 4.x updates in place: the installed runtime (typically 4.8.1)
            // can load any 4.x target, regardless of the version we were built for.
            return true;
#else
            return targetFramework.Version <= AGENT_RUNTIME.Version;
#endif
        }

        public ITestAgent GetAgent(TestPackage package)
        {
            if (!IsAgentAvailable(package))
                throw new InvalidOperationException(
                    $"No agents are available for package {package.Name}. GetAgent should not be called when IsAgentAvailable returns false.");

            if (_agent is null)
            {
                Guard.OperationValid(_extensionService is not null, "ExtensionService is not available.");
                var node = _extensionService.GetExtensionNodes(INPROCESS_AGENTS_PATH).FirstOrDefault()
                    ?? throw new InvalidOperationException($"No in-process agent extension is installed at {INPROCESS_AGENTS_PATH}.");

                object extension;
                try
                {
                    // In-process agents take their agent id as a constructor argument.
                    extension = node.CreateExtensionObject(INPROCESS_AGENT_ID);
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException(
                        $"Extension {node.TypeName} at {INPROCESS_AGENTS_PATH} could not be created.", ex);
                }

                _agent = extension as ITestAgent ?? throw new InvalidOperationException(
                    $"Extension {node.TypeName} at {INPROCESS_AGENTS_PATH} does not implement ITestAgent.");
            }

            log.Info($"Selected agent {AGENT_NAME}");
            package.ApplySetting(SettingDefinitions.SelectedAgentName.WithValue(AGENT_NAME), false);
            return _agent;
        }

        public void ReturnAgent(ITestAgent agent)
        {
            // TODO: Close any open tests, dispose resources, keep the agent available for reuse.
        }

        #endregion

        #region Service Overrides

        public override void StartService()
        {
            // The base call verifies that a ServiceContext exists and marks the service as started.
            base.StartService();

            try
            {
                // GetService throws if ExtensionService is not registered.
                _extensionService = ServiceContext.GetService<ExtensionService>();
            }
            catch (Exception ex)
            {
                Status = ServiceStatus.Error;
                log.Error($"Failed to start InProcessAgentProvider: {ex}");
                throw;
            }
        }

        #endregion
    }
}
