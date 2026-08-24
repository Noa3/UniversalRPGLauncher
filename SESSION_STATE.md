# UniversalRPG Autonomous Session State

> Updated: 2026-08-24
> Purpose: small durable checkpoint for Hermes/other autonomous agents.

## Current card

|K-031 is DONE — bounded character/event sprite descriptors and camera state implemented and verified. Next card: K-032 message/window/picture presentation layer. Keep MZ detection-only.|
|midnightschool.exe (C:\Users\noa3\Desktop\Neuer Ordner (3)) analyzed detection-only: NSIS-3 Unicode installer wrapping `$PLUGINSDIR/app-64.7z` = Electron x64 distribution; `resources/app.asar` contains a complete unencrypted RPG Maker MZ 1.x game under `project/` (title: 深夜学校のパイズリ怪異, 858 files / ~238 MiB extracted to %TEMP%\midnight-extract\mzgame with standard layout index.html + js/rmmz_core.js + rmmz_managers.js + data/System.json). The extracted Electron host was externally launch-verified with process exit 0 and visually confirmed by the user. Static ASAR inspection shows `package.json` main=`src/main.js`; the host creates an Electron window and loads `project/index.html` from inside the ASAR. This proves the vendor launcher works, not a UniversalRPG runtime path; the existing MZ plugin remains detection-only and must not mark the installer EXE as directly startable.|

## Last verified baseline

`GODOT_BIN=E:/URPG/Godot_v4.7.2/Godot_v4.7.2-stable_mono_win64_console.exe ./scripts/validate.sh` passed on Windows: headless C# suite `217/217`, exit 0, .NET build `0` warnings / `0` errors. The Godot project now lives under `project/`; validate.sh handles both.

## Layout note (2026-08-23)

Godot project files (`project.godot`, csproj/sln, app/, src/, tests/, assets/, locale/, scenes/, plugins/) moved to `project/`. Root keeps docs/notes, `docs/`, `scripts/`, and the Godot runtime under `tools/godot/`. Build/test commands must target the project dir (validate.sh does this automatically).

## Validated stabilization changes

- `VirtualClock` repeating callback cadence fixed; stable event IDs introduced; slow-motion speed factor corrected; monotonic FPS sampling added.
- Compatibility game-specific flags now truly override global defaults.
- `RM2KDatabase` compile/serialization defects repaired and round-trip tests added.
- New VirtualClock regression suite and RM2KDatabase regression suite.
- `GameDetector` now refuses symlink/junction directory matches for Data/www/js/Scripts discovery.
- `scripts/validate.sh` and GitHub validation workflow added.
- Kanban/agent recovery protocol added.
- `RM2KDatabase` array comprehensions were replaced with valid GDScript serialization loops; all database collections now have focused serialization coverage.
- `VirtualClock` uses GDScript `float`/`maxi()` types compatible with Godot 4.7.2 warning-as-error parsing.
- `scripts/validate.sh` discovers the local Windows Godot 4.7.2 editor without `GODOT_BIN`.

## Completed K-002

- Normalized CP932/SJIS aliases to Godot's supported `SHIFT_JIS` decoder name; added `test_legacy_text_decoder.gd`.
- Replaced the VFS `"\\u0000"` source literal with byte-level NUL detection and retained security regression coverage.
- Updated current test counts and validation status in project documentation.

## Completed K-010

- Added provenance-pinned EasyRPG/TestGame RM2000 and RM2003 LDB/LMT/LMU fixtures with SHA-256 notes.
- Added real-fixture parser/framing tests for both databases and maps.
- Accepted valid zero-length LDB struct-array sections and retained unknown top-level chunks.

## Completed engine plugin foundation

- Added trusted in-process plugin contracts, deterministic registries, typed probe/lifecycle errors, and runtime host cleanup under `src/plugins/`.
- Added bounded read-only folder/ZIP inspection and built-in detection plugins for RM95, RM2K, RM2K3, XP, VX, VX Ace, MV, MZ, WOLF, and Unite research detection.
- Added the first functional parser-backed RM2K/RM2K3 runtime bootstrap: validated LDB/LMT/LMU loading, deterministic clock updates, and safe lifecycle start/stop without `RPG_RT.exe`.
- RM95, MV, MZ, and Unite remain detection-only; RGSS exposes bounded metadata lifecycle slices, WOLF exposes an explicitly unencrypted plain-data slice, and RM2K/RM2K3 retain the parser-backed runtime bootstrap.
- Rewired `GameDetector`, `GameLibrary`, `RuntimeLauncher`, and the Godot UI to preserve ranked detection reports, persist import metadata, and refuse unsafe/unsupported runtime selection without external fallback.
- Added contract, detection, archive, persistence, ambiguity, platform, and lifecycle regression coverage.
- Added RGSS and WOLF regression fixtures/tests; validation passes with Godot 4.7.2 Mono: `159/159` tests.
- Nullable contracts were hardened across C# core/UI/test code; `.NET` build now reports `0` warnings and `0` errors.

## Current action

K-022, K-030, and K-031 are complete for their bounded slices. `GameSimulationState` supports bounded map configuration and movement. `VirtualFramebuffer`/`Rm2kRendererAdapter` assemble validated lower/upper layers; `Rm2kSpriteAdapter` and `Rm2kCameraState` provide bounded player/event descriptors and camera state without invoking foreign game code.

Fixture reconnaissance: `D:\NextCloud\Games\PornGames\SkiesInflateableAdventure` is an unencrypted RPG Maker MZ tree (`index.html`, `js/rmmz_core.js`, `js/rmmz_managers.js`, `data/System.json`, title `Skie's Inflatable Adventures (v0.30.001)`, 7,039 files). `D:\NextCloud\Games\PornGames\IntheHamletofLoliBigtits_v103a` is not an MZ web tree at its root: no `index.html`, `js/rmmz_*`, or `data/System.json`; Japanese locale remains unconfirmed and no encrypted marker was found in the bounded filename scan. Both were inspected detection-only; no game code executed.

## Next action

|Start K-031: character/event sprite renderer and camera, after verifying available asset references and lifecycle boundaries.|

## Completed K-012

- `ParseDatabase` decodes actors into typed entries (verified `ChunkActor` IDs; liblcf-default values for absent fields); switches/variables decode as id/name entries.
- Duplicate structure IDs rejected; unknown actor/entry fields retained per entry.
- Synthetic coverage: defaults, unknown retention, duplicate IDs, missing terminator. Real-fixture tests assert typed counts equal section counts on both TestGame LDBs.
- Validation evidence in `KANBAN.md`; suite now `165/165`.

## Failure log

- 2026-08-22 | K-015 | Signature: new typed-section regression -> `The given key was not present in the dictionary` in `Test_ParseDatabaseDecodesTypedSkillItemStateAndClassEntries`. Hypothesis: the test exposed that the parser only returned actors/switches/variables. Action: added verified scalar field contracts and typed result arrays for skills/items/states/classes. Result: focused behavior became green; full suite then exposed the separate dispatch-boundary regression below.
- 2026-08-22 | K-015 | Signature: full validation -> `No scalar field contract exists for LDB section 0xE/0x1F` in `ParseDatabase`, breaking real-fixture parsing and RM2K runtime initialization. Hypothesis: all LDB array sections were routed through the new scalar decoder. Action: restricted typed dispatch to sections with an implemented contract while retaining bounded framing/count parsing for the rest. Result: `166/166` tests and smoke validation passed.
- 2026-08-22 | K-015 | Signature: new combat-section regression -> `The given key was not present in the dictionary` in `Test_ParseDatabaseDecodesTypedEnemyTerrainAndAttributeEntries`. Hypothesis: parser output still exposed only the previous typed batches. Action: added verified scalar contracts and result arrays for enemies/terrains/attributes. Result: `167/167` tests and smoke validation passed.
- 2026-08-22 | K-015 | Signature: new presentation-section regression -> `The given key was not present in the dictionary` in `Test_ParseDatabaseDecodesTypedTroopAnimationAndChipsetEntries`. Hypothesis: parser output still exposed only the previous typed batches. Action: added verified scalar contracts and result arrays for troops/animations/chipsets. Result: `168/168` tests and smoke validation passed.

- 2026-08-22 | K-016 | Signature: existing `SmokeMzDetection` failed after MZ validation required `rmmz_managers.js`. Hypothesis: the new MZ boundary was correct but the legacy smoke fixture was incomplete. Action: added the manager signature to the synthetic smoke fixture. Result: full validation passed at `171/171`.
- 2026-08-21 | C# migration | Signature: Godot Mono headless -> `Cannot instantiate C# script because the associated class could not be found. Script: 'res://tests/csharp_runner.cs'`. Hypothesis 1: stale incremental build skipped source generators. Evidence: forced `-t:Rebuild -p:EmitCompilerGeneratedFiles=true` ran ScriptMethods/Properties/Signals generators for all classes, but `ScriptPathAttributeGenerator` produced no output and `UniversalRPG.dll` contains zero `[ScriptPath]` attributes (only 5 unrelated `res://` strings). GodotSharp 4.7.2 defines `ScriptPathAttribute`; SDK targets disable nothing; generator class exists in the package. Attempt 1 (rebuild) did not resolve. Next attempt: manual `[ScriptPathAttribute]` annotation on scene-referenced classes; if that fails, decompile the generator for its emission condition.
- 2026-08-21 | C# migration | Manual `[ScriptPathAttribute]` annotations did not register scene scripts. Root cause: `ScriptPathAttributeGenerator` requires case-sensitive file/class name equality and emits `AssemblyHasScriptsAttribute`; `main.cs`/`Main` and `csharp_runner.cs`/`CSharpRunner` were skipped. Renamed files to `Main.cs` and `CSharpRunner.cs`, updated scenes, rebuilt, and verified generated script-path registry.
- 2026-08-21 | C# migration | C# runner initially failed 5 database assertions because `List<int>` and typed dictionary lists do not implement `IEnumerable<object>`, and test cast `List<Dictionary<...>>` to `List<object>`. Changed deserialization to non-generic `IEnumerable`; test now uses `ICollection`. Result: `128/128` passed, exit `0`.
- 2026-08-20 | K-001 | Signature: `./scripts/validate.sh` -> exit 127, `Godot 4.7.2 was not found`. Hypothesis: the wrapper only knows POSIX/editor-PATH locations while this Windows checkout has a local Godot binary. Evidence: `E:/URPG/Godot_v4.7.2/Godot_v4.7.2-stable_mono_win64_console.exe` exists and reports `4.7.2.stable.mono.official.ed1daf0bf`. Changed prerequisite: supplied `GODOT_BIN`; result: validation reached import/tests and exposed source failures. Next attempt will repair the source signatures, not retry discovery unchanged.
- 2026-08-20 | K-001 | Signature: Godot test runner -> `Parse Error: Expected closing "]" after array elements` at `src/rm2k/database/rm2k_database.gd:341`, preventing `RM2KDatabase` and `tests/core/test_rm2k_database.gd` from loading. Hypothesis: Python-style array comprehensions are not valid GDScript 4.7.2. Evidence: direct Godot load reports the exact parser location. Attempt 1: source inspection/direct load; confirmed. Next attempt will replace only the invalid serialization syntax and add focused coverage.
- 2026-08-20 | K-001 | Signature: Godot test runner -> `Could not find type "double"` at `src/core/virtual_clock.gd:54,232`, followed by Variant-inference warnings treated as errors at lines 150 and 158. Hypothesis: the stabilization patch used a non-GDScript type and generic `max()` where typed `float`/`maxi()` are required. Evidence: direct Godot load reproduces all locations. Attempt 1: source inspection/direct load; confirmed. Repair: changed the time values to `float`, made `now`/`elapsed` explicit floats, and replaced `max()` with `maxi()`. Result: targeted core suite and full validation passed.
- 2026-08-20 | K-001 | Signature: direct `godot --headless --path . --script res://src/rm2k/database/rm2k_database.gd` timed out after 120s with no further output. Cause: a pure `RefCounted` class script does not own a `SceneTree` exit path when invoked as the main script. Action: terminated by timeout and did not repeat unchanged; validation uses `tests/runner.gd`, which exits normally. Result: no source failure indicated; core suite passed.
- 2026-08-20 | K-002 | Signature: successful smoke run emitted `ERROR: Conversion failed: Unknown encoding` from `legacy_text_decoder.gd:25` on Windows for CP932 metadata. Repair: normalized CP932/SJIS aliases to the supported `SHIFT_JIS` name and added three decoder tests. Result: `95/95` core tests and the full validation pass without the diagnostic.
- 2026-08-20 | K-002 | Signature: successful VFS suite emitted six `Unexpected NUL character` parser diagnostics from the `"\\u0000"` literal in the VFS security check and its test. Repair: changed production code to byte-level NUL detection and tested the helper with `PackedByteArray` values, avoiding an engine warning while preserving the security assertion. Result: full validation pass has no NUL diagnostics.
- 2026-08-20 | K-010 | Signature: real RM2003 LDB parse rejected `class_duplicate` at offset `0x60D85` with EOF on an empty payload. Hypothesis: the valid fixture uses zero-length encoding for an empty struct array instead of BER count zero. Evidence: independent raw framing showed chunk `0x1f` length `0` and the next chunk begins exactly at `0x60D85`. Repair: accept empty struct-array payloads as count zero; keep non-empty BER/truncation checks unchanged. Result: both real LDBs/LMUs and `102/102` core tests pass.

## Recovery rule

If validation fails, keep the failure signature here. Use at most three materially different attempts for the same signature; after that mark the corresponding Kanban card blocked and continue with an independent ready card.
