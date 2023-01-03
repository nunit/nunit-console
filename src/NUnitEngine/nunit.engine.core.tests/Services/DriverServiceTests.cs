// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using NUnit.Engine.Drivers;
using NUnit.Engine.Extensibility;

namespace NUnit.Engine.Services
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

        const string NOTEST_ASSEMBLY =
#if DEBUG
            "../../../../../TestData/notest-assembly/bin/Debug/notest-assembly.dll";
#else
            "../../../../../TestData/notest-assembly/bin/Release/notest-assembly.dll";
#endif

        static IEnumerable<TestCaseData> DriverData()
        {
#if NETCOREAPP3_1
            yield return new TestCaseData(TestData.MockAssemblyPath("netcoreapp3.1"), false, typeof(NUnitNetCore31Driver));
            yield return new TestCaseData(TestData.MockAssemblyPath("netcoreapp3.1"), true, typeof(NUnitNetCore31Driver));
            yield return new TestCaseData(TestData.NoTestAssemblyPath("netcoreapp3.1"), false, typeof(NUnitNetCore31Driver));
#elif NETCOREAPP2_1
            yield return new TestCaseData(TestData.MockAssemblyPath("netcoreapp2.1"), false, typeof(NUnitNetStandardDriver));
            yield return new TestCaseData(TestData.MockAssemblyPath("netcoreapp2.1"), true, typeof(NUnitNetStandardDriver));
            yield return new TestCaseData(TestData.NoTestAssemblyPath("netcoreapp2.1"), false, typeof(NUnitNetStandardDriver));
#else
            yield return new TestCaseData(TestData.MockAssemblyPath("net35"), false, typeof(NUnit3FrameworkDriver));
            yield return new TestCaseData(TestData.MockAssemblyPath("net35"), true, typeof(NUnit3FrameworkDriver));
            yield return new TestCaseData(TestData.NoTestAssemblyPath("net35"), false, typeof(NUnit3FrameworkDriver));
#endif
            yield return new TestCaseData("mock-assembly.pdb", false, typeof(InvalidAssemblyFrameworkDriver));
            yield return new TestCaseData("mock-assembly.pdb", true, typeof(InvalidAssemblyFrameworkDriver));
            yield return new TestCaseData("junk.dll", false, typeof(InvalidAssemblyFrameworkDriver));
            yield return new TestCaseData("junk.dll", true, typeof(InvalidAssemblyFrameworkDriver));
            yield return new TestCaseData("nunit.engine.core.dll", false, typeof(InvalidAssemblyFrameworkDriver));
            yield return new TestCaseData("nunit.engine.core.dll", true, typeof(SkippedAssemblyFrameworkDriver));
            yield return new TestCaseData(TestData.NoTestAssemblyPath("net35"), true, typeof(SkippedAssemblyFrameworkDriver));
        }

        [TestCaseSource(nameof(DriverData))]
        public void CorrectDriverIsUsed(string assemblyPath, bool skipNonTestAssemblies, Type expectedType)
        {
            var driver = _driverService.GetDriver(AppDomain.CurrentDomain, assemblyPath, null, skipNonTestAssemblies);
            Assert.That(driver, Is.InstanceOf(expectedType));
        }
    }
}
