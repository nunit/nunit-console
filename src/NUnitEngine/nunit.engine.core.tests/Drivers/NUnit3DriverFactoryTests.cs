// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using NUnit.Engine.Drivers;
using NUnit.Framework;
using System;
using System.Reflection;

namespace NUnit.Engine.Drivers
{
    public class NUnit3DriverFactoryTests
    {
        private NUnit3DriverFactory _factory;

        [SetUp]
        public void SetUp()
        {
            _factory = new NUnit3DriverFactory();
        }

        [TestCase(2, ExpectedResult = false)]
        [TestCase(3, ExpectedResult = true)]
        [TestCase(4, ExpectedResult = true)]
        public bool SupportsNetFramework(int majorVersion)
        {
            AssemblyName name = new AssemblyName(NUnit3DriverFactory.NUNIT_FRAMEWORK)
            {
                Version = new Version(majorVersion, 0)
            };

            return _factory.IsSupportedTestFramework(name);
        }

        [Test]
        public void DoesNotSupportOtherFrameworks()
        {
            AssemblyName name = new AssemblyName("VSTest.dll")
            {
                Version = new Version(3, 0)
            };

            Assert.That(_factory.IsSupportedTestFramework(name), Is.False);
        }
    }
}
