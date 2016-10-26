using System;
using NUnit.Common;
using NUnit.Engine;
using NUnit.Framework;

namespace NUnit.ConsoleRunner.Tests
{
    class ConsoleRunnerTests
    {
        [Test]
        public void ThrowsExceptionWhenWorkingDirectoryIsUnAuthorized()
        {
            var options = new ConsoleOptions("tests.dll","-result=results.xml",@"--work=c:\windows\");
            var engine = TestEngineActivator.CreateInstance();
            var consoleRunner = new ConsoleRunner(engine, options, new ColorConsoleWriter());

            var exception = Assert.Throws<UnauthorizedAccessException>(()=> { consoleRunner.Execute(); });
            Assert.That(exception.Message, Is.EqualTo("Error: Unauthorized working directory"));
        }
    }
}
