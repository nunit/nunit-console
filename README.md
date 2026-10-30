# NUnit 4 Console and Engine

The NUnit Console Runner and Engine are used for executing NUnit tests with a wide variety of options. Version 4, the next major release, is currently in beta, with many new features. The primary package, `NUnit.ConsoleRunner`, contains the Console Runner and the NUnit Engine, which is used to execute tests. The following table shows the latest NuGet releases, including pre-releases for the Console Runner, Engine, and all support packages and agents. Packages on NuGet are also on Myget, but MyGet includes additonal intermediate releases used in the development process.

<div style="position: absolute; left: 40em; max-width: 30em; border: 2px solid black">
    <p style="font-weight: bold; text-align: center; background-color: #e0e0e0; border-bottom: 2px solid black; padding: 8px">Note to V4 Developerment Team</p>
    <p style="text-wrap: wrap; padding: 0 8px;">The values in the table show the latest versions of each package.</p>
    <p style="text-wrap: wrap; padding: 0 8px;">Depending on the particular package, we may wish to use the latest stable verson, the latest nuget pre-release or the latest myget pre-release.</p>
    <p style="text-wrap: wrap; padding: 0 8px;">Work is underway to allow enable automatic version selection in our scripts, but for now it's up to us.</p>
</div>

|  | Latest Versions |  |
| ---: | --- | --- |
| ![Static Badge](https://img.shields.io/badge/Console,%20Common%20and%20Engine--black?labelColor=black&color=black) | ![NuGet Version](https://img.shields.io/nuget/vpre/NUnit.ConsoleRunner.svg) | ![MyGet Version](https://img.shields.io/myget/nunit/vpre/NUnit.ConsoleRunner?label=myget)
| ![Static Badge](https://img.shields.io/badge/Engine%20API--black?labelColor=black&color=black) | ![NuGet Version](https://img.shields.io/nuget/vpre/NUnit.Engine.Api.svg) | ![MyGet Version](https://img.shields.io/myget/nunit/vpre/NUnit.Engine.Api?label=myget)
| ![Static Badge](https://img.shields.io/badge/Extensibility%20and%20Extensibility%20API--black?labelColor=black&color=black) | ![NuGet Version](https://img.shields.io/nuget/vpre/NUnit.Extensibility.svg) | ![MyGet Version](https://img.shields.io/myget/nunit/vpre/NUnit.Extensibility?label=myget)
| ![Static Badge](https://img.shields.io/badge/Agent%20Core--black?labelColor=black&color=black) | ![NuGet Version](https://img.shields.io/nuget/vpre/NUnit.Agent.Core.svg) | ![MyGet Version](https://img.shields.io/myget/nunit/vpre/NUnit.Agent.Core?label=myget)
| ![Static Badge](https://img.shields.io/badge/.NET%204.6.2%20Agent--black?labelColor=black&color=black) | ![NuGet Version](https://img.shields.io/nuget/vpre/NUnit.Extension.Net462PluggableAgent.svg) | ![MyGet Version](https://img.shields.io/myget/nunit/vpre/NUnit.Extension.Net462PluggableAgent?label=myget)
| ![Static Badge](https://img.shields.io/badge/.NET%208.0%20Agent--black?labelColor=black&color=black) | ![NuGet Version](https://img.shields.io/nuget/vpre/NUnit.Extension.Net80PluggableAgent.svg) | ![MyGet Version](https://img.shields.io/myget/nunit/vpre/NUnit.Extension.Net80PluggableAgent?label=myget)
| ![Static Badge](https://img.shields.io/badge/.NET%209.0%20Agent--black?labelColor=black&color=black) | ![NuGet Version](https://img.shields.io/nuget/vpre/NUnit.Extension.Net90PluggableAgent.svg) | ![MyGet Version](https://img.shields.io/myget/nunit/vpre/NUnit.Extension.Net90PluggableAgent?label=myget)
| ![Static Badge](https://img.shields.io/badge/.NET%2010.0%20Agent--black?labelColor=black&color=black) | ![NuGet Version](https://img.shields.io/nuget/vpre/NUnit.Extension.Net10PluggableAgent.svg) | ![MyGet Version](https://img.shields.io/myget/nunit/vpre/NUnit.Extension.Net10PluggableAgent?label=myget)

## Table of Contents

- [Downloads](#downloads)
- [Documentation](#documentation)
- [Contributing](#contributing)
- [License](#license)
- [NUnit Projects](#nunit-projects)

## Downloads

The latest stable release of the NUnit Console is [available on NuGet](https://www.nuget.org/packages/NUnit.ConsoleRunner/), [Chocolatey](https://chocolatey.org/packages/nunit-console-runner), or can be [downloaded from GitHub](https://github.com/nunit/nunit-console/releases). Pre-release builds are [available on MyGet](https://www.myget.org/feed/nunit/package/nuget/NUnit.ConsoleRunner).

The Console/Engine are available in various packages:
- [NUnit.ConsoleRunner](https://www.nuget.org/packages/NUnit.ConsoleRunner/): The NUnit Console itself, with no extensions. It uses the NUnit engine and pluggable agents to execute tests. Also available as a [Chocolatey package](https://community.chocolatey.org/packages/nunit-console-runner).
- [NUnit.ConsoleRunner.NetCore](https://www.nuget.org/packages/NUnit.ConsoleRunner.NetCore/): A version of the runner built for .NET 10, also using the NUnit engine and pluggable agents to execute tests.
- [NUnit.Engine](https://www.nuget.org/packages/NUnit.Engine/) & [NUnit.Engine.Api](https://www.nuget.org/packages/NUnit.Engine.Api/): Packages intended for custom runners integrating directly with the NUnit Engine. 

Development builds of all packages are available from our MyGet feed at https://www.myget.org/feed/Packages/nunit

## Documentation

Documentation for all NUnit projects are available at [https://docs.nunit.org/](https://docs.nunit.org/).

## Contributing

For more information on contributing to the NUnit project, please see [CONTRIBUTING.md](https://github.com/nunit/nunit-console/blob/master/CONTRIBUTING.md) and the [Developer Docs](https://github.com/nunit/docs/wiki/Team-Practices#technical-practices).

Contributors to version 4.0 include [Charlie Poole](https://github.com/CharliePoole), [Chris Maddock](https://github.com/ChrisMaddock), [Manfred Brands](https://github.com/manfred-brands), [Steven Werdenburg](https://github.com/stevenaw), [Mikkel Nylander Bundgaard](https://github.com/mikkelbu), [Rob Prouse](https://github.com/rprouse), [Joseph Musser](https://github.com/jnm2), [Ben Randall](https://github.com/veleek) and numerous community contributors.

The previous version 3.0 was created by [Charlie Poole](https://github.com/CharliePoole), [Rob Prouse](https://github.com/rprouse), [Simone Busoli](https://github.com/simoneb), [Neil Colvin](https://github.com/oznetmaster) and numerous community contributors.

Earlier versions of NUnit were developed by James W. Newkirk, Alexei A. Vorontsov, Michael C. Two and Charlie Poole (version 2.0), and Philip A. Craig (version 1.0).

A complete list of contributors since the nunit-console repository was created can be [found on GitHub](https://github.com/nunit/nunit-console/graphs/contributors).

## License

NUnit is Open Source software and the NUnit 4 Console Runner and Engine are released under the [MIT license](https://github.com/nunit/docs/wiki/License). Releases earlier than version 3.0 used the [NUnit license](http://www.nunit.org/nuget/license.html). Both of these licenses allow the use of NUnit in free and commercial applications and libraries without restrictions.
