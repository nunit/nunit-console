static string Target; Target = Argument("target", Argument("t", "Default"));

#load cake/constants.cake
#load cake/build-settings.cake
#load cake/dotnet.cake
#load cake/header-check.cake
#load cake/package-checks.cake
#load cake/test-results.cake
#load cake/versioning.cake
#load cake/utilities.cake
#load cake/package-definitions.cake

// Install Tools
#tool NuGet.CommandLine&version=6.0.0
#tool dotnet:?package=GitVersion.Tool&version=5.12.0
#tool dotnet:?package=GitReleaseManager.Tool&version=0.17.0

var UnreportedErrors = new List<string>();
var installedNetCoreRuntimes = GetInstalledNetCoreRuntimes();

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
    .Does(() =>
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
        if (IsRunningOnWindows())
            BuildSolution(settings);
        else
            BuildEachProjectSeparately(settings);
    });

public void BuildSolution(BuildSettings settings)
{
    MSBuild(SOLUTION_FILE, CreateMSBuildSettings("Build", settings).WithRestore());
}

private void BuildEachProjectSeparately(BuildSettings settings)
{
    DotNetRestore(SOLUTION_FILE);

    BuildProject(ENGINE_PROJECT, settings);
    BuildProject(NETFX_CONSOLE_PROJECT, settings);
    //BuildProject(NETCORE_CONSOLE_PROJECT, settings);
    BuildProject(NET20_AGENT_PROJECT, settings);
    BuildProject(NET20_AGENT_X86_PROJECT, settings);
    BuildProject(NET462_AGENT_PROJECT, settings);
    BuildProject(NET462_AGENT_X86_PROJECT, settings);
    BuildProject(NET50_AGENT_PROJECT, settings);
    BuildProject(NET60_AGENT_PROJECT, settings);
    BuildProject(NETCORE31_AGENT_PROJECT, settings);

    BuildProject(ENGINE_TESTS_PROJECT, settings, "net462", "netcoreapp2.1", "netcoreapp3.1");
    BuildProject(ENGINE_CORE_TESTS_PROJECT, settings, "net462", "netcoreapp2.1", "netcoreapp3.1", "net5.0", "net6.0");
    BuildProject(CONSOLE_TESTS_PROJECT, settings, NETFX_CONSOLE_TARGET, "net6.0");

    BuildProject(MOCK_ASSEMBLY_X86_PROJECT, settings, "net35", "net462", "netcoreapp2.1", "netcoreapp3.1");
    BuildProject(NOTEST_PROJECT, settings, "net35", "netcoreapp2.1", "netcoreapp3.1");
    BuildProject(WINDOWS_FORMS_TEST_PROJECT, settings);
    BuildProject(ASP_NET_CORE_TEST_PROJECT, settings);
}

// NOTE: If we use DotNet to build on Linux, then our net35 projects fail.
// If we use MSBuild, then the net5.0 projects fail. So we build each project
// differently depending on whether it has net35 as one of its targets. 
private void BuildProject(string project, BuildSettings settings, params string[] targetFrameworks)
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
            if (framework == "net35")
                MSBuild(project, CreateMSBuildSettings("Build", settings).WithProperty("TargetFramework", framework));
            else
                DotNetMSBuild(project, CreateDotNetMSBuildSettings("Build", settings).WithProperty("TargetFramework", framework));
        }
    }
}

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
// TEST .NET 4.6.2 ENGINE CORE
//////////////////////////////////////////////////////////////////////

Task("TestNet462EngineCore")
    .Description("Tests the Net 4.6.2 engine core assembly")
    .IsDependentOn("Build")
    .OnError(exception => { UnreportedErrors.Add(exception.Message); })
    .Does<BuildSettings>(settings =>
    {
        RunNUnitLiteTests(ENGINE_CORE_TESTS_PROJECT, settings.Configuration, "net462");
    });

//////////////////////////////////////////////////////////////////////
// TEST .NET CORE 3.1 ENGINE CORE
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
// TEST .NET 6.0 ENGINE CORE
//////////////////////////////////////////////////////////////////////

Task("TestNet60EngineCore")
    .Description("Tests the .NET 6.0 Engine core assembly")
    .IsDependentOn("Build")
    .OnError(exception => { UnreportedErrors.Add(exception.Message); })
    .Does<BuildSettings>(settings =>
    {
        RunDotnetNUnitLiteTests(ENGINE_CORE_TESTS_PROJECT, settings.Configuration, "net6.0");
    });

//////////////////////////////////////////////////////////////////////
// TEST .NET 8.0 ENGINE CORE
//////////////////////////////////////////////////////////////////////

Task("TestNet80EngineCore")
    .Description("Tests the .NET 8.0 Engine core assembly")
    .IsDependentOn("Build")
    .OnError(exception => { UnreportedErrors.Add(exception.Message); })
    .Does<BuildSettings>(settings =>
    {
        RunDotnetNUnitLiteTests(ENGINE_CORE_TESTS_PROJECT, settings.Configuration, "net8.0");
    });

//////////////////////////////////////////////////////////////////////
// TEST .NET Framework ENGINE
//////////////////////////////////////////////////////////////////////

Task("TestNetFxEngine")
    .Description("Tests the NETFX 4.6.2 engine")
    .IsDependentOn("Build")
    .OnError(exception => { UnreportedErrors.Add(exception.Message); })
    .Does<BuildSettings>(settings =>
    {
        RunNUnitLiteTests(ENGINE_TESTS_PROJECT, settings.Configuration, "net462");
    });

//////////////////////////////////////////////////////////////////////
// TEST NETCORE ENGINE
//////////////////////////////////////////////////////////////////////

Task("TestNetCoreEngine")
    .Description("Tests the .NET Core 8.0 Engine")
    .IsDependentOn("Build")
    .OnError(exception => { UnreportedErrors.Add(exception.Message); })
    .Does<BuildSettings>(settings =>
    {
        RunDotnetNUnitLiteTests(ENGINE_TESTS_PROJECT, settings.Configuration, "net8.0");
    });

//////////////////////////////////////////////////////////////////////
// TEST .NET FRAMEWORK  CONSOLE
//////////////////////////////////////////////////////////////////////

Task("TestNetFxConsole")
    .Description("Tests the .NET Framework console runner")
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
    .Description("Tests the .NET Core console runner")
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
                Source = new[] { "https://www.myget.org/F/nunit/api/v2" },
                Prerelease = true
                //Source = new[] { "https://www.nuget.org/api/v2" }
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

        var addinsDir = zipImageDirectory + "bin/addins/";
        CleanDirectory(addinsDir);

        foreach (var packageDir in System.IO.Directory.GetDirectories(EXTENSIONS_DIR))
            CopyPackageContents(packageDir, addinsDir);
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
    //.IsDependentOn("PackageMsi")
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
                        PushNuGetPackage(PACKAGE_DIR + package.PackageFileName, settings.MyGetApiKey, MYGET_PUSH_URL);
                    else if (package is ChocolateyPackageDefinition)
                        PushChocolateyPackage(PACKAGE_DIR + package.PackageFileName, settings.MyGetApiKey, MYGET_PUSH_URL);
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
                        PushNuGetPackage(PACKAGE_DIR + package.PackageFileName, settings.NuGetApiKey, NUGET_PUSH_URL);
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
                        PushChocolateyPackage(PACKAGE_DIR + package.PackageFileName, settings.ChocolateyApiKey, CHOCO_PUSH_URL);
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

            if (!settings.NoPush)
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
    .Does<BuildSettings>(settings =>
    {
        if (settings.IsProductionRelease)
        {
            string token = EnvironmentVariable(GITHUB_ACCESS_TOKEN);
            string tagName = settings.ProductVersion;

            var assetList = new List<string>();
            foreach (var package in settings.AllPackages)
                assetList.Add(PACKAGE_DIR + package.PackageFileName);
            string assets = $"\"{string.Join(',', assetList.ToArray())}\"";

            Information($"Publishing release {tagName} to GitHub");

            if (settings.NoPush)
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

#if false
    Task("RegistryTest")
        .Does(() =>
        {
            Information($"x64: {RegistryKey.LocalMashine.OpenSubKey(@"Software/dotnet/setup")?.GetValue("Path")"32 bit base key is ")
            Information("Install Dir is " + GetDotNetInstallDirectory())
        }
        private static string GetDotNetInstallDirectory()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                var baseKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64);

                // Running on Windows so use registry
                RegistryKey baseKey = Environment.Is64BitProcess
                    ? RegistryKey.
                    : RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry32);
                RegistryKey key = Environment.Is64BitProcess
                    ? baseKey.OpenSubKey(@"Software\dotnet\SetUp\InstalledVersions\x64\sharedHost\")
                    : baseKey.OpenSubKey(@"Software\dotnet\SetUp\InstalledVersions\x86\sharedHost\");
                return (string)key?.GetValue("Path");
                //if (Environment.Is64BitProcess)
                //{
                //    RegistryKey key =
                //        Registry.LocalMachine.OpenSubKey(@"Software\dotnet\SetUp\InstalledVersions\x64\sharedHost\");
                //    return (string)key?.GetValue("Path");
                //}
                //else
                //    return (@"C:\Program Files (x86)\dotnet\");
            }
            else
                return "/usr/shared/dotnet/";
        }
#endif

//////////////////////////////////////////////////////////////////////
// TASK TARGETS
//////////////////////////////////////////////////////////////////////

Task("TestConsole")
    .Description("Builds and tests the console runner")
    .IsDependentOn("TestNetFxConsole")
    .IsDependentOn("TestNetCoreConsole");

Task("TestEngineCore")
    .Description("Builds and tests the engine core assembly")
    .IsDependentOn("TestNet462EngineCore")
    .IsDependentOn("TestNetCore31EngineCore")
    .IsDependentOn("TestNet60EngineCore")
    .IsDependentOn("TestNet80EngineCore");

Task("TestEngine")
    .Description("Builds and tests the engine assembly")
    .IsDependentOn("TestNetFxEngine")
    .IsDependentOn("TestNetCoreEngine");

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
    .IsDependentOn("PackageExistingBuild");

Task("ContinuousIntegration")
    .Description("Perform continuous integration run")
    .IsDependentOn("Build")
    .IsDependentOn("Test")
    .IsDependentOn("Package")
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
