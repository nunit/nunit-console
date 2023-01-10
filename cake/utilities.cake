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

MSBuildSettings CreateMSBuildSettings(string target, BuildSettings buildSettings)
{
    var msbuildSettings = new MSBuildSettings()
        .SetConfiguration(buildSettings.Configuration)
        .SetVerbosity(Verbosity.Minimal)
        .WithProperty("Version", buildSettings.ProductVersion)
        .WithProperty("ApiFileVersion", buildSettings.SemVer + ".0")
        .WithTarget(target)
        // Workaround for https://github.com/Microsoft/msbuild/issues/3626
        .WithProperty("AddSyntheticProjectReferencesForSolutionDependencies", "false");

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

DotNetMSBuildSettings CreateDotNetMSBuildSettings(string target, BuildSettings buildSettings)
{
    return new DotNetMSBuildSettings()
        .SetConfiguration(buildSettings.Configuration)
        .WithProperty("Version", buildSettings.ProductVersion)
        .WithProperty("ApiFileVersion", buildSettings.SemVer + ".0")
        .WithTarget(target);
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

string GetProjectBinDir(string projectPath, string configuration)
{
    var projectDir = System.IO.Path.GetDirectoryName(projectPath);
    return projectDir + $"/bin/{configuration}/";
}

string GetProjectBinDir(string projectPath, string configuration, string targetRuntime)
{
    return GetProjectBinDir(projectPath, configuration) + targetRuntime + "/";
}

void RunNUnitLiteTests(string projectPath, string configuration, string targetRuntime)
{
    var testAssembly = System.IO.Path.GetFileNameWithoutExtension(projectPath) + ".exe";
    var workingDir = GetProjectBinDir(projectPath, configuration, targetRuntime);
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

void RunDotnetNUnitLiteTests(string projectPath, string configuration, string targetRuntime)
{
    var testAssembly = System.IO.Path.GetFileNameWithoutExtension(projectPath) + ".dll";
    var workingDir = GetProjectBinDir(projectPath, configuration, targetRuntime);
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

void RunNetFxConsole(string projectPath, string configuration, string targetRuntime)
{
    var testAssembly = System.IO.Path.GetFileNameWithoutExtension(projectPath) + ".dll";
    var workingDir = GetProjectBinDir(projectPath, configuration, targetRuntime);
    var assemblyPath = workingDir + testAssembly;
    var resultPath = GetResultXmlPath(assemblyPath, targetRuntime).FullPath;

    int rc = StartProcess(
        NETFX_CONSOLE_DIR + $"bin/{configuration}/{NETFX_CONSOLE_TARGET}/nunit4-console.exe",
        new ProcessSettings()
        {
            Arguments = $"\"{assemblyPath}\" --result:\"{resultPath}\" --trace:Debug",
            WorkingDirectory = workingDir
        });

    if (rc > 0)
        UnreportedErrors.Add($"{testAssembly}({targetRuntime}): {rc} tests failed");
    else if (rc < 0)
        UnreportedErrors.Add($"{testAssembly}({targetRuntime}) returned rc = {rc}");
}

void RunNetCoreConsole(string projectPath, string configuration, string targetRuntime)
{
    var testAssembly = System.IO.Path.GetFileNameWithoutExtension(projectPath) + ".dll";
    var workingDir = GetProjectBinDir(projectPath, configuration, targetRuntime);
    var assemblyPath = workingDir + testAssembly;
    var resultPath = GetResultXmlPath(assemblyPath, targetRuntime).FullPath;

    int rc = StartProcess(
        "dotnet",
        new ProcessSettings
        {
            Arguments = $"\"{NETCORE_CONSOLE_DIR}bin/{configuration}/{NETCORE_CONSOLE_TARGET}/nunit4-netcore-console.dll\" \"{assemblyPath}\" --result:{resultPath}",
            WorkingDirectory = workingDir
        });

    if (rc > 0)
        UnreportedErrors.Add($"{testAssembly}({targetRuntime}): {rc} tests failed");
    else if (rc < 0)
        UnreportedErrors.Add($"{testAssembly}({targetRuntime}) returned rc = {rc}");
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
    NuGetPush(package, new NuGetPushSettings() { ApiKey = apiKey, Source = url });
}

public void PushChocolateyPackage(FilePath package, string apiKey, string url)
{
	CheckPackageExists(package);
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
