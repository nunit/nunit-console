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
                    foreach (var domainUsage in Enum.GetValues(typeof(DomainUsage)).Cast<DomainUsage>())
                    {
                        var testName = "Single project (list ctor) - " +
                                       $"{nameof(EnginePackageSettings.ProcessModel)}:{processModel} " +
                                       $"{nameof(EnginePackageSettings.DomainUsage)}:{domainUsage}";

                        var package = TestPackageFactory.OneProjectListCtor();
                        package.AddSetting(EnginePackageSettings.ProcessModel, processModel.ToString());
                        package.AddSetting(EnginePackageSettings.DomainUsage, domainUsage.ToString());

                        var expected = Net20SingleProjectListCtorExpectedRunnerResults.ResultFor(processModel, domainUsage);
                        yield return new TestCaseData(package, expected).SetName($"{{m}}({testName})");

                        testName = "Single project (string ctor) - " +
                                       $"{nameof(EnginePackageSettings.ProcessModel)}:{processModel} " +
                                       $"{nameof(EnginePackageSettings.DomainUsage)}:{domainUsage}";

                        package = TestPackageFactory.OneProjectStringCtor();
                        package.AddSetting(EnginePackageSettings.ProcessModel, processModel.ToString());
                        package.AddSetting(EnginePackageSettings.DomainUsage, domainUsage.ToString());

                        expected = Net20SingleProjectStringCtorExpectedRunnerResults.ResultFor(processModel, domainUsage);
                        yield return new TestCaseData(package, expected).SetName($"{{m}}({testName})");

                        testName = "Two projects - " +
                                       $"{nameof(EnginePackageSettings.ProcessModel)}:{processModel} " +
                                       $"{nameof(EnginePackageSettings.DomainUsage)}:{domainUsage}";

                        package = TestPackageFactory.TwoProjects();
                        package.AddSetting(EnginePackageSettings.ProcessModel, processModel.ToString());
                        package.AddSetting(EnginePackageSettings.DomainUsage, domainUsage.ToString());

                        expected = Net20TwoProjectExpectedRunnerResults.ResultFor(processModel, domainUsage);
                        yield return new TestCaseData(package, expected).SetName($"{{m}}({testName})");
                    }
                }
            }
        }
    }
#endif
}
