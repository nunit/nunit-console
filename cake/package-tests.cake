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

// Tests using NUnit 3
static PackageTest Net35Test = new PackageTest(
    "Net35Test",
    "Run mock-assembly.dll under .NET 3.5",
    "net35/mock-assembly.dll",
    MockAssemblyExpectedResult(1));

static PackageTest Net35X86Test = new PackageTest(
    "Net35X86Test",
    "Run mock-assembly-x86.dll under .NET 3.5",
    "net35/mock-assembly-x86.dll",
    MockAssemblyExpectedResult(1));

static PackageTest Net462Test = new PackageTest(
    "Net462Test",
    "Run mock-assembly.dll under .NET 4.6.2",
    "net462/mock-assembly.dll --trace:Debug",
    MockAssemblyExpectedResult(1));

static PackageTest Net462X86Test = new PackageTest(
    "Net462X86Test",
    "Run mock-assembly-x86.dll under .NET 4.6.2",
    "net462/mock-assembly-x86.dll",
    MockAssemblyExpectedResult(1));

static PackageTest Net35PlusNet462Test = new PackageTest(
    "Net35PlusNet462Test",
    "Run both copies of mock-assembly together",
    "net35/mock-assembly.dll net462/mock-assembly.dll",
    MockAssemblyExpectedResult(2));

static PackageTest Net60Test = new PackageTest(
    "Net60Test",
    "Run mock-assembly.dll under .NET 6.0",
    "net6.0/mock-assembly.dll",
    MockAssemblyExpectedResult(1));

static PackageTest Net50Test = new PackageTest(
    "Net50Test",
    "Run mock-assembly.dll under .NET 5.0",
    "net5.0/mock-assembly.dll",
    MockAssemblyExpectedResult(1));

static PackageTest Net50PlusNet60Test = new PackageTest(
    "Net50PlusNet60Test",
    "Run mock-assembly under .Net 5.0 and .Net 6.0 together",
    "net5.0/mock-assembly.dll net6.0/mock-assembly.dll",
    MockAssemblyExpectedResult(2));
    
static PackageTest NetCore31Test = new PackageTest(
    "NetCore31Test",
    "Run mock-assembly.dll under .NET Core 3.1",
    "netcoreapp3.1/mock-assembly.dll",
    MockAssemblyExpectedResult(1));

static PackageTest NetCore31X86Test = new PackageTest(
    "NetCore31X86Test",
    "Run mock-assembly-x86.dll under .NET Core 3.1",
    "netcoreapp3.1/mock-assembly-x86.dll",
    MockAssemblyExpectedResult(1));

static PackageTest NetCore21Test = new PackageTest(
    "NetCore21Test",
    "Run mock-assembly.dll targeting .NET Core 2.1",
    "netcoreapp2.1/mock-assembly.dll",
    MockAssemblyExpectedResult(1));

static PackageTest NetCore21X86Test = new PackageTest(
    "NetCore21X86Test",
    "Run mock-assembly-x86.dll under .NET Core 2.1",
    "netcoreapp2.1/mock-assembly-x86.dll",
    MockAssemblyExpectedResult(1));

static PackageTest NetCore21PlusNetCore31Test = new PackageTest(
    "NetCore21PlusNetCore31Test",
    "Run two copies of mock-assembly together",
    "netcoreapp2.1/mock-assembly.dll netcoreapp3.1/mock-assembly.dll",
    MockAssemblyExpectedResult(2));

static PackageTest NetCore21PlusNetCore31PlusNet50PlusNet60Test = new PackageTest(
    "NetCore21PlusNetCore31PlusNet50PlusNet60Test",
    "Run four copies of mock-assembly together",
    "netcoreapp2.1/mock-assembly.dll netcoreapp3.1/mock-assembly.dll net5.0/mock-assembly.dll net6.0/mock-assembly.dll",
    MockAssemblyExpectedResult(4));

static PackageTest Net462PlusNet60Test = new PackageTest(
    "Net462PlusNet60Test",
    "Run mock-assembly under .Net Framework 4.6.2 and .Net 6.0 together",
    "net462/mock-assembly.dll net6.0/mock-assembly.dll",
    MockAssemblyExpectedResult(2));

static PackageTest NUnitProjectTest;
NUnitProjectTest = new PackageTest(
    "NUnitProjectTest",
    "Run project with both copies of mock-assembly",
    $"../../NetFXTests.nunit --config={Configuration}",
    MockAssemblyExpectedResult(2));

// Tests using NUnit 4
static PackageTest Net45PlusNet60NUnit4Test = new PackageTest(
    "Net45PlusNet60Test",
    "Run mock-assembly-nunit4 under .Net Framework 4.5 and .Net 6.0 together",
    "net45/mock-assembly-nunit4.dll net6.0/mock-assembly-nunit4.dll",
    MockAssemblyExpectedResult(2));

static PackageTest Net45NUnit4Test = new PackageTest(
    "Net45NUnit4Test",
    "Run mock-assembly-nunit4.dll under .NET 4.5",
    "net45/mock-assembly-nunit4.dll",
    MockAssemblyExpectedResult(1));

static PackageTest Net60NUnit4Test = new PackageTest(
    "Net60NUnit4Test",
    "Run mock-assembly-nunit4.dll under .NET 6.0",
    "net6.0/mock-assembly-nunit4.dll",
    MockAssemblyExpectedResult(1));

static PackageTest Net50NUnit4Test = new PackageTest(
    "Net50NUnit4Test",
    "Run mock-assembly-nunit4.dll under .NET 5.0",
    "net5.0/mock-assembly-nunit4.dll",
    MockAssemblyExpectedResult(1));

static PackageTest NetCore31NUnit4Test = new PackageTest(
    "NetCore31NUnit4Test",
    "Run mock-assembly-nunit4.dll under .NET Core 3.1",
    "netcoreapp3.1/mock-assembly-nunit4.dll",
    MockAssemblyExpectedResult(1));

// Representation of a single test to be run against a pre-built package.
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

