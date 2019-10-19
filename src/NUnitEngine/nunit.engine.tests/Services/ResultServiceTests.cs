// ***********************************************************************
// Copyright (c) 2014 Charlie Poole, Rob Prouse
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
using System.Reflection;
using NUnit.Framework;

namespace NUnit.Engine.Services.Tests
{
    public class ResultServiceTests
    {
        private ResultService _resultService;

        [SetUp]
        public void CreateService()
        {
            var services = new ServiceContext();
#if !NETCOREAPP1_1
            services.Add(new ExtensionService());
#endif
            _resultService = new ResultService();
            services.Add(_resultService);
            services.ServiceManager.StartServices();
        }

        [Test]
        public void ServiceIsStarted()
        {
            Assert.That(_resultService.Status, Is.EqualTo(ServiceStatus.Started));
        }

        [Test]
        public void AvailableFormats()
        {
#if NETCOREAPP1_1
            Assert.That(_resultService.Formats, Is.EquivalentTo(new string[] { "nunit3", "cases" }));
#else
            Assert.That(_resultService.Formats, Is.EquivalentTo(new string[] { "nunit3", "cases", "user" }));
#endif
        }

        [TestCase("nunit3", null, ExpectedResult = "NUnit3XmlResultWriter")]
        //[TestCase("nunit2", null, ExpectedResult = "NUnit2XmlResultWriter")]
        [TestCase("cases", null, ExpectedResult = "TestCaseResultWriter")]
        //[TestCase("user", new object[] { "TextSummary.xslt" }, ExpectedResult = "XmlTransformResultWriter")]
        public string CanGetWriter(string format, object[] args)
        {
            var writer = _resultService.GetResultWriter(format, args);

            Assert.NotNull(writer);
            return writer.GetType().Name;
        }

#if !NETCOREAPP1_1 && !NETCOREAPP2_1
        [Test]
        public void CanGetWriterUser()
        {
            string actual = CanGetWriter("user", new object[] { Path.Combine(TestContext.CurrentContext.TestDirectory, "TextSummary.xslt") });
            Assert.That(actual, Is.EqualTo("XmlTransformResultWriter"));
        }

        [Test]
        public void NUnit3Format_NonExistentTransform_ThrowsArgumentException()
        {
            Assert.That(
                () => _resultService.GetResultWriter("user", new object[] { "junk.xslt" }),
                Throws.ArgumentException);
        }

        [Test]
        public void NUnit3Format_NullArgument_ThrowsArgumentNullException()
        {
            Assert.That(
                () => _resultService.GetResultWriter("user", null),
                Throws.TypeOf<ArgumentNullException>());
        }
#endif
    }
}
