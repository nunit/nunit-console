#load build-scripts/build-parameters.cake
#tool NuGet.CommandLine&version=5.3.1

var target = Argument("target", "Default");
var ErrorDetail = new List<string>();

//////////////////////////////////////////////////////////////////////
// SETUP AND TEARDOWN TASKS
//////////////////////////////////////////////////////////////////////
Setup<BuildParameters>(context =>
{
    var parameters = BuildParameters.Create(context);

    if (BuildSystem.IsRunningOnAppVeyor)
        AppVeyor.UpdateBuildVersion(parameters.PackageVersion);

    // Executed BEFORE the first task.
    Information("Building {0} version {1} of NUnit Console/Engine.", parameters.Configuration, parameters.PackageVersion);

    return parameters;
});

Teardown(context =>
{
    // Executed AFTER the last task.
    CheckForError(ref ErrorDetail);
});

Task("DumpSettings")
    .Does<BuildParameters>(parms =>
   {
       parms.DumpSettings();
   });

//////////////////////////////////////////////////////////////////////
// CLEAN
//////////////////////////////////////////////////////////////////////

Task("Clean")
    .Description("Cleans directories.")
    .Does<BuildParameters>((parms) =>
    {
        CleanDirectory(parms.OutputDirectory);
        CleanDirectory(parms.PackageDirectory);
        CleanDirectory(parms.ImageDirectory);
        CleanDirectory(parms.ZipImageDirectory);
        CleanDirectory(parms.ExtensionsDirectory);
    });

// Not currently used in CI but useful for cleaning your local obj
// directories, particularly after changing the target of a project.
Task("CleanAll")
    .Description("Cleans obj directories in additon to standard clean")
    .IsDependentOn("Clean")
    .Does<BuildParameters>((parms) =>
    {
        foreach (var dir in GetDirectories(parms.ProjectDirectory + "src/**/obj/"))
            DeleteDirectory(dir, new DeleteDirectorySettings() { Recursive = true });
    });

//////////////////////////////////////////////////////////////////////
// INITIALIZE FOR BUILD
//////////////////////////////////////////////////////////////////////

Task("UpdateAssemblyInfo")
    .Description("Sets the assembly versions to the calculated version.")
    .Does<BuildParameters>((parms) =>
    {
        PatchAssemblyInfo("src/NUnitConsole/ConsoleVersion.cs", parms.AssemblyFileVersion, parms.AssemblyVersion);
        PatchAssemblyInfo("src/NUnitEngine/EngineApiVersion.cs", parms.AssemblyFileVersion, assemblyVersion: null);
        PatchAssemblyInfo("src/NUnitEngine/EngineVersion.cs", parms.AssemblyFileVersion, parms.AssemblyVersion);
    });

//////////////////////////////////////////////////////////////////////
// BUILD ENGINE AND CONSOLE
//////////////////////////////////////////////////////////////////////

MSBuildSettings CreateMSBuildSettings(string target, string configuration, string packageVersion)
{
    var settings = new MSBuildSettings()
        .SetConfiguration(configuration)
        .SetVerbosity(Verbosity.Minimal)
        .WithProperty("PackageVersion", packageVersion)
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
    .Does<BuildParameters>((parms) =>
    {
        string configuration = parms.Configuration;
        string version = parms.PackageVersion;
        string binDir = parms.OutputDirectory;

        MSBuild(parms.SolutionFile, CreateMSBuildSettings("Build", configuration, version).WithRestore());

        Information("Publishing .NET Core & Standard projects so that dependencies are present...");

        foreach(var framework in new [] { "netstandard2.0", "netcoreapp3.1" })
            MSBuild(parms.EngineProject, CreateMSBuildSettings("Publish", configuration, version)
               .WithProperty("TargetFramework", framework)
               .WithProperty("PublishDir", binDir + framework));

        foreach (var framework in new [] { "netstandard2.0" })
             MSBuild(parms.EngineApiProject, CreateMSBuildSettings("Publish", configuration, version)
                .WithProperty("TargetFramework", framework)
                .WithProperty("PublishDir", binDir + framework));

        foreach(var framework in new [] { "netcoreapp2.1", "netcoreapp3.1" })
             MSBuild(parms.EngineTestsProject, CreateMSBuildSettings("Publish", configuration, version)
                .WithProperty("TargetFramework", framework)
                .WithProperty("PublishDir", binDir + framework));

        MSBuild(parms.ConsoleProject, CreateMSBuildSettings("Publish", configuration, version)
            .WithProperty("TargetFramework", "netcoreapp3.1")
            .WithProperty("PublishDir", binDir + "netcoreapp3.1"));

         MSBuild(parms.ConsoleTestsProject, CreateMSBuildSettings("Publish", configuration, version)
            .WithProperty("TargetFramework", "netcoreapp3.1")
            .WithProperty("PublishDir", binDir + "netcoreapp3.1"));

    });

//////////////////////////////////////////////////////////////////////
// BUILD C++ TESTS
//////////////////////////////////////////////////////////////////////

Task("BuildCppTestFiles")
    .Description("Builds the C++ mock test assemblies")
    .WithCriteria(IsRunningOnWindows)
    .Does<BuildParameters>((parms) =>
    {
        MSBuild(
            parms.EngineDirectory + "mock-cpp-clr/mock-cpp-clr-x86.vcxproj",
            CreateMSBuildSettings("Build", parms.Configuration, parms.PackageVersion).WithProperty("Platform", "x86"));

        MSBuild(
            parms.EngineDirectory + "mock-cpp-clr/mock-cpp-clr-x64.vcxproj",
            CreateMSBuildSettings("Build", parms.Configuration, parms.PackageVersion).WithProperty("Platform", "x64"));
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
    .Does<BuildParameters>((parms) =>
    {
        RunTest(
            parms.Net20ConsoleRunner,
            parms.OutputDirectory + "net35/",
            ENGINE_TESTS, 
            "net35",
            ref ErrorDetail);
    });

//////////////////////////////////////////////////////////////////////
// TEST .NET 2.0 CONSOLE
//////////////////////////////////////////////////////////////////////

Task("TestNet20Console")
    .Description("Tests the .NET 2.0 console runner")
    .IsDependentOn("Build")
    .OnError(exception => { ErrorDetail.Add(exception.Message); })
    .Does<BuildParameters>((parms) =>
    {
        RunTest(
            parms.Net20ConsoleRunner,
            parms.OutputDirectory + "net35/",
            CONSOLE_TESTS,
            "net35",
            ref ErrorDetail);
    });

//////////////////////////////////////////////////////////////////////
// TEST .NET CORE 3.1 CONSOLE
//////////////////////////////////////////////////////////////////////

Task("TestNetCore31Console")
    .Description("Tests the .NET Core 3.1 console runner")
    .IsDependentOn("Build")
    .OnError(exception => { ErrorDetail.Add(exception.Message); })
    .Does<BuildParameters>((parms) =>
    {
        var runtimes = new[] { "3.1", "5.0" };

        foreach (var runtime in runtimes)
        {
            RunDotnetCoreTests(
                parms.NetCore31ConsoleRunner,
                parms.OutputDirectory + "netcoreapp3.1/",
                CONSOLE_TESTS,
                runtime,
                ref ErrorDetail);
        }
    });

//////////////////////////////////////////////////////////////////////
// TEST NETSTANDARD 2.0 ENGINE
//////////////////////////////////////////////////////////////////////

Task("TestNetStandard20Engine")
    .Description("Tests the .NET Standard Engine")
    .IsDependentOn("Build")
    .OnError(exception => { ErrorDetail.Add(exception.Message); })
    .Does<BuildParameters>((parms) =>
    {
        string binDir = parms.OutputDirectory + "netcoreapp2.1/";
        RunDotnetCoreNUnitLiteTests(
            binDir + ENGINE_TESTS,
            binDir,
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
    .Does<BuildParameters>((parms) =>
    {
        var runtimes = new[] { "3.1", "5.0" };

        foreach (var runtime in runtimes)
        {
            RunDotnetCoreTests(
                parms.NetCore31ConsoleRunner,
                parms.OutputDirectory + "netcoreapp3.1/",
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
    .Does<BuildParameters>((parms) =>
    {
        CleanDirectory(parms.CurrentImageDirectory);
        CopyFiles(RootFiles, parms.CurrentImageDirectory);
        CopyDirectory(parms.OutputDirectory, parms.CurrentImageDirectory + "bin/");
    });

Task("BuildNugetPackages")
    .Description("Creates NuGet packages of the engine/console")
    .IsDependentOn("CreateImage")
    .Does<BuildParameters>((parms) =>
    {
        CreateDirectory(parms.PackageDirectory);

        NuGetPack("nuget/engine/nunit.engine.api.nuspec", new NuGetPackSettings()
        {
            Version = parms.PackageVersion,
            BasePath = parms.CurrentImageDirectory,
            OutputDirectory = parms.PackageDirectory,
            NoPackageAnalysis = true
        });

        NuGetPack("nuget/engine/nunit.engine.nuspec", new NuGetPackSettings()
        {
            Version = parms.PackageVersion,
            BasePath = parms.CurrentImageDirectory,
            OutputDirectory = parms.PackageDirectory,
            NoPackageAnalysis = true
        });

        NuGetPack("nuget/runners/nunit.console-runner.nuspec", new NuGetPackSettings()
        {
            Version = parms.PackageVersion,
            BasePath = parms.CurrentImageDirectory,
            OutputDirectory = parms.PackageDirectory,
            NoPackageAnalysis = true
        });

        NuGetPack("nuget/runners/nunit.console-runner-with-extensions.nuspec", new NuGetPackSettings()
        {
            Version = parms.PackageVersion,
            BasePath = parms.CurrentImageDirectory,
            OutputDirectory = parms.PackageDirectory,
            NoPackageAnalysis = true
        });

        NuGetPack("nuget/runners/nunit.console-runner.netcore.nuspec", new NuGetPackSettings()
        {
            Version = parms.PackageVersion,
            BasePath = parms.CurrentImageDirectory,
            OutputDirectory = parms.PackageDirectory,
            NoPackageAnalysis = true
        });

        NuGetPack("nuget/deprecated/nunit.runners.nuspec", new NuGetPackSettings()
        {
            Version = parms.PackageVersion,
            BasePath = parms.CurrentImageDirectory,
            OutputDirectory = parms.PackageDirectory,
            NoPackageAnalysis = true
        });

        NuGetPack("nuget/deprecated/nunit.engine.netstandard.nuspec", new NuGetPackSettings()
        {
            Version = parms.PackageVersion,
            BasePath = parms.CurrentImageDirectory,
            OutputDirectory = parms.PackageDirectory,
            NoPackageAnalysis = true
        });
    });

Task("TestNugetPackage")
    .Does<BuildParameters>((parms) =>
    {
        new NuGetPackageTester(parms).RunAllTests(1);
    });

Task("BuildChocolateyPackages")
    .Description("Creates chocolatey packages of the console runner")
    .Does<BuildParameters>((parms) =>
    {
        EnsureDirectoryExists(parms.PackageDirectory);

        string chocoDir = parms.ChocoDirectory;
        string currentImageDir = parms.CurrentImageDirectory;
        string net20ImageBinDir = currentImageDir + "bin/net20/";

        ChocolateyPack("choco/nunit-console-runner.nuspec",
            new ChocolateyPackSettings()
            {
                Version = parms.PackageVersion,
                OutputDirectory = parms.PackageDirectory,
                Files = new [] {
                    new ChocolateyNuSpecContent { Source = currentImageDir + "LICENSE.txt", Target = "tools" },
                    new ChocolateyNuSpecContent { Source = currentImageDir + "NOTICES.txt", Target = "tools" },
                    new ChocolateyNuSpecContent { Source = chocoDir + "VERIFICATION.txt", Target = "tools" },
                    new ChocolateyNuSpecContent { Source = chocoDir + "nunit.choco.addins", Target = "tools" },
                    new ChocolateyNuSpecContent { Source = chocoDir + "nunit.agent.addins", Target = "tools/agents/net20" },
                    new ChocolateyNuSpecContent { Source = chocoDir + "nunit.agent.addins", Target = "tools/agents/net40" },
                    new ChocolateyNuSpecContent { Source = currentImageDir + "bin/agents/net20/nunit-agent.exe", Target="tools/agents/net20" },
                    new ChocolateyNuSpecContent { Source = currentImageDir + "bin/agents/net20/nunit-agent.exe.config", Target="tools/agents/net20" },
                    new ChocolateyNuSpecContent { Source = chocoDir + "nunit-agent.exe.ignore", Target="tools/agents/net20" },
                    new ChocolateyNuSpecContent { Source = currentImageDir + "bin/agents/net20/nunit-agent-x86.exe", Target="tools/agents/net20" },
                    new ChocolateyNuSpecContent { Source = currentImageDir + "bin/agents/net20/nunit-agent-x86.exe.config", Target="tools/agents/net20" },
                    new ChocolateyNuSpecContent { Source = chocoDir + "nunit-agent-x86.exe.ignore", Target="tools/agents/net20" },
                    new ChocolateyNuSpecContent { Source = currentImageDir + "bin/agents/net20/nunit.engine.api.dll", Target="tools/agents/net20" },
                    new ChocolateyNuSpecContent { Source = currentImageDir + "bin/agents/net20/nunit.engine.api.xml", Target="tools/agents/net20" },
                    new ChocolateyNuSpecContent { Source = currentImageDir + "bin/agents/net20/nunit.engine.core.dll", Target="tools/agents/net20" },
                    new ChocolateyNuSpecContent { Source = currentImageDir + "bin/agents/net20/testcentric.engine.metadata.dll", Target="tools/agents/net20" },
                    new ChocolateyNuSpecContent { Source = currentImageDir + "bin/agents/net40/nunit-agent.exe", Target="tools/agents/net40" },
                    new ChocolateyNuSpecContent { Source = currentImageDir + "bin/agents/net40/nunit-agent.exe.config", Target="tools/agents/net40" },
                    new ChocolateyNuSpecContent { Source = chocoDir + "nunit-agent.exe.ignore", Target="tools/agents/net40" },
                    new ChocolateyNuSpecContent { Source = currentImageDir + "bin/agents/net40/nunit-agent-x86.exe", Target="tools/agents/net40" },
                    new ChocolateyNuSpecContent { Source = currentImageDir + "bin/agents/net40/nunit-agent-x86.exe.config", Target="tools/agents/net40" },
                    new ChocolateyNuSpecContent { Source = chocoDir + "nunit-agent-x86.exe.ignore", Target="tools/agents/net40" },
                    new ChocolateyNuSpecContent { Source = currentImageDir + "bin/agents/net40/nunit.engine.api.dll", Target="tools/agents/net40" },
                    new ChocolateyNuSpecContent { Source = currentImageDir + "bin/agents/net40/nunit.engine.api.xml", Target="tools/agents/net40" },
                    new ChocolateyNuSpecContent { Source = currentImageDir + "bin/agents/net40/nunit.engine.core.dll", Target="tools/agents/net40" },
                    new ChocolateyNuSpecContent { Source = currentImageDir + "bin/agents/net40/testcentric.engine.metadata.dll", Target="tools/agents/net40" },
                    new ChocolateyNuSpecContent { Source = net20ImageBinDir + "nunit3-console.exe", Target="tools" },
                    new ChocolateyNuSpecContent { Source = net20ImageBinDir + "nunit3-console.exe.config", Target="tools" },
                    new ChocolateyNuSpecContent { Source = net20ImageBinDir + "nunit.engine.api.dll", Target="tools" },
                    new ChocolateyNuSpecContent { Source = net20ImageBinDir + "nunit.engine.api.xml", Target="tools" },
                    new ChocolateyNuSpecContent { Source = net20ImageBinDir + "nunit.engine.core.dll", Target="tools" },
                    new ChocolateyNuSpecContent { Source = net20ImageBinDir + "nunit.engine.dll", Target="tools" },
                    new ChocolateyNuSpecContent { Source = net20ImageBinDir + "testcentric.engine.metadata.dll", Target="tools" }
                }
            });

        ChocolateyPack("choco/nunit-console-with-extensions.nuspec",
            new ChocolateyPackSettings()
            {
                Version = parms.PackageVersion,
                OutputDirectory = parms.PackageDirectory,
                Files = new [] {
                    new ChocolateyNuSpecContent { Source = currentImageDir + "LICENSE.txt", Target = "tools" },
                    new ChocolateyNuSpecContent { Source = currentImageDir + "NOTICES.txt", Target = "tools" },
                    new ChocolateyNuSpecContent { Source = chocoDir + "VERIFICATION.txt", Target = "tools" }
                }
            });
    });

Task("TestChocolateyPackage")
    .Does<BuildParameters>((parms) =>
    {
        new ChocolateyPackageTester(parms).RunAllTests(1);
    });

//////////////////////////////////////////////////////////////////////
// PACKAGE COMBINED DISTRIBUTIONS
//////////////////////////////////////////////////////////////////////

Task("FetchExtensions")
.Does<BuildParameters>((parms) =>
{
    CleanDirectory(parms.ExtensionsDirectory);

    foreach(var package in EXTENSION_PACKAGES)
    {
        NuGetInstall(package, new NuGetInstallSettings {
                        OutputDirectory = parms.ExtensionsDirectory,
                        Source = new [] { "https://www.nuget.org/api/v2" }
                    });
    }
});

Task("CreateCombinedImage")
.IsDependentOn("CreateImage")
.IsDependentOn("FetchExtensions")
.Does<BuildParameters>((parms) =>
{
    foreach(var framework in NETFX_FRAMEWORKS)
    {
        var addinsImgDir = parms.CurrentImageDirectory + "bin/" + framework +"/addins/";

        CopyDirectory(parms.MsiDirectory + "resources/", parms.CurrentImageDirectory);
        CleanDirectory(addinsImgDir);

        foreach(var packageDir in GetAllDirectories(parms.ExtensionsDirectory))
            CopyPackageContents(packageDir, addinsImgDir);
    }
});

Task("BuildMsiPackage")
.IsDependentOn("CreateCombinedImage")
.Does<BuildParameters>((parms) =>
{
    MSBuild(parms.MsiDirectory + "nunit/nunit.wixproj", new MSBuildSettings()
        .WithTarget("Rebuild")
        .SetConfiguration(parms.Configuration)
        .WithProperty("Version", parms.MsiVersion)
        .WithProperty("DisplayVersion", parms.MsiVersion)
        .WithProperty("OutDir", parms.PackageDirectory)
        .WithProperty("Image", parms.CurrentImageDirectory)
        .SetMSBuildPlatform(MSBuildPlatform.x86)
        .SetNodeReuse(false)
        );
});

Task("TestMsiPackage")
    .Does<BuildParameters>((parms) =>
    {
        new MsiPackageTester(parms).RunAllTests(1);
    });

Task("BuildZipPackage")
.IsDependentOn("CreateCombinedImage")
.Does<BuildParameters>((parms) =>
{
    CleanDirectory(parms.ZipImageDirectory);
    CopyDirectory(parms.CurrentImageDirectory, parms.ZipImageDirectory);

    foreach(var framework in NETFX_FRAMEWORKS)
    {
        //Ensure single and correct addins file (.NET Framework only)
        var netfxZipImg = parms.ZipImageDirectory + "bin/" + framework + "/";
        DeleteFiles(parms.ZipImageDirectory + "*.addins");
        DeleteFiles(netfxZipImg + "*.addins");
        CopyFile(parms.CurrentImageDirectory + "nunit.bundle.addins", netfxZipImg + "nunit.bundle.addins");
    }

    var zipPath = string.Format("{0}NUnit.Console-{1}.zip", parms.PackageDirectory, parms.PackageVersion);
    Zip(parms.ZipImageDirectory, zipPath);
});

Task("TestZipPackage")
    .Does<BuildParameters>((parms) =>
    {
        new ZipPackageTester(parms).RunAllTests(1);
    });

Task("InstallSigningTool")
    .Does(() =>
    {
        var result = StartProcess("dotnet.exe", new ProcessSettings {  Arguments = "tool install SignClient --global" });
    });

Task("SignPackages")
    .IsDependentOn("InstallSigningTool")
    .IsDependentOn("Package")
    .Does<BuildParameters>((parms) =>
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
        var files = GetFiles(string.Concat(parms.PackageDirectory, "*.nupkg")) +
            GetFiles(string.Concat(parms.PackageDirectory, "*.msi"));

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

Task("CheckPackageContent")
    .Description("Checks package content and runs package tests")
    .Does<BuildParameters>((parameters) =>
    {
        new PackageChecker(parameters).CheckAllPackages();
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
    var selectedFramework = INSTALLED_NET_CORE_RUNTIMES.Where(v => v.StartsWith(fxVersion)).OrderByDescending(Version.Parse).FirstOrDefault();

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
    .IsDependentOn("TestNetStandard20Engine")
    .IsDependentOn("TestNetCore31Engine");

Task("Test")
    .Description("Builds and tests the engine")
    .IsDependentOn("TestEngine")
    .IsDependentOn("TestConsole");

Task("BuildPackages")
    .Description("Builds all packages for distribution")
    .IsDependentOn("CheckForError")
    .IsDependentOn("BuildNugetPackages")
    .IsDependentOn("BuildChocolateyPackages")
    .IsDependentOn("BuildMsiPackage")
    .IsDependentOn("BuildZipPackage");

Task("TestPackages")
    .Description("Tests the packages")
    .IsDependentOn("CheckPackageContent")
    .IsDependentOn("TestNugetPackage")
    .IsDependentOn("TestChocolateyPackage")
    .IsDependentOn("TestMsiPackage")
    .IsDependentOn("TestZipPackage");

Task("Package")
    .Description("Builds and tests all packages")
    .IsDependentOn("BuildPackages")
    .IsDependentOn("TestPackages");

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
