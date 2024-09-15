// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using System.Reflection;
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

        [Test]
        public void LoadAspNetCore()
        {
            Assembly.Load("Microsoft.AspNetCore, Version=8.0.0.0, Culture=Neutral, PublicKeyToken=adb9793829ddae60");
        }
    }
}