// Load the recipe
#load nuget:?package=NUnit.Cake.Recipe&version=2.0.0-beta.4.12
// Comment out above line and uncomment below for local tests of recipe changes
//#load ../NUnit.Cake.Recipe/recipe/*.cake

// Load additional cake files
#load package-tests.cake
#load KnownExtensions.cake

// Initialize BuildSettings
BuildSettings.Initialize(
    Context,
    title: "NUnit Console and Engine",
    githubRepository: "nunit-console",
    solutionFile: "NUnitConsole.slnx",
    exemptFiles: new[] { "Options.cs", "ProcessUtils.cs", "ProcessUtilsTests.cs", "CallerArgumentExpressionAttribute.cs" });

//////////////////////////////////////////////////////////////////////
// INDIVIDUAL PACKAGE DEFINITIONS
//////////////////////////////////////////////////////////////////////

var THIS_VERSION = BuildSettings.PackageVersion;
var RUNNER_DESCRIPTION =
      "This package includes the console runner and test engine for version 3 and higher of the NUnit unit-testing framework." +
      "\r\n\nAny extensions, if needed, may be installed as separate packages.";

PackageDefinition NUnitCommonPackage = new NuGetPackage(
    id: "NUnit.Common",
    source: BuildSettings.SourceDirectory + "NUnitCommon/nunit.common/nunit.common.csproj",
    checks: new PackageCheck[]
    {
        HasFile("LICENSE.txt"),
        HasDirectory("lib/net462").WithFile("nunit.common.dll"),
        HasDirectory("lib/netstandard2.0").WithFile("nunit.common.dll"),
        HasDependency("NUnit.Engine.Api")
    },
    symbols: new PackageCheck[]
    {
        HasDirectory("lib/net462").WithFile("nunit.common.pdb"),
        HasDirectory("lib/netstandard2.0").WithFile("nunit.common.pdb"),
    });

PackageDefinition NUnitEnginePackage = new NuGetPackage(
    id: "NUnit.Engine",
    source: BuildSettings.SourceDirectory + "NUnitEngine/nunit.engine/nunit.engine.csproj",
    checks: new PackageCheck[]
    {
        HasFiles("LICENSE.txt"),
        HasDirectory("lib/net462").WithFile("nunit.engine.dll"),
        HasDirectory("lib/net8.0").WithFile("nunit.engine.dll"),
        HasDependency("NUnit.Engine.Api"),
        HasDependency("NUnit.Common"),
        HasDependency("NUnit.Extensibility")
    },
    symbols: new PackageCheck[]
    {
        HasDirectory("lib/net462").WithFile("nunit.engine.pdb"),
        HasDirectory("lib/net8.0").WithFile("nunit.engine.pdb")
    });
// TODO: Revise AgentSelector and reinstate tests
//testRunner: new AgentSelector(
//    BuildSettings.NuGetTestDirectory + $"NUnit.Engine.{BuildSettings.PackageVersion}/agents"),
//tests: EngineTests),

PackageDefinition NUnitConsoleRunnerDotNetToolPackage = new DotNetToolPackage(
    id: "NUnit.ConsoleRunner.NetCore",
    source: BuildSettings.SourceDirectory + "NUnitConsole/nunit4-netcore-console/nunit4-netcore-console.csproj",
    checks: new PackageCheck[]
    {
        HasFiles("nunit.exe"),
        HasDirectory(".store/nunit.consolerunner.netcore/**/tools/net8.0/any").WithFiles(
            "nunit-netcore-console.dll", "nunit-netcore-console.dll.config",
            "nunit.engine.dll", "nunit.agent.core.dll", "testcentric.metadata.dll",
            "Microsoft.Extensions.DependencyModel.dll")
    },
    testRunner: new ConsoleRunnerSelfTester(BuildSettings.NuGetTestDirectory + "nunit.exe"),
    tests: NetCoreRunnerTests);

// NUnit.ConsoleRunner uses a nuspec file to specify the bundled pluggable agents
PackageDefinition NUnitConsoleRunnerNuGetPackage = new NuGetPackage(
    id: "NUnit.ConsoleRunner",
    source: BuildSettings.NuGetDirectory + "runners/nunit.console-runner.nuspec",
    description: RUNNER_DESCRIPTION,
    checks: new PackageCheck[] {
        HasDependencies(KnownExtensions.BundledNuGetAgents)
    },
    testRunner: new ConsoleRunnerSelfTester(BuildSettings.NuGetTestDirectory
        + $"NUnit.ConsoleRunner.{BuildSettings.PackageVersion}/tools/nunit-console.exe"),
    tests: StandardRunnerTests);

// Add all packages to BuildSettings in order they should be build.
// Dependencies must precede all the packages that depend on them.
BuildSettings.Packages.AddRange(new PackageDefinition[] {
    NUnitCommonPackage,
    NUnitEnginePackage,
    NUnitConsoleRunnerDotNetToolPackage,
    NUnitConsoleRunnerNuGetPackage,
});

Task("BuildPackages")
    .Description("Just build packages, without installing or running package tests")
    .IsDependentOn("Build")
    .Does(() =>
    {
        foreach (var package in BuildSettings.Packages)
            package.BuildPackage();
    });

//////////////////////////////////////////////////////////////////////
// CONSOLE PACKAGE TEST RUNNER
//////////////////////////////////////////////////////////////////////

// Use the console runner we just built to run package tests
public class ConsoleRunnerSelfTester : TestRunner, IPackageTestRunner
{
    private string _executablePath;

    public ConsoleRunnerSelfTester(string executablePath)
    {
        _executablePath = executablePath;
    }

    public int RunPackageTest(string arguments, bool redirectOutput)
    {
        Console.WriteLine($"Running package test with arguments {arguments}");
        return base.RunPackageTest(_executablePath, new ProcessSettings() { Arguments = arguments, RedirectStandardOutput = redirectOutput });
    }
}

//////////////////////////////////////////////////////////////////////
// EXECUTION
//////////////////////////////////////////////////////////////////////

Build.Run()
