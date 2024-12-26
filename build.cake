// Load the recipe 
#load nuget:?package=NUnit.Cake.Recipe&version=1.3.0-alpha.6
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
    exemptFiles: new[] { "Options.cs", "ProcessUtils.cs", "ProcessUtilsTests.cs" });

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
            HasDirectory("tools").WithFiles(
                "nunit4-console.exe", "nunit4-console.exe.config",
                "nunit.engine.dll", "nunit.engine.core.dll", "nunit.engine.api.dll", "testcentric.engine.metadata.dll"),
            HasDirectory("tools/agents/net462").WithFiles(
                "nunit-agent-net462.exe", "nunit-agent-net462.exe.config", "nunit-agent-net462-x86.exe", "nunit-agent-net462-x86.exe.config",
                "nunit.engine.core.dll", "nunit.engine.api.dll", "testcentric.engine.metadata.dll"),
            HasDirectory("tools/agents/netcoreapp3.1").WithFiles(
                "nunit-agent-netcore31.dll", "nunit-agent-netcore31.dll.config",
                "nunit.engine.core.dll", "nunit.engine.api.dll", "testcentric.engine.metadata.dll"),
            HasDirectory("tools/agents/net6.0").WithFiles(
                "nunit-agent-net60.dll", "nunit-agent-net60.dll.config",
                "nunit.engine.core.dll", "nunit.engine.api.dll", "testcentric.engine.metadata.dll"),
            HasDirectory("tools/agents/net7.0").WithFiles(
                "nunit-agent-net70.dll", "nunit-agent-net70.dll.config",
                "nunit.engine.core.dll", "nunit.engine.api.dll", "testcentric.engine.metadata.dll"),
            HasDirectory("tools/agents/net8.0").WithFiles(
                "nunit-agent-net80.dll", "nunit-agent-net80.dll.config",
                "nunit.engine.core.dll", "nunit.engine.api.dll", "testcentric.engine.metadata.dll")
        },
        symbols: new PackageCheck[] {
            HasDirectory("tools").WithFiles(
                "nunit.engine.pdb", "nunit.engine.core.pdb", "nunit.engine.api.pdb", "nunit4-console.pdb"),
            HasDirectory("tools/agents/net462").WithFiles(
                "nunit-agent.pdb", "nunit-agent-x86.pdb", "nunit.engine.core.pdb", "nunit.engine.api.pdb"),
            HasDirectory("tools/agents/netcoreapp3.1").WithFiles(
                "nunit-agent.pdb", "nunit.engine.core.pdb", "nunit.engine.api.pdb"),
            HasDirectory("tools/agents/net6.0").WithFiles(
                "nunit-agent.pdb", "nunit.engine.core.pdb", "nunit.engine.api.pdb"),
            HasDirectory("tools/agents/net7.0").WithFiles(
                "nunit-agent.pdb", "nunit.engine.core.pdb", "nunit.engine.api.pdb"),
            HasDirectory("tools/agents/net8.0").WithFiles(
                "nunit-agent.pdb", "nunit.engine.core.pdb", "nunit.engine.api.pdb")
        },
        testRunner: new ConsoleRunnerSelfTester(BuildSettings.NuGetTestDirectory
            + $"NUnit.ConsoleRunner.{BuildSettings.PackageVersion}/tools/nunit4-console.exe"),
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
                "nunit4-netcore-console.dll", "nunit4-netcore-console.dll.config",
                "nunit.engine.dll", "nunit.engine.core.dll", "nunit.engine.api.dll", "testcentric.engine.metadata.dll",
                "Microsoft.Extensions.DependencyModel.dll")
        },
        testRunner: new ConsoleRunnerSelfTester(BuildSettings.NuGetTestDirectory
            + $"NUnit.ConsoleRunner.NetCore.{BuildSettings.PackageVersion}/nunit.exe"),
        tests: NetCoreRunnerTests),

    NUnitConsoleRunnerChocolateyPackage = new ChocolateyPackage(
        id: "nunit-console-runner",
        source: BuildSettings.ChocolateyDirectory + "nunit-console-runner.nuspec",
        checks: new PackageCheck[] {
            HasDirectory("tools").WithFiles(
                "LICENSE.txt", "NOTICES.txt", "VERIFICATION.txt", "nunit4-console.exe", "nunit4-console.exe.config",
                "nunit.engine.dll", "nunit.engine.core.dll", "nunit.engine.api.dll", "testcentric.engine.metadata.dll"),
            HasDirectory("tools/agents/net462").WithFiles(
                "nunit-agent-net462.exe", "nunit-agent-net462.exe.config", "nunit-agent-net462-x86.exe", "nunit-agent-net462-x86.exe.config",
                "nunit.engine.core.dll", "nunit.engine.api.dll", "testcentric.engine.metadata.dll"),
            HasDirectory("tools/agents/netcoreapp3.1").WithFiles(
                "nunit-agent-netcore31.dll", "nunit-agent-netcore31.dll.config",
                "nunit.engine.core.dll", "nunit.engine.api.dll", "testcentric.engine.metadata.dll"),
            HasDirectory("tools/agents/net6.0").WithFiles(
                "nunit-agent-net60.dll", "nunit-agent-net60.dll.config",
                "nunit.engine.core.dll", "nunit.engine.api.dll", "testcentric.engine.metadata.dll"),
            HasDirectory("tools/agents/net7.0").WithFiles(
                "nunit-agent-net70.dll", "nunit-agent-net70.dll.config",
                "nunit.engine.core.dll", "nunit.engine.api.dll", "testcentric.engine.metadata.dll"),
            HasDirectory("tools/agents/net8.0").WithFiles(
                "nunit-agent-net80.dll", "nunit-agent-net80.dll.config",
                "nunit.engine.core.dll", "nunit.engine.api.dll", "testcentric.engine.metadata.dll")
        },
        testRunner: new ConsoleRunnerSelfTester(BuildSettings.ChocolateyTestDirectory
            + $"nunit-console-runner.{BuildSettings.PackageVersion}/tools/nunit4-console.exe"),
        tests: StandardRunnerTests),

    //NUnitConsoleZipPackage = new ZipPackage(
    //    id: "NUnit.Console",
    //    source: BuildSettings.ZipImageDirectory,
    //    checks: new PackageCheck[] {
    //        HasFiles("LICENSE.txt", "NOTICES.txt", "CHANGES.txt"),
    //        HasDirectory("bin/net462").WithFiles(
    //            "nunit4-console.exe", "nunit4-console.exe.config", "nunit4-console.pdb",
    //            "nunit.engine.dll", "nunit.engine.core.dll", "nunit.engine.api.dll", "testcentric.engine.metadata.dll",
    //            "nunit.engine.pdb", "nunit.engine.core.pdb", "nunit.engine.api.pdb"),
    //        //HasDirectory("bin/net462/addins").WithFiles(
    //        //    "nunit.core.dll", "nunit.core.interfaces.dll", "nunit.engine.api.dll",
    //        //    "nunit.v2.driver.dll", "nunit-project-loader.dll", "nunit-v2-result-writer.dll",
    //        //    "teamcity-event-listener.dll", "vs-project-loader.dll"),
    //        HasDirectory("bin/netcoreapp3.1").WithFiles(
    //            "nunit.engine.core.dll", "nunit.engine.api.dll", "testcentric.engine.metadata.dll",
    //            "nunit.engine.core.pdb", "nunit.engine.api.pdb"),
    //        HasDirectory("bin/agents/net462").WithFiles(
    //            "nunit-agent-net462.exe", "nunit-agent-net462.exe.config",
    //            "nunit-agent-net462-x86.exe", "nunit-agent-net462-x86.exe.config",
    //            "nunit.engine.core.dll", "nunit.engine.api.dll", "testcentric.engine.metadata.dll",
    //            "nunit-agent-net462.pdb", "nunit-agent-net462-x86.pdb", "nunit.engine.core.pdb", "nunit.engine.api.pdb"),
    //        HasDirectory("bin/agents/net6.0").WithFiles(
    //            "nunit-agent-net60.dll", "nunit-agent-net60.dll.config",
    //            "nunit.engine.core.dll", "nunit.engine.api.dll", "testcentric.engine.metadata.dll",
    //            "Microsoft.Extensions.DependencyModel.dll",
    //            "nunit-agent-net60.pdb", "nunit.engine.core.pdb", "nunit.engine.api.pdb"),
    //        HasDirectory("bin/agents/net7.0").WithFiles(
    //            "nunit-agent-net70.dll", "nunit-agent-net70.dll.config",
    //            "nunit.engine.core.dll", "nunit.engine.api.dll", "testcentric.engine.metadata.dll",
    //            "Microsoft.Extensions.DependencyModel.dll",
    //            "nunit-agent-net70.pdb", "nunit.engine.core.pdb", "nunit.engine.api.pdb"),
    //        HasDirectory("bin/agents/net8.0").WithFiles(
    //            "nunit-agent-net80.dll", "nunit-agent-net80.dll.config",
    //            "nunit.engine.core.dll", "nunit.engine.api.dll", "testcentric.engine.metadata.dll",
    //            "Microsoft.Extensions.DependencyModel.dll",
    //            "nunit-agent-net80.pdb", "nunit.engine.core.pdb", "nunit.engine.api.pdb")
    //    },
    //    testRunner: new ConsoleRunnerSelfTester(BuildSettings.ZipTestDirectory
    //        + $"NUnit.Console.{BuildSettings.PackageVersion}/bin/net462/nunit4-console.exe"),
    //    tests: StandardRunnerTests,
    //    bundledExtensions: new [] {
    //        KnownExtensions.VSProjectLoader.NuGetPackage,
    //        KnownExtensions.NUnitProjectLoader.NuGetPackage,
    //        //KnownExtensions.NUnitV2Driver.NuGetPackage,
    //        KnownExtensions.NUnitV2ResultWriter.NuGetPackage,
    //        KnownExtensions.TeamCityEventListener.NuGetPackage
    //    }),

    // NOTE: Packages below this point have no direct tests

    NUnitEnginePackage = new NuGetPackage(
        id: "NUnit.Engine",
        source: BuildSettings.NuGetDirectory + "engine/nunit.engine.nuspec",
        checks: new PackageCheck[] {
            HasFiles("LICENSE.txt", "NOTICES.txt"),
            HasDirectory("lib/net462").WithFiles(
                "nunit.engine.dll", "nunit.engine.core.dll", "nunit.engine.api.dll", "testcentric.engine.metadata.dll"),
            HasDirectory("lib/net8.0").WithFiles(
                "nunit.engine.dll", "nunit.engine.core.dll", "nunit.engine.api.dll", "testcentric.engine.metadata.dll", "Microsoft.Extensions.DependencyModel.dll"),
            HasDirectory("agents/net462").WithFiles(
                "nunit-agent-net462.exe", "nunit-agent-net462.exe.config",
                "nunit-agent-net462-x86.exe", "nunit-agent-net462-x86.exe.config",
                "nunit.engine.core.dll", "nunit.engine.api.dll", "testcentric.engine.metadata.dll") },
        symbols: new PackageCheck[] {
            HasDirectory("lib/net462").WithFiles(
                "nunit.engine.pdb", "nunit.engine.core.pdb", "nunit.engine.api.pdb"),
            HasDirectory("lib/netstandard2.0").WithFiles(
                "nunit.engine.pdb", "nunit.engine.core.pdb", "nunit.engine.api.pdb"),
            HasDirectory("lib/net6.0").WithFiles(
                "nunit.engine.pdb", "nunit.engine.core.pdb", "nunit.engine.api.pdb"),
            HasDirectory("lib/net8.0").WithFiles(
                "nunit.engine.pdb", "nunit.engine.core.pdb", "nunit.engine.api.pdb"),
            HasDirectory("contentFiles/any/agents/net462").WithFiles(
                "nunit-agent.pdb", "nunit-agent-x86.pdb", "nunit.engine.core.pdb", "nunit.engine.api.pdb")
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
