# Building NUnit 3 Console and Engine

NUnit 3 consists of three separate layers: the Framework, the Engine and the Console Runner. This
repository contains the Engine and Console Runner. The source code is kept in the GitHub repository
at https://github.com/nunit/nunit-console. Source for the framework can be found
at https://github.com/nunit/nunit

Note that assemblies in one layer must not reference those in any other layer, except as follows:
 * The console runner references the nunit.engine.api assembly, but not the nunit.engine assembly.
 * Tests in any layer reference nunit.framework.
Developers should make sure not to introduce any other references.

There are two ways to build NUnit: using the solution file in an IDE or through the build script.
See also [Building and testing for Linux on a Windows machine](#building-and-testing-for-linux-on-a-windows-machine).

## Prerequisites

- Visual Studio 2017 Update 5 or newer to build on Windows. Preferably use VS2022.
- .NET 4.5+ or Mono 5.10.0+
- A set of runtimes, from .NET Core 2.1.30 runtime, and some more, see below. You will also need the net framework 3.5 service pack 1 installed.
- An appropriate newer dotnet SDK, e.g. .net 6. 
- Chocolatey  : [Install instructions here](https://docs.chocolatey.org/en-us/choco/setup)
- Wix: [Install instructions here](https://wixtoolset.org/docs/wix3/). Note that you need to install both the [3.11 toolset](https://github.com/wixtoolset/wix3/releases/tag/wix3112rtm) (Use the wix311.exe installer)  and the [VS2022 Vsix extension](https://marketplace.visualstudio.com/items?itemName=WixToolset.WixToolsetVisualStudio2022Extension).

## Solution Build

All projects are built together using a single Visual Studio solution NUnitConsole.sln, which may be
built with Visual Studio or on the command line using Cake. The projects all place their output in
a common bin directory.

The easiest way is to use the command line with just `build`.  If that works, you can test it doing `build -t test` and then create a package doing `build -t package`.
On each step you may get some errors, mostly due to missing frameworks. Install whatever is required. We will work on getting a complete list up.
The generated packages can be found in the `package` subfolder.

## Setting a new version

Before building a new package, ensure you set the appropriate (according to [Semver](https://semver.org) ) next version number to be used for the final package. Note that you dont need to set any prerelase tags, those will be done automatically based on whatever non-main branch you are building from.
You set that version number in the 1st line of the `gitversion.yml` file.

## Build Script

We use **Cake** (http://cakebuild.net) to build NUnit for distribution. The primary script that controls
building, running tests and packaging is build.cake. We modify build.cake when we need to add new
targets or change the way the build is done. Normally build.cake is not invoked directly but through
build.ps1 (on Windows) or build.sh (on Linux). These two scripts are provided by the Cake project
and ensure that Cake is properly installed before trying to run the cake script. This helps the
build to work on CI servers using newly created agents to run the build and we generally run it
the same way on our own machines.

Key arguments to build.cmd / build.ps1 / build.sh :
 * --target=<task>                 The task to run (default is Build)
 * --configuration=[Release|Debug] The configuration to use (default is Release)
 
The build.cake script contains a large number of interdependent tasks. The most
important top-level tasks to use are listed here:

```
 * Build               Builds everything. This is the default if no target is given.
 * Test                Runs all unit tests. Dependent on Build.
 * Package             Builds, Verifies and Tests all packages. Dependent on Build.
```

### Building and testing for Linux on a Windows machine

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
