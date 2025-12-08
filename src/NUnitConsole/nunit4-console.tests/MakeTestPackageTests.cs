// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using System.IO;
using NUnit.Framework;
using NUnit.ConsoleRunner.Options;
using System.Linq;
using NUnit.Engine;
using NUnit.Common;

namespace NUnit.ConsoleRunner
{
    public class MakeTestPackageTests
    {
        [Test]
        public void SingleAssembly()
        {
            var options = ConsoleMocks.Options("test.dll");
            var package = ConsoleRunner.MakeTestPackage(options);

            Assert.That(package.SubPackages.Count, Is.EqualTo(1));
            Assert.That(package.SubPackages[0].FullName, Is.EqualTo(Path.GetFullPath("test.dll")));
        }

        [Test]
        public void MultipleAssemblies()
        {
            var names = new string[] { "test1.dll", "test2.dll", "test3.dll" };
            var options = ConsoleMocks.Options(names);
            var package = ConsoleRunner.MakeTestPackage(options);

            Assert.That(package.SubPackages.Count, Is.EqualTo(3));
            Assert.That(package.SubPackages[0].FullName, Is.EqualTo(Path.GetFullPath("test1.dll")));
            Assert.That(package.SubPackages[1].FullName, Is.EqualTo(Path.GetFullPath("test2.dll")));
            Assert.That(package.SubPackages[2].FullName, Is.EqualTo(Path.GetFullPath("test3.dll")));
        }

        [TestCase("--testRunTimeout=200000", "TestRunTimeout", 200000)]
        [TestCase("--testCaseTimeout=50", FrameworkPackageSettings.DefaultTimeout, 50)]
        [TestCase("--dispose-runners", "DisposeRunners", true)]
        [TestCase("--config=Release", "ActiveConfig", "Release")]
        [TestCase("--trace=Error", FrameworkPackageSettings.InternalTraceLevel, "Error")]
        [TestCase("--trace=error", FrameworkPackageSettings.InternalTraceLevel, "Error")]
        [TestCase("--seed=1234", FrameworkPackageSettings.RandomSeed, 1234)]
        [TestCase("--workers=3", FrameworkPackageSettings.NumberOfTestWorkers, 3)]
        [TestCase("--workers=0", FrameworkPackageSettings.NumberOfTestWorkers, 0)]
        [TestCase("--skipnontestassemblies", "SkipNonTestAssemblies", true)]
#if NETCOREAPP
        [TestCase("--list-resolution-stats", "ListResolutionStats", true)]
#else
        [TestCase("--x86", "RunAsX86", true)]
        [TestCase("--shadowcopy", "ShadowCopyFiles", true)]
        [TestCase("--framework=net-4.6.2", "RequestedRuntimeFramework", "net-4.6.2")]
        [TestCase("--configfile=mytest.config", "ConfigurationFile", "mytest.config")]
        [TestCase("--agents=5", "MaxAgents", 5)]
        [TestCase("--debug", FrameworkPackageSettings.DebugTests, true)]
        [TestCase("--pause", FrameworkPackageSettings.PauseBeforeRun, true)]
        [TestCase("--set-principal-policy:UnauthenticatedPrincipal", "PrincipalPolicy", "UnauthenticatedPrincipal")]
#if DEBUG
        [TestCase("--debug-agent", "DebugAgent", true)]
#endif
#endif
        public void WhenOptionIsSpecified_PackageIncludesSetting(string option, string key, object val)
        {
            var options = ConsoleMocks.Options("test.dll", option);
            var package = ConsoleRunner.MakeTestPackage(options);

            Assert.That(package.Settings.HasSetting(key), $"Setting not included for {options}", option);
            Assert.That(package.Settings.GetSetting(key), Is.EqualTo(val), $"{key} not set correctly for {option}");
        }

        [Test]
        public void TestRunParametersAreIncludedInSettings()
        {
            var options = ConsoleMocks.Options("test.dll", "--param:X=5", "--param:Y=7");
            var settings = ConsoleRunner.MakeTestPackage(options).Settings;

            // Both the newer dictionary setting and the legacy string representation should be included
            var dictionarySetting = SettingDefinitions.TestParametersDictionary;
            var legacySetting = SettingDefinitions.TestParameters;

            Assert.That(settings.HasSetting(dictionarySetting), $"{dictionarySetting.Name} setting not included.");
            var paramDictionary = settings.GetValueOrDefault(dictionarySetting);

            string[] expectedKeys = { "X", "Y" };
            Assert.That(paramDictionary.Keys, Is.EqualTo(expectedKeys));
            Assert.That(paramDictionary["X"], Is.EqualTo("5"));
            Assert.That(paramDictionary["Y"], Is.EqualTo("7"));

            Assert.That(settings.HasSetting(legacySetting), $"{legacySetting.Name} setting not included.");
            Assert.That(settings.GetValueOrDefault(legacySetting), Is.EqualTo("X=5;Y=7"));
        }

#if NETFRAMEWORK
        [Test]
        public void WhenDebugging_NumberOfTestWorkersDefaultsToZero()
        {
            var options = ConsoleMocks.Options("test.dll", "--debug");
            var package = ConsoleRunner.MakeTestPackage(options);

            Assert.That(package.Settings.GetValueOrDefault(SettingDefinitions.DebugTests), Is.EqualTo(true));
            Assert.That(package.Settings.GetValueOrDefault(SettingDefinitions.NumberOfTestWorkers), Is.EqualTo(0));
        }

        [Test]
        public void WhenDebugging_NumberOfTestWorkersMayBeOverridden()
        {
            var options = ConsoleMocks.Options("test.dll", "--debug", "--workers=3");
            var package = ConsoleRunner.MakeTestPackage(options);

            Assert.That(package.Settings.GetValueOrDefault(SettingDefinitions.DebugTests), Is.EqualTo(true));
            Assert.That(package.Settings.GetValueOrDefault(SettingDefinitions.NumberOfTestWorkers), Is.EqualTo(3));
        }
#endif

        [Test]
        public void WhenNoOptionsAreSpecified_PackageContainsOnlyTwoSettings()
        {
            var options = ConsoleMocks.Options("test.dll");
            var package = ConsoleRunner.MakeTestPackage(options);

            string[] expected = new string[] { FrameworkPackageSettings.WorkDirectory, "DisposeRunners" };
            Assert.That(package.Settings.Select(s => s.Name), Is.EquivalentTo(expected));
        }
    }
}
