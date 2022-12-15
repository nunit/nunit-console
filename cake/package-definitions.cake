//////////////////////////////////////////////////////////////////////
// INITIALIZE DEFINITIONS OF PACKAGES
//////////////////////////////////////////////////////////////////////
static PackageDefinition[] AllPackages;
static PackageDefinition ConsolePackage;
static PackageDefinition ConsoleRunnerPackage;
static PackageDefinition NetCoreConsoleRunnerPackage;
static PackageDefinition ChocolateyPackage;
static PackageDefinition MsiPackage;
static PackageDefinition ZipPackage;
static PackageDefinition EnginePackage;
static PackageDefinition EngineApiPackage;

// Called from SetUp
public void InitializePackageDefinitions(ISetupContext context, BuildSettings settings)
{
    AllPackages = new PackageDefinition[] {
        ConsolePackage = new NUnitConsoleNuGetPackage(context, settings.ProductVersion),
        ConsoleRunnerPackage = new NUnitConsoleRunnerNuGetPackage(context, settings.ProductVersion),
        NetCoreConsoleRunnerPackage = new NUnitNetCoreConsoleRunnerPackage(context, settings.ProductVersion),
        ChocolateyPackage = new NUnitConsoleRunnerChocolateyPackage(context, settings.ProductVersion),
        MsiPackage = new NUnitConsoleMsiPackage(context, settings.SemVer),
        ZipPackage = new NUnitConsoleZipPackage(context, settings.ProductVersion),
        EnginePackage = new NUnitEnginePackage(context, settings.ProductVersion),
        EngineApiPackage = new NUnitEngineApiPackage(context, settings.ProductVersion)
    };
}

//////////////////////////////////////////////////////////////////////
// PACKAGE DEFINITION ABSTRACT CLASS
//////////////////////////////////////////////////////////////////////

/// <summary>
/// The abstract base of all packages
/// </summary>
public abstract class PackageDefinition
{
    protected ICakeContext _context;

    /// <summary>
    /// Construct without arguments - derived class must set properties
    /// </summary>
    protected PackageDefinition(ICakeContext context, string packageVersion)
    {
        _context = context;
        PackageVersion = packageVersion;
    }

	public string PackageId { get; protected set; }
    public string PackageVersion { get; protected set; }
	public string PackageSource { get; protected set; }
    public string BasePath { get; protected set; }
    public string TestExecutable { get; protected set; }
    public PackageCheck[] PackageChecks { get; protected set; }
    public PackageCheck[] SymbolChecks { get; protected set; }
    public IEnumerable<PackageTest> PackageTests { get; protected set; }
    public bool HasTests => PackageTests != null;
    public bool HasChecks => PackageChecks != null;
    public bool HasSymbols => SymbolChecks != null;

    public abstract string PackageFileName { get; }
    public abstract string InstallDirectory { get; }
    public abstract string ResultDirectory { get; }

    public string PackageFilePath => PACKAGE_DIR + PackageFileName;

    protected abstract void doBuildPackage();
    protected abstract void doInstallPackage();

    public void BuildPackage()
    {
        DisplayAction("Building");
        doBuildPackage();
    }

    public void InstallPackage()
    {
        Console.WriteLine($"Installing package to {InstallDirectory}");
        _context.CleanDirectory(InstallDirectory);
        doInstallPackage();
    }

    public void RunTests()
    {
        DisplayAction("Testing");

        InstallPackage();

        var reporter = new ResultReporter(PackageFileName);

        _context.CleanDirectory(ResultDirectory);

        foreach (var packageTest in PackageTests)
        {
            var testResultDir = ResultDirectory + packageTest.Name + SEPARATOR;
            var resultFile = testResultDir + "TestResult.xml";

            DisplayBanner(packageTest.Description);

            Console.WriteLine($"Running {InstallDirectory + TestExecutable}");

            int rc = _context.StartProcess(
                InstallDirectory + TestExecutable,
                new ProcessSettings()
                {
                    Arguments = $"{packageTest.Arguments} --work={testResultDir}",
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

    public void DisplayAction(string action)
    {
        DisplayBanner($"{action} package {PackageFileName}");
    }

    public virtual string SymbolPackageName => throw new System.NotImplementedException($"Symbols are not available for {GetType().Name} packages.");
}
