public class MsiPackage : PackageDefinition
{
    public MsiPackage(
        string id, 
        string source, 
        PackageTestRunner testRunner = null,
        PackageCheck[] checks = null, 
        IEnumerable<PackageTest> tests = null,
        PackageReference[] bundledExtensions = null)
    : base(
        PackageType.Msi, 
        id, 
        source, 
        testRunner: testRunner, 
        checks: checks, 
        tests: tests)
        {
            PackageVersion = BuildSettings.BuildVersion.SemVer;
            BundledExtensions = bundledExtensions;
        }

    // MSI and ZIP packages support bundling of extensions
    // if any are specified in the definition.
    public PackageReference[] BundledExtensions { get; }

    // The file name of this package, including extension
    public override string PackageFileName => $"{PackageId}-{PackageVersion}.msi";
    // The directory into which this package is installed
    public override string PackageInstallDirectory => BuildSettings.MsiTestDirectory;
    // The directory used to contain results of package tests for this package
    public override string PackageResultDirectory => BuildSettings.MsiResultDirectory + PackageId + "/";
    // The directory into which extensions to the test runner are installed
    // TODO: FIx this for msi addins
    public override string ExtensionInstallDirectory => BuildSettings.PackageTestDirectory;
  
    public override void BuildPackage()
    {
        FetchBundledExtensions(BundledExtensions);

        CreateMsiImage();

        _context.MSBuild(PackageSource, new MSBuildSettings()
            .WithTarget("Rebuild")
            .SetConfiguration(BuildSettings.Configuration)
            .WithProperty("Version", PackageVersion)
            .WithProperty("DisplayVersion", PackageVersion)
            .WithProperty("OutDir", BuildSettings.PackageDirectory)
            .WithProperty("Image", BuildSettings.MsiImageDirectory)
            .SetMSBuildPlatform(MSBuildPlatform.x86)
            .SetNodeReuse(false));
    }

    private void CreateMsiImage()
    {
        _context.CleanDirectory(BuildSettings.MsiImageDirectory);

        _context.CopyFiles(
            new FilePath[] { "LICENSE.txt", "NOTICES.txt", "CHANGES.txt", "nunit.ico" },
            BuildSettings.MsiImageDirectory);

        _context.CopyDirectory(
            BuildSettings.OutputDirectory,
            BuildSettings.MsiImageDirectory + "bin/" );

        foreach (var runtime in new[] { "net20", "net35" })
        {
            var addinsImgDir = BuildSettings.MsiImageDirectory + $"bin/{runtime}/addins/";

            _context.CopyDirectory(
                BuildSettings.MsiDirectory + "resources/",
                BuildSettings.MsiImageDirectory);

            _context.CleanDirectory(addinsImgDir);

            foreach (var packageDir in System.IO.Directory.GetDirectories(BuildSettings.ExtensionsDirectory))
            {
                var files = _context.GetFiles(packageDir + "/tools/*").Concat(_context.GetFiles(packageDir + "/tools/net20/*"));
                _context.CopyFiles(files.Where(f => f.GetExtension() != ".addins"), addinsImgDir);
            }
        }
    }

    public override void InstallPackage()
    {
        // Msiexec does not tolerate forward slashes!
        string package = PackageFilePath.Replace("/", "\\");
        string testDir = System.IO.Path.Combine(PackageInstallDirectory.Replace("/", "\\"), $"{PackageId}.{PackageVersion}");

        Console.WriteLine($"Installing msi to {testDir}");
        int rc = _context.StartProcess("msiexec", $"/a {package} TARGETDIR={testDir} /q");

        if (rc != 0)
            Console.WriteLine($"  ERROR: Installer returned {rc.ToString()}");
    }
}
