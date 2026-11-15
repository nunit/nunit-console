// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using System.Linq;
using NUnit.Common;
using NUnit.Extensibility;
using NUnit.Framework;

namespace NUnit.Engine.Services
{
    /// <summary>
    /// Checks launcher metadata against the runtime selected for execution, including
    /// a missing assembly runtime that was promoted to the coordinator's newer runtime.
    /// </summary>
    public class LocalProcessAgentSelectionTests
    {
        [TestCase(6, 10, 8, false)]
        [TestCase(7, 10, 8, false)]
        [TestCase(7, 10, 9, false)]
        [TestCase(7, 10, 10, true)]
        [TestCase(8, 10, 8, false)]
        [TestCase(8, 8, 8, true)]
        [TestCase(7, 8, 8, true)]
        [TestCase(7, 0, 8, true)]
        [TestCase(10, 0, 8, false)]
        public void UsesResolvedRuntimeWhenPresent(int imageVersion, int resolvedVersion, int agentVersion, bool expected)
        {
            var package = new TestPackage("test-assembly.dll").SubPackages[0];
            package.AddSetting(SettingDefinitions.ImageTargetFrameworkName.WithValue($".NETCoreApp,Version=v{imageVersion}.0"), false);
            if (resolvedVersion != 0)
                package.AddSetting(SettingDefinitions.TargetFrameworkName.WithValue($".NETCoreApp,Version=v{resolvedVersion}.0"), false);

            using var assembly = new ExtensionAssembly(typeof(LocalProcessAgentSelectionTests).Assembly.Location, false);
            var type = assembly.Assembly.MainModule.Types.Single(value => value.FullName == typeof(LocalProcessAgentSelectionTests).FullName);
            var node = new ExtensionNode(assembly, type);
            node.AddProperty("TargetFramework", $".NETCoreApp,Version=v{agentVersion}.0");

            Assert.That(LocalProcessAgentProvider.CanCreateAgent(node, package), Is.EqualTo(expected));
        }
    }
}
