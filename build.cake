#load ci.cake
#load package-checks.cake

// Install Tools
#tool NuGet.CommandLine&version=5.3.1

//////////////////////////////////////////////////////////////////////
// ARGUMENTS & INITIALISATION
//////////////////////////////////////////////////////////////////////

var target = Argument("target", "Default");
var configuration = Argument("configuration", "Release");
var productVersion = Argument("productVersion", "3.12.0-beta2");

var ErrorDetail = new List<string>();
var installedNetCoreRuntimes = GetInstalledNetCoreRuntimes();

//////////////////////////////////////////////////////////////////////
// SET PACKAGE VERSION
//////////////////////////////////////////////////////////////////////

var dash = productVersion.IndexOf('-');
var version = dash > 0
    ? productVersion.Substring(0, dash)
    : productVersion;

var isAppveyor = BuildSystem.IsRunningOnAppVeyor;

//////////////////////////////////////////////////////////////////////
// DEFINE RUN CONSTANTS
//////////////////////////////////////////////////////////////////////

var PROJECT_DIR = Context.Environment.WorkingDirectory.FullPath + "/";
var PACKAGE_DIR = Argument("artifact-dir", PROJECT_DIR + "package") + "/";
var BIN_DIR = PROJECT_DIR + "bin/" + configuration + "/";
var NET35_BIN_DIR = BIN_DIR + "net35/";
var NETCOREAPP11_BIN_DIR = BIN_DIR + "netcoreapp1.1/";
var NETCOREAPP21_BIN_DIR = BIN_DIR + "netcoreapp2.1/";
var NETCOREAPP31_BIN_DIR = BIN_DIR + "netcoreapp3.1/";
var CHOCO_DIR = PROJECT_DIR + "choco/";
var TOOLS_DIR = PROJECT_DIR + "tools/";
var IMAGE_DIR = PROJECT_DIR + "images/";
var MSI_DIR = PROJECT_DIR + "msi/";
var CURRENT_IMG_DIR = IMAGE_DIR + $"NUnit-{productVersion}/";
var CURRENT_IMG_NET20_BIN_DIR = CURRENT_IMG_DIR + "bin/net20/";
var EXTENSION_PACKAGES_DIR = PROJECT_DIR + "extension-packages/";
var ZIP_IMG = PROJECT_DIR + "zip-image/";

var SOLUTION_FILE = PROJECT_DIR + "NUnitConsole.sln";
var ENGINE_CSPROJ = PROJECT_DIR + "src/NUnitEngine/nunit.engine/nunit.engine.csproj";
var ENGINE_API_CSPROJ = PROJECT_DIR + "src/NUnitEngine/nunit.engine.api/nunit.engine.api.csproj";
var ENGINE_TESTS_CSPROJ = PROJECT_DIR + "src/NUnitEngine/nunit.engine.tests/nunit.engine.tests.csproj";
var CONSOLE_CSPROJ = PROJECT_DIR + "src/NUnitConsole/nunit3-console/nunit3-console.csproj";
var CONSOLE_TESTS_CSPROJ = PROJECT_DIR + "src/NUnitConsole/nunit3-console.tests/nunit3-console.tests.csproj";
var MOCK_ASSEMBLY_CSPROJ = PROJECT_DIR + "src/NUnitEngine/mock-assembly/mock-assembly.csproj";

var NETFX_FRAMEWORKS = new [] { "net20", "net35" }; //Production code targets net20, tests target nets35

// Test Runners
var NET20_CONSOLE = BIN_DIR + "net20/" + "nunit3-console.exe";
var NETCORE31_CONSOLE = BIN_DIR + "netcoreapp3.1/" + "nunit3-console.dll";

// Test Assemblies
var ENGINE_TESTS = "nunit.engine.tests.dll";
var CONSOLE_TESTS = "nunit3-console.tests.dll";

// Package sources for nuget restore
var PACKAGE_SOURCE = new string[]
{
    "https://www.nuget.org/api/v2",
    "https://www.myget.org/F/nunit/api/v2"
};

var EXTENSION_PACKAGES = new []
{
  "NUnit.Extension.VSProjectLoader",
  "NUnit.Extension.NUnitProjectLoader",
  "NUnit.Extension.NUnitV2Driver",
  "NUnit.Extension.NUnitV2ResultWriter",
  "NUnit.Extension.TeamCityEventListener"
};

//////////////////////////////////////////////////////////////////////
// SETUP AND TEARDOWN TASKS
//////////////////////////////////////////////////////////////////////
Setup(context =>
{
    if (BuildSystem.IsRunningOnAppVeyor)
    {
        var buildNumber = AppVeyor.Environment.Build.Number.ToString("00000");
        var branch = AppVeyor.Environment.Repository.Branch;
        var isPullRequest = AppVeyor.Environment.PullRequest.IsPullRequest;

        if (branch == "master" && !isPullRequest)
        {
            productVersion = version + "-dev-" + buildNumber;
        }
        else
        {
            var suffix = "-ci-" + buildNumber;

            if (isPullRequest)
                suffix += "-pr-" + AppVeyor.Environment.PullRequest.Number;
            else if (AppVeyor.Environment.Repository.Branch.StartsWith("release", StringComparison.OrdinalIgnoreCase))
                suffix += "-pre-" + buildNumber;
            else
                suffix += "-" + System.Text.RegularExpressions.Regex.Replace(branch, "[^0-9A-Za-z-]+", "-");

            // Nuget limits "special version part" to 20 chars. Add one for the hyphen.
            if (suffix.Length > 21)
                suffix = suffix.Substring(0, 21);

            productVersion = version + suffix;
        }

        AppVeyor.UpdateBuildVersion(productVersion);
    }

    // Executed BEFORE the first task.
    Information("Building {0} version {1} of NUnit Console/Engine.", configuration, productVersion);
});

Teardown(context =>
{
    // Executed AFTER the last task.
    CheckForError(ref ErrorDetail);
});

//////////////////////////////////////////////////////////////////////
// CLEAN
//////////////////////////////////////////////////////////////////////

Task("Clean")
    .Description("Cleans directories.")
    .Does(() =>
    {
        CleanDirectory(PROJECT_DIR + "bin/");
        CleanDirectory(PROJECT_DIR + "packages/");
        CleanDirectory(IMAGE_DIR);
        CleanDirectory(EXTENSION_PACKAGES_DIR);
        CleanDirectory(ZIP_IMG);
        CleanDirectory(PACKAGE_DIR);
    });

//////////////////////////////////////////////////////////////////////
// INITIALIZE FOR BUILD
//////////////////////////////////////////////////////////////////////

Task("UpdateAssemblyInfo")
    .Description("Sets the assembly versions to the calculated version.")
    .Does(() =>
    {
        PatchAssemblyInfo("src/NUnitConsole/ConsoleVersion.cs", productVersion, version);
        PatchAssemblyInfo("src/NUnitEngine/EngineApiVersion.cs", productVersion, assemblyVersion: null);
        PatchAssemblyInfo("src/NUnitEngine/EngineVersion.cs", productVersion, version);
    });

//////////////////////////////////////////////////////////////////////
// BUILD ENGINE AND CONSOLE
//////////////////////////////////////////////////////////////////////

MSBuildSettings CreateMSBuildSettings(string target)
{
    var settings = new MSBuildSettings()
        .SetConfiguration(configuration)
        .SetVerbosity(Verbosity.Minimal)
        .WithProperty("PackageVersion", productVersion)
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

Task("Build")
    .Description("Builds the engine and console")
    .IsDependentOn("UpdateAssemblyInfo")
    .Does(() =>
    {
        MSBuild(SOLUTION_FILE, CreateMSBuildSettings("Build").WithRestore());

        Information("Publishing .NET Core & Standard projects so that dependencies are present...");

        foreach(var framework in new [] { "netstandard1.6", "netstandard2.0", "netcoreapp3.1" })
             MSBuild(ENGINE_CSPROJ, CreateMSBuildSettings("Publish")
                .WithProperty("TargetFramework", framework)
                .WithProperty("PublishDir", BIN_DIR + framework));

        foreach(var framework in new [] { "netstandard1.6", "netstandard2.0" })
             MSBuild(ENGINE_API_CSPROJ, CreateMSBuildSettings("Publish")
                .WithProperty("TargetFramework", framework)
                .WithProperty("PublishDir", BIN_DIR + framework));

        foreach(var framework in new [] { "netcoreapp1.1", "netcoreapp2.1", "netcoreapp3.1" })
             MSBuild(ENGINE_TESTS_CSPROJ, CreateMSBuildSettings("Publish")
                .WithProperty("TargetFramework", framework)
                .WithProperty("PublishDir", BIN_DIR + framework));

        MSBuild(CONSOLE_CSPROJ, CreateMSBuildSettings("Publish")
            .WithProperty("TargetFramework", "netcoreapp3.1")
            .WithProperty("PublishDir", BIN_DIR + "netcoreapp3.1"));

         MSBuild(CONSOLE_TESTS_CSPROJ, CreateMSBuildSettings("Publish")
            .WithProperty("TargetFramework", "netcoreapp3.1")
            .WithProperty("PublishDir", BIN_DIR + "netcoreapp3.1"));

    });

//////////////////////////////////////////////////////////////////////
// BUILD C++ TESTS
//////////////////////////////////////////////////////////////////////

Task("BuildCppTestFiles")
    .Description("Builds the C++ mock test assemblies")
    .WithCriteria(IsRunningOnWindows)
    .Does(() =>
    {
        MSBuild(
            "./src/NUnitEngine/mock-cpp-clr/mock-cpp-clr-x86.vcxproj",
             CreateMSBuildSettings("Build").WithProperty("Platform", "x86"));

        MSBuild(
            "./src/NUnitEngine/mock-cpp-clr/mock-cpp-clr-x64.vcxproj",
            CreateMSBuildSettings("Build").WithProperty("Platform", "x64"));
    });

//////////////////////////////////////////////////////////////////////
// TEST
//////////////////////////////////////////////////////////////////////

Task("CheckForError")
    .Description("Checks for errors running the test suites")
    .Does(() => CheckForError(ref ErrorDetail));

//////////////////////////////////////////////////////////////////////
// TEST ENGINE
//////////////////////////////////////////////////////////////////////

Task("TestNet20Engine")
    .Description("Tests the engine")
    .IsDependentOn("Build")
    .OnError(exception => { ErrorDetail.Add(exception.Message); })
    .Does(() =>
    {
        RunTest(NET20_CONSOLE, NET35_BIN_DIR, ENGINE_TESTS, "net35", ref ErrorDetail);
    });

//////////////////////////////////////////////////////////////////////
// TEST .NET 2.0 CONSOLE
//////////////////////////////////////////////////////////////////////

Task("TestNet20Console")
    .Description("Tests the .NET 2.0 console runner")
    .IsDependentOn("Build")
    .OnError(exception => { ErrorDetail.Add(exception.Message); })
    .Does(() =>
    {
        RunTest(NET20_CONSOLE, NET35_BIN_DIR, CONSOLE_TESTS, "net35", ref ErrorDetail);
    });

//////////////////////////////////////////////////////////////////////
// TEST .NET CORE 3.1 CONSOLE
//////////////////////////////////////////////////////////////////////

Task("TestNetCore31Console")
    .Description("Tests the .NET Core 3.1 console runner")
    .IsDependentOn("Build")
    .OnError(exception => { ErrorDetail.Add(exception.Message); })
    .Does(() =>
    {
        var runtimes = new[] { "3.1", "5.0" };

        foreach (var runtime in runtimes)
        {
            RunDotnetCoreTests(
                NETCORE31_CONSOLE,
                NETCOREAPP31_BIN_DIR,
                CONSOLE_TESTS,
                runtime,
                ref ErrorDetail);
        }
    });

//////////////////////////////////////////////////////////////////////
// TEST NETSTANDARD 1.6 ENGINE
//////////////////////////////////////////////////////////////////////

Task("TestNetStandard16Engine")
    .Description("Tests the .NET Standard Engine")
    .IsDependentOn("Build")
    .WithCriteria(!BuildSystem.IsRunningOnAzurePipelines)   //Unable to find Azure build supporting both .NET Core 1.1 and .NET Core 3.1
    .OnError(exception => { ErrorDetail.Add(exception.Message); })
    .Does(() =>
    {
        RunDotnetCoreNUnitLiteTests(
            NETCOREAPP11_BIN_DIR + ENGINE_TESTS,
            NETCOREAPP11_BIN_DIR,
            "netcoreapp1.1",
            ref ErrorDetail);
    });

//////////////////////////////////////////////////////////////////////
// TEST NETSTANDARD 2.0 ENGINE
//////////////////////////////////////////////////////////////////////

Task("TestNetStandard20Engine")
    .Description("Tests the .NET Standard Engine")
    .IsDependentOn("Build")
    .OnError(exception => { ErrorDetail.Add(exception.Message); })
    .Does(() =>
    {
        RunDotnetCoreNUnitLiteTests(
            NETCOREAPP21_BIN_DIR + ENGINE_TESTS,
            NETCOREAPP21_BIN_DIR,
            "netcoreapp2.1",
            ref ErrorDetail);
    });


//////////////////////////////////////////////////////////////////////
// TEST NETCORE 3.1 ENGINE
//////////////////////////////////////////////////////////////////////

Task("TestNetCore31Engine")
    .Description("Tests the .NET Core 3.1 Engine")
    .IsDependentOn("Build")
    .OnError(exception => { ErrorDetail.Add(exception.Message); })
    .Does(() =>
    {
        var runtimes = new[] { "3.1", "5.0" };

        foreach (var runtime in runtimes)
        {
            RunDotnetCoreTests(
                NETCORE31_CONSOLE,
                NETCOREAPP31_BIN_DIR,
                ENGINE_TESTS,
                runtime,
                ref ErrorDetail);
        }
    });


//////////////////////////////////////////////////////////////////////
// PACKAGE
//////////////////////////////////////////////////////////////////////

var RootFiles = new FilePath[]
{
    "LICENSE.txt",
    "NOTICES.txt",
    "CHANGES.txt",
    "nunit.ico"
};

Task("CreateImage")
    .Description("Copies all files into the image directory")
    .Does(() =>
    {
        CleanDirectory(CURRENT_IMG_DIR);
        CopyFiles(RootFiles, CURRENT_IMG_DIR);
        CopyDirectory(BIN_DIR, CURRENT_IMG_DIR + "bin/");
    });

Task("PackageNuGet")
    .Description("Creates NuGet packages of the engine/console")
    .IsDependentOn("CreateImage")
    .Does(() =>
    {
        CreateDirectory(PACKAGE_DIR);

        NuGetPack("nuget/engine/nunit.engine.api.nuspec", new NuGetPackSettings()
        {
            Version = productVersion,
            BasePath = CURRENT_IMG_DIR,
            OutputDirectory = PACKAGE_DIR,
            NoPackageAnalysis = true
        });

        NuGetPack("nuget/engine/nunit.engine.nuspec", new NuGetPackSettings()
        {
            Version = productVersion,
            BasePath = CURRENT_IMG_DIR,
            OutputDirectory = PACKAGE_DIR,
            NoPackageAnalysis = true
        });

        NuGetPack("nuget/runners/nunit.console-runner.nuspec", new NuGetPackSettings()
        {
            Version = productVersion,
            BasePath = CURRENT_IMG_DIR,
            OutputDirectory = PACKAGE_DIR,
            NoPackageAnalysis = true
        });

        NuGetPack("nuget/runners/nunit.console-runner-with-extensions.nuspec", new NuGetPackSettings()
        {
            Version = productVersion,
            BasePath = CURRENT_IMG_DIR,
            OutputDirectory = PACKAGE_DIR,
            NoPackageAnalysis = true
        });

        NuGetPack("nuget/runners/nunit.console-runner.netcore.nuspec", new NuGetPackSettings()
        {
            Version = productVersion,
            BasePath = CURRENT_IMG_DIR,
            OutputDirectory = PACKAGE_DIR,
            NoPackageAnalysis = true
        });

        NuGetPack("nuget/deprecated/nunit.runners.nuspec", new NuGetPackSettings()
        {
            Version = productVersion,
            BasePath = CURRENT_IMG_DIR,
            OutputDirectory = PACKAGE_DIR,
            NoPackageAnalysis = true
        });

        NuGetPack("nuget/deprecated/nunit.engine.netstandard.nuspec", new NuGetPackSettings()
        {
            Version = productVersion,
            BasePath = CURRENT_IMG_DIR,
            OutputDirectory = PACKAGE_DIR,
            NoPackageAnalysis = true
        });
    });

Task("PackageChocolatey")
    .Description("Creates chocolatey packages of the console runner")
    .Does(() =>
    {
        EnsureDirectoryExists(PACKAGE_DIR);

        ChocolateyPack("choco/nunit-console-runner.nuspec",
            new ChocolateyPackSettings()
            {
                Version = productVersion,
                OutputDirectory = PACKAGE_DIR,
                Files = new [] {
                    new ChocolateyNuSpecContent { Source = CURRENT_IMG_DIR + "LICENSE.txt", Target = "tools" },
                    new ChocolateyNuSpecContent { Source = CURRENT_IMG_DIR + "NOTICES.txt", Target = "tools" },
                    new ChocolateyNuSpecContent { Source = CURRENT_IMG_DIR + "CHANGES.txt", Target = "tools" },
                    new ChocolateyNuSpecContent { Source = CHOCO_DIR + "VERIFICATION.txt", Target = "tools" },
                    new ChocolateyNuSpecContent { Source = CHOCO_DIR + "nunit.choco.addins", Target = "tools" },
                    new ChocolateyNuSpecContent { Source = CHOCO_DIR + "nunit.agent.addins", Target = "tools/agents/net20" },
                    new ChocolateyNuSpecContent { Source = CHOCO_DIR + "nunit.agent.addins", Target = "tools/agents/net40" },
                    new ChocolateyNuSpecContent { Source = CURRENT_IMG_DIR + "bin/agents/net20/nunit-agent.exe", Target="tools/agents/net20" },
                    new ChocolateyNuSpecContent { Source = CURRENT_IMG_DIR + "bin/agents/net20/nunit-agent.exe.config", Target="tools/agents/net20" },
                    new ChocolateyNuSpecContent { Source = CHOCO_DIR + "nunit-agent.exe.ignore", Target="tools/agents/net20" },
                    new ChocolateyNuSpecContent { Source = CURRENT_IMG_DIR + "bin/agents/net20/nunit-agent-x86.exe", Target="tools/agents/net20" },
                    new ChocolateyNuSpecContent { Source = CURRENT_IMG_DIR + "bin/agents/net20/nunit-agent-x86.exe.config", Target="tools/agents/net20" },
                    new ChocolateyNuSpecContent { Source = CHOCO_DIR + "nunit-agent-x86.exe.ignore", Target="tools/agents/net20" },
                    new ChocolateyNuSpecContent { Source = CURRENT_IMG_DIR + "bin/agents/net20/nunit.engine.api.dll", Target="tools/agents/net20" },
                    new ChocolateyNuSpecContent { Source = CURRENT_IMG_DIR + "bin/agents/net20/nunit.engine.api.xml", Target="tools/agents/net20" },
                    new ChocolateyNuSpecContent { Source = CURRENT_IMG_DIR + "bin/agents/net20/nunit.engine.core.dll", Target="tools/agents/net20" },
                    new ChocolateyNuSpecContent { Source = CURRENT_IMG_DIR + "bin/agents/net20/testcentric.engine.metadata.dll", Target="tools/agents/net20" },
                    new ChocolateyNuSpecContent { Source = CURRENT_IMG_DIR + "bin/agents/net40/nunit-agent.exe", Target="tools/agents/net40" },
                    new ChocolateyNuSpecContent { Source = CURRENT_IMG_DIR + "bin/agents/net40/nunit-agent.exe.config", Target="tools/agents/net40" },
                    new ChocolateyNuSpecContent { Source = CHOCO_DIR + "nunit-agent.exe.ignore", Target="tools/agents/net40" },
                    new ChocolateyNuSpecContent { Source = CURRENT_IMG_DIR + "bin/agents/net40/nunit-agent-x86.exe", Target="tools/agents/net40" },
                    new ChocolateyNuSpecContent { Source = CURRENT_IMG_DIR + "bin/agents/net40/nunit-agent-x86.exe.config", Target="tools/agents/net40" },
                    new ChocolateyNuSpecContent { Source = CHOCO_DIR + "nunit-agent-x86.exe.ignore", Target="tools/agents/net40" },
                    new ChocolateyNuSpecContent { Source = CURRENT_IMG_DIR + "bin/agents/net40/nunit.engine.api.dll", Target="tools/agents/net40" },
                    new ChocolateyNuSpecContent { Source = CURRENT_IMG_DIR + "bin/agents/net40/nunit.engine.api.xml", Target="tools/agents/net40" },
                    new ChocolateyNuSpecContent { Source = CURRENT_IMG_DIR + "bin/agents/net40/nunit.engine.core.dll", Target="tools/agents/net40" },
                    new ChocolateyNuSpecContent { Source = CURRENT_IMG_DIR + "bin/agents/net40/testcentric.engine.metadata.dll", Target="tools/agents/net40" },
                    new ChocolateyNuSpecContent { Source = CURRENT_IMG_NET20_BIN_DIR + "nunit3-console.exe", Target="tools" },
                    new ChocolateyNuSpecContent { Source = CURRENT_IMG_NET20_BIN_DIR + "nunit3-console.exe.config", Target="tools" },
                    new ChocolateyNuSpecContent { Source = CURRENT_IMG_NET20_BIN_DIR + "nunit.engine.api.dll", Target="tools" },
                    new ChocolateyNuSpecContent { Source = CURRENT_IMG_NET20_BIN_DIR + "nunit.engine.api.xml", Target="tools" },
                    new ChocolateyNuSpecContent { Source = CURRENT_IMG_NET20_BIN_DIR + "nunit.engine.core.dll", Target="tools" },
                    new ChocolateyNuSpecContent { Source = CURRENT_IMG_NET20_BIN_DIR + "nunit.engine.dll", Target="tools" },
                    new ChocolateyNuSpecContent { Source = CURRENT_IMG_NET20_BIN_DIR + "testcentric.engine.metadata.dll", Target="tools" }
                }
            });

        ChocolateyPack("choco/nunit-console-with-extensions.nuspec",
            new ChocolateyPackSettings()
            {
                Version = productVersion,
                OutputDirectory = PACKAGE_DIR,
                Files = new [] {
                    new ChocolateyNuSpecContent { Source = CURRENT_IMG_DIR + "LICENSE.txt", Target = "tools" },
                    new ChocolateyNuSpecContent { Source = CURRENT_IMG_DIR + "NOTICES.txt", Target = "tools" },
                    new ChocolateyNuSpecContent { Source = CHOCO_DIR + "VERIFICATION.txt", Target = "tools" }
                }
            });
    });

//////////////////////////////////////////////////////////////////////
// PACKAGE COMBINED DISTRIBUTIONS
//////////////////////////////////////////////////////////////////////

Task("FetchExtensions")
.Does(() =>
{
    CleanDirectory(EXTENSION_PACKAGES_DIR);

    foreach(var package in EXTENSION_PACKAGES)
    {
        NuGetInstall(package, new NuGetInstallSettings {
                        OutputDirectory = EXTENSION_PACKAGES_DIR,
                        Source = new [] { "https://www.nuget.org/api/v2" }
                    });
    }
});

Task("CreateCombinedImage")
.IsDependentOn("CreateImage")
.IsDependentOn("FetchExtensions")
.Does(() =>
{
    foreach(var framework in NETFX_FRAMEWORKS)
    {
        var addinsImgDir = CURRENT_IMG_DIR + "bin/" + framework +"/addins/";

        CopyDirectory(MSI_DIR + "resources/", CURRENT_IMG_DIR);
        CleanDirectory(addinsImgDir);

        foreach(var packageDir in GetAllDirectories(EXTENSION_PACKAGES_DIR))
            CopyPackageContents(packageDir, addinsImgDir);
    }
});

Task("PackageMsi")
.IsDependentOn("CreateCombinedImage")
.Does(() =>
{
    MSBuild(MSI_DIR + "nunit/nunit.wixproj", new MSBuildSettings()
        .WithTarget("Rebuild")
        .SetConfiguration(configuration)
        .WithProperty("Version", version)
        .WithProperty("DisplayVersion", version)
        .WithProperty("OutDir", PACKAGE_DIR)
        .WithProperty("Image", CURRENT_IMG_DIR)
        .SetMSBuildPlatform(MSBuildPlatform.x86)
        .SetNodeReuse(false)
        );
});

Task("PackageZip")
.IsDependentOn("CreateCombinedImage")
.Does(() =>
{
    CleanDirectory(ZIP_IMG);
    CopyDirectory(CURRENT_IMG_DIR, ZIP_IMG);

    foreach(var framework in NETFX_FRAMEWORKS)
    {
        //Ensure single and correct addins file (.NET Framework only)
        var netfxZipImg = ZIP_IMG + "bin/" + framework + "/";
        DeleteFiles(ZIP_IMG + "*.addins");
        DeleteFiles(netfxZipImg + "*.addins");
        CopyFile(CURRENT_IMG_DIR + "nunit.bundle.addins", netfxZipImg + "nunit.bundle.addins");
    }

    var zipPath = string.Format("{0}NUnit.Console-{1}.zip", PACKAGE_DIR, productVersion);
    Zip(ZIP_IMG, zipPath);
});

Task("InstallSigningTool")
    .Does(() =>
    {
        var result = StartProcess("dotnet.exe", new ProcessSettings {  Arguments = "tool install SignClient --global" });
    });

Task("SignPackages")
    .IsDependentOn("InstallSigningTool")
    .IsDependentOn("Package")
    .Does(() =>
    {
        // Get the secret.
        var secret = EnvironmentVariable("SIGNING_SECRET");
        if(string.IsNullOrWhiteSpace(secret)) {
            throw new InvalidOperationException("Could not resolve signing secret.");
        }
        // Get the user.
        var user = EnvironmentVariable("SIGNING_USER");
        if(string.IsNullOrWhiteSpace(user)) {
            throw new InvalidOperationException("Could not resolve signing user.");
        }

        var signClientPath = Context.Tools.Resolve("SignClient.exe") ?? Context.Tools.Resolve("SignClient") ?? throw new Exception("Failed to locate sign tool");

        var settings = File("./signclient.json");

        // Get the files to sign.
        var files = GetFiles(string.Concat(PACKAGE_DIR, "*.nupkg")) +
            GetFiles(string.Concat(PACKAGE_DIR, "*.msi"));

        foreach(var file in files)
        {
            Information("Signing {0}...", file.FullPath);

            // Build the argument list.
            var arguments = new ProcessArgumentBuilder()
                .Append("sign")
                .AppendSwitchQuoted("-c", MakeAbsolute(settings.Path).FullPath)
                .AppendSwitchQuoted("-i", MakeAbsolute(file).FullPath)
                .AppendSwitchQuotedSecret("-s", secret)
                .AppendSwitchQuotedSecret("-r", user)
                .AppendSwitchQuoted("-n", "NUnit.org")
                .AppendSwitchQuoted("-d", "NUnit is a unit-testing framework for all .NET languages.")
                .AppendSwitchQuoted("-u", "https://nunit.org/");

            // Sign the binary.
            var result = StartProcess(signClientPath.FullPath, new ProcessSettings {  Arguments = arguments });
            if(result != 0)
            {
                // We should not recover from this.
                throw new InvalidOperationException("Signing failed!");
            }
        }
    });

Task("CheckPackages")
    .Description("Check content of NuGet packages")
    .Does(() =>
    {
        CheckAllPackages();
    });

Task("ListInstalledNetCoreRuntimes")
    .Description("Lists all installed .NET Core Runtimes")
    .Does(() => 
    {
        var runtimes = GetInstalledNetCoreRuntimes();
        foreach (var runtime in runtimes)
        {
            Information(runtime);            
        }
    });

//////////////////////////////////////////////////////////////////////
// HELPER METHODS - GENERAL
//////////////////////////////////////////////////////////////////////

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
    catch(Exception)
    {
        Warning(".NET Core SDK is not installed. It can be installed from https://www.microsoft.com/net/core");
        return false;
    }
    return true;
}

void CheckForError(ref List<string> errorDetail)
{
    if(errorDetail.Count != 0)
    {
        var copyError = new List<string>();
        copyError = errorDetail.Select(s => s).ToList();
        errorDetail.Clear();
        throw new Exception("One or more unit tests failed, breaking the build.\n"
                              + copyError.Aggregate((x,y) => x + "\n" + y));
    }
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

void RunTest(FilePath exePath, DirectoryPath workingDir, string testAssembly, string framework, ref List<string> errorDetail)
{
    int rc = StartProcess(
        MakeAbsolute(exePath),
        new ProcessSettings()
        {
            Arguments = new ProcessArgumentBuilder()
                .Append(testAssembly)
                .AppendSwitchQuoted("--result", ":", GetResultXmlPath(testAssembly, framework).FullPath)
                .Render(),
            WorkingDirectory = workingDir
        });

    if (rc > 0)
        errorDetail.Add(string.Format("{0}: {1} tests failed", framework, rc));
    else if (rc < 0)
        errorDetail.Add(string.Format("{0} returned rc = {1}", exePath, rc));
}

void RunDotnetCoreNUnitLiteTests(FilePath exePath, DirectoryPath workingDir, string framework, ref List<string> errorDetail)
{
    RunDotnetCoreTests(exePath, workingDir, arguments: null, framework, ref errorDetail);
}

void RunDotnetCoreTests(FilePath exePath, DirectoryPath workingDir, string arguments, string framework, ref List<string> errorDetail)
{
    //Filename is first arg if running on NUnit Console, or exePath if running NUnitLite tests
    var fileName = arguments ?? exePath.GetFilename();

    //Find most suitable runtime
    var fxVersion = new string(framework.SkipWhile(c => !char.IsDigit(c)).ToArray());
    //Select latest runtime matching requested major/minor version
    var selectedFramework = installedNetCoreRuntimes.Where(v => v.StartsWith(fxVersion)).OrderByDescending(Version.Parse).FirstOrDefault();

    if (selectedFramework == null)
    {
        var msg = $"No suitable runtime found to run tests under {framework}";
        if (!BuildSystem.IsLocalBuild)
            throw new Exception(msg);

        Warning(msg);
        return;
    }
    else
    {
        Information($"Runtime framework version {selectedFramework} selected to run {fileName} for {framework}.");
    }

    //Run Tests

    var args = new ProcessArgumentBuilder()
                .AppendSwitch("--fx-version", " ", selectedFramework)
                .AppendQuoted(exePath.FullPath)
                .Append(arguments)
                .AppendSwitchQuoted("--result", ":", GetResultXmlPath(exePath.FullPath, framework).FullPath)
                .Render();

    int rc = StartProcess(
        "dotnet",
        new ProcessSettings
        {
            Arguments = args,
            WorkingDirectory = workingDir
        });

    if (rc > 0)
        errorDetail.Add(string.Format("{0}: {1} tests failed", framework, rc));
    else if (rc < 0)
        errorDetail.Add(string.Format("{0} returned rc = {1}", exePath, rc));
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
// HELPER METHODS - PACKAGE
//////////////////////////////////////////////////////////////////////

public string[] GetAllDirectories(string dirPath)
{
    return System.IO.Directory.GetDirectories(dirPath);
}

public void CopyPackageContents(DirectoryPath packageDir, DirectoryPath outDir)
{
    var files = GetFiles(packageDir + "/tools/*").Concat(GetFiles(packageDir + "/tools/net20/*"));
    CopyFiles(files.Where(f => f.GetExtension() != ".addins"), outDir);
}

//////////////////////////////////////////////////////////////////////
// TASK TARGETS
//////////////////////////////////////////////////////////////////////

Task("Rebuild")
    .Description("Rebuilds the engine and console runner")
    .IsDependentOn("Clean")
    .IsDependentOn("Build");

Task("TestConsole")
    .Description("Builds and tests the console runner")
    .IsDependentOn("TestNet20Console")
    .IsDependentOn("TestNetCore31Console");

Task("TestEngine")
    .Description("Builds and tests the engine")
    .IsDependentOn("TestNet20Engine")
    .IsDependentOn("TestNetStandard16Engine")
    .IsDependentOn("TestNetStandard20Engine")
    .IsDependentOn("TestNetCore31Engine");

Task("Test")
    .Description("Builds and tests the engine")
    .IsDependentOn("TestEngine")
    .IsDependentOn("TestConsole");

Task("Package")
    .Description("Packages the engine and console runner")
    .IsDependentOn("CheckForError")
    .IsDependentOn("PackageNuGet")
    .IsDependentOn("PackageChocolatey")
    .IsDependentOn("PackageMsi")
    .IsDependentOn("PackageZip")
    .IsDependentOn("CheckPackages");

Task("Appveyor")
    .Description("Builds, tests and packages on AppVeyor")
    .IsDependentOn("Build")
    .IsDependentOn("Test")
    .IsDependentOn("Package");

Task("Travis")
    .Description("Builds and tests on Travis")
    .IsDependentOn("Build")
    .IsDependentOn("Test");

Task("Default")
    .Description("Builds the engine and console runner")
    .IsDependentOn("Build");

//////////////////////////////////////////////////////////////////////
// EXECUTION
//////////////////////////////////////////////////////////////////////

RunTarget(target);
