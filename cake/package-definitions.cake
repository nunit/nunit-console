//////////////////////////////////////////////////////////////////////
// PACKAGE DEFINITION ABSTRACT CLASS
//////////////////////////////////////////////////////////////////////

/// <summary>
/// The abstract base of all packages
/// </summary>
public abstract class PackageDefinition
{
    protected BuildSettings _settings;
    protected ICakeContext _context;

    /// <summary>
    /// Construct without arguments - derived class must set properties
    /// </summary>
    protected PackageDefinition(BuildSettings settings)
    {
        _settings = settings;
        _context = settings.Context;
        PackageVersion = settings.ProductVersion;
    }

	public string PackageId { get; protected set; }
    public string PackageVersion { get; protected set; }
	public string PackageSource { get; protected set; }
    public string BasePath { get; protected set; }
    public string TestExecutable { get; protected set; }
    public PackageCheck[] PackageChecks { get; protected set; }
    public PackageCheck[] SymbolChecks { get; protected set; }
    public IEnumerable<PackageTest> PackageTests { get; protected set; }
    public bool HasTests => PackageTests != null;
    public bool HasChecks => PackageChecks != null;
    public bool HasSymbols => SymbolChecks != null;
    public virtual string SymbolPackageName => throw new System.NotImplementedException($"Symbols are not available for this type of package.");

    public abstract string PackageFileName { get; }
    public abstract string InstallDirectory { get; }
    public abstract string ResultDirectory { get; }

    protected abstract void doBuildPackage();
    protected abstract void doInstallPackage();

    public void BuildVerifyAndTest()
    {
        _context.EnsureDirectoryExists(PACKAGE_DIR);

        BuildPackage();
        InstallPackage();

        if (HasChecks)
            VerifyPackage();

        if (HasSymbols)
            VerifySymbolPackage();

        if (HasTests)
            TestPackage();
    }

    public void BuildPackage()
    {
        DisplayAction("Building");
        doBuildPackage();
    }

    public void InstallPackage()
    {
        DisplayAction("Installing");
        Console.WriteLine($"Installing package to {InstallDirectory}");
        _context.CleanDirectory(InstallDirectory);
        doInstallPackage();
    }

    public void VerifyPackage()
    {
        DisplayAction("Verifying");

        bool allOK = true;
        foreach (var check in PackageChecks)
            allOK &= check.Apply(InstallDirectory);

        if (allOK)
            WriteInfo("All checks passed!");
        else 
            throw new Exception("Verification failed!");
    }

    public void TestPackage()
    {
        DisplayAction("Testing");

        var reporter = new ResultReporter(PackageFileName);

        _context.CleanDirectory(ResultDirectory);

        foreach (var packageTest in PackageTests)
        {
            var testResultDir = ResultDirectory + packageTest.Name + "/";
            var resultFile = testResultDir + "TestResult.xml";

            DisplayBanner(packageTest.Description);

            Console.WriteLine($"Running {InstallDirectory + TestExecutable}");

            int rc = TestExecutable.EndsWith(".dll")
                ? _context.StartProcess(
                    "dotnet",
                    new ProcessSettings()
                    {
                        Arguments = $"\"{InstallDirectory}{TestExecutable}\" {packageTest.Arguments} --work={testResultDir}",
                    })
                : _context.StartProcess(
                    InstallDirectory + TestExecutable,
                    new ProcessSettings()
                    {
                        Arguments = $"{packageTest.Arguments} --work={testResultDir}",
                    });

            try
            {
                var result = new ActualResult(resultFile);
                var report = new TestReport(packageTest, result);
                reporter.AddReport(report);

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

    public void DisplayAction(string action)
    {
        DisplayBanner($"{action} package {PackageFileName}");
    }

    public virtual void VerifySymbolPackage() { } // Overridden for NuGet packages
}

//////////////////////////////////////////////////////////////////////
// NUGET PACKAGES
//////////////////////////////////////////////////////////////////////

// Users may only instantiate the derived classes, which avoids
// exposing PackageType and makes it impossible to create a
// PackageDefinition with an unknown package type.
public abstract class NuGetPackageDefinition : PackageDefinition
{
    protected NuGetPackageDefinition(BuildSettings settings) : base(settings) { }

    public override string PackageFileName => $"{PackageId}.{PackageVersion}.nupkg";
    public override string SymbolPackageName => System.IO.Path.ChangeExtension(PackageFileName, ".snupkg");
    public override string InstallDirectory => PACKAGE_TEST_DIR + $"nuget/{PackageId}/";
    public override string ResultDirectory => PACKAGE_RESULT_DIR + $"nuget/{PackageId}/";

    protected override void doBuildPackage()
    {
        var nugetPackSettings = new NuGetPackSettings()
        {
            Version = PackageVersion,
            OutputDirectory = PACKAGE_DIR,
            BasePath = BasePath,
            NoPackageAnalysis = true,
            Symbols = HasSymbols
        };

        if (HasSymbols)
            nugetPackSettings.SymbolPackageFormat = "snupkg";

        _context.NuGetPack(PackageSource, nugetPackSettings);
    }

    protected override void doInstallPackage()
    {
        _context.NuGetInstall(PackageId, new NuGetInstallSettings
        {
            Source = new[] { PACKAGE_DIR },
            Prerelease = true,
            OutputDirectory = PACKAGE_TEST_DIR + "nuget",
            ExcludeVersion = true
        });
    }
}

public class NUnitConsoleNuGetPackage : NuGetPackageDefinition
{
    public NUnitConsoleNuGetPackage(BuildSettings settings) : base(settings)
    {
        PackageId = "NUnit.Console";
        PackageSource = PROJECT_DIR + "nuget/runners/nunit.console-runner-with-extensions.nuspec";
        BasePath = PROJECT_DIR;
        PackageChecks = new PackageCheck[] { HasFile("LICENSE.txt") };
    }


    protected override void doInstallPackage()
    {
        // TODO: This has dependencies, which are only satisified
        // after we are done building, so we just unzip it to verify.
        _context.Unzip(PACKAGE_DIR + PackageFileName, InstallDirectory);
    }
}

public class NUnitConsoleRunnerNuGetPackage : NuGetPackageDefinition
{
    public NUnitConsoleRunnerNuGetPackage(BuildSettings settings) : base(settings)
    {
        PackageId = "NUnit.ConsoleRunner";
        PackageSource = PROJECT_DIR + "nuget/runners/nunit.console-runner.nuspec";
        BasePath = settings.NetFxConsoleBinDir;
        PackageChecks = new PackageCheck[] {
            HasFiles("LICENSE.txt", "NOTICES.txt"),
            HasDirectory("tools").WithFiles(
                "nunit4-console.exe", "nunit4-console.exe.config",
                "nunit.engine.dll", "nunit.engine.core.dll", "nunit.engine.api.dll",
                "testcentric.engine.metadata.dll", "nunit.console.nuget.addins"),
            HasDirectory("tools/agents/nunit-agent-net20").WithFiles(
                "nunit-agent-net20.exe", "nunit-agent-net20.exe.config",
                "nunit-agent-net20-x86.exe", "nunit-agent-net20-x86.exe.config",
                "nunit.engine.core.dll", "nunit.engine.api.dll", "testcentric.engine.metadata.dll"),
            HasDirectory("tools/agents/nunit-agent-net462").WithFiles(
                "nunit-agent-net462.exe", "nunit-agent-net462.exe.config",
                "nunit-agent-net462-x86.exe", "nunit-agent-net462-x86.exe.config",
                "nunit.engine.core.dll", "nunit.engine.api.dll", "testcentric.engine.metadata.dll"),
            HasDirectory("tools/agents/nunit-agent-netcore31").WithFiles(
                "nunit-agent-netcore31.dll", "nunit-agent-netcore31.dll.config", "Microsoft.Extensions.DependencyModel.dll",
                "nunit.engine.core.dll", "nunit.engine.api.dll", "testcentric.engine.metadata.dll"),
            HasDirectory("tools/agents/nunit-agent-net50").WithFiles(
                "nunit-agent-net50.dll", "nunit-agent-net50.dll.config", "Microsoft.Extensions.DependencyModel.dll",
                "nunit.engine.core.dll", "nunit.engine.api.dll", "testcentric.engine.metadata.dll"),
            HasDirectory("tools/agents/nunit-agent-net60").WithFiles(
                "nunit-agent-net60.dll", "nunit-agent-net60.dll.config", "Microsoft.Extensions.DependencyModel.dll",
                "nunit.engine.core.dll", "nunit.engine.api.dll", "testcentric.engine.metadata.dll"),
            HasDirectory("tools/agents/nunit-agent-net70").WithFiles(
                "nunit-agent-net70.dll", "nunit-agent-net70.dll.config", "Microsoft.Extensions.DependencyModel.dll",
                "nunit.engine.core.dll", "nunit.engine.api.dll", "testcentric.engine.metadata.dll"),
        };
        SymbolChecks = new PackageCheck[] {
            HasDirectory("tools").WithFiles(
                "nunit4-console.pdb", "nunit.engine.pdb", "nunit.engine.core.pdb", "nunit.engine.api.pdb"),
            HasDirectory("tools/agents/net20").WithFiles(
                "nunit-agent.pdb", "nunit-agent-x86.pdb", "nunit.engine.core.pdb", "nunit.engine.api.pdb"),
            HasDirectory("tools/agents/net462").WithFiles(
                "nunit-agent.pdb", "nunit-agent-x86.pdb", "nunit.engine.core.pdb", "nunit.engine.api.pdb"),
            HasDirectory("tools/agents/netcoreapp3.1").WithFiles(
                "nunit-agent.pdb", "nunit.engine.core.pdb", "nunit.engine.api.pdb"),
            HasDirectory("tools/agents/net5.0").WithFiles(
                "nunit-agent.pdb", "nunit.engine.core.pdb", "nunit.engine.api.pdb"),
            HasDirectory("tools/agents/net6.0").WithFiles(
                "nunit-agent.pdb", "nunit.engine.core.pdb", "nunit.engine.api.pdb"),
            HasDirectory("tools/agents/net7.0").WithFiles(
                "nunit-agent.pdb", "nunit.engine.core.pdb", "nunit.engine.api.pdb")
        };
        TestExecutable = "tools/nunit4-console.exe";
        PackageTests = settings.StandardRunnerTests;
    }
}

public class NUnitNetCoreConsoleRunnerPackage : NuGetPackageDefinition
{
    public NUnitNetCoreConsoleRunnerPackage(BuildSettings settings) : base(settings)
    {
        PackageId = "NUnit.ConsoleRunner.NetCore";
        PackageSource = NETCORE_CONSOLE_PROJECT;
        BasePath = settings.NetCoreConsoleBinDir;
        PackageChecks = new PackageCheck[] {
            HasDirectory("content").WithFiles("LICENSE.txt", "NOTICES.txt"),
            HasDirectory($"tools/{NETCORE_CONSOLE_TARGET}/any").WithFiles(
                "nunit4-netcore-console.dll", "nunit4-netcore-console.dll.config",
                "nunit.engine.dll", "nunit.engine.core.dll", "nunit.engine.api.dll",
                "testcentric.engine.metadata.dll")};
        SymbolChecks = new PackageCheck[] {
            HasDirectory($"tools/{NETCORE_CONSOLE_TARGET}/any").WithFiles(
                "nunit4-netcore-console.pdb",
                "nunit.engine.pdb", "nunit.engine.core.pdb", "nunit.engine.api.pdb",
                "testcentric.engine.metadata.pdb")};
        TestExecutable = $"tools/{NETCORE_CONSOLE_TARGET}/any/nunit4-netcore-console.dll";
        PackageTests = settings.NetCoreRunnerTests;
    }

    // Build package from project file
    protected override void doBuildPackage()
    {
        var settings = new DotNetPackSettings()
        {
            Configuration = _settings.Configuration,
            OutputDirectory = PACKAGE_DIR,
            IncludeSymbols = HasSymbols,
            ArgumentCustomization = args => args.Append($"/p:Version={PackageVersion}")
        };

        if (HasSymbols)
            settings.SymbolPackageFormat = "snupkg";

        _context.DotNetPack(PackageSource, settings);
    }

    protected override void doInstallPackage()
    {
        // TODO: We can't use NuGet to install this package because
        // it's a CLI tool package. For now, just unzip it.
        _context.Unzip(PACKAGE_DIR + PackageFileName, InstallDirectory);
    }
}

public class NUnitEngineNuGetPackage : NuGetPackageDefinition
{
    public NUnitEngineNuGetPackage(BuildSettings settings) : base(settings)
    {
        PackageId = "NUnit.Engine";
        PackageSource = PROJECT_DIR + "nuget/engine/nunit.engine.nuspec";
        BasePath = PROJECT_DIR + $"src/NUnitEngine/nunit.engine/bin/{settings.Configuration}/";
        PackageChecks = new PackageCheck[] {
            HasFiles("LICENSE.txt", "NOTICES.txt"),
            HasDirectory("lib/net462").WithFiles(
                "nunit.engine.dll", "nunit.engine.core.dll", "nunit.engine.api.dll",
                "testcentric.engine.metadata.dll", "nunit.engine.nuget.addins"),
            HasDirectory("lib/netstandard2.0").WithFiles(
                "nunit.engine.dll", "nunit.engine.core.dll", "nunit.engine.api.dll",
                "testcentric.engine.metadata.dll", "nunit.engine.nuget.addins"),
            HasDirectory("lib/netcoreapp3.1").WithFiles(
                "nunit.engine.dll", "nunit.engine.core.dll", "nunit.engine.api.dll",
                "testcentric.engine.metadata.dll", "nunit.engine.nuget.addins"),
            HasDirectory("agents/nunit-agent-net20").WithFiles(
                "nunit-agent-net20.exe", "nunit-agent-net20.exe.config",
                "nunit-agent-net20-x86.exe", "nunit-agent-net20-x86.exe.config",
                "nunit.engine.core.dll", "nunit.engine.api.dll", "testcentric.engine.metadata.dll"),
            HasDirectory("agents/nunit-agent-net462").WithFiles(
                "nunit-agent-net462.exe", "nunit-agent-net462.exe.config",
                "nunit-agent-net462-x86.exe", "nunit-agent-net462-x86.exe.config",
                "nunit.engine.core.dll", "nunit.engine.api.dll", "testcentric.engine.metadata.dll"),
            HasDirectory("agents/nunit-agent-netcore31").WithFiles(
                "nunit-agent-netcore31.dll", "nunit-agent-netcore31.dll.config", "Microsoft.Extensions.DependencyModel.dll",
                "nunit.engine.core.dll", "nunit.engine.api.dll", "testcentric.engine.metadata.dll"),
        };
        SymbolChecks = new PackageCheck[] {
            HasDirectory("lib/net462").WithFiles(
                "nunit.engine.pdb", "nunit.engine.core.pdb", "nunit.engine.api.pdb"),
            HasDirectory("lib/netstandard2.0").WithFiles(
                "nunit.engine.pdb", "nunit.engine.core.pdb", "nunit.engine.api.pdb"),
            HasDirectory("lib/netcoreapp3.1").WithFiles(
                "nunit.engine.pdb", "nunit.engine.core.pdb", "nunit.engine.api.pdb"),
            HasDirectory("agents/net20").WithFiles(
                "nunit-agent.pdb", "nunit-agent-x86.pdb", "nunit.engine.core.pdb", "nunit.engine.api.pdb"),
            HasDirectory("agents/net462").WithFiles(
                "nunit-agent.pdb", "nunit-agent-x86.pdb", "nunit.engine.core.pdb", "nunit.engine.api.pdb"),
            HasDirectory("agents/netcoreapp3.1").WithFiles("nunit-agent.pdb", "nunit.engine.core.pdb", "nunit.engine.api.pdb")
        };
    }
}

public class NUnitEngineApiNuGetPackage : NuGetPackageDefinition
{
    public NUnitEngineApiNuGetPackage(BuildSettings settings) : base(settings)
    {
        PackageId = "NUnit.Engine.Api";
        PackageSource = PROJECT_DIR + "nuget/engine/nunit.engine.api.nuspec";
        BasePath = PROJECT_DIR + $"src/NUnitEngine/nunit.engine.api/bin/{settings.Configuration}/";
        PackageChecks = new PackageCheck[] {
            HasFile("LICENSE.txt"),
            HasDirectory("lib/net20").WithFile("nunit.engine.api.dll"),
            HasDirectory("lib/netstandard2.0").WithFile("nunit.engine.api.dll"),
        };
        SymbolChecks = new PackageCheck[] {
            HasDirectory("lib/net20").WithFile("nunit.engine.api.pdb"),
            HasDirectory("lib/netstandard2.0").WithFile("nunit.engine.api.pdb")
        };
    }
}

//////////////////////////////////////////////////////////////////////
// CHOCOLATEY PACKAGE
//////////////////////////////////////////////////////////////////////

public abstract class ChocolateyPackageDefinition : PackageDefinition
{
    protected ChocolateyPackageDefinition(BuildSettings settings) : base(settings) { }

    public override string PackageFileName => $"{PackageId}.{PackageVersion}.nupkg";
    public override string InstallDirectory => PACKAGE_TEST_DIR + $"choco/{PackageId}/";
    public override string ResultDirectory => PACKAGE_RESULT_DIR + $"choco/{PackageId}/";
    
    protected override void doBuildPackage()
    {
        _context.ChocolateyPack(PackageSource,
            new ChocolateyPackSettings()
            {
                Version = PackageVersion,
                OutputDirectory = PACKAGE_DIR,
                ArgumentCustomization = args => args.Append($"BASE={BasePath}")
            });
    }

    protected override void doInstallPackage()
    {
        // TODO: We can't run chocolatey install effectively
        // so for now we just unzip the package.
        _context.Unzip(PACKAGE_DIR + PackageFileName, InstallDirectory);
    }
}

public class NUnitConsoleRunnerChocolateyPackage : ChocolateyPackageDefinition
{
    public NUnitConsoleRunnerChocolateyPackage(BuildSettings settings) : base(settings)
    {
        PackageId = "nunit-console-runner";
        PackageSource = PROJECT_DIR + "choco/nunit-console-runner.nuspec";
        BasePath = settings.NetFxConsoleBinDir;
        PackageChecks = new PackageCheck[] {
            HasDirectory("tools").WithFiles(
                "LICENSE.txt", "NOTICES.txt", "VERIFICATION.txt", "nunit.choco.addins",
                "nunit4-console.exe", "nunit4-console.exe.config",
                "nunit.engine.dll", "nunit.engine.core.dll", "nunit.engine.api.dll", "testcentric.engine.metadata.dll"),
            HasDirectory("tools/agents/nunit-agent-net20").WithFiles(
                "nunit-agent-net20.exe", "nunit-agent-net20.exe.config",
                "nunit-agent-net20-x86.exe", "nunit-agent-net20-x86.exe.config"),
            HasDirectory("tools/agents/nunit-agent-net462").WithFiles(
                "nunit-agent-net462.exe", "nunit-agent-net462.exe.config",
                "nunit-agent-net462-x86.exe", "nunit-agent-net462-x86.exe.config"),
            HasDirectory("tools/agents/nunit-agent-netcore31").WithFiles(
                "nunit-agent-netcore31.dll", "nunit-agent-netcore31.dll.config"),
            HasDirectory("tools/agents/nunit-agent-net50").WithFiles(
                "nunit-agent-net50.dll", "nunit-agent-net50.dll.config"),
            HasDirectory("tools/agents/nunit-agent-net60").WithFiles(
                "nunit-agent-net60.dll", "nunit-agent-net60.dll.config"),
            HasDirectory("tools/agents/nunit-agent-net70").WithFiles(
                "nunit-agent-net70.dll", "nunit-agent-net70.dll.config"),
        };
        TestExecutable = "tools/nunit4-console.exe";
        PackageTests = settings.StandardRunnerTests;
    }
}

//////////////////////////////////////////////////////////////////////
// MSI PACKAGE
//////////////////////////////////////////////////////////////////////

public abstract class MsiPackageDefinition : PackageDefinition
{
    protected MsiPackageDefinition(BuildSettings settings) : base(settings) 
    {
        // Required version format for MSI
        PackageVersion = settings.BuildVersion.SemVer;
    }

    public override string PackageFileName => $"{PackageId}-{PackageVersion}.msi";
    public override string InstallDirectory => PACKAGE_TEST_DIR + $"msi/{PackageId}/";
    public override string ResultDirectory => PACKAGE_RESULT_DIR + $"msi/{PackageId}/";

    protected override void doBuildPackage()
    {
        _context.MSBuild(PackageSource, new MSBuildSettings()
            .WithTarget("Rebuild")
            .SetConfiguration(_settings.Configuration)
            .WithProperty("Version", PackageVersion)
            .WithProperty("DisplayVersion", PackageVersion)
            .WithProperty("OutDir", PACKAGE_DIR)
            .WithProperty("Image", BasePath)
            .SetMSBuildPlatform(MSBuildPlatform.x86)
            .SetNodeReuse(false));
    }

    protected override void doInstallPackage()
    {
        // Msiexec does not tolerate forward slashes!
        string testDir = PACKAGE_TEST_DIR.Replace('/', '\\') + "msi\\" + PackageId;
        string packageUnderTest = PACKAGE_DIR.Replace('/', '\\') + PackageFileName;

        int rc = _context.StartProcess("msiexec", $"/a {packageUnderTest} TARGETDIR={testDir} /q");
        if (rc != 0)
            Console.WriteLine($"  ERROR: Installer returned {rc.ToString()}");
    }
}

public class NUnitConsoleMsiPackage : MsiPackageDefinition
{
    public NUnitConsoleMsiPackage(BuildSettings settings) : base(settings)
    {
        PackageId = "NUnit.Console";
        PackageSource = PROJECT_DIR + "msi/nunit/nunit.wixproj";
        BasePath = settings.NetFxConsoleBinDir;
        PackageVersion = settings.BuildVersion.SemVer;
        PackageChecks = new PackageCheck[] {
            HasDirectory("NUnit.org").WithFiles("LICENSE.txt", "NOTICES.txt", "nunit.ico"),
            HasDirectory("NUnit.org/nunit-console").WithFiles(
                "nunit4-console.exe", "nunit4-console.exe.config",
                "nunit.engine.dll", "nunit.engine.core.dll", "nunit.engine.api.dll",
                "testcentric.engine.metadata.dll", "nunit.bundle.addins"),
            HasDirectory("NUnit.org/nunit-console/agents/nunit-agent-net20").WithFiles(
                "nunit-agent-net20.exe", "nunit-agent-net20.exe.config",
                "nunit-agent-net20-x86.exe", "nunit-agent-net20-x86.exe.config"),
            HasDirectory("NUnit.org/nunit-console/agents/nunit-agent-net462").WithFiles(
                "nunit-agent-net462.exe", "nunit-agent-net462.exe.config",
                "nunit-agent-net462-x86.exe", "nunit-agent-net462-x86.exe.config"),
            HasDirectory("NUnit.org/nunit-console/agents/nunit-agent-netcore31").WithFiles(
                "nunit-agent-netcore31.dll", "nunit-agent-netcore31.dll.config"),
            HasDirectory("NUnit.org/nunit-console/agents/nunit-agent-net50").WithFiles(
                "nunit-agent-net50.dll", "nunit-agent-net50.dll.config"),
            HasDirectory("NUnit.org/nunit-console/agents/nunit-agent-net60").WithFiles(
                "nunit-agent-net60.dll", "nunit-agent-net60.dll.config"),
            HasDirectory("NUnit.org/nunit-console/agents/nunit-agent-net70").WithFiles(
                "nunit-agent-net70.dll", "nunit-agent-net70.dll.config"),
            HasDirectory("Nunit.org/nunit-console/addins").WithFile("nunit-project-loader.dll")
        };
        TestExecutable = "NUnit.org/nunit-console/nunit4-console.exe";
        PackageTests = settings.StandardRunnerTests.Concat(new[] { settings.NUnitProjectTest });
    }
}

//////////////////////////////////////////////////////////////////////
// ZIP PACKAGE
//////////////////////////////////////////////////////////////////////

public abstract class ZipPackageDefinition : PackageDefinition
{
    protected ZipPackageDefinition(BuildSettings settings) : base(settings) { }

    public override string PackageFileName => $"{PackageId}-{PackageVersion}.zip";
    public override string InstallDirectory => PACKAGE_TEST_DIR + $"zip/{PackageId}/";
    public override string ResultDirectory => PACKAGE_RESULT_DIR + $"zip/{PackageId}/";

    protected abstract string ZipImageDirectory { get; }
    
    protected override void doBuildPackage()
    {
        _context.Zip(ZipImageDirectory, PACKAGE_DIR + PackageFileName);
    }

    protected override void doInstallPackage()
    {
        _context.Unzip(PACKAGE_DIR + PackageFileName, InstallDirectory);
    }
}

public class NUnitConsoleZipPackage : ZipPackageDefinition
{
    public NUnitConsoleZipPackage(BuildSettings settings) : base(settings)
    {
        PackageId = "NUnit.Console";
        PackageSource = ZipImageDirectory;
        BasePath = ZipImageDirectory;
        PackageChecks = new PackageCheck[] {
            HasFiles("LICENSE.txt", "NOTICES.txt", "CHANGES.txt", "nunit.ico"),
            HasDirectory("bin").WithFiles(
                "nunit4-console.exe", "nunit4-console.exe.config",
                "nunit.engine.dll", "nunit.engine.core.dll", "nunit.engine.api.dll",
                "testcentric.engine.metadata.dll", "nunit.bundle.addins",
                "nunit4-console.pdb", "nunit.engine.pdb", "nunit.engine.core.pdb", "nunit.engine.api.pdb"),
            HasDirectory("bin/agents/nunit-agent-net20").WithFiles(
                "nunit-agent-net20.exe", "nunit-agent-net20.exe.config",
                "nunit-agent-net20-x86.exe", "nunit-agent-net20-x86.exe.config",
                "nunit.engine.core.dll", "nunit.engine.api.dll", "testcentric.engine.metadata.dll",
                "nunit-agent-net20.pdb", "nunit-agent-net20-x86.pdb",
                "nunit.engine.core.pdb", "nunit.engine.api.pdb"),
            HasDirectory("bin/agents/nunit-agent-net462").WithFiles(
                "nunit-agent-net462.exe", "nunit-agent-net462.exe.config",
                "nunit-agent-net462-x86.exe", "nunit-agent-net462-x86.exe.config",
                "nunit.engine.core.dll", "nunit.engine.api.dll", "testcentric.engine.metadata.dll",
                "nunit-agent-net462.pdb", "nunit-agent-net462-x86.pdb",
                "nunit.engine.core.pdb", "nunit.engine.api.pdb"),
            HasDirectory("bin/agents/nunit-agent-netcore31").WithFiles(
                "nunit-agent-netcore31.dll", "nunit-agent-netcore31.dll.config",
                "nunit.engine.core.dll", "nunit.engine.api.dll", "testcentric.engine.metadata.dll",
                "Microsoft.Extensions.DependencyModel.dll",
                "nunit.engine.core.pdb", "nunit.engine.api.pdb"),
            HasDirectory("bin/agents/nunit-agent-net50").WithFiles(
                "nunit-agent-net50.dll", "nunit-agent-net50.dll.config",
                "nunit.engine.core.dll", "nunit.engine.api.dll", "testcentric.engine.metadata.dll",
                "Microsoft.Extensions.DependencyModel.dll",
                "nunit.engine.core.pdb", "nunit.engine.api.pdb"),
            HasDirectory("bin/agents/nunit-agent-net60").WithFiles(
                "nunit-agent-net60.dll", "nunit-agent-net60.dll.config",
                "nunit.engine.core.dll", "nunit.engine.api.dll", "testcentric.engine.metadata.dll",
                "Microsoft.Extensions.DependencyModel.dll",
                "nunit.engine.core.pdb", "nunit.engine.api.pdb"),
            HasDirectory("bin/agents/nunit-agent-net70").WithFiles(
                 "nunit-agent-net70.dll", "nunit-agent-net70.dll.config",
                "nunit.engine.core.dll", "nunit.engine.api.dll", "testcentric.engine.metadata.dll",
                "Microsoft.Extensions.DependencyModel.dll",
                "nunit.engine.core.pdb", "nunit.engine.api.pdb"),
};
        TestExecutable = "bin/nunit4-console.exe";
        PackageTests = settings.StandardRunnerTests.Concat(new [] { settings.NUnitProjectTest });
    }

    protected override string ZipImageDirectory => PACKAGE_DIR + "zip-image";
}
