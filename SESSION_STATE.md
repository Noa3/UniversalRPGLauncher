# UniversalRPG Autonomous Session State

> Updated: 2026-08-26
> Purpose: small durable checkpoint for Hermes/other autonomous agents.

## Current card

K-060, K-061, K-070, and K-071 are DONE. Compatibility profiles validate bounded schema versions, reports export deterministic redacted Markdown, the UI exposes Faithful/Enhanced rendering plus bounded integer scaling, and the RM2K input mapper handles keyboard, joypad, and bounded touch input.

|K-032, K-040, K-041, K-050, and K-055 are DONE; K-033 through K-039 and K-042 are also DONE — engine-neutral `IRuntimeSaveTools` and `IRuntimeDebugTools` gate in-memory save snapshots and local debug mutations. K-050 adds a read-only bounded original `LcfSaveData` framing model with unknown-chunk retention; semantic field mapping, save mutation, and UI integration remain separate. RM2K/RM2K3 explicitly declare `SaveLoad`/`Debugging`; debug tools are off by default.|
|midnightschool.exe (C:\Users\noa3\Desktop\Neuer Ordner (3)) analyzed detection-only: NSIS-3 Unicode installer wrapping `$PLUGINSDIR/app-64.7z` = Electron x64 distribution; `resources/app.asar` contains a complete unencrypted RPG Maker MZ 1.x game under `project/` (title: 深夜学校のパイズリ怪異, 858 files / ~238 MiB extracted to %TEMP%\midnight-extract\mzgame with standard layout index.html + js/rmmz_core.js + rmmz_managers.js + data/System.json). The extracted Electron host was externally launch-verified with process exit 0 and visually confirmed by the user. Static ASAR inspection shows `package.json` main=`src/main.js`; the host creates an Electron window and loads `project/index.html` from inside the ASAR. This proves the vendor launcher works, not a UniversalRPG runtime path; the existing MZ plugin remains detection-only and must not mark the installer EXE as directly startable.|

## Last verified baseline

`GODOT_BIN=E:/URPG/Godot_v4.7.2/Godot_v4.7.2-stable_mono_win64_console.exe ./scripts/validate.sh` passed on Windows: latest headless C# suite `284/284`, exit 0, .NET build clean. The Godot project now lives under `project/`; validate.sh handles both.

## Cross-engine QA pass (t_1b2292d4) — completed 2026-08-24

All four parent tracks (t_ae3e01c0 docs-only, t_ba1d255d RGSS XP/VX/Ace, t_dbb7d1bd Dante98/RM95, t_a37367ee WOLF) merged on base `1da7e2a`; full matrix + defect fixes in `docs/CROSS_ENGINE_QA_REPORT.md`. Defects fixed with regression tests (suite 245 → 248):
- D1: `GameDetector.FromPluginId()` was missing the Dante98 mapping — facade reported Unknown. Added case; test `Test_Dante98FacadeEngineResolution`.
- D2: bounded inspection flagged >4096-entry well-formed games as malformed, hard-failing runtime init (real XP/MZ trees are 7k+). New `partial` advisory flag distinct from malformed in `EngineDetectionContract.cs`; `RgssEngineRuntime`/`EngineBootstrapRuntime` accept partial with a Warning. Test `Test_PartialEntryBudgetDoesNotRefuseDetection`.
- D3: MV `ExtractMetadata` + shared `JsonTitle` used first-match regex for `"gameTitle"`, so nested keys could shadow the top-level title. Switched to bounded System.Text.Json root-property read (MaxDepth 64, malformed → empty). Test `Test_MvMetadataTitleIgnoresNestedGameTitleKeys`.
Residual: RM95/Dante/WOLF have no live on-disk fixtures (plugin tests + audit doc only); RGSS/MV/MZ real-fixture runs were detection/metadata-only this pass.

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
- RM95, RGSS, MV, MZ, and Unite remain detection-only; WOLF exposes an explicitly unencrypted plain-data slice, and RM2K/RM2K3 retain the parser-backed runtime bootstrap.
- Rewired `GameDetector`, `GameLibrary`, `RuntimeLauncher`, and the Godot UI to preserve ranked detection reports, persist import metadata, and refuse unsafe/unsupported runtime selection without external fallback.
- Added contract, detection, archive, persistence, ambiguity, platform, and lifecycle regression coverage.
- Added RGSS and WOLF regression fixtures/tests; RGSS selector refusal is verified and validation passes with Godot 4.7.2 Mono: `279/279` tests.
- Nullable contracts were hardened across C# core/UI/test code; `.NET` build now reports `0` warnings and `0` errors.

## Current action

K-022, K-030, and K-031 are complete for their bounded slices; the runtime update/scheduler integration and lifecycle reset evidence were extended in the current slice. `GameSimulationState` supports bounded map configuration and movement. `Rm2kEngineRuntime` now bridges LMU geometry/map ID/start-map diagnostics into simulation, creates a bounded `VirtualFramebuffer` from validated lower/upper layers, builds bounded player/event sprite descriptors through `Rm2kSpriteAdapter`, and forwards decoded events to `Rm2kEventScheduler`; `Update()` drives native autorun commands through the deterministic clock; `Stop()` clears scheduler, clock, presentation, simulation, loaded map data, framebuffer, and sprite-descriptor state. `VirtualFramebuffer`/`Rm2kRendererAdapter` assemble validated lower/upper layers; sprite/camera adapters remain bounded and data-only.

Fixture reconnaissance: `D:\NextCloud\Games\PornGames\SkiesInflateableAdventure` is an unencrypted RPG Maker MZ tree (`index.html`, `js/rmmz_core.js`, `js/rmmz_managers.js`, `data/System.json`, title `Skie's Inflatable Adventures (v0.30.001)`, 7,039 files). `D:\NextCloud\Games\PornGames\IntheHamletofLoliBigtits_v103a` is not an MZ web tree at its root: no `index.html`, `js/rmmz_*`, or `data/System.json`; Japanese locale remains unconfirmed and no encrypted marker was found in the bounded filename scan. Both were inspected detection-only; no game code executed.

## Completed K-055

- Added bounded `TryWriteFile`/`TryReadFile` APIs to the JSON-only `Rm2kSimulationSaveCodec` for runtime-owned slot files.
- Slot paths are confined under the caller-supplied directory; invalid names and traversal are rejected before I/O.
- Writes serialize to a temporary file and replace the target; temporary cleanup is attempted after success/failure.
- Regression coverage verifies slot round-trip, gold preservation, traversal rejection, and cleanup.
- This does not parse or write original RM2K/RM2K3 `LSD` saves.

## Completed K-040

- Added explicit in-memory `RtpRegistry`/`RtpProfile` registration and deterministic asset resolution by engine, generation, dependency, and bounded relative path.
- Rejects invalid identifiers, missing or reparse-point roots, duplicate profile IDs, traversal/absolute/NUL paths, and reparse-point escapes.
- Resolution only checks file existence and returns a structured result; it never opens, parses, downloads, or executes RTP data.
- K-041 now provides the follow-up missing-asset diagnostics and bounded per-game metadata.

## Completed K-041

- Added bounded `RtpGameProfile` metadata and a JSON codec with payload/list/path limits.
- Added `RtpAssetDiagnostics` with distinct `Available`, `MissingAsset`, `NoMatchingProfile`, and `InvalidPath` statuses.
- Diagnostics use the explicit registry only and never open, parse, download, or execute RTP assets.
- Profile metadata is not yet persisted into `GameLibrary` records; that remains a separate integration decision.

## Completed K-050

- Added read-only `Rm2kLsdSaveCodec` and typed `Rm2kLsdSaveModel` over the existing bounded LCF reader.
- Preserves chunk IDs, lengths, offsets, payload bytes, and unknown-chunk count; rejects invalid paths, malformed/truncated framing, missing terminators, oversized files, and oversized chunks.
- No event commands, scripts, plugins, or native content are executed; original saves are never written.
- Validation: analyzer build clean and full headless suite `279/279` passed.

## Latest completed lifecycle slice (2026-08-28)

- `EnginePluginHost` now permits `Stopped → Start`.
- A stopped runtime is disposed exactly once before a fresh runtime is selected and initialized; stopped runtime objects are never re-initialized.
- `Test_Rm2kRuntimeCanRestartAfterStopWithFreshRuntimeState` verifies `Start → Update → Stop → Start`, fresh framebuffer/map state, clock reset, scheduler reload, and distinct runtime identity against the real RM2K fixture.
- Focused result: `TestPluginDetection 22/22`; canonical result: `All 280 tests passed`.

## Latest completed sprite synchronization slice (2026-08-28)

- `Main._UnhandledInput` now routes movement through `Rm2kEngineRuntime.TryMove()`.
- The runtime refreshes bounded player/event descriptors only after successful movement, preserving the parser map as the event-data source and avoiding direct UI mutation of simulation/render state.
- `Test_Rm2kRuntimeMovementSynchronizesPlayerSpriteDescriptor` covers movement and descriptor position synchronization against the real RM2K fixture.
- Focused result: `TestPluginDetection 23/23`; canonical result: `All 281 tests passed`.

## Latest completed pending-transfer validation slice (2026-08-28)

- `EventInterpreter` rejects map IDs outside `1..GameSimulationState.MaxMapId` and negative transfer coordinates before mutating pending state.
- Invalid requests preserve an existing pending transfer and emit bounded diagnostics.
- `Test_TeleportRejectsInvalidMapIdsWithoutOverwritingPendingState` covers the contract.
- Focused result: `TestEventInterpreter 34/34`; canonical result: `All 282 tests passed`.

## Latest completed transfer-facing validation slice (2026-08-28)

- `EventInterpreter` validates the optional transfer facing parameter against RM2K directions `2/4/6/8` before mutating state.
- Valid facing is applied; invalid facing preserves the existing direction and pending transfer atomically.
- `Test_TeleportAppliesValidFacingAndRejectsInvalidFacingAtomically` covers the contract.
- Focused result: `TestEventInterpreter 35/35`; canonical result: `All 283 tests passed`.

## Latest completed choice lifecycle slice (2026-08-28)

- `EventInterpreter` clears `PresentationState.ActiveChoice` after a valid selection is confirmed and logged.
- `Test_ShowChoicePausesUntilSelection` verifies that the interpreter advances without leaving stale choice UI state.
- Focused result: `TestEventInterpreter 35/35`; canonical result: `All 283 tests passed`.

## Latest completed InputNumber lifecycle slice (2026-08-29)

- `EventInterpreter` pauses an `InputNumber` command when a different variable already owns the pending presentation input.
- The existing pending variable/value remain unchanged; no conflicting variable is created or mutated.
- `Test_InputNumberDoesNotConsumePendingValueForDifferentVariable` covers the conflict contract.
- Focused result: `TestEventInterpreter 36/36`; canonical result: `All 284 tests passed`.

## Latest completed ChangeGold interpreter slice (2026-08-29)

- `EventInterpreter` now handles verified RM2K command `10310` (`ChangeGold`) with EasyRPG semantics: operation `0` adds and operation `1` subtracts; operands may be constants or bounded variables.
- Gold is clamped to the modeled RM2K range `0..999999`; malformed parameters, invalid operand variables, and unsupported operations fail closed with bounded diagnostics.
- `Test_ChangeGoldAddsConstantOperand`, `Test_ChangeGoldClampsToBoundedRange`, `Test_ChangeGoldSubtractsAndClampsBelowZero`, `Test_ChangeGoldReadsVariableOperand`, and `Test_ChangeGoldRejectsInvalidParametersFailClosed` cover the new command path and bounds.
- Focused result: `TestEventInterpreter 41/41`; canonical result: `All 289 tests passed`; build and `scripts/validate.sh` passed.
- Chipset passability remains intentionally fail-closed: `PassabilityLayer` has no verified LMU/Chipset parser source yet. Unblock requires a verified liblcf/EasyRPG field mapping plus a fixture distinguishing passable and impassable tiles.

## Next action

Continue with the next RM2K/2003 runtime slice only after its command/data semantics and regression oracle are verified. RGSS remains detection-only until a bounded Ruby implementation exists; its former metadata bootstrap is retained only as unregistered code and is not startable through the runtime selector.

## Audit note 2026-08-26

The completed-card audit found and corrected an unsafe RGSS capability claim: XP/VX/VX Ace were marked `Runtime` even though no bounded Ruby interpreter exists. `RgssPlugin` now exposes only `Detection | Parsing`, and `TestRgssRuntime` verifies selector refusal with `UnsupportedEngine`. Current canonical validation is `279/279`. The scheduler now stops source enumeration at its bounded event cap and diagnoses truncation.

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
