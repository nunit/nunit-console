// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

/// <summary>
/// Selects test categories within the existing NUnitLite assemblies. The normal
/// Test target runs all completed regressions; only explicitly pending acceptance
/// tests are excluded. Each assembly and suite writes a separate NUnit report.
/// </summary>
public sealed class TestSuiteRunner : NUnitLiteRunner, IUnitTestRunner
{
    private string _suite = "Required";

    /// <summary>
    /// Runs one development suite and restores the default selection afterward,
    /// including when tests fail or several script targets are requested together.
    /// </summary>
    public void RunSuite(string suite)
    {
        string previous = _suite;
        _suite = suite;
        try
        {
            UnitTesting.RunAllTests();
        }
        finally
        {
            _suite = previous;
        }
    }

    /// <summary>
    /// Reimplements the recipe's runner interface while preserving its NUnitLite
    /// executable discovery. Unexpected process exits and invalid reports fail
    /// even when the selected suite is pending acceptance work.
    /// </summary>
    public new int RunUnitTest(FilePath testPath)
    {
        string filter = _suite switch
        {
            "Unit" => "cat != Integration && cat != Acceptance && cat != PendingAcceptance",
            "Integration" => "cat == Integration && cat != PendingAcceptance",
            "Acceptance" => "cat == Acceptance && cat != PendingAcceptance",
            "PendingAcceptance" => "cat == PendingAcceptance",
            "Required" => "cat != PendingAcceptance",
            _ => throw new ArgumentException("Unknown test suite: " + _suite)
        };

        string resultDirectory = BuildSettings.PackageResultDirectory + "tests/"
            + BuildSettings.Configuration + "/" + _suite + "/"
            + testPath.GetDirectory().GetDirectoryName();
        Context.CreateDirectory(resultDirectory);
        string resultPath = resultDirectory + "/" + testPath.GetFilenameWithoutExtension() + ".xml";
        if (System.IO.File.Exists(resultPath))
            System.IO.File.Delete(resultPath);

        var settings = new ProcessSettings
        {
            Arguments = "--where=\"" + filter + "\" --result=\"" + resultPath + "\""
        };
        if (CommandLineOptions.TraceLevel.Exists)
            settings.EnvironmentVariables = new Dictionary<string, string>
            {
                { "NUNIT_INTERNAL_TRACE_LEVEL", CommandLineOptions.TraceLevel.Value }
            };

        int exitCode = base.RunUnitTest(testPath, settings);
        // A fresh report is required even when the process reports success.
        // This also prevents a stale report from disguising a crashed runner.
        var report = System.Xml.Linq.XDocument.Load(resultPath).Root;
        if (report == null || report.Name != "test-run")
            throw new InvalidOperationException("No NUnit test-run report: " + resultPath);

        int? failed = (int?)report.Attribute("failed");
        int? total = (int?)report.Attribute("total");
        string outcome = (string)report.Attribute("result");
        if (failed == null || failed < 0 || total == null || total < failed
            || !new[] { "Passed", "Failed", "Warning", "Inconclusive", "Skipped" }.Contains(outcome)
            || (exitCode == 0 && failed != 0)
            || report.Descendants("test-case").Any(test => (string)test.Attribute("label") == "Invalid"))
            throw new InvalidOperationException("Invalid NUnit test-run report: " + resultPath);

        if (_suite == "PendingAcceptance" && exitCode > 0
            && outcome == "Failed" && failed == exitCode)
        {
            Context.Warning("Pending acceptance failures: {0}. Report: {1}", exitCode, resultPath);
            return 0;
        }

        return exitCode;
    }
}
