# Platform Support Lifecycle

"Platform Support" for this project includes three somewhat different things:
1. Test assembly target platforms, that is, the target platforms for which tests may be written.
2. The target platforms under which those tests will actually run, which is equivalent to the list of agents we provide.
3. The minimum platforms required to execute the runner and engine themselves, without regard to the tests being run.

## Test Assembly Target Runtimes

We currently support execution of tests written to target any version of the .NET Framework >= 2.0 and any version
of .NET Core >= 3.1, including .NET 5.0 and higher. We will continue to support tests targeting runtimes which are
out of support from Microsoft's perspective, so long as we are able to do so without significant additional effort
and without security risk. If no agent is available for a runtime, the tests will be run on the closest higher 
runtime available.

## Agents Provided

We currently (July 3, 2024) supply the following agents with the console runner:
* .NET Framework 4.6.2
* ,NET Core 3.1
* .NET 6.0
* .NET 7.0
* .NET 8.0 (coming in version 3.18.0)

As a general policy, we will continue to provide agents for any Microsoft runtime for at least six months after its
official end of life. This is intended to support continued testing of legacy applications while users are in the 
process of upgrade. However, agents for runtimes which have been declared a security risk may be removed immediately.

Based on that policy and the planned end-of-life dates for runtimes, we expect to retire agents on or after the
dates listed in the following table. 

| Runtime              | Microsoft<br>End of Support | Agent Retirement | Notes |
| -------------------- | --------------- | --------------------- | --- |
| .NET Framework 2.0   | July, 2011      | July, 2024            | Removed in version 3.18.0
| .NET Framework 4.6.2 | January, 2027   | after July, 2027      | May be upgraded to 4.8.1 before retirement date |
| .NET Core 3.1        | December, 2022  | after December, 2024  | 
| .NET 5.0             | May, 2022       | July, 2024            | Removed in version 3.18.0
| .NET 6.0             | November, 2024  | after May, 2025       |
| .NET 7.0             | May, 2024       | after November, 2024  |
| .NET 8.0             | November, 2027  | after May, 2027       |

## Required Runtimes

The console runner and engine target .NET Framework 4.6.2, so that runtime or greater is required to execute any tests at all
without loss of features. In individual cases, it may be possible to run using a lesser version of .NET 4.x. It's possible
that we may require a higher level of the framework in a future 3.x release.

