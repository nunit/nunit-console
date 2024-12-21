# NUnit 4 Console and Engine


[![Follow NUnit](https://img.shields.io/twitter/follow/nunit.svg?style=social)](https://twitter.com/nunit) [![Slack](https://img.shields.io/badge/chat-on%20Slack-brightgreen)](https://join.slack.com/t/nunit/shared_invite/zt-jz58jw68-Led8y3WH4n2a~Y5WjuOpKA)

The NUnit Console Runner and Engine are used for executing NUnit tests with a wide variety of options. Version 4 is the latest major release, with many new features. in a bais a unit-testing framework for all .NET languages. Initially ported from JUnit, the current production release, version 3, has been completely rewritten with many new features and support for a wide range of .NET platforms.

## Table of Contents

- [Downloads](#downloads)
- [Documentation](#documentation)
- [Contributing](#contributing)
- [License](#license)
- [NUnit Projects](#nunit-projects)

## Downloads

The latest stable release of the NUnit Console is [available on NuGet](https://www.nuget.org/packages/NUnit.ConsoleRunner/), [Chocolatey](https://chocolatey.org/packages/nunit-console-runner), or can be [downloaded from GitHub](https://github.com/nunit/nunit-console/releases). Pre-release builds are [available on MyGet](https://www.myget.org/feed/nunit/package/nuget/NUnit.ConsoleRunner).

The Console/Engine are available in various packages:
- [NUnit.ConsoleRunner](https://www.nuget.org/packages/NUnit.ConsoleRunner/): The NUnit Console itself, with no extensions. Also available as a [Chocolatey package](https://community.chocolatey.org/packages/nunit-console-runner).
- [NUnit.ConsoleRunner.NetCore](https://www.nuget.org/packages/NUnit.ConsoleRunner.NetCore/): A version of the runner built for .NET 8, which runs .NET Core tests in process.
- [NUnit.Console](https://www.nuget.org/packages/NUnit.Console/): The NUnit Console, with key extensions additionally packaged.
- [NUnit.Engine](https://www.nuget.org/packages/NUnit.Engine/) & [NUnit.Engine.Api](https://www.nuget.org/packages/NUnit.Engine.Api/): Packages intended for custom runners integrating directly with the NUnit Engine. 

Development builds of all packages are available from our MyGet feed at https://www.myget.org/feed/Packages/nunit
## Documentation

Documentation for all NUnit projects are available at [https://docs.nunit.org/](https://docs.nunit.org/).

## Contributing

For more information on contributing to the NUnit project, please see [CONTRIBUTING.md](https://github.com/nunit/nunit-console/blob/master/CONTRIBUTING.md) and the [Developer Docs](https://github.com/nunit/docs/wiki/Team-Practices#technical-practices).

NUnit 3.0 was created by [Charlie Poole](https://github.com/CharliePoole), [Rob Prouse](https://github.com/rprouse), [Simone Busoli](https://github.com/simoneb), [Neil Colvin](https://github.com/oznetmaster) and numerous community contributors. A complete list of contributors since the nunit-console repository was created can be [found on GitHub](https://github.com/nunit/nunit-console/graphs/contributors).

Earlier versions of NUnit were developed by Charlie Poole, James W. Newkirk, Alexei A. Vorontsov, Michael C. Two and Philip A. Craig.

## License

NUnit is Open Source software and the NUnit 4 Console Runner and Engine are released under the [MIT license](https://github.com/nunit/docs/wiki/License). Earlier releases used the [NUnit license](http://www.nunit.org/nuget/license.html). Both of these licenses allow the use of NUnit in free and commercial applications and libraries without restrictions.

