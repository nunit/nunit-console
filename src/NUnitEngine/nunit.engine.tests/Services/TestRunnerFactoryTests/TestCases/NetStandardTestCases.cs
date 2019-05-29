// ***********************************************************************
// Copyright (c) 2018 Charlie Poole, Rob Prouse
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
// ***********************************************************************

using System.Collections.Generic;
using NUnit.Engine.Runners;
using NUnit.Framework;

namespace NUnit.Engine.Tests.Services.TestRunnerFactoryTests.TestCases
{
#if NETCOREAPP1_1 || NETCOREAPP2_0
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

#if NETCOREAPP2_0
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
#endif
            }
        }
    }
#endif
}