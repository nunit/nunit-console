public static class PackageReleaseManager
{
	private static ICakeContext _context;
	
	static PackageReleaseManager()
	{
		_context = BuildSettings.Context;
	}

    private static bool _hadErrors = false;

    public static void Publish()
    {
        _hadErrors = false;

        PublishToMyGet();
        PublishToNuGet();
        PublishToChocolatey();

        if (_hadErrors)
            throw new Exception("One of the publishing steps failed.");
    }

    public static void PublishToMyGet()
    {
        if (!BuildSettings.ShouldPublishToMyGet)
            _context.Information("Nothing to publish to MyGet from this run.");
        else if (CommandLineOptions.NoPush)
            _context.Information("NoPush option suppressing publication to MyGet");
        else
            foreach (var package in BuildSettings.Packages)
            {
                try
                {
                    if (package.PackageType == PackageType.NuGet)
                    {
                        var packageName = $"{package.PackageId}.{BuildSettings.PackageVersion}.nupkg";
                        var packagePath = BuildSettings.PackageDirectory + packageName;
                        PushNuGetPackage(packagePath, BuildSettings.MyGetApiKey, BuildSettings.MyGetPushUrl);
                    }
                    else if (package.PackageType == PackageType.Chocolatey)
                    {
                        var packageName = $"{package.PackageId}.{BuildSettings.LegacyPackageVersion}.nupkg";
                        var packagePath = BuildSettings.PackageDirectory + packageName;
                        PushChocolateyPackage(packagePath, BuildSettings.MyGetApiKey, BuildSettings.MyGetPushUrl);
                    }
                }
                catch (Exception ex)
                {
                    _context.Error(ex.Message);
                    _hadErrors = true;
                }
            }
    }

    public static void PublishToNuGet()
    {
        if (!BuildSettings.ShouldPublishToNuGet)
            _context.Information("Nothing to publish to NuGet from this run.");
        else if (CommandLineOptions.NoPush)
            _context.Information("NoPush option suppressing publication to NuGet");
        else
            foreach (var package in BuildSettings.Packages)
            {
                var packageName = $"{package.PackageId}.{BuildSettings.PackageVersion}.nupkg";
                var packagePath = BuildSettings.PackageDirectory + packageName;
                try
                {
                    if (package.PackageType == PackageType.NuGet)
                        PushNuGetPackage(packagePath, BuildSettings.NuGetApiKey, BuildSettings.NuGetPushUrl);
                }
                catch (Exception ex)
                {
                    _context.Error(ex.Message);
                    _hadErrors = true;
                }
            }
    }

    public static void PublishToChocolatey()
    {
        if (!BuildSettings.ShouldPublishToChocolatey)
            _context.Information("Nothing to publish to Chocolatey from this run.");
        else if (CommandLineOptions.NoPush)
            _context.Information("NoPush option suppressing publication to Chocolatey");
        else
            foreach (var package in BuildSettings.Packages)
            {
                var packageName = $"{package.PackageId}.{BuildSettings.LegacyPackageVersion}.nupkg";
                var packagePath = BuildSettings.PackageDirectory + packageName;
                try
                {
                    if (package.PackageType == PackageType.Chocolatey)
                        PushChocolateyPackage(packagePath, BuildSettings.ChocolateyApiKey, BuildSettings.ChocolateyPushUrl);
                }
                catch (Exception ex)
                {
                    _context.Error(ex.Message);
                    _hadErrors = true;
                }
            }
    }

    //public static void Publish()
    //{
    //	bool hadErrors = false;

    //	foreach (var package in BuildSettings.SelectedPackages)
    //	{
    //		var packageName = $"{package.PackageId}.{BuildSettings.PackageVersion}.nupkg";
    //           var packagePath = BuildSettings.PackageDirectory + packageName;
    //		var packageType = package.PackageType;

    //		bool publishToMyGet = BuildSettings.ShouldPublishToMyGet;
    //		bool publishToNuGet = BuildSettings.ShouldPublishToNuGet && packageType == PackageType.NuGet;
    //		bool publishToChocolatey = BuildSettings.ShouldPublishToChocolatey && packageType == PackageType.Chocolatey;

    //           // If --nopush was specified, give a detailed message showing what would have been pushed
    //           if (CommandLineOptions.NoPush)
    //           {
    //               List<string> whereToPublish = new List<string>();
    //               if (publishToMyGet)
    //                   whereToPublish.Add("MyGet");
    //               if (publishToNuGet)
    //                   whereToPublish.Add("NuGet");
    //               if (publishToChocolatey)
    //                   whereToPublish.Add("Chocolatey");

    //			string destinations = string.Join(", ", whereToPublish);
    //               _context.Information($"NoPush option skipping publication of {package.PackageId} to {destinations}");
    //               continue;
    //           }

    //           try
    //           {
    //               //ApplyReleaseTagToBuild(BuildSettings.PackageVersion);

    //			if (publishToMyGet)
    //				if (packageType == PackageType.NuGet)
    //					PushNuGetPackage(packagePath, BuildSettings.MyGetApiKey, BuildSettings.MyGetPushUrl);
    //				else
    //					PushChocolateyPackage(packagePath, BuildSettings.MyGetApiKey, BuildSettings.MyGetPushUrl);

    //			if (publishToNuGet)
    //				PushNuGetPackage(packagePath, BuildSettings.NuGetApiKey, BuildSettings.NuGetPushUrl);

    //			if (publishToChocolatey)
    //				PushChocolateyPackage(packagePath, BuildSettings.ChocolateyApiKey, BuildSettings.ChocolateyPushUrl);
    //		}
    //           catch (Exception ex)
    //           {
    //               _context.Error(ex.Message);
    //               hadErrors = true;
    //           }
    //       }

    //	if (hadErrors)
    //		throw new Exception("One of the publishing steps failed.");
    //}

    //public static void ApplyReleaseTagToBuild(string releaseTag)
    //{
    //	_context.Information($"Applying release tag {releaseTag}...");

    //	string currentTag = _context.GitDescribe(BuildSettings.ProjectDirectory, GitDescribeStrategy.Tags);
    //	if (currentTag == releaseTag)
    //	{
    //		_context.Warning("  Tag already set on HEAD, possibly in a prior run.");
    //		return;
    //	}

    //	try
    //	{
    //           _context.GitTag(BuildSettings.ProjectDirectory, releaseTag);
    //           _context.Information($"  Release tagged as {releaseTag}.");

    //		if (releaseTag.Contains("-alpha."))
    //		{
    //			_context.GitPushRef(BuildSettings.ProjectDirectory, BuildSettings.GitHubOwner, BuildSettings.GitHubAccessToken, "origin", releaseTag);
    //			_context.Information($"  Release tag {releaseTag} was pushed to origin.");
    //		}
    //       }
    //       catch (Exception ex)
    //	{
    //		if (!ex.Message.ToLower().Contains("tag already exists"))
    //			throw;

    //		throw new Exception($"The {releaseTag} tag was used on an earlier commit. If no packages have been published, you may be able to remove that tag. Otherwise, you should advance the release version before proceeding.", ex);
    //	}
    //}

    /// <summary>
    /// Re-publish a symbol package after a failure. Must specify --where
    /// if more than one NuGet package exists in the current project.
    /// </summary>
    public static void PublishSymbolsPackage()
	{
		if (!BuildSettings.ShouldPublishToNuGet)
			_context.Information("Nothing to publish to NuGet from this run.");
		else if (CommandLineOptions.NoPush)
			_context.Information("NoPush option suppressing publication to NuGet");
		else
		{
			var package = GetSingleNuGetPackage();
			var packageName = $"{package.PackageId}.{BuildSettings.PackageVersion}.snupkg";
			var packagePath = BuildSettings.PackageDirectory + packageName;
			_context.NuGetPush(packagePath, new NuGetPushSettings() { ApiKey = BuildSettings.NuGetApiKey, Source = BuildSettings.NuGetPushUrl });
		}
	}

    private static PackageDefinition GetSingleNuGetPackage()
	{
        var nugetPackages = BuildSettings.SelectedPackages.Where(p => p.PackageType == PackageType.NuGet);

		switch (nugetPackages.Count())
		{
			case 0:
				throw new Exception("No NuGet packages were selected!");
			case 1:
				return nugetPackages.First();
			default:
				throw new Exception("Multiple NuGet packages found. Select only one using the --where option.");
		}
    }

	private static void PushNuGetPackage(FilePath package, string apiKey, string url)
	{
		CheckPackageExists(package);
		_context.NuGetPush(package, new NuGetPushSettings() { ApiKey = apiKey, Source = url });
	}

	private static void PushChocolateyPackage(FilePath package, string apiKey, string url)
	{
		CheckPackageExists(package);
		_context.ChocolateyPush(package, new ChocolateyPushSettings() { ApiKey = apiKey, Source = url });
	}

	private static void CheckPackageExists(FilePath package)
	{
		if (!_context.FileExists(package))
			throw new InvalidOperationException(
				$"Package not found: {package.GetFilename()}.\nCode may have changed since package was last built.");
	}

	private const string DRAFT_RELEASE_ERROR =
		"A direct call to CreateDraftRelease is permitted only:\r\n" +
		"  * On a release branch (release-x.x.x) OR\r\n" +
		"  * Using option --packageVersion to specify a release version";

	public static void CreateDraftRelease()
	{
		string releaseVersion =
			CommandLineOptions.PackageVersion.Exists 
				? CommandLineOptions.PackageVersion.Value
				: BuildSettings.IsReleaseBranch 
					? BuildSettings.BuildVersion.BranchName.Substring(8) 
					: null;

		if (releaseVersion != null)
		{
			if (CommandLineOptions.NoPush)
				_context.Information($"NoPush option skipping creation of draft release for version {releaseVersion}");
			else
			{
				string releaseName = $"{BuildSettings.Title} {releaseVersion}";
				_context.Information($"Creating draft release for {releaseName}");

				try
				{
					_context.GitReleaseManagerCreate(BuildSettings.GitHubAccessToken, BuildSettings.GitHubOwner, BuildSettings.GitHubRepository, new GitReleaseManagerCreateSettings()
					{
						Name = releaseName,
						Milestone = releaseVersion
					});
				}
				catch
				{
					_context.Error($"Unable to create draft release for {releaseName}.");
					_context.Error($"Check that there is a {releaseVersion} milestone with at least one closed issue.");
					_context.Error("");
					throw;
				}
			}
		}
		else
		{
			bool calledDirectly = CommandLineOptions.Target.Value == "CreateDraftRelease";
			if (calledDirectly)
				throw new InvalidOperationException(DRAFT_RELEASE_ERROR);
			else
				_context.Information("Skipping creation of draft release because this is not a release branch");
		}
	}

	private const string UPDATE_RELEASE_ERROR =
		"A direct call to UpdateReleaseNotes is permitted only:\r\n" +
		"  * On the main branch tagged for a production release\r\n" +
		"  * Using option --packageVersion to specify a release version";

	public static void UpdateReleaseNotes()
	{
		string releaseVersion =
			CommandLineOptions.PackageVersion.Exists 
				? CommandLineOptions.PackageVersion.Value
				: BuildSettings.ShouldPublishToGitHub 
					? BuildSettings.PackageVersion 
					: null;

		if (releaseVersion == null)
			throw new InvalidOperationException(UPDATE_RELEASE_ERROR);

		if (CommandLineOptions.NoPush)
			_context.Information($"NoPush option skipping update of release notes for version {releaseVersion}");
		else
		{
			string releaseName = $"{BuildSettings.Title} {releaseVersion}";
			_context.Information($"Updating release notes for {releaseName}");

			try
			{
				_context.GitReleaseManagerCreate(BuildSettings.GitHubAccessToken, BuildSettings.GitHubOwner, BuildSettings.GitHubRepository, new GitReleaseManagerCreateSettings()
				{
					Name = releaseName,
					Milestone = releaseVersion
				});
			}
			catch
			{
				_context.Error($"Unable to update release notes for {releaseName}.");
				_context.Error($"Check that there is a {releaseVersion} milestone with a matching release.");
				_context.Error("");
				throw;
			}
		}

	}

	public static void DownloadDraftRelease()
	{
		if (!BuildSettings.IsReleaseBranch)
			throw new Exception("DownloadDraftRelease requires a release branch!");

		string milestone = BuildSettings.BranchName.Substring(8);

		_context.GitReleaseManagerExport(BuildSettings.GitHubAccessToken, BuildSettings.GitHubOwner, BuildSettings.GitHubRepository, "DraftRelease.md",
			new GitReleaseManagerExportSettings() { TagName = milestone });
	}

	public static void CreateProductionRelease()
	{
		if (!BuildSettings.ShouldPublishToGitHub)
		{
			_context.Information("Skipping CreateProductionRelease because this is not a production release");
		}
		else if (CommandLineOptions.NoPush)
			_context.Information($"NoPush option skipping creation of production release for version {BuildSettings.PackageVersion}");
		else
		{
			string token = BuildSettings.GitHubAccessToken;
			string owner = BuildSettings.GitHubOwner;
			string repository = BuildSettings.GitHubRepository;
			string tagName = BuildSettings.PackageVersion;
            string assets = string.Join<string>(',', BuildSettings.Packages.Select(p => p.PackageFilePath));

			//IsRunningOnWindows()
            //	? $"\"{BuildSettings.NuGetPackage},{BuildSettings.ChocolateyPackage}\""
            //	: $"\"{BuildSettings.NuGetPackage}\"";

			_context.Information($"Publishing release {tagName} to GitHub");
			_context.Information($"  Assets: {assets}");

			_context.GitReleaseManagerAddAssets(token, owner, repository, tagName, assets);
			_context.GitReleaseManagerClose(token, owner, repository, tagName);
		}
	}
}
