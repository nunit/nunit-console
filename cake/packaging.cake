public void BuildPackage(PackageDefinition package)
{
    DisplayBanner($"Building package {package.PackageName}");

    switch (package.PackageType)
    {
        case PackageType.NuGet:
            BuildNuGetPackage(package);
            break;

        case PackageType.Chocolatey:
            BuildChocolateyPackage(package);
            break;

        case PackageType.Msi:
            BuildMsiPackage(package);
            break;

        case PackageType.Zip:
            BuildZipPackage(package);
            break;
    }

    void BuildNuGetPackage(PackageDefinition package)
    {
        var nugetPackSettings = new NuGetPackSettings()
        {
            Version = productVersion,
            BasePath = BIN_DIR,
            OutputDirectory = PACKAGE_DIR,
            NoPackageAnalysis = true,
            Symbols = package.HasSymbols
        };

        // Not yet supported by cake as a setting
        if (package.HasSymbols)
            nugetPackSettings.ArgumentCustomization =
                args => args.Append("-SymbolPackageFormat snupkg");

        NuGetPack(package.PackageSource, nugetPackSettings);
    }

    void BuildChocolateyPackage(PackageDefinition package)
    {
        ChocolateyPack(package.PackageSource,
            new ChocolateyPackSettings()
            {
                Version = productVersion,
                OutputDirectory = PACKAGE_DIR,
                ArgumentCustomization = args => args.Append($"BIN_DIR={BIN_DIR}")
            });
    }

    void BuildMsiPackage(PackageDefinition package)
    {
        CreateMsiImage();

        MSBuild(package.PackageSource, new MSBuildSettings()
            .WithTarget("Rebuild")
            .SetConfiguration(configuration)
            .WithProperty("Version", version)
            .WithProperty("DisplayVersion", version)
            .WithProperty("OutDir", PACKAGE_DIR)
            .WithProperty("Image", MSI_IMG_DIR)
            .SetMSBuildPlatform(MSBuildPlatform.x86)
            .SetNodeReuse(false));
    }

    void BuildZipPackage(PackageDefinition package)
    {
        CreateZipImage();

        Zip(ZIP_IMG_DIR, $"{PACKAGE_DIR}{package.PackageName}");
    }
}

public void FetchExtensions()
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
}

public void CreateMsiImage()
{
    CleanDirectory(MSI_IMG_DIR);
    CopyFiles(
        new FilePath[] { "LICENSE.txt", "NOTICES.txt", "CHANGES.txt", "nunit.ico" },
        MSI_IMG_DIR);
    CopyDirectory(BIN_DIR, MSI_IMG_DIR + "bin/");

    foreach (var framework in NETFX_FRAMEWORKS)
    {
        var addinsImgDir = MSI_IMG_DIR + "bin/" + framework + "/addins/";

        CopyDirectory(MSI_DIR + "resources/", MSI_IMG_DIR);
        CleanDirectory(addinsImgDir);

        foreach (var packageDir in System.IO.Directory.GetDirectories(EXTENSIONS_DIR))
            CopyPackageContents(packageDir, addinsImgDir);
    }
}

public void CreateZipImage()
{
    CleanDirectory(ZIP_IMG_DIR);
    CopyFiles(
        new FilePath[] { "LICENSE.txt", "NOTICES.txt", "CHANGES.txt", "nunit.ico" },
        ZIP_IMG_DIR);
    CopyDirectory(BIN_DIR, ZIP_IMG_DIR + "bin/");

    foreach (var framework in NETFX_FRAMEWORKS)
    {
        var frameworkDir = ZIP_IMG_DIR + "bin/" + framework + "/";
        CopyFileToDirectory(ZIP_DIR + "nunit.bundle.addins", frameworkDir);

        var addinsDir = frameworkDir + "addins/";
        CleanDirectory(addinsDir);

        foreach (var packageDir in System.IO.Directory.GetDirectories(EXTENSIONS_DIR))
            CopyPackageContents(packageDir, addinsDir);
    }
}

public static void DisplayBanner(string message)
{
    Console.WriteLine("\r\n=================================================="); ;
    Console.WriteLine(message);
    Console.WriteLine("==================================================");
}
