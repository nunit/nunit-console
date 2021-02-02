using System.Text.RegularExpressions;

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
