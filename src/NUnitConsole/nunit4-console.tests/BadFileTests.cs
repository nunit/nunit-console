// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using System.IO;
using System.Text;
using NUnit.ConsoleRunner.Options;
using NUnit.Engine;
using NUnit.Engine.Runners;
using NUnit.Engine.Services;
using NUnit.Framework;
using NUnit.TextDisplay;

namespace NUnit.ConsoleRunner
{
    [Ignore("Temporarily ignoring this fixture")]
    public class BadFileTests : ITestEventListener
    {
        [TestCase("junk.dll", "File not found")]
        [TestCase("ConsoleTests.nunit", "File type is not supported")]
        public void MissingFileTest(string filename, string message)
        {
            var fullname = Path.Combine(TestContext.CurrentContext.TestDirectory, filename);

            var services = new ServiceContext();
            services.Add(new TestRunnerFactory());
            services.Add(new ExtensionService());
#if NETFRAMEWORK
            services.Add(new RuntimeFrameworkService());
            services.Add(new TestAgency());
#endif

            var package = new TestPackage(fullname);
            package.AddSetting(new PackageSetting<string>("ProcessModel", "InProcess"));

            var runner = new MasterTestRunner(services, package);

            var result = runner.Run(this, TestFilter.Empty);
            var sb = new StringBuilder();
            var writer = new ExtendedTextWrapper(new StringWriter(sb));

            ResultReporter.WriteErrorsFailuresAndWarningsReport(result, writer);
            var report = sb.ToString();

            Assert.That(report, Contains.Substring($"1) Invalid : {fullname}"));
            Assert.That(report, Contains.Substring(message));
        }

        public void OnTestEvent(string report)
        {
        }
    }
}
