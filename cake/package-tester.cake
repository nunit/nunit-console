/// <summary>
/// PackageTester knows how to run all the tests for a given package
/// </summary>
public class PackageTester
{
    private ICakeContext _context;

    private PackageType _packageType;
    private string _packageName;
    private string _installDirectory;
    private string _resultDirectory;
    private string _packageUnderTest;
    private string _testExecutable;
    private IEnumerable<PackageTest> _packageTests;

    public PackageTester(ICakeContext context, PackageDefinition package)
    {
        _context = context;

        _packageType = package.PackageType;
        _packageName = package.PackageName;
        var subdir = $"{_packageType.ToString().ToLower()}/{package.PackageId}/";
        _installDirectory = PACKAGE_TEST_DIR + subdir;
        _resultDirectory = PACKAGE_RESULT_DIR + subdir;
        _packageUnderTest = PACKAGE_DIR + _packageName;
        _testExecutable = package.TestExecutable;
        _packageTests = package.PackageTests;
    }

    public void RunTests()
    {
        DisplayBanner("Testing package " + _packageName);

        Console.WriteLine("Creating Test Directory...");
        CreatePackageInstallDirectory();

        RunPackageTests();
    }

    public void CreatePackageInstallDirectory()
    {
        _context.CleanDirectory(_installDirectory);

        if (_packageType == PackageType.Msi)
        {
            // Msiexec does not tolerate forward slashes!
            string package = _packageUnderTest.ToString().Replace("/", "\\");
            string testDir = _installDirectory.Replace("/", "\\");
            Console.WriteLine($"Installing msi to {testDir}");
            int rc = _context.StartProcess("msiexec", $"/a {package} TARGETDIR={testDir} /q");
            if (rc != 0)
                Console.WriteLine($"  ERROR: Installer returned {rc.ToString()}");
            else
            {
                var binDir = _installDirectory + "NUnit.org/nunit-console/";
                var dlls = _context.GetFiles(binDir + "*.dll");
                var pdbs = _context.GetFiles(binDir + "*.pdb");
                var filesToCopy = dlls.Concat(pdbs);

                // Administrative install is used to create a file image, from which
                // users may do their own installls. For security reasons, we can't
                // do a full install so we simulate the user portion of the install,
                // copying certain files to their final destination.
                Console.WriteLine("Copying agent files");
                _context.CopyFiles(filesToCopy, binDir + "agents/net20");
                _context.CopyFiles(filesToCopy, binDir + "agents/net40");
            }
        }
        else
        {
            Console.WriteLine($"Unzipping package to {_installDirectory}");
            _context.Unzip(_packageUnderTest, _installDirectory);

            if (_packageType == PackageType.NuGet || _packageType == PackageType.Chocolatey)
            {
                foreach (string packageDir in System.IO.Directory.GetDirectories(EXTENSIONS_DIR))
                {
                    string subdir = _packageType.ToString().ToLower();
                    string packageName = System.IO.Path.GetFileName(packageDir);
                    string targetDir = $"{PACKAGE_TEST_DIR}{subdir}/{packageName}";

                    _context.CopyDirectory(packageDir, targetDir);
            
                    if (_packageType == PackageType.Chocolatey)
                        RenamePackageForChocolatey(targetDir);
                }
            }
        }
    }

    private void RenamePackageForChocolatey(string nugetDir)
    {
        string chocoDir = nugetDir
            .Replace("NUnit.Extension.NUnitProjectLoader", "nunit-extension-nunit-project-loader")
            .Replace("NUnit.Extension.VSProjectLoader", "nunit-extension-vs-project-loader")
            .Replace("NUnit.Extension.NUnitV2ResultWriter", "nunit-extension-v2-result-writer")
            .Replace("NUnit.Extension.NUnitV2Driver", "nunit-extension-nunit-v2-driver")
            .Replace("NUnit.Extension.TeamCityEventListener", "nunit-extension-teamcity-event-listener");

        if (chocoDir != nugetDir)
            _context.MoveDirectory(nugetDir, chocoDir);
    }

    private void RunPackageTests()
    {
        var reporter = new ResultReporter(_packageName);

        _context.CleanDirectory(_resultDirectory);

        foreach (var packageTest in _packageTests)
        {
            var testResultDir = _resultDirectory + packageTest.Name + "/";
            var resultFile = testResultDir + "TestResult.xml";

            DisplayBanner(packageTest.Description);

            Console.WriteLine($"Running {_installDirectory + _testExecutable}");

            var outputDir = System.IO.Path.GetFullPath(
                $"bin/{Configuration}/");
            int rc = _context.StartProcess(
                _installDirectory + _testExecutable,
                new ProcessSettings()
                {
                    Arguments = $"{packageTest.Arguments} --work={testResultDir}",
                    WorkingDirectory = outputDir
                });

            try
            {
                var result = new ActualResult(resultFile);
                var report = new TestReport(packageTest, result);
                reporter.AddReport(report);

                Console.WriteLine(report.Errors.Count == 0
                    ? "\nSUCCESS: Test Result matches expected result!"
                    : "\nERROR: Test Result not as expected!");
            }
            catch (Exception ex)
            {
                reporter.AddReport(new TestReport(packageTest, ex));

                Console.WriteLine("\nERROR: No result found!");
            }
        }

        bool hadErrors = reporter.ReportResults();
        Console.WriteLine();

        if (hadErrors)
            throw new Exception("One or more package tests had errors!");
    }
}
