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
    Console.WriteLine("\r\n=================================================="); ;
    Console.WriteLine(message);
    Console.WriteLine("==================================================");
}

//////////////////////////////////////////////////////////////////////
// HELPER METHODS - BUILD
//////////////////////////////////////////////////////////////////////

MSBuildSettings CreateMSBuildSettings(string target)
{
    var settings = new MSBuildSettings()
        .SetConfiguration(Configuration)
        .SetVerbosity(Verbosity.Minimal)
        .WithProperty("Version", ProductVersion)
        .WithProperty("ApiFileVersion", SemVer)
        .WithTarget(target)
        // Workaround for https://github.com/Microsoft/msbuild/issues/3626
        .WithProperty("AddSyntheticProjectReferencesForSolutionDependencies", "false");

    if (IsRunningOnWindows())
    {
        // The fallback is in case only a preview of VS is installed.
        var vsInstallation =
            VSWhereLatest(new VSWhereLatestSettings { Requires = "Microsoft.Component.MSBuild" })
            ?? VSWhereLatest(new VSWhereLatestSettings { Requires = "Microsoft.Component.MSBuild", IncludePrerelease = true });

        if (vsInstallation != null)
        {
            var msBuildPath = vsInstallation.CombineWithFilePath(@"MSBuild\Current\Bin\MSBuild.exe");

            if (!FileExists(msBuildPath))
                msBuildPath = vsInstallation.CombineWithFilePath(@"MSBuild\15.0\Bin\MSBuild.exe");

            if (FileExists(msBuildPath))
                settings.ToolPath = msBuildPath;
        }
    }

    return settings;
}

DotNetMSBuildSettings CreateDotNetMSBuildSettings(string target)
{
    return new DotNetMSBuildSettings()
        .SetConfiguration(Configuration)
        .WithProperty("Version", ProductVersion)
        .WithProperty("ApiFileVersion", SemVer)
        .WithTarget(target);
}

//////////////////////////////////////////////////////////////////////
// HELPER METHODS - TEST
//////////////////////////////////////////////////////////////////////

FilePath GetResultXmlPath(string testAssembly, string framework)
{
    var assemblyName = System.IO.Path.GetFileNameWithoutExtension(testAssembly);

    // Required for test suites running under NUnitLite
    CreateDirectory($@"test-results\{framework}");

    return MakeAbsolute(new FilePath($@"test-results\{framework}\{assemblyName}.xml"));
}

void RunNUnitLiteTests(string testAssembly, string framework)
{
    var workingDir = BIN_DIR + framework + "/";
    var assemblyPath = workingDir + testAssembly;

    int rc = StartProcess(
        assemblyPath,
        new ProcessSettings()
        {
            Arguments = new ProcessArgumentBuilder()
                .AppendSwitchQuoted("--result", ":", GetResultXmlPath(testAssembly, framework).FullPath)
                .Render(),
            WorkingDirectory = workingDir
        });

    if (rc > 0)
        UnreportedErrors.Add($"{testAssembly}({framework}): {rc} tests failed");
    else if (rc < 0)
        UnreportedErrors.Add($"{testAssembly}({framework}) returned rc = {rc}");
}

void RunDotnetNUnitLiteTests(string testAssembly, string framework)
{
    var workingDir = BIN_DIR + framework + "/";
    var assemblyPath = workingDir + testAssembly;

    int rc = StartProcess(
        "dotnet",
        new ProcessSettings
        {
            Arguments = new ProcessArgumentBuilder()
                .AppendQuoted(assemblyPath)
                .AppendSwitchQuoted("--result", ":", GetResultXmlPath(assemblyPath, framework).FullPath)
                .Render(),
            WorkingDirectory = workingDir
        });

    if (rc > 0)
        UnreportedErrors.Add($"{testAssembly}({framework}): {rc} tests failed");
    else if (rc < 0)
        UnreportedErrors.Add($"{testAssembly}({framework}) returned rc = {rc}");
}

void RunNet20Console(string testAssembly, string framework)
{
    var workingDir = BIN_DIR + framework;

    int rc = StartProcess(
        NET20_CONSOLE,
        new ProcessSettings()
        {
            Arguments = new ProcessArgumentBuilder()
                .Append(testAssembly)
                .AppendSwitchQuoted("--result", ":", GetResultXmlPath(testAssembly, framework).FullPath)
                .Render(),
            WorkingDirectory = workingDir
        });

    if (rc > 0)
        UnreportedErrors.Add($"{testAssembly}({framework}): {rc} tests failed");
    else if (rc < 0)
        UnreportedErrors.Add($"{testAssembly}({framework}) returned rc = {rc}");
}

void RunNetCoreConsole(string testAssembly, string framework)
{
    var workingDir = BIN_DIR + framework + "/";
    var assemblyPath = workingDir + testAssembly;

    int rc = StartProcess(
        "dotnet",
        new ProcessSettings
        {
            Arguments = new ProcessArgumentBuilder()
                .AppendQuoted(NETCORE31_CONSOLE)
                .AppendQuoted(assemblyPath)
                .AppendSwitchQuoted("--result", ":", GetResultXmlPath(assemblyPath, framework).FullPath)
                .Render(),
            WorkingDirectory = workingDir
        });

    if (rc > 0)
        UnreportedErrors.Add($"{testAssembly}({framework}): {rc} tests failed");
    else if (rc < 0)
        UnreportedErrors.Add($"{testAssembly}({framework}) returned rc = {rc}");
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
