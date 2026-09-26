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
| K-081 | 0 | DONE | Decode real LMU event pages: fix struct-array field collection and verify liblcf IDs | K-013 |
| K-082 | 0 | DONE | Align event-page trigger ids with liblcf and fail closed on undecodable pages | K-081 |
| K-083 | 0 | DONE | Correct ControlSwitches/ControlVariables parameter layout to the verified EasyRPG spec | K-081 |
| K-084 | 1 | DONE | Implement verified actor-stat, screen-effect, and event-control interpreter commands | K-023 |
| K-085 | 2 | DONE | Bring RPG Maker MV to data-directory and System.json metadata parity with MZ | K-017 |
| K-086 | 1 | DONE | Decode verified RM2K chipset passability arrays from the LDB chipset section | K-015 |
| K-087 | 2 | DONE | Add verified RM2K autotile animation ticking (counter values blocked: no verified data source) | K-015 |
| K-088 | 2 | DONE | Apply verified RM2K tile substitution tables (source: liblcf SaveMapInfo, not LMT) | K-086 |
| K-089 | 2 | DONE | Decode RM2K per-map terrain tags via verified `Game_Map::GetChipId` substitution | K-015 |
| K-091 | 2 | DONE | Apply verified `Game_Map::IsCounter` action-trigger propagation across up to 3 counter tiles | K-015 |
| K-092 | 2 | DONE | Drive movement and event triggers from player input in the RM2K runtime | K-015 |
| K-093 | 3 | DONE | Route the Godot host input through the verified turn order instead of ad-hoc triggers | K-092 |
| K-094 | 2 | READY | Verify and implement RM2K vehicle get on/off for the action-event order | K-092 |
| K-095 | 3 | DONE | Resolve verified chipset source rectangles for blocks C, E and F | K-087 |
| K-096 | 3 | DONE | Build the verified block D autotile quarter table and block geometry | K-095 |
| K-097 | 3 | DONE | Build the verified block A/B autotile composition from `BlockA_Subtiles_IDS` | K-096 |
| K-098 | 3 | DONE | Decode the indexed RM2K chipset bitmap and blit the resolved rectangles | K-097 |
| K-099 | 3 | READY | Compose a full map frame from chipset tiles, map layers and the z-order rule | K-098 |
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

### K-081 — Real LMU event-page decoding

**Status (2026-08-31) — DONE**

**Problem**
- `ParseStructArray` and `ReadStructFields` only materialized objects/fields when a `pCollectFields` flag was set. Nested `rpg::EventPage` arrays were read with that flag off, so `events[].pages` was always empty for real LMU files: the event interpreter, scheduler, and page-condition paths had never run against real data.
- `EventInterpreter.End` was `0`; liblcf `lcf::rpg::Cmd` defines `END = 10`.
- Page field ids carried unverified fallbacks (`0x09`/`0x08`/`0x06`, plus `0x0b` for the command list) that do not exist in liblcf.
- The nested `EventPageCondition` struct was read as if it were already field-decoded, which threw `KeyNotFoundException` and faulted RM2K runtime initialization.

**Fix**
- Struct arrays and struct fields are always materialized; the collection flag was removed.
- `EventInterpreter.End = 10` (verified liblcf).
- Page ids limited to the verified set: condition `0x02`, move_frequency `0x20`, trigger `0x21`, layer `0x22`, move_route `0x29`, `event_commands_size` `0x33`, `event_commands` `0x34`.
- Nested struct payloads are decoded through `ReadNestedStructFields` before dispatch.
- An undecodable command vector is contained per page (`command_error` + `event_commands_bytes`) instead of failing the whole map.

**Validation evidence (2026-08-31)**
- Real fixtures now decode event pages: RM2000 `Map0001.lmu` 22 pages, RM2003 `Map0001.lmu` 38 pages; RM2000 decodes every command vector without error.
- `event_commands_bytes` equals the declared `event_commands_size` on decoded pages.
- One RM2003 page contains a 5-byte BER value above 31 bits; the page is contained with a diagnostic instead of guessing the encoding.
- Regression coverage: `Test_RealMapEventPagesDecodeCommandCountsMatchingLiblcfSizes`, `Test_Rm2000RealMapPagesDecodeEveryCommandVector`, `Test_LiblcfEndCommandStopsInterpreterWithoutDiagnostic`.
- `dotnet build project/UniversalRPG.csproj --no-restore` — 0 warnings, 0 errors; headless runner `All 300 tests passed`, exit `0`.

### K-082 — Event-page trigger ids and undecodable page containment

**Status (2026-08-31) — DONE**

**Problem**
- `Rm2kEventTrigger` used invented values (`Autorun=0, Parallel=1, Action=2, Touch=3`). liblcf `lcf::rpg::EventPage::Trigger` defines `action=0, touched=1, collision=2, auto_start=3, parallel=4`, and EasyRPG Player compares those values directly against decoded page data. With the old enum no real autorun, parallel, or action page could ever match.
- A page whose command vector failed to decode was bridged into the runtime as an empty page and could start as if it were valid.

**Fix**
- `Rm2kEventTrigger` now mirrors liblcf: `Action=0, Touched=1, Collision=2, AutoStart=3, Parallel=4`.
- `Rm2kEngineRuntime` skips pages carrying a non-empty `command_error` and records a diagnostic instead of running them.

**Validation evidence (2026-08-31)**
- Regression coverage: `Test_TriggerValuesMatchVerifiedLiblcfEventPageTrigger`, `Test_EventPageSelectorIgnoresOtherTriggerKinds`, `Test_RealMapPageTriggersUseLiblcfEventPageTriggerValues`, `Test_Rm2kRuntimeExecutesRealFixtureActionPages`.
- `Test_Rm2kRuntimeExecutesRealFixtureActionPages` starts the RM2K runtime on the pinned fixture, triggers a real action page, advances 20 frames, and requires interpreter diagnostics — the first test that proves real fixture commands execute end to end.
- `dotnet build project/UniversalRPG.csproj --no-restore` — 0 warnings, 0 errors; headless runner `All 304 tests passed`, exit `0`.
- Cross-checked against EasyRPG Player `Game_Event::AreConditionsMet`: switch A and switch B both require ON, RM2000 uses `variable >= value` while RM2K3 uses the six compare operators, and timers compare with `secs > limit`. Existing page-condition code already matches, so no change was made.

### K-083 — ControlSwitches/ControlVariables parameter layout

**Status (2026-08-31) — DONE**

**Problem**
- Both commands read `parameters[0]` as the first id. EasyRPG stores the lvalue form in `parameters[0]` (`Game_Interpreter_Shared::TargetEvalMode`), with `parameters[1]` as the start id and `parameters[2]` as the range end. Real RM2K/2003 payloads therefore decoded as start id `0` and were always rejected with `invalid range 0-…`, so no real switch or variable command ever executed.
- Verified widths from `Game_Interpreter::ExecuteCommand`: `ControlSwitches` 4 parameters, `ControlVars` 7 parameters, `ChangeLevel` 6, `ConditionalBranch` 6.

**Fix**
- `ControlSwitches` reads `[targetMode, start, end, mode]`; `ControlVars` reads `[targetMode, start, end, operation, operandMode, operand, bitfield]`.
- `TargetEvalSingle` collapses the range end to the start id, matching `DecodeTargetEvaluationMode`.
- Patch-only target modes (`IndirectSingle`, `IndirectRange`, `Expression`) stay fail-closed with diagnostics.
- Added the verified `VarOperandVariableIndirect` mode (`v[v[x]]`).

**Validation evidence (2026-08-31)**
- Regression coverage: `Test_ControlSwitchesAndVarsUseVerifiedParameterLayout`, `Test_ControlVarsRangeTargetWritesEveryVariableInRange`, `Test_ControlVarsIndirectOperandReadsVariableOfVariable`, `Test_ControlSwitchesAndVarsRejectPatchOnlyTargetModes`, `Test_RealFixtureCommandsUseVerifiedParameterWidths`.
- `Test_RealFixtureCommandsUseVerifiedParameterWidths` asserts the pinned fixtures satisfy the verified minimum widths; `Test_Rm2kRuntimeExecutesRealFixtureActionPages` now also fails if a real control command is rejected as an invalid range or patch-only target mode.
- `dotnet build project/UniversalRPG.csproj --no-restore` — 0 warnings, 0 errors; headless runner `All 309 tests passed`, exit `0`.
- Still diagnostic-only by design: `ChangeLevel` (10420), `ChangeHeroName` (10610), screen effects (11040/11050/11070), `CallEvent` (12330), battle commands (1009), and Maniac codes present in the fixtures.

### K-084 — Verified actor-stat, screen-effect and event-control commands

**Status (2026-08-31) — DONE**

Implemented from the verified `lcf::rpg::Cmd` table and EasyRPG `ExecuteCommand` dispatch widths:

- `ChangeLevel` (10420) and `ChangeExp` (10410), 6 parameters `[actorMode, actorId, operation, operandMode, operand, showMessage]`, using `GetActors` modes (party / hero / variable-held hero) and `OperateValue` add/subtract. Levels clamp to `1..99`, exp to `0..999999`.
- `ChangeHeroName` (10610), 1 parameter; the command string is the new name, bounded to 64 characters.
- `EndEventProcessing` (12310) ends the current frame instead of the whole interpreter.
- `FlashScreen` (11040, 6 parameters), `ShakeScreen` (11050, 4 parameters) and `WeatherEffects` (11070, 2 parameters) drive new bounded screen-effect state on `PresentationState`; the runtime ticks effects with elapsed simulation frames, and the wait flag reuses the RM2K tenths-to-frames conversion. Weather strength clamps to 2 and unknown RM2K types fold to 0.
- `CallEvent` (12330, 3 parameters) pushes a bounded nested frame for map events through an injected resolver; nested `END` returns to the caller, recursion is capped at `MaxScriptRecursion`, and common-event targets stay diagnostic-only because the LDB common-event section is not decoded yet.
- `ChangeEventLocation` (10860, 4 parameters) and `EraseEvent` (12320) mutate event position and activity through scheduler-backed hooks, so `TriggerAt` observes the new position.
- `Rm2kMap.EventPage.Trigger` comment corrected to the liblcf enum.

**Validation evidence (2026-08-31)**
- New regression coverage in `TestEventInterpreter`: level/exp party-wide, clamping, variable operand, variable-held actor id, fail-closed modes, hero name, `EndEventProcessing`, flash/shake/weather bounds and waits, presentation-absent path, nested call with return, unsupported call targets, recursion bound, event location with variable coordinates, and erase-event activation.
- `dotnet build project/UniversalRPG.csproj --no-restore` — 0 warnings, 0 errors; headless runner `All 330 tests passed`, exit `0`.
- Still diagnostic-only by design: `ChangeBattleCommands` (1009), menu/Maniac codes (5001-5005, 11610), `MoveEvent` (11330, needs move routes), `ChangeMapTileset` (11710), and battle-dependent commands.

### K-085 — RPG Maker MV data-directory and metadata parity

**Status (2026-08-31) — DONE for the bounded data-only slice**

**Scope boundary**
- MV/MZ gameplay needs a JavaScript engine. That stays blocked: card K-090 is `BACKLOG` behind the RM2K playable milestone, and repository policy forbids executing imported JavaScript. This card is data-only.

**Problem**
- Only MZ had a bounded `data/` inventory; MV was limited to `gameTitle` from `System.json`.

**Fix**
- The inventory reader is now shared: `WebDataDirectoryResult` with `MzDataDirectoryResult` and `MvDataDirectoryResult` wrappers. Each wrapper requires its own runtime signature, so an MV snapshot is never read as MZ and the reverse is equally refused.
- `MvMetadataResult` now also reports `versionId`, `locale`, `currencyUnit`, `startMapId`, `startX`, `startY`, and bounded `partyMembers` (actor ids `1..50000`, capped at four). MV stores its version as `versionId` where MZ uses `systemVersion`; both keys are read from the top-level object only, so nested keys cannot shadow them.
- No JavaScript, HTML, or native file is executed or evaluated; only bounded JSON text is parsed.

**Validation evidence (2026-08-31)**
- New `TestMvDataDirectory` suite: inventory extraction, database section counts, missing files, malformed and non-array JSON, malformed optional sections with siblings kept, encrypted assets, verified System.json keys, and mutual signature refusal in both directions.
- `dotnet build project/UniversalRPG.csproj --no-restore` — 0 warnings, 0 errors; headless runner `All 340 tests passed`, exit `0`.
- RPG Maker AX was evaluated and deliberately left out: no verifiable file signature is documented publicly, and the repository forbids inventing format details. It stays unsupported rather than guessed.

### K-086 — RM2K chipset passability decoding

**Status (2026-09-26) — DONE: verified chipset passability drives real movement**

This card closed the blocker that was repeated in every slice note ("chipset passability remains fail-closed").

**Verified constants (EasyRPG Player `src/map_data.h` + `Game_Map` passability helpers)**
- Passability bits: `Down=0x01`, `Left=0x02`, `Right=0x04`, `Up=0x08`, `Above=0x10`, `Wall=0x20`, `Counter=0x40`.
- Tile blocks: `BLOCK_A=0` (stride 1000, index 0), `BLOCK_B=2000` (1000, 2), `BLOCK_C=3000` (50, 3), `BLOCK_D=4000` (50, 6), `BLOCK_E=5000` (1, 18), `BLOCK_F=10000` (1, 162); block ends 2000/3000/3150/4600/5144/10144; `NUM_LOWER_TILES=162`, `NUM_UPPER_TILES=144`.
- `GetPassableMask` maps a step to `Right`/`Left`/`Down`/`Up`.
- `IsPassableTile` decides from the upper layer first and only falls through to the lower layer when the upper entry carries `Above`; the lower lookup honours the `Wall` exception for autotiles 20-23, 33-37, 42, 43, 45, 46.

**Implemented**
- `project/src/rm2k/simulation/Rm2kChipset.cs` — verified `ChipIdToIndex`/`IndexToChipId`, `DirectionBit`, `IsPassableLowerTile`, `IsPassableTile`, `BuildDirectionMasks`. Unknown tile ids, missing tables, and mismatched layer lengths fail closed.
- `GameSimulationState` keeps `PassabilityMasks` as the authoritative per-tile direction mask, adds `IsPassableInDirection`, and keeps the old `IEnumerable<bool>` `ConfigureMap` contract by mapping passable to all four directions. `TryMove` now checks the direction bit.
- `Rm2kEngineRuntime` reads `passable_data_lower`/`passable_data_upper` from the LDB chipset section, verifies the 162/144 lengths, builds masks from the LMU `lower_layer`/`upper_layer`, and configures the simulation; the stale fail-closed diagnostics are gone.

**Validation evidence (2026-09-26)**
- `dotnet build project/UniversalRPG.csproj` — 0 errors; headless runner `All 350 tests passed`, exit `0`.
- `test_rm2k_chipset.cs` pins the verified bit values, block constants, chip-id round trips, direction mapping, upper-then-lower resolution, the wall autotole exception, and the fail-closed cases.
- `Test_RealFixtureChipsetProducesBothPassableAndBlockedTiles` drives real RM2000/RM2003 maps: each fixture yields walkable and impassable tiles, and a real step onto a walkable tile succeeds while a step into an impassable tile is refused.
- `TestPluginDetection` asserts the runtime decoded non-empty masks from the real fixture and no longer reports missing passability.


### K-087 — RM2K autotile animation and event counters

**Status (2026-09-26) — autotile animation DONE; counter values not implementable from verified data**

Follow-up to K-086. Same evidence discipline: nothing below was inferred from memory.

**Autotile animation (implemented)**
- Verified in EasyRPG Player `src/tilemap_layer.cpp` (Draw), `src/game_map.cpp` (SetChipset, GetAnimationType/Speed) and liblcf `src/generated/lcf/ldb/chunks.h` (`ChunkChipset`).
- liblcf field ids: `animation_type = 0x0B`, `animation_speed = 0x0C`; the project's scalar field contract matches upstream.
- `Game_Map::GetAnimationSpeed()` returns `animation_speed != 0 ? 12 : 24`, so `animation_speed` is only an animated/not flag, **not** a frame rate and **not** an on/off switch: even the zero default keeps AB autotiles cycling, just at half speed.
- AB autotiles (blocks A1/A2/B, `id < BLOCK_C`): `step = frames / speed`, then cyclic (`animation_type != 0`) `% 3`, reciprocating (`animation_type == 0`) `% 4` with `3 → 1`, i.e. 0,1,2,1.
- Block C: `step = (frames / 6) % 4` on a fixed cycle that ignores both chipset animation settings.
- Blocks D, E and F never animate.
- `frames` is the RPG_RT frame counter (`Game_System::GetFrameCounter`), which the simulation already ticks as `FrameCount`.
- Implemented as `Rm2kChipset.AnimationSpeed/ReciprocatingStep/CyclicStep/CBlockStep/ChipAnimationStep`, exposed through `GameSimulationState.ChipsetAnimationType`, `ChipsetAnimationSpeed` and `GetChipAnimationStep`.
- The runtime now selects the chipset entry by the LMU `chipset_id` instead of assuming the first chipset, which is what the Player does (`SetChipset(map->chipset_id)`). The parser stores the passability tables on the matching typed chipset entry and keeps the section-level keys for the first entry so the existing contract still holds.

**Event counters (deliberately not implemented)**
- `Game_Map::IsCounter` is verified: the upper layer must hold `>= BLOCK_F`, the id runs through the `upper_tiles` substitution table, and the entry's `Counter` bit (`0x40`) marks it. The Player uses it only to look for an action trigger across at most 3 counter tiles in a row.
- The counter *value* mechanism (plates and steps that close again) is **not** implementable: liblcf `master` has no per-map counter/chip-data array on `lcf::rpg::Map` or `lcf::rpg::MapInfo`, so there is no verified data source to decode. Implementing it would mean inventing a format, which is exactly what K-086 forbids.
- The substitution tables (`map_info.lower_tiles`/`upper_tiles`, identity via `std::iota` in `Game_Map::Setup`) come from `lcf::rpg::MapInfo`. The current resolution treats them as identity, which matches the verified default, and the tables themselves remain a separate card.

**Validation evidence (2026-09-26)**
- `dotnet build project/UniversalRPG.csproj` — 0 errors; headless runner `All 356 tests passed`, exit `0`.
- `test_rm2k_chipset.cs` pins the speed mapping, the reciprocating 0,1,2,1 cycle, the cyclic three-frame cycle, the block C fixed cycle, the per-block dispatch, the D-F static blocks, and the frame-counter-driven step through `GameSimulationState`.
- `Test_RealFixtureChipsetProducesBothPassableAndBlockedTiles` proves both fixtures resolve their `chipset_id` to a real chipset entry with the expected animation defaults.
- `TestPluginDetection` asserts the runtime carries chipset animation values and starts on autotile frame zero.

**Lesson recorded**
- Passability and animation data belong to a single chipset entry. Reading the first entry "because it is the map's chipset" was an unverified assumption; the LMU `chipset_id` is the verified selector.


### K-088 — RM2K tile substitution tables

**Status (2026-09-26) — DONE: verified substitution applied; the tables themselves come from save files**

**Card correction**
The card originally said "LMT map-info tile substitution tables". That was wrong. liblcf `lcf::rpg::MapInfo` has no substitution fields and liblcf `ChunkMapInfo` (LMT) has no `lower_tiles`/`upper_tiles` field ids. The tables live in `lcf::rpg::SaveMapInfo` (`lower_tiles`, `upper_tiles`, 144 entries each, identity by default), so they are save-file data, not map-tree data.

**Verified resolution order (EasyRPG Player `src/game_map.cpp`)**
- `Setup` fills both tables with `std::iota` (identity), which matches the liblcf `SaveMapInfo` default, so identity is the correct behaviour for a freshly loaded map.
- Upper layer, `IsPassableTile` and `IsCounter`: `tile_id = upper_layer[i] - BLOCK_F` and then `tile_id = map_info.upper_tiles[tile_id]`, so the substitution happens **after** reducing the raw id and **before** the flag lookup.
- Lower block E, `IsPassableLowerTile`: `tile_id = tile_raw_id - BLOCK_E; tile_id = map_info.lower_tiles[tile_id] + BLOCK_E_INDEX`. Only block E is substituted; blocks A/B/C/D are used as-is.
- `GetChipId` (terrain lookup) converts the raw id to a chip index first and only then remaps indices in `[BLOCK_E_INDEX, NUM_LOWER_TILES)`.

**Implemented**
- New `Rm2kTileSubstitution` with the verified identity default, `SubstituteLower`, `SubstituteUpper` and `ResolveChipIndex` (the `GetChipId` order). Tables whose length or entries do not fit the 144-entry range fall back to identity instead of clamping, and requests outside the range return -1 so they fail closed.
- `Rm2kChipset.IsPassableLowerTile`, `IsPassableTile` and `BuildDirectionMasks` take an optional `Rm2kTileSubstitution`; the existing overloads keep identity behaviour, so the runtime is unchanged until save data provides a table.

**Not implemented, on purpose**
- Reading the tables out of a save file. That belongs with the open save-game work (K-050 family: "semantic field mapping, save mutation"), not with the chipset parser.

**Validation evidence (2026-09-26)**
- `dotnet build project/UniversalRPG.csproj` — 0 errors; headless runner `All 360 tests passed`, exit `0`.
- `test_rm2k_chipset.cs` pins the identity default, that substitution changes passability lookups, that only the verified ranges are remapped, that malformed tables fall back to identity, that out-of-range requests fail closed, and the `GetChipId` index-first order.

### K-089 — RM2K per-map terrain tags

**Status (2026-09-26) — DONE: terrain table decoded and resolved per map tile**

**Verified (EasyRPG Player `src/game_map.cpp` `GetTerrainTag` / `GetChipId`, liblcf `ChunkChipset`)**
- `terrain_data = 0x03`, an array of 162 **shorts** (324 bytes), `int16_t` in `rpg::Chipset`, defaulting to all ones.
- RPG_RT omits an all-ones table, and the Player returns terrain 1 when the table is empty, so an absent table is normal data and not a decode failure.
- The **lower** layer alone decides the terrain; the upper layer is never consulted.
- Resolution order: raw id -> `ChipIdToIndex` -> substitution for indices in `[BLOCK_E_INDEX, NUM_LOWER_TILES)` -> `terrain_data[chip_index]`.
- Out-of-bounds coordinates use chip index 0, i.e. the terrain of the first lower tile; on looping maps the coordinate wraps first.

**Implemented**
- Parser decodes `terrain_data` (0x03) with a bounded 162 x 2 byte length check, per chipset entry plus the section-level key for the first entry, and reports an unexpected length as `terrain_data_unverified_length` with its offset.
- `Rm2kTileSubstitution.GetTerrainTag` implements the verified lookup, falling back to `Rm2kChipset.DefaultTerrainTag` (1) when the table is absent or does not cover the chip index, instead of reading out of bounds the way the Player's `assert` allows.
- `GameSimulationState.TerrainData`, `LowerLayer`, `TileSubstitution` and `GetTerrainTagAt` expose it to the runtime and to event conditions.
- `Rm2kEngineRuntime` reads the terrain table of the chipset the map actually uses.

**Validation evidence (2026-09-26)**
- `dotnet build project/UniversalRPG.csproj` — 0 errors; headless runner `All 363 tests passed`, exit `0`.
- `test_rm2k_chipset.cs` pins the chip-index mapping, the substitution effect on terrain, the absent and short table fallbacks, and the out-of-bounds behaviour through `GetTerrainTagAt`.
- `Test_RealFixtureChipsetProducesBothPassableAndBlockedTiles` verifies the real RM2000 chipset table has 162 entries with valid tag ids and that every lower tile of the map resolves a tag.
- `TestPluginDetection` asserts the real runtime map resolves a valid terrain tag, including out of bounds.

### K-091 — Counter tile action-trigger propagation

**Status (2026-09-26) — DONE: verified propagation over at most three counter tiles**

**Verified (EasyRPG Player `src/game_player.cpp`, `src/game_map.cpp`, liblcf)**
- `Game_Map::IsCounter`: the upper layer must hold a tile `>= BLOCK_F`, the id runs through `upper_tiles`, and the resolved entry must carry `Passable::Counter` (`0x40`).
- `Game_Map::XwithDirection` / `YwithDirection`: the tile in front, with the looping map wrap applied.
- `Game_Player::CheckEventTriggerThere` (action): check the tile in front; then while no action event was found and at most three times, if the current tile is a counter tile, step one tile further in the facing direction and check again. RPG_RT allows a maximum of three counter tiles, so four in a row stop the search.
- Layer rules differ by position and are easy to get backwards: events **in front** of the player must have `Layers_same` (`1`), events **on the player's own tile** must **not** have it.
- The walking case evaluates only `Trigger_touched` and `Trigger_collision` on the tile in front and does **not** walk counter tiles.
- liblcf `LMU_Reader::ChunkEventPage`: `trigger = 0x21`, `layer = 0x22`; `rpg::EventPage::Layers` is `below = 0`, `same = 1`, `above = 2`.

**Defect found and fixed**
- The LMU field `0x22` was decoded and stored under the name `priority`. liblcf has no `priority` field: `0x22` is `layer`. The name was wrong and the value was unusable, so the layer rules could not be implemented. It is now `layer`, carried into `Rm2kMap.EventPage.Layer`.

**Implemented**
- `Rm2kChipset.IsCounterTile` (upper id, substitution, counter flag, fail closed).
- `GameSimulationState.UpperLayer`, `UpperPassability`, `IsCounterAt`, `FrontTile` and the looping `Wrap` helper.
- `Rm2kEventScheduler.TriggerActionFacing`, `TriggerActionHere` and `TriggerTouchOrCollisionFacing` implement the three verified cases, with `Rm2kTriggerLayerRule` for the explicit same/not-same decision and `MaxCounterTiles = 3`.

**Validation evidence (2026-09-26)**
- `dotnet build project/UniversalRPG.csproj` — 0 errors; headless runner `All 369 tests passed`, exit `0`.
- `test_event_interpreter.cs` pins the reachable event behind a three tile chain, the stop behind a four tile chain, the layer rules for front versus own tile, and that touch/collision do not walk counter tiles.
- `test_rm2k_chipset.cs` pins `IsCounterTile` including the substitution and the fail-closed cases, and `FrontTile` including the map wrap.
- `Test_RealFixtureChipsetProducesBothPassableAndBlockedTiles` verifies both real fixtures expose a valid `layer` and `trigger` on every event page.

### K-092 — Player input drives movement and triggers

**Status (2026-09-26) — DONE: verified turn order applied to real input; host wiring still missing**

**Card correction**
The card said a successful step triggers touched/collision "on the tile in front". That is wrong. In the Player, `Game_Player::UpdateNextMovementAction` calls `CheckEventTriggerThere` (tile in front, layer same) only when the step was **blocked**, while `Game_Player::UpdateMovement` calls `CheckEventTriggerHere` (own tile, layer **not** same) when the player comes to a stop after a **successful** step. The layer rules are therefore opposite in the two cases.

**Verified ordering**
- `Game_Player::UpdateNextMovementAction`: `Move(move_dir)`, and if the player is still stopping, evaluate touched/collision on the tile in front.
- If stopping and the decision key is pressed, the vehicle toggle runs first and the action event check only runs when no vehicle was toggled.
- `Game_Player::CheckActionEvent`: touched/collision in front, then action on the own tile, then action in front continuing over at most three counter tiles; the result is the union.
- A running event page blocks movement (`Game_Map::IsRunning`).
- `Game_Map::XwithDirection`/`YwithDirection` wrap on looping maps.

**Implemented**
- `Rm2kEventScheduler.TriggerTouchOrCollisionHere` and the complete `CheckActionEvent`, complementing the K-091 entry points.
- New `Rm2kPlayerTurn`, a Godot-free class that applies one resolved input action in the verified order: refuse while paused, in a menu, or while an event page runs; a direction attempts `TryMove` and then picks the `Here` or `There` trigger path; `Confirm` runs `CheckActionEvent`.
- `Rm2kEngineRuntime.SubmitInput(Rm2kInputAction)` exposes the turn to the host and refuses input unless the runtime is running.

**Deliberate simplification**
- Vehicles and the airship are not implemented, so the vehicle toggle in front of `CheckActionEvent` cannot change anything and the action check always runs. This is recorded rather than faked.

**Still missing**
- Nothing feeds `SubmitInput` yet: `Rm2kInputMapper` is still unreferenced by the host scene, so the game cannot receive real input. That is the next card.

**Validation evidence (2026-09-26)**
- `dotnet build project/UniversalRPG.csproj` — 0 errors; headless runner `All 375 tests passed`, exit `0`.
- `test_event_interpreter.cs` pins the successful-step `Here` path, the blocked-step `There` path, the confirm path, the empty confirm, the pause and running-event guards, and that `None`/`Menu`/`Cancel` are not map steps.
- `TestPluginDetection` feeds `MoveRight` and `Confirm` into the real runtime map and asserts the position contract.

### K-093 — Godot host input routes through the verified turn order

**Status (2026-09-26) — DONE: host now uses the verified turn order**

**Card correction**
The card claimed "`Rm2kInputMapper` is unreferenced and nothing forwards input". That was wrong. `Main.cs` already constructs the mapper, configures the touch viewport in `_Ready`, and handles `_UnhandledInput` with the verified key edge rules (pressed, not echo). The real defect was narrower and worse: the host **had** an input path, but it bypassed everything K-091 and K-092 verified.

**What the host did before**
- `Confirm` computed a facing target with its own `GetFacingTarget` helper, which has no looping map wrap, then called `EventScheduler.TriggerAt(x, y, Action)`: no layer rule, no touched/collision in front, no counter tile walk.
- A direction called `Rm2kEngineRuntime.TryMove` and then `TriggerAt(mapX, mapY, Touched)` on success only: no layer rule, and the blocked-step in-front path did not exist at all.
- So the host was reachable but wrong in exactly the ways the verified Player logic is not.

**Fixed**
- The map input branch now calls `Rm2kEngineRuntime.SubmitInput(action)`, so the host inherits the verified `Here` versus `There` choice, the layer rules, the counter tile walk, the pause and running-event guards, and the map wrap in `FrontTile`.
- `GetFacingTarget` is deleted; the unwrapped direction helper no longer exists anywhere.
- Input is marked handled when the runtime consumed it, and also when a map input was consumed without moving, such as a blocked step, so it cannot fall through to the UI. `None`, `Menu` and `Cancel` stay unhandled as before.
- The message, choice and numeric-input priority order in `_UnhandledInput` is unchanged; that is the `Game_Message::IsMessageActive` gate and must stay ahead of map input.
- Removed the now unused `UniversalRPG.Rm2k.Simulation` import.

**Not covered by tests**
- The host wiring itself is a Node override and cannot be exercised headlessly without the scene. The runtime side is regression tested in `TestPluginDetection`; the `Main.cs` branch was verified by reading the resulting code path, not by an automated test.

### K-094 — Vehicles for the action-event order

**Status (2026-09-26) — READY: verification first**

**Scope**
- `Game_Player::CheckActionEvent` is only reached when `GetOnOffVehicle()` returns false, so the vehicle toggle can suppress the action event on boat, ship and airship tiles.
- `Rm2kPlayerTurn` documents that no vehicle can toggle anything today, so the action check always runs. Implementing boat/ship/airship would need the verified LMU/LDB vehicle data and the verified boarding rules.

**Unblock condition**
- Read `Game_Player::GetOnOffVehicle`, `GetOffVehicle` and `GetOnVehicle` plus `Game_Vehicle` for the exact conditions, and verify the vehicle sprites and LMU fields in liblcf, before writing any vehicle code. Do not approximate with the `Boat`/`Ship`/`Airship` terrain booleans already present in the terrain data.

### K-095 — Chipset source rectangles for blocks C, E and F

**Status (2026-09-26) — DONE: verified chipset rectangles resolved, no pixels yet**

**Why this slice**
`VirtualFramebuffer` deliberately stores tile ids only, so nothing is drawn yet. The verified `Rm2kChipset` work from K-086 to K-089 produced the tile-id resolution the renderer needs. This slice resolves a tile id to the chipset rectangle it is blitted from, which is the last step before real blitting, and it is fully verifiable without any graphics dependency.

**Verified (EasyRPG Player `src/tilemap_layer.cpp`, `Draw`)**
- Block C is blitted straight from the chipset: `col = 3 + (id - BLOCK_C) / 50`, `row = 4 + animation_step_c`. `BLOCK_C_TILES` is 3, so block C occupies columns 3 to 5 and rows 4 to 7.
- Block E applies the substitution table first (`id = substitutions[tile.ID - BLOCK_E]`), then `col = 12 + id % 6, row = id / 6` for `id < 96` and `col = 18 + (id - 96) % 6, row = (id - 96) / 6` afterwards.
- Block F applies the substitution table first (`id = substitutions[tile.ID - BLOCK_F]`), then `col = 18 + id % 6, row = 8 + id / 6` for `id < 48` and `col = 24 + (id - 48) % 6, row = (id - 48) / 6` afterwards.
- Blocks A, B and D are **not** blitted from the chipset: they come from the generated caches `autotiles_ab_screen` and `autotiles_d_screen`, so this slice refuses them instead of guessing.
- The formulas require at least 30 columns and 16 rows of 16 pixel tiles, which follows from the largest computed column (24 + 5) and row ((143 - 48) / 6).

**Range detail worth keeping**
The Player guards block C with `id >= BLOCK_C && id < BLOCK_D`, not with the end of block C, so ids between 3150 and 3999 still resolve. Its passability lookup uses the same range. `Rm2kChipsetSource` keeps that on purpose so the renderer and the simulation always resolve a tile id identically; it is documented so it is not "fixed" later.

**Implemented**
- New `Rm2kChipsetSource.TryResolve` with `ChipsetRect`, plus an identity-substitution overload and `Columns`/`Rows` bounds. Unknown ids, the autotile cache blocks and unresolvable substitutions return false so callers fail closed.

**Not implemented**
- No bitmap decoding and no blitting. The pinned fixtures contain no `Chipset.png`, so there is nothing real to decode yet.

**Validation evidence (2026-09-26)**
- `dotnet build project/UniversalRPG.csproj` — 0 errors; headless runner `All 381 tests passed`, exit `0`.
- `test_rm2k_chipset_source.cs` pins the three formulas including the `< BLOCK_D` range detail, the substitution effect on blocks E and F, the block C cycle over several chipset settings, the fail-closed set, and that every resolved rectangle stays inside the 30 by 16 chipset grid.

### K-096 — Block D autotile quarters

**Status (2026-09-26) — DONE: block D resolves to four verified chipset quarters**

**Verified (EasyRPG Player `src/tilemap_layer.cpp`)**
- `BlockA_Subtiles_IDS[47][2][2]` (int8, `-1` means the B block supplies the quarter) and `BlockD_Subtiles_IDS[50][2][2][2]` (uint8) are static tables in the Player source, ordered top-left, top-right, bottom-left, bottom-right.
- `GenerateAutotileD`: `block = (ID - 4000) / 50`, `variant = ID - 4000 - block * 50`, refusing `block >= 12 || variant >= 50 || block < 0 || variant < 0`. Block origin is `(block % 2) * 3, 8 + (block / 2) * 4` for `block < 4` and `6 + (block % 2) * 3, ((block - 4) / 2) * 4` afterwards. Each quarter is the block origin plus its table offset.
- The Player composes autotiles from four 16x16 quarters, so a tile id resolves to four chipset rectangles, not one.

**Transcription discipline**
- Both tables were extracted mechanically from the Player source with a script instead of being typed by hand: 188 values for block A and 400 for block D, with the count, value range and first/last rows checked against the source before any C# was written.
- The same script generated the block D anchor expectations in the test, so the test cannot drift from the table it verifies.
- Tables are stored flat: four values per block A variant, eight per block D variant.

**Implemented**
- `Rm2kAutotileQuarters.TryResolveBlockD` returns the four `ChipsetRect` quarters for a block D tile id and refuses out-of-range ids.
- `Rm2kAutotileQuarters.TryGetBlockAQuarters` exposes the block A variant table so the block A/B composition can use it and so the transcription can be regression tested.

**Not implemented**
- The block A/B composition itself (the quarter selection combines the A and B bit patterns with the animation step) and any bitmap decoding or blitting.
- Blocks A, B and D still do not resolve through `Rm2kChipsetSource`; only the block D quarters are available, and the composition is what turns them into a drawable tile.

**Validation evidence (2026-09-26)**
- `dotnet build project/UniversalRPG.csproj` — 0 errors; headless runner `All 387 tests passed`, exit `0`.
- `test_rm2k_autotile_quarters.cs` pins ten block D anchor rows against the Player table, the block origin for all twelve blocks, all 600 block D ids resolving with every quarter inside the chipset, the range refusals, and the block A table anchors and value range.
- The first run caught a real defect: the block D variant offset used `variant * 4` while a variant spans eight values, so every variant after the first read the wrong row.

### K-097 — Block A/B autotile composition

**Status (2026-09-26) — DONE: all lower layer blocks resolve to verified quarters**

**Defect found in K-096 while reading the source for this card**
`GenerateAutotiles` packs the quarter pairs into a hash with the last quarter on top and unpacks `x` first, so the **second** value of a pair is the chipset column and the **first** value is the row. K-096 had assumed the opposite. The block D rectangle code and its anchor expectations were corrected. The K-096 test had not caught this because it verified the table, not the axis order, so the axis is now documented in the code and pinned by the A/B column range tests.

**Verified (EasyRPG Player `src/tilemap_layer.cpp`)**
- `GenerateAutotileAB`: `block = ID / 1000`, `b_subtile = (ID - block * 1000) / 50`, `a_subtile = ID - block * 1000 - b_subtile * 50`, refusing `b_subtile >= TILE_SIZE` and `a_subtile >= 47`. `#define TILE_SIZE 16` is in `src/options.h`, so the B pattern is a four bit value.
- Three passes in this order: quarters the A table leaves to the B block with `t = (b_subtile >> (j * 2 + i)) & 1` and `t ^= 3` for block 2; quarters the A table supplies with the row `animID + (block == 1 ? 3 : 0)`; and the A/B combination pass, which runs last and therefore wins.
- The Player packs the quarters into a hash and de-duplicates them; that only affects the layout of the generated cache, not the quarter values, so it is not reproduced.
- `t ^= 3` swaps the two bits of the value, so a cleared bit 0 becomes 3 and a set bit 0 becomes 2. All four B variants, chipset columns 4 to 7, are reachable, and no more.

**Implemented**
- `Rm2kAutotileQuarters.TryResolveBlockAB` reproduces the three passes and returns the four quarters, refusing out-of-range blocks, B subtiles, A variants and animation steps.
- With K-095 and K-096, every lower layer block now resolves: A, B and D through the autotile tables, C, E and F straight from the chipset.

**Validation evidence (2026-09-26)**
- `dotnet build project/UniversalRPG.csproj` — 0 errors; headless runner `All 394 tests passed`, exit `0`.
- `test_rm2k_autotile_quarters.cs` pins the B bit pattern per quarter, the animation step as the row, the block 2 flip in both directions, the A table supplying a quarter with the column range split, the block 1 row shift, the combination pass overriding the A table, the reachable B column set, and the range refusals.
- The test also asserts that A quarters stay in columns 0 to 3 and B quarters in columns 4 to 7, which would fail if the pair axes were transposed again.

### K-098 — Chipset bitmap decoding and blitting

**Status (2026-09-26) — DONE: the real pinned chipset decodes and blits**

**Blocker resolved by research, not by invention**
The card said this was blocked on a real `Chipset.png`. The pinned fixtures had none, but the fixtures come from the public `EasyRPG/TestGame` repository, which ships the chipset images. The right chipset was determined, not guessed: the map's `chipset_id` is `1` and that LDB entry's `chipset_name` is `World`, so `TestGame-2000/ChipSet/World.png` from the **same pinned commit** is the real chipset for the pinned LDB.

The fixture README previously stated that no image is imported. That was true while the project only parsed LCF data; it is now updated with the reason, the pinned source URL and the SHA-256, and the image is a passive, never executed asset.

**Verified (EasyRPG Player)**
- `src/cache.cpp`, the `Material::Chipset` spec: directory `ChipSet`, loaded with `transparent` true, and 480 by 256 pixels.
- `src/image_png.cpp`, `ReadPalettedData`: for a paletted PNG every colour is opaque except **palette index 0**, which becomes alpha 0.
- The real fixture is an 8 bit paletted, non interlaced PNG of exactly 480 by 256 pixels, which independently confirms the `30 * 16` tile grid derived from the chipset formulas in K-095.

**Implemented**
- `Rm2kChipsetBitmap.TryParse`/`TryLoad`: bounded paletted PNG decoding, keeping the **palette index** rather than only the converted colour so the transparency rule survives. It refuses a wrong signature, a non 8 bit depth, a non paletted colour type, interlacing, oversized dimensions, a missing or oversized palette, missing image data and unknown scanline filters instead of reinterpreting them.
- `TryBlitTile` and `TryBlitRectangle` implement the verified transparency rule: index 0 is left untouched so a background shows through, every other index is painted opaque.
- `Rm2kPixelBuffer`, a Godot free RGBA buffer, so the blit stays deterministic and testable like the rest of the rendering code.

**Validation evidence (2026-09-26)**
- `dotnet build project/UniversalRPG.csproj` — 0 errors; headless runner `All 400 tests passed`, exit `0`.
- `test_rm2k_chipset_bitmap.cs` decodes the real fixture, checks its size against the derived tile grid, verifies that index 0 is present and that every used index is covered by the palette, checks that a blitted pixel is opaque exactly when its index is not 0, refuses rectangles outside the image, and pins the malformed input cases.
- The strongest check: every chipset rectangle that K-095 through K-097 can produce for the real chipset, over 1000 of them, is blittable inside the real 480 by 256 image.

### K-099 — Compose a full map frame

**Status (2026-09-26) — READY: verification first**

**Scope**
- Turn a parsed map into a pixel frame: for each tile, resolve the lower layer and upper layer chip ids, blit the autotile quarters or the direct chipset rectangle, and apply the z-order rule so a wall tile is drawn above the character and an "above" upper tile forms the top sublayer.

**Unblock condition**
- The z-order rule is already verified in `CreateTileCacheAt`: for an upper tile the sublayer depends on the `Above` flag after the substitution, and for a lower tile on the `Wall` or `Above` flag of the resolved chip index, using the same chip index ranges as the passability lookup. Re-read it, then read how the two layers and the three sublayers are drawn, before implementing. Do not invent a draw order.

## Agent maintenance rules
- Do not create hundreds of speculative cards for distant phases. Expand the next 1–2 milestones in detail and keep later phases coarse.
- At the end of a work session update this board and `SESSION_STATE.md` with exactly what is next.
