# UniversalRPG session checkpoint

Updated: 2026-09-12. Branch: docs/refresh-2026-09-09, PR #1.
Parent: f3d31dcf57d053bc78ab65025c9e3bb1e089b1c4. No Kanban.

## Priority

MV/MZ first, installed native application first. Preserve original custom scripts and other runtime work. General browser deployment/features are deferred; only implement the interfaces needed by original games. Do not replace game logic with a hard-coded native imitation.

## Latest implementation

- NativeBootManifestParser reads standard static MV index scripts or MZ's leading literal scriptUrls without executing imported markup/main.js; preserves extra local project libraries and validates core order/paths/bounds.
- NativeCoreScriptSet snapshots/hashes original library/core/config sources from one root/www mount. Missing/oversized/invalid data returns no partial executable set.
- Original PluginManager.setup runs against original $plugins. Initial loadScript scheduling is temporarily captured and checked against the existing enabled-plugin plan, then restored. Original parameter methods/aliases remain intact.
- WebScriptRuntime now scopes currentScript for original library/core preludes as well as plugins. Existing constructor and isolated-plugin path remain supported; failures stay fail-stop.
- Developer probe adds --inspect-core and --execute-core with distinct reports. main.js is read/hashed but NOT executed; core-scripts-passed is NOT a game boot/playability result.
- Added two Node suites, 18 C# methods, original MIT MV PluginManager test fixture/notice and CI steps.

## Actual validation

Executed on Node v22.16.0: 36/36 production manifest-JS checks and 18/18 original-plugin-setup checks passed; both node --check commands passed. Verified source baseline Git hashes and workflow YAML/job dependencies.

Not run: all 18 added C# methods, SDK/Jint/Godot build, native probe scene, user projects, platform exports or complete game startup. Toolchain is absent and retrieval failed network/DNS. Parent's PR-run query returned no validator. Never reuse historical counts as branch validation; do not merge before fresh full verification/review.

Details: docs/VALIDATION_CORE_STARTUP_2026-09-12.md.
Modes/limitations: docs/CORE_STARTUP.md.

## Next work

1. Establish actual ./scripts/validate.sh success and repair compiler/test failures.
2. Exercise the new original-core path with authorized default MV/MZ exports. Implement the first real missing native rendering/input/audio API rather than more isolated success-returning shims.
3. Add the original entry-point lifecycle (main.js/window-load/effects/Scene_Boot) after its dependencies are real. Do not mistake dependency extraction for arbitrary custom-main compatibility.
4. Target title -> New Game -> map -> dialogue -> transfer -> save/load with original plugins. Initial scheduling capture does not support custom loadScript side effects or dynamic loaders yet.
5. Maintain SDK/VFS identity, resource policy, fail-stop sessions and existing other-engine regressions. No external runtime executable or browser process fallback.

An in-process VM is not an OS sandbox. After three materially different failed strategies for one signature, record evidence/unblock conditions and continue independent useful work. Do not disable correct tests, replay partial initialization or claim playability from class names or isolated tests.
