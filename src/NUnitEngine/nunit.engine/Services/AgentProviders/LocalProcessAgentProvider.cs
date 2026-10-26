// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using NUnit.Common;
using NUnit.Engine.Extensibility;
using NUnit.Extensibility;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Versioning;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NUnit.Engine.Services
{
    internal class LocalProcessAgentProvider : ITestAgentProvider
    {
        private static readonly Logger log = InternalTrace.GetLogger(typeof(LocalProcessAgentProvider));

        private const string AGENT_LAUNCHERS_PATH = "/NUnit/Engine/AgentLaunchers";

        private const int NORMAL_TIMEOUT = 30000;               // 30 seconds
        private const int DEBUG_TIMEOUT = NORMAL_TIMEOUT * 10;  // 5 minutes

        // The TestAgency instance for which we are providing local process agents.
        private TestAgency _agency;

        // The AgentStore instance used to track agents and their processes.
        private AgentStore _agentStore;

        // Index used to retrieve the agentId of a terminated process
        internal readonly ConcurrentDictionary<Process, Guid> _agentProcessIndex = new ConcurrentDictionary<Process, Guid>();

        public LocalProcessAgentProvider(TestAgency agency)
        {
            _agency = agency;
            _agentStore = agency._agentStore;
        }

        private List<TestAgentInfo>? _availableAgents;

        #region ITestAgentInfo Implementation

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

        public IList<TestAgentInfo> GetAgentsForPackage(TestPackage package)
        {
            Guard.ArgumentNotNull(package, nameof(package));

            // This method is primarily intended for use in implementing a command-line
            // option to allow selection of the agent to be used for a test run. Because
            // the option would apply to all assemblies being run, any agents returned
            // must be able to run all assemblies.

            // Initialize lists with ALL available agents
            var validAgentNames = new HashSet<string>(AvailableAgents.Select(info => info.AgentName));

            // Look at each included assembly package to see if any names should be removed
            foreach (var assemblyPackage in package.Select(p => p.IsAssemblyPackage))
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

        public bool IsAgentAvailable(TestPackage package)
        {
            foreach (var node in LauncherNodes)
                if (CanCreateAgent(node, package))
                    return true;
            return false;
        }

        public ITestAgent GetAgent(TestPackage package)
        {
            string runtimeSetting = package.Settings.GetValueOrDefault(SettingDefinitions.TargetFrameworkName);
            var targetRuntime = new FrameworkName(runtimeSetting);
            var agentId = Guid.NewGuid();
            var agentProcess = CreateAgentProcess(agentId, _agency.TcpEndPoint, package);
            agentProcess.Exited += (sender, e) => OnAgentExit(agentProcess);

            agentProcess.Start();
            log.Debug("Launched Agent process {0} - see nunit-agent_{0}.log", agentProcess.Id);
            log.Debug("Command line: \"{0}\" {1}", agentProcess.StartInfo.FileName, agentProcess.StartInfo.Arguments);

            _agentStore.AddAgent(agentId, agentProcess);
            _agentProcessIndex.TryAdd(agentProcess, agentId);

            // An agent can exit before its process is added to the index.
            if (agentProcess.HasExited)
                OnAgentExit(agentProcess);

            log.Debug($"Waiting for agent {agentId:B} to register");

            const int pollTime = 200;

            // Increase the timeout to give time to attach a debugger
            bool debug = package.Settings.GetValueOrDefault(SettingDefinitions.DebugAgent) ||
                         package.Settings.GetValueOrDefault(SettingDefinitions.PauseBeforeRun);

            int waitTime = debug ? DEBUG_TIMEOUT : NORMAL_TIMEOUT;

            // Wait for agent registration based on the agent actually getting processor time to avoid falling over
            // under process starvation.
            while (!agentProcess.HasExited && waitTime > agentProcess.TotalProcessorTime.TotalMilliseconds)
            {
                Thread.Sleep(pollTime);

                if (_agentStore.IsReady(agentId, out var agent))
                {
                    log.Debug($"Returning new agent {agentId:B}");

                    return agent;
                }
            }

            if (agentProcess.HasExited)
                throw new NUnitEngineException($"Unable to acquire remote process agent (exit code {agentProcess.ExitCode}). " +
                    TestAgency.GetAgentExitMessage(agentProcess.ExitCode));

            throw new NUnitEngineException("Unable to acquire remote process agent");
        }

        public void ReleaseAgent(ITestAgent agent)
        {
            if (_agentStore.IsAgentActive(agent.Id, out Process? process))
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
                    catch (InvalidOperationException)
                    {
                        // The process has not exited yet. We can't get the exit code, but we know the connection was forcibly closed.
                        exitCode = -16;
                    }
                    catch (NotSupportedException)
                    {
                        // The process is running on a remote computer. We can't get the exit code, but we know the connection was forcibly closed.
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

        private List<ExtensionNode>? _launcherNodes;
        internal List<ExtensionNode> LauncherNodes
        {
            get
            {
                if (_launcherNodes is null)
                {
                    Guard.OperationValid(_agency._extensionService is not null, "LauncherNodes property may not be accessed before TestAgency service is started");

                    _launcherNodes = [.. _agency._extensionService.GetExtensionNodes(AGENT_LAUNCHERS_PATH)];

                    _launcherNodes.Sort((l1, l2) =>
                    {
                        var s1 = l1.GetValues("TargetFramework").FirstOrDefault();
                        var fn1 = s1 is not null ? new FrameworkName(s1) : GetLauncherInstance(l1).AgentInfo.TargetRuntime;
                        var s2 = l2.GetValues("TargetFramework").FirstOrDefault();
                        var fn2 = s2 is not null ? new FrameworkName(s2) : GetLauncherInstance(l2).AgentInfo.TargetRuntime;

                        int result = fn1.Identifier.CompareTo(fn2.Identifier);
                        if (result == 0)
                            result = fn1.Version.CompareTo(fn2.Version);

                        return result;
                    });
                }

                return _launcherNodes;
            }
        }

        internal static bool CanCreateAgent(ExtensionNode node, TestPackage package)
        {
            // Newer implementations use a TargetFramework property to avoid
            // intantiating any agents, which will not be used.
            var runtimes = node.GetValues("TargetFramework");

            // If there is no property, we have to instantiate it to check.
            if (!runtimes.Any())
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

        private static IAgentLauncher GetLauncherInstance(ExtensionNode node)
        {
            var launcher = node.ExtensionObject as IAgentLauncher;
            if (launcher is not null)
                return launcher;

            throw new NUnitEngineException("Internal Error: Expected an IAgentLancher");
        }

        internal virtual Process CreateAgentProcess(Guid agentId, string agencyUrl, TestPackage package)
        {
            // Check to see if a specific agent was selected
            string requestedAgent = package.Settings.GetValueOrDefault(SettingDefinitions.RequestedAgentName);
            bool specificAgentRequested = !string.IsNullOrEmpty(requestedAgent);
            string requestedRuntime = package.Settings.GetValueOrDefault(SettingDefinitions.RequestedFrameworkName);
            bool specificRuntimeRequested = !string.IsNullOrEmpty(requestedRuntime);

            foreach (var node in LauncherNodes)
            {
                if (specificAgentRequested && node.TypeName != requestedAgent)
                    continue;

                if (specificRuntimeRequested)
                {
                    var requestedFrameworkName = new FrameworkName(requestedRuntime);
                    var runtimes = node.GetValues("TargetFramework");
                    if (!runtimes.Any() || new RuntimeFramework(runtimes.First()).FrameworkName != requestedFrameworkName)
                        continue;
                }

                if (!CanCreateAgent(node, package))
                    continue;

                var launcher = GetLauncherInstance(node);

                var launcherName = launcher.GetType().Name;
                log.Info($"Selected launcher {launcherName}");
                package.ApplySetting(SettingDefinitions.SelectedAgentName.WithValue(launcherName), false);
                return launcher.CreateAgent(agentId, agencyUrl, package);
            }

            if (specificAgentRequested)
                throw new NUnitEngineException($"The requested launcher {requestedAgent} cannot load package {package.Name}");
            else if (specificRuntimeRequested)
                throw new NUnitEngineException($"No agent available to run {package.Name} under {requestedRuntime}");
            else
                throw new NUnitEngineException($"No agent available for {package.Name}");
        }

        internal void OnAgentExit(Process process)
        {
            if (_agentProcessIndex.TryGetValue(process, out var agentId))
                _agentStore.MarkTerminated(agentId);
        }
    }
}
