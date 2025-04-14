// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using System;
using System.Collections.Generic;
using NUnit.Framework;
using NUnit.TestData.Assemblies;
using NUnit.Engine.Drivers;

namespace NUnit.Engine.Services
{
    public class TestFilteringTests
    {
        private const string MOCK_ASSEMBLY = "mock-assembly.dll";

        private NUnitFrameworkDriver _driver;

        [SetUp]
        public void LoadAssembly()
        {
            var mockAssemblyPath = System.IO.Path.Combine(TestContext.CurrentContext.TestDirectory, MOCK_ASSEMBLY);
            var nunitRef = typeof(TestAttribute).Assembly.GetName();
            var assemblyPath = typeof(TestAttribute).Assembly.Location;
            string driverId = "99";
#if NETFRAMEWORK
            _driver = new NUnitFrameworkDriver(AppDomain.CurrentDomain, driverId, nunitRef);
#else
            _driver = new NUnitFrameworkDriver(driverId, nunitRef);
#endif
            _driver.Load(mockAssemblyPath, new Dictionary<string, object>());
        }

        // TODO: Uncomment the "double negative" tests when we are using an updated framework that handles them correctly.
        [TestCase("<filter/>", MockAssembly.Tests)]
        [TestCase("<filter><test>NUnit.TestData.Assemblies.MockTestFixture</test></filter>", MockTestFixture.Tests)]
        //[TestCase("<filter><not><not><test>NUnit.TestData.Assemblies.MockTestFixture</test></not></not></filter>", MockTestFixture.Tests)]
        [TestCase("<filter><test>NUnit.TestData.Assemblies.MockTestFixture.IgnoreTest</test></filter>", 1)]
        //[TestCase("<filter><not><not><test>NUnit.TestData.Assemblies.MockTestFixture.IgnoreTest</test></not></not></filter>", 1)]
        [TestCase("<filter><class>NUnit.TestData.Assemblies.MockTestFixture</class></filter>", MockTestFixture.Tests)]
        [TestCase("<filter><name>IgnoreTest</name></filter>", 1)]
        [TestCase("<filter><name>MockTestFixture</name></filter>", MockTestFixture.Tests + NUnit.TestData.TestAssembly.MockTestFixture.Tests)]
        [TestCase("<filter><method>IgnoreTest</method></filter>", 1)]
        [TestCase("<filter><cat>FixtureCategory</cat></filter>", MockTestFixture.Tests)]
        //[TestCase("<filter><not><not><cat>FixtureCategory</cat></not></not></filter>", MockTestFixture.Tests)]
        public void UsingXml(string filter, int count)
        {
            Assert.That(_driver.CountTestCases(filter), Is.EqualTo(count));
        }

        [TestCase("NUnit.TestData.Assemblies.MockTestFixture", MockTestFixture.Tests, TestName = "{m}_MockTestFixture")]
        [TestCase("NUnit.TestData.Assemblies.MockTestFixture.IgnoreTest", 1, TestName = "{m}_MockTest4")]
        public void UsingTestFilterBuilderAddTest(string testName, int count)
        {
            var builder = new TestFilterBuilder();
            builder.AddTest(testName);

            Assert.That(_driver.CountTestCases(builder.GetFilter().Text), Is.EqualTo(count));
        }

        [TestCase("test==NUnit.TestData.Assemblies.MockTestFixture", MockTestFixture.Tests, TestName = "{m}_MockTestFixture")]
        [TestCase("test==NUnit.TestData.Assemblies.MockTestFixture.IgnoreTest", 1, TestName = "{m}_MockTest4")]
        [TestCase("class==NUnit.TestData.Assemblies.MockTestFixture", MockTestFixture.Tests)]
        [TestCase("name==IgnoreTest", 1)]
        [TestCase("name==MockTestFixture", MockTestFixture.Tests + NUnit.TestData.TestAssembly.MockTestFixture.Tests)]
        [TestCase("method==IgnoreTest", 1)]
        [TestCase("cat==FixtureCategory", MockTestFixture.Tests)]
        public void UsingTestFilterBuilderSelectWhere(string expression, int count)
        {
            var builder = new TestFilterBuilder();
            builder.SelectWhere(expression);

            Assert.That(_driver.CountTestCases(builder.GetFilter().Text), Is.EqualTo(count));
        }
    }
}
