// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using System;
using System.IO;
using NUnit.Framework;
using NUnit.Engine.Drivers;

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

        [TestCaseSource(nameof(DriverSelectionTestCases))]
        public void CorrectDriverIsUsed(string fileName, bool skipNonTestAssemblies, Type expectedType)
        {
            var assemblyPath = Path.Combine(TestContext.CurrentContext.TestDirectory, fileName);
            var driver = _driverService.GetDriver(AppDomain.CurrentDomain, new TestPackage(assemblyPath), assemblyPath, null, skipNonTestAssemblies);
            Assert.That(driver, Is.InstanceOf(expectedType));
        }

        private static TestCaseData[] DriverSelectionTestCases = new[]
        {
            new TestCaseData("mock-assembly.dll", false, typeof(NUnitFrameworkDriver)),
            new TestCaseData("mock-assembly.dll", true, typeof(NUnitFrameworkDriver)),
            new TestCaseData("notest-assembly.dll", false, typeof(NUnitFrameworkDriver)).Ignore("Assembly not present"),
        };
    }
}
