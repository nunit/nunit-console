// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using System.Collections.Generic;
using NUnit.Engine.Runners;
using NUnit.Framework;

namespace NUnit.Engine.Tests.Services.TestRunnerFactoryTests
{
    internal class TestRunnerFactoryData : TestCaseData
    {
        public TestRunnerFactoryData(string testName, TestPackage package, RunnerResult result)
            : base(package, result)
        {
            SetName($"{{m}}({testName}");
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
                    new TestPackage("a.nunit"),
                    RunnerResult.AggregatingTestRunner(2));

                yield return new TestRunnerFactoryData(
                    "TwoProjects",
                    new TestPackage("a.nunit", "a.nunit"),
                    RunnerResult.AggregatingTestRunner(4));

                yield return new TestRunnerFactoryData(
                    "OneProjectOneAssembly",
                    new TestPackage("a.nunit", "c.dll"),
                    RunnerResult.AggregatingTestRunner(RunnerResult.ProcessRunner, 3));

                yield return new TestRunnerFactoryData(
                    "TwoProjectsOneAssembly",
                    new TestPackage("a.nunit", "a.nunit", "x.dll"),
                    RunnerResult.AggregatingTestRunner(5));

                yield return new TestRunnerFactoryData(
                    "TwoAssembliesOneProject",
                    new TestPackage("x.dll", "y.dll", "a.nunit"),
                    RunnerResult.AggregatingTestRunner(4));

                yield return new TestRunnerFactoryData(
                    "OneUnknownOneAssemblyOneProject",
                    new TestPackage("a.junk", "a.dll", "a.nunit"),
                    RunnerResult.AggregatingTestRunner(4));
#else
                yield return new TestRunnerFactoryData(
                    "SingleAssembly",
                    new TestPackage("a.dll"),
                    RunnerResult.LocalTestRunner
                );

                yield return new TestRunnerFactoryData(
                    "Two assemblies",
                    new TestPackage("a.dll", "b.dll"),
                    new RunnerResult
                    {
                        TestRunner = typeof(AggregatingTestRunner),
                        SubRunners = new[]
                        {
                            RunnerResult.LocalTestRunner,
                            RunnerResult.LocalTestRunner
                        }
                    });

                yield return new TestRunnerFactoryData(
                    "SingleProject",
                    new TestPackage("a.nunit"),
                    RunnerResult.LocalTestRunner
                );

                yield return new TestRunnerFactoryData(
                    "Single unknown extension",
                    new TestPackage("a.junk"),
                    RunnerResult.LocalTestRunner
                );

                yield return new TestRunnerFactoryData(
                    "Two projects",
                    new TestPackage("a.nunit", "b.nunit"),
                    new RunnerResult
                    {
                        TestRunner = typeof(AggregatingTestRunner),
                        SubRunners = new []
                        {
                            RunnerResult.LocalTestRunner,
                            RunnerResult.LocalTestRunner,
                            RunnerResult.LocalTestRunner
                        }
                    }
                );

                yield return new TestRunnerFactoryData(
                    "One project, one assembly",
                    new TestPackage("a.nunit", "a.dll"),
                    new RunnerResult
                    {
                        TestRunner = typeof(AggregatingTestRunner),
                        SubRunners = new []
                        {
                            RunnerResult.LocalTestRunner,
                            RunnerResult.LocalTestRunner,
                            RunnerResult.LocalTestRunner
                        }
                    }
                );

                yield return new TestRunnerFactoryData(
                    "Two projects, one assembly",
                    new TestPackage("a.nunit", "b.nunit", "a.dll"),
                    new RunnerResult
                    {
                        TestRunner = typeof(AggregatingTestRunner),
                        SubRunners = new []
                        {
                            RunnerResult.LocalTestRunner,
                            RunnerResult.LocalTestRunner,
                            RunnerResult.LocalTestRunner,
                            RunnerResult.LocalTestRunner
                        }
                    }
                );

                yield return new TestRunnerFactoryData(
                    "Two assemblies, one project",
                    new TestPackage("a.dll", "b.dll", "a.nunit"),
                    new RunnerResult
                    {
                        TestRunner = typeof(AggregatingTestRunner),
                        SubRunners = new List<RunnerResult>
                        {
                            RunnerResult.LocalTestRunner,
                            RunnerResult.LocalTestRunner,
                            RunnerResult.LocalTestRunner,
                            RunnerResult.LocalTestRunner
                        }
                    }
                );

                yield return new TestRunnerFactoryData(
                    "Two unknown extensions",
                    new TestPackage("a.junk", "b.junk"),
                    new RunnerResult
                    {
                        TestRunner = typeof(AggregatingTestRunner),
                        SubRunners = new[]
                        {
                            RunnerResult.LocalTestRunner,
                            RunnerResult.LocalTestRunner
                        }
                    }
                );

                yield return new TestRunnerFactoryData(
                    "One assembly, one project, one unknown",
                    new TestPackage("a.junk", "a.dll", "a.nunit"),
                    new RunnerResult
                    {
                        TestRunner = typeof(AggregatingTestRunner),
                        SubRunners = new []
                        {
                            RunnerResult.LocalTestRunner,
                            RunnerResult.LocalTestRunner,
                            RunnerResult.LocalTestRunner,
                            RunnerResult.LocalTestRunner
                        }
                    }
                );
#endif
            }
        }
    }
}