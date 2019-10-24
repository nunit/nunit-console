// ***********************************************************************
// Copyright (c) 2016 Charlie Poole, Rob Prouse
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

#if !NETCOREAPP1_1
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

#if !NETCOREAPP2_1
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
#endif