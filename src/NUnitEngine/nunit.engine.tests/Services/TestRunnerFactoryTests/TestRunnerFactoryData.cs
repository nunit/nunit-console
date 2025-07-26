// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using System.Collections.Generic;
using NUnit.Engine.Runners;
using NUnit.Framework;

namespace NUnit.Engine.Services.TestRunnerFactoryTests
{
    internal class TestRunnerFactoryData : TestCaseData
    {
        private const string MISSING_ASSEMBLY = "missing.dll";
        private const string UNKNOWN_FILE_TYPE = "test.junk";
#if NETFRAMEWORK
        private const string EXISTING_ASSEMBLY = "testdata/net462/mock-assembly.dll";
        private const string PROJECT_WITH_TWO_ASSEMBLIES = "mock2.nunit";
        private const string PROJECT_WITH_ONE_ASSEMBLY = "mock1.nunit";
#else
        private const string EXISTING_ASSEMBLY = "testdata/net8.0/mock-assembly.dll";
#endif

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
                    "SingleExistingAssembly",
                    new TestPackage(EXISTING_ASSEMBLY).SubPackages[0],
                    RunnerResult.ProcessRunner);

                yield return new TestRunnerFactoryData(
                    "SingleMissingAssembly",
                    new TestPackage(MISSING_ASSEMBLY).SubPackages[0],
                    RunnerResult.InvalidAssemblyTestRunner);

                yield return new TestRunnerFactoryData(
                    "SingleUnknownFileType",
                    new TestPackage(UNKNOWN_FILE_TYPE).SubPackages[0],
                    RunnerResult.InvalidAssemblyTestRunner);

                yield return new TestRunnerFactoryData(
                    "TwoExistingAssemblies",
                    new TestPackage(EXISTING_ASSEMBLY, EXISTING_ASSEMBLY),
                    RunnerResult.AggregatingTestRunner(RunnerResult.ProcessRunner, 2));

                yield return new TestRunnerFactoryData(
                    "TwoMissingAssemblies",
                    new TestPackage(MISSING_ASSEMBLY, MISSING_ASSEMBLY),
                    RunnerResult.AggregatingTestRunner(RunnerResult.InvalidAssemblyTestRunner, 2));

                yield return new TestRunnerFactoryData(
                    "TwoUnknownFileTypes",
                    new TestPackage(UNKNOWN_FILE_TYPE, UNKNOWN_FILE_TYPE),
                    RunnerResult.AggregatingTestRunner(RunnerResult.InvalidAssemblyTestRunner, 2));

                yield return new TestRunnerFactoryData(
                    "ProjectWithTwoAssemblies",
                    new TestPackage(PROJECT_WITH_TWO_ASSEMBLIES),
                    RunnerResult.AggregatingTestRunner(RunnerResult.ProcessRunner, 2));

                yield return new TestRunnerFactoryData(
                    "TwoProjectsWithTwoAssembliesEach",
                    new TestPackage(PROJECT_WITH_TWO_ASSEMBLIES, PROJECT_WITH_TWO_ASSEMBLIES),
                    RunnerResult.AggregatingTestRunner(RunnerResult.ProcessRunner, 4));

                yield return new TestRunnerFactoryData(
                    "OneProjectPlusOneMissingAssembly",
                    new TestPackage(PROJECT_WITH_TWO_ASSEMBLIES, MISSING_ASSEMBLY),
                    RunnerResult.AggregatingTestRunner(
                        RunnerResult.ProcessRunner,
                        RunnerResult.ProcessRunner,
                        RunnerResult.InvalidAssemblyTestRunner));

                yield return new TestRunnerFactoryData(
                    "TwoProjectsPlusOneExistingAssembly",
                    new TestPackage(PROJECT_WITH_TWO_ASSEMBLIES, PROJECT_WITH_TWO_ASSEMBLIES, EXISTING_ASSEMBLY),
                    RunnerResult.AggregatingTestRunner(RunnerResult.ProcessRunner, 5));

                yield return new TestRunnerFactoryData(
                    "TwoAssembliesOneProject",
                    new TestPackage(EXISTING_ASSEMBLY, EXISTING_ASSEMBLY, PROJECT_WITH_TWO_ASSEMBLIES),
                    RunnerResult.AggregatingTestRunner(RunnerResult.ProcessRunner, 4));

                yield return new TestRunnerFactoryData(
                    "OneUnknownOneMissingAssemblyOneProject",
                    new TestPackage(UNKNOWN_FILE_TYPE, MISSING_ASSEMBLY, PROJECT_WITH_TWO_ASSEMBLIES),
                    RunnerResult.AggregatingTestRunner(
                        RunnerResult.InvalidAssemblyTestRunner,
                        RunnerResult.InvalidAssemblyTestRunner,
                        RunnerResult.ProcessRunner,
                        RunnerResult.ProcessRunner));
#else
                // NOTE: Some unsupported test cases are commented out in case
                // we want to reinstate the feature that allows running multiple
                // compatible assemblies under the .NET Core runner. Major
                // decisions need to be made about the features of this runner!

                yield return new TestRunnerFactoryData(
                    "SingleExistingAssembly",
                    new TestPackage(EXISTING_ASSEMBLY).SubPackages[0],
                    RunnerResult.LocalTestRunner);

                yield return new TestRunnerFactoryData(
                    "SingleMissingAssembly",
                    new TestPackage(MISSING_ASSEMBLY).SubPackages[0],
                    RunnerResult.InvalidAssemblyTestRunner);

                yield return new TestRunnerFactoryData(
                    "SingleUnknownFileType",
                    new TestPackage(UNKNOWN_FILE_TYPE).SubPackages[0],
                    RunnerResult.InvalidAssemblyTestRunner);

                //yield return new TestRunnerFactoryData(
                //    "Two assemblies",
                //    new TestPackage(MISSING_ASSEMBLY, MISSING_ASSEMBLY"),
                //    RunnerResult.AggregatingTestRunner(2));

                //yield return new TestRunnerFactoryData(
                //    "SingleProject, One assembly",
                //    new TestPackage(PROJECT_WITH_ONE_ASSEMBLY),
                //    RunnerResult.LocalTestRunner);

                //yield return new TestRunnerFactoryData(
                //    "SingleProject, Two assemblies",
                //    new TestPackage(PROJECT_WITH_TWO_ASSEMBLIES),
                //    RunnerResult.AggregatingTestRunner(2));

                //yield return new TestRunnerFactoryData(
                //    "Two projects",
                //    new TestPackage(PROJECT_WITH_TWO_ASSEMBLIES, PROJECT_WITH_ONE_ASSEMBLY),
                //    RunnerResult.AggregatingTestRunner(3));

                //yield return new TestRunnerFactoryData(
                //    "One project, one assembly",
                //    new TestPackage(PROJECT_WITH_TWO_ASSEMBLIES, MISSING_ASSEMBLY),
                //    RunnerResult.AggregatingTestRunner(3));

                //yield return new TestRunnerFactoryData(
                //    "Two projects, one assembly",
                //    new TestPackage(PROJECT_WITH_TWO_ASSEMBLIES, PROJECT_WITH_ONE_ASSEMBLY, "a.dll"),
                //    RunnerResult.AggregatingTestRunner(4));

                //yield return new TestRunnerFactoryData(
                //    "Two assemblies, one project",
                //    new TestPackage("a.dll", "b.dll", PROJECT_WITH_TWO_ASSEMBLIES),
                //    RunnerResult.AggregatingTestRunner(4));

                //yield return new TestRunnerFactoryData(
                //    "Two unknown extensions",
                //    new TestPackage("a.junk", "b.junk"),
                //    RunnerResult.AggregatingTestRunner(2));

                //yield return new TestRunnerFactoryData(
                //    "One assembly, one project, one unknown",
                //    new TestPackage("a.junk", "a.dll", PROJECT_WITH_TWO_ASSEMBLIES),
                //    RunnerResult.AggregatingTestRunner(4));
#endif
            }
        }
    }
}