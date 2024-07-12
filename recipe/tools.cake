// Load all tools used by the recipe
#tool NuGet.CommandLine&version=6.9.1
#tool dotnet:?package=GitVersion.Tool&version=5.12.0
#tool dotnet:?package=GitReleaseManager.Tool&version=0.17.0

public static class Tools
{
	public static DirectoryPath FindInstalledTool(string packageId)
	{
		if (SIO.Directory.Exists(BuildSettings.ToolsDirectory + packageId))
			return BuildSettings.ToolsDirectory + packageId;

		foreach(var dir in BuildSettings.Context.GetDirectories(BuildSettings.ToolsDirectory + $"{packageId}.*"))
			return dir; // Use first one found

		return null;
	}

	public static DirectoryPath FindInstalledTool(string packageId, string version)
	{
		if (version == null)
			throw new ArgumentNullException(nameof(version));

		var toolPath = BuildSettings.ToolsDirectory + $"{packageId}.{version}";
		return BuildSettings.ToolsDirectory + $"{packageId}.{version}";
	}
}