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

        BranchName = _gitVersion.BranchName;
        IsReleaseBranch = BranchName.StartsWith("release-");

        // TODO: Get GitVersion to work on Linux
        string productVersion = context.HasArgument("productVersion")
            ? context.Argument("productVersion", "3.14.0")
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

    string ReplaceAttributeString(string source, string attributeName, string value)
    {
        var matches = Regex.Matches(source, $@"\[assembly: {Regex.Escape(attributeName)}\(""(?<value>[^""]+)""\)\]");
        if (matches.Count != 1) throw new InvalidOperationException($"Expected exactly one line similar to:\r\n[assembly: {attributeName}(\"1.2.3-optional\")]");

        var group = matches[0].Groups["value"];
        return source.Substring(0, group.Index) + value + source.Substring(group.Index + group.Length);
    }

    void ReplaceFileContents(string filePath, Func<string, string> update)
    {
        using (var file = new FileStream(filePath, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
        {
            string source;
            using (var reader = new StreamReader(file, new UTF8Encoding(false), true, 4096, leaveOpen: true))
                source = reader.ReadToEnd();

            var newSource = update.Invoke(source);
            if (newSource == source) return;

            file.Seek(0, SeekOrigin.Begin);
            using (var writer = new StreamWriter(file, new UTF8Encoding(false), 4096, leaveOpen: true))
                writer.Write(newSource);
            file.SetLength(file.Position);
        }
    }
}
