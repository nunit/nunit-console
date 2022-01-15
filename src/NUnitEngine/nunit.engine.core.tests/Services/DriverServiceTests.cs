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
            var serviceContext = new ServiceContext();
            serviceContext.Add(new ExtensionService());
            _driverService = new DriverService();
            serviceContext.Add(_driverService);
            serviceContext.ServiceManager.StartServices();
        }

        [Test]
        public void ServiceIsStarted()
        {
            Assert.That(_driverService.Status, Is.EqualTo(ServiceStatus.Started), "Failed to start service");
        }


#if NET5_0_OR_GREATER
        [TestCase("mock-assembly.dll", false, typeof(NUnitNetCore31Driver))]
        [TestCase("mock-assembly.dll", true, typeof(NUnitNetCore31Driver))]
        //[TestCase("notest-assembly.dll", false, typeof(NUnitNetCore31Driver))]
#elif NETCOREAPP3_1
        [TestCase("mock-assembly.dll", false, typeof(NUnitNetCore31Driver))]
        [TestCase("mock-assembly.dll", true, typeof(NUnitNetCore31Driver))]
        [TestCase("notest-assembly.dll", false, typeof(NUnitNetCore31Driver))]
#elif NETCOREAPP2_1
        [TestCase("mock-assembly.dll", false, typeof(NUnitNetStandardDriver))]
        [TestCase("mock-assembly.dll", true, typeof(NUnitNetStandardDriver))]
        [TestCase("notest-assembly.dll", false, typeof(NUnitNetStandardDriver))]
#else
        [TestCase("mock-assembly.dll", false, typeof(NUnit3FrameworkDriver))]
        [TestCase("mock-assembly.dll", true, typeof(NUnit3FrameworkDriver))]
        [TestCase("notest-assembly.dll", false, typeof(NUnit3FrameworkDriver))]
#endif
        [TestCase("mock-assembly.pdb", false, typeof(InvalidAssemblyFrameworkDriver))]
        [TestCase("mock-assembly.pdb", true, typeof(InvalidAssemblyFrameworkDriver))]
        [TestCase("junk.dll", false, typeof(InvalidAssemblyFrameworkDriver))]
        [TestCase("junk.dll", true, typeof(InvalidAssemblyFrameworkDriver))]
        [TestCase("nunit.engine.dll", false, typeof(InvalidAssemblyFrameworkDriver))]
#if !NET5_0 // Not yet working
        [TestCase("nunit.engine.dll", true, typeof(SkippedAssemblyFrameworkDriver))]
        [TestCase("notest-assembly.dll", true, typeof(SkippedAssemblyFrameworkDriver))]
#endif
        public void CorrectDriverIsUsed(string fileName, bool skipNonTestAssemblies, Type expectedType)
        {
            var driver = _driverService.GetDriver(AppDomain.CurrentDomain, Path.Combine(TestContext.CurrentContext.TestDirectory, fileName), null, skipNonTestAssemblies);
            Assert.That(driver, Is.InstanceOf(expectedType));
        }
    }
}
