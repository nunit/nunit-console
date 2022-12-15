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

        Configuration = context.Argument("configuration", "Release");

        BuildVersion = new BuildVersion(context);
    }

    public string Target { get; }
    public IEnumerable<string> TasksToExecute { get; }

    public ICakeContext Context => _context;

    public string Configuration { get; }

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
        Console.WriteLine($"    Bin Dir:       {NETFX_CONSOLE_PROJECT_BIN_DIR}");
        Console.WriteLine($"  NetCore Runner:  {NETCORE_CONSOLE_PROJECT}");
        Console.WriteLine($"    Bin Dir:       {NETCORE_CONSOLE_PROJECT_BIN_DIR}");
    }
}
