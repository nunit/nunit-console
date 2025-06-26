// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using System;
using System.Xml;
using NUnit.Framework;

namespace NUnit.Engine.Services
{
    public class TestSelectionParserTests
    {
        // Category Filter
        [TestCase("cat=Urgent", "<cat><![CDATA[Urgent]]></cat>")]
        [TestCase("cat==Urgent", "<cat><![CDATA[Urgent]]></cat>")]
        [TestCase("cat!=Urgent", "<not><cat><![CDATA[Urgent]]></cat></not>")]
        [TestCase("cat =~ Urgent", "<cat re=\"1\"><![CDATA[Urgent]]></cat>")]
        [TestCase("cat !~ Urgent", "<not><cat re=\"1\"><![CDATA[Urgent]]></cat></not>")]
        [TestCase("cat = Urgent || cat = High", "<or><cat><![CDATA[Urgent]]></cat><cat><![CDATA[High]]></cat></or>")]
        // Property Filter
        [TestCase("Priority == High", "<prop name=\"Priority\"><![CDATA[High]]></prop>")]
        [TestCase("Priority != Urgent", "<not><prop name=\"Priority\"><![CDATA[Urgent]]></prop></not>")]
        [TestCase("Author =~ Jones", "<prop name=\"Author\" re=\"1\"><![CDATA[Jones]]></prop>")]
        [TestCase("Author !~ Jones", "<not><prop name=\"Author\" re=\"1\"><![CDATA[Jones]]></prop></not>")]
        // Name Filter
        [TestCase("name='SomeTest'", "<name><![CDATA[SomeTest]]></name>")]
        // Method Filter
        [TestCase("method=TestMethod", "<method><![CDATA[TestMethod]]></method>")]
        [TestCase("method=Test1||method=Test2||method=Test3", "<or><method><![CDATA[Test1]]></method><method><![CDATA[Test2]]></method><method><![CDATA[Test3]]></method></or>")]
        // Namespace Filter
        [TestCase("namespace=Foo", "<namespace><![CDATA[Foo]]></namespace>")]
        [TestCase("namespace=Foo.Bar", "<namespace><![CDATA[Foo.Bar]]></namespace>")]
        [TestCase("namespace=Foo||namespace=Bar", "<or><namespace><![CDATA[Foo]]></namespace><namespace><![CDATA[Bar]]></namespace></or>")]
        [TestCase("namespace=Foo.Bar||namespace=Bar.Baz", "<or><namespace><![CDATA[Foo.Bar]]></namespace><namespace><![CDATA[Bar.Baz]]></namespace></or>")]
        // Test Filter
        [TestCase("test='My.Test.Fixture.Method(42)'", "<test><![CDATA[My.Test.Fixture.Method(42)]]></test>")]
        [TestCase("test='My.Test.Fixture.Method(\"xyz\")'", "<test><![CDATA[My.Test.Fixture.Method(\"xyz\")]]></test>")]
        [TestCase("test='My.Test.Fixture.Method(\"abc\\'s\")'", "<test><![CDATA[My.Test.Fixture.Method(\"abc's\")]]></test>")]
        [TestCase("test='My.Test.Fixture.Method(\"x&y&z\")'", "<test><![CDATA[My.Test.Fixture.Method(\"x&y&z\")]]></test>")]
        [TestCase("test='My.Test.Fixture.Method(\"<xyz>\")'", "<test><![CDATA[My.Test.Fixture.Method(\"<xyz>\")]]></test>")]
        [TestCase("test == namespace.class(1).test1(1)", "<test><![CDATA[namespace.class(1).test1(1)]]></test>")]
        [TestCase("test == \"namespace.class(1).test1(1)\"", "<test><![CDATA[namespace.class(1).test1(1)]]></test>")]
        [TestCase("test == 'namespace.class(1).test1(1)'", "<test><![CDATA[namespace.class(1).test1(1)]]></test>")]
        [TestCase("test =~ \"(namespace\\.test1\\(1\\)|namespace\\.test2\\(2\\))\"", "<test re=\"1\"><![CDATA[(namespace.test1(1)|namespace.test2(2))]]></test>")]
        [TestCase("test =~ '(namespace\\.test1\\(1\\)|namespace\\.test2\\(2\\))'", "<test re=\"1\"><![CDATA[(namespace.test1(1)|namespace.test2(2))]]></test>")]
        [TestCase("test =~ /(namespace\\.test1\\(1\\)|namespace\\.test2\\(2\\))/", "<test re=\"1\"><![CDATA[(namespace.test1(1)|namespace.test2(2))]]></test>")]
        [TestCase("test =~ \"(namespace1|namespace2)\\.test1\"", "<test re=\"1\"><![CDATA[(namespace1|namespace2).test1]]></test>")]
        [TestCase("test =~ '(namespace1|namespace2)\\.test1'", "<test re=\"1\"><![CDATA[(namespace1|namespace2).test1]]></test>")]
        [TestCase("test =~ /(namespace1|namespace2)\\.test1/", "<test re=\"1\"><![CDATA[(namespace1|namespace2).test1]]></test>")]
        [TestCase("test='My.Test.Fixture.Method(\" A \\\\\" B \\\\\" C \")'", "<test><![CDATA[My.Test.Fixture.Method(\" A \\\" B \\\" C \")]]></test>")]
        // And Filter
        [TestCase("cat==Urgent && test=='My.Tests'", "<and><cat><![CDATA[Urgent]]></cat><test><![CDATA[My.Tests]]></test></and>")]
        [TestCase("cat==Urgent and test=='My.Tests'", "<and><cat><![CDATA[Urgent]]></cat><test><![CDATA[My.Tests]]></test></and>")]
        // Or Filter
        [TestCase("cat==Urgent || test=='My.Tests'", "<or><cat><![CDATA[Urgent]]></cat><test><![CDATA[My.Tests]]></test></or>")]
        [TestCase("cat==Urgent or test=='My.Tests'", "<or><cat><![CDATA[Urgent]]></cat><test><![CDATA[My.Tests]]></test></or>")]
        // Mixed And Filter with Or Filter
        [TestCase("cat==Urgent || test=='My.Tests' && cat == high", "<or><cat><![CDATA[Urgent]]></cat><and><test><![CDATA[My.Tests]]></test><cat><![CDATA[high]]></cat></and></or>")]
        [TestCase("cat==Urgent && test=='My.Tests' || cat == high", "<or><and><cat><![CDATA[Urgent]]></cat><test><![CDATA[My.Tests]]></test></and><cat><![CDATA[high]]></cat></or>")]
        [TestCase("cat==Urgent && (test=='My.Tests' || cat == high)", "<and><cat><![CDATA[Urgent]]></cat><or><test><![CDATA[My.Tests]]></test><cat><![CDATA[high]]></cat></or></and>")]
        [TestCase("cat==Urgent && !(test=='My.Tests' || cat == high)", "<and><cat><![CDATA[Urgent]]></cat><not><or><test><![CDATA[My.Tests]]></test><cat><![CDATA[high]]></cat></or></not></and>")]
        // Not Filter
        [TestCase("!(test!='My.Tests')", "<not><not><test><![CDATA[My.Tests]]></test></not></not>")]
        [TestCase("!(cat!=Urgent)", "<not><not><cat><![CDATA[Urgent]]></cat></not></not>")]
        public void TestParser(string input, string output)
        {
            Assert.That(TestSelectionParser.Parse(input), Is.EqualTo(output));

            XmlDocument doc = new XmlDocument();
            Assert.DoesNotThrow(() => doc.LoadXml(output));
        }

        [TestCase(null!, typeof(ArgumentNullException))]
        [TestCase("", typeof(TestSelectionParserException))]
        [TestCase("   ", typeof(TestSelectionParserException))]
        [TestCase("  \t\t ", typeof(TestSelectionParserException))]
        public void TestParser_InvalidInput(string input, Type type)
        {
            Assert.That(() => TestSelectionParser.Parse(input), Throws.TypeOf(type));
        }
    }
}
