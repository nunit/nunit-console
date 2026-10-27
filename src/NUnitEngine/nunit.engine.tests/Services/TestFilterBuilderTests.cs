// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using System;
using System.Xml;
using NUnit.Engine;
using NUnit.Framework;

namespace NUnit.Engine.Services.Tests
{
    public class TestFilterBuilderTests
    {
        TestFilterBuilder builder;

        [SetUp]
        public void CreateBuilder()
        {
            this.builder = new TestFilterBuilder();
        }

        [Test]
        public void EmptyFilter()
        {
            TestFilter filter = builder.GetFilter();
            Assert.That(filter.Text, Is.EqualTo("<filter></filter>"));
        }

        [Test]
        public void OneTestSelected()
        {
            builder.AddTest("My.Test.Name");
            TestFilter filter = builder.GetFilter();

            Assert.That(filter.Text, Is.EqualTo(
                "<filter><test>My.Test.Name</test></filter>"));
        }

        [Test]
        public void OneTestSelected_XmlEscape()
        {
            builder.AddTest("My.Test.Name<T>(\"abc\")");
            TestFilter filter = builder.GetFilter();

            Assert.That(filter.Text, Is.EqualTo(
                "<filter><test>My.Test.Name&lt;T&gt;(&quot;abc&quot;)</test></filter>"));
        }

        [Test]
        public void ThreeTestsSelected()
        {
            builder.AddTest("My.First.Test");
            builder.AddTest("My.Second.Test");
            builder.AddTest("My.Third.Test");
            TestFilter filter = builder.GetFilter();

            Assert.That(filter.Text, Is.EqualTo(
                "<filter><or><test>My.First.Test</test><test>My.Second.Test</test><test>My.Third.Test</test></or></filter>"));
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

        [TestCase("'", "Repro.TupleOnly.Tuple((PROJECT, GLOBAL))")]
        [TestCase("\"", "Repro.TupleOnly.Tuple((PROJECT, GLOBAL))")]
        [TestCase("/", "Repro.TupleOnly.Tuple((PROJECT, GLOBAL))")]
        [TestCase("'", "Repro.Shapes.SpaceBeforeParen (Case 1)")]
        [TestCase("\"", "Repro.Shapes.SpaceBeforeParen (Case 1)")]
        [TestCase("/", "Repro.Shapes.SpaceBeforeParen (Case 1)")]
        [TestCase("'", " Leading and trailing  spaces ")]
        [TestCase("'", "Repro.Shapes.StringArg(\"a b\")")]
        [TestCase("'", "Repro.Shapes.TwoEnums(PROJECT,GLOBAL)")]
        public void SelectWherePreservesQuotedTestName(string quote, string testName)
        {
            builder.SelectWhere("test == " + quote + testName + quote);
            TestFilter filter = builder.GetFilter();
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(filter.Text);

            Assert.That(doc.SelectSingleNode("/filter/test")!.InnerText, Is.EqualTo(testName));

            TestFilterBuilder exactNameBuilder = new TestFilterBuilder();
            exactNameBuilder.AddTest(testName);
            Assert.That(filter.Text, Is.EqualTo(exactNameBuilder.GetFilter().Text));
        }

        [Test]
        public void OneTestAndWhereClause()
        {
            builder.AddTest("My.Test.Name");
            builder.SelectWhere("cat != Slow");
            TestFilter filter = builder.GetFilter();

            Assert.That(filter.Text, Is.EqualTo(
                "<filter><test>My.Test.Name</test><not><cat>Slow</cat></not></filter>"));
        }
    }
}
