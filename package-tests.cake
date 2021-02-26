// Representation of a single test to be run against a pre-built package.
public struct PackageTest
{
    public string Description;
    public string Arguments;
    public ExpectedResult ExpectedResult;

    public PackageTest(string description, string arguments, ExpectedResult expectedResult)
    {
        Description = description;
        Arguments = arguments;
        ExpectedResult = expectedResult;
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

        foreach (var packageTest in PackageTests)
        {
            var resultFile = _outputDir + "TestResult.xml";
            // Delete result file ahead of time so we don't mistakenly
            // read a left-over file from another test run. Leave the
            // file after the run in case we need it to debug a failure.
            if (_context.FileExists(resultFile))
                _context.DeleteFile(resultFile);

            DisplayBanner(packageTest.Description);

            int rc = _context.StartProcess(
                PackageBinDir + "nunit3-console.exe",
                new ProcessSettings()
                {
                    Arguments = packageTest.Arguments,
                    WorkingDirectory = _outputDir
                });

            try
            {
                var result = new ActualResult(resultFile);
                reporter.AddResult(packageTest, result);

                Console.WriteLine(result.Errors.Count == 0
                    ? "\nSUCCESS: Test Result matches expected result!"
                    : "\nERROR: Test Result not as expected!");
            }
            catch (Exception ex)
            {
                reporter.AddResult(packageTest, ex);

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

public abstract class NetFXPackageTester : PackageTester
{
    public NetFXPackageTester(ICakeContext context, string packageVersion)
        : base(context, packageVersion)
    {
        PackageTests.Add(new PackageTest(
            "Run mock-assembly.dll under .NET 3.5",
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

        PackageTests.Add(new PackageTest(
            "Run mock-assembly.dll under .NET 4.x",
            "net40/mock-assembly.dll",
            new ExpectedResult("Failed")
            {
                Total = 37,
                Passed = 23,
                Failed = 5,
                Warnings = 0,
                Inconclusive = 1,
                Skipped = 7
            }));

        PackageTests.Add(new PackageTest(
            "Run both copies of mock-assembly together",
            "net35/mock-assembly.dll net40/mock-assembly.dll",
            new ExpectedResult("Failed")
            {
                Total = 2 * 37,
                Passed = 2 * 23,
                Failed = 2 * 5,
                Warnings = 0,
                Inconclusive = 2 * 1,
                Skipped = 2 * 7
            }));
    }
}

public abstract class NetCorePackageTester : PackageTester
{
    public NetCorePackageTester(ICakeContext context, string packageVersion)
        : base(context, packageVersion)
    {
        PackageTests.Add(new PackageTest(
            "Run mock-assembly.dll targeting .NET Core 2.1",
            "netcoreapp2.1/mock-assembly.dll",
            new ExpectedResult("Failed")
            {
                Total = 37,
                Passed = 23,
                Failed = 5,
                Warnings = 0,
                Inconclusive = 1,
                Skipped = 7
            }));

        PackageTests.Add(new PackageTest(
            "Run mock-assembly targeting .NET Core 3.1",
            "netcoreapp3.1/mock-assembly.dll",
            new ExpectedResult("Failed")
            {
                Total = 37,
                Passed = 23,
                Failed = 5,
                Warnings = 0,
                Inconclusive = 1,
                Skipped = 7
            }));

        PackageTests.Add(new PackageTest(
            "Run both copies of mock-assembly together",
            "netcoreapp2.1/mock-assembly.dll netcoreapp3.1/mock-assembly.dll",
            new ExpectedResult("Failed")
            {
                Total = 2 * 37,
                Passed = 2 * 23,
                Failed = 2 * 5,
                Warnings = 0,
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
}

public class NuGetNetCorePackageTester : NetCorePackageTester
{
    public NuGetNetCorePackageTester(ICakeContext context, string packageVersion)
        : base(context, packageVersion) { }

    protected override string PackageName => $"NUnit.ConsoleRunner.NetCore.{_packageVersion}.nupkg";
    protected override string PackageInstallDirectory => _packageDir + "test/nuget-netcore/";
    protected override string PackageBinDir => PackageInstallDirectory + "tools/netcoreapp3.1/any/";
}

public class ChocolateyPackageTester : NetFXPackageTester
{
    public ChocolateyPackageTester(ICakeContext context, string packageVersion)
        : base(context, packageVersion) { }

    protected override string PackageName => $"nunit-console-runner.{_packageVersion}.nupkg";
    protected override string PackageInstallDirectory => _packageDir + "test/choco/";
    protected override string PackageBinDir => PackageInstallDirectory + "tools/";
}

public class MsiPackageTester : NetFXPackageTester
{
    public MsiPackageTester(ICakeContext context, string packageVersion)
        : base(context, packageVersion)
    {
        PackageTests.Add(new PackageTest(
            "Run project with both copies of mock-assembly",
            $"../../NetFXTests.nunit --config={_config}",
            new ExpectedResult("Failed")
            {
                Total = 2 * 37,
                Passed = 2 * 23,
                Failed = 2 * 5,
                Warnings = 0,
                Inconclusive = 2 * 1,
                Skipped = 2 * 7
            }));
    }

    protected override string PackageName => $"NUnit.Console-{_packageVersion}.msi";
    protected override string PackageInstallDirectory => _packageDir + "test/msi/";
    protected override string PackageBinDir => PackageInstallDirectory + "NUnit.org/nunit-console/";

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
            // Administrative install doesn't copy these files to
            // their final destination, so we do it.
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
        PackageTests.Add(new PackageTest(
            "Run project with both copies of mock-assembly",
            $"../../NetFXTests.nunit --config={_config}",
            new ExpectedResult("Failed")
            {
                Total = 2 * 37,
                Passed = 2 * 23,
                Failed = 2 * 5,
                Warnings = 0,
                Inconclusive = 2 * 1,
                Skipped = 2 * 7
            }));
    }

    protected override string PackageName => $"NUnit.Console-{_packageVersion}.zip";
    protected override string PackageInstallDirectory => _packageDir + "test/zip/";
    protected override string PackageBinDir => PackageInstallDirectory + "bin/net20/";
}
