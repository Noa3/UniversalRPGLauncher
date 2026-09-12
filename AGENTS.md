# UniversalRPG agent instructions

Updated: 2026-09-12. **MV/MZ first, installed application first.** The user has deferred browser delivery/general browser work and wants actual games running in URPG. Keep only the JavaScript/browser-shaped APIs required to run original MV/MZ code inside the native application; do not turn the project into a generic browser. Do not remove essential script compatibility or substitute native hard-coded gameplay for game-authored plugins.

## Product and source of truth

URPG is a compatibility runtime/launcher, not a game remake. Preserve original script ordering, custom plugins, data, game rules and engine behavior. Enhancements stay opt-in. Read AGENTS, SESSION_STATE, PROJECT_STATUS, docs/NATIVE_MV_MZ.md and relevant source/tests. Older browser-first or RM2K-first ordering is superseded.

There is intentionally no Kanban. Do not create a work board. C#/.NET is canonical, the Godot project is under project/, and the shared SDK must stay Godot-free. Preserve existing public constructors/contracts. The repository currently pins Godot.NET.Sdk/4.7.2 and .NET 8; verify exact tools rather than silently changing pins or inventing test results.

## Work selection

Fix measured build/test/security failures first. Then develop the actual installed-app MV/MZ path: original core/library load order and version differences; local data/asset access; real rendering, audio, input and storage; title -> New Game -> map -> dialogue -> transfer -> save/load. Build the minimum engine services for this path rather than accumulating isolated browser APIs. Never call a plugin-only test a playable engine.

Existing RM2000/2003, RGSS and WOLF work remains intact and receives regression maintenance. Do not wait for it to be complete before advancing MV/MZ. Native Windows DLL execution, RM95/Dante98/Unite and browser deployment are later work. No original-engine, EasyRPG, mkxp, Wine, NW.js or system-browser subprocess fallback. Licensed embedded dependencies are allowed implementation details.

## Current boundary

MV/MZ has an experimental Jint adapter, ordered plugin pipeline, PluginManager shim, frame timing/currentScript subset and a read-only native local JSON adapter. The latter exposes the XMLHttpRequest API shape expected by original DataManager code, but routes only data/*.json GET requests to an explicitly supplied VFS mount. It does not perform HTTP, open a browser or grant general host filesystem access.

The diagnostic probe still executes only an explicitly requested trusted plugin subset. It does not boot the full core or certify game playability. Missing rendering/audio/engine globals must remain explicit failures, not no-ops.

RM2000/2003 remains partial; RGSS has parsing/ordering but no Ruby backend; WOLF is an experimental understood plain-data subset. Detection/parsing/isolated language execution and playable Runtime are distinct. Check PluginCapability.Runtime at both selection and creation; never promote it based on class names.

## Implementation and validation

Choose a coherent slice, inspect existing callers, preserve tested behavior, implement it, add regressions, execute focused checks and ./scripts/validate.sh where available, then update only affected docs. Do not replace working components just for style or add duplicate runtime models.

The native data callback accepts a primitive URL and returns bounded text/error metadata. Keep game mounts read-only, prefixes unambiguous and caller-owned. Enforce policy on every read and aggregate request/byte limits across the outer VM call. Do not expose reflected CLR objects. A VFS must bound reads before allocation; post-read limits do not create an OS sandbox. VM resource/time limits also are not a hard whole-process memory limit.

Inspection never runs scripts/binaries. Keep process/native/network access denied by default, preserve protected-content restrictions, do not bypass third-party DRM, and do not redistribute proprietary games/RTPs. Record exact licenses for all third-party code and fixtures. A permissively published core-source excerpt is not a license for unrelated assets.

## Recovery and completion

Use one host thread per VM. Reject reentrant execution/reset/disposal. A failed script load/bootstrap/frame/hook requires a fresh runtime/VM, not replay into partial state; this is fail-stop, not rollback.

For the same failure signature, allow at most three genuinely different strategies without new evidence. Preserve logs and useful work, revert only harmful attempts, record the blocker/unblock condition in SESSION_STATE, and continue independent useful work. Never weaken correct tests, hide errors or disable safety to obtain green status.

Distinguish source written, C# tests added, Node semantics executed, real Jint/Godot tests and full game evidence. Mocked transport/timers do not validate native VFS integration. Historical test counts do not validate the current branch. Do not merge the large PR without fresh full build/test evidence and review.

Keep SESSION_STATE concise: current objective, changes, actual validation, blockers and next action. Continue ordinary development without requiring the user to select every step; escalate only genuinely non-resolvable or destructive decisions.
