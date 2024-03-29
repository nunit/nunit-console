﻿// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using System.IO;
using System.Xml;
using NUnit.Engine.Runners;
using NUnit.Engine.Services;
using NUnit.Framework;

namespace NUnit.Engine.Services.ResultWriters
{
    [Ignore("Temporarily ignoring this fixture")]
    public class XmlTransformResultWriterTests
    {
        private XmlNode _engineResult;
        private const string AssemblyName = "mock-assembly.dll";

        [SetUp]
        public void SetUp()
        {
            var assemblyPath = GetLocalPath(AssemblyName);

            var serviceContext = new ServiceContext();
            serviceContext.Add(new TestRunnerFactory());
#if NETFRAMEWORK
            serviceContext.Add(new TestAgency());
#endif
            serviceContext.Add(new ExtensionService());
            serviceContext.Add(new FakeRuntimeService());

            var package = new TestPackage(assemblyPath);
            using (var runner = new MasterTestRunner(serviceContext, package))
            {
                runner.Load();
                _engineResult = runner.Run(null, TestFilter.Empty);
            }
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
