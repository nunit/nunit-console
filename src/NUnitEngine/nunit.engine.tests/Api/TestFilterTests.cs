// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using System;
using System.Xml;
using NUnit.Engine;
using NUnit.Framework;

namespace NUnit.Engine.Api.Tests
{
    public class TestFilterTests
    {
        [Test]
        public void EmptyFilter()
        {
            TestFilter filter = TestFilter.Empty;
            Assert.That(filter.Text, Is.EqualTo("<filter/>"));
        }

        [Test]
        public void FilterWithOneTest()
        {
            string text = "<filter><tests><test>My.Test.Name</test></tests></filter>";
            TestFilter filter = new TestFilter(text);
            Assert.That(filter.Text, Is.EqualTo(text));
        }

        [Test]
        public void FilterWithThreeTests()
        {
            string text = "<filter><tests><test>My.First.Test</test><test>My.Second.Test</test><test>My.Third.Test</test></tests></filter>";
            TestFilter filter = new TestFilter(text);
            Assert.That(filter.Text, Is.EqualTo(text));
        }
    }
}
