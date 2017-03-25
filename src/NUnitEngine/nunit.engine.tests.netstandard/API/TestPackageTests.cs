// ***********************************************************************
// Copyright (c) 2017 Charlie Poole
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

using NUnit.Framework;

namespace NUnit.Engine.Api.Tests
{
    public class TestPackageTests_SingleAssembly
    {
        private TestPackage package;

        [SetUp]
        public void CreatePackage()
        {
            package = new TestPackage(@"c:\test.dll");
        }

        [Test]
        public void PackageIDsAreUnique()
        {
            var another = new TestPackage(@"c:\another.dll");
            Assert.That(another.ID, Is.Not.EqualTo(package.ID));
        }

        [Test]
        public void AssemblyPathIsUsedAsFilePath()
        {
            Assert.AreEqual(@"c:\test.dll", package.FullName);
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

        [Test]
        public void NonRootedPathThrows()
        {
            Assert.That(() => new TestPackage("test1.dll"), Throws.InstanceOf<NUnitEngineException>());
        }
    }

    public class TestPackageTests_MultipleAssemblies
    {
        private TestPackage package;

        [SetUp]
        public void CreatePackage()
        {
            package = new TestPackage(new string[] { @"c:\test1.dll", @"c:\test2.dll", @"c:\test3.dll" });
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
            Assert.That(package.SubPackages[0].FullName, Is.EqualTo(@"c:\test1.dll"));
            Assert.That(package.SubPackages[1].FullName, Is.EqualTo(@"c:\test2.dll"));
            Assert.That(package.SubPackages[2].FullName, Is.EqualTo(@"c:\test3.dll"));
        }
    }
}
