// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using System;
using System.Reflection;
using NUnit.Engine.Extensibility;
using NUnit.Framework;

namespace NUnit.Engine.Tests.Extensibility
{
    public class ExtensionAssemblyTests
    {
        private static readonly Assembly THIS_ASSEMBLY = Assembly.GetExecutingAssembly();
        private static readonly string THIS_ASSEMBLY_PATH = THIS_ASSEMBLY.Location;
        private static readonly string THIS_ASSEMBLY_FULL_NAME = THIS_ASSEMBLY.GetName().FullName;
        private static readonly string THIS_ASSEMBLY_NAME = THIS_ASSEMBLY.GetName().Name;
        private static readonly Version THIS_ASSEMBLY_VERSION = THIS_ASSEMBLY.GetName().Version;

        private ExtensionAssembly _ea;

        [OneTimeSetUp]
        public void CreateExtensionAssemblies()
        {
            _ea = new ExtensionAssembly(THIS_ASSEMBLY_PATH, false);
        }

        [Test]
        public void AssemblyDefinition()
        {
            Assert.That(_ea.Assembly.FullName, Is.EqualTo(THIS_ASSEMBLY_FULL_NAME));
        }

        [Test]
        public void MainModule()
        {
            Assert.That(_ea.MainModule.Assembly.FullName, Is.EqualTo(THIS_ASSEMBLY_FULL_NAME));
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

#if NETFRAMEWORK
        [Test]
        public void TargetFramework()
        {
            Assert.Multiple(() =>
            {
                Assert.That(_ea.TargetFramework, Has.Property(nameof(RuntimeFramework.Runtime)).EqualTo(RuntimeType.Any));
                Assert.That(_ea.TargetFramework, Has.Property(nameof(RuntimeFramework.FrameworkVersion)).EqualTo(new Version(2, 0)));
            });
        }
#endif
    }
}
