public enum PackageType
{
    NuGet,      // A standard nuget package
    Chocolatey, // Package for use with chocolatey
    Tool        // A dotnet tool package
}

public class PackageDefinition
{
    public ICakeContext _context;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="packageType">A PackageType value specifying one of the four known package types</param>
    /// <param name="id">A string containing the package ID, used as the root of the PackageName</param>
    /// <param name="source">A string representing the source used to create the package, e.g. a nuspec file</param>
    /// <param name="testRunner">A TestRunner instance used to run package tests.</param>
    /// <param name="checks">An array of PackageChecks be made on the content of the package. Optional.</param>
    /// <param name="symbols">An array of PackageChecks to be made on the symbol package, if one is created. Optional. Only supported for nuget packages.</param>
    /// <param name="tests">An array of PackageTests to be run against the package. Optional.</param>
	public PackageDefinition(
        PackageType packageType,
        string id,
        string source,
        string basePath = null, // Defaults to OutputDirectory
        IPackageTestRunner testRunner = null,
        string extraTestArguments = null,
        PackageCheck[] checks = null,
        PackageCheck[] symbols = null,
        IEnumerable<PackageTest> tests = null)
    {
        if (testRunner == null && tests != null)
            throw new System.InvalidOperationException($"Unable to create {packageType} package {id}: TestRunner must be provided if there are package tests.");

        _context = BuildSettings.Context;

        PackageType = packageType;
        PackageId = id;
        PackageVersion = BuildSettings.PackageVersion;
        PackageSource = source;
        BasePath = basePath ?? BuildSettings.OutputDirectory;
        TestRunner = testRunner;
        ExtraTestArguments = extraTestArguments;
        PackageChecks = checks;
        SymbolChecks = symbols;
        PackageTests = tests;

        switch (packageType)
        {
            case PackageType.NuGet:
                PackageInstallDirectory = BuildSettings.NuGetTestDirectory;
                PackageResultDirectory = BuildSettings.NuGetResultDirectory;
                ExtensionInstallDirectory = BuildSettings.NuGetTestDirectory;
                PackageTestDirectory = $"{PackageInstallDirectory}{PackageId}.{PackageVersion}/";
                break;
            case PackageType.Chocolatey:
                PackageInstallDirectory = BuildSettings.ChocolateyTestDirectory;
                PackageResultDirectory = BuildSettings.ChocolateyResultDirectory;
                ExtensionInstallDirectory = BuildSettings.ChocolateyTestDirectory;
                PackageTestDirectory = $"{PackageInstallDirectory}{PackageId}.{PackageVersion}/";
                break;
            case PackageType.Tool:
                PackageInstallDirectory = BuildSettings.PackageTestDirectory;
                PackageResultDirectory = BuildSettings.NuGetResultDirectory;
                ExtensionInstallDirectory = BuildSettings.NuGetTestDirectory;
                PackageTestDirectory = PackageInstallDirectory;
                break;
        }
    }

    public PackageType PackageType { get; }
	public string PackageId { get; }
    public string PackageVersion { get; protected set; }
	public string PackageSource { get; }
    public string BasePath { get; }

    public IPackageTestRunner TestRunner { get; }
    public string ExtraTestArguments { get; }
    public PackageCheck[] PackageChecks { get; }
    public PackageCheck[] SymbolChecks { get; }
    public IEnumerable<PackageTest> PackageTests { get; }

    public bool HasSymbols => SymbolChecks is not null;

    // The file name of this package, including extension
    public string PackageFileName => $"{PackageId}.{PackageVersion}.nupkg";
    // The file name of any symbol package, including extension
    public string SymbolPackageName => SIO.Path.ChangeExtension(PackageFileName, ".snupkg");
    // The directory into which this package is installed
    public string PackageInstallDirectory { get; }
    // The directory used to contain results of package tests for this package
    public string PackageResultDirectory { get; }
    // The directory into which extensions to the test runner are installed
    public string ExtensionInstallDirectory { get; }
    // The directory containing the package executable after installation
    public string PackageTestDirectory { get; }

    public string PackageFilePath => BuildSettings.PackageDirectory + PackageFileName;

    public bool IsSelectedBy(string selectionExpression)
    {
        return IsSelectedByAny(selectionExpression.Split("|", StringSplitOptions.RemoveEmptyEntries));

        bool IsSelectedByAny(string[] terms)
        {
            foreach (var term in terms)
                if (IsSelectedByAll(term.Split("&", StringSplitOptions.RemoveEmptyEntries)))
                    return true;

            return false;
        }

        bool IsSelectedByAll(string[] factors)
        {
            foreach (string factor in factors)
            {
                int index = factor.IndexOf("=");
                if (index <= 0)
                    throw new ArgumentException("Selection expression does not contain =", "where");
                string prop = factor.Substring(0, index).Trim();
                if (factor[++index] == '=') ++index; // == operator
                string val = factor.Substring(index).Trim();

                switch(prop.ToUpper())
                {
                    case "ID":
                        return PackageId.ToLower() == val.ToLower();
                    case "TYPE":
                        return PackageType.ToString().ToLower() == val.ToLower();
                    default:
                        throw new Exception($"Not a valid selection property: {prop}");
                }
            }

            return false;
        }
    }

    public void BuildVerifyAndTest()
    {
        _context.EnsureDirectoryExists(BuildSettings.PackageDirectory);

        Banner.Display($"Building {PackageFileName}");
        BuildPackage();

        Banner.Display($"Installing {PackageFileName}");
        InstallPackage();

        if (PackageChecks != null)
        {
            Banner.Display($"Verifying {PackageFileName}");
            VerifyPackage();
        }

        if (SymbolChecks != null)
        {
            Banner.Display($"Verifying {SymbolPackageName}");
            VerifySymbolPackage();
        }

        if (PackageTests != null)
        {
            Banner.Display($"Testing {PackageFileName}");
            RunPackageTests();
        }
    }

    public void BuildPackage()
    {
        if (PackageType == PackageType.Chocolatey)
        {
            _context.ChocolateyPack(PackageSource,
                new ChocolateyPackSettings()
                {
                    Version = PackageVersion,
                    OutputDirectory = BuildSettings.PackageDirectory,
                    ArgumentCustomization = args => args.Append($"BIN_DIR={BuildSettings.OutputDirectory}")
                });
        }
        else
        {
            var NuGetPackSettings = new NuGetPackSettings()
            {
                Version = PackageVersion,
                BasePath = BasePath,
                OutputDirectory = BuildSettings.PackageDirectory,
                NoPackageAnalysis = true,
                Symbols = HasSymbols,
                Verbosity = BuildSettings.NuGetVerbosity
            };

            if (HasSymbols)
                NuGetPackSettings.SymbolPackageFormat = "snupkg";

            if (string.IsNullOrEmpty(PackageSource))
                _context.NuGetPack(NuGetPackSettings);
            else if (PackageSource.EndsWith(".nuspec"))
                _context.NuGetPack(PackageSource, NuGetPackSettings);
            else if (PackageSource.EndsWith(".csproj"))
                _context.MSBuild(PackageSource,
                    new MSBuildSettings
                    {
                        Target = "pack",
                        Verbosity = BuildSettings.MSBuildVerbosity,
                        Configuration = BuildSettings.Configuration,
                        PlatformTarget = PlatformTarget.MSIL,
                        //AllowPreviewVersion = BuildSettings.MSBuildAllowPreviewVersion
                    }.WithProperty("Version", BuildSettings.PackageVersion));
            else
                throw new ArgumentException(
                    $"Invalid package source specified: {PackageSource}", "source");
        }
    }

    public void InstallPackage()
    {
        if (PackageType == PackageType.Tool)
        {
            var arguments = $"tool install {PackageId} --version {BuildSettings.PackageVersion} " +
                $"--add-source \"{BuildSettings.PackageDirectory}\" --tool-path \"{PackageInstallDirectory}\"";
            Console.WriteLine($"Executing dotnet {arguments}");
            _context.StartProcess("dotnet", arguments);
        }
        else
        {
            var installSettings = new NuGetInstallSettings
            {
                Source = new[] {
                // Package will be found here
                BuildSettings.PackageDirectory,
                // Dependencies may be in any of these
			    "https://www.myget.org/F/nunit/api/v3/index.json",
                "https://api.nuget.org/v3/index.json" },
                Version = PackageVersion,
                OutputDirectory = PackageInstallDirectory,
                //ExcludeVersion = true,
                Prerelease = true,
                Verbosity = BuildSettings.NuGetVerbosity,
                ArgumentCustomization = args => args.Append("-NoHttpCache")
            };

            _context.NuGetInstall(PackageId, installSettings);
        }
    }

    public void VerifyPackage()
    {
        bool allOK = true;

        if (PackageChecks != null)
            foreach (var check in PackageChecks)
                allOK &= check.ApplyTo(PackageTestDirectory);

        if (allOK)
            Console.WriteLine("All checks passed!");
        else 
            throw new Exception("Verification failed!");
    }

    public void RunPackageTests()
    {
        _context.Information($"Package tests will run at level {BuildSettings.PackageTestLevel}");

        var reporter = new ResultReporter(PackageFileName);

        _context.CleanDirectory(PackageResultDirectory);

        // Ensure we start out each package with no extensions installed.
        // If any package test installs an extension, it remains available
        // for subsequent tests of the same package only.
        foreach (DirectoryPath dirPath in _context.GetDirectories(ExtensionInstallDirectory + "*"))
        {
            string dirName = dirPath.Segments.Last();
            if (IsRemovableExtension(dirName))
            {
                _context.DeleteDirectory(dirPath, new DeleteDirectorySettings() { Recursive = true });
                _context.Information("Deleted directory " + dirPath.GetDirectoryName());
            }
        }

        foreach (var packageTest in PackageTests)
        {
            if (packageTest.Level > BuildSettings.PackageTestLevel)
                continue;

            InstallExtensions(packageTest.ExtensionsNeeded);

            string testResultDir = $"{PackageResultDirectory}/{packageTest.Name}/";
            string resultFile = testResultDir + "TestResult.xml";

            Banner.Display(packageTest.Description);

			_context.CreateDirectory(testResultDir);
            string arguments = $"{packageTest.Arguments} {ExtraTestArguments} --work={testResultDir}";
            if (CommandLineOptions.TraceLevel.Value != "Off")
                arguments += $" --trace:{CommandLineOptions.TraceLevel.Value}";
            bool redirectOutput = packageTest.ExpectedOutput != null;

            int rc = TestRunner.RunPackageTest(arguments, redirectOutput);

            var actualResult = packageTest.ExpectedResult != null ? new ActualResult(resultFile) : null;
 
            try
            {
                var report = new PackageTestReport(packageTest, actualResult, TestRunner);
                reporter.AddReport(report);

                Console.WriteLine(report.Errors.Count == 0
                    ? "\nSUCCESS: Test Result matches expected result!"
                    : "\nERROR: Test Result not as expected!");
            }
            catch (Exception ex)
            {
                reporter.AddReport(new PackageTestReport(packageTest, ex));

                Console.WriteLine("\nERROR: No result found!");
            }

            //else
            //{
            //    var report = new PackageTestReport(packageTest, rc, TestRunner);
            //    reporter.AddReport(report);

            //    if (rc != packageTest.ExpectedReturnCode)
            //        Console.WriteLine($"\nERROR: Expected rc = {packageTest.ExpectedReturnCode} but got {rc}!");
            //}
        }

        // Create report as a string
        var sw = new StringWriter();
        bool hadErrors = reporter.ReportResults(sw);
        string reportText = sw.ToString();

        //Display it on the console
        Console.WriteLine(reportText);

        // Save it to the result directory as well
        using (var reportFile = new StreamWriter($"{PackageResultDirectory}/PackageTestSummary.txt"))
            reportFile.Write(reportText);

        if (hadErrors)
            throw new Exception("One or more package tests had errors!");
    }
    
    protected virtual void InstallExtensions(ExtensionSpecifier[] extensionsNeeded)
    {
        foreach (ExtensionSpecifier extension in extensionsNeeded)
            extension.InstallExtension(this);
    }

    private bool IsRemovableExtension(string id)
    {
        // Our target package could be an extension... don't remove it!
        if (id.StartsWith(PackageId))
            return false;

        // Is it an extension?
        if (!IsExtension(id))
            return false;

        // Remove all except agents, which are required to run any tests.
        return !id.Contains("PluggableAgent") && !id.Contains("pluggable-agent");
    }

    private bool IsExtension(string id) => PackageType == PackageType.Chocolatey
        ? id.StartsWith("nunit-extension-")
        : id.StartsWith("NUnit.Extension.");

    public void VerifySymbolPackage()
    {
        if (!SIO.File.Exists(BuildSettings.PackageDirectory + SymbolPackageName))
        {
            _context.Error($"  ERROR: File {SymbolPackageName} was not found.");
            throw new Exception("Verification Failed!");
        }

        string tempDir = SIO.Directory.CreateTempSubdirectory().FullName;
        _context.Unzip(BuildSettings.PackageDirectory + SymbolPackageName, tempDir);

        bool allOK = true;

        if (allOK && SymbolChecks != null)
            foreach (var check in SymbolChecks)
                allOK &= check.ApplyTo(tempDir);

        SIO.Directory.Delete(tempDir, true);

        if (allOK)
            Console.WriteLine("All checks passed!");
        else
            throw new Exception("Verification failed!");
    }
}
