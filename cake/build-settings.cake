#load ./ci.cake
#load ./packaging.cake
#load ./package-checks.cake
#load ./test-results.cake
#load ./package-tests.cake
#load ./package-tester.cake
#load ./header-check.cake
#load ./local-tasks.cake

public class BuildSettings
{
    public BuildSettings(ISetupContext context)
    {
        if (context == null)
            throw new System.ArgumentNullException(nameof(context));

        Target = context.TargetTask.Name;
        Configuration = context.Argument("configuration", "Release");
    }

    public string Target { get; }
    public string Configuration { get; }
}