using System.Reflection;

// Static class holding information about known extensions.
public static class KnownExtensions
{
    // Static Variables representing well-known Extensions with the latest tested version
    public static ExtensionSpecifier NUnitV2Driver = new ExtensionSpecifier(
        "NUnit.Extension.NUnitV2Driver", "nunit-extension-nunit-v2-driver", "3.9.0");
    public static ExtensionSpecifier NUnitProjectLoader = new ExtensionSpecifier(
        "NUnit.Extension.NUnitProjectLoader", "nunit-extension-nunit-project-loader", "4.0.0-dev00016");
    public static ExtensionSpecifier VSProjectLoader = new ExtensionSpecifier(
        "NUnit.Extension.VSProjectLoader", "nunit-extension-vs-project-loader", "3.9.0");
    public static ExtensionSpecifier NUnitV2ResultWriter = new ExtensionSpecifier(
        "NUnit.Extension.NUnitV2ResultWriter", "nunit-extension-nunit-v2-result-writer", "4.0.0-beta.1");
    public static ExtensionSpecifier Net462PluggableAgent = new ExtensionSpecifier(
        "NUnit.Extension.Net462PluggableAgent", "nunit-extension-net462-pluggable-agent", "4.1.0-alpha.5");
    public static ExtensionSpecifier Net80PluggableAgent = new ExtensionSpecifier(
        "NUnit.Extension.Net80PluggableAgent", "nunit-extension-net80-pluggable-agent", "4.1.0-alpha.6");
    public static ExtensionSpecifier Net90PluggableAgent = new ExtensionSpecifier(
        "NUnit.Extension.Net90PluggableAgent", "nunit-extension-net90-pluggable-agent", "4.1.0-alpha.4");

    private static ExtensionSpecifier[] BundledAgents =>
    [
        Net462PluggableAgent,
        Net80PluggableAgent,
        Net90PluggableAgent
    ];

    public static IEnumerable<PackageReference> BundledNuGetAgents =>
        BundledAgents.Select(a => a.NuGetPackage);

    public static IEnumerable<PackageReference> BundledChocolateyAgents =>
        BundledAgents.Select(a => a.ChocoPackage);
}

Task("InstallNuGetAgents")
    .Description("Installs just the NuGet agents we bundle with the GUI runner in the BIN  directory for testing.")
    .Does(() =>
    {
        foreach (var agent in KnownExtensions.BundledNuGetAgents)
            agent.Install(BuildSettings.ProjectDirectory + "bin");
    });

Task("InstallChocolateyAgents")
    .Description("Installs just the Chocolatey agents we bundle with the GUI runner in the BIN  directory for testing.")
    .Does(() =>
    {
        foreach (var agent in KnownExtensions.BundledNuGetAgents)
            agent.Install(BuildSettings.ProjectDirectory + "bin");
    });
