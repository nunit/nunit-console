// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using NUnit.Framework;
using Microsoft.AspNetCore.Components.Forms;

// Test which resolves issue #1203
namespace Test1
{
    [TestFixture]
    public class AspNetCoreTest
    {
        [Test]
        public void WithoutFramework()
        {
            Assert.Pass();
        }

        [Test]
        public void WithFramework()
        {
            InputCheckbox checkbox = new InputCheckbox();
            Assert.Pass();
        }
    }
}