// This file contains classes used to interpret the result XML that is
// produced by test runs of the GUI.

using System.Xml;

public class ResultSummary
{
	public ResultSummary(XmlNode testRun)
	{
		OverallResult = GetAttribute(testRun, "result");
		Total = IntAttribute(testRun, "total");
		Passed = IntAttribute(testRun, "passed");
		Failed = IntAttribute(testRun, "failed");
		Warnings = IntAttribute(testRun, "warnings");
		Inconclusive = IntAttribute(testRun, "inconclusive");
		Skipped = IntAttribute(testRun, "skipped");
	}

	protected ResultSummary()
	{
	}

	public string OverallResult { get; set; }
	public int Total  { get; set; }
	public int Passed  { get; set; }
	public int Failed  { get; set; }
	public int Warnings  { get; set; }
	public int Inconclusive  { get; set; }
	public int Skipped { get; set; }

	private string GetAttribute(XmlNode testRun, string name)
	{
		return testRun.Attributes[name]?.Value;
	}

	private int IntAttribute(XmlNode node, string name)
	{
		string s = GetAttribute(node, name);
		return s == null ? 0 : int.Parse(s);
	}
}

public class ExpectedResult : ResultSummary
{
	public ExpectedResult(string overallResult)
	{
		OverallResult = overallResult;
		// Initialize counters to -1, indicating no expected value.
		// Set properties of those items to be checked.
		Total = Passed = Failed = Warnings = Inconclusive = Skipped = -1;
	}

    private int _errorCount;

	public int CheckResult(ResultSummary actual)
	{
        _errorCount = 0;

        if (OverallResult != actual.OverallResult)
            ReportError($"  Expected: Overall Result = {OverallResult}\n   But was: {actual.OverallResult}");
        CheckCounter("Test Count", Total, actual.Total);
        CheckCounter("Passed", Passed, actual.Passed);
        CheckCounter("Failed", Failed, actual.Failed);
        CheckCounter("Warnings", Warnings, actual.Warnings);
        CheckCounter("Inconclusive", Inconclusive, actual.Inconclusive);
        CheckCounter("Skipped", Skipped, actual.Skipped);

        if (_errorCount == 0)
            Console.WriteLine("SUCCESS: Test Result matches expected result!");

		return _errorCount;
	}

    private void CheckCounter(string label, int expected, int actual)
    {
        if (expected > 0 && expected != actual)
            ReportError($"  Expected: {label} = {expected}\n   But was: {actual}");
    }

    private void ReportError(string message)
    {
        if (_errorCount++ == 0)
            Console.WriteLine("ERROR: Test Result not as expected!\n");
        Console.WriteLine(message);
    }
}

public class ResultReporter
{
	private string _resultFile;
	private XmlNode _testRun;

	public ResultReporter(string resultFile)
	{
		_resultFile = resultFile;
	
		var doc = new XmlDocument();
		doc.Load(resultFile);

		_testRun = doc.DocumentElement;
		if (_testRun.Name != "test-run")
			throw new Exception("The test-run element was not found.");

		Summary = new ResultSummary(_testRun);
		
		if (Summary.OverallResult == null)
			throw new Exception("The test-run element has no result attribute.");
	}

	public ResultSummary Summary { get; }

	public int Report(ExpectedResult expectedResult)
	{
		Console.WriteLine("\nTest Run Summary");
		Console.WriteLine("  Overall Result: " + Summary.OverallResult);

		Console.WriteLine($"  Test Count: {Summary.Total}, Passed: {Summary.Passed}, Failed: {Summary.Failed}"
			+$" Warnings: {Summary.Warnings}, Inconclusive: {Summary.Inconclusive}, Skipped: {Summary.Skipped}\n");

		return  expectedResult.CheckResult(Summary);
	}

	private string GetAttribute(XmlNode node, string name)
	{
		return node.Attributes[name]?.Value;
	}
}
