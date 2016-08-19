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

var version = "3.5.0";
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
var ADDIN_TESTS = "addins/tests/addin-tests.dll";
var V2_DRIVER_TESTS = "addins/v2-tests/nunit.v2.driver.tests.dll";
var CONSOLE_TESTS = "nunit3-console.tests.dll";

// Packages
var SRC_PACKAGE = PACKAGE_DIR + "NUnit-" + version + modifier + "-src.zip";
var ZIP_PACKAGE = PACKAGE_DIR + "NUnit-" + packageVersion + ".zip";

//////////////////////////////////////////////////////////////////////
// CLEAN
//////////////////////////////////////////////////////////////////////

Task("Clean")
    .Does(() =>
    {
        CleanDirectory(BIN_DIR);
    });


//////////////////////////////////////////////////////////////////////
// INITIALIZE FOR BUILD
//////////////////////////////////////////////////////////////////////

Task("InitializeBuild")
    .Does(() =>
    {
		NuGetRestore(SOLUTION_FILE, new NuGetRestoreSettings()
		{
			Source = PACKAGE_SOURCE
		});

		if (BuildSystem.IsRunningOnAppVeyor)
		{
			var tag = AppVeyor.Environment.Repository.Tag;

			if (tag.IsTag)
			{
				packageVersion = tag.Name;
			}
			else
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
			}

			AppVeyor.UpdateBuildVersion(packageVersion);
		}
	});

//////////////////////////////////////////////////////////////////////
// BUILD ENGINE
//////////////////////////////////////////////////////////////////////

Task("BuildEngine")
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

        // Addins
        BuildProject("./src/NUnitEngine/Addins/nunit-project-loader/nunit-project-loader.csproj", configuration);
        BuildProject("./src/NUnitEngine/Addins/vs-project-loader/vs-project-loader.csproj", configuration);
        BuildProject("./src/NUnitEngine/Addins/nunit-v2-result-writer/nunit-v2-result-writer.csproj", configuration);
        BuildProject("./src/NUnitEngine/Addins/nunit.v2.driver/nunit.v2.driver.csproj", configuration);

        // Addin tests
        BuildProject("./src/NUnitEngine/Addins/addin-tests/addin-tests.csproj", configuration);
        BuildProject("./src/NUnitEngine/Addins/nunit.v2.driver.tests/nunit.v2.driver.tests.csproj", configuration);
    });

//////////////////////////////////////////////////////////////////////
// BUILD CONSOLE
//////////////////////////////////////////////////////////////////////

Task("BuildConsole")
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
    .Does(() => CheckForError(ref ErrorDetail));

//////////////////////////////////////////////////////////////////////
// TEST ENGINE
//////////////////////////////////////////////////////////////////////

Task("TestEngine")
    .IsDependentOn("Build")
    .OnError(exception => { ErrorDetail.Add(exception.Message); })
    .Does(() =>
    {
        RunTest(NUNIT3_CONSOLE, BIN_DIR, ENGINE_TESTS, "TestEngine", ref ErrorDetail);
    });

Task("TestAddins")
    .OnError(exception => { ErrorDetail.Add(exception.Message); })
    .IsDependentOn("Build")
    .Does(() =>
    {
        RunTest(NUNIT3_CONSOLE, BIN_DIR, ADDIN_TESTS,"TestAddins", ref ErrorDetail);
    });

Task("TestV2Driver")
    .IsDependentOn("Build")
    .OnError(exception => { ErrorDetail.Add(exception.Message); })
    .Does(() =>
    {
        RunTest(NUNIT3_CONSOLE, BIN_DIR, V2_DRIVER_TESTS,"TestV2Driver", ref ErrorDetail);
    });

//////////////////////////////////////////////////////////////////////
// TEST CONSOLE
//////////////////////////////////////////////////////////////////////

Task("TestConsole")
    .IsDependentOn("Build")
    .OnError(exception => { ErrorDetail.Add(exception.Message); })
    .Does(() =>
    {
        RunTest(NUNIT3_CONSOLE, BIN_DIR, CONSOLE_TESTS, "TestConsole", ref ErrorDetail);
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
    "Mono.Cecil.dll",
    "nunit-agent-x86.exe",
    "nunit-agent-x86.exe.config",
    "nunit-agent.exe",
    "nunit-agent.exe.config",
    "nunit.engine.addin.xml",
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
    "addins/nunit-project-loader.dll",
    "addins/nunit-v2-result-writer.dll",
    "addins/nunit.core.dll",
    "addins/nunit.core.interfaces.dll",
    "addins/nunit.v2.driver.dll",
    "addins/vs-project-loader.dll",
    "addins/tests/addin-tests.dll",
    "addins/tests/nunit-project-loader.dll",
    "addins/tests/nunit.engine.api.dll",
    "addins/tests/nunit.engine.api.xml",
    "addins/tests/nunit.framework.dll",
    "addins/tests/nunit.framework.xml",
    "addins/tests/vs-project-loader.dll",
    "addins/v2-tests/nunit.framework.dll",
    "addins/v2-tests/nunit.framework.xml",
    "addins/v2-tests/nunit.v2.driver.tests.dll"
};

Task("PackageSource")
  .Does(() =>
    {
        CreateDirectory(PACKAGE_DIR);
        RunGitCommand(string.Format("archive -o {0} HEAD", SRC_PACKAGE));
    });

Task("CreateImage")
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

Task("PackageExtensions")
    .IsDependentOn("CreateImage")
    .Does(() =>
    {
        var currentImageDir = IMAGE_DIR + "NUnit-" + packageVersion + "/";

        CreateDirectory(PACKAGE_DIR);

        NuGetPack("nuget/extensions/nunit-project-loader.nuspec", new NuGetPackSettings()
        {
            Version = packageVersion,
            BasePath = currentImageDir,
            OutputDirectory = PACKAGE_DIR,
            NoPackageAnalysis = true
        });

        NuGetPack("nuget/extensions/vs-project-loader.nuspec", new NuGetPackSettings()
        {
            Version = packageVersion,
            BasePath = currentImageDir,
            OutputDirectory = PACKAGE_DIR,
            NoPackageAnalysis = true
        });

        NuGetPack("nuget/extensions/nunit-v2-result-writer.nuspec", new NuGetPackSettings()
        {
            Version = packageVersion,
            BasePath = currentImageDir,
            OutputDirectory = PACKAGE_DIR,
            NoPackageAnalysis = true
        });

        NuGetPack("nuget/extensions/nunit.v2.driver.nuspec", new NuGetPackSettings()
        {
            Version = packageVersion,
            BasePath = currentImageDir,
            OutputDirectory = PACKAGE_DIR,
            NoPackageAnalysis = true
        });
    });

Task("PackageConsole")
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

Task("PackageZip")
    .IsDependentOn("CreateImage")
    .Does(() =>
    {
        CreateDirectory(PACKAGE_DIR);

        var currentImageDir = IMAGE_DIR + "NUnit-" + packageVersion + "/";

        var zipFiles =
            GetFiles(currentImageDir + "*.*") +
            GetFiles(currentImageDir + "bin/*.*") +
            GetFiles(currentImageDir + "bin/addins/*.*") +
            GetFiles(currentImageDir + "bin/addins/tests/*.*") +
            GetFiles(currentImageDir + "bin/addins/v2-tests/*.*");
        Zip(currentImageDir, File(ZIP_PACKAGE), zipFiles);
    });

//////////////////////////////////////////////////////////////////////
// SETUP AND TEARDOWN TASKS
//////////////////////////////////////////////////////////////////////
Setup(() =>
{
    // Executed BEFORE the first task.
});

Teardown(() =>
{
    // Executed AFTER the last task.
    CheckForError(ref ErrorDetail);
});

//////////////////////////////////////////////////////////////////////
// HELPER METHODS - GENERAL
//////////////////////////////////////////////////////////////////////

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
    BuildProject(projectPath, configuration, MSBuildPlatform.Automatic);
}

void BuildProject(string projectPath, string configuration, MSBuildPlatform buildPlatform)
{
    if(IsRunningOnWindows())
    {
        // Use MSBuild
        MSBuild(projectPath, new MSBuildSettings()
            .SetConfiguration(configuration)
            .SetMSBuildPlatform(buildPlatform)
            .SetVerbosity(Verbosity.Minimal)
            .SetNodeReuse(false)
        );
    }
    else
    {
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
  .IsDependentOn("BuildEngine")
  .IsDependentOn("BuildConsole");

Task("Rebuild")
    .IsDependentOn("Clean")
    .IsDependentOn("Build");

Task("Test")
    .IsDependentOn("TestEngine")
    .IsDependentOn("TestAddins")
    .IsDependentOn("TestV2Driver")
    .IsDependentOn("TestConsole");

Task("Package")
    .IsDependentOn("CheckForError")
    .IsDependentOn("PackageEngine")
    .IsDependentOn("PackageExtensions")
    .IsDependentOn("PackageConsole")
    .IsDependentOn("PackageZip");

Task("Appveyor")
    .IsDependentOn("Build")
    .IsDependentOn("Test")
    .IsDependentOn("Package");

Task("Travis")
    .IsDependentOn("Build")
    .IsDependentOn("Test");

Task("Default")
    .IsDependentOn("Build"); // Rebuild?

//////////////////////////////////////////////////////////////////////
// EXECUTION
//////////////////////////////////////////////////////////////////////

RunTarget(target);
