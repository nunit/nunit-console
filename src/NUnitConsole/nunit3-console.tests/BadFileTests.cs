// ***********************************************************************
// Copyright (c) 2018 Charlie Poole, Rob Prouse
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
// ***********************************************************************

using System;
using System.IO;
using System.Text;
using NUnit.Common;
using NUnit.Engine;
using NUnit.Engine.Runners;
using NUnit.Engine.Services;
using NUnit.Framework;

namespace NUnit.ConsoleRunner.Tests
{
    public class BadFileTests : ITestEventListener
    {
        [TestCase("junk.dll", "File not found")]
        [TestCase("EngineTests.nunit", "File type is not supported")]
        public void MissingFileTest(string filename, string message)
        {
            var fullname = Path.Combine(TestContext.CurrentContext.TestDirectory, filename);

            var services = new ServiceContext();
            services.Add(new InProcessTestRunnerFactory());
            services.Add(new ExtensionService());
            services.Add(new RuntimeFrameworkService());
            services.Add(new DriverService());

            var package = new TestPackage(fullname);
            package.AddSetting("ProcessModel", "InProcess");
            package.AddSetting("DomainUsage", "None");

            var runner = new MasterTestRunner(services, package);

            var result = runner.Run(this, TestFilter.Empty);
            var sb = new StringBuilder();
            var writer = new ExtendedTextWrapper(new StringWriter(sb));
            var reporter = new ResultReporter(result, writer, new ConsoleOptions());

            reporter.WriteErrorsFailuresAndWarningsReport();
            var report = sb.ToString();

            Assert.That(report, Contains.Substring($"1) Invalid : {fullname}"));
            Assert.That(report, Contains.Substring(message));
        }

        public void OnTestEvent(string report)
        {
        }
    }
}
