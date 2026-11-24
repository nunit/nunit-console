// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using System;
using System.Runtime.Versioning;
using NSubstitute;
using NUnit.Extensibility;
using NUnit.Framework;

namespace NUnit.Engine.Services
{
    /// <summary>
    /// When several pluggable agents are installed, more than one may provide an
    /// in-process agent. The provider must choose the one built for its own runtime.
    /// </summary>
    [TestFixture]
    public class InProcessAgentNodeSelectionTests
    {
        private const string NET462 = ".NETFramework,Version=v4.6.2";
        private const string NET100 = ".NETCoreApp,Version=v10.0";

        private static readonly FrameworkName NET462_RUNTIME = new FrameworkName(NET462);
        private static readonly FrameworkName NET100_RUNTIME = new FrameworkName(NET100);

        [Test]
        public void SelectsNodeMatchingRuntime_WhenOtherRuntimeIsListedFirst()
        {
            var net10Node = CreateNode("Net10", NET100);
            var net462Node = CreateNode("Net462", NET462);

            Assert.That(InProcessAgentProvider.SelectAgentNode(new[] { net10Node, net462Node }, NET462_RUNTIME), Is.SameAs(net462Node));
            Assert.That(InProcessAgentProvider.SelectAgentNode(new[] { net462Node, net10Node }, NET100_RUNTIME), Is.SameAs(net10Node));
        }

        [Test]
        public void ReturnsNull_WhenNoNodeMatchesRuntime()
        {
            var nodes = new[] { CreateNode("Net10", NET100) };

            Assert.That(InProcessAgentProvider.SelectAgentNode(nodes, NET462_RUNTIME), Is.Null);
        }

        [Test]
        public void ReturnsNull_WhenOnlyVersionDiffers()
        {
            var nodes = new[] { CreateNode("Net90", ".NETCoreApp,Version=v9.0") };

            Assert.That(InProcessAgentProvider.SelectAgentNode(nodes, NET100_RUNTIME), Is.Null);
        }

        [Test]
        public void IgnoresNodeWithoutTargetFramework()
        {
            var noTarget = CreateNode("NoTarget");
            var net10Node = CreateNode("Net10", NET100);

            Assert.That(InProcessAgentProvider.SelectAgentNode(new[] { noTarget, net10Node }, NET100_RUNTIME), Is.SameAs(net10Node));
        }

        [Test]
        public void IgnoresNodeWithInvalidTargetFramework()
        {
            var invalid = CreateNode("Invalid", "not a framework name");
            var net10Node = CreateNode("Net10", NET100);

            Assert.That(InProcessAgentProvider.SelectAgentNode(new[] { invalid, net10Node }, NET100_RUNTIME), Is.SameAs(net10Node));
        }

        private static IExtensionNode CreateNode(string typeName, params string[] targetFrameworks)
        {
            var node = Substitute.For<IExtensionNode>();
            node.TypeName.Returns(typeName);
            node.GetValues("TargetFramework").Returns(targetFrameworks);
            return node;
        }
    }
}
