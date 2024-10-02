// Load the recipe 
#load nuget:?package=NUnit.Cake.Recipe&version=1.2.0-dev00007
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
            HasDirectory("tools").WithFiles("nunit3-console.exe", "nunit3-console.exe.config", "nunit.console.nuget.addins").AndFiles(ENGINE_FILES),
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

    NUnitConsoleRunnerNetCorePackage = new DotNetToolPackage(
        id: "NUnit.ConsoleRunner.NetCore",
        source: BuildSettings.NuGetDirectory + "runners/nunit.console-runner.netcore.nuspec",
        checks: new PackageCheck[] { HasFiles("nunit.exe") },
        testRunner: new ConsoleRunnerSelfTester(BuildSettings.NuGetTestDirectory
            + $"NUnit.ConsoleRunner.NetCore.{BuildSettings.PackageVersion}/nunit.exe"),
        tests: NetCoreRunnerTests),

    NUnitConsoleRunnerNet80Package = new DotNetToolPackage(
        id: "NUnit.ConsoleRunner.Net80",
        source: BuildSettings.NuGetDirectory + "runners/nunit.console-runner.net80.nuspec",
        checks: new PackageCheck[] { HasFiles("nunit-net80.exe") },
        testRunner: new ConsoleRunnerSelfTester(BuildSettings.NuGetTestDirectory
            + $"NUnit.ConsoleRunner.Net80.{BuildSettings.PackageVersion}/nunit-net80.exe"),
        tests: NetCoreRunnerTests),

    NUnitConsoleRunnerChocolateyPackage = new ChocolateyPackage(
        id: "nunit-console-runner",
        source: BuildSettings.ChocolateyDirectory + "nunit-console-runner.nuspec",
        checks: new PackageCheck[] {
            HasDirectory("tools").WithFiles("LICENSE.txt", "NOTICES.txt", "VERIFICATION.txt", "nunit3-console.exe", "nunit3-console.exe.config", "nunit.console.choco.addins").AndFiles(ENGINE_FILES),
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
            HasDirectory("bin/net462").WithFiles("nunit3-console.exe", "nunit3-console.exe.config", "nunit3-console.pdb").AndFiles(ENGINE_FILES).AndFiles(ENGINE_PDB_FILES),
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
            new PackageReference("NUnit.Extension.TeamCityEventListener", "1.0.7")
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
