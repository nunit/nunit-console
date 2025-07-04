﻿// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using System.Collections.Generic;
using System.Xml;

namespace NUnit.Engine
{
    /// <summary>
    /// ResultHelper provides static methods for working with
    /// TestEngineResults to wrap, combiner and aggregate them
    /// in various ways.
    /// </summary>
    public static class ResultHelper
    {
        private const string TEST_RUN_ELEMENT = "test-run";
        private const string TEST_SUITE_ELEMENT = "test-suite";
        private const string PROJECT_SUITE_TYPE = "Project";

        /// <summary>
        /// Aggregate the XmlNodes under a TestEngineResult into a single XmlNode.
        /// </summary>
        /// <param name="result">A new TestEngineResult with xml nodes for each assembly or project.</param>
        /// <param name="elementName">The root node name under which to aggregate the nodes.</param>
        /// <param name="suiteType">The suite type to associate with the <see cref="TestEngineResult"/>.</param>
        /// <param name="id">The id of the <see cref="TestEngineResult"/></param>
        /// <param name="name">The name of the <see cref="TestEngineResult"/>.</param>
        /// <param name="fullName">The full name of the <see cref="TestEngineResult"/>.</param>
        /// <returns>A TestEngineResult with a single top-level element.</returns>
        public static TestEngineResult Aggregate(this TestEngineResult result, string elementName, string suiteType, string id, string? name, string? fullName)
        {
            return new TestEngineResult(Aggregate(elementName, suiteType, id, name, fullName, result.XmlNodes));
        }

        /// <summary>
        /// Aggregate the XmlNodes under a TestEngineResult into a single XmlNode.
        /// </summary>
        /// <param name="result">A new TestEngineResult with xml nodes for each assembly or project.</param>
        /// <param name="elementName">The root node name under which to aggregate the results.</param>
        /// <param name="id">The id of the <see cref="TestEngineResult"/></param>
        /// <param name="name">The name of the <see cref="TestEngineResult"/>.</param>
        /// <param name="fullName">The full name of the <see cref="TestEngineResult"/>.</param>
        /// <returns>A TestEngineResult with a single top-level element.</returns>
        public static TestEngineResult Aggregate(this TestEngineResult result, string elementName, string id, string? name, string? fullName)
        {
            return new TestEngineResult(Aggregate(elementName, id, name, fullName, result.XmlNodes));
        }

        /// <summary>
        /// Aggregate all the separate assembly results of a project as a single node.
        /// </summary>
        /// <param name="result">A new TestEngineResult with xml nodes for each assembly or project.</param>
        /// <param name="package">The <see cref="TestPackage"/> for which we are aggregating.</param>
        /// <returns>A TestEngineResult with a single top-level element.</returns>
        public static TestEngineResult MakeProjectResult(this TestEngineResult result, TestPackage package)
        {
            return Aggregate(result, TEST_SUITE_ELEMENT, PROJECT_SUITE_TYPE, package.ID, package.Name, package.FullName);
        }

        /// <summary>
        /// Aggregate all the separate assembly results of a test run as a single node.
        /// </summary>
        /// <param name="result">A new TestEngineResult with xml nodes for each assembly or project.</param>
        /// <param name="package">The <see cref="TestPackage"/> for which we are aggregating.</param>
        /// <returns>A TestEngineResult with a single top-level element.</returns>
        public static TestEngineResult MakeTestRunResult(this TestEngineResult result, TestPackage package)
        {
            return Aggregate(result, TEST_RUN_ELEMENT, package.ID, package.Name, package.FullName);
        }

        /// <summary>
        /// Merges multiple test engine results into a single result. The
        /// result element contains all the XML nodes found in the input.
        /// </summary>
        /// <param name="results">A collection of <see cref="TestEngineResult"/> to merge.</param>
        /// <returns>A TestEngineResult merging all the input results.</returns>
        /// <remarks>Used by AbstractTestRunner MakePackageResult method.</remarks>
        public static TestEngineResult Merge(IList<TestEngineResult> results)
        {
            var mergedResult = new TestEngineResult();

            foreach (TestEngineResult result in results)
                foreach (XmlNode node in result.XmlNodes)
                    mergedResult.Add(node);

            return mergedResult;
        }

        /// <summary>
        /// Aggregates a collection of XmlNodes under a single XmlNode.
        /// </summary>
        /// <param name="elementName">The root node name under which to aggregate the results.</param>
        /// <param name="name">The name to associated with the root node.</param>
        /// <param name="fullName">The full name to associated with the root node.</param>
        /// <param name="resultNodes">A collection of XmlNodes to aggregate</param>
        /// <returns>A single XmlNode containing the aggregated list of XmlNodes.</returns>
        public static XmlNode Aggregate(string elementName, string id, string? name, string? fullName, IList<XmlNode> resultNodes)
        {
            return Aggregate(elementName, null, id, name, fullName, resultNodes);
        }

        /// <summary>
        /// Aggregates a collection of XmlNodes under a single XmlNode.
        /// </summary>
        /// <param name="elementName">The root node name under which to aggregate the results.</param>
        /// <param name="testType">The type to associated with the root node.</param>
        /// <param name="name">The name to associated with the root node.</param>
        /// <param name="fullName">The full name to associated with the root node.</param>
        /// <param name="resultNodes">A collection of XmlNodes to aggregate</param>
        /// <returns>A single XmlNode containing the aggregated list of XmlNodes.</returns>
        public static XmlNode Aggregate(string elementName, string? testType, string id, string? name, string? fullName, IList<XmlNode> resultNodes)
        {
            XmlNode combinedNode = XmlHelper.CreateTopLevelElement(elementName);
            if (testType is not null)
                combinedNode.AddAttribute("type", testType);
            combinedNode.AddAttribute("id", id);
            if (name is not null && name != string.Empty)
                combinedNode.AddAttribute("name", name);
            if (fullName is not null && fullName != string.Empty)
                combinedNode.AddAttribute("fullname", fullName);
            combinedNode.AddAttribute("runstate", "Runnable"); // If not, we would not have gotten this far

            string aggregateResult = "Inconclusive";
            string? aggregateLabel = null;
            string? aggregateSite = null;

            //double totalDuration = 0.0d;
            int testcasecount = 0;
            int total = 0;
            int passed = 0;
            int failed = 0;
            int warnings = 0;
            int inconclusive = 0;
            int skipped = 0;
            int asserts = 0;

            bool isTestRunResult = false;

            foreach (XmlNode node in resultNodes)
            {
                testcasecount += node.GetAttribute("testcasecount", 0);

                XmlAttribute? resultAttribute = node.Attributes?["result"];
                if (resultAttribute is not null)
                {
                    isTestRunResult = true;

                    string? label = node.GetAttribute("label");

                    switch (resultAttribute.Value)
                    {
                        case "Skipped":
                            if (aggregateResult == "Inconclusive" || aggregateResult == "Passed" && label == "Ignored")
                            {
                                aggregateResult = "Skipped";
                                aggregateLabel = label;
                            }
                            break;
                        case "Passed":
                            if (aggregateResult != "Failed" && aggregateLabel != "Ignored" && aggregateResult != "Warning")
                                aggregateResult = "Passed";
                            break;
                        case "Failed":
                            aggregateResult = "Failed";
                            aggregateLabel = label;
                            if (elementName == "test-suite")
                                aggregateSite = "Child";
                            break;
                        case "Warning":
                            if (aggregateResult == "Inconclusive" || aggregateResult == "Passed" || aggregateResult == "Skipped")
                                aggregateResult = "Warning";
                            break;
                    }

                    total += node.GetAttribute("total", 0);
                    passed += node.GetAttribute("passed", 0);
                    failed += node.GetAttribute("failed", 0);
                    warnings += node.GetAttribute("warnings", 0);
                    inconclusive += node.GetAttribute("inconclusive", 0);
                    skipped += node.GetAttribute("skipped", 0);
                    asserts += node.GetAttribute("asserts", 0);
                }

                if (combinedNode.OwnerDocument is not null)
                {
                    XmlNode import = combinedNode.OwnerDocument.ImportNode(node, true);
                    combinedNode.AppendChild(import);
                }
            }

            combinedNode.AddAttribute("testcasecount", testcasecount.ToString());

            if (isTestRunResult)
            {
                combinedNode.AddAttribute("result", aggregateResult);
                if (aggregateLabel is not null)
                    combinedNode.AddAttribute("label", aggregateLabel);
                if (aggregateSite is not null)
                    combinedNode.AddAttribute("site", aggregateSite);

                //combinedNode.AddAttribute("duration", totalDuration.ToString("0.000000", NumberFormatInfo.InvariantInfo));
                combinedNode.AddAttribute("total", total.ToString());
                combinedNode.AddAttribute("passed", passed.ToString());
                combinedNode.AddAttribute("failed", failed.ToString());
                combinedNode.AddAttribute("warnings", warnings.ToString());
                combinedNode.AddAttribute("inconclusive", inconclusive.ToString());
                combinedNode.AddAttribute("skipped", skipped.ToString());
                combinedNode.AddAttribute("asserts", asserts.ToString());
            }

            return combinedNode;
        }
    }
}
