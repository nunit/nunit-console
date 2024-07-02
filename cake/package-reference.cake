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
			Source = new [] { "https://www.myget.org/F/nunit/api/v3/index.json" } 
		} );

		foreach (var package in packageList)
			return new PackageReference(package.Name, package.Version);
		
		return this;
	}

	private PackageReference GetLatestRelease()
	{
		var packageList = _context.NuGetList(Id, new NuGetListSettings()
		{
			Prerelease = true, 
			Source = new [] { 
				"https://www.nuget.org/api/v2/",
				"https://community.chocolatey.org/api/v2/" } 
		} );

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

			var packageSources = new []
			{
				"https://www.myget.org/F/nunit/api/v3/index.json",
				"https://api.nuget.org/v3/index.json",
				"https://community.chocolatey.org/api/v2/"
			};

			Console.WriteLine("Package Sources:");
			foreach(var source in packageSources)
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
