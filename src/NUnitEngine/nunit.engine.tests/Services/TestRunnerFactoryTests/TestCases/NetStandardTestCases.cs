// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using System.Collections.Generic;
using NUnit.Engine.Runners;
using NUnit.Framework;

namespace NUnit.Engine.Tests.Services.TestRunnerFactoryTests.TestCases
{
#if NETCOREAPP
    internal static class NetStandardTestCases
    {
        public class TestRunnerFactoryData : TestCaseData
        {
            public TestRunnerFactoryData(string testName, TestPackage package, RunnerResult result)
                : base(package, result)
            {
                SetName($"{{m}}({testName}");
            }
        }

        public static IEnumerable<TestCaseData> TestCases
        {
            get
            {
                yield return new TestRunnerFactoryData(
                    "SingleAssembly (string ctor)",
                    new TestPackage("a.dll"),
                    RunnerResult.LocalTestRunner
                );

                yield return new TestRunnerFactoryData(
                    "Single assembly (list ctor)",
                    new TestPackage(new[] { "a.dll" }),
                    RunnerResult.LocalTestRunner
                );

                yield return new TestRunnerFactoryData(
                    "Two assemblies",
                    new TestPackage(new[] { "a.dll", "b.dll" }),
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
                    "SingleProject (list ctor)",
                    new TestPackage(new[] { "a.nunit" }),
                    RunnerResult.LocalTestRunner
                );

                yield return new TestRunnerFactoryData(
                    "Single project (string ctor)",
                    new TestPackage("a.nunit"),
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
                    "Single unknown extension (list ctor)",
                    new TestPackage(new[] { "a.junk" }),
                    RunnerResult.LocalTestRunner
                );

                yield return new TestRunnerFactoryData(
                    "Two projects",
                    new TestPackage(new[] { "a.nunit", "b.nunit" }),
                    new RunnerResult
                    {
                        TestRunner = typeof(AggregatingTestRunner),
                        SubRunners = new List<RunnerResult>
                        {
                            new RunnerResult
                            {
                                TestRunner = typeof(AggregatingTestRunner),
                                SubRunners = new List<RunnerResult>
                                {
                                    RunnerResult.LocalTestRunner,
                                    RunnerResult.LocalTestRunner
                                }
                            },
                            RunnerResult.LocalTestRunner
                        }
                    }
                );

                yield return new TestRunnerFactoryData(
                    "One project, one assembly",
                    new TestPackage(new[] { "a.nunit", "a.dll" }),
                    new RunnerResult
                    {
                        TestRunner = typeof(AggregatingTestRunner),
                        SubRunners = new List<RunnerResult>
                        {
                            new RunnerResult
                            {
                                TestRunner = typeof(AggregatingTestRunner),
                                SubRunners = new List<RunnerResult>
                                {
                                    RunnerResult.LocalTestRunner,
                                    RunnerResult.LocalTestRunner
                                }
                            },
                            RunnerResult.LocalTestRunner
                        }
                    }
                );

                yield return new TestRunnerFactoryData(
                    "Two projects, one assembly",
                    new TestPackage(new[] { "a.nunit", "b.nunit", "a.dll" }),
                    new RunnerResult
                    {
                        TestRunner = typeof(AggregatingTestRunner),
                        SubRunners = new List<RunnerResult>
                        {
                            new RunnerResult
                            {
                                TestRunner = typeof(AggregatingTestRunner),
                                SubRunners = new List<RunnerResult>
                                {
                                    RunnerResult.LocalTestRunner,
                                    RunnerResult.LocalTestRunner
                                }
                            },
                            RunnerResult.LocalTestRunner,
                            RunnerResult.LocalTestRunner
                        }
                    }
                );

                yield return new TestRunnerFactoryData(
                    "Two assemblies, one project",
                    new TestPackage(new[] { "a.dll", "b.dll", "a.nunit" }),
                    new RunnerResult
                    {
                        TestRunner = typeof(AggregatingTestRunner),
                        SubRunners = new List<RunnerResult>
                        {
                            RunnerResult.LocalTestRunner,
                            RunnerResult.LocalTestRunner,
                            new RunnerResult
                            {
                                TestRunner = typeof(AggregatingTestRunner),
                                SubRunners = new List<RunnerResult>
                                {
                                    RunnerResult.LocalTestRunner,
                                    RunnerResult.LocalTestRunner
                                }
                            }
                        }
                    }
                );

                yield return new TestRunnerFactoryData(
                    "Two unknown extensions",
                    new TestPackage(new[] { "a.junk", "b.junk" }),
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
                    new TestPackage(new[] { "a.junk", "a.dll", "a.nunit" }),
                    new RunnerResult
                    {
                        TestRunner = typeof(AggregatingTestRunner),
                        SubRunners = new List<RunnerResult>
                        {
                            RunnerResult.LocalTestRunner,
                            RunnerResult.LocalTestRunner,
                            new RunnerResult
                            {
                                TestRunner = typeof(AggregatingTestRunner),
                                SubRunners = new List<RunnerResult>
                                {
                                    RunnerResult.LocalTestRunner,
                                    RunnerResult.LocalTestRunner
                                }
                            }
                        }
                    }
                );
            }
        }
    }
#endif
}