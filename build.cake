// Load the recipe 
#load nuget:?package=NUnit.Cake.Recipe&version=1.3.1-alpha.1
// Comment out above line and uncomment below for local tests of recipe changes
//#load ../NUnit.Cake.Recipe/recipe/*.cake

#load package-tests.cake

// Initialize BuildSettings
BuildSettings.Initialize(
    Context,
    title: "NUnit Console and Engine",
    githubRepository: "nunit-console",
    solutionFile: "NUnitConsole.sln",
    exemptFiles: new[] { "Options.cs", "ProcessUtils.cs", "ProcessUtilsTests.cs" });

//////////////////////////////////////////////////////////////////////
// LISTS OF FILES USED IN CHECKING PACKAGES
////////////////////////////////////////////////////////////////////// 

FilePath[] ConsoleFiles = { 
    "nunit3-console.dll", "nunit3-console.dll.config", "nunit3-console.exe", "nunit3-console.pdb", 
    "nunit3-console.deps.json", "nunit3-console.runtimeconfig.json" };
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

//////////////////////////////////////////////////////////////////////
// INDIVIDUAL PACKAGE DEFINITIONS
//////////////////////////////////////////////////////////////////////

PackageDefinition NUnitConsoleNuGetPackage;
PackageDefinition NUnitConsoleRunnerNuGetPackage;
PackageDefinition NUnitConsoleRunnerNetCorePackage;
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
            HasDirectory("tools").WithFiles("nunit3-console.exe", "nunit3-console.exe.config").AndFiles(ENGINE_FILES),
            HasDirectory("tools/agents/net462").WithFiles(AGENT_FILES),
            HasDirectory("tools/agents/netcoreapp3.1").WithFiles(AGENT_FILES_NETCORE),
            HasDirectory("tools/agents/net6.0").WithFiles(AGENT_FILES_NETCORE),
            HasDirectory("tools/agents/net7.0").WithFiles(AGENT_FILES_NETCORE),
            HasDirectory("tools/agents/net8.0").WithFiles(AGENT_FILES_NETCORE)
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

    NUnitConsoleRunnerNetCorePackage = new DotNetToolPackage(
        id: "NUnit.ConsoleRunner.NetCore",
        source: BuildSettings.NuGetDirectory + "runners/nunit.console-runner.netcore.nuspec",
        checks: new PackageCheck[]
        { 
            HasFiles("nunit.exe"),
            HasDirectory(".store/nunit.consolerunner.netcore/**/tools/net8.0/any")
                .WithFiles(ENGINE_FILES).AndFiles(ConsoleFiles).AndFile("Microsoft.Extensions.DependencyModel.dll")
        },
        testRunner: new ConsoleRunnerSelfTester(BuildSettings.NuGetTestDirectory
            + $"NUnit.ConsoleRunner.NetCore.{BuildSettings.PackageVersion}/nunit.exe"),
        tests: NetCoreRunnerTests),

    NUnitConsoleRunnerChocolateyPackage = new ChocolateyPackage(
        id: "nunit-console-runner",
        source: BuildSettings.ChocolateyDirectory + "nunit-console-runner.nuspec",
        checks: new PackageCheck[] {
            HasDirectory("tools").WithFiles("LICENSE.txt", "NOTICES.txt", "VERIFICATION.txt", "nunit3-console.exe", "nunit3-console.exe.config").AndFiles(ENGINE_FILES),
            HasDirectory("tools/agents/net462").WithFiles(AGENT_FILES),
            HasDirectory("tools/agents/netcoreapp3.1").WithFiles(AGENT_FILES_NETCORE),
            HasDirectory("tools/agents/net6.0").WithFiles(AGENT_FILES_NETCORE),
            HasDirectory("tools/agents/net7.0").WithFiles(AGENT_FILES_NETCORE),
            HasDirectory("tools/agents/net8.0").WithFiles(AGENT_FILES_NETCORE)
        },
        testRunner: new ConsoleRunnerSelfTester(BuildSettings.ChocolateyTestDirectory
            + $"nunit-console-runner.{BuildSettings.PackageVersion}/tools/nunit3-console.exe"),
        tests: StandardRunnerTests),

    NUnitConsoleZipPackage = new ZipPackage(
        id: "NUnit.Console",
        source: BuildSettings.ZipImageDirectory,
        checks: new PackageCheck[] {
            HasFiles("LICENSE.txt", "NOTICES.txt", "CHANGES.txt"),
            HasDirectory("bin/net462").WithFiles("nunit3-console.exe", "nunit3-console.exe.config", 
                "nunit3-console.pdb").AndFiles(ENGINE_FILES).AndFiles(ENGINE_PDB_FILES),
            HasDirectory("NUnit.Extension.NUnitProjectLoader.3.8.0"),
            HasDirectory("NUnit.Extension.NUnitV2Driver.3.9.0"),
            HasDirectory("NUnit.Extension.NUnitV2ResultWriter.3.8.0"),
            HasDirectory("NUnit.Extension.TeamCityEventListener.1.0.7"),
            HasDirectory("NUnit.Extension.VSProjectLoader.3.9.0"),
            //HasDirectory("bin/net462/addins").WithFiles(
            //    "nunit.core.dll", "nunit.core.interfaces.dll", "nunit.engine.api.dll", 
            //    "nunit.v2.driver.dll", "nunit-project-loader.dll", "nunit-v2-result-writer.dll", 
            //    "teamcity-event-listener.dll", "vs-project-loader.dll"),
            HasDirectory("bin/netcoreapp3.1").WithFiles(ENGINE_CORE_FILES).AndFiles(ENGINE_CORE_PDB_FILES),
            HasDirectory("bin/agents/net462").WithFiles(AGENT_FILES).AndFiles(AGENT_PDB_FILES),
            HasDirectory("bin/agents/net6.0").WithFiles(AGENT_FILES_NETCORE).AndFiles(AGENT_PDB_FILES_NETCORE),
            HasDirectory("bin/agents/net7.0").WithFiles(AGENT_FILES_NETCORE).AndFiles(AGENT_PDB_FILES_NETCORE),
            HasDirectory("bin/agents/net8.0").WithFiles(AGENT_FILES_NETCORE).AndFiles(AGENT_PDB_FILES_NETCORE),
        },
        testRunner: new ConsoleRunnerSelfTester(BuildSettings.ZipTestDirectory
            + $"NUnit.Console.{BuildSettings.PackageVersion}/bin/net462/nunit3-console.exe"),
        tests: StandardRunnerTests,
        bundledExtensions: new [] {
            KnownExtensions.VSProjectLoader.NuGetPackage,
            KnownExtensions.NUnitProjectLoader.NuGetPackage,
            KnownExtensions.NUnitV2Driver.NuGetPackage,
            KnownExtensions.NUnitV2ResultWriter.NuGetPackage,
            KnownExtensions.TeamCityEventListener.NuGetPackage
        }),

    // NOTE: Packages below this point have no direct tests

    NUnitEnginePackage = new NuGetPackage(
        id: "NUnit.Engine",
        source: BuildSettings.NuGetDirectory + "engine/nunit.engine.nuspec",
        checks: new PackageCheck[] {
            HasFiles("LICENSE.txt", "NOTICES.txt"),
            HasDirectory("lib/net462").WithFiles(ENGINE_FILES),
            HasDirectory("lib/net8.0").WithFiles(ENGINE_FILES).AndFile("Microsoft.Extensions.DependencyModel.dll"),
            HasDirectory("contentFiles/any/agents/net462").WithFiles(AGENT_FILES)
        },
        symbols: new PackageCheck[] {
            HasDirectory("lib/net462").WithFiles(ENGINE_PDB_FILES),
            HasDirectory("lib/netstandard2.0").WithFiles(ENGINE_PDB_FILES),
            HasDirectory("lib/net6.0").WithFiles(ENGINE_PDB_FILES),
            HasDirectory("lib/net8.0").WithFiles(ENGINE_PDB_FILES),
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

Task("BuildZipPackage")
    .Does(() =>
    {
        NUnitConsoleZipPackage.BuildPackage();
    });

Task("InstallZipPackage")
    .Does(() =>
    {
        NUnitConsoleZipPackage.InstallPackage();
    });

Task("VerifyZipPackage")
    .Does(() =>
    {
        NUnitConsoleZipPackage.VerifyPackage();
    });

Task("TestZipPackage")
    .Does(() =>
    {
        NUnitConsoleZipPackage.RunPackageTests();
    });

Task("TestNetCorePackage")
    .Does(() =>
    {
        NUnitConsoleRunnerNetCorePackage.RunPackageTests();
    });

//////////////////////////////////////////////////////////////////////
// TEST RUNNERS
//////////////////////////////////////////////////////////////////////

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
// EXECUTION
//////////////////////////////////////////////////////////////////////

Build.Run()
