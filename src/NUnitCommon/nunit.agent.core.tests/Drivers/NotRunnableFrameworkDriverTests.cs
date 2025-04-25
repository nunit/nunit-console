﻿// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using System.Collections.Generic;
using System.IO;
using System.Xml;
using NUnit.Framework;
using NUnit.Framework.Internal;
using NUnit.Engine.Extensibility;
using System;

namespace NUnit.Engine.Drivers
{
    // Functional tests of the NUnitFrameworkDriver calling into the framework.
    public abstract class NotRunnableFrameworkDriverTests
    {
        private const string EXPECTED_ID = "99-1";

        protected string? _expectedRunState;
        protected string? _expectedReason;
        protected string? _expectedResult;
        protected string? _expectedLabel;

        [TestCase("junk.dll", "Assembly")]
        [TestCase("junk.exe", "Assembly")]
        [TestCase("junk.cfg", "Unknown")]
        public void Load(string filePath, string expectedType)
        {
            IFrameworkDriver driver = GetDriver(filePath);
            var result = XmlHelper.CreateXmlNode(driver.Load(filePath, new Dictionary<string, object>()));

            Assert.That(result.Name, Is.EqualTo("test-suite"));
            Assert.That(result.GetAttribute("type"), Is.EqualTo(expectedType));
            Assert.That(result.GetAttribute("id"), Is.EqualTo(EXPECTED_ID));
            Assert.That(result.GetAttribute("name"), Is.EqualTo(filePath));
            Assert.That(result.GetAttribute("fullname"), Is.EqualTo(Path.GetFullPath(filePath)));
            Assert.That(result.GetAttribute("runstate"), Is.EqualTo(_expectedRunState));
            Assert.That(result.GetAttribute("testcasecount"), Is.EqualTo("0"));
            Assert.That(GetSkipReason(result), Is.EqualTo(_expectedReason));
            Assert.That(result.SelectNodes("test-suite")?.Count, Is.EqualTo(0), "Load result should not have child tests");
        }

        [TestCase("junk.dll", "Assembly")]
        [TestCase("junk.exe", "Assembly")]
        [TestCase("junk.cfg", "Unknown")]
        public void Explore(string filePath, string expectedType)
        {
            IFrameworkDriver driver = GetDriver(filePath);
            var result = XmlHelper.CreateXmlNode(driver.Explore(TestFilter.Empty.Text));

            Assert.That(result.Name, Is.EqualTo("test-suite"));
            Assert.That(result.GetAttribute("id"), Is.EqualTo(EXPECTED_ID));
            Assert.That(result.GetAttribute("name"), Is.EqualTo(filePath));
            Assert.That(result.GetAttribute("fullname"), Is.EqualTo(Path.GetFullPath(filePath)));
            Assert.That(result.GetAttribute("type"), Is.EqualTo(expectedType));
            Assert.That(result.GetAttribute("runstate"), Is.EqualTo(_expectedRunState));
            Assert.That(result.GetAttribute("testcasecount"), Is.EqualTo("0"));
            Assert.That(GetSkipReason(result), Is.EqualTo(_expectedReason));
            Assert.That(result.SelectNodes("test-suite")?.Count, Is.EqualTo(0), "Result should not have child tests");
        }

        [TestCase("junk.dll")]
        [TestCase("junk.exe")]
        [TestCase("junk.cfg")]
        public void CountTestCases(string filePath)
        {
            IFrameworkDriver driver = CreateDriver(filePath);
            Assert.That(driver.CountTestCases(TestFilter.Empty.Text), Is.EqualTo(0));
        }

        [TestCase("junk.dll", "Assembly")]
        [TestCase("junk.exe", "Assembly")]
        [TestCase("junk.cfg", "Unknown")]
        public void Run(string filePath, string expectedType)
        {
            IFrameworkDriver driver = GetDriver(filePath);
            var result = XmlHelper.CreateXmlNode(driver.Run(new NullListener(), TestFilter.Empty.Text));
            Assert.That(result.Name, Is.EqualTo("test-suite"));
            Assert.That(result.GetAttribute("id"), Is.EqualTo(EXPECTED_ID));
            Assert.That(result.GetAttribute("name"), Is.EqualTo(filePath));
            Assert.That(result.GetAttribute("fullname"), Is.EqualTo(Path.GetFullPath(filePath)));
            Assert.That(result.GetAttribute("type"), Is.EqualTo(expectedType));
            Assert.That(result.GetAttribute("runstate"), Is.EqualTo(_expectedRunState));
            Assert.That(result.GetAttribute("testcasecount"), Is.EqualTo("0"));
            Assert.That(GetSkipReason(result), Is.EqualTo(_expectedReason));
            Assert.That(result.SelectNodes("test-suite")?.Count, Is.EqualTo(0), "Load result should not have child tests");
            Assert.That(result.GetAttribute("result"), Is.EqualTo(_expectedResult));
            Assert.That(result.GetAttribute("label"), Is.EqualTo(_expectedLabel));
            Assert.That(result.SelectSingleNode("reason/message")?.InnerText, Is.EqualTo(_expectedReason));
        }

        protected abstract IFrameworkDriver CreateDriver(string filePath);

        private IFrameworkDriver GetDriver(string filePath)
        {
            IFrameworkDriver driver = CreateDriver(filePath);
            return driver;
        }

        private static string? GetSkipReason(XmlNode result)
        {
            var propNode = result.SelectSingleNode(string.Format("properties/property[@name='{0}']", PropertyNames.SkipReason));
            return propNode is null ? null : propNode.GetAttribute("value");
        }

        private class NullListener : ITestEventListener
        {
            public void OnTestEvent(string testEvent)
            {
                // No action
            }
        }
    }

    public class InvalidAssemblyFrameworkDriverTests : NotRunnableFrameworkDriverTests
    {
        public InvalidAssemblyFrameworkDriverTests()
        {
            _expectedRunState = "NotRunnable";
            _expectedReason = "Assembly could not be found";
            _expectedResult = "Failed";
            _expectedLabel = "Invalid";
        }

        protected override IFrameworkDriver CreateDriver(string filePath)
        {
            return new InvalidAssemblyFrameworkDriver(filePath, "99", _expectedReason ?? "Not Specified");
        }
    }

    public class SkippedAssemblyFrameworkDriverTests : NotRunnableFrameworkDriverTests
    {
        public SkippedAssemblyFrameworkDriverTests()
        {
            _expectedRunState = "Runnable";
            _expectedReason = "Skipping non-test assembly";
            _expectedResult = "Skipped";
            _expectedLabel = "NoTests";
        }

        protected override IFrameworkDriver CreateDriver(string filePath)
        {
            return new SkippedAssemblyFrameworkDriver(filePath, "99");
        }
    }
}
