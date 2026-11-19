// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Versioning;
using System.Text;
using System.Threading.Tasks;

using NUnit.Framework;
using NUnit.Common;

namespace NUnit.Engine.Services
{
    public class InProcessAgentProviderTests
    {
#if NETFRAMEWORK
        private static bool IS_NETFX_BUILD = true;
#else
        private static bool IS_NETFX_BUILD = false;
#endif

        private static readonly FrameworkName AGENT_RUNTIME = IS_NETFX_BUILD
            ? new FrameworkName(".NETFramework", new Version(4, 6, 2))
            : new FrameworkName(".NETCoreApp", new Version(10, 0));

        private static readonly TestCaseData[] TargetRuntimes =
        [
            // These are all valid for the net462 console runner
            new TestCaseData(".NETFramework,Version=2.0", IS_NETFX_BUILD),
            new TestCaseData(".NETFramework,Version=3.5", IS_NETFX_BUILD),
            new TestCaseData(".NETFramework,Version=4.0", IS_NETFX_BUILD),
            new TestCaseData(".NETFramework,Version=4.6.2", IS_NETFX_BUILD),
            new TestCaseData(".NETFramework,Version=4.8.1", IS_NETFX_BUILD),
            // These are all valid for the net10.0 console runner
            new TestCaseData(".NETCoreApp,Version=2.1", !IS_NETFX_BUILD),
            new TestCaseData(".NETCoreApp,Version=3.1", !IS_NETFX_BUILD),
            new TestCaseData(".NETCoreApp,Version=5.0", !IS_NETFX_BUILD),
            new TestCaseData(".NETCoreApp,Version=6.0", !IS_NETFX_BUILD),
            new TestCaseData(".NETCoreApp,Version=7.0", !IS_NETFX_BUILD),
            new TestCaseData(".NETCoreApp,Version=8.0", !IS_NETFX_BUILD),
            new TestCaseData(".NETCoreApp,Version=9.0", !IS_NETFX_BUILD),
            new TestCaseData(".NETCoreApp,Version=10.0", !IS_NETFX_BUILD),
            // These are not valid for either console runner.
            new TestCaseData(".NETCoreApp,Version=25.0", false),
        ];

        [Test]
        public void AvailableAgents_ReturnsSingleAgent()
        {
            var provider = new InProcessAgentProvider();
            var agents = provider.AvailableAgents;
            Assert.That(agents, Has.Count.EqualTo(1));
            Assert.That(agents[0].AgentType, Is.EqualTo(TestAgentType.InProcess));
            Assert.That(agents[0].TargetRuntime, Is.EqualTo(AGENT_RUNTIME));
        }

        [TestCaseSource(nameof(TargetRuntimes))]
        public void GetAgentsForPackage_ReturnsAgentInfoForCompatiblePackage(string targetRuntime, bool isCompatible)
        {
            var package = new TestPackage("dummy.dll");
            package.AddSetting(SettingDefinitions.TargetFrameworkName.WithValue(targetRuntime), true);

            var provider = new InProcessAgentProvider();
            var agents = provider.GetAgentsForPackage(package);
            if (isCompatible)
            {
                Assert.That(agents, Has.Count.EqualTo(1));
                Assert.That(agents[0].AgentType, Is.EqualTo(TestAgentType.InProcess));
                Assert.That(agents[0].TargetRuntime, Is.EqualTo(AGENT_RUNTIME));
            }
            else
            {
                Assert.That(agents, Is.Empty);
            }
        }

        [TestCaseSource(nameof(TargetRuntimes))]
        public void IsAgentAvailable_ReturnsTrueForCompatiblePackage(string targetRuntime, bool isCompatible)
        {
            var package = new TestPackage("dummy.dll");
            package.AddSetting(SettingDefinitions.TargetFrameworkName.WithValue(AGENT_RUNTIME), true);
            var provider = new InProcessAgentProvider();
            Assert.That(provider.IsAgentAvailable(package), Is.EqualTo(isCompatible));
        }

        [TestCaseSource(nameof(TargetRuntimes))]
        public void GetAgent_ReturnsAgentForCompatiblePackage(string targetRuntime, bool isCompatible)
        {
            var package = new TestPackage("dummy.dll");
            package.AddSetting(SettingDefinitions.ImageTargetFrameworkName.WithValue(targetRuntime), true);
            var provider = new InProcessAgentProvider();
            //Assert.That(provider.IsAgentAvailable(package), Is.EqualTo(isCompatible));

            if (isCompatible)
            {
                package.AddSetting(SettingDefinitions.TargetFrameworkName.WithValue(AGENT_RUNTIME.ToString()), true);
                Assert.That(provider.GetAgent(package), Is.Not.Null);
            }
            else
                Assert.That(() => provider.GetAgent(package), Throws.InvalidOperationException);
        }

        [TestCaseSource(nameof(TargetRuntimes))]
        public void ReturnAgent_DoesNotThrow(string targetRuntime, bool isCompatible)
        {
            var package = new TestPackage("dummy.dll");
            package.AddSetting(SettingDefinitions.TargetFrameworkName.WithValue(targetRuntime), true);
            var provider = new InProcessAgentProvider();
            Assert.That(provider.IsAgentAvailable(package), Is.EqualTo(isCompatible));

            //if (isCompatible)
            //{
            //    var agent = provider.GetAgent(package);
            //    Assert.That(() => provider.ReturnAgent(agent), Throws.Nothing);
            //}
            //else
            //{
            //    Assert.That(() => provider.GetAgent(package), Throws.InvalidOperationException);
            //}
        }
    }
}
