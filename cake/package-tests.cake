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
    "Net35Test",
    "Run mock-assembly.dll under .NET 3.5",
    "net35/mock-assembly.dll",
    MockAssemblyExpectedResult(1));

static PackageTest Net35X86Test = new PackageTest(
    "Net35X86Test",
    "Run mock-assembly-x86.dll under .NET 3.5",
    "net35/mock-assembly-x86.dll",
    MockAssemblyExpectedResult(1));

static PackageTest Net40Test = new PackageTest(
    "Net40Test",
    "Run mock-assembly.dll under .NET 4.x",
    "net40/mock-assembly.dll",
    MockAssemblyExpectedResult(1));

static PackageTest Net40X86Test = new PackageTest(
    "Net40X86Test",
    "Run mock-assembly-x86.dll under .NET 4.x",
    "net40/mock-assembly-x86.dll",
    MockAssemblyExpectedResult(1));

static PackageTest Net35PlusNet40Test = new PackageTest(
    "Net35PlusNet40Test",
    "Run both copies of mock-assembly together",
    "net35/mock-assembly.dll net40/mock-assembly.dll",
    MockAssemblyExpectedResult(2));

static PackageTest Net80Test = new PackageTest(
    "Net80Test",
    "Run mock-assembly.dll under .NET 8.0",
    "net8.0/mock-assembly.dll",
    MockAssemblyExpectedResult(1));

static PackageTest Net80X86Test = new PackageTest(
    "Net80X86Test",
    "Run mock-assembly-x86.dll under .NET 8.0",
    "net8.0/mock-assembly-x86.dll",
    MockAssemblyExpectedResult(1));

static PackageTest Net70Test = new PackageTest(
    "Net70Test",
    "Run mock-assembly.dll under .NET 7.0",
    "net7.0/mock-assembly.dll",
    MockAssemblyExpectedResult(1));

static PackageTest Net70X86Test = new PackageTest(
    "Net70X86Test",
    "Run mock-assembly-x86.dll under .NET 7.0",
    "net7.0/mock-assembly-x86.dll",
    MockAssemblyExpectedResult(1));

static PackageTest Net60Test = new PackageTest(
    "Net60Test",
    "Run mock-assembly.dll under .NET 6.0",
    "net6.0/mock-assembly.dll",
    MockAssemblyExpectedResult(1));

static PackageTest Net60X86Test = new PackageTest(
    "Net60X86Test",
    "Run mock-assembly-x86.dll under .NET 6.0",
    "net6.0/mock-assembly-x86.dll",
    MockAssemblyExpectedResult(1));

static PackageTest Net50Test = new PackageTest(
    "Net50Test",
    "Run mock-assembly.dll under .NET 5.0",
    "net5.0/mock-assembly.dll",
    MockAssemblyExpectedResult(1));

static PackageTest Net50X86Test = new PackageTest(
    "Net50X86Test",
    "Run mock-assembly-x86.dll under .NET 5.0",
    "net5.0/mock-assembly-x86.dll",
    MockAssemblyExpectedResult(1));

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

static PackageTest Net50PlusNet60Test = new PackageTest(
    "Net50PlusNet60Test",
    "Run mock-assembly under .NET 5.0 and 6.0 together",
    "net5.0/mock-assembly.dll net6.0/mock-assembly.dll",//" net7.0/mock-assembly.dll net8.0/mock-assembly.dll",
    MockAssemblyExpectedResult(2));

static PackageTest Net40PlusNet60Test = new PackageTest(
    "Net40PlusNet60Test",
    "Run mock-assembly under .Net Framework 4.0 and .Net 6.0 together",
    "net40/mock-assembly.dll net6.0/mock-assembly.dll",
    MockAssemblyExpectedResult(2));

static PackageTest NUnitProjectTest;
NUnitProjectTest = new PackageTest(
    "NUnitProjectTest",
    "Run project with both copies of mock-assembly",
    $"../../NetFXTests.nunit --config={Configuration}",
    MockAssemblyExpectedResult(2));

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

