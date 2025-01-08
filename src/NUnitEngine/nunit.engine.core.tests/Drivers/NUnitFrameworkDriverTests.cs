// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Xml;
using NUnit.TestData.Assemblies;
using NUnit.Framework;
using NUnit.Framework.Internal;
using NUnit.Engine.Internal;

namespace NUnit.Engine.Drivers
{
    // Functional tests of the NUnitFrameworkDriver calling into the framework.
    public class NUnitFrameworkDriverTests
    {
        private const string MOCK_ASSEMBLY = "mock-assembly.dll";
        private const string LOAD_MESSAGE = "Method called without calling Load first. Possible error in runner.";

        private IDictionary<string, object> _settings = new Dictionary<string, object>();

        private NUnitFrameworkDriver _driver;
        private string _mockAssemblyPath;

        [SetUp]
        public void CreateDriver()
        {
            var nunitRef = typeof(NUnit.Framework.TestAttribute).Assembly.GetName();
            _mockAssemblyPath = System.IO.Path.Combine(TestContext.CurrentContext.TestDirectory, MOCK_ASSEMBLY);
#if NETFRAMEWORK
            _driver = new NUnitFrameworkDriver(AppDomain.CurrentDomain, nunitRef);
#else
            _driver = new NUnitFrameworkDriver(nunitRef);
#endif
        }

        [Test]
        public void Load_GoodFile_ReturnsRunnableSuite()
        {
            var result = XmlHelper.CreateXmlNode(_driver.Load(_mockAssemblyPath, _settings));

            Assert.That(result.Name, Is.EqualTo("test-suite"));
            Assert.That(result.GetAttribute("type"), Is.EqualTo("Assembly"));
            Assert.That(result.GetAttribute("runstate"), Is.EqualTo("Runnable"));
            Assert.That(result.GetAttribute("testcasecount"), Is.EqualTo(MockAssembly.Tests.ToString()));
            Assert.That(result.SelectNodes("test-suite")?.Count, Is.EqualTo(0), "Load result should not have child tests");
        }

        [Test]
        public void Explore_AfterLoad_ReturnsRunnableSuite()
        {
            _driver.Load(_mockAssemblyPath, _settings);
            var result = XmlHelper.CreateXmlNode(_driver.Explore(TestFilter.Empty.Text));

            Assert.That(result.Name, Is.EqualTo("test-suite"));
            Assert.That(result.GetAttribute("type"), Is.EqualTo("Assembly"));
            Assert.That(result.GetAttribute("runstate"), Is.EqualTo("Runnable"));
            Assert.That(result.GetAttribute("testcasecount"), Is.EqualTo(MockAssembly.Tests.ToString()));
            Assert.That(result.SelectNodes("test-suite")?.Count, Is.GreaterThan(0), "Explore result should have child tests");
        }

        [Test]
        public void ExploreTestsAction_WithoutLoad_ThrowsInvalidOperationException()
        {
            var ex = Assert.Catch(() => _driver.Explore(TestFilter.Empty.Text));
            if (ex is System.Reflection.TargetInvocationException)
                ex = ex.InnerException;
            Assert.That(ex, Is.TypeOf<InvalidOperationException>());
            Assert.That(ex.Message, Is.EqualTo(LOAD_MESSAGE));
        }

        [Test]
        public void CountTestsAction_AfterLoad_ReturnsCorrectCount()
        {
            _driver.Load(_mockAssemblyPath, _settings);
            Assert.That(_driver.CountTestCases(TestFilter.Empty.Text), Is.EqualTo(MockAssembly.Tests));
        }

        [Test]
        public void CountTestsAction_WithoutLoad_ThrowsInvalidOperationException()
        {
            var ex = Assert.Catch(() => _driver.CountTestCases(TestFilter.Empty.Text));
            if (ex is System.Reflection.TargetInvocationException)
                ex = ex.InnerException;
            Assert.That(ex, Is.TypeOf<InvalidOperationException>());
            Assert.That(ex.Message, Is.EqualTo(LOAD_MESSAGE));
        }

        [Test]
        public void RunTestsAction_AfterLoad_ReturnsRunnableSuite()
        {
            _driver.Load(_mockAssemblyPath, _settings);
            var result = XmlHelper.CreateXmlNode(_driver.Run(new NullListener(), TestFilter.Empty.Text));

            Assert.That(result.Name, Is.EqualTo("test-suite"));
            Assert.That(result.GetAttribute("type"), Is.EqualTo("Assembly"));
            Assert.That(result.GetAttribute("runstate"), Is.EqualTo("Runnable"));
            Assert.That(result.GetAttribute("testcasecount"), Is.EqualTo(MockAssembly.Tests.ToString()));
            Assert.That(result.GetAttribute("result"), Is.EqualTo("Failed"));
            Assert.That(result.GetAttribute("passed"), Is.EqualTo(MockAssembly.PassedInAttribute.ToString()));
            Assert.That(result.GetAttribute("failed"), Is.EqualTo(MockAssembly.Failed.ToString()));
            Assert.That(result.GetAttribute("skipped"), Is.EqualTo(MockAssembly.Skipped.ToString()));
            Assert.That(result.GetAttribute("inconclusive"), Is.EqualTo(MockAssembly.Inconclusive.ToString()));
            Assert.That(result.SelectNodes("test-suite")?.Count, Is.GreaterThan(0), "Explore result should have child tests");
        }

        [Test]
        public void RunTestsAction_WithoutLoad_ThrowsInvalidOperationException()
        {
            var ex = Assert.Catch(() => _driver.Run(new NullListener(), TestFilter.Empty.Text));
            Assert.That(ex, Is.TypeOf<InvalidOperationException>());
            Assert.That(ex.Message, Is.EqualTo(LOAD_MESSAGE));
        }

#if NETFRAMEWORK
        //[Test]
        public void RunTestsAction_WithInvalidFilterElement_ThrowsNUnitEngineException()
        {
            _driver.Load(_mockAssemblyPath, _settings);

            var invalidFilter = "<filter><invalidElement>foo</invalidElement></filter>";
            var ex = Assert.Catch(() => _driver.Run(new NullListener(), invalidFilter));
            Assert.That(ex, Is.TypeOf<NUnitEngineException>());
        }

        private class CallbackEventHandler : System.Web.UI.ICallbackEventHandler
        {
            private string? _result;

            public string? GetCallbackResult()
            {
                return _result;
            }

            public void RaiseCallbackEvent(string eventArgument)
            {
                _result = eventArgument;
            }
        }
#endif

        public class NullListener : ITestEventListener
        {
            public void OnTestEvent(string testEvent)
            {
                // No action
            }
        }
    }
}
