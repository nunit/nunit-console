// Load the additional cake files
#load cake/*.cake

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

var THIS_VERSION = BuildSettings.PackageVersion;
PackageDefinition NUnitExtensibilityApiPackage = new PackageDefinition(
    PackageType.NuGet,
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
    });

PackageDefinition NUnitEngineApiPackage = new PackageDefinition(
    PackageType.NuGet,
    id: "NUnit.Engine.Api",
    source: BuildSettings.SourceDirectory + "NUnitEngine/nunit.engine.api/nunit.engine.api.csproj",
    checks: new PackageCheck[] {
        HasFile("LICENSE.txt"),
        HasDirectory("lib/net462").WithFile("nunit.engine.api.dll"),
        HasDirectory("lib/netstandard2.0").WithFile("nunit.engine.api.dll"),
        HasDependency("NUnit.Extensibility.Api", THIS_VERSION)
    },
    symbols: new PackageCheck[] {
        HasDirectory("lib/net462").WithFile("nunit.engine.api.pdb"),
        HasDirectory("lib/netstandard2.0").WithFile("nunit.engine.api.pdb")
    });

PackageDefinition NUnitCommonPackage = new PackageDefinition(
    PackageType.NuGet,
    id: "NUnit.Common",
    source: BuildSettings.SourceDirectory + "NUnitCommon/nunit.common/nunit.common.csproj",
    checks: new PackageCheck[]
    {
        HasFile("LICENSE.txt"),
        HasDirectory("lib/net462").WithFile("nunit.common.dll"),
        HasDirectory("lib/netstandard2.0").WithFile("nunit.common.dll"),
        HasDependency("NUnit.Engine.Api", THIS_VERSION)
    },
    symbols: new PackageCheck[]
    {
        HasDirectory("lib/net462").WithFile("nunit.common.pdb"),
        HasDirectory("lib/netstandard2.0").WithFile("nunit.common.pdb"),
    });

PackageDefinition NUnitExtensibilityPackage = new PackageDefinition(
    PackageType.NuGet,
    id: "NUnit.Extensibility",
    source: BuildSettings.SourceDirectory + "NUnitCommon/nunit.extensibility/nunit.extensibility.csproj",
    checks: new PackageCheck[]
    {
        HasFile("LICENSE.txt"),
        HasDirectory("lib/net462").WithFile("nunit.extensibility.dll"),
        HasDirectory("lib/netstandard2.0").WithFile("nunit.extensibility.dll"),
        HasDependency("NUnit.Extensibility.Api", THIS_VERSION),
        HasDependency("NUnit.Engine.Api", THIS_VERSION),
        HasDependency("NUnit.Common", THIS_VERSION),
        HasDependency("TestCentric.Metadata", "3.0.3")
    },
    symbols: new PackageCheck[]
    {
        HasDirectory("lib/net462").WithFile("nunit.extensibility.pdb"),
        HasDirectory("lib/netstandard2.0").WithFile("nunit.extensibility.pdb")
    });

PackageDefinition NUnitAgentCorePackage = new PackageDefinition(
    PackageType.NuGet,
    id: "NUnit.Agent.Core",
    source: BuildSettings.SourceDirectory + "NUnitCommon/nunit.agent.core/nunit.agent.core.csproj",
    checks: new PackageCheck[]
    {
        HasFiles("LICENSE.txt"),
        HasDirectory("lib/net462").WithFile("nunit.agent.core.dll" ),
        HasDirectory("lib/net8.0").WithFiles("nunit.agent.core.dll"),
        HasDependency("NUnit.Engine.Api", THIS_VERSION),
        HasDependency("NUnit.Common", THIS_VERSION),
        HasDependency("TestCentric.Metadata", "3.0.3")
    },
    symbols: new PackageCheck[]
    {
        HasDirectory("lib/net462").WithFile("nunit.agent.core.pdb"),
        HasDirectory("lib/net8.0").WithFile("nunit.agent.core.pdb")
    },
    testRunner: new DirectTestAgentRunner(),
    tests: AgentCoreTests);

PackageDefinition NUnitEnginePackage = new PackageDefinition(
    PackageType.NuGet,
    id: "NUnit.Engine",
    source: BuildSettings.SourceDirectory + "NUnitEngine/nunit.engine/nunit.engine.csproj",
    checks: new PackageCheck[]
    {
        HasFiles("LICENSE.txt"),
        HasDirectory("lib/net462").WithFile("nunit.engine.dll"),
        HasDirectory("lib/net8.0").WithFile("nunit.engine.dll"),
        HasDependency("NUnit.Engine.Api", THIS_VERSION),
        HasDependency("NUnit.Common", THIS_VERSION),
        HasDependency("NUnit.Extensibility", THIS_VERSION)
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

PackageDefinition NUnitConsoleRunnerNuGetPackage = new PackageDefinition(
    PackageType.NuGet,
    id: "NUnit.ConsoleRunner",
    source: BuildSettings.NuGetDirectory + "runners/nunit.console-runner.nuspec",
    checks: new PackageCheck[] {
        HasFiles("LICENSE.txt", "NOTICES.txt"),
        HasDirectory("tools").WithFiles(
            "nunit-console.exe", "nunit-console.exe.config", "nunit.engine.dll",
            "nunit.extensibility.dll", "nunit.extensibility.api.dll", "nunit.common.dll",
            "nunit.engine.api.dll", "testcentric.metadata.dll"),
        HasDependency("NUnit.Extension.Net462PluggableAgent", "4.1.0-alpha.3"),
        HasDependency("NUnit.Extension.Net80PluggableAgent", "4.1.0-alpha.4"),
        HasDependency("NUnit.Extension.Net90PluggableAgent", "4.1.0-alpha.3")
    },
    symbols: new PackageCheck[] {
        HasDirectory("tools").WithFiles(
            "nunit.engine.pdb", "nunit.extensibility.pdb", "nunit.extensibility.api.pdb",
            "nunit.common.pdb", "nunit.engine.api.pdb", "nunit-console.pdb"),
    },
    testRunner: new ConsoleRunnerSelfTester(BuildSettings.NuGetTestDirectory
        + $"NUnit.ConsoleRunner.{BuildSettings.PackageVersion}/tools/nunit-console.exe"),
    tests: StandardRunnerTests);

// NOTE: Must follow ConsoleRunner, upon which it depends
PackageDefinition NUnitConsoleNuGetPackage = new PackageDefinition(
    PackageType.NuGet,
    id: "NUnit.Console",
    source: BuildSettings.NuGetDirectory + "runners/nunit.console-runner-with-extensions.nuspec",
    checks: new PackageCheck[] { HasFile("LICENSE.txt") });

PackageDefinition NUnitConsoleRunnerDotNetToolPackage = new PackageDefinition(
    PackageType.Tool,
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
    testRunner: new ConsoleRunnerSelfTester(BuildSettings.PackageTestDirectory + "nunit.exe"),
    tests: NetCoreRunnerTests);

PackageDefinition NUnitConsoleRunnerChocolateyPackage = new PackageDefinition(
    PackageType.Chocolatey,
    id: "nunit-console-runner",
    source: BuildSettings.ChocolateyDirectory + "nunit-console-runner.nuspec",
    checks: new PackageCheck[] {
        HasDirectory("tools").WithFiles(
            "LICENSE.txt", "NOTICES.txt", "VERIFICATION.txt", "nunit-console.exe", "nunit-console.exe.config",
            "nunit.engine.dll", "nunit.extensibility.dll", "nunit.extensibility.api.dll",
            "nunit.common.dll", "nunit.engine.api.dll", "testcentric.metadata.dll"),
        HasDependency("nunit-extension-net462-pluggable-agent", "4.1.0-alpha.3"),
        HasDependency("nunit-extension-net80-pluggable-agent", "4.1.0-alpha.4"),
        HasDependency("nunit-extension-net90-pluggable-agent", "4.1.0-alpha.3")
    },
    testRunner: new ConsoleRunnerSelfTester(BuildSettings.ChocolateyTestDirectory
        + $"nunit-console-runner.{BuildSettings.ChocolateyPackageVersion}/tools/nunit-console.exe"),
    tests: StandardRunnerTests);

// Add all packages to BuildSettings in order they should be build.
// Dependencies must precede all the packages that depend on them.
BuildSettings.Packages.AddRange(new PackageDefinition[] {
    NUnitExtensibilityApiPackage,
    NUnitEngineApiPackage,
    NUnitCommonPackage,
    NUnitExtensibilityPackage,
    NUnitAgentCorePackage,
    NUnitEnginePackage,
    NUnitConsoleRunnerNuGetPackage,
    NUnitConsoleRunnerDotNetToolPackage,
    NUnitConsoleRunnerChocolateyPackage,
    NUnitConsoleNuGetPackage
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
