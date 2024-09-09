// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using System;
using System.Windows.Controls;
using NUnit.Framework;

// Test which resolves issue #1203
namespace Test1
{
    [TestFixture]
    public class WPFTest
    {
        [Test]
        public void WithoutFramework()
        {
            Assert.Pass();
        }

        [Test]
        public void WithFramework()
        {
            //CheckBox checkbox;

        }
    }
}