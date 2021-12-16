// The checks in this script basically do no more than what the programmer might 
// do in opening the package itself and examining the content. In particular, the
// packages are not actually installed, leaving that for manual testing before
// each release. This might be automated at some future point. However, they do
// provide some degree of regression testing, since changes in the package
// organization will result in a failure. 
//
// Note that msi packages are not being checked at this time.

public void CheckAllPackages()
{
    string[] ENGINE_FILES = {
        "nunit.engine.dll", "nunit.engine.core.dll", "nunit.engine.api.dll", "testcentric.engine.metadata.dll" };
    string[] ENGINE_PDB_FILES = {
        "nunit.engine.pdb", "nunit.engine.core.pdb", "nunit.engine.api.pdb"};
    string[] ENGINE_CORE_FILES = {
        "nunit.engine.core.dll", "nunit.engine.api.dll", "testcentric.engine.metadata.dll" };
    string[] ENGINE_CORE_PDB_FILES = {
        "nunit.engine.core.pdb", "nunit.engine.api.pdb"};
    string[] AGENT_FILES = {
        "nunit-agent.exe", "nunit-agent.exe.config",
        "nunit-agent-x86.exe", "nunit-agent-x86.exe.config",
        "nunit.engine.core.dll", "nunit.engine.api.dll", "testcentric.engine.metadata.dll"};
    string[] AGENT_FILES_NETCORE = {
        "nunit-agent.dll", "nunit-agent.dll.config",
        "nunit.engine.core.dll", "nunit.engine.api.dll", "testcentric.engine.metadata.dll"};
    string[] AGENT_PDB_FILES = {
        "nunit-agent.pdb", "nunit-agent-x86.pdb", "nunit.engine.core.pdb", "nunit.engine.api.pdb"};
    string[] AGENT_PDB_FILES_NETCORE = {
        "nunit-agent.pdb", "nunit.engine.core.pdb", "nunit.engine.api.pdb"};
    string[] CONSOLE_FILES = {
        "nunit3-console.exe", "nunit3-console.exe.config" };

    bool isOK =
        CheckNuGetPackage(
            "NUnit.Console",
            HasFile("LICENSE.txt")) &
        CheckNuGetPackage(
            "NUnit.ConsoleRunner",
            HasFiles("LICENSE.txt", "NOTICES.txt"),
            HasDirectory("tools").WithFiles(CONSOLE_FILES).AndFiles(ENGINE_FILES).AndFile("nunit.console.nuget.addins"),
            HasDirectory("tools/agents/net20").WithFiles(AGENT_FILES).AndFile("nunit.agent.addins"),
            HasDirectory("tools/agents/net40").WithFiles(AGENT_FILES).AndFile("nunit.agent.addins"),
            HasDirectory("tools/agents/netcoreapp3.1").WithFiles(AGENT_FILES_NETCORE).AndFile("nunit.agent.addins")) &
        CheckNuGetSourcePackage(
            "NUnit.ConsoleRunner",
            HasDirectory("tools").WithFiles(ENGINE_PDB_FILES).AndFile("nunit3-console.pdb"),
            HasDirectory("tools/agents/net20").WithFiles(AGENT_PDB_FILES),
            HasDirectory("tools/agents/net40").WithFiles(AGENT_PDB_FILES),
            HasDirectory("tools/agents/netcoreapp3.1").WithFiles(AGENT_PDB_FILES_NETCORE)) &
        CheckNuGetPackage("NUnit.Engine",
            HasFiles("LICENSE.txt", "NOTICES.txt"),
            HasDirectory("lib/net20").WithFiles(ENGINE_FILES),
            HasDirectory("lib/netstandard2.0").WithFiles(ENGINE_FILES),
            HasDirectory("lib/netcoreapp3.1").WithFiles(ENGINE_CORE_FILES),
            HasDirectory("contentFiles/any/lib/net20").WithFile("nunit.engine.nuget.addins"),
            HasDirectory("contentFiles/any/lib/netstandard2.0").WithFile("nunit.engine.nuget.addins"),
            HasDirectory("contentFiles/any/lib/netcoreapp3.1").WithFile("nunit.engine.nuget.addins"),
            HasDirectory("contentFiles/any/agents/net20").WithFiles(AGENT_FILES).AndFile("nunit.agent.addins"),
            HasDirectory("contentFiles/any/agents/net40").WithFiles(AGENT_FILES).AndFile("nunit.agent.addins")) &
        CheckNuGetSourcePackage("NUnit.Engine",
            HasDirectory("lib/net20").WithFiles(ENGINE_PDB_FILES),
            HasDirectory("lib/netstandard2.0").WithFiles(ENGINE_PDB_FILES),
            HasDirectory("lib/netcoreapp3.1").WithFiles(ENGINE_CORE_PDB_FILES),
            HasDirectory("contentFiles/any/agents/net20").WithFiles(AGENT_PDB_FILES),
            HasDirectory("contentFiles/any/agents/net40").WithFiles(AGENT_PDB_FILES)) &
        CheckNuGetPackage(
            "NUnit.Engine.Api",
            HasFile("LICENSE.txt"),
            HasDirectory("lib/net20").WithFile("nunit.engine.api.dll"),
            HasDirectory("lib/netstandard2.0").WithFile("nunit.engine.api.dll")) &
        CheckNuGetSourcePackage(
            "NUnit.Engine.Api",
            HasDirectory("lib/net20").WithFile("nunit.engine.api.pdb"),
            HasDirectory("lib/netstandard2.0").WithFile("nunit.engine.api.pdb")) &
        CheckChocolateyPackage(
            "nunit-console-runner",
            HasDirectory("tools").WithFiles("LICENSE.txt", "NOTICES.txt", "VERIFICATION.txt").AndFiles(CONSOLE_FILES).AndFiles(ENGINE_FILES).AndFile("nunit.choco.addins"),
            HasDirectory("tools/agents/net20").WithFiles(AGENT_FILES).AndFile("nunit.agent.addins"),
            HasDirectory("tools/agents/net40").WithFiles(AGENT_FILES).AndFile("nunit.agent.addins")) &
        CheckZipPackage(
            "NUnit.Console",
            HasFiles("LICENSE.txt", "license.rtf", "NOTICES.txt", "CHANGES.txt"),
            HasDirectory("bin/net20").WithFiles(CONSOLE_FILES).AndFiles(ENGINE_FILES).AndFile("nunit3-console.pdb").AndFiles(ENGINE_PDB_FILES),
            HasDirectory("bin/net35").WithFiles(CONSOLE_FILES).AndFiles(ENGINE_FILES).AndFile("nunit3-console.pdb").AndFiles(ENGINE_PDB_FILES),
            HasDirectory("bin/netstandard2.0").WithFiles(ENGINE_FILES).AndFiles(ENGINE_PDB_FILES),
            HasDirectory("bin/netcoreapp2.1").WithFiles(ENGINE_FILES).AndFiles(ENGINE_PDB_FILES),
            HasDirectory("bin/netcoreapp3.1").WithFiles(ENGINE_FILES).AndFiles(ENGINE_PDB_FILES),
            HasDirectory("bin/agents/net20").WithFiles(AGENT_FILES).AndFiles(AGENT_PDB_FILES),
            HasDirectory("bin/agents/net40").WithFiles(AGENT_FILES).AndFiles(AGENT_PDB_FILES)) &
        CheckMsiPackage("NUnit.Console", 
            HasDirectory("NUnit.org").WithFiles("LICENSE.txt", "NOTICES.txt", "nunit.ico"),
            HasDirectory("NUnit.org/nunit-console").WithFiles(CONSOLE_FILES).AndFiles(ENGINE_FILES).AndFile("nunit.bundle.addins"),
            HasDirectory("Nunit.org/nunit-console/addins").WithFiles("nunit.core.dll", "nunit.core.interfaces.dll", "nunit.v2.driver.dll", "nunit-project-loader.dll", "vs-project-loader.dll", "nunit-v2-result-writer.dll", "teamcity-event-listener.dll"));

    if (!isOK)
        throw new Exception("One or more package checks failed. See listing.");
}

private bool CheckNuGetPackage(string packageId, params ICheck[] checks)
{
    return CheckPackage($"{PACKAGE_DIR}{packageId}.{productVersion}.nupkg", checks);
}

private bool CheckNuGetSourcePackage(string packageId, params ICheck[] checks)
{
    return CheckPackage($"{PACKAGE_DIR}{packageId}.{productVersion}.snupkg", checks);
}

private bool CheckChocolateyPackage(string packageId, params ICheck[] checks)
{
    return CheckPackage($"{PACKAGE_DIR}{packageId}.{productVersion}.nupkg", checks);
}

private bool CheckZipPackage(string packageName, params ICheck[] checks)
{
    return CheckPackage($"{PACKAGE_DIR}{packageName}-{productVersion}.zip", checks);
}

// NOTE: Msi package currently uses "version" rather than "productVersion"

private bool CheckMsiPackage(string packageName, params ICheck[] checks)
{
    return CheckPackage($"{PACKAGE_DIR}{packageName}-{version}.msi", checks);
}

private bool CheckPackage(string package, params ICheck[] checks)
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

private bool ApplyChecks(string dir, ICheck[] checks)
{
    bool allOK = true;

    foreach (var check in checks)
        allOK &= check.Apply(dir);

    return allOK;
}

private interface ICheck
{
    bool Apply(string dir);
}

private class FileCheck : ICheck
{
    string[] _paths;

    public FileCheck(string[] paths)
    {
        _paths = paths;
    }

    public bool Apply(string dir)
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

private class DirectoryCheck : ICheck
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

    public bool Apply(string dir)
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

private FileCheck HasFile(string file) => HasFiles(new [] { file });
private FileCheck HasFiles(params string[] files) => new FileCheck(files);  

private DirectoryCheck HasDirectory(string dir) => new DirectoryCheck(dir);

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
