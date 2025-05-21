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
using NUnit.Engine.Agents;
using NUnit.Engine.Communication.Transports.Tcp;
using NUnit.Engine.Extensibility;

namespace NUnit.Engine.Services
{
    /// <summary>
    /// The TestAgency class provides RemoteTestAgents
    /// on request and tracks their status.
    /// </summary>
    public class TestAgency : ITestAgentProvider, ITestAgency, IAgentInfoProvider, IService
    {
        private static readonly Logger log = InternalTrace.GetLogger(typeof(TestAgency));

        private const int NORMAL_TIMEOUT = 30000;               // 30 seconds
        private const int DEBUG_TIMEOUT = NORMAL_TIMEOUT * 10;  // 5 minutes

        private readonly AgentStore _agentStore = new AgentStore();

        private IRuntimeFrameworkService? _runtimeService;
        private IAvailableRuntimes? _availableRuntimeService;

        private readonly List<IAgentLauncher> _launchers = new List<IAgentLauncher>();

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

        #region IAgentInfoProvider Implementation

        /// <summary>
        /// Gets a list containing <see cref="TestAgentInfo"/> for all available agents.
        /// </summary>
        public IList<TestAgentInfo> AvailableAgents { get; private set; } = new List<TestAgentInfo>();

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
            foreach (var assemblyPackage in targetPackage.Select(p => p.IsAssemblyPackage()))
            {
                // Collect names of agents that work for each assembly
                var agentsForAssembly = new HashSet<string>();
                foreach (var launcher in _launchers.Where(l => validAgentNames.Contains(l.AgentInfo.AgentName)))
                    if (launcher.CanCreateAgent(assemblyPackage))
                        agentsForAssembly.Add(launcher.AgentInfo.AgentName);

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
            foreach (var launcher in _launchers)
                if (launcher.CanCreateAgent(package))
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
        /// <remarks>Virtual to allow substitution of agents in testing.</remarks>
        public virtual ITestAgent GetAgent(TestPackage package)
        {
            // Target Runtime must be specified by this point
            string runtimeSetting = package.GetSetting(EnginePackageSettings.TargetFrameworkName, string.Empty);
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
            bool debug = package.GetSetting(EnginePackageSettings.DebugAgent, false) ||
                         package.GetSetting(EnginePackageSettings.PauseBeforeRun, false);

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

        public IServiceLocator? ServiceContext { get; set; }

        public ServiceStatus Status { get; private set; }

        // TODO: it would be better if we had a list of transports to start and stop!

        public void StopService()
        {
            try
            {
                _tcpTransport.Stop();
            }
            finally
            {
                Status = ServiceStatus.Stopped;
            }
        }

        [MemberNotNull(nameof(_runtimeService), nameof(_availableRuntimeService))]
        public void StartService()
        {
            try
            {
                if (ServiceContext is null)
                    throw new InvalidOperationException("ServiceContext is required for TestAgency");

                _runtimeService = ServiceContext.GetService<IRuntimeFrameworkService>();
                _availableRuntimeService = ServiceContext.GetService<IAvailableRuntimes>();

                _launchers.Add(new Net462AgentLauncher());
                _launchers.Add(new Net80AgentLauncher());

                foreach (var launcher in _launchers)
                    AvailableAgents.Add(launcher.AgentInfo);

                _tcpTransport.Start();

                Status = ServiceStatus.Started;
            }
            catch
            {
                Status = ServiceStatus.Error;
                throw;
            }
        }

        #endregion

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
            // Check to see if a specific agent was selected
            string requestedAgentName = package.GetSetting(EnginePackageSettings.RequestedAgentName, "DEFAULT");
            log.Debug($"RequestedAgentName: {requestedAgentName}");

            foreach (var launcher in _launchers)
            {
                var launcherName = launcher.GetType().Name;
                log.Debug($"Examining Launcher {launcherName}");

                if ((launcherName == requestedAgentName || requestedAgentName == "DEFAULT") && launcher.CanCreateAgent(package))
                {
                    log.Info($"Selected launcher {launcherName}");
                    package.AddSetting(EnginePackageSettings.SelectedAgentName, launcherName);
                    return launcher.CreateAgent(agentId, agencyUrl, package);
                }
            }

            throw new NUnitEngineException($"No agent available for TestPackage {package.Name}");
        }
    }
}
#endif
