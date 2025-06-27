public static class BuildSettings
{
    // Defaults
    private static readonly string[] DEFAULT_VALID_CONFIGS = { "Release", "Debug" };
    private static readonly string[] DEFAULT_STANDARD_HEADER = new[] {
    "// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt" };

    // URLs for uploading packages
    private const string MYGET_PUSH_URL = "https://www.myget.org/F/nunit/api/v2";
    private const string NUGET_PUSH_URL = "https://api.nuget.org/v3/index.json";
    private const string CHOCO_PUSH_URL = "https://push.chocolatey.org/";

    // Environment Variable names holding API keys
    private const string MYGET_API_KEY = "MYGET_API_KEY";
    private const string NUGET_API_KEY = "NUGET_API_KEY";
    private const string CHOCO_API_KEY = "CHOCO_API_KEY";
    private const string GITHUB_ACCESS_TOKEN = "GITHUB_ACCESS_TOKEN";

    // Pre-release labels that we publish
    private static readonly string[] LABELS_WE_PUBLISH = { "dev", "alpha", "beta", "rc" };
    private static readonly string[] LABELS_WE_PUBLISH_ON_MYGET = { "dev", "alpha", "beta", "rc" };
    private static readonly string[] LABELS_WE_PUBLISH_ON_NUGET = { "beta", "rc" };
    private static readonly string[] LABELS_WE_PUBLISH_ON_CHOCOLATEY = { "beta", "rc" };
    private static readonly string[] LABELS_WE_PUBLISH_ON_GITHUB = { "beta", "rc" };
    private static readonly string[] LABELS_USED_AS_TAGS = { "alpha", "beta", "rc" };

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
        bool buildWithMSBuild = false,

        DotNetVerbosity dotnetVerbosity = DotNetVerbosity.Minimal,
        Verbosity msbuildVerbosity = Verbosity.Minimal,
        NuGetVerbosity nugetVerbosity = NuGetVerbosity.Normal,
        bool chocolateyVerbosity = false,

        string[] validConfigurations = null,
        string githubOwner = "NUnit",

        string unitTests = null, // Defaults to "**/*.tests.dll|**/*.tests.exe" (case insensitive)
        IUnitTestRunner unitTestRunner = null, // If not set, NUnitLite is used
        string unitTestArguments = null,

        string defaultTarget = null // Defaults to "Build"
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
        BuildWithMSBuild = buildWithMSBuild;

        DotNetVerbosity = dotnetVerbosity;
        MSBuildVerbosity = msbuildVerbosity;
        NuGetVerbosity = nugetVerbosity;
        ChocolateyVerbosity = chocolateyVerbosity;

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

        // Skip remaining initialization if help was requested
        if (CommandLineOptions.Usage)
            return;

        ValidateSettings();

        context.Information($"{Title} {Configuration} version {PackageVersion}");

        // Output like this should go after the run title display
        if (solutionFile == null && SolutionFile != null)
            Context.Warning($"  SolutionFile: '{SolutionFile}'");
        Context.Information($"  PackageTestLevel: {PackageTestLevel}");
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
        // for some reason. This check is a workaround.
        if (IsRunningOnGitHubActions && _buildSystem.GitHubActions.Environment.PullRequest.IsPullRequest)
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
    public static bool IsRunningOnGitHubActions => _buildSystem.GitHubActions.IsRunningOnGitHubActions;

    // Versioning
    public static BuildVersion BuildVersion { get; private set; }
    public static string BranchName => BuildVersion.BranchName;
    public static bool IsReleaseBranch => BuildVersion.IsReleaseBranch;
    public static string PackageVersion => BuildVersion.PackageVersion;
    public static string LegacyPackageVersion => BuildVersion.LegacyPackageVersion;
    public static string AssemblyVersion => BuildVersion.AssemblyVersion;
    public static string AssemblyFileVersion => BuildVersion.AssemblyFileVersion;
    public static string AssemblyInformationalVersion => BuildVersion.AssemblyInformationalVersion;
    public static bool IsDevelopmentRelease => PackageVersion.Contains("-dev");

    // Standard Directory Structure - not changeable by user
    public static string ProjectDirectory => Context.Environment.WorkingDirectory.FullPath + "/";
    public static string SourceDirectory => ProjectDirectory + "src/";
    public static string OutputDirectory => ProjectDirectory + "bin/" + Configuration + "/";
    public static string NuGetDirectory => ProjectDirectory + "nuget/";
    public static string ChocolateyDirectory => ProjectDirectory + "choco/";
    public static string PackageDirectory => ProjectDirectory + "package/";
    public static string PackageTestDirectory => PackageDirectory + "tests/";
    public static string NuGetTestDirectory => PackageTestDirectory + "nuget/";
    public static string ChocolateyTestDirectory => PackageTestDirectory + "choco/";
    public static string PackageResultDirectory => PackageDirectory + "results/";
    public static string NuGetResultDirectory => PackageResultDirectory + "nuget/";
    public static string ChocolateyResultDirectory => PackageResultDirectory + "choco/";
    public static string ImageDirectory => PackageDirectory + "images/";
    public static string ToolsDirectory => ProjectDirectory + "tools/";

    // Files
    public static string SolutionFile { get; set; }

    // Building
    public static string[] ValidConfigurations { get; set; }

    public static DotNetVerbosity DotNetVerbosity { get; set; }
    public static Verbosity MSBuildVerbosity { get; set; }
    public static NuGetVerbosity NuGetVerbosity { get; set; }
    // The chocolatey Setting is actually bool Verbose, but we use verbosity 
    // so it lines up with the settings for NuGet
    public static bool ChocolateyVerbosity { get; set; }

    public static bool BuildWithMSBuild { get; set; }
    public static MSBuildSettings MSBuildSettings => new MSBuildSettings
    {
        Verbosity = MSBuildVerbosity,
        Configuration = Configuration,
        PlatformTarget = PlatformTarget.MSIL
    };
    public static DotNetBuildSettings DotNetBuildSettings => new DotNetBuildSettings
    {
        Configuration = Configuration,
        NoRestore = true,
        Verbosity = DotNetVerbosity,
        MSBuildSettings = new DotNetMSBuildSettings
        {
            BinaryLogger = new MSBuildBinaryLoggerSettings
            {
                Enabled = true,
                FileName = "build-results/NUnitConsole.binlog",
                Imports = MSBuildBinaryLoggerImports.Embed
            }
        }.WithProperty("Version", PackageVersion)
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
    public static List<PackageDefinition> SelectedPackages { get; } = new List<PackageDefinition>();

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

    // Publishing - Policies
    public static bool IsPreRelease => BuildVersion.IsPreRelease;
    public static bool ShouldPublishRelease =>
        !IsPreRelease || LABELS_WE_PUBLISH.Contains(BuildVersion.PreReleaseLabel);
    public static bool ShouldPublishToMyGet =>
        !IsPreRelease || LABELS_WE_PUBLISH_ON_MYGET.Contains(BuildVersion.PreReleaseLabel);
    public static bool ShouldPublishToNuGet =>
        !IsPreRelease || LABELS_WE_PUBLISH_ON_NUGET.Contains(BuildVersion.PreReleaseLabel) && !IsFractionalPreRelease;
    public static bool ShouldPublishToChocolatey =>
        !IsPreRelease || LABELS_WE_PUBLISH_ON_CHOCOLATEY.Contains(BuildVersion.PreReleaseLabel) && !IsFractionalPreRelease;
    public static bool ShouldPublishToGitHub =>
        !IsPreRelease || LABELS_WE_PUBLISH_ON_GITHUB.Contains(BuildVersion.PreReleaseLabel) && !IsFractionalPreRelease;
    public static bool IsFractionalPreRelease
    {
        get
        {
            int dots = 0;
            foreach (char c in BuildVersion.PreReleaseSuffix)
                if (c == '.') dots++;
            return dots > 1;
        }
    }

    private static void ValidateSettings()
    {
        var validationErrors = new List<string>();

        if (!ValidConfigurations.Contains(Configuration))
            validationErrors.Add($"Invalid configuration: {Configuration}");

        if (validationErrors.Count > 0)
        {
            var msg = new StringBuilder("Parameter validation failed! Errors found:\n");
            foreach (var error in validationErrors)
                msg.AppendLine("  " + error);

            throw new InvalidOperationException(msg.ToString());
        }
    }
}
