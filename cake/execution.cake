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

CommandLineOptions.Initialize(Context);

public static class CommandLineOptions
{
    private const string DEFAULT_CONFIGURATION = "Release";

    static private ICakeContext _context;

    static public ValueOption<string> Target;
    static public MultiValueOption<string> Targets;

    static public ValueOption<string> Configuration;
    static public ValueOption<string> PackageVersion;
    static public ValueOption<string> PackageId;
    static public ValueOption<string> PackageType;
    static public ValueOption<int> TestLevel;
    static public ValueOption<string> TraceLevel;
    static public SimpleOption NoBuild;
    static public SimpleOption NoPush;
    static public SimpleOption Usage;

    public static void Initialize(ICakeContext context)
    {
        _context = context;

        // The name of the TARGET task to be run, e.g. Test.
        Target = new ValueOption<string>("target|t", "Default");

        // Multiple targets to be run
        Targets = new MultiValueOption<string>("target|t", "Default");

        Configuration = new ValueOption<String>("configuration|c", DEFAULT_CONFIGURATION);

        PackageVersion = new ValueOption<string>("packageVersion", null);

        PackageId = new ValueOption<string>("packageId|id", null);

        PackageType = new ValueOption<string>("packageType|type", null);

        TestLevel = new ValueOption<int>("level|lev", 0);

        TraceLevel = new ValueOption<string>("trace|tr", "Off");

        NoBuild = new SimpleOption("nobuild|nob");

        NoPush = new SimpleOption("nopush|nop");

        Usage = new SimpleOption("usage");
    }

    // Nested classes to represent individual options

    // AbstractOption has a name and can tell us if it exists.
    public abstract class AbstractOption
    {
        public List<string> Aliases { get; }

        public bool Exists
        {
            get
            {
                foreach (string alias in Aliases)
                    if (_context.HasArgument(alias))
                        return true;
                return false;
            }
        }

        public string Description { get; }

        public AbstractOption(string aliases, string description = null)
        {
            Aliases = new List<string>(aliases.Split('|'));
            Description = description;
        }
    }

    // Simple Option adds an implicit boolean conversion operator.
    // It throws an exception if you gave it a value on the command-line.
    public class SimpleOption : AbstractOption
    {
        static public implicit operator bool(SimpleOption o) => o.Exists;

        public SimpleOption(string aliases, string description = null)
            : base(aliases, description)
        {
            foreach (string alias in Aliases)
                if (_context.Argument(alias, (string)null) != null)
                    throw new Exception($"Option --{alias} does not take a value.");
        }
    }

    // Generic ValueOption adds Value as well as a default value
    public class ValueOption<T> : AbstractOption
    {
        public T DefaultValue { get; }

        public ValueOption(string aliases, T defaultValue, string description = null)
            : base(aliases, description)
        {
            DefaultValue = defaultValue;
        }

        public T Value
        {
            get
            {
                foreach (string alias in Aliases)
                    if (_context.HasArgument(alias))
                        return _context.Argument<T>(alias);

                return DefaultValue;
            }
        }
    }

    // Generic MultiValueOption adds Values, which returns a collection of values
    public class MultiValueOption<T> : ValueOption<T>
    {
        public MultiValueOption(string aliases, T defaultValue, string description = null)
            : base(aliases, defaultValue, description) { }

        public ICollection<T> Values
        {
            get
            {
                var result = new List<T>();

                foreach (string alias in Aliases)
                    if (_context.HasArgument(alias))
                        result.AddRange(_context.Arguments<T>(alias));

                if (result.Count == 0) result.Add(DefaultValue);

                return result;
            }
        }
    }
}

static public class HelpMessages
{
    static public string Usage => $"""
        BUILD.CAKE

        This script builds the {BuildSettings.Title} project. It makes use of
        NUnit.Cake.Recipe, which provides a number of built-in options and
        tasks. You may define additional options and tasks in build.cake or
        in additional cake files you load from build.cake.

        Usage: build [options]

        Options:

            --target, -t=TARGET
                The TARGET task to be run, e.g. Test. Default is Build. This option
                may be repeated to run multiple targets. For a list of supported
                targets, use the Cake `--description` option.

            --configuration, -c=CONFIG
                The name of the configuration to build. Default is Release.

            --packageVersion=VERSION
                Specifies the full package version, including any pre-release
                suffix. If provided, this version is used directly in place of
                the default version calculated by the script.

            --packageId, --id=ID
                Specifies the id of a package for which packaging is to be performed.
                If not specified, all ids are processed.

            --packageType, --type=TYPE
                Specifies the type package for which packaging is to be performed.
                Valid values for TYPE are 'nuget' and 'choco'.
                If not specified, both types are processed.

            --level, --lev=LEVEL
                Specifies the level of package testing, 1, 2 or 3. Defaults are
                  1 = for normal CI tests run every time you build a package
                  2 = for PRs and Dev builds uploaded to MyGet
                  3 = prior to publishing a release

            --trace, --tr=LEVEL
                Specifies the default trace level for this run. Values are Off,
                Error, Warning, Info or Debug. Default is Off. If used, this option
                affects the trace level for both unit and package tests.

            --nobuild, --nob
                Indicates that the Build task should not be run even if other
                tasks depend on it. The existing build is used instead.

            --nopush, --nop
                Indicates that no publishing or releasing should be done. If
                publish or release targets are run, a message is displayed.

            --usage

                Displays this help message. No targets are run.

        Selected Cake Options:
            
            --version
                Displays the cake version in use.

            --description
                Displays a list of the available tasks (targets).

            --tree
                Displays the task dependency tree

            --help
                Displays help information for cake itself.

            NOTE: The above Cake options bypass execution of the script.
        """;
}
