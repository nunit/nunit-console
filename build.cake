// Load the recipe 
#load nuget:?package=NUnit.Cake.Recipe&version=1.0.1-dev00001
// Comment out above line and uncomment below for local tests of recipe changes
//#load ../NUnit.Cake.Recipe/recipe/*.cake

// Initialize BuildSettings
BuildSettings.Initialize(
    Context,
    title: "NUnit Console and Engine",
    githubRepository: "nunit-console",
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
        Net462Test,
        Net462X86Test,
        Net462PlusNet462Test,
        NetCore31Test,
        Net60Test,
        Net70Test,
        Net80Test,
        Net60PlusNet80Test,
        Net462PlusNet60Test,
        NUnitProjectTest,
        V2ResultWriterTest
    };

    // Tests run for the NETCORE runner package
    var NetCoreRunnerTests = new List<PackageTest>
    {
        NetCore31Test,
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


static PackageTest Net462Test = new PackageTest(
    1, "Net462Test",
    "Run mock-assembly.dll under .NET 4.6.2",
    "net462/mock-assembly.dll",
    MockAssemblyExpectedResult("net-4.6.2"));

static PackageTest Net462X86Test = new PackageTest(
    1, "Net462X86Test",
    "Run mock-assembly-x86.dll under .NET 4.6.2",
    "net462/mock-assembly-x86.dll",
    MockAssemblyX86ExpectedResult("net-4.6.2"));

static PackageTest Net462PlusNet462Test = new PackageTest(
    1, "Net462PlusNet462Test",
    "Run two copies of mock-assembly together",
    "net462/mock-assembly.dll net462/mock-assembly.dll",
    MockAssemblyExpectedResult("net-4.6.2", "net-4.6.2"));

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

static PackageTest Net60PlusNet80Test = new PackageTest(
    1, "Net60PlusNet80Test",
    "Run mock-assembly under .NET6.0 and 8.0 together",
    "net6.0/mock-assembly.dll net8.0/mock-assembly.dll",
    MockAssemblyExpectedResult("netcore-6.0", "netcore-8.0"));

static PackageTest Net462PlusNet60Test = new PackageTest(
    1, "Net462PlusNet60Test",
    "Run mock-assembly under .Net Framework 4.6.2 and .Net 6.0 together",
    "net462/mock-assembly.dll net6.0/mock-assembly.dll",
    MockAssemblyExpectedResult("net-4.6.2", "netcore-6.0"));

// Test with latest released version of each of our extensions

static ExtensionSpecifier NUnitProjectLoader = KnownExtensions.NUnitProjectLoader.SetVersion("3.8.0");
static ExtensionSpecifier NUnitV2ResultWriter = KnownExtensions.NUnitV2ResultWriter.SetVersion("3.8.0");

static PackageTest NUnitProjectTest = new PackageTest(
    1, "NUnitProjectTest",
    "Run project with both copies of mock-assembly",
    "../../NetFXTests.nunit --config=Release --trace=Debug",
    MockAssemblyExpectedResult("net-4.6.2", "netcore-6.0"),
    NUnitProjectLoader);

static PackageTest V2ResultWriterTest = new PackageTest(
    1, "V2ResultWriterTest",
    "Run mock-assembly under .NET 6.0 and produce V2 output",
    "net6.0/mock-assembly.dll --result=TestResult.xml --result=NUnit2TestResult.xml;format=nunit2",
    MockAssemblyExpectedResult("netcore-6.0"),
    NUnitV2ResultWriter);

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
            HasDirectory("tools/agents/net462").WithFiles(AGENT_FILES).AndFile("nunit.console.nuget.agent.addins"),
            HasDirectory("tools/agents/netcoreapp3.1").WithFiles(AGENT_FILES_NETCORE).AndFile("nunit.console.nuget.agent.addins"),
            HasDirectory("tools/agents/net6.0").WithFiles(AGENT_FILES_NETCORE).AndFile("nunit.console.nuget.agent.addins"),
            HasDirectory("tools/agents/net7.0").WithFiles(AGENT_FILES_NETCORE).AndFile("nunit.console.nuget.agent.addins"),
            HasDirectory("tools/agents/net8.0").WithFiles(AGENT_FILES_NETCORE).AndFile("nunit.console.nuget.agent.addins")
        },
        symbols: new PackageCheck[] {
            HasDirectory("tools").WithFiles(ENGINE_PDB_FILES).AndFile("nunit3-console.pdb"),
            HasDirectory("tools/agents/net462").WithFiles(AGENT_PDB_FILES),
            HasDirectory("tools/agents/netcoreapp3.1").WithFiles(AGENT_PDB_FILES_NETCORE),
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
            HasDirectory("tools/agents/net462").WithFiles(AGENT_FILES).AndFile("nunit.console.choco.agent.addins"),
            HasDirectory("tools/agents/netcoreapp3.1").WithFiles(AGENT_FILES_NETCORE).AndFile("nunit.console.choco.agent.addins"),
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
            HasDirectory("bin/net462").WithFiles(CONSOLE_FILES).AndFiles(ENGINE_FILES).AndFile("nunit3-console.pdb").AndFiles(ENGINE_PDB_FILES),
            HasDirectory("bin/netstandard2.0").WithFiles(ENGINE_FILES).AndFiles(ENGINE_PDB_FILES),
            HasDirectory("bin/netcoreapp3.1").WithFiles(ENGINE_CORE_FILES).AndFiles(ENGINE_CORE_PDB_FILES),
            HasDirectory("bin/agents/net462").WithFiles(AGENT_FILES).AndFiles(AGENT_PDB_FILES),
            HasDirectory("bin/agents/net6.0").WithFiles(AGENT_FILES_NETCORE).AndFiles(AGENT_PDB_FILES_NETCORE),
            HasDirectory("bin/agents/net7.0").WithFiles(AGENT_FILES_NETCORE).AndFiles(AGENT_PDB_FILES_NETCORE),
            HasDirectory("bin/agents/net8.0").WithFiles(AGENT_FILES_NETCORE).AndFiles(AGENT_PDB_FILES_NETCORE)
        },
        testRunner: new ConsoleRunnerSelfTester(BuildSettings.ZipTestDirectory 
            + $"NUnit.Console.{BuildSettings.PackageVersion}/bin/net462/nunit3-console.exe"),
        tests: StandardRunnerTests,
        bundledExtensions: new [] {
            new PackageReference("NUnit.Extension.VSProjectLoader", "3.9.0"),
            new PackageReference("NUnit.Extension.NUnitProjectLoader", "3.8.0"),
            new PackageReference("NUnit.Extension.NUnitV2Driver", "3.9.0"),
            new PackageReference("NUnit.Extension.NUnitV2ResultWriter", "3.8.0"),
            new PackageReference("NUnit.Extension.TeamCityEventListener", "1.0.9")
        }),

    // NOTE: Packages below this point have no direct tests

    NUnitEnginePackage = new NuGetPackage(
        id: "NUnit.Engine",
        source: BuildSettings.NuGetDirectory + "engine/nunit.engine.nuspec",
        checks: new PackageCheck[] {
            HasFiles("LICENSE.txt", "NOTICES.txt"),
            HasDirectory("lib/net462").WithFiles(ENGINE_FILES),
            HasDirectory("lib/netstandard2.0").WithFiles(ENGINE_FILES),
            HasDirectory("contentFiles/any/lib/net462").WithFile("nunit.engine.nuget.addins"),
            HasDirectory("contentFiles/any/lib/netstandard2.0").WithFile("nunit.engine.nuget.addins"),
            HasDirectory("contentFiles/any/agents/net462").WithFiles(AGENT_FILES).AndFile("nunit.agent.addins")
        },
        symbols: new PackageCheck[] {
            HasDirectory("lib/net462").WithFiles(ENGINE_PDB_FILES),
            HasDirectory("lib/netstandard2.0").WithFiles(ENGINE_PDB_FILES),
            HasDirectory("contentFiles/any/agents/net462").WithFiles(AGENT_PDB_FILES)
        }),

    NUnitEngineApiPackage = new NuGetPackage(
        id: "NUnit.Engine.Api",
        source: BuildSettings.NuGetDirectory + "engine/nunit.engine.api.nuspec",
        checks: new PackageCheck[] {
            HasFile("LICENSE.txt"),
            HasDirectory("lib/net462").WithFile("nunit.engine.api.dll"),
            HasDirectory("lib/netstandard2.0").WithFile("nunit.engine.api.dll"),
        },
        symbols: new PackageCheck[] {
            HasDirectory("lib/net462").WithFile("nunit.engine.api.pdb"),
            HasDirectory("lib/netstandard2.0").WithFile("nunit.engine.api.pdb")
        })
});

//////////////////////////////////////////////////////////////////////
// TEST RUNNERS
//////////////////////////////////////////////////////////////////////

// Custom unit test runner to run console vs engine tests differently
// TODO: Use NUnitLite for all tests?
public class CustomTestRunner : TestRunner, IUnitTestRunner
{
    public int RunUnitTest(FilePath testPath)
    {
        // Run console tests under the just-built console
        if(testPath.ToString().Contains("nunit3-console.tests.dll"))
        {
            return BuildSettings.Context.StartProcess(
                BuildSettings.OutputDirectory + "net462/nunit3-console.exe",
                $"\"{testPath}\" {BuildSettings.UnitTestArguments}" );
        }

        // All other tests use NUnitLite
        return new NUnitLiteRunner().RunUnitTest(testPath);
    }
}

// Use the console runner we just built to run package tests
public class ConsoleRunnerSelfTester : TestRunner, IPackageTestRunner
{
    private string _executablePath;

	public ConsoleRunnerSelfTester(string executablePath)
	{
		_executablePath = executablePath;
	}

	public int RunPackageTest(string arguments)
	{
        Console.WriteLine("Running package test");
		return base.RunTest(_executablePath, arguments);
	}
}

//////////////////////////////////////////////////////////////////////
// ADDITIONAL TARGETS USED FOR RECOVERY AND DEBUGGING
//////////////////////////////////////////////////////////////////////

// Some of these targets may be moved into the recipe itself in the future.

// When a NuGet package was published successfully but the corresponding symbols 
// package failed, use this target locally after correcting the error.
// TODO: This task is extemely complicated because it has to copy lots of code
// from the recipe. It would be simpler if it were integrated in the recipe.
// TODO: This has been tested on NUnit.ConsoleRunner, so the branches with either
// zero or one packages are speculative at this point. They will need testing
// if this is incorporated into the recipe.
Task("PublishSymbolsPackage")
    .Description("Re-publish a specific symbols package to NuGet after a failure")
    .Does(() => 
    {
		if (!BuildSettings.ShouldPublishToNuGet)
			Information("Nothing to publish to NuGet from this run.");
		else if (CommandLineOptions.NoPush)
			Information("NoPush option suppressing publication to NuGet");
        else
        {
            List<PackageDefinition> packages;

            if (BuildSettings.Packages.Count == 0)
                throw new Exception("No packages exist!");
            else if (BuildSettings.Packages.Count == 1)
            {
                if (BuildSettings.Packages[0].PackageType != PackageType.NuGet)
                    throw new Exception("The only package is not a NuGet package");

                packages = BuildSettings.Packages;
            }
            else // count is > 1
            {
                if (!CommandLineOptions.PackageSelector.Exists)
                    throw new Exception("Multiple packages exist. Specify a nuget package id using the '--where' option");

                packages = new List<PackageDefinition>();

                foreach (var package in BuildSettings.Packages)
                    if (package.IsSelectedBy(CommandLineOptions.PackageSelector.Value))
                        packages.Add(package);

                if (packages.Count > 1)
                    throw new Exception("The '--where' option selected multiple packages");

                if (packages[0].PackageType != PackageType.NuGet)
                    throw new Exception("The selected package is a {package.PackageType} package. It must be a package for nuget.org.");
            }

            // At this point we have a single NuGet package in packages
		    var packageName = $"{packages[0].PackageId}.{BuildSettings.PackageVersion}.snupkg";
			var packagePath = BuildSettings.PackageDirectory + packageName;
            NuGetPush(packagePath, new NuGetPushSettings() { ApiKey = BuildSettings.NuGetApiKey, Source = BuildSettings.NuGetPushUrl });
        }
    });

//////////////////////////////////////////////////////////////////////
// EXECUTION
//////////////////////////////////////////////////////////////////////

Build.Run()
