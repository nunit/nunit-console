// ***********************************************************************
// Copyright (c) 2014 Charlie Poole, Rob Prouse
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
// ***********************************************************************

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
#if !NETCOREAPP1_1
            serviceContext.Add(new ExtensionService());
#endif
            _driverService = new DriverService();
            serviceContext.Add(_driverService);
            serviceContext.ServiceManager.StartServices();
        }

        [Test]
        public void ServiceIsStarted()
        {
            Assert.That(_driverService.Status, Is.EqualTo(ServiceStatus.Started), "Failed to start service");
        }


#if NETCOREAPP1_1 || NETCOREAPP2_1
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
        [TestCase("nunit.engine.dll", true, typeof(SkippedAssemblyFrameworkDriver))]
        [TestCase("notest-assembly.dll", true, typeof(SkippedAssemblyFrameworkDriver))]
        public void CorrectDriverIsUsed(string fileName, bool skipNonTestAssemblies, Type expectedType)
        {
            var driver = _driverService.GetDriver(
#if !NETCOREAPP1_1
                AppDomain.CurrentDomain,
#endif
                Path.Combine(TestContext.CurrentContext.TestDirectory, fileName),
#if !NETCOREAPP1_1
                null,
#endif
                skipNonTestAssemblies);

            Assert.That(driver, Is.InstanceOf(expectedType));
        }
    }
}
