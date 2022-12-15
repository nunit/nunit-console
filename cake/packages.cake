//////////////////////////////////////////////////////////////////////
// NUGET PACKAGES
//////////////////////////////////////////////////////////////////////

public abstract class NuGetPackageDefinition : PackageDefinition
{
    protected NuGetPackageDefinition(ICakeContext context, string packageVersion) : base(context, packageVersion) { }

    public override string PackageFileName => $"{PackageId}.{PackageVersion}.nupkg";
    public override string SymbolPackageName => System.IO.Path.ChangeExtension(PackageFileName, ".snupkg");
    public override string InstallDirectory => PACKAGE_TEST_DIR + $"nuget/{PackageId}/";
    public override string ResultDirectory => PACKAGE_RESULT_DIR + $"nuget/{PackageId}/";

    protected override void doBuildPackage()
    {
        DisplayAction("Building");

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
    public NUnitConsoleNuGetPackage(ICakeContext context, string packageVersion) : base(context, packageVersion)
    {
        PackageId = "NUnit.Console";
        PackageSource = PROJECT_DIR + "nuget/runners/nunit.console-runner-with-extensions.nuspec";
        BasePath = PROJECT_DIR;
        PackageChecks = new PackageCheck[] { HasFile("LICENSE.txt") };
    }
}

public class NUnitConsoleRunnerNuGetPackage : NuGetPackageDefinition
{
    public NUnitConsoleRunnerNuGetPackage(ICakeContext context, string packageVersion) : base(context, packageVersion)
    {
        PackageId = "NUnit.ConsoleRunner";
        PackageSource = PROJECT_DIR + "nuget/runners/nunit.console-runner.nuspec";
        BasePath = NETFX_CONSOLE_DIR;
        PackageChecks = new PackageCheck[] {
            HasFiles("LICENSE.txt", "NOTICES.txt"),
            HasDirectory("tools").WithFiles(CONSOLE_FILES).AndFiles(ENGINE_FILES).AndFile("nunit.console.nuget.addins"),
            HasDirectory("tools/agents/net20").WithFiles(AGENT_FILES).AndFile("nunit.agent.addins"),
            HasDirectory("tools/agents/net462").WithFiles(AGENT_FILES).AndFile("nunit.agent.addins"),
            HasDirectory("tools/agents/netcoreapp3.1").WithFiles(AGENT_FILES_NETCORE).AndFile("nunit.agent.addins"),
            HasDirectory("tools/agents/net5.0").WithFiles(AGENT_FILES_NETCORE).AndFile("nunit.agent.addins"),
            HasDirectory("tools/agents/net6.0").WithFiles(AGENT_FILES_NETCORE).AndFile("nunit.agent.addins"),
            HasDirectory("tools/agents/net7.0").WithFiles(AGENT_FILES_NETCORE).AndFile("nunit.agent.addins")
        };
        SymbolChecks = new PackageCheck[] {
            HasDirectory("tools").WithFiles(ENGINE_PDB_FILES).AndFile("nunit3-console.pdb"),
            HasDirectory("tools/agents/net20").WithFiles(AGENT_PDB_FILES),
            HasDirectory("tools/agents/net462").WithFiles(AGENT_PDB_FILES),
            HasDirectory("tools/agents/netcoreapp3.1").WithFiles(AGENT_PDB_FILES_NETCORE),
            HasDirectory("tools/agents/net5.0").WithFiles(AGENT_PDB_FILES_NETCORE),
            HasDirectory("tools/agents/net6.0").WithFiles(AGENT_PDB_FILES_NETCORE),
            HasDirectory("tools/agents/net7.0").WithFiles(AGENT_PDB_FILES_NETCORE)
        };
        TestExecutable = "tools/nunit3-console.exe";
        PackageTests = StandardRunnerTests;
    }
}

public class NUnitNetCoreConsoleRunnerPackage : NuGetPackageDefinition
{
    public NUnitNetCoreConsoleRunnerPackage(ICakeContext context, string packageVersion) : base(context, packageVersion)
    {
        PackageId = "NUnit.ConsoleRunner.NetCore";
        PackageSource = PROJECT_DIR + "nuget/runners/nunit.console-runner.netcore.nuspec";
        BasePath = NETCORE_CONSOLE_DIR;
        PackageChecks = new PackageCheck[] {
            HasFiles("LICENSE.txt", "NOTICES.txt"),
            HasDirectory($"tools/{NETCORE_CONSOLE_TARGET}").WithFiles(CONSOLE_FILES_NETCORE).AndFiles(ENGINE_FILES).AndFile("nunit.console.nuget.addins")
        };
        SymbolChecks = new PackageCheck[] {
            HasDirectory($"tools/{NETCORE_CONSOLE_TARGET}").WithFile("nunit3-netcore-console.pdb").AndFiles(ENGINE_PDB_FILES)
        };
        TestExecutable = $"tools/{NETCORE_CONSOLE_TARGET}/nunit3-netcore-console.exe";
        PackageTests = NetCoreRunnerTests;
    }

    protected override void doInstallPackage()
    {
        // TODO: We can't use NuGet to install this package because
        // it's a CLI tool package. For now, just unzip it.
        _context.Unzip(PackageFilePath, InstallDirectory);
    }
}

public class NUnitEnginePackage : NuGetPackageDefinition
{
    public NUnitEnginePackage(ICakeContext context, string packageVersion) : base(context, packageVersion)
    {
        PackageId = "NUnit.Engine";
        PackageSource = PROJECT_DIR + "nuget/engine/nunit.engine.nuspec";
        BasePath = ENGINE_PROJECT_BIN_DIR;
        PackageChecks = new PackageCheck[] {
            HasFiles("LICENSE.txt", "NOTICES.txt"),
            HasDirectory("lib/net462").WithFiles(ENGINE_FILES),
            HasDirectory("lib/netstandard2.0").WithFiles(ENGINE_FILES),
            HasDirectory("lib/netcoreapp3.1").WithFiles(ENGINE_CORE_FILES),
            HasDirectory("contentFiles/any/lib/net462").WithFile("nunit.engine.nuget.addins"),
            HasDirectory("contentFiles/any/lib/netstandard2.0").WithFile("nunit.engine.nuget.addins"),
            HasDirectory("contentFiles/any/lib/netcoreapp3.1").WithFile("nunit.engine.nuget.addins"),
            HasDirectory("agents/net20").WithFiles(AGENT_FILES).AndFile("nunit.agent.addins"),
            HasDirectory("agents/net462").WithFiles(AGENT_FILES).AndFile("nunit.agent.addins"),
            HasDirectory("agents/netcoreapp3.1").WithFile("nunit.agent.addins")
        };
        SymbolChecks = new PackageCheck[] {
            HasDirectory("lib/net462").WithFiles(ENGINE_PDB_FILES),
            HasDirectory("lib/netstandard2.0").WithFiles(ENGINE_PDB_FILES),
            HasDirectory("lib/netcoreapp3.1").WithFiles(ENGINE_PDB_FILES),
            HasDirectory("agents/net20").WithFiles(AGENT_PDB_FILES),
            HasDirectory("agents/net462").WithFiles(AGENT_PDB_FILES),
            HasDirectory("agents/netcoreapp3.1").WithFiles(AGENT_PDB_FILES_NETCORE)
        };
    }
}

public class NUnitEngineApiPackage : NuGetPackageDefinition
{
    public NUnitEngineApiPackage(ICakeContext context, string packageVersion) : base(context, packageVersion)
    {
        PackageId = "NUnit.Engine.Api";
        PackageSource = PROJECT_DIR + "nuget/engine/nunit.engine.api.nuspec";
        BasePath = ENGINE_API_PROJECT_BIN_DIR;
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
    protected ChocolateyPackageDefinition(ICakeContext context, string packageVersion) : base(context, packageVersion) { }

    public override string PackageFileName => $"{PackageId}.{PackageVersion}.nupkg";
    public override string InstallDirectory => PACKAGE_TEST_DIR + $"choco/{PackageId}/";
    public override string ResultDirectory => PACKAGE_RESULT_DIR + $"choco/{PackageId}/";
    
    protected override void doBuildPackage()
    {
        DisplayAction("Building");

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
        _context.Unzip(PackageFilePath, InstallDirectory);
    }
}

public class NUnitConsoleRunnerChocolateyPackage : ChocolateyPackageDefinition
{
    public NUnitConsoleRunnerChocolateyPackage(ICakeContext context, string packageVersion) : base(context, packageVersion)
    {
        PackageId = "nunit-console-runner";
        PackageSource = PROJECT_DIR + "choco/nunit-console-runner.nuspec";
        BasePath = NETFX_CONSOLE_DIR;
        PackageChecks = new PackageCheck[] {
            HasDirectory("tools").WithFiles("LICENSE.txt", "NOTICES.txt", "VERIFICATION.txt").AndFiles(CONSOLE_FILES).AndFiles(ENGINE_FILES).AndFile("nunit.choco.addins"),
            HasDirectory("tools/agents/net20").WithFiles(AGENT_FILES).AndFile("nunit.agent.addins"),
            HasDirectory("tools/agents/net462").WithFiles(AGENT_FILES).AndFile("nunit.agent.addins"),
            HasDirectory("tools/agents/netcoreapp3.1").WithFiles(AGENT_FILES_NETCORE).AndFile("nunit.agent.addins"),
            HasDirectory("tools/agents/net5.0").WithFiles(AGENT_FILES_NETCORE).AndFile("nunit.agent.addins"),
            HasDirectory("tools/agents/net6.0").WithFiles(AGENT_FILES_NETCORE).AndFile("nunit.agent.addins"),
            HasDirectory("tools/agents/net7.0").WithFiles(AGENT_FILES_NETCORE).AndFile("nunit.agent.addins")
        };
        TestExecutable = "tools/nunit3-console.exe";
        PackageTests = StandardRunnerTests;
    }
}

//////////////////////////////////////////////////////////////////////
// MSI PACKAGE
//////////////////////////////////////////////////////////////////////

public abstract class MsiPackageDefinition : PackageDefinition
{
    protected MsiPackageDefinition(ICakeContext context, string packageVersion) : base(context, packageVersion) { }

    public override string PackageFileName => $"{PackageId}-{PackageVersion}.msi";
    public override string InstallDirectory => PACKAGE_TEST_DIR + $"msi/{PackageId}/";
    public override string ResultDirectory => PACKAGE_RESULT_DIR + $"msi/{PackageId}/";

    protected override void doBuildPackage()
    {
        DisplayAction("Building");

        _context.MSBuild(PackageSource, new MSBuildSettings()
            .WithTarget("Rebuild")
            .SetConfiguration(Configuration)
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
    public NUnitConsoleMsiPackage(ICakeContext context, string packageVersion) : base(context, packageVersion)
    {
        PackageId = "NUnit.Console";
        PackageSource = PROJECT_DIR + "msi/nunit/nunit.wixproj";
        BasePath = NETFX_CONSOLE_DIR;
        PackageChecks = new PackageCheck[] {
            HasDirectory("NUnit.org").WithFiles("LICENSE.txt", "NOTICES.txt", "nunit.ico"),
            HasDirectory("NUnit.org/nunit-console").WithFiles(CONSOLE_FILES).AndFiles(ENGINE_FILES).AndFile("nunit.bundle.addins"),
            HasDirectory("NUnit.org/nunit-console/agents/net20").WithFiles(AGENT_FILES),
            HasDirectory("NUnit.org/nunit-console/agents/net462").WithFiles(AGENT_FILES),
            HasDirectory("NUnit.org/nunit-console/agents/netcoreapp3.1").WithFile("nunit-agent.dll"),
            HasDirectory("NUnit.org/nunit-console/agents/net5.0").WithFile("nunit-agent.dll"),
            HasDirectory("NUnit.org/nunit-console/agents/net6.0").WithFile("nunit-agent.dll"),
            HasDirectory("NUnit.org/nunit-console/agents/net7.0").WithFile("nunit-agent.dll"),
            HasDirectory("Nunit.org/nunit-console/addins").WithFiles("nunit.core.dll", "nunit.core.interfaces.dll", "nunit.v2.driver.dll", "nunit-project-loader.dll", "vs-project-loader.dll", "nunit-v2-result-writer.dll", "teamcity-event-listener.dll")
        };
        TestExecutable = "NUnit.org/nunit-console/nunit3-console.exe";
        PackageTests = StandardRunnerTests.Concat(new[] { NUnitProjectTest });
    }
}

//////////////////////////////////////////////////////////////////////
// ZIP PACKAGE
//////////////////////////////////////////////////////////////////////

public abstract class ZipPackageDefinition : PackageDefinition
{
    protected ZipPackageDefinition(ICakeContext context, string packageVersion) : base(context, packageVersion) { }

    public override string PackageFileName => $"{PackageId}-{PackageVersion}.zip";
    public override string InstallDirectory => PACKAGE_TEST_DIR + $"zip/{PackageId}/";
    public override string ResultDirectory => PACKAGE_RESULT_DIR + $"zip/{PackageId}/";

    protected abstract string ZipImageDirectory { get; }
    
    protected override void doBuildPackage()
    {
        DisplayAction("Building");

        _context.Zip(ZipImageDirectory, PackageFilePath);
    }

    protected override void doInstallPackage()
    {
        _context.Unzip(PackageFilePath, InstallDirectory);
    }
}

public class NUnitConsoleZipPackage : ZipPackageDefinition
{
    public NUnitConsoleZipPackage(ICakeContext context, string packageVersion) : base(context, packageVersion)
    {
        PackageId = "NUnit.Console";
        PackageSource = ZipImageDirectory;
        BasePath = ZipImageDirectory;
        PackageChecks = new PackageCheck[] {
            HasFiles("LICENSE.txt", "NOTICES.txt", "CHANGES.txt", "nunit.ico"),
            HasDirectory("bin").WithFiles(CONSOLE_FILES).AndFiles(ENGINE_FILES).AndFile("nunit3-console.pdb").AndFiles(ENGINE_PDB_FILES),
            HasDirectory("bin/agents/net20").WithFiles(AGENT_FILES).AndFiles(AGENT_PDB_FILES),
            HasDirectory("bin/agents/net462").WithFiles(AGENT_FILES).AndFiles(AGENT_PDB_FILES),
            HasDirectory("bin/agents/net5.0").WithFiles(AGENT_FILES_NETCORE).AndFiles(AGENT_PDB_FILES_NETCORE),
            HasDirectory("bin/agents/net6.0").WithFiles(AGENT_FILES_NETCORE).AndFiles(AGENT_PDB_FILES_NETCORE),
            HasDirectory("bin/agents/net7.0").WithFiles(AGENT_FILES_NETCORE).AndFiles(AGENT_PDB_FILES_NETCORE)
        };
        TestExecutable = "bin/nunit3-console.exe";
        PackageTests = StandardRunnerTests.Concat(new [] { NUnitProjectTest });
    }

    protected override string ZipImageDirectory => PACKAGE_DIR + "zip-image";
}
