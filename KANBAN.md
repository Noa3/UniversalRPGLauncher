# UniversalRPG Autonomous Kanban

> Updated: 2026-08-24
> Owner: autonomous agent/Hermes
> Ordering: lowest priority number first, then card ID.

## Workflow states

`BACKLOG` → `READY` → `IN PROGRESS` → `VERIFY` → `DONE`

Use `BLOCKED` only with evidence and a concrete unblock condition. Keep at most one implementation card `IN PROGRESS` at a time.

## Active board

| ID | P | State | Card | Depends on |
|---|---:|---|---|---|
| K-001 | 0 | DONE | Validate 2026-08-20 stabilization changes on Godot 4.7.2 | — |
| K-002 | 0 | DONE | Harden core test baseline and eliminate remaining parser/runtime compile warnings | K-001 |
| K-003 | 0 | DONE | Replace superseded GDScript implementation with validated C#/.NET runtime | K-002 |
| K-004 | 0 | DONE | Integrate trusted engine plugin catalog, bounded detection, import persistence, and safe runtime selection | K-003 |
| K-010 | 0 | DONE | Validate LCF reader/parser against legal real-world RM2K/2003 fixtures | K-001 |
| K-011 | 0 | DONE | Implement LMT map-tree parser with bounded BER/structure handling | K-010 |
| K-012 | 0 | DONE | Expand LDB decoding into typed core database sections | K-010 |
| K-013 | 0 | DONE | Expand LMU event/page metadata decoding without executing commands | K-010 |
| K-014 | 0 | DONE | Preserve unknown LCF fields/chunks for diagnostics and forward compatibility | K-010 |
| K-015 | 0 | DONE | Decode remaining LDB array sections into typed models | K-012 |
| K-016 | 0 | DONE | Prioritized RPG Maker MZ detection and bounded metadata inspection | K-004 |
| K-017 | 2 | DONE | Bounded MZ data-directory metadata inspection (Actors/MapInfos/encrypted assets) | K-016 |
| K-018 | 2 | DONE | Complete MZ database inventory (section counts, system name arrays, map files) | K-017 |
| K-019 | 1 | DONE | ConditionalBranch condition evaluation (switch/variable comparisons) | K-023 |
| K-020 | 1 | DONE | Define faithful RM2K/2003 simulation state model | K-011,K-012,K-013 |
| K-021 | 1 | DONE | Implement first event-interpreter slice: message/switch/variable/branch/wait/transfer | K-020 |
| K-023 | 2 | DONE | Replace placeholder interpreter opcodes with verified RM2K/2003 command codes | K-021 |
| K-024 | 2 | DONE | Move Godot project into `project/` and keep runtime/tooling at repo root | — |
| K-022 | 1 | DONE | Implement map/player movement and passability simulation | K-020 |
| K-030 | 1 | DONE | Godot renderer adapter: virtual framebuffer + lower/upper tile layers | K-020 |
| K-031 | 1 | DONE | Character/event sprite renderer and camera | K-030 |
| K-032 | 1 | DONE | Message/window/picture/choice/input presentation and runtime/UI handoff | K-030,K-021 |
| K-033 | 1 | DONE | Visible RM2K map/framebuffer and sprite overlay in runtime UI | K-030,K-031,K-032 |
| K-034 | 1 | DONE | Safe keyboard movement handoff to RM2K simulation | K-022,K-033 |
| K-035 | 1 | DONE | Keyboard message dismissal, choice navigation, and numeric input handoff | K-032,K-034 |
| K-036 | 1 | DONE | Advance deterministic runtime simulation frame count from virtual clock | K-020,K-034 |
| K-037 | 1 | DONE | Clickable message, choice, and numeric-input presentation controls | K-032,K-035 |
| K-038 | 1 | DONE | Avoid per-frame choice-control reconstruction in runtime UI | K-037 |
| K-039 | 1 | DONE | Expose explicit runtime stop control and hide stale presentation controls | K-037,K-038 |
| K-040 | 1 | DONE | RTP registry/resolver without bundled proprietary RTP data | K-012 |
| K-041 | 1 | DONE | Missing-asset diagnostics and per-game RTP profile | K-040 |
| K-042 | 1 | DONE | RM2K event-page selection and bounded trigger scheduler | K-020,K-021 |
| K-043 | 1 | DONE | Decode LMU event-command vectors and feed native scheduler | K-042 |
| K-044 | 1 | DONE | Dispatch action/touch events from player input and movement | K-042,K-043 |
| K-045 | 1 | DONE | Decode LMU event-page switch and variable conditions | K-042,K-043 |
| K-046 | 1 | DONE | Complete selector evaluation for switch B and variable comparisons | K-045 |
| K-047 | 1 | DONE | Diagnose unsupported RM2K commands without execution | K-043 |
| K-048 | 1 | DONE | Separate LMU move-route and event-command presence metadata | K-045 |
| K-049 | 1 | DONE | Evaluate bounded RM2K item and actor page conditions | K-045 |
| K-051 | 1 | DONE | Add deterministic RM2K Timer 1/Timer 2 conditions | K-045 |
| K-053 | 1 | DONE | Adaptive application render FPS without changing simulation Hz | K-036 |
| K-052 | 1 | DONE | Add bounded JSON simulation save/load roundtrip | K-020 |
| K-054 | 1 | DONE | Add capability-gated RM2K save/debug tool contracts | K-052 |
| K-055 | 1 | DONE | Add bounded runtime-owned RM2K JSON save-directory slots | K-052 |
| K-050 | 2 | DONE | Original-format read-only LSD save model and safe save directory integration | K-020 |
| K-060 | 2 | DONE | Game compatibility profile schema versioning/validation | K-002 |
| K-061 | 2 | DONE | Compatibility report export for GitHub issues | K-060 |
| K-070 | 3 | DONE | Faithful-vs-Enhanced profile and integer scaling controls | K-030 |
| K-071 | 3 | DONE | Controller/touch remapping layer | K-020 |
| K-080 | 4 | BACKLOG | RGSS architecture spike after RM2K/2003 playable milestone | RM2K playable milestone |
| K-090 | 4 | BACKLOG | MV/MZ JavaScript runtime architecture spike | RM2K playable milestone |
| K-100 | 5 | BACKLOG | PE/DLL inspector research and safe metadata-only parser | Stable primary runtimes |

## Card details

### K-022 — Map/player movement and passability simulation

**Acceptance criteria**
- Configure bounded map dimensions and row-major passability data.
- Move only one cardinal tile per call; update facing using RM direction codes 2/4/6/8.
- Reject map bounds, impassable tiles, malformed passability lengths, diagonal moves, and invalid map dimensions without changing position.
- Increment `Steps` only after successful movement; retain bounded diagnostics for blocked/rejected movement.

**Progress evidence (2026-08-24)**
- Added `GameSimulationState.ConfigureMap` and `TryMove`.
- Added regression coverage for successful movement, facing, blocked tiles, map bounds, diagonal rejection, and passability-shape validation.
- `dotnet build project/UniversalRPG.csproj --no-restore` — passed, 0 warnings, 0 errors.
- `GODOT_BIN=E:/URPG/Godot_v4.7.2/Godot_v4.7.2-stable_mono_win64_console.exe ./scripts/validate.sh` — passed; `213/213` tests.
- RM2K-specific chipset passability decoding remains separate: current implementation intentionally does not invent unverified chipset rules.

### K-031 — Character/event sprite renderer and camera

**Acceptance criteria**
- Produce bounded player and map-event sprite descriptors from parsed map data.
- Reject malformed events and coordinates outside map bounds.
- Maintain camera center clamped to map and viewport bounds.
- Keep texture loading and foreign game-code execution outside this data adapter.

**Validation evidence (2026-08-24)**
- Added `project/src/rm2k/rendering/Rm2kSpriteRenderer.cs` and `project/tests/core/test_rm2k_sprite_renderer.cs`.
- Build: `dotnet build project/UniversalRPG.csproj --no-restore` — 0 warnings, 0 errors.
- Full validation: `GODOT_BIN=E:/URPG/Godot_v4.7.2/Godot_v4.7.2-stable_mono_win64_console.exe ./scripts/validate.sh` — exit 0, `221/221` tests passed.
- Scope boundary: descriptors/camera only; no untrusted asset/script/native execution.

### K-032 — Message/window/picture presentation layer

**Acceptance criteria**
- Store bounded message state and continuation text.
- Store, replace, and erase bounded picture descriptors.
- Allow `EventInterpreter` to publish ShowMessage output into presentation state through explicit dependency injection.
- Reject oversized or malformed presentation data without executing foreign code.

**Validation evidence (2026-08-24)**
- Added `project/src/rm2k/presentation/PresentationState.cs` and `project/tests/core/test_presentation_state.cs`.
- `EventInterpreter` now optionally receives `PresentationState`; ShowMessage updates it while retaining existing diagnostics behavior.
- Build: `dotnet build project/UniversalRPG.csproj --no-restore` — 0 warnings, 0 errors.
- Full validation: `GODOT_BIN=E:/URPG/Godot_v4.7.2/Godot_v4.7.2-stable_mono_win64_console.exe ./scripts/validate.sh` — exit 0, `225/225` tests passed.
- Scope boundary: no texture loading, external scripts, native plugins, or game executables are invoked.

### K-040 — RTP registry/resolver without bundled proprietary RTP data

**Acceptance criteria**
- Register only explicit user-provided RTP roots; do not bundle, download, or auto-discover proprietary RTP data.
- Resolve assets by engine, generation, dependency name, and bounded relative path in deterministic registration order.
- Reject absolute paths, traversal, NUL bytes, invalid identifiers, missing roots, duplicate profile IDs, and reparse-point escapes.
- Return structured status for no profile, missing asset, invalid path, and successful resolution without opening or executing the asset.

**Validation evidence (2026-08-24)**
- Added `project/src/rm2k/assets/RtpRegistry.cs` and `project/tests/core/test_rtp_registry.cs`.
- Build: `dotnet build project/UniversalRPG.csproj --no-restore` — 0 warnings, 0 errors.
- Full validation: `GODOT_BIN=E:/URPG/Godot_v4.7.2/Godot_v4.7.2-stable_mono_win64_console.exe ./scripts/validate.sh` — exit 0, `254/254` tests passed.
- Scope boundary: K-040 is an in-memory explicit registry only; diagnostics integration and persisted per-game RTP profiles remain K-041.

### K-041 — Missing-asset diagnostics and per-game RTP profile

**Acceptance criteria**
- Represent a bounded per-game RTP profile without copying or embedding RTP data.
- Serialize and deserialize profile metadata through a bounded JSON codec with validation.
- Report required assets as `Available`, `MissingAsset`, `NoMatchingProfile`, or `InvalidPath`.
- Keep diagnostics data-only; no asset opening, parsing, downloading, or execution.

**Validation evidence (2026-08-24)**
- Added `project/src/rm2k/assets/RtpDiagnostics.cs` and `project/tests/core/test_rtp_diagnostics.cs`.
- Build: `dotnet build project/UniversalRPG.csproj --no-restore` — 0 warnings, 0 errors.
- Full validation: `GODOT_BIN=E:/URPG/Godot_v4.7.2/Godot_v4.7.2-stable_mono_win64_console.exe ./scripts/validate.sh` — exit 0, `258/258` tests passed.
- Scope boundary: profile metadata is not yet wired into persisted `GameLibrary` records; that integration remains a follow-up if required by the save/runtime UI.

### K-030 — Godot renderer adapter

**Acceptance criteria**
- Store lower and upper RM2K tile IDs in a deterministic virtual framebuffer.
- Convert bounded parser map output into the framebuffer without executing game code.
- Reject malformed dimensions, layer lengths, non-integer tile IDs, and negative tile IDs.
- Keep Godot rendering APIs out of the parser-facing adapter; actual texture/tile drawing remains a later presentation slice.

**Validation evidence (2026-08-24)**
- Added `project/src/rm2k/rendering/VirtualFramebuffer.cs` and `project/tests/core/test_rm2k_renderer.cs`.
- Build: `dotnet build project/UniversalRPG.csproj --no-restore` — 0 warnings, 0 errors.
- Full validation: `GODOT_BIN=E:/URPG/Godot_v4.7.2/Godot_v4.7.2-stable_mono_win64_console.exe ./scripts/validate.sh` — exit 0, `217/217` tests passed.
- Scope boundary: this is renderer-neutral framebuffer assembly; no chipset passability inference, texture loading, camera, or native/game-script execution was added.

### K-001 — Validate stabilization changes

**Acceptance criteria**
- `./scripts/validate.sh` runs with Godot 4.7.2 stable.
- Import/syntax validation succeeds.
- C# core/smoke runner passes under Godot .NET.
- Smoke runner passes.
- Any newly found regression gets its own test before the fix is marked complete.

**Failure policy**
Do not remove new regression tests to restore green status. Use the anti-loop policy in `AGENTS.md`.

**Validation evidence (2026-08-20)**
- `./scripts/validate.sh` — passed with Godot `4.7.2.stable.mono.official.ed1daf0bf`.
- Import/syntax validation — passed.
- Core suite — `92/92` tests passed.
- Smoke suite — passed.
- Repaired GDScript parser compatibility in `RM2KDatabase` and `VirtualClock`; added database serialization regression coverage and Windows Godot discovery candidates.

### K-002 — Harden core baseline

**Acceptance criteria**
- No known GDScript parse errors in source files reachable by the app/tests.
- Core abstractions have deterministic tests for documented behavior.
- Documentation accurately states current test count/status.
- C# migration is tracked and validated by K-003.

**Validation evidence (2026-08-20)**
- `./scripts/validate.sh` — passed with Godot `4.7.2.stable.mono.official.ed1daf0bf`.
- Core suite — `95/95` tests passed, including the new legacy-decoder suite.
- Removed the unsupported CP932 conversion attempt on Windows by normalizing CP932/SJIS aliases to `SHIFT_JIS`.
- Replaced the GDScript `"\\u0000"` source literal with byte-level NUL detection; the VFS security regression remains covered without parser diagnostics.
- Remaining non-fatal output is limited to intentional invalid-input diagnostics and Godot's `EditorSettings` headless-editor message.

### K-003 — C#/.NET migration

**Acceptance criteria**

- `dotnet build UniversalRPG.csproj` passes with zero errors.
- Godot .NET headless runner instantiates scene scripts and passes all ported tests.
- Superseded source, application, and test `.gd` files are removed.
- Scenes, validation script, and active documentation reference C# paths.

**Validation evidence (2026-08-21)**

- Godot `4.7.2.stable.mono.official.ed1daf0bf` instantiated `tests/CSharpRunner.cs` after PascalCase file renames required by `ScriptPathAttributeGenerator`.
- C# runner passed `128/128` tests with exit code `0`.
- `scripts/validate.sh` now runs .NET restore/build, Godot import, and the C# runner.

### K-004 — Engine plugin foundation and application wiring

**Acceptance criteria**
- Trusted compiled plugin contracts expose metadata, capabilities, probe results, runtime lifecycle, and typed diagnostics.
- Built-in descriptors cover RM95, RM2K, RM2K3, XP, VX, VX Ace, MV, MZ, WOLF, and Unite research detection. RM95/RGSS/MV/MZ/Unite remain detection-only; WOLF has an explicitly unencrypted plain-data slice, and RM2K/RM2K3 additionally parse LDB/LMT/LMU data.
- Detection uses bounded read-only folder/ZIP inspection and retains ranked candidates, evidence, ambiguity, malformed-input, and unknown diagnostics.
- Library import/scan persists versioned detection metadata and revalidates persisted selections on relaunch.
- Runtime selection refuses ambiguous, unknown, malformed, detection-only, missing, capability-incompatible, platform-incompatible, and probe-failing candidates without external fallback.
- Godot UI displays plugin/candidate status and structured diagnostics.

**Validation evidence (2026-08-21)**
- `dotnet build UniversalRPG.csproj --no-restore` — passed with `0` warnings and `0` errors after nullable-contract hardening.
- `GODOT_BIN=E:/URPG/Godot_v4.7.2/Godot_v4.7.2-stable_mono_win64_console.exe ./scripts/validate.sh` — passed; `159/159` C# tests including RGSS/WOLF slices.
- Detection never executes imported EXE, DLL, Ruby, JavaScript, shell, or native plugin files; ZIPs are inspected without extraction. RM2K/RM2K3 runtime tests load only validated fixture data and advance the deterministic clock.

### K-010 — Real LCF validation

**Acceptance criteria**
- Add legal/reproducible fixture provenance notes.
- Verify LDB and LMU headers/chunk boundaries on at least two independent fixtures where available.
- Parser must reject truncation, invalid BER, oversized chunks and unreasonable dimensions without crashes or unbounded allocation.
- Unknown fields are retained or reported rather than silently interpreted as known data.

**Validation evidence (2026-08-20)**
- Added pinned, hashed RM2000 and RM2003 LDB/LMU/LMT fixtures from `EasyRPG/TestGame` commit `4f7a35b2b3f6ef3cdd3ae22f2f616cfb0e5e8313`; provenance is in `tests/fixtures/easyrpg-testgame/README.md`.
- Real-fixture tests verify both LDBs and both LMUs, exact file sizes, headers, chunk counts, terminator behavior, and reader position at EOF: `5/5` real-fixture tests passed.
- Full core suite: `102/102` tests passed; full `./scripts/validate.sh` passed.
- Repaired valid zero-length RM2003 struct-array sections and added unknown top-level chunk retention coverage.

### K-011 — LMT map tree

**Acceptance criteria**
- Parse `LcfMapTree` container safely.
- Extract map IDs, names, parent relationship and start-position metadata that is verified against fixtures/documentation.
- Detect cycles/invalid parent references defensively.
- Unit tests cover valid, empty, truncated and malicious-size fixtures.

**Validation evidence (2026-08-20)**
- `./scripts/validate.sh` — passed with Godot `4.7.2.stable.mono.official.ed1daf0bf`.
- Core suite — `109/109` tests passed, including real LMT and bounded malformed-input coverage.
- Implemented `parse_map_tree()` with verified LMT field IDs, signed RM2000 map IDs, parent/tree-order validation, cycle detection, and raw unknown-field retention.

### K-012 — Typed LDB sections

**Acceptance criteria**
- Decode sections incrementally into typed data models.
- Every decoded field has a verified LCF field ID/source; no guessed offsets.
- Unknown fields remain preserved for diagnostics.
- Synthetic fixtures and at least one real fixture comparison exist.

**Validation evidence (2026-08-22)**
- `bash scripts/validate.sh` passed with Godot `4.7.2.stable.mono` on Linux; headless C# suite `165/165`.
- Actors section decodes to typed entries with verified liblcf field IDs (`src/generated/lcf/ldb/chunks.h`, `ChunkActor`): strings 0x01/0x02/0x03/0x0F, integers 0x04/0x05/0x07/0x08/0x09/0x0A/0x10; defaults mirror `rpg::Actor` initializers.
- Switches/variables decode as id/name entries (`ChunkSwitch`/`ChunkVariable`: name=0x01); duplicate structure IDs are rejected.
- Unknown actor/entry fields retained per entry; synthetic tests cover defaults, unknown retention, duplicate IDs, missing terminators.
- Real-fixture comparison: typed entry counts equal `section_counts` on both pinned EasyRPG TestGame LDBs.
- Scope note: per agent maintenance rules the remaining array sections were split into successor card K-015; this card is done for actors/switches/variables plus framing already covered earlier.

### K-015 — Remaining typed LDB array sections

**Acceptance criteria**
- Decode skills, items, enemies, troops, terrains, attributes, states, animations, chipsets, classes, and battle commands incrementally using field IDs verified against liblcf `ldb/chunks.h`.
- Nested structures stay data-only; unknown fields remain preserved.
- Synthetic malformed-input fixtures and real-fixture count comparisons exist per section batch.

**Progress evidence (2026-08-22)**
- Implemented the first K-015 batch for skills (`0x0c`), items (`0x0d`), states (`0x12`), and classes (`0x1e`). Scalar field IDs are verified against EasyRPG liblcf; nested arrays remain preserved as unknown fields.
- Added synthetic typed-section coverage for names, scalar values, unknown-field retention, duplicate-safe framing, and section-count parity.
- `dotnet build --no-restore` — passed with `0` warnings and `0` errors.
- `GODOT_BIN=E:/URPG/Godot_v4.7.2/Godot_v4.7.2-stable_mono_win64_console.exe ./scripts/validate.sh` — passed; `166/166` C# tests and smoke validation.
- Implemented the second K-015 batch for enemies (`0x0e`), terrains (`0x10`), and attributes (`0x11`). Scalar field IDs are verified against EasyRPG liblcf; nested arrays remain preserved as unknown fields.
- Added synthetic typed-section coverage for names, combat/environment scalar values, unknown-field retention, and section-count parity.
- `GODOT_BIN=E:/URPG/Godot_v4.7.2/Godot_v4.7.2-stable_mono_win64_console.exe ./scripts/validate.sh` — passed; `167/167` C# tests and smoke validation.
- Implemented the third K-015 batch for troops (`0x0f`), animations (`0x13`), and chipsets (`0x14`). Scalar metadata is typed; nested members, frames, and tile arrays remain preserved as unknown fields.
- Added synthetic typed-section coverage for presentation metadata, nested-field retention, and section-count parity.
- `GODOT_BIN=E:/URPG/Godot_v4.7.2/Godot_v4.7.2-stable_mono_win64_console.exe ./scripts/validate.sh` — passed; `168/168` C# tests and smoke validation.
- Implemented the fourth K-015 batch for battle commands (`0x1d`). Scalar metadata uses verified liblcf field IDs; nested command data remains preserved as unknown fields and trailing data is rejected.
- Added synthetic battle-command coverage and extended real-fixture count parity to every typed LDB array section.
- `GODOT_BIN=E:/URPG/Godot_v4.7.2/Godot_v4.7.2-stable_mono_win64_console.exe ./scripts/validate.sh` — passed; `170/170` C# tests and smoke validation.
- K-015 acceptance criteria are complete; K-015 is `DONE`. K-016 is now the active MZ-priority card.

### K-016 — Prioritized RPG Maker MZ detection and bounded metadata inspection

**Acceptance criteria**
- Strengthen MZ detection using the MZ runtime layout and `data/System.json`; MV signatures must not be accepted as MZ.
- Inspect bounded MZ metadata only; never execute `index.html`, `rmmz_*.js`, `plugins.js`, native binaries, or external runtimes.
- Keep MZ detection-only and non-launchable until a separately verified JavaScript runtime exists.
- Add positive, negative, malformed, and oversized metadata regression coverage.
- Update detection/security documentation with the exact supported boundary.

**Progress evidence (2026-08-22)**
- Added MZ-specific validation on top of the shared web detector: `rmmz_core.js`, `rmmz_managers.js`, and bounded `data/System.json` JSON-object validation are required.
- MV remains on the generic `rpg_core.js` path and is not affected by the MZ-only checks.
- Added positive, missing-manager, malformed-JSON, and oversized-metadata fixtures; no JavaScript, HTML, native binary, or external runtime is executed.
- `GODOT_BIN=E:/URPG/Godot_v4.7.2/Godot_v4.7.2-stable_mono_win64_console.exe ./scripts/validate.sh` — passed; `171/171` C# tests and smoke validation.
- Typed bounded MZ metadata extraction and encrypted-asset diagnostics landed; K-016 is `DONE`.

### K-021 — First event-interpreter slice

**Acceptance criteria**
- Interpret message, wait, if/else/endIf, loop/breakLoop commands deterministically without side effects beyond the simulation state.
- Interpret switch, variable, and transfer-player commands against the bounded `GameSimulationState`.
- Malformed or out-of-range payloads produce diagnostics and are skipped safely; no crashes, no unbounded loops.
- Regression coverage for each command family including malformed payloads.

**Progress evidence (2026-08-23)**
- Fixed the Variant cast in `GetCmdParams` (build blocker) and removed the dead `_shouldBreak` field.
- Added dispatch plus bounded executors for `ControlSwitches`, `ControlVariables` (set/add/sub/mul/div/mod with division-by-zero diagnostic), and `TransferPlayer` (pending-transfer state).
- Removed the placeholder move-route case whose opcode literal collided with `ControlSwitches` (`CS0152`).
- Placeholder opcode constants (101–118, 105–107) documented as such; migration is tracked as K-023.
- `bash scripts/validate.sh` — passed; `198/198` C# tests and smoke validation after the K-023 opcode migration (typed EventCommand model).

### K-024 — Repository layout split

**Acceptance criteria**
- Godot project (project.godot, csproj/sln, app/, src/, tests/, assets/, locale/, scenes/, plugins/) lives under `project/`.
- Repo root keeps development elements: docs, notes, `docs/`, `scripts/`, and the pinned Godot runtime under `tools/godot/`.
- `scripts/validate.sh` runs restore/build/import/tests from the new layout unchanged for CI.

**Progress evidence (2026-08-23)**
- Moved project files via `git mv`; `.godot` cache regenerated inside `project/`.
- `validate.sh` now builds and runs Godot with `--path "$ROOT_DIR/project"`; Godot binary discovery still uses root `tools/godot/editors/4.7.2/`.
- Full validation green: `199/199`.

### K-017 — Bounded MZ data-directory metadata inspection

User-directed MZ slice (extends the K-016 line); stays detection/metadata-only.

**Acceptance criteria**
- Decode bounded metadata from `data/Actors.json` and `data/MapInfos.json` via a real JSON parser: entry counts plus the first 32 names, name length capped.
- Per-file size cap with truncation/oversize rejection; malformed or non-array JSON yields a per-file diagnostic instead of failing detection.
- MZ-specific encrypted assets are detected by their real extensions (`.rpgmvp`, `.rpgmvo`, `.rpgmvm`) and reported diagnostically; no decryption, no execution.
- Snapshots without the `rmmz_core.js`/`rmmz_managers.js` runtime signature are refused (MV folders cannot be inspected as MZ).
- Regression coverage for happy path, missing files, malformed JSON, non-array JSON, encrypted assets, and MV-refusal.

**Progress evidence (2026-08-23)**
- Added `MzDataDirectoryResult.Extract(GameInspectionSnapshot)` in `project/src/plugins/BuiltInEnginePlugins.cs`; JSON parsed with Godot's `Json` parser under strict bounds (2048 KiB/file, 9999 actors, 9999 maps).
- Added `TestMzDataDirectory` suite with five tests over synthetic MZ/MV game folders; suite total `205/205`.
- `bash scripts/validate.sh` — passed; `203/203` C# tests and smoke validation.

### K-019 — ConditionalBranch condition evaluation (DONE)

Implements EasyRPG `CommandConditionalBranch` (code 12010) semantics for the two condition types the deterministic core can model.

**Acceptance criteria**
- Type 0 (switch): switch state compared against ON/OFF polarity (`parameters[2] == 0` means "is ON").
- Type 1 (variable): variable vs constant or variable operand with the six CheckOperator comparisons (==, >=, <=, >, <, !=).
- Unsupported types (timer/gold/item/actor) evaluate false with a diagnostic; else path is taken deterministically.
- True path runs then-body and skips else via matching EndBranch; false path jumps to ElseBranch or EndBranch; nesting handled by depth counting, not indent.
- Regression coverage: switch polarity, false-runs-else, variable operators, var-vs-var operand with nested branch, unsupported-type diagnostic.

**Status (audited 2026-08-26) — DONE**
- Implementation is complete in `project/src/rm2k/interpreter/EventInterpreter.cs`; current suite executes the five conditional-branch regression tests.
- Current canonical validation: `All 279 tests passed`.
- Remaining boundary: timer/gold/item/actor conditions outside the modeled state remain diagnostic-only.

### K-018 — Complete MZ database inventory

User-directed MZ slice; extends K-017, stays metadata-only.

**Acceptance criteria**
- Entry counts for present optional database sections (Classes, Skills, Items, Weapons, Armors, Enemies, Troops) under the same bounds; absent sections are omitted silently (trimmed games are normal).
- System.json `switches`/`variables` name-array counts with the bounded cap.
- Physical `data/Map###.json` file count (3-4 digit numeric stems only), capped at 1000.
- Malformed or oversized optional sections produce per-file diagnostics without affecting sibling sections or detection.

**Progress evidence (2026-08-23)**
- Extended `MzDataDirectoryResult` with `SectionCounts`, `SwitchNameCount`, `VariableNameCount`, and `MapFileCount`.
- Added two inventory tests; malformed-JSON engine log lines from `Json.ParseString` on deliberately broken fixtures are expected and asserted via diagnostics.
- `bash scripts/validate.sh` — passed; `205/205` C# tests and smoke validation.

### K-055 — Bounded runtime-owned RM2K JSON save-directory slots

**Acceptance criteria**
- Write and read the existing bounded JSON simulation snapshot through an explicitly supplied save directory and slot name.
- Reject empty/invalid slot names and path traversal without touching files outside the save directory.
- Use a temporary file followed by replacement, clean up temporary files after the operation, and return I/O/validation failures as diagnostics.
- Keep this separate from original RM2K/RM2K3 `LSD` compatibility; do not overwrite original game saves.

**Validation evidence (2026-08-24)**
- Added `TryWriteFile` and `TryReadFile` to `project/src/rm2k/simulation/Rm2kSimulationSaveCodec.cs`.
- Added bounded slot round-trip/traversal regression coverage in `project/tests/core/test_game_simulation_state.cs`.
- `dotnet build project/UniversalRPG.csproj --no-restore /p:RunAnalyzers=true /p:RunAnalyzersDuringBuild=true` — passed with 0 warnings and 0 errors.
- `GODOT_BIN=E:/URPG/Godot_v4.7.2/Godot_v4.7.2-stable_mono_win64_console.exe ./scripts/validate.sh` — passed; `244/244` tests.
### K-050 — Original-format read-only LSD save model

**Acceptance criteria**
- Read original `LcfSaveData` framing from an explicitly supplied save directory and slot.
- Preserve chunk ID, length, offsets, payload bytes, and unknown-chunk count without executing save contents.
- Reject invalid slot paths, traversal, malformed/truncated framing, missing terminators, oversized files, and oversized chunks.
- Keep this reader read-only; original saves are never overwritten and no speculative Gold/party/inventory mapping is claimed.

**Validation evidence (2026-08-24)**
- Added `project/src/rm2k/parser/rm2k_lsd_save_codec.cs` and `project/tests/core/test_rm2k_lsd_save_model.cs`.
- Synthetic tests cover raw unknown-chunk preservation, BER framing, traversal/absolute-path rejection, size limits, malformed headers, and missing terminators.
- `dotnet build project/UniversalRPG.csproj --no-restore /p:RunAnalyzers=true /p:RunAnalyzersDuringBuild=true` — 0 warnings, 0 errors.
- `GODOT_BIN=E:/URPG/Godot_v4.7.2/Godot_v4.7.2-stable_mono_win64_console.exe ./scripts/validate.sh` — exit 0, `261/261` tests passed.
- Save mutation, UI integration, and field-level semantic mapping remain separate follow-up work; this card does not claim full native gameplay save restoration.

### K-023 — Verified RM2K/2003 command codes

**Acceptance criteria**
- Interpreter command constants match the verified liblcf numeric table (`lcf::rpg::Cmd`).
- Parameter layouts for implemented commands match EasyRPG Player semantics (ControlSwitches 10210 mode 0=ON/1=OFF/2=flip; ControlVars 10220 [target][op][operandType][value]; Teleport 10810 map/x/y; Wait 11410 tenths of a second).
- Commands are consumed from the typed `Rm2kMap.EventCommand` model (code/int parameters/text), matching the parser output.
- Unsupported or malformed payloads produce diagnostics and are skipped safely.
- Regression tests cover each implemented command family plus loop jump-back and break-jump-past behavior.

**Progress evidence (2026-08-23)**
- Verified code table extracted from liblcf `src/generated/lcf/rpg/eventcommand.h`; parameter semantics cross-checked against EasyRPG Player `game_interpreter.cpp` and `game_interpreter_map.cpp` (CommandControlSwitches, CommandControlVariables, CommandTeleport 10810, SetupWait).
- Rewrote `EventInterpreter` on the typed model: message continuation (20110), comment continuation (22410), tenths-based waits with frame clamp, switch flip mode, variable operand type (const/var), bounded loop stack with EndLoop jump-back and BreakLoop jump-past.
- Known limitations documented in code: ShowChoice/InputNumber remain skipped pending presentation/input slices; unsupported commands remain diagnostic-only.
- Current canonical validation: `All 279 tests passed`.

### K-013 — LMU events/pages

**Acceptance criteria**
- Decode event metadata (id, name, x, y) and page metadata (trigger, priority, frequency, list framing).
- Do not execute event commands while parsing.
- Bound page/command counts and payload sizes.
- Preserve raw/unknown commands for later interpreter work.
- Synthetic fixtures cover valid, empty, truncated and oversized payloads.

**Validation evidence (audited 2026-08-26)**
- `Rm2kParser.ParseMap` decodes bounded event IDs/names/coordinates, page metadata, page conditions, move-list presence, command-list presence, and data-only command vectors.
- `Rm2kEventCommandDecoder` enforces command, parameter, string, terminator, and trailing-byte bounds; it never executes commands.
- `TestEventInterpreter` and `test_rm2k_parser.cs` cover event/page selection, command-vector framing, malformed input, and condition decoding.
- Current canonical validation: `All 279 tests passed`.
- Remaining limit: complete RM2K field-semantic coverage for every event/page subfield is not claimed.

### K-014 — Preserve unknown LCF fields/chunks

**Acceptance criteria**
- Decode event/page structures as data only.
- Do not execute event commands while parsing.
- Bound page/command counts and payload sizes.
- Preserve raw/unknown commands for later interpreter work.

**Validation evidence (audited 2026-08-26)**
- `Rm2kParser` retains unknown top-level and per-entry fields as raw bounded dictionaries with IDs, payloads, offsets, and lengths.
- `Rm2kEventCommandDecoder` retains command data as typed data objects; unsupported command codes are diagnosed by the native interpreter rather than executed during parsing.
- `Rm2kEngineRuntime.Update()` is covered by a native autorun integration test that proves Clock → Scheduler → EventInterpreter execution; map initialization now creates a bounded `VirtualFramebuffer` through `Rm2kRendererAdapter`, and `Stop()` reset coverage includes clock/presentation/framebuffer cleanup.
- `Rm2kEventScheduler` caps imported map events at 1000 and emits a bounded diagnostic when additional events are skipped; this is regression-tested.
- Regression coverage exists in `test_rm2k_parser.cs`, `test_rm2k_lsd_save_model.cs`, `test_event_interpreter.cs`, and `TestPluginDetection.cs`.
- Current canonical validation: `All 279 tests passed`.

### K-071 — Controller/touch remapping layer

**Status (audited 2026-08-26) — DONE**
- `Rm2kInputMapper` maps keyboard, joypad buttons, and bounded touch zones to engine-neutral actions.
- `Main._UnhandledInput` consumes the mapper for movement, confirmation, choices, and numeric-input confirmation without executing imported scripts.
- Custom key bindings replace defaults for the selected action; released and unbound events are ignored.
- Regression coverage: `TestRm2kInputMapper` (`3/3`); current canonical validation: `All 280 tests passed`.

### K-072 — Reusable RM2K host lifecycle

**Status (2026-08-28) — DONE for bounded lifecycle slice**
- `EnginePluginHost` accepts `Stopped → Start` and disposes the previous stopped runtime exactly once before selecting and creating a fresh runtime.
- `Rm2kEngineRuntime.Stop()` remains a full cleanup boundary; the restart test verifies cleared map/framebuffer state and a fresh clock/scheduler.
- No stopped runtime is re-initialized. The second start follows the normal selection → creation → initialization → start path.
- Regression coverage: `Test_Rm2kRuntimeCanRestartAfterStopWithFreshRuntimeState` in `TestPluginDetection`.
- Fresh canonical validation: `All 280 tests passed`.

### K-073 — Synchronize runtime sprite descriptors after movement

**Status (2026-08-28) — DONE for bounded movement/render-state slice**
- `Main._UnhandledInput` routes RM2K movement through `Rm2kEngineRuntime.TryMove()` instead of mutating `Simulation` directly.
- Successful movement rebuilds bounded player/event sprite descriptors from the current map data; blocked or invalid movement leaves descriptors unchanged.
- Regression coverage: `Test_Rm2kRuntimeMovementSynchronizesPlayerSpriteDescriptor` in `TestPluginDetection`.
- Fresh canonical validation: `All 281 tests passed`.

### K-074 — Fail-closed pending transfer parameters

**Status (2026-08-28) — DONE for bounded transfer-request slice**
- `EventInterpreter` accepts pending transfer requests only for map IDs `1..GameSimulationState.MaxMapId` and nonnegative coordinates.
- Invalid transfer payloads produce one diagnostic and cannot overwrite an existing pending transfer.
- This remains a data-only `PendingTransfer` request; no target map is loaded or executed.
- Regression coverage: `Test_TeleportRejectsInvalidMapIdsWithoutOverwritingPendingState` in `TestEventInterpreter`.
- Fresh canonical validation: `All 282 tests passed`.

### K-075 — Transfer facing direction validation

**Status (2026-08-28) — DONE for bounded transfer-request slice**
- The optional RM2K3 transfer facing parameter is accepted only for directions `2/4/6/8`.
- Transfer validation is atomic: invalid facing values do not alter the facing direction or an existing pending transfer.
- Transfers remain data-only `PendingTransfer` requests; no target map is loaded or executed.
- Regression coverage: `Test_TeleportAppliesValidFacingAndRejectsInvalidFacingAtomically` in `TestEventInterpreter`.
- Fresh canonical validation: `All 283 tests passed`.

### K-076 — Clear confirmed choice presentation state

**Status (2026-08-28) — DONE for bounded choice lifecycle slice**
- A confirmed `ShowChoice` selection is logged and then clears `PresentationState.ActiveChoice` before the interpreter advances.
- The UI therefore cannot keep displaying or consuming a stale choice after confirmation.
- Regression coverage: `Test_ShowChoicePausesUntilSelection` in `TestEventInterpreter`.
- Fresh canonical validation: `All 283 tests passed`.

### K-077 — Preserve pending InputNumber state across variable conflicts

**Status (2026-08-29) — DONE for bounded input lifecycle slice**
- `EventInterpreter` pauses an `InputNumber` command when a different variable already owns the pending presentation input.
- The existing pending variable/value remain unchanged; no conflicting variable is created or mutated.
- This does not execute foreign scripts and does not broaden the bounded input model.
- Regression coverage: `Test_InputNumberDoesNotConsumePendingValueForDifferentVariable` in `TestEventInterpreter`.
- Fresh canonical validation: `All 284 tests passed`.

### K-078 — Implement bounded RM2K ChangeItems command

**Status (2026-08-29) — DONE for bounded inventory mutation slice**
- `EventInterpreter` handles verified command `10320` with five parameters: operation, item-ID mode/value, and amount operand mode/value.
- EasyRPG semantics are preserved: operation `0` adds and operation `1` subtracts; constant and variable item IDs/amounts are supported.
- Counts are clamped to `0..999999`; malformed parameters, invalid IDs/variables, negative amounts, unsupported operand types, and unsupported operations fail closed with bounded diagnostics.
- Regression coverage: `Test_ChangeItemsAddsConstantItemCount`, `Test_ChangeItemsSubtractsAndReadsVariableOperands`, and `Test_ChangeItemsClampsAndRejectsInvalidOperation` in `TestEventInterpreter`.
- Fresh canonical validation: `TestEventInterpreter 44/44`; `All 292 tests passed`; build and `scripts/validate.sh` passed.
- Remaining boundary: chipset passability remains blocked until a verified LMU/Chipset field mapping and distinguishing fixtures exist.

### K-079 — Implement bounded RM2K ChangePartyMembers command

**Status (2026-08-29) — DONE for bounded party mutation slice**
- `EventInterpreter` handles verified command `10330` with three parameters: operation, actor-ID mode, and actor-ID value.
- EasyRPG semantics are preserved: operation `0` adds and operation `1` removes; actor IDs may be constant or variable.
- Party size is bounded to `GameSimulationState.MaxPartyMembers` (`4`); duplicate additions, absent-actor removal, invalid IDs/variables, malformed parameters, and unsupported operations fail closed with diagnostics.
- Regression coverage: `Test_ChangePartyMembersAddsConstantActor`, `Test_ChangePartyMembersRemovesVariableActor`, and `Test_ChangePartyMembersRejectsDuplicateAndInvalidActor` in `TestEventInterpreter`.
- Fresh canonical validation: `TestEventInterpreter 47/47`; `All 296 tests passed`; build and `scripts/validate.sh` passed.
- RGSS/XP/VX/Ace and MV/MZ remain detection/metadata-only; no foreign Ruby or JavaScript is executed.
- Remaining boundary: chipset passability remains blocked until a verified LMU/Chipset field mapping and distinguishing fixtures exist.

## Agent maintenance rules

- Hermes may split a card when implementation reveals genuinely independent work, but must preserve traceability to the parent ID.
- New defects found during a card become `P0`/`P1` bug cards when they threaten correctness/security; otherwise add them to backlog.
- Do not create hundreds of speculative cards for distant phases. Expand the next 1–2 milestones in detail and keep later phases coarse.
- At the end of a work session update this board and `SESSION_STATE.md` with exactly what is next.
