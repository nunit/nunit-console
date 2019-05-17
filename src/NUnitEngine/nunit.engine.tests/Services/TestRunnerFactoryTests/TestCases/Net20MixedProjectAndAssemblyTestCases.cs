using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Engine.Internal;
using NUnit.Engine.Tests.Services.TestRunnerFactoryTests.Results;
using NUnit.Framework;

namespace NUnit.Engine.Tests.Services.TestRunnerFactoryTests.TestCases
{
#if !NETCOREAPP
    internal class Net20MixedProjectAndAssemblyTestCases
    {
        public static IEnumerable<TestCaseData> TestCases
        {
            get
            {
                foreach (var processModel in Enum.GetValues(typeof(ProcessModel)).Cast<ProcessModel>())
                {
                    foreach (var domainUsage in Enum.GetValues(typeof(DomainUsage)).Cast<DomainUsage>())
                    {
                        var testName = "One project, one assembly - " +
                                       $"{nameof(EnginePackageSettings.ProcessModel)}:{processModel} " +
                                       $"{nameof(EnginePackageSettings.DomainUsage)}:{domainUsage}";

                        var package = TestPackageFactory.OneProjectOneAssembly();
                        package.AddSetting(EnginePackageSettings.ProcessModel, processModel.ToString());
                        package.AddSetting(EnginePackageSettings.DomainUsage, domainUsage.ToString());

                        var expected =
                            Net20OneAssemblyOneProjectExpectedRunnerResults.ResultFor(processModel, domainUsage);
                        yield return new TestCaseData(package, expected).SetName($"{{m}}({testName})");

                        testName = "Two projects, one assembly - " +
                                   $"{nameof(EnginePackageSettings.ProcessModel)}:{processModel} " +
                                   $"{nameof(EnginePackageSettings.DomainUsage)}:{domainUsage}";

                        package = TestPackageFactory.TwoProjectsOneAssembly();
                        package.AddSetting(EnginePackageSettings.ProcessModel, processModel.ToString());
                        package.AddSetting(EnginePackageSettings.DomainUsage, domainUsage.ToString());

                        expected = Net20ThreeItemExpectedRunnerResults.ResultFor(processModel,
                            domainUsage);
                        yield return new TestCaseData(package, expected).SetName($"{{m}}({testName})");

                        testName = "Two assemblies, one project - " +
                                   $"{nameof(EnginePackageSettings.ProcessModel)}:{processModel} " +
                                   $"{nameof(EnginePackageSettings.DomainUsage)}:{domainUsage}";

                        package = TestPackageFactory.TwoAssembliesOneProject();
                        package.AddSetting(EnginePackageSettings.ProcessModel, processModel.ToString());
                        package.AddSetting(EnginePackageSettings.DomainUsage, domainUsage.ToString());

                        expected = Net20ThreeItemExpectedRunnerResults.ResultFor(processModel, domainUsage);
                        yield return new TestCaseData(package, expected).SetName($"{{m}}({testName})");

                        testName = "One assembly, one project, one unknown - " +
                                   $"{nameof(EnginePackageSettings.ProcessModel)}:{processModel} " +
                                   $"{nameof(EnginePackageSettings.DomainUsage)}:{domainUsage}";

                        package = TestPackageFactory.OneAssemblyOneProjectOneUnknown();
                        package.AddSetting(EnginePackageSettings.ProcessModel, processModel.ToString());
                        package.AddSetting(EnginePackageSettings.DomainUsage, domainUsage.ToString());

                        expected = Net20ThreeItemExpectedRunnerResults.ResultFor(processModel, domainUsage);
                        yield return new TestCaseData(package, expected).SetName($"{{m}}({testName})");
                    }
                }
            }
        }
    }
#endif
}