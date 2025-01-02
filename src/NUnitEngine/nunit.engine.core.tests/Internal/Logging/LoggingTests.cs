﻿// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using System.IO;
using NUnit.Framework;

namespace NUnit.Engine.Internal.Logging
{
    public class LoggingTests
    {
        static readonly InternalTraceLevel[] LEVELS = new [] { InternalTraceLevel.Error, InternalTraceLevel.Warning, InternalTraceLevel.Info, InternalTraceLevel.Debug };

        [Test, Combinatorial]
        public void LoggerSelectsMessagesToWrite(
            [ValueSource(nameof(LEVELS))] InternalTraceLevel logLevel,
            [ValueSource(nameof(LEVELS))] InternalTraceLevel msgLevel)
        {
            var writer = new StringWriter();
            var logger = new Logger("MyLogger", () => logLevel, () => writer);

            Assert.That(logger.TraceLevel, Is.EqualTo(logLevel));

            var msg = "This is my message";

            switch (msgLevel)
            {
                case InternalTraceLevel.Error:
                    logger.Error(msg);
                    break;
                case InternalTraceLevel.Warning:
                    logger.Warning(msg);
                    break;
                case InternalTraceLevel.Info:
                    logger.Info(msg);
                    break;
                case InternalTraceLevel.Debug:
                    logger.Debug(msg);
                    break;
            }

            var output = writer.ToString();

            if (logLevel >= msgLevel)
            {
                Assert.That(output, Contains.Substring($" {msgLevel} "));
                Assert.That(output, Does.EndWith($"MyLogger: {msg}" + System.Environment.NewLine));
            }
            else
                Assert.That(output, Is.Empty);
        }

        [Test]
        public void GetLoggerWithDefaultTraceLevel()
        {
            var logger = InternalTrace.GetLogger("MyLogger");
            Assert.That(logger.TraceLevel, Is.EqualTo(InternalTrace.DefaultTraceLevel));
        }

        [TestCaseSource(nameof(LEVELS))]
        public void GetLoggerWithSpecifiedTraceLevel(InternalTraceLevel level)
        {
            var logger = InternalTrace.GetLogger("MyLogger", level);
            Assert.That(logger.TraceLevel, Is.EqualTo(level));
        }
    }
}
