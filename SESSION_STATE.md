# UniversalRPG session checkpoint

Updated: 2026-09-12. Branch: `docs/refresh-2026-09-09`, PR #1.
No Kanban is maintained. Preserve unrelated source and choose work from actual code/tests.

## Latest work

Parent: `e038cfb04d7ffc4cf4ff5e2d3eb8a4bfc5a7a880`.

- MV/MZ and RGSS loaders now stop permanently after a failed external load, bootstrap or hook. A retry cannot replay earlier scripts into the partially initialized VM. Recovery requires a fresh runtime and VM.
- Hooks require completed bootstrap; reentrant operations are refused. Ordinary provider/VM exceptions become explicit diagnostics. These guards are single-host-thread lifecycle rules, not general thread safety or rollback.
- Shared `WebPluginLoadPlan` uses stable configured order and the first enabled exact plugin name. Loader and parameter shim share that plan. Selected parameter dictionaries are bounded snapshots.
- PluginManager gained `setParameters` and scheduled `_scripts` names. Dynamic script loading, DOM/rendering/audio APIs remain unimplemented rather than faked.
- Added 16 C# session-safety methods and 10 load-plan methods; existing RGSS hook test now bootstraps first.
- `validate.sh` now verifies non-empty Jint/Godot completion summaries, rejects reported failures even with exit 0, requires the pinned .NET Godot build, and retains fresh per-stage logs.
- CI includes the new JavaScript contract tests and simulated-tool validation-driver tests.

## Validation evidence

Executed locally on Node v22.16.0:

- new production PluginManager contract: **18/18 passed**;
- existing production PluginManager regressions: **8/8 passed**.

Executed locally using Python's standard library and Bash:

- actual validation driver with simulated tool processes: **18 test methods passed**;
- Bash syntax and workflow YAML/dependency-key checks passed.

**Not executed:** the 26 new C# methods, SDK/Jint/Godot compilation, actual Jint/Godot suites, exports or real games. Toolchain installation remains unavailable; direct network access failed DNS resolution. No fresh canonical Actions result was available before the changes. Never reuse historical 296/296 main evidence as proof of this branch.

Details: `docs/VALIDATION_SCRIPT_STARTUP_2026-09-12.md`.
Script lifecycle/reference: `docs/SCRIPT_COMPATIBILITY.md`.

## Engine boundary

- RM2000/2003: partial runtime; automatic LMU movement-route scheduling/timing and complete runtime sprite synchronization still need integration.
- WOLF: experimental understood plain-data subset.
- XP/VX/VX Ace: archive/inventory/pipeline; no embedded Ruby backend.
- MV/MZ: experimental Jint adapter and plugin shims; no complete browser/render/audio host or playable engine registration.
- RM95/Dante98/Unite: detection/research.
- Existing content/VFS and engine-managed MV/MZ asset readers do not imply full game compatibility.

## Next useful work

1. Run `./scripts/validate.sh` with .NET and pinned Godot Mono, inspect retained logs, and repair measured compiler/test failures first. No merge before fresh complete validation.
2. Connect decoded LMU page movement routes to active-page lifecycle and explicit timing; publish runtime positions/facing to render descriptors without modifying parsed maps.
3. Add a two-map traversal/save regression before claiming RM2K playability.
4. After actual VM validation, extend browser/RPG Maker services behind truthful capabilities; retain the distinct RGSS profiles and source ordering.
5. Audit dependency notices and actual target exports before release.

On recovery read AGENTS, this checkpoint, project status and relevant source/tests. After three materially different failed strategies for a failure signature, preserve evidence and continue an independent useful path. Do not disable correct tests, silently replay initialization, create a task board, or claim support from class names alone.
