/////////////////////////////////////////////////////////////////////////////
// TEST RUNNER INTERFACES
/////////////////////////////////////////////////////////////////////////////

/// <Summary>
/// Common interface for all test runners
/// </Summary>
public interface ITestRunner
{
	string PackageId { get; }
	string Version { get; }
}

/// <Summary>
/// A runner capable of running unit tests
/// </Summary>
public interface IUnitTestRunner : ITestRunner
{
	int RunUnitTest(FilePath testPath);
}

/// <Summary>
/// A runner capable of running package tests
/// </Summary>
public interface IPackageTestRunner : ITestRunner
{
	int RunPackageTest(string arguments);
}

/////////////////////////////////////////////////////////////////////////////
// ABSTRACT TEST RUNNER
/////////////////////////////////////////////////////////////////////////////

/// <summary>
/// The TestRunner class is the abstract base for all TestRunners used to run unit-
/// or package-tests. A TestRunner knows how to run a test assembly and provide a result.
/// All base functionality is implemented in this class. Derived classes make that
/// functionality available selectively by implementing specific interfaces.
/// </summary>
public abstract class TestRunner : ITestRunner
{
	protected ICakeContext Context => BuildSettings.Context;

	public string PackageId { get; protected set; }
	public string Version { get; protected set; }

	protected int RunTest(FilePath executablePath, string arguments = null)
	{
		return RunTest(executablePath, new ProcessSettings { Arguments = arguments });
	}

	protected int RunTest(FilePath executablePath, ProcessSettings processSettings=null)
	{
		if (executablePath == null)
			throw new ArgumentNullException(nameof(executablePath));

		if (processSettings == null)
			processSettings = new ProcessSettings();

		// Add default values to settings if not present
		if (processSettings.WorkingDirectory == null)
			processSettings.WorkingDirectory = BuildSettings.OutputDirectory;

		if (executablePath.GetExtension() == ".dll")
			return Context.StartProcess("dotnet", processSettings);
        else
			return Context.StartProcess(executablePath, processSettings);
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

	protected abstract FilePath ExecutableRelativePath { get; }

	// Path under tools directory where package would be installed by Cake #tool directive.
	// NOTE: When used to run unit tests, a #tool directive is required. If derived package
	// is only used for package tests, it is optional.
	protected DirectoryPath ToolInstallDirectory => BuildSettings.ToolsDirectory + $"{PackageId}.{Version}"; 
	protected bool IsInstalledAsTool =>
		ToolInstallDirectory != null && Context.DirectoryExists(ToolInstallDirectory);
	
	protected DirectoryPath InstallDirectory;

	public FilePath ExecutablePath => InstallDirectory.CombineWithFilePath(ExecutableRelativePath);

	public void Install(DirectoryPath installDirectory)
	{
		InstallDirectory = installDirectory.Combine($"{PackageId}.{Version}");

		// If the runner package is already installed as a cake tool, we just copy it
		if (IsInstalledAsTool)
			Context.CopyDirectory(ToolInstallDirectory, InstallDirectory);
		// Otherwise, we install it to the requested location
		else
			Context.NuGetInstall(
				PackageId,
				new NuGetInstallSettings() { OutputDirectory = installDirectory, Version = Version });
	}
}

/////////////////////////////////////////////////////////////////////////////
// TEST RUNNER SOURCE
/////////////////////////////////////////////////////////////////////////////

/// <Summary>
/// TestRunnerSource is a provider of TestRunners. It is used when the tests
/// are to be run under multiple TestRunners rather than just one.
/// </Summary>
public class TestRunnerSource
{
	public TestRunnerSource(TestRunner runner1, params TestRunner[] moreRunners)
	{
		AllRunners.Add(runner1);
		AllRunners.AddRange(moreRunners);
	}

	public List<TestRunner> AllRunners { get; } = new List<TestRunner>();

	public IEnumerable<IUnitTestRunner> UnitTestRunners
	{
		get { foreach(var runner in AllRunners.Where(r => r is IUnitTestRunner)) yield return (IUnitTestRunner)runner; }
	}

	public IEnumerable<IPackageTestRunner> PackageTestRunners
	{
		get { foreach(var runner in AllRunners.Where(r => r is IPackageTestRunner)) yield return (IPackageTestRunner)runner; }
	}
}

/////////////////////////////////////////////////////////////////////////////
// NUNITLITE RUNNER
/////////////////////////////////////////////////////////////////////////////

// For NUnitLite tests, the test is run directly
public class NUnitLiteRunner : TestRunner, IUnitTestRunner
{
    public int RunUnitTest(FilePath testPath) =>
        RunTest(testPath, BuildSettings.UnitTestArguments);
}

/////////////////////////////////////////////////////////////////////////////
// NUNIT CONSOLE RUNNERS
/////////////////////////////////////////////////////////////////////////////

// NUnitConsoleRunner is used for both unit and package tests. It must be pre-installed
// in the tools directory by use of a #tools directive.
public class NUnitConsoleRunner : InstallableTestRunner, IUnitTestRunner, IPackageTestRunner
{
	protected override FilePath ExecutableRelativePath => "tools/nunit3-console.exe";
	
	public NUnitConsoleRunner(string version) : base("NUnit.ConsoleRunner", version) { }

	// Run a unit test
	public int RunUnitTest(FilePath testPath) => RunTest(ToolInstallDirectory.CombineWithFilePath(ExecutableRelativePath), $"\"{testPath}\" {BuildSettings.UnitTestArguments}");

	// Run a package test
	public int RunPackageTest(string arguments) => RunTest(ExecutablePath, arguments);
}

public class NUnitNetCoreConsoleRunner : InstallableTestRunner, IUnitTestRunner, IPackageTestRunner
{
	protected override FilePath ExecutableRelativePath => "tools/net6.0/nunit3-console.exe";

	public NUnitNetCoreConsoleRunner(string version) : base("NUnit.ConsoleRunner.NetCore", version) { }

	// Run a unit test
	public int RunUnitTest(FilePath testPath) => RunTest(ExecutablePath, $"\"{testPath}\" {BuildSettings.UnitTestArguments}");

	// Run a package test
	public int RunPackageTest(string arguments) => RunTest(ExecutablePath, arguments);
}

public class EngineExtensionTestRunner : TestRunner, IPackageTestRunner
{
	private IPackageTestRunner[] _runners = new IPackageTestRunner[] {
		new NUnitConsoleRunner("3.17.0"),
		new NUnitConsoleRunner("3.15.5")
	};

	public int RunPackageTest(string arguments)
	{
		
		return _runners[0].RunPackageTest(arguments);
	}
}

/////////////////////////////////////////////////////////////////////////////
// AGENT RUNNER
/////////////////////////////////////////////////////////////////////////////

/// <summary>
/// Class that knows how to run an agent directly. (For future use)
/// </summary>
public class AgentRunner : TestRunner, IPackageTestRunner
{
    private string _stdExecutable;
    private string _x86Executable;

    private FilePath _executablePath;

	public AgentRunner(string stdExecutable, string x86Executable = null)
	{
        _stdExecutable = stdExecutable;
        _x86Executable = x86Executable;
    }

    public int RunPackageTest(string arguments)
    {
		_executablePath = arguments.Contains("--x86")
            ? _x86Executable
            : _stdExecutable;

        return base.RunTest(_executablePath, arguments.Replace("--x86", string.Empty));
	}
}
