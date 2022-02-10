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
                yield return SingleAssemblyTest();
                yield return SingleUnknownExtensionTest();
                yield return TwoAssembliesTest();
                yield return TwoUnknownsTest();
            }
        }

        private static TestCaseData SingleAssemblyTest()
        {
            var testName = $"Single assembly";
            var package = new TestPackage("a.dll");
            var expected = Net20SingleAssemblyExpectedRunnerResults.ResultFor(ProcessModel.Default);
            return new TestCaseData(package, expected).SetName($"{{m}}({testName})");
        }
        
        private static TestCaseData SingleUnknownExtensionTest()
        {
            var testName = "Single unknown";
            var package = new TestPackage("a.junk");
            var expected = Net20SingleAssemblyExpectedRunnerResults.ResultFor(ProcessModel.Default);
            return new TestCaseData(package, expected).SetName($"{{m}}({testName})");
        }

        private static TestCaseData TwoAssembliesTest()
        {
            var testName = "Two assemblies";
            var package = new TestPackage("a.dll", "b.dll");
            var expected = Net20TwoAssemblyExpectedRunnerResults.ResultFor(ProcessModel.Default);
            return new TestCaseData(package, expected).SetName($"{{m}}({testName})");
        }

        private static TestCaseData TwoUnknownsTest()
        {
            var testName = "Two unknown extensions";
            var package = new TestPackage("a.junk", "b.junk");
            var expected = Net20TwoAssemblyExpectedRunnerResults.ResultFor(ProcessModel.Default);
            return new TestCaseData(package, expected).SetName($"{{m}}({testName})");
        }
    }
#endif
}