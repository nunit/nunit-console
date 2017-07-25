## Building the MSI Installer

To retrieve the latest assemblies, and build the installer, a **Cake** (http://cakebuild.net) script is provided.

Normally build.cake is not invoked directly but through build.ps1.
These scripts are provided by the Cake project and ensure that Cake is properly installed before trying to run the cake script. 
This helps the build to work on CI servers using newly created agents to run the build. We generally run it the same way on our own machines.

The build.cmd script is provided as an easy way to run build.ps1, from the command line.
In addition to passing the arguments through to build.cake, it can supply added arguments
through the CAKE_ARGS environment variable. The rest of this document will assume use of this script.

There is one case in which use of the CAKE_ARGS environment variable will be essential, if not necessary.
If you are running builds on a 32-bit Windows system, you must always supply the -Experimental argument
to the build. Use set CAKE_ARGS=-Experimental to ensure this is always done and avoid having to type
it out each time.

Key arguments to build.cmd / build:

```
 -Target, -t <task>                 The task to run - see below.
 -Configuration, -c [Release|Debug] The configuration to use (default is Release).
 -Experimental, -e                  Use the experimental build of Roslyn.
 -version                           Pass required version of the installer to build. A default value is specified in the Cake script.
 ```

The build.cake script contains a number of interdependent tasks. The most 
important top-level tasks to use are listed here:

```
 FetchPackages           Retrieves the latest versions of the required assemblies, from the latest NuGet packages.
 CreateImage             Extracts the assemblies from the NuGet packages, and adds other required files to an image directory.
 PackageMsi              Builds the MSI.
 PackageZip              Builds the Zip.
 PackageAll              Builds all distributables.
```
