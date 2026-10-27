// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using System.Xml;
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
                "<filter><test><![CDATA[My.First.Test]]></test><test><![CDATA[My.Second.Test]]></test><test><![CDATA[My.Third.Test]]></test></filter>"));
        }

        [Test]
        public void WhereClause()
        {
            builder.SelectWhere("cat==Dummy");
            TestFilter filter = builder.GetFilter();

            Assert.That(filter.Text, Is.EqualTo("<filter><cat><![CDATA[Dummy]]></cat></filter>"));
        }

        [Test]
        public void WhereClause_XmlEscape()
        {
            builder.SelectWhere("test=='My.Test.Name<T>(\"abc\")'");
            TestFilter filter = builder.GetFilter();

            Assert.That(filter.Text, Is.EqualTo(
                "<filter><test><![CDATA[My.Test.Name<T>(\"abc\")]]></test></filter>"));
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
                "<filter><test><![CDATA[My.Test.Name]]></test><not><cat><![CDATA[Slow]]></cat></not></filter>"));
        }
    }
}
