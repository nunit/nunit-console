Experimental session deployment for #167 / #1925

This is a draft session increment on feature/remote-agents after standalone
server #1962 merged there as e667566eb5c8cbc22692eb83a038793976ad8e2f. It is not wired into the existing
solution, Console, engine selection, build recipe, packages or release workflow.
The engine has no dependency on the deployment component. The coordinator
references the engine to perform the optional one-shot execution mode.

The first slice runs on the backend machine. It takes explicit backend-local
host, launcher and test-package directories, creates an owned snapshot, starts
the staged host, and records readiness and results outside the disposable tree.
The host is deployed for the session; no resident server installation is required.
Transporting these inputs and starting this coordinator remotely remain the
responsibility of a future deployment transport. This is not yet a single-command
SSH deployment implementation or a new Console option.

Build with: dotnet build NUnit.RemoteSession.slnx -c Release
Run the built coordinator with an absolute JSON plan filename:
  dotnet /absolute/path/RemoteSessionRunner.dll /absolute/path/plan.json

The example-plan.json uses illustrative Windows paths, not machine discovery.
Every input path must identify a real local directory. Use a fresh evidence
directory and a dedicated deployment root that do not overlap the source inputs.
hostDirectory must contain the complete #1962 nunit-test-server dependency closure.
toolsDirectory is the extracted standard agent package's tools directory, and
launcherRelativePath is relative to that directory. The example selects the
verified .NET 9 launcher's net10.0 entry point. It does not install a runtime.
hostAssemblyRelativePath is explicit because older prototype plans used another
host name; the existing compatibility default is preserved.

Modes
* One-shot: stage, discover/filter/run through the engine, write events and result,
  stop the host, and attempt verified cleanup.
* --stdio: one-shot with sequenced event records on stdout and cooperative
  cancellation through the owning stdin channel. EOF requests cancellation.
* --serve: stage and publish a ready record containing backend paths and port;
  an external frontend uses the existing provider. Input completion, EOF or the
  bounded hosting deadline ends the session. A successful hosting exit confirms
  hosting/cleanup, not the separate frontend's test outcome.

Ownership and evidence
Each deployment has a random identity, owner.txt, SHA-256 manifest and generated
addins referencing staged launcher paths. Portable paths, containment and ordinary
filesystem entries are checked. Source credentials/key files are rejected by the
payload rules; this does not substitute for reviewing package contents.
Readiness must identify the actual child PID and at least one loaded launcher.
Cleanup requires matching ownership, confirmed successful host shutdown, ordinary
tree validation, and an independent native process-reference audit. Uncertain
shutdown, startup failure, tampering or failed audit retains payload and evidence.
Dispose closes the lifetime channel and records disposition; it never deletes the
deployment. Explicit cleanup can retry after a confirmed exit and preserves earlier
errors. This is a cooperative owned-tree contract, not protection against a hostile
process concurrently rewriting the filesystem or an untrusted network peer.

Scope and placement
The provisional server remains loopback-only. SSH/SFTP transfer, authentication,
tunnels, WAN exposure, runtime installation, processor packaging and backend
reservation policy are outside this slice. No processor or hardware was contacted.
Both projects are internal, signed, net10.0, non-packable and use upstream C# 13
and analyzers. No stable public deployment API is proposed. The separate solution
lets this code move to a dedicated remote-execution repository if preferred.

Validation
The new signed library and coordinator build with zero warnings/errors. The
private 77-case Windows harness passes all cases, including real .NET 8/9/10
agents, exact filters, events/results, cancellation, deadlines, lifecycle failure,
retention and cleanup controls. Its source and receipts accompany the review.
This harness is not yet an automatic upstream CI target; placement and test wiring
are review decisions. Native macOS/Linux results validate the earlier hash-frozen
prototype: session 69 pass/8 Windows-only skips, workflow adapter 23 pass/4 skips,
and separate provider-event suite 20 pass on each platform. They are not a native
rebuild or validation of this newly signed packaging. The current packaging changes
only project placement/signing/analyzer integration and a constant-array hoist.
