// This file contains classes encapsulating the result XML that is
// produced by our package tests.

public abstract class TestResultSummary
{
    public string OverallResult { get; set; }
    public int Total { get; set; }
    public int Passed { get; set; }
    public int Failed { get; set; }
    public int Warnings { get; set; }
    public int Inconclusive { get; set; }
    public int Skipped { get; set; }
}

public class ExpectedResult : TestResultSummary
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
    public ExpectedAssemblyResult(string name, string expectedRuntime = null, string expectedAgent = null)
    {
        AssemblyName = name;
        Runtime = expectedRuntime;
        AgentName = expectedAgent;
    }

    public string AssemblyName { get; }
    public string Runtime { get; }
    public string AgentName { get; }
}

public class ActualResult : TestResultSummary
{
    public ActualResult(string resultFile)
    {
        var doc = new XmlDocument();
        doc.Load(resultFile);

        Xml = doc.DocumentElement;
        if (Xml.Name != "test-run" && Xml.Name != "test-suite")
            throw new Exception("Top-level <test-run> or <test-suite> element not found.");

        OverallResult = GetAttribute(Xml, "result");
        Total = IntAttribute(Xml, "total");
        Passed = IntAttribute(Xml, "passed");
        Failed = IntAttribute(Xml, "failed");
        Warnings = IntAttribute(Xml, "warnings");
        Inconclusive = IntAttribute(Xml, "inconclusive");
        Skipped = IntAttribute(Xml, "skipped");

        var assemblies = new List<ActualAssemblyResult>();

        foreach (XmlNode node in Xml.SelectNodes("//test-suite[@type='Unknown'] | //test-suite[@type='Assembly'] | //test-suite[@type='Project']"))
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
        // attribute, after issue #707 is fixed.
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

        // If TargetRuntimeFramework setting is not present, the Runner will probably crash
        var runtimeSetting = settings?.SelectSingleNode("setting[@name='TargetRuntimeFramework']");
        Runtime = runtimeSetting?.Attributes["value"]?.Value;

        var agentSetting = settings?.SelectSingleNode("setting[@name='SelectedAgentName']");
        AgentName = agentSetting?.Attributes["value"]?.Value;
    }

    public string AssemblyName { get; }
    public string AgentName { get; }

    public string Runtime { get; }
}

#if false
public class ActualAssemblyResult
{
	public ActualAssemblyResult(XmlNode xml, Version engineVersion)
    {
		Name = xml.Attributes["name"]?.Value;

		var env = xml.SelectSingleNode("environment");
		var settings = xml.SelectSingleNode("settings");

		// If TargetRuntimeFramework setting is not present, the GUI will have crashed anyway
		var runtimeSetting = settings.SelectSingleNode("setting[@name='ImageTargetFrameworkName']");
		TargetRuntime = runtimeSetting?.Attributes["value"]?.Value;

		// Engine 3.10 and earlier doesn't provide enough info
		if (engineVersion >= new Version(3,11,0,0))
			Runtime = DeduceActualRuntime(xml);
	}

	public string Name { get; }
	public string Runtime { get; }

	public string TargetRuntime { get; }

	// Code to determine the runtime actually used is adhoc
	// and doesn't work for all assemblies because the needed
    // information may not be present in the result file.
	// TODO: Modify result file schema so this can be cleaner
	private static string DeduceActualRuntime(XmlNode assembly)
	{
		var env = assembly.SelectSingleNode("environment");
		// The TargetRuntimeFramework setting is only present
        // under the 3.12 and later versions of the engine.
		var runtimeSetting = assembly.SelectSingleNode("settings/setting[@name='TargetRuntimeFramework']");
		var targetRuntime = runtimeSetting?.Attributes["value"]?.Value;
		if (targetRuntime != null)
			return targetRuntime;

		var clrVersion = env.Attributes["clr-version"]?.Value;
		if (clrVersion == null)
			return null;

		if (clrVersion.StartsWith("2.0"))
			return "net-2.0";
		if (clrVersion.StartsWith("4.0"))
			return "net-4";

		return null;
	}
}
#endif
