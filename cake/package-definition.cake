public enum PackageType
{
    NuGet,
    Chocolatey,
    Zip
}

public abstract class PackageDefinition
{
    protected ICakeContext _context;

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
	protected PackageDefinition(
        PackageType packageType,
        string id,
        string source,
        PackageTestRunner testRunner = null,
        string extraTestArguments = null,
        PackageCheck[] checks = null,
        PackageCheck[] symbols = null,
        IEnumerable<PackageTest> tests = null)
    {
        if (testRunner == null && tests != null)
            throw new System.ArgumentException($"Unable to create {packageType} package {id}: TestRunner must be provided if there are tests", nameof(testRunner));

        _context = BuildSettings.Context;

        PackageType = packageType;
        PackageId = id;
        PackageVersion = BuildSettings.PackageVersion;
        PackageSource = source;
        BasePath = BuildSettings.OutputDirectory;
        TestRunner = testRunner;
        ExtraTestArguments = extraTestArguments;
        PackageChecks = checks;
        SymbolChecks = symbols;
        PackageTests = tests;
    }

    public PackageType PackageType { get; }
	public string PackageId { get; }
    public string PackageVersion { get; protected set; }
	public string PackageSource { get; }
    public string BasePath { get; }
    public PackageTestRunner TestRunner { get; }
    public string ExtraTestArguments { get; }
    public PackageCheck[] PackageChecks { get; }
    public PackageCheck[] SymbolChecks { get; protected set; }
    public IEnumerable<PackageTest> PackageTests { get; }

    public bool HasSymbols { get; protected set; } = false;
    public virtual string SymbolPackageName => throw new System.NotImplementedException($"Symbols are not available for {PackageType} packages.");

    // The file name of this package, including extension
    public abstract string PackageFileName { get; }
    // The directory into which this package is installed
    public abstract string PackageInstallDirectory { get; }
    // The directory used to contain results of package tests for this package
    public abstract string PackageResultDirectory { get; }
    // The directory into which extensions to the test runner are installed
    public abstract string ExtensionInstallDirectory { get; }
    
    public string PackageFilePath => BuildSettings.PackageDirectory + PackageFileName;
    public string PackageTestDirectory => $"{PackageInstallDirectory}{PackageId}.{PackageVersion}/";

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
                string val = factor.Substring(index+1).Trim();

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
            // TODO: Override this in NuGetPackage
            VerifySymbolPackage();
        }

        if (PackageTests != null)
        {
            Banner.Display($"Testing {PackageFileName}");
            RunPackageTests();
        }
    }

    protected void FetchBundledExtensions(PackageReference[] extensions)
    {
        foreach (var extension in extensions)
            if (!extension.IsInstalled(BuildSettings.ExtensionsDirectory))
                extension.Install(BuildSettings.ExtensionsDirectory);
    }

    public abstract void BuildPackage();

    // Base implementation is used for installing both NuGet and
    // Chocolatey packages. Other package types should override.
    public virtual void InstallPackage()
    {
	    var installSettings = new NuGetInstallSettings
	    {
		    Source = new [] {
                // Package will be found here
                BuildSettings.PackageDirectory,
                // Dependencies may be in any of these
			    "https://www.myget.org/F/nunit/api/v3/index.json",
			    "https://api.nuget.org/v3/index.json" },
            Version = PackageVersion,
            OutputDirectory = PackageInstallDirectory,
            //ExcludeVersion = true,
		    Prerelease = true,
		    NoCache = true
	    };

        _context.NuGetInstall(PackageId, installSettings);
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
		//foreach (DirectoryPath dirPath in _context.GetDirectories(ExtensionInstallDirectory + "*"))
        //{
		//    _context.DeleteDirectory(dirPath, new DeleteDirectorySettings() { Recursive = true });
		//    _context.Information("Deleted directory " + dirPath.GetDirectoryName());
        //}

        //if (TestRunner.RequiresInstallation)
        //    TestRunner.Install();

        foreach (var packageTest in PackageTests)
        {
            if (packageTest.Level > BuildSettings.PackageTestLevel)
                continue;

            foreach (ExtensionSpecifier extension in packageTest.ExtensionsNeeded)
                extension.InstallExtension(this);

            var testResultDir = $"{PackageResultDirectory}/{packageTest.Name}/";
            var resultFile = testResultDir + "TestResult.xml";

            Banner.Display(packageTest.Description);

			_context.CreateDirectory(testResultDir);
            string arguments = $"{packageTest.Arguments} {ExtraTestArguments} --work={testResultDir}";
            if (CommandLineOptions.TraceLevel.Value != "Off")
                arguments += $" --trace:{CommandLineOptions.TraceLevel.Value}";

            int rc = TestRunner.Run(arguments);

            try
            {
                var result = new ActualResult(resultFile);
                var report = new PackageTestReport(packageTest, result);
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

    public virtual void VerifySymbolPackage() { } // Does nothing. Overridden for NuGet packages.
}
