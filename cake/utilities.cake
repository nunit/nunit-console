// Load all tools used by the recipe
#tool NuGet.CommandLine&version=6.9.1
#tool dotnet:?package=GitVersion.Tool&version=5.12.0
#tool dotnet:?package=GitReleaseManager.Tool&version=0.18.0
#addin nuget:?package=Cake.Git&version=5.0.1

// Using statements needed in the scripts
using Cake.Git;
using System.Text.RegularExpressions;
using System.Xml;
using SIO = System.IO;

// Banner class displays banners similar to those created by Cake
// We use this to display intermediate steps within a task
public static class Banner
{
    public static void Display(string message, char barChar = '-', int length = 0)
    {
        if (length == 0) length = MaxLineLength(message);
        var bar = new string(barChar, length);

        Console.WriteLine();
        Console.WriteLine(bar);
        Console.WriteLine(message);
        Console.WriteLine(bar);

        int MaxLineLength(string message)
        {
            int length = 0;
            foreach (string line in message.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries))
                length = Math.Max(length, line.Length);
            return length;
        }
    }
}

public static class Dotnet
{
    // Experimenting with finding dotnet installs for X64 vs x86
    // This code will end up moved into the engine as well.

    private static ICakeContext _context = BuildSettings.Context;
    private static bool _onWindows = SIO.Path.DirectorySeparatorChar == '\\';

    // NOTES:
    // * We don't need an IsInstalled property because our scripts all run under dotnet.

    public static string InstallPath { get; } = GetDotnetInstallDirectory(false);
    public static string X86InstallPath { get; } = GetDotnetInstallDirectory(true);

    // These three properties use => to avoid issues with initialization order!
    public static string Executable => InstallPath + "dotnet.exe";
    public static string X86Executable => X86InstallPath + "dotnet.exe";
    public static bool IsX86Installed => SIO.Directory.Exists(X86InstallPath) && SIO.File.Exists(X86Executable);

    public static void Display()
    {
        _context.Information($"Install Path:      {InstallPath}");
        _context.Information($"Executable:        {Executable}");
        _context.Information("Runtimes:");
        foreach (string dir in SIO.Directory.GetDirectories(SIO.Path.Combine(InstallPath, "shared")))
        {
            string runtime = SIO.Path.GetFileName(dir);
            foreach (string dir2 in SIO.Directory.GetDirectories(dir))
            {
                string version = SIO.Path.GetFileName(dir2);
                _context.Information($"  {runtime} {version}");
            }
        }

        if (IsX86Installed)
        {
            _context.Information($"\nX86 Install Path:  {X86InstallPath}");
            _context.Information($"X86 Executable:    {X86Executable}");
            _context.Information("Runtimes:");
            foreach (var dir in SIO.Directory.GetDirectories(SIO.Path.Combine(X86InstallPath, "shared")))
            {
                string runtime = SIO.Path.GetFileName(dir);
                foreach (string dir2 in SIO.Directory.GetDirectories(dir))
                {
                    string version = SIO.Path.GetFileName(dir2);
                    _context.Information($"  {runtime} {version}");
                }
            }
        }
        else
            _context.Information("\nDotnet X86 is not installed");
    }

    private static string GetDotnetInstallDirectory(bool forX86 = false)
    {
        if (_onWindows)
        {
            if (forX86)
            {
                Microsoft.Win32.RegistryKey key =
                    Microsoft.Win32.Registry.LocalMachine.OpenSubKey(@"SOFTWARE\WOW6432Node\dotnet\SetUp\InstalledVersions\x86\");
                return (string)key?.GetValue("InstallLocation");
            }
            else
            {
                Microsoft.Win32.RegistryKey key =
                    Microsoft.Win32.Registry.LocalMachine.OpenSubKey(@"SOFTWARE\dotnet\SetUp\InstalledVersions\x64\sharedHost\");
                return (string)key?.GetValue("Path");
            }
        }
        else // Assuming linux for now
            return "/usr/shared/dotnet/";
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

public class BuildVersion
{
    private ICakeContext _context;
    private GitVersion _gitVersion;

    // NOTE: This is complicated because (1) the user may have specified 
    // the package version on the command-line and (2) GitVersion may
    // or may not be available. We'll work on solving (2) by getting
    // GitVersion to run for us on Linux, but (1) will alwas remain.
    //
    // We simplify things a by figuring out the full package version and
    // then parsing it to provide information that is used in the build.
    public BuildVersion(ICakeContext context)
    {
        if (context == null)
            throw new ArgumentNullException(nameof(context));

        _context = context;
        _gitVersion = context.GitVersion();

        BranchName = _gitVersion.BranchName;
        IsReleaseBranch = BranchName.StartsWith("release-");

        string packageVersion = CommandLineOptions.PackageVersion.Value ?? CalculatePackageVersion();

        int dash = packageVersion.IndexOf('-');
        IsPreRelease = dash > 0;

        string versionPart = packageVersion;
        string suffix = "";
        string label = "";

        if (IsPreRelease)
        {
            versionPart = packageVersion.Substring(0, dash);
            suffix = packageVersion.Substring(dash + 1);
            foreach (char c in suffix)
            {
                if (!char.IsLetter(c))
                    break;
                label += c;
            }
        }

        Version version = new Version(versionPart);
        SemVer = version.ToString(3);
        PreReleaseLabel = label;
        PreReleaseSuffix = suffix;

        PackageVersion = LegacyPackageVersion = packageVersion;
        AssemblyVersion = SemVer + ".0";
        AssemblyFileVersion = SemVer;
        AssemblyInformationalVersion = packageVersion;

        // We use a legacy SemVer 1.0 format for alpha, beta and rc releases on chocolatey
        var labelWithDot = label + ".";
        int num;
        if (suffix.StartsWith(labelWithDot) && int.TryParse(suffix.Substring(labelWithDot.Length), out num))
            LegacyPackageVersion = $"{SemVer}-{label}{num:000}";
    }

    public string BranchName { get; }
    public bool IsReleaseBranch { get; }

    public string PackageVersion { get; }
    public string LegacyPackageVersion { get; }
    public string AssemblyVersion { get; }
    public string AssemblyFileVersion { get; }
    public string AssemblyInformationalVersion { get; }

    public string SemVer { get; }
    public bool IsPreRelease { get; }
    public string PreReleaseLabel { get; }
    public string PreReleaseSuffix { get; }

    private string CalculatePackageVersion()
    {
        string label = _gitVersion.PreReleaseLabel;

        // Non pre-release is easy
        if (string.IsNullOrEmpty(label))
            return _gitVersion.MajorMinorPatch;

        string branchName = _gitVersion.BranchName;

        // We don't currently use this pattern, but check in case we do later.
        if (branchName.StartsWith("feature/"))
            branchName = branchName.Substring(8);

        // Arbitrary branch names are ci builds
        if (label == branchName)
            label = "ci";

        string suffix = "-" + label;

        switch (label)
        {
            case "ci":
                branchName = Regex.Replace(branchName, "[^0-9A-Za-z-]+", "-");
                suffix += _gitVersion.CommitsSinceVersionSourcePadded + "-" + branchName;
                break;
            case "dev":
            case "pre":
            case "pr":
            case "rc":
            case "alpha":
            case "beta":
            default:
                suffix += "." + _gitVersion.PreReleaseNumber;
                break;
        }

        // Nuget limits "special version part" to 20 chars. Add one for the hyphen.
        if (suffix.Length > 21)
            suffix = suffix.Substring(0, 21);
        return _gitVersion.MajorMinorPatch + suffix;
    }
}
