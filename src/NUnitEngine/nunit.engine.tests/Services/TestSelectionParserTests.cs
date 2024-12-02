// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using NUnit.Framework;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Xml;

namespace NUnit.Engine.Tests
{
    public class TestSelectionParserTests
    {
        private TestSelectionParser _parser;

        [SetUp]
        public void CreateParser()
        {
            _parser = new TestSelectionParser();
        }

        [TestCaseSource(nameof(UniqueOutputs))]
        public void AllOutputsAreValidXml(string output)
        {
            XmlDocument doc = new XmlDocument();
            Assert.DoesNotThrow(() => doc.LoadXml(output));
        }

        [TestCaseSource(nameof(ParserTestCases))]
        public void TestParser(string input, string output)
        {
            Assert.That(_parser.Parse(input), Is.EqualTo(output));
        }

        [TestCase(null, typeof(ArgumentNullException))]
        [TestCase("", typeof(TestSelectionParserException))]
        [TestCase("   ", typeof(TestSelectionParserException))]
        [TestCase("  \t\t ", typeof(TestSelectionParserException))]
        public void TestParser_InvalidInput(string input, Type type)
        {
            Assert.That(() => _parser.Parse(input), Throws.TypeOf(type));
        }

        private static readonly TestCaseData[] ParserTestCases = new[]
        {
            // Category Filter
            new TestCaseData("cat=Urgent", "<cat>Urgent</cat>"),
            new TestCaseData("cat=/Urgent/", "<cat>Urgent</cat>"),
            new TestCaseData("cat='Urgent'", "<cat>Urgent</cat>"),
            new TestCaseData("cat==Urgent", "<cat>Urgent</cat>"),
            new TestCaseData("cat!=Urgent", "<not><cat>Urgent</cat></not>"),
            new TestCaseData("cat =~ Urgent", "<cat re='1'>Urgent</cat>"),
            new TestCaseData("cat !~ Urgent", "<not><cat re='1'>Urgent</cat></not>"),
            // Property Filter
            new TestCaseData("Priority == High", "<prop name='Priority'>High</prop>"),
            new TestCaseData("Priority != Urgent", "<not><prop name='Priority'>Urgent</prop></not>"),
            new TestCaseData("Author =~ Jones", "<prop name='Author' re='1'>Jones</prop>"),
            new TestCaseData("Author !~ Jones", "<not><prop name='Author' re='1'>Jones</prop></not>"),
            // Name Filter
            new TestCaseData("name='SomeTest'", "<name>SomeTest</name>"),
            // Method Filter
            new TestCaseData("method=TestMethod", "<method>TestMethod</method>"),
            new TestCaseData("method=Test1||method=Test2||method=Test3", "<or><method>Test1</method><method>Test2</method><method>Test3</method></or>"),
            // Namespace Filter
            new TestCaseData("namespace=Foo", "<namespace>Foo</namespace>"),
            new TestCaseData("namespace=Foo.Bar", "<namespace>Foo.Bar</namespace>"),
            new TestCaseData("namespace=Foo||namespace=Bar", "<or><namespace>Foo</namespace><namespace>Bar</namespace></or>"),
            new TestCaseData("namespace=Foo.Bar||namespace=Bar.Baz", "<or><namespace>Foo.Bar</namespace><namespace>Bar.Baz</namespace></or>"),
            // Test Filter
            new TestCaseData("test='My.Test.Fixture.Method(42)'", "<test>My.Test.Fixture.Method(42)</test>"),
            new TestCaseData("test='My.Test.Fixture.Method(\"xyz\")'", "<test>My.Test.Fixture.Method(&quot;xyz&quot;)</test>"),
            new TestCaseData("test='My.Test.Fixture.Method(\"abc\\'s\")'", "<test>My.Test.Fixture.Method(&quot;abc&apos;s&quot;)</test>"),
            new TestCaseData("test='My.Test.Fixture.Method(\"x&y&z\")'", "<test>My.Test.Fixture.Method(&quot;x&amp;y&amp;z&quot;)</test>"),
            new TestCaseData("test='My.Test.Fixture.Method(\"<xyz>\")'", "<test>My.Test.Fixture.Method(&quot;&lt;xyz&gt;&quot;)</test>"),
            new TestCaseData("test=='Issue1510.TestSomething ( Option1 , \"ABC\" ) '", "<test>Issue1510.TestSomething(Option1,&quot;ABC&quot;)</test>"),
            new TestCaseData("test=='Issue1510.TestSomething ( Option1 , \"A B C\" ) '", "<test>Issue1510.TestSomething(Option1,&quot;A B C&quot;)</test>"),
            new TestCaseData("test=/My.Test.Fixture.Method(42)/", "<test>My.Test.Fixture.Method(42)</test>"),
            new TestCaseData("test=/My.Test.Fixture.Method(\"xyz\")/", "<test>My.Test.Fixture.Method(&quot;xyz&quot;)</test>"),
            new TestCaseData("test=/My.Test.Fixture.Method(\"abc\\'s\")/", "<test>My.Test.Fixture.Method(&quot;abc&apos;s&quot;)</test>"),
            new TestCaseData("test=/My.Test.Fixture.Method(\"x&y&z\")/", "<test>My.Test.Fixture.Method(&quot;x&amp;y&amp;z&quot;)</test>"),
            new TestCaseData("test=/My.Test.Fixture.Method(\"<xyz>\")/", "<test>My.Test.Fixture.Method(&quot;&lt;xyz&gt;&quot;)</test>"),
            new TestCaseData("test==/Issue1510.TestSomething ( Option1 , \"ABC\" ) /", "<test>Issue1510.TestSomething(Option1,&quot;ABC&quot;)</test>"),
            new TestCaseData("test==/Issue1510.TestSomething ( Option1 , \"A B C\" ) /", "<test>Issue1510.TestSomething(Option1,&quot;A B C&quot;)</test>"),
            new TestCaseData("test=My.Test.Fixture.Method(42)", "<test>My.Test.Fixture.Method(42)</test>"),
            new TestCaseData("test=My.Test.Fixture.Method(\"xyz\")", "<test>My.Test.Fixture.Method(&quot;xyz&quot;)</test>"),
            new TestCaseData("test=My.Test.Fixture.Method(\"abc\\'s\")", "<test>My.Test.Fixture.Method(&quot;abc&apos;s&quot;)</test>"),
            new TestCaseData("test=My.Test.Fixture.Method(\"x&y&z\")", "<test>My.Test.Fixture.Method(&quot;x&amp;y&amp;z&quot;)</test>"),
            new TestCaseData("test=My.Test.Fixture.Method(\"<xyz>\")", "<test>My.Test.Fixture.Method(&quot;&lt;xyz&gt;&quot;)</test>"),
            new TestCaseData("test==Issue1510.TestSomething ( Option1 , \"ABC\" ) ", "<test>Issue1510.TestSomething(Option1,&quot;ABC&quot;)</test>"),
            new TestCaseData("test==Issue1510.TestSomething ( Option1 , \"A B C\" ) ", "<test>Issue1510.TestSomething(Option1,&quot;A B C&quot;)</test>"),
            // And Filter
            new TestCaseData("cat==Urgent && test=='My.Tests'", "<and><cat>Urgent</cat><test>My.Tests</test></and>"),
            new TestCaseData("cat==Urgent and test=='My.Tests'", "<and><cat>Urgent</cat><test>My.Tests</test></and>"),
            // Or Filter
            new TestCaseData("cat==Urgent || test=='My.Tests'", "<or><cat>Urgent</cat><test>My.Tests</test></or>"),
            new TestCaseData("cat==Urgent or test=='My.Tests'", "<or><cat>Urgent</cat><test>My.Tests</test></or>"),
            // Mixed And Filter with Or Filter
            new TestCaseData("cat = Urgent || cat = High", "<or><cat>Urgent</cat><cat>High</cat></or>"),
            new TestCaseData("cat==Urgent || test=='My.Tests' && cat == high", "<or><cat>Urgent</cat><and><test>My.Tests</test><cat>high</cat></and></or>"),
            new TestCaseData("cat==Urgent && test=='My.Tests' || cat == high", "<or><and><cat>Urgent</cat><test>My.Tests</test></and><cat>high</cat></or>"),
            new TestCaseData("cat==Urgent && (test=='My.Tests' || cat == high)", "<and><cat>Urgent</cat><or><test>My.Tests</test><cat>high</cat></or></and>"),
            new TestCaseData("cat==Urgent && !(test=='My.Tests' || cat == high)", "<and><cat>Urgent</cat><not><or><test>My.Tests</test><cat>high</cat></or></not></and>"),
            // Not Filter
            new TestCaseData("!(test!='My.Tests')", "<not><not><test>My.Tests</test></not></not>"),
            new TestCaseData("!(cat!=Urgent)", "<not><not><cat>Urgent</cat></not></not>")
        };

        private static IEnumerable<string> UniqueOutputs()
        {
            List<string> alreadyReturned = new List<string>();

            foreach (var testCase in ParserTestCases) 
            {
                var output = testCase.Arguments[1] as string;
                if (!alreadyReturned.Contains(output))
                {
                    alreadyReturned.Add(output);
                    yield return output;
                }
            }
        }
    }
}
