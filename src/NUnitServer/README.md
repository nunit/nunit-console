# Standalone test server (first increment)

This executable implements the first standalone-server increment for #167, consumed
by the configured remote provider in #1925. It hosts the engine's public
`ITestAgentProvider` and loads ordinary pluggable launchers through the public
`IExtensionService`. The engine does not reference this project. The only build
reference back to the executable is from the modern integration-test target, with
`ReferenceOutputAssembly="false"`.

## Build and start

Build `NUnitConsole.slnx` in Release, or build
`src/NUnitServer/nunit-test-server/nunit-test-server.csproj` directly. The executable
and its engine dependencies are written to `bin/Release/server/net10.0/`.

Stage the server in an owned directory outside a source tree containing other
extension packages. Extract compatible published launcher packages beside it with
their `NUnit.Extension.*` package-directory names, `tools` directories, agent files
and dependencies intact. For example:

```text
session/server/nunit-test-server.dll
session/NUnit.Extension.Net10PluggableAgent/tools/net8.0/nunit-agent-launcher-net10.dll
session/NUnit.Extension.Net10PluggableAgent/tools/agent/...
```

The engine's normal startup discovery searches ancestor directories for that package
layout. Merely pointing an `.addins` file at another directory does not isolate the
host from packages found by that search. The optional launcher-root check below
fails closed if an unexpected extension was discovered. This executable does not
bundle, install, replace or disable agent packages.

Start the server with:

```text
dotnet nunit-test-server.dll <absolute-unused-readiness-file> [absolute-launcher-root]
```

The optional launcher root checks that discovered extension assembly paths are under
that directory. It is a deployment-consistency check for trusted local files, not a
sandbox or a substitute for verifying staged package ownership and contents.

The server listens only on literal IPv4 loopback and chooses a free port. It writes
the readiness file atomically after loading extensions and starting its listener.
The XML contains `port`, `pid`, available-agent count, engine location and extension
assembly paths. An existing readiness file or its `.tmp` counterpart is not replaced.
The caller owns those receipts and any staged files.

Keep standard input open while using the server. Closing it requests orderly
shutdown; ordinary input lines do not. A zero exit means the server completed its
shutdown sequence. An error is written to standard error and produces exit code 1.
Neither an acknowledged agent return nor server exit alone proves all native child
processes have exited. Audit owned processes independently before deleting a session
payload; retain files and diagnostics if cleanup is uncertain.

## Use from the engine or Console

Pre-stage the test assemblies and dependencies on the execution machine, and create
the existing configured-provider definition using the reported port:

```xml
<nunit-remote-server name="example" endpoint="tcp://127.0.0.1:PORT" />
```

Set the package's `RemoteServerDefinition` to this file's absolute path and select
`RemoteProcess` as described in the feature branch's configured-provider tests.
A project-loader extension can supply those settings to the unmodified Console;
this increment does not bundle such a loader or add Console switches. The separate
prototype validation uses its existing `.nremote` loader. `RemoteTestAssembly` can
identify an assembly already on the backend. The server does not transfer files or
discover machines.

Provider commands use NUnit's existing TCP framing and runner messages.
`RemoteAgentConnection.cs` is source-linked from the engine, keeping one provisional
command/envelope implementation. Moving that source or creating another package is
deferred until the protocol's eventual home is agreed. There is no reverse dependency
from the engine to this executable, so moving it to another repository later remains
possible.

## Deliberate scope

The host requires modern .NET 10. Agents may use the runtimes supported by their
installed launchers. This increment does not introduce an engine/API change, a
permanent service, a NuGet release, package transfer, automatic bootstrap, queueing,
administration, discovery, reconnect/recovery, authentication or TLS. Owner IDs
correlate leases; they are not credentials. The provisional loopback endpoint must
not be exposed directly to other machines or the WAN. Protected transport and
session deployment remain separate increments.

Integration tests launch the actual executable as a child process and use the
published .NET 10 launcher and existing mock test assembly. They exercise readiness,
metadata, exact discovery/execution, result/event forwarding, lease ownership and
shutdown. Additional standalone lifecycle validation is recorded with the review
evidence. No tests require a processor or another machine.
