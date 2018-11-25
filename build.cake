#load ci.cake

//////////////////////////////////////////////////////////////////////
// ARGUMENTS & INITIALISATION
//////////////////////////////////////////////////////////////////////

var target = Argument("target", "Default");
var configuration = Argument("configuration", "Release");

var ErrorDetail = new List<string>();
bool IsDotNetCoreInstalled = false;

//////////////////////////////////////////////////////////////////////
// SET PACKAGE VERSION
//////////////////////////////////////////////////////////////////////

var version = "3.10.0";
var modifier = "";

var isAppveyor = BuildSystem.IsRunningOnAppVeyor;
var dbgSuffix = configuration == "Debug" ? "-dbg" : "";
var productVersion = version + modifier + dbgSuffix;

//////////////////////////////////////////////////////////////////////
// DEFINE RUN CONSTANTS
//////////////////////////////////////////////////////////////////////

var PROJECT_DIR = Context.Environment.WorkingDirectory.FullPath + "/";
var PACKAGE_DIR = PROJECT_DIR + "package/";
var BIN_DIR = PROJECT_DIR + "bin/" + configuration + "/net35/";
var CHOCO_DIR = PROJECT_DIR + "choco/";
var TOOLS_DIR = PROJECT_DIR + "tools/";
var IMAGE_DIR = PROJECT_DIR + "images/";
var MSI_DIR = PROJECT_DIR + "msi/";
var CURRENT_IMG_DIR = IMAGE_DIR + $"NUnit-{productVersion}/";
var CURRENT_IMG_BIN_DIR = CURRENT_IMG_DIR + "bin/";
var EXTENSION_PACKAGES_DIR = PROJECT_DIR + "extension-packages/";
var ZIP_IMG = PROJECT_DIR + "zip-image/";

var SOLUTION_FILE = "NUnitConsole.sln";
var DOTNETCORE_SOLUTION_FILE = "NUnit.Engine.NetStandard.sln";

// Test Runner
var NUNIT3_CONSOLE = BIN_DIR + "nunit3-console.exe";

// Test Assemblies
var ENGINE_TESTS = "nunit.engine.tests.dll";
var CONSOLE_TESTS = "nunit3-console.tests.dll";
var DOTNETCORE_TEST_ASSEMBLY = "src/NUnitEngine/nunit.engine.tests.netstandard/bin/" + configuration + "/netcoreapp1.1/nunit.engine.tests.netstandard.dll";

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
            productVersion = version + "-dev-" + buildNumber + dbgSuffix;
        }
        else
        {
            var suffix = "-ci-" + buildNumber + dbgSuffix;

            if (isPullRequest)
                suffix += "-pr-" + AppVeyor.Environment.PullRequest.Number;
            else if (AppVeyor.Environment.Repository.Branch.StartsWith("release", StringComparison.OrdinalIgnoreCase))
                suffix += "-pre-" + buildNumber;
            else
                suffix += "-" + branch;

            // Nuget limits "special version part" to 20 chars. Add one for the hyphen.
            if (suffix.Length > 21)
                suffix = suffix.Substring(0, 21);

            productVersion = version + suffix;
        }

        AppVeyor.UpdateBuildVersion(productVersion);
    }

    // Executed BEFORE the first task.
    Information("Building {0} version {1} of NUnit Console/Engine.", configuration, productVersion);
    IsDotNetCoreInstalled = CheckIfDotNetCoreInstalled();
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
// BUILD ENGINE
//////////////////////////////////////////////////////////////////////

MSBuildSettings CreateMSBuildSettings(string target) => new MSBuildSettings()
    .SetConfiguration(configuration)
    .SetVerbosity(Verbosity.Minimal)
    .WithProperty("PackageVersion", productVersion)
    .WithTarget(target)
    // Workaround for https://github.com/Microsoft/msbuild/issues/3626
    .WithProperty("AddSyntheticProjectReferencesForSolutionDependencies", "false");

Task("BuildNetFramework")
    .Description("Builds the .NET Framework version of the engine and console")
    .IsDependentOn("UpdateAssemblyInfo")
    .Does(() =>
    {
        MSBuild(SOLUTION_FILE, CreateMSBuildSettings("Build").WithRestore());
    });

Task("BuildNetStandardEngine")
    .Description("Builds the .NET Standard engine")
    .IsDependentOn("UpdateAssemblyInfo")
    .WithCriteria(IsRunningOnWindows())
    .Does(() =>
    {
        if (IsDotNetCoreInstalled)
        {
            MSBuild(DOTNETCORE_SOLUTION_FILE, CreateMSBuildSettings("Build").WithRestore());
        }
        else
        {
            Warning("Skipping .NET Standard build because .NET Core is not installed");
        }
    });

//////////////////////////////////////////////////////////////////////
// BUILD C++  TESTS
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

Task("TestEngine")
    .Description("Tests the engine")
    .IsDependentOn("Build")
    .OnError(exception => { ErrorDetail.Add(exception.Message); })
    .Does(() =>
    {
        RunTest(NUNIT3_CONSOLE, BIN_DIR, ENGINE_TESTS, "TestEngine", ref ErrorDetail);
    });

//////////////////////////////////////////////////////////////////////
// TEST CONSOLE
//////////////////////////////////////////////////////////////////////

Task("TestConsole")
    .Description("Tests the console runner")
    .IsDependentOn("Build")
    .OnError(exception => { ErrorDetail.Add(exception.Message); })
    .Does(() =>
    {
        RunTest(NUNIT3_CONSOLE, BIN_DIR, CONSOLE_TESTS, "TestConsole", ref ErrorDetail);
    });

//////////////////////////////////////////////////////////////////////
// TEST NETSTANDARD ENGINE
//////////////////////////////////////////////////////////////////////

Task("TestNetStandardEngine")
    .Description("Tests the .NET Standard Engine")
    .IsDependentOn("BuildNetStandardEngine")
    .WithCriteria(IsRunningOnWindows())
    .OnError(exception => { ErrorDetail.Add(exception.Message); })
    .Does(() =>
    {
        if(IsDotNetCoreInstalled)
        {
            DotNetCoreExecute(DOTNETCORE_TEST_ASSEMBLY);
        }
        else
        {
            Warning("Skipping .NET Standard tests because .NET Core is not installed");
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

var BinFiles = new FilePath[]
{
    "ConsoleTests.nunit",
    "EngineTests.nunit",
    "mock-assembly.dll",
	"notest-assembly.dll",
    "Mono.Cecil.dll",
    "nunit-agent-x86.exe",
    "nunit-agent-x86.exe.config",
    "nunit-agent.exe",
    "nunit-agent.exe.config",
    "nunit.engine.addins",
    "nunit.engine.api.dll",
    "nunit.engine.api.xml",
    "nunit.engine.dll",
    "nunit.engine.tests.dll",
    "nunit.engine.tests.dll.config",
    "nunit.framework.dll",
    "nunit.framework.xml",
    "NUnit.System.Linq.dll",
    "NUnit2TestResult.xsd",
    "nunit3-console.exe",
    "nunit3-console.exe.config",
    "nunit3-console.tests.dll",
    "TestListWithEmptyLine.tst",
    "TextSummary.xslt",
};

Task("CreateImage")
    .Description("Copies all files into the image directory")
    .Does(() =>
    {
        CleanDirectory(CURRENT_IMG_DIR);

        CopyFiles(RootFiles, CURRENT_IMG_DIR);

        CreateDirectory(CURRENT_IMG_BIN_DIR);
        Information("Created directory " + CURRENT_IMG_BIN_DIR);

        foreach(FilePath file in BinFiles)
        {
          if (FileExists(BIN_DIR + file))
          {
              CreateDirectory(CURRENT_IMG_BIN_DIR + file.GetDirectory());
              CopyFile(BIN_DIR + file, CURRENT_IMG_BIN_DIR + file);
            }
        }
    });

Task("PackageEngine")
    .Description("Creates NuGet packages of the engine")
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
    });

Task("PackageConsole")
    .Description("Creates NuGet packages of the console runner")
    .IsDependentOn("CreateImage")
    .Does(() =>
    {
        CreateDirectory(PACKAGE_DIR);

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

        NuGetPack("nuget/runners/nunit.runners.nuspec", new NuGetPackSettings()
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
                    new ChocolateyNuSpecContent { Source = PROJECT_DIR + "LICENSE.txt", Target = "tools" },
                    new ChocolateyNuSpecContent { Source = PROJECT_DIR + "NOTICES.txt", Target = "tools" },
                    new ChocolateyNuSpecContent { Source = PROJECT_DIR + "CHANGES.txt", Target = "tools" },
                    new ChocolateyNuSpecContent { Source = CHOCO_DIR + "VERIFICATION.txt", Target = "tools" },
                    new ChocolateyNuSpecContent { Source = CHOCO_DIR + "nunit.choco.addins", Target = "tools" },
                    new ChocolateyNuSpecContent { Source = BIN_DIR + "nunit-agent.exe", Target="tools" },
                    new ChocolateyNuSpecContent { Source = BIN_DIR + "nunit-agent.exe.config", Target="tools" },
                    new ChocolateyNuSpecContent { Source = CHOCO_DIR + "nunit-agent.exe.ignore", Target="tools" },
                    new ChocolateyNuSpecContent { Source = BIN_DIR + "nunit-agent-x86.exe", Target="tools" },
                    new ChocolateyNuSpecContent { Source = BIN_DIR + "nunit-agent-x86.exe.config", Target="tools" },
                    new ChocolateyNuSpecContent { Source = CHOCO_DIR + "nunit-agent-x86.exe.ignore", Target="tools" },
                    new ChocolateyNuSpecContent { Source = BIN_DIR + "nunit3-console.exe", Target="tools" },
                    new ChocolateyNuSpecContent { Source = BIN_DIR + "nunit3-console.exe.config", Target="tools" },
                    new ChocolateyNuSpecContent { Source = BIN_DIR + "nunit.engine.api.dll", Target="tools" },
                    new ChocolateyNuSpecContent { Source = BIN_DIR + "nunit.engine.api.xml", Target="tools" },
                    new ChocolateyNuSpecContent { Source = BIN_DIR + "nunit.engine.dll", Target="tools" },
                    new ChocolateyNuSpecContent { Source = BIN_DIR + "Mono.Cecil.dll", Target="tools" }
                }
			});

		ChocolateyPack("choco/nunit-console-with-extensions.nuspec",
			new ChocolateyPackSettings()
			{
				Version = productVersion,
				OutputDirectory = PACKAGE_DIR,
                Files = new [] {
                    new ChocolateyNuSpecContent { Source = PROJECT_DIR + "LICENSE.txt", Target = "tools" },
                    new ChocolateyNuSpecContent { Source = PROJECT_DIR + "NOTICES.txt", Target = "tools" },
                    new ChocolateyNuSpecContent { Source = CHOCO_DIR + "VERIFICATION.txt", Target = "tools" }
                }
			});
	});

//////////////////////////////////////////////////////////////////////
// PACKAGE NETSTANDARD ENGINE
//////////////////////////////////////////////////////////////////////

Task("PackageNetStandardEngine")
    .Description("Copies the .NET Standard Engine nuget package in the packages directory")
    .WithCriteria(IsRunningOnWindows())
    .Does(() =>
    {
        if(IsDotNetCoreInstalled)
        {
            var nuget = $"nunit.engine.netstandard.{productVersion}.nupkg";
            var src   = "src/NUnitEngine/nunit.engine.netstandard/bin/" + configuration + "/" + nuget;
            var dest  = PACKAGE_DIR + nuget;

            CreateDirectory(PACKAGE_DIR);
            CopyFile(src, dest);
        }
    });

//////////////////////////////////////////////////////////////////////
// PACKAGE COMBINED DISTRIBUTIONS
//////////////////////////////////////////////////////////////////////

Task("FetchExtensions")
.Does(() =>
{
    foreach(var package in EXTENSION_PACKAGES)
    {
        NuGetInstall(package, new NuGetInstallSettings {
                        OutputDirectory = EXTENSION_PACKAGES_DIR,
                        Source = new [] { "https://www.nuget.org/api/v2" }
                    });
    }
});

Task("CreateCombinedImage")
.IsDependentOn("FetchExtensions")
.Does(() =>
{
    var addinsImgDir = CURRENT_IMG_BIN_DIR + "addins/";

    CopyDirectory(MSI_DIR + "resources/", CURRENT_IMG_DIR);
    CleanDirectory(addinsImgDir);

    foreach(var packageDir in GetAllDirectories(EXTENSION_PACKAGES_DIR))
        CopyPackageContents(packageDir, addinsImgDir);
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

    //Flatten for zip
    CopyDirectory(CURRENT_IMG_DIR, ZIP_IMG);
    CopyDirectory(ZIP_IMG + "bin/", ZIP_IMG);
    DeleteDirectory(ZIP_IMG + "bin/", new DeleteDirectorySettings { Recursive = true });

    //Ensure single and correct addins file
    DeleteFiles(ZIP_IMG + "*.addins");
    CopyFile(CURRENT_IMG_DIR + "nunit.bundle.addins", ZIP_IMG + "nunit.bundle.addins");

    var zipPath = string.Format("{0}NUnit.Console-{1}.zip", PACKAGE_DIR, version);
    Zip(ZIP_IMG, zipPath);
});

//////////////////////////////////////////////////////////////////////
// HELPER METHODS - GENERAL
//////////////////////////////////////////////////////////////////////

bool CheckIfDotNetCoreInstalled()
{
    try
    {
        Information("Checking if .NET Core SDK is installed");
        StartProcess("dotnet", new ProcessSettings
        {
            Arguments = "--version"
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

void RunTest(FilePath exePath, DirectoryPath workingDir, string framework, ref List<string> errorDetail)
{
    int rc = StartProcess(
        MakeAbsolute(exePath),
        new ProcessSettings()
        {
            WorkingDirectory = workingDir
        });

    if (rc > 0)
        errorDetail.Add(string.Format("{0}: {1} tests failed",framework, rc));
    else if (rc < 0)
        errorDetail.Add(string.Format("{0} returned rc = {1}", exePath, rc));
}

void RunTest(FilePath exePath, DirectoryPath workingDir, string arguments, string framework, ref List<string> errorDetail)
{
    int rc = StartProcess(
        MakeAbsolute(exePath),
        new ProcessSettings()
        {
            Arguments = arguments,
            WorkingDirectory = workingDir
        });

    if (rc > 0)
        errorDetail.Add(string.Format("{0}: {1} tests failed",framework, rc));
    else if (rc < 0)
        errorDetail.Add(string.Format("{0} returned rc = {1}", exePath, rc));
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
    var files = GetFiles(packageDir + "/tools/*");
    CopyFiles(files.Where(f => f.GetExtension() != ".addins"), outDir);
}

//////////////////////////////////////////////////////////////////////
// TASK TARGETS
//////////////////////////////////////////////////////////////////////

Task("Build")
    .Description("Builds the engine and console runner")
    .IsDependentOn("BuildNetFramework")
    .IsDependentOn("BuildNetStandardEngine");

Task("Rebuild")
    .Description("Rebuilds the engine and console runner")
    .IsDependentOn("Clean")
    .IsDependentOn("Build");

Task("Test")
    .Description("Builds and tests the engine and console runner")
    .IsDependentOn("TestEngine")
    .IsDependentOn("TestConsole")
    .IsDependentOn("TestNetStandardEngine");

Task("Package")
    .Description("Packages the engine and console runner")
    .IsDependentOn("CheckForError")
    .IsDependentOn("PackageEngine")
    .IsDependentOn("PackageConsole")
    .IsDependentOn("PackageNetStandardEngine")
	.IsDependentOn("PackageChocolatey")
    .IsDependentOn("PackageMsi")
    .IsDependentOn("PackageZip");

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
    .IsDependentOn("Build"); // Rebuild?

//////////////////////////////////////////////////////////////////////
// EXECUTION
//////////////////////////////////////////////////////////////////////

RunTarget(target);
