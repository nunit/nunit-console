// Tests run for all runner packages except NETCORE runner
public static List<PackageTest> StandardRunnerTests = new List<PackageTest>();

//////////////////////////////////////////////////////////////////////
// INDIVIDUAL PACKAGE TEST DEFINITIONS
//////////////////////////////////////////////////////////////////////

public static class PackageTests
{
    // Tests run for all runner packages except NETCORE runner
    public static List<PackageTest> StandardRunnerTests { get; }
    // Tests run for the NETCORE runner package
    public static List<PackageTest> NetCoreRunnerTests { get; }

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
    public PackageTest Net35Test { get; }
    public PackageTest Net462Test { get; }
    public PackageTest NetCore21Test { get; }
    public PackageTest NetCore31Test { get; }
    public PackageTest Net50Test { get; }
    public PackageTest Net60Test { get; }
    public PackageTest Net70Test { get; }
    // X86 Tests
    public PackageTest Net35X86Test { get; }
    public PackageTest Net462X86Test { get; }
    public PackageTest NetCore31X86Test { get; }
    public PackageTest Net60X86Test { get; }
    public PackageTest Net80X86Test { get; }
    // Special Test Situations
    public PackageTest Net60WindowsFormsTest { get; }
    public PackageTest Net60AspNetCoreTest { get; }
    // Multiple Assemblies
    public PackageTest Net35PlusNet462Test { get; }
    public PackageTest Net50PlusNet60Test { get; }
    public PackageTest Net462PlusNet60Test { get; }
    // NUnit Project Test
    public PackageTest NUnitProjectTest { get; }
    // Tests Using NUnit 4 Framework
    public PackageTest Net462NUnit4Test { get; }
    public PackageTest NetCore31NUnit4Test { get; }
    public PackageTest Net50NUnit4Test { get; }
    public PackageTest Net60NUnit4Test { get; }

        // Single Assembly Tests
        Net35Test = new PackageTest(
            "Net35Test",
            "Run mock-assembly.dll under .NET 3.5",
            $"src/TestData/mock-assembly/bin/{Configuration}/net35/mock-assembly.dll",
            MockAssemblyExpectedResult(1));
        Net462Test = new PackageTest(
            "Net4462Test",
            "Run mock-assembly.dll under .NET 4.6.2",
            $"src/TestData/mock-assembly/bin/{Configuration}/net462/mock-assembly.dll",
            MockAssemblyExpectedResult(1));
        NetCore31Test = new PackageTest(
            "NetCore31Test",
            "Run mock-assembly.dll under .NET Core 3.1",
            $"src/TestData/mock-assembly/bin/{Configuration}/netcoreapp3.1/mock-assembly.dll",
            MockAssemblyExpectedResult(1));
        Net50Test = new PackageTest(
            "Net50Test",
            "Run mock-assembly.dll under .NET 5.0",
            $"src/TestData/mock-assembly/bin/{Configuration}/net5.0/mock-assembly.dll",
            MockAssemblyExpectedResult(1));
        Net60Test = new PackageTest(
            "Net60Test",
            "Run mock-assembly.dll under .NET 6.0",
            $"src/TestData/mock-assembly/bin/{Configuration}/net6.0/mock-assembly.dll",
            MockAssemblyExpectedResult(1));
        Net70Test = new PackageTest(
            "Net70Test",
            "Run mock-assembly.dll under .NET 7.0",
            $"src/TestData/mock-assembly/bin/{Configuration}/net7.0/mock-assembly.dll",
            MockAssemblyExpectedResult(1));

        // X86 assembly tests
        Net35X86Test = new PackageTest(
            "Net35X86Test",
            "Run mock-assembly-x86.dll under .NET 3.5",
            $"src/TestData/mock-assembly-x86/bin/{Configuration}/net35/mock-assembly-x86.dll",
            MockAssemblyExpectedResult(1));
        Net462X86Test = new PackageTest(
            "Net462X86Test",
            "Run mock-assembly-x86.dll under .NET 4.6.2",
            $"src/TestData/mock-assembly-x86/bin/{Configuration}/net462/mock-assembly-x86.dll",
            MockAssemblyExpectedResult(1));
        NetCore31X86Test = new PackageTest(
            "NetCore31X86Test",
            "Run mock-assembly-x86.dll under .NET Core 3.1",
            $"src/TestData/mock-assembly-x86/bin/{Configuration}/netcoreapp3.1/mock-assembly-x86.dll --trace:Debug --X86",
            MockAssemblyExpectedResult(1));
        Net60X86Test = new PackageTest(
            "Net60X86Test",
            "Run mock-assembly-x86.dll under .NET 6.0",
            $"src/TestData/mock-assembly-x86/bin/{Configuration}/net6.0/mock-assembly-x86.dll --trace:Debug",
            MockAssemblyExpectedResult(1));
        Net80X86Test = new PackageTest(
            "Net80X86Test",
            "Run mock-assembly-x86.dll under .NET 8.0",
            $"src/TestData/mock-assembly-x86/bin/{Configuration}/net8.0/mock-assembly-x86.dll --trace:Debug --X86",
            MockAssemblyExpectedResult(1));
        
        // Windows Forms Tests
        Net60WindowsFormsTest = new PackageTest(
            "Net60WindowsFormsTest",
            "Run test using windows forms under .NET 6.0",
            $"src/TestData/windows-test/bin/{Configuration}/net6.0-windows/windows-test.dll",
            new ExpectedResult("Passed"));

        // AspNetCore Tests
        Net60AspNetCoreTest = new PackageTest(
            "Net60AspNetCoreTest",
            "Run test using AspNetCore under .NET 6.0",
            $"src/TestData/aspnetcore-test/bin/{Configuration}/net6.0/aspnetcore-test.dll",
            new ExpectedResult("Passed"));

        // Multiple Assembly Tests
        Net35PlusNet462Test = new PackageTest(
            "Net35PlusNet462Test",
            "Run both copies of mock-assembly together",
            $"src/TestData/mock-assembly/bin/{Configuration}/net35/mock-assembly.dll src/TestData/mock-assembly/bin/{Configuration}/net462/mock-assembly.dll",
            MockAssemblyExpectedResult(2));
        Net50PlusNet60Test = new PackageTest(
            "Net50PlusNet60Test",
            "Run mock-assembly under .Net 5.0 and .Net 6.0 together",
            $"src/TestData/mock-assembly/bin/{Configuration}/net5.0/mock-assembly.dll src/TestData/mock-assembly/bin/{Configuration}/net6.0/mock-assembly.dll",
            MockAssemblyExpectedResult(2));
        Net462PlusNet60Test = new PackageTest(
            "Net462PlusNet60Test",
            "Run mock-assembly under .Net Framework 4.6.2 and .Net 6.0 together",
            $"src/TestData/mock-assembly/bin/{Configuration}/net462/mock-assembly.dll src/TestData/mock-assembly/bin/{Configuration}/net6.0/mock-assembly.dll",
            MockAssemblyExpectedResult(2));
        NUnitProjectTest = new PackageTest(
            "NUnitProjectTest",
            "Run project with two copies of mock-assembly",
            $"NetFXTests.nunit --config={Configuration}",
            MockAssemblyExpectedResult(2));

        // Tests using NUnit4
        Net462NUnit4Test = new PackageTest(
            "Net462NUnit4Test",
            "Run mock-assembly-nunit4.dll under .NET 4.6.2",
            $"src/TestData/mock-assembly-nunit4/bin/{Configuration}/net462/mock-assembly-nunit4.dll",
            MockAssemblyExpectedResult(1));
        NetCore31NUnit4Test = new PackageTest(
            "NetCore31NUnit4Test",
            "Run mock-assembly-nunit4.dll under .NET Core 3.1",
            $"src/TestData/mock-assembly-nunit4/bin/{Configuration}/netcoreapp3.1/mock-assembly-nunit4.dll",
            MockAssemblyExpectedResult(1));
        Net50NUnit4Test = new PackageTest(
            "Net50NUnit4Test",
            "Run mock-assembly-nunit4.dll under .NET 5.0",
            $"src/TestData/mock-assembly-nunit4/bin/{Configuration}/net5.0/mock-assembly-nunit4.dll",
            MockAssemblyExpectedResult(1));
        Net60NUnit4Test = new PackageTest(
            "Net60NUnit4Test",
            "Run mock-assembly-nunit4.dll under .NET 6.0",
            $"src/TestData/mock-assembly-nunit4/bin/{Configuration}/net6.0/mock-assembly-nunit4.dll",
            MockAssemblyExpectedResult(1));

        StandardRunnerTests = new List<PackageTest>
        {
            Net35Test,
            Net462Test,
            NetCore31Test,
            Net50Test,
            Net60Test,
            Net70Test,
            Net35PlusNet462Test,
            Net50PlusNet60Test,
            Net462PlusNet60Test,
            Net35X86Test,
            Net462X86Test,
            Net60AspNetCoreTest,
            Net462NUnit4Test,
            NetCore31NUnit4Test,
            Net50NUnit4Test,
            Net60NUnit4Test
        };

        NetCoreRunnerTests = new List<PackageTest>
        {
            NetCore31Test,
            Net50Test,
            Net60Test,
            Net50PlusNet60Test,
            Net60AspNetCoreTest,
            NetCore31NUnit4Test,
            Net50NUnit4Test,
            Net60NUnit4Test
        };

        if (IsRunningOnWindows)
        {
            StandardRunnerTests.Add(Net60WindowsFormsTest);
            NetCoreRunnerTests.Add(Net60WindowsFormsTest);

            // TODO: Remove the limitation in DotnetInfo, which only works on Windows
            if (_dotnet.IsX86Installed)
            {
                StandardRunnerTests.Add(NetCore31X86Test);
                StandardRunnerTests.Add(Net60X86Test);
                StandardRunnerTests.Add(Net80X86Test);
                // TODO: Make X86 work in .NET Core Runner
                //NetCoreRunnerTests.Add(NetCore31X86Test);
                //NetCoreRunnerTests.Add(Net60X86Test);
                //NetCoreRunnerTests.Add(Net80X86Test);
            }
        }

