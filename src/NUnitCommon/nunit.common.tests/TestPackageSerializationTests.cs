// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using Org.XmlUnit.Constraints;

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

            // Add settings intended for top level and both subpackages.
            // All these settings are non-standard, defined by the user.
            TEST_PACKAGE.AddSetting("foo", "bar");
            TEST_PACKAGE.AddSetting("num", 42);
            TEST_PACKAGE.AddSetting("critical", true);

            // Add a setting to the first subpackage only
            SUBPACKAGE1.AddSetting("cpu", "x86");

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
            Assert.That(xml, CompareConstraint.IsIdenticalTo(EXPECTED_XML));
        }

        [Test]
        public void TestPackageFromXml()
        {
            var package = PackageHelper.FromXml(EXPECTED_XML);
            ComparePackages(package, TEST_PACKAGE);
        }

        [Test]
        public void TestPackageFromXml_BadRootElementThrowsInvalidOperationException()
        {
            Assert.That(() => PackageHelper.FromXml("<Junk>Not a TestPackage</Junk>"), Throws.InvalidOperationException);
        }

        [Test]
        public void TestPackageFromXml_XmlDeclarationIsIgnored()
        {
            var package = PackageHelper.FromXml(EXPECTED_XML_WITH_XML_DECLARATION);
            ComparePackages(package, TEST_PACKAGE);
        }

        [Test]
        public void TestPackageRoundTrip()
        {
            var xml = TEST_PACKAGE.ToXml();
            Assert.That(xml, CompareConstraint.IsIdenticalTo(EXPECTED_XML));
            var package = PackageHelper.FromXml(xml);
            ComparePackages(package, TEST_PACKAGE);
        }

        [TestCaseSource(nameof(Data))]
        public void StandardSettingsRoundTrip<T>(SettingDefinition<T> definition, T value, string? xmlRepresentation)
            where T : notnull
        {
            if (xmlRepresentation == "NYI")
            {
                Assert.Warn($"Setting {definition.Name} is not yet implemented.");
                return;
            }

            var setting = definition.WithValue(value);

            // Verify That the setting is created correctly
            Assert.That(setting.Value, Is.EqualTo(value));
            Assert.That(setting.Value, Is.AssignableTo(definition.ValueType));

            var oldPackage = new TestPackage("test.dll").SubPackages[0];
            oldPackage.AddSetting(setting);
            var xml = oldPackage.ToXml();

            // Check the XML we just created is what we expect
            var attrValue = xmlRepresentation ?? value.ToString();
            Assert.That(xml, Is.EqualTo(
                $"<TestPackage id=\"{oldPackage.ID}\" fullname=\"{oldPackage.FullName}\">" +
                $"<Settings {setting.Name}=\"{attrValue}\" /></TestPackage>"));

            // Create a new package from the XML and check it's the same
            var newPackage = PackageHelper.FromXml(xml);
            ComparePackages(newPackage, oldPackage);
        }

        private static void ComparePackages(TestPackage newPackage, TestPackage oldPackage)
        {
            Assert.Multiple(() =>
            {
                Assert.That(newPackage.Name, Is.EqualTo(oldPackage.Name));
                Assert.That(newPackage.FullName, Is.EqualTo(oldPackage.FullName));
                Assert.That(newPackage.Settings.Count, Is.EqualTo(oldPackage.Settings.Count));
                Assert.That(newPackage.SubPackages.Count, Is.EqualTo(oldPackage.SubPackages.Count));

                foreach (var setting in oldPackage.Settings)
                {
                    var key = setting.Name;
                    Assert.That(newPackage.Settings.HasSetting(key));
                    var oldValue = oldPackage.Settings.GetSetting(key);
                    var newValue = newPackage.Settings.GetSetting(key);

                    Assert.That(newValue, Is.EqualTo(oldValue));
                }

                for (int i = 0; i < oldPackage.SubPackages.Count; i++)
                    ComparePackages(newPackage.SubPackages[i], oldPackage.SubPackages[i]);
            });
        }

        private static IEnumerable<TestCaseData> Data
        {
            get
            {
                yield return new TestCaseData(SettingDefinitions.ActiveConfig, "CONFIG", null).SetName("{m}_ActiveConfig");
                yield return new TestCaseData(SettingDefinitions.AutoBinPath, true, null).SetName("{m}_AutoBinPath");
                yield return new TestCaseData(SettingDefinitions.BasePath, "BASE", null).SetName("{m}_BasePath");
                yield return new TestCaseData(SettingDefinitions.ConfigurationFile, "FILE", null).SetName("{m}_ConfigurationFile");
                yield return new TestCaseData(SettingDefinitions.DebugTests, true, null).SetName("{m}_DebugTests");
                yield return new TestCaseData(SettingDefinitions.DebugAgent, true, null).SetName("{m}_DebugAgent");
                yield return new TestCaseData(SettingDefinitions.PrivateBinPath, "PRIVATE", null).SetName("{m}_PrivateBinPath");
                yield return new TestCaseData(SettingDefinitions.MaxAgents, 5, null).SetName("{m}_MaxAgents");
                yield return new TestCaseData(SettingDefinitions.RequestedRuntimeFramework, "RUNTIME", null).SetName("{m}_RequestedRuntimeFramework");
                yield return new TestCaseData(SettingDefinitions.RequestedFrameworkName, "FRAMEWORK", null).SetName("{m}_RequestedFrameworkName");
                yield return new TestCaseData(SettingDefinitions.TargetFrameworkName, "TARGET", null).SetName("{m}_TargetFrameworkName");
                yield return new TestCaseData(SettingDefinitions.RequestedAgentName, "AGENT", null).SetName("{m}_RequestedAgentName");
                yield return new TestCaseData(SettingDefinitions.SelectedAgentName, "AGENT", null).SetName("{m}_SelectedAgentName");
                yield return new TestCaseData(SettingDefinitions.RunAsX86, true, null).SetName("{m}_RunAsX86");
                yield return new TestCaseData(SettingDefinitions.DisposeRunners, true, null).SetName("{m}_DisposeRunners");
                yield return new TestCaseData(SettingDefinitions.ShadowCopyFiles, true, null).SetName("{m}_ShadowCopyFiles");
                yield return new TestCaseData(SettingDefinitions.LoadUserProfile, true, null).SetName("{m}_LoadUserProfile");
                yield return new TestCaseData(SettingDefinitions.SkipNonTestAssemblies, true, null).SetName("{m}_SkipNonTestAssemblies");
                yield return new TestCaseData(SettingDefinitions.PauseBeforeRun, true, null).SetName("{m}_PauseBeforeRun");
                yield return new TestCaseData(SettingDefinitions.InternalTraceLevel, "Info", null).SetName("{m}_InternalTraceLevel");
                yield return new TestCaseData(SettingDefinitions.WorkDirectory, "WORK", null).SetName("{m}_WorkDirectory");
                yield return new TestCaseData(SettingDefinitions.ImageRuntimeVersion, "1.2.3", null).SetName("{m}_ImageRuntimeVersion");
                yield return new TestCaseData(SettingDefinitions.ImageRequiresX86, true, null).SetName("{m}_ImageRequiresX86");
                yield return new TestCaseData(SettingDefinitions.DisposeRunners, true, null).SetName("{m}_DisposeRunners");
                yield return new TestCaseData(SettingDefinitions.ImageTargetFrameworkName, "TARGET", null).SetName("{m}_ImageTargetFrameworkName");
                yield return new TestCaseData(SettingDefinitions.DefaultTimeout, 5000, null).SetName("{m}_DefaultTimeout");
                yield return new TestCaseData(SettingDefinitions.DefaultCulture, "EN-us", null).SetName("{m}_DefaultCulture");
                yield return new TestCaseData(SettingDefinitions.DefaultUICulture, "FR-fr", null).SetName("{m}_DefaultUICulture");
                StringWriter writer = new StringWriter();
                yield return new TestCaseData(SettingDefinitions.InternalTraceWriter, writer, "NYI").SetName("{m}_InternalTraceWriter");
                var tests = new[] { "test1", "test2", "test3" };
                yield return new TestCaseData(SettingDefinitions.LOAD, tests, "test1;test2;test3").SetName("{m}_LOAD");
                yield return new TestCaseData(SettingDefinitions.NumberOfTestWorkers, 42, null).SetName("{m}_NumberOfTestWorkers");
                yield return new TestCaseData(SettingDefinitions.RandomSeed, 123456789, null).SetName("{m}_RandomSeed");
                yield return new TestCaseData(SettingDefinitions.StopOnError, true, null).SetName("{m}_StopOnError");
                yield return new TestCaseData(SettingDefinitions.ThrowOnEachFailureUnderDebugger, true, null).SetName("{m}_ThrowOnEachFailureUnderDebugger");
                yield return new TestCaseData(SettingDefinitions.SynchronousEvents, true, null).SetName("{m}_SynchronousEvents");
                yield return new TestCaseData(SettingDefinitions.TestParameters, "X=7;Y=5", null).SetName("{m}_TestParameters");
                yield return new TestCaseData(SettingDefinitions.RunOnMainThread, true, null).SetName("{m}_RunOnMainThread");
                var dict = new Dictionary<string, string>();
                dict.Add("X", "7");
                dict.Add("Y", "5");
                yield return new TestCaseData(SettingDefinitions.TestParametersDictionary, dict,
                    "&lt;parms>&lt;parm key='X' value='7' />&lt;parm key='Y' value='5' />&lt;/parms>").SetName("{m}_TestParametersDictionary");
            }
        }
    }
}
