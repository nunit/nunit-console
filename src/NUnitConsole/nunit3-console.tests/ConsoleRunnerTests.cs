using System;
using System.IO;
using System.Xml;
using NUnit.Common;
using NUnit.Engine;
using NUnit.Engine.Extensibility;
using NUnit.Engine.Services;
using NUnit.Framework;

namespace NUnit.ConsoleRunner.Tests
{
    class ConsoleRunnerTests
    {
        [Test, Ignore("Needs to be rewritten")]
        public void ThrowsNUnitEngineExceptionWhenTestResultsAreNotWriteable()
        {
            var testEngine = new TestEngine();

            // This worked when we only needed one service. We now
            // would need to create three fakes. We should find a
            // better way to test this. Since it's a relatively
            // minor test, I'm leaving it for the future.
            testEngine.Services.Add(new FakeResultService());

            var consoleRunner = new ConsoleRunner(testEngine, new ConsoleOptions("mock-assembly.dll"), new ColorConsoleWriter());
            
            var ex = Assert.Throws<NUnitEngineException>(() => { consoleRunner.Execute(); });
            Assert.That(ex.Message, Is.EqualTo("The path specified in --result TestResult.xml could not be written to"));
        }
    }

    internal class FakeResultService : Service, IResultService
    {
        public string[] Formats
        {
            get
            {
                return new[] { "nunit3" };
            }
        }

        public IResultWriter GetResultWriter(string format, object[] args)
        {
            return new FakeResultWriter();
        }
    }

    internal class FakeResultWriter : IResultWriter
    {
        public void CheckWritability(string outputPath)
        {
            throw new UnauthorizedAccessException();
        }

        public void WriteResultFile(XmlNode resultNode, string outputPath)
        {
            throw new System.NotImplementedException();
        }

        public void WriteResultFile(XmlNode resultNode, TextWriter writer)
        {
            throw new System.NotImplementedException();
        }
    }
}
