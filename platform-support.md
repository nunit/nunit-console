# Platform Support Lifecycle

"Platform Support" for this project includes three somewhat different things:
1. The target platforms for which tests may be written.
2. The platforms under which those tests will run, which is equivalent to the list of agents we supply.
3. The minimum platforms required to execute the runner and engine themselves, without regard to the tests being run.

## Test Assembly Target Runtimes

We will continue to support tests targeting runtimes which are out of support from Microsoft's perspective, so long
as we are able to do so without significant additional effort or security risk. If no agent is available for a runtime,
the tests will be run on the closest higher runtime available.

> _We currently support execution of tests written to target any version of the .NET Framework >= 2.0 and any version
> of .NET Core >= 3.1, including .NET 5.0 and higher._

## Agents Provided

We will continue to provide agents for any Microsoft runtime for at least six months after its end of life. This is 
intended to support continued testing of legacy applications while users are in the process of upgrade. However, agents
for runtimes which have been declared a security risk may be removed immediately.

> _We currently supply agents for .NET Famework 2.0 and 4.6.2. .NET Core 2,1 and 3.1 and .NET 5.0, 6.0 and 7.0. The .NET
> Core 2.1 runner is being removed in version 3.18.0 and .NET 8.0 will probably be added before this document is finalized.
> Once this document is approved, we will begin to phase out agents according to the above criteria.

## Required Runtimes

The console runner and engine target .NET 4.6.2, so that runtime or greater is required to execute any tests at all
without loss of features. In individual cases, it may be possible to run using a lesser version of .NET 4.x.

> **Note:** Editorial comments in italics will be removed in the final version of this document
