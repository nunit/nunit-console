// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using System;
using NUnit.Engine;
using NUnit.Framework;

namespace NUnit.Agents
{
    public class AgentOptionTests
    {
        static TestCaseData[] DefaultSettings = new[]
        {
            new TestCaseData("AgentId", Guid.Empty),
            new TestCaseData("AgencyUrl", string.Empty),
            new TestCaseData("AgencyPid", string.Empty),
            new TestCaseData("DebugAgent", false),
            new TestCaseData("DebugTests", false),
            new TestCaseData("TraceLevel", InternalTraceLevel.Off),
            new TestCaseData("WorkDirectory", string.Empty)
        };

        [TestCaseSource(nameof(DefaultSettings))]
        public void DefaultOptionSettings<T>(string propertyName, T defaultValue)
        {
            var options = new AgentOptions();
            var prop = typeof(AgentOptions).GetProperty(propertyName);
            Assert.That(prop, Is.Not.Null, $"Property {propertyName} does not exist");
            Assert.That(prop.GetValue(options, new object[0]), Is.EqualTo(defaultValue));
        }

        static readonly Guid AGENT_GUID = Guid.NewGuid();
        static readonly TestCaseData[] ValidSettings = new[]
        {
            // Boolean options - no values provided
            new TestCaseData("--debug-agent", "DebugAgent", true),
            new TestCaseData("--debug-tests", "DebugTests", true),
            // Options with values - using '=' as delimiter
            new TestCaseData($"--agentId={AGENT_GUID}", "AgentId", AGENT_GUID),
            new TestCaseData("--agencyUrl=THEURL", "AgencyUrl", "THEURL"),
            new TestCaseData("--pid=1234", "AgencyPid", "1234"),
            new TestCaseData("--trace=Info", "TraceLevel", InternalTraceLevel.Info),
            new TestCaseData("--work=WORKDIR", "WorkDirectory", "WORKDIR"),
            // Options with values - using ':' as delimiter
            new TestCaseData("--trace:Error", "TraceLevel", InternalTraceLevel.Error),
            new TestCaseData("--work:WORKDIR", "WorkDirectory", "WORKDIR"),
            // Value with spaces (provided OS passes them through)
            new TestCaseData("--work:MY WORK DIR", "WorkDirectory", "MY WORK DIR"),
        };

        [TestCaseSource(nameof(ValidSettings))]
        public void ValidOptionSettings<T>(string option, string propertyName, T expectedValue)
        {
            var options = new AgentOptions(option);
            var prop = typeof(AgentOptions).GetProperty(propertyName);
            Assert.That(prop, Is.Not.Null, $"Property {propertyName} does not exist");
            Assert.That(prop.GetValue(options, new object[0]), Is.EqualTo(expectedValue));
        }

        [Test]
        public void MultipleOptions()
        {
            var options = new AgentOptions("--debug-tests", "--trace=Info", "--work", "MYWORKDIR");
            Assert.That(options.DebugAgent, Is.False);
            Assert.That(options.DebugTests);
            Assert.That(options.TraceLevel, Is.EqualTo(InternalTraceLevel.Info));
            Assert.That(options.WorkDirectory, Is.EqualTo("MYWORKDIR"));
        }

        [Test]
        public void FileNameSupplied()
        {
            var filename = GetType().Assembly.Location;
            var options = new AgentOptions(filename);
            Assert.That(options.Files.Count, Is.EqualTo(1));
            Assert.That(options.Files[0], Is.EqualTo(filename));
        }
    }
}
