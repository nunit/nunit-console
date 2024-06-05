// This file contains classes used to interpret the result XML that is
// produced by test runs of the GUI.

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

	public ExpectedAssemblyResult[] Assemblies { get; set; } = new ExpectedAssemblyResult[0];
}

public class ExpectedAssemblyResult
{
	public ExpectedAssemblyResult(string assemblyName, string agentName = null)
	{
		AssemblyName = assemblyName;
		AgentName = agentName;
	}

	public string AssemblyName { get; }
	public string AgentName { get; }
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
		var assemblies = new List<ActualAssemblyResult>();

		foreach (XmlNode node in Xml.SelectNodes("//test-suite[@type='Assembly']"))
			assemblies.Add(new ActualAssemblyResult(node));

		Assemblies = assemblies.ToArray();
    }

    public XmlNode Xml { get; }

	public ActualAssemblyResult[] Assemblies { get; }

    private string GetAttribute(XmlNode node, string name)
    {
        return node.Attributes[name]?.Value;
    }

    private int IntAttribute(XmlNode node, string name)
    {
        string s = GetAttribute(node, name);
        // TODO: We should replace 0 with -1, representing a missing counter
        // attribute, after issue #904 is fixed.
        return s == null ? 0 : int.Parse(s);
    }
}

public class ActualAssemblyResult
{
	public ActualAssemblyResult(XmlNode xml)
    {
		AssemblyName = xml.Attributes["name"]?.Value;

		//var env = xml.SelectSingleNode("environment");
		var settings = xml.SelectSingleNode("settings");

		// If TargetRuntimeFramework setting is not present, the GUI will probably crash
		// However, it is optional when using tc-lite directly.
		var runtimeSetting = settings?.SelectSingleNode("setting[@name='TargetRuntimeFramework']");
		TargetRuntime = runtimeSetting?.Attributes["value"]?.Value;

		var agentSetting = settings?.SelectSingleNode("setting[@name='SelectedAgentName']");
		AgentName = agentSetting?.Attributes["value"]?.Value;
	}

	public string AssemblyName { get; }
	public string AgentName { get; }

	public string TargetRuntime { get; }
}

public class TestReport
{
    public PackageTest Test;
    public ActualResult Result;
    public List<string> Errors;

    public TestReport(PackageTest test, ActualResult actualResult)
    {
		if (actualResult == null)
			throw new ArgumentNullException(nameof(actualResult));

        Test = test;
        Result = actualResult;
        Errors = new List<string>();

        var expectedResult = test.ExpectedResult;

        ReportMissingFiles();

        if (actualResult.OverallResult == null)
            Errors.Add("     The test-run element has no result attribute.");
        else if (expectedResult.OverallResult != actualResult.OverallResult)
            Errors.Add($"     Expected: Overall Result = {expectedResult.OverallResult}\n    But was: {actualResult.OverallResult}");
        CheckCounter("Test Count", expectedResult.Total, actualResult.Total);
        CheckCounter("Passed", expectedResult.Passed, actualResult.Passed);
        CheckCounter("Failed", expectedResult.Failed, actualResult.Failed);
        CheckCounter("Warnings", expectedResult.Warnings, actualResult.Warnings);
        CheckCounter("Inconclusive", expectedResult.Inconclusive, actualResult.Inconclusive);
        CheckCounter("Skipped", expectedResult.Skipped, actualResult.Skipped);

		var expectedAssemblies = expectedResult.Assemblies;
		var actualAssemblies = actualResult.Assemblies;

		for (int i = 0; i < expectedAssemblies.Length && i < actualAssemblies.Length; i++)
        {
			var expected = expectedAssemblies[i];
			var actual = actualAssemblies[i];

			if (expected.AssemblyName != actual.AssemblyName)
				Errors.Add($"   Expected: {expected.AssemblyName}\r\n    But was: { actual.AssemblyName ?? "<null>"}");
			else if (expected.AgentName != null && actual.AgentName != null && expected.AgentName != actual.AgentName)
				Errors.Add($"   Assembly {actual.AssemblyName}\r\n     Expected: {expected.AgentName}\r\n      But was: {actual.AgentName ?? "<null>"}");
        }

		for (int i = actualAssemblies.Length; i < expectedAssemblies.Length; i++)
			Errors.Add($"   Assembly {expectedAssemblies[i].AssemblyName} was not found");

		for (int i = expectedAssemblies.Length; i < actualAssemblies.Length; i++)
			Errors.Add($"   Found unexpected assembly {actualAssemblies[i].AssemblyName}");
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

    // File level errors, like missing or mal-formatted files, need to be highlighted
    // because otherwise it's hard to detect the cause of the problem without debugging.
    // This method finds and reports that type of error.
    private void ReportMissingFiles()
    {
        // Start with all the top-level test suites. Note that files that
        // cannot be found show up as Unknown as do unsupported file types.
        var suites = Result.Xml.SelectNodes(
            "//test-suite[@type='Unknown'] | //test-suite[@type='Project'] | //test-suite[@type='Assembly']");

        // If there is no top-level suite, it generally means the file format could not be interpreted
        if (suites.Count == 0)
            Errors.Add("     No top-level suites! Possible empty command-line or misformed project.");

        foreach (XmlNode suite in suites)
        {
            // Narrow down to the specific failures we want
            string runState = GetAttribute(suite, "runstate");
            string suiteResult = GetAttribute(suite, "result");
            string label = GetAttribute(suite, "label");
            string site = suite.Attributes["site"]?.Value ?? "Test";
            if (runState == "NotRunnable" || suiteResult == "Failed" && site == "Test" && (label == "Invalid" || label=="Error"))
            {
                string message = suite.SelectSingleNode("reason/message")?.InnerText;
                Errors.Add($"     {message}");
            }
        }
    }

    private void CheckCounter(string label, int expected, int actual)
    {
        // If expected value of counter is negative, it means no check is needed
        if (expected >=0 && expected != actual)
            Errors.Add($"     Expected: {label} = {expected}\n      But was: {actual}");
    }

    private string GetAttribute(XmlNode node, string name)
    {
        return node.Attributes[name]?.Value;
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
        DisplayBanner($"Test Results for {_packageName}");

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
