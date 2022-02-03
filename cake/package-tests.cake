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

static PackageTest Net35Test = new PackageTest(
    "Run mock-assembly.dll under .NET 3.5",
    "net35/mock-assembly.dll",
    MockAssemblyExpectedResult(1));

static PackageTest Net35X86Test = new PackageTest(
    "Run mock-assembly-x86.dll under .NET 3.5",
    "net35/mock-assembly-x86.dll",
    MockAssemblyExpectedResult(1));

static PackageTest Net40Test = new PackageTest(
    "Run mock-assembly.dll under .NET 4.x",
    "net40/mock-assembly.dll",
    MockAssemblyExpectedResult(1));

static PackageTest Net40X86Test = new PackageTest(
    "Run mock-assembly-x86.dll under .NET 4.x",
    "net40/mock-assembly-x86.dll",
    MockAssemblyExpectedResult(1));

static PackageTest Net35PlusNet40Test = new PackageTest(
    "Run both copies of mock-assembly together",
    "net35/mock-assembly.dll net40/mock-assembly.dll",
    MockAssemblyExpectedResult(2));

static PackageTest Net60Test = new PackageTest(
    "Run mock-assembly.dll under .NET 6.0",
    "net6.0/mock-assembly.dll",
    MockAssemblyExpectedResult(1));

static PackageTest Net50Test = new PackageTest(
    "Run mock-assembly.dll under .NET 5.0",
    "net5.0/mock-assembly.dll",
    MockAssemblyExpectedResult(1));

static PackageTest NetCore31Test = new PackageTest(
    "Run mock-assembly.dll under .NET Core 3.1",
    "netcoreapp3.1/mock-assembly.dll",
    MockAssemblyExpectedResult(1));

static PackageTest NetCore31X86Test = new PackageTest(
    "Run mock-assembly-x86.dll under .NET Core 3.1",
    "netcoreapp3.1/mock-assembly-x86.dll",
    MockAssemblyExpectedResult(1));

static PackageTest NetCore21Test = new PackageTest(
        "Run mock-assembly.dll targeting .NET Core 2.1",
        "netcoreapp2.1/mock-assembly.dll",
    MockAssemblyExpectedResult(1));

static PackageTest NetCore21X86Test = new PackageTest(
    "Run mock-assembly-x86.dll under .NET Core 2.1",
    "netcoreapp2.1/mock-assembly-x86.dll",
    MockAssemblyExpectedResult(1));

static PackageTest NetCore21PlusNetCore31Test = new PackageTest(
    "Run two copies of mock-assembly together",
    "netcoreapp2.1/mock-assembly.dll netcoreapp3.1/mock-assembly.dll",
    MockAssemblyExpectedResult(2));

static PackageTest NetCore21PlusNetCore31PlusNet50PlusNet60Test = new PackageTest(
    "Run four copies of mock-assembly together",
    "netcoreapp2.1/mock-assembly.dll netcoreapp3.1/mock-assembly.dll net5.0/mock-assembly.dll net6.0/mock-assembly.dll",
    MockAssemblyExpectedResult(4));

static PackageTest Net40PlusNet60Test = new PackageTest(
    "Run mock-assembly under .Net Framework 4.0 and .Net 6.0 together",
    "net40/mock-assembly.dll net6.0/mock-assembly.dll",
    MockAssemblyExpectedResult(2));

static PackageTest NUnitProjectTest;
NUnitProjectTest = new PackageTest(
    "Run project with both copies of mock-assembly",
    $"../../NetFXTests.nunit --config={Configuration}",
    MockAssemblyExpectedResult(2));

// Representation of a single test to be run against a pre-built package.
public struct PackageTest
{
    public string Description;
    public string Arguments;
    public ExpectedResult ExpectedResult;

    public PackageTest(string description, string arguments, ExpectedResult expectedResult)
    {
        Description = description;
        Arguments = arguments;
        ExpectedResult = expectedResult;
    }
}

