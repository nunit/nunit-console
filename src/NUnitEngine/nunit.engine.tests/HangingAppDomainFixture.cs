// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using System;
using System.Threading;
using NUnit.Framework;

namespace NUnit.Engine.Tests
{
    [Explicit]
    public class HangingAppDomainFixture
    {
        [Test]
        public void PassingTest()
        {
            Assert.Pass();
        }

        ~HangingAppDomainFixture()
        {
            Thread.Sleep(TimeSpan.FromDays(1));
        }
    }
}
