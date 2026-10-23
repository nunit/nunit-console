# Localhost agent-provider test server

This is an initial implementation for nunit/nunit-console#1921, based on
`main` at `6ee68b3658f22bc8450ef85326adec20e8c82b35`. It is deliberately in
the engine test assembly. No production engine, Console, API, Common or agent
source changes are needed, and neither #1919 nor API PR #22 is required to
compile or run it.

The server implements the current internal `ITestAgentProvider` contract. Its
real-agent mode delegates to the existing `TestAgency`, which finds standard
pluggable launchers through `ExtensionService`. A test client implements the
same contract over TCP. The route is test client -> provider server -> existing
local agency -> standard agent -> NUnit framework. The public launcher packages
are copied under the test output directory; no machine-wide installation or
private package feed is required.

## Running the tests

Build the engine test project with an explicit configuration:

```text
dotnet build src/NUnitEngine/nunit.engine.tests/nunit.engine.tests.csproj -c Debug
```

Run the focused tests from `bin/Debug`:

```text
dotnet net10.0/nunit.engine.tests.dll --where="class == NUnit.Engine.Integration.TestAgentProviderServerTests" --work=<writable-directory>
net462/nunit.engine.tests.exe --where="class == NUnit.Engine.Integration.TestAgentProviderServerTests" --work=<another-writable-directory>
```

Each case starts its own child server and binds only to IPv4 loopback on an
OS-assigned port. The child publishes a readiness file by atomic rename and is
stopped through its redirected standard input. The tests check the child exit
code. Real-agent cases additionally record native process identity and verify
that the agent exited with code zero before removing the staged package.

The server can also be started explicitly using the same test executable:

```text
dotnet net10.0/nunit.engine.tests.dll --test-provider-server <ready-file> <launcher-directory>
```

The launcher directory must contain exactly one `nunit-agent-launcher-*.dll`
and retain the standard package's sibling agent layout. Press Enter, or close
standard input, to stop the server. Passing `fake` instead of a launcher
directory selects the deterministic provider used by fault tests.

## Contract and lifecycle coverage

- Listing agents, package suitability and package-specific agent information do
  not acquire an agent. Names, types and target runtimes survive the TCP boundary.
- Acquisition returns an opaque agent ID. A client session must return it through
  the same provider; server-side ownership checks reject another client's release.
- Release errors preserve the lease for retry. Duplicate release and operations
  after confirmed release are rejected.
- The client forwards runner creation, load/unload/reload, counting, discovery,
  execution, events/results and stop operations. Tests explicitly exercise exact
  discovery and execution of both a passing and a deliberately failing test in a
  separately staged assembly using real released agents.
- Run and stop use separate request connections, allowing a stop request to
  overtake synchronous execution. Controlled-provider cases verify cooperative
  and forced-stop dispatch; they do not claim that released agents implement all
  planned engine forced-stop semantics or synthesize missing completion events.
- A killed controlled-provider server completes the asynchronous client result
  with an infrastructure failure. The client does not treat disconnection as
  release confirmation or silently run tests locally.

## Scope and integration

NUnit's existing `BinarySerializationProtocol`, `SocketReader`, package XML and
runner message codes are reused. Additional provider command codes and XML
envelopes are provisional test scaffolding, not a proposed public protocol.
Socket operations have a 30-second timeout suitable for this bounded test suite.
The client handles partial sends and retains one reader across progress/result
messages on each connection.

This is a trusted-loopback test server. It supplies no network authentication,
remote machine discovery, installation, deployment, persistent service management
or target-selection UI. It is not added to published runtime packages.

After #1919, the real-provider creation site in `ProviderServerHost` can be adapted
to the agreed `TestAgentService` boundary. Production `RemoteProcessTestAgency`
and provider-proxy integration remain separate work. This test server establishes
an executable contract and test cases without freezing that unfinished design.
