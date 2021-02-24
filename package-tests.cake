// Representation of a single test to be run against a pre-built package.
public struct PackageTest
{
    public string Description;
    public string Arguments;

    public PackageTest(string description, string arguments)
    {
        Description = description;
        Arguments = arguments;
    }
}

// Abstract base for all package testers. 
public abstract class PackageTester
{
    protected ICakeContext _context;
    protected string _packageVersion;
    protected string _packageDir;
    protected string _outputDir;

    public PackageTester(ICakeContext context, string packageVersion)
    {
        _context = context;
        _packageVersion = packageVersion;
        _packageDir = System.IO.Path.GetFullPath(context.Argument("artifact-dir", "package")) + "/";
        _outputDir = System.IO.Path.GetFullPath("bin/" + context.Argument("configuration", "Release")) + "/";

        PackageTests = new List<PackageTest>();
    }

    protected abstract string PackageName { get; }
    protected abstract string PackageInstallDirectory { get; }
    protected abstract string PackageBinDir { get; }
    //protected abstract string ExtensionInstallDirectory { get; }

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
        foreach (var packageTest in PackageTests)
        {
            DisplayBanner(packageTest.Description);
            DisplayTestEnvironment(packageTest);

            int rc = _context.StartProcess(
                PackageBinDir + "nunit3-console.exe",
                new ProcessSettings()
                {
                    Arguments = packageTest.Arguments,
                    WorkingDirectory = _outputDir
                });
        }
    }

    private void DisplayBanner(string message)
    {
        Console.WriteLine("\n=================================================="); ;
        Console.WriteLine(message);
        Console.WriteLine("==================================================");
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

public abstract class NetFXPackageTester : PackageTester
{
    public NetFXPackageTester(ICakeContext context, string packageVersion)
        : base(context, packageVersion)
    {
        PackageTests.Add(new PackageTest(
            "Run mock-assembly.dll under .NET 3.5",
            "net35/mock-assembly.dll"));
    }
}

public abstract class NetCorePackageTester : PackageTester
{
    public NetCorePackageTester(ICakeContext context, string packageVersion)
        : base(context, packageVersion)
    {
        PackageTests.Add(new PackageTest(
            "Run mock-assembly.dll targeting .NET Core 2.1",
            "netcoreapp2.1/mock-assembly.dll"));
    }
}

public class NuGetNetFXPackageTester : NetFXPackageTester
{
    public NuGetNetFXPackageTester(ICakeContext context, string packageVersion)
        : base(context, packageVersion) { }

    protected override string PackageName => $"NUnit.ConsoleRunner.{_packageVersion}.nupkg";
    protected override string PackageInstallDirectory => _packageDir + "test/nuget-netfx/";
    protected override string PackageBinDir => PackageInstallDirectory + "tools/";
    //protected override string ExtensionInstallDirectory => _parameters.PackageInstallDirectory;
}

public class NuGetNetCorePackageTester : NetCorePackageTester
{
    public NuGetNetCorePackageTester(ICakeContext context, string packageVersion)
        : base(context, packageVersion) { }

    protected override string PackageName => $"NUnit.ConsoleRunner.NetCore.{_packageVersion}.nupkg";
    protected override string PackageInstallDirectory => _packageDir + "test/nuget-netcore/";
    protected override string PackageBinDir => PackageInstallDirectory + "tools/netcoreapp3.1/any/";
    //protected override string ExtensionInstallDirectory => _parameters.PackageInstallDirectory;
}

public class ChocolateyPackageTester : NetFXPackageTester
{
    public ChocolateyPackageTester(ICakeContext context, string packageVersion)
        : base(context, packageVersion) { }

    protected override string PackageName => $"nunit-console-runner.{_packageVersion}.nupkg";
    protected override string PackageInstallDirectory => _packageDir + "test/choco/";
    protected override string PackageBinDir => PackageInstallDirectory + "tools/";
    //protected override string ExtensionInstallDirectory => _parameters.PackageInstallDirectory;
}

public class MsiPackageTester : NetFXPackageTester
{
    public MsiPackageTester(ICakeContext context, string packageVersion)
        : base(context, packageVersion) { }

    protected override string PackageName => $"NUnit.Console-{_packageVersion}.msi";
    protected override string PackageInstallDirectory => _packageDir + "test/msi/";
    protected override string PackageBinDir => PackageInstallDirectory + "NUnit.org/nunit-console/";
    //protected override string ExtensionInstallDirectory => _parameters.PackageInstallDirectory;

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
        : base(context, packageVersion) { }

    protected override string PackageName => $"NUnit.Console-{_packageVersion}.zip";
    protected override string PackageInstallDirectory => _packageDir + "test/zip/";
    protected override string PackageBinDir => PackageInstallDirectory + "bin/net20/";
    //protected override string ExtensionInstallDirectory => _parameters.PackageInstallDirectory;
}
