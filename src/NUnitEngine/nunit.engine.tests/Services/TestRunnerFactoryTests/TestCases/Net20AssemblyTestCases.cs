// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Engine.Internal;
using NUnit.Engine.Tests.Services.TestRunnerFactoryTests.Results;
using NUnit.Framework;

namespace NUnit.Engine.Tests.Services.TestRunnerFactoryTests.TestCases
{
#if !NETCOREAPP
    internal static class Net20AssemblyTestCases
    {
        public static IEnumerable<TestCaseData> TestCases
        {
            get
            {
                foreach (var processModel in Enum.GetValues(typeof(ProcessModel)).Cast<ProcessModel>())
                {
                    yield return SingleAssemblyStringCtorTest(processModel);
                    yield return SingleAssemblyListCtorTest(processModel);
                    yield return SingleUnknownExtensionTest(processModel);
                    yield return TwoAssembliesTest(processModel);
                    yield return TwoUnknownsTest(processModel);
                }
            }
        }

        private static TestCaseData SingleAssemblyStringCtorTest(ProcessModel processModel)
        {
            var testName = "Single assembly (string ctor) - " +
                           $"{nameof(EnginePackageSettings.ProcessModel)}:{processModel}";
            var package = TestPackageFactory.OneAssemblyStringCtor();
            package.AddSetting(EnginePackageSettings.ProcessModel, processModel.ToString());

            var expected = Net20SingleAssemblyStringCtorExpectedRunnerResults.ResultFor(processModel);
            var testCase = new TestCaseData(package, expected).SetName($"{{m}}({testName})");
            return testCase;
        }

        private static TestCaseData SingleAssemblyListCtorTest(ProcessModel processModel)
        {
            var testName = "Single assembly (list ctor) - " +
                              $"{nameof(EnginePackageSettings.ProcessModel)}:{processModel}";
            var package = TestPackageFactory.OneAssemblyListCtor();
            package.AddSetting(EnginePackageSettings.ProcessModel, processModel.ToString());

            var expected = Net20SingleAssemblyListCtorExpectedRunnerResults.ResultFor(processModel);
            return new TestCaseData(package, expected).SetName($"{{m}}({testName})");
        }

        private static TestCaseData SingleUnknownExtensionTest(ProcessModel processModel)
        {
            var testName = "Single unknown - " +
                           $"{nameof(EnginePackageSettings.ProcessModel)}:{processModel}";
            var package = TestPackageFactory.OneUnknownExtension();
            package.AddSetting(EnginePackageSettings.ProcessModel, processModel.ToString());

            var expected = Net20SingleAssemblyListCtorExpectedRunnerResults.ResultFor(processModel);
            return new TestCaseData(package, expected).SetName($"{{m}}({testName})");
        }

        private static TestCaseData TwoAssembliesTest(ProcessModel processModel)
        {
            var testName = "Two assemblies - " +
                           $"{nameof(EnginePackageSettings.ProcessModel)}:{processModel}";
            var package = TestPackageFactory.TwoAssemblies();
            package.AddSetting(EnginePackageSettings.ProcessModel, processModel.ToString());

            var expected = Net20TwoAssemblyExpectedRunnerResults.ResultFor(processModel);
            return new TestCaseData(package, expected).SetName($"{{m}}({testName})");
        }

        private static TestCaseData TwoUnknownsTest(ProcessModel processModel)
        {
            var testName = "Two unknown extensions - " +
                           $"{nameof(EnginePackageSettings.ProcessModel)}:{processModel}";
            var package = TestPackageFactory.TwoUnknownExtension();
            package.AddSetting(EnginePackageSettings.ProcessModel, processModel.ToString());

            var expected = Net20TwoAssemblyExpectedRunnerResults.ResultFor(processModel);
            return new TestCaseData(package, expected).SetName($"{{m}}({testName})");
        }
    }
#endif
}