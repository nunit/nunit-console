# Building NUnit 3 Console and Engine

NUnit 3 consists of three separate layers: the Framework, the Engine and the Console Runner. This repository contains the Engine and Console Runner. The source code is kept in the GitHub repository at https://github.com/nunit/nunit-console. Source for the framework can be found at https://github.com/nunit/nunit

Note that assemblies in one layer must not reference those in any other layer, except as follows:
 * The console runner references the nunit.engine.api assembly, but not the nunit.engine assembly.
 * Tests in any layer reference nunit.framework.
Developers should make sure not to introduce any other references.

There are two ways to build NUnit: using the solution file in an IDE or through the build script.
See also [Building and testing for Linux on a Windows machine](#building-and-testing-for-linux-on-a-windows-machine).

## Prerequisites

- Visual Studio 2022 or newer to build on Windows
- .NET 8.0 or newer

## Solution Build

All projects are built together using a single Visual Studio solution NUnitConsole.sln, which may be
built with Visual Studio or on the command line using Cake. The projects all place their output in
a common bin directory.

The solution build is useful for verifying that everything builds correctly but doesn't create packages or run any tests. Developers working on this project are advised to run the build script periodically.

## Build Script

We use **Cake** (https://cakebuild.net) to build NUnit for distribution. Our standard script targets are all wrapped in what **Cake** calls a recipe. `NUnit.Cake.Recpe` is currently used for building the runner, engine and a number of our extensions. To see all the various targets and options our recipe supports, enter `build --usage` at the command-line. **Note:** We cannot use `--help` because **Cake** uses that option for it's own help.

The primary script that sets our options for building, running tests and packaging is `build.cake`. To reduce the size of `build.cake`, our package test definitions are included from a separate file, `package-tests.cake`. There are two places in the script where we specify options.

 1. In the call to `BuildSettings.Initialize`, general options for he entire build are given. Eventually, we hope to add a help screen that explains the available options. At this time, you will need to examine the [source code](https://github.com/nunit/NUnit.Cake.Recipe/blob/main/recipe/build-settings.cake). 
 2. Our packages are all defined in `build.cake` by instantiating objects of type `NuGetPackage` or `ChocolateyPackage`. Once again, for documentation of available constructor arguments, you should examine the code for [NuGetPackage](https://github.com/nunit/NUnit.Cake.Recipe/blob/main/recipe/nuget-package.cake) and [ChocolatePackage](https://github.com/nunit/NUnit.Cake.Recipe/blob/main/recipe/chocolatey-package.cake).

The build is normally started by running `build` or `build.ps1` on Windows or `build.sh` on Linux. The three comands are functionally identical. They check that the necessary software is installed and then run `dotnet cake`. When running on a local machine where you know that everything is properly installed, you may use `dotnet cake` directly but it should not be used on uninitialized build agents.
 
For development purposes, the usual commands to run are...
 * `build` alone, to simply compile everything.
 * `build -t Test` to compile and run unit tests.
 * `build -t Package` to compile, create packages and test them.
 * `build -t Test -t Package` to compile, unit test and create and test packages.

### Building and testing for Linux on a Windows machine

> _**NOTE:** Instructions in this section have not yet been verified on the latest build of the console and engine.
If you find problems, please file an issue._

Most of the time, it's not necessary to build or run tests on platforms other than your primary
platform. The continuous integration which runs on every PR is enough to catch any problems.

Once in a while you may find it desirable to be primarily developing the repository on a Windows
machine but to run Linux tests on the same set of files while you edit them in Windows.
One convenient way to do this is to pass the same arguments to
[build-mono-docker.ps1](.\build-mono-docker.ps1) that you would pass to build.ps1. It requires
[Docker](https://docs.docker.com/docker-for-windows/install/) to be installed.

For example, to build and test everything: `.\build-mono-docker.ps1 -t test`

This will build a docker image and run a temporary container
based on the [Mono image](https://hub.docker.com/r/library/mono/) and adding in
.NET Core. The script mounts the repo inside the container and executes the
[build.sh](build.sh) Cake bootstrapper with the arguments you specify.

The first build will be slow as it builds the new image, but subsequent runs will
be much quicker.
