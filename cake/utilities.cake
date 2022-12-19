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

void BuildSolution(BuildSettings settings)
{
    MSBuild(SOLUTION_FILE, CreateMSBuildSettings("Build", settings).WithRestore());
}

MSBuildSettings CreateMSBuildSettings(string target, BuildSettings settings)
{
    var msbuildSettings = new MSBuildSettings()
        .SetConfiguration(Configuration)
        .SetVerbosity(Verbosity.Minimal)
        .WithProperty("Version", settings.ProductVersion)
        .WithProperty("ApiFileVersion", settings.SemVer + ".0")
        .WithTarget(target)
        // Workaround for https://github.com/Microsoft/msbuild/issues/3626
        .WithProperty("AddSyntheticProjectReferencesForSolutionDependencies", "false");

    if (BuildSystem.IsRunningOnAppVeyor)
        msbuildSettings.ToolPath = System.IO.Path.GetFullPath(@".dotnetsdk\sdk\7.0.100-rc.2.22477.23\MSBuild.dll");
    else 
    if (IsRunningOnWindows())
    {
        // Originally, we only used previews when no other version is installed.
        // Currently we use the latest version, even if it is a preview.
        var vsInstallation =
            VSWhereLatest(new VSWhereLatestSettings { Requires = "Microsoft.Component.MSBuild", IncludePrerelease = true });

        if (vsInstallation != null)
        {
            var msBuildPath = vsInstallation.CombineWithFilePath(@"MSBuild\Current\Bin\MSBuild.exe");

            if (!FileExists(msBuildPath))
                msBuildPath = vsInstallation.CombineWithFilePath(@"MSBuild\15.0\Bin\MSBuild.exe");

            if (FileExists(msBuildPath))
            {
                msbuildSettings.ToolPath = msBuildPath;
                Information("Using MSBuild at " + msBuildPath);
            }
        }
    }

    return msbuildSettings;
}

DotNetMSBuildSettings CreateDotNetMSBuildSettings(string target, BuildSettings settings)
{
    return new DotNetMSBuildSettings()
        .SetConfiguration(Configuration)
        .WithProperty("Version", settings.ProductVersion)
        .WithProperty("ApiFileVersion", settings.SemVer + ".0")
        .WithTarget(target);
}

private void BuildEachProjectSeparately(BuildSettings settings)
{
    Information($"Restoring {SOLUTION_FILE}");
    DotNetRestore(SOLUTION_FILE);

    BuildProject(ENGINE_API_PROJECT, settings);

    BuildProject(MOCK_ASSEMBLY_PROJECT, settings);
    BuildProject(MOCK_ASSEMBLY_X86_PROJECT, settings);
    BuildProject(NOTEST_PROJECT, settings);
    if (IsRunningOnWindows())
        BuildProject(WINDOWS_TEST_PROJECT, settings);
    BuildProject(ASPNETCORE_TEST_PROJECT, settings);

    BuildProject(ENGINE_CORE_PROJECT, settings);
    BuildProject(AGENT_PROJECT, settings);
    BuildProject(AGENT_X86_PROJECT, settings);
    BuildProject(ENGINE_PROJECT, settings);
    
    BuildProject(NETFX_CONSOLE_PROJECT, settings);
    BuildProject(NETCORE_CONSOLE_PROJECT, settings);

    BuildProject(ENGINE_TESTS_PROJECT, settings);
    BuildProject(ENGINE_CORE_TESTS_PROJECT, settings);
    BuildProject(CONSOLE_TESTS_PROJECT, settings);
}

// NOTE: If we use DotNet to build on Linux, then our net35 projects fail.
// If we use MSBuild, then the net5.0 projects fail. So we build each project
// differently depending on whether it has net35 as one of its targets. 
void BuildProject(string project, BuildSettings settings, params string[] targetFrameworks)
{
    if (targetFrameworks.Length == 0)
    {
        DisplayBanner($"Building {System.IO.Path.GetFileName(project)}");
        DotNetMSBuild(project, CreateDotNetMSBuildSettings("Build", settings));
    }
    else
    {
        foreach (var framework in targetFrameworks)
        {
            DisplayBanner($"Building {System.IO.Path.GetFileName(project)} for {framework}");
            if (framework == "net35" || framework.StartsWith("net4"))
                MSBuild(project, CreateMSBuildSettings("Build", settings).WithProperty("TargetFramework", framework));
            else
                DotNetMSBuild(project, CreateDotNetMSBuildSettings("Build", settings).WithProperty("TargetFramework", framework));
        }
    }
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

void RunNUnitLiteTests(string projectPath, string targetRuntime, string additionalArgs="")
{
    var testAssembly = System.IO.Path.GetFileNameWithoutExtension(projectPath) + ".exe";
    var workingDir = GetProjectBinDir(projectPath, targetRuntime);
    var resultPath = GetResultXmlPath( testAssembly, targetRuntime).FullPath;

    int rc = StartProcess(
        workingDir + testAssembly,
        new ProcessSettings()
        {
            Arguments = $"--result:{resultPath} {additionalArgs}",
            WorkingDirectory = workingDir
        });

    if (rc > 0)
        UnreportedErrors.Add($"{testAssembly}({targetRuntime}): {rc} tests failed");
    else if (rc < 0)
        UnreportedErrors.Add($"{testAssembly}({targetRuntime}) returned rc = {rc}");
}

void RunDotnetNUnitLiteTests(string projectPath, string targetRuntime, string additionalArgs="")
{
    var testAssembly = System.IO.Path.GetFileNameWithoutExtension(projectPath) + ".dll";
    var workingDir = GetProjectBinDir(projectPath, targetRuntime);
    var assemblyPath = workingDir + testAssembly;
    var resultPath = GetResultXmlPath(assemblyPath, targetRuntime).FullPath;

    int rc = StartProcess(
        "dotnet",
        new ProcessSettings
        {
            Arguments = $"\"{assemblyPath}\" --result:\"{resultPath}\" {additionalArgs}",
            WorkingDirectory = workingDir
        });

    if (rc > 0)
        UnreportedErrors.Add($"{testAssembly}({targetRuntime}): {rc} tests failed");
    else if (rc < 0)
        UnreportedErrors.Add($"{testAssembly}({targetRuntime}) returned rc = {rc}");
}

void RunNetFxConsole(string projectPath, string targetRuntime, string additionalArgs="")
{
    var testAssembly = System.IO.Path.GetFileNameWithoutExtension(projectPath) + ".dll";
    var workingDir = GetProjectBinDir(projectPath, targetRuntime);
    var assemblyPath = workingDir + testAssembly;
    var resultPath = GetResultXmlPath(assemblyPath, targetRuntime).FullPath;

    int rc = StartProcess(
        NETFX_CONSOLE,
        new ProcessSettings()
        {
            Arguments = $"\"{assemblyPath}\" --result:{resultPath} {additionalArgs}",
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
