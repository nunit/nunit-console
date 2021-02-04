using System.Text.RegularExpressions;

public class BuildVersion
{
    private ISetupContext _context;
    private BuildSystem _buildSystem;

	public BuildVersion(ISetupContext context)
	{
        _context = context;
        _buildSystem = context.BuildSystem();

        PackageVersion = context.Argument("productVersion", "3.13.0");

        var dash = PackageVersion.IndexOf('-');
        SemVer = dash > 0
            ? PackageVersion.Substring(0, dash)
            : PackageVersion;

        if (_buildSystem.IsRunningOnAppVeyor)
        {
            var buildNumber = _buildSystem.AppVeyor.Environment.Build.Number.ToString("00000");
            var branch = _buildSystem.AppVeyor.Environment.Repository.Branch;
            var isPullRequest = _buildSystem.AppVeyor.Environment.PullRequest.IsPullRequest;

            if (branch == "master" && !isPullRequest)
            {
                PackageVersion = SemVer + "-dev-" + buildNumber;
            }
            else
            {
                var suffix = "-ci-" + buildNumber;

                if (isPullRequest)
                    suffix += "-pr-" + _buildSystem.AppVeyor.Environment.PullRequest.Number;
                else if (_buildSystem.AppVeyor.Environment.Repository.Branch.StartsWith("release", StringComparison.OrdinalIgnoreCase))
                    suffix += "-pre-" + buildNumber;
                else
                    suffix += "-" + System.Text.RegularExpressions.Regex.Replace(branch, "[^0-9A-Za-z-]+", "-");

                // Nuget limits "special version part" to 20 chars. Add one for the hyphen.
                if (suffix.Length > 21)
                    suffix = suffix.Substring(0, 21);

                PackageVersion = SemVer + suffix;
            }
        }
    }

    public string BranchName { get; }
    public bool IsReleaseBranch { get; }

	public string PackageVersion { get; }
    public string SemVer { get; }
    public string AssemblyVersion => SemVer + ".0";
    public string AssemblyFileVersion => PackageVersion;
    public string AssemblyInformationalVersion => PackageVersion;

    //public void PatchAssemblyInfo(string sourceFile, string assemblyVersion = null)
    //{
    //    ReplaceFileContents(sourceFile, source =>
    //    {
    //        source = ReplaceAttributeString(source, "AssemblyVersion", assemblyVersion ?? _parameters.AssemblyVersion);

    //        source = ReplaceAttributeString(source, "AssemblyFileVersion", _parameters.AssemblyFileVersion);

    //        source = ReplaceAttributeString(source, "AssemblyInformationalVersion", _parameters.AssemblyInformationalVersion);

    //        return source;
    //    });

    //    string ReplaceAttributeString(string source, string attributeName, string value)
    //    {
    //        var matches = Regex.Matches(source, $@"\[assembly: {Regex.Escape(attributeName)}\(""(?<value>[^""]+)""\)\]");
    //        if (matches.Count != 1) throw new InvalidOperationException($"Expected exactly one line similar to:\r\n[assembly: {attributeName}(\"1.2.3-optional\")]");

    //        var group = matches[0].Groups["value"];
    //        return source.Substring(0, group.Index) + value + source.Substring(group.Index + group.Length);
    //    }

    //    void ReplaceFileContents(string filePath, Func<string, string> update)
    //    {
    //        using (var file = new FileStream(filePath, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
    //        {
    //            string source;
    //            using (var reader = new StreamReader(file, new UTF8Encoding(false), true, 4096, leaveOpen: true))
    //                source = reader.ReadToEnd();

    //            var newSource = update.Invoke(source);
    //            if (newSource == source) return;

    //            file.Seek(0, SeekOrigin.Begin);
    //            using (var writer = new StreamWriter(file, new UTF8Encoding(false), 4096, leaveOpen: true))
    //                writer.Write(newSource);
    //            file.SetLength(file.Position);
    //        }
    //    }
    //}
}

public static void PatchAssemblyInfo(string sourceFile, string assemblyInformationalVersion, string assemblyVersion)
{
    ReplaceFileContents(sourceFile, source =>
    {
        if (assemblyInformationalVersion != null)
            source = ReplaceAttributeString(source, "AssemblyInformationalVersion", assemblyInformationalVersion);

        if (assemblyVersion != null)
            source = ReplaceAttributeString(source, "AssemblyVersion", assemblyVersion);

        return source;
    });

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
