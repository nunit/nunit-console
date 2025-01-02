// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using NUnit.Framework;
using System;

namespace NUnit.ConsoleRunner
{
    [TestFixture, Explicit]
    public class ConsoleOutputTests
    {
        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            Console.WriteLine("OneTimeSetUp: Console.WriteLine()");
            Console.Error.WriteLine("OneTimeSetUp: Console.Error.WriteLine()");
            TestContext.Out.WriteLine("OneTimeSetUp: TestContext.WriteLine()");

        }

        [SetUp]
        public void SetUp()
        {
            Console.WriteLine("SetUp: Console.WriteLine()");
            Console.Error.WriteLine("SetUp: Console.Error.WriteLine()");
            TestContext.Out.WriteLine("SetUp: TestContext.WriteLine()");
        }

        [TearDown]
        public void TearDown()
        {
            Console.WriteLine("TearDown: Console.WriteLine()");
            Console.Error.WriteLine("TearDown: Console.Error.WriteLine()");
            TestContext.Out.WriteLine("TearDown: TestContext.WriteLine()");
        }

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            Console.WriteLine("OneTimeTearDown: Console.WriteLine()");
            Console.Error.WriteLine("OneTimeTearDown: Console.Error.WriteLine()");
            TestContext.Out.WriteLine("OneTimeTearDown: TestContext.WriteLine()");
        }

        [Test]
        public void Test()
        {
            Console.WriteLine("Test: Console.WriteLine()");
            Console.Error.WriteLine("Test: Console.Error.WriteLine()");
            TestContext.Out.WriteLine("Test: TestContext.WriteLine()");
        }

        [Test]
        public void ConsoleEncoding()
        {
            TestContext.Out.WriteLine("•ÑÜńĭŧ·");
        }
    }
}
