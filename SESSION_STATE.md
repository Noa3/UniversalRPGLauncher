# UniversalRPG session checkpoint

Updated: 2026-09-12. Branch: docs/refresh-2026-09-09, PR #1.
Current parent: 7a538b8df494a077cbd3922387d086a3fea2d7c3.

## Priority decision

**MV/MZ FIRST**, explicitly requested because the user has more projects available to test. Preserve RM2K/RGSS/WOLF code and regression coverage, but do not gate MV/MZ work on those engines' completion. AGENTS and the Hermes prompt now reflect this. No Kanban; URPG is a compatibility runtime, not a game-content remake.

## Latest implementation

- Opt-in WebBrowserHostPrelude: window/self, virtual performance.now, bounded function timers/cancellation, requestAnimationFrame and currentScript metadata.
- WebScriptRuntime keeps its old constructor and adds a new host-enabled overload. AdvanceFrame pumps seconds through one constrained VM call after bootstrap; errors retain fail-stop lifecycle. Script URI metadata is scoped around each enabled plugin.
- Explicit frame-quantized subset: snapshot queues, defer new callbacks, coalesce missed intervals. Not full browser conformance, rendering, audio, DOM or Node.
- Developer scene res://tools/web_plugin_probe.tscn: inspect by default, --execute-plugins required for trusted-project subset execution. JSON reports never claim full-game playability and are written outside the game folder.
- VFS script reads verify a known inventory SHA-256; changed bytes require reinspection.
- Synthetic MV/MZ fixtures and CI steps added. Their core-named files are markers, not proprietary/vendor cores.

## Validation truth

Actually run here: 46/46 production-JS browser-host semantic checks and 2/2 synthetic plugin fixtures passed on Node v22.16.0. Source baseline Git hashes and workflow YAML structure were checked.

Not run: 19 new C# methods (15 real-Jint frame-host + 4 source-identity), .NET/Jint/Godot builds, the actual probe scene, user games or platform exports. .NET/Godot are absent; current container network attempts fail DNS resolution. Do not claim whole-branch green or reuse earlier pass counts. Evidence: docs/VALIDATION_MV_MZ_HOST_2026-09-12.md.

## Immediate next work

1. Establish complete ./scripts/validate.sh success and actual MV/MZ synthetic probe runs under Godot/Jint. No merge before fresh validation/review.
2. Use docs/MV_MZ_TESTING.md to collect scoped reports from user-authorized projects; do not call subset-passed a game compatibility pass.
3. Implement a real MV/MZ core/library boot manifest and version profiles, then VFS-backed data/assets and actual rendering/audio/input/storage. Target a small title/New Game/map/dialogue/transfer/save path.
4. Keep unsupported APIs explicit. Preserve script order, source identity and fail-stop sessions. Avoid invented SceneManager/PIXI/DOM no-ops.
5. Maintain other engines without deleting work or reverting to old priority ordering.

In-process VM limits are not an OS sandbox. No auto execution on import, no arbitrary host API grants, no third-party DRM bypass and no proprietary asset redistribution. After three materially different failed strategies for one failure signature, record evidence/unblock conditions and move to an independent useful slice rather than looping.
