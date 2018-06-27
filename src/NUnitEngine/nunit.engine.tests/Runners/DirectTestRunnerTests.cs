﻿// ***********************************************************************
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
using System.Collections.Generic;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using NUnit.Engine.Extensibility;
using NUnit.Engine.Tests.Runners.Fakes;
using NUnit.Framework;

namespace NUnit.Engine.Tests.Runners
{
    public class DirectTestRunnerTests
    {
        private IFrameworkDriver driver;
        private EmptyDirectTestRunner directTestRunner;
        private TestFilter testFilter = new TestFilter(string.Empty);

        [SetUp]
        public void Initialize()
        {
            driver = Substitute.For<IFrameworkDriver>();

            var driverService = Substitute.For<IDriverService>();
            driverService.GetDriver(AppDomain.CurrentDomain, string.Empty, string.Empty, false).ReturnsForAnyArgs(driver);

            var serviceLocator = Substitute.For<IServiceLocator>();
            serviceLocator.GetService<IDriverService>().Returns(driverService);

            directTestRunner = new EmptyDirectTestRunner(serviceLocator, new TestPackage("mock-assembly.dll"));
        }

        [Test]
        public void Explore_Passes_Along_NUnitEngineException()
        {
            driver.Explore(Arg.Any<string>()).Throws(new NUnitEngineException("Message"));
            var ex = Assert.Throws<NUnitEngineException>(() => directTestRunner.Explore(new TestFilter(string.Empty)));
            Assert.That(ex.Message, Is.EqualTo("Message"));
        }

        [Test]
        public void Explore_Throws_NUnitEngineException()
        {
            driver.Explore(Arg.Any<string>()).Throws(new ArgumentException("Message"));
            var ex = Assert.Throws<NUnitEngineException>(() => directTestRunner.Explore(new TestFilter(string.Empty)));
            Assert.That(ex.InnerException is ArgumentException);
            Assert.That(ex.InnerException.Message, Is.EqualTo("Message"));
        }

        [Test]
        public void Load_Passes_Along_NUnitEngineException()
        {
            driver.Load(Arg.Any<string>(), Arg.Any<Dictionary<string, object>>()).Throws(new NUnitEngineException("Message"));
            var ex = Assert.Throws<NUnitEngineException>(() => directTestRunner.Load());
            Assert.That(ex.Message, Is.EqualTo("Message"));
        }

        [Test]
        public void Load_Throws_NUnitEngineException()
        {
            driver.Load(Arg.Any<string>(), Arg.Any<Dictionary<string, object>>()).Throws(new ArgumentException("Message"));
            var ex = Assert.Throws<NUnitEngineException>(() => directTestRunner.Load());
            Assert.That(ex.InnerException is ArgumentException);
            Assert.That(ex.InnerException.Message, Is.EqualTo("Message"));
        }

        [Test]
        public void CountTestCases_Passes_Along_NUnitEngineException()
        {
            driver.CountTestCases(Arg.Any<string>()).Throws(new NUnitEngineException("Message"));
            var ex = Assert.Throws<NUnitEngineException>(() => directTestRunner.CountTestCases(testFilter));
            Assert.That(ex.Message, Is.EqualTo("Message"));
        }

        [Test]
        public void CountTestCases_Throws_NUnitEngineException()
        {
            driver.CountTestCases(Arg.Any<string>()).Throws(new ArgumentException("Message"));
            var ex = Assert.Throws<NUnitEngineException>(() => directTestRunner.CountTestCases(testFilter));
            Assert.That(ex.InnerException is ArgumentException);
            Assert.That(ex.InnerException.Message, Is.EqualTo("Message"));
        }

        [Test]
        public void Run_Passes_Along_NUnitEngineException()
        {
            driver.Run(Arg.Any<ITestEventListener>(), Arg.Any<string>()).Throws(new NUnitEngineException("Message"));
            var ex = Assert.Throws<NUnitEngineException>(() => directTestRunner.Run(Substitute.For<ITestEventListener>(), testFilter));
            Assert.That(ex.Message, Is.EqualTo("Message"));
        }

        [Test]
        public void Run_Throws_NUnitEngineException()
        {
            driver.Run(Arg.Any<ITestEventListener>(), Arg.Any<string>()).Throws(new ArgumentException("Message"));
            var ex = Assert.Throws<NUnitEngineException>(() => directTestRunner.Run(Substitute.For<ITestEventListener>(), testFilter));
            Assert.That(ex.InnerException is ArgumentException);
            Assert.That(ex.InnerException.Message, Is.EqualTo("Message"));
        }

        [Test]
        public void StopRun_Passes_Along_NUnitEngineException()
        {
            driver.When(x => x.StopRun(Arg.Any<bool>()))
                .Do(x => { throw new NUnitEngineException("Message"); });

            var ex = Assert.Throws<NUnitEngineException>(() => directTestRunner.StopRun(true));
            Assert.That(ex.Message, Is.EqualTo("Message"));
        }

        [Test]
        public void StopRun_Throws_NUnitEngineException()
        {
            driver.When(x => x.StopRun(Arg.Any<bool>()))
                .Do(x => { throw new ArgumentException("Message"); });

            var ex = Assert.Throws<NUnitEngineException>(() => directTestRunner.StopRun(true));
            Assert.That(ex.InnerException is ArgumentException);
            Assert.That(ex.InnerException.Message, Is.EqualTo("Message"));
        }
    }
}
