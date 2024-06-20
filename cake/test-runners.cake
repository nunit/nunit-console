/// <summary>
/// The TestRunner class is the abstract base for all TestRunners used to run unit-
/// or package-tests. A TestRunner knows how to run a test assembly and provide a result.
/// </summary>
public abstract class TestRunner
{
	public virtual bool RequiresInstallation => false;

	protected FilePath ExecutablePath { get; set; }

	// Base install does nothing
	public virtual void Install() { } 
}

public abstract class UnitTestRunner : TestRunner
{
    // Unit tests are run by providing the path to a test assembly.
	public virtual int Run(FilePath testAssembly)
	{
		if (ExecutablePath == null)
			throw new InvalidOperationException("Unable to run tests. Executable path has not been set.");

	    var processSettings = new ProcessSettings() {
            WorkingDirectory = BuildSettings.OutputDirectory,
            // HACK: Equality indicates we are running under NUnitLite
            Arguments = ExecutablePath == testAssembly
                ? BuildSettings.UnitTestArguments
                : $"{testAssembly} {BuildSettings.UnitTestArguments}" };

		if (ExecutablePath.GetExtension() == ".dll")
			return BuildSettings.Context.StartProcess("dotnet", processSettings);
        else
			return BuildSettings.Context.StartProcess(ExecutablePath,processSettings);
	}
}

public abstract class PackageTestRunner : TestRunner
{
    // Package Tests are run by providing the arguments, which
    // will include one or more test assemblies.
	public virtual int Run(string arguments=null)
	{
		if (ExecutablePath == null)
			throw new InvalidOperationException("Unable to run tests. Executable path has not been set.");

 	    var processSettings = new ProcessSettings() { WorkingDirectory = BuildSettings.OutputDirectory };

       if (ExecutablePath.GetExtension() == ".dll")
		{
			processSettings.Arguments = $"{ExecutablePath} {arguments}";
			return BuildSettings.Context.StartProcess("dotnet", processSettings);
		}
		else
		{
			processSettings.Arguments = arguments;
			return BuildSettings.Context.StartProcess(ExecutablePath, processSettings);
		}
	}
}

/// <summary>
/// The InstallableTestRunner class is the abstract base for TestRunners which
/// must be installed using a published package before they can be used.
/// </summary>
public abstract class InstallableTestRunner : TestRunner
{
	public override bool RequiresInstallation => true;

	public InstallableTestRunner(string packageId, string version)
	{
		if (packageId == null)
			throw new ArgumentNullException(nameof(packageId));
		if (version == null)
			throw new ArgumentNullException(nameof(version));

		PackageId = packageId;
		Version = version;
	}

	public string PackageId { get; }
	public string Version { get; }

	public abstract string InstallPath { get; }
}

public class NUnitLiteRunner : UnitTestRunner
{
	public override int Run(FilePath testPath)
	{
        ExecutablePath = testPath;
        return base.Run(testPath);
	}
}

/// <summary>
/// Class that knows how to run an agent directly.
/// </summary>
public class AgentRunner : PackageTestRunner
{
    private string _stdExecutable;
    private string _x86Executable;

	public AgentRunner(string stdExecutable, string x86Executable = null)
	{
        _stdExecutable = stdExecutable;
        _x86Executable = x86Executable;
    }

    public override int Run(string arguments)
    {
		ExecutablePath = arguments.Contains("--x86")
            ? _x86Executable
            : _stdExecutable;

        return base.Run(arguments.Replace("--x86", string.Empty));
	}
}
