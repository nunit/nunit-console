Setup((context) =>
{  
	var target = context.TargetTask.Name;
	var tasksToExecute = context.TasksToExecute.Select(t => t.Name);

	// Ensure that BuildSettings have been initialized
	if (BuildSettings.Context == null)
		throw new Exception("BuildSettings have not been initialized. Call BuildSettings.Initialize() from your build.cake script.");

	// Ensure Api Keys and tokens are available if needed for tasks to be executed

	// MyGet Api Key
	bool needMyGetApiKey = tasksToExecute.Contains("PublishToMyGet") && BuildSettings.ShouldPublishToMyGet && !CommandLineOptions.NoPush;
	if (needMyGetApiKey && string.IsNullOrEmpty(BuildSettings.MyGetApiKey))
		DisplayErrorAndThrow("MyGet ApiKey is required but was not set.");

	// NuGet Api Key
	bool needNuGetApiKey = tasksToExecute.Contains("PublishToNuGet") && BuildSettings.ShouldPublishToNuGet && !CommandLineOptions.NoPush;
	if (needNuGetApiKey && string.IsNullOrEmpty(BuildSettings.NuGetApiKey))
		DisplayErrorAndThrow("NuGet ApiKey is required but was not set.");

	// Chocolatey Api Key
	bool needChocolateyApiKey = tasksToExecute.Contains("PublishToChocolatey") && BuildSettings.ShouldPublishToChocolatey && !CommandLineOptions.NoPush;
	if (needChocolateyApiKey && string.IsNullOrEmpty(BuildSettings.ChocolateyApiKey))
		DisplayErrorAndThrow("Chocolatey ApiKey is required but was not set.");

	// GitHub Access Token, Owner and Repository
	if (!CommandLineOptions.NoPush)
		if (tasksToExecute.Contains("CreateDraftRelease") && BuildSettings.IsReleaseBranch ||
			tasksToExecute.Contains("CreateProductionRelease") && BuildSettings.IsProductionRelease)
		{
			if (string.IsNullOrEmpty(BuildSettings.GitHubAccessToken))
				DisplayErrorAndThrow("GitHub Access Token is required but was not set.");
			if (string.IsNullOrEmpty(BuildSettings.GitHubOwner))
				DisplayErrorAndThrow("GitHub Owner is required but was not set.");
			if (string.IsNullOrEmpty(BuildSettings.GitHubRepository))
				DisplayErrorAndThrow("GitHub Repository is required but was not set.");
		}
	
	// Add settings to BuildSettings
	BuildSettings.Target = target;
	BuildSettings.TasksToExecute = tasksToExecute;

	void DisplayErrorAndThrow(string message)
	{
		message += $"\r\n  Tasks: {string.Join(", ", tasksToExecute)}";

		context.Error(message);
		throw new Exception(message);
	}
});
