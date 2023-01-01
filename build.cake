static string Target; Target = Argument("target", Argument("t", "Default"));

#load cake/constants.cake
#load cake/build-settings.cake
#load cake/header-check.cake
#load cake/package-checks.cake
#load cake/test-results.cake
#load cake/versioning.cake
#load cake/utilities.cake
#load cake/package-definitions.cake
#load cake/packages.cake

// Install Tools
#tool NuGet.CommandLine&version=6.3.1
#tool dotnet:?package=GitVersion.Tool&version=5.6.3
#tool dotnet:?package=GitReleaseManager.Tool&version=0.12.1

var UnreportedErrors = new List<string>();

//////////////////////////////////////////////////////////////////////
// SETUP AND TEARDOWN TASKS
//////////////////////////////////////////////////////////////////////
Setup(context =>
{
    var settings = new BuildSettings(context);

    Information($"Building {settings.Configuration} version {settings.ProductVersion} of NUnit Console/Engine.");
    Information($"PreReleaseLabel is {settings.PreReleaseLabel}");

    if (BuildSystem.IsRunningOnAppVeyor)
        AppVeyor.UpdateBuildVersion(settings.ProductVersion + "-" + AppVeyor.Environment.Build.Number);

    return settings;
});

Teardown(context =>
{
    // Executed AFTER the last task.
    DisplayUnreportedErrors();
});

//////////////////////////////////////////////////////////////////////
// DISPLAY THE BUILD SETTINGS
//////////////////////////////////////////////////////////////////////

Task("DisplaySettings")
    .Description("Dispay BuildSettings")
    .Does<BuildSettings>(settings => settings.Display());

//////////////////////////////////////////////////////////////////////
// CLEANING
//////////////////////////////////////////////////////////////////////

Task("Clean")
    .Description("Cleans directories.")
    .Does<BuildSettings>(settings =>
    {
        Information($"Cleaning bin/{settings.Configuration} directories");
        foreach (var dir in GetDirectories($"src/**/bin/{settings.Configuration}"))
            CleanDirectory(dir);
        
        Information("Cleaning Extensions Directory");
        CleanDirectory(EXTENSIONS_DIR);
        Information("Cleaning Package Directory");
        CleanDirectory(PACKAGE_DIR);
    });

Task("CleanAll")
    .Description("Cleans both Debug and Release Directories followed by deleting object directories")
    .Does<BuildSettings>(settings =>
    {
        Information("Cleaning both Debug and Release Directories");
        foreach (var dir in GetDirectories("src/**/bin/"))
            CleanDirectory(dir);
        
        Information("Cleaning Extensions Directory");
        CleanDirectory(EXTENSIONS_DIR);
        Information("Cleaning Package Directory");
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
    .Does<BuildSettings>(settings =>
    {
        // TEMP change for use with .NET 7.0 RC 2
        // We must build one project at a time
        if (settings.IsLocalBuild && settings.IsRunningOnWindows)
            BuildSolution(settings);
        else
            BuildEachProjectSeparately(settings);
    });

//////////////////////////////////////////////////////////////////////
// BUILD C++ TESTS
//////////////////////////////////////////////////////////////////////

Task("BuildCppTestFiles")
    .Description("Builds the C++ mock test assemblies")
    .WithCriteria(IsRunningOnWindows)
    .Does<BuildSettings>(settings =>
    {
        MSBuild(
            PROJECT_DIR + "src/NUnitEngine/mock-cpp-clr/mock-cpp-clr-x86.vcxproj",
            CreateMSBuildSettings("Build", settings).WithProperty("Platform", "x86"));

        MSBuild(
            PROJECT_DIR + "src/NUnitEngine/mock-cpp-clr/mock-cpp-clr-x64.vcxproj",
            CreateMSBuildSettings("Build", settings).WithProperty("Platform", "x64"));
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
    .Does<BuildSettings>(settings =>
    {
        RunNUnitLiteTests(ENGINE_CORE_TESTS_PROJECT, settings.Configuration, "net35");
    });

//////////////////////////////////////////////////////////////////////
// TEST NETSTANDARD 2.0 ENGINE CORE
//////////////////////////////////////////////////////////////////////

Task("TestNetStandard20EngineCore")
    .Description("Tests the .NET Standard Engine core assembly")
    .IsDependentOn("Build")
    .OnError(exception => { UnreportedErrors.Add(exception.Message); })
    .Does<BuildSettings>(settings =>
    {
        RunDotnetNUnitLiteTests(ENGINE_CORE_TESTS_PROJECT, settings.Configuration, "netcoreapp2.1");
    });

//////////////////////////////////////////////////////////////////////
// TEST NETCORE 3.1 ENGINE CORE
//////////////////////////////////////////////////////////////////////

Task("TestNetCore31EngineCore")
    .Description("Tests the .NET Core 3.1 Engine core assembly")
    .IsDependentOn("Build")
    .OnError(exception => { UnreportedErrors.Add(exception.Message); })
    .Does<BuildSettings>(settings =>
    {
        RunDotnetNUnitLiteTests(ENGINE_CORE_TESTS_PROJECT, settings.Configuration, "netcoreapp3.1");
    });

//////////////////////////////////////////////////////////////////////
// TEST .NET 4.6.2 ENGINE
//////////////////////////////////////////////////////////////////////

Task("TestNetFxEngine")
    .Description("Tests the .NET Framework build of the engine")
    .IsDependentOn("Build")
    .OnError(exception => { UnreportedErrors.Add(exception.Message); })
    .Does<BuildSettings>(settings =>
    {
        RunNUnitLiteTests(ENGINE_TESTS_PROJECT, settings.Configuration, "NET462");
    });

//////////////////////////////////////////////////////////////////////
// TEST NETSTANDARD 2.0 ENGINE
//////////////////////////////////////////////////////////////////////

Task("TestNetStandard20Engine")
    .Description("Tests the .NET Standard Engine")
    .IsDependentOn("Build")
    .OnError(exception => { UnreportedErrors.Add(exception.Message); })
    .Does<BuildSettings>(settings =>
    {
        RunDotnetNUnitLiteTests(ENGINE_TESTS_PROJECT, settings.Configuration, "netcoreapp2.1");
    });

//////////////////////////////////////////////////////////////////////
// TEST NETCORE 3.1 ENGINE
//////////////////////////////////////////////////////////////////////

Task("TestNetCore31Engine")
    .Description("Tests the .NET Core 3.1 Engine")
    .IsDependentOn("Build")
    .OnError(exception => { UnreportedErrors.Add(exception.Message); })
    .Does<BuildSettings>(settings =>
    {
        RunDotnetNUnitLiteTests(ENGINE_TESTS_PROJECT, settings.Configuration, "netcoreapp3.1");
    });
    
//////////////////////////////////////////////////////////////////////
// TEST .NET FRAMEWORK CONSOLE
//////////////////////////////////////////////////////////////////////

Task("TestNetFxConsole")
    .Description("Tests the .NET 4.6.2 console runner")
    .IsDependentOn("Build")
    .OnError(exception => { UnreportedErrors.Add(exception.Message); })
    .Does<BuildSettings>(settings =>
    {
        RunNetFxConsole(CONSOLE_TESTS_PROJECT, settings.Configuration, NETFX_CONSOLE_TARGET);
    });

//////////////////////////////////////////////////////////////////////
// TEST .NET CORE CONSOLE
//////////////////////////////////////////////////////////////////////

Task("TestNetCoreConsole")
    .Description("Tests the .NET 6.0 console runner")
    .IsDependentOn("Build")
    .OnError(exception => { UnreportedErrors.Add(exception.Message); })
    .Does<BuildSettings>(settings =>
    {
        RunNetCoreConsole(CONSOLE_TESTS_PROJECT, settings.Configuration, NETCORE_CONSOLE_TARGET);
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
// CREATE ZIP IMAGE
//////////////////////////////////////////////////////////////////////
Task("CreateZipImage")
    .IsDependentOn("FetchBundledExtensions")
    .Does<BuildSettings>(settings =>
    {
        var zipImageDirectory = PACKAGE_DIR + "zip-image/";

        CleanDirectory(zipImageDirectory);
        CopyFiles(
            new FilePath[] { "LICENSE.txt", "NOTICES.txt", "CHANGES.txt", "nunit.ico" },
            zipImageDirectory);
        CopyDirectory(settings.NetFxConsoleBinDir, zipImageDirectory + "bin/");
        CopyFileToDirectory(PROJECT_DIR + "zip/nunit.bundle.addins", zipImageDirectory + "bin/");

        // Currently, only the .NET Framework runner accepts extensions
        foreach (var framework in new[] { NETFX_CONSOLE_TARGET })
        {
            var addinsDir = zipImageDirectory + "bin/addins/";
            CleanDirectory(addinsDir);

            foreach (var packageDir in System.IO.Directory.GetDirectories(EXTENSIONS_DIR))
                CopyPackageContents(packageDir, addinsDir);
        }
    });

//////////////////////////////////////////////////////////////////////
// BUILD AND TEST ALL PACKAGES USING PREVIOUSLY BUILT BINARIES
//////////////////////////////////////////////////////////////////////
Task("PackageExistingBuild")
    .Description("Builds and tests all packages, using previously build binaries")
    .IsDependentOn("PackageConsole")
    .IsDependentOn("PackageConsoleRunner")
    .IsDependentOn("PackageDotNetConsoleRunner")
    .IsDependentOn("PackageChocolateyConsoleRunner")
    .IsDependentOn("PackageMsi")
    .IsDependentOn("PackageZip")
    .IsDependentOn("PackageEngine")
    .IsDependentOn("PackageEngineApi");

//////////////////////////////////////////////////////////////////////
// BUILD AND TEST INDIVIDUAL PACKAGES
//////////////////////////////////////////////////////////////////////

Task("PackageConsole")
    .Description("Build and Test NUnit.Console NuGet Package")
    .Does<BuildSettings>(settings =>
    {
        settings.ConsoleNuGetPackage.BuildVerifyAndTest();
    });

Task("PackageConsoleRunner")
    .Description("Build and Test NUnit.ConsoleRunner NuGet Package")
    .Does<BuildSettings>(settings =>
    {
        settings.ConsoleRunnerNuGetPackage.BuildVerifyAndTest();
    });
        
Task("PackageDotNetConsoleRunner")
    .Description("Build and Test NUnit.ConsoleRunner NuGet Package")
    .Does<BuildSettings>(settings =>
    {
        settings.DotNetConsoleRunnerNuGetPackage.BuildVerifyAndTest();
    });

Task("PackageChocolateyConsoleRunner")
    .Description("Build Verify and Test the Chocolatey nunit-console-runner package")
    .Does<BuildSettings>(settings =>
    {
        settings.ConsoleRunnerChocolateyPackage.BuildVerifyAndTest();
    });

Task("PackageMsi")
    .Description("Build, Verify and Test the MSI package")
    .IsDependentOn("FetchBundledExtensions")
    .Does<BuildSettings>(settings =>
    {
        settings.ConsoleMsiPackage.BuildVerifyAndTest();
    });

Task("PackageZip")
    .Description("Build, Verify and Test the Zip package")
    .IsDependentOn("FetchBundledExtensions")
    .IsDependentOn("CreateZipImage")
    .Does<BuildSettings>(settings =>
    {
        settings.ConsoleZipPackage.BuildVerifyAndTest();
    });

Task("PackageEngine")
    .Description("Build and Verify the NUnit.Engine package")
    .Does<BuildSettings>(settings =>
    {
        settings.EngineNuGetPackage.BuildVerifyAndTest();
    });

Task("PackageEngineApi")
    .Description("Build and Verify the NUnit.Engine.Api package")
    .Does<BuildSettings>(settings =>
    {
        settings.EngineApiNuGetPackage.BuildVerifyAndTest();
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
    .Does<BuildSettings>(settings =>
    {
        if (!settings.ShouldPublishToMyGet)
            Information("Nothing to publish to MyGet from this run.");
        else if (settings.NoPush)
            Information("NoPush option suppressing publication to MyGet");
        else
        {
            foreach (var package in settings.AllPackages)
                try
                {
                    if (package is NuGetPackageDefinition)
                        PushNuGetPackage(package.PackageFilePath, settings.MyGetApiKey, MYGET_PUSH_URL);
                    else if (package is ChocolateyPackageDefinition)
                        PushChocolateyPackage(package.PackageFilePath, settings.MyGetApiKey, MYGET_PUSH_URL);
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
    .Does<BuildSettings>(settings =>
    {
        if (!settings.ShouldPublishToNuGet)
            Information("Nothing to publish to NuGet from this run.");
        else if (settings.NoPush)
            Information("NoPush option suppressing publication to NuGet");
        else
        {
            foreach (var package in settings.AllPackages)
                if (package is NuGetPackageDefinition)
                    try
                    {
                        PushNuGetPackage(package.PackageFilePath, settings.NuGetApiKey, NUGET_PUSH_URL);
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
    .Does<BuildSettings>(settings =>
    {
        if (!settings.ShouldPublishToChocolatey)
            Information("Nothing to publish to Chocolatey from this run.");
        else if (settings.NoPush)
            Information("NoPush option suppressing publication to Chocolatey");
        else
        {
            foreach (var package in settings.AllPackages)
                if (package is ChocolateyPackageDefinition)
                    try
                    {
                        PushChocolateyPackage(package.PackageFilePath, settings.ChocolateyApiKey, CHOCO_PUSH_URL);
                    }
                    catch (Exception)
                    {
                        HadPublishingErrors = true;
                    }
        }
    });

Task("ListInstalledNetCoreRuntimes")
    .Description("Lists all installed .NET Core Runtimes visible to the script")
    .Does(() => 
    {
        foreach (var runtime in GetInstalledNetCoreRuntimes())
        {
            Information(runtime);            
        }
    });

//////////////////////////////////////////////////////////////////////
// CREATE A DRAFT RELEASE
//////////////////////////////////////////////////////////////////////

Task("CreateDraftRelease")
    .Does<BuildSettings>(settings =>
    {
        bool isDirectTarget = Target == "CreateDraftRelease";

        if (isDirectTarget && !HasArgument("productVersion"))
            throw new Exception("Must specify --productVersion with the CreateDraftRelease target.");

        if (settings.IsReleaseBranch || isDirectTarget)
        {
            string milestone = settings.IsReleaseBranch
                ? settings.BranchName.Substring(8)
                : settings.ProductVersion;
            string releaseName = $"NUnit Console and Engine {milestone}";

            Information($"Creating draft release for {releaseName}");

            if (settings.NoPush)
                Information("NoPush option suppressed creation of draft release");
            else
                try
                {
                    GitReleaseManagerCreate(settings.GitHubAccessToken, GITHUB_OWNER, GITHUB_REPO, new GitReleaseManagerCreateSettings()
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
    .Does<BuildSettings>(settings =>
    {
        if (settings.IsProductionRelease)
        {
            string token = settings.GitHubAccessToken;
            string tagName = settings.ProductVersion;

            var assetList = new List<string>();
            foreach (var package in settings.AllPackages)
                assetList.Add(package.PackageFilePath);
            string assets = $"\"{string.Join(',', assetList.ToArray())}\"";

            Information($"Publishing release {tagName} to GitHub");

            if (settings.NoPush)
            {
                Information("NoPush option suppressed publishing of assets:");
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
    .IsDependentOn("TestNetFxConsole")
    .IsDependentOn("TestNetCoreConsole");

Task("TestEngineCore")
    .Description("Builds and tests the engine core assembly")
    .IsDependentOn("TestNet20EngineCore")
    .IsDependentOn("TestNetStandard20EngineCore")
    .IsDependentOn("TestNetCore31EngineCore");

Task("TestEngine")
    .Description("Builds and tests the engine assembly")
    .IsDependentOn("TestNetFxEngine")
    .IsDependentOn("TestNetStandard20Engine")
    .IsDependentOn("TestNetCore31Engine");

Task("Test")
    .Description("Builds and tests the engine and console runner")
    .IsDependentOn("TestEngineCore")
    .IsDependentOn("TestEngine")
    .IsDependentOn("TestConsole")
    .IsDependentOn("CheckForTestErrors");

Task("Package")
    .Description("Builds and tests all packages")
    .IsDependentOn("Build")
    .IsDependentOn("PackageExistingBuild");

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
