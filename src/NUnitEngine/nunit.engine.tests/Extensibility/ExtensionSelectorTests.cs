// ***********************************************************************
// Copyright (c) 2018 Charlie Poole, Rob Prouse
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
// ***********************************************************************


#if !NETCOREAPP1_1 && !NETCOREAPP2_0
using System;
using NSubstitute;
using NUnit.Engine.Extensibility;
using NUnit.Framework;

namespace NUnit.Engine.Tests.Extensibility
{
    internal class ExtensionSelectorTests
    {
        [Test]
        public void IsDuplicateOfWithSame()
        {
            var first = MockExtension("Extension1");
            var second = MockExtension("Extension1");
            Assert.Multiple(() =>
            {
                Assert.That(first.IsDuplicateOf(second), Is.True);
                Assert.That(second.IsDuplicateOf(first), Is.True);
            });
        }

        [Test]
        public void IsDuplicateOfWithDifferent()
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
            var first = MockExtension(assemblyVersion: new Version(2, 0));
            var second = MockExtension(assemblyVersion: new Version(4, 7));
            Assert.Multiple(() =>
            {
                Assert.That(first.IsBetterVersionOf(second), Is.False);
                Assert.That(second.IsBetterVersionOf(first), Is.True);
            });
        }

        [Test]
        public void IsBetterVersionOfChoosesHighestTargetFramework()
        {
            var first = MockExtension(targetFramework: new Version(2, 0));
            var second = MockExtension(targetFramework: new Version(4, 7));
            Assert.Multiple(() =>
            {
                Assert.That(first.IsBetterVersionOf(second), Is.False);
                Assert.That(second.IsBetterVersionOf(first), Is.True);
            });
        }

        [Test]
        public void IsBetterVersionOfPrioritisesAssemblyVersionOverTargetFramework()
        {
            var first = MockExtension(assemblyVersion: new Version(2, 0), targetFramework: new Version(2, 0));
            var second = MockExtension(assemblyVersion: new Version(1, 0), targetFramework: new Version(4, 7));
            Assert.Multiple(() =>
            {
                Assert.That(first.IsBetterVersionOf(second), Is.True);
                Assert.That(second.IsBetterVersionOf(first), Is.False);
            });
        }

        [Test]
        public void IsBetterVersionOfPrefersDirectlySpecifiedToWildcard()
        {
            var first = MockExtension(fromWildcard: false);
            var second = MockExtension(fromWildcard: true);
            Assert.Multiple(() =>
            {
                Assert.That(first.IsBetterVersionOf(second), Is.True);
                Assert.That(second.IsBetterVersionOf(first), Is.False);
            });
        }

        [Test]
        public void IsBetterVersionOfPrefersNoChangeIfFromWildcard()
        {
            var first = MockExtension(fromWildcard: true);
            var second = MockExtension(fromWildcard: true);
            Assert.Multiple(() =>
            {
                Assert.That(first.IsBetterVersionOf(second), Is.False);
                Assert.That(second.IsBetterVersionOf(first), Is.False);
            });
        }

        private static IExtensionAssembly MockExtension(string assemblyName = "ExtensionSelectorTestsExtension",
            Version assemblyVersion = null,
            Version targetFramework = null,
            bool fromWildcard = false)
        {
            var sub = Substitute.For<IExtensionAssembly>();
            sub.AssemblyName.Returns(assemblyName);
            sub.AssemblyVersion.Returns(assemblyVersion ?? new Version(1, 0));
            targetFramework = targetFramework ?? new Version(2, 0);
            sub.TargetFramework.Returns(new RuntimeFramework(RuntimeType.Any, targetFramework));
            sub.FromWildCard.Returns(fromWildcard);
            return sub;
        }
    }
}
#endif