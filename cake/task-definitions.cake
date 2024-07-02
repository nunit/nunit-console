// This file defines what each of the tasks in the recipe actually does.
// You should not change these definitions unless you intend to change
// the behavior of a task for all projects that use the recipe.
//
// To make a change for a single project, you should add code to your build.cake
// or another project-specific cake file. See extending.cake for examples.

BuildTasks.DefaultTask = Task("Default")
    .Description("Default task if none specified by user")
    .IsDependentOn("Build");

BuildTasks.DumpSettingsTask = Task("DumpSettings")
	.Description("Display BuildSettings properties")
	.Does(() => BuildSettings.DumpSettings());

BuildTasks.CheckHeadersTask = Task("CheckHeaders")
	.Description("Check source files for valid copyright headers")
	.WithCriteria(() => !CommandLineOptions.NoBuild)
	.WithCriteria(() => !BuildSettings.SuppressHeaderCheck)
	.Does(() => Headers.Check());

BuildTasks.CleanTask = Task("Clean")
	.Description("Clean output and package directories")
	.WithCriteria(() => !CommandLineOptions.NoBuild)
	.Does(() => 
	{
		foreach (var binDir in GetDirectories($"**/bin/{BuildSettings.Configuration}/"))
			CleanDirectory(binDir);
        
        CleanDirectory(BuildSettings.PackageDirectory);
        CleanDirectory(BuildSettings.ImageDirectory);
        CleanDirectory(BuildSettings.ExtensionsDirectory);
		
        DeleteFiles(BuildSettings.ProjectDirectory + "*.log");
	});

BuildTasks.CleanAllTask = Task("CleanAll")
	.Description("Clean everything!")
	.Does(() => 
	{
		foreach (var binDir in GetDirectories("**/bin/"))
			CleanDirectory(binDir);

        CleanDirectory(BuildSettings.PackageDirectory);
        CleanDirectory(BuildSettings.ImageDirectory);
        CleanDirectory(BuildSettings.ExtensionsDirectory);

		DeleteFiles(BuildSettings.ProjectDirectory + "*.log");

		foreach (var dir in GetDirectories("src/**/obj/"))
			DeleteDirectory(dir, new DeleteDirectorySettings() { Recursive = true });
	});

BuildTasks.RestoreTask = Task("Restore")
	.Description("Restore referenced packages")
	.WithCriteria(() => BuildSettings.SolutionFile != null)
	.WithCriteria(() => !CommandLineOptions.NoBuild)
	.Does(() =>	{
		NuGetRestore(BuildSettings.SolutionFile, new NuGetRestoreSettings() {
		    Source = new string[]	{ 
                "https://www.nuget.org/api/v2",
                "https://www.myget.org/F/nunit/api/v2" }
        });
	});

BuildTasks.BuildTask = Task("Build")
	.WithCriteria(() => BuildSettings.SolutionFile != null)
	.WithCriteria(() => !CommandLineOptions.NoBuild)
	.IsDependentOn("Clean")
	.IsDependentOn("Restore")
	.IsDependentOn("CheckHeaders")
	.Description("Build the solution")
	.Does(() =>	{
		MSBuild(BuildSettings.SolutionFile, BuildSettings.MSBuildSettings.WithProperty("Version", BuildSettings.PackageVersion));
	});

BuildTasks.UnitTestTask = Task("Test")
	.Description("Run unit tests")
	.IsDependentOn("Build")
	.Does(() => UnitTesting.RunAllTests());

BuildTasks.PackageTask = Task("Package")
	.IsDependentOn("Build")
	.Description("Build, Install, Verify and Test all packages")
	.Does(() => {
        var selector = CommandLineOptions.PackageSelector;
		foreach(var package in BuildSettings.Packages)
            if (!selector.Exists || package.IsSelectedBy(selector.Value))
    			package.BuildVerifyAndTest();
	});

BuildTasks.BuildTestAndPackageTask = Task("BuildTestAndPackage")
	.Description("Do Build, Test and Package all in one run")
	.IsDependentOn("Build")
	.IsDependentOn("Test")
	.IsDependentOn("Package");

BuildTasks.PublishTask = Task("Publish")
	.Description("Publish all packages for current branch")
	.IsDependentOn("Package")
	.Does(() => PackageReleaseManager.Publish());

BuildTasks.PublishToMyGetTask = Task("PublishToMyGet")
    .Description("Publish packages to MyGet")
    .Does(() => PackageReleaseManager.PublishToMyGet() );

BuildTasks.PublishToNuGetTask = Task("PublishToNuGet")
    .Description("Publish packages to NuGet")
    .Does(() => PackageReleaseManager.PublishToNuGet() );

BuildTasks.PublishToChocolateyTask = Task("PublishToChocolatey")
    .Description("Publish packages to Chocolatey")
    .Does(() => PackageReleaseManager.PublishToChocolatey() );

BuildTasks.CreateDraftReleaseTask = Task("CreateDraftRelease")
	.Description("Create a draft release on GitHub")
	.Does(() => PackageReleaseManager.CreateDraftRelease() );

BuildTasks.CreateProductionReleaseTask = Task("CreateProductionRelease")
	.Description("Create a production GitHub Release")
	.Does(() => PackageReleaseManager.CreateProductionRelease() );

BuildTasks.ContinuousIntegrationTask = Task("ContinuousIntegration")
	.Description("Perform continuous integration run")
	.IsDependentOn("Build")
	.IsDependentOn("Test")
	.IsDependentOn("Package")
	.IsDependentOn("Publish")
	.IsDependentOn("CreateDraftRelease")
	.IsDependentOn("CreateProductionRelease");

BuildTasks.AppveyorTask = Task("Appveyor")
	.Description("Target for running on AppVeyor")
	.IsDependentOn("ContinuousIntegration");
