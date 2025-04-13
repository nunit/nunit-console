// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

#if NETFRAMEWORK
using System;
using System.Runtime.Versioning;
using NSubstitute;
using NUnit.Extensibility;
using NUnit.Framework;

namespace NUnit.Extensibility
{
    internal class ExtensionSelectorTests
    {
        [Test]
        public void IsDuplicateOfWithSameName()
        {
            var first = MockExtension("SameExtension");
            var second = MockExtension("SameExtension");
            Assert.Multiple(() =>
            {
                Assert.That(first.IsDuplicateOf(second), Is.True);
                Assert.That(second.IsDuplicateOf(first), Is.True);
            });
        }

        [Test]
        public void IsDuplicateOfWithDifferentNames()
        {
            var first = MockExtension("Extension1");
            var second = MockExtension("Extension2");
            Assert.Multiple(() =>
            {
                Assert.That(first.IsDuplicateOf(second), Is.False);
                Assert.That(second.IsDuplicateOf(first), Is.False);
            });
        }

        [Test]
        public void IsBetterVersionOfThrowsWhenNotDuplicates()
        {
            var first = MockExtension("Extension1");
            var second = MockExtension("Extension2");
            Assert.That(() => first.IsBetterVersionOf(second), Throws.InvalidOperationException);
        }

        [Test]
        public void IsBetterVersionOfChoosesHighestAssemblyVersion()
        {
            var first = MockExtension("SameExtension", assemblyVersion: new Version(2, 0));
            var second = MockExtension("SameExtension", assemblyVersion: new Version(4, 7));
            Assert.Multiple(() =>
            {
                Assert.That(first.IsBetterVersionOf(second), Is.False);
                Assert.That(second.IsBetterVersionOf(first), Is.True);
            });
        }

        [Test]
        public void IsBetterVersionOfChoosesHighestTargetFramework()
        {
            var first = MockExtension("SameExtension", targetFramework: new Version(2, 0));
            var second = MockExtension("SameExtension", targetFramework: new Version(4, 7));
            Assert.Multiple(() =>
            {
                Assert.That(first.IsBetterVersionOf(second), Is.False);
                Assert.That(second.IsBetterVersionOf(first), Is.True);
            });
        }

        [Test]
        public void IsBetterVersionOfPrioritisesAssemblyVersionOverTargetFramework()
        {
            var first = MockExtension("SameExtension", assemblyVersion: new Version(2, 0), targetFramework: new Version(2, 0));
            var second = MockExtension("SameExtension", assemblyVersion: new Version(1, 0), targetFramework: new Version(4, 7));
            Assert.Multiple(() =>
            {
                Assert.That(first.IsBetterVersionOf(second), Is.True);
                Assert.That(second.IsBetterVersionOf(first), Is.False);
            });
        }

        [Test]
        public void IsBetterVersionOfPrefersDirectlySpecifiedToWildcard()
        {
            var first = MockExtension("SameExtension", fromWildcard: false);
            var second = MockExtension("SameExtension", fromWildcard: true);
            Assert.Multiple(() =>
            {
                Assert.That(first.IsBetterVersionOf(second), Is.True);
                Assert.That(second.IsBetterVersionOf(first), Is.False);
            });
        }

        [Test]
        public void IsBetterVersionOfPrefersNoChangeIfFromWildcard()
        {
            var first = MockExtension("SameExtension", fromWildcard: true);
            var second = MockExtension("SameExtension", fromWildcard: true);
            Assert.Multiple(() =>
            {
                Assert.That(first.IsBetterVersionOf(second), Is.False);
                Assert.That(second.IsBetterVersionOf(first), Is.False);
            });
        }

        private static IExtensionAssembly MockExtension(
            string assemblyName,
            Version? assemblyVersion = null,
            Version? targetFramework = null,
            bool fromWildcard = false)
        {
            var sub = Substitute.For<IExtensionAssembly>();
            sub.AssemblyName.Returns(assemblyName);
            sub.AssemblyVersion.Returns(assemblyVersion ?? new Version(1, 0));
            targetFramework = targetFramework ?? new Version(2, 0);
            sub.FrameworkName.Returns(new FrameworkName(FrameworkIdentifiers.NetFramework, targetFramework));
            sub.FromWildCard.Returns(fromWildcard);
            return sub;
        }
    }
}
#endif