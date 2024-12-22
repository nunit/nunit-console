// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using System;
using System.Windows;
using System.Windows.Controls;
using NUnit.Framework;

// Test which resolves issue #1203
namespace Test1
{
    [TestFixture]
    public class WPFTest : IWeakEventListener
    {
        [Test]
        public void AssertPass()
        {
            Assert.Pass();
        }

        [Test, Apartment(System.Threading.ApartmentState.STA)]
        public void CreateCheckBox()
        {
            CheckBox checkbox;
            checkbox = new CheckBox();
        }

        public bool ReceiveWeakEvent(Type managerType, object sender, EventArgs e)
        {
            throw new NotImplementedException();
        }
    }
}