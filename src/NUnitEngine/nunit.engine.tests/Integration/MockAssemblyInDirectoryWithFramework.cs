#if !NETCOREAPP1_1
using System;
using System.IO;
using NUnit.Framework;

namespace NUnit.Engine.Tests.Integration
{
    internal sealed class MockAssemblyInDirectoryWithFramework : IDisposable
    {
        private readonly DirectoryWithNeededAssemblies directory;

        public string MockAssemblyDll => Path.Combine(directory.Directory, "mock-assembly.dll");

        public MockAssemblyInDirectoryWithFramework()
        {
            directory = new DirectoryWithNeededAssemblies("mock-assembly");

            Assert.That(Path.Combine(directory.Directory, "nunit.framework.dll"), Does.Exist, "This test must be run with nunit.framework.dll in the same directory as the mock assembly.");
        }

        public void Dispose()
        {
            directory.Dispose();
        }
    }
}
#endif