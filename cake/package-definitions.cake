//////////////////////////////////////////////////////////////////////
// INDIVIDUAL PACKAGE DEFINITIONS
//////////////////////////////////////////////////////////////////////

PackageDefinition NUnitConsoleNuGetPackage;
PackageDefinition NUnitConsoleRunnerNuGetPackage;
PackageDefinition NUnitConsoleRunnerNet60Package;
PackageDefinition NUnitEnginePackage;
PackageDefinition NUnitEngineApiPackage;
PackageDefinition NUnitConsoleRunnerChocolateyPackage;
PackageDefinition NUnitConsoleMsiPackage;
PackageDefinition NUnitConsoleZipPackage;

public void InitializePackageDefinitions(ICakeContext context)
{
    const string DOTNET_EXE_X86 = @"C:\Program Files (x86)\dotnet\dotnet.exe";
    bool dotnetX86Available = IsRunningOnWindows() && System.IO.File.Exists(DOTNET_EXE_X86);

    // Tests run for all runner packages except NETCORE runner
    var StandardRunnerTests = new List<PackageTest>
    {
        Net35Test,
        Net35X86Test,
        Net40Test,
        Net40X86Test,
        Net35PlusNet40Test,
        NetCore21Test,
        NetCore31Test,
        Net50Test,
        Net60Test,
        Net70Test,
        NetCore21PlusNetCore31Test,
        NetCore21PlusNetCore31PlusNet50PlusNet60Test,
        Net40PlusNet60Test
    };

    if (dotnetX86Available)
    {
        StandardRunnerTests.Add(NetCore21X86Test);
        StandardRunnerTests.Add(NetCore31X86Test);
    }

    // Tests run for the NETCORE runner package
    var NetCoreRunnerTests = new List<PackageTest>
    {
        NetCore21Test,
        NetCore31Test,
        Net50Test,
        Net60Test,
        NetCore21PlusNetCore31Test,
        NetCore21PlusNetCore31PlusNet50PlusNet60Test
    };

    AllPackages.AddRange(new PackageDefinition[] {

        NUnitConsoleNuGetPackage = new NuGetPackage(
            context: context,
            id: "NUnit.Console",
            version: ProductVersion,
            source: NUGET_DIR + "runners/nunit.console-runner-with-extensions.nuspec",
            checks: new PackageCheck[] { HasFile("LICENSE.txt") }),

        NUnitConsoleRunnerNuGetPackage = new NuGetPackage(
            context: context,
            id: "NUnit.ConsoleRunner",
            version: ProductVersion,
            source: NUGET_DIR + "runners/nunit.console-runner.nuspec",
            checks: new PackageCheck[] {
                HasFiles("LICENSE.txt", "NOTICES.txt"),
                HasDirectory("tools").WithFiles(CONSOLE_FILES).AndFiles(ENGINE_FILES).AndFile("nunit.console.nuget.addins"),
                HasDirectory("tools/agents/net20").WithFiles(AGENT_FILES).AndFile("nunit.agent.addins"),
                HasDirectory("tools/agents/net462").WithFiles(AGENT_FILES).AndFile("nunit.agent.addins"),
                HasDirectory("tools/agents/netcoreapp3.1").WithFiles(AGENT_FILES_NETCORE).AndFile("nunit.agent.addins"),
                HasDirectory("tools/agents/net5.0").WithFiles(AGENT_FILES_NETCORE).AndFile("nunit.agent.addins"),
                HasDirectory("tools/agents/net6.0").WithFiles(AGENT_FILES_NETCORE).AndFile("nunit.agent.addins"),
                HasDirectory("tools/agents/net7.0").WithFiles(AGENT_FILES_NETCORE).AndFile("nunit.agent.addins")
            },
            symbols: new PackageCheck[] {
                HasDirectory("tools").WithFiles(ENGINE_PDB_FILES).AndFile("nunit3-console.pdb"),
                HasDirectory("tools/agents/net20").WithFiles(AGENT_PDB_FILES),
                HasDirectory("tools/agents/net462").WithFiles(AGENT_PDB_FILES),
                HasDirectory("tools/agents/netcoreapp3.1").WithFiles(AGENT_PDB_FILES_NETCORE),
                HasDirectory("tools/agents/net5.0").WithFiles(AGENT_PDB_FILES_NETCORE),
                HasDirectory("tools/agents/net6.0").WithFiles(AGENT_PDB_FILES_NETCORE),
                HasDirectory("tools/agents/net7.0").WithFiles(AGENT_PDB_FILES_NETCORE)
            },
            executable: "tools/nunit3-console.exe",
            tests: StandardRunnerTests),

        NUnitConsoleRunnerNet60Package = new NuGetPackage(
            context: context,
            id: "NUnit.ConsoleRunner.NetCore",
            version: ProductVersion,
            source: NUGET_DIR + "runners/nunit.console-runner.netcore.nuspec",
            checks: new PackageCheck[] {
                HasFiles("LICENSE.txt", "NOTICES.txt"),
                HasDirectory($"tools/{NETCORE_CONSOLE_TARGET}/any").WithFiles(CONSOLE_FILES_NETCORE).AndFiles(ENGINE_FILES).AndFile("nunit.console.nuget.addins")
            },
            symbols: new PackageCheck[] {
                HasDirectory($"tools/{NETCORE_CONSOLE_TARGET}/any").WithFile("nunit3-netcore-console.pdb").AndFiles(ENGINE_PDB_FILES)
            },
            executable: $"tools/{NETCORE_CONSOLE_TARGET}/any/nunit3-netcore-console.exe",
            tests: NetCoreRunnerTests),

        NUnitConsoleRunnerChocolateyPackage = new ChocolateyPackage(
            context: context,
            id: "nunit-console-runner",
            version: ProductVersion,
            source: CHOCO_DIR + "nunit-console-runner.nuspec",
            checks: new PackageCheck[] {
                HasDirectory("tools").WithFiles("LICENSE.txt", "NOTICES.txt", "VERIFICATION.txt").AndFiles(CONSOLE_FILES).AndFiles(ENGINE_FILES).AndFile("nunit.choco.addins"),
                HasDirectory("tools/agents/net20").WithFiles(AGENT_FILES).AndFile("nunit.agent.addins"),
                HasDirectory("tools/agents/net462").WithFiles(AGENT_FILES).AndFile("nunit.agent.addins"),
                HasDirectory("tools/agents/netcoreapp3.1").WithFiles(AGENT_FILES_NETCORE).AndFile("nunit.agent.addins"),
                HasDirectory("tools/agents/net5.0").WithFiles(AGENT_FILES_NETCORE).AndFile("nunit.agent.addins"),
                HasDirectory("tools/agents/net6.0").WithFiles(AGENT_FILES_NETCORE).AndFile("nunit.agent.addins"),
                HasDirectory("tools/agents/net7.0").WithFiles(AGENT_FILES_NETCORE).AndFile("nunit.agent.addins")
            },
            executable: "tools/nunit3-console.exe",
            tests: StandardRunnerTests),

        NUnitConsoleMsiPackage = new MsiPackage(
            context: context,
            id: "NUnit.Console",
            version: SemVer,
            source: MSI_DIR + "nunit/nunit.wixproj",
            checks: new PackageCheck[] {
                HasDirectory("NUnit.org").WithFiles("LICENSE.txt", "NOTICES.txt", "nunit.ico"),
                HasDirectory("NUnit.org/nunit-console").WithFiles(CONSOLE_FILES).AndFiles(ENGINE_FILES).AndFile("nunit.bundle.addins"),
                HasDirectory("NUnit.org/nunit-console/agents/net20").WithFiles("nunit-agent.exe", "nunit-agent.exe.config"),
                HasDirectory("NUnit.org/nunit-console/agents/net462").WithFiles("nunit-agent.exe", "nunit-agent.exe.config"),
                HasDirectory("NUnit.org/nunit-console/agents/netcoreapp3.1").WithFile("nunit-agent.dll"),
                HasDirectory("NUnit.org/nunit-console/agents/net5.0").WithFile("nunit-agent.dll"),
                HasDirectory("NUnit.org/nunit-console/agents/net6.0").WithFile("nunit-agent.dll"),
                //HasDirectory("NUnit.org/nunit-console/agents/net7.0").WithFile("nunit-agent.dll"),
                HasDirectory("Nunit.org/nunit-console/addins").WithFiles("nunit.core.dll", "nunit.core.interfaces.dll", "nunit.v2.driver.dll", "nunit-project-loader.dll", "vs-project-loader.dll", "nunit-v2-result-writer.dll", "teamcity-event-listener.dll")
            },
            executable: "NUnit.org/nunit-console/nunit3-console.exe",
            tests: StandardRunnerTests.Concat(new[] { NUnitProjectTest })),

        NUnitConsoleZipPackage = new ZipPackage(
            context: context,
            id: "NUnit.Console",
            version: ProductVersion,
            source: ZIP_IMG_DIR,
            checks: new PackageCheck[] {
                HasFiles("LICENSE.txt", "NOTICES.txt", "CHANGES.txt"),
                HasDirectory($"bin/{NETFX_CONSOLE_TARGET}").WithFiles(CONSOLE_FILES).AndFiles(ENGINE_FILES).AndFile("nunit3-console.pdb").AndFiles(ENGINE_PDB_FILES),
                HasDirectory("bin/netstandard2.0").WithFiles(ENGINE_FILES).AndFiles(ENGINE_PDB_FILES),
                HasDirectory("bin/netcoreapp2.1").WithFiles(ENGINE_FILES).AndFiles(ENGINE_PDB_FILES),
                HasDirectory("bin/netcoreapp3.1").WithFiles(ENGINE_FILES).AndFiles(ENGINE_PDB_FILES),
                //HasDirectory("bin/net5.0").WithFiles(ENGINE_FILES).AndFiles(ENGINE_PDB_FILES),
                HasDirectory("bin/agents/net20").WithFiles(AGENT_FILES).AndFiles(AGENT_PDB_FILES),
                HasDirectory("bin/agents/net462").WithFiles(AGENT_FILES).AndFiles(AGENT_PDB_FILES),
                HasDirectory("bin/agents/net5.0").WithFiles(AGENT_FILES_NETCORE).AndFiles(AGENT_PDB_FILES_NETCORE),
                HasDirectory("bin/agents/net6.0").WithFiles(AGENT_FILES_NETCORE).AndFiles(AGENT_PDB_FILES_NETCORE)
            },
            executable: $"bin/{NETFX_CONSOLE_TARGET}/nunit3-console.exe",
            tests: StandardRunnerTests.Concat(new[] { NUnitProjectTest })),

        // NOTE: Packages below this point have no direct tests

        NUnitEnginePackage = new NuGetPackage(
            context: context,
            id: "NUnit.Engine",
            version: ProductVersion,
            source: NUGET_DIR + "engine/nunit.engine.nuspec",
            checks: new PackageCheck[] {
                HasFiles("LICENSE.txt", "NOTICES.txt"),
                HasDirectory("lib/net462").WithFiles(ENGINE_FILES),
                HasDirectory("lib/netstandard2.0").WithFiles(ENGINE_FILES),
                HasDirectory("lib/netcoreapp3.1").WithFiles(ENGINE_FILES),
                HasDirectory("contentFiles/any/lib/net462").WithFile("nunit.engine.nuget.addins"),
                HasDirectory("contentFiles/any/lib/netstandard2.0").WithFile("nunit.engine.nuget.addins"),
                HasDirectory("contentFiles/any/lib/netcoreapp3.1").WithFile("nunit.engine.nuget.addins"),
                HasDirectory("contentFiles/any/agents/net20").WithFiles(AGENT_FILES).AndFile("nunit.agent.addins"),
                HasDirectory("contentFiles/any/agents/net462").WithFiles(AGENT_FILES).AndFile("nunit.agent.addins")
            },
            symbols: new PackageCheck[] {
                HasDirectory("lib/net462").WithFiles(ENGINE_PDB_FILES),
                HasDirectory("lib/netstandard2.0").WithFiles(ENGINE_PDB_FILES),
                HasDirectory("lib/netcoreapp3.1").WithFiles(ENGINE_PDB_FILES),
                HasDirectory("contentFiles/any/agents/net20").WithFiles(AGENT_PDB_FILES),
                HasDirectory("contentFiles/any/agents/net462").WithFiles(AGENT_PDB_FILES)
            }),

        NUnitEngineApiPackage = new NuGetPackage(
            context: context,
            id: "NUnit.Engine.Api",
            version: ProductVersion,
            source: NUGET_DIR + "engine/nunit.engine.api.nuspec",
            checks: new PackageCheck[] {
                HasFile("LICENSE.txt"),
                HasDirectory("lib/net20").WithFile("nunit.engine.api.dll"),
                HasDirectory("lib/netstandard2.0").WithFile("nunit.engine.api.dll"),
            },
            symbols: new PackageCheck[] {
                HasDirectory("lib/net20").WithFile("nunit.engine.api.pdb"),
                HasDirectory("lib/netstandard2.0").WithFile("nunit.engine.api.pdb")
            })
    });
}

//////////////////////////////////////////////////////////////////////
// LIST OF ALL PACKAGES
//////////////////////////////////////////////////////////////////////

var AllPackages = new List<PackageDefinition>();

//////////////////////////////////////////////////////////////////////
// PACKAGE DEFINITION IMPLEMENTATION
//////////////////////////////////////////////////////////////////////

public enum PackageType
{
    NuGet,
    Chocolatey,
    Msi,
    Zip
}

/// <summary>
/// 
/// </summary>
public abstract class PackageDefinition
{
    protected ICakeContext _context;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="packageType">A PackageType value specifying one of the four known package types</param>
    /// <param name="id">A string containing the package ID, used as the root of the PackageName</param>
    /// <param name="version">A string representing the package version, used as part of the PackageName</param>
    /// <param name="source">A string representing the source used to create the package, e.g. a nuspec file</param>
    /// <param name="executable">A string containing the path to the executable used in running tests. If relative, the path is contained within the package itself.</param>
    /// <param name="checks">An array of PackageChecks be made on the content of the package. Optional.</param>
    /// <param name="symbols">An array of PackageChecks to be made on the symbol package, if one is created. Optional. Only supported for nuget packages.</param>
    /// <param name="tests">An array of PackageTests to be run against the package. Optional.</param>
	protected PackageDefinition(
        ICakeContext context,
        PackageType packageType,
        string id,
        string version,
        string source,
        string executable = null,
        PackageCheck[] checks = null,
        PackageCheck[] symbols = null,
        IEnumerable<PackageTest> tests = null)
    {
        if (executable == null && tests != null)
            throw new System.ArgumentException($"Unable to create {packageType} package {id}: Executable must be provided if there are tests", nameof(executable));

        _context = context;

        PackageType = packageType;
        PackageId = id;
        PackageVersion = version;
        PackageSource = source;
        TestExecutable = executable;
        PackageChecks = checks;
        PackageTests = tests;
        SymbolChecks = symbols;
    }

    public PackageType PackageType { get; }
	public string PackageId { get; }
    public string PackageVersion { get; }
	public string PackageSource { get; }
    public string TestExecutable { get; }
    public PackageCheck[] PackageChecks { get; }
    public PackageCheck[] SymbolChecks { get; protected set; }
    public IEnumerable<PackageTest> PackageTests { get; }

    public abstract string PackageName { get; }
    public abstract void BuildPackage();

    public bool HasSymbols { get; protected set; } = false;
    public virtual string SymbolPackageName => throw new System.NotImplementedException($"Symbols are not available for {PackageType} packages.");
}

// Users may only instantiate the derived classes, which avoids
// exposing PackageType and makes it impossible to create a
// PackageDefinition with an unknown package type.
public class NuGetPackage : PackageDefinition
{
    public NuGetPackage(ICakeContext context, string id, string version, string source, string executable = null,
        PackageCheck[] checks = null, PackageCheck[] symbols = null, IEnumerable<PackageTest> tests = null)
        : base(context, PackageType.NuGet, id, version, source, executable: executable, checks: checks, symbols: symbols, tests: tests)
    {
        if (symbols != null)
        {
            HasSymbols = true;
            SymbolChecks = symbols;
        }
    }

    public override string PackageName => $"{PackageId}.{PackageVersion}.nupkg";
    public override string SymbolPackageName => System.IO.Path.ChangeExtension(PackageName, ".snupkg");

    public override void BuildPackage()
    {
        var nugetPackSettings = new NuGetPackSettings()
        {
            Version = PackageVersion,
            BasePath = BIN_DIR,
            OutputDirectory = PACKAGE_DIR,
            NoPackageAnalysis = true,
            Symbols = HasSymbols
        };

        if (HasSymbols)
            nugetPackSettings.SymbolPackageFormat = "snupkg";

        _context.NuGetPack(PackageSource, nugetPackSettings);
    }
}

public class ChocolateyPackage : PackageDefinition
{
    public ChocolateyPackage(ICakeContext context, string id, string version, string source, string executable = null, 
        PackageCheck[] checks = null, IEnumerable<PackageTest> tests = null)
        : base(context, PackageType.Chocolatey, id, version, source, executable: executable, checks: checks, tests: tests) { }

    public override string PackageName => $"{PackageId}.{PackageVersion}.nupkg";
    
    public override void BuildPackage()
    {
        _context.ChocolateyPack(PackageSource,
            new ChocolateyPackSettings()
            {
                Version = PackageVersion,
                OutputDirectory = PACKAGE_DIR,
                ArgumentCustomization = args => args.Append($"BIN_DIR={BIN_DIR}")
            });
    }
}

public class MsiPackage : PackageDefinition
{
    public MsiPackage(ICakeContext context, string id, string version, string source, string executable = null,
        PackageCheck[] checks = null, IEnumerable<PackageTest> tests = null)
        : base(context, PackageType.Msi, id, version, source, executable: executable, checks: checks, tests: tests) { }

    public override string PackageName => $"{PackageId}-{PackageVersion}.msi";

    public override void BuildPackage()
    {
        _context.MSBuild(PackageSource, new MSBuildSettings()
            .WithTarget("Rebuild")
            .SetConfiguration(Configuration)
            .WithProperty("Version", PackageVersion)
            .WithProperty("DisplayVersion", PackageVersion)
            .WithProperty("OutDir", PACKAGE_DIR)
            .WithProperty("Image", MSI_IMG_DIR)
            .SetMSBuildPlatform(MSBuildPlatform.x86)
            .SetNodeReuse(false));
    }
}

public class ZipPackage : PackageDefinition
{
    public ZipPackage(ICakeContext context, string id, string version, string source, string executable = null,
        PackageCheck[] checks = null, IEnumerable<PackageTest> tests = null)
        : base(context, PackageType.Zip, id, version, source, executable: executable, checks: checks, tests: tests) { }

    public override string PackageName => $"{PackageId}-{PackageVersion}.zip";
    
    public override void BuildPackage()
    {
        _context.Zip(ZIP_IMG_DIR, $"{PACKAGE_DIR}{PackageName}");
    }
}
