// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using System;
using System.IO;
using System.Runtime.Versioning;
using NUnit.Framework;

namespace NUnit.Engine.Extensibility
{
    // TODO: This should actually give us 3.5
    [TestFixture("net462", FrameworkIdentifiers.NetFramework, "4.6.2")]
    [TestFixture("netcoreapp3.1", FrameworkIdentifiers.NetCoreApp, "3.1")]
    [TestFixture("net6.0", FrameworkIdentifiers.NetCoreApp, "6.0")]
    public class ExtensionAssemblyTests
    {
        private string _assemblyPath;
        private string _assemblyFileName;
        private FrameworkName _expectedTargetRuntime;
        private ExtensionAssembly _ea;

        public ExtensionAssemblyTests(string runtimeDir, string expectedRuntime, string expectedVersion)
        {
            _assemblyPath = TestData.MockAssemblyPath(runtimeDir);
            _assemblyFileName = Path.GetFileNameWithoutExtension(_assemblyPath);
            _expectedTargetRuntime = new FrameworkName(expectedRuntime, new Version(expectedVersion));
        }

        [OneTimeSetUp]
        public void CreateExtensionAssemblies()
        {
            _ea = new ExtensionAssembly(_assemblyPath, false);
        }

        [Test]
        public void AssemblyName()
        {
            Assert.That(_ea.AssemblyName, Is.EqualTo(_assemblyFileName));
        }

        [Test]
        public void TargetFramework()
        {
            Assert.That(_ea.TargetRuntime, Is.EqualTo(_expectedTargetRuntime));
        }
    }
}
