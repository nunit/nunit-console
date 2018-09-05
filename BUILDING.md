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

- Visual Studio 2017 Update 5 or newer to build on Windows
- .NET 4.5+ or Mono 5.10.0+
- .NET Core 1.1.6 or newer

## Solution Build

All projects are built together using a single Visual Studio solution NUnitConsole.sln, which may be
built with Visual Studio or on the command line using Cake. The projects all place their output in
a common bin directory.

## Build Script

We use **Cake** (http://cakebuild.net) to build NUnit for distribution. The primary script that controls
building, running tests and packaging is build.cake. We modify build.cake when we need to add new
targets or change the way the build is done. Normally build.cake is not invoked directly but through
build.ps1 (on Windows) or build.sh (on Linux). These two scripts are provided by the Cake project
and ensure that Cake is properly installed before trying to run the cake script. This helps the
build to work on CI servers using newly created agents to run the build and we generally run it
the same way on our own machines.

The build shell script and build.cmd script are provided as an easy way to run the above commands.
In addition to passing their arguments through to build.cake, they can supply added arguments
through the CAKE_ARGS environment variable. The rest of this document will assume use of these commands.

There is one case in which use of the CAKE_ARGS environment variable will be essential, if not necessary.
If you are running builds on a 32-bit Windows system, you must always supply the -Experimental argument
to the build. Use set CAKE_ARGS=-Experimental to ensure this is always done and avoid having to type
it out each time.

Key arguments to build.cmd / build:
 * -Target, -t <task>                 The task to run - see below.
 * -Configuration, -c [Release|Debug] The configuration to use (default is Release)
 * -ShowDescription                   Shows all of the build tasks and their descriptions
 * -Experimental, -e                  Use the experimental build of Roslyn

The build.cake script contains a large number of interdependent tasks. The most
important top-level tasks to use are listed here:

```
 * Build               Builds everything. This is the default if no target is given.
 * Rebuild             Cleans the output directory and builds everything
 * Test                Runs all tests. Dependent on Build.
 * Package             Creates all packages without building first. See Note below.
```

For a full list of tasks, run `build.cmd -ShowDescription`.

### Notes:
 1. By design, the Package target does not depend on Build. This is to allow re-packaging
    when necessary without changing the binaries themselves. Of course, this means that
    you have to be very careful that the build is up to date before packaging.

 2. For additional targets, refer to the build.cake script itself.

### Building and testing for Linux on a Windows machine

Most of the time, it's not necessary to build or run tests on platforms other than your primary
platform. The continuous integration which runs on every PR is enough to catch any problems.

Once in a while you may find it desirable to be primarily developing the repository on a Windows
machine but to run Linux tests on the same set of files while you edit them in Windows.
One convenient way to do this is to pass the same arguments to
[build-mono-docker.ps1](.\build-mono-docker.ps1) that you would pass to build.ps1. It requires
[Docker](https://docs.docker.com/docker-for-windows/install/) to be installed.

For example, to build and test everything: `.\build-mono-docker.ps1 -t test`

This will run a temporary container using a custom
[rprouse/nunit-docker](https://hub.docker.com/r/rprouse/nunit-docker/) image
based on the [Mono image](https://hub.docker.com/r/library/mono/) and adding in
.NET Core. The script mounts the repo inside the container and executes the
[build.sh](build.sh) Cake bootstrapper with the arguments you specify.
