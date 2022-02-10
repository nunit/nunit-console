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
                var testName = "One project, one assembly";
                    var package = new TestPackage("a.nunit", "c.dll");
                    var expected =
                        Net20OneAssemblyOneProjectExpectedRunnerResults.ResultFor(ProcessModel.Default);
                    yield return new TestCaseData(package, expected).SetName($"{{m}}({testName})");

                testName = "Two projects, one assembly";
                    package = new TestPackage("a.nunit", "a.nunit", "x.dll");
                    expected = Net20TwoProjectsOneAssemblyExpectedRunnerResults.ResultFor(ProcessModel.Default);
                    yield return new TestCaseData(package, expected).SetName($"{{m}}({testName})");

                testName = "Two assemblies, one project";
                    package = new TestPackage("x.dll", "y.dll", "a.nunit");
                    expected = Net20TwoAssembliesOneProjectExpectedRunnerResults.ResultFor(ProcessModel.Default);
                    yield return new TestCaseData(package, expected).SetName($"{{m}}({testName})");

                testName = "One unknown, one assembly, one project";
                    package = new TestPackage("a.junk", "a.dll", "a.nunit");
                    expected = Net20OneUnknownOneAssemblyOneProjectExpectedRunnerResults.ResultFor(ProcessModel.Default);
                    yield return new TestCaseData(package, expected).SetName($"{{m}}({testName})");
            }
        }
    }
#endif
}