// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using System;
using System.Collections.Generic;
using System.Text;
using NUnit.Engine.Runners;
using NUnit.Framework;

namespace NUnit.Engine.Services.Tests
{
    public class InProcessTestRunnerFactoryTests
    {
        [SetUp]
        public void CreateServiceContext()
        {
            var services = new ServiceContext();
            services.ServiceManager.StartServices();
        }

#if NETCOREAPP
        [TestCase("x.dll", typeof(LocalTestRunner))]
        [TestCase("x.dll y.dll", typeof(LocalTestRunner))]
        [TestCase("x.dll y.dll z.dll", typeof(LocalTestRunner))]
#else
        // Single file
        [TestCase("x.dll", typeof(TestDomainRunner))]
        [TestCase("x.dll y.dll", typeof(MultipleTestDomainRunner))]
        [TestCase("x.dll y.dll z.dll", typeof(MultipleTestDomainRunner))]
#endif
        public void CorrectRunnerIsUsed(string files, Type expectedType)
        {
            var package = new TestPackage(files.Split(new char[] { ' ' }));
            Assert.That(InProcessTestRunnerFactory.MakeTestRunner(new ServiceContext(), package), Is.TypeOf(expectedType));
        }
    }
}
