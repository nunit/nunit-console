// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using System;
using System.IO;
using System.Xml;
using NSubstitute;
using NUnit.ConsoleRunner.Options;
using NUnit.Engine;
using NUnit.Engine.Extensibility;
using NUnit.Engine.Services;
using NUnit.Framework;

namespace NUnit.ConsoleRunner
{
    class ConsoleRunnerTests
    {
        [Test]
        public void ThrowsNUnitEngineExceptionWhenTestResultsAreNotWriteable()
        {
            using (var testEngine = new TestEngine())
            {
                testEngine.Services.Add(new FakeResultService());
                testEngine.Services.Add(new TestFilterService());
                testEngine.Services.Add(Substitute.For<IService, IExtensionService>());

                var consoleRunner = new ConsoleRunner(testEngine, ConsoleMocks.Options("mock-assembly.dll"), new ColorConsoleWriter());

                var ex = Assert.Throws<NUnitEngineException>(() => { consoleRunner.Execute(); });
                Assert.That(ex, Has.Message.EqualTo("The path specified in --result TestResult.xml could not be written to"));
            }
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
