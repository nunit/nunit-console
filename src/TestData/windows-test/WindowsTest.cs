// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using System.Windows.Forms;
using NUnit.Framework;

// Test which resolves issue #1203
namespace Test1
{
    [TestFixture]
    public class Class1
    {
        [Test]
        public void WithoutFramework()
        {
            Assert.Pass();
        }

        [Test]
        public void WithFramework()
        {
            var checkbox = new CheckBox();
            Assert.Pass();
        }
    }
}