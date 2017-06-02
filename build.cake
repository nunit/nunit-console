#addin "Cake.FileHelpers"

//////////////////////////////////////////////////////////////////////
// ARGUMENTS
//////////////////////////////////////////////////////////////////////

var target = Argument("target", "Default");
var configuration = Argument("configuration", "Debug");

//////////////////////////////////////////////////////////////////////
// SET ERROR LEVELS
//////////////////////////////////////////////////////////////////////

var ErrorDetail = new List<string>();

//////////////////////////////////////////////////////////////////////
// SET PACKAGE VERSION
//////////////////////////////////////////////////////////////////////

var version = "3.7.0";
var modifier = "";

var isAppveyor = BuildSystem.IsRunningOnAppVeyor;
var dbgSuffix = configuration == "Debug" ? "-dbg" : "";
var packageVersion = version + modifier + dbgSuffix;

//////////////////////////////////////////////////////////////////////
// DEFINE RUN CONSTANTS
//////////////////////////////////////////////////////////////////////

var PROJECT_DIR = Context.Environment.WorkingDirectory.FullPath + "/";
var PACKAGE_DIR = PROJECT_DIR + "package/";
var BIN_DIR = PROJECT_DIR + "bin/" + configuration + "/";
var IMAGE_DIR = PROJECT_DIR + "images/";

var SOLUTION_FILE = "NUnitConsole.sln";
var DOTNETCORE_SOLUTION_FILE = "NUnit.Engine.NetStandard.sln";
var DOTNETCORE_TEST_ASSEMBLY = "src/NUnitEngine/nunit.engine.tests.netstandard/bin/" + configuration + "/netcoreapp1.1/nunit.engine.tests.netstandard.dll";

// Package sources for nuget restore
var PACKAGE_SOURCE = new string[]
	{
		"https://www.nuget.org/api/v2",
		"https://www.myget.org/F/nunit/api/v2"
	};

// Test Runner
var NUNIT3_CONSOLE = BIN_DIR + "nunit3-console.exe";

// Test Assemblies
var ENGINE_TESTS = "nunit.engine.tests.dll";
var CONSOLE_TESTS = "nunit3-console.tests.dll";

bool IsDotNetCoreInstalled = false;

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
            packageVersion = version + "-dev-" + buildNumber + dbgSuffix;
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

            packageVersion = version + suffix;
        }

        AppVeyor.UpdateBuildVersion(packageVersion);
    }

    // Executed BEFORE the first task.
    Information("Building version {0} of NUnit.", packageVersion);
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
    .Description("Deletes all files in the BIN directory")
    .Does(() =>
    {
        CleanDirectory(BIN_DIR);
    });


//////////////////////////////////////////////////////////////////////
// INITIALIZE FOR BUILD
//////////////////////////////////////////////////////////////////////

Task("InitializeBuild")
    .Description("Initializes the build")
    .Does(() =>
    {
        Information("Restoring NuGet packages");
		NuGetRestore(SOLUTION_FILE, new NuGetRestoreSettings
        {
            Source = PACKAGE_SOURCE,
            Verbosity = NuGetVerbosity.Detailed
        });

        if(IsDotNetCoreInstalled && IsRunningOnWindows())
        {
            Information("Restoring .NET Core packages");
            DotNetCoreRestore(DOTNETCORE_SOLUTION_FILE);
        }
	});

//////////////////////////////////////////////////////////////////////
// BUILD ENGINE
//////////////////////////////////////////////////////////////////////

Task("BuildEngine")
    .Description("Builds the engine")
    .IsDependentOn("InitializeBuild")
    .Does(() =>
    {
        // Engine Commponents
        BuildProject("./src/NUnitEngine/nunit.engine.api/nunit.engine.api.csproj", configuration);
        BuildProject("./src/NUnitEngine/nunit.engine/nunit.engine.csproj", configuration);
        BuildProject("./src/NUnitEngine/nunit-agent/nunit-agent.csproj", configuration);
        BuildProject("./src/NUnitEngine/nunit-agent/nunit-agent-x86.csproj", configuration);

        // Engine tests
        BuildProject("./src/NUnitEngine/nunit.engine.tests/nunit.engine.tests.csproj", configuration);
        BuildProject("./src/NUnitEngine/notest-assembly/notest-assembly.csproj", configuration);
    });

//////////////////////////////////////////////////////////////////////
// BUILD NETSTANDARD ENGINE
//////////////////////////////////////////////////////////////////////

Task("BuildNetStandardEngine")
    .Description("Builds the .NET Standard engine")
    .IsDependentOn("InitializeBuild")
    .WithCriteria(IsRunningOnWindows())
    .Does(() =>
    {
        if(IsDotNetCoreInstalled)
        {
            var settings = new DotNetCoreBuildSettings
            {
                Configuration = configuration,
                EnvironmentVariables = new Dictionary<string, string>()
            };
            settings.EnvironmentVariables.Add("PackageVersion", packageVersion);
            DotNetCoreBuild(DOTNETCORE_SOLUTION_FILE, settings);
        }
        else
        {
            Warning("Skipping .NET Standard build because .NET Core is not installed");
        }
    });

//////////////////////////////////////////////////////////////////////
// BUILD CONSOLE
//////////////////////////////////////////////////////////////////////

Task("BuildConsole")
    .Description("Builds the console runner")
    .IsDependentOn("InitializeBuild")
    .Does(() =>
    {
        BuildProject("src/NUnitConsole/nunit3-console/nunit3-console.csproj", configuration);
        BuildProject("src/NUnitConsole/nunit3-console.tests/nunit3-console.tests.csproj", configuration);
    });

//////////////////////////////////////////////////////////////////////
// BUILD C++  TESTS
//////////////////////////////////////////////////////////////////////

Task("BuildCppTestFiles")
    .Description("Builds the C++ mock test assemblies")
    .IsDependentOn("InitializeBuild")
    .WithCriteria(IsRunningOnWindows)
    .Does(() =>
    {
        MSBuild("./src/NUnitEngine/mock-cpp-clr/mock-cpp-clr-x86.vcxproj", new MSBuildSettings()
            .SetConfiguration(configuration)
            .WithProperty("Platform", "x86")
            .SetVerbosity(Verbosity.Minimal)
            .SetNodeReuse(false)
        );
        MSBuild("./src/NUnitEngine/mock-cpp-clr/mock-cpp-clr-x64.vcxproj", new MSBuildSettings()
            .SetConfiguration(configuration)
            .WithProperty("Platform", "x64")
            .SetVerbosity(Verbosity.Minimal)
            .SetNodeReuse(false)
        );
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
        var currentImageDir = IMAGE_DIR + "NUnit-" + packageVersion + "/";
        var imageBinDir = currentImageDir + "bin/";

        CleanDirectory(currentImageDir);

        CopyFiles(RootFiles, currentImageDir);

        CreateDirectory(imageBinDir);
        Information("Created directory " + imageBinDir);

        foreach(FilePath file in BinFiles)
        {
          if (FileExists(BIN_DIR + file))
          {
              CreateDirectory(imageBinDir + file.GetDirectory());
              CopyFile(BIN_DIR + file, imageBinDir + file);
            }
        }
    });

Task("PackageEngine")
    .Description("Creates NuGet packages of the engine")
    .IsDependentOn("CreateImage")
    .Does(() =>
    {
        var currentImageDir = IMAGE_DIR + "NUnit-" + packageVersion + "/";

        CreateDirectory(PACKAGE_DIR);

        NuGetPack("nuget/engine/nunit.engine.api.nuspec", new NuGetPackSettings()
        {
            Version = packageVersion,
            BasePath = currentImageDir,
            OutputDirectory = PACKAGE_DIR,
            NoPackageAnalysis = true
        });

        NuGetPack("nuget/engine/nunit.engine.nuspec", new NuGetPackSettings()
        {
            Version = packageVersion,
            BasePath = currentImageDir,
            OutputDirectory = PACKAGE_DIR,
            NoPackageAnalysis = true
        });

        NuGetPack("nuget/engine/nunit.engine.tool.nuspec", new NuGetPackSettings()
        {
            Version = packageVersion,
            BasePath = currentImageDir,
            OutputDirectory = PACKAGE_DIR,
            NoPackageAnalysis = true
        });
    });

Task("PackageConsole")
    .Description("Creates NuGet packages of the console runner")
    .IsDependentOn("CreateImage")
    .Does(() =>
    {
        var currentImageDir = IMAGE_DIR + "NUnit-" + packageVersion + "/";

        CreateDirectory(PACKAGE_DIR);

        NuGetPack("nuget/runners/nunit.console-runner.nuspec", new NuGetPackSettings()
        {
            Version = packageVersion,
            BasePath = currentImageDir,
            OutputDirectory = PACKAGE_DIR,
            NoPackageAnalysis = true
        });

        NuGetPack("nuget/runners/nunit.console-runner-with-extensions.nuspec", new NuGetPackSettings()
        {
            Version = packageVersion,
            BasePath = currentImageDir,
            OutputDirectory = PACKAGE_DIR,
            NoPackageAnalysis = true
        });

        NuGetPack("nuget/runners/nunit.runners.nuspec", new NuGetPackSettings()
        {
            Version = packageVersion,
            BasePath = currentImageDir,
            OutputDirectory = PACKAGE_DIR,
            NoPackageAnalysis = true
        });
    });

Task("PackageChocolatey")
	.Description("Creates chocolate packages of the console runner")
    .IsDependentOn("CreateImage")
	.Does(() =>
	{
		var currentImageDir = IMAGE_DIR + "NUnit-" + packageVersion + "/";
		
		EnsureDirectoryExists(PACKAGE_DIR);
		
		// Note: Since cake does not yet support a working directory and separate output directory for chocolatey, the following copying and hacks are needed.
		
		// List with the addins (addin name, primary dll, additional files)
		var addins = new Tuple<string, string, string[]>[] {
			new Tuple<string, string, string[]>(
				"NUnit.Extension.VSProjectLoader",
				"vs-project-loader.dll",
				null
			),
			new Tuple<string, string, string[]>(
				"NUnit.Extension.NUnitProjectLoader",
				"nunit-project-loader.dll",
				null
			),
			new Tuple<string, string, string[]>(
				"NUnit.Extension.NUnitV2ResultWriter",
				"nunit-v2-result-writer.dll",
				null
			),
			new Tuple<string, string, string[]>(
				"NUnit.Extension.NUnitV2Driver",
				"nunit.v2.driver.dll",
				new [] {
					"nunit.core.dll",
					"nunit.core.interfaces.dll",
					"nunit.v2.driver.addins"
				}
			),
			new Tuple<string, string, string[]>(
				"NUnit.Extension.TeamCityEventListener",
				"teamcity-event-listener.dll",
				null
			)
		};
		
		// Install and copy the addin files
		var toolsDir = "tools";
		var nugetInstallSettings = new NuGetInstallSettings { OutputDirectory = toolsDir, ExcludeVersion = true };
		var addinsDir = System.IO.Path.Combine(currentImageDir, "addins");
		EnsureDirectoryExists(addinsDir);
		foreach (var addin in addins) {
			// Install the extension
			NuGetInstall(addin.Item1, nugetInstallSettings);
			var addinToolsPath = System.IO.Path.Combine(toolsDir, addin.Item1, "tools");
			// Copy primary dll
			var primaryDllPath = System.IO.Path.Combine(addinToolsPath, addin.Item2);
			CopyFileToDirectory(primaryDllPath, addinsDir);
			// Copy additional files
			if (addin.Item3 != null) {
				foreach (var additionalItem in addin.Item3) {
					var additionalItemPath = System.IO.Path.Combine(addinToolsPath, additionalItem);
					CopyFileToDirectory(additionalItemPath, addinsDir);
				}
			}
			// Write the primary dll to the addins file
			FileAppendLines(System.IO.Path.Combine(currentImageDir, "nunit.engine.addins"), new[] {
				System.IO.Path.Combine("addins", addin.Item2)
			});
		}	
				
		// Copy the nuspec file
		CopyFileToDirectory("choco/nunit-console.nuspec", currentImageDir);
		
		// Set the working directory
		Context.Environment.WorkingDirectory = currentImageDir;
		
		ChocolateyPack("nunit-console.nuspec", 
			new ChocolateyPackSettings()
			{
				Version = packageVersion,
				//WorkingDirectory = currentImageDir,
				OutputDirectory = PACKAGE_DIR
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
            var nuget = "nunit.engine.netstandard." + packageVersion + ".nupkg";
            var src   = "src/NUnitEngine/nunit.engine.netstandard/bin/" + configuration + "/" + nuget;
            var dest  = PACKAGE_DIR + nuget;

            CreateDirectory(PACKAGE_DIR);
            CopyFile(src, dest);
        }
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

void RunGitCommand(string arguments)
{
    StartProcess("git", new ProcessSettings()
    {
        Arguments = arguments
    });
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
// HELPER METHODS - BUILD
//////////////////////////////////////////////////////////////////////

void BuildProject(string projectPath, string configuration)
{
    if(IsRunningOnWindows())
    {
        // Use MSBuild
        MSBuild(projectPath, new MSBuildSettings()
            .SetConfiguration(configuration)
            .SetMSBuildPlatform(MSBuildPlatform.Automatic)
            .SetVerbosity(Verbosity.Minimal)
            .SetNodeReuse(false)
			.SetPlatformTarget(PlatformTarget.MSIL)
        );
    }
    else
    {
        Information(string.Format("Building {0}...", projectPath));
        // Use XBuild
        XBuild(projectPath, new XBuildSettings()
            .WithTarget("Build")
            .WithProperty("Configuration", configuration)
            .SetVerbosity(Verbosity.Minimal)
        );
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
// TASK TARGETS
//////////////////////////////////////////////////////////////////////

Task("Build")
    .Description("Builds the engine and console runner")
    .IsDependentOn("BuildEngine")
    .IsDependentOn("BuildConsole")
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
	.IsDependentOn("PackageChocolatey");

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
