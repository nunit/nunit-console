public class BuildSettings
{
    private ISetupContext _context;
    private BuildSystem _buildSystem;

    public BuildSettings(ISetupContext context)
    {
        if (context == null)
            throw new System.ArgumentNullException(nameof(context));

        _context = context;
        _buildSystem = context.BuildSystem();

        Target = context.TargetTask.Name;
        TasksToExecute = context.TasksToExecute.Select(t => t.Name);

        Configuration = context.Argument("configuration", context.Argument("c", "Release"));
        NoPush = context.HasArgument("nopush");

        BuildVersion = new BuildVersion(context);

        Net35Test = new PackageTest(
            "Net35Test",
            "Run mock-assembly.dll under .NET 3.5",
            $"src/NUnitEngine/mock-assembly/bin/{Configuration}/net35/mock-assembly.dll",
            MockAssemblyExpectedResult(1));
        Net40Test = new PackageTest(
            "Net40Test",
            "Run mock-assembly.dll under .NET 4.x",
            $"src/NUnitEngine/mock-assembly/bin/{Configuration}/net462/mock-assembly.dll",
            MockAssemblyExpectedResult(1));
        NetCore21Test = new PackageTest(
            "NetCore21Test",
            "Run mock-assembly.dll targeting .NET Core 2.1",
            $"src/NUnitEngine/mock-assembly/bin/{Configuration}/netcoreapp2.1/mock-assembly.dll",
            MockAssemblyExpectedResult(1));
        NetCore31Test = new PackageTest(
            "NetCore31Test",
            "Run mock-assembly.dll under .NET Core 3.1",
            $"src/NUnitEngine/mock-assembly/bin/{Configuration}/netcoreapp3.1/mock-assembly.dll",
            MockAssemblyExpectedResult(1));
        Net50Test = new PackageTest(
            "Net50Test",
            "Run mock-assembly.dll under .NET 5.0",
            $"src/NUnitEngine/mock-assembly/bin/{Configuration}/net5.0/mock-assembly.dll",
            MockAssemblyExpectedResult(1));
        Net60Test = new PackageTest(
            "Net60Test",
            "Run mock-assembly.dll under .NET 6.0",
            $"src/NUnitEngine/mock-assembly/bin/{Configuration}/net6.0/mock-assembly.dll",
            MockAssemblyExpectedResult(1));
        Net70Test = new PackageTest(
            "Net70Test",
            "Run mock-assembly.dll under .NET 7.0",
            $"src/NUnitEngine/mock-assembly/bin/{Configuration}/net7.0/mock-assembly.dll",
            MockAssemblyExpectedResult(1));
        Net35X86Test = new PackageTest(
            "Net35X86Test",
            "Run mock-assembly-x86.dll under .NET 3.5",
            $"src/NUnitEngine/mock-assembly-x86/bin/{Configuration}/net35/mock-assembly-x86.dll",
            MockAssemblyExpectedResult(1));
        Net40X86Test = new PackageTest(
            "Net40X86Test",
            "Run mock-assembly-x86.dll under .NET 4.x",
            $"src/NUnitEngine/mock-assembly-x86/bin/{Configuration}/net462/mock-assembly-x86.dll",
            MockAssemblyExpectedResult(1));
        NetCore31X86Test = new PackageTest(
            "NetCore31X86Test",
            "Run mock-assembly-x86.dll under .NET Core 3.1",
            $"src/NUnitEngine/mock-assembly-x86/bin/{Configuration}/netcoreapp3.1/mock-assembly-x86.dll",
            MockAssemblyExpectedResult(1));
        Net60WindowsFormsTest = new PackageTest(
            "Net60WindowsFormsTest",
            "Run test using windows forms under .NET 6.0",
            $"src/NUnitEngine/windows-test/bin/{Configuration}/net6.0-windows/windows-test.dll",
            new ExpectedResult("Passed"));
        Net60AspNetCoreTest = new PackageTest(
            "Net60AspNetCoreTest",
            "Run test using AspNetCore under .NET 6.0",
            $"src/NUnitEngine/aspnetcore-test/bin/{Configuration}/net6.0/aspnetcore-test.dll",
            new ExpectedResult("Passed"));
        Net35PlusNet40Test = new PackageTest(
            "Net35PlusNet40Test",
            "Run both copies of mock-assembly together",
            $"src/NUnitEngine/mock-assembly/bin/{Configuration}/net35/mock-assembly.dll src/NUnitEngine/mock-assembly/bin/{Configuration}/net462/mock-assembly.dll",
            MockAssemblyExpectedResult(2));
        Net40PlusNet60Test = new PackageTest(
            "Net40PlusNet60Test",
            "Run mock-assembly under .Net Framework 4.0 and .Net 6.0 together",
            $"src/NUnitEngine/mock-assembly/bin/{Configuration}/net462/mock-assembly.dll src/NUnitEngine/mock-assembly/bin/{Configuration}/net6.0/mock-assembly.dll",
            MockAssemblyExpectedResult(2));
        Net50PlusNet60Test = new PackageTest(
            "Net50PlusNet60Test",
            "Run mock-assembly under .Net 5.0 and .Net 6.0 together",
            $"src/NUnitEngine/mock-assembly/bin/{Configuration}/net5.0/mock-assembly.dll src/NUnitEngine/mock-assembly/bin/{Configuration}/net6.0/mock-assembly.dll",
            MockAssemblyExpectedResult(2));
        NUnitProjectTest = new PackageTest(
            "NUnitProjectTest",
            "Run project with both copies of mock-assembly",
            $"NetFXTests.nunit --config={Configuration}",
            MockAssemblyExpectedResult(2));

        StandardRunnerTests = new List<PackageTest>
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
        if (IsRunningOnWindows)
            StandardRunnerTests.Add(Net60WindowsFormsTest);

        NetCoreRunnerTests = new List<PackageTest>
        {
            NetCore21Test,
            NetCore31Test,
            Net50Test,
            Net60Test,
            Net50PlusNet60Test,
            Net60AspNetCoreTest
        };
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

    public bool IsPreRelease => !string.IsNullOrEmpty(PreReleaseLabel);

    public bool ShouldPublishToMyGet => IsPreRelease && LABELS_WE_PUBLISH_ON_MYGET.Contains(PreReleaseLabel);
    public bool ShouldPublishToNuGet => !IsPreRelease || LABELS_WE_PUBLISH_ON_NUGET.Contains(PreReleaseLabel);
    public bool ShouldPublishToChocolatey => !IsPreRelease || LABELS_WE_PUBLISH_ON_CHOCOLATEY.Contains(PreReleaseLabel);
    public bool IsProductionRelease => !IsPreRelease || LABELS_WE_RELEASE_ON_GITHUB.Contains(PreReleaseLabel);

    public string MyGetApiKey => _context.EnvironmentVariable(MYGET_API_KEY);
    public string NuGetApiKey => _context.EnvironmentVariable(NUGET_API_KEY);
    public string ChocolateyApiKey => _context.EnvironmentVariable(CHOCO_API_KEY);

    public string GitHubAccessToken => _context.EnvironmentVariable(GITHUB_ACCESS_TOKEN);

    public bool NoPush { get; }

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
    public PackageTest Net35Test { get; }
    public PackageTest Net40Test { get; }
    public PackageTest NetCore21Test { get; }
    public PackageTest NetCore31Test { get; }
    public PackageTest Net50Test { get; }
    public PackageTest Net60Test { get; }
    public PackageTest Net70Test { get; }
    // X86 Tests
    public PackageTest Net35X86Test { get; }
    public PackageTest Net40X86Test { get; }
    public PackageTest NetCore31X86Test { get; }
    // Special Test Situations
    public PackageTest Net60WindowsFormsTest { get; }
    public PackageTest Net60AspNetCoreTest { get; }
    // Multiple Assemblies
    public PackageTest Net35PlusNet40Test { get; }
    public PackageTest Net40PlusNet60Test { get; }
    public PackageTest Net50PlusNet60Test { get; }
    // NUnit Project
    public PackageTest NUnitProjectTest { get; }

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

        Console.WriteLine("\nPUBLISHING");
        Console.WriteLine("MyGet Push URL:               " + MYGET_PUSH_URL);
        ShowApiKeyAvailability(MyGetApiKey);
        Console.WriteLine("NuGet Push URL:               " + NUGET_PUSH_URL);
        ShowApiKeyAvailability(NuGetApiKey);
        Console.WriteLine("Chocolatey Push URL:          " + CHOCO_PUSH_URL);
        ShowApiKeyAvailability(ChocolateyApiKey);
        
        if (string.IsNullOrEmpty(GitHubAccessToken))
            Console.WriteLine("GitHubAccessToken:            NOT Available");
        else
            Console.WriteLine("GitHubAccessToken:            Available");

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
        Console.WriteLine($"  NetCore Runner:  {NETCORE_CONSOLE_PROJECT}");
    }

    private void ShowApiKeyAvailability(string apikey)
    {
        if (string.IsNullOrEmpty(apikey))
            Console.WriteLine("      API Key:                NOT Available");
        else
            Console.WriteLine("      API Key:                Available");
    }
}
