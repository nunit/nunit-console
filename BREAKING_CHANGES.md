# Breaking Changes in Version 4.0

As a major release, version 4 contains a number of breaking changes in addition to new features and enhancements.

## Console Runner
These changes affect anyone who runs tests using the console runner.

### Renamed Runner
The console executable is now `nunit-console.exe` rather than `nunit3-console.exe`.

### Packaging
The zip package has been eliminated. Only nuget and chocolatey packages are provided.

### Pluggable Agents
There are no longer built-in agents. All agents are now pluggable extensions and may be installed and uninstalled separately. In order to simplify things for the general user, the console runner comes with several bundled agents. With the beta.1 release, the following agents are included:

* .NET Framework 4.6.2 Pluggable Agent
* .NET 8.0 Pluggable Agent
* .NET 9.0 Pluggable Agent

Changing the set of agents bundled will not be considered a breaking change in the future but may be done on any minor version change. This is because users are free to install and uninstall agents at any time.

This change also impacts developers. See "Pluggable Agents" in the Engine section.

### Command-Line Options
Various options have been removed or changed in some way. 

* The `--domain` option is no longer available.
* The options `--process` and `--inprocess` are no longer available.
* The deprecated `-params` option has been removed.
* The `--timeout` option has been changed to `--testCaseTimeout`.
* Deprecated value `All` for the `--labels` option has been removed: 

### Bundled Extensions
The following extensions are no longer bundled in the `NUnit.Console` package:
* TeamCity extension (now managed separately by JetBrains).
* V2 Result Writer
* V2 Framework Driver

V2 Result Writer and Framework Driver may be reinstated in a future release if there is demand for them.

### TestPackage
The constructor for `TestPackage(string fileName)` no longer exists. Instead, `new TestPackage("test.dll")` now calls `TestPackage(params string[] testFile)`. This means that users are generally only able to create top-level anonymous TestPackages. Named sub-packages can be created using the `AddSubPackage` method. Older code using the string constructor will need to be rewritten.

### Platform Support
The console runner now runs under .NET 4.6.2 and .NET 8.0. This may be a breaking change for some users migrating from earlier V3 releases. Agents are available to run tests under .NET 4.6.2, .NET 8.0 and .NET 9.0. Agents for .NET Core 2.1, .NET Core 3.1, .NET 5.0 and .NET 7.0 are no longer provided.

## Engine
These changes affect people who create their own runners, calling the engine directly. They may also affect the authors of extensions.

### TestEngineActivator
The ``TestEngineActivator` class has been removed. Authors of runners should reference the NUnit.Engine package and use new to create an instance of `TestEngine`.

### Engine Services
The `UserSettings` and `RecentFiles` services have been removed from the engine. This is only a breaking change for authors of 3rd party runners using those engine services. Runners should manage their own settings, which is what the console runner now does, making the change transparent to users of the runner.

### Pluggable Agents
The engine no longer comes with any agents. Authors of runners should reference packages for any agents they wish to use along with the engine itself.
