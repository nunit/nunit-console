public static class BuildSettings
{
	private static BuildSystem _buildSystem;

	public static void Initialize(
      // Required parameters
        ICakeContext context,
        string title,
        string githubRepository,

      // Optional Parameters
        bool suppressHeaderCheck = false,
        string[] standardHeader = null,
        string[] exemptFiles = null,
        
        string solutionFile = null,
        string[] validConfigurations = null,
        string githubOwner = "NUnit",

		bool msbuildAllowPreviewVersion = false,
		Verbosity msbuildVerbosity = Verbosity.Minimal,

        string unitTests = null, // Defaults to "**/*.tests.dll|**/*.tests.exe" (case insensitive)
        IUnitTestRunner unitTestRunner = null, // If not set, NUnitLite is used
        string unitTestArguments = null
        )
	{
        // Required arguments
        Context = context ?? throw new ArgumentNullException(nameof(context));
        Title = title ?? throw new ArgumentNullException(nameof(title));
        GitHubRepository = githubRepository ?? throw new ArgumentNullException(nameof(githubRepository));

        // NOTE: Order of initialization can be sensitive. Obviously,
        // we have to set any properties in this method before we
        // make use of them. Less obviously, some of the classes we
        // construct here have dependencies on certain properties
        // being set before the constructor is called. I have
        // tried to annotate such dependencies below.

        _buildSystem = context.BuildSystem();
	
		SolutionFile = solutionFile ?? DeduceSolutionFile();

		ValidConfigurations = validConfigurations ?? DEFAULT_VALID_CONFIGS;

		UnitTests = unitTests;
		// NUnitLiteRunner depends indirectly on ValidConfigurations
		UnitTestRunner = unitTestRunner ?? new NUnitLiteRunner();
		UnitTestArguments = unitTestArguments;

		BuildVersion = new BuildVersion(context);

        GitHubOwner = githubOwner;

		// File Header Checks
		SuppressHeaderCheck = suppressHeaderCheck && !CommandLineOptions.NoBuild;
		StandardHeader = standardHeader ?? DEFAULT_STANDARD_HEADER;
		ExemptFiles = exemptFiles ?? new string[0];
        
		//if (defaultTarget != null)
		//	BuildTasks.DefaultTask.IsDependentOn(defaultTarget);

		// Skip remaining initialization if help was requested
		if (CommandLineOptions.Usage)
			return;
			
		ValidateSettings();

		context.Information($"{Title} {Configuration} version {PackageVersion}");

		// Output like this should go after the run title display
		if (solutionFile == null && SolutionFile != null)
			Context.Warning($"  SolutionFile: '{SolutionFile}'");
		Context.Information($"  PackageTestLevel: {PackageTestLevel}");

		// Keep this last
		if (IsRunningOnAppVeyor)
		{
			var buildNumber = _buildSystem.AppVeyor.Environment.Build.Number;
			_buildSystem.AppVeyor.UpdateBuildVersion($"{PackageVersion}-{buildNumber}");
		}
    }

	// If solution file was not provided, uses TITLE.sln if it exists or 
    // the solution found in the root directory provided there is only one. 
	private static string DeduceSolutionFile()			
	{
		if (SIO.File.Exists(Title + ".sln"))
			return Title + ".sln";

		var files = SIO.Directory.GetFiles(ProjectDirectory, "*.sln");
		if (files.Length == 1 && SIO.File.Exists(files[0]))
            return files[0];

        return null;
	}

	private static int CalcPackageTestLevel()
	{
		if (!BuildVersion.IsPreRelease)
			return 3;

		// TODO: The prerelease label is no longer being set to pr by GitVersion
		// for some reason. This check in AppVeyor is a workaround.
		if (IsRunningOnAppVeyor && _buildSystem.AppVeyor.Environment.PullRequest.IsPullRequest)
			return 2;
		
		switch (BuildVersion.PreReleaseLabel)
		{
			case "pre":
			case "rc":
			case "alpha":
			case "beta":
				return 3;

			case "dev":
			case "pr":
				return 2;

			case "ci":
			default:
				return 1;
		}
	}

	// Cake Context
	public static ICakeContext Context { get; private set; }

    // NOTE: These are set in setup.cake
	public static string Target { get; set; }
	public static IEnumerable<string> TasksToExecute { get; set; }
   
	// Arguments
	public static string Configuration
	{
		get
		{
			// Correct casing on user-provided config if necessary
			foreach (string config in ValidConfigurations)
				if (string.Equals(config, CommandLineOptions.Configuration.Value, StringComparison.OrdinalIgnoreCase))
					return config;

			// Return the (invalid) user-provided config
			return CommandLineOptions.Configuration.Value;
		}
	}

    // Build Environment
	public static bool IsLocalBuild => _buildSystem.IsLocalBuild;
	public static bool IsRunningOnUnix => Context.IsRunningOnUnix();
	public static bool IsRunningOnWindows => Context.IsRunningOnWindows();
	public static bool IsRunningOnAppVeyor => _buildSystem.AppVeyor.IsRunningOnAppVeyor;

	// Versioning
    public static BuildVersion BuildVersion { get; private set;}
	public static string BranchName => BuildVersion.BranchName;
	public static bool IsReleaseBranch => BuildVersion.IsReleaseBranch;
	public static string PackageVersion => BuildVersion.PackageVersion;
	public static string AssemblyVersion => BuildVersion.AssemblyVersion;
	public static string AssemblyFileVersion => BuildVersion.AssemblyFileVersion;
	public static string AssemblyInformationalVersion => BuildVersion.AssemblyInformationalVersion;
	public static bool IsDevelopmentRelease => PackageVersion.Contains("-dev");

	// Standard Directory Structure - not changeable by user
	public static string ProjectDirectory => Context.Environment.WorkingDirectory.FullPath + "/";
	public static string SourceDirectory                => ProjectDirectory + SRC_DIR;
	public static string OutputDirectory                => ProjectDirectory + BIN_DIR + Configuration + "/";
	public static string NuGetDirectory                 => ProjectDirectory + NUGET_DIR;
	public static string ChocolateyDirectory            => ProjectDirectory + CHOCO_DIR;
    public static string ZipDirectory                   => ProjectDirectory + ZIP_DIR;
	public static string PackageDirectory               => ProjectDirectory + PACKAGE_DIR;
	public static string PackageTestDirectory           => ProjectDirectory + PKG_TEST_DIR;
	public static string NuGetTestDirectory             => ProjectDirectory + NUGET_TEST_DIR;
	public static string ChocolateyTestDirectory        => ProjectDirectory + CHOCO_TEST_DIR;
	public static string ZipTestDirectory               => ProjectDirectory + ZIP_TEST_DIR;
	public static string PackageResultDirectory         => ProjectDirectory + PKG_RSLT_DIR;
	public static string NuGetResultDirectory           => ProjectDirectory + NUGET_RSLT_DIR;
	public static string ChocolateyResultDirectory      => ProjectDirectory + CHOCO_RSLT_DIR;
	public static string ZipResultDirectory             => ProjectDirectory + ZIP_RSLT_DIR;
	public static string ImageDirectory                 => ProjectDirectory + IMAGE_DIR;
    public static string ZipImageDirectory              => ProjectDirectory + ZIP_IMG_DIR;
	public static string ExtensionsDirectory            => ProjectDirectory + "bundled-extensions/";
	public static string ToolsDirectory                 => ProjectDirectory + "tools/";

    // Files
    public static string SolutionFile { get; set; }

    // Building
	public static string[] ValidConfigurations { get; set; }
	public static bool MSBuildAllowPreviewVersion { get; set; }
	public static Verbosity MSBuildVerbosity { get; set; }
	public static MSBuildSettings MSBuildSettings => new MSBuildSettings {
		Verbosity = MSBuildVerbosity,
		Configuration = Configuration,
		PlatformTarget = PlatformTarget.MSIL,
		AllowPreviewVersion = MSBuildAllowPreviewVersion
	};

	// File Header Checks
	public static bool SuppressHeaderCheck { get; private set; }
	public static string[] StandardHeader { get; private set; }
	public static string[] ExemptFiles { get; private set; }

	//Testing
	public static string UnitTests { get; set; }
	public static IUnitTestRunner UnitTestRunner { get; private set; }
	public static string UnitTestArguments { get; private set; }

	// Packaging
	public static string Title { get; private set; }
    public static List<PackageDefinition> Packages { get; } = new List<PackageDefinition>();

	// Package Testing
	public static int PackageTestLevel =>
		CommandLineOptions.TestLevel.Value > 0
			? CommandLineOptions.TestLevel.Value
			: CalcPackageTestLevel();

	// Publishing - MyGet
	public static string MyGetPushUrl => MYGET_PUSH_URL;
	public static string MyGetApiKey => Context.EnvironmentVariable(MYGET_API_KEY);

	// Publishing - NuGet
	public static string NuGetPushUrl => NUGET_PUSH_URL;
	public static string NuGetApiKey => Context.EnvironmentVariable(NUGET_API_KEY);

	// Publishing - Chocolatey
	public static string ChocolateyPushUrl => CHOCO_PUSH_URL;
	public static string ChocolateyApiKey => Context.EnvironmentVariable(CHOCO_API_KEY);

	// Publishing - GitHub
	public static string GitHubOwner { get; set; }
	public static string GitHubRepository { get; set; }
	public static string GitHubAccessToken => Context.EnvironmentVariable(GITHUB_ACCESS_TOKEN);

	public static bool IsPreRelease => BuildVersion.IsPreRelease;
	public static bool ShouldPublishToMyGet =>
		!IsPreRelease || LABELS_WE_PUBLISH_ON_MYGET.Contains(BuildVersion.PreReleaseLabel);
	public static bool ShouldPublishToNuGet =>
		!IsPreRelease || LABELS_WE_PUBLISH_ON_NUGET.Contains(BuildVersion.PreReleaseLabel);
	public static bool ShouldPublishToChocolatey =>
		!IsPreRelease || LABELS_WE_PUBLISH_ON_CHOCOLATEY.Contains(BuildVersion.PreReleaseLabel);
	public static bool IsProductionRelease =>
		!IsPreRelease || LABELS_WE_RELEASE_ON_GITHUB.Contains(BuildVersion.PreReleaseLabel);

	private static void ValidateSettings()
	{
		var validationErrors = new List<string>();

		if (!ValidConfigurations.Contains(Configuration))
			validationErrors.Add($"Invalid configuration: {Configuration}");

		if (validationErrors.Count > 0)
		{
			DumpSettings();

			var msg = new StringBuilder("Parameter validation failed! See settings above.\n\nErrors found:\n");
			foreach (var error in validationErrors)
				msg.AppendLine("  " + error);

			throw new InvalidOperationException(msg.ToString());
		}
	}

	public static void DumpSettings()
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
		Console.WriteLine("PackageVersion:               " + PackageVersion);
		Console.WriteLine("AssemblyVersion:              " + AssemblyVersion);
		Console.WriteLine("AssemblyFileVersion:          " + AssemblyFileVersion);
		Console.WriteLine("AssemblyInformationalVersion: " + AssemblyInformationalVersion);
		Console.WriteLine("SemVer:                       " + BuildVersion.SemVer);
		Console.WriteLine("IsPreRelease:                 " + BuildVersion.IsPreRelease);
		Console.WriteLine("PreReleaseLabel:              " + BuildVersion.PreReleaseLabel);
		Console.WriteLine("PreReleaseSuffix:             " + BuildVersion.PreReleaseSuffix);

		Console.WriteLine("\nDIRECTORIES");
		Console.WriteLine("Project:       " + ProjectDirectory);
		Console.WriteLine("Output:        " + OutputDirectory);
		Console.WriteLine("Source:        " + SourceDirectory);
		Console.WriteLine("NuGet:         " + NuGetDirectory);
		Console.WriteLine("Chocolatey:    " + ChocolateyDirectory);
		Console.WriteLine("Package:       " + PackageDirectory);
		Console.WriteLine("PackageTest:   " + PackageTestDirectory);
		Console.WriteLine("NuGetTest:     " + NuGetTestDirectory);
		Console.WriteLine("ChocoTest:     " + ChocolateyTestDirectory);
		Console.WriteLine("ZipTest:       " + ZipTestDirectory);
		Console.WriteLine("PackageResult: " + PackageResultDirectory);
		Console.WriteLine("NuGetResult:   " + NuGetResultDirectory);
		Console.WriteLine("ChocoResult:   " + ChocolateyResultDirectory);
		Console.WriteLine("ZipResult:     " + ZipResultDirectory);
		Console.WriteLine("Image:         " + ImageDirectory);
		Console.WriteLine("ZipImage:      " + ZipImageDirectory);

		Console.WriteLine("\nBUILD");
		Console.WriteLine("Configuration:   " + Configuration);

        Console.WriteLine("\nUNIT TESTS");
        Console.WriteLine("UnitTests:                 " + UnitTests);
        Console.WriteLine("UnitTestRunner:            " + UnitTestRunner?.GetType().Name ?? "<NUnitLiteRunner>");

		Console.WriteLine("\nPACKAGING");
		Console.WriteLine("PackageTestLevel:          " + PackageTestLevel);
		Console.WriteLine("MyGetPushUrl:              " + MyGetPushUrl);
		Console.WriteLine("NuGetPushUrl:              " + NuGetPushUrl);
		Console.WriteLine("ChocolateyPushUrl:         " + ChocolateyPushUrl);
		Console.WriteLine("MyGetApiKey:               " + (!string.IsNullOrEmpty(MyGetApiKey) ? "AVAILABLE" : "NOT AVAILABLE"));
		Console.WriteLine("NuGetApiKey:               " + (!string.IsNullOrEmpty(NuGetApiKey) ? "AVAILABLE" : "NOT AVAILABLE"));
		Console.WriteLine("ChocolateyApiKey:          " + (!string.IsNullOrEmpty(ChocolateyApiKey) ? "AVAILABLE" : "NOT AVAILABLE"));
        Console.WriteLine("GitHubAccessToken:         " + (!string.IsNullOrEmpty(GitHubAccessToken) ? "AVAILABLE" : "NOT AVAILABLE"));

		Console.WriteLine("\nPACKAGES");
		foreach (var package in Packages)
		{
			Console.WriteLine(package.PackageId);
			Console.WriteLine("  PackageType:               " + package.PackageType);
			Console.WriteLine("  PackageFileName:           " + package.PackageFileName);
			Console.WriteLine("  PackageInstallDirectory:   " + package.PackageInstallDirectory);
            Console.WriteLine("  PackageTestDirectory:      " + package.PackageTestDirectory);
		}

		Console.WriteLine("\nPUBLISHING");
		Console.WriteLine("ShouldPublishToMyGet:      " + ShouldPublishToMyGet);
		Console.WriteLine("ShouldPublishToNuGet:      " + ShouldPublishToNuGet);
		Console.WriteLine("ShouldPublishToChocolatey: " + ShouldPublishToChocolatey);

		Console.WriteLine("\nRELEASING");
		Console.WriteLine("BranchName:                   " + BranchName);
		Console.WriteLine("IsReleaseBranch:              " + IsReleaseBranch);
		Console.WriteLine("IsProductionRelease:          " + IsProductionRelease);
	}
}