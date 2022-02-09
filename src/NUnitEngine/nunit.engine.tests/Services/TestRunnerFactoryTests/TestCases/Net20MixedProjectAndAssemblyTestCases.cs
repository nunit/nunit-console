// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

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
                        var testName = "One project, one assembly - " +
                                       $"{nameof(EnginePackageSettings.ProcessModel)}:{processModel}";

                        var package = new TestPackage("a.nunit", "c.dll");
                        package.AddSetting(EnginePackageSettings.ProcessModel, processModel.ToString());

                        var expected =
                            Net20OneAssemblyOneProjectExpectedRunnerResults.ResultFor(processModel);
                        yield return new TestCaseData(package, expected).SetName($"{{m}}({testName})");

                        testName = "Two projects, one assembly - " +
                                   $"{nameof(EnginePackageSettings.ProcessModel)}:{processModel}";

                        package = new TestPackage("a.nunit", "a.nunit", "x.dll");
                        package.AddSetting(EnginePackageSettings.ProcessModel, processModel.ToString());

                        expected = Net20TwoProjectsOneAssemblyExpectedRunnerResults.ResultFor(processModel);
                        yield return new TestCaseData(package, expected).SetName($"{{m}}({testName})");

                        testName = "Two assemblies, one project - " +
                                   $"{nameof(EnginePackageSettings.ProcessModel)}:{processModel}";
                        package = new TestPackage("x.dll", "y.dll", "a.nunit");
                        package.AddSetting(EnginePackageSettings.ProcessModel, processModel.ToString());

                        expected = Net20TwoAssembliesOneProjectExpectedRunnerResults.ResultFor(processModel);
                        yield return new TestCaseData(package, expected).SetName($"{{m}}({testName})");

                        testName = "One unknown, one assembly, one project - " +
                                   $"{nameof(EnginePackageSettings.ProcessModel)}:{processModel}";
                        package = new TestPackage("a.junk", "a.dll", "a.nunit");
                        package.AddSetting(EnginePackageSettings.ProcessModel, processModel.ToString());
                        expected = Net20OneUnknownOneAssemblyOneProjectExpectedRunnerResults.ResultFor(processModel);
                        yield return new TestCaseData(package, expected).SetName($"{{m}}({testName})");
                }
            }
        }
    }
#endif
}