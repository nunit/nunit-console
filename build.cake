// Load the recipe 
#load nuget:?package=NUnit.Cake.Recipe&version=1.4.0-alpha.11
// Comment out above line and uncomment below for local tests of recipe changes
//#load ../NUnit.Cake.Recipe/recipe/*.cake

#load package-tests.cake

// Initialize BuildSettings
BuildSettings.Initialize(
    Context,
    title: "NUnit Console and Engine",
    githubRepository: "nunit-console",
    solutionFile: "NUnitConsole.sln",
    buildWithMSBuild: true,
    exemptFiles: new[] { "Options.cs", "ProcessUtils.cs", "ProcessUtilsTests.cs", "CallerArgumentExpressionAttribute.cs" });

//////////////////////////////////////////////////////////////////////
// INDIVIDUAL PACKAGE DEFINITIONS
//////////////////////////////////////////////////////////////////////

PackageDefinition NUnitConsoleNuGetPackage;
PackageDefinition NUnitConsoleRunnerNuGetPackage;
PackageDefinition NUnitConsoleRunnerNetCorePackage;
PackageDefinition NUnitConsoleRunnerNet80Package;
PackageDefinition NUnitEnginePackage;
PackageDefinition NUnitEngineApiPackage;
PackageDefinition NUnitExtensibilityApiPackage;
PackageDefinition NUnitConsoleRunnerChocolateyPackage;

BuildSettings.Packages.AddRange(new PackageDefinition[] {

    NUnitConsoleRunnerNuGetPackage = new NuGetPackage(
        id: "NUnit.ConsoleRunner",
        source: BuildSettings.NuGetDirectory + "runners/nunit.console-runner.nuspec",
        checks: new PackageCheck[] {
            HasFiles("LICENSE.txt", "NOTICES.txt"),
            HasDirectory("tools").WithFiles(
                "nunit-console.exe", "nunit-console.exe.config", "nunit.engine.dll",
                "nunit.extensibility.dll", "nunit.extensibility.api.dll", "nunit.common.dll", 
                "nunit.engine.api.dll", "testcentric.metadata.dll"),
            HasDirectory("tools/agents/net462").WithFiles(
                "nunit-agent-net462.exe", "nunit-agent-net462.exe.config", "nunit-agent-net462-x86.exe", "nunit-agent-net462-x86.exe.config",
                "nunit.agent.core.dll", "nunit.extensibility.dll", "nunit.extensibility.api.dll", 
                "nunit.common.dll", "nunit.engine.api.dll", "testcentric.metadata.dll"),
            HasDirectory("tools/agents/net8.0").WithFiles(
                "nunit-agent-net80.dll", "nunit-agent-net80.dll.config",
                "nunit.agent.core.dll", "nunit.extensibility.dll", "nunit.extensibility.api.dll",
                "nunit.common.dll", "nunit.engine.api.dll", "testcentric.metadata.dll")
        },
        symbols: new PackageCheck[] {
            HasDirectory("tools").WithFiles(
                "nunit.engine.pdb", "nunit.extensibility.pdb", "nunit.extensibility.api.pdb",
                "nunit.common.pdb", "nunit.engine.api.pdb", "nunit-console.pdb"),
            HasDirectory("tools/agents/net462").WithFiles(
                "nunit-agent-net462.pdb", "nunit-agent-net462-x86.pdb", "nunit.agent.core.pdb", 
                "nunit.extensibility.pdb", "nunit.extensibility.api.pdb", "nunit.common.pdb", "nunit.engine.api.pdb"),
            HasDirectory("tools/agents/net8.0").WithFiles(
                "nunit-agent-net80.pdb", "nunit.agent.core.pdb", "nunit.extensibility.pdb", "nunit.extensibility.api.pdb",
                "nunit.common.pdb", "nunit.engine.api.pdb")
        },
        testRunner: new ConsoleRunnerSelfTester(BuildSettings.NuGetTestDirectory
            + $"NUnit.ConsoleRunner.{BuildSettings.PackageVersion}/tools/nunit-console.exe"),
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
            HasDirectory(".store/nunit.consolerunner.netcore/**/tools/net8.0/any").WithFiles(
                "nunit-netcore-console.dll", "nunit-netcore-console.dll.config",
                "nunit.engine.dll", "nunit.agent.core.dll", "nunit.extensibility.dll", 
                "nunit.extensibility.api.dll", "nunit.engine.api.dll", "testcentric.metadata.dll",
                "Microsoft.Extensions.DependencyModel.dll")
        },
        testRunner: new ConsoleRunnerSelfTester(BuildSettings.NuGetTestDirectory + "nunit.exe"),
        tests: NetCoreRunnerTests),

    NUnitConsoleRunnerChocolateyPackage = new ChocolateyPackage(
        id: "nunit-console-runner-v4",
        source: BuildSettings.ChocolateyDirectory + "nunit-console-runner.nuspec",
        checks: new PackageCheck[] {
            HasDirectory("tools").WithFiles(
                "LICENSE.txt", "NOTICES.txt", "VERIFICATION.txt", "nunit-console.exe", "nunit-console.exe.config",
                "nunit.engine.dll", "nunit.extensibility.dll", "nunit.extensibility.api.dll",
                "nunit.common.dll", "nunit.engine.api.dll", "testcentric.metadata.dll"),
            HasDirectory("tools/agents/net462").WithFiles(
                "nunit-agent-net462.exe", "nunit-agent-net462.exe.config", "nunit-agent-net462-x86.exe", "nunit-agent-net462-x86.exe.config",
                "nunit.agent.core.dll", "nunit.extensibility.dll", "nunit.extensibility.api.dll",
                "nunit.common.dll", "nunit.engine.api.dll", "testcentric.metadata.dll"),
            HasDirectory("tools/agents/net8.0").WithFiles(
                "nunit-agent-net80.dll", "nunit-agent-net80.dll.config",
                "nunit.agent.core.dll", "nunit.extensibility.dll", "nunit.extensibility.api.dll",
                "nunit.common.dll", "nunit.engine.api.dll", "testcentric.metadata.dll")
        },
        testRunner: new ConsoleRunnerSelfTester(BuildSettings.ChocolateyTestDirectory
            + $"nunit-console-runner-v4.{BuildSettings.PackageVersion}/tools/nunit-console.exe"),
        tests: StandardRunnerTests),

    NUnitEnginePackage = new NuGetPackage(
        id: "NUnit.Engine",
        source: BuildSettings.NuGetDirectory + "engine/nunit.engine.nuspec",
        checks: new PackageCheck[] {
            HasFiles("LICENSE.txt", "NOTICES.txt"),
            HasDirectory("lib/net462").WithFiles(
                "nunit.engine.dll", "nunit.extensibility.dll", "nunit.extensibility.api.dll",
                "nunit.common.dll", "nunit.engine.api.dll", "testcentric.metadata.dll"),
            HasDirectory("lib/net8.0").WithFiles(
                "nunit.engine.dll", "nunit.agent.core.dll", "nunit.extensibility.dll", "nunit.extensibility.api.dll",
                "nunit.common.dll", "nunit.engine.api.dll", "testcentric.metadata.dll", "Microsoft.Extensions.DependencyModel.dll"),
            HasDirectory("agents/net462").WithFiles(
                "nunit-agent-net462.exe", "nunit-agent-net462.exe.config",
                "nunit-agent-net462-x86.exe", "nunit-agent-net462-x86.exe.config",
                "nunit.agent.core.dll", "nunit.extensibility.dll", "nunit.extensibility.api.dll",
                "nunit.common.dll", "nunit.engine.api.dll", "testcentric.metadata.dll"),
            HasDirectory("agents/net8.0").WithFiles(
                "nunit-agent-net80.dll", "nunit-agent-net80.dll.config",
                "nunit.agent.core.dll", "nunit.extensibility.dll", "nunit.extensibility.api.dll",
                "nunit.common.dll", "nunit.engine.api.dll", "testcentric.metadata.dll") },
        symbols: new PackageCheck[] {
            HasDirectory("lib/net462").WithFiles(
                "nunit.engine.pdb", "nunit.extensibility.pdb",
                "nunit.extensibility.api.pdb", "nunit.engine.api.pdb"),
            HasDirectory("lib/net8.0").WithFiles(
                "nunit.engine.pdb","nunit.extensibility.pdb", "nunit.extensibility.api.pdb",
                "nunit.common.pdb", "nunit.engine.api.pdb"),
            HasDirectory("agents/net462").WithFiles(
                "nunit-agent-net462.pdb", "nunit-agent-net462-x86.pdb", "nunit.agent.core.pdb", "nunit.extensibility.pdb",
                "nunit.extensibility.api.pdb", "nunit.common.pdb", "nunit.engine.api.pdb"),
            HasDirectory("agents/net8.0").WithFiles(
                "nunit-agent-net80.pdb", "nunit.agent.core.pdb", "nunit.extensibility.pdb", "nunit.extensibility.api.pdb",
                "nunit.common.pdb", "nunit.engine.api.pdb")
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
        }),

    NUnitExtensibilityApiPackage = new NuGetPackage(
        id: "NUnit.Extensibility.Api",
        source: BuildSettings.SourceDirectory + "NUnitCommon/nunit.extensibility.api/nunit.extensibility.api.csproj",
        checks: new PackageCheck[] {
            HasFile("LICENSE.txt"),
            HasDirectory("lib/net462").WithFile("nunit.extensibility.api.dll"),
            HasDirectory("lib/netstandard2.0").WithFile("nunit.extensibility.api.dll")
        },
        symbols: new PackageCheck[] {
            HasDirectory("lib/net462").WithFile("nunit.extensibility.api.pdb"),
            HasDirectory("lib/netstandard2.0").WithFile("nunit.extensibility.api.pdb")
        })
});

//////////////////////////////////////////////////////////////////////
// PACKAGE TEST RUNNER
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
        Console.WriteLine("Running package test");
        return base.RunPackageTest(_executablePath, new ProcessSettings() { Arguments = arguments, RedirectStandardOutput = redirectOutput });
    }
}

//////////////////////////////////////////////////////////////////////
// EXECUTION
//////////////////////////////////////////////////////////////////////

Build.Run()
