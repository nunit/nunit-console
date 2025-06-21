﻿// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using NUnit.Framework;

namespace NUnit.Engine.Services
{
    public class TestFilterBuilderTests
    {
        private TestFilterBuilder builder;

        [SetUp]
        public void CreateBuilder()
        {
            this.builder = new TestFilterBuilder();
        }

        [Test]
        public void EmptyFilter()
        {
            TestFilter filter = builder.GetFilter();
            Assert.That(filter.Text, Is.EqualTo("<filter />"));
        }

        [Test]
        public void OneTestSelected()
        {
            builder.AddTest("My.Test.Name");
            TestFilter filter = builder.GetFilter();

            Assert.That(filter.Text, Is.EqualTo(
                "<filter><test><![CDATA[My.Test.Name]]></test></filter>"));
        }

        [Test]
        public void OneTestSelected_XmlEscape()
        {
            builder.AddTest("My.Test.Name<T>(\"abc\")");
            TestFilter filter = builder.GetFilter();

            Assert.That(filter.Text, Is.EqualTo(
                "<filter><test><![CDATA[My.Test.Name<T>(\"abc\")]]></test></filter>"));
        }

        [Test]
        public void ThreeTestsSelected()
        {
            builder.AddTest("My.First.Test");
            builder.AddTest("My.Second.Test");
            builder.AddTest("My.Third.Test");
            TestFilter filter = builder.GetFilter();

            Assert.That(filter.Text, Is.EqualTo(
                "<filter><or><test><![CDATA[My.First.Test]]></test><test><![CDATA[My.Second.Test]]></test><test><![CDATA[My.Third.Test]]></test></or></filter>"));
        }

        [Test]
        public void WhereClause()
        {
            builder.SelectWhere("cat==Dummy");
            TestFilter filter = builder.GetFilter();

            Assert.That(filter.Text, Is.EqualTo("<filter><cat>Dummy</cat></filter>"));
        }

        [Test]
        public void WhereClause_XmlEscape()
        {
            builder.SelectWhere("test=='My.Test.Name<T>(\"abc\")'");
            TestFilter filter = builder.GetFilter();

            Assert.That(filter.Text, Is.EqualTo(
                "<filter><test>My.Test.Name&lt;T&gt;(&quot;abc&quot;)</test></filter>"));
        }

        [Test]
        public void OneTestAndWhereClause()
        {
            builder.AddTest("My.Test.Name");
            builder.SelectWhere("cat != Slow");
            TestFilter filter = builder.GetFilter();

            Assert.That(filter.Text, Is.EqualTo(
                "<filter><test><![CDATA[My.Test.Name]]></test><not><cat>Slow</cat></not></filter>"));
        }
    }
}
