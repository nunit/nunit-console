//////////////////////////////////////////////////////////////////////
// HELPER METHODS - GENERAL
//////////////////////////////////////////////////////////////////////

T GetArgument<T>(string pattern, T defaultValue)
{
    foreach (string name in pattern.Split('|'))
        if (HasArgument(name))
            return Argument<T>(name);

    return defaultValue;
}

bool CheckIfDotNetCoreInstalled()
{
    try
    {
        Information("Checking if .NET Core SDK is installed...");
        StartProcess("dotnet", new ProcessSettings
        {
            Arguments = "--info"
        });
    }
    catch (Exception)
    {
        Warning(".NET Core SDK is not installed. It can be installed from https://www.microsoft.com/net/core");
        return false;
    }
    return true;
}

public List<string> GetInstalledNetCoreRuntimes()
{
    var list = new List<string>();

    var process = StartProcess("dotnet",
            new ProcessSettings
            {
                Arguments = "--list-runtimes",
                RedirectStandardOutput = true,
                RedirectedStandardOutputHandler =
                s => {
                    if (s == null || !s.StartsWith("Microsoft.NETCore.App"))
                        return s;

                    var version = s.Split(' ')[1];

                    list.Add(version);
                    return s;
                }
            });
    return list;
}

void DisplayUnreportedErrors()
{
    if (UnreportedErrors.Count > 0)
    {
        string msg = "One or more unit tests failed, breaking the build.\r\n"
          + UnreportedErrors.Aggregate((x, y) => x + "\r\n" + y);

        UnreportedErrors.Clear();
        throw new Exception(msg);
    }
}

public static void DisplayBanner(string message)
{
    var bar = new string('-', Math.Max(message.Length, 40));
    Console.WriteLine();
    Console.WriteLine(bar);
    Console.WriteLine(message);
    Console.WriteLine(bar);
}

//////////////////////////////////////////////////////////////////////
// HELPER METHODS - BUILD
//////////////////////////////////////////////////////////////////////

void BuildSolution()
{
    MSBuild(SOLUTION_FILE, CreateMSBuildSettings("Build").WithRestore());
}

MSBuildSettings CreateMSBuildSettings(string target)
{
    var settings = new MSBuildSettings()
        .SetConfiguration(Configuration)
        .SetVerbosity(Verbosity.Minimal)
        .WithProperty("Version", ProductVersion)
        .WithProperty("ApiFileVersion", SemVer + ".0")
        .WithTarget(target)
        // Workaround for https://github.com/Microsoft/msbuild/issues/3626
        .WithProperty("AddSyntheticProjectReferencesForSolutionDependencies", "false");

    if (BuildSystem.IsRunningOnAppVeyor)
        settings.ToolPath = System.IO.Path.GetFullPath(@".dotnetsdk\sdk\7.0.100-rc.2.22477.23\MSBuild.dll");
    else 
    if (IsRunningOnWindows())
    {
        // The fallback is in case only a preview of VS is installed.
        var vsInstallation =
            //VSWhereLatest(new VSWhereLatestSettings { Requires = "Microsoft.Component.MSBuild" })
            VSWhereLatest(new VSWhereLatestSettings { Requires = "Microsoft.Component.MSBuild", IncludePrerelease = true });

        if (vsInstallation != null)
        {
            var msBuildPath = vsInstallation.CombineWithFilePath(@"MSBuild\Current\Bin\MSBuild.exe");

            if (!FileExists(msBuildPath))
                msBuildPath = vsInstallation.CombineWithFilePath(@"MSBuild\15.0\Bin\MSBuild.exe");

            if (FileExists(msBuildPath))
            {
                settings.ToolPath = msBuildPath;
                Information("Using MSBuild at " + msBuildPath);
            }
        }
    }

    return settings;
}

DotNetMSBuildSettings CreateDotNetMSBuildSettings(string target)
{
    return new DotNetMSBuildSettings()
        .SetConfiguration(Configuration)
        .WithProperty("Version", ProductVersion)
        .WithProperty("ApiFileVersion", SemVer + ".0")
        .WithTarget(target);
}

private void BuildEachProjectSeparately()
{
    DotNetRestore(SOLUTION_FILE);

    BuildProject(ENGINE_API_PROJECT);

    BuildProject(MOCK_ASSEMBLY_PROJECT);//, "net35", "net462", "netcoreapp2.1", "netcoreapp3.1", "net5.0", "net6.0", "net7.0");
    BuildProject(MOCK_ASSEMBLY_X86_PROJECT);//, "net35", "net462", "netcoreapp2.1", "netcoreapp3.1");
    BuildProject(NOTEST_PROJECT);//, "net35", "netcoreapp2.1", "netcoreapp3.1");
    BuildProject(WINDOWS_TEST_PROJECT);
    BuildProject(ASPNETCORE_TEST_PROJECT);

    BuildProject(ENGINE_CORE_PROJECT);
    BuildProject(AGENT_PROJECT);
    BuildProject(AGENT_X86_PROJECT);
    BuildProject(ENGINE_PROJECT);
    
    BuildProject(NETFX_CONSOLE_PROJECT);
    BuildProject(NETCORE_CONSOLE_PROJECT);

    BuildProject(ENGINE_TESTS_PROJECT);//, "net462", "netcoreapp2.1", "netcoreapp3.1");
    BuildProject(ENGINE_CORE_TESTS_PROJECT);//, "net35", "netcoreapp2.1", "netcoreapp3.1", "net5.0", "net6.0");
    BuildProject(CONSOLE_TESTS_PROJECT);//, "net462", "net6.0");

    /*
    DisplayBanner("Publish .NET Core & Standard projects");

    MSBuild(ENGINE_PROJECT, CreateMSBuildSettings("Publish")
       .WithProperty("TargetFramework", "netstandard2.0")
       .WithProperty("PublishDir", BIN_DIR + "netstandard2.0"));
    CopyFileToDirectory(
       BIN_DIR + "netstandard2.0/testcentric.engine.metadata.dll",
       BIN_DIR + "netcoreapp2.1");
    MSBuild(ENGINE_TESTS_PROJECT, CreateMSBuildSettings("Publish")
       .WithProperty("TargetFramework", "netcoreapp2.1")
       .WithProperty("PublishDir", BIN_DIR + "netcoreapp2.1"));
    MSBuild(ENGINE_CORE_TESTS_PROJECT, CreateMSBuildSettings("Publish")
       .WithProperty("TargetFramework", "netcoreapp2.1")
       .WithProperty("PublishDir", BIN_DIR + "netcoreapp2.1"));
    */
}

// NOTE: If we use DotNet to build on Linux, then our net35 projects fail.
// If we use MSBuild, then the net5.0 projects fail. So we build each project
// differently depending on whether it has net35 as one of its targets. 
void BuildProject(string project, params string[] targetFrameworks)
{
    if (targetFrameworks.Length == 0)
    {
        DisplayBanner($"Building {System.IO.Path.GetFileName(project)}");
        DotNetMSBuild(project, CreateDotNetMSBuildSettings("Build"));
    }
    else
    {
        foreach (var framework in targetFrameworks)
        {
            DisplayBanner($"Building {System.IO.Path.GetFileName(project)} for {framework}");
            if (framework == "net35" || framework.StartsWith("net4"))
                MSBuild(project, CreateMSBuildSettings("Build").WithProperty("TargetFramework", framework));
            else
                DotNetMSBuild(project, CreateDotNetMSBuildSettings("Build").WithProperty("TargetFramework", framework));
        }
    }
}

void CopyAgentsToDirectory(string targetDir)
{
    CreateDirectory( targetDir + "agents");
    CopyDirectory(AGENT_PROJECT_BIN_DIR,  targetDir + "agents");
    CopyFiles(AGENT_X86_PROJECT_BIN_DIR + "**/nunit-agent-x86.*", targetDir + "agents", true);
}

//////////////////////////////////////////////////////////////////////
// HELPER METHODS - TEST
//////////////////////////////////////////////////////////////////////

FilePath GetResultXmlPath(string testAssembly, string targetRuntime)
{
    var assemblyName = System.IO.Path.GetFileNameWithoutExtension(testAssembly);

    // Required for test suites running under NUnitLite
    CreateDirectory($@"test-results\{targetRuntime}");

    return MakeAbsolute(new FilePath($@"test-results\{targetRuntime}\{assemblyName}.xml"));
}

string GetProjectBinDir(string projectPath)
{
    var projectDir = System.IO.Path.GetDirectoryName(projectPath);
    return projectDir + $"/bin/{Configuration}/";
}

string GetProjectBinDir(string projectPath, string targetRuntime)
{
    return GetProjectBinDir(projectPath) + targetRuntime + "/";
}

void RunNUnitLiteTests(string projectPath, string targetRuntime)
{
    var testAssembly = System.IO.Path.GetFileNameWithoutExtension(projectPath) + ".exe";
    var workingDir = GetProjectBinDir(projectPath, targetRuntime);
    var resultPath = GetResultXmlPath( testAssembly, targetRuntime).FullPath;

    int rc = StartProcess(
        workingDir + testAssembly,
        new ProcessSettings()
        {
            Arguments = $"--result:{resultPath}",
            WorkingDirectory = workingDir
        });

    if (rc > 0)
        UnreportedErrors.Add($"{testAssembly}({targetRuntime}): {rc} tests failed");
    else if (rc < 0)
        UnreportedErrors.Add($"{testAssembly}({targetRuntime}) returned rc = {rc}");
}

void RunDotnetNUnitLiteTests(string projectPath, string targetRuntime)
{
    var testAssembly = System.IO.Path.GetFileNameWithoutExtension(projectPath) + ".dll";
    var workingDir = GetProjectBinDir(projectPath, targetRuntime);
    var assemblyPath = workingDir + testAssembly;
    var resultPath = GetResultXmlPath(assemblyPath, targetRuntime).FullPath;

    int rc = StartProcess(
        "dotnet",
        new ProcessSettings
        {
            Arguments = $"\"{assemblyPath}\" --result:{resultPath}",
            WorkingDirectory = workingDir
        });

    if (rc > 0)
        UnreportedErrors.Add($"{testAssembly}({targetRuntime}): {rc} tests failed");
    else if (rc < 0)
        UnreportedErrors.Add($"{testAssembly}({targetRuntime}) returned rc = {rc}");
}

void RunNetFxConsole(string projectPath, string targetRuntime)
{
    var testAssembly = System.IO.Path.GetFileNameWithoutExtension(projectPath) + ".dll";
    var workingDir = GetProjectBinDir(projectPath, targetRuntime);
    var assemblyPath = workingDir + testAssembly;
    var resultPath = GetResultXmlPath(assemblyPath, targetRuntime).FullPath;

    int rc = StartProcess(
        NETFX_CONSOLE,
        new ProcessSettings()
        {
            Arguments = $"\"{assemblyPath}\" --result:{resultPath}",
            WorkingDirectory = workingDir
        });

    if (rc > 0)
        UnreportedErrors.Add($"{testAssembly}({targetRuntime}): {rc} tests failed");
    else if (rc < 0)
        UnreportedErrors.Add($"{testAssembly}({targetRuntime}) returned rc = {rc}");
}

void RunNetCoreConsole(string projectPath, string targetRuntime)
{
    var testAssembly = System.IO.Path.GetFileNameWithoutExtension(projectPath) + ".dll";
    var workingDir = GetProjectBinDir(projectPath, targetRuntime);
    var assemblyPath = workingDir + testAssembly;
    var resultPath = GetResultXmlPath(assemblyPath, targetRuntime).FullPath;

    int rc = StartProcess(
        "dotnet",
        new ProcessSettings
        {
            Arguments = $"\"{NETCORE_CONSOLE}\" \"{assemblyPath}\" --result:{resultPath}",
            WorkingDirectory = workingDir
        });

    if (rc > 0)
        UnreportedErrors.Add($"{testAssembly}({targetRuntime}): {rc} tests failed");
    else if (rc < 0)
        UnreportedErrors.Add($"{testAssembly}({targetRuntime}) returned rc = {rc}");
}

//////////////////////////////////////////////////////////////////////
// HELPER METHODS - PACKAGING
//////////////////////////////////////////////////////////////////////

public int VerifyPackage(PackageDefinition package)
{
    int failures = 0;

    if (!CheckPackage($"{PACKAGE_DIR}{package.PackageName}", package.PackageChecks))
        ++failures;

    if (package.HasSymbols && !CheckPackage($"{PACKAGE_DIR}{package.SymbolPackageName}", package.SymbolChecks))
        ++failures;

    return failures;
}


public void CopyPackageContents(DirectoryPath packageDir, DirectoryPath outDir)
{
    var files = GetFiles(packageDir + "/tools/*").Concat(GetFiles(packageDir + "/tools/net20/*"));
    CopyFiles(files.Where(f => f.GetExtension() != ".addins"), outDir);
}

public void PushNuGetPackage(FilePath package, string apiKey, string url)
{
	CheckPackageExists(package);
    if (NoPush)
        Information($"Push {package} to {url}");
    else
        NuGetPush(package, new NuGetPushSettings() { ApiKey = apiKey, Source = url });
}

public void PushChocolateyPackage(FilePath package, string apiKey, string url)
{
	CheckPackageExists(package);
    if (NoPush)
        Information($"Push {package} to {url}");
    else
        ChocolateyPush(package, new ChocolateyPushSettings() { ApiKey = apiKey, Source = url });
}

private void CheckPackageExists(FilePath package)
{
	if (!FileExists(package))
		throw new InvalidOperationException(
			$"Package not found: {package.GetFilename()}.\nCode may have changed since package was last built.");
}

public bool IsPreRelease => !string.IsNullOrEmpty(PreReleaseLabel);

public bool ShouldPublishToMyGet => IsPreRelease && LABELS_WE_PUBLISH_ON_MYGET.Contains(PreReleaseLabel);
public bool ShouldPublishToNuGet => !IsPreRelease || LABELS_WE_PUBLISH_ON_NUGET.Contains(PreReleaseLabel);
public bool ShouldPublishToChocolatey => !IsPreRelease || LABELS_WE_PUBLISH_ON_CHOCOLATEY.Contains(PreReleaseLabel);
public bool IsProductionRelease => !IsPreRelease || LABELS_WE_RELEASE_ON_GITHUB.Contains(PreReleaseLabel);
