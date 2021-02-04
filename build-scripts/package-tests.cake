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
	public string Arguments;
	public ExpectedResult ExpectedResult;
	public string[] ExtensionsNeeded;
	
	public PackageTest(int level, string description, string arguments, ExpectedResult expectedResult, params string[] extensionsNeeded)
	{
		Level = level;
		Description = description;
		Arguments = arguments;
		ExpectedResult = expectedResult;
		ExtensionsNeeded = extensionsNeeded;
	}
}

// Abstract base for all package testers. Currently, we only
// have one package of each type (Zip, NuGet, Chocolatey).
public abstract class PackageTester
{
    protected BuildParameters _parameters;
    protected ICakeContext _context;

    public PackageTester(BuildParameters parameters)
    {
        _parameters = parameters;
        _context = parameters.Context;

        PackageTests = new List<PackageTest>();

        // Level 1 tests are run each time we build the packages
        PackageTests.Add(new PackageTest(1, "Run mock-assembly.dll under .NET 3.5",
            "net35/mock-assembly.dll",
            new ExpectedResult("Failed")
            {
                Total = 37,
                Passed = 23,
                Failed = 5,
                Warnings = 0,
                Inconclusive = 1,
                Skipped = 7
            }));

        // We don't have a net40 test-assembly... should we?
        //PackageTests.Add(new PackageTest(1, "Run mock-assembly.dll under .NET 4.x",
        //    "net40/mock-assembly.dll",
        //    new ExpectedResult("Failed")
        //    {
        //        Total = 37,
        //        Passed = 23,
        //        Failed = 5,
        //        Warnings = 0,
        //        Inconclusive = 1,
        //        Skipped = 7
        //    }));

        //PackageTests.Add(new PackageTest(1, "Run mock-assembly.dll under .NET Core 2.1",
        //    "engine-tests/netcoreapp2.1/mock-assembly.dll",
        //    new ExpectedResult("Failed")
        //    {
        //        Total = 36,
        //        Passed = 23,
        //        Failed = 5,
        //        Warnings = 0,
        //        Inconclusive = 1,
        //        Skipped = 7
        //    }));

        //PackageTests.Add(new PackageTest(1, "Run mock-assembly.dll under .NET Core 3.1",
        //    "engine-tests/netcoreapp3.1/mock-assembly.dll",
        //    new ExpectedResult("Failed")
        //    {
        //        Total = 36,
        //        Passed = 23,
        //        Failed = 5,
        //        Warnings = 0,
        //        Inconclusive = 1,
        //        Skipped = 7
        //    }));

        //PackageTests.Add(new PackageTest(1, "Run mock-assembly.dll targeting .NET Core 1.1",
        //    "engine-tests/netcoreapp1.1/mock-assembly.dll",
        //    new ExpectedResult("Failed")
        //    {
        //        Total = 36,
        //        Passed = 23,
        //        Failed = 5,
        //        Warnings = 0,
        //        Inconclusive = 1,
        //        Skipped = 7
        //    }));

        //PackageTests.Add(new PackageTest(1, "Run mock-assembly.dll under .NET 5.0",
        //    "engine-tests/net5.0/mock-assembly.dll",
        //    new ExpectedResult("Failed")
        //    {
        //        Total = 32,
        //        Passed = 19,
        //        Failed = 5,
        //        Warnings = 0,
        //        Inconclusive = 1,
        //        Skipped = 7
        //    }));

        //// Level 2 tests are run for PRs and when packages will be published

        //PackageTests.Add(new PackageTest(2, "Run mock-assembly.dll built for NUnit V2",
        //    "v2-tests/mock-assembly.dll",
        //    new ExpectedResult("Failed")
        //    {
        //        Total = 28,
        //        Passed = 18,
        //        Failed = 5,
        //        Warnings = 0,
        //        Inconclusive = 1,
        //        Skipped = 4
        //    },
        //    NUnitV2Driver));

        //PackageTests.Add(new PackageTest(2, "Run different builds of mock-assembly.dll together",
        //    "engine-tests/net35/mock-assembly.dll engine-tests/netcoreapp2.1/mock-assembly.dll",
        //    new ExpectedResult("Failed")
        //    {
        //        Total = 72,
        //        Passed = 46,
        //        Failed = 10,
        //        Warnings = 0,
        //        Inconclusive = 2,
        //        Skipped = 14
        //    }));
    }

    protected abstract string PackageName { get; }
    protected abstract FilePath PackageUnderTest { get; }
    protected abstract string PackageTestDirectory { get; }
    protected abstract string PackageTestBinDirectory { get; }
    protected abstract string ExtensionInstallDirectory { get; }

    protected virtual string NUnitV2Driver => "NUnit.Extension.NUnitV2Driver";
    protected virtual string NUnitProjectLoader => "NUnit.Extension.NUnitProjectLoader";

    // NOTE: Currently, we use the same tests for all packages. There seems to be
    // no reason for the three packages to differ in capability so the only reason
    // to limit tests on some of them would be efficiency... so far not a problem.
    private List<PackageTest> PackageTests { get; }

    // Test Runners (from old build.cake)
    //var NET20_CONSOLE = BIN_DIR + "net20/" + "nunit3-console.exe";
    //var NETCORE31_CONSOLE = BIN_DIR + "netcoreapp3.1/" + "nunit3-console.dll";

    protected string Net20Runner => "nunit3-console.exe";
    protected string NetCore31Runner => "nunit3-console.dll";

    public void RunAllTests(int level)
    {
        Console.WriteLine("Testing package " + PackageName);

        CreateTestDirectory();

        RunPackageTests(level);

        //CheckTestErrors(ref ErrorDetail);
    }

    protected virtual void CreateTestDirectory()
    {
        Console.WriteLine("Unzipping package to directory\n  " + PackageTestDirectory);
        _context.CleanDirectory(PackageTestDirectory);
        _context.Unzip(PackageUnderTest, PackageTestDirectory);
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

                int rc = _parameters.Context.StartProcess(
                    PackageTestBinDirectory + Net20Runner,
                    new ProcessSettings()
                    {
                        Arguments = packageTest.Arguments,
                        WorkingDirectory = _parameters.OutputDirectory
                    }); ;

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
        Console.WriteLine("\n========================================"); ;
        Console.WriteLine(message);
        Console.WriteLine("========================================");
    }

    private void DisplayTestEnvironment(PackageTest test)
    {
        Console.WriteLine("Test Environment");
        Console.WriteLine($"   OS Version: {Environment.OSVersion.VersionString}");
        Console.WriteLine($"  CLR Version: {Environment.Version}");
        Console.WriteLine($"    Arguments: {test.Arguments}");
        Console.WriteLine();
    }
}

public class ZipPackageTester : PackageTester
{
    public ZipPackageTester(BuildParameters parameters) : base(parameters) { }

    protected override string PackageName => _parameters.ZipPackageName;
    protected override FilePath PackageUnderTest => _parameters.ZipPackage;
    protected override string PackageTestDirectory => _parameters.ZipTestDirectory;
    protected override string PackageTestBinDirectory => PackageTestDirectory + "bin/net20/";
    protected override string ExtensionInstallDirectory => PackageTestBinDirectory + "addins/";

    protected override void InstallEngineExtension(string extension)
    {
        _context.NuGetInstall(extension,
            new NuGetInstallSettings() { OutputDirectory = ExtensionInstallDirectory });
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

    protected override void InstallEngineExtension(string extension)
    {
        _context.NuGetInstall(extension,
            new NuGetInstallSettings() { OutputDirectory = ExtensionInstallDirectory });
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

    protected override void InstallEngineExtension(string extension)
    {
        // Install with NuGet because choco requires administrator access
        _context.NuGetInstall(extension,
            new NuGetInstallSettings()
            {
                Source = new[] { "https://chocolatey.org/api/v2/" },
                OutputDirectory = ExtensionInstallDirectory
            });
    }
}

public class MsiPackageTester : PackageTester
{
    public MsiPackageTester(BuildParameters parameters) : base(parameters) { }

    protected override string PackageName => _parameters.MsiPackageName;
    protected override FilePath PackageUnderTest => _parameters.MsiPackage;
    protected override string PackageTestDirectory => _parameters.MsiTestDirectory;
    protected override string PackageTestBinDirectory => PackageTestDirectory + "NUnit.org/nunit-console/";
    protected override string ExtensionInstallDirectory => _parameters.TestDirectory;

    protected override void InstallEngineExtension(string extension)
    {
        _context.NuGetInstall(extension,
            new NuGetInstallSettings() { OutputDirectory = ExtensionInstallDirectory });
    }

    protected override void CreateTestDirectory()
    {
        Console.WriteLine("Installing msi to directory\n  " + PackageTestDirectory);
        _context.CleanDirectory(PackageTestDirectory);

        // Msiexec does not tolerate forward slashes!
        string package = PackageUnderTest.ToString().Replace("/", "\\");
        string testDir = PackageTestDirectory.Replace("/", "\\");
        int rc = _context.StartProcess("msiexec", $"/a {package} TARGETDIR={testDir} /q");
        if (rc != 0)
            WriteError($"Installer returned {rc.ToString()}");
        else
        {
            // Administrative install doesn't copy these files
            _context.CopyFiles(
                PackageTestBinDirectory + "*.dll",
                PackageTestBinDirectory + "agents/net20/");
            _context.CopyFiles(
                PackageTestBinDirectory + "*.dll",
                PackageTestBinDirectory + "agents/net40/");
        }
    }
}
