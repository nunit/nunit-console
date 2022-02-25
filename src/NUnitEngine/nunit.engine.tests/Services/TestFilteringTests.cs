// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using System;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using NUnit.TestData.Assemblies;

namespace NUnit.Engine.Services
{
    using Drivers;

    public class TestFilteringTests
    {
        private const string MOCK_ASSEMBLY = "mock-assembly.dll";

#if NETCOREAPP3_1_OR_GREATER
        private NUnitNetCore31Driver _driver;
#elif NETCOREAPP2_1
        private NUnitNetStandardDriver _driver;
#else
        private NUnit3FrameworkDriver _driver;
#endif

        [SetUp]
        public void LoadAssembly()
        {
            var mockAssemblyPath = System.IO.Path.Combine(TestContext.CurrentContext.TestDirectory, MOCK_ASSEMBLY);
#if NETCOREAPP3_1_OR_GREATER
            _driver = new NUnitNetCore31Driver();
#elif NETCOREAPP2_1
            _driver = new NUnitNetStandardDriver();
#else
            var assemblyName = typeof(NUnit.Framework.TestAttribute).Assembly.GetName();
            _driver = new NUnit3FrameworkDriver(AppDomain.CurrentDomain, assemblyName);
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
