// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using System.Collections.Generic;
using NUnit.Engine.Runners;
using NUnit.Framework;

namespace NUnit.Engine.Services.TestRunnerFactoryTests
{
    internal class TestRunnerFactoryData : TestCaseData
    {
        public TestRunnerFactoryData(string testName, TestPackage package, RunnerResult result)
            : base(package, result)
        {
            SetName($"{{m}}({testName})");
        }

        public static IEnumerable<TestRunnerFactoryData> TestCases
        {
            get
            {
#if NETFRAMEWORK
                yield return new TestRunnerFactoryData(
                    "SingleAssembly",
                    new TestPackage("a.dll"),
                    RunnerResult.ProcessRunner);

                yield return new TestRunnerFactoryData(
                    "SingleUnknown",
                    new TestPackage("a.junk"),
                    RunnerResult.ProcessRunner);

                yield return new TestRunnerFactoryData(
                    "TwoAssemblies",
                    new TestPackage("a.dll", "b.dll"),
                    RunnerResult.MultipleTestProcessRunner(2));

                yield return new TestRunnerFactoryData(
                    "TwoUnknowns",
                    new TestPackage("a.junk", "b.junk"),
                    RunnerResult.MultipleTestProcessRunner(2));

                yield return new TestRunnerFactoryData(
                    "OneProject",
                    new TestPackage("xy.nunit"),
                    RunnerResult.AggregatingTestRunner(2));

                yield return new TestRunnerFactoryData(
                    "TwoProjects",
                    new TestPackage("xy.nunit", "xy.nunit"),
                    RunnerResult.AggregatingTestRunner(4));

                yield return new TestRunnerFactoryData(
                    "OneProjectOneAssembly",
                    new TestPackage("xy.nunit", "c.dll"),
                    RunnerResult.AggregatingTestRunner(RunnerResult.ProcessRunner, 3));

                yield return new TestRunnerFactoryData(
                    "TwoProjectsOneAssembly",
                    new TestPackage("xy.nunit", "xy.nunit", "x.dll"),
                    RunnerResult.AggregatingTestRunner(5));

                yield return new TestRunnerFactoryData(
                    "TwoAssembliesOneProject",
                    new TestPackage("x.dll", "y.dll", "xy.nunit"),
                    RunnerResult.AggregatingTestRunner(4));

                yield return new TestRunnerFactoryData(
                    "OneUnknownOneAssemblyOneProject",
                    new TestPackage("a.junk", "a.dll", "xy.nunit"),
                    RunnerResult.AggregatingTestRunner(4));
#else
                yield return new TestRunnerFactoryData(
                    "SingleAssembly",
                    new TestPackage("a.dll"),
                    RunnerResult.LocalTestRunner);

                yield return new TestRunnerFactoryData(
                    "Two assemblies",
                    new TestPackage("a.dll", "b.dll"),
                    RunnerResult.AggregatingTestRunner(2));

                yield return new TestRunnerFactoryData(
                    "SingleProject",
                    new TestPackage("xy.nunit"),
                    RunnerResult.AggregatingTestRunner(2));

                yield return new TestRunnerFactoryData(
                    "Single unknown extension",
                    new TestPackage("a.junk"),
                    RunnerResult.LocalTestRunner);

                yield return new TestRunnerFactoryData(
                    "Two projects",
                    new TestPackage("xy.nunit", "z.nunit"),
                    RunnerResult.AggregatingTestRunner(3));

                yield return new TestRunnerFactoryData(
                    "One project, one assembly",
                    new TestPackage("xy.nunit", "a.dll"),
                    RunnerResult.AggregatingTestRunner(3));

                yield return new TestRunnerFactoryData(
                    "Two projects, one assembly",
                    new TestPackage("xy.nunit", "z.nunit", "a.dll"),
                    RunnerResult.AggregatingTestRunner(4));

                yield return new TestRunnerFactoryData(
                    "Two assemblies, one project",
                    new TestPackage("a.dll", "b.dll", "xy.nunit"),
                    RunnerResult.AggregatingTestRunner(4));

                yield return new TestRunnerFactoryData(
                    "Two unknown extensions",
                    new TestPackage("a.junk", "b.junk"),
                    RunnerResult.AggregatingTestRunner(2));

                yield return new TestRunnerFactoryData(
                    "One assembly, one project, one unknown",
                    new TestPackage("a.junk", "a.dll", "xy.nunit"),
                    RunnerResult.AggregatingTestRunner(4));
#endif
            }
        }
    }
}