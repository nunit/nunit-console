// ***********************************************************************
// Copyright (c) 2011 Charlie Poole, Rob Prouse
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

#if !NETCOREAPP1_1
using System.IO;
using System.Xml;
using NUnit.Engine.Runners;
using NUnit.Engine.Services.Tests.Fakes;
using NUnit.Framework;

namespace NUnit.Engine.Services.ResultWriters.Tests
{
    public class XmlTransformResultWriterTests
    {
        private XmlNode _engineResult;
        private const string AssemblyName = "mock-assembly.dll";

        [SetUp]
        public void SetUp()
        {
            var assemblyPath = GetLocalPath(AssemblyName);

            var serviceContext = new ServiceContext();
            serviceContext.Add(new DriverService());
            serviceContext.Add(new InProcessTestRunnerFactory());
            serviceContext.Add(new ExtensionService());
            serviceContext.Add(new FakeRuntimeService());
#if NETFRAMEWORK
            serviceContext.Add(new DomainManager());
#endif

            var runner = new MasterTestRunner(serviceContext, new TestPackage(assemblyPath));
            runner.Load();
            _engineResult = runner.Run(null, TestFilter.Empty);
        }


        [Test]
        public void SummaryTransformTest()
        {
            var transformPath = GetLocalPath("TextSummary.xslt");
            StringWriter writer = new StringWriter();
            new XmlTransformResultWriter(new object[] { transformPath }).WriteResultFile(_engineResult, writer);

            string summary = string.Format(
                "Tests Run: {0}, Passed: {1}, Failed: {2}, Inconclusive: {3}, Skipped: {4}",
                _engineResult.Attributes["total"].Value,
                _engineResult.Attributes["passed"].Value,
                _engineResult.Attributes["failed"].Value,
                _engineResult.Attributes["inconclusive"].Value,
                _engineResult.Attributes["skipped"].Value);

            string output = writer.GetStringBuilder().ToString();

            Assert.That(output, Contains.Substring(summary));
        }

        [Test]
        public void XmlTransformResultWriterIgnoresDTDs()
        {
            var transformPath = GetLocalPath("TransformWithDTD.xslt");
            Assert.DoesNotThrow(() => new XmlTransformResultWriter(new object[] { transformPath }));
        }

        private static string GetLocalPath(string fileName)
        {
            return Path.Combine(TestContext.CurrentContext.TestDirectory, fileName);
        }
    }
}
#endif