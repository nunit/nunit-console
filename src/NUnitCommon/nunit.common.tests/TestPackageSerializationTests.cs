// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using System;
using System.IO;
using NUnit.Framework;

namespace NUnit.Engine.Internal
{
    public class TestPackageSerializationTests
    {
        private const string CR = "\r";
        private const string LF = "\n";

        private static readonly TestPackage TEST_PACKAGE;
        private static readonly TestPackage SUBPACKAGE1;
        private static readonly TestPackage SUBPACKAGE2;

        private static readonly string EXPECTED_XML;
        private static readonly string EXPECTED_XML_WITH_XML_DECLARATION;

        static TestPackageSerializationTests()
        {
            // Create top-level TestPackage
            TEST_PACKAGE = new TestPackage(new string[] { "test1.dll", "test2.dll" });
            SUBPACKAGE1 = TEST_PACKAGE.SubPackages[0];
            SUBPACKAGE2 = TEST_PACKAGE.SubPackages[1];

            // Add settings intended for top level and both subpackages
            TEST_PACKAGE.AddSetting(new PackageSetting<string>("foo", "bar"));
            TEST_PACKAGE.AddSetting(new PackageSetting<int>("num", 42));
            TEST_PACKAGE.AddSetting(new PackageSetting<bool>("critical", true));

            // Add a setting to the first subpackage only
            SUBPACKAGE1.AddSetting(new PackageSetting<string>("cpu", "x86"));

            // Multi-line for ease of editing only
            EXPECTED_XML = $"""
                <TestPackage id="{TEST_PACKAGE.ID}">
                <Settings foo="bar" num="42" critical="True" />
                <TestPackage id="{SUBPACKAGE1.ID}" fullname="{Path.GetFullPath("test1.dll")}">
                <Settings foo="bar" num="42" critical="True" cpu="x86" /></TestPackage>
                <TestPackage id="{SUBPACKAGE2.ID}" fullname="{Path.GetFullPath("test2.dll")}">
                <Settings foo="bar" num="42" critical="True" /></TestPackage></TestPackage>
                """.Replace(CR, string.Empty).Replace(LF, string.Empty);

            EXPECTED_XML_WITH_XML_DECLARATION = $"""<?xml version = "1.0" encoding="utf-16"?>{EXPECTED_XML}""";
        }

        [Test]
        public void TestPackageToXml()
        {
            var xml = TEST_PACKAGE.ToXml();
            Console.WriteLine(xml);
            Assert.That(xml, Is.EqualTo(EXPECTED_XML));
        }

        [Test]
        public void TestPackageFromXml()
        {
            var package = new TestPackage().FromXml(EXPECTED_XML);
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
            var package = new TestPackage().FromXml(EXPECTED_XML_WITH_XML_DECLARATION);
            Console.WriteLine(package.ToXml());
            ComparePackages(package, TEST_PACKAGE);
        }

        [Test]
        public void TestPackageRoundTrip()
        {
            var xml = TEST_PACKAGE.ToXml();
            Console.WriteLine(xml);
            Assert.That(xml, Is.EqualTo(EXPECTED_XML));
            var package = new TestPackage().FromXml(xml);
            ComparePackages(package, TEST_PACKAGE);
        }

        // TODO: Currently modified to only warn when the old and new values are not equal.
        private static void ComparePackages(TestPackage newPackage, TestPackage oldPackage)
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
                    var oldValue = oldPackage.Settings[key].Value;
                    var newValue = newPackage.Settings[key].Value;

                    // TODO: Reinstate after fixing issue #1677
                    //Assert.That(newValue, Is.EqualTo(oldValue));
                    if (oldValue != newValue)
                    {
                        Assert.That(newValue.ToString(), Is.EqualTo(oldValue.ToString()));
                        Warn.Unless(newValue, Is.EqualTo(oldValue), "Value was serialized as string and must be parsed.");
                    }
                }

                for (int i = 0; i < oldPackage.SubPackages.Count; i++)
                    ComparePackages(newPackage.SubPackages[i], oldPackage.SubPackages[i]);
            });
        }
    }
}
