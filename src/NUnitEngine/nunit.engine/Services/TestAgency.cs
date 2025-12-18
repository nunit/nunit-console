// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

#if NETFRAMEWORK
using System;
using System.Threading;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Runtime.Versioning;
using NUnit.Common;
using NUnit.Engine.Communication.Transports.Tcp;
using NUnit.Engine.Extensibility;
using NUnit.Extensibility;

namespace NUnit.Engine.Services
{
    /// <summary>
    /// The TestAgency class provides RemoteTestAgents
    /// on request and tracks their status.
    /// </summary>
    public class TestAgency : Service, ITestAgentInfo, ITestAgentProvider, ITestAgency
    {
        private static readonly Logger log = InternalTrace.GetLogger(typeof(TestAgency));

        private const int NORMAL_TIMEOUT = 30000;               // 30 seconds
        private const int DEBUG_TIMEOUT = NORMAL_TIMEOUT * 10;  // 5 minutes
        private const string AGENT_LAUNCHERS_PATH = "/NUnit/Engine/AgentLaunchers";

        private readonly AgentStore _agentStore = new AgentStore();

        private ExtensionService? _extensionService;

        // Index used to retrieve the agentId of a terminated process
        private readonly Dictionary<Process, Guid> _agentProcessIndex = new Dictionary<Process, Guid>();

        // Transports used for various target runtimes
        private TestAgencyTcpTransport _tcpTransport; // .NET Standard 2.0

        internal virtual string TcpEndPoint => _tcpTransport.ServerUrl;

        public TestAgency() : this("TestAgency", 0)
        {
        }

        public TestAgency(string uri, int port)
        {
            _tcpTransport = new TestAgencyTcpTransport(this, port);
        }

        #region ITestAgentInfo Implementation

        private List<TestAgentInfo>? _availableAgents;

        /// <summary>
        /// Gets a list containing <see cref="TestAgentInfo"/> for all available agents.
        /// </summary>
        public IList<TestAgentInfo> AvailableAgents
        {
            get
            {
                if (_availableAgents is null)
                {
                    _availableAgents = new List<TestAgentInfo>();

                    foreach (var node in LauncherNodes)
                    {
                        string agentName = node.TypeName;
                        TestAgentType agentType = TestAgentType.LocalProcess;
                        string? targetFramework = node.GetValues("TargetFramework").FirstOrDefault();
                        // We try to avoid instantiating the agent to determine its target runtime,
                        // which may be specified as a property of the extension node.
                        var targetFrameworkName = targetFramework is not null
                            ? new FrameworkName(targetFramework)
                            : GetLauncherInstance(node).AgentInfo.TargetRuntime;

                        _availableAgents.Add(new TestAgentInfo(agentName, agentType, targetFrameworkName));
                    }
                }

                return _availableAgents;
            }
        }

        /// <summary>
        /// Gets a list containing <see cref="TestAgentInfo"/> for any available agents,
        /// which are able to handle the specified package.
        /// </summary>
        /// <param name="package">A TestPackage</param>
        /// <returns>
        /// A list of suitable agents for running the package or an empty
        /// list if no agent is available for the package.
        /// </returns>
        public IList<TestAgentInfo> GetAgentsForPackage(TestPackage targetPackage)
        {
            Guard.ArgumentNotNull(targetPackage, nameof(targetPackage));

            // This method is primarily intended for use in implementing a command-line
            // option to allow selection of the agent to be used for a test run. Because
            // the option would apply to all assemblies being run, any agents returned
            // must be able to run all assemblies.

            // Initialize lists with ALL available agents
            var validAgentNames = new HashSet<string>(AvailableAgents.Select(info => info.AgentName));

            // Look at each included assembly package to see if any names should be removed
            foreach (var assemblyPackage in targetPackage.Select(p => p.IsAssemblyPackage))
            {
                // Collect names of agents that work for each assembly
                var agentsForAssembly = new HashSet<string>();
                foreach (var node in LauncherNodes)
                {
                    if (CanCreateAgent(node, assemblyPackage))
                        agentsForAssembly.Add(node.TypeName);
                }

                // Remove agents from final result if they don't work for this assembly
                validAgentNames.IntersectWith(agentsForAssembly);
            }

            // Finish up by excluding all unsuitable entries from the List of available agents.
            return AvailableAgents.Where(info => validAgentNames.Contains(info.AgentName)).ToList();
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
            foreach (var node in LauncherNodes)
            {
                if (CanCreateAgent(node, package))
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Return an agent, which best matches the criteria defined
        /// in a TestPackage.
        /// </summary>
        /// <param name="package">The test package to be run</param>
        /// <returns>An ITestAgent</returns>
        /// <exception cref="ArgumentException">If no agent is available.</exception>
        /// <remarks>Virtual to allow substitution of agents in testing.</remarks>
        public virtual ITestAgent GetAgent(TestPackage package)
        {
            // Target Runtime must be specified by this point
            string runtimeSetting = package.Settings.GetValueOrDefault(SettingDefinitions.TargetFrameworkName);
            Guard.OperationValid(runtimeSetting.Length > 0, "LaunchAgentProcess called with no target runtime specified");

            var targetRuntime = new FrameworkName(runtimeSetting);
            var agentId = Guid.NewGuid();
            var agentProcess = CreateAgentProcess(agentId, TcpEndPoint, package);

            agentProcess.Exited += (sender, e) => OnAgentExit(agentProcess);

            agentProcess.Start();
            log.Debug("Launched Agent process {0} - see nunit-agent_{0}.log", agentProcess.Id);
            log.Debug("Command line: \"{0}\" {1}", agentProcess.StartInfo.FileName, agentProcess.StartInfo.Arguments);

            _agentStore.AddAgent(agentId, agentProcess);
            _agentProcessIndex.Add(agentProcess, agentId);

            log.Debug($"Waiting for agent {agentId:B} to register");

            const int pollTime = 200;

            // Increase the timeout to give time to attach a debugger
            bool debug = package.Settings.GetValueOrDefault(SettingDefinitions.DebugAgent) ||
                         package.Settings.GetValueOrDefault(SettingDefinitions.PauseBeforeRun);

            int waitTime = debug ? DEBUG_TIMEOUT : NORMAL_TIMEOUT;

            // Wait for agent registration based on the agent actually getting processor time to avoid falling over
            // under process starvation.
            while (waitTime > agentProcess.TotalProcessorTime.TotalMilliseconds && !agentProcess.HasExited)
            {
                Thread.Sleep(pollTime);

                if (_agentStore.IsReady(agentId, out var agent))
                {
                    log.Debug($"Returning new agent {agentId:B}");

                    return agent;
                }
            }

            throw new NUnitEngineException("Unable to acquire remote process agent");
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
            Process? process;

            if (_agentStore.IsAgentActive(agent.Id, out process))
                try
                {
                    log.Debug("Stopping remote agent");
                    agent.Stop();
                }
                catch (SocketException se)
                {
                    int exitCode;

                    try
                    {
                        exitCode = process.ExitCode;
                    }
                    catch (NotSupportedException)
                    {
                        exitCode = -17;
                    }

                    if (exitCode == 0)
                    {
                        log.Warning("Agent connection was forcibly closed. Exit code was 0, so agent shutdown OK");
                    }
                    else
                    {
                        var stopError = $"Agent connection was forcibly closed. Exit code was {exitCode}. {Environment.NewLine}{ExceptionHelper.BuildMessageAndStackTrace(se)}";
                        log.Error(stopError);

                        throw;
                    }
                }
                catch (Exception e)
                {
                    var stopError = "Failed to stop the remote agent." + Environment.NewLine + ExceptionHelper.BuildMessageAndStackTrace(e);
                    log.Error(stopError);
                }
        }

        #endregion

        #region ITestAgency Implementation

        public void Register(ITestAgent agent)
        {
            log.Debug($"Registered agent {agent.Id:B}");
            _agentStore.Register(agent);
        }

        #endregion

        #region IService Implementation

        // TODO: it would be better if we had a list of transports to start and stop!

        public override void StopService()
        {
            _tcpTransport.Stop();
            base.StopService();
        }

        public override void StartService()
        {
            base.StartService();

            try
            {
                _extensionService = ServiceContext.GetService<ExtensionService>().ShouldNotBeNull();

                _tcpTransport.Start();

                base.StartService();
            }
            catch
            {
                Status = ServiceStatus.Error;
                throw;
            }
        }

        #endregion

        private List<ExtensionNode>? _launcherNodes;
        private List<ExtensionNode> LauncherNodes
        {
            get
            {
                if (_launcherNodes is null)
                {
                    Guard.OperationValid(_extensionService is not null, "LauncherNodes property may not be accessed before TestAgency service is started");

                    _launcherNodes = new List<ExtensionNode>();

                    foreach (var node in _extensionService.GetExtensionNodes(AGENT_LAUNCHERS_PATH))
                        _launcherNodes.Add(node);
                }

                return _launcherNodes;
            }
        }

        internal bool IsAgentActive(Guid agentId, [NotNullWhen(true)] out Process? process)
        {
            return _agentStore.IsAgentActive(agentId, out process);
        }

        private void OnAgentExit(Process process)
        {
            if (_agentProcessIndex.TryGetValue(process, out var agentId))
            {
                _agentStore.MarkTerminated(agentId);

                string errorMsg;

                switch (process.ExitCode)
                {
                    case AgentExitCodes.OK:
                        return;
                    case AgentExitCodes.PARENT_PROCESS_TERMINATED:
                        errorMsg = "Remote test agent believes agency process has exited.";
                        break;
                    case AgentExitCodes.UNEXPECTED_EXCEPTION:
                        errorMsg = "Unhandled exception on remote test agent. " +
                                   "To debug, try using --trace=debug to output logs.";
                        break;
                    case AgentExitCodes.FAILED_TO_START_REMOTE_AGENT:
                        errorMsg = "Failed to start remote test agent.";
                        break;
                    case AgentExitCodes.DEBUGGER_SECURITY_VIOLATION:
                        errorMsg = "Debugger could not be started on remote agent due to System.Security.Permissions.UIPermission not being set.";
                        break;
                    case AgentExitCodes.DEBUGGER_NOT_IMPLEMENTED:
                        errorMsg = "Debugger could not be started on remote agent as not available on platform.";
                        break;
                    case AgentExitCodes.UNABLE_TO_LOCATE_AGENCY:
                        errorMsg = "Remote test agent unable to locate agency process.";
                        break;
                    default:
                        errorMsg = $"Remote test agent exited with non-zero exit code {process.ExitCode}";
                        break;
                }

                throw new NUnitEngineException(errorMsg);
            }
        }

        private Process CreateAgentProcess(Guid agentId, string agencyUrl, TestPackage package)
        {
            if (_extensionService is null)
                throw new InvalidOperationException("The field '_extensionService' must be non null when calling this method");

            // Check to see if a specific agent was selected
            string requestedAgent = package.Settings.GetValueOrDefault(SettingDefinitions.RequestedAgentName);
            bool specificAgentRequested = !string.IsNullOrEmpty(requestedAgent);

            foreach (var node in _extensionService.GetExtensionNodes<IAgentLauncher>())
            {
                if (specificAgentRequested && node.TypeName != requestedAgent)
                    continue;

                if (CanCreateAgent(node, package))
                {
                    var launcher = GetLauncherInstance(node);

                    var launcherName = launcher.GetType().Name;
                    log.Info($"Selected launcher {launcherName}");
                    package.Settings.Set(SettingDefinitions.SelectedAgentName.WithValue(launcherName));
                    return launcher.CreateAgent(agentId, agencyUrl, package);
                }
            }

            if (specificAgentRequested)
                throw new NUnitEngineException($"The requested launcher {requestedAgent} cannot load package {package.Name}");
            else
                throw new NUnitEngineException($"No agent available for TestPackage {package.Name}");
        }

        private bool CanCreateAgent(ExtensionNode node, TestPackage package)
        {
            // Newer implementations use a TargetFramework property to avoid
            // intantiating any agents, which will not be used.
            var runtimes = node.GetValues("TargetFramework");

            // If there is no property, we have to instantiate it to check.
            if (runtimes.Count() == 0)
            {
                var launcher = GetLauncherInstance(node);
                return launcher is not null && launcher.CanCreateAgent(package);
            }

            // The property is present, so no instantiation is needed.
            var agentTarget = new FrameworkName(runtimes.First());
            log.Debug($"Agent {node.TypeName} targets {agentTarget}");
            var packageTargetSetting =
                package.Settings.GetValueOrDefault(SettingDefinitions.ImageTargetFrameworkName);

            if (!string.IsNullOrEmpty(packageTargetSetting))
            {
                var packageTarget = new FrameworkName(packageTargetSetting);
                return agentTarget.Identifier == packageTarget.Identifier
                    && agentTarget.Version.Major >= packageTarget.Version.Major;
            }

            var packageRuntimeVersion =
                package.Settings.GetValueOrDefault(SettingDefinitions.ImageRuntimeVersion);
            if (!string.IsNullOrEmpty(packageRuntimeVersion))
                return agentTarget.Identifier == FrameworkIdentifiers.NetFramework &&
                    new Version(packageRuntimeVersion).Major <= agentTarget.Version.Major;

            return false;
        }

        private IAgentLauncher GetLauncherInstance(ExtensionNode node)
        {
            var launcher = node.ExtensionObject as IAgentLauncher;
            if (launcher is not null)
                return launcher;

            throw new NUnitEngineException("Internal Error: Expected an IAgentLancher");
        }
    }
}
#endif
