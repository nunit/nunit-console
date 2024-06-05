public class BuildSettings
{
    private ISetupContext _context;
    private BuildSystem _buildSystem;
    private DotnetInfo _dotnet;

    public BuildSettings(ISetupContext context)
    {
        if (context == null)
            throw new System.ArgumentNullException(nameof(context));

        _context = context;
        _buildSystem = context.BuildSystem();
        _dotnet = new DotnetInfo(context);

        Target = context.TargetTask.Name;
        TasksToExecute = context.TasksToExecute.Select(t => t.Name);

        Configuration = context.Argument("configuration", context.Argument("c", "Release"));
        NoPush = context.HasArgument("nopush");
        TestName = context.Argument("testName", context.Argument<string>("test", null));

        BuildVersion = new BuildVersion(context);

        MyGetApiKey = GetApiKey(MYGET_API_KEY, FALLBACK_MYGET_API_KEY);
        NuGetApiKey = GetApiKey(NUGET_API_KEY, FALLBACK_NUGET_API_KEY);
        ChocolateyApiKey = GetApiKey(CHOCO_API_KEY, FALLBACK_CHOCO_API_KEY);

        // Single Assembly Tests
        Net35Test = new PackageTest(
            "Net35Test",
            "Run mock-assembly.dll under .NET 3.5",
            $"src/TestData/mock-assembly/bin/{Configuration}/net35/mock-assembly.dll",
            MockAssemblyExpectedResult( "net-2.0-agent" ));
        Net462Test = new PackageTest(
            "Net4462Test",
            "Run mock-assembly.dll under .NET 4.6.2",
            $"src/TestData/mock-assembly/bin/{Configuration}/net462/mock-assembly.dll",
            MockAssemblyExpectedResult( "net-4.6.2-agent" ));
        NetCore31Test = new PackageTest(
            "NetCore31Test",
            "Run mock-assembly.dll under .NET Core 3.1",
            $"src/TestData/mock-assembly/bin/{Configuration}/netcoreapp3.1/mock-assembly.dll",
            MockAssemblyExpectedResult( "netcore-3.1-agent"));
        Net50Test = new PackageTest(
            "Net50Test",
            "Run mock-assembly.dll under .NET 5.0",
            $"src/TestData/mock-assembly/bin/{Configuration}/net5.0/mock-assembly.dll",
            MockAssemblyExpectedResult( "netcore-5.0-agent" ));
        Net60Test = new PackageTest(
            "Net60Test",
            "Run mock-assembly.dll under .NET 6.0",
            $"src/TestData/mock-assembly/bin/{Configuration}/net6.0/mock-assembly.dll",
            MockAssemblyExpectedResult( "netcore-6.0-agent" ));
        Net70Test = new PackageTest(
            "Net70Test",
            "Run mock-assembly.dll under .NET 7.0",
            $"src/TestData/mock-assembly/bin/{Configuration}/net7.0/mock-assembly.dll",
            MockAssemblyExpectedResult( "netcore-7.0-agent" ));

        // X86 assembly tests
        Net35X86Test = new PackageTest(
            "Net35X86Test",
            "Run mock-assembly-x86.dll under .NET 3.5",
            $"src/TestData/mock-assembly-x86/bin/{Configuration}/net35/mock-assembly-x86.dll",
            MockAssemblyExpectedResult( "net-2.0-x86-agent" ));
        Net462X86Test = new PackageTest(
            "Net462X86Test",
            "Run mock-assembly-x86.dll under .NET 4.6.2",
            $"src/TestData/mock-assembly-x86/bin/{Configuration}/net462/mock-assembly-x86.dll",
            MockAssemblyExpectedResult( "net-4.6.2-x86-agent" ));
        NetCore31X86Test = new PackageTest(
            "NetCore31X86Test",
            "Run mock-assembly-x86.dll under .NET Core 3.1",
            $"src/TestData/mock-assembly-x86/bin/{Configuration}/netcoreapp3.1/mock-assembly-x86.dll --trace:Debug",
            MockAssemblyExpectedResult( "netcore-3.1-x86-agent" ));
        Net60X86Test = new PackageTest(
            "Net60X86Test",
            "Run mock-assembly-x86.dll under .NET 6.0",
            $"src/TestData/mock-assembly-x86/bin/{Configuration}/net6.0/mock-assembly-x86.dll --trace:Debug",
            MockAssemblyExpectedResult( "netcore-6.0-x86-agent" ));
        Net80X86Test = new PackageTest(
            "Net80X86Test",
            "Run mock-assembly-x86.dll under .NET 8.0",
            $"src/TestData/mock-assembly-x86/bin/{Configuration}/net8.0/mock-assembly-x86.dll --trace:Debug",
            MockAssemblyExpectedResult( "netcore-8.0-x86-agent" ));
        
        // Windows Forms Tests
        Net60WindowsFormsTest = new PackageTest(
            "Net60WindowsFormsTest",
            "Run test using windows forms under .NET 6.0",
            $"src/TestData/windows-test/bin/{Configuration}/net6.0-windows/windows-test.dll",
            new ExpectedResult("Passed") { Assemblies = new [] { new ExpectedAssemblyResult("windows-test.dll", "netcore-6.0-agent") } });

        // AspNetCore Tests
        Net60AspNetCoreTest = new PackageTest(
            "Net60AspNetCoreTest",
            "Run test using AspNetCore under .NET 6.0",
            $"src/TestData/aspnetcore-test/bin/{Configuration}/net6.0/aspnetcore-test.dll",
            new ExpectedResult("Passed") { Assemblies = new [] { new ExpectedAssemblyResult("aspnetcore-test.dll", "netcore-6.0-agent") } });

        // Multiple Assembly Tests
        Net35PlusNet462Test = new PackageTest(
            "Net35PlusNet462Test",
            "Run mock-assembly under .Net Framework 2.0 and 4.6.2 together",
            $"src/TestData/mock-assembly/bin/{Configuration}/net35/mock-assembly.dll src/TestData/mock-assembly/bin/{Configuration}/net462/mock-assembly.dll",
            MockAssemblyExpectedResult("net-2.0-agent", "net-4.6.2-agent"));
        Net50PlusNet60Test = new PackageTest(
            "Net50PlusNet60Test",
            "Run mock-assembly under .Net 5.0 and .Net 6.0 together",
            $"src/TestData/mock-assembly/bin/{Configuration}/net5.0/mock-assembly.dll src/TestData/mock-assembly/bin/{Configuration}/net6.0/mock-assembly.dll",
            MockAssemblyExpectedResult("netcore-5.0-agent", "netcore-6.0-agent"));
        Net462PlusNet60Test = new PackageTest(
            "Net462PlusNet60Test",
            "Run mock-assembly under .Net Framework 4.6.2 and .Net 6.0 together",
            $"src/TestData/mock-assembly/bin/{Configuration}/net462/mock-assembly.dll src/TestData/mock-assembly/bin/{Configuration}/net6.0/mock-assembly.dll",
            MockAssemblyExpectedResult("net-4.6.2-agent", "netcore-6.0-agent"));
        NUnitProjectTest = new PackageTest(
            "NUnitProjectTest",
            "Run project with two copies of mock-assembly",
            $"NetFXTests.nunit --config={Configuration}",
            MockAssemblyExpectedResult("net-2.0-agent", "net-4.6.2-agent"));

        // Tests using NUnit4
        Net462NUnit4Test = new PackageTest(
            "Net462NUnit4Test",
            "Run mock-assembly-nunit4.dll under .NET 4.6.2",
            $"src/TestData/mock-assembly-nunit4/bin/{Configuration}/net462/mock-assembly-nunit4.dll",
            MockAssemblyNUnit4ExpectedResult( "net-4.6.2-agent" ));
        NetCore31NUnit4Test = new PackageTest(
            "NetCore31NUnit4Test",
            "Run mock-assembly-nunit4.dll under .NET Core 3.1",
            $"src/TestData/mock-assembly-nunit4/bin/{Configuration}/netcoreapp3.1/mock-assembly-nunit4.dll",
            MockAssemblyNUnit4ExpectedResult( "netcore-3.1-agent" ));
        Net50NUnit4Test = new PackageTest(
            "Net50NUnit4Test",
            "Run mock-assembly-nunit4.dll under .NET 5.0",
            $"src/TestData/mock-assembly-nunit4/bin/{Configuration}/net5.0/mock-assembly-nunit4.dll",
            MockAssemblyNUnit4ExpectedResult( "netcore-5.0-agent" ));
        Net60NUnit4Test = new PackageTest(
            "Net60NUnit4Test",
            "Run mock-assembly-nunit4.dll under .NET 6.0",
            $"src/TestData/mock-assembly-nunit4/bin/{Configuration}/net6.0/mock-assembly-nunit4.dll",
            MockAssemblyNUnit4ExpectedResult( "netcore-6.0-agent" ));

        StandardRunnerTests = new List<PackageTest>()
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
                // TODO: Make tests run on AppVeyor
                if (!IsRunningOnAppVeyor)
                    StandardRunnerTests.Add(NetCore31X86Test);
                StandardRunnerTests.Add(Net60X86Test);
                if (!IsRunningOnAppVeyor)
                    StandardRunnerTests.Add(Net80X86Test);
                // TODO: Figure out how to test this.
                // Currently, NetCoreRunner runs tests in process. As a result,
                // X86 tests will work in our environment, although uses may run
                // it as a tool using the X86 architecture.
                //NetCoreRunnerTests.Add(NetCore31X86Test);
                //NetCoreRunnerTests.Add(Net60X86Test);
                //NetCoreRunnerTests.Add(Net80X86Test);
            }
        }

        AllPackages = new PackageDefinition[] {
            ConsoleNuGetPackage = new NUnitConsoleNuGetPackage(this),
            ConsoleRunnerNuGetPackage = new NUnitConsoleRunnerNuGetPackage(this),
            DotNetConsoleRunnerNuGetPackage = new NUnitNetCoreConsoleRunnerPackage(this),
            ConsoleRunnerChocolateyPackage = new NUnitConsoleRunnerChocolateyPackage(this),
            ConsoleMsiPackage = new NUnitConsoleMsiPackage(this),
            ConsoleZipPackage = new NUnitConsoleZipPackage(this),
            EngineNuGetPackage = new NUnitEngineNuGetPackage(this),
            EngineApiNuGetPackage = new NUnitEngineApiNuGetPackage(this)
        };
    }

    public string Target { get; }
    public IEnumerable<string> TasksToExecute { get; }

    public ICakeContext Context => _context;

    public string Configuration { get; }

    public string NetFxConsoleBinDir => NETFX_CONSOLE_DIR + $"bin/{Configuration}/{NETFX_CONSOLE_TARGET}/";
    public string NetCoreConsoleBinDir => NETCORE_CONSOLE_DIR + $"bin/{Configuration}/{NETCORE_CONSOLE_TARGET}";

    public BuildVersion BuildVersion { get; }
    public string SemVer => BuildVersion.SemVer;
    public string ProductVersion => BuildVersion.ProductVersion;
	public string PreReleaseLabel => BuildVersion.PreReleaseLabel;
    public string AssemblyVersion => BuildVersion.AssemblyVersion;
	public string AssemblyFileVersion => BuildVersion.AssemblyFileVersion;
	public string AssemblyInformationalVersion => BuildVersion.AssemblyInformationalVersion;
    public string BranchName => BuildVersion.BranchName;
	public bool IsReleaseBranch => BuildVersion.IsReleaseBranch;

	public bool IsLocalBuild => _buildSystem.IsLocalBuild;
	public bool IsRunningOnUnix => _context.IsRunningOnUnix();
	public bool IsRunningOnWindows => _context.IsRunningOnWindows();
	public bool IsRunningOnAppVeyor => _buildSystem.AppVeyor.IsRunningOnAppVeyor;
    // TODO: Fix this so it works on non-windows
    public bool IsDotNetX86Installed => IsRunningOnWindows && _dotnet.IsX86Installed;

    public bool IsPreRelease => !string.IsNullOrEmpty(PreReleaseLabel);

    public bool ShouldPublishToMyGet => IsPreRelease && LABELS_WE_PUBLISH_ON_MYGET.Contains(PreReleaseLabel);
    public bool ShouldPublishToNuGet => !IsPreRelease || LABELS_WE_PUBLISH_ON_NUGET.Contains(PreReleaseLabel);
    public bool ShouldPublishToChocolatey => !IsPreRelease || LABELS_WE_PUBLISH_ON_CHOCOLATEY.Contains(PreReleaseLabel);
    public bool IsProductionRelease => !IsPreRelease || LABELS_WE_RELEASE_ON_GITHUB.Contains(PreReleaseLabel);

    public string MyGetApiKey { get; }
    public string NuGetApiKey { get; }
    public string ChocolateyApiKey { get; }

    public string GitHubAccessToken => _context.EnvironmentVariable(GITHUB_ACCESS_TOKEN);

    public bool NoPush { get; }

    public string TestName { get; }

//////////////////////////////////////////////////////////////////////
// INDIVIDUAL PACKAGE TEST DEFINITIONS
//////////////////////////////////////////////////////////////////////

    static ExpectedResult MockAssemblyExpectedResult(int nCopies = 1) =>
        new ExpectedResult("Failed")
        {
            Total = 37 * nCopies,
            Passed = 23 * nCopies,
            Failed = 5 * nCopies,
            Warnings = 1 * nCopies,
            Inconclusive = 1 * nCopies,
            Skipped = 7 * nCopies
        };

    static ExpectedResult MockAssemblyExpectedResult(params string[] runtimes)
    {
        var nCopies = runtimes.Length;

        var assemblies = new ExpectedAssemblyResult[nCopies];
        for (int i = 0; i < nCopies; i++)
        {
            var runtime = runtimes[i];
            var assembly = runtime.Contains("-x86-") 
                ? "mock-assembly-x86.dll" 
                : "mock-assembly.dll";
            assemblies[i] = new ExpectedAssemblyResult(assembly, runtime);
        }

        return new ExpectedResult("Failed")
        {
            Total = 37 * nCopies,
            Passed = 23 * nCopies,
            Failed = 5 * nCopies,
            Warnings = 1 * nCopies,
            Inconclusive = 1 * nCopies,
            Skipped = 7 * nCopies,
            Assemblies = assemblies
        };
    }

    static ExpectedResult MockAssemblyNUnit4ExpectedResult(params string[] runtimes)
    {
        var nCopies = runtimes.Length;

        var assemblies = new ExpectedAssemblyResult[nCopies];
        for (int i = 0; i < nCopies; i++)
        {
            var runtime = runtimes[i];
            var assembly = runtime.Contains("-x86-") 
                ? "mock-assembly-nunit4-x86.dll" 
                : "mock-assembly-nunit4.dll";
            assemblies[i] = new ExpectedAssemblyResult(assembly, runtime);
        }

        return new ExpectedResult("Failed")
        {
            Total = 37 * nCopies,
            Passed = 23 * nCopies,
            Failed = 5 * nCopies,
            Warnings = 1 * nCopies,
            Inconclusive = 1 * nCopies,
            Skipped = 7 * nCopies,
            Assemblies = assemblies
        };
    }

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

    // Tests run for all runner packages except NETCORE runner
    public List<PackageTest> StandardRunnerTests { get; }
    // Tests run for the NETCORE runner package
    public List<PackageTest> NetCoreRunnerTests { get; }

//////////////////////////////////////////////////////////////////////
// PACKAGE DEFINITIONS
//////////////////////////////////////////////////////////////////////

    public PackageDefinition[] AllPackages { get; }

    public PackageDefinition ConsoleNuGetPackage;
    public PackageDefinition ConsoleRunnerNuGetPackage;
    public PackageDefinition DotNetConsoleRunnerNuGetPackage;
    public PackageDefinition ConsoleRunnerChocolateyPackage;
    public PackageDefinition ConsoleMsiPackage;
    public PackageDefinition ConsoleZipPackage;
    public PackageDefinition EngineNuGetPackage;
    public PackageDefinition EngineApiNuGetPackage;

    public void Display()
    {
		Console.WriteLine("\nTASKS");
		Console.WriteLine("Target:                       " + Target);
		Console.WriteLine("TasksToExecute:               " + string.Join(", ", TasksToExecute));

		Console.WriteLine("\nENVIRONMENT");
		Console.WriteLine("IsLocalBuild:                 " + IsLocalBuild);
		Console.WriteLine("IsRunningOnWindows:           " + IsRunningOnWindows);
		Console.WriteLine("IsRunningOnUnix:              " + IsRunningOnUnix);
		Console.WriteLine("IsRunningOnAppVeyor:          " + IsRunningOnAppVeyor);

		Console.WriteLine("\nVERSIONING");
		Console.WriteLine("ProductVersion:               " + ProductVersion);
		Console.WriteLine("AssemblyVersion:              " + AssemblyVersion);
		Console.WriteLine("AssemblyFileVersion:          " + AssemblyFileVersion);
		Console.WriteLine("AssemblyInformationalVersion: " + AssemblyInformationalVersion);
		Console.WriteLine("SemVer:                       " + BuildVersion.SemVer);
		Console.WriteLine("IsPreRelease:                 " + BuildVersion.IsPreRelease);
		Console.WriteLine("PreReleaseLabel:              " + BuildVersion.PreReleaseLabel);
		Console.WriteLine("PreReleaseSuffix:             " + BuildVersion.PreReleaseSuffix);

		Console.WriteLine("\nRELEASING");
		Console.WriteLine("BranchName:                   " + BranchName);
		Console.WriteLine("IsReleaseBranch:              " + IsReleaseBranch);

		Console.WriteLine("\nDIRECTORIES");
        Console.WriteLine($"Project:         {PROJECT_DIR}");
        Console.WriteLine($"Package:         {PACKAGE_DIR}");
        Console.WriteLine($"Package Test:    {PACKAGE_TEST_DIR}");
        Console.WriteLine($"Package Results: {PACKAGE_RESULT_DIR}");
        Console.WriteLine($"Extensions:      {EXTENSIONS_DIR}");
        Console.WriteLine();
        Console.WriteLine("Solution and Projects");
        Console.WriteLine($"  Solution:        {SOLUTION_FILE}");
        Console.WriteLine($"  NetFx Runner:    {NETFX_CONSOLE_PROJECT}");
        Console.WriteLine($"    Bin Dir:       {NetFxConsoleBinDir}");
        Console.WriteLine($"  NetCore Runner:  {NETCORE_CONSOLE_PROJECT}");
        Console.WriteLine($"    Bin Dir:       {NetCoreConsoleBinDir}");
    }

    private string GetApiKey(string name, string fallback=null)
    {
        var apikey = _context.EnvironmentVariable(name);

        if (string.IsNullOrEmpty(apikey) && fallback != null)
            apikey = _context.EnvironmentVariable(fallback);

        return apikey;
    }

    private void ShowApiKeyAvailability(string apikey)
    {
        if (string.IsNullOrEmpty(apikey))
            Console.WriteLine("      API Key:                NOT Available");
        else
            Console.WriteLine("      API Key:                Available");
    }
}
