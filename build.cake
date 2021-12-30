#load cake/ci.cake
#load cake/packaging.cake
#load cake/package-checks.cake
#load cake/test-results.cake
#load cake/package-tests.cake
#load cake/package-tester.cake
#load cake/header-check.cake
#load cake/local-tasks.cake

// Install Tools
#tool NuGet.CommandLine&version=5.3.1

//////////////////////////////////////////////////////////////////////
// ARGUMENTS & INITIALISATION
//////////////////////////////////////////////////////////////////////

var target = Argument("target", "Default");
var configuration = Argument("configuration", "Release");
var productVersion = Argument("productVersion", "3.13.0");

var UnreportedErrors = new List<string>();
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

// Some values are static so they may be used in property initialization
static string PROJECT_DIR; PROJECT_DIR = Context.Environment.WorkingDirectory.FullPath + "/";
static string PACKAGE_DIR; PACKAGE_DIR = Argument("artifact-dir", PROJECT_DIR + "package") + "/";
static string PACKAGE_TEST_DIR; PACKAGE_TEST_DIR = PACKAGE_DIR + "tests/";
static string PACKAGE_RESULT_DIR; PACKAGE_RESULT_DIR = PACKAGE_DIR + "results/";
var BIN_DIR = PROJECT_DIR + "bin/" + configuration + "/";
var NUGET_DIR = PROJECT_DIR + "nuget/";
var CHOCO_DIR = PROJECT_DIR + "choco/";
var MSI_DIR = PROJECT_DIR + "msi/";
var ZIP_DIR = PROJECT_DIR + "zip/";
var TOOLS_DIR = PROJECT_DIR + "tools/";
var IMAGE_DIR = PROJECT_DIR + "images/";
var MSI_IMG_DIR = IMAGE_DIR + "msi/";
var ZIP_IMG_DIR = IMAGE_DIR + "zip/";
var SOURCE_DIR = PROJECT_DIR + "src/";
var EXTENSIONS_DIR = PROJECT_DIR + "bundled-extensions";
var ZIP_IMG = PROJECT_DIR + "zip-image/";

var SOLUTION_FILE = PROJECT_DIR + "NUnitConsole.sln";
var ENGINE_CSPROJ = SOURCE_DIR + "NUnitEngine/nunit.engine/nunit.engine.csproj";
var AGENT_CSPROJ = SOURCE_DIR + "NUnitEngine/nunit-agent/nunit-agent.csproj";
var ENGINE_API_CSPROJ = SOURCE_DIR + "NUnitEngine/nunit.engine.api/nunit.engine.api.csproj";
var ENGINE_TESTS_CSPROJ = SOURCE_DIR + "NUnitEngine/nunit.engine.tests/nunit.engine.tests.csproj";
var CONSOLE_CSPROJ = SOURCE_DIR + "NUnitConsole/nunit3-console/nunit3-console.csproj";
var CONSOLE_TESTS_CSPROJ = SOURCE_DIR + "NUnitConsole/nunit3-console.tests/nunit3-console.tests.csproj";

var NETFX_FRAMEWORKS = new [] { "net20", "net35" }; //Production code targets net20, tests target nets35

// Test Runners
var NET20_CONSOLE = BIN_DIR + "net20/nunit3-console.exe";
var NETCORE31_CONSOLE = BIN_DIR + "netcoreapp3.1/nunit3-console.dll";

// Test Assemblies
var ENGINE_TESTS = "nunit.engine.tests.dll";
var CONSOLE_TESTS = "nunit3-console.tests.dll";

// Package sources for nuget restore
var PACKAGE_SOURCE = new string[]
{
    "https://www.nuget.org/api/v2",
    "https://www.myget.org/F/nunit/api/v2"
};

// Extensions we bundle
var BUNDLED_EXTENSIONS = new []
{
  "NUnit.Extension.VSProjectLoader",
  "NUnit.Extension.NUnitProjectLoader",
  "NUnit.Extension.NUnitV2Driver",
  "NUnit.Extension.NUnitV2ResultWriter",
  "NUnit.Extension.TeamCityEventListener"
};

// This needs to be loaded after we have defined our constants
#load cake/package-definitions.cake

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

            // NuGet limits "special version part" to 20 chars. Add one for the hyphen.
            if (suffix.Length > 21)
                suffix = suffix.Substring(0, 21);

            productVersion = version + suffix;
        }

        AppVeyor.UpdateBuildVersion(productVersion);
    }

    InitializePackageDefinitions();

    // Executed BEFORE the first task.
    Information("Building {0} version {1} of NUnit Console/Engine.", configuration, productVersion);
});

Teardown(context =>
{
    // Executed AFTER the last task.
    DisplayUnreportedErrors();
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
        CleanDirectory(EXTENSIONS_DIR);
        CleanDirectory(PACKAGE_DIR);
    });

//////////////////////////////////////////////////////////////////////
// INITIALIZE FOR BUILD
//////////////////////////////////////////////////////////////////////

Task("UpdateAssemblyInfo")
    .Description("Sets the assembly versions to the calculated version.")
    .Does(() =>
    {
        PatchAssemblyInfo(SOURCE_DIR + "NUnitConsole/ConsoleVersion.cs", productVersion, version);
        PatchAssemblyInfo(SOURCE_DIR + "NUnitEngine/EngineApiVersion.cs", productVersion, assemblyVersion: null);
        PatchAssemblyInfo(SOURCE_DIR + "NUnitEngine/EngineVersion.cs", productVersion, version);
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
    .IsDependentOn("CheckHeaders")
    .IsDependentOn("Clean")
    .IsDependentOn("UpdateAssemblyInfo")
    .Does(() =>
    {
        MSBuild(SOLUTION_FILE, CreateMSBuildSettings("Build").WithRestore());

        Information("Publishing .NET Core & Standard projects so that dependencies are present...");

        foreach(var framework in new [] { "netstandard2.0", "netcoreapp3.1" })
            MSBuild(ENGINE_CSPROJ, CreateMSBuildSettings("Publish")
               .WithProperty("TargetFramework", framework)
               .WithProperty("PublishDir", BIN_DIR + framework));

        foreach (var framework in new[] { "netcoreapp3.1" })
            MSBuild(AGENT_CSPROJ, CreateMSBuildSettings("Publish")
               .WithProperty("TargetFramework", framework)
               .WithProperty("PublishDir", BIN_DIR + "agents/" + framework));

        foreach (var framework in new[] { "netstandard2.0" })
            MSBuild(ENGINE_API_CSPROJ, CreateMSBuildSettings("Publish")
               .WithProperty("TargetFramework", framework)
               .WithProperty("PublishDir", BIN_DIR + framework));

        foreach (var framework in new [] { "netcoreapp2.1", "netcoreapp3.1" })
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
// TEST .NET 2.0 ENGINE
//////////////////////////////////////////////////////////////////////

Task("TestNet20Engine")
    .Description("Tests the engine")
    .IsDependentOn("Build")
    .OnError(exception => { UnreportedErrors.Add(exception.Message); })
    .Does(() =>
    {
        RunTest(
            NET20_CONSOLE,
            BIN_DIR + "net35/",
            ENGINE_TESTS,
            "net35", 
            ref UnreportedErrors);
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
        RunDotnetCoreNUnitLiteTests(
            BIN_DIR + "netcoreapp2.1/" + ENGINE_TESTS,
            BIN_DIR + "netcoreapp2.1",
            "netcoreapp2.1",
            ref UnreportedErrors);
    });

//////////////////////////////////////////////////////////////////////
// TEST NETCORE 3.1 ENGINE
//////////////////////////////////////////////////////////////////////

Task("TestNetCore31Engine")
    .Description("Tests the .NET Core 3.1 Engine")
    .IsDependentOn("Build")
    .OnError(exception => { UnreportedErrors.Add(exception.Message); })
    .Does(() =>
    {
        var runtimes = new[] { "3.1", "5.0" };

        foreach (var runtime in runtimes)
        {
            RunDotnetCoreTests(
                NETCORE31_CONSOLE,
                BIN_DIR + "netcoreapp3.1/",
                ENGINE_TESTS,
                runtime,
                ref UnreportedErrors);
        }
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
        RunTest(NET20_CONSOLE,
            BIN_DIR + "net35/",
            CONSOLE_TESTS,
            "net35",
            ref UnreportedErrors);
    });

//////////////////////////////////////////////////////////////////////
// TEST .NET CORE 3.1 CONSOLE
//////////////////////////////////////////////////////////////////////

Task("TestNetCore31Console")
    .Description("Tests the .NET Core 3.1 console runner")
    .IsDependentOn("Build")
    .OnError(exception => { UnreportedErrors.Add(exception.Message); })
    .Does(() =>
    {
        var runtimes = new[] { "3.1", "5.0" };

        foreach (var runtime in runtimes)
        {
            RunDotnetCoreTests(
                NETCORE31_CONSOLE,
                BIN_DIR + "netcoreapp3.1/",
                CONSOLE_TESTS,
                runtime,
                ref UnreportedErrors);
        }
    });

//////////////////////////////////////////////////////////////////////
// BUILD PACKAGES
//////////////////////////////////////////////////////////////////////

Task("BuildPackages")
    .Does(() =>
    {
        EnsureDirectoryExists(PACKAGE_DIR);
        FetchExtensions();

        foreach (var package in AllPackages)
            BuildPackage(package);
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

void DisplayUnreportedErrors()
{
    if(UnreportedErrors.Count > 0)
    {
        string msg = "One or more unit tests failed, breaking the build.\r\n"
          + UnreportedErrors.Aggregate((x, y) => x + "\r\n" + y);

        UnreportedErrors.Clear();
        throw new Exception(msg);
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

public void CopyPackageContents(DirectoryPath packageDir, DirectoryPath outDir)
{
    var files = GetFiles(packageDir + "/tools/*").Concat(GetFiles(packageDir + "/tools/net20/*"));
    CopyFiles(files.Where(f => f.GetExtension() != ".addins"), outDir);
}

//////////////////////////////////////////////////////////////////////
// TASK TARGETS
//////////////////////////////////////////////////////////////////////

Task("TestConsole")
    .Description("Builds and tests the console runner")
    .IsDependentOn("TestNet20Console")
    .IsDependentOn("TestNetCore31Console");

Task("TestEngine")
    .Description("Builds and tests the engine")
    .IsDependentOn("TestNet20Engine")
    .IsDependentOn("TestNetStandard20Engine")
    .IsDependentOn("TestNetCore31Engine");

Task("Test")
    .Description("Builds and tests the engine and console runner")
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
    .IsDependentOn("BuildTestAndPackage");

Task("Default")
    .Description("Builds the engine and console runner")
    .IsDependentOn("Build");

//////////////////////////////////////////////////////////////////////
// EXECUTION
//////////////////////////////////////////////////////////////////////

RunTarget(target);
