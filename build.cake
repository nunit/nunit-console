// Load the recipe 
#load nuget:?package=NUnit.Cake.Recipe&version=1.5.0-alpha.4
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
PackageDefinition NUnitConsoleRunnerDotNetToolPackage;
PackageDefinition NUnitConsoleRunnerNet80Package;
PackageDefinition NUnitAgentCorePackage;
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

    NUnitConsoleRunnerDotNetToolPackage = new DotNetToolPackage(
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

    NUnitAgentCorePackage = new NuGetPackage(
        id: "NUnit.Agent.Core",
        source: BuildSettings.NuGetDirectory + "nunit.agent.core/nunit.agent.core.nuspec",
        checks: new PackageCheck[]
        {
            HasFiles("LICENSE.txt", "NOTICES.txt"),
            HasDirectory("lib/net462").WithFiles(
                "nunit.agent.core.dll", "nunit.extensibility.dll", "nunit.extensibility.api.dll", 
                "nunit.common.dll", "nunit.engine.api.dll", "testcentric.metadata.dll"),
            HasDirectory("lib/net8.0").WithFiles(
                "nunit.agent.core.dll", "nunit.extensibility.dll", "nunit.extensibility.api.dll", 
                "nunit.common.dll", "nunit.engine.api.dll", "testcentric.metadata.dll",
                "Microsoft.Extensions.DependencyModel.dll")
        },
        symbols: new PackageCheck[]
        {
            HasDirectory("lib/net462").WithFiles(
                "nunit.agent.core.pdb", "nunit.extensibility.pdb", "nunit.extensibility.api.pdb",
                "nunit.common.pdb", "nunit.engine.api.pdb"),
            HasDirectory("lib/net8.0").WithFiles(
                "nunit.agent.core.pdb", "nunit.extensibility.pdb", "nunit.extensibility.api.pdb",
                "nunit.common.pdb", "nunit.engine.api.pdb")
        },
        testRunner: new DirectTestAgentRunner(),
        tests: AgentCoreTests),

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
        },
        testRunner: new AgentSelector(
            BuildSettings.NuGetTestDirectory + $"NUnit.Engine.{BuildSettings.PackageVersion}/agents"),
        tests: EngineTests),

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
        Console.WriteLine("Running package test");
        return base.RunPackageTest(_executablePath, new ProcessSettings() { Arguments = arguments, RedirectStandardOutput = redirectOutput });
    }
}

//////////////////////////////////////////////////////////////////////
// AGENT CORE PACKAGE TEST RUNNER
//////////////////////////////////////////////////////////////////////

public class DirectTestAgentRunner : TestRunner, IPackageTestRunner
{
    public int RunPackageTest(string arguments, bool redirectOutput)
    {
        // First argument must be relative path to a test assembly.
        // It's immediate directory name is the name of the runtime.
        string testAssembly = arguments.Trim();
        testAssembly = BuildSettings.OutputDirectory + (testAssembly[0] == '"'
            ? testAssembly.Substring(1, testAssembly.IndexOf('"', 1) - 1)
            : testAssembly.Substring(0, testAssembly.IndexOf(' ')));

        if (!System.IO.File.Exists(testAssembly))
            throw new FileNotFoundException($"File not found: {testAssembly}");

        string testRuntime = System.IO.Path.GetFileName(System.IO.Path.GetDirectoryName(testAssembly));
        string agentRuntime = testRuntime;

        if (agentRuntime.EndsWith("-windows"))
            agentRuntime = agentRuntime.Substring(0, 6);

        // Avoid builds we don't have
        if (agentRuntime == "net35")
            agentRuntime = "net20";
        else if (agentRuntime == "net5.0")
            agentRuntime = "net6.0";

        var executablePath = BuildSettings.OutputDirectory + $"{agentRuntime}/DirectTestAgent.exe";

        if (!System.IO.File.Exists(executablePath))
            throw new FileNotFoundException($"File not found: {executablePath}");

        Console.WriteLine($"Trying to run {executablePath} with arguments {arguments}");

        return BuildSettings.Context.StartProcess(executablePath, new ProcessSettings()
        {
            Arguments = arguments,
            WorkingDirectory = BuildSettings.OutputDirectory
        });
    }
}

//////////////////////////////////////////////////////////////////////
// EXECUTION
//////////////////////////////////////////////////////////////////////

Build.Run()
