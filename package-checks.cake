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
    string[] ENGINE_FILES = { "nunit.engine.dll", "nunit.engine.core.dll", "nunit.engine.api.dll", "Mono.Cecil.dll" };
    string[] AGENT_FILES = { 
        "nunit-agent.exe", "nunit-agent.exe.config", "nunit-agent-x86.exe", "nunit-agent-x86.exe.config", "nunit.engine.core.dll", "nunit.engine.api.dll", "Mono.Cecil.dll" };
    string[] CONSOLE_FILES = { "nunit3-console.exe", "nunit3-console.exe.config" };

    bool isOK =
        CheckNuGetPackage(
            "NUnit.Console",
            HasFile("LICENSE.txt")) &
        CheckNuGetPackage(
            "NUnit.ConsoleRunner",
            HasFiles("LICENSE.txt", "NOTICES.txt", "CHANGES.txt"),
            HasDirectory("tools").WithFiles(CONSOLE_FILES).AndFiles(ENGINE_FILES),
            HasDirectory("tools/agents/net20").WithFiles(AGENT_FILES),
            HasDirectory("tools/agents/net40").WithFiles(AGENT_FILES)) &
        CheckNuGetPackage("NUnit.Engine",
            HasFiles("LICENSE.txt", "NOTICES.txt", "CHANGES.txt"),
            HasDirectory("lib/net20").WithFiles(ENGINE_FILES),
            HasDirectory("lib/netstandard1.6").WithFiles(ENGINE_FILES),
            HasDirectory("lib/netstandard2.0").WithFiles(ENGINE_FILES),
            HasDirectory("contentFiles/any/agents/net20").WithFiles(AGENT_FILES),
            HasDirectory("contentFiles/any/agents/net40").WithFiles(AGENT_FILES)) &
        CheckNuGetPackage(
            "NUnit.Engine.Api",
            HasFile("LICENSE.txt"),
            HasDirectory("lib/net20").WithFile("nunit.engine.api.dll"),
            HasDirectory("lib/netstandard1.6").WithFile("nunit.engine.api.dll"),
            HasDirectory("lib/netstandard2.0").WithFile("nunit.engine.api.dll")) &
        CheckNuGetPackage(
            "NUnit.Runners",
            HasFile("LICENSE.txt")) &
        CheckChocolateyPackage(
            "nunit-console-runner",
            HasDirectory("tools").WithFiles("LICENSE.txt", "NOTICES.txt", "CHANGES.txt", "VERIFICATION.txt").AndFiles(CONSOLE_FILES).AndFiles(ENGINE_FILES),
            HasDirectory("tools/agents/net20").WithFiles(AGENT_FILES),
            HasDirectory("tools/agents/net40").WithFiles(AGENT_FILES)) &
        CheckChocolateyPackage(
            "nunit-console-with-extensions",
            HasDirectory("tools").WithFiles("LICENSE.txt", "NOTICES.txt", "VERIFICATION.txt")) &
        CheckZipPackage(
            "NUnit.Console",
            HasFiles("LICENSE.txt", "license.rtf", "NOTICES.txt", "CHANGES.txt"),
            HasDirectory("bin/net20").WithFiles("nunit3-console.exe", "nunit3-console.exe.config").AndFiles(ENGINE_FILES),
            HasDirectory("bin/net35").WithFiles("nunit3-console.exe", "nunit3-console.exe.config").AndFiles(ENGINE_FILES),
            HasDirectory("bin/netstandard1.6").WithFiles(ENGINE_FILES),
            HasDirectory("bin/netstandard2.0").WithFiles(ENGINE_FILES),
            HasDirectory("bin/netcoreapp1.1").WithFiles(ENGINE_FILES),
            HasDirectory("bin/netcoreapp1.1").WithFiles(ENGINE_FILES),
            HasDirectory("bin/agents/net20").WithFiles(AGENT_FILES),
            HasDirectory("bin/agents/net40").WithFiles(AGENT_FILES)) &
        CheckMsiPackage("NUnit.Console", HasDirectory("NUnit.org/nunit-console")); // Placeholder for later implementation

    if (!isOK)
        throw new Exception("One or more package checks failed. See listing.");
}

private bool CheckNuGetPackage(string packageId, params ICheck[] checks)
{
    return CheckPackage($"{PACKAGE_DIR}{packageId}.{productVersion}.nupkg", checks);
}

private bool CheckChocolateyPackage(string packageId, params ICheck[] checks)
{
    return CheckPackage($"{PACKAGE_DIR}{packageId}.{productVersion}.nupkg", checks);
}

// NOTE: Zip and msi packages currently use "version" rather than "productVersion"

private bool CheckZipPackage(string packageName, params ICheck[] checks)
{
    return CheckPackage($"{PACKAGE_DIR}{packageName}-{version}.zip", checks);
}

private bool CheckMsiPackage(string packageName, params ICheck[] checks)
{
    return CheckPackage($"{PACKAGE_DIR}{packageName}-{version}.msi", checks);
}

private bool CheckPackage(string path, params ICheck[] checks)
{
    Console.WriteLine("\nPackage Name: " + System.IO.Path.GetFileName(path));

    if (!FileExists(path))
    {
        WriteError("Package was not found!");
        return false;
    }

    bool allOK = true;

    // TODO: Figure out how to check content of msi
    if (checks.Length == 0 || path.EndsWith(".msi"))
    {
        WriteWarning("Package found but not checked.");
    }
    else
    {
        var tempDir = System.IO.Path.GetTempPath() + System.IO.Path.GetRandomFileName() + "/";

        try
        {
            WriteInfo("Unzipping to " + tempDir);
            Unzip(path, tempDir);

            foreach (var check in checks)
                allOK &= check.Apply(tempDir);
        }
        finally
        {
            DeleteDirectory(tempDir, new DeleteDirectorySettings()
            {
                Recursive = true,
                Force = true
            });
        }

        if (allOK)
            WriteInfo("All checks passed!");
    }

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
