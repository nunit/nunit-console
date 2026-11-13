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
            localProvider.AvailableAgents.Returns([NET462_INFO, NET80_INFO, NET90_INFO, NET10_INFO]);
            localProvider.GetAgentsForPackage(Arg.Any<TestPackage>()).Returns(
                callInfo =>
                {
                    var package = callInfo.Arg<TestPackage>();
                    var imageTargetFrameworkName = package.Settings.GetValueOrDefault(SettingDefinitions.ImageTargetFrameworkName);
                    if (string.IsNullOrEmpty(imageTargetFrameworkName))
                        return Array.Empty<TestAgentInfo>();

                    var imageTargetFramework = new FrameworkName(imageTargetFrameworkName);
                    return imageTargetFramework.Identifier.Equals(FrameworkIdentifiers.NetFramework)
                        ? [NET462_INFO]
                        : [NET80_INFO, NET90_INFO, NET10_INFO];
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
        }

        [Test]
        public void GetAvailableAgents()
        {
            Assert.That(_testAgentService.AvailableAgents.Select(a => a.AgentName),
                Is.EquivalentTo([NET462AGENT, NET80AGENT, NET90AGENT, NET10AGENT, INPROCESSAGENT]));
        }

        [Test]
        public void GetAgentsForPackage_Net462()
        {
            var package = new TestPackage("test-assembly.dll");
            package.AddSetting(SettingDefinitions.ImageTargetFrameworkName.WithValue(
                new FrameworkName(FrameworkIdentifiers.NetFramework, new Version(4, 6, 2)).ToString()), true);
            Assert.That(_testAgentService.GetAgentsForPackage(package).Select(a => a.AgentName), Is.EquivalentTo(
                new[] { NET462AGENT }));
        }

        [Test]
        public void GetAgentsForPackage_Net80()
        {
            var package = new TestPackage("test-assembly.dll");
            package.AddSetting(SettingDefinitions.ImageTargetFrameworkName.WithValue(
                new FrameworkName(FrameworkIdentifiers.NetCoreApp, new Version(8, 0, 0)).ToString()), true);
            Assert.That(_testAgentService.GetAgentsForPackage(package).Select(a => a.AgentName),
                Is.EquivalentTo([NET80AGENT, NET90AGENT, NET10AGENT]));
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
    }
}
