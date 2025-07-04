﻿// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using System.Xml;
using NUnit.Framework;

namespace NUnit.Engine.Internal
{
    public class ResultHelperTests
    {
        private const string resultText1 = "<test-assembly result=\"Passed\" total=\"23\" passed=\"23\" failed=\"0\" inconclusive=\"0\" skipped=\"0\" warnings=\"0\" asserts=\"40\" />";
        private const string resultText2 = "<test-assembly result=\"Failed\" total=\"42\" passed=\"31\" failed=\"3\" inconclusive=\"5\" skipped=\"2\" warnings=\"1\" asserts=\"53\" />";

        private TestEngineResult result1;
        private TestEngineResult result2;

        private TestEngineResult[] twoResults;

        private XmlNode[] twoNodes;

        [SetUp]
        public void SetUp()
        {
            result1 = new TestEngineResult(resultText1);
            result2 = new TestEngineResult(resultText2);
            twoResults = new[] { result1, result2 };
            twoNodes = new[] { result1.Xml, result2.Xml };
        }

        [Test]
        public void MergeTestResults()
        {
            TestEngineResult mergedResult = ResultHelper.Merge(twoResults);

            Assert.That(mergedResult.XmlNodes.Count, Is.EqualTo(2));
            Assert.That(mergedResult.XmlNodes[0].OuterXml, Is.EqualTo(resultText1));
            Assert.That(mergedResult.XmlNodes[1].OuterXml, Is.EqualTo(resultText2));
        }

        [Test]
        public void AggregateTestResult()
        {
            TestEngineResult combined = result2.Aggregate("test-run", "ID", "NAME", "FULLNAME");
            Assert.That(combined.IsSingle);

            XmlNode combinedNode = combined.Xml;

            Assert.That(combinedNode.Name, Is.EqualTo("test-run"));
            Assert.That(combinedNode.Attributes, Is.Not.Null);
            Assert.That(combinedNode.Attributes["id"]?.Value, Is.EqualTo("ID"));
            Assert.That(combinedNode.Attributes["name"]?.Value, Is.EqualTo("NAME"));
            Assert.That(combinedNode.Attributes["fullname"]?.Value, Is.EqualTo("FULLNAME"));
            Assert.That(combinedNode.Attributes["result"]?.Value, Is.EqualTo("Failed"));
            Assert.That(combinedNode.Attributes["total"]?.Value, Is.EqualTo("42"));
            Assert.That(combinedNode.Attributes["passed"]?.Value, Is.EqualTo("31"));
            Assert.That(combinedNode.Attributes["failed"]?.Value, Is.EqualTo("3"));
            Assert.That(combinedNode.Attributes["warnings"]?.Value, Is.EqualTo("1"));
            Assert.That(combinedNode.Attributes["inconclusive"]?.Value, Is.EqualTo("5"));
            Assert.That(combinedNode.Attributes["skipped"]?.Value, Is.EqualTo("2"));
            Assert.That(combinedNode.Attributes["asserts"]?.Value, Is.EqualTo("53"));
        }

        [Test]
        public void MergeAndAggregateTestResults()
        {
            TestEngineResult combined = ResultHelper.Merge(twoResults).Aggregate("test-suite", "Project", "ID", "NAME", "FULLNAME");
            Assert.That(combined.IsSingle);

            XmlNode combinedNode = combined.Xml;

            Assert.That(combinedNode.Name, Is.EqualTo("test-suite"));
            Assert.That(combinedNode.Attributes, Is.Not.Null);
            Assert.That(combinedNode.Attributes["type"]?.Value, Is.EqualTo("Project"));
            Assert.That(combinedNode.Attributes["id"]?.Value, Is.EqualTo("ID"));
            Assert.That(combinedNode.Attributes["name"]?.Value, Is.EqualTo("NAME"));
            Assert.That(combinedNode.Attributes["fullname"]?.Value, Is.EqualTo("FULLNAME"));
            Assert.That(combinedNode.Attributes["result"]?.Value, Is.EqualTo("Failed"));
            Assert.That(combinedNode.Attributes["total"]?.Value, Is.EqualTo("65"));
            Assert.That(combinedNode.Attributes["passed"]?.Value, Is.EqualTo("54"));
            Assert.That(combinedNode.Attributes["failed"]?.Value, Is.EqualTo("3"));
            Assert.That(combinedNode.Attributes["warnings"]?.Value, Is.EqualTo("1"));
            Assert.That(combinedNode.Attributes["inconclusive"]?.Value, Is.EqualTo("5"));
            Assert.That(combinedNode.Attributes["skipped"]?.Value, Is.EqualTo("2"));
            Assert.That(combinedNode.Attributes["asserts"]?.Value, Is.EqualTo("93"));
        }

        [Test]
        public void AggregateXmlNodes()
        {
            XmlNode combined = ResultHelper.Aggregate("test-run", "ID", "NAME", "FULLNAME", twoNodes);

            Assert.That(combined.Name, Is.EqualTo("test-run"));
            Assert.That(combined.Attributes, Is.Not.Null);
            Assert.That(combined.Attributes["id"]?.Value, Is.EqualTo("ID"));
            Assert.That(combined.Attributes["name"]?.Value, Is.EqualTo("NAME"));
            Assert.That(combined.Attributes["fullname"]?.Value, Is.EqualTo("FULLNAME"));
            Assert.That(combined.Attributes["result"]?.Value, Is.EqualTo("Failed"));
            Assert.That(combined.Attributes["total"]?.Value, Is.EqualTo("65"));
            Assert.That(combined.Attributes["passed"]?.Value, Is.EqualTo("54"));
            Assert.That(combined.Attributes["failed"]?.Value, Is.EqualTo("3"));
            Assert.That(combined.Attributes["warnings"]?.Value, Is.EqualTo("1"));
            Assert.That(combined.Attributes["inconclusive"]?.Value, Is.EqualTo("5"));
            Assert.That(combined.Attributes["skipped"]?.Value, Is.EqualTo("2"));
            Assert.That(combined.Attributes["asserts"]?.Value, Is.EqualTo("93"));
        }

        [TestCase("Skipped", "Skipped", "Skipped")]
        [TestCase("Passed", "Passed", "Passed")]
        [TestCase("Failed", "Failed", "Failed")]
        [TestCase("Warning", "Warning", "Warning")]
        [TestCase("Skipped", "Passed", "Passed")]
        [TestCase("Passed", "Skipped", "Passed")]
        [TestCase("Skipped", "Failed", "Failed")]
        [TestCase("Failed", "Skipped", "Failed")]
        [TestCase("Skipped", "Warning", "Warning")]
        [TestCase("Warning", "Skipped", "Warning")]
        [TestCase("Passed", "Failed", "Failed")]
        [TestCase("Failed", "Passed", "Failed")]
        [TestCase("Passed", "Warning", "Warning")]
        [TestCase("Warning", "Passed", "Warning")]
        [TestCase("Failed", "Warning", "Failed")]
        [TestCase("Warning", "Failed", "Failed")]
        public void Aggregate_CalculatesAggregateResultCorrectly(string firstResult, string secondResult, string aggregateResult)
        {
            string firstResultText = $"<test-assembly result=\"{firstResult}\" total=\"23\" passed=\"23\" failed=\"0\" inconclusive=\"0\" skipped=\"0\" warnings=\"0\" asserts=\"40\" />";
            string secondResultText = $"<test-assembly result=\"{secondResult}\" total=\"42\" passed=\"31\" failed=\"3\" inconclusive=\"5\" skipped=\"2\" warnings=\"1\" asserts=\"53\" />";

            var firstEngineResult = new TestEngineResult(firstResultText);
            var secondEngineResult = new TestEngineResult(secondResultText);
            var data = new XmlNode[] { firstEngineResult.Xml, secondEngineResult.Xml };
            XmlNode combined = ResultHelper.Aggregate("test-run", "ID", "NAME", "FULLNAME", data);
            Assert.That(combined.Attributes?["result"]?.Value, Is.EqualTo(aggregateResult));
        }
    }
}