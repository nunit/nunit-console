// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using NUnit.Framework;
using NUnit.Framework.Internal;

namespace NUnit.Engine.Runners
{
    public abstract class NotRunnableTestRunnerTests
    {
        protected const string PACKAGE_ID = "99";
        protected const string EXPECTED_TEST_ID = "99-1";

        protected string? _expectedRunState;
        protected string? _expectedReason;
        protected string? _expectedResult;
        protected string? _expectedLabel;

        [TestCase("junk.dll", "Assembly")]
        [TestCase("junk.exe", "Assembly")]
        [TestCase("junk.cfg", "Unknown")]
        public void Load(string filePath, string expectedType)
        {
            ITestEngineRunner runner = CreateRunner(filePath);
            CheckLoadResult(runner.Load(), filePath, expectedType);
        }

        [TestCase("junk.dll", "Assembly")]
        [TestCase("junk.exe", "Assembly")]
        [TestCase("junk.cfg", "Unknown")]
        public void Explore(string filePath, string expectedType)
        {
            ITestEngineRunner runner = CreateRunner(filePath);
            CheckLoadResult(runner.Explore(TestFilter.Empty), filePath, expectedType);
        }

        [TestCase("junk.dll")]
        [TestCase("junk.exe")]
        [TestCase("junk.cfg")]
        public void CountTestCases(string filePath)
        {
            ITestEngineRunner runner = CreateRunner(filePath);
            Assert.That(runner.CountTestCases(TestFilter.Empty), Is.EqualTo(0));
        }

        [TestCase("junk.dll", "Assembly")]
        [TestCase("junk.exe", "Assembly")]
        [TestCase("junk.cfg", "Unknown")]
        public void Run(string filePath, string expectedType)
        {
            ITestEngineRunner runner = CreateRunner(filePath);
            var events = new EventListener();
            CheckRunResult(runner.Run(events, TestFilter.Empty), filePath, expectedType);
        }

        [TestCase("junk.dll", "Assembly")]
        [TestCase("junk.exe", "Assembly")]
        [TestCase("junk.cfg", "Unknown")]
        public void RunAsync(string filePath, string expectedType)
        {
            ITestEngineRunner runner = CreateRunner(filePath);
            var events = new EventListener();
            runner.RunAsync(events, TestFilter.Empty);
            Assert.That(events.Count, Is.EqualTo(1));
            CheckRunResult(new TestEngineResult(events[0]), filePath, expectedType);
        }

        private void CheckLoadResult(TestEngineResult result, string filePath, string expectedType)
        {
            var node = result.XmlNodes.First();
            Assert.That(node.Name, Is.EqualTo("test-suite"));
            Assert.That(node.GetAttribute("type"), Is.EqualTo(expectedType));
            Assert.That(node.GetAttribute("id"), Is.EqualTo(EXPECTED_TEST_ID));
            Assert.That(node.GetAttribute("name"), Is.EqualTo(filePath));
            Assert.That(node.GetAttribute("fullname"), Is.EqualTo(Path.GetFullPath(filePath)));
            Assert.That(node.GetAttribute("runstate"), Is.EqualTo(_expectedRunState));
            Assert.That(node.GetAttribute("testcasecount"), Is.EqualTo("0"));
            Assert.That(GetSkipReason(node), Is.EqualTo(_expectedReason));
            Assert.That(node.SelectNodes("test-suite")?.Count, Is.EqualTo(0), "Load result should not have child tests");
        }

        private void CheckRunResult(TestEngineResult result, string filePath, string expectedType)
        {
            var node = result.XmlNodes.First();
            Assert.That(node.Name, Is.EqualTo("test-suite"));
            Assert.That(node.GetAttribute("id"), Is.EqualTo(EXPECTED_TEST_ID));
            Assert.That(node.GetAttribute("name"), Is.EqualTo(filePath));
            Assert.That(node.GetAttribute("fullname"), Is.EqualTo(Path.GetFullPath(filePath)));
            Assert.That(node.GetAttribute("type"), Is.EqualTo(expectedType));
            Assert.That(node.GetAttribute("runstate"), Is.EqualTo(_expectedRunState));
            Assert.That(node.GetAttribute("testcasecount"), Is.EqualTo("0"));
            Assert.That(GetSkipReason(node), Is.EqualTo(_expectedReason));
            Assert.That(node.SelectNodes("test-suite")?.Count, Is.EqualTo(0), "Load result should not have child tests");
            Assert.That(node.GetAttribute("result"), Is.EqualTo(_expectedResult));
            Assert.That(node.GetAttribute("label"), Is.EqualTo(_expectedLabel));
            Assert.That(node.SelectSingleNode("reason/message")?.InnerText, Is.EqualTo(_expectedReason));
        }

        protected abstract ITestEngineRunner CreateRunner(string filePath);

        private class EventListener : List<string>, ITestEventListener
        {
            public void OnTestEvent(string report)
            {
                Add(report);
            }
        }

        private static string? GetSkipReason(XmlNode result)
        {
            var propNode = result.SelectSingleNode(string.Format("properties/property[@name='{0}']", PropertyNames.SkipReason));
            return propNode is null ? null : propNode.GetAttribute("value");
        }
    }

    public class InvalidAssemblyTestRunnerTests : NotRunnableTestRunnerTests
    {
        public InvalidAssemblyTestRunnerTests()
        {
            _expectedRunState = "NotRunnable";
            _expectedReason = "Assembly could not be found";
            _expectedResult = "Failed";
            _expectedLabel = "Invalid";
        }

        protected override ITestEngineRunner CreateRunner(string filePath)
        {
            return new InvalidAssemblyTestRunner(filePath, _expectedReason ?? "Not Specified")
            {
                ID = PACKAGE_ID
            };
        }
    }

    public class UnmanagedExecutableTestRunnerTests : NotRunnableTestRunnerTests
    {
        public UnmanagedExecutableTestRunnerTests()
        {
            _expectedRunState = "NotRunnable";
            _expectedReason = "Unmanaged libraries or applications are not supported";
            _expectedResult = "Failed";
            _expectedLabel = "Invalid";
        }

        protected override ITestEngineRunner CreateRunner(string filePath)
        {
            return new UnmanagedExecutableTestRunner(filePath)
            {
                ID = PACKAGE_ID
            };
        }
    }

    public class SkippedAssemblyTestRunnerTests : NotRunnableTestRunnerTests
    {
        public SkippedAssemblyTestRunnerTests()
        {
            _expectedRunState = "Runnable";
            _expectedReason = "Skipping non-test assembly";
            _expectedResult = "Skipped";
            _expectedLabel = "NoTests";
        }

        protected override ITestEngineRunner CreateRunner(string filePath)
        {
            return new SkippedAssemblyTestRunner(filePath)
            {
                ID = PACKAGE_ID
            };
        }
    }
}
