// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using System.Threading;
using NUnit.Framework;

namespace NUnit.Tests
{
    // The tests in this fixture contain a delay, so we can exercise the stop button
    public class SlowFixture
    {
        [SetUp]
        public void SetUp()
        {
            TestContext.Progress.WriteLine($"SetUp starting");
            Thread.Sleep(3000);
            TestContext.Progress.WriteLine($"SetUp complete");
        }

        [TearDown]
        public void TearDown()
        {
            TestContext.Progress.WriteLine($"TearDown starting");
            Thread.Sleep(3000);
            TestContext.Progress.WriteLine($"TearDown complete");
        }

        [Test]
        public void TestMethod([Range(1, 5)] int i)
        {
            TestContext.Progress.WriteLine($"Test starting");
            Thread.Sleep(3000);
            TestContext.Progress.WriteLine($"Test complete");
        }
    }
}
