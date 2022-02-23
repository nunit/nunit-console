// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using System.IO;
using NUnit.Framework;

namespace NUnit.Engine.Api
{
    [TestFixture("test.dll")]
    [TestFixture("test1.dll,test2.dll,test3.dll")]
    public class TestPackageTests
    {
        private string[] _fileNames;
        private TestPackage _package;

        public TestPackageTests(string fileNames)
        {
            _fileNames = fileNames.Split(new char[] { ',' });
            _package = new TestPackage(_fileNames);
        }

        [Test]
        public void PackageIDsAreUnique()
        {
            var another = new TestPackage(_fileNames);
            Assert.That(another.ID, Is.Not.EqualTo(_package.ID));
        }

        [Test]
        public void PackageIsAnonymous()
        {
            Assert.Null(_package.Name);
            Assert.Null(_package.FullName);
        }

        [Test]
        public void HasSubPackageForEachFile()
        {
            Assert.That(_package.SubPackages.Count, Is.EqualTo(_fileNames.Length));
            for (int i = 0; i < _fileNames.Length; i++)
            {
                TestPackage subPackage = _package.SubPackages[i];
                string fileName = _fileNames[i];

                Assert.That(subPackage.Name, Is.EqualTo(fileName));
                Assert.That(subPackage.FullName, Is.EqualTo(Path.GetFullPath(fileName)));
            }
        }

        [Test]
        public void SubPackagesHaveNoSubPackages()
        {
            foreach (TestPackage subPackage in _package.SubPackages)
                Assert.That(subPackage.SubPackages.Count, Is.EqualTo(0));
        }
    }
}
