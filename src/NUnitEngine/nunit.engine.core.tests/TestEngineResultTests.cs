// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using System.Xml;

namespace NUnit.Engine.Tests
{
    using Framework;
    using Internal;

    [TestFixture]
    public class TestEngineResultTests
    {
        private static readonly string xmlText = "<test-assembly result=\"Passed\" total=\"23\" passed=\"23\" failed=\"0\" inconclusive=\"0\" skipped=\"0\" asserts=\"40\" />";

        [Test]
        public void CanCreateFromXmlString()
        {
            TestEngineResult result = new TestEngineResult(xmlText);
            Assert.That(result.IsSingle, Is.True);
            Assert.That(result.Xml.Name, Is.EqualTo("test-assembly"));
            Assert.That(result.Xml.Attributes["result"].Value, Is.EqualTo("Passed"));
            Assert.That(result.Xml.Attributes["total"].Value, Is.EqualTo("23"));
            Assert.That(result.Xml.Attributes["passed"].Value, Is.EqualTo("23"));
            Assert.That(result.Xml.Attributes["failed"].Value, Is.EqualTo("0"));
            Assert.That(result.Xml.Attributes["inconclusive"].Value, Is.EqualTo("0"));
            Assert.That(result.Xml.Attributes["skipped"].Value, Is.EqualTo("0"));
            Assert.That(result.Xml.Attributes["asserts"].Value, Is.EqualTo("40"));
        }

        [Test]
        public void CanCreateFromXmlNode()
        {
            XmlNode node = XmlHelper.CreateTopLevelElement("test-assembly");
            XmlHelper.AddAttribute(node, "result", "Passed");
            XmlHelper.AddAttribute(node, "total", "23");
            XmlHelper.AddAttribute(node, "passed", "23");
            XmlHelper.AddAttribute(node, "failed", "0");
            XmlHelper.AddAttribute(node, "inconclusive", "0");
            XmlHelper.AddAttribute(node, "skipped", "0");
            XmlHelper.AddAttribute(node, "asserts", "40");

            TestEngineResult result = new TestEngineResult(node);
            Assert.That(result.IsSingle, Is.True);
            Assert.That(result.Xml.OuterXml, Is.EqualTo(xmlText));
        }
    }
}
