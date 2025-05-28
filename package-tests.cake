// Tests run for all runner packages except NETCORE runner
var StandardRunnerTests = new List<PackageTest>();

// Tests run for the NETCORE runner package
var NetCoreRunnerTests = new List<PackageTest>();

// Method for adding to both lists
void AddToBothLists(PackageTest test)
{
    StandardRunnerTests.Add(test);
    NetCoreRunnerTests.Add(test);
}

public static class Extensions
{
    // Extensions used in tests, with version specified.
    public static ExtensionSpecifier NUnitV2Driver = new ExtensionSpecifier(
        "NUnit.Extension.NUnitV2Driver", "nunit-extension-nunit-v2-driver", "3.9.0");
    public static ExtensionSpecifier NUnitProjectLoader = new ExtensionSpecifier(
        "NUnit.Extension.NUnitProjectLoader", "nunit-extension-nunit-project-loader", "3.8.0");
    public static ExtensionSpecifier VSProjectLoader = new ExtensionSpecifier(
        "NUnit.Extension.VSProjectLoader", "nunit-extension-vs-project-loader", "3.9.0");
    public static ExtensionSpecifier NUnitV2ResultWriter = new ExtensionSpecifier(
        "NUnit.Extension.NUnitV2ResultWriter", "nunit-extension-nunit-v2-result-writer", "3.8.0");
    public static ExtensionSpecifier TeamCityEventListener = new ExtensionSpecifier(
        "NUnit.Extension.TeamCityEventListener", "nunit-extension-teamcity-event-listener", "1.0.9");
}

//////////////////////////////////////////////////////////////////////
// RUN MOCK-ASSEMBLY UNDER EACH RUNTIME
//////////////////////////////////////////////////////////////////////

class MockAssemblyExpectedResult : ExpectedResult
{
    public MockAssemblyExpectedResult(params string[] runtimes) : base("Failed")
    {
        int nCopies = runtimes.Length;
        Total = 37 * nCopies;
        Passed = 23 * nCopies;
        Failed = 5 * nCopies;
        Warnings = 1 * nCopies;
        Inconclusive = 1 * nCopies;
        Skipped = 7 * nCopies;
        Assemblies = new ExpectedAssemblyResult[nCopies];
        for (int i = 0; i < nCopies; i++)
            Assemblies[i] = new ExpectedAssemblyResult("mock-assembly.dll", runtimes[i]);
    }
}

StandardRunnerTests.Add(new PackageTest(1, "Net462Test")
{
    Description = "Run mock-assembly.dll under .NET 4.6.2",
    Arguments = "testdata/net462/mock-assembly.dll",
    ExpectedResult = new MockAssemblyExpectedResult("net-4.6.2")
});

AddToBothLists(new PackageTest(1, "Net80Test")
{
    Description = "Run mock-assembly.dll under .NET 8.0",
    Arguments = "testdata/net8.0/mock-assembly.dll",
    ExpectedResult = new MockAssemblyExpectedResult("netcore-8.0")
});

AddToBothLists(new PackageTest(1, "Net70Test")
{
    Description = "Run mock-assembly.dll under .NET 7.0",
    Arguments = "testdata/net7.0/mock-assembly.dll",
    ExpectedResult = new MockAssemblyExpectedResult("netcore-7.0")
});

AddToBothLists(new PackageTest(1, "Net60Test")
{
    Description = "Run mock-assembly.dll under .NET 6.0",
    Arguments = "testdata/net6.0/mock-assembly.dll",
    ExpectedResult = new MockAssemblyExpectedResult("netcore-6.0")
});

AddToBothLists(new PackageTest(1, "NetCore31Test")
{
    Description = "Run mock-assembly.dll under .NET Core 3.1",
    Arguments = "testdata/netcoreapp3.1/mock-assembly.dll",
    ExpectedResult = new MockAssemblyExpectedResult("netcore-3.1")
});

//////////////////////////////////////////////////////////////////////
// RUN MOCK-ASSEMBLY-X86 UNDER EACH RUNTIME
//////////////////////////////////////////////////////////////////////

const string DOTNET_EXE_X86 = @"C:\Program Files (x86)\dotnet\dotnet.exe";
// TODO: Remove the limitation to Windows
bool dotnetX86Available = IsRunningOnWindows() && System.IO.File.Exists(DOTNET_EXE_X86);

class MockAssemblyX86ExpectedResult : MockAssemblyExpectedResult
{
    public MockAssemblyX86ExpectedResult(params string[] runtimes) : base(runtimes)
    {
        for (int i = 0; i < runtimes.Length; i++)
            Assemblies[i] = new ExpectedAssemblyResult("mock-assembly-x86.dll", runtimes[i]);
    }
}

// X86 is always available for .NET Framework
StandardRunnerTests.Add(new PackageTest(1, "Net462X86Test")
{
    Description = "Run mock-assembly-x86.dll under .NET 4.6.2",
    Arguments = "testdata/net462/mock-assembly-x86.dll",
    ExpectedResult = new MockAssemblyX86ExpectedResult("net-4.6.2")
});

if (dotnetX86Available)
{
    // TODO: Make tests run on all build platforms
    bool onAppVeyor = BuildSystem.IsRunningOnAppVeyor;
    bool onGitHubActions = BuildSystem.IsRunningOnGitHubActions;

    StandardRunnerTests.Add(new PackageTest(1, "Net80X86Test")
    {
        Description = "Run mock-assembly-x86.dll under .NET 8.0",
        Arguments = "testdata/net8.0/mock-assembly-x86.dll",
        ExpectedResult = new MockAssemblyX86ExpectedResult("netcore-8.0")
    });

    if (!onGitHubActions)
        StandardRunnerTests.Add(new PackageTest(1, "Net70X86Test")
        {
            Description = "Run mock-assembly-x86.dll under .NET 7.0",
            Arguments = "testdata/net7.0/mock-assembly-x86.dll",
            ExpectedResult = new MockAssemblyX86ExpectedResult("netcore-7.0")
        });

    StandardRunnerTests.Add(new PackageTest(1, "Net60X86Test")
    {
        Description = "Run mock-assembly-x86.dll under .NET 6.0",
        Arguments = "testdata/net6.0/mock-assembly-x86.dll",
        ExpectedResult = new MockAssemblyX86ExpectedResult("netcore-6.0")
    });

    if (!onGitHubActions)
        StandardRunnerTests.Add(new PackageTest(1, "NetCore31X86Test")
        {
            Description = "Run mock-assembly-x86.dll under .NET Core 3.1",
            Arguments = "testdata/netcoreapp3.1/mock-assembly-x86.dll",
            ExpectedResult = new MockAssemblyX86ExpectedResult("netcore-3.1")
        });
}

//////////////////////////////////////////////////////////////////////
// RUN MULTIPLE COPIES OF MOCK-ASSEMBLY
//////////////////////////////////////////////////////////////////////

// TODO: Remove agents arg when current bug is fixed.

StandardRunnerTests.Add(new PackageTest(1, "Net462PlusNet462Test")
{
    Description = "Run two copies of mock-assembly together",
    Arguments = "testdata/net462/mock-assembly.dll testdata/net462/mock-assembly.dll --agents:1",
    ExpectedResult = new MockAssemblyExpectedResult("net-4.6.2", "net-4.6.2")
});

StandardRunnerTests.Add(new PackageTest(1, "Net60PlusNet80Test")
{
    Description = "Run mock-assembly under .NET6.0 and 8.0 together",
    Arguments = "testdata/net6.0/mock-assembly.dll testdata/net8.0/mock-assembly.dll --agents:1",
    ExpectedResult = new MockAssemblyExpectedResult("netcore-6.0", "netcore-8.0")
});

StandardRunnerTests.Add(new PackageTest(1, "Net462PlusNet60Test")
{
    Description = "Run mock-assembly under .Net Framework 4.6.2 and .Net 6.0 together",
    Arguments = "testdata/net462/mock-assembly.dll testdata/net6.0/mock-assembly.dll --agents:1",
    ExpectedResult = new MockAssemblyExpectedResult("net-4.6.2", "netcore-6.0")
});

//////////////////////////////////////////////////////////////////////
// TEST OLDER VERSIONS OF NUNIT SWITCHING API IF NEEDED
//////////////////////////////////////////////////////////////////////

StandardRunnerTests.Add(new PackageTest(1, "NUnit30Test")
{
    Description = "Run a test under NUnit 3.0 using 2009 API",
    Arguments = "testdata/NUnit3.0/net462/NUnit3.0.dll",
    ExpectedResult = new ExpectedResult("Passed")
    {
        Assemblies = new[] { new ExpectedAssemblyResult("NUnit3.0.dll", "net462") }
    }
});

StandardRunnerTests.Add(new PackageTest(1, "NUnit301Test")
{
    Description = "Run a test under NUnit 3.0.1 using 2009 API",
    Arguments = "testdata/NUnit3.0.1/net462/NUnit3.0.1.dll",
    ExpectedResult = new ExpectedResult("Passed")
    {
        Assemblies = new[] { new ExpectedAssemblyResult("NUnit3.0.1.dll", "net462") }
    }
});

StandardRunnerTests.Add(new PackageTest(1, "NUnit32Test")
{
    Description = "Run a test under NUnit 3.2 using 20018 API",
    Arguments = "testdata/NUnit3.2/net462/NUnit3.2.dll",
    ExpectedResult = new ExpectedResult("Passed")
    {
        Assemblies = new[] { new ExpectedAssemblyResult("NUnit3.2.dll", "net462") }
    }
});

StandardRunnerTests.Add(new PackageTest(1, "NUnit310Test")
{
    Description = "Run a test under NUnit 3.10 using 2018 API",
    Arguments = "testdata/NUnit3.10/net462/NUnit3.10.dll",
    ExpectedResult = new ExpectedResult("Passed")
    {
        Assemblies = new[] { new ExpectedAssemblyResult("NUnit3.10.dll", "net462") }
    }
});

//////////////////////////////////////////////////////////////////////
// ASP.NETCORE TESTS
//////////////////////////////////////////////////////////////////////

AddToBothLists(new PackageTest(1, "Net60AspNetCoreTest")
{
    Description = "Run test using AspNetCore targeting .NET 6.0",
    Arguments = "testdata/net6.0/aspnetcore-test.dll",
    ExpectedResult = new ExpectedResult("Passed")
    {
        Total = 2,
        Passed = 2,
        Failed = 0,
        Warnings = 0,
        Inconclusive = 0,
        Skipped = 0,
        Assemblies = new ExpectedAssemblyResult[] { new ExpectedAssemblyResult("aspnetcore-test.dll", "netcore-6.0") }
    }
});

AddToBothLists(new PackageTest(1, "Net80AspNetCoreTest")
{
    Description = "Run test using AspNetCore targeting .NET 8.0",
    Arguments = "testdata/net8.0/aspnetcore-test.dll",
    ExpectedResult = new ExpectedResult("Passed")
    {
        Total = 2,
        Passed = 2,
        Failed = 0,
        Warnings = 0,
        Inconclusive = 0,
        Skipped = 0,
        Assemblies = new ExpectedAssemblyResult[] { new ExpectedAssemblyResult("aspnetcore-test.dll", "netcore-8.0") }
    }
});

//////////////////////////////////////////////////////////////////////
// WINDOWS FORMS TESTS
//////////////////////////////////////////////////////////////////////

AddToBothLists(new PackageTest(1, "Net60WindowsFormsTest")
{
    Description = "Run test using windows forms under .NET 6.0",
    Arguments = "testdata/net6.0-windows/windows-test.dll",
    ExpectedResult = new ExpectedResult("Passed")
    {
        Total = 2,
        Passed = 2,
        Failed = 0,
        Warnings = 0,
        Inconclusive = 0,
        Skipped = 0,
        Assemblies = new ExpectedAssemblyResult[] { new ExpectedAssemblyResult("windows-test.dll", "netcore-6.0") }
    }
});

// Runs under Net80 runner but not NetCore
StandardRunnerTests.Add(new PackageTest(1, "Net80WindowsFormsTest")
{
    Description = "Run test using windows forms under .NET 8.0",
    Arguments = "testdata/net8.0-windows/windows-test.dll",
    ExpectedResult = new ExpectedResult("Passed")
    {
        Total = 2,
        Passed = 2,
        Failed = 0,
        Warnings = 0,
        Inconclusive = 0,
        Skipped = 0,
        Assemblies = new ExpectedAssemblyResult[] { new ExpectedAssemblyResult("windows-test.dll", "netcore-8.0") }
    }
});

//////////////////////////////////////////////////////////////////////
// WPF TESTS
//////////////////////////////////////////////////////////////////////

AddToBothLists(new PackageTest(1, "Net60WPFTest")
{
    Description = "Run test using WPF under .NET 6.0",
    Arguments = "testdata/net6.0-windows/WpfTest.dll",
    ExpectedResult = new ExpectedResult("Passed") { Assemblies = new[] { new ExpectedAssemblyResult("WpfTest.dll", "netcore-6.0") } }
});

AddToBothLists(new PackageTest(1, "Net80WPFTest")
{
    Description = "Run test using WPF under .NET 8.0",
    Arguments = "testdata/net8.0-windows/WpfTest.dll",
    ExpectedResult = new ExpectedResult("Passed") { Assemblies = new[] { new ExpectedAssemblyResult("WpfTest.dll", "netcore-8.0") } }
});

//////////////////////////////////////////////////////////////////////
// TESTS OF EXTENSION LISTING
//////////////////////////////////////////////////////////////////////

AddToBothLists(new PackageTest(1, "NoExtensionsInstalled")
{
    Description = "List Extensions shows none installed",
    Arguments = "--list-extensions",
    ExpectedOutput = new[] { DoesNotContain("Extension:") }
});

AddToBothLists(new PackageTest(1, "ExtensionsInstalledFromAddedDirectory")
{
    Description = "List Extensions shows extension from added directory",
    Arguments = "--extensionDirectory ../../src/TestData/FakeExtensions --list-extensions",
    ExpectedOutput = new[] { Contains("Extension:", exactly: 5) }
});

//////////////////////////////////////////////////////////////////////
// RUN TESTS USING EACH OF OUR EXTENSIONS
//////////////////////////////////////////////////////////////////////

StandardRunnerTests.Add(new PackageTest(1, "FakeEventListenerTest")
{
    Description = "Test that event listener gets all reports",
    Arguments = "testdata/net462/mock-assembly.dll --extensionDirectory ../../src/TestData/FakeExtensions",
    ExpectedResult = new MockAssemblyExpectedResult("netcore-4.6.2")
});

// TODO: Add back extension tests after latest changes to ExtensionManager
// are ported. Most extensions will require an update to work under V4.

//NUnit Project Loader Tests
//StandardRunnerTests.Add(new PackageTest(1, "NUnitProjectTest")
//{
//    Description = "Run NUnit project with mock-assembly.dll built for .NET 4.6.2 and 6.0",
//    Arguments = "../../MixedTests.nunit --config=Release",
//    ExpectedResult = new MockAssemblyExpectedResult("net-4.6.2", "net-6.0"),
//    ExtensionsNeeded = new[] { Extensions.NUnitProjectLoader }
//});

//NetCoreRunnerTests.Add(new PackageTest(1, "NUnitProjectTest")
//{
//    Description = "Run NUnit project with mock-assembly.dll built for .NET 6.0 and 8.0",
//    Arguments = "../../NetCoreTests.nunit --config=Release",
//    ExpectedResult = new MockAssemblyExpectedResult("netcore-6.0", "netcore-8.0"),
//    ExtensionsNeeded = new[] { Extensions.NUnitProjectLoader }
//});

// V2 Result Writer Test
StandardRunnerTests.Add(new PackageTest(1, "V2ResultWriterTest_Net462")
{
    Description = "Run mock-assembly under .NET 4.6.2 and produce V2 output",
    Arguments = "testdata/net462/mock-assembly.dll --result=TestResult.xml --result=NUnit2TestResult.xml;format=nunit2",
    ExpectedResult = new MockAssemblyExpectedResult("net-4.6.2"),
    ExtensionsNeeded = new[] { Extensions.NUnitV2ResultWriter }
});

//StandardRunnerTests.Add(new PackageTest(1, "V2ResultWriterTest_Net60")
//{
//    Description = "Run mock-assembly under .NET 6.0 and produce V2 output",
//    Arguments = "testdata/net6.0/mock-assembly.dll --result=TestResult.xml --result=NUnit2TestResult.xml;format=nunit2",
//    ExpectedResult = new MockAssemblyExpectedResult("netcore-6.0"),
//    ExtensionsNeeded = new[] { Extensions.NUnitV2ResultWriter }
//});

//// VS Project Loader Tests
//StandardRunnerTests.Add(new PackageTest(1, "VSProjectLoaderTest_Project")
//{
//    Description = "Run mock-assembly using the .csproj file",
//    Arguments = "../../src/TestData/mock-assembly/mock-assembly.csproj --config=Release",
//    ExpectedResult = new MockAssemblyExpectedResult("net462", "netcore-3.1", "netcore-6.0", "netcore-7.0", "netcore-8.0"),
//    ExtensionsNeeded = new[] { Extensions.VSProjectLoader }
//});

//StandardRunnerTests.Add(new PackageTest(1, "VSProjectLoaderTest_Solution")
//{
//    Description = "Run mock-assembly using the .sln file",
//    Arguments = "../../src/TestData/TestData.sln --config=Release",
//    ExpectedResult = new ExpectedResult("Failed")
//    {
//        Total = 37 * 5,
//        Passed = 23 * 5,
//        Failed = 5 * 5,
//        Warnings = 1 * 5,
//        Inconclusive = 1 * 5,
//        Skipped = 7 * 5,
//        Assemblies = new ExpectedAssemblyResult[]
//        {
//            new ExpectedAssemblyResult("mock-assembly.dll", "net-4.6.2"),
//            new ExpectedAssemblyResult("mock-assembly.dll", "netcore-3.1"),
//            new ExpectedAssemblyResult("mock-assembly.dll", "netcore-6.0"),
//            new ExpectedAssemblyResult("mock-assembly.dll", "netcore-7.0"),
//            new ExpectedAssemblyResult("mock-assembly.dll", "netcore-8.0"),
//            new ExpectedAssemblyResult("notest-assembly.dll", "net-4.6.2"),
//            new ExpectedAssemblyResult("notest-assembly.dll", "netcore-3.1"),
//            new ExpectedAssemblyResult("notest-assembly.dll", "netstandard-2.0"),
//            new ExpectedAssemblyResult("WpfApp.exe")
//        }
//    },
//    ExtensionsNeeded = new[] { Extensions.VSProjectLoader }
//});

// TeamCity Event Listener Test
StandardRunnerTests.Add(new PackageTest(1, "TeamCityListenerTest")
{
    Description = "Run mock-assembly with --teamcity enabled",
    Arguments = "testdata/net462/mock-assembly.dll --teamcity",
    ExpectedResult = new MockAssemblyExpectedResult("net-4.6.2"),
    ExtensionsNeeded = new[] { Extensions.TeamCityEventListener }
});

//// V2 Framework Driver Tests
//StandardRunnerTests.Add(new PackageTest(1, "V2FrameworkDriverTest")
//{
//    Description = "Run mock-assembly-v2 using the V2 Driver in process",
//    Arguments = "v2-tests/net462/mock-assembly-v2.dll",
//    ExpectedResult = new ExpectedResult("Failed")
//    {
//        Total = 28,
//        Passed = 18,
//        Failed = 5,
//        Warnings = 0,
//        Inconclusive = 1,
//        Skipped = 4,
//        Assemblies = new ExpectedAssemblyResult[] { new ExpectedAssemblyResult("mock-assembly-v2.dll", "net-4.6.2") }
//    },
//    ExtensionsNeeded = new[] { Extensions.NUnitV2Driver }
//});

//StandardRunnerTests.Add(new PackageTest(1, "V2FrameworkDriverTest")
//{
//    Description = "Run mock-assembly-v2 using the V2 Driver out of process",
//    Arguments = "v2-tests/net462/mock-assembly-v2.dll --list-extensions",
//    ExpectedResult = new ExpectedResult("Failed")
//    {
//        Total = 28,
//        Passed = 18,
//        Failed = 5,
//        Warnings = 0,
//        Inconclusive = 1,
//        Skipped = 4,
//        Assemblies = new ExpectedAssemblyResult[] { new ExpectedAssemblyResult("mock-assembly-v2.dll", "net-4.6.2") }
//    },
//    ExtensionsNeeded = new[] { Extensions.NUnitV2Driver }
//});

//////////////////////////////////////////////////////////////////////
// SPECIAL CASES
//////////////////////////////////////////////////////////////////////

StandardRunnerTests.Add(new PackageTest(1, "InvalidTestNameTest_Net462")
{
    Description = "Ensure we handle invalid test names correctly under .NET 4.6.2",
    Arguments = "testdata/net462/InvalidTestNames.dll",
    ExpectedResult = new ExpectedResult("Passed")
    {
        Assemblies = new ExpectedAssemblyResult[]
        {
            new ExpectedAssemblyResult("InvalidTestNames.dll", "net-4.6.2")
        }
    }
});

AddToBothLists(new PackageTest(1, "InvalidTestNameTest_Net60")
{
    Description = "Ensure we handle invalid test names correctly under .NET 6.0",
    Arguments = "testdata/net6.0/InvalidTestNames.dll",
    ExpectedResult = new ExpectedResult("Passed")
    {
        Assemblies = new ExpectedAssemblyResult[]
        {
            new ExpectedAssemblyResult("InvalidTestNames.dll", "netcore-6.0")
        }
    }
});

AddToBothLists(new PackageTest(1, "InvalidTestNameTest_Net80")
{
    Description = "Ensure we handle invalid test names correctly under .NET 8.0",
    Arguments = "testdata/net8.0/InvalidTestNames.dll",
    ExpectedResult = new ExpectedResult("Passed")
    {
        Assemblies = new ExpectedAssemblyResult[]
        {
            new ExpectedAssemblyResult("InvalidTestNames.dll", "netcore-8.0")
        }
    }
});

AddToBothLists(new PackageTest(1, "AppContextBaseDirectory_NET80")
{
    Description = "Test Setting the BaseDirectory to match test assembly location under .NET 8.0",
    Arguments = "testdata/net8.0/AppContextTest.dll",
    ExpectedResult = new ExpectedResult("Passed")
    {
        Assemblies = new ExpectedAssemblyResult[] { new ExpectedAssemblyResult("AppContextTest.dll", "netcore-8.0") }
    }
});

// NOTE: Tests for NUnit.Engine and NUnit.Agent.Core here are quite limited. At this
// point, the main purpose they serve is to demonstrate that we are ABLE to  run
// the tests without using the console runner.
//
// That's because multiple packages are created by this build script and these tests
// really just repetitions of tests we perform for the console runner package.
// When either of these packages is moved to a separate repository, the tests will
// become more meaningful and will then be expanded.

// Tests for NUnit.Engine package
var EngineTests = new List<PackageTest>()
{
    new PackageTest(1, "Net462AgentTest")
    {
        Description = "Run mock-assembly.dll under .NET 4.6.2",
        Arguments = "testdata/net462/mock-assembly.dll",
        ExpectedResult = new MockAssemblyExpectedResult("net-4.6.2")
    },
    new PackageTest(1, "Net462X86AgentTest")
    {
        Description = "Run mock-assembly-x86.dll under .NET 4.6.2",
        Arguments = "testdata/net462/mock-assembly-x86.dll --x86",
        ExpectedResult = new MockAssemblyX86ExpectedResult("net-4.6.2")
    },
    new PackageTest(1, "Net80AgentTest")
    {
        Description = "Run mock-assembly.dll under .NET 8.0",
        Arguments = "testdata/net8.0/mock-assembly.dll",
        ExpectedResult = new MockAssemblyExpectedResult("netcore-8.0")
    }
};

// Tests for NUnit.Agent.Core package
var AgentCoreTests = new List<PackageTest>()
{
    new PackageTest(1, "Net462AgentTest")
    {
        Description = "Run mock-assembly.dll under .NET 4.6.2",
        Arguments = "testdata/net462/mock-assembly.dll",
        ExpectedResult = new MockAssemblyExpectedResult("net-4.6.2")
    },
    new PackageTest(1, "Net80AgentTest")
    {
        Description = "Run mock-assembly.dll under .NET 8.0",
        Arguments = "testdata/net8.0/mock-assembly.dll",
        ExpectedResult = new MockAssemblyExpectedResult("netcore-8.0")
    }
};

