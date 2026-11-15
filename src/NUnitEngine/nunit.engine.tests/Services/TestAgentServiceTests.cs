// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Versioning;
using NSubstitute;
using NUnit.Common;
using NUnit.Framework;
using NUnit.Framework.Constraints;

namespace NUnit.Engine.Services
{
    public class TestAgentServiceTests
    {
        private TestAgentService _testAgentService;
        private ServiceContext _services;

        // TODO: Decide between launcher and agent names and whether to use fullnames
        private const string NET462AGENT = "NUnit.Engine.Agents.Net462AgentLauncher";
        private const string NET80AGENT = "NUnit.Engine.Agents.Net80AgentLauncher";
        private const string NET90AGENT = "NUnit.Engine.Agents.Net90AgentLauncher";
        private const string NET10AGENT = "NUnit.Engine.Agents.Net10AgentLauncher";
        private const string INPROCESSAGENT = "NUnit.Engine.Agents.InProcessAgent";

        private static readonly TestAgentInfo NET462_INFO = new TestAgentInfo(
            NET462AGENT, TestAgentType.LocalProcess, new FrameworkName(FrameworkIdentifiers.NetFramework, new Version(4, 6, 2)));
        private static readonly TestAgentInfo NET80_INFO = new TestAgentInfo(
            NET80AGENT, TestAgentType.LocalProcess, new FrameworkName(FrameworkIdentifiers.NetFramework, new Version(4, 8, 0)));
        private static readonly TestAgentInfo NET90_INFO = new TestAgentInfo(
            NET90AGENT, TestAgentType.LocalProcess, new FrameworkName(FrameworkIdentifiers.NetFramework, new Version(4, 9, 0)));
        private static readonly TestAgentInfo NET10_INFO = new TestAgentInfo(
            NET10AGENT, TestAgentType.LocalProcess, new FrameworkName(FrameworkIdentifiers.NetFramework, new Version(1, 0, 0)));
        private static readonly TestAgentInfo INPROCESS_INFO = new TestAgentInfo(
#if NETFRAMEWORK
            INPROCESSAGENT, TestAgentType.InProcess, new FrameworkName(".NETFramework,Version=v4.6.2"));
#else
            INPROCESSAGENT, TestAgentType.InProcess, new FrameworkName(".NETCoreApp,Version=v10.0"));
#endif

        [SetUp]
        public void CreateServiceContext()
        {
            var localProvider = Substitute.For<ITestAgentProvider, IService>();
            // NOTE: This code simulatesthe behavior expected of the
            // local provider, which is not being tested here. At some point
            // we may want to use the actual providers in this fixture.
            localProvider.AvailableAgents.Returns([NET462_INFO, NET80_INFO, NET90_INFO, NET10_INFO]);
            localProvider.GetAgentsForPackage(Arg.Any<TestPackage>()).Returns(
                callInfo =>
                {
                    var package = callInfo.Arg<TestPackage>();
                    var imageTargetFrameworkName = package.Settings.GetValueOrDefault(SettingDefinitions.ImageTargetFrameworkName);
                    if (string.IsNullOrEmpty(imageTargetFrameworkName))
                        return Array.Empty<TestAgentInfo>();

                    var imageTargetFramework = new FrameworkName(imageTargetFrameworkName);
                    if (imageTargetFramework.Identifier.Equals(FrameworkIdentifiers.NetFramework))
                        return [NET462_INFO];
                    else
                        switch (imageTargetFramework.Version.Major)
                        {
                            case < 8:
                                return [NET80_INFO, NET90_INFO, NET10_INFO];
                            case 9:
                                return [NET90_INFO, NET10_INFO];
                            case 10:
                                return [NET10_INFO];
                            default:
                                return Array.Empty<TestAgentInfo>();
                        }
                });
            localProvider.IsAgentAvailable(Arg.Any<TestPackage>()).Returns(
                callInfo =>
                {
                    var package = callInfo.Arg<TestPackage>();
                    var imageTargetFrameworkName = package.Settings.GetValueOrDefault(SettingDefinitions.ImageTargetFrameworkName);
                    if (string.IsNullOrEmpty(imageTargetFrameworkName))
                        return false;
                    var imageTargetFramework = new FrameworkName(imageTargetFrameworkName);
                    return imageTargetFramework.Identifier.Equals(FrameworkIdentifiers.NetFramework)
                        || imageTargetFramework.Version.Major <= 10;
                });
            localProvider.GetAgent(Arg.Any<TestPackage>()).Returns(
                callInfo =>
                {
                    var package = callInfo.Arg<TestPackage>();
                    var targetFrameworkName = package.Settings.GetValueOrDefault(SettingDefinitions.TargetFrameworkName);
                    if (string.IsNullOrEmpty(targetFrameworkName))
                        throw new InvalidOperationException("No target framework specified");
                    var targetFramework = new FrameworkName(targetFrameworkName);
                    if (targetFramework.Identifier.Equals(FrameworkIdentifiers.NetFramework))
                        return Substitute.For<ITestAgent>();
                    else
                        switch (targetFramework.Version.Major)
                        {
                            case < 10:
                                return Substitute.For<ITestAgent>();
                            default:
                                throw new InvalidOperationException($"No agent available for {targetFramework}");
                        }
                });

            var remoteProvider = Substitute.For<ITestAgentProvider, IService>();

            var inProcessProvider = Substitute.For<ITestAgentProvider, IService>();
            inProcessProvider.AvailableAgents.Returns([INPROCESS_INFO]);

            _services = new ServiceContext();
            _services.Add((IService)localProvider);
            _services.Add((IService)remoteProvider);
            _services.Add((IService)inProcessProvider);
            _testAgentService = new TestAgentService();
            _services.Add(_testAgentService);
            _services.ServiceManager.StartServices();
        }

        [TearDown]
        public void TearDown()
        {
            _testAgentService.Dispose();
            _services.ServiceManager.Dispose();
        }

        [Test]
        public void ServiceIsStarted()
        {
            Assert.That(_testAgentService.Status, Is.EqualTo(ServiceStatus.Started));
            Assert.That(_services.GetService<TestAgentService>(), Is.SameAs(_testAgentService));
            Assert.That(_services.GetService<ITestAgentService>(), Is.SameAs(_testAgentService));
            Assert.That(_services.GetService<ITestAgentInfo>(), Is.Not.SameAs(_testAgentService));
            Assert.That(_services.GetService<ITestAgentProvider>(), Is.Not.SameAs(_testAgentService));
        }

        [Test]
        public void ServiceIsStopped()
        {
            _services.ServiceManager.StopServices();
            Assert.That(_testAgentService.Status, Is.EqualTo(ServiceStatus.Stopped));
        }

        [Test]
        public void GetAvailableAgents()
        {
            Assert.That(_testAgentService.AvailableAgents.Select(a => a.AgentName),
                Is.EquivalentTo([NET462AGENT, NET80AGENT, NET90AGENT, NET10AGENT, INPROCESSAGENT]));
        }

        [TestCaseSource(nameof(SupportedRuntimes))]
        public void GetAgentsForPackage(FrameworkName runtime)
        {
            var package = new TestPackage("test-assembly.dll");
            package.AddSetting(SettingDefinitions.ImageTargetFrameworkName.WithValue(runtime.ToString()), true);
            if (runtime.Identifier.Equals(FrameworkIdentifiers.NetFramework))
                Assert.That(_testAgentService.GetAgentsForPackage(package).Select(a => a.AgentName),
                    Is.EquivalentTo([NET462AGENT]));
            else
            {
                string[] expectedAgents = Array.Empty<string>();
                switch (runtime.Version.Major)
                {
                    case < 8:
                        expectedAgents = [NET80AGENT, NET90AGENT, NET10AGENT];
                        break;
                    case 9:
                        expectedAgents = [NET90AGENT, NET10AGENT];
                        break;
                    case 10:
                        expectedAgents = [NET10AGENT];
                        break;
                    default:
                        break;
                }

                Assert.That(_testAgentService.GetAgentsForPackage(package).Select(a => a.AgentName),
                    Is.EquivalentTo(expectedAgents));
            }
        }

        [Test]
        public void GetAgentsForPackage_ConflictingAssemblies()
        {
            var package = new TestPackage("test1.dll", "test2.dll");
            package.SubPackages[0].AddSetting(SettingDefinitions.ImageTargetFrameworkName.WithValue(
                new FrameworkName(FrameworkIdentifiers.NetFramework, new Version(4, 6, 2)).ToString()), true);
            package.SubPackages[1].AddSetting(SettingDefinitions.ImageTargetFrameworkName.WithValue(
                new FrameworkName(FrameworkIdentifiers.NetCoreApp, new Version(8, 0, 0)).ToString()), true);
            Assert.That(_testAgentService.GetAgentsForPackage(package).Select(a => a.AgentName), Is.Empty);
        }

        [TestCaseSource(nameof(SupportedRuntimes))]
        public void IsAgentAvailable_ReturnsTrueForAvailableAgent(FrameworkName runtime)
        {
            var package = new TestPackage("test-assembly.dll");
            package.AddSetting(SettingDefinitions.ImageTargetFrameworkName.WithValue(runtime.ToString()), true);
            Assert.That(_testAgentService.IsAgentAvailable(package), Is.True);
        }

        [Test]
        public void IsAgentAvailable_ReturnsFalseForUnavailableAgent()
        {
            var package = new TestPackage("test-assembly.dll");
            package.AddSetting(SettingDefinitions.ImageTargetFrameworkName.WithValue(
                new FrameworkName(FrameworkIdentifiers.NetCoreApp, new Version(11, 0, 0)).ToString()), true);
            Assert.That(_testAgentService.IsAgentAvailable(package), Is.False);
        }

        [TestCaseSource(nameof(SupportedRuntimes))]
        public void GetAgent_ReturnsAgentForAvailableAgent(FrameworkName runtime)
        {
            var package = new TestPackage("test-assembly.dll");
            package.AddSetting(SettingDefinitions.ImageTargetFrameworkName.WithValue(runtime.ToString()), true);
            package.AddSetting(SettingDefinitions.TargetFrameworkName.WithValue(
                new FrameworkName(FrameworkIdentifiers.NetCoreApp, new Version(8, 0, 0)).ToString()), true);
            var agent = _testAgentService.GetAgent(package);
            Assert.That(agent, Is.Not.Null);
        }

        [Test]
        public void GetAgent_ThrowsForUnavailableAgent()
        {
            var package = new TestPackage("test-assembly.dll");
            package.AddSetting(SettingDefinitions.ImageTargetFrameworkName.WithValue(
                new FrameworkName(FrameworkIdentifiers.NetCoreApp, new Version(11, 0, 0)).ToString()), true);
            package.AddSetting(SettingDefinitions.TargetFrameworkName.WithValue(
                new FrameworkName(FrameworkIdentifiers.NetCoreApp, new Version(11, 0, 0)).ToString()), true);
            Assert.That(() => _testAgentService.GetAgent(package), Throws.InvalidOperationException);
        }

        [Test]
        public void ReturnAgent_SucceedsForOwnedAgent()
        {
            var package = new TestPackage("test-assembly.dll");
            package.AddSetting(SettingDefinitions.ImageTargetFrameworkName.WithValue(
                new FrameworkName(FrameworkIdentifiers.NetCoreApp, new Version(8, 0, 0)).ToString()), true);
            package.AddSetting(SettingDefinitions.TargetFrameworkName.WithValue(
                new FrameworkName(FrameworkIdentifiers.NetCoreApp, new Version(8, 0, 0)).ToString()), true);
            var agent = _testAgentService.GetAgent(package);
            Assert.That(_testAgentService.IsAgentActive(agent.Id), Is.True);
            Assert.That(() => _testAgentService.ReturnAgent(agent), Throws.Nothing);
            Assert.That(_testAgentService.IsAgentActive(agent.Id), Is.False);
        }

        [Test]
        public void ReturnAgent_ThrowsForUnownedAgent()
        {
            var package = new TestPackage("test-assembly.dll");

            // Get an agent and verify that we own it
            package.AddSetting(SettingDefinitions.ImageTargetFrameworkName.WithValue(
                new FrameworkName(FrameworkIdentifiers.NetCoreApp, new Version(8, 0, 0)).ToString()), true);
            package.AddSetting(SettingDefinitions.TargetFrameworkName.WithValue(
                new FrameworkName(FrameworkIdentifiers.NetCoreApp, new Version(8, 0, 0)).ToString()), true);
            var agent = _testAgentService.GetAgent(package);
            Assert.That(_testAgentService.IsAgentActive(agent.Id), Is.True);

            // Now create a new agent that we don't own and verify that we don't own it
            var unownedAgent = Substitute.For<ITestAgent>();
            unownedAgent.Id.Returns(Guid.NewGuid());
            Assert.That(_testAgentService.IsAgentActive(unownedAgent.Id), Is.False);
            // Trying to return it throws an exception
            Assert.That(() => _testAgentService.ReturnAgent(unownedAgent), Throws.InvalidOperationException);

            // Verify that we still own the agent we originally created
            Assert.That(_testAgentService.IsAgentActive(agent.Id), Is.True);
        }

        private static FrameworkName[] SupportedRuntimes => new[]
        {
            // We have actual agents for these frameworks
            new FrameworkName(FrameworkIdentifiers.NetFramework, new Version(4, 6, 2)),
            new FrameworkName(FrameworkIdentifiers.NetCoreApp, new Version(8, 0, 0)),
            new FrameworkName(FrameworkIdentifiers.NetCoreApp, new Version(9, 0, 0)),
            new FrameworkName(FrameworkIdentifiers.NetCoreApp, new Version(10, 0, 0)),
            // A selection of runtimes with compatible agents
            new FrameworkName(FrameworkIdentifiers.NetFramework, new Version(4, 0, 0)),
            new FrameworkName(FrameworkIdentifiers.NetCoreApp, new Version(3, 1, 0)),
            new FrameworkName(FrameworkIdentifiers.NetCoreApp, new Version(5, 0, 0)),
            new FrameworkName(FrameworkIdentifiers.NetCoreApp, new Version(7, 0, 0))
        };
    }
}
