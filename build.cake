// Load scripts
#load cake/*.cake

// Initialize BuildSettings
BuildSettings.Initialize(
    Context,
    "NUnit Console and Engine",
    "nunit-console",
    solutionFile: "NUnitConsole.sln",
    exemptFiles: new [] { "Options.cs", "ProcessUtils.cs", "ProcessUtilsTests.cs" },
    unitTests: "**/*.tests.exe|**/nunit3-console.tests.dll",
    unitTestRunner: new CustomTestRunner() );

//////////////////////////////////////////////////////////////////////
// PACKAGE TEST LISTS
//////////////////////////////////////////////////////////////////////

    // Tests run for all runner packages except NETCORE runner
    var StandardRunnerTests = new List<PackageTest>
    {
        Net35Test,
        Net35X86Test,
        Net40Test,
        Net40X86Test,
        Net35PlusNet40Test,
        NetCore31Test,
        Net50Test,
        Net60Test,
        Net70Test,
        Net80Test,
        Net50PlusNet60Test,
        Net40PlusNet60Test,
        NUnitProjectTest
    };

    // Tests run for the NETCORE runner package
    var NetCoreRunnerTests = new List<PackageTest>
    {
        NetCore31Test,
        Net50Test,
        Net60Test,
        Net70Test,
        Net80Test,
    };

    const string DOTNET_EXE_X86 = @"C:\Program Files (x86)\dotnet\dotnet.exe";
    bool dotnetX86Available = IsRunningOnWindows() && System.IO.File.Exists(DOTNET_EXE_X86);

    // TODO: Remove the limitation to Windows
    if (IsRunningOnWindows() && dotnetX86Available)
    {
        StandardRunnerTests.Add(Net60X86Test);
        // TODO: Make these tests run on AppVeyor
        if (!BuildSystem.IsRunningOnAppVeyor)
        {
            StandardRunnerTests.Add(NetCore31X86Test);
            StandardRunnerTests.Add(Net50X86Test);
            StandardRunnerTests.Add(Net70X86Test);
            StandardRunnerTests.Add(Net80X86Test);
        }
        // Currently, NetCoreRunner runs tests in process. As a result,
        // X86 tests will work in our environment, although uses may run
        // it as a tool using the X86 architecture.
    }

//////////////////////////////////////////////////////////////////////
// INDIVIDUAL PACKAGE TEST DEFINITIONS
//////////////////////////////////////////////////////////////////////

static ExpectedResult MockAssemblyExpectedResult(int nCopies = 1) => new ExpectedResult("Failed")
{
    Total = 37 * nCopies,
    Passed = 23 * nCopies,
    Failed = 5 * nCopies,
    Warnings = 1 * nCopies,
    Inconclusive = 1 * nCopies,
    Skipped = 7 * nCopies
};

static ExpectedResult MockAssemblyExpectedResult(params string[] runtimes)
{
    int nCopies = runtimes.Length;
    var result = MockAssemblyExpectedResult(nCopies);
    result.Assemblies = new ExpectedAssemblyResult[nCopies];
    for (int i = 0; i < nCopies; i++)
        result.Assemblies[i] = new ExpectedAssemblyResult("mock-assembly.dll", runtimes[i]);
    return result;
}

static ExpectedResult MockAssemblyX86ExpectedResult(params string[] runtimes)
{
    int nCopies = runtimes.Length;
    var result = MockAssemblyExpectedResult(nCopies);
    result.Assemblies = new ExpectedAssemblyResult[nCopies];
    for (int i = 0; i < nCopies; i++)
        result.Assemblies[i] = new ExpectedAssemblyResult("mock-assembly-x86.dll", runtimes[i]);
    return result;
}

static PackageTest Net35Test = new PackageTest(
    1, "Net35Test",
    "Run mock-assembly.dll under .NET 3.5",
    "net35/mock-assembly.dll",
    MockAssemblyExpectedResult("net-2.0"));

static PackageTest Net35X86Test = new PackageTest(
    1, "Net35X86Test",
    "Run mock-assembly-x86.dll under .NET 3.5",
    "net35/mock-assembly-x86.dll",
    MockAssemblyX86ExpectedResult("net-2.0"));

static PackageTest Net40Test = new PackageTest(
    1, "Net40Test",
    "Run mock-assembly.dll under .NET 4.x",
    "net40/mock-assembly.dll",
    MockAssemblyExpectedResult("net-4.0"));

static PackageTest Net40X86Test = new PackageTest(
    1, "Net40X86Test",
    "Run mock-assembly-x86.dll under .NET 4.x",
    "net40/mock-assembly-x86.dll",
    MockAssemblyX86ExpectedResult("net-4.0"));

static PackageTest Net35PlusNet40Test = new PackageTest(
    1, "Net35PlusNet40Test",
    "Run both copies of mock-assembly together",
    "net35/mock-assembly.dll net40/mock-assembly.dll",
    MockAssemblyExpectedResult("net-2.0", "net-4.0"));

static PackageTest Net80Test = new PackageTest(
    1, "Net80Test",
    "Run mock-assembly.dll under .NET 8.0",
    "net8.0/mock-assembly.dll",
    MockAssemblyExpectedResult("netcore-8.0"));

static PackageTest Net80X86Test = new PackageTest(
    1, "Net80X86Test",
    "Run mock-assembly-x86.dll under .NET 8.0",
    "net8.0/mock-assembly-x86.dll",
    MockAssemblyX86ExpectedResult("netcore-8.0"));

static PackageTest Net70Test = new PackageTest(
    1, "Net70Test",
    "Run mock-assembly.dll under .NET 7.0",
    "net7.0/mock-assembly.dll",
    MockAssemblyExpectedResult("netcore-7.0"));

static PackageTest Net70X86Test = new PackageTest(
    1, "Net70X86Test",
    "Run mock-assembly-x86.dll under .NET 7.0",
    "net7.0/mock-assembly-x86.dll",
    MockAssemblyX86ExpectedResult("netcore-7.0"));

static PackageTest Net60Test = new PackageTest(
    1, "Net60Test",
    "Run mock-assembly.dll under .NET 6.0",
    "net6.0/mock-assembly.dll",
    MockAssemblyExpectedResult("netcore-6.0"));

static PackageTest Net60X86Test = new PackageTest(
    1, "Net60X86Test",
    "Run mock-assembly-x86.dll under .NET 6.0",
    "net6.0/mock-assembly-x86.dll",
    MockAssemblyX86ExpectedResult("netcore-6.0"));

static PackageTest Net50Test = new PackageTest(
    1, "Net50Test",
    "Run mock-assembly.dll under .NET 5.0",
    "net5.0/mock-assembly.dll",
    MockAssemblyExpectedResult("netcore-5.0"));

static PackageTest Net50X86Test = new PackageTest(
    1, "Net50X86Test",
    "Run mock-assembly-x86.dll under .NET 5.0",
    "net5.0/mock-assembly-x86.dll",
    MockAssemblyX86ExpectedResult("netcore-5.0"));

static PackageTest NetCore31Test = new PackageTest(
    1, "NetCore31Test",
    "Run mock-assembly.dll under .NET Core 3.1",
    "netcoreapp3.1/mock-assembly.dll",
    MockAssemblyExpectedResult("netcore-3.1"));

static PackageTest NetCore31X86Test = new PackageTest(
    1, "NetCore31X86Test",
    "Run mock-assembly-x86.dll under .NET Core 3.1",
    "netcoreapp3.1/mock-assembly-x86.dll",
    MockAssemblyX86ExpectedResult("netcore-3.1"));

static PackageTest Net50PlusNet60Test = new PackageTest(
    1, "Net50PlusNet60Test",
    "Run mock-assembly under .NET 5.0 and 6.0 together",
    "net5.0/mock-assembly.dll net6.0/mock-assembly.dll",//" net7.0/mock-assembly.dll net8.0/mock-assembly.dll",
    MockAssemblyExpectedResult("netcore-5.0", "netcore-6.0"));

static PackageTest Net40PlusNet60Test = new PackageTest(
    1, "Net40PlusNet60Test",
    "Run mock-assembly under .Net Framework 4.0 and .Net 6.0 together",
    "net40/mock-assembly.dll net6.0/mock-assembly.dll",
    MockAssemblyExpectedResult("net-4.0", "netcore-6.0"));

static PackageTest NUnitProjectTest = new PackageTest(
    1, "NUnitProjectTest",
    "Run project with both copies of mock-assembly",
    "../../NetFXTests.nunit --config=Release --trace=Debug",
    MockAssemblyExpectedResult("net-2.0", "net-4.0"),
    KnownExtensions.NUnitProjectLoader);

//////////////////////////////////////////////////////////////////////
// LISTS OF FILES USED IN CHECKING PACKAGES
//////////////////////////////////////////////////////////////////////

FilePath[] ENGINE_FILES = {
        "nunit.engine.dll", "nunit.engine.core.dll", "nunit.engine.api.dll", "testcentric.engine.metadata.dll" };
FilePath[] ENGINE_PDB_FILES = {
        "nunit.engine.pdb", "nunit.engine.core.pdb", "nunit.engine.api.pdb"};
FilePath[] ENGINE_CORE_FILES = {
        "nunit.engine.core.dll", "nunit.engine.api.dll", "testcentric.engine.metadata.dll" };
FilePath[] ENGINE_CORE_PDB_FILES = {
        "nunit.engine.core.pdb", "nunit.engine.api.pdb"};
FilePath[] AGENT_FILES = {
        "nunit-agent.exe", "nunit-agent.exe.config",
        "nunit-agent-x86.exe", "nunit-agent-x86.exe.config",
        "nunit.engine.core.dll", "nunit.engine.api.dll", "testcentric.engine.metadata.dll"};
FilePath[] AGENT_FILES_NETCORE = {
        "nunit-agent.dll", "nunit-agent.dll.config",
        "nunit.engine.core.dll", "nunit.engine.api.dll", "testcentric.engine.metadata.dll",
        "Microsoft.Extensions.DependencyModel.dll"};
FilePath[] AGENT_PDB_FILES = {
        "nunit-agent.pdb", "nunit-agent-x86.pdb", "nunit.engine.core.pdb", "nunit.engine.api.pdb"};
FilePath[] AGENT_PDB_FILES_NETCORE = {
        "nunit-agent.pdb", "nunit.engine.core.pdb", "nunit.engine.api.pdb"};
FilePath[] CONSOLE_FILES = {
        "nunit3-console.exe", "nunit3-console.exe.config" };
FilePath[] CONSOLE_FILES_NETCORE = {
        "nunit3-console.exe", "nunit3-console.dll" };

//////////////////////////////////////////////////////////////////////
// INDIVIDUAL PACKAGE DEFINITIONS
//////////////////////////////////////////////////////////////////////

PackageDefinition NUnitConsoleNuGetPackage;
PackageDefinition NUnitConsoleRunnerNuGetPackage;
PackageDefinition NUnitConsoleRunnerNet60Package;
PackageDefinition NUnitConsoleRunnerNet80Package;
PackageDefinition NUnitEnginePackage;
PackageDefinition NUnitEngineApiPackage;
PackageDefinition NUnitConsoleRunnerChocolateyPackage;
PackageDefinition NUnitConsoleZipPackage;

BuildSettings.Packages.AddRange(new PackageDefinition[] {

    NUnitConsoleRunnerNuGetPackage = new NuGetPackage(
        id: "NUnit.ConsoleRunner",
        source: BuildSettings.NuGetDirectory + "runners/nunit.console-runner.nuspec",
        checks: new PackageCheck[] {
            HasFiles("LICENSE.txt", "NOTICES.txt"),
            HasDirectory("tools").WithFiles(CONSOLE_FILES).AndFiles(ENGINE_FILES).AndFile("nunit.console.nuget.addins"),
            HasDirectory("tools/agents/net20").WithFiles(AGENT_FILES).AndFile("nunit.console.nuget.agent.addins"),
            HasDirectory("tools/agents/net40").WithFiles(AGENT_FILES).AndFile("nunit.console.nuget.agent.addins"),
            HasDirectory("tools/agents/netcoreapp3.1").WithFiles(AGENT_FILES_NETCORE).AndFile("nunit.console.nuget.agent.addins"),
            HasDirectory("tools/agents/net5.0").WithFiles(AGENT_FILES_NETCORE).AndFile("nunit.console.nuget.agent.addins"),
            HasDirectory("tools/agents/net6.0").WithFiles(AGENT_FILES_NETCORE).AndFile("nunit.console.nuget.agent.addins"),
            HasDirectory("tools/agents/net7.0").WithFiles(AGENT_FILES_NETCORE).AndFile("nunit.console.nuget.agent.addins"),
            HasDirectory("tools/agents/net8.0").WithFiles(AGENT_FILES_NETCORE).AndFile("nunit.console.nuget.agent.addins")
        },
        symbols: new PackageCheck[] {
            HasDirectory("tools").WithFiles(ENGINE_PDB_FILES).AndFile("nunit3-console.pdb"),
            HasDirectory("tools/agents/net20").WithFiles(AGENT_PDB_FILES),
            HasDirectory("tools/agents/net40").WithFiles(AGENT_PDB_FILES),
            HasDirectory("tools/agents/netcoreapp3.1").WithFiles(AGENT_PDB_FILES_NETCORE),
            HasDirectory("tools/agents/net5.0").WithFiles(AGENT_PDB_FILES_NETCORE),
            HasDirectory("tools/agents/net6.0").WithFiles(AGENT_PDB_FILES_NETCORE),
            HasDirectory("tools/agents/net7.0").WithFiles(AGENT_PDB_FILES_NETCORE),
            HasDirectory("tools/agents/net8.0").WithFiles(AGENT_PDB_FILES_NETCORE)
        },
        testRunner: new ConsoleRunnerSelfTester(BuildSettings.NuGetTestDirectory 
            + $"NUnit.ConsoleRunner.{BuildSettings.PackageVersion}/tools/nunit3-console.exe"),
        tests: StandardRunnerTests),

    // NOTE: Must follow ConsoleRunner, upon which it depends
    NUnitConsoleNuGetPackage = new NuGetPackage(
        id: "NUnit.Console",
        source: BuildSettings.NuGetDirectory + "runners/nunit.console-runner-with-extensions.nuspec",
        checks: new PackageCheck[] { HasFile("LICENSE.txt") }),

    NUnitConsoleRunnerNet80Package = new NuGetPackage(
        id: "NUnit.ConsoleRunner.NetCore",
        source: BuildSettings.NuGetDirectory + "runners/nunit.console-runner.netcore.nuspec",
        checks: new PackageCheck[] {
            HasFiles("LICENSE.txt", "NOTICES.txt"),
            HasDirectory("tools/net8.0").WithFiles(CONSOLE_FILES_NETCORE).AndFiles(ENGINE_CORE_FILES).AndFile("nunit.console.nuget.addins")
        },
        symbols: new PackageCheck[] {
            HasDirectory("tools/net8.0").WithFile("nunit3-console.pdb").AndFiles(ENGINE_PDB_FILES)
        },
        testRunner: new ConsoleRunnerSelfTester(BuildSettings.NuGetTestDirectory 
            + $"NUnit.ConsoleRunner.NetCore.{BuildSettings.PackageVersion}/tools/net8.0/nunit3-console.exe"),
        tests: NetCoreRunnerTests),

    NUnitConsoleRunnerNet60Package = new NuGetPackage(
        id: "NUnit.ConsoleRunner.NetCore",
        source: BuildSettings.NuGetDirectory + "runners/nunit.console-runner.netcore.nuspec",
        checks: new PackageCheck[] {
            HasFiles("LICENSE.txt", "NOTICES.txt"),
            HasDirectory("tools/net6.0").WithFiles(CONSOLE_FILES_NETCORE).AndFiles(ENGINE_CORE_FILES).AndFile("nunit.console.nuget.addins")
        },
        symbols: new PackageCheck[] {
            HasDirectory("tools/net6.0").WithFile("nunit3-console.pdb").AndFiles(ENGINE_PDB_FILES)
        },
        testRunner: new ConsoleRunnerSelfTester(BuildSettings.NuGetTestDirectory 
            + $"NUnit.ConsoleRunner.NetCore.{BuildSettings.PackageVersion}/tools/net6.0/nunit3-console.exe"),
        tests: NetCoreRunnerTests),

    NUnitConsoleRunnerChocolateyPackage = new ChocolateyPackage(
        id: "nunit-console-runner",
        source: BuildSettings.ChocolateyDirectory + "nunit-console-runner.nuspec",
        checks: new PackageCheck[] {
            HasDirectory("tools").WithFiles("LICENSE.txt", "NOTICES.txt", "VERIFICATION.txt").AndFiles(CONSOLE_FILES).AndFiles(ENGINE_FILES).AndFile("nunit.console.choco.addins"),
            HasDirectory("tools/agents/net20").WithFiles(AGENT_FILES).AndFile("nunit.console.choco.agent.addins"),
            HasDirectory("tools/agents/net40").WithFiles(AGENT_FILES).AndFile("nunit.console.choco.agent.addins"),
            HasDirectory("tools/agents/netcoreapp3.1").WithFiles(AGENT_FILES_NETCORE).AndFile("nunit.console.choco.agent.addins"),
            HasDirectory("tools/agents/net5.0").WithFiles(AGENT_FILES_NETCORE).AndFile("nunit.console.choco.agent.addins"),
            HasDirectory("tools/agents/net6.0").WithFiles(AGENT_FILES_NETCORE).AndFile("nunit.console.choco.agent.addins"),
            HasDirectory("tools/agents/net7.0").WithFiles(AGENT_FILES_NETCORE).AndFile("nunit.console.choco.agent.addins"),
            HasDirectory("tools/agents/net8.0").WithFiles(AGENT_FILES_NETCORE).AndFile("nunit.console.choco.agent.addins")
        },
        testRunner: new ConsoleRunnerSelfTester(BuildSettings.ChocolateyTestDirectory 
            + $"nunit-console-runner.{BuildSettings.PackageVersion}/tools/nunit3-console.exe"),
        tests: StandardRunnerTests),

    NUnitConsoleZipPackage = new ZipPackage(
        id: "NUnit.Console",
        source: BuildSettings.ZipImageDirectory,
        checks: new PackageCheck[] {
            HasFiles("LICENSE.txt", "NOTICES.txt", "CHANGES.txt"),
            HasDirectory("bin/net20").WithFiles(CONSOLE_FILES).AndFiles(ENGINE_FILES).AndFile("nunit3-console.pdb").AndFiles(ENGINE_PDB_FILES),
            HasDirectory("bin/net35").WithFiles(CONSOLE_FILES).AndFiles(ENGINE_FILES).AndFile("nunit3-console.pdb").AndFiles(ENGINE_PDB_FILES),
            HasDirectory("bin/netstandard2.0").WithFiles(ENGINE_FILES).AndFiles(ENGINE_PDB_FILES),
            HasDirectory("bin/netcoreapp3.1").WithFiles(ENGINE_CORE_FILES).AndFiles(ENGINE_CORE_PDB_FILES),
            //HasDirectory("bin/net5.0").WithFiles(ENGINE_FILES).AndFiles(ENGINE_PDB_FILES),
            HasDirectory("bin/agents/net20").WithFiles(AGENT_FILES).AndFiles(AGENT_PDB_FILES),
            HasDirectory("bin/agents/net40").WithFiles(AGENT_FILES).AndFiles(AGENT_PDB_FILES),
            HasDirectory("bin/agents/net5.0").WithFiles(AGENT_FILES_NETCORE).AndFiles(AGENT_PDB_FILES_NETCORE),
            HasDirectory("bin/agents/net6.0").WithFiles(AGENT_FILES_NETCORE).AndFiles(AGENT_PDB_FILES_NETCORE),
            HasDirectory("bin/agents/net7.0").WithFiles(AGENT_FILES_NETCORE).AndFiles(AGENT_PDB_FILES_NETCORE),
            HasDirectory("bin/agents/net8.0").WithFiles(AGENT_FILES_NETCORE).AndFiles(AGENT_PDB_FILES_NETCORE)
        },
        testRunner: new ConsoleRunnerSelfTester(BuildSettings.ZipTestDirectory 
            + $"NUnit.Console.{BuildSettings.PackageVersion}/bin/net20/nunit3-console.exe"),
        tests: StandardRunnerTests,
        bundledExtensions: new [] {
            new PackageReference("NUnit.Extension.VSProjectLoader", "3.9.0"),
            new PackageReference("NUnit.Extension.NUnitProjectLoader", "3.7.1"),
            new PackageReference("NUnit.Extension.NUnitV2Driver", "3.9.0"),
            new PackageReference("NUnit.Extension.NUnitV2ResultWriter", "3.7.0"),
            new PackageReference("NUnit.Extension.TeamCityEventListener", "1.0.9")
        }),

    // NOTE: Packages below this point have no direct tests

    NUnitEnginePackage = new NuGetPackage(
        id: "NUnit.Engine",
        source: BuildSettings.NuGetDirectory + "engine/nunit.engine.nuspec",
        checks: new PackageCheck[] {
            HasFiles("LICENSE.txt", "NOTICES.txt"),
            HasDirectory("lib/net20").WithFiles(ENGINE_FILES),
            HasDirectory("lib/netstandard2.0").WithFiles(ENGINE_FILES),
            HasDirectory("contentFiles/any/lib/net20").WithFile("nunit.engine.nuget.addins"),
            HasDirectory("contentFiles/any/lib/netstandard2.0").WithFile("nunit.engine.nuget.addins"),
            HasDirectory("contentFiles/any/agents/net20").WithFiles(AGENT_FILES).AndFile("nunit.agent.addins"),
            HasDirectory("contentFiles/any/agents/net40").WithFiles(AGENT_FILES).AndFile("nunit.agent.addins")
        },
        symbols: new PackageCheck[] {
            HasDirectory("lib/net20").WithFiles(ENGINE_PDB_FILES),
            HasDirectory("lib/netstandard2.0").WithFiles(ENGINE_PDB_FILES),
            HasDirectory("contentFiles/any/agents/net20").WithFiles(AGENT_PDB_FILES),
            HasDirectory("contentFiles/any/agents/net40").WithFiles(AGENT_PDB_FILES)
        }),

    NUnitEngineApiPackage = new NuGetPackage(
        id: "NUnit.Engine.Api",
        source: BuildSettings.NuGetDirectory + "engine/nunit.engine.api.nuspec",
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

//////////////////////////////////////////////////////////////////////
// TEST RUNNERS
//////////////////////////////////////////////////////////////////////

// Custom unit test runner to run console vs engine tests differently
// TODO: Use NUnitLite for all tests?
public class CustomTestRunner : UnitTestRunner
{
    public override int Run(FilePath testPath)
    {
        // Run console tests under the just-built console
        if(testPath.ToString().Contains("nunit3-console.tests.dll"))
        {
            return BuildSettings.Context.StartProcess(
                BuildSettings.OutputDirectory + "net20/nunit3-console.exe",
                $"\"{testPath}\" {BuildSettings.UnitTestArguments}" );
        }

        // All other tests use NUnitLite
        return new NUnitLiteRunner().Run(testPath);
    }
}

// Use the console runner we just built to run package tests
public class ConsoleRunnerSelfTester : PackageTestRunner
{
	public ConsoleRunnerSelfTester(string executablePath)
	{
		ExecutablePath = executablePath;
	}

	public override int Run(string arguments)
	{
		return base.Run(arguments);
	}
}

//////////////////////////////////////////////////////////////////////
// EXECUTION
//////////////////////////////////////////////////////////////////////

Build.Run()
