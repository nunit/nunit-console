//////////////////////////////////////////////////////////////////////
// COMMON TASKS - ADD PROJECT-SPECIFIC TASKS IN BUILD.CAKE
//////////////////////////////////////////////////////////////////////

//////////////////////////////////////////////////////////////////////
// DUMP SETTINGS
//////////////////////////////////////////////////////////////////////

Task("DumpSettings")
	.Does<BuildSettings>((settings) =>
	{
		settings.DumpSettings();
	});

//////////////////////////////////////////////////////////////////////
// CLEANING
//////////////////////////////////////////////////////////////////////

Task("Clean")
    .Description("Cleans directories.")
    .Does((settings) =>
    {
        CleanDirectory(BIN_DIR);
        CleanDirectory(PACKAGE_DIR);
        CleanDirectory(IMAGE_DIR);
        CleanDirectory(EXTENSIONS_DIR);
        CleanDirectory(PACKAGE_DIR);
    });

Task("CleanAll")
    .Description("Cleans both Debug and Release Directories followed by deleting object directories")
    .Does((settings) =>
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

