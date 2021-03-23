// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using System.Threading;
using NUnit.Framework;

namespace NUnit.Tests
{
    public class HangingFixture
    {
        [SetUp]
        public void SetUp()
        {
            TestContext.Progress.WriteLine($"SetUp executing");
        }

        [TearDown]
        public void TearDown()
        {
            TestContext.Progress.WriteLine($"TearDown executing");
        }

        [Test]
        public void HangingTest()
        {
            TestContext.Progress.WriteLine("Test starting");
            while (true)
            {
                Thread.Sleep(3000);
                TestContext.Progress.WriteLine("Test continuing");
            }
        }
    }
}
