// Representation of a single test to be run against a pre-built package.
public struct PackageTest
{
    public string Name;
    public string Description;
    public string Arguments;
    public ExpectedResult ExpectedResult;

    public PackageTest(string name, string description, string arguments, ExpectedResult expectedResult)
    {
        Name = name;
        Description = description;
        Arguments = arguments;
        ExpectedResult = expectedResult;

        // Add Common tests here - currently there are none
        // Derived classes may add more tests
    }
}

// Abstract base for all package testers. 
public abstract class PackageTester
{
    protected ICakeContext _context;
    protected string _packageVersion;
    protected string _packageDir;
    protected string _config;
    protected string _outputDir;

    public PackageTester(ICakeContext context, string packageVersion)
    {
        _context = context;
        _packageVersion = packageVersion;
        _packageDir = System.IO.Path.GetFullPath(context.Argument("artifact-dir", "package")) + "/";
        _config = context.Argument("configuration", "Release");
        _outputDir = System.IO.Path.GetFullPath($"bin/{_config}/");

        PackageTests = new List<PackageTest>();
    }

    protected abstract string PackageName { get; }
    protected abstract string PackageInstallDirectory { get; }
    protected abstract string PackageResultDirectory { get; }
    protected abstract string PackageBinDir { get; }

    protected string PackageUnderTest => _packageDir + PackageName;
    protected List<PackageTest> PackageTests { get; }

    public void RunTests()
    {
        Console.WriteLine("Testing package " + PackageName);

        Console.WriteLine($"Creating Test Directory:\n  {PackageInstallDirectory}");
        CreatePackageInstallDirectory();

        RunPackageTests();
    }

    // Default is to just unzip... individual package testers may override
    protected virtual void CreatePackageInstallDirectory()
    {
        _context.CleanDirectory(PackageInstallDirectory);
        _context.Unzip(PackageUnderTest, PackageInstallDirectory);
    }

    private void RunPackageTests()
    {
        var reporter = new ResultReporter(PackageName);

        _context.CleanDirectory(PackageResultDirectory);

        foreach (var packageTest in PackageTests)
        {
            var resultDir = PackageResultDirectory + packageTest.Name + "/";
            var resultFile = resultDir + "TestResult.xml";

            DisplayBanner(packageTest.Description);

            int rc = _context.StartProcess(
                PackageBinDir + "nunit3-console.exe",
                new ProcessSettings()
                {
                    Arguments = $"{packageTest.Arguments} --work={resultDir}",
                    WorkingDirectory = _outputDir
                });

            try
            {
                var result = new ActualResult(resultFile);
                var report = new TestReport(packageTest, result);
                reporter.AddReport(report);

                Console.WriteLine($"Ran tests in {_outputDir}");

                Console.WriteLine(report.Errors.Count == 0
                    ? "\nSUCCESS: Test Result matches expected result!"
                    : "\nERROR: Test Result not as expected!");
            }
            catch (Exception ex)
            {
                reporter.AddReport(new TestReport(packageTest, ex));

                Console.WriteLine("\nERROR: No result found!");
            }
        }

        bool hadErrors = reporter.ReportResults();
        Console.WriteLine();

        if (hadErrors)
            throw new Exception("One or more package tests had errors!");
    }

    private void DisplayBanner(string message)
    {
        Console.WriteLine("\n=================================================="); ;
        Console.WriteLine(message);
        Console.WriteLine("==================================================");
    }
}

// These are tests using the .NET Framework build of the console runner.
// However, they now include running tests under .Net Core.
public abstract class NetFXPackageTester : PackageTester
{
    static readonly string DOTNET_EXE_X86 = @"C:\Program Files (x86)\dotnet\dotnet.exe";

    public NetFXPackageTester(ICakeContext context, string packageVersion)
        : base(context, packageVersion)
    {
        bool dotnetX86Available = _context.IsRunningOnWindows() && System.IO.File.Exists(DOTNET_EXE_X86);

        // Add common tests for running under .NET Framework
        PackageTests.Add(new PackageTest(
            "net35",
            "Run mock-assembly.dll under .NET 3.5",
            "net35/mock-assembly.dll",
            new ExpectedResult("Failed")
            {
                Total = 37,
                Passed = 23,
                Failed = 5,
                Warnings = 1,
                Inconclusive = 1,
                Skipped = 7
            }));

        PackageTests.Add(new PackageTest(
            "net35-x86",
            "Run mock-assembly-x86.dll under .NET 3.5",
            "net35/mock-assembly-x86.dll",
            new ExpectedResult("Failed")
            {
                Total = 37,
                Passed = 23,
                Failed = 5,
                Warnings = 1,
                Inconclusive = 1,
                Skipped = 7
            }));

        PackageTests.Add(new PackageTest(
            "net40",
            "Run mock-assembly.dll under .NET 4.x",
            "net40/mock-assembly.dll",
            new ExpectedResult("Failed")
            {
                Total = 37,
                Passed = 23,
                Failed = 5,
                Warnings = 1,
                Inconclusive = 1,
                Skipped = 7
            }));

        PackageTests.Add(new PackageTest(
            "net40-x86",
            "Run mock-assembly-x86.dll under .NET 4.x",
            "net40/mock-assembly-x86.dll",
            new ExpectedResult("Failed")
            {
                Total = 37,
                Passed = 23,
                Failed = 5,
                Warnings = 1,
                Inconclusive = 1,
                Skipped = 7
            }));

        PackageTests.Add(new PackageTest(
            "net35_plus_net40",
            "Run both copies of mock-assembly together",
            "net35/mock-assembly.dll net40/mock-assembly.dll",
            new ExpectedResult("Failed")
            {
                Total = 2 * 37,
                Passed = 2 * 23,
                Failed = 2 * 5,
                Warnings = 2 * 1,
                Inconclusive = 2 * 1,
                Skipped = 2 * 7
            }));

        PackageTests.Add(new PackageTest(
            "netcoreapp3.1",
            "Run mock-assembly.dll under .NET Core 3.1",
            "netcoreapp3.1/mock-assembly.dll",
            new ExpectedResult("Failed")
            {
                Total = 37,
                Passed = 23,
                Failed = 5,
                Warnings = 1,
                Inconclusive = 1,
                Skipped = 7
            }));

        if (dotnetX86Available)
            PackageTests.Add(new PackageTest(
                "netcoreapp3.1-x86",
                "Run mock-assembly-x86.dll under .NET Core 3.1",
                "netcoreapp3.1/mock-assembly-x86.dll",
                new ExpectedResult("Failed")
                {
                    Total = 37,
                    Passed = 23,
                    Failed = 5,
                    Warnings = 1,
                    Inconclusive = 1,
                    Skipped = 7
                }));
    }
}

public abstract class NetCorePackageTester : PackageTester
{
    public NetCorePackageTester(ICakeContext context, string packageVersion)
        : base(context, packageVersion)
    {
        // Add common tests for running under .NET Core (2.1 or higher)
        PackageTests.Add(new PackageTest(
            "netcoreapp2.1",
            "Run mock-assembly.dll targeting .NET Core 2.1",
            "netcoreapp2.1/mock-assembly.dll",
            new ExpectedResult("Failed")
            {
                Total = 37,
                Passed = 23,
                Failed = 5,
                Warnings = 1,
                Inconclusive = 1,
                Skipped = 7
            }));

        PackageTests.Add(new PackageTest(
            "netcoreapp3.1",
            "Run mock-assembly targeting .NET Core 3.1",
            "netcoreapp3.1/mock-assembly.dll",
            new ExpectedResult("Failed")
            {
                Total = 37,
                Passed = 23,
                Failed = 5,
                Warnings = 1,
                Inconclusive = 1,
                Skipped = 7
            }));

        PackageTests.Add(new PackageTest(
            "netcoreapp2.1_plus_netcoreapp3.1",
            "Run both copies of mock-assembly together",
            "netcoreapp2.1/mock-assembly.dll netcoreapp3.1/mock-assembly.dll",
            new ExpectedResult("Failed")
            {
                Total = 2 * 37,
                Passed = 2 * 23,
                Failed = 2 * 5,
                Warnings = 2 * 1,
                Inconclusive = 2 * 1,
                Skipped = 2 * 7
            }));
    }
}

public class NuGetNetFXPackageTester : NetFXPackageTester
{
    public NuGetNetFXPackageTester(ICakeContext context, string packageVersion)
        : base(context, packageVersion) { }

    protected override string PackageName => $"NUnit.ConsoleRunner.{_packageVersion}.nupkg";
    protected override string PackageInstallDirectory => _packageDir + "test/nuget-netfx/";
    protected override string PackageBinDir => PackageInstallDirectory + "tools/";
    protected override string PackageResultDirectory => _packageDir + "test-results/nuget-netfx/";
}

public class NuGetNetCorePackageTester : NetCorePackageTester
{
    public NuGetNetCorePackageTester(ICakeContext context, string packageVersion)
        : base(context, packageVersion) { }

    protected override string PackageName => $"NUnit.ConsoleRunner.NetCore.{_packageVersion}.nupkg";
    protected override string PackageInstallDirectory => _packageDir + "test/nuget-netcore/";
    protected override string PackageBinDir => PackageInstallDirectory + "tools/netcoreapp3.1/any/";
    protected override string PackageResultDirectory => _packageDir + "test-results/nuget-netcore/";
}

public class ChocolateyPackageTester : NetFXPackageTester
{
    public ChocolateyPackageTester(ICakeContext context, string packageVersion)
        : base(context, packageVersion) { }

    protected override string PackageName => $"nunit-console-runner.{_packageVersion}.nupkg";
    protected override string PackageInstallDirectory => _packageDir + "test/choco/";
    protected override string PackageBinDir => PackageInstallDirectory + "tools/";
    protected override string PackageResultDirectory => _packageDir + "test-results/choco/";
}

public class MsiPackageTester : NetFXPackageTester
{
    public MsiPackageTester(ICakeContext context, string packageVersion)
        : base(context, packageVersion)
    {
        // Add tests specific to the msi package
        PackageTests.Add(new PackageTest(
            "net35_plus_net40_project",
            "Run project with both copies of mock-assembly",
            $"../../NetFXTests.nunit --config={_config}",
            new ExpectedResult("Failed")
            {
                Total = 2 * 37,
                Passed = 2 * 23,
                Failed = 2 * 5,
                Warnings = 2 * 1,
                Inconclusive = 2 * 1,
                Skipped = 2 * 7
            }));
    }

    protected override string PackageName => $"NUnit.Console-{_packageVersion}.msi";
    protected override string PackageInstallDirectory => _packageDir + "test/msi/";
    protected override string PackageBinDir => PackageInstallDirectory + "NUnit.org/nunit-console/";
    protected override string PackageResultDirectory => _packageDir + "test-results/msi/";

    protected override void CreatePackageInstallDirectory()
    {
        // Msiexec does not tolerate forward slashes!
        string package = PackageUnderTest.ToString().Replace("/", "\\");
        string testDir = PackageInstallDirectory.Replace("/", "\\");
        int rc = _context.StartProcess("msiexec", $"/a {package} TARGETDIR={testDir} /q");
        if (rc != 0)
            Console.WriteLine($"  ERROR: Installer returned {rc.ToString()}");
        else
        {
            // Administrative install is used to create a file image, from which
            // users may do their own installls. For security reasons, we can't
            // do a full install so we simulate the user portion of the install,
            // copying certain files to their final destination.
            _context.CopyFiles(
                PackageBinDir + "*.dll",
                PackageBinDir + "agents/net20/");
            _context.CopyFiles(
                PackageBinDir + "*.dll",
                PackageBinDir + "agents/net40/");
        }
    }
}

public class ZipPackageTester : NetFXPackageTester
{
    public ZipPackageTester(ICakeContext context, string packageVersion)
        : base(context, packageVersion)
    {
        // Add tests specific to the zip package
        PackageTests.Add(new PackageTest(
            "net35_plus_net40_project",
            "Run project with both copies of mock-assembly",
            $"../../NetFXTests.nunit --config={_config}",
            new ExpectedResult("Failed")
            {
                Total = 2 * 37,
                Passed = 2 * 23,
                Failed = 2 * 5,
                Warnings = 2 * 1,
                Inconclusive = 2 * 1,
                Skipped = 2 * 7
            }));
    }

    protected override string PackageName => $"NUnit.Console-{_packageVersion}.zip";
    protected override string PackageInstallDirectory => _packageDir + "test/zip/";
    protected override string PackageBinDir => PackageInstallDirectory + "bin/net20/";
    protected override string PackageResultDirectory => _packageDir + "test-results/zip/";
}
