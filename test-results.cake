// This file contains classes used to interpret the result XML that is
// produced by test runs of the GUI.

using System.Xml;

public abstract class ResultSummary
{
    public string OverallResult { get; set; }
    public int Total { get; set; }
    public int Passed { get; set; }
    public int Failed { get; set; }
    public int Warnings { get; set; }
    public int Inconclusive { get; set; }
    public int Skipped { get; set; }
}

public class ExpectedResult : ResultSummary
{
	public ExpectedResult(string overallResult)
	{
        if (string.IsNullOrEmpty(overallResult))
            throw new ArgumentNullException(nameof(overallResult));

		OverallResult = overallResult;
		// Initialize counters to -1, indicating no expected value.
		// Set properties of those items to be checked.
		Total = Passed = Failed = Warnings = Inconclusive = Skipped = -1;
	}
}

public class ActualResult : ResultSummary
{
    public ActualResult(string resultFile)
    {
        var doc = new XmlDocument();
        doc.Load(resultFile);

        Xml = doc.DocumentElement;
        if (Xml.Name != "test-run")
            throw new Exception("The test-run element was not found.");

        OverallResult = GetAttribute(Xml, "result");
        Total = IntAttribute(Xml, "total");
        Passed = IntAttribute(Xml, "passed");
        Failed = IntAttribute(Xml, "failed");
        Warnings = IntAttribute(Xml, "warnings");
        Inconclusive = IntAttribute(Xml, "inconclusive");
        Skipped = IntAttribute(Xml, "skipped");
    }

    public XmlNode Xml { get; }

    private string GetAttribute(XmlNode node, string name)
    {
        return node.Attributes[name]?.Value;
    }

    private int IntAttribute(XmlNode node, string name)
    {
        string s = GetAttribute(node, name);
        return s == null ? 0 : int.Parse(s);
    }
}

public class TestReport
{
    public PackageTest Test;
    public ActualResult Result;
    public List<string> Errors;

    public TestReport(PackageTest test, ActualResult result)
    {
        Test = test;
        Result = result;
        Errors = new List<string>();

        var expected = test.ExpectedResult;

        if (result.OverallResult == null)
            Errors.Add("The test-run element has no result attribute.");
        else if (expected.OverallResult != result.OverallResult)
            Errors.Add($"   Expected: Overall Result = {expected.OverallResult}\n    But was: {result.OverallResult}");
        CheckCounter("Test Count", expected.Total, result.Total);
        CheckCounter("Passed", expected.Passed, result.Passed);
        CheckCounter("Failed", expected.Failed, result.Failed);
        CheckCounter("Warnings", expected.Warnings, result.Warnings);
        CheckCounter("Inconclusive", expected.Inconclusive, result.Inconclusive);
        CheckCounter("Skipped", expected.Skipped, result.Skipped);
    }

    public TestReport(PackageTest test, Exception ex)
    {
        Test = test;
        Result = null;
        Errors = new List<string>();
        Errors.Add($"     {ex.Message}");
    }

    public void Display(int index)
    {
        Console.WriteLine($"\n{index}. {Test.Description}");
        Console.WriteLine($"   Args: {Test.Arguments}\n");

        foreach (var error in Errors)
            Console.WriteLine(error);

        Console.WriteLine(Errors.Count == 0
            ? "   SUCCESS: Test Result matches expected result!"
            : "\n   ERROR: Test Result not as expected!");
    }

    private void CheckCounter(string label, int expected, int actual)
    {
        if (expected > 0 && expected != actual)
            Errors.Add($"     Expected: {label} = {expected}\n      But was: {actual}");
    }
}

public class ResultReporter
{
    private string _packageName;
    private List<TestReport> _reports = new List<TestReport>();

    public ResultReporter(string packageName)
    {
        _packageName = packageName;
    }

    public void AddReport(TestReport report)
    {
        _reports.Add(report);
    }

    public bool ReportResults()
    {
        Console.WriteLine("\n=================================================="); ;
        Console.WriteLine($"Test Results for {_packageName}");
        Console.WriteLine("=================================================="); ;

        Console.WriteLine("\nTest Environment");
        Console.WriteLine($"   OS Version: {Environment.OSVersion.VersionString}");
        Console.WriteLine($"  CLR Version: {Environment.Version}\n");

        int index = 0;
        bool hasErrors = false;

        foreach (var report in _reports)
        {
            hasErrors |= report.Errors.Count > 0;
            report.Display(++index);
        }

        return hasErrors;
    }
}
