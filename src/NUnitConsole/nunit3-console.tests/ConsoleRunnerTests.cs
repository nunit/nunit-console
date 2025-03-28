﻿// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using NSubstitute;
using NUnit.Common;
using NUnit.Engine;
using NUnit.Engine.Extensibility;
using NUnit.Engine.Services;
using NUnit.Framework;

namespace NUnit.ConsoleRunner.Tests
{
    class ConsoleRunnerTests
    {
        private ITestEngine _testEngine;
        private IResultService _resultService;

        [SetUp]
        public void Setup()
        {
            _testEngine = Substitute.For<ITestEngine>();
            _resultService = new FakeResultService();

            _testEngine.Services.GetService<IResultService>().Returns(_resultService);
        }

        [Test]
        public void ThrowsNUnitEngineExceptionWhenTestResultsAreNotWriteable()
        {
            ((FakeResultService)_resultService).ThrowsUnauthorizedAccessException = true;

            var consoleRunner = new ConsoleRunner(_testEngine, ConsoleMocks.Options("mock-assembly.dll"), new ColorConsoleWriter());

            var ex = Assert.Throws<NUnitEngineException>(() => { consoleRunner.Execute(); });
            Assert.That(ex, Has.Message.EqualTo("The path specified in --result TestResult.xml could not be written to"));
        }

        [Test]
        public void ThrowsRequiredExtensionExceptionWhenTeamcityOptionIsSpecifiedButNotAvailable()
        {
            var ex = Assert.Throws<RequiredExtensionException>(
                () => new ConsoleRunner(_testEngine, ConsoleMocks.Options("mock-assembly.dll", "--teamcity"), new ColorConsoleWriter()));

            Assert.That(ex, Has.Message.EqualTo("Required extension 'NUnit.Engine.Listeners.TeamCityEventListener' is not installed."));
        }

        [Test]
        public void ThrowsRequiredExtensionExceptionWhenEnableOptionSpecifiesUnavailableExtension()
        {
            var ex = Assert.Throws<RequiredExtensionException>(
                () => new ConsoleRunner(_testEngine, ConsoleMocks.Options("mock-assembly.dll", "--enable:Not.An.Extension"), new ColorConsoleWriter()));

            Assert.That(ex, Has.Message.EqualTo("Required extension 'Not.An.Extension' is not installed."));
        }
    }

    internal class FakeResultService : Service, IResultService
    {
        public bool ThrowsUnauthorizedAccessException;

        public string[] Formats
        {
            get
            {
                return new[] { "nunit3" };
            }
        }

        public IResultWriter GetResultWriter(string format, object[] args)
        {
            return new FakeResultWriter(this);
        }

        class FakeResultWriter : IResultWriter
        {
            private FakeResultService _service;

            public FakeResultWriter(FakeResultService service)
            {
                _service = service;
            }

            public void CheckWritability(string outputPath)
            {
                if (_service.ThrowsUnauthorizedAccessException)
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
}
