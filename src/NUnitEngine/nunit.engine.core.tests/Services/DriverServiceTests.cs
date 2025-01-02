// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using NUnit.Engine.Drivers;
using NUnit.Engine.Extensibility;

namespace NUnit.Engine.Services.Tests
{
    [TestFixture]
    public class DriverServiceTests
    {
        private DriverService _driverService;

        [SetUp]
        public void CreateDriverFactory()
        {
            _driverService = new DriverService();
        }

        [TestCaseSource(nameof(DriverSelectionTestCases))]
        public void CorrectDriverIsUsed(string fileName, bool skipNonTestAssemblies, Type expectedType)
        {
            var driver = _driverService.GetDriver(AppDomain.CurrentDomain, Path.Combine(TestContext.CurrentContext.TestDirectory, fileName), null, skipNonTestAssemblies);
            Assert.That(driver, Is.InstanceOf(expectedType));
        }

        static TestCaseData[] DriverSelectionTestCases = new[]
        {
            // TODO: make commented tests work
#if NETFRAMEWORK
            new TestCaseData("mock-assembly.dll", false, typeof(NUnit3FrameworkDriver)),
            new TestCaseData("mock-assembly.dll", true, typeof(NUnit3FrameworkDriver)),
            //new TestCaseData("notest-assembly.dll", false, typeof(NUnit3FrameworkDriver)),
#elif NET5_0_OR_GREATER
            new TestCaseData("mock-assembly.dll", false, typeof(NUnitNetCore31Driver)),
            new TestCaseData("mock-assembly.dll", true, typeof(NUnitNetCore31Driver)),
            //new TestCaseData("notest-assembly.dll", false, typeof(NUnitNetCore31Driver)),
#elif NETCOREAPP3_1
            new TestCaseData("mock-assembly.dll", false, typeof(NUnitNetCore31Driver)),
            new TestCaseData("mock-assembly.dll", true, typeof(NUnitNetCore31Driver)),
            //new TestCaseData("notest-assembly.dll", false, typeof(NUnitNetCore31Driver)),
// TODO: This is never used. We need to test net standard driver in some way, possibly
// by forcing it's use in a separate test.
#elif NETCOREAPP2_1
            new TestCaseData("mock-assembly.dll", false, typeof(NUnitNetStandardDriver)),
            new TestCaseData("mock-assembly.dll", true, typeof(NUnitNetStandardDriver)),
            new TestCaseData("notest-assembly.dll", false, typeof(NUnitNetStandardDriver)),
#endif
// Invalid cases should work with all target runtimes
            new TestCaseData("mock-assembly.pdb", false, typeof(InvalidAssemblyFrameworkDriver)),
            new TestCaseData("mock-assembly.pdb", true, typeof(InvalidAssemblyFrameworkDriver)),
            new TestCaseData("junk.dll", false, typeof(InvalidAssemblyFrameworkDriver)),
            new TestCaseData("junk.dll", true, typeof(InvalidAssemblyFrameworkDriver)),
            new TestCaseData("nunit.engine.core.dll", false, typeof(InvalidAssemblyFrameworkDriver)),
            new TestCaseData("nunit.engine.core.dll", true, typeof(SkippedAssemblyFrameworkDriver))
//#if !NET5_0_OR_GREATER // Not yet working
//            new TestCaseData"notest-assembly.dll", true, typeof(SkippedAssemblyFrameworkDriver))
//#endif
        };

        [Test]
        public void EnsureWeHaveSomeValidTestCases()
        {
            // We currently build these tests for net462, net 8.0, net 6.0 and net core 3.1.
            // This test is needed because of the conditional compilation used in generating
            // the test cases. If the test project is updated to add a new target runtime,
            // and no test cases are added for that runtime, this test will fail.
            foreach (var testcase in DriverSelectionTestCases)
            {
                // Third argument is the Type of the driver
                var driverType = testcase.Arguments[2] as Type;
                if (driverType is null || !(driverType.BaseType == typeof(NotRunnableFrameworkDriver)))
                    break;

                // All expected drivers derive from NotRunnableFrameworkDriver
                Assert.Fail("Only invalid test cases were provided for this runtime. Update DriverServiceTests.cs to include some valid cases.");
            }
        }
    }
}
