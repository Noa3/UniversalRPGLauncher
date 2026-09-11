# UniversalRPG Session Checkpoint

Reviewed: 2026-09-12. Work branch: `docs/refresh-2026-09-09`, PR #1.
There is intentionally no Kanban. Select coherent work from source, tests and project status.

## Current objective

Stabilize the existing SDK, script adapter and RM2K runtime before adding more unvalidated subsystems. Custom game scripts and external SDK consumers remain first-class requirements.

## Latest stabilization

- Fixed move-route references to nonexistent `PlayerX/PlayerY`; use canonical `MapX/MapY`.
- Valid route switch IDs now expand lazy switch storage. Route inputs are snapshotted and invalid positions fail before arithmetic.
- Added 14 movement-runner regression methods; corrected the old directional-passability test to check both adjacent edge flags over 256 mask pairs.
- Jint invocation preserves the receiver (`this`), resolves getters inside one constrained call, and accepts only bounded primitive arguments rather than arbitrary CLR objects.
- Aggregate stored script text now has a separate source-memory budget cleared on reset/dispose. This is not a total process-memory cap.
- PluginManager configuration uses JSON.parse, preserving `__proto__` as data. MZ callbacks retain self and falsy arguments; real plugin-name whitespace is not trimmed.
- Added four C#/Jint PluginManager regression methods and expanded the standalone Jint smoke program.
- Added two executable Node semantic suites extracting the actual JavaScript constants from production C#; these do not substitute for Jint tests.
- CI has independent JavaScript-semantic and portable .NET jobs followed by Godot validation. Node is a development-test tool, not a game/runtime dependency.

## Validation truth

Executed here: both Node semantic suites, **12/12 + 8/8 passed**, on Node v22.16.0. Workflow YAML syntax/dependency checks also passed.
Not executed here: .NET build, Jint smoke, Godot import, C# regression suites, platform exports, or real-game playthrough.

The editing environment has no .NET/Godot toolchain and could not retrieve/install it. No fresh PR-triggered validation run was returned for the pre-change head. This does not establish why Actions runs are absent. Do not describe this branch as green or merge it on the historical test count.

Detailed evidence and pending commands: `docs/VALIDATION_2026-09-12.md`.

## Actual engine boundary

- RM2000/2003: partial event/simulation runtime. Move-route decoder and runner exist, but automatic LMU page-route scheduling/timing and complete sprite synchronization remain to be connected.
- WOLF: experimental understood plain-data subset, not broad native-game support.
- XP/VX/VX Ace: script archive/inventory/pipeline; no embedded Ruby/RGSS execution backend.
- MV/MZ: real Jint adapter source and PluginManager shim; no complete browser/render/audio host or registered playable MV/MZ runtime. C#/Jint integration is pending validation.
- RM95/Dante98/Unite: detection/research.
- Read-only VFS/archive and MV/MZ encrypted-asset readers exist. Full RGSS archive readers and protected WOLF execution are not implemented.

## Next actions

1. Run `./scripts/validate.sh` and resolve actual compiler/runtime failures before expanding the host API surface.
2. Connect LMU event-page movement routes to the scheduler with explicit timing and active-page changes; wire runtime positions/facing into sprite refresh. Preserve parsed maps unchanged.
3. Add a real two-map traversal/save regression before claiming a playable RM2K milestone.
4. Verify the Jint smoke suite, then expand minimal browser APIs behind explicit capabilities. Do not turn missing rendering/audio into successful no-ops.
5. Audit resolved dependency notices and platform packages before release.

Recover by reading AGENTS, this checkpoint, project status and relevant source/tests. Preserve unrelated changes. After three materially different unsuccessful strategies for one failure signature, record evidence/unblock conditions and choose an independent useful task; never delete correct tests to obtain green status.
