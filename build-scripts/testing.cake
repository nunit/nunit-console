//////////////////////////////////////////////////////////////////////
// TESTING HELPER METHODS
//////////////////////////////////////////////////////////////////////

static void CheckTestErrors(ref List<string> errorDetail)
{
    if(errorDetail.Count != 0)
    {
        var copyError = new List<string>();
        copyError = errorDetail.Select(s => s).ToList();
        errorDetail.Clear();
        throw new Exception("One or more tests failed, breaking the build.\n"
                              + copyError.Aggregate((x,y) => x + "\n" + y));
    }
}

private void RunNUnitLite(string testName, string framework, string directory)
{
	bool isDotNetCore = framework.StartsWith("netcoreapp");
	string ext = isDotNetCore ? ".dll" : ".exe";
	string testPath = directory + testName + ext;

	Information("==================================================");
	Information("Running tests under " + framework);
	Information("==================================================");

	int rc = isDotNetCore
		? StartProcess("dotnet", testPath)
		: StartProcess(testPath);

	if (rc > 0)
		ErrorDetail.Add($"{testName}: {rc} tests failed running under {framework}");
	else if (rc < 0)
		ErrorDetail.Add($"{testName} returned rc = {rc} running under {framework}");
}

// Class that knows how to run tests against either GUI
public class GuiTester
{
	private BuildParameters _parameters;

	public GuiTester(BuildParameters parameters)
	{
		_parameters = parameters;
	}

	public void RunGuiUnattended(string runnerPath, string arguments)
	{
		if (!arguments.Contains(" --run"))
			arguments += " --run";
		if (!arguments.Contains(" --unattended"))
			arguments += " --unattended";

		RunGui(runnerPath, arguments);
	}

	public void RunGui(string runnerPath, string arguments)
	{
		_parameters.Context.StartProcess(runnerPath, new ProcessSettings()
		{
			Arguments = arguments,
			WorkingDirectory = _parameters.OutputDirectory
		});
	}
}

// Representation of a single test to be run against a pre-built package.
// Each test has a Level, with the following values defined...
//  0 Do not run - used for temporarily disabling a test
//  1 Run for all CI tests - that is every time we test packages
//  2 Run only on PRs, dev builds and when publishing
//  3 Run only when publishing
public struct PackageTest
{
	public int Level;
	public string Description;
	public string Runner;
	public string Arguments;
	public ExpectedResult ExpectedResult;
	public string[] ExtensionsNeeded;
	
	public PackageTest(int level, string description, string runner, string arguments, ExpectedResult expectedResult, params string[] extensionsNeeded)
	{
		Level = level;
		Description = description;
		Runner = runner;
		Arguments = arguments;
		ExpectedResult = expectedResult;
		ExtensionsNeeded = extensionsNeeded;
	}
}

// Abstract base for all package testers. Currently, we only
// have one package of each type (Zip, NuGet, Chocolatey).
public abstract class PackageTester : GuiTester
{
	protected static readonly string[] ENGINE_FILES = {
		"testcentric.engine.dll", "testcentric.engine.core.dll", "nunit.engine.api.dll", "testcentric.engine.metadata.dll"};
	protected static readonly string[] ENGINE_CORE_FILES = {
		"testcentric.engine.core.dll", "nunit.engine.api.dll", "testcentric.engine.metadata.dll" };
	protected static readonly string[] NET_FRAMEWORK_AGENT_FILES = {
		"testcentric-agent.exe", "testcentric-agent.exe.config", "testcentric-agent-x86.exe", "testcentric-agent-x86.exe.config" };
	protected static readonly string[] NET_CORE_AGENT_FILES = {
		"testcentric-agent.dll", "testcentric-agent.dll.config", "testcentric-agent-x86.dll", "testcentric-agent-x86.dll.config" };
	protected static readonly string[] GUI_FILES = {
        "testcentric.exe", "testcentric.exe.config", "tc-next.exe", "tc-next.exe.config", "nunit.uiexception.dll",
        "TestCentric.Gui.Runner.dll", "Experimental.Gui.Runner.dll", "TestCentric.Gui.Model.dll", "TestCentric.Common.dll" };
    protected static readonly string[] TREE_ICONS_JPG = {
        "Success.jpg", "Failure.jpg", "Ignored.jpg", "Inconclusive.jpg", "Skipped.jpg" };
    protected static readonly string[] TREE_ICONS_PNG = {
        "Success.png", "Failure.png", "Ignored.png", "Inconclusive.png", "Skipped.png" };

	protected BuildParameters _parameters;
	private ICakeContext _context;

	public PackageTester(BuildParameters parameters)
		: base(parameters) 
	{
		_parameters = parameters;
		_context = parameters.Context;

		PackageTests = new List<PackageTest>();

		// Level 1 tests are run each time we build the packages
		PackageTests.Add(new PackageTest(2, "Re-run tests of the TestCentric model", StandardRunner,
			"TestCentric.Gui.Model.Tests.dll",
			new ExpectedResult("Passed")));
		
		PackageTests.Add(new PackageTest(1, "Run mock-assembly.dll under .NET 4.5", StandardRunner,
			"mock-assembly.dll",
			new ExpectedResult("Failed")
			{
				Total = 31,
				Passed = 18,
				Failed = 5,
				Warnings = 0,
				Inconclusive = 1,
				Skipped = 7
			}));
		
		PackageTests.Add(new PackageTest(1, "Run mock-assembly.dll under .NET 3.5", StandardRunner,
			"engine-tests/net35/mock-assembly.dll",
			new ExpectedResult("Failed")
			{
				Total = 36,
				Passed = 23,
				Failed = 5,
				Warnings = 0,
				Inconclusive = 1,
				Skipped = 7
			}));
		
		PackageTests.Add(new PackageTest(1, "Run mock-assembly.dll under .NET Core 2.1", StandardRunner,
			"engine-tests/netcoreapp2.1/mock-assembly.dll",
			new ExpectedResult("Failed")
			{
				Total = 36,
				Passed = 23,
				Failed = 5,
				Warnings = 0,
				Inconclusive = 1,
				Skipped = 7
			}));

		PackageTests.Add(new PackageTest(1, "Run mock-assembly.dll under .NET Core 3.1", StandardRunner,
			"engine-tests/netcoreapp3.1/mock-assembly.dll",
			new ExpectedResult("Failed")
			{
				Total = 36,
				Passed = 23,
				Failed = 5,
				Warnings = 0,
				Inconclusive = 1,
				Skipped = 7
			}));

		PackageTests.Add(new PackageTest(1, "Run mock-assembly.dll targeting .NET Core 1.1", StandardRunner,
			"engine-tests/netcoreapp1.1/mock-assembly.dll",
			new ExpectedResult("Failed")
			{
				Total = 36,
				Passed = 23,
				Failed = 5,
				Warnings = 0,
				Inconclusive = 1,
				Skipped = 7
			}));

		PackageTests.Add(new PackageTest(1, "Run mock-assembly.dll under .NET 5.0", StandardRunner,
			"engine-tests/net5.0/mock-assembly.dll",
			new ExpectedResult("Failed")
			{
				Total = 32,
				Passed = 19,
				Failed = 5,
				Warnings = 0,
				Inconclusive = 1,
				Skipped = 7
			}));

		// Level 2 tests are run for PRs and when packages will be published

		// TODO: Ensure that experimental runner saves results and handles --unattended
		// PackageTests.Add(new PackageTest(2, "Run tests of the TestCentric model using the Experimental Runner", ExperimentalRunner,
		//     "TestCentric.Gui.Model.Tests.dll",
		//     new ExpectedResult("Passed"));

		PackageTests.Add(new PackageTest(2, "Run mock-assembly.dll built for NUnit V2", StandardRunner,
			"v2-tests/mock-assembly.dll",
			new ExpectedResult("Failed")
			{
				Total = 28,
				Passed = 18,
				Failed = 5,
				Warnings = 0,
				Inconclusive = 1,
				Skipped = 4
			},
			NUnitV2Driver));

		PackageTests.Add( new PackageTest(2, "Run different builds of mock-assembly.dll together", StandardRunner,
			"engine-tests/net35/mock-assembly.dll engine-tests/netcoreapp2.1/mock-assembly.dll",
			new ExpectedResult("Failed")
			{
				Total = 72,
				Passed = 46,
				Failed = 10,
				Warnings = 0,
				Inconclusive = 2,
				Skipped = 14
			}));

		// TODO: Make test work on AppVeyor - currently runs locally only
		if (_parameters.IsLocalBuild)
			PackageTests.Add( new PackageTest(2, "Run an NUnit project, specifying Release config", StandardRunner,
				"../../GuiTests.nunit --config=Release --trace=Debug",
				new ExpectedResult("Passed"),
				NUnitProjectLoader));
	}

	protected abstract string PackageName { get; }
	protected abstract FilePath PackageUnderTest { get; }
	protected abstract string PackageTestDirectory { get; }
	protected abstract string PackageTestBinDirectory { get; }
	protected abstract string ExtensionInstallDirectory { get; }

	protected virtual string NUnitV2Driver => "NUnit.Extension.NUnitV2Driver";
	protected virtual string NUnitProjectLoader => "NUnit.Extension.NUnitProjectLoader";

	// PackageChecks differ for each package type.
	protected abstract PackageCheck[] PackageChecks { get; }

	// NOTE: Currently, we use the same tests for all packages. There seems to be
	// no reason for the three packages to differ in capability so the only reason
	// to limit tests on some of them would be efficiency... so far not a problem.
	private List<PackageTest> PackageTests { get; }

	protected string StandardRunner => PackageTestBinDirectory + GUI_RUNNER;
	protected string ExperimentalRunner => PackageTestBinDirectory + EXPERIMENTAL_RUNNER;

	public void RunAllTests()
	{
		Console.WriteLine("Testing package " + PackageName);

		CreateTestDirectory();

		RunChecks();

		RunPackageTests(_parameters.PackageTestLevel);

		CheckTestErrors(ref ErrorDetail);
	}

	private void CreateTestDirectory()
	{
		Console.WriteLine("Unzipping package to directory\n  " + PackageTestDirectory);
		_context.CleanDirectory(PackageTestDirectory);
		_context.Unzip(PackageUnderTest, PackageTestDirectory);
	}

    private void RunChecks()
    {
		DisplayBanner("Checking Package Content");

        bool allPassed = true;

        if (PackageChecks.Length == 0)
        {
            Console.WriteLine("  Package found but no checks were specified.");
        }
        else
        {
            foreach (var check in PackageChecks)
                allPassed &= check.Apply(PackageTestDirectory);

            if (allPassed)
                Console.WriteLine("  All checks passed!");
        }

        if (!allPassed)
     		throw new Exception($"Package check failed for {PackageName}");
    }

	private void CheckExtensionIsInstalled(string extension)
	{
		bool alreadyInstalled = _context.GetDirectories($"{ExtensionInstallDirectory}{extension}.*").Count > 0;

		if (!alreadyInstalled)
		{
			DisplayBanner($"Installing {extension}");
			InstallEngineExtension(extension);
		}
	}

	protected abstract void InstallEngineExtension(string extension);

	private void RunPackageTests(int testLevel)
	{
		bool anyErrors = false;
		int testCount = 0;

		foreach (var packageTest in PackageTests)
		{
			if (packageTest.Level > 0 && packageTest.Level <= testLevel)
			{
				++testCount;

				foreach (string extension in packageTest.ExtensionsNeeded)
					CheckExtensionIsInstalled(extension);

				var resultFile = _parameters.OutputDirectory + DEFAULT_TEST_RESULT_FILE;
				// Delete result file ahead of time so we don't mistakenly
				// read a left-over file from another test run. Leave the
				// file after the run in case we need it to debug a failure.
				if (_context.FileExists(resultFile))
					_context.DeleteFile(resultFile);
				
				DisplayBanner(packageTest.Description);
				DisplayTestEnvironment(packageTest);

				RunGuiUnattended(packageTest.Runner, packageTest.Arguments);

				var reporter = new ResultReporter(resultFile);
				anyErrors |= reporter.Report(packageTest.ExpectedResult) > 0;
			}
		}

		Console.WriteLine($"\nRan {testCount} package tests on {PackageName}");

		// All package tests are run even if one of them fails. If there are
		// any errors,  we stop the run at this point.
		if (anyErrors)
			throw new Exception("One or more package tests had errors!");
	}

	private void DisplayBanner(string message)
	{
		Console.WriteLine("\n========================================");;
		Console.WriteLine(message);
		Console.WriteLine("========================================");
	}

	private void DisplayTestEnvironment(PackageTest test)
	{
		Console.WriteLine("Test Environment");
		Console.WriteLine($"   OS Version: {Environment.OSVersion.VersionString}");
		Console.WriteLine($"  CLR Version: {Environment.Version}");
		Console.WriteLine($"       Runner: {test.Runner}");
		Console.WriteLine($"    Arguments: {test.Arguments}");
		Console.WriteLine();
	}

    protected FileCheck HasFile(string file) => HasFiles(new[] { file });
    protected FileCheck HasFiles(params string[] files) => new FileCheck(files);

    protected DirectoryCheck HasDirectory(string dir) => new DirectoryCheck(dir);
}

public class ZipPackageTester : PackageTester
{
	public ZipPackageTester(BuildParameters parameters) : base(parameters) { }

	protected override string PackageName => _parameters.ZipPackageName;
	protected override FilePath PackageUnderTest => _parameters.ZipPackage;
	protected override string PackageTestDirectory => _parameters.ZipTestDirectory;
	protected override string PackageTestBinDirectory => PackageTestDirectory + "bin/";
	protected override string ExtensionInstallDirectory => PackageTestBinDirectory + "addins/";
	
	protected override PackageCheck[] PackageChecks => new PackageCheck[]
	{
		HasFiles("CHANGES.txt", "LICENSE.txt", "NOTICES.txt"),
		HasDirectory("bin").WithFiles(GUI_FILES).AndFiles(ENGINE_FILES).AndFile("testcentric.zip.addins"),
		HasDirectory("bin/agents/net20").WithFiles(NET_FRAMEWORK_AGENT_FILES).AndFile("testcentric-agent.zip.addins"),
		HasDirectory("bin/agents/net40").WithFiles(NET_FRAMEWORK_AGENT_FILES).AndFile("testcentric-agent.zip.addins"),
		HasDirectory("bin/agents/netcoreapp2.1").WithFiles(NET_CORE_AGENT_FILES).AndFile("testcentric-agent.zip.addins"),
		HasDirectory("bin/agents/netcoreapp3.1").WithFiles(NET_CORE_AGENT_FILES).AndFile("testcentric-agent.zip.addins"),
		HasDirectory("bin/agents/net5.0").WithFiles(NET_CORE_AGENT_FILES).AndFile("testcentric-agent.zip.addins"),
		HasDirectory("bin/Images").WithFiles("DebugTests.png", "RunTests.png"),
		HasDirectory("bin/Images/Tree/Circles").WithFiles(TREE_ICONS_JPG),
		HasDirectory("bin/Images/Tree/Classic").WithFiles(TREE_ICONS_JPG),
		HasDirectory("bin/Images/Tree/Default").WithFiles(TREE_ICONS_PNG),
		HasDirectory("bin/Images/Tree/Visual Studio").WithFiles(TREE_ICONS_PNG)
	};

	protected override void InstallEngineExtension(string extension)
	{
		_parameters.Context.NuGetInstall(extension,
			new NuGetInstallSettings() { OutputDirectory = ExtensionInstallDirectory});
	}
}

public class NuGetPackageTester : PackageTester
{
	public NuGetPackageTester(BuildParameters parameters) : base(parameters) { }

	protected override string PackageName => _parameters.NuGetPackageName;
	protected override FilePath PackageUnderTest => _parameters.NuGetPackage;
	protected override string PackageTestDirectory => _parameters.NuGetTestDirectory;
	protected override string PackageTestBinDirectory => PackageTestDirectory + "tools/";
	protected override string ExtensionInstallDirectory => _parameters.TestDirectory;
	
	protected override PackageCheck[] PackageChecks => new PackageCheck[]
	{
		HasFiles("CHANGES.txt", "LICENSE.txt", "NOTICES.txt", "testcentric.png"),
		HasDirectory("tools").WithFiles(GUI_FILES).AndFiles(ENGINE_FILES).AndFile("testcentric.nuget.addins"),
		HasDirectory("tools/agents/net20").WithFiles(NET_FRAMEWORK_AGENT_FILES).AndFiles(ENGINE_CORE_FILES).AndFile("testcentric-agent.nuget.addins"),
		HasDirectory("tools/agents/net40").WithFiles(NET_FRAMEWORK_AGENT_FILES).AndFiles(ENGINE_CORE_FILES).AndFile("testcentric-agent.nuget.addins"),
		HasDirectory("tools/agents/netcoreapp2.1").WithFiles(NET_CORE_AGENT_FILES).AndFiles(ENGINE_CORE_FILES).AndFile("testcentric-agent.nuget.addins"),
		HasDirectory("tools/agents/netcoreapp3.1").WithFiles(NET_CORE_AGENT_FILES).AndFiles(ENGINE_CORE_FILES).AndFile("testcentric-agent.nuget.addins"),
		HasDirectory("tools/agents/net5.0").WithFiles(NET_CORE_AGENT_FILES).AndFiles(ENGINE_CORE_FILES).AndFile("testcentric-agent.nuget.addins"),
		HasDirectory("tools/Images").WithFiles("DebugTests.png", "RunTests.png"),
		HasDirectory("tools/Images/Tree/Circles").WithFiles(TREE_ICONS_JPG),
		HasDirectory("tools/Images/Tree/Classic").WithFiles(TREE_ICONS_JPG),
		HasDirectory("tools/Images/Tree/Default").WithFiles(TREE_ICONS_PNG),
		HasDirectory("tools/Images/Tree/Visual Studio").WithFiles(TREE_ICONS_PNG)
	};

	protected override void InstallEngineExtension(string extension)
	{
		_parameters.Context.NuGetInstall(extension,
			new NuGetInstallSettings() { OutputDirectory = ExtensionInstallDirectory});
	}
}

public class ChocolateyPackageTester : PackageTester
{
	public ChocolateyPackageTester(BuildParameters parameters) : base(parameters) { }

	protected override string PackageName => _parameters.ChocolateyPackageName;
	protected override FilePath PackageUnderTest => _parameters.ChocolateyPackage;
	protected override string PackageTestDirectory => _parameters.ChocolateyTestDirectory;
	protected override string PackageTestBinDirectory => PackageTestDirectory + "tools/";
	protected override string ExtensionInstallDirectory => _parameters.TestDirectory;
	
	// Chocolatey packages have a different naming convention from NuGet
	protected override string NUnitV2Driver => "nunit-extension-nunit-v2-driver";
	protected override string NUnitProjectLoader => "nunit-extension-nunit-project-loader";

	protected override PackageCheck[] PackageChecks => new PackageCheck[]
	{
		HasDirectory("tools").WithFiles("CHANGES.txt", "LICENSE.txt", "NOTICES.txt", "VERIFICATION.txt", "testcentric.choco.addins").AndFiles(GUI_FILES).AndFiles(ENGINE_FILES).AndFile("testcentric.choco.addins"),
		HasDirectory("tools/agents/net20").WithFiles(NET_FRAMEWORK_AGENT_FILES).AndFile("testcentric-agent.choco.addins"),
		HasDirectory("tools/agents/net40").WithFiles(NET_FRAMEWORK_AGENT_FILES).AndFile("testcentric-agent.choco.addins"),
		HasDirectory("tools/agents/netcoreapp2.1").WithFiles(NET_CORE_AGENT_FILES).AndFile("testcentric-agent.choco.addins"),
		HasDirectory("tools/agents/netcoreapp3.1").WithFiles(NET_CORE_AGENT_FILES).AndFile("testcentric-agent.choco.addins"),
		HasDirectory("tools/agents/net5.0").WithFiles(NET_CORE_AGENT_FILES).AndFile("testcentric-agent.choco.addins"),
		HasDirectory("tools/Images").WithFiles("DebugTests.png", "RunTests.png"),
		HasDirectory("tools/Images/Tree/Circles").WithFiles(TREE_ICONS_JPG),
		HasDirectory("tools/Images/Tree/Classic").WithFiles(TREE_ICONS_JPG),
		HasDirectory("tools/Images/Tree/Default").WithFiles(TREE_ICONS_PNG),
		HasDirectory("tools/Images/Tree/Visual%20Studio").WithFiles(TREE_ICONS_PNG)
	};

	protected override void InstallEngineExtension(string extension)
	{
		// Install with NuGet because choco requires administrator access
		_parameters.Context.NuGetInstall(extension,
			new NuGetInstallSettings()
			{
				Source = new [] { "https://chocolatey.org/api/v2/" },
				OutputDirectory = ExtensionInstallDirectory
			});
	}
}

public abstract class PackageCheck
{
    public abstract bool Apply(string dir);

    protected static void RecordError(string msg)
    {
        Console.WriteLine("  ERROR: " + msg);
    }
}

public class FileCheck : PackageCheck
{
    string[] _paths;

    public FileCheck(string[] paths)
    {
        _paths = paths;
    }

    public override bool Apply(string dir)
    {
        var isOK = true;

        foreach (string path in _paths)
        {
            if (!System.IO.File.Exists(dir + path))
            {
                RecordError($"File {path} was not found.");
                isOK = false;
            }
        }

        return isOK;
    }
}

public class DirectoryCheck : PackageCheck
{
    private string _path;
    private List<string> _files = new List<string>();

    public DirectoryCheck(string path)
    {
        _path = path;
    }

    public DirectoryCheck WithFiles(params string[] files)
    {
        _files.AddRange(files);
        return this;
    }

    public DirectoryCheck AndFiles(params string[] files)
    {
        return WithFiles(files);
    }

    public DirectoryCheck WithFile(string file)
    {
        _files.Add(file);
        return this;
    }

	public DirectoryCheck AndFile(string file)
	{
		return AndFiles(file);
	}

    public override bool Apply(string dir)
    {
        if (!System.IO.Directory.Exists(dir + _path))
        {
            RecordError($"Directory {_path} was not found.");
            return false;
        }

        bool isOK = true;

        if (_files != null)
        {
            foreach (var file in _files)
            {
                if (!System.IO.File.Exists(System.IO.Path.Combine(dir + _path, file)))
                {
                    RecordError($"File {file} was not found in directory {_path}.");
                    isOK = false;
                }
            }
        }

        return isOK;
    }
}
