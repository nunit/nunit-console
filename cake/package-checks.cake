//////////////////////////////////////////////////////////////////////
// LISTS OF FILES USED IN CHECKING PACKAGES
//////////////////////////////////////////////////////////////////////

static readonly string[] ENGINE_FILES = {
        "nunit.engine.dll", "nunit.engine.core.dll", "nunit.engine.api.dll", "testcentric.engine.metadata.dll" };
static readonly string[] ENGINE_PDB_FILES = {
        "nunit.engine.pdb", "nunit.engine.core.pdb", "nunit.engine.api.pdb"};
static readonly string[] ENGINE_CORE_FILES = {
        "nunit.engine.core.dll", "nunit.engine.api.dll", "testcentric.engine.metadata.dll", "Microsoft.Extensions.DependencyModel.dll" };
static readonly string[] ENGINE_CORE_PDB_FILES = {
        "nunit.engine.core.pdb", "nunit.engine.api.pdb"};
static readonly string[] AGENT_FILES = {
        "nunit-agent.exe", "nunit-agent.exe.config",
        "nunit-agent-x86.exe", "nunit-agent-x86.exe.config",
        "nunit.engine.core.dll", "nunit.engine.api.dll", "testcentric.engine.metadata.dll"};
static readonly string[] AGENT_FILES_NETCORE = {
        "nunit-agent.dll", "nunit-agent.dll.config", "Microsoft.Extensions.DependencyModel.dll",
        "nunit.engine.core.dll", "nunit.engine.api.dll", "testcentric.engine.metadata.dll"};
static readonly string[] AGENT_PDB_FILES = {
        "nunit-agent.pdb", "nunit-agent-x86.pdb", "nunit.engine.core.pdb", "nunit.engine.api.pdb"};
static readonly string[] AGENT_PDB_FILES_NETCORE = {
        "nunit-agent.pdb", "nunit.engine.core.pdb", "nunit.engine.api.pdb"};
static readonly string[] CONSOLE_FILES = {
        "nunit3-console.exe", "nunit3-console.exe.config" };
static readonly string[] CONSOLE_FILES_NETCORE = {
        "nunit3-netcore-console.exe", "nunit3-netcore-console.dll", "nunit3-netcore-console.dll.config" };

//////////////////////////////////////////////////////////////////////
// PACKAGE CHECK IMPLEMENTATION
//////////////////////////////////////////////////////////////////////

// NOTE: Package checks basically do no more than what the programmer might 
// do in opening the package itself and examining the content.

public bool CheckPackage(string package, params PackageCheck[] checks)
{
    Console.WriteLine("\nPackage Name: " + System.IO.Path.GetFileName(package));

    if (!FileExists(package))
    {
        WriteError("Package was not found!");
        return false;
    }

    if (checks.Length == 0)
    {
        WriteWarning("Package found but no checks were specified.");
        return true;
    }

    bool isMsi = package.EndsWith(".msi"); 
    string tempDir = isMsi
        ? InstallMsiToTempDir(package)
        : UnzipToTempDir(package);

    if (!System.IO.Directory.Exists(tempDir))
    {
        WriteError("Temporary directory was not created!");
        return false;
    }

    try
    {
        bool allPassed = ApplyChecks(tempDir, checks);
        if (allPassed)
            WriteInfo("All checks passed!");

        return allPassed;
    }
    finally
    {
        DeleteDirectory(tempDir, new DeleteDirectorySettings()
        {
            Recursive = true,
            Force = true
        });
    }
}

private string InstallMsiToTempDir(string package)
{
    // Msiexec does not tolerate forward slashes!
    package = package.Replace("/", "\\");
    var tempDir = GetTempDirectoryPath();
    
    WriteInfo("Installing to " + tempDir);
    int rc = StartProcess("msiexec", $"/a {package} TARGETDIR={tempDir} /q");
    if (rc != 0)
        WriteError($"Installer returned {rc.ToString()}");

    return tempDir;
}

private string UnzipToTempDir(string package)
{
    var tempDir = GetTempDirectoryPath();
 
    WriteInfo("Unzipping to " + tempDir);
    Unzip(package, tempDir);

    return tempDir;
}

private string GetTempDirectoryPath()
{
   return System.IO.Path.GetTempPath() + System.IO.Path.GetRandomFileName() + "\\";
}

private bool ApplyChecks(string dir, PackageCheck[] checks)
{
    bool allOK = true;

    foreach (var check in checks)
        allOK &= check.Apply(dir);

    return allOK;
}

public abstract class PackageCheck
{
    public abstract bool Apply(string dir);
}

public class FileCheck : PackageCheck
{
    string[] _paths;

    public FileCheck(string[] paths)
    {
        _paths = paths;
    }

    public override bool Apply(string dir)
    {
        var isOK = true;

        foreach (string path in _paths)
        {
            if (!System.IO.File.Exists(dir + path))
            {
                WriteError($"File {path} was not found.");
                isOK = false;
            }
        }

        return isOK;
    }
}

public class DirectoryCheck : PackageCheck
{
    private string _path;
    private List<string> _files = new List<string>();

    public DirectoryCheck(string path)
    {
        _path = path;
    }

    public DirectoryCheck WithFiles(params string[] files)
    {
        _files.AddRange(files);
        return this;
    }

    public DirectoryCheck AndFiles(params string[] files)
    {
        return WithFiles(files);
    }

    public DirectoryCheck WithFile(string file)
    {
        _files.Add(file);
        return this;
    }

    public DirectoryCheck AndFile(string file)
    {
        return AndFiles(file);
    }

    public override bool Apply(string dir)
    {
        if (!System.IO.Directory.Exists(dir + _path))
        {
            WriteError($"Directory {_path} was not found.");
            return false;
        }

        bool isOK = true;

        if (_files != null)
        {
            foreach (var file in _files)
            {
                if (!System.IO.File.Exists(System.IO.Path.Combine(dir + _path, file)))
                {
                    WriteError($"File {file} was not found in directory {_path}.");
                    isOK = false;
                }
            }
        }

        return isOK;
    }
}

private static FileCheck HasFile(string file) => HasFiles(new [] { file });
private static FileCheck HasFiles(params string[] files) => new FileCheck(files);  

private static DirectoryCheck HasDirectory(string dir) => new DirectoryCheck(dir);

private static void WriteError(string msg)
{
    Console.WriteLine("  ERROR: " + msg);
}

private static void WriteWarning(string msg)
{
    Console.WriteLine("  WARNING: " + msg);
}

private static void WriteInfo(string msg)
{
    Console.WriteLine("  " + msg);
}
