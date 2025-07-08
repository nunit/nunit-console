//////////////////////////////////////////////////////////////////////
// STATIC SYNTAX FOR EXPRESSING PACKAGE CHECKS
//////////////////////////////////////////////////////////////////////

public static FileCheck HasFile(FilePath file) => HasFiles(new[] { file });
public static FileCheck HasFiles(params FilePath[] files) => new FileCheck(files);

public static DirectoryCheck HasDirectory(string dirPathOrPattern) => new DirectoryCheck(dirPathOrPattern);

public static DependencyCheck HasDependency(string packageId, string packageVersion = null) => new DependencyCheck(packageId, packageVersion);
public static DependencyCheck HasDependency(PackageReference packageReference) => new DependencyCheck(packageReference);

//////////////////////////////////////////////////////////////////////
// PACKAGECHECK CLASS
//////////////////////////////////////////////////////////////////////

public abstract class PackageCheck
{
	protected ICakeContext _context;

	public PackageCheck()
	{
		_context = BuildSettings.Context;
	}
	
	public abstract bool ApplyTo(DirectoryPath testDirPath);

	protected bool CheckDirectoryExists(DirectoryPath dirPath)
	{
		if (!_context.DirectoryExists(dirPath))
		{
			DisplayError($"Directory {dirPath} was not found.");
			return false;
		}

		return true;
	}

	protected bool CheckFileExists(FilePath filePath)
	{
		if (!_context.FileExists(filePath))
		{
			DisplayError($"File {filePath} was not found.");
			return false;
		}

		return true;
	}

	protected bool CheckFilesExist(IEnumerable<FilePath> filePaths)
	{
		bool isOK = true;

		foreach (var filePath in filePaths)
			isOK &= CheckFileExists(filePath);

		return isOK;
	}

	protected bool DisplayError(string msg)
	{
		_context.Error("  ERROR: " + msg);

		// The return value may be ignored or used as a shortcut
		// for an immediate return from ApplyTo as in
		//    return DisplayError(...)
		return false;
	}
}

//////////////////////////////////////////////////////////////////////
// FILECHECK CLASS
//////////////////////////////////////////////////////////////////////

public class FileCheck : PackageCheck
{
	FilePath[] _files;

	public FileCheck(FilePath[] files)
	{
		_files = files;
	}

	public override bool ApplyTo(DirectoryPath testDirPath)
	{
		return CheckFilesExist(_files.Select(file => testDirPath.CombineWithFilePath(file)));
	}
}

//////////////////////////////////////////////////////////////////////
// DIRECTORYCHECK CLASS
//////////////////////////////////////////////////////////////////////

public class DirectoryCheck : PackageCheck
{
	private string _dirPathOrPattern;
	private List<FilePath> _files = new List<FilePath>();

	public DirectoryCheck(string dirPathOrPattern)
	{
		_dirPathOrPattern = dirPathOrPattern;
	}

	public DirectoryCheck WithFiles(params FilePath[] files)
	{
		_files.AddRange(files);
		return this;
	}

    public DirectoryCheck AndFiles(params FilePath[] files)
    {
        return WithFiles(files);
    }

	public DirectoryCheck WithFile(FilePath file)
	{
		_files.Add(file);
		return this;
	}

    public DirectoryCheck AndFile(FilePath file)
    {
        return AndFiles(file);
    }

	public override bool ApplyTo(DirectoryPath testDirPath)
	{
        if (_dirPathOrPattern.Contains('*') || _dirPathOrPattern.Contains('?')) // Wildcard
        {
            var absDirPattern = testDirPath.Combine(_dirPathOrPattern).ToString();
            foreach (var dir in _context.GetDirectories(absDirPattern))
            {
                // Use first one found
                return CheckFilesExist(_files.Select(file => dir.CombineWithFilePath(file)));
            }
        }
        else // No wildcard
        {
            var absDirPath = testDirPath.Combine(_dirPathOrPattern);
            if (!CheckDirectoryExists(absDirPath))
                return false;

            return CheckFilesExist(_files.Select(file => absDirPath.CombineWithFilePath(file)));
        }

		return false;
	}
}

//////////////////////////////////////////////////////////////////////
// DEPENDENCYCHECK CLASS
//////////////////////////////////////////////////////////////////////

public class DependencyCheck : PackageCheck
{
    private string _packageId;
    private string _packageVersion;


    private List<DirectoryCheck> _directoryChecks = new List<DirectoryCheck>();
    private List<FilePath> _files = new List<FilePath>();

    public DependencyCheck(string packageId, string packageVersion)
    {
        _packageId = packageId;
        _packageVersion = packageVersion;
    }

    public DependencyCheck(PackageReference packageReference)
    {
        _packageId = packageReference.Id;
        _packageVersion = packageReference.Version;
    }

    public DependencyCheck WithFiles(params FilePath[] files)
    {
        _files.AddRange(files);
        return this;
    }

    public DependencyCheck WithFile(FilePath file)
    {
        _files.Add(file);
        return this;
    }

    public DependencyCheck WithDirectory(string relDirPath)
    {
        _directoryChecks.Add(new DirectoryCheck(relDirPath));
        return this;
    }

    public override bool ApplyTo(DirectoryPath testDirPath)
    {
        var packageInstallPath = testDirPath.GetParent();
        string pattern = packageInstallPath.Combine(_packageId) + ".*";
        var installedPackages = new List<string>(_context.GetDirectories(pattern).Select(p => p.FullPath));

        DirectoryPath packagePath = GetDependentPackagePath(packageInstallPath);
        if (packagePath == null)
            return false;

        bool isOK = CheckFilesExist(_files.Select(file => packagePath.CombineWithFilePath(file)));

        foreach (var directoryCheck in _directoryChecks)
            isOK &= directoryCheck.ApplyTo(packagePath);

        return isOK;

        DirectoryPath GetDependentPackagePath(DirectoryPath packageInstallPath)
        {
            if (_packageVersion != null)
            {
                var packagePath = packageInstallPath.Combine(_packageId + "." + _packageVersion);
                if (_context.DirectoryExists(packagePath))
                {
                    _context.Information($"  Using version {_packageVersion} of package {_packageId}");
                    return packagePath;
                }
            }

            // At this point, either no package version was specified or, if it was, it was not found.

            switch (installedPackages.Count)
            {
                case 0:
                    DisplayError($"Package {_packageId} is not installed.");
                    return null;

                case 1:
                    if (_packageVersion == null)
                    {
                        var packagePath = installedPackages[0];
                        var packageVersion = System.IO.Path.GetFileName(packagePath).Substring(_packageId.Length + 1);
                        _context.Information($"  Using version {packageVersion} of package {_packageId}");

                        return packagePath;
                    }

                    DisplayError($"Version {_packageVersion} of {_packageId} is not installed.");
                    return null;

                default:
                    DisplayError(_packageVersion == null
                        ? $"Version was not specified and multiple versions of package {_packageId} were found."
                        : $"Version {_packageVersion} of {_packageId} is not installed.");
                    return null;
            }
        }
    }
}

//////////////////////////////////////////////////////////////////////
// STATIC SYNTAX FOR EXPRESSING OUTPUT CHECKS
//////////////////////////////////////////////////////////////////////

public static OutputContainsCheck Contains(string expectedText, int atleast = 1, int exactly = -1)
    => new OutputContainsCheck(expectedText, atleast, exactly);

public static OutputDoesNotContain DoesNotContain(string text) => new OutputDoesNotContain(text);

//////////////////////////////////////////////////////////////////////
// OutputCheck class is used to check content of redirected package test output
//////////////////////////////////////////////////////////////////////

public abstract class OutputCheck
{
    protected string _expectedText;
    protected int _atleast;
    protected int _exactly;

    public OutputCheck(string expectedText, int atleast = 1, int exactly = -1)
    {
        _expectedText = expectedText;
        _atleast = atleast;
        _exactly = exactly;
    }

    public bool Matches(IEnumerable<string> output) => Matches(string.Join("\r\n", output));

    public abstract bool Matches(string output);

    public string Message { get; protected set; }
}

//////////////////////////////////////////////////////////////////////
// Derived class for checking that some content is present
//////////////////////////////////////////////////////////////////////

public class OutputContainsCheck : OutputCheck
{
    public OutputContainsCheck(string expectedText, int atleast = 1, int exactly = -1) : base(expectedText, atleast, exactly) { }

    public override bool Matches(string output)
    {
        int found = 0;

        int index = output.IndexOf(_expectedText);
        int textLength = _expectedText.Length;
        int outputLength = output.Length;
        while (index >= 0 && index < output.Length - textLength)
        {
            ++found;
            index += textLength;
            index = output.IndexOf(_expectedText, index);
        }

        if (_atleast > 0 && found >= _atleast || _exactly > 0 && found == _exactly)
            return true;

        var sb = new StringBuilder("   Expected: ");
        if (_atleast > 0)
        {
            sb.Append($"at least {_atleast} ");
            sb.Append(_atleast == 1 ? "line " : "lines ");
            sb.Append($"containing \"{_expectedText}\" but found {found}");
        }
        else
        {
            sb.Append($"exactly {_exactly} ");
            sb.Append(_exactly == 1 ? "line " : "lines ");
            sb.Append($"containing \"{_expectedText}\" but found {found}");
        }

        if (_exactly > 0)
            if (found == _exactly)
                return true;
            else
            {
                Message = $"   Expected: at least one line containing \"{_expectedText}\"   But none were found";
                return false;
            }

        return false;
    }
}

//////////////////////////////////////////////////////////////////////
// Derived class for checking that some content is not present
//////////////////////////////////////////////////////////////////////

public class OutputDoesNotContain : OutputCheck
{
    public OutputDoesNotContain(string expectedText) : base(expectedText) { }

    public override bool Matches(string output)
    {
        if (output.Contains(_expectedText))
        {
            Message = $"   Expected: no lines containing \"{_expectedText}\"   But at least one was found";
            return false;
        }

        return true;
    }
}

// Representation of a single test to be run against a pre-built package.
// Each test has a Level, with the following values defined...
//  0 Do not run - used for temporarily disabling a test
//  1 Run for all CI tests - that is every time we test packages
//  2 Run only on PRs, dev builds and when publishing
//  3 Run only when publishing
public class PackageTest
{
    public int Level { get; private set; }
    public string Name { get; private set; }

    public string Description { get; set; }
    public string Arguments { get; set; }
    public int ExpectedReturnCode { get; set; } = 0;
    public ExpectedResult ExpectedResult { get; set; }
    public OutputCheck[] ExpectedOutput { get; set; }
    public ExtensionSpecifier[] ExtensionsNeeded { get; set; } = new ExtensionSpecifier[0];

    public PackageTest(int level, string name)
    {
        if (name == null)
            throw new ArgumentNullException(nameof(name));

        Level = level;
        Name = name;
        Description = name;
    }
}

// Representation of an extension, for use by PackageTests. Because our
// extensions usually exist as both nuget and chocolatey packages, each
// extension may have a nuget id, a chocolatey id or both. A default version
// is used unless the user overrides it using SetVersion.
public class ExtensionSpecifier
{
    public ExtensionSpecifier(string nugetId, string chocoId, string version)
    {
        NuGetId = nugetId;
        ChocoId = chocoId;
        Version = version;
    }

    public string NuGetId { get; }
    public string ChocoId { get; }
    public string Version { get; }

    public PackageReference NuGetPackage => new PackageReference(NuGetId, Version);
    public PackageReference ChocoPackage => new PackageReference(ChocoId, Version);
    public PackageReference LatestChocolateyRelease => ChocoPackage.LatestRelease;

    // Return an extension specifier using the same package ids as this
    // one but specifying a particular version to be used.
    public ExtensionSpecifier SetVersion(string version)
    {
        return new ExtensionSpecifier(NuGetId, ChocoId, version);
    }

    // Install this extension for a package
    public void InstallExtension(PackageDefinition targetPackage)
    {
        PackageReference extensionPackage = targetPackage.PackageType == PackageType.Chocolatey
            ? ChocoPackage
            : NuGetPackage;

        extensionPackage.Install(targetPackage.ExtensionInstallDirectory);
    }
}

// Representation of a package reference, containing everything needed to install it
public class PackageReference
{
    private ICakeContext _context;

    public string Id { get; }
    public string Version { get; }

    public PackageReference(string id, string version)
    {
        _context = BuildSettings.Context;

        Id = id;
        Version = version;
    }

    public PackageReference LatestDevBuild => GetLatestDevBuild();
    public PackageReference LatestRelease => GetLatestRelease();

    private PackageReference GetLatestDevBuild()
    {
        var packageList = _context.NuGetList(Id, new NuGetListSettings()
        {
            Prerelease = true,
            Source = new[] { "https://www.myget.org/F/nunit/api/v3/index.json" }
        });

        foreach (var package in packageList)
            return new PackageReference(package.Name, package.Version);

        return this;
    }

    private PackageReference GetLatestRelease()
    {
        var packageList = _context.NuGetList(Id, new NuGetListSettings()
        {
            Prerelease = true,
            Source = new[] {
                "https://www.nuget.org/api/v2/",
                "https://community.chocolatey.org/api/v2/" }
        });

        // TODO: There seems to be an error in NuGet or in Cake, causing the list to
        // contain ALL NuGet packages, so we check the Id in this loop.
        foreach (var package in packageList)
            if (package.Name == Id)
                return new PackageReference(Id, package.Version);

        return this;
    }

    public bool IsInstalled(string installDirectory)
    {
        return _context.GetDirectories($"{installDirectory}{Id}.*").Count > 0;
    }

    public void InstallExtension(PackageDefinition targetPackage)
    {
        Install(targetPackage.ExtensionInstallDirectory);
    }

    public void Install(string installDirectory)
    {
        if (!IsInstalled(installDirectory))
        {
            Banner.Display($"Installing {Id} version {Version}");

            var packageSources = new[]
            {
                "https://www.myget.org/F/nunit/api/v3/index.json",
                "https://api.nuget.org/v3/index.json",
                "https://community.chocolatey.org/api/v2/"
            };

            Console.WriteLine("Package Sources:");
            foreach (var source in packageSources)
                Console.WriteLine($"  {source}");
            Console.WriteLine();

            _context.NuGetInstall(Id,
                new NuGetInstallSettings()
                {
                    OutputDirectory = installDirectory,
                    Version = Version,
                    Source = packageSources
                });
        }
    }
}
