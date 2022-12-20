//////////////////////////////////////////////////////////////////////
/// Representation of a single test to be run against a pre-built package.
//////////////////////////////////////////////////////////////////////

public struct PackageTest
{
    public string Name;
    public string Description;
    public string Arguments;
    public ExpectedResult ExpectedResult;

    public PackageTest(string name, string description, string arguments, ExpectedResult expectedResult)
    {
        Name = name;
        Description = description;
        Arguments = arguments;
        ExpectedResult = expectedResult;
    }
}

//////////////////////////////////////////////////////////////////////
// INDIVIDUAL PACKAGE TEST DEFINITIONS
//////////////////////////////////////////////////////////////////////

static ExpectedResult MockAssemblyExpectedResult(int nCopies = 1) => new ExpectedResult("Failed")
{
    Total = 37 * nCopies,
    Passed = 23 * nCopies,
    Failed = 5 * nCopies,
    Warnings = 1 * nCopies,
    Inconclusive = 1 * nCopies,
    Skipped = 7 * nCopies
};

//Single Assembly Tests using each agent

static PackageTest Net35Test = new PackageTest(
    "Net35Test",
    "Run mock-assembly.dll under .NET 3.5",
    "src/NUnitEngine/mock-assembly/bin/Release/net35/mock-assembly.dll",
    MockAssemblyExpectedResult(1));

static PackageTest Net40Test = new PackageTest(
    "Net40Test",
    "Run mock-assembly.dll under .NET 4.x",
    "src/NUnitEngine/mock-assembly/bin/Release/net462/mock-assembly.dll",
    MockAssemblyExpectedResult(1));

static PackageTest NetCore21Test = new PackageTest(
    "NetCore21Test",
    "Run mock-assembly.dll targeting .NET Core 2.1",
    "src/NUnitEngine/mock-assembly/bin/Release/netcoreapp2.1/mock-assembly.dll",
    MockAssemblyExpectedResult(1));

static PackageTest NetCore31Test = new PackageTest(
    "NetCore31Test",
    "Run mock-assembly.dll under .NET Core 3.1",
    "src/NUnitEngine/mock-assembly/bin/Release/netcoreapp3.1/mock-assembly.dll",
    MockAssemblyExpectedResult(1));

static PackageTest Net50Test = new PackageTest(
    "Net50Test",
    "Run mock-assembly.dll under .NET 5.0",
    "src/NUnitEngine/mock-assembly/bin/Release/net5.0/mock-assembly.dll",
    MockAssemblyExpectedResult(1));

static PackageTest Net60Test = new PackageTest(
    "Net60Test",
    "Run mock-assembly.dll under .NET 6.0",
    "src/NUnitEngine/mock-assembly/bin/Release/net6.0/mock-assembly.dll",
    MockAssemblyExpectedResult(1));

static PackageTest Net70Test = new PackageTest(
    "Net70Test",
    "Run mock-assembly.dll under .NET 7.0",
    "src/NUnitEngine/mock-assembly/bin/Release/net7.0/mock-assembly.dll",
    MockAssemblyExpectedResult(1));

// X86 Tests

static PackageTest Net35X86Test = new PackageTest(
    "Net35X86Test",
    "Run mock-assembly-x86.dll under .NET 3.5",
    "src/NUnitEngine/mock-assembly-x86/bin/Release/net35/mock-assembly-x86.dll",
    MockAssemblyExpectedResult(1));

static PackageTest Net40X86Test = new PackageTest(
    "Net40X86Test",
    "Run mock-assembly-x86.dll under .NET 4.x",
    "src/NUnitEngine/mock-assembly-x86/bin/Release/net462/mock-assembly-x86.dll",
    MockAssemblyExpectedResult(1));

static PackageTest NetCore31X86Test = new PackageTest(
    "NetCore31X86Test",
    "Run mock-assembly-x86.dll under .NET Core 3.1",
    "src/NUnitEngine/mock-assembly-x86/bin/Release/netcoreapp3.1/mock-assembly-x86.dll",
    MockAssemblyExpectedResult(1));

// Special Test Situations

static PackageTest Net60WindowsFormsTest = new PackageTest(
    "Net60WindowsFormsTest",
    "Run test using windows forms under .NET 6.0",
    "src/NUnitEngine/windows-test/bin/Release/net6.0-windows/windows-test.dll",
    new ExpectedResult("Passed"));

static PackageTest Net60AspNetCoreTest = new PackageTest(
    "Net60AspNetCoreTest",
    "Run test using AspNetCore under .NET 6.0",
    "src/NUnitEngine/aspnetcore-test/bin/Release/net6.0/aspnetcore-test.dll",
    new ExpectedResult("Passed"));

// Multiple Assemblies

static PackageTest Net35PlusNet40Test = new PackageTest(
    "Net35PlusNet40Test",
    "Run both copies of mock-assembly together",
    "src/NUnitEngine/mock-assembly/bin/Release/net35/mock-assembly.dll src/NUnitEngine/mock-assembly/bin/Release/net462/mock-assembly.dll",
    MockAssemblyExpectedResult(2));

static PackageTest Net40PlusNet60Test = new PackageTest(
    "Net40PlusNet60Test",
    "Run mock-assembly under .Net Framework 4.0 and .Net 6.0 together",
    "src/NUnitEngine/mock-assembly/bin/Release/net462/mock-assembly.dll src/NUnitEngine/mock-assembly/bin/Release/net6.0/mock-assembly.dll",
    MockAssemblyExpectedResult(2));

static PackageTest Net50PlusNet60Test = new PackageTest(
    "Net50PlusNet60Test",
    "Run mock-assembly under .Net 5.0 and .Net 6.0 together",
    "src/NUnitEngine/mock-assembly/bin/Release/net5.0/mock-assembly.dll src/NUnitEngine/mock-assembly/bin/Release/net6.0/mock-assembly.dll",
    MockAssemblyExpectedResult(2));

// NUnit Project

static PackageTest NUnitProjectTest;
NUnitProjectTest = new PackageTest(
    "NUnitProjectTest",
    "Run project with both copies of mock-assembly",
    $"NetFXTests.nunit --config={Configuration}",
    MockAssemblyExpectedResult(2));

//////////////////////////////////////////////////////////////////////
// LISTS OF PACKAGE TESTS
//////////////////////////////////////////////////////////////////////

// Tests run for all runner packages except NETCORE runner
static List<PackageTest> StandardRunnerTests = new List<PackageTest>
{
    Net35Test,
    Net40Test,
    NetCore21Test,
    NetCore31Test,
    Net50Test,
    Net60Test,
    Net70Test,
    Net35PlusNet40Test,
    Net40PlusNet60Test,
    Net50PlusNet60Test,
    Net35X86Test,
    Net40X86Test,
    Net60AspNetCoreTest
};

// Tests run for the NETCORE runner package
static List<PackageTest> NetCoreRunnerTests = new List<PackageTest>
{
    NetCore21Test,
    NetCore31Test,
    Net50Test,
    Net60Test,
    Net50PlusNet60Test,
    Net60AspNetCoreTest
};
