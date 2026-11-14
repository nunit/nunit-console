// Load the recipe
#load nuget:?package=NUnit.Cake.Recipe&version=2.0.0-beta.5.9
//Comment out above line and uncomment below for local tests of recipe changes
//#load ../NUnit.Cake.Recipe/recipe/*.cake

// Load additional cake files
#load package-tests.cake
#load KnownExtensions.cake
#load test-suites.cake

var testSuiteRunner = new TestSuiteRunner();

// Initialize BuildSettings
BuildSettings.Initialize(
    Context,
    title: "NUnit Console and Engine",
    githubRepository: "nunit-console",
    solutionFile: "NUnitConsole.slnx",
    unitTestRunner: testSuiteRunner,
    exemptFiles: new[] { "Options.cs", "ProcessUtils.cs", "ProcessUtilsTests.cs", "CallerArgumentExpressionAttribute.cs" });

foreach (string suite in new[] { "Unit", "Integration", "Acceptance", "PendingAcceptance" })
{
    Task("Test" + suite)
        .Description("Run the " + suite + " test suite")
        .IsDependentOn("Build")
        .WithCriteria(() => !CommandLineOptions.NoTests)
        .Does(() => testSuiteRunner.RunSuite(suite));
}

Task("TestAll")
    .Description("Run required regressions and report pending acceptance tests")
    .IsDependentOn("Test")
    .IsDependentOn("TestPendingAcceptance");

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
        HasDependency("NUnit.Engine.Api").LatestDevBuild
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
        HasDirectory("lib/net10.0").WithFile("nunit.engine.dll"),
        HasDependency("NUnit.Engine.Api").LatestDevBuild,
        HasDependency("NUnit.Common"), // No version check as we build this here
        HasDependency("NUnit.Extensibility").LatestDevBuild
    },
    symbols: new PackageCheck[]
    {
        HasDirectory("lib/net462").WithFile("nunit.engine.pdb"),
        HasDirectory("lib/net10.0").WithFile("nunit.engine.pdb")
    });
// TODO: Revise AgentSelector and reinstate tests
//testRunner: new AgentSelector(
//    BuildSettings.NuGetTestDirectory + $"NUnit.Engine.{BuildSettings.PackageVersion}/agents"),
//tests: EngineTests),

// NUnit.ConsoleRunner uses a nuspec file to specify the bundled pluggable agents
PackageDefinition NUnitConsoleRunnerNuGetPackage = new NuGetPackage(
    id: "NUnit.ConsoleRunner",
    source: BuildSettings.NuGetDirectory + "runners/NUnit.ConsoleRunner.nuspec",
    description: RUNNER_DESCRIPTION,
    checks: new PackageCheck[] {
        HasFiles("LICENSE.txt", "NOTICES.txt", "nunit_256.png"),
        HasDirectory("tools").WithFiles("nunit-console.exe", "nunit-console.exe.config", "nunit.engine.api.dll", "nunit.extensibility.dll", "nunit.extensibility.api.dll", "nunit.common.dll", "nunit.engine.dll", "testcentric.metadata.dll"),
        HasDependencies(KnownExtensions.BundledNuGetAgents)
        //HasDependency(KnownExtensions.Net462PluggableAgent.NuGetPackage).LatestDevBuild,
        //HasDependency(KnownExtensions.Net80PluggableAgent.NuGetPackage).LatestDevBuild,
        //HasDependency(KnownExtensions.Net90PluggableAgent.NuGetPackage).LatestDevBuild,
        //HasDependency(KnownExtensions.Net10PluggableAgent.NuGetPackage).LatestDevBuild
    },
    testRunner: new ConsoleRunnerSelfTester(BuildSettings.NuGetTestDirectory
        + $"NUnit.ConsoleRunner.{BuildSettings.PackageVersion}/tools/nunit-console.exe"),
    tests: StandardRunnerTests);

PackageDefinition NUnitConsoleRunnerDotNetToolPackage = new DotNetToolPackage(
    id: "NUnit.ConsoleRunner.NetCore",
    source: BuildSettings.SourceDirectory + "NUnitConsole/nunit4-netcore-console/nunit4-netcore-console.csproj",
    checks: new PackageCheck[]
    {
        HasFiles("nunit.exe"),
        HasDirectory(".store/nunit.consolerunner.netcore/**/tools/net10.0/any").WithFiles(
            "nunit-netcore-console.dll", "nunit-netcore-console.dll.config", "nunit-netcore-console.exe",
            "nunit-netcore-console.runtimeconfig.json", "DotnetToolSettings.xml",
            "NUnit.Common.dll", "NUnit.Engine.Api.dll", "nunit.engine.dll", 
            "NUnit.Extensibility.Api.dll", "NUnit.Extensibility.dll", "TestCentric.Metadata.dll"),
        // TODO: Fix recipe so dependency check works for DotNetToolPackage. Issue #141
        //HasDependencies(KnownExtensions.BundledNuGetAgents)
        //HasDependency(KnownExtensions.Net462PluggableAgent.NuGetPackage).LatestDevBuild,
        //HasDependency(KnownExtensions.Net80PluggableAgent.NuGetPackage).LatestDevBuild,
        //HasDependency(KnownExtensions.Net90PluggableAgent.NuGetPackage).LatestDevBuild,
        //HasDependency(KnownExtensions.Net10PluggableAgent.NuGetPackage).LatestDevBuild
    },
    testRunner: new ConsoleRunnerSelfTester(BuildSettings.NuGetTestDirectory + "nunit.exe"),
    tests: NetCoreRunnerTests);

// Add all packages to BuildSettings in order they should be build.
// Dependencies must precede all the packages that depend on them.
BuildSettings.Packages.AddRange(new PackageDefinition[] {
    NUnitCommonPackage,
    NUnitEnginePackage,
    NUnitConsoleRunnerNuGetPackage,
    NUnitConsoleRunnerDotNetToolPackage
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
