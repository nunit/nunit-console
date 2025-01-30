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

StandardRunnerTests.Add(new PackageTest(
    1, "Net462Test",
    "Run mock-assembly.dll under .NET 4.6.2",
    "testdata/net462/mock-assembly.dll --trace:Debug",
    new MockAssemblyExpectedResult("net-4.6.2")));

AddToBothLists(new PackageTest(
    1, "Net80Test",
    "Run mock-assembly.dll under .NET 8.0",
    "testdata/net8.0/mock-assembly.dll",
    new MockAssemblyExpectedResult("netcore-8.0")));

AddToBothLists(new PackageTest(
    1, "Net70Test",
    "Run mock-assembly.dll under .NET 7.0",
    "testdata/net7.0/mock-assembly.dll",
    new MockAssemblyExpectedResult("netcore-7.0")));

AddToBothLists(new PackageTest(
    1, "Net60Test",
    "Run mock-assembly.dll under .NET 6.0",
    "testdata/net6.0/mock-assembly.dll",
    new MockAssemblyExpectedResult("netcore-6.0")));

AddToBothLists(new PackageTest(
    1, "NetCore31Test",
    "Run mock-assembly.dll under .NET Core 3.1",
    "testdata/netcoreapp3.1/mock-assembly.dll",
    new MockAssemblyExpectedResult("netcore-3.1")));

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
StandardRunnerTests.Add(new PackageTest(
    1, "Net462X86Test",
    "Run mock-assembly-x86.dll under .NET 4.6.2",
    "testdata/net462/mock-assembly-x86.dll",
    new MockAssemblyX86ExpectedResult("net-4.6.2")));

if (dotnetX86Available)
{
    // TODO: Make tests run on all build platforms
    bool onAppVeyor = BuildSystem.IsRunningOnAppVeyor;
    bool onGitHubActions = BuildSystem.IsRunningOnGitHubActions;

    if (!onAppVeyor)
        StandardRunnerTests.Add(new PackageTest(
            1, "Net80X86Test",
            "Run mock-assembly-x86.dll under .NET 8.0",
            "testdata/net8.0/mock-assembly-x86.dll",
            new MockAssemblyX86ExpectedResult("netcore-8.0")));

    if (!onAppVeyor && !onGitHubActions)
        StandardRunnerTests.Add(new PackageTest(
            1, "Net70X86Test",
            "Run mock-assembly-x86.dll under .NET 7.0",
            "testdata/net7.0/mock-assembly-x86.dll",
            new MockAssemblyX86ExpectedResult("netcore-7.0")));

    StandardRunnerTests.Add(new PackageTest(
        1, "Net60X86Test",
        "Run mock-assembly-x86.dll under .NET 6.0",
        "testdata/net6.0/mock-assembly-x86.dll",
        new MockAssemblyX86ExpectedResult("netcore-6.0")));

    if (!onAppVeyor && !onGitHubActions)
        StandardRunnerTests.Add(new PackageTest(
            1, "NetCore31X86Test",
            "Run mock-assembly-x86.dll under .NET Core 3.1",
            "testdata/netcoreapp3.1/mock-assembly-x86.dll",
            new MockAssemblyX86ExpectedResult("netcore-3.1")));
}

//////////////////////////////////////////////////////////////////////
// RUN MULTIPLE COPIES OF MOCK-ASSEMBLY
//////////////////////////////////////////////////////////////////////

StandardRunnerTests.Add(new PackageTest(
    1, "Net462PlusNet462Test",
    "Run two copies of mock-assembly together",
    "testdata/net462/mock-assembly.dll testdata/net462/mock-assembly.dll",
    new MockAssemblyExpectedResult("net-4.6.2", "net-4.6.2")));

StandardRunnerTests.Add(new PackageTest(
    1, "Net60PlusNet80Test",
    "Run mock-assembly under .NET6.0 and 8.0 together",
    "testdata/net6.0/mock-assembly.dll testdata/net8.0/mock-assembly.dll",
    new MockAssemblyExpectedResult("netcore-6.0", "netcore-8.0")));

StandardRunnerTests.Add(new PackageTest(
    1, "Net462PlusNet60Test",
    "Run mock-assembly under .Net Framework 4.6.2 and .Net 6.0 together",
    "testdata/net462/mock-assembly.dll testdata/net6.0/mock-assembly.dll",
    new MockAssemblyExpectedResult("net-4.6.2", "netcore-6.0")));

//////////////////////////////////////////////////////////////////////
// TEST OLDER VERSIONS OF NUNIT SWITCHING API IF NEEDED
//////////////////////////////////////////////////////////////////////

StandardRunnerTests.Add(new PackageTest(
    1, "NUnit30Test",
    "Run a test under NUnit 3.0 using 2009 API",
    "testdata/NUnit3.0/net462/NUnit3.0.dll",
    new ExpectedResult("Passed")
    {
        Assemblies = new[] { new ExpectedAssemblyResult("NUnit3.0.dll", "net462") }
    }));

StandardRunnerTests.Add(new PackageTest(
    1, "NUnit301Test",
    "Run a test under NUnit 3.0.1 using 2009 API",
    "testdata/NUnit3.0.1/net462/NUnit3.0.1.dll",
    new ExpectedResult("Passed")
    {
        Assemblies = new[] { new ExpectedAssemblyResult("NUnit3.0.1.dll", "net462") }
    }));

StandardRunnerTests.Add(new PackageTest(
    1, "NUnit32Test",
    "Run a test under NUnit 3.2 using 20018 API",
    "testdata/NUnit3.2/net462/NUnit3.2.dll",
    new ExpectedResult("Passed")
    {
        Assemblies = new[] { new ExpectedAssemblyResult("NUnit3.2.dll", "net462") }
    }));

StandardRunnerTests.Add(new PackageTest(
    1, "NUnit310Test",
    "Run a test under NUnit 3.10 using 2018 API",
    "testdata/NUnit3.10/net462/NUnit3.10.dll",
    new ExpectedResult("Passed")
    {
        Assemblies = new[] { new ExpectedAssemblyResult("NUnit3.10.dll", "net462") }
    }));

//////////////////////////////////////////////////////////////////////
// ASP.NETCORE TESTS
//////////////////////////////////////////////////////////////////////

AddToBothLists(new PackageTest(
    1, "Net60AspNetCoreTest", "Run test using AspNetCore targeting .NET 6.0",
    "testdata/net6.0/aspnetcore-test.dll", new ExpectedResult("Passed")
    {
        Total = 2,
        Passed = 2,
        Failed = 0,
        Warnings = 0,
        Inconclusive = 0,
        Skipped = 0,
        Assemblies = new ExpectedAssemblyResult[] { new ExpectedAssemblyResult("aspnetcore-test.dll", "netcore-6.0") }
    }));

AddToBothLists(new PackageTest(
    1, "Net80AspNetCoreTest", "Run test using AspNetCore targeting .NET 8.0",
    "testdata/net8.0/aspnetcore-test.dll", new ExpectedResult("Passed")
    {
        Total = 2,
        Passed = 2,
        Failed = 0,
        Warnings = 0,
        Inconclusive = 0,
        Skipped = 0,
        Assemblies = new ExpectedAssemblyResult[] { new ExpectedAssemblyResult("aspnetcore-test.dll", "netcore-8.0") }
    }));

//////////////////////////////////////////////////////////////////////
// WINDOWS FORMS TESTS
//////////////////////////////////////////////////////////////////////

AddToBothLists(new PackageTest(
    1, "Net60WindowsFormsTest", "Run test using windows forms under .NET 6.0",
    "testdata/net6.0-windows/windows-test.dll", new ExpectedResult("Passed")
    {
        Total = 2,
        Passed = 2,
        Failed = 0,
        Warnings = 0,
        Inconclusive = 0,
        Skipped = 0,
        Assemblies = new ExpectedAssemblyResult[] { new ExpectedAssemblyResult("windows-test.dll", "netcore-6.0") }
    }));

// Runs under Net80 runner but not NetCore
StandardRunnerTests.Add(new PackageTest(
    1, "Net80WindowsFormsTest", "Run test using windows forms under .NET 8.0",
    "testdata/net8.0-windows/windows-test.dll", new ExpectedResult("Passed")
    {
        Total = 2,
        Passed = 2,
        Failed = 0,
        Warnings = 0,
        Inconclusive = 0,
        Skipped = 0,
        Assemblies = new ExpectedAssemblyResult[] { new ExpectedAssemblyResult("windows-test.dll", "netcore-8.0") }
    }));

//////////////////////////////////////////////////////////////////////
// WPF TESTS
//////////////////////////////////////////////////////////////////////

AddToBothLists(new PackageTest(
    1, "Net60WPFTest", "Run test using WPF under .NET 6.0",
    "testdata/net6.0-windows/WpfTest.dll --trace=Debug",
    new ExpectedResult("Passed") { Assemblies = new[] { new ExpectedAssemblyResult("WpfTest.dll", "netcore-6.0") } }));

AddToBothLists(new PackageTest(
    1, "Net80WPFTest", "Run test using WPF under .NET 8.0",
    "testdata/net8.0-windows/WpfTest.dll --trace=Debug",
    new ExpectedResult("Passed") { Assemblies = new[] { new ExpectedAssemblyResult("WpfTest.dll", "netcore-8.0") } }));

//////////////////////////////////////////////////////////////////////
// RUN TESTS USING EACH OF OUR EXTENSIONS
//////////////////////////////////////////////////////////////////////

// TODO: Add back extension tests after latest changes to ExtensionManager
// are ported. Most extensions will require an update to work under V4.

//// NUnit Project Loader Tests
//StandardRunnerTests.Add(new PackageTest(
//    1, "NUnitProjectTest",
//    "Run NUnit project with mock-assembly.dll built for .NET 4.6.2 and 6.0",
//    "../../MixedTests.nunit --config=Release",
//    new MockAssemblyExpectedResult("net-4.6.2", "net-6.0"),
//    KnownExtensions.NUnitProjectLoader));

//NetCoreRunnerTests.Add(new PackageTest(
//    1, "NUnitProjectTest",
//    "Run NUnit project with mock-assembly.dll built for .NET 6.0 and 8.0",
//    "../../NetCoreTests.nunit --config=Release",
//    new MockAssemblyExpectedResult("netcore-6.0", "netcore-8.0"),
//    KnownExtensions.NUnitProjectLoader));

//// V2 Result Writer Test
//StandardRunnerTests.Add(new PackageTest(
//    1, "V2ResultWriterTest",
//    "Run mock-assembly under .NET 4.6.2 and produce V2 output",
//    "testdata/net462/mock-assembly.dll --result=TestResult.xml --result=NUnit2TestResult.xml;format=nunit2",
//    new MockAssemblyExpectedResult("net-4.6.2"),
//    KnownExtensions.NUnitV2ResultWriter));

//StandardRunnerTests.Add(new PackageTest(
//    1, "V2ResultWriterTest",
//    "Run mock-assembly under .NET 6.0 and produce V2 output",
//    "testdata/net6.0/mock-assembly.dll --result=TestResult.xml --result=NUnit2TestResult.xml;format=nunit2",
//    new MockAssemblyExpectedResult("netcore-6.0"),
//    KnownExtensions.NUnitV2ResultWriter));

//// VS Project Loader Tests
//StandardRunnerTests.Add(new PackageTest(
//    1, "VSProjectLoaderTest_Project",
//    "Run mock-assembly using the .csproj file",
//    "../../src/TestData/mock-assembly/mock-assembly.csproj --config=Release",
//    new MockAssemblyExpectedResult("net462", "netcore-3.1", "netcore-6.0", "netcore-7.0", "netcore-8.0"),
//    KnownExtensions.VSProjectLoader));

//StandardRunnerTests.Add(new PackageTest(
//    1, "VSProjectLoaderTest_Solution",
//    "Run mock-assembly using the .sln file",
//    "../../src/TestData/TestData.sln --config=Release --trace=Debug",
//    new ExpectedResult("Failed")
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
//    KnownExtensions.VSProjectLoader));

//// TeamCity Event Listener Test
//StandardRunnerTests.Add(new PackageTest(
//    1, "TeamCityListenerTest",
//    "Run mock-assembly with --teamcity enabled",
//    "testdata/net462/mock-assembly.dll --teamcity",
//    new MockAssemblyExpectedResult("net-4.6.2"),
//    KnownExtensions.TeamCityEventListener));

//// V2 Framework Driver Tests
//StandardRunnerTests.Add(new PackageTest(
//    1, "V2FrameworkDriverTest",
//    "Run mock-assembly-v2 using the V2 Driver in process",
//    "v2-tests/net462/mock-assembly-v2.dll",
//    new ExpectedResult("Failed")
//    {
//        Total = 28,
//        Passed = 18,
//        Failed = 5,
//        Warnings = 0,
//        Inconclusive = 1,
//        Skipped = 4,
//        Assemblies = new ExpectedAssemblyResult[] { new ExpectedAssemblyResult("mock-assembly-v2.dll", "net-4.6.2") }
//    },
//    KnownExtensions.NUnitV2Driver));

//StandardRunnerTests.Add(new PackageTest(
//    1, "V2FrameworkDriverTest",
//    "Run mock-assembly-v2 using the V2 Driver out of process",
//    "v2-tests/net462/mock-assembly-v2.dll --list-extensions",
//    new ExpectedResult("Failed")
//    {
//        Total = 28,
//        Passed = 18,
//        Failed = 5,
//        Warnings = 0,
//        Inconclusive = 1,
//        Skipped = 4,
//        Assemblies = new ExpectedAssemblyResult[] { new ExpectedAssemblyResult("mock-assembly-v2.dll", "net-4.6.2") }
//    },
//    KnownExtensions.NUnitV2Driver));

//////////////////////////////////////////////////////////////////////
// SPECIAL CASES
//////////////////////////////////////////////////////////////////////

StandardRunnerTests.Add(new PackageTest(
    1, "InvalidTestNameTest_Net462",
    "Ensure we handle invalid test names correctly under .NET 4.6.2",
    "testdata/net462/InvalidTestNames.dll --trace:Debug",
    new ExpectedResult("Passed")
    {
        Assemblies = new ExpectedAssemblyResult[]
        {
            new ExpectedAssemblyResult("InvalidTestNames.dll", "net-4.6.2")
        }
    }));

AddToBothLists(new PackageTest(
    1, "InvalidTestNameTest_Net60",
    "Ensure we handle invalid test names correctly under .NET 6.0",
    "testdata/net6.0/InvalidTestNames.dll --trace:Debug",
    new ExpectedResult("Passed")
    {
        Assemblies = new ExpectedAssemblyResult[]
        {
            new ExpectedAssemblyResult("InvalidTestNames.dll", "netcore-6.0")
        }
    }));

AddToBothLists(new PackageTest(
    1, "InvalidTestNameTest_Net80",
    "Ensure we handle invalid test names correctly under .NET 8.0",
    "testdata/net8.0/InvalidTestNames.dll --trace:Debug",
    new ExpectedResult("Passed")
    {
        Assemblies = new ExpectedAssemblyResult[]
        {
            new ExpectedAssemblyResult("InvalidTestNames.dll", "netcore-8.0")
        }
    }));

AddToBothLists(new PackageTest(
    1, "AppContextBaseDirectory_NET80",
    "Test Setting the BaseDirectory to match test assembly location under .NET 8.0",
    "testdata/net8.0/AppContextTest.dll",
    new ExpectedResult("Passed")
    {
        Assemblies = new ExpectedAssemblyResult[] { new ExpectedAssemblyResult("AppContextTest.dll", "netcore-8.0") }
    }));
