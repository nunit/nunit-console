public class ZipPackage : PackageDefinition
{
    public ZipPackage(
        string id, 
        string source, 
        PackageTestRunner testRunner = null,
        PackageCheck[] checks = null, 
        IEnumerable<PackageTest> tests = null,
        PackageReference[] bundledExtensions = null )
    : base(
        PackageType.Zip, 
        id, 
        source, 
        testRunner: testRunner, 
        checks: checks, 
        tests: tests) 
    {
        BundledExtensions = bundledExtensions;
    }

    // ZIP package supports bundling of extensions
    public PackageReference[] BundledExtensions { get; }

    // The file name of this package, including extension
    public override string PackageFileName => $"{PackageId}-{PackageVersion}.zip";
    // The directory into which this package is installed
    public override string PackageInstallDirectory => BuildSettings.ZipTestDirectory;
    // The directory used to contain results of package tests for this package
    public override string PackageResultDirectory => $"{BuildSettings.ZipResultDirectory}{PackageId}/";
    // The directory into which extensions to the test runner are installed
    public override string ExtensionInstallDirectory => $"{BuildSettings.ZipTestDirectory}{PackageId}.{PackageVersion}/bin/addins/";
    
    public override void BuildPackage()
    {
        FetchBundledExtensions(BundledExtensions);

        CreateZipImage();

        _context.Zip(BuildSettings.ZipImageDirectory, $"{BuildSettings.PackageDirectory}{PackageFileName}");
    }

    public override void InstallPackage()
    {
        _context.Unzip($"{BuildSettings.PackageDirectory}{PackageFileName}", $"{PackageInstallDirectory}{PackageId}.{PackageVersion}");
    }

    private void CreateZipImage()
    {
        _context.CleanDirectory(BuildSettings.ZipImageDirectory);

        _context.CopyFiles(
            new FilePath[] { "LICENSE.txt", "NOTICES.txt", "CHANGES.txt", "nunit.ico" },
            BuildSettings.ZipImageDirectory);

        _context.CopyDirectory(
            BuildSettings.OutputDirectory,
            BuildSettings.ZipImageDirectory + "bin/" );

        foreach (var runtime in new[] { "net20", "net35" })
        {
            var runtimeDir = BuildSettings.ZipImageDirectory + $"bin/{runtime}/";

            _context.CopyFileToDirectory(
                BuildSettings.ZipDirectory + "nunit.bundle.addins",
                BuildSettings.ZipImageDirectory);

            var addinsDir = runtimeDir + "addins/";
            _context.CleanDirectory(addinsDir);

            foreach (var packageDir in System.IO.Directory.GetDirectories(BuildSettings.ExtensionsDirectory))
            {
                var files = _context.GetFiles(packageDir + "/tools/*").Concat(_context.GetFiles(packageDir + "/tools/net20/*"));
                _context.CopyFiles(files.Where(f => f.GetExtension() != ".addins"), addinsDir);
            }
        }
    }
}
