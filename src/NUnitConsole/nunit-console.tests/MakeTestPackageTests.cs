// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using System.IO;
using System.Collections.Generic;
using NUnit.Framework;
using NUnit.ConsoleRunner.Options;

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
            var names = new [] { "test1.dll", "test2.dll", "test3.dll" };
            var options = ConsoleMocks.Options(names);
            var package = ConsoleRunner.MakeTestPackage(options);

            Assert.That(package.SubPackages.Count, Is.EqualTo(3));
            Assert.That(package.SubPackages[0].FullName, Is.EqualTo(Path.GetFullPath("test1.dll")));
            Assert.That(package.SubPackages[1].FullName, Is.EqualTo(Path.GetFullPath("test2.dll")));
            Assert.That(package.SubPackages[2].FullName, Is.EqualTo(Path.GetFullPath("test3.dll")));
        }

        [TestCase("--testCaseTimeout=50", "DefaultTimeout", 50)]
        [TestCase("--dispose-runners", "DisposeRunners", true)]
        [TestCase("--config=Release", "ActiveConfig", "Release")]
        [TestCase("--trace=Error", "InternalTraceLevel", "Error")]
        [TestCase("--trace=error", "InternalTraceLevel", "Error")]
        [TestCase("--seed=1234", "RandomSeed", 1234)]
        [TestCase("--workers=3", "NumberOfTestWorkers", 3)]
        [TestCase("--workers=0", "NumberOfTestWorkers", 0)]
        [TestCase("--skipnontestassemblies", "SkipNonTestAssemblies", true)]
#if NET35
        [TestCase("--x86", "RunAsX86", true)]
        [TestCase("--shadowcopy", "ShadowCopyFiles", true)]
        [TestCase("--framework=net-4.0", "RequestedRuntimeFramework", "net-4.0")]
        [TestCase("--configfile=mytest.config", "ConfigurationFile", "mytest.config")]
        [TestCase("--agents=5", "MaxAgents", 5)]
        [TestCase("--debug", "DebugTests", true)]
        [TestCase("--pause", "PauseBeforeRun", true)]
        [TestCase("--set-principal-policy:UnauthenticatedPrincipal", "PrincipalPolicy", "UnauthenticatedPrincipal")]
#if DEBUG
        [TestCase("--debug-agent", "DebugAgent", true)]
#endif
#endif
        public void WhenOptionIsSpecified_PackageIncludesSetting(string option, string key, object val)
        {
            var options = ConsoleMocks.Options("test.dll", option);
            var package = ConsoleRunner.MakeTestPackage(options);

            Assert.That(package.Settings.ContainsKey(key), "Setting not included for {0}", option);
            Assert.That(package.Settings[key], Is.EqualTo(val), "NumberOfTestWorkers not set correctly for {0}", option);
        }

        [Test]
        public void TestRunParametersAreIncludedInSettings()
        {
            var options = ConsoleMocks.Options("test.dll", "--param:X=5", "--param:Y=7");
            var settings = ConsoleRunner.MakeTestPackage(options).Settings;

            Assert.That(settings.ContainsKey("TestParametersDictionary"), "TestParametersDictionary setting not included.");
            var paramDictionary = settings["TestParametersDictionary"] as IDictionary<string, string>;
            Assert.That(paramDictionary.Keys, Is.EqualTo(new[] { "X", "Y" }));
            Assert.That(paramDictionary["X"], Is.EqualTo("5"));
            Assert.That(paramDictionary["Y"], Is.EqualTo("7"));

            Assert.That(settings.ContainsKey("TestParameters"), "TestParameters setting not included.");
            var paramString = settings["TestParameters"] as string;
            Assert.That(paramString, Is.EqualTo("X=5;Y=7"));
        }

#if NET35
        [Test]
        public void WhenDebugging_NumberOfTestWorkersDefaultsToZero()
        {
            var options = ConsoleMocks.Options("test.dll", "--debug");
            var package = ConsoleRunner.MakeTestPackage(options);

            Assert.That(package.Settings["DebugTests"], Is.EqualTo(true));
            Assert.That(package.Settings["NumberOfTestWorkers"], Is.EqualTo(0));
        }

        [Test]
        public void WhenDebugging_NumberOfTestWorkersMayBeOverridden()
        {
            var options = ConsoleMocks.Options("test.dll", "--debug", "--workers=3");
            var package = ConsoleRunner.MakeTestPackage(options);

            Assert.That(package.Settings["DebugTests"], Is.EqualTo(true));
            Assert.That(package.Settings["NumberOfTestWorkers"], Is.EqualTo(3));
        }
#endif

        [Test]
        public void WhenNoOptionsAreSpecified_PackageContainsOnlyTwoSettings()
        {
            var options = ConsoleMocks.Options("test.dll");
            var package = ConsoleRunner.MakeTestPackage(options);

            Assert.That(package.Settings.Keys, Is.EquivalentTo(new string[] { "WorkDirectory", "DisposeRunners" }));
        }

    }
}
