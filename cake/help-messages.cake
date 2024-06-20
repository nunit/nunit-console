static public class HelpMessages
{
	static public string Usage => $"""
        BUILD.CAKE

        This script builds the {BuildSettings.Title} project. It makes use of
        NUnit.Cake.Recipe, which provides a number of built-in options and
        tasks. You may define additional options and tasks in build.cake or
        in additional cake files you load from build.cake. Recipe options may 
        be specified in abbreviated form, down to the minimum length shown in
        square braces. Single character abbreviations use a single dash.

        Usage: build [options]

        Options:

            --target=TARGET         [-t]
                The TARGET task to be run, e.g. Test. Default is Build.

            --configuration=CONFIG  [-c]
                The name of the configuration to build. Default is Release.

            --packageVersion        [--pack]
                Specifies the full package version, including any pre-release
                suffix. If provided, this version is used directly in place of
                the default version calculated by the script.

            --where=SELECTION       [-w]
                Specifies a selction expression used to choose which packages
                to build and test, for use in debugging. Consists of one or
                more specifications, separated by '|' and '&'. Each specification
                is of the form "prop=value", where prop may be either id or type.
                Examples:
                    --where type=msi
                    --where id=NUnit.Engine.Api
                    --where "type=msi|type=zip"

            --level=LEVEL           [--lev]
                Specifies the level of package testing, 1, 2 or 3. Defaults are
                  1 = for normal CI tests run every time you build a package
                  2 = for PRs and Dev builds uploaded to MyGet
                  3 = prior to publishing a release

            --trace=LEVEL           [--tr]
                Specifies the default trace level for this run. Values are Off,
                Error, Warning, Info or Debug. Default is value of environment
                variable NUNIT_INTERNAL_TRACE_LEVEL if set. If not,
                tracing is turned Off.

            --nobuild               [--nob]
                Indicates that the Build task should not be run even if other
                tasks depend on it. The existing build is used instead.

            --nopush                [--nop]
                Indicates that no publishing or releasing should be done. If
                publish or release targets are run, a message is displayed.

            --usage                 [--us]
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
