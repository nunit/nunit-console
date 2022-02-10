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
                var testName = "Single project";
                    var package = new TestPackage("a.nunit");
                    var expected = Net20SingleProjectExpectedRunnerResults.ResultFor(ProcessModel.Default);
                    yield return new TestCaseData(package, expected).SetName($"{{m}}({testName})");

                testName = "Two projects";
                    package = new TestPackage("a.nunit", "a.nunit");
                    expected = Net20TwoProjectExpectedRunnerResults.ResultFor(ProcessModel.Default);
                    yield return new TestCaseData(package, expected).SetName($"{{m}}({testName})");
            }
        }
    }
#endif
}
