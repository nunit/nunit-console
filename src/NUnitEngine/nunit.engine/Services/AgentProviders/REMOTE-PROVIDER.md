# Remote provider integration candidate

This is an incremental implementation for #1925 on top of the merged provider ownership
change in #1933, the public provider contract from engine.api #22 and the service rename in #1936. It exercises the trusted localhost server contract from
#1922 through the built-in `RemoteProcessAgentProvider`. The settings, definition
schema and provider commands below are proposals for review, not a stable public
remote-execution API.

Providers implement the public `NUnit.Engine.Services.ITestAgentProvider` from
`NUnit.Engine.Api`; the engine-local duplicate has been removed. Acquisition uses
`GetAgent` and the exact reference is returned through `ReturnAgent`. The current
integration pin is the published MyGet `4.0.0-beta.101.2`, built from the merged API
commit. The engine uses Agent.Core `4.0.0-beta.6.2` and Extensibility/API `4.0.0-beta.6`, matching current main. Development package publication/pins remain coordinated with upstream releases.
The provisional provider wire command names remain unchanged by this API adoption.

## Selection and ownership

The existing path remains `ITestEngine.GetRunner` -> `MasterTestRunner` ->
`ProcessRunner` -> `TestAgentService` -> the selected provider. A package with no remote
selection continues to use the local provider. The Console requires no source or
command-line parser change: a project loader can set the package settings.

For this candidate, `RemoteServerDefinition` is an absolute path to an explicitly
selected definition file. Its presence requests remote execution. `TestAgentType`
may also be specified using the existing API's documented setting name, but a
conflicting local/in-process selection is rejected. The latter has not yet been
added as a canonical `SettingDefinitions` entry in the public API package.

The local provider rejects explicit remote requests before inspecting launchers.
An unavailable or invalid selected server therefore fails the request; it cannot
silently run tests locally. An unconfigured remote provider contacts no endpoint.
There is no server discovery or process launch during capability queries.
Explicitly remote unmanaged images are rejected before runner selection, since the
current unmanaged runner executes locally and does not use an agent provider.

The remote provider qualifies advertised agent names as `server-name::agent-name`
and reports `RemoteProcess` as their type. Server names must be unique among the
definitions used by an engine instance. This is not a globally unique or durable
server identity. It remains possible to select a backend launcher by its original
unqualified name; a name explicitly qualified for another server is rejected.

Each acquired proxy retains its original connection settings and owning client.
Editing the definition changes subsequent package queries/acquisitions and does
not redirect existing agent operations or release. The backend owns the native
process. The coordinator returns the exact proxy to its original provider, without
requiring a native `Process` in the coordinator's `AgentStore`.

## Provisional definition

```xml
<nunit-remote-server name="office"
                     endpoint="tcp://127.0.0.1:12345"
                     connectTimeout="10000"
                     operationTimeout="30000"
                     runReadTimeout="120000" />
```

Only literal IPv4/IPv6 loopback addresses are accepted. A separately established,
trusted tunnel can end on that loopback address; this candidate does not establish
or authenticate a tunnel. It does not enable plaintext connections to LAN/WAN
addresses, resolve DNS names, expose the test server to a network or distribute
credentials. A local process able to access the endpoint is inside this test's
trust boundary. Authentication, authorization and a production server are separate
work, not demonstrated by these loopback tests.

The bounded XML reader prohibits DTDs and rejects unknown attributes, child
elements, non-whitespace text and invalid endpoints. Deadlines are milliseconds.
Connect/control deadlines must be positive; zero is permitted only for an explicitly
infinite run read timeout. That read timeout is socket inactivity, not an absolute
run duration or a framework test timeout. Repeated incoming data can extend it.

## Transport, package handling and failure

The client uses the existing NUnit binary framing and runner message codes. The
additional provider commands/envelopes originate in the #1922 test server and need
agreement/versioning before becoming a public protocol. Each request uses a separate
connection so a stop request can overtake a synchronous run. A single reader handles
progress events and the final reply without discarding coalesced messages.

Before forwarding a package, the client clones it, removes coordinator routing and
previously selected-launcher settings, and translates this server's qualified
requested agent names. The caller's package remains intact. This does not transfer
assemblies or map filesystem paths: the server must already be able to read the
package paths. The Console proof deliberately uses files staged on this same host.

A release error retains ownership for retry. An asynchronous transport failure
completes with `Failed`/`Error`, rather than leaving the caller waiting or reporting
success. This is not complete per-test terminal-event synthesis after disconnection.

An acquisition error can hide a lease whose reply was lost. The candidate therefore
fences further acquisition to that endpoint within the provider instance. Changing
the file name, server alias or timeouts cannot clear that fence. Existing leases can
still be released and capabilities can still be queried. This conservative policy
also fences explicit server-side errors; the provisional protocol does not distinguish
every failure stage. It is not durable recovery: creating a new engine instance,
changing a tunnel's endpoint or restarting the server is outside that guarantee.
Idempotent requests, reconciliation, lease expiry, bounded engine shutdown and
explicit recovery need a server lifecycle design before general use.

## Validation and remaining integration

Tests cover selection, definition parsing, metadata without allocation, original
endpoint release, failed-release retry, qualification, cancellation, server loss,
and a server that allocates an agent but intentionally drops its acquisition reply.
Real released .NET Framework/.NET 10 agents are exercised through the public engine
path. The test-only Console project loader lives outside the upstream source patch.
The Console proof includes local execution and remote exact-filter pass/fail and
discovery with native agent exit receipts.

These new tests run on Windows on both engine runtimes. They do not replace Linux,
macOS, stock-adapter, GUI or device validation of this upstream integration. The
maintained remote prototype has broader historical coverage; those receipts do not
prove this newly adapted provider implementation on other platforms.

The current master runner still analyzes local assembly files before selecting a
provider. For explicitly remote packages, runtime selection now preserves the image's
portable minimum or the explicitly requested runtime, validates its syntax, and defers
availability to the backend provider/launcher. Coordinator installation checks and
fallback remain unchanged for local requests. A root runtime constraint still requires
local availability if any leaf is local; all-remote project children do not require it.
This is not a remote installed-runtime inventory or an availability guarantee from
advertised metadata. An unsupported backend request fails without local substitution.

The unchanged Console also pre-checks its global `--framework` option against the
coordinator's runtime list before it loads a project. Explicit constraints supplied by
a project loader are tested in this slice. Accepting a backend-only runtime via that
command-line option requires separate frontend/API work; it is not demonstrated here.

Handling remote-only paths remains necessary. Deployment, temporary
server launch, authenticated transport, result/evidence collection, reservations,
restoration and backend cleanup belong to subsequent integration work. A persistent,
pre-installed server is not required by the eventual architecture; this initial
contract server is explicitly started by the test harness.

Further API decisions include canonical agent-type/endpoint settings, loading
configured servers before a package is selected, display metadata and whether a
server identity is distinct from a connection profile. Those decisions should be
coordinated with the in-process provider work without changing default local runs.
