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
using NUnit.Engine.Runners;
using NUnit.Framework;

namespace NUnit.Engine.Services.Tests
{
    using Fakes;

    public class DefaultTestRunnerFactoryTests
    {
        private ServiceContext _services;
        private DefaultTestRunnerFactory _factory;

        [SetUp]
        public void CreateServiceContext()
        {
            _services = new ServiceContext();
#if !NETCOREAPP1_1
            _services.Add(new ExtensionService());
            _services.Add(new FakeProjectService());
#endif
            _factory = new DefaultTestRunnerFactory();
            _services.Add(_factory);
            _services.ServiceManager.StartServices();
        }

        [Test]
        public void ServiceIsStarted()
        {
            Assert.That(_factory.Status, Is.EqualTo(ServiceStatus.Started));
        }

#if NETCOREAPP1_1
        // Single file
        [TestCase("x.dll",             null,        typeof(LocalTestRunner))]
        // Two files
        [TestCase("x.dll y.dll",       null,        typeof(AggregatingTestRunner))]
        // Three files
        [TestCase("x.dll y.dll z.dll",   null,       typeof(AggregatingTestRunner))]
#elif NETCOREAPP2_0
        // Single file
        [TestCase("x.nunit",           null,        typeof(AggregatingTestRunner))]
        [TestCase("x.dll",             null,        typeof(LocalTestRunner))]
        // Two files
        [TestCase("x.nunit y.nunit",   null,        typeof(AggregatingTestRunner))]
        [TestCase("x.nunit y.dll",     null,        typeof(AggregatingTestRunner))]
        [TestCase("x.dll y.dll",       null,        typeof(AggregatingTestRunner))]
        // Three files
        [TestCase("x.nunit y.dll z.nunit", null,       typeof(AggregatingTestRunner))]
        [TestCase("x.dll y.nunit z.dll",   null,       typeof(AggregatingTestRunner))]

#else
        // Single file
        [TestCase("x.nunit",           null,        typeof(AggregatingTestRunner))]
        [TestCase("x.dll",             null,        typeof(ProcessRunner))]
        [TestCase("x.nunit",           "Single",    typeof(AggregatingTestRunner))]
        [TestCase("x.dll",             "Single",    typeof(TestDomainRunner))]
        [TestCase("x.nunit",           "Separate",  typeof(ProcessRunner))]
        [TestCase("x.dll",             "Separate",  typeof(ProcessRunner))]
        [TestCase("x.nunit",           "Multiple",  typeof(MultipleTestProcessRunner))]
        [TestCase("x.dll",             "Multiple",  typeof(MultipleTestProcessRunner))]
        // Two files
        [TestCase("x.nunit y.nunit",   null,        typeof(AggregatingTestRunner))]
        [TestCase("x.nunit y.dll",     null,        typeof(AggregatingTestRunner))]
        [TestCase("x.dll y.dll",       null,        typeof(MultipleTestProcessRunner))]
        [TestCase("x.nunit y.nunit",   "Single",    typeof(AggregatingTestRunner))]
        [TestCase("x.nunit y.dll",     "Single",    typeof(AggregatingTestRunner))]
        [TestCase("x.dll y.dll",       "Single",    typeof(MultipleTestDomainRunner))]
        [TestCase("x.nunit y.nunit",   "Separate",  typeof(AggregatingTestRunner))]
        [TestCase("x.nunit y.dll",     "Separate",  typeof(AggregatingTestRunner))]
        [TestCase("x.dll y.dll",       "Separate",  typeof(ProcessRunner))]
        [TestCase("x.nunit y.nunit",   "Multiple",  typeof(AggregatingTestRunner))]
        [TestCase("x.nunit y.dll",     "Multiple",  typeof(AggregatingTestRunner))]
        [TestCase("x.dll y.dll",       "Multiple",  typeof(MultipleTestProcessRunner))]
        // Three files
        [TestCase("x.nunit y.dll z.nunit", null,       typeof(AggregatingTestRunner))]
        [TestCase("x.dll y.nunit z.dll",   null,       typeof(AggregatingTestRunner))]
        [TestCase("x.nunit y.dll z.nunit", "Single",   typeof(AggregatingTestRunner))]
        [TestCase("x.dll y.nunit z.dll",   "Single",   typeof(AggregatingTestRunner))]
        [TestCase("x.dll y.dll z.dll",     "Single",   typeof(MultipleTestDomainRunner))]
        [TestCase("x.nunit y.dll z.nunit", "Separate", typeof(AggregatingTestRunner))]
        [TestCase("x.dll y.nunit z.dll",   "Separate", typeof(AggregatingTestRunner))]
        [TestCase("x.dll y.dll z.dll",     "Separate", typeof(ProcessRunner))]
        [TestCase("x.nunit y.dll z.nunit", "Multiple", typeof(AggregatingTestRunner))]
        [TestCase("x.dll y.nunit z.dll",   "Multiple", typeof(AggregatingTestRunner))]
        [TestCase("x.dll y.dll z.dll",     "Multiple", typeof(MultipleTestProcessRunner))]
#endif
        public void CorrectRunnerIsUsed(string files, string processModel, Type expectedType)
        {
            var package = new TestPackage(files.Split(new char[] { ' ' }));
            if (processModel != null)
                package.AddSetting("ProcessModel", processModel);

            var runner = _factory.MakeTestRunner(package);

            Assert.That(runner, Is.TypeOf(expectedType));
        }

#if NETCOREAPP1_1 || NETCOREAPP2_0
        [TestCase("x.junk", typeof(LocalTestRunner))]
        [TestCase("x.junk y.dll", typeof(AggregatingTestRunner))]
        [TestCase("x.junk y.junk", typeof(AggregatingTestRunner))]
        [TestCase("x.dll y.junk z.dll", typeof(AggregatingTestRunner))]
        [TestCase("x.dll y.junk z.junk", typeof(AggregatingTestRunner))]
#else
        [TestCase("x.junk", typeof(ProcessRunner))]
        [TestCase("x.junk y.dll", typeof(MultipleTestProcessRunner))]
        [TestCase("x.junk y.junk", typeof(MultipleTestProcessRunner))]
        [TestCase("x.dll y.junk z.dll", typeof(MultipleTestProcessRunner))]
        [TestCase("x.dll y.junk z.junk", typeof(MultipleTestProcessRunner))]
#endif
        public void CorrectRunnerIsUsed_InvalidExtension(string files, Type expectedType)
        {
            var package = new TestPackage(files.Split(new char[] {' '}));
            var runner = _factory.MakeTestRunner(package);

            Assert.That(runner, Is.TypeOf(expectedType));
        }
    }
}
