// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using System;
using System.Reflection;
using NUnit.Framework;
using NUnit.Framework.Internal;

namespace NUnit.Extensibility
{
    public class ExtensionAssemblyTests
    {
        private static readonly Assembly THIS_ASSEMBLY = Assembly.GetExecutingAssembly();
        private static readonly string THIS_ASSEMBLY_PATH = THIS_ASSEMBLY.Location;
        private static readonly string THIS_ASSEMBLY_NAME = THIS_ASSEMBLY.GetName().Name.ShouldNotBeNull();
        private static readonly Version THIS_ASSEMBLY_VERSION = THIS_ASSEMBLY.GetName().Version.ShouldNotBeNull();

        private ExtensionAssembly _ea;

        [OneTimeSetUp]
        public void CreateExtensionAssemblies()
        {
            _ea = new ExtensionAssembly(THIS_ASSEMBLY_PATH, false);
        }

        [OneTimeTearDown]
        public void DisposeExtensionAssemblies()
        {
            _ea.Dispose();
        }

        [Test]
        public void AssemblyName()
        {
            Assert.That(_ea.AssemblyName, Is.EqualTo(THIS_ASSEMBLY_NAME));
        }

        [Test]
        public void AssemblyVersion()
        {
            Assert.That(_ea.AssemblyVersion, Is.EqualTo(THIS_ASSEMBLY_VERSION));
        }

#if NET462
        [Test]
        public void FrameworkName()
        {
            Assert.Multiple(() =>
            {
                Assert.That(_ea.FrameworkName.Identifier, Is.EqualTo(".NETFramework"));
                Assert.That(_ea.FrameworkName.Version, Is.EqualTo(new Version(4,6,2)));
            });
        }
#endif
    }
}
