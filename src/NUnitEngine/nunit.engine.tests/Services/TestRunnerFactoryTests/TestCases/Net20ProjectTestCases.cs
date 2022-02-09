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
    internal static class Net20ProjectTestCases
    {
        public static IEnumerable<TestCaseData> TestCases
        {
            get
            {
                foreach (var processModel in Enum.GetValues(typeof(ProcessModel)).Cast<ProcessModel>())
                {
                        var testName = "Single project - " +
                                       $"{nameof(EnginePackageSettings.ProcessModel)}:{processModel}";
                        var package = TestPackageFactory.OneProject();
                        package.AddSetting(EnginePackageSettings.ProcessModel, processModel.ToString());
                        var expected = Net20SingleProjectExpectedRunnerResults.ResultFor(processModel);
                        yield return new TestCaseData(package, expected).SetName($"{{m}}({testName})");

                        testName = "Two projects - " +
                                       $"{nameof(EnginePackageSettings.ProcessModel)}:{processModel}";
                        package = TestPackageFactory.TwoProjects();
                        package.AddSetting(EnginePackageSettings.ProcessModel, processModel.ToString());
                        expected = Net20TwoProjectExpectedRunnerResults.ResultFor(processModel);
                        yield return new TestCaseData(package, expected).SetName($"{{m}}({testName})");
                }
            }
        }
    }
#endif
}
