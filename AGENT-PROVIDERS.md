# Agent providers: draft integration

This is the engine half of the provider proposal in [discussion #1909](https://github.com/nunit/nunit-console/discussions/1909).
The companion `nunit.engine.api` change publishes `NUnit.Engine.Services.ITestAgentProvider`.
Its placement, namespace, selection policy and release sequencing are proposals for review.

## Execution path

The runner still uses `MasterTestRunner` and the existing lower-level runners.
`ProcessRunner` now acquires and releases agents through `TestAgentService`,
which coordinates providers. `TestAgency` remains the provider of local process
agents, using the existing pluggable launchers and agents.

Providers are engine extensions registered at `/NUnit/Engine/AgentProviders`.
Discovery is lazy because runners can add extension directories after engine
service startup. Discovered providers are consulted before the local agency.
Without an explicit selection, the first provider reporting suitability is used.
An optional remote provider should decline ordinary local packages.

The provisional string package setting `RequestedAgentProvider` selects a
provider by its full type name. Apply it recursively when constructing an
aggregate package. An invalid, unavailable or ambiguous explicit selection is
an error. Acquisition failure does not trigger fallback to a different provider.

Capability queries do not acquire execution ownership. The coordinator records
the actual agent reference and its owner, and releases it through that same
provider. Concurrent or reentrant release of one agent is rejected. A failed
release retains the ownership record for reconciliation/retry by the caller;
this change does not add an automatic retry or recovery scheduler. In particular,
runner disposal must not be interpreted as proof that remote execution stopped.
The private diagnostics interface preserves local agent-exit diagnostics without
adding a process-specific requirement to the public provider contract.

## Scope

No Console frontend, test framework or agent implementation change is included.
Local execution now passes through the coordinator and must retain its existing
behavior. Providers are an extension seam, not the complete remote product:
server deployment, package transfer, authentication, transport, reservations,
recovery and evidence remain outside this change.

The API change is additive. The former internal engine interface is removed so
the engine and extensions share one public contract identity. Existing private
prototype extensions referencing that former engine-defined type must be rebuilt.

## Building the dependent draft

The currently pinned public API package does **not** contain the new contract.
This draft therefore requires the companion API source/package before it can
build. The public package pin is deliberately left unchanged until a coordinated
version is agreed; ordinary CI restoration of that pin cannot validate this draft.

For isolated validation, pack the API companion locally under a distinct private
version, add that directory to a temporary NuGet configuration, and use
`CustomAfterMicrosoftCommonTargets` to update the central API version for this
build. For example, an external targets file can contain:

```xml
<Project>
  <ItemGroup>
    <PackageVersion Update="NUnit.Engine.Api"
                    Version="4.0.0-beta.101.providerdraft.1" />
  </ItemGroup>
</Project>
```

This is a local validation version, not a published NUnit version. Do not replace
the contents of the public beta.101 package in an existing package cache.

The existing engine driver-filtering tests also access a driver that is now
internal to Agent.Core. Our isolated full-suite test build uses merged Agent.Core
source and grants friend access only to the signed engine test assembly. That
test overlay is not part of the engine, API, or runnable agent distribution.

The PR description records exact source bases, test results and limits. Earlier
cross-platform remote acceptance of the prototype is separate evidence; it does
not substitute for validation of a newly combined dependency graph.
