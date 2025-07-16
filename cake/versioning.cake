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
