static string Target; Target = GetArgument("target|t", "Default");
static string Configuration; Configuration = GetArgument("configuration|c", "Release");
static bool NoPush; NoPush = HasArgument("nopush");

#load cake/constants.cake
#load cake/header-check.cake
#load cake/package-checks.cake
#load cake/test-results.cake
#load cake/package-tests.cake
#load cake/package-tester.cake
#load cake/versioning.cake
#load cake/utilities.cake
#load cake/package-definitions.cake

// Install Tools
#tool NuGet.CommandLine&version=6.0.0
#tool dotnet:?package=GitVersion.Tool&version=5.6.3
#tool dotnet:?package=GitReleaseManager.Tool&version=0.12.1

BuildVersion _buildVersion;
string ProductVersion => _buildVersion.ProductVersion;
string SemVer => _buildVersion.SemVer;
string PreReleaseLabel => _buildVersion.PreReleaseLabel;
bool IsReleaseBranch => _buildVersion.IsReleaseBranch;

var UnreportedErrors = new List<string>();
var installedNetCoreRuntimes = GetInstalledNetCoreRuntimes();

//////////////////////////////////////////////////////////////////////
// SETUP AND TEARDOWN TASKS
//////////////////////////////////////////////////////////////////////
Setup(context =>
{
    Information("Creating BuildVersion");
    _buildVersion = new BuildVersion(context);

    Information("Building {0} version {1} of NUnit Console/Engine.", Configuration, ProductVersion);
    Information("PreReleaseLabel is " + PreReleaseLabel);

    Information("Initializing PackageDefinitions");
    InitializePackageDefinitions(context);

    if (BuildSystem.IsRunningOnAppVeyor)
        AppVeyor.UpdateBuildVersion(ProductVersion + "-" + AppVeyor.Environment.Build.Number);
});

Teardown(context =>
{
    // Executed AFTER the last task.
    DisplayUnreportedErrors();
});

//////////////////////////////////////////////////////////////////////
// CLEANING
//////////////////////////////////////////////////////////////////////

Task("Clean")
    .Description("Cleans directories.")
    .Does(() =>
    {
        CleanDirectory(BIN_DIR);
        CleanDirectory(PACKAGE_DIR);
        CleanDirectory(IMAGE_DIR);
        CleanDirectory(EXTENSIONS_DIR);
        CleanDirectory(PACKAGE_DIR);
    });

Task("CleanAll")
    .Description("Cleans both Debug and Release Directories followed by deleting object directories")
    .Does(() =>
    {
        Information("Cleaning both Debug and Release");
        CleanDirectory(PROJECT_DIR + "bin");
        CleanDirectory(PACKAGE_DIR);
        CleanDirectory(IMAGE_DIR);
        CleanDirectory(EXTENSIONS_DIR);
        CleanDirectory(PACKAGE_DIR);

        Information("Deleting object directories");
        foreach (var dir in GetDirectories("src/**/obj/"))
            DeleteDirectory(dir, new DeleteDirectorySettings() { Recursive = true });
    });

//////////////////////////////////////////////////////////////////////
// BUILD ENGINE AND CONSOLE
//////////////////////////////////////////////////////////////////////

Task("Build")
    .Description("Builds the engine and console")
    .IsDependentOn("CheckHeaders")
    .IsDependentOn("Clean")
    .Does(() =>
    {
        if (IsRunningOnWindows())
            BuildSolution();
        else
            BuildEachProjectSeparately();
    });

public void BuildSolution()
{
    MSBuild(SOLUTION_FILE, CreateMSBuildSettings("Build").WithRestore());

    // Publishing in place where needed to ensure that all references are present.

    // TODO: May not be needed
    DisplayBanner("Publishing ENGINE API Project for NETSTANDARD_2.0");
    MSBuild(ENGINE_API_PROJECT, CreateMSBuildSettings("Publish")
        .WithProperty("TargetFramework", "netstandard2.0")
        .WithProperty("PublishDir", BIN_DIR + "netstandard2.0"));

    DisplayBanner("Publishing ENGINE Project for NETSTANDARD2.0");
    MSBuild(ENGINE_PROJECT, CreateMSBuildSettings("Publish")
        .WithProperty("TargetFramework", "netstandard2.0")
        .WithProperty("PublishDir", BIN_DIR + "netstandard2.0"));

    DisplayBanner("Publishing ENGINE TESTS Project for NETCOREAPP2.1");
    MSBuild(ENGINE_TESTS_PROJECT, CreateMSBuildSettings("Publish")
        .WithProperty("TargetFramework", "netcoreapp2.1")
        .WithProperty("PublishDir", BIN_DIR + "netcoreapp2.1"));

    // TODO: May not be needed
    foreach (var framework in new[] { "netcoreapp3.1", "net5.0" })
    {
        DisplayBanner($"Publishing AGENT Project for {framework.ToUpper()}");
        MSBuild(AGENT_PROJECT, CreateMSBuildSettings("Publish")
            .WithProperty("TargetFramework", framework)
            .WithProperty("PublishDir", BIN_DIR + "agents/" + framework));
    }
}

private void BuildEachProjectSeparately()
{
    DotNetRestore(SOLUTION_FILE);

    BuildProject(ENGINE_PROJECT);
    BuildProject(CONSOLE_PROJECT);
    BuildProject(AGENT_PROJECT);
    BuildProject(AGENT_X86_PROJECT);

    BuildProject(ENGINE_TESTS_PROJECT, "net35", "netcoreapp2.1");
    BuildProject(ENGINE_CORE_TESTS_PROJECT, "net35", "netcoreapp2.1", "netcoreapp3.1", "net5.0", "net6.0");
    BuildProject(CONSOLE_TESTS_PROJECT, "net35", "net6.0");

    BuildProject(MOCK_ASSEMBLY_X86_PROJECT, "net35", "net40", "netcoreapp2.1", "netcoreapp3.1");
    BuildProject(NOTEST_PROJECT, "net35", "netcoreapp2.1", "netcoreapp3.1");


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
}

// NOTE: If we use DotNet to build on Linux, then our net35 projects fail.
// If we use MSBuild, then the net5.0 projects fail. So we build each project
// differently depending on whether it has net35 as one of its targets. 
private void BuildProject(string project, params string[] targetFrameworks)
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
            if (framework == "net35")
                MSBuild(project, CreateMSBuildSettings("Build").WithProperty("TargetFramework", framework));
            else
                DotNetMSBuild(project, CreateDotNetMSBuildSettings("Build").WithProperty("TargetFramework", framework));
        }
    }
}

//////////////////////////////////////////////////////////////////////
// BUILD C++ TESTS
//////////////////////////////////////////////////////////////////////

Task("BuildCppTestFiles")
    .Description("Builds the C++ mock test assemblies")
    .WithCriteria(IsRunningOnWindows)
    .Does(() =>
    {
        MSBuild(
            SOURCE_DIR + "NUnitEngine/mock-cpp-clr/mock-cpp-clr-x86.vcxproj",
            CreateMSBuildSettings("Build").WithProperty("Platform", "x86"));

        MSBuild(
            SOURCE_DIR + "NUnitEngine/mock-cpp-clr/mock-cpp-clr-x64.vcxproj",
            CreateMSBuildSettings("Build").WithProperty("Platform", "x64"));
    });

//////////////////////////////////////////////////////////////////////
// TEST
//////////////////////////////////////////////////////////////////////

// All Unit Tests are run and any error results are saved in
// the global PendingUnitTestErrors. This method task displays them
// and throws if there were any errors.
Task("CheckForTestErrors")
    .Description("Checks for errors running the test suites")
    .Does(() => DisplayUnreportedErrors());

//////////////////////////////////////////////////////////////////////
// TEST .NET 2.0 ENGINE CORE
//////////////////////////////////////////////////////////////////////

Task("TestNet20EngineCore")
    .Description("Tests the engine core assembly")
    .IsDependentOn("Build")
    .OnError(exception => { UnreportedErrors.Add(exception.Message); })
    .Does(() =>
    {
        RunNUnitLiteTests(NETFX_ENGINE_CORE_TESTS, "net35");
    });

//////////////////////////////////////////////////////////////////////
// TEST NETSTANDARD 2.0 ENGINE CORE
//////////////////////////////////////////////////////////////////////

Task("TestNetStandard20EngineCore")
    .Description("Tests the .NET Standard Engine core assembly")
    .IsDependentOn("Build")
    .OnError(exception => { UnreportedErrors.Add(exception.Message); })
    .Does(() =>
    {
        RunDotnetNUnitLiteTests(NETCORE_ENGINE_CORE_TESTS, "netcoreapp2.1");
    });

//////////////////////////////////////////////////////////////////////
// TEST NETCORE 3.1 ENGINE CORE
//////////////////////////////////////////////////////////////////////

Task("TestNetCore31EngineCore")
    .Description("Tests the .NET Core 3.1 Engine core assembly")
    .IsDependentOn("Build")
    .OnError(exception => { UnreportedErrors.Add(exception.Message); })
    .Does(() =>
    {
        RunDotnetNUnitLiteTests(NETCORE_ENGINE_CORE_TESTS, "netcoreapp3.1");
    });

//////////////////////////////////////////////////////////////////////
// TEST NET 5.0 ENGINE CORE
//////////////////////////////////////////////////////////////////////

Task("TestNet50EngineCore")
    .Description("Tests the .NET 5.0 Engine core assembly")
    .IsDependentOn("Build")
    .OnError(exception => { UnreportedErrors.Add(exception.Message); })
    .Does(() =>
    {
        RunDotnetNUnitLiteTests(NETCORE_ENGINE_CORE_TESTS, "net5.0");
    });

//////////////////////////////////////////////////////////////////////
// TEST NET 6.0 ENGINE CORE
//////////////////////////////////////////////////////////////////////

Task("TestNet60EngineCore")
    .Description("Tests the .NET 6.0 Engine core assembly")
    .IsDependentOn("Build")
    .OnError(exception => { UnreportedErrors.Add(exception.Message); })
    .Does(() =>
    {
        RunDotnetNUnitLiteTests(NETCORE_ENGINE_CORE_TESTS, "net6.0");
    });

//////////////////////////////////////////////////////////////////////
// TEST .NET 2.0 ENGINE
//////////////////////////////////////////////////////////////////////

Task("TestNet20Engine")
    .Description("Tests the engine")
    .IsDependentOn("Build")
    .OnError(exception => { UnreportedErrors.Add(exception.Message); })
    .Does(() =>
    {
        RunNUnitLiteTests(NETFX_ENGINE_TESTS, "net35");
    });

//////////////////////////////////////////////////////////////////////
// TEST NETSTANDARD 2.0 ENGINE
//////////////////////////////////////////////////////////////////////

Task("TestNetStandard20Engine")
    .Description("Tests the .NET Standard Engine")
    .IsDependentOn("Build")
    .OnError(exception => { UnreportedErrors.Add(exception.Message); })
    .Does(() =>
    {
        RunDotnetNUnitLiteTests(NETCORE_ENGINE_TESTS, "netcoreapp2.1");
    });

//////////////////////////////////////////////////////////////////////
// TEST .NET 2.0 CONSOLE
//////////////////////////////////////////////////////////////////////

Task("TestNet20Console")
    .Description("Tests the .NET 2.0 console runner")
    .IsDependentOn("Build")
    .OnError(exception => { UnreportedErrors.Add(exception.Message); })
    .Does(() =>
    {
        RunNet20Console(CONSOLE_TESTS, "net35");
    });

//////////////////////////////////////////////////////////////////////
// TEST .NET 6.0 CONSOLE
//////////////////////////////////////////////////////////////////////

Task("TestNet60Console")
    .Description("Tests the .NET 6.0 console runner")
    .IsDependentOn("Build")
    .OnError(exception => { UnreportedErrors.Add(exception.Message); })
    .Does(() =>
    {
        RunNetCoreConsole(CONSOLE_TESTS, "net6.0");
    });

//////////////////////////////////////////////////////////////////////
// FETCH BUNDLED EXTENSIONS
//////////////////////////////////////////////////////////////////////

Task("FetchBundledExtensions")
    .Does(() =>
    {
        CleanDirectory(EXTENSIONS_DIR);

        DisplayBanner("Fetching bundled extensions");

        foreach (var extension in BUNDLED_EXTENSIONS)
        {
            DisplayBanner(extension);

            NuGetInstall(extension, new NuGetInstallSettings
            {
                OutputDirectory = EXTENSIONS_DIR,
                Source = new[] { "https://www.nuget.org/api/v2" }
            });
        }
    });

//////////////////////////////////////////////////////////////////////
// CREATE MSI IMAGE
//////////////////////////////////////////////////////////////////////

Task("CreateMsiImage")
    .IsDependentOn("FetchBundledExtensions")
    .Does(() =>
    {
        CleanDirectory(MSI_IMG_DIR);
        CopyFiles(
            new FilePath[] { "LICENSE.txt", "NOTICES.txt", "CHANGES.txt", "nunit.ico" },
            MSI_IMG_DIR);
        CopyDirectory(BIN_DIR, MSI_IMG_DIR + "bin/");

        foreach (var framework in new[] { "net20", "net35" })
        {
            var addinsImgDir = MSI_IMG_DIR + "bin/" + framework + "/addins/";

            CopyDirectory(MSI_DIR + "resources/", MSI_IMG_DIR);
            CleanDirectory(addinsImgDir);

            foreach (var packageDir in System.IO.Directory.GetDirectories(EXTENSIONS_DIR))
                CopyPackageContents(packageDir, addinsImgDir);
        }
    });

//////////////////////////////////////////////////////////////////////
// CREATE ZIP IMAGE
//////////////////////////////////////////////////////////////////////

Task("CreateZipImage")
    .IsDependentOn("FetchBundledExtensions")
    .Does(() =>
    {
        CleanDirectory(ZIP_IMG_DIR);
        CopyFiles(
            new FilePath[] { "LICENSE.txt", "NOTICES.txt", "CHANGES.txt", "nunit.ico" },
            ZIP_IMG_DIR);
        CopyDirectory(BIN_DIR, ZIP_IMG_DIR + "bin/");

        foreach (var framework in new[] { "net20", "net35" })
        {
            var frameworkDir = ZIP_IMG_DIR + "bin/" + framework + "/";
            CopyFileToDirectory(ZIP_DIR + "nunit.bundle.addins", frameworkDir);

            var addinsDir = frameworkDir + "addins/";
            CleanDirectory(addinsDir);

            foreach (var packageDir in System.IO.Directory.GetDirectories(EXTENSIONS_DIR))
                CopyPackageContents(packageDir, addinsDir);
        }
    });

Task("BuildPackages")
    .IsDependentOn("CreateMsiImage")
    .IsDependentOn("CreateZipImage")
    .Does(() =>
    {
        EnsureDirectoryExists(PACKAGE_DIR);

        foreach (var package in AllPackages)
        {
            DisplayBanner($"Building package {package.PackageName}");

            package.BuildPackage();
        }
    });

//////////////////////////////////////////////////////////////////////
// VERIFY PACKAGES
//////////////////////////////////////////////////////////////////////

Task("VerifyPackages")
    .Description("Check content of all the packages we build")
    .Does(() =>
    {
        int failures = 0;

        foreach (var package in AllPackages)
        {
            if (!CheckPackage($"{PACKAGE_DIR}{package.PackageName}", package.PackageChecks))
                ++failures;

            if (package.HasSymbols && !CheckPackage($"{PACKAGE_DIR}{package.SymbolPackageName}", package.SymbolChecks))
                ++failures;
        }

        if (failures == 0)
            Information("\nAll packages passed verification.");
        else
            throw new System.Exception($"{failures} packages failed verification.");
    });

//////////////////////////////////////////////////////////////////////
// TEST PACKAGES
//////////////////////////////////////////////////////////////////////

Task("TestPackages")
    .Does(() =>
    {
        foreach (var package in AllPackages)
        {
            if (package.PackageTests != null)
                new PackageTester(Context, package).RunTests();
        }
    });

//////////////////////////////////////////////////////////////////////
// INSTALL SIGNING TOOL
//////////////////////////////////////////////////////////////////////

Task("InstallSigningTool")
    .Does(() =>
    {
        var result = StartProcess("dotnet.exe", new ProcessSettings {  Arguments = "tool install SignClient --global" });
    });

//////////////////////////////////////////////////////////////////////
// SIGN PACKAGES
//////////////////////////////////////////////////////////////////////

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

//////////////////////////////////////////////////////////////////////
// PUBLISH PACKAGES
//////////////////////////////////////////////////////////////////////

//////////////////////////////////////////////////////////////////////
// PUBLISH PACKAGES
//////////////////////////////////////////////////////////////////////

static bool HadPublishingErrors = false;

Task("PublishPackages")
    .Description("Publish nuget and chocolatey packages according to the current settings")
    .IsDependentOn("PublishToMyGet")
    .IsDependentOn("PublishToNuGet")
    .IsDependentOn("PublishToChocolatey")
    .Does(() =>
    {
        if (HadPublishingErrors)
            throw new Exception("One of the publishing steps failed.");
    });

// This task may either be run by the PublishPackages task,
// which depends on it, or directly when recovering from errors.
Task("PublishToMyGet")
    .Description("Publish packages to MyGet")
    .Does(() =>
    {
        if (!ShouldPublishToMyGet)
            Information("Nothing to publish to MyGet from this run.");
        else
        {
            var apiKey = EnvironmentVariable(MYGET_API_KEY);

            foreach (var package in AllPackages)
                try
                {
                    switch (package.PackageType)
                    {
                        case PackageType.NuGet:
                            PushNuGetPackage(PACKAGE_DIR + package.PackageName, apiKey, MYGET_PUSH_URL);
                            break;
                        case PackageType.Chocolatey:
                            PushChocolateyPackage(PACKAGE_DIR + package.PackageName, apiKey, MYGET_PUSH_URL);
                            break;
                    }
                }
                catch(Exception)
                {
                    HadPublishingErrors = true;
                }
        }
    });

// This task may either be run by the PublishPackages task,
// which depends on it, or directly when recovering from errors.
Task("PublishToNuGet")
    .Description("Publish packages to NuGet")
    .Does(() =>
    {
        if (!ShouldPublishToNuGet)
            Information("Nothing to publish to NuGet from this run.");
        else
        {
            var apiKey = EnvironmentVariable(NUGET_API_KEY);

            foreach (var package in AllPackages)
                if (package.PackageType == PackageType.NuGet)
                    try
                    {
                        PushNuGetPackage(PACKAGE_DIR + package.PackageName, apiKey, NUGET_PUSH_URL);
                    }
                    catch (Exception)
                    {
                        HadPublishingErrors = true;
                    }
        }
    });

// This task may either be run by the PublishPackages task,
// which depends on it, or directly when recovering from errors.
Task("PublishToChocolatey")
    .Description("Publish packages to Chocolatey")
    .Does(() =>
    {
        if (!ShouldPublishToChocolatey)
            Information("Nothing to publish to Chocolatey from this run.");
        else
        {
            var apiKey = EnvironmentVariable(CHOCO_API_KEY);

            foreach (var package in AllPackages)
                if (package.PackageType == PackageType.Chocolatey)
                    try
                    {
                        PushChocolateyPackage(PACKAGE_DIR + package.PackageName, apiKey, CHOCO_PUSH_URL);
                    }
                    catch (Exception)
                    {
                        HadPublishingErrors = true;
                    }
        }
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
// CREATE A DRAFT RELEASE
//////////////////////////////////////////////////////////////////////

Task("CreateDraftRelease")
    .Does(() =>
    {
        bool isDirectTarget = Target == "CreateDraftRelease";

        if (isDirectTarget && !HasArgument("productVersion"))
            throw new Exception("Must specify --productVersion with the CreateDraftRelease target.");

        if (IsReleaseBranch || isDirectTarget)
        {
            string milestone = IsReleaseBranch
                ? _buildVersion.BranchName.Substring(8)
                : ProductVersion;
            string releaseName = $"NUnit Console and Engine {milestone}";

            Information($"Creating draft release for {releaseName}");

            if (!NoPush)
                try
                {
                    GitReleaseManagerCreate(EnvironmentVariable(GITHUB_ACCESS_TOKEN), GITHUB_OWNER, GITHUB_REPO, new GitReleaseManagerCreateSettings()
                    {
                        Name = releaseName,
                        Milestone = milestone
                    });
                }
                catch
                {
                    Error($"Unable to create draft release for {releaseName}.");
                    Error($"Check that there is a {milestone} milestone with at least one closed issue.");
                    Error("");
                    throw;
                }
        }
        else
        {
            Information("Skipping Release creation because this is not a release branch");
        }
    });

//////////////////////////////////////////////////////////////////////
// CREATE A PRODUCTION RELEASE
//////////////////////////////////////////////////////////////////////

Task("CreateProductionRelease")
    .Does(() =>
    {
        if (IsProductionRelease)
        {
            string token = EnvironmentVariable(GITHUB_ACCESS_TOKEN);
            string tagName = ProductVersion;

            var assetList = new List<string>();
            foreach (var package in AllPackages)
                assetList.Add(PACKAGE_DIR + package.PackageName);
            string assets = $"\"{string.Join(',', assetList.ToArray())}\"";

            Information($"Publishing release {tagName} to GitHub");

            if (NoPush)
            {
                Information($"Assets:");
                foreach (var asset in assetList)
                    Information("  " + asset);
            }
            else
            {
                GitReleaseManagerAddAssets(token, GITHUB_OWNER, GITHUB_REPO, tagName, assets);
                GitReleaseManagerClose(token, GITHUB_OWNER, GITHUB_REPO, tagName);
            }
        }
        else
        {
            Information("Skipping CreateProductionRelease because this is not a production release");
        }
    });

//////////////////////////////////////////////////////////////////////
// TASK TARGETS
//////////////////////////////////////////////////////////////////////

Task("TestConsole")
    .Description("Builds and tests the console runner")
    .IsDependentOn("TestNet20Console")
    .IsDependentOn("TestNet60Console");

Task("TestEngineCore")
    .Description("Builds and tests the engine core assembly")
    .IsDependentOn("TestNet20EngineCore")
    .IsDependentOn("TestNetStandard20EngineCore")
    .IsDependentOn("TestNetCore31EngineCore")
    .IsDependentOn("TestNet50EngineCore")
    .IsDependentOn("TestNet60EngineCore");

Task("TestEngine")
    .Description("Builds and tests the engine assembly")
    .IsDependentOn("TestNet20Engine")
    .IsDependentOn("TestNetStandard20Engine");

Task("Test")
    .Description("Builds and tests the engine and console runner")
    .IsDependentOn("TestEngineCore")
    .IsDependentOn("TestEngine")
    .IsDependentOn("TestConsole")
    .IsDependentOn("CheckForTestErrors");

Task("Package")
    .Description("Builds and tests all packages")
    .IsDependentOn("Build")
    .IsDependentOn("BuildPackages")
    .IsDependentOn("VerifyPackages")
    .IsDependentOn("TestPackages");

Task("PackageExistingBuild")
    .Description("Builds and tests all packages, using previously build binaries")
    .IsDependentOn("BuildPackages")
    .IsDependentOn("VerifyPackages")
    .IsDependentOn("TestPackages");

Task("BuildTestAndPackage")
    .Description("Builds, tests and packages")
    .IsDependentOn("Build")
    .IsDependentOn("Test")
    .IsDependentOn("Package");

Task("Appveyor")
    .Description("Target we run in our AppVeyor CI")
    .IsDependentOn("BuildTestAndPackage")
    .IsDependentOn("PublishPackages")
    .IsDependentOn("CreateDraftRelease")
    .IsDependentOn("CreateProductionRelease");

Task("Default")
    .Description("Builds the engine and console runner")
    .IsDependentOn("Build");

//////////////////////////////////////////////////////////////////////
// EXECUTION
//////////////////////////////////////////////////////////////////////

RunTarget(Target);
