# MV/MZ priority and developer testing

Decision date: 2026-09-12. **MV and MZ are now the first implementation priority**, because the user can test more projects from these generations. Older RM2K-first orderings are superseded, not an instruction to delete those engines.

## What exists in this pass

The ordered plugin pipeline can opt into a browser **subset** via `WebBrowserHostOptions`. The existing five-argument constructor remains available and unchanged in meaning. With the host enabled, its prelude is first, existing compatibility preludes follow, then enabled plugins execute in their configured order.

Implemented APIs: `window`, `self`, `performance.now`, function-valued `setTimeout`/`setInterval`, their cancellation methods, `requestAnimationFrame`/`cancelAnimationFrame`, and read-only `document.currentScript` metadata during a synchronous plugin file.

This is NOT a complete DOM. No canvas, WebGL, WebAudio, Image loader, fetch, Node, persistent storage or actual RPG Maker core boot is supplied. Missing functions are not successful empty stubs. No engine plugin was promoted to playable Runtime.

`document.currentScript.src` is a URI-escaped `urpg://game/...` metadata string, never a fetched host URL. It is cleared after a successful top-level plugin file. Timer/frame callbacks see null. Promise/microtask checkpoints and dynamically inserted scripts are not covered by this implementation.

## Frame semantics and limitations

Call `WebScriptRuntime.AdvanceFrame(deltaSeconds)` only after `LoadScripts` and `ExecuteBootstrap` succeed. No background timer thread is started. Pause by not advancing; keep simulation/presentation policy explicit. The VM receives milliseconds internally.

Both callback queues are snapshotted at pump start. Timers run in due-time/registration order, then the snapshotted animation callbacks share the current timestamp. Cancellation applies to callbacks still waiting in the batch. Newly scheduled callbacks, including animation requests created by timers, wait for the next pump. Intervals fire at most once per pump and coalesce missed periods. Nested short timers apply the four-millisecond clamp beyond nesting level five.

These policies are deliberately frame-quantized, not a fully conforming browser event loop. Errors stop the session instead of continuing other callbacks as a browser might. Queue/argument/callback limits are explicit. String timer handlers remain unsupported because they require dynamic code compilation. Wall-clock Date is not virtualized.

The control object is immutable realm-local compatibility plumbing, not a privileged security boundary: game code can see it. VM resource restrictions still apply, but an in-process interpreter is not an OS sandbox or a hard total-process memory quota.

## Developer probe — actual game folders

The new Godot scene is a diagnostic tool, **not the Play button**. Its C# integration has not been compiled/executed in the editing environment. Run the full validation first with the repository's pinned .NET-enabled Godot toolchain; replace `godot` below with your binary path as necessary. Run from the repository root.

Inspection only (no game code execution):

```sh
godot --headless --path project res://tools/web_plugin_probe.tscn -- --game "/path/to/game" --engine mz
```

Explicit plugin-subset test, for projects you trust:

```sh
godot --headless --path project res://tools/web_plugin_probe.tscn -- --game "/path/to/game" --engine mz --execute-plugins --frames 3
```

Use `--engine mv` for MV. The tool accepts a game folder, including the usual `www/` layout, not a ZIP in this initial entry point. `--frames` allows 1..600 and `--step-ms` a finite positive value up to 1000; default is three frames at 60 Hz. The supplied generation is checked for its expected core filename, not independently proven by that filename.

Reports are uniquely named JSON files under Godot's `user://web-plugin-probes/`. The native output path is printed. The tool never writes to the game directory. It includes inventory/order/hashes, diagnostics and load/bootstrap/frame results; it omits source text, plugin parameters and encryption keys. Review error messages before sharing a report, as a script can put arbitrary text in an exception.

Status meanings:

| Status | Meaning |
|---|---|
| inspection-only | Inventory completed; no scripts ran |
| inspection-incomplete / blocked | Unsafe or incomplete prerequisites; no execution approval |
| nothing-to-execute | No enabled plugins; not a successful game test |
| subset-failed | First unsupported API, script error or resource limit; session stops |
| subset-passed | Enabled plugins initialized and requested frames ran in this limited host only |

All reports retain `fullGameRuntimeExecuted: false` and `playability: not-tested`. The probe intentionally does NOT manufacture SceneManager/PIXI/other RPG Maker globals so that a plugin calling an unimplemented engine API fails honestly. Source hashes recorded during inventory are checked again when the VFS provider reads script bytes; changed files require reinspection. Hashes identify content, not trustworthiness.

Exit codes: 0 for complete inspection or a passed subset run, 1 for a script-subset failure, 2 for invalid/blocked/tool/report errors, 3 for an explicitly requested run with no enabled plugins. Always read scope/status; exit 0 is not a game-compatibility badge.

## Synthetic fixtures

`project/tests/fixtures/web-probe/mv` and `mz` contain small project-owned timing/parameter/command fixtures. Their `rpg_core.js`/`rmmz_core.js` files are explicitly labeled **synthetic filename markers**, not vendor cores. They cannot validate an actual RPG Maker boot. No proprietary game/RTP assets are included.

```sh
godot --headless --path project res://tools/web_plugin_probe.tscn -- --game "project/tests/fixtures/web-probe/mz" --engine mz --execute-plugins --frames 3
```

## Next milestone, in order

Establish actual .NET/Jint/Godot validation; then a version-aware core/library boot manifest and actionable first-failure reports. Implement real data/asset access through the existing VFS, then enough DOM/render/audio/input/storage behavior to run a simple default MV/MZ project through title, New Game, one map, dialogue, transfer and save/load. Add user custom plugins on top of that core path, preserving order and monkey patches rather than replacing game logic.

Evidence for this pass: [VALIDATION_MV_MZ_HOST_2026-09-12.md](VALIDATION_MV_MZ_HOST_2026-09-12.md).

## Primary behavioral references

- WHATWG timer initialization and cancellation: https://html.spec.whatwg.org/multipage/timers-and-user-prompts.html#timers
- WHATWG animation callback snapshot/cancellation: https://html.spec.whatwg.org/multipage/imagebitmap-and-animations.html#animation-frames
- W3C High Resolution Time monotonic clock: https://www.w3.org/TR/hr-time-2/

References inform behavior; no browser engine source or runtime executable is bundled by this change. Differences from the complete standards are called out above.
