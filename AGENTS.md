# UniversalRPG Agent Instructions

Updated: 2026-09-12. **Current product priority: RPG Maker MV and MZ.**
The user can provide/test more MV/MZ projects. This explicit decision supersedes the older RM2000/2003-first ordering in historical roadmaps and handoffs. Preserve those implementations; do not make their completion a prerequisite for MV/MZ.

## Product and source of truth

URPG is a compatibility runtime/launcher, not a game remake. Do not invent new stories, worlds or gameplay for imported games. Original game code, plugin order and engine behavior remain the compatibility target. Enhancements must be opt-in.

Read `AGENTS.md`, `SESSION_STATE.md`, `docs/PROJECT_STATUS.md`, `docs/MV_MZ_TESTING.md`, then relevant source/tests and architecture. Source and executed tests override stale prose. There is intentionally **no Kanban**; do not create a work board. The checkpoint is not a history log.

C#/.NET is canonical. The Godot project is under `project/`; the repository currently pins Godot.NET.Sdk/4.7.2 and .NET 8. Do not invent a toolchain validation result or silently change versions. The standalone SDK must remain Godot-free and shared, not compiled into multiple competing contract assemblies. Preserve existing public signatures where practical.

## Work selection

Repair measured build/test/security regressions first. Then prioritize a real MV/MZ path: bounded project inspection and diagnostics; original script/dependency load order; shared browser host; data/asset loading through VFS; actual rendering/audio/input/storage; default project boot; representative custom plugins; independent MV/MZ profiles. Use user-authorized projects and small synthetic fixtures. Do not substitute an isolated successful plugin for a successful game boot.

RM2000/2003, RGSS and WOLF remain supported development tracks but receive regression maintenance rather than taking over the primary milestone. Native Windows DLLs, RM95, Dante98 and Unite remain later research. No original-engine, EasyRPG, mkxp, Wine, NW.js or system-browser subprocess fallback in the normal game path. Embedded licensed dependencies are implementation details.

## Current boundaries

MV/MZ has a concrete experimental Jint adapter, ordered plugin pipeline, PluginManager shim and opt-in frame-pumped timer/currentScript subset. It has **no full DOM/Canvas/WebGL/WebAudio/Node host or playable game-engine registration**. The developer probe runs inspection by default and requires `--execute-plugins` to run trusted project scripts in the limited host. It is not a general browser sandbox.

RM2000/2003 remains partial (including the recent LMU event-page import fix). XP/VX/VX Ace has script parsing/ordering but no embedded Ruby backend. WOLF is an experimental understood plain-data subset. Detection and parsing must never imply launchability; require `PluginCapability.Runtime` defensively at selection and creation.

## Engineering and testing

Choose a coherent implementation slice from real source, write regression coverage, execute focused tests and the full `./scripts/validate.sh` where tooling exists, then update only affected docs. Avoid duplicated interpreters, giant host shims and unimplemented APIs that silently return success. Stop on unsupported behavior with actionable diagnostics.

The browser timing host is an explicit subset: virtual time advances only through `AdvanceFrame`, queues are bounded, new callbacks wait for another pump and missed interval periods are coalesced. This is not complete browser event-loop fidelity. Do not silently present it as such. Validate full core boot before expanding runtime capability flags.

An in-process VM is not an OS sandbox. Inspection never executes game scripts/binaries. Runtime code must not receive arbitrary CLR objects, filesystem/process/native/network permissions or unrestricted host callbacks. Respect VFS containment, content identity, resource limits and the project's protected-content policy. Do not bypass third-party DRM or redistribute proprietary games/RTPs. Track exact third-party licenses.

## Recovery

Preserve unrelated edits. A script session that fails during load/bootstrap/frame/hook is not safe to replay; use a fresh VM/runtime. This is fail-stop, not rollback.

For one normalized failure signature, try at most three genuinely different approaches without new evidence. Do not run an identical failed command repeatedly. Preserve logs, revert only harmful experiments, record the blocker/unblock condition in `SESSION_STATE.md`, then move to independent useful work. Never weaken correct tests, hide errors or disable security to obtain green status.

## Completion and reporting

A complete slice requires implementation, relevant tests and actual validation. When .NET/Godot is unavailable, distinguish added C# tests from executed Node semantic tests and from simulated-tool tests. Never reuse a historical count for the current branch. Do not merge this large PR without fresh full validation and review.

Keep `SESSION_STATE.md` concise: current priority, implemented changes, exact validation evidence, blockers and next action. Dated reports are snapshots. Continue ordinary implementation without asking the user to pick every step; escalate only genuinely non-resolvable/destructive decisions.
