﻿// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using System.IO;
using System.Collections.Generic;
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

        [TestCase("--testCaseTimeout=50", FrameworkPackageSettings.DefaultTimeout, 50)]
        [TestCase("--dispose-runners", "DisposeRunners", true)]
        [TestCase("--config=Release", "ActiveConfig", "Release")]
        [TestCase("--trace=Error", FrameworkPackageSettings.InternalTraceLevel, "Error")]
        [TestCase("--trace=error", FrameworkPackageSettings.InternalTraceLevel, "Error")]
        [TestCase("--seed=1234", FrameworkPackageSettings.RandomSeed, 1234)]
        [TestCase("--workers=3", FrameworkPackageSettings.NumberOfTestWorkers, 3)]
        [TestCase("--workers=0", FrameworkPackageSettings.NumberOfTestWorkers, 0)]
        [TestCase("--skipnontestassemblies", "SkipNonTestAssemblies", true)]
#if NETFRAMEWORK
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

            Assert.That(settings.TryGetSetting(SettingDefinitions.TestParametersDictionary,
                                               out IDictionary<string, string>? paramDictionary),
                        $"{SettingDefinitions.TestParametersDictionary.Name} setting not included.");
            Assert.That(paramDictionary, Is.Not.Null);
            string[] expectedKeys = new[] { "X", "Y" };
            Assert.That(paramDictionary.Keys, Is.EqualTo(expectedKeys));
            Assert.That(paramDictionary["X"], Is.EqualTo("5"));
            Assert.That(paramDictionary["Y"], Is.EqualTo("7"));

            Assert.That(settings.TryGetSetting(SettingDefinitions.TestParameters, out string? paramString),
                        $"{SettingDefinitions.TestParameters.Name} setting not included.");
            Assert.That(paramString, Is.EqualTo("X=5;Y=7"));
        }

#if NETFRAMEWORK
        [Test]
        public void WhenDebugging_NumberOfTestWorkersDefaultsToZero()
        {
            var options = ConsoleMocks.Options("test.dll", "--debug");
            var package = ConsoleRunner.MakeTestPackage(options);

            Assert.That(package.GetSetting(SettingDefinitions.DebugTests, false), Is.EqualTo(true));
            Assert.That(package.GetSetting(SettingDefinitions.NumberOfTestWorkers, 1), Is.EqualTo(0));
        }

        [Test]
        public void WhenDebugging_NumberOfTestWorkersMayBeOverridden()
        {
            var options = ConsoleMocks.Options("test.dll", "--debug", "--workers=3");
            var package = ConsoleRunner.MakeTestPackage(options);

            Assert.That(package.GetSetting(SettingDefinitions.DebugTests, false), Is.EqualTo(true));
            Assert.That(package.GetSetting(SettingDefinitions.NumberOfTestWorkers, 1), Is.EqualTo(3));
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
