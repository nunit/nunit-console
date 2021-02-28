// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using System.IO;
using NUnit.Framework;

namespace NUnit.Engine.Api.Tests
{
    public class TestPackageTests_SingleAssembly
    {
        private TestPackage package;

        [SetUp]
        public void CreatePackage()
        {
            package = new TestPackage("test.dll");
        }

        [Test]
        public void PackageIDsAreUnique()
        {
            var another = new TestPackage("another.dll");
            Assert.That(another.ID, Is.Not.EqualTo(package.ID));
        }

        [Test]
        public void AssemblyPathIsUsedAsFilePath()
        {
            Assert.That(package.FullName, Is.EqualTo(Path.GetFullPath("test.dll")));
        }

        [Test]
        public void FileNameIsUsedAsPackageName()
        {
            Assert.That(package.Name, Is.EqualTo("test.dll"));
        }

        [Test]
        public void HasNoSubPackages()
        {
            Assert.That(package.SubPackages.Count, Is.EqualTo(0));
        }
    }

    public class TestPackageTests_MultipleAssemblies
    {
        private TestPackage package;

        [SetUp]
        public void CreatePackage()
        {
            package = new TestPackage(new string[] { "test1.dll", "test2.dll", "test3.dll" });
        }

        [Test]
        public void PackageIsAnonymous()
        {
            Assert.Null(package.FullName);
        }

        [Test]
        public void PackageContainsThreeSubpackages()
        {
            Assert.That(package.SubPackages.Count, Is.EqualTo(3));
            Assert.That(package.SubPackages[0].FullName, Is.EqualTo(Path.GetFullPath("test1.dll")));
            Assert.That(package.SubPackages[1].FullName, Is.EqualTo(Path.GetFullPath("test2.dll")));
            Assert.That(package.SubPackages[2].FullName, Is.EqualTo(Path.GetFullPath("test3.dll")));
        }
    }
}
