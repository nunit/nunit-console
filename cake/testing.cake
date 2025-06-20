/////////////////////////////////////////////////////////////////////////////
// TEST RUNNER INTERFACES
/////////////////////////////////////////////////////////////////////////////

/// <Summary>
/// A runner capable of running unit tests
/// </Summary>
public interface IUnitTestRunner
{
    string PackageId { get; }
    string Version { get; }
    
	int RunUnitTest(FilePath testPath);
}

/// <Summary>
/// A runner capable of running package tests
/// </Summary>
public interface IPackageTestRunner
{
    string PackageId { get; }
    string Version { get; }

    string Output { get; }

    int RunPackageTest(string arguments, bool redirectOutput = false);
}

/////////////////////////////////////////////////////////////////////////////
// TEST RUNNER BASE CLASS
/////////////////////////////////////////////////////////////////////////////

/// <summary>
/// The TestRunner class is the abstract base for all TestRunners used to run unit-
/// or package-tests. A TestRunner knows how to run both types of tests. The reason
/// for this design is that some derived runners are used for unit tests, others for
/// package tests and still others for both types. Derived classes implement one or
/// both interfaces to indicate what they support.
/// </summary>
public abstract class TestRunner
{
	protected ICakeContext Context => BuildSettings.Context;

	public string PackageId { get; protected set; }
	public string Version { get; protected set; }

	public string Output { get; private set; }

    protected int RunPackageTest(FilePath executablePath, string arguments = null, bool redirectOutput = false)
    {
        return RunPackageTest(executablePath, new ProcessSettings { Arguments = arguments, RedirectStandardOutput = redirectOutput });
    }

    protected int RunUnitTest(FilePath executablePath, ProcessSettings processSettings)
    {
        if (executablePath == null)
            throw new ArgumentNullException(nameof(executablePath));

        if (processSettings == null)
            throw new ArgumentNullException(nameof(processSettings));

        // Add default values to settings if not present
        if (processSettings.WorkingDirectory == null)
            processSettings.WorkingDirectory = BuildSettings.OutputDirectory;

        var traceLevel = CommandLineOptions.TraceLevel.Value;
        if (traceLevel != "Off")
            processSettings.Arguments.Append($" --trace:{traceLevel}");

        Console.WriteLine($"Arguments: {processSettings.Arguments.Render()}");
        return Context.StartProcess(executablePath, processSettings);
    }

	protected int RunPackageTest(FilePath executablePath, ProcessSettings processSettings)
	{
		if (executablePath == null)
			throw new ArgumentNullException(nameof(executablePath));

        if (processSettings == null)
            throw new ArgumentNullException(nameof(processSettings));
        
		// Add default values to settings if not present
        if (processSettings.WorkingDirectory == null)
			processSettings.WorkingDirectory = BuildSettings.OutputDirectory;

		// Was Output Requested?
		if (processSettings.RedirectStandardOutput)
			processSettings.RedirectedStandardOutputHandler = OutputHandler;

		IEnumerable<string> output;
		// If Redirected Output was not requested, output will be null
		int rc = Context.StartProcess(executablePath, processSettings, out output);
		Output = output != null ? string.Join("\r\n", output) : null;
		return rc;
    }

	internal string OutputHandler(string output)
	{
		// Ensure that package test output displays and is also re-directed.
		// If the derived class doesn't need the output, it doesn't retrieve it.
		Console.WriteLine(output);
		return output;
	}
}

/// <Summary>
/// A TestRunner requiring some sort of installation before use.
/// </Summary>
public abstract class InstallableTestRunner : TestRunner
{
	protected InstallableTestRunner(string packageId, string version)
	{
		PackageId = packageId;
		Version = version;
	}

	//private bool IsChocolateyPackage => PackageId.Contains('-'); // Hack!

	protected FilePath ExecutableRelativePath { get; set; }
	protected bool IsDotNetTool { get; set; } = false;

	// Path under tools directory where package would be installed by Cake #tool directive.
	// NOTE: When used to run unit tests, a #tool directive is required. If derived package
	// is only used for package tests, it is optional.
	protected DirectoryPath ToolInstallDirectory => IsDotNetTool
		? BuildSettings.ToolsDirectory
		: BuildSettings.ToolsDirectory + $"{PackageId}.{Version}";
	protected bool IsInstalledAsTool =>
		Context.DirectoryExists(ToolInstallDirectory);
	
	protected DirectoryPath InstallDirectory;

	public FilePath ExecutablePath => InstallDirectory.CombineWithFilePath(ExecutableRelativePath);

	public void Install(DirectoryPath installDirectory)
	{
		Context.Information($"Installing runner {PackageId} {Version} to directory {installDirectory}");
		InstallDirectory = installDirectory.Combine($"{PackageId}.{Version}");
		Context.CreateDirectory(InstallDirectory);

		// If the runner package is already installed as a cake tool, we just copy it
		if (IsInstalledAsTool)
			if (IsDotNetTool)
			{
				Context.CopyFileToDirectory(BuildSettings.ToolsDirectory + ExecutableRelativePath, InstallDirectory);
				Context.CopyDirectory(BuildSettings.ToolsDirectory + ".store", InstallDirectory);
			}
			else
				Context.CopyDirectory(ToolInstallDirectory, InstallDirectory);
		// Otherwise, we install it to the requested location
        else
            Context.NuGetInstall(
                PackageId,
                new NuGetInstallSettings() { OutputDirectory = installDirectory, Version = Version });
    }
}

/////////////////////////////////////////////////////////////////////////////
// NUNITLITE RUNNER
/////////////////////////////////////////////////////////////////////////////

// For NUnitLite tests, the test is run directly
public class NUnitLiteRunner : TestRunner, IUnitTestRunner
{
    public int RunUnitTest(FilePath testPath)
    {
        var processSettings = new ProcessSettings { Arguments = BuildSettings.UnitTestArguments };
        if (CommandLineOptions.TraceLevel.Exists)
            processSettings.EnvironmentVariables = new Dictionary<string, string>
            {
                { "NUNIT_INTERNAL_TRACE_LEVEL", CommandLineOptions.TraceLevel.Value }
            };

        return base.RunUnitTest(testPath, processSettings);
    }
}

/////////////////////////////////////////////////////////////////////////////
// AgentSelector
/////////////////////////////////////////////////////////////////////////////

public class AgentSelector : TestRunner, IPackageTestRunner
{
    private DirectoryPath _agentBaseDirectory;

    public AgentSelector(string agentBaseDirectory)
    {
        if (agentBaseDirectory == null)
            throw new ArgumentNullException("Null argument", nameof(agentBaseDirectory));

        _agentBaseDirectory = agentBaseDirectory;
    }

    public int RunPackageTest(string arguments, bool redirectOutput = false)
    {
        if (!SIO.Directory.Exists(_agentBaseDirectory.ToString()))
            throw new DirectoryNotFoundException($"Directory not found: {_agentBaseDirectory}");

        bool isX86 = arguments.Contains("x86");
        arguments = arguments.Replace("--x86", string.Empty);

        if (arguments.Contains("net462"))
        {
            var agentPath = _agentBaseDirectory.CombineWithFilePath(isX86 ? "net462/nunit-agent-net462-x86.exe" : "net462/nunit-agent-net462.exe");
            var settings = new ProcessSettings() { Arguments = arguments, RedirectStandardOutput = redirectOutput };
            return base.RunPackageTest(agentPath, settings);
        }
        else // must be "net8.0"
        {
            var agentPath = _agentBaseDirectory.CombineWithFilePath("net8.0/nunit-agent-net80.dll");
            var dotnetExe = (isX86 ? Dotnet.X86Executable : Dotnet.Executable) ?? "dotnet";
            var settings = new ProcessSettings() { Arguments = $"\"{agentPath}\" {arguments}", RedirectStandardOutput = redirectOutput };
            return base.RunPackageTest(dotnetExe, settings);
        }
    }
}

//////////////////////////////////////////////////////////////////////
// UNIT TEST RUNNER
//////////////////////////////////////////////////////////////////////

public static class UnitTesting
{
    static ICakeContext _context;
    static IUnitTestRunner _runner;

    static UnitTesting()
    {
        _context = BuildSettings.Context;
        _runner = BuildSettings.UnitTestRunner ?? new NUnitLiteRunner();
    }

    public static void RunAllTests()
    {
        var unitTests = FindUnitTestFiles(BuildSettings.UnitTests);

        _context.Information($"Located {unitTests.Count} unit test assemblies.");
        var errors = new List<string>();

        foreach (var testPath in unitTests)
        {
            var testFile = testPath.GetFilename();
            var containingDir = testPath.GetDirectory().GetDirectoryName();
            var runtime = IsValidRuntime(containingDir) ? containingDir : null;

            Banner.Display(runtime != null
                ? $"Running {testFile} under {runtime}"
                : $"Running {testFile}");

            int rc = _runner.RunUnitTest(testPath);

            var name = runtime != null
                ? $"{testFile}({runtime})"
                : testFile;
            if (rc > 0)
                errors.Add($"{name}: {rc} tests failed");
            else if (rc < 0)
                errors.Add($"{name} returned rc = {rc}");
        }

        if (unitTests.Count == 0)
            _context.Warning("No unit tests were found");
        else if (errors.Count > 0)
            throw new Exception(
                "One or more unit tests failed, breaking the build.\r\n"
                + errors.Aggregate((x, y) => x + "\r\n" + y));

        static bool IsValidRuntime(string text)
        {
            string[] VALID_RUNTIMES = {
                "net20", "net30", "net35", "net40", "net45", "net451", "net451",
                "net46", "net461", "net462", "net47", "net471", "net472", "net48", "net481",
                "netcoreapp1.1", "netcoreapp2.1", "netcoreapp3.1",
                "net5.0", "net6.0", "net7.0", "net8.0"
            };

            return VALID_RUNTIMES.Contains(text);
        }
    }

    private static List<FilePath> FindUnitTestFiles(string patternSet)
    {
        var result = new List<FilePath>();

        if (!string.IsNullOrEmpty(patternSet))
        {
            // User supplied a set of patterns for the unit tests
            foreach (string filePattern in patternSet.Split('|'))
                foreach (var testPath in _context.GetFiles(BuildSettings.OutputDirectory + filePattern))
                    result.Add(testPath);
        }
        else
        {
            // Use default patterns to find unit tests - case insensitive because
            // we don't know how the user may have named test assemblies.
            var defaultPatterns = _runner is NUnitLiteRunner
                ? new[] { "**/*.tests.exe" }
                : new[] { "**/*.tests.dll", "**/*.tests.exe" };
            var globberSettings = new GlobberSettings { IsCaseSensitive = false };
            foreach (string filePattern in defaultPatterns)
                foreach (var testPath in _context.GetFiles(BuildSettings.OutputDirectory + filePattern, globberSettings))
                    result.Add(testPath);
        }

        result.Sort(ComparePathsByFileName);

        return result;

        static int ComparePathsByFileName(FilePath x, FilePath y)
        {
            return x.GetFilename().ToString().CompareTo(y.GetFilename().ToString());
        }
    }
}
