//////////////////////////////////////////////////////////////////////
// PACKAGE DEFINITION IMPLEMENTATION
//////////////////////////////////////////////////////////////////////

/// <summary>
/// The abstract base of all packages
/// </summary>
public abstract class PackageDefinition
{
    protected ICakeContext _context;

    /// <summary>
    /// Construct with arguments
    /// </summary>
    /// <param name="id">A string containing the package ID, used as the root of the PackageFileName</param>
    /// <param name="version">A string representing the package version, used as part of the PackageFileName</param>
    /// <param name="source">A string representing the source used to create the package, e.g. a nuspec file</param>
    /// <param name="executable">A string containing the path to the executable used in running tests. If relative, the path is contained within the package itself.</param>
    /// <param name="checks">An array of PackageChecks be made on the content of the package. Optional.</param>
    /// <param name="symbols">An array of PackageChecks to be made on the symbol package, if one is created. Optional. Only supported for nuget packages.</param>
    /// <param name="tests">An array of PackageTests to be run against the package. Optional.</param>
	protected PackageDefinition(
        ICakeContext context,
        string id,
        string version,
        string source,
        string basepath,
        string executable = null,
        PackageCheck[] checks = null,
        PackageCheck[] symbols = null,
        IEnumerable<PackageTest> tests = null)
    {
        if (executable == null && tests != null)
            throw new System.ArgumentException($"Unable to create package {id}: Executable must be provided if there are tests", nameof(executable));

        _context = context;

        PackageId = id;
        PackageVersion = version;
        PackageSource = source;
        BasePath = basepath;
        TestExecutable = executable;
        PackageChecks = checks;
        PackageTests = tests;
        SymbolChecks = symbols;
    }

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

    public bool HasSymbols { get; protected set; } = false;
    public virtual string SymbolPackageName => throw new System.NotImplementedException($"Symbols are not available for {GetType().Name} packages.");
}
