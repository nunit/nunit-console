// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using System;
using System.IO;
using NUnit.Framework;

namespace NUnit.Engine.Internal
{
    public class TestPackageSerializationTests
    {
        private static readonly string ASSEMBLY_1 = Path.GetFullPath("mock-assembly.dll");
        private static readonly string ASSEMBLY_2 = Path.GetFullPath("notest-assembly.dll");
        private static readonly TestPackage TEST_PACKAGE = new TestPackage(new string[] { "mock-assembly.dll", "notest-assembly.dll" });
        private static readonly string TEST_PACKAGE_XML =
            "<TestPackage id=\"0\"><Settings foo=\"bar\" />" +
            $"<TestPackage id=\"1\" fullname=\"{ASSEMBLY_1}\"><Settings foo=\"bar\" /></TestPackage>" +
            $"<TestPackage id=\"2\" fullname=\"{ASSEMBLY_2}\"><Settings foo=\"bar\" /></TestPackage>" +
            "</TestPackage>";
        private static readonly string TEST_PACKAGE_XML_WITH_XML_DECLARATION = "<?xml version = \"1.0\" encoding=\"utf-16\"?>" + TEST_PACKAGE_XML;

        static TestPackageSerializationTests() 
        {
            TEST_PACKAGE.AddSetting("foo", "bar");
        }

        [Test]
        public void TestPackageToXml()
        {
            var xml = TEST_PACKAGE.ToXml();
            Console.WriteLine(xml);
            Assert.That(xml, Is.EqualTo(TEST_PACKAGE_XML));
        }

        [Test]
        public void TestPackageFromXml()
        {
            var package = new TestPackage().FromXml(TEST_PACKAGE_XML);
            Console.WriteLine(package.ToXml());
            ComparePackages(package, TEST_PACKAGE);
        }

        [Test]
        public void TestPackageFromXml_BadRootElementThrowsInvalidOperationException()
        {
            Assert.That(() => new TestPackage().FromXml("<Junk>Not a TestPackage</Junk>"), Throws.InvalidOperationException);
        }

        [Test]
        public void TestPackageFromXml_XmlDeclarationIsIgnored()
        {
            var package = new TestPackage().FromXml(TEST_PACKAGE_XML_WITH_XML_DECLARATION);
            Console.WriteLine(package.ToXml());
            ComparePackages(package, TEST_PACKAGE);
        }

        [Test]
        public void TestPackageRoundTrip()
        {
            var xml = TEST_PACKAGE.ToXml();
            Console.WriteLine(xml);
            var package = new TestPackage().FromXml(xml);
            ComparePackages(package, TEST_PACKAGE);
        }

        private void ComparePackages(TestPackage newPackage, TestPackage oldPackage)
        {
            Assert.Multiple(() =>
            {
                Assert.That(newPackage.Name, Is.EqualTo(oldPackage.Name));
                Assert.That(newPackage.FullName, Is.EqualTo(oldPackage.FullName));
                Assert.That(newPackage.Settings.Count, Is.EqualTo(oldPackage.Settings.Count));
                Assert.That(newPackage.SubPackages.Count, Is.EqualTo(oldPackage.SubPackages.Count));

                foreach (var key in oldPackage.Settings.Keys)
                {
                    Assert.That(newPackage.Settings.ContainsKey(key));
                    Assert.That(newPackage.Settings[key], Is.EqualTo(oldPackage.Settings[key]));
                }

                for (int i = 0; i < oldPackage.SubPackages.Count; i++)
                    ComparePackages(newPackage.SubPackages[i], oldPackage.SubPackages[i]);
            });
        }
    }
}
