//////////////////////////////////////////////////////////////////////
// EXECUTION
//////////////////////////////////////////////////////////////////////

public Builder Build
{
    get
    {
        if (CommandLineOptions.Usage)
            return new Builder(() => Information(HelpMessages.Usage));

        if (CommandLineOptions.Targets.Values.Count() == 1)
            return new Builder(() => RunTarget(CommandLineOptions.Target.Value));

        return new Builder(() => RunTargets(CommandLineOptions.Targets.Values));
    }
}

CakeReport RunTargets(ICollection<string> targets)
    => RunTarget(GetOrAddTargetsTask(targets).Name);

Task<CakeReport> RunTargetsAsync(ICollection<string> targets)
    => RunTargetAsync(GetOrAddTargetsTask(targets).Name);

private ICakeTaskInfo GetOrAddTargetsTask(ICollection<string> targets)
{
    var targetsTaskName = string.Join('+', targets);
    var targetsTask = Tasks.FirstOrDefault(task => task.Name.Equals(targetsTaskName, StringComparison.OrdinalIgnoreCase));

    if (targetsTask == null)
    {
        var task = Task(targetsTaskName);

        foreach(var target in targets)
            task.IsDependentOn(target);

        targetsTask = task.Task;
    }

    return targetsTask;
}

public class Builder
{
    private Action _action;

    public Builder(Action action)
    {
        _action = action;
    }

    public void Run()
    {
        _action();
    }
}

//////////////////////////////////////////////////////////////////////
// SETUP
//////////////////////////////////////////////////////////////////////

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
        DisplayTaskErrorAndThrow("MyGet ApiKey is required but was not set.");

    // NuGet Api Key
    bool needNuGetApiKey = tasksToExecute.Contains("PublishToNuGet") && BuildSettings.ShouldPublishToNuGet && !CommandLineOptions.NoPush;
    if (needNuGetApiKey && string.IsNullOrEmpty(BuildSettings.NuGetApiKey))
        DisplayTaskErrorAndThrow("NuGet ApiKey is required but was not set.");

    // Chocolatey Api Key
    bool needChocolateyApiKey = tasksToExecute.Contains("PublishToChocolatey") && BuildSettings.ShouldPublishToChocolatey && !CommandLineOptions.NoPush;
    if (needChocolateyApiKey && string.IsNullOrEmpty(BuildSettings.ChocolateyApiKey))
        DisplayTaskErrorAndThrow("Chocolatey ApiKey is required but was not set.");

    // GitHub Access Token, Owner and Repository
    if (!CommandLineOptions.NoPush)
        if (tasksToExecute.Contains("CreateDraftRelease") && BuildSettings.IsReleaseBranch ||
            tasksToExecute.Contains("CreateProductionRelease") && BuildSettings.ShouldPublishToGitHub)
        {
            if (string.IsNullOrEmpty(BuildSettings.GitHubAccessToken))
                DisplayTaskErrorAndThrow("GitHub Access Token is required but was not set.");
            if (string.IsNullOrEmpty(BuildSettings.GitHubOwner))
                DisplayTaskErrorAndThrow("GitHub Owner is required but was not set.");
            if (string.IsNullOrEmpty(BuildSettings.GitHubRepository))
                DisplayTaskErrorAndThrow("GitHub Repository is required but was not set.");
        }

    // SelectedPackages
    string id = CommandLineOptions.PackageId.Value;
    string type = CommandLineOptions.PackageType.Value?.ToLower();

    foreach (var package in BuildSettings.Packages)
        if ((id == null || id == package.PackageId.ToString()) && (type == null || type == package.PackageType.ToString().ToLower()))
            BuildSettings.SelectedPackages.Add(package);

    if (BuildSettings.SelectedPackages.Count == 0)
    {
        if (id == null && type == null)
            DisplayErrorAndThrow("No packages have been defined");

        string msg = "No package found ";
        if (type != null)
            msg += $" of type {type}";
        if (id != null)
            msg += $" with id ${id}";

        DisplayErrorAndThrow(msg);
    }

    // Add settings to BuildSettings
    BuildSettings.Target = target;
    BuildSettings.TasksToExecute = tasksToExecute;

    void DisplayErrorAndThrow(string message)
    {
        context.Error(message);
        throw new Exception(message);
    }

    void DisplayTaskErrorAndThrow(string message)
    {
        DisplayErrorAndThrow(message + $"\r\n  Tasks: {string.Join(", ", tasksToExecute)}");
    }
});