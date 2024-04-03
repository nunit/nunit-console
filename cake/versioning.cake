using System.Text.RegularExpressions;

public class BuildVersion
{
    private ISetupContext _context;
    private GitVersion _gitVersion;

    // NOTE: This is complicated because (1) the user may have specified 
    // the package version on the command-line and (2) GitVersion may
    // or may not be available. We'll work on solving (2) by getting
    // GitVersion to run for us on Linux, but (1) will alwas remain.
    //
    // We simplify things a by figuring out the full package version and
    // then parsing it to provide information that is used in the build.
    public BuildVersion(ISetupContext context)
    {
        _context = context;
        _gitVersion = context.GitVersion();
        Information("Running GitVersion: {0}", _gitVersion.FullSemVer);
        BranchName = _gitVersion.BranchName;
        IsReleaseBranch = BranchName.StartsWith("release-");

        string productVersion = context.HasArgument("productVersion")
            ? context.Argument<string>("productVersion")
            : CalculateProductVersion();
        
        int dash = productVersion.IndexOf('-');
        IsPreRelease = dash > 0;

        string versionPart = productVersion;
        string suffix = "";
        string label = "";

        if (IsPreRelease)
        {
            versionPart = productVersion.Substring(0, dash);
            suffix = productVersion.Substring(dash + 1);
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

        ProductVersion = productVersion;
        AssemblyVersion = SemVer + ".0";
        AssemblyFileVersion = SemVer;
        AssemblyInformationalVersion = productVersion;
    }

    public string BranchName { get; }
    public bool IsReleaseBranch { get; }

    public string ProductVersion { get; }
    public string AssemblyVersion { get; }
    public string AssemblyFileVersion { get; }
    public string AssemblyInformationalVersion { get; }

    public string SemVer { get; }
    public bool IsPreRelease { get; }
    public string PreReleaseLabel { get; }
    public string PreReleaseSuffix { get; }

    private string CalculateProductVersion()
    {
        string label = _gitVersion.PreReleaseLabel;

        // Non pre-release is easy
        if (string.IsNullOrEmpty(label))
            return _gitVersion.MajorMinorPatch;

        string branchName = _gitVersion.BranchName;

        // Treat version3 branch as an alternate "main"
        if (branchName == "version3X" || branchName == "version315" || branchName == "version317")
            label = "dev";

        // We don't currently use this pattern, but check in case we do later.
        if (branchName.StartsWith("feature/"))
            branchName = branchName.Substring(8);

        // Arbitrary branch names are ci builds
        if (label == branchName)
            label = "ci";

        string suffix = "-" + label + _gitVersion.CommitsSinceVersionSourcePadded;

        switch (label)
        {
            case "ci":
                branchName = Regex.Replace(branchName, "[^0-9A-Za-z-]+", "-");
                suffix += "-" + branchName;
                // Nuget limits "special version part" to 20 chars. Add one for the hyphen.
                if (suffix.Length > 21)
                    suffix = suffix.Substring(0, 21);
                return _gitVersion.MajorMinorPatch + suffix;

            case "dev":
            case "pre":
                return _gitVersion.MajorMinorPatch + suffix;

            case "pr":
                return _gitVersion.LegacySemVerPadded;

            case "rc":
            case "alpha":
            case "beta":
            default:
                return _gitVersion.LegacySemVer;
        }
    }
}
