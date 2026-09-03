// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using NUnit.Framework;
using System;
using System.IO;
using System.Security.Cryptography.X509Certificates;

namespace NUnit.Engine
{
    public class LoggingTests
    {
        private static readonly InternalTraceLevel[] LEVELS = [InternalTraceLevel.Error, InternalTraceLevel.Warning, InternalTraceLevel.Info, InternalTraceLevel.Debug];

        [Test, Combinatorial]
        public void LoggerSelectsMessagesToWrite(
            [ValueSource(nameof(LEVELS))] InternalTraceLevel logLevel,
            [ValueSource(nameof(LEVELS))] InternalTraceLevel msgLevel,
            [Values] bool echo)
        {
            var logWriter = new StringWriter();
            var logger = new Logger("MyLogger", logLevel, new InternalTraceWriter(logWriter), echo);

            Assert.That(logger.Name, Is.EqualTo("MyLogger"));
            Assert.That(logger.TraceLevel, Is.EqualTo(logLevel));
            Assert.That(logger.EchoToConsole, Is.EqualTo(echo));

            string msg = "This is my message";
            string logOutput = string.Empty;
            string consoleOutput = string.Empty;

            var originalOut = Console.Out;

            using (var consoleWriter = new StringWriter())
                try
                {
                    Console.SetOut(consoleWriter);
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

                    logOutput = logWriter.ToString();
                    consoleOutput = consoleWriter.ToString();
                }
                finally
                {
                    Console.SetOut(originalOut);
                }

            CheckOutput(logOutput, msgLevel);
            if (echo)
                CheckOutput(consoleOutput, msgLevel);
            else
                Assert.That(consoleOutput, Is.Empty);

            void CheckOutput(string output, InternalTraceLevel level)
            {
                if (logLevel >= level)
                {
                    Assert.That(output, Contains.Substring($" {level} "));
                    Assert.That(output, Does.EndWith($"MyLogger: {msg}" + System.Environment.NewLine));
                }
                else
                    Assert.That(output, Is.Empty);
            }
        }

        [Test]
        public void GetLoggerWithDefaultTraceLevel()
        {
            var logger = InternalTrace.GetLogger("MyLogger");
            Assert.That(logger.Name, Is.EqualTo("MyLogger"));
            Assert.That(logger.TraceLevel, Is.EqualTo(InternalTrace.DefaultTraceLevel));
            Assert.That(logger.EchoToConsole, Is.False);
        }

        [TestCaseSource(nameof(LEVELS))]
        public void GetLoggerWithSpecifiedTraceLevel(InternalTraceLevel level)
        {
            var logger = InternalTrace.GetLogger("MyLogger", level);
            Assert.That(logger.Name, Is.EqualTo("MyLogger"));
            Assert.That(logger.TraceLevel, Is.EqualTo(level));
            Assert.That(logger.EchoToConsole, Is.False);
        }
    }
}
