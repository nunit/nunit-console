// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using System;
using System.Xml;
using NUnit.Framework;

namespace NUnit.Engine.Services
{
    public class TestSelectionParserTests
    {
        // Category Filter
        [TestCase("cat=Urgent", "<cat>Urgent</cat>")]
        [TestCase("cat==Urgent", "<cat>Urgent</cat>")]
        [TestCase("cat!=Urgent", "<not><cat>Urgent</cat></not>")]
        [TestCase("cat =~ Urgent", "<cat re='1'>Urgent</cat>")]
        [TestCase("cat !~ Urgent", "<not><cat re='1'>Urgent</cat></not>")]
        [TestCase("cat = Urgent || cat = High", "<or><cat>Urgent</cat><cat>High</cat></or>")]
        // Property Filter
        [TestCase("Priority == High", "<prop name='Priority'>High</prop>")]
        [TestCase("Priority != Urgent", "<not><prop name='Priority'>Urgent</prop></not>")]
        [TestCase("Author =~ Jones", "<prop name='Author' re='1'>Jones</prop>")]
        [TestCase("Author !~ Jones", "<not><prop name='Author' re='1'>Jones</prop></not>")]
        // Name Filter
        [TestCase("name='SomeTest'", "<name>SomeTest</name>")]
        // Method Filter
        [TestCase("method=TestMethod", "<method>TestMethod</method>")]
        [TestCase("method=Test1||method=Test2||method=Test3", "<or><method>Test1</method><method>Test2</method><method>Test3</method></or>")]
        // Namespace Filter
        [TestCase("namespace=Foo", "<namespace>Foo</namespace>")]
        [TestCase("namespace=Foo.Bar", "<namespace>Foo.Bar</namespace>")]
        [TestCase("namespace=Foo||namespace=Bar", "<or><namespace>Foo</namespace><namespace>Bar</namespace></or>")]
        [TestCase("namespace=Foo.Bar||namespace=Bar.Baz", "<or><namespace>Foo.Bar</namespace><namespace>Bar.Baz</namespace></or>")]
        // Test Filter
        [TestCase("test='My.Test.Fixture.Method(42)'", "<test>My.Test.Fixture.Method(42)</test>")]
        [TestCase("test='My.Test.Fixture.Method(\"xyz\")'", "<test>My.Test.Fixture.Method(&quot;xyz&quot;)</test>")]
        [TestCase("test='My.Test.Fixture.Method(\"abc\\'s\")'", "<test>My.Test.Fixture.Method(&quot;abc&apos;s&quot;)</test>")]
        [TestCase("test='My.Test.Fixture.Method(\"x&y&z\")'", "<test>My.Test.Fixture.Method(&quot;x&amp;y&amp;z&quot;)</test>")]
        [TestCase("test='My.Test.Fixture.Method(\"<xyz>\")'", "<test>My.Test.Fixture.Method(&quot;&lt;xyz&gt;&quot;)</test>")]
        [TestCase("test == namespace.class(1).test1(1)", "<test>namespace.class(1).test1(1)</test>")]
        [TestCase("test == \"namespace.class(1).test1(1)\"", "<test>namespace.class(1).test1(1)</test>")]
        [TestCase("test == 'namespace.class(1).test1(1)'", "<test>namespace.class(1).test1(1)</test>")]
        [TestCase("test =~ \"(namespace\\.test1\\(1\\)|namespace\\.test2\\(2\\))\"", "<test re='1'>(namespace.test1(1)|namespace.test2(2))</test>")]
        [TestCase("test =~ '(namespace\\.test1\\(1\\)|namespace\\.test2\\(2\\))'", "<test re='1'>(namespace.test1(1)|namespace.test2(2))</test>")]
        [TestCase("test =~ /(namespace\\.test1\\(1\\)|namespace\\.test2\\(2\\))/", "<test re='1'>(namespace.test1(1)|namespace.test2(2))</test>")]
        [TestCase("test =~ \"(namespace1|namespace2)\\.test1\"", "<test re='1'>(namespace1|namespace2).test1</test>")]
        [TestCase("test =~ '(namespace1|namespace2)\\.test1'", "<test re='1'>(namespace1|namespace2).test1</test>")]
        [TestCase("test =~ /(namespace1|namespace2)\\.test1/", "<test re='1'>(namespace1|namespace2).test1</test>")]
        [TestCase("test='My.Test.Fixture.Method(\" A \\\\\" B \\\\\" C \")'", "<test>My.Test.Fixture.Method(&quot; A \\&quot; B \\&quot; C &quot;)</test>")]
        // And Filter
        [TestCase("cat==Urgent && test=='My.Tests'", "<and><cat>Urgent</cat><test>My.Tests</test></and>")]
        [TestCase("cat==Urgent and test=='My.Tests'", "<and><cat>Urgent</cat><test>My.Tests</test></and>")]
        // Or Filter
        [TestCase("cat==Urgent || test=='My.Tests'", "<or><cat>Urgent</cat><test>My.Tests</test></or>")]
        [TestCase("cat==Urgent or test=='My.Tests'", "<or><cat>Urgent</cat><test>My.Tests</test></or>")]
        // Mixed And Filter with Or Filter
        [TestCase("cat==Urgent || test=='My.Tests' && cat == high", "<or><cat>Urgent</cat><and><test>My.Tests</test><cat>high</cat></and></or>")]
        [TestCase("cat==Urgent && test=='My.Tests' || cat == high", "<or><and><cat>Urgent</cat><test>My.Tests</test></and><cat>high</cat></or>")]
        [TestCase("cat==Urgent && (test=='My.Tests' || cat == high)", "<and><cat>Urgent</cat><or><test>My.Tests</test><cat>high</cat></or></and>")]
        [TestCase("cat==Urgent && !(test=='My.Tests' || cat == high)", "<and><cat>Urgent</cat><not><or><test>My.Tests</test><cat>high</cat></or></not></and>")]
        // Not Filter
        [TestCase("!(test!='My.Tests')", "<not><not><test>My.Tests</test></not></not>")]
        [TestCase("!(cat!=Urgent)", "<not><not><cat>Urgent</cat></not></not>")]
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
