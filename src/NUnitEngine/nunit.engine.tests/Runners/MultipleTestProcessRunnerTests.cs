// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

#if NETFRAMEWORK
using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace NUnit.Engine.Runners.Tests
{
    public class MultipleTestProcessRunnerTests
    {
        const int ASSUMED_PROCESSOR_COUNT = 8;

        [TestCase(1, null, 1)]
        [TestCase(1, 1, 1)]
        // Temporarily disabled - fails on a machine with less than 3 cores
        //[TestCase(3, null, 3)]
        [TestCase(3, 1, 1)]
        [TestCase(3, 2, 2)]
        [TestCase(3, 3, 3)]
        [TestCase(20, 8, 8)]
        [TestCase(8, 20, 8)]
        public void CheckLevelOfParallelism_ListOfAssemblies(int assemblyCount, int? maxAgents, int expected)
        {
            if (maxAgents == null)
                expected = Math.Min(assemblyCount, ASSUMED_PROCESSOR_COUNT);

            var assemblies = new List<string>();
            for (int i = 1; i <= assemblyCount; i++)
                assemblies.Add($"test{i}.dll");
            var package = new TestPackage(assemblies);
            if (maxAgents != null)
                package.Settings[EnginePackageSettings.MaxAgents] = maxAgents;
            var runner = new MultipleTestProcessRunner(new ServiceContext(), package);
            Assert.That(runner.LevelOfParallelism, Is.EqualTo(expected));
        }

        [Test]
        public void CheckLevelOfParallelism_SingleAssembly()
        {
            var package = new TestPackage(new string[] { "junk.dll" });
            Assert.That(new MultipleTestProcessRunner(new ServiceContext(), package).LevelOfParallelism, Is.EqualTo(1));
        }

        [TestCase(1, null, 1)]
        [TestCase(1, 1, 1)]
        // Temporarily disabled - fails on a machine with less than 3 cores
        //[TestCase(3, null, 3)]
        [TestCase(3, 1, 1)]
        [TestCase(3, 2, 2)]
        [TestCase(3, 3, 3)]
        [TestCase(20, 8, 8)]
        [TestCase(8, 20, 8)]
        public void CheckLevelOfParallelism_ProjectWithAssemblies(int assemblyCount, int? maxAgents, int expected)
        {
            var package = new TestPackage(new string[] { "proj.nunit" });
            for (int i = 1; i <= assemblyCount; i++)
                package.SubPackages[0].AddSubPackage(new TestPackage($"test{i}.dll"));
            if (maxAgents != null)
                package.Settings[EnginePackageSettings.MaxAgents] = maxAgents;
            var runner = new MultipleTestProcessRunner(new ServiceContext(), package);
            Assert.That(runner.LevelOfParallelism, Is.EqualTo(expected));
        }
    }
}
#endif