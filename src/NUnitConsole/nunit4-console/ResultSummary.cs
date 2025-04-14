﻿// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using System;
using System.Globalization;
using System.Xml;
using NUnit.ConsoleRunner.Utilities;

namespace NUnit.ConsoleRunner
{
    /// <summary>
    /// Summary description for ResultSummary.
    /// </summary>
    public class ResultSummary
    {
        public ResultSummary(XmlNode result)
        {
            if (result.Name != "test-run")
                throw new InvalidOperationException("Expected <test-run> as top-level element but was <" + result.Name + ">");

            InitializeCounters();

            Summarize(result, false);
        }

        /// <summary>
        /// Gets the number of test cases for which results
        /// have been summarized. Any tests excluded by use of
        /// Category or Explicit attributes are not counted.
        /// </summary>
        public int TestCount { get; private set; }

        /// <summary>
        /// Returns the number of test cases actually run.
        /// </summary>
        public int RunCount
        {
            get { return PassCount + FailureCount + ErrorCount + InconclusiveCount;  }
        }

        /// <summary>
        /// Returns the number of test cases not run for any reason.
        /// </summary>
        public int NotRunCount
        {
            get { return IgnoreCount + ExplicitCount + InvalidCount + SkipCount;  }
        }

        /// <summary>
        /// Returns the number of failed test cases (including errors and invalid tests)
        /// </summary>
        public int FailedCount
        {
            get { return FailureCount + InvalidCount + ErrorCount;  }
        }

        public int WarningCount { get; private set; }

        /// <summary>
        /// Returns the sum of skipped test cases, including ignored and explicit tests
        /// </summary>
        public int TotalSkipCount
        {
            get { return SkipCount + IgnoreCount + ExplicitCount;  }
        }

        /// <summary>
        /// Gets the count of passed tests
        /// </summary>
        public int PassCount { get; private set; }

        /// <summary>
        /// Gets the count of failed tests, excluding errors and invalid tests
        /// </summary>
        public int FailureCount { get; private set; }

        /// <summary>
        /// Returns the number of test cases that had an error.
        /// </summary>
        public int ErrorCount { get; private set; }

        /// <summary>
        /// Gets the count of inconclusive tests
        /// </summary>
        public int InconclusiveCount { get; private set; }

        /// <summary>
        /// Returns the number of test cases that were not runnable
        /// due to errors in the signature of the class or method.
        /// Such tests are also counted as Errors.
        /// </summary>
        public int InvalidCount { get; private set; }

        /// <summary>
        /// Gets the count of skipped tests, excluding ignored and explicit tests
        /// </summary>
        public int SkipCount { get; private set; }

        /// <summary>
        /// Gets the count of ignored tests
        /// </summary>
        public int IgnoreCount { get; private set; }

        /// <summary>
        /// Gets the count of tests not run because the are Explicit
        /// </summary>
        public int ExplicitCount { get; private set; }

        /// <summary>
        /// Gets the count of invalid assemblies
        /// </summary>
        public int InvalidAssemblies { get; private set; }

        /// <summary>
        /// An Unexpected error occurred
        /// </summary>
        public bool UnexpectedError { get; private set; }

        /// <summary>
        /// Invalid test fixture(s) were found
        /// </summary>
        public int InvalidTestFixtures { get; private set; }

        private void InitializeCounters()
        {
            TestCount = 0;
            PassCount = 0;
            FailureCount = 0;
            WarningCount = 0;
            ErrorCount = 0;
            InconclusiveCount = 0;
            SkipCount = 0;
            IgnoreCount = 0;
            ExplicitCount = 0;
            InvalidCount = 0;
            InvalidAssemblies = 0;
        }

        private void Summarize(XmlNode node, bool failedInFixtureTearDown)
        {
            string? type = node.GetAttribute("type");
            string? status = node.GetAttribute("result");
            string? label = node.GetAttribute("label");
            string? site = node.GetAttribute("site");

            switch (node.Name)
            {
                case "test-case":
                    TestCount++;

                    switch (status)
                    {
                        case "Passed":
                            if (failedInFixtureTearDown)
                                ErrorCount++;
                            else
                                PassCount++;
                            break;
                        case "Failed":
                            if (failedInFixtureTearDown)
                                ErrorCount++;
                            else if (label is null)
                                FailureCount++;
                            else if (label == "Invalid")
                                InvalidCount++;
                            else
                                ErrorCount++;
                            break;
                        case "Warning":
                            if (failedInFixtureTearDown)
                                ErrorCount++;
                            else
                                WarningCount++;
                            break;
                        case "Inconclusive":
                            if (failedInFixtureTearDown)
                                ErrorCount++;
                            else
                                InconclusiveCount++;
                            break;
                        case "Skipped":
                            if (label == "Ignored")
                                IgnoreCount++;
                            else if (label == "Explicit")
                                ExplicitCount++;
                            else
                                SkipCount++;
                            break;
                        default:
                            //SkipCount++;
                            break;
                    }
                    break;

                case "test-suite":
                    if (status == "Failed" && label == "Invalid")
                    {
                        if (type == "Assembly")
                            InvalidAssemblies++;
                        else
                            InvalidTestFixtures++;
                    }
                    if (type == "Assembly" && status == "Failed" && label == "Error")
                    {
                        InvalidAssemblies++;
                        UnexpectedError = true;
                    }
                    if ((type == "SetUpFixture" || type == "TestFixture") && status == "Failed" && label == "Error" && site == "TearDown")
                    {
                        failedInFixtureTearDown = true;
                    }

                    Summarize(node.ChildNodes, failedInFixtureTearDown);
                    break;

                case "test-run":
                    Summarize(node.ChildNodes, failedInFixtureTearDown);
                    break;
            }
        }

        private void Summarize(XmlNodeList nodes, bool failedInFixtureTearDown)
        {
            foreach (XmlNode childResult in nodes)
                Summarize(childResult, failedInFixtureTearDown);
        }
    }
}
