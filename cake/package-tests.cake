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

// Single Assembly Tests using each agent

PackageTest Net35Test = new PackageTest(
    "Net35Test",
    "Run mock-assembly.dll under .NET 3.5",
    MOCK_ASSEMBLY_PROJECT_BIN_DIR + "net35/mock-assembly.dll",
    MockAssemblyExpectedResult(1));

PackageTest Net462Test = new PackageTest(
    "Net462Test",
    "Run mock-assembly.dll under .NET 4.6.2",
    MOCK_ASSEMBLY_PROJECT_BIN_DIR + "net462/mock-assembly.dll",
    MockAssemblyExpectedResult(1));

PackageTest NetCore21Test = new PackageTest(
    "NetCore21Test",
    "Run mock-assembly.dll targeting .NET Core 2.1",
    MOCK_ASSEMBLY_PROJECT_BIN_DIR + "netcoreapp2.1/mock-assembly.dll",
    MockAssemblyExpectedResult(1));

PackageTest NetCore31Test = new PackageTest(
    "NetCore31Test",
    "Run mock-assembly.dll under .NET Core 3.1",
    MOCK_ASSEMBLY_PROJECT_BIN_DIR + "netcoreapp3.1/mock-assembly.dll",
    MockAssemblyExpectedResult(1));

PackageTest Net50Test = new PackageTest(
    "Net50Test",
    "Run mock-assembly.dll under .NET 5.0",
    MOCK_ASSEMBLY_PROJECT_BIN_DIR + "net5.0/mock-assembly.dll",
    MockAssemblyExpectedResult(1));

PackageTest Net60Test = new PackageTest(
    "Net60Test",
    "Run mock-assembly.dll under .NET 6.0",
    MOCK_ASSEMBLY_PROJECT_BIN_DIR + "net6.0/mock-assembly.dll",
    MockAssemblyExpectedResult(1));

PackageTest Net70Test = new PackageTest(
    "Net70Test",
    "Run mock-assembly.dll under .NET 7.0",
    MOCK_ASSEMBLY_PROJECT_BIN_DIR + "net7.0/mock-assembly.dll",
    MockAssemblyExpectedResult(1));

// X86 Tests

PackageTest Net35X86Test = new PackageTest(
    "Net35X86Test",
    "Run mock-assembly-x86.dll under .NET 3.5",
    MOCK_ASSEMBLY_X86_PROJECT_BIN_DIR + "net35/mock-assembly-x86.dll",
    MockAssemblyExpectedResult(1));

PackageTest Net462X86Test = new PackageTest(
    "Net462X86Test",
    "Run mock-assembly-x86.dll under .NET 4.6.2",
    MOCK_ASSEMBLY_X86_PROJECT_BIN_DIR + "net462/mock-assembly-x86.dll",
    MockAssemblyExpectedResult(1));

PackageTest NetCore31X86Test = new PackageTest(
    "NetCore31X86Test",
    "Run mock-assembly-x86.dll under .NET Core 3.1",
    MOCK_ASSEMBLY_X86_PROJECT_BIN_DIR + "netcoreapp3.1/mock-assembly-x86.dll",
    MockAssemblyExpectedResult(1));

PackageTest NetCore21X86Test = new PackageTest(
    "NetCore21X86Test",
    "Run mock-assembly-x86.dll under .NET Core 2.1",
    MOCK_ASSEMBLY_X86_PROJECT_BIN_DIR + "netcoreapp2.1/mock-assembly-x86.dll",
    MockAssemblyExpectedResult(1));

// Special Test Situations

PackageTest Net60WindowsFormsTest = new PackageTest(
    "Net60WindowsFormsTest",
    "Run test using windows forms under .NET 6.0",
     WINDOWS_TEST_PROJECT_BIN_DIR + "net6.0-windows/windows-test.dll",
    new ExpectedResult("Passed"));

PackageTest Net60AspNetCoreTest = new PackageTest(
    "Net60AspNetCoreTest",
    "Run test using AspNetCore under .NET 6.0",
    ASP_NET_CORE_TEST_PROJECT_BIN_DIR + "net6.0/aspnetcore-test.dll",
    new ExpectedResult("Passed"));

// Multiple Assemblies

PackageTest Net35PlusNet462Test = new PackageTest(
    "Net35PlusNet462Test",
    "Run both copies of mock-assembly together",
    $"{MOCK_ASSEMBLY_PROJECT_BIN_DIR}net35/mock-assembly.dll {MOCK_ASSEMBLY_PROJECT_BIN_DIR}net462/mock-assembly.dll",
    MockAssemblyExpectedResult(2));

PackageTest Net50PlusNet60Test = new PackageTest(
    "Net50PlusNet60Test",
    "Run mock-assembly under .Net 5.0 and .Net 6.0 together",
    $"{MOCK_ASSEMBLY_PROJECT_BIN_DIR}net5.0/mock-assembly.dll {MOCK_ASSEMBLY_PROJECT_BIN_DIR}net6.0/mock-assembly.dll",
    MockAssemblyExpectedResult(2));
    
PackageTest Net462PlusNet60Test = new PackageTest(
    "Net462PlusNet60Test",
    "Run mock-assembly under .Net Framework 4.6.2 and .Net 6.0 together",
    $"{MOCK_ASSEMBLY_PROJECT_BIN_DIR}net462/mock-assembly.dll {MOCK_ASSEMBLY_PROJECT_BIN_DIR}net6.0/mock-assembly.dll",
    MockAssemblyExpectedResult(2));

// NUnitProject Test

PackageTest NUnitProjectTest;
NUnitProjectTest = new PackageTest(
    "NUnitProjectTest",
    "Run project with both copies of mock-assembly",
    $"NetFXTests.nunit --config={Configuration}",
    MockAssemblyExpectedResult(2));

// Tests using NUnit 4

PackageTest Net462NUnit4Test = new PackageTest(
    "Net462NUnit4Test",
    "Run mock-assembly-nunit4.dll under .NET 4.6.2",
    MOCK_ASSEMBLY_NUNIT4_PROJECT_BIN_DIR + "net462/mock-assembly-nunit4.dll",
    MockAssemblyExpectedResult(1));

PackageTest NetCore31NUnit4Test = new PackageTest(
    "NetCore31NUnit4Test",
    "Run mock-assembly-nunit4.dll under .NET Core 3.1",
    MOCK_ASSEMBLY_NUNIT4_PROJECT_BIN_DIR + "netcoreapp3.1/mock-assembly-nunit4.dll",
    MockAssemblyExpectedResult(1));

PackageTest Net50NUnit4Test = new PackageTest(
    "Net50NUnit4Test",
    "Run mock-assembly-nunit4.dll under .NET 5.0",
    MOCK_ASSEMBLY_NUNIT4_PROJECT_BIN_DIR + "net5.0/mock-assembly-nunit4.dll",
    MockAssemblyExpectedResult(1));

PackageTest Net60NUnit4Test = new PackageTest(
    "Net60NUnit4Test",
    "Run mock-assembly-nunit4.dll under .NET 6.0",
    MOCK_ASSEMBLY_NUNIT4_PROJECT_BIN_DIR + "net6.0/mock-assembly-nunit4.dll",
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

