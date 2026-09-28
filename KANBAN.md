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
| K-094 | 2 | VERIFY | Verify and implement RM2K vehicle get on/off for the action-event order | K-092 |
| K-095 | 3 | DONE | Resolve verified chipset source rectangles for blocks C, E and F | K-087 |
| K-096 | 3 | DONE | Build the verified block D autotile quarter table and block geometry | K-095 |
| K-097 | 3 | DONE | Build the verified block A/B autotile composition from `BlockA_Subtiles_IDS` | K-096 |
| K-098 | 3 | DONE | Decode the indexed RM2K chipset bitmap and blit the resolved rectangles | K-097 |
| K-099 | 3 | DONE | Compose a full map frame from chipset tiles, map layers and the z-order rule | K-098 |
| K-100 | 3 | DONE | Render the real map in the runtime and show chipset pixels in the host preview | K-099 |
| K-101 | 3 | DONE | Decode the RM2K charset geometry and draw character frames | K-100 |
| K-102 | 3 | DONE | Decode event sprite fields and place characters per draw stage | K-101 |
| K-103 | 2 | DONE | Resolve the hero charset and draw the hero and events in the runtime frame | K-102 |
| K-104 | 2 | DONE | Re-render the frame when the player moves | K-103 |
| K-105 | 2 | DONE | Camera viewport instead of a full-map frame | K-104 |
| K-106 | 2 | DONE | Block E passability offset and the two-sided movement check | K-105 |
| K-107 | 2 | DONE | Walk animation, the per frame step budget and the hero sprite wiring | K-106 |
| K-108 | 3 | DONE | WOLF binary .mps reader, built from the verified format | K-094 |
| K-109 | 3 | DONE | WOLF event command list, decoded from the verified signature table | K-108 |
| K-110 | 3 | VERIFY | WOLF transfer, move route, database and common event binary formats | K-109 |
| K-111 | 2 | DONE | RM2K move route, so events walk at the verified per frame rate | K-107 |
| K-094 | 2 | VERIFY | Verify and implement RM2K vehicle get on/off for the action-event order | K-092 |
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

**Status (2026-09-26) — DONE: the real pinned map renders from the real pinned chipset**

**Verified (EasyRPG Player)**
- `CreateTileCacheAt` assigns each tile a sublayer. An upper layer tile goes into the above sublayer when its substituted entry carries `Above`; a lower layer tile goes into the above sublayer when its resolved chip index carries `Wall` or `Above`. The chip index ranges are the same as the passability lookup: block E through the lower substitution table plus `BLOCK_E_INDEX`, block D and block C by their stride, everything else the block number.
- The two sublayers are two drawables: `lower_layer(this, Priority_TilesetBelow + TileBelow + layer)` and `upper_layer(this, Priority_TilesetAbove + TileAbove + layer)`, with `TileBelow = 0`, `TileAbove = 100`, `Priority_TilesetBelow = 20`, `Priority_TilesetAbove = 50` and `Priority_Player = 40`. Drawables are sorted ascending, so the effective order is lower layer, then the hero, then upper layer. That is why a wall tile covers the hero.
- Without passability data the Player keeps the default `TileBelow`, which is the fail-closed case.

**Defect fixed**
`Rm2kChipsetSource.TryResolve` returned false for block E and F when no substitution was supplied, even though `Game_Map::Setup` fills both tables with `std::iota`. An absent table is the identity, so those lookups now fall back to it instead of making every block E and F tile unresolvable. The caller no longer has to build a substitution just to get the default.

**Implemented**
- `Rm2kTileZOrder` with the verified `ResolveChipIndex`, `LowerLayerSubLayer` and `UpperLayerSubLayer`.
- `Rm2kMapFrameRenderer` with `RenderLower` and `RenderUpper`, drawing each layer's below sublayer before its above sublayer and blitting every resolved chipset rectangle. The hero is deliberately not drawn: it belongs between the two calls, which is what exposes a wall tile.
- `Rm2kMapLayers` and `Rm2kChipsetTables` as the input, both Godot free.

**What the pinned fixture actually contains, now measured rather than assumed**
The real RM2000 testgame map is 20 by 15 tiles, its lower layer uses only block D and E, and its upper layer only block F. Its upper tiles are fully transparent in the real chipset, so drawing them changes nothing, and because it contains no A, B or C tile it has no animated autotile. Three of my initial expectations were wrong for that reason and were replaced by measurements of the fixture. The upper layer draw path and the animation are verified with synthetic maps instead, and the real map test now documents its own shape so those facts cannot silently change.

**Validation evidence (2026-09-26)**
- `dotnet build project/UniversalRPG.csproj` — 0 errors; headless runner `All 408 tests passed`, exit `0`.
- `test_rm2k_map_frame.cs` pins the sublayer rules for `Wall`, `Above` and both, the fail-closed case without passability, the chip index resolution including the block E substitution, the real map rendering with its measured shape, a visible upper tile changing the tile area, a fully transparent upper tile painting nothing, animation across frame 0 and 24 for the blocks that paint, and block D not animating.

### K-100 — Runtime renders the map and the host shows it

**Status (2026-09-26) — DONE: a real RM2K game renders pixels, with a golden image baseline**

**What this delivers**
A real RM2K game directory now produces a real map image. The chain is end to end verified: the LDB chipset tables, the LMU layers, the chip id resolution, the autotile quarter tables, the real chipset PNG and the verified draw order, all against the pinned fixtures.

**Verified (EasyRPG Player)**
- `src/cache.cpp`: the chipset is read from the `ChipSet` directory, and `Cache::Chipset` goes through the standard `LoadBitmap` path, so the image name is `<chipset_name>.png` inside `ChipSet`.
- `Game_Map::GetChipsetName` supplies the name from the database, and the map selects the chipset by its own `chipset_id`, which is already implemented in K-087.

**Implemented**
- `Rm2kEngineRuntime` reads `chipset_name`, resolves `<root>/ChipSet/<name>.png`, decodes it, checks the 480 by 256 size and renders the map into `RenderedMap` with `ChipsetImage` and `RenderDiagnostic` exposed. A missing, malformed or wrongly sized image is reported and leaves the runtime **running**, because the Player treats the chipset as an asset and the simulation does not depend on it. The tile id framebuffer keeps working next to the pixels.
- `Rm2kMapPreview` uploads the pixels once per change and draws them scaled with the nearest neighbour filter, with the player marker on top and the render diagnostic when there is no image. It falls back to the tile id view when no image exists.
- `Main.cs` forwards `RenderedMap` and `RenderDiagnostic` and reports the pixel size.

**Golden image**
`rm2000/rendered/Map0001.png` is this project's own output, not upstream, and is pinned as a regression baseline with its SHA-256. The rendering test compares every byte, so a change in the chipset resolution, the autotile tables, the transparency rule or the draw order now fails the suite instead of quietly producing a different picture.

**Measured facts about the pinned map, not assumptions**
20 by 15 tiles, 320 by 240 pixels, lower layer of block D and E only, upper layer of block F only, those upper tiles fully transparent in the real chipset, 13 distinct colours, and every pixel covered because the room's floor and wall tiles are solid. Three of my expectations were wrong for those reasons and were replaced with measurements. Transparency is therefore verified per tile, and the animated blocks with synthetic maps.

**Validation evidence (2026-09-26)**
- `dotnet build project/UniversalRPG.csproj` — 0 errors; headless runner `All 412 tests passed`, exit `0`.
- `test_rm2k_runtime_rendering.cs` renders a real game directory built from the pinned fixtures, compares it against the golden image byte for byte, checks the frame size and the colour count, and verifies that a missing or malformed chipset image is reported while the runtime keeps running and the tile id framebuffer stays available. Stopping clears the rendered map.

### K-101 — Charset geometry and character frames

**Status (2026-09-26) — DONE: verified charset geometry with a real charset fixture**

**Verified (EasyRPG Player)**
- `src/sprite_character.cpp`, `GetCharacterRect`: the cell is `24 * (TILE_SIZE / 16) * 3` by `32 * (TILE_SIZE / 16) * 4`, which is **72 by 128** with `TILE_SIZE = 16`, placed at `(index % 4, index / 4)`. Each cell holds a 3 by 4 frame grid, so one frame is **24 by 32**.
- `Sprite_Character::Draw`: `row = character->GetFacing()` and `frame = character->GetAnimFrame()`, with anything from `Frame_middle2` replaced by `Frame_middle`. liblcf `rpg::EventPage::Frame` is `left = 0, middle = 1, right = 2, middle2 = 3`.
- `src/game_character.cpp`, `UpdateFacing`: for the four cardinal directions the facing is set to the direction itself, so liblcf `rpg::EventPage::Direction` `up = 0, right = 1, down = 2, left = 3` is the sprite row directly. Diagonal directions have their own rule, which RM2K characters never use.
- The sprite offsets are `SetOx(chara_width / 2)` and `SetOy(chara_height)`, which centres the frame on the tile and puts its feet on the tile bottom.
- `src/cache.cpp` loads charset material as transparent, like the chipset.

**Fixture**
`rm2000/CharSet/Chara1.png` from the same pinned commit, added the same way as the chipset. It independently confirms the geometry: 288 by 384 pixels is exactly four 72 pixel cells across and three 128 pixel cells down, giving twelve characters.

**Refactor**
The paletted PNG decoder was generalised to `Rm2kIndexedImage`, with `Rm2kChipsetBitmap` as the chipset specific wrapper that owns the 480 by 256 contract. Charset, chipset and later picture material share one decoder and one transparency rule.

**Implemented**
- `Rm2kCharset` with the verified cell and frame constants, `FacingToRow` for the project's facing values (2 down, 4 left, 6 right, 8 up), `ClampFrame` matching the Player's `middle2` clamp, `TryGetCell`, `TryGetFrameRect` and `TryDrawCharacter` which places the feet on the tile bottom and clips at the frame edge.
- `Rm2kIndexedImage` is the shared decoder; `Rm2kChipsetBitmap` keeps the chipset size check.

**Validation evidence (2026-09-26)**
- `dotnet build project/UniversalRPG.csproj` — 0 errors; headless runner `All 417 tests passed`, exit `0`.
- `test_rm2k_charset.cs` pins the cell and frame geometry against the real image, the `(index % 4, index / 4)` cell split, the frame clamping, the facing conversion, and the drawing including the clipping behaviour: a character at tile (0, 0) is cut off above the tile bottom, and one fully outside the frame paints nothing without throwing.

### K-102 — Event sprite fields and per-stage placement

**Status (2026-09-26) — DONE: verified sprite placement, not yet wired into the runtime**

**Verified (EasyRPG Player and liblcf)**
- `src/sprite_character.cpp`: `character_name = character->GetSpriteName()` and `character_index = character->GetSpriteIndex()`, and the charset is requested from the `CharSet` directory, like the chipset from `ChipSet`.
- liblcf `LMU_Reader::ChunkEventPage`: `character_name = 0x15`, `character_index = 0x16`, `character_direction = 0x17`. The direction is an `rpg::EventPage::Direction` value.
- The drawable priorities split the characters into three stages: `Priority_EventsBelow = 30` between the map layers, `Priority_Player = 40` shared with "same as hero" events, and `Priority_EventsAbove = 60` after `Priority_TilesetAbove = 50`.

**Parsing gap found and fixed**
The parser declared `character_name` and `character_index` for actors (chunk `0x03`/`0x04`) but never for event pages, even though the ids `0x15`/`0x16` are verified in liblcf. Event sprite data was therefore not available at all. The parser now decodes `character_name`, `character_index` and `character_direction` for every event page, and a missing name yields an empty string, which is what a page without a character graphic means.

**Implemented**
- `Rm2kCharacterSprite` with the verified `StageForLayer` for the three page layers and `FacingFromLiblcfDirection` for the direction, plus a `Skipped` flag so a caller can report a character that could not be drawn.
- `Rm2kMapFrameRenderer.RenderSprites` draws one stage at a time, so the caller can interleave the stages with the two map layers in the verified order, and reports how many were drawn. A character index beyond the charset capacity is skipped and flagged, never taken from an arbitrary cell.
- `Rm2kMapFrameRenderer` can now be created without a chipset for sprite only passes; tile drawing then does nothing instead of throwing.

**Validation evidence (2026-09-26)**
- `dotnet build project/UniversalRPG.csproj` — 0 errors; headless runner `All 419 tests passed`, exit `0`.
- `test_rm2k_charset.cs` pins the layer to stage mapping, the liblcf direction to facing conversion and the fact that the two conversions are inverse, and checks that each stage draws only its own characters, that an out of range index is skipped and flagged, and that the below stage and the hero stage paint different characters.

### K-103 — Hero and events in the runtime frame

**Status (2026-09-26) — DONE: the hero and the events are drawn in the verified order**

**Verified (EasyRPG Player)**
- `src/game_player.cpp`, `Game_Player::ResetGraphic`: `auto* actor = Main_Data::game_party->GetActor(0)` and, when it is null, `SetSpriteGraphic("", 0)`. With an actor it calls `SetSpriteGraphic(ToString(actor->GetSpriteName()), actor->GetSpriteIndex())`. The hero therefore has no page of its own: it is the **first** party member.
- `src/game_actor.h`: `GetSpriteName()` returns the runtime override `data.sprite_name` when it is non empty and otherwise falls back to `dbActor->character_name`; `GetSpriteIndex()` uses `data.sprite_id` in the same case. `SetSprite` clears the override when the requested graphic equals the database values, so a fresh game always draws the LDB graphic and no override has to be invented.
- `src/game_party.cpp`, `Game_Party::SetupNewGame`: `data.party = lcf::Data::system.party`, so the leading actor id is the first entry of the LDB system party list.
- The stage split and the per-stage draw call already existed from K-102; this card only wires it up.

**Parser gap found and fixed**
`LoadCurrentMapEvents` decoded the trigger, the layer and the conditions but never copied `character_name`, `character_index` or `character_direction` into the page, although K-102 had verified the liblcf ids `0x15`/`0x16`/`0x17`. The event sprites were therefore unreachable at runtime. The page now carries all three, and the liblcf direction is converted to this project's facing instead of being stored raw.

**LDB system chunk was not decoded at all**
The hero resolution needs the starting party, and `system` was only a raw chunk. Verified against liblcf `src/generated/lcf/ldb/chunks.h` `struct ChunkSystem` and `src/generated/ldb_system.cpp`: the party list is the size/data pair `party_size 0x15` plus `party 0x16`, and the three vehicle graphics are the scalars `boat_name 0x0b`, `ship_name 0x0c`, `airship_name 0x0d` with `boat_index 0x0e`, `ship_index 0x0f`, `airship_index 0x10`. `DecodeLdbSystem` types those and keeps every other field in `unknown_fields` with its count and framing. A database without a system chunk yields liblcf's empty defaults instead of failing, because that is what a fresh empty database means.

**Defects found while implementing**
- The first `system` decoder overwrote the seeded defaults, so a database without the chunk lost every default key. It now only reports the fields the chunk actually carries and the caller merges them.
- An LCF string field is the raw encoded text; the first test built a length prefix, which the shared decoder does not strip. The test was corrected, not the decoder.
- The declared party size can exceed the stored data, so the list is clamped to `min(declared, data.Length / 2)` and never reads past the chunk. `MaxSystemArrayEntries = 4096` bounds a malformed size field.

**Render order defect found**
`RenderCurrentMap` ran before `LoadCurrentMapEvents`, so the first frame was rendered with an empty event list. The events are now loaded before the framebuffer and the first render. Without this the whole card was silently inert: the suite stayed green and the golden image matched, because nothing was drawn at all.

**Test fixture defect found**
`CopyRealGame` copied `ChipSet` but never `CharSet`, so no character could ever be drawn and the failure hid behind a missing-file diagnostic. The charset is now part of the copied fixture. The constant also needed the `FixtureRoot` prefix, because `GlobalizePath` does not resolve a bare relative path.

**Measured, not assumed**
The pinned LDB has **no starting party** (`party` decodes to an empty list), so the verified `GetActor(0) == null` path applies and this particular game draws no hero graphic. That is the correct result, and the test asserts it instead of inventing a hero. The frame therefore contains the chipset plus the event characters: 85 colours instead of the 13 the chipset-only frame of K-099 produced, and 20 character figures, confirmed by inspecting the rendered image.

**Implemented**
- `Rm2kHeroSprite.FromActor` with the verified fallback and the `CharSet/<name>.png` file name.
- `Rm2kEngineRuntime` builds the frame in the verified order: lower layer, below events, hero plus same-layer events, upper layer, above events. A charset that is missing or undecodable is reported and skips only its characters.
- The page graphic fields are filled in `LoadCurrentMapEvents`, and a skipped character is reported instead of drawing from an arbitrary cell.

**Validation evidence (2026-09-26)**
- `dotnet build project/UniversalRPG.csproj --no-restore` — 0 errors, 0 warnings; headless runner `All 425 tests passed`, exit `0`.
- `test_rm2k_parser.cs` pins the system chunk party list, the three vehicle names and indices, the unknown field count, the empty defaults without a system chunk, and the clamp to the stored data.
- `test_rm2k_runtime_rendering.cs` pins the character drawing (more colours than the chipset-only frame, no missing-charset diagnostic), the hero resolution from the first party actor including the null case, and that a missing charset is reported while the map still renders.
- The golden image is regenerated and re-pinned with SHA-256 `a67ed0672ab97b977c17dc8dd729ef1ffffed8b8339c7e96db2a267cf09a764a`; the byte-for-byte comparison is green again.

**Not implemented**
- No movement animation: the hero and the events are drawn with the static middle frame, so the walk cycle is not exercised.
- The hero is not re-rendered after the player moves; the frame is produced once during initialization.
- The `frame_name` and the transparency level of an actor are decoded but not applied.

### K-104 — Re-render the frame when the player moves

**Status (2026-09-26) — DONE: the frame follows a move, tiles stay cached**

**Verified (EasyRPG Player)**
- `src/scene_map.cpp`, `Scene_Map::vUpdate` → `UpdateStage1` → `UpdateGraphics()` once per frame, and `PreUpdate`/`PreUpdateForegroundEvents` call it again. So the update is per frame, not per input.
- `src/spriteset_map.cpp`, `Spriteset_Map::Update`: the tilemap only receives `SetOx(GetDisplayX() / (SCREEN_TILE_SIZE / TILE_SIZE))` and `SetOy(...)`, i.e. a scroll offset. The tile layers are **static sprites that are not re-rastered on movement**; only `character_sprite->Update()` and the tone change per frame.
- Note the class is spelled `Spriteset_Map`, not `SpriteSet_Map`. An earlier probe with the wrong casing silently matched nothing, which is why the order of verification matters.

**Design consequence**
The map is rastered once into two cached layer buffers, and only the characters are re-composited. This matches the Player instead of re-rastering a whole map per step, and it keeps simulation and presentation separate: `RecomposeFrame` runs inside `Update` on a simulation frame boundary, never per rendered frame, so a higher display frame rate cannot change the simulation.

**Composition order** (`RecomposeFrame`)
1. copy of the cached lower tile layer,
2. below-layer event characters,
3. hero and same-layer event characters,
4. the cached upper tile layer laid over them,
5. above-layer event characters.

**Three defects found and fixed while implementing**
- `PaintOver` first copied every byte, including alpha 0, so the upper layer erased the lower layer and the whole floor. It now keeps the destination pixel where the source is transparent, which is the same rule the verified chipset blit uses. The K-099 golden test caught this immediately.
- The upper layer was originally rastered into the same buffer as the lower layer, so it carried the lower layer with it and covered every character. It is now rastered into its own buffer.
- The character pass drew onto an **empty** buffer instead of the lower layer, which dropped the floor entirely (`opaque=6513` instead of `76800`).

**Defect in the existing API found by the new test**
`RenderSprites` threw on a null map although it never reads the map: a character is placed by its own tile coordinates and the Player's character sprites are independent of the tilemap sprite. The parameter is now nullable and documented, so a sprite pass does not have to invent a map.

**Validation evidence (2026-09-26)**
- `dotnet build project/UniversalRPG.csproj --no-restore` — 0 errors, 0 warnings; headless runner `All 427 tests passed`, exit `0`.
- `TestRm2kRuntimeRendering 9/9`, `TestRm2kCharset 7/7`.
- The strongest check: the rendered frame is **byte identical** to the K-103 golden image, SHA-256 `a67ed0672ab97b977c17dc8dd729ef1ffffed8b8339c7e96db2a267cf09a764a`, with the same 85 colours and 76800 opaque pixels. The refactor therefore changes no output, which is what makes the caching safe to keep.
- New tests pin that moving a character to another tile changes the composited frame, and that `PaintOver` respects transparency and refuses a mismatched buffer.

**Not implemented**
- The frame is still a full-map buffer, not a camera viewport. The Player scrolls by offsetting the two layer sprites; this runtime still renders the whole map, which is correct but not yet efficient.
- Movement is still a single discrete step: there is no walk animation, so the hero jumps from tile to tile and the frame is invalidated per completed step.
- Events have no move routes, so only the hero position changes between frames.

### K-105 — Camera viewport instead of a full-map frame

**Status (2026-09-26) — DONE: the camera is applied and the suite proves it**

**What is implemented**
- `project/src/rm2k/rendering/Rm2kMapCamera.cs`: `DefaultPanX`/`DefaultPanY` (9 and 7 screen tiles at 320x240), `PositionX`/`PositionY`, `OffsetPixelsX`/`OffsetPixelsY` and `PositiveModulo`, all as pure calculations.
- `Rm2kPixelBuffer.TryCopyRegion` reads a window out of a cached layer and **refuses** a region that does not fit instead of clipping it.
- The runtime frame is screen sized (320x240) instead of `width * 16` by `height * 16`. `RecomposeFrame` cuts the cached layers at `ResolveCameraOffsetX`/`ResolveCameraOffsetY` and gives the characters the same offsets through `Rm2kCharacterSprite.PixelOffsetX`/`PixelOffsetY`.
- `AppliedCameraOffsetX`/`AppliedCameraOffsetY` expose what the runtime actually applied, so a test can assert the wiring and not only the arithmetic.

**Verified (EasyRPG Player)**
- `Game_Map::GetDisplayX` = `map_info.position_x + shake * 16`, so the stored position is already the scroll offset. Screen shake is deliberately not implemented: it is presentation state and would couple a cosmetic effect to the deterministic core.
- `Game_Map::SetPositionX`/`SetPositionY` clamp to `[0, tiles * SCREEN_TILE_SIZE - screen_width]` or apply `Utils::PositiveModulo` when the map loops. The source says `std::clamp` must not be used, because for a map smaller than the screen the lower bound exceeds the upper bound.
- `Game_Player::GetDefaultPanX` = `ceil(screen_width / TILE_SIZE / 2) - 1) * SCREEN_TILE_SIZE`.
- `Spriteset_Map::Update` does `SetOx(GetDisplayX() / (SCREEN_TILE_SIZE / TILE_SIZE))`, which is a **division** by 16. This is `OffsetPixelsX`. Multiplying by 16 instead was the first attempt and put the viewport 16 times past the end of the map; the bound proves the direction: the last column of a 40 tile map is 7680 screen tiles, and 7680 / 16 = 480, which is inside the 640 pixel map, while 7680 * 16 = 122880 is not.

**Two upstream unit mixes, both reproduced and pinned**
- `SetPositionX` counts the map extent in screen tiles but the screen in pixels, so a map exactly one screen wide in pixels still has a positive bound (`20 * 256 - 320 = 4800`) and still scrolls.
- The same mix means the reachable offset on a 40 tile map is 480 pixels, which is 160 more than a 320 pixel window needs. The excess is the Player's black border, and `CopyViewport` leaves it unpainted rather than reading past the layer. A test that demanded `offset + screen <= map` was wrong and was corrected.

**Unblock condition met**
A synthetic 40 by 30 map (640 by 480 pixels) is written by the test with the verified LMU field ids (`0x01` chipset, `0x02` width, `0x03` height, `0x47` lower, `0x48` upper, `0x51` events) and event characters at known tiles. Mutation A, forcing the applied camera offset to zero, now **fails** the suite, which it did not before this card. The tile arithmetic in `Rm2kMapCamera` was already unit tested; what was missing was a test that reaches the runtime wiring.

**Still not implemented, and why it matters**
- The scrolled frame is **not** compared pixel by pixel. `GameSimulationState.TileSubstitution` is never populated, so a synthetic map's floor does not render, and the frame contains only the characters. Comparing pixels would compare an empty floor, so the test asserts the applied offsets instead. Populating the LDB tile substitution is the prerequisite for the pixel comparison and is the next card.
- Loop horizontal/vertical flags are exposed on the camera API but never set by the runtime, because the map loop fields are not decoded.
- No screen shake and no configurable resolution: `RenderProfile` is a scaling policy, not a screen size. The renderer uses `Rm2kMapCamera.DefaultScreenWidth/Height` as named constants.
- The above-layer compositing is only covered where the upper layer is transparent in the fixture, so its opacity rule is untested in the runtime.

### K-106 — Block E passability offset and the two-sided movement check

**Status (2026-09-26) — DONE: the premise was wrong, and chasing it found two real bugs**

**The premise in this card was false, and that is the first result**
The card assumed `GameSimulationState.TileSubstitution` was never populated because the LDB chipset carries `lower_substitution_ids` and `upper_substitution_ids` that had to be decoded. Verified EasyRPG `Game_Map::Setup` says otherwise:

```
std::iota(map_info.lower_tiles.begin(), map_info.lower_tiles.end(), 0);
std::iota(map_info.upper_tiles.begin(), map_info.upper_tiles.end(), 0);
```

Both tables start as the identity and are only ever changed by `SubstituteDown`/`SubstituteUp`, which exist for the tile substitution event commands. There is no LDB field to decode, so `Rm2kTileSubstitution`'s identity fallback was already correct, and `Validate` checking both tables against 144 entries is also correct because `BlockEEnd = BlockE + 144` and `BlockFEnd = BlockF + 144`. Nothing needed populating. The card was rewritten once that was proven, and `chunks.h` was checked for `lower_tiles`/`upper_tiles` to confirm they are not LDB chunk fields at all.

**Bug 1 — block E passability read the wrong entry**
`Rm2kChipset.IsPassableLowerTile` applied `+ BLOCK_E_INDEX` only when a substitution object was present. Verified `Game_Map::IsPassableLowerTile` applies it unconditionally, because a missing table is the identity, not a skipped offset:

```cpp
tile_id = tile_raw_id - BLOCK_E;
tile_id = map_info.lower_tiles[tile_id] + BLOCK_E_INDEX;
```

Without the offset a block E tile read passability entry 0 instead of entry 18, so every block E tile in every game inherited the first autotile's passability. In the pinned EasyRPG TestGame that turned 20 tile ids into whatever entry 0 said. Regression: `Test_BlockEUsesTheEIndexOffsetWithoutASubstitutionTable`, which pins the contract with and without a table and proves block C is unaffected. RED was `TestRm2kChipset: 23/24`.

**Bug 2 — movement only checked the target tile**
`GameSimulationState.TryMove` checked `IsPassableInDirection(targetX, targetY, directionBit)` and nothing else. Verified `Game_Map::IsPassable` computes two masks:

```cpp
const int bit_from = GetPassableMask(from_x, from_y, to_x, to_y);
const int bit_to   = GetPassableMask(to_x, to_y, from_x, from_y);
```

`bit_from` is the direction leaving the current tile and is tested against the current tile. Only testing the target let a player walk out of an impassable tile and made one-way tiles wrong in both directions. `TryMove` now checks the current tile with `directionBit` and the target with the reverse bit. Regression: the blocked-tile case in `Test_RealFixtureChipsetProducesBothPassableAndBlockedTiles` now uses a two tile strip with a real blocked id from the decoded table.

**A test that was pinning the bug**
`Test_RealFixtureChipsetProducesBothPassableAndBlockedTiles` asserted `blockedTiles > 0` on the map itself. That only held because of bug 1, which pushed block E tiles onto entry 0. With the bug fixed the EasyRPG map legitimately resolves to passable entries only, so the assertion was rewritten to ask the chipset table for a blocked id and to build the blocked strip from real decoded data. Asserting "this specific map has a wall" was a false claim about a fixture, not a requirement.

**A fourth real defect found on the way: the fixture's upper layer was hiding the lower layer**
The synthetic wide map filled the upper layer with tile 0, which is a block A autotile and paints over the floor. The pinned fixture uses tile 10000 (block F), which is transparent. With tile 0 the frame was 13 colours; with 10000 it is 58 and the floor is visible. The wide map test now uses the fixture's own id and asserts the pixel difference.

**Mutation evidence, all four detected**
- block E `+ BLOCK_E_INDEX` removed: caught by `Test_BlockEUsesTheEIndexOffsetWithoutASubstitutionTable`.
- the source tile check removed from `TryMove`: caught by `Test_RealFixtureChipsetProducesBothPassableAndBlockedTiles` for rm2000 and rm2003.
- camera offset forced to 0: caught by `Test_AMapLargerThanTheScreenScrollsWithTheCamera`.
- `RecomposeFrame` removed from `TryMove`: **caught now**, by the pixel comparison. Before this card the same mutation passed the suite, because the test used the test hooks and recomposed on its own.

**Validation:** build 0 errors/0 warnings; `All 441 tests passed`, exit 0; `TestRm2kChipset 24/24`; `TestRm2kRuntimeRendering 13/13`; pinned golden image unchanged.

**Mostly closed by K-107**
The walk animation and the per frame step budget are implemented, tested and mutation checked, and the event facing bug is fixed. What is still open is the move route: events cannot follow `move_route` at all, so the per frame budget only runs for the player. That is the remaining gap between "the hero walks" and "playable".

### K-107 — RM2K character walk animation and the per frame step budget

**Status (2026-09-26) — VERIFY. The animation and the step budget are implemented, tested and mutation checked. The move route is not started, and one piece of sprite wiring cannot be covered by the available fixture.**

**The animation, verified from upstream**
`Game_Character::UpdateAnim` counts `anim_count` once per update and only advances the visible frame when a per speed threshold is reached: stationary `{12,10,8,6,5,4}`, continuous `{16,12,10,8,7,6}`, spin `{24,16,12,8,6,4}`, indexed by a one based speed. `IncAnimFrame` is `(anim_frame + 1) % 4` and resets the count. A character cell has three columns, and `Sprite_Character::Draw` clamps `Frame_middle2` back to `Frame_middle`, so the fourth rotation value is deliberately drawn as the middle frame. Cycling over three frames would animate at a different rate, so the four value rotation is a test of its own.

**The thresholds overlap.** At the default move speed 3 the stationary limit 8 is reached before the continuous limit 10, so the frame advances on the eighth tick, not the ninth. Four of my first test expectations were wrong about this; the reader was right every time.

**The step budget, verified from `Game_Character::Move`, `UpdateMovement` and `GetSpriteX`**
A move is not a tile snap. `Move` sets the logical tile to the target immediately and sets `remaining_step` to `SCREEN_TILE_SIZE`, 256. `Update` subtracts `1 << (1 + move_speed)` per update, so at the default move speed 3 a tile takes exactly sixteen updates. The drawn position is `GetX() * 256 - remaining_step` for a move to the right, which is what makes the sprite walk across the tile it just entered. `UpdateMovement` clamps at zero so an overshoot cannot wrap the sprite across the map. `GetMaxStopCountForStep` is `1 << (9 - freq)` and 8 or more means no wait, and it uses the move **frequency**, not the move speed, so a character can be slow and still start the next step immediately.

**Implemented**
- `Rm2kStepBudget` with the movement amount, the stop count tables, `Advance`, the `SpriteX`/`SpriteY` formulas and the pixel offsets.
- `GameSimulationState.RemainingStep`, filled by `TryMove` and spent by `UpdateCharacterAnimation`, cleared by `Reset`.
- The runtime's `Update` advances the character once per simulation tick and recomposes the frame while a step is unspent, and the hero sprite carries the step offset and the animation frame.

**A real reset bug this card found, the same class as K-106**
`Reset` did not clear `RemainingStep`, so a new game inherited a half finished step. The regression test found it by driving the real state rather than a fresh instance.

**A real sprite wiring bug this card found**
`BuildCharacterSprites` assigned the camera offset onto the hero sprite, overwriting the step offset `TryBuildHeroSprite` had just set. The step budget therefore reached the state and never reached the renderer, so the hero would have snapped to its tile while walking. The offsets are now added, not replaced.

**A real rendering bug found on the way, unrelated to the animation**
`FacingFromLiblcfDirection` read its argument as a one based axis (`1 => 6, 3 => 4, 2 => 2`) while its own comment documented the real one, `Game_Character::Direction`: `Up = 0, Right = 1, Down = 2, Left = 3`. Every event facing sideways drew mirrored, and nothing caught it because the fixture only has events facing down. Now on the verified axis, pinned by a permutation test over all four directions.

**The golden image changed, as a correction**
Wiring the LMT `character_pattern` through made the fixture's events render their real stored pose 0 instead of the runtime default 1. The difference is confined to the character bands, y 37 to 159, across 48 sixteen by sixteen cells, and the colour count rose from 85 to 92. The old golden encoded the default rather than the game data, so it was replaced after that analysis.

**Tests.** `test_rm2k_step_budget.cs`, 11 cases, covering the movement amounts, the sixteen updates per tile, the clamp, the per update pixel positions, each direction on its own axis, the stop count tables and their independence from the move speed, and the range refusal. `test_rm2k_character_animation.cs`, 10 cases. Two runtime tests: one drives the real `Update` path over the wide map and compares composed frames, one records that an empty party yields no hero sprite.

**Mutation evidence.** Detected: the movement amount shifted to `1 << speed`, the clamp removed, the step offset sign flipped, `RemainingStep` not filled by `TryMove`, `RemainingStep` not cleared by `Reset`, the per frame animation tick loop removed, the frame count changed from four to three, the modulo changed to three, the stationary guard changed, the move speed range check removed, the left and right facings swapped, and the out of range fallback changed.

**Two mutations reported as not mutant, recorded rather than chased.** Moving the direction mapping to a one based axis, and deleting the `0 => up` arm, both leave the function identical on every input because `0` was already handled by the `_ => up` fallback.

**Closing the verification gap: a starting party fixture**
The hero sprite's step offset was not mutation covered, because the pinned LDB defines an empty party, so the verified `ResetGraphic` path yields no hero and the hero sprite is never built. `Rm2kPartyFixtureBuilder` now appends the missing system section to a **copy** of the pinned database, leaving the fixture itself untouched.

Three encodings had to be right, and two of them are not obvious:
- A struct field is `id + length + payload` with both numbers in BER.
- `party_size` (0x15) is a **signed BER integer**, because the library reads it through its signed BER decoder.
- `party` (0x16) is a packed list of **two byte little endian** values, because the library reads it as `(short)(lo | hi << 8)`.

Writing either payload in the other's encoding parses and then reports trailing bytes, or parses and reports an empty party, which is the exact failure the builder exists to prevent.

The section is **appended**, not inserted. liblcf's `Struct<S>::ReadLcf` loops until EOF and breaks on each section's own terminator, and when a nested struct reads fewer bytes than the chunk declared it seeks to `off + length` and logs a corruption warning rather than trusting the inner walk. Our parser does the same, so a database is a sequence of terminated sections and one more is simply appended. Inserting at the first terminator lands inside the actors section, whose declared length is an upper bound that the reader seeks past.

**With a hero present the wiring is now covered, and two more assertions were needed**
- The composed sprite offset is the **camera scroll plus the step**, so the test asserts the difference against `AppliedCameraOffsetX/Y`. Asserting the composed value directly would only have asserted the camera.
- The hero is identified by its charset cell index and map position, not by composition order: an event can share the layer and the tile, and a wide map fixture has both.
- The animation frame needed a second assertion, because after one update the frame has not moved yet. Driving the step forward proves it changes, and driving the rotation to its fourth value proves the `ClampFrame` is applied where the sprite is built rather than only in the charset.

**Mutation evidence for the wiring, all now detected:** the hero's step offset not computed, the camera offset overwriting the step offset instead of being added, the animation frame not assigned at all, and the animation frame assigned without the clamp. Before the party fixture existed, the first two of these escaped.

**Still open on this card**
There is no move route. Events cannot follow `move_route` at all, so the per frame budget only ever runs for the player. That is the remaining gap between "the hero walks" and "events walk", and it is now a card of its own.

### K-108 — WOLF binary .mps reader, built from the verified format

**Status (2026-09-26) — DONE for the map format. WOLF is still not playable; see the honest gaps below.**

**What the previous entry found, and what this entry did about it**
The WOLF reader was JSON-only, so no real WOLF game could ever load. This card implements the verified binary `.mps` map format. The user was asked how to proceed and chose: build the binary readers, no real game is available, so validate against the specification.

**Implemented**
- `project/src/wolf/WolfBinaryMapData.cs`: `WolfBinaryMapData`, `WolfBinaryMapPixel`, `WolfBinaryEvent`, `WolfBinaryEventPage`.
- `project/src/wolf/WolfBinaryMapReader.cs`: `HasMapHeader` and `Read`, plus a bounded little endian `WolfByteCursor` where every read is checked, so a truncated or hostile file yields a diagnostic instead of an out of range access.
- `WolfDataReader.LooksLikeJson` still reports a non JSON payload as an unimplemented binary format rather than blaming the JSON parser. That was the honest-rejection part of the previous entry and it stays.

**Verified format facts used, none guessed**
- Header: ten zero bytes, `WOLFM`, a zero byte, a version header byte (0x00 v2, 0x55 v3), three zero bytes, a u4 that must be 0x64, then a version byte that must be 0x65 (v2) or 0x66 (v3).
- Then a length prefixed title (u4 byte count then the bytes, decoded as Shift-JIS), tileset id, width, height and event count, all u4.
- The map body is a first pixel u4. A value of 0xFFFFFFFF means the map does not exist and **no body follows**; otherwise the body is width * height * 12 bytes read as width * height mappixels of three u4 values each.
- A mappixel's first u4 carries the autotile id as raw / 100000 and the four corner modes as raw % 10000 / 1000, raw % 1000 / 100, raw % 100 / 10 and raw % 10.
- An event starts with 0x6F, a u4 that must be 0x3039, its id, a length prefixed title, map x, map y, the page count, a zero u4, the pages, and a 0x70 footer.
- An event page starts with the five byte signature 79 FF FF FF FF. The reference implementation derives the icon row as (byte >> 1) - 1.
- The map ends with a 0x66 footer.

**A real bug the test caught: a signed/unsigned comparison**
`ReadUInt32` returns `uint`. The first pixel was cast to `int` and compared against the literal `0xFFFFFFFF`, which C# types as `uint`. So `firstPixel` was `-1` and the literal was `4294967295`, the comparison was never equal, and **every map that does not exist decoded as if it had a pixel body**. That shifted the whole file and produced a wrong error, "ends at byte 53 but 57 were needed", which pointed at the reader instead of at the comparison. Fixed by keeping the value in unsigned space. This is the kind of defect that a green test suite with a hand written JSON fixture would never have found.

**A fixture bug found the same way**
The test's `BuildMap` wrote the two base tile values even when the first pixel was 0xFFFFFFFF, which cannot happen in a real file because the format skips the body entirely. The fixture now follows the same rule, so it cannot encode a frame the editor could not produce.

**Tests:** `project/tests/core/test_wolf_binary_map.cs`, 10 cases, all building bytes from the specification rather than from the reader's own output: full field decode, the autotile digit split with all four digits distinct, the non existing map, the event framing, a foreign magic, a wrong event signature, a missing footer, an out of range dimension refused before allocation, a truncated file refused with a byte offset, and an unknown version refused rather than guessed. `TestWolfRuntime` keeps the JSON rejection test.

**Mutation evidence, all three detected:** the signed/unsigned comparison restored, the footer check removed, and the header check removed. Each fails the suite.

**What this card does not claim**
- **No real WOLF game has been parsed.** The framing and the field order are proven against the specification; the interpretation of any single field is not. The next real game this runtime is pointed at is the first genuine test of that.
- The event page body after the signature is only partially decoded: graphic, trigger, move speed, frequency and route. **The command list is not decoded.** A wrong command count would desynchronise every following event, so it is a separate card rather than a guess.
- `database_dat`, `commonevent_dat` and `game_dat` are not implemented. The JSON reader still covers those, so the runtime cannot load a real project end to end.
- Games ship inside a DXLib archive and are frequently compressed or encrypted. The per version keys are published in clear text, so decryption is technically possible, but it is a separate decision and this card does not take it.
- There is no WOLF renderer. Nothing here draws a map.

### K-109 — WOLF event command list, decoded from the verified signature table

**Status (2026-09-26) — DONE for the core command set. WOLF is still not playable.**

**Why this card existed**
A command list with a wrong length desynchronises every following command, every event and every map, so the list is the one part of the WOLF format that must not be guessed.

**An important correction to the source material**
The published `event_command` description carries the header comment "event_command-related structures, **not used for file parsing**". It defines the sub-structures of individual commands but not the generic command frame. An earlier attempt inferred a frame of a **big-endian** signature u4 plus a padding byte from the map parser. **That inference was wrong and the card's original claims below have been corrected.**

**The command frame, as the schema actually describes it**
The frame is `param_count` (`u1`), then `command_type` (`u4` **little-endian**) when `param_count` is nonzero, then a parameter block whose shape belongs to the command, then `branch_depth` (`u1`), `string_count` (`u1`), that many strings, `have_route` (`u1`) and, when set, the route data. A `param_count` of **zero terminates the list**; it is not a parameterless command. The signature and the big-endian reader were removed, and `WolfByteCursor.ReadUInt32BigEndian` is no longer used for the command type.

**Implemented** `WolfBinaryEventCommand` with the verified command type and a name only where the schema gives one, `WolfEventCommandReader` decoding `param_count`, the little-endian type and the type specific parameter block, and `WolfMoveRouteReader` for the optional route.

**Real spec errors found by the tests**
- A command list written as zero bytes is not a list of parameterless commands: zero is the terminator, so such a fixture desynchronised everything after it.
- The `NumberCondition` and `CallCommonByName` layouts in the earlier attempt were guesses. They are now either decoded from the schema or refused.
- `CallCommonByName = 59` was invented. The verified type is **300 (0x12C)**.
- Operation names for the type `121` variants by parameter count were guessed and have been **removed**; those commands are distinguished only by their verified type and parameter count.

**Unknown command types stop the read instead of being skipped**
Skipping an unknown command would shift every following one, so the reader refuses and reports the type. A command this runtime does not implement is a diagnostic, not a silently missing line of a game's script.

**Tests:** `project/tests/core/test_wolf_event_command.cs`, 10 cases, every byte sequence built from the schema's command envelope rather than from the reader's output. `test_wolf_common_event.cs` covers the file header and the list, and `test_wolf_move_route.cs` covers the self-describing route entries.

**Mutation evidence:** the command type read big-endian, the `param_count` not consumed, a route argument count taken from a type table instead of from the file, and each option bit of a route's behaviour and option bytes swapped independently were all detected. The type table mutation is the important one: it proves unknown route entries stay readable.

**What this card does not claim**
- The command frame is read and its types are known, but a command's **meaning** is not implemented. A real event that uses a command this runtime does not interpret stops the read with a precise diagnostic.
- The command list is decoded **as data only**. No WOLF command executes. `WolfEventVm` still runs the JSON command model, not these bytes.
- The transfer format is not read at all, and there is still no WOLF renderer.
- Still no real WOLF game has been parsed. Framing and field order are proven against the schema; the meaning of a command is not.

### K-110 — WOLF database, game settings, common events, commands and move routes
**Status (2026-09-26) — VERIFY. The scoped binary readers are implemented; WOLF is still not playable.**

**Why this card existed**
The user confirmed the scope: binary `.mps`, `database_dat`, `commonevent_dat` and `game_dat`, and that **no real WOLF fixture exists**. Without that last fact every claim here is structural.

**Implemented** `WolfBinaryDatabaseReader` (header, version at byte 10, property position `raw/1000` with index `raw%1000`), `WolfGameSettingsReader` (V2 and V3, the twelve string block, the 23 value u16 record, editor version at index 16), `WolfBinaryCommonEventReader` (15 byte header, the fixed five byte `unknown4` block, the command list), `WolfEventCommandReader` and `WolfMoveRouteReader`. The JSON readers were preserved and only the binary/data discrimination in `WolfDataReader` was changed.

**The WOLFM magic and version framing**
The magic is the six bytes `00 57 00 00 4F 4C`, then a version header byte, `46 4D 00` at bytes 7..9, the version at byte 10 and the type count at bytes 11..14. Guessed bytes were removed after the header was measured.

**Move routes are self-describing, which is the point**
A route entry carries its own argument counts: a four byte count, that many words, a one byte count, that many bytes. An argumentless entry still writes both lengths as zero. There are 59 route types and 12 of them are parameterised, but **the counts are not inferred from a type table** because an unknown type then becomes unreadable. Unknown entries stay readable and keep their raw arguments.

**Route options are bitfields, not bytes**
The behaviour byte holds eight flags. The route option byte uses the **upper three bits**; the lower five are reserved. Reading it as a full byte is a mutation the suite catches, one bit at a time, because a single test with all bits set does not detect a swap.

**Real errors found and fixed**
- Three `X_OKX` sentinels, an invalid `PluginResult<T>.Ok` and a non-existent `PluginErrorCode.CorruptData` were replaced with the repository's real API.
- The database version was read at byte 9 and is at byte 10.
- `HasDatabaseHeader` required 15 bytes including the type count, so a magic-only fixture failed; the two cases were split.
- A binary file was blamed on the JSON reader before the discrimination was fixed.
- `0xFFFFFFFF` needed unsigned handling and a non-existent map sentinel means no body follows.

**Tests:** `test_wolf_binary_map.cs`, `test_wolf_binary_database.cs` (17), `test_wolf_game_settings.cs` (13), `test_wolf_common_event.cs` (17), `test_wolf_event_command.cs` (10), `test_wolf_move_route.cs` (11). Every fixture is byte-authored from the schemas.

**Measured, not guessed:** `0x83 0x65 0x83 0x58 0x83 0x67` decodes to `テスト`, not to the text the first fixture assumed. The expectation was corrected after measuring.

**What this card does not claim**
- The transfer format is **not** read at all.
- Nothing here executes. `WolfEventVm` still runs the JSON model.
- **No real WOLF game has been parsed.** Every test is synthetic. These are structural claims, never real game evidence.

### K-111 — RM2K event move routes, decoded and executed
**Status (2026-09-26) — DONE. Event move routes are decoded from the LMT and stepped in the runtime.**

**Verified structure** The route lives under `EventPage` `0x29`, with the command count at `0x0B`, the array at `0x0C`, `repeat` at `0x15` and `skippable` at `0x16`.

**Semantics that were wrong before they were measured**
- The first update **starts** movement and consumes no step budget.
- The command index advances only after movement **completes**.
- A blocked move advances only when `skippable` is true.
- Facing commands execute immediately and consume no movement.
- A finished route clears the remaining step budget.

**Implemented** `Rm2kMoveRoute`, `Rm2kMoveRouteState`, `rm2k_move_route_decoder.cs` and the runtime tick in `Rm2kEngineRuntime.Update`, with typed `MoveCommand`/`MoveRoute` models on `Rm2kMap`.

**Fixture boundary, stated honestly:** the pinned `Map0001.lmu` has 22 events and pages and **zero** move-route chunks, so it cannot prove a route. The end to end proof is a byte-authored synthetic LMU, and that is what `test_rm2k_event_move_route.cs` uses.

**Tests:** route `9/9`, decoder `9/9`, state `12/12`, end to end `7/7`.

**What this card does not claim:** only the verified command set is implemented. The pinned fixture exercises none of it, so no real game's route has been stepped.

### K-112 — RGSS archive format, shared by XP, VX and VX Ace
**Status (2026-09-26) — DONE as a reader and writer. Nothing executes an entry.**

**Why this was first for XP/VX/Ace** Without the archive an XP game cannot start at all, and this format is the one thing all three of the RGSS engines share, so it counts for three criteria where a Ruby virtual machine would count for none of them until it ran.

**Verified against the reference implementation.** Magic `RGSSAD`; every value is exclusive ored with the output of a linear congruential generator that starts at `0xDEADCAFE` and advances **once per value** by `magic = magic * 7 + 3`. A value is obfuscated with the generator's state **before** that step.

**The header is eight bytes**: the name `RGSSAD`, one byte the format does **not** check, and the version. The reference reader compares the first six bytes and reads the version from the last. A reader that also required the seventh byte to be zero would refuse a file the format allows, so the suite proves that byte is ignored instead of assuming it is zero. The version byte is what tells an XP or VX archive from a VX Ace one.

**Each entry** is a name, a size and a body. The name is obfuscated byte by byte and a backslash in it folds to a slash. The list ends when a name can no longer be read, not at a terminator. An entry claiming more bytes than the file holds is refused rather than handed back short, because a short body looks like a successful read.

**Two of my own bugs, both caught by tests rather than by reading.** A regular expression pass removed the `return` from three failure paths, so a refused archive fell through and was read anyway while the diagnostic said it was not an archive. And an unused version read indexed one byte past the end of an eight byte header, which crashed on an empty archive.

**Two of my own wrong expectations:** I had the header as three zero bytes after the name, and I had the generator taking two steps per field. The first surfaced as an out of bounds read on an empty archive, the second as a round trip that decoded a name length of three hundred million, which was **reproduced outside C#** before the reader was touched again.

**Tests:** `test_rgss_archive.cs` 15/15, total `678/678`.

**Mutation evidence, six of six detected:** a seed off by one, a wrong multiplier, a name byte read without the key, a version read from the wrong offset, the unchecked byte checked, and an entry list that started four bytes late.

**What this card does not claim:** the reader lists and reads entries. It **executes nothing**; a game script is bytes. There is no real RPG Maker game in the repository, so no real archive has been read.

### K-113 — Ruby Marshal reader for the RPG Maker data files
**Status (2026-09-26) — DONE as a reader. No game class is instantiated and no script runs.**

**Why this was second** With the archive in place the other half of the data pipeline was missing: XP, VX and VX Ace keep their data in `.rxdata`, `.rvdata` and `.rvdata2`, which are Ruby Marshal streams.

**Verified against the published Ruby specification**, not from memory: a two byte version, then one value, where a value is a type byte and a payload whose shape belongs to the type.

**Integers are the part that is easy to get wrong, and I got it wrong first.** A marshalled integer is a type byte and then one to five bytes, where the first of those encodes sign and width in a single value. Eight values are special; the rest is a sign extended byte with an offset of five. A reader that treats the first byte as a length decodes small numbers correctly and everything else as something plausible but wrong.

**An object takes its index before its contents are read**, because a value inside a collection may link back to that collection and the link names an object the stream has already defined. Numbering afterwards would point every such link at the wrong object.

**A link does not take an index of its own**, because it names an object that already exists. My first expectation had this wrong and the measurement corrected it.

**A regexp carries no class name**: the specification gives a source and an option byte and nothing else. A bignum is **refused rather than read**, because a game's data uses fixnums for anything that fits and a bignum would mean arbitrary precision this reader does not carry.

**Refusals, not partial trees:** a stream that ends inside a value, declares a length past the limit, or carries an undefined type byte raises. A major version this reader does not implement is refused outright and a **newer minor version** is refused too, because it may use a type this reader has never heard of; an older minor version is read.

**Two mistakes of mine, the second only visible under mutation.** A grouped `case` list plus single `case` labels for the same values left the later ones unreachable, so a regexp fell through to the refusal branch; and when that label was removed the routing line went with it, so no regexp could be read at all. Routing and payload are now separate concerns.

**The reader produces a value tree, not live objects**, on purpose: a game database is full of instances of classes this project has never heard of, and resolving them would mean either running the game's Ruby or inventing classes that do not exist.

**Tests:** `test_marshal_reader.cs` 29/29, total `678/678`. The fixtures are written by hand from the specification's type table, because a round trip through a writer of our own would pass even if the reader and the writer were wrong in the same way.

**Mutation evidence, fifteen detected:** a flipped sign offset, a swapped sign case, a zero case that swallowed a byte, a width read one byte short, an array numbered after its contents, a hash likewise, an uncapped nesting depth, an unchecked major version, an unchecked minor version, an uncapped byte count, an unchecked symbol link, an object link accepting index zero, a regexp reading a class name, a symbol link resolving out of range and a delayed array index.

**Two mutations turned out to be equivalent rather than escaping.** Moving `++ObjectCount` below the `ReadLength` call changes nothing, because reading a length does not touch the counter. A mutation that cannot change behaviour cannot be caught by a test, and recording it as a gap would have been wrong. A mutation that really delays the index until after the elements were read is detected.

**What this card does not claim:** no real `.rxdata` has been read, because the repository has no RPG Maker game.

### K-114 — RM2K vehicles: state, boarding, sprites and the airship shadow
**Status (2026-09-26) — VERIFY. Simulation and rendering are implemented and mutation tested; K-094 is not closed by it.**

**Verified from the reference implementation, not guessed.** Boat and ship move at speed 4 and the airship at 5, so a move speed of 3 means half speed. A vehicle's altitude is measured in tile units against a budget of 256 and falls by 8 per update. A moving vehicle animates over 12 frames and a stopped one over 16, both modulo 4. The airship's shadow is a separate sprite drawn from `(128,32,16,16)` and `(144,32,16,16)` at opacity `(int)(0.26 * 255) = 66`, one below the airship, and visible only while the player is aboard.

**Boarding is asymmetric.** An airship refuses a boarding attempt from a tile it is not directly over, and refuses a disembark while still in the air. A boat or a ship does not. Boarding has priority over an action event, which the reference implementation proves by the order `if (!GetOnOffVehicle()) CheckActionEvent(); return;`.

**Vehicle background music is deliberately absent.** There is no BGM state contract in this project, so switching a vehicle's track would mean inventing one. It is not hidden behind a diagnostic flag; it is simply not there.

**The pinned fixture cannot show a vehicle, and the tests say so.** All three vehicles in the pinned `RPG_RT.lmt` target map 39, while `Map0001` is map 1, and the pinned `vehicle.png` is absent. The drawing path is therefore exercised through a **derived** fixture that copies the game and adds the official reference test image, leaving the pinned data untouched. A test states the boundary explicitly instead of pretending otherwise.

**Test-only hooks** exist to place a vehicle on the map under test and to re-render. They are called from tests only and are documented as such.

**Tests:** vehicle `11/11`, boarding `11/11`, decision turn `12/12`, sprite `9/9`, compositing `6/6`, runtime rendering `19/19`.

**Measured after the fact:** the airship's system index is **3**, not the 2 the first expectation assumed.

**What this card does not claim:** `move_random`, hero directed movement, broader event and audio integration and the rest of the whole engine remain open. This card is one slice of K-094, which stays `VERIFY`.

### K-115 — Ruby lexer for the RGSS engines
**Status (2026-09-26) — DONE as a lexer. It calls nothing, resolves nothing and runs nothing.**

**Where this sits** With K-112 and K-113 in place, this is the third of the three layers XP, VX and VX Ace need before their scripts can be read, and the first that looks at the script text itself.

**The keyword list is Ruby's own, not written from memory.** Forty one reserved words extracted from the grammar's `parse.y`. A keyword is reserved, so a lexer that treated one as a name would accept files Ruby rejects and reject files Ruby accepts.

**A name beginning with an upper case letter is a constant, and the reserved word check comes first.** Two reserved words, `BEGIN` and `END`, begin with an upper case letter and the grammar's `reswords` production lists them as keywords. Checking for a constant first read them as names. A test over **all forty one** words is what found it, because the single example I had chosen happened to be lower case.

**A slash divides where a value has just ended and opens a regular expression where one could begin.** The first version had this backwards, so `a / b` was read as a regular expression that ran off the end of the line. Both shapes are in the suite because they differ only in what came before the slash.

**A single quoted string interprets only two escapes**, the quote and the backslash. Reading it like a double quoted one loses a backslash a game asked to keep, which is the entire reason the form exists.

**A regular expression keeps its backslashes**, because the pattern engine is what interprets an escape, and a `/` inside a character class does not close it.

**A string keeps its bytes as well as its text**, because a Shift-JIS script is not UTF-8 and a reader that kept only text would silently corrupt it.

**An octal literal may be `0o17` or `017`.** The marker sits between the leading zero and the digits; checking the current character instead of the next one read `0o17` as a bare zero.

**Refusals, not partial token lists:** an unknown character, an unclosed string, an unclosed regular expression and a number with no digits in its base all raise with their line.

**Tests:** `test_ruby_lexer.cs` 33/33, total `711/711`.

**Mutation evidence, fourteen run and eleven detected:** a keyword list never consulted, the reserved word check moved after the constant check, the constant rule inverted, a slash always a regexp, a slash always a division, single quoted escapes applied in full, the octal marker not skipped, a shorter operator matched first, an unclosed string accepted, a line continuation read as a break, a block comment not skipped, a class variable read with one at sign, a regexp losing its backslash, an unterminated block comment end.

**Two mutations were equivalent rather than escaping.** Appending `<=` and `<<` to the operator list changes nothing, because every multi character operator already appears before the shorter one it starts with; the check printed the whole list to establish that. Turning a byte escape's `((char)value).ToString()` into `value.ToString()` changes nothing, because the cast already produces values in the range where the two agree. A mutation that cannot change behaviour is not a gap in the tests.

**Four gaps the mutations found were real and are now closed:** the keyword lookup was untested, the single quoted escapes were only checked for one letter, the operator order was checked only for the operators the test happened to use, and the line continuation test filtered the very newline it was about.

**One mistake of mine in the tests hid four failures.** The helper that drops whitespace-only tokens did not drop the end of input token, so every list based assertion was off by one element. A probe with a different filter showed the lexer's output had been right all along.

**What this card does not claim:** there is no parser yet, so a script is a token stream and nothing more. **No real RPG Maker script has been tokenised**, because the repository has no RPG Maker game.

### K-116 — Ruby parser for the RGSS engines
**Status (2026-09-26) — DONE as a parser. It builds a tree and runs nothing.**

**Where this sits** With K-112 (archive), K-113 (Marshal) and K-115 (lexer) in
place, the data of an XP, VX or VX Ace install is readable from end to end as
data. A game's Ruby now has three layers: bytes, tokens, and this tree. What is
still missing is everything that would give the tree meaning.

**What it does** `RubyParser` turns a token stream into a tree of shapes. It
answers one question — what shape was written. It does not answer what any name
means, whether a call succeeds, or what a value is at run time. A node that
records a call names the method as written and knows nothing about whether this
runtime has ever heard of it.

**The precedence is the grammar's own.** Every level was taken from the
declaration order in the Ruby grammar rather than from memory. This turned out
to matter more than expected: the first table written from memory had the
relations and the equality on separate levels, which the grammar's
`rel_expr %prec tCMP` shows are one. A reader that gets one level wrong parses a
game's arithmetic into a different tree, and nothing about the result looks
wrong.

**What is deliberately not here**
- No name resolution, no method lookup, no constant lookup.
- No execution, no evaluation, no calling of anything.
- No literal Ruby objects, no binding, no class loading.
- A shape this parser cannot read raises with its line. A tree that stopped
  early would be worse than none, because nothing would mark it as complete.

**Verification (2026-09-26)**
- `TestRubyParser` 44/44, total 755/755, `scripts/validate.sh` passed.
- Every expected tree is written out by hand from the grammar's rules. A tree
  produced by the parser and compared against itself would prove nothing.
- 21 mutations, all detected.

**Errors the tests found in this parser, all fixed**
- The precedence table from memory had relations and equality on two levels; the
  grammar resolves them onto one with `rel_expr %prec tCMP`.
- `**` sat at the arithmetic level instead of above it, so `a * b ** c` parsed
  as `(a * b) ** c`.
- A member call's argument list was skipped whenever the receiver was a name, so
  `sprite.draw(x, y)` read its parentheses as a grouping.
- A block's body was read as a whole program, so every `def` and `do` reported a
  missing `end` on a file that is well formed.
- A `do` belonging to a `while` was read as a block on the loop's own condition.
- The range operator had no level at all, so `1..2` parsed as two statements.
- `not` was read both in `ParseUnary` and in `ParseBinary`. The second was
  unreachable, and the mutation suite showed the 44 tests passed with it gone, so
  it was removed rather than kept as a second route to the same node.
- Two mutation escapes turned out to be untested boundaries rather than wrong
  code: nothing crossed the logical/bitwise boundary, and nothing pinned `not`
  to its own level. Both now have tests.

### K-119 Read a whole number wider than this machine holds
`READY` → `IN PROGRESS` → `DONE`

**The question that started this** The marshal work had been checked against
Ruby 3.4, because that is the documentation that is easiest to reach. The
engines of this repository's line run older rubies, so the whole ground truth
was suspect. It was checked against the sources themselves:

- **XP is Ruby 1.8.1, VX is 1.8.3, VX Ace is 1.9.2.** All three carry a marshal
  format.
- All twenty five type bytes are **identical** across 1.8.7, 1.9.3 and 3.4.1.
  The format did not change for the engines in question.
- The whole number form did change, and in the direction that matters:
  **1.8 and 1.9 write `i` for a number that fits in thirty one bits and `l` for
  the digits of anything larger. Ruby 3 swaps the two letters and writes the
  large form in binary.** A reader built from the 3.4 table would refuse every
  file an engine of this line writes.

**What the reader had wrong, and it was wrong about the sign**

A whole number that does not fit is written as a sign and one byte per digit,
and the digits of a negative number are the number carried to the width it was
written in, so every byte after the first is the top of the width. The reader
was negating the unsigned value instead, which looks the same for a one byte
number and is not the same for any other: **it read one byte too many and took
the first byte of whatever followed in the file.** A game's negative coordinate
would have had the next value's bytes inside it, and nothing downstream can tell
that from a real number.

The one byte negative form was also on the wrong side of the boundary. The rule
is five to one hundred and twenty seven is the number with five taken off, and
minus one hundred and twenty nine to minus five is the number with five added,
with minus one to minus four the wide form. The reader had the last two the
wrong way round.

**How it was found** Not by three hand written cases. A test walks six thousand
and one numbers through the writer taken from 1.8.7's own loop and compares
each against what the reader says, and a second test holds sixteen numbers
against the bytes that loop produces, because the first version of those was
written from memory and was wrong about four of the sixteen. **Every fault found
in this card was in the test rather than in the reader**, which is the opposite
of what the range test was written expecting, and the reason it is worth having
is that it is the only one of the two that can find a fault at all.

**A fault this card found in a fault of an earlier card** The earlier card
refused a wide number with the reason that it would need arbitrary precision.
That reason was wrong: a game's number is written as decimal digits, so the
number itself is readable, and the only question is whether it fits this
machine. It is read now, and refused only when it does not fit, with the number
of digits in the reason so that a number too large and a file that is not
marshal are not the same fault.

**One thing this card did not add** A check refusing a count byte wider than a
whole number. Such a count cannot occur: five to one hundred and twenty seven is
the one byte form, and a count of one hundred and twenty seven is the number one
hundred and twenty two. The check was written from a reading of the byte range
rather than of the rule, and it refused a length a game writes for every list it
has. It is gone, and the fact it was based on is tested instead.

- Tests: `TestMarshalReader` 39/39, total 818/818, validator passed, 0 warnings.
- 8 mutations of the packing, all detected.

**Still missing** A whole number wider than a whole number this machine holds is
read and then refused, which is honest but means a game holding one will not
load. No archive from any of the three engines is in the repository, so all of
this is verified against the rubies' own sources and not against a game.

### K-118 Name what every child of a tree is for
`READY` → `IN PROGRESS` → `DONE`

**What it is** A consumer of the parse tree has to ask a node for the test, the
body, the left of an operation or the first argument, instead of knowing the
layout of every kind by heart.

**The fault this found** The parser said only that a child was there, and what
the list meant depended on the kind and on nothing else. A keyword that opens a
test held the keyword first and the test second, a ternary held the test first, a
block on a call held the call, the parameters and the body, and a block that was
a body held only statements. **One kind could mean two things, and the second
meaning was invisible.** Every consumer would have had to learn the layouts from
the parser's source, and nothing would have said when one of them was wrong.

**What changed** Every child carries the role it plays. `Children` stays for a
reader that wants the order and does not care what the order means, and a node
whose roles are empty has not been given roles rather than having none. A lookup
for a role that is not there answers null instead of falling back to the first
child, so an absent role cannot be mistaken for a present one.

A name is held in `Name` and not in `Text`. That was worth a test, because a
test reading `Text` finds null and could be fixed either by filling `Text` or by
reading `Name`, and only one of those is right.

- Roles filled for a keyword that opens a test, a ternary, an assignment, an
  operation and a call. The four places that build a call say their shape through
  one helper rather than each repeating it.
- Tests: `TestRubyParser` 52/52, total 808/808, validator passed.
- 6 mutations on the roles and the lookup, all detected. Two of them escaped at
  first because a lookup that takes the last of a role and one that takes the
  first cannot be told apart while every node holds at most one child under a
  role, and because a lookup that fell back to the first child passed every test
  that asked for a role that was there. Both are now tested.

**Why this comes before a machine** A machine that ran the tree would have had to
read the parser's source to know which child was the body, and a mistake there
runs a name as if it were a statement. That is quietly wrong rather than loudly
wrong, which is the worst shape a mistake can have.

**Still missing for XP, VX and VX Ace** A machine to run the tree, and any real
archive from any of the three engines.

### K-117 — The value layer between a game's data and its language
**Status (2026-09-26) — DONE as a value layer. It names values and judges none.**

**Where this sits** K-112 reads the archive, K-113 reads Marshal, K-115 reads
the tokens and K-116 reads the tree. What was missing between "a file said this"
and "the language calls this a value" is this card. Without it the two layers
would each have their own idea of what a game's data means, and they would
disagree without either being wrong.

**What it does** `RubyValue` holds the seven kinds the language defines, as
data, with a value's identity and its contents and nothing else.
`RubyValueConverter` turns a decoded Marshal value into one, following the
file's links, and refuses anything that has no equivalent.

**The refusals are the point.** A kind the language has no name for, a payload
that contradicts its kind, a mapping entry without its other half, a mapping that
holds the same key twice, a link to an entry that was never decoded: each of
these raises with the reason, and each refusal is counted and remembered. A value
that is nearly right is a value a game cannot be trusted with, because nothing
downstream can tell it apart from a real one.

**What is deliberately not here**
- No arithmetic, no comparison, no conversion between kinds.
- No method dispatch, no calling of anything.
- No class loading: a game's own class is kept as the text the file wrote.
- No decoding of a string's bytes. A game's strings are in its author's
  encoding, usually CP932, and choosing one is a decision this layer does not
  make.

**Verification (2026-09-26)**
- `TestRubyValue` 15/15, `TestRubyValueConverter` 30/30, total 802/802,
  `scripts/validate.sh` passed.
- 13 mutations on the converter's kind names, its refusals, the link following
  and the value identities, all detected.
- The link tests read real byte streams written with the format's own packing.
  A Marshal long is not eight bytes, and a stream written with eight would be a
  different stream from the one a game writes.

**A bug this work found in the reader the card before it**
The converter was written against kind names spelled out from memory. The reader
emits `array, false, float, integer, nil, object, regexp, string, struct,
symbol, true` — and two of the converter's names were not on that list. A whole
number arrives as `integer` and a string as `string`, so **every number and
every string in a real game's data would have been refused**. The kind names are
now taken from the reader itself rather than from memory, and two mutations that
delete each of the two arms are both detected.

**The numbering is the reader's, and the specification is explicit about it**
A stream holds one copy of each object and one of each symbol. The first object
has the number one and the first symbol the number zero. The converter reads
that number from the value the reader handed over instead of counting again,
because counting again would be a second opinion about a number that was
already decided.

A container is numbered **before** its contents are read. That is not an
implementation detail: it is the only reason a container can hold a reference to
itself, which a game's data does whenever a structure names itself. The
documented stream for an array holding the same string twice,
`"\004\b[\a\"\nhello@\006"`, has the array at one and the string at two, and
the link names two.

**Other bugs this work found in the converter**
- The converter never recorded the stream's own entry numbers, so every link was
  reported as pointing at nothing.
- The first version numbered values from zero and after their contents, which
  gave a container a higher number than its first member.
- A value was filed under its number before its class was attached, so a link to
  a game's value found an object that no longer said what class it was.
- A value that points at itself is refused with the number in it, because there
  is no value to return yet and returning something else would give it a second
  identity inside its own contents.
- The class name was being attached twice, once where the contents are read and
  once afterwards. The second could never change anything, and a mutation that
  removed it went unnoticed, which is how the duplicate was found. It and the
  helper it alone used are gone.

### K-120 Read the data three real games actually wrote
`READY` → `IN PROGRESS` → `DONE`

**The gap that started this** Every parser and reader in this repository was
written against a fixture this repository made, or against EasyRPG's test game.
Both are right for what they are and neither is a game: the RM2K test game has two
hundred and ten bytes of database, six bytes of map tree and five maps, and every
count, length and index in a game of that size fits in a byte and would not in a
real one. **No fixture in this repository was written by an engine.** A reader
that has only ever read a hand made file has never been shown a game.

Three games were given to the repository to use. They were classified from their
own files and nothing in them was executed.

| Game | Engine | Decided by |
|---|---|---|
| `rgss-xp` | RPG Maker XP / RGSS1 | `RGSS104J.dll`, `Game.rxproj` naming `Scripts.rxdata` |
| `rgss-xp-microquest` | RPG Maker XP / RGSS1 | `RGSS104E.dll`, `Game.ini` |
| `rm2k-dragon-destiny` | RPG Maker 2000 | `RPG_RT.ldb`, 743 `.lmu` files, `RPG_RT.ini` |
| `kirikiri` | **KiriKiri, not WOLF** | `SoftModeFlag`, `FrameSkip`, `SEandBGM`, no `Game.dat` |

**What the real files found**

The XP games keep their database as marshal and, with an unencrypted archive, in
plain files under `Data/`. Sixteen of them are now in the repository, ~310
kilobytes, and **all of them are read**: `TestRealXpData` walks every value of
every file and finds no fault. The marshal reader is now checked against bytes an
engine wrote, not only against the rubies' own sources.

Two of my own assumptions were wrong and the files said so:

- **A map is not a hash.** It is an `RPG::Map` object with eleven members, and
  eleven keys. The reader was right; the expectation written from memory was not.
- **A `.lmu` holds an `LcfMapUnit`**, not an `LcfMap`. Measured, not remembered.

**The KiriKiri game was offered as a WOLF game and is not one.** It carries
folders called `BasicData` and `MapData`, which are two of the three things the
Wolf detector looks for, and its data folder is laid out the way a Wolf game's is.
It has no `Game.dat` anywhere, and that is the third thing. The detector refuses
it, and `TestKirikiriIsNotAWolfGame` proves the refusal is a decision rather than
an accident of not having looked: **the same folder with a `Game.dat` in it is
detected as Wolf.** A folder full of what a Wolf game would have is not a Wolf
game, and a detector that answers either way rather than refusing has guessed.

**What is claimed and what is not**

Claimed: the XP detector recognises an XP installation from its own files and
does not confuse it with VX or VX Ace; the marshal reader reads sixteen real
files from two independent installations; the RM2K parser reads a 416 kilobyte
database, a 57 kilobyte map tree and two maps of a 743 map game.

Not claimed: that a game's data is **understood**. A database read as a
dictionary of chunks is a database read; it is not an actor, an event, a page or
a chipset. A map file is read as an `RPG::Map` holding its members; nothing here
knows what a member called `@events` is for. There is still no renderer, no
script execution, no save path, and `RgssEngineRuntime` is still metadata only.

**Tests and evidence**

- `TestRealXpData` 6/6 — sixteen real files, two installations, two encodings.
- `TestRealXpDetection` 3/3 — an XP folder is XP, is not VX or VX Ace, and a
  folder with data but no layout is either XP or nothing.
- `TestRealRm2kData` 3/3 — a real 416 KB database, its 57 KB map tree, two maps.
- `TestKirikiriIsNotAWolfGame` 2/2 — the refusal, and what makes it a decision.
- `TestMarshalReader` 40/40 — the last one added tells a number this machine
  cannot carry apart from a file it cannot read, which is the one mutation of the
  eight that escaped the first suite.
- Total **833/833**, validator passed, build 0 warnings / 0 errors.
- **Eight mutations of the reader, all detected.** Two of the first suite's eight
  did not test anything: it counted a mutation as breaking the build whenever
  `error CS` appeared anywhere in a run, and the run prints the mutation report
  of the step before it, so six that compiled were reported as broken. The suite
  now compiles each mutation on its own and only calls it broken if that build
  really fails.
- `project/tests/fixtures/RGSS_FIXTURES.md` holds every file with its size and
  SHA-256. No executable, DLL, save, image, audio or script is imported.

**Deferred, not done**

- The XP games' `Scripts.rxdata` is deliberately **not** imported. A script is
  code, and this repository does not run a game's code.
- 743 maps of `rm2k-dragon-destiny` are not in the repository; two are, and the
  rest are the same format at a different size.
- No archive from any of the three engines is in the repository, so the
  `RgssArchiveReader` still has no real file to read.

### K-121 Read the data an RPG Maker MZ game wrote
`READY` → `IN PROGRESS` → `DONE`

**The gap that started this** K-120 brought in real data for RGSS and RM2K and
left MV and MZ where they were: **detection and a count of entries**. There was
no reader that returned a game's values. An MZ game keeps its database as plain
JSON, so a reader for it can exist without a runtime and without running a line
of the game's own code, and none was written.

An MZ 1.9.1 game was given to the repository. It was classified from its own
files — `game.rmmzproject`, `node.dll`, `package.json`, `js/rmmz_core.js` — and
nothing in it was executed.

**What was built**

- `project/src/mz/MzJson.cs`: JSON read the way a game wrote it, with nesting
  bounded, a string that is not closed refused rather than run to the end, an
  escape the editor never writes refused by name, and a number with an exponent
  and no digits refused.
- `project/src/mz/MzDataFile.cs`: one of a game's data files, holding **one root
  value** and the file's own text so a caller can hash what was read.

**Three things the format has, all found by reading the file and not a
description, and two of which were wrong in a first draft of the test**

1. **A database file's first entry is null.** `Actors.json` is `[
null,
{...}]`.
   The editor numbers its actors from one so zero can mean "no actor".
2. **A command is a small number and is not packed.** This game's commands are
   `121`, `231`, `357`, `657` and nothing above a thousand anywhere in the file.
   In the generation before, a command's number is its value times a thousand
   and a reader divides by a thousand. **A reader written for MV and pointed at
   this file would divide every command to zero.**
3. **A map's events are indexed by event, not padded to the field.** `Map002` is
   seventeen by thirteen and its `events` array holds seven entries, the first
   null. A first draft of this test claimed the array ran over the whole field.

**An API of mine that was a trap, and removed rather than documented**

The first version of `MzDataFile` exposed `Top` as a `List<MzValue>` holding the
one root value, so `Top[0]` was the file and `Top[0][0]` its first element. The
test that was written against it then read the array where the object was and
failed in four places at once. **A one element list is not a root value**, and it
is now `Root`, an `MzValue`.

**The reader that was already here is a different thing, and the difference is
now stated by a test**

`MzDataDirectoryResult` exists and counts entries, takes names and caps a file
at 2 MiB. `TestMzReaderBoundary` says so and checks the cap it states. It returns
no map, no event, no command and no coordinate, and the new reader returns
values and does not name a game. Neither is derived from the other.

**Tests and evidence**

- `TestRealMzData` 15/15 — eleven real data files, the three format traps above,
  and five refusals.
- `TestRealMzDetection` 3/3 — the game is MZ, is not MV, and the folder with the
  previous generation's runtime is answered differently.
- `TestMzReaderBoundary` 2/2 — the boundary to the reader that was already here.
- `TestMzDataDirectory` 8/8, unchanged.
- Eight mutations of the new reader. **The first suite detected one of eight**,
  and that is the honest number: it found five real gaps — an unclosed string was
  run to the end of the file, an unknown escape was taken as text, nesting was
  unbounded, a broken exponent became a number, and a file's own text was thrown
  away — plus one anchor that did not exist. Each gap got a test of its own and
  the suite was rerun.
- Total **853/853**, validator passed, build 0 warnings / 0 errors.
- `project/tests/fixtures/MZ_FIXTURES.md` holds every file with its size and
  SHA-256. The two `js` files are **placeholders carrying the real names**: the
  runtime is 175 KB and 83 KB of a game's own code and is not imported.

**Still not true of MZ**

- No JavaScript runtime, so **no plugin, no script, no event command runs.** An
  MZ game does not play.
- Nothing here knows what command 231 does or what a page's conditions mean.
  Values are read; they are not understood.
- MV shares the data format and has **no fixture at all** from a real game, and
  its command numbering is the packed one, which is exactly the difference the
  second trap above is about.

### K-122 Name every command an RPG Maker MZ game stores
`READY` → `IN PROGRESS` → `DONE`

**The gap that started this** K-121 read a game's numbers and could say a
command was 121, which told a caller nothing. A name per command needs a source,
and the source is not a documentation page: it is the method the engine dispatches
the command to, which the engine's own source carries the name of in the comment
above it.

**What was built**

- `project/src/mz/MzCommandName.cs`: **114 commands, 101 to 603**, every number
  and every name generated out of the engine source of a real game. A record
  `MzCommand(int Code, string Name)`, so a number and its name are one value.
- `project/src/mz/MzCommandTable.cs`: what a number in a command list is — a
  command, the data of a command, the editor's own indent, or unknown — and which
  command reads which data number.

**A table written from memory, and what it cost**

The first draft of the table was written by hand. Compared against the engine,
**79 of its 178 names were wrong.** 129 was written "Change Hp" and the engine
calls it "Change Party Member". 231 was "Move Event" and the engine calls it
"Show Picture". Twenty six commands the engine has were missing and forty three
that it does not have were there. A plausible command a game does not use is
invisible until a game uses it, and this game uses 231.

**The rule that is not a rule, measured**

"Which command does this data belong to" invites `code - 300`. Against this game
that is right **four times out of eight**:

| Data | Owner measured | `-300` says | What that is |
|---:|---:|---:|---|
| 401 | 101 Show Text | 101 | right |
| 405 | 105 Show Scrolling Text | 105 | right |
| 408 | 108 Comment | 108 | right |
| 655 | 355 Script | 355 | right |
| 412 | 111 Conditional Branch | 112 | **Loop** |
| 501 | 102 Show Choices | 201 | **Transfer Player** |
| 605 | 302 Shop Processing | 305 | not a command here |
| 657 | 355 Script | 357 | **Plugin Command** |

Two of the four mistakes point at a command that exists in this generation and
does something else. A reader that used the rule would read a branch's else as a
loop, a choice as a teleport, a shop's purchases as a number meaning nothing, and
a script line as a plugin call. **The owners are written down because none of them
can be calculated**, and a test says so by running the rule and counting four.

**411, 412 and 413: two commands and one piece of data**

All three sit at an indent of their own under a branch, so all three look like the
branch's options. The engine names **411 "Else"** and **413 "Repeat Above"** as
commands of their own, and gives **412 no method at all**. A reader that treated
the family as data would refuse two real commands; one that treated it as
commands would run a branch's structure as an instruction. Both fail silently.

**A name written twice, and three mutations nobody saw**

The first shape was an enum with a name in each member's doc comment and a second
table beside it carrying the same names as strings, because a C# identifier cannot
be `Show Text`. **The two copies drifted and three name mutations were invisible**
— the reader handed out the string while the enum carried the prose, so changing
either alone changed nothing a test could see. It is a record now and a name is
written once.

**Tests and evidence**

- `TestMzCommandTable` 13/13 — every command named, every command this game uses
  named, the three data codes refused as commands, the four that are commands not
  refused, the rule measured at four of eight, and every number the table does
  not hold checked rather than the ones someone thought of.
- Eleven mutations. **The first suite detected four of nine**, all three
  name mutations escaping for the reason above. After the record replaced the
  enum and two tests were added, the name mutations are all seen, and the
  mutation that had nothing to test — adding a command to the owner map, which
  cannot matter because a command is decided before an owner is consulted — was
  replaced by one that can fail.
- Total **866/866**, validator passed, build 0 warnings / 0 errors.
- `grep` for `Execute`, `Run`, `Invoke` and `Eval` in `project/src/mz/`: none.
  A 657 line is held as the text the author wrote and is never run.

**Still not true of MZ**

- **An MZ game does not play.** A command is now named, which is the opposite of
  running it, and this repository will not run a game's script. A 657 line is
  text here and stays text.
- Nothing interprets 111's six comparisons or 121's three modes. Naming a command
  is not doing it.
- MV shares the format and has no fixture; its numbering is the packed one, which
  is exactly what the second trap of K-121 is about.

### K-123 Decide a conditional branch the way the engine does
`READY` → `IN PROGRESS` → `DONE`

**The gap that started this** K-122 named every command, so a branch read 111
with its six parameters and nothing more. Naming a command is the opposite of
doing it, and the first command whose whole effect can be taken from the engine
without running anything is a branch: the engine decides it in one method, and
every number in that method is readable.

**What was built**

- `project/src/mz/MzBranch.cs`: a branch, what it tests, the six ways of
  comparing, and the facts a caller has.
- `project/src/mz/MzBranchEvaluator.cs`: decides a branch from those facts and
  from nothing else. Fourteen kinds, of which nine are decided and the rest say
  what is missing.

**One branch is deliberately not decided.** Kind 12 asks whether a line of the
author's own JavaScript is true and the engine writes `result = !!eval(params[1])`
for it. This repository does not evaluate a game's JavaScript, so that branch is
`ScriptNotRun`, the author's text is kept, and the answer is neither true nor
false. **A mutation that made it answer true was the first thing the suite
checked and it was caught.**

**Three things in the method that were got wrong, each by a reader that had read
it**

1. **The third parameter only says whether the right side is a variable.**
   `params[2] === 0` picks between the number `params[3]` and
   `$gameVariables.value(params[3])`. This reader read `params[2]` as the
   variable, so the game's own branch `[1, 77, 1, 78, 1]` asked about variable
   one where the game asked about variable seventy eight. The test harness then
   made the opposite mistake, so the two hid each other for one run.
2. **Gold has a numbering of its own.** `switch (params[2])` with case 0 at
   least, 1 at most, 2 less — where a variable's case 0 is equal to and case 1 is
   at least. The first three are the same words in a different order. A purchase
   gated on a hundred gold **opens at ninety and shuts at a hundred and ten**,
   and the reader is right about the arithmetic and wrong about the question.
3. **A timer has no number in the parameters.** The second is a threshold in
   seconds and the third is the way, because the branch asks the one timer the
   event owns. This reader asked for a timer called five on a branch about five
   seconds, and refused a branch it could have answered.

**What is not known is not off.** A branch that asks about a switch nobody
supplied comes back `Unknown` and names the switch. A reader that treated the
missing as off would skip a game's content with nothing to show for it, which is
the one failure here that would be invisible, and a mutation of it was caught.

**Tests and evidence**

- `TestMzBranchEvaluator` 11/11 — the game's own three branches decided and none
  refused, each of the six comparisons at its own boundaries, gold under its own
  numbering with the ninety and a hundred and ten case, a stopped timer not
  compared, a script branch reported and not run, a missing thing refused by name,
  and every number that is not one of the fourteen kinds checked rather than the
  ones someone thought of.
- Nine mutations, **the first suite at eight of nine**. The one that got through
  folded a kind the engine has no name for into the nearest kind it does have,
  which is the shape of every mistake this file was prone to. It now checks every
  number from 14 to 657 that is not a kind, and that the refusal names it.
- Total **877/877**, validator passed, build 0 warnings / 0 errors.

**Still not true of MZ**

- **A branch is decided; nothing else is.** 121's three modes, 126's change to
  an item and 126's change to a weapon are not, and a game's flow is a chain of
  commands, not one of them.
- There is no interpreter holding an index into a list, so a branch decides
  something and nothing acts on it yet.
- Still no renderer, no save path, no input, and a 657 line is text.

### K-124 Walk an event list with an index the way the engine moves it
`READY` → `IN PROGRESS` → `DONE`

**The gap that started this** K-123 decided a branch and nothing acted on it,
because there was no index into the list to move. A game's flow is a chain of
commands, and a branch decides one of them and no more. The first thing the
interpreter has to be right about is not what a command does but **where the
index goes after it**, because every other rule in an interpreter hangs off
that.

**What was built**

- `project/src/mz/MzCommandEntry.cs`: one command out of a game's list.
- `project/src/mz/MzOperation.cs`: the four operands, the operation types, and
  a `MzRandom` **held per interpreter** — a static one would be shared between
  two runs of two events and give a game the same numbers twice.
- `project/src/mz/MzCommands.cs`: 121 and 122, and a rule that says which
  commands this reader acts on.
- `project/src/mz/MzControlFlow.cs`: the commands whose whole effect is the
  index, and the three answers they give.
- `project/src/mz/MzInterpreter.cs`: the index, the branch results per indent,
  the step limit, and the four ways a run can end.

**The index rules, each read out of `Game_Interpreter` and not reasoned about**

1. **Every command that returns true is followed by `this._index++`.** A first
   draft added a flag for "the command moved the index itself" and then did not
   step over a command that had, which made an else land on the false arm it had
   just skipped. The flag is gone.
2. **A repeat above is not an exception.** It walks back to the first command at
   its own indent, and the step then moves off that one — so `112`, body, `413`
   goes round properly without any special case.
3. **A command the engine has no method for is stepped over, not refused.**
   `executeCommand` asks `typeof this[methodName] === "function"` and, when it
   is not, still does `this._index++`. **Every one of those commands is one this
   game stores on purpose**: 0 the end of a block, 401 a line of text under a
   101, 412 the end of a branch, and 655 and 657 the two halves of a script.
   Refusing any of them would strand the game on a command the engine itself ran
   past.
4. **A list that ends inside a branch is said, not read past.** The engine's
   `skipBranch` has no test for the end of the list; this reader reports
   `Truncated` and names what is wrong.
5. **The step limit is the engine's `checkFreeze`.** A hundred thousand
   commands in one frame freezes the game in the engine. This reader has no
   frames, so it counts the same way and reports `Frozen`.

**Two findings that came out of the real map, and neither is a test mistake**

1. **This game stores a loop that nothing can leave.** Event 4 is a 112 with
   seven message commands and a 413, and nothing between them tests anything or
   breaks. The engine plays it until `checkFreeze` stops it. The reader reports
   the same thing, and the test says a freeze there is the correct answer rather
   than papering over it.
2. **Random is drawn per variable, not per range.** A first draft claimed one
   draw for a whole range. The engine's `command122` calls `Math.randomInt`
   **inside** `for (let i = startId; i <= endId; i++)`, so three variables get
   three rolls. The test was wrong in the same direction as the first draft and
   was corrected against the source.

**Test evidence**

- 18 tests in `project/tests/core/test_mz_interpreter.cs`, every list in the
  shape the editor writes — most of which were got wrong first, and the file
  says which and how.
- Total **896/896**, validator passed, build 0 warnings / 0 errors.

**Still not true of MZ**

- **Ten commands of a hundred and fourteen have an effect.** 117, 126, 230,
  231, 232, 235, 351 and 357 are read as text. A game's flow is a chain of
  commands, and this walks the chain for eleven of them.
- Still no renderer, no save path, no input, and a 655 or 657 line is text.

**Three rules that the mutation run found untested, and one of them was a claim
the file had been making wrongly**

1. **A repeat above is not a jump.** The engine's `command413` is `do {
   this._index--; } while (currentCommand().indent !== this._indent); return
   true;` — it writes the index and never calls `jumpTo`, so it clears no
   branch result. Only `command119` calls `jumpTo`, and that clears the result
   of every indent it steps over. **The test file had asserted the opposite
   for two cards' worth of work**, on the reasoning that a repeat above is a
   jump. Reading `command413` settled it: it is not, and a reader that treated
   it as one would clear results the engine keeps.
2. **A jump that points backwards at a label is a loop, in the engine as much
   as here.** `jumpTo` sets the index to the label, `executeCommand` steps on,
   and the jump is met again. A first draft of the label test was shaped that
   way and hung the suite for a hundred thousand steps, which is `checkFreeze`
   doing its work. **This game stores no label and no jump at all** — not one
   118 or 119 in the map read here — so only the shape that ends is asserted.
3. **A jump clears the result of an indent it LEAVES, and nothing else.** The
   engine's walk is `if (newIndent !== indent) { this._branch[indent] = null; }`,
   and every earlier test jumped from indent 0 to indent 0, so no test had ever
   gone through that loop. A reader that dropped the clearing entirely passed
   all seventeen. The movement is now claimed directly, both ways: a jump that
   changes indent clears the indent it left, and a jump that stays on one
   indent keeps what was there.

**Two ways a mutation run lies about itself**

The first run reported five escapes. Three of them were the runner's fault and
not the suite's:

1. **An anchor that is not in the file proves nothing.** Three mutations were
   written from a remembered line and reported `NOMATCH`. A mutation that never
   applied is neither caught nor escaped; it is a hole in the run, and counting
   it as "escaped" would have said a rule is untested when in fact the rule was
   never touched. Every anchor in the second run was read out of the file first.
2. **A mutation that lands on the wrong occurrence of a shape passes for a
   reason that has nothing to do with the rule.** `return false;` appears five
   times in `MzCommands.cs`; replacing the first one changes the refusal of a
   script operand, which a test does not look at, so the mutation survived and
   looked like a gap in the arithmetic. **A mutation has to name the place, not
   the shape** — the same rule that emptied this card's test file twice.


### K-125 Run the list a command calls, and stop at a wait
`DONE`

**The gap that started this** K-124 could walk one list. **An MZ event is almost
never one list**: this game stores eight common events in the one map read here
and reaches them with 117, and a 117 whose list is not available is not a command
a reader may step over — the rest of the list behind it never happens. The same
card took the 230 wait, because a wait is the other thing that stops a list
short of its end.

**What was built**

- `project/src/mz/MzEventRunner.cs`: a run over a list and every list it calls,
  with the engine's own answers for three things that are easy to get wrong.
- `MzInterpreter.Wait` and `PassFrame`: a wait holds the index and a caller
  counts the frames down.
- `MzAction.CommonEvent` and `MzAction.Wait`, and `Result.MissingCommonEvent`
  and `Result.WaitingFrames` as **fields rather than prose**.

**Three rules, each read out of `Game_Interpreter`**

1. **A called list runs to its end before the caller moves on.** `updateChild`
   gives the child its own `update()` and the parent breaks the frame while the
   child is still running, so a caller that carried on straight away would run
   its own next command first. The three tests claim the order of the innermost
   list's command before the middle one's and the middle one's before the
   outermost's.
2. **Every list in a run shares one set of facts.** Both go through the one
   `$gameVariables`. A runner that gave each list its own would have a called
   list change something its caller cannot see.
3. **The event id travels with the call, and only on a map.** `isOnCurrentMap()`
   decides, and that is what lets a common event address "this event".

**A wait is a fourth ending, and it is not a failure**

`command230` is `this._waitCount = params[0]`, and `updateWaitCount` takes one
off it per frame and breaks the frame while it is above zero. **The index does
not move**, so the same command is read again the frame after. A reader that
stepped over the wait would run the rest of a list three frames early. There
are no frames here, so the run is handed back `Waiting` with the count, and the
caller decides when the next frame is.

**What this repository will not do, and says so at every place it happens**

The bounded fixture carries **no `CommonEvents.json`** — the real one is 4.5 MB
and was left out on purpose — so every 117 in this game names an index that
cannot be handed over. The engine's own line is `if (commonEvent)`, and a
missing one leaves the index where it was and carries on. **This reader refuses
and names the index instead**, because a silent step-over would run the rest of
a game's list as if the call had never been there. Reading a called list out of
a fixture that does not contain it would mean writing the game's own scripts.

**Measured on the one map in the fixture, not guessed**

Six event pages, and the six end four different ways: three reach a common event
and name the index, one stops at a 230 and says it is waiting, one is refused
because it **opens with fifty-eight lines of the game's own JavaScript** — a
355 and fifty-seven 655, which this repository does not evaluate — and one is a
single 0 and runs through. A first draft of that test guessed three, one, one
and one, and two of the four numbers were wrong.

**A number read out of prose is not a number**

The test took the common event's index out of the message text with an offset,
and got **76 for 476** because it counted a space twice. The index is now a
field, and the test checks the field and the prose against each other.

**Test evidence**

- **16 tests** in `project/tests/core/test_mz_event_runner.cs`; the interpreter
  suite stayed at 18 with the `Waiting` case added to the walk of a real list.
- Total **924/924**, validator passed, build 0 warnings / 0 errors.
**What a mutation run does and does not prove.** Three runs, 14 rule variants
in all. **Fourteen caught** in the end, and the four that survived the first
pass were each looked at rather than counted either way:

1. **`MzInterpreter.Run` did not read `Waiting` as an ending of its own**, so
   the K-124 path could hand back a wait as if the list were done. A real hole.
   Now documented in the code and claimed by the four-way ending.
2. **`CommandLimit` over a whole run, and over the nested path, was untested.**
   `checkFreeze` counts the run and not one list. Three tests now claim it,
   including a pair of lists that call each other.
3. **`MissingCommonEvent` was only readable out of the message**, and the
   message is the one thing that changes shape. A first draft read the number
   out of the prose with an offset and got 76 for 476.
4. **`PassFrame` on a count of zero.** A first run asked `<= 0` against `< 0`
   and it survived, which was a real gap: nothing held a caller that keeps
   passing frames past the end of a wait.
5. **`MaxDepth` was untested**, and a `replace(..., 1)` mutation hid why: the
   line `MissingCommonEvent = index,` stands in two branches, and the mutation
   hit the depth one, which no test reached. It is a field now, with three tests.

   **And then the tests for it were not tight enough either.** A first draft
   asked only *whether* a self-calling list was refused, and it is refused at
   every limit from zero to eight — so all of it passed with a reader that
   refused one level early, one level late, or twice as deep as it should. The
   thing that tells the levels apart is **how many calls the run managed**,
   and that is what is claimed now: a limit of zero records one action, one
   records two, three records four, and a limit of six is not a limit of three.
6. **The map and the event of a child were untested**, and writing those tests
   found **a real fault in the runner**: it passed the *caller's* map down to
   the child, and read the map and the event off the frame rather than off the
   interpreter. `setup` sets `_mapId` from `$gameMap.mapId()` — the map the game
   is on — and `command117` reads `this._eventId` off the calling interpreter.
   Both are now read from where the engine reads them, and `Result.Child` hands
   the caller the child so the three fields can be checked rather than trusted.

   **And a first draft of that test claimed the event id falls away on the
   second level, which the engine does not do.** `setup` takes `eventId || 0`,
   so a chain on the map carries the same event all the way down. Only a list
   that is *not* on the map passes zero, and there it stays zero. Both
   directions are now claimed, because the first draft got the interesting one
   backwards.

**Three mutations that survived are equivalent mutants, and each one changed
the code rather than the test.**

1. The wait case's own `return false` cannot become `return true` and change
   anything, because `ExecuteOne` ends with `return Stopped == MzStep.Stepped`
   and `Wait` has just set `Stopped` to `Waiting`. That is a dead branch, and
   the code now says so where it would otherwise look like a rule with no test.
2. `WaitFrames <= 0` against `< 0`, and `WaitFrames--` against `-= 2`, differ
   only in states nothing can reach: a wait is never set below zero and
   `PassFrame` clamps it. Both are the same in every state a caller can be in.
3. **The one that changed the design.** `Frame` carried the map and the event
   as well as the interpreter, and a mutation showed that reading them off the
   frame and reading them off the interpreter give the same answer in every
   reachable state — the frame's copy always matched. **Two copies of one truth
   is how the event id came back from the dead in a first draft**, so the frame
   now carries only the interpreter and the question has one place to be asked.

A mutation that cannot be caught because it cannot be reached is not a test
gap, and writing a test for an unreachable state would only have named the
unreachable state.

**Still not true of MZ**

- **Thirteen of a hundred and fourteen commands have an effect.** 126, 231, 232,
  235, 351 and 357 are still read as text, and a 355 or 657 line is text.
- A called list that this repository *has* runs; one it does not have is named.
  The 4.5 MB that would supply the eight is deliberately not in the fixture.
- Still no renderer, no save path, no input and no audio.

### K-126 Change what the party is carrying
`DONE`

**The gap that started this** K-121 to K-125 read MZ data and walked event
lists, and neither needed to know what a game **owns**. A 126 does. It is
`Change Items`, this game's map uses it **eighteen times** over fifteen
different items, and it is the next command with a real effect that can be
checked against the game's own `Items.json` — which the fixture carries, 75 KB
of it.

**Built** `project/src/mz/MzParty.cs`, `MzCommandTable.ChangeItems`, the 126
case in `MzCommands`, and `MzBranchFacts.MaxItems`.

**Four rules, each read out of `Game_Party`**

1. **The count is clamped to ninety-nine, not to the number the event asked
   for.** `container[item.id] = newNumber.clamp(0, this.maxItems(item))` and
   `maxItems` is `return 99` — no argument, no per-item case. **Five of this
   game's eighteen commands ask for 999.** An implementation that added the
   number as written would hand a player a thousand of something the engine
   refuses to hold.
2. **A count that lands on zero is deleted**, not stored as a zero:
   `if (container[item.id] === 0) { delete container[item.id]; }`. A reader
   that kept a zero would answer `hasItem` differently the moment a game asked.
3. **Losing more than there is clamps to zero**, because the clamp is from
   below as well as above. Taking four of one is none, not minus three — and
   adding three back then gives three, which is what the engine's clamp makes
   true.
4. **An id with no item behind it changes nothing and says so.**
   `itemContainer` returns null and `gainItem` returns early, so the engine
   steps over it. This reader says it did not happen, because a game asking
   for an item this repository cannot hand over would otherwise look like a
   game that had it and used it.

**And one that is easy to get wrong in the other direction.** `operateValue`
asks the operand's **kind** first — `operandType === 0 ? operand :
$gameVariables.value(operand)` — and only reads the game for a variable
operand. A first draft read the variable either way, which made every one of
this game's seventeen literal amounts depend on whatever a variable held. And
`operation === 0 ? value : -value` has **no third case**: an operation of
seven removes, exactly as an operation of one does.

**The clamp is invisible in the middle of the range.** A test that only ever
added four to an empty bag would pass with no clamp at all. Every rule here is
asked about at its boundary, and the default is claimed to be the engine's
ninety-nine rather than a number chosen here.

**Measured on the one map in the fixture, not guessed** Eighteen 126s, fifteen
items, five above ninety-nine. **A first draft got nine** — it counted what a
walk reached, and one of the two pages stops at a 230, so the counts are two
different claims: one about the game's data, one about what a reader with
frames sees. Both are now claimed, and the difference between them is the
test.

**Test evidence** 12 tests in `project/tests/core/test_mz_party.cs`; the
interpreter's own suite is unchanged at 18. Total **924/924**, validator
passed, build 0 warnings / 0 errors.

**Mutations** Seventeen rules over two runs. The first run caught seven of
eleven, and **all four that escaped were one gap in one place**: every test
called `GainItem` directly, so nothing proved the interpreter passes the
right four numbers. Writing the test for the wiring found the two faults above
and killed all six rules in the second run, 6 of 6 caught.

**The wiring between the interpreter and the party was untested, and writing
that test found two real faults.**

1. **The party was built without the ids.** `new MzParty(pFacts)` knew no
   items, so every 126 was answered from a list the interpreter could not see
   and every count came back zero — **silently**, with nothing saying why. The
   ids now travel in `MzBranchFacts.KnownItems`, and a facts that carries none
   means the game has not been read.
2. **An empty set of known ids was read as "everything exists".** That is the
   opposite of what it means, and it would have handed a player 999 of an item
   the game never had while looking as if it worked. **Nothing known is nothing
   allowed**, and the code says so.

**Three mistakes of my own, recorded because the next one will make them too.**
A first draft of the wiring test drove the interpreter by hand, and a fresh
`MzInterpreter` has `Stopped` at whatever it starts as rather than at
`Stepped` — so `ExecuteOne` answered false on the very first command and the
loop gave up before it had run anything. It also wrote `new(2, ...)` where the
code belongs: **126 is the command, not the item**, and a page of codes 2, 3
and 4 is a list the engine steps over. And it read `party.Notices` on a party
it had made itself while the interpreter builds its own over the same facts,
so the notice was on the action and not where the test was looking.

**A known gap this card found and now names.** A lone `MzInterpreter` knows no
common events at all, so `HasEffect` is false for 117 and one is **stepped
over like a 0**. That silent step-over is exactly what K-125 was written to
refuse, and it is still reachable through this door. The runner is the door
that names a missing call, and both answers are claimed side by side rather
than one of them being quietly assumed.

**Still not true of MZ** Twelve of a hundred and fourteen commands have an
effect. 231, 232, 235, 351 and 357 are still read as text. No renderer, no
save path, no input, no audio.

## Agent maintenance rules
- Do not create hundreds of speculative cards for distant phases. Expand the next 1–2 milestones in detail and keep later phases coarse.
- At the end of a work session update this board and `SESSION_STATE.md` with exactly what is next.


### K-127 Put a picture on the screen and move it off again
`DONE` — pictures, P2, no dependencies

**What it is.** K-121 to K-126 read MZ data, walked event lists, changed what
the party carries. **This is the first command in this game that needs
something other than numbers to have an effect**: nine of them on the one map
in the fixture, on images 1, 86 and 87 — three show a picture, four move one,
two erase one. A reader with no place to put a picture has nothing to say
about them.

**Not 127 and not 128.** Those are Change Weapons and Change Armors, and this
game's `Map002` has **none of them** — no 127, no 128, no 129, no 130. So
carrying them would have meant writing rules no data in this repository can
check, and the fixture has no `Weapons.json` and no `Armors.json` to check
them against. The pictures were chosen because the data is here.

**The rules, each read out of rmmz_objects.js 1.9.1 rather than inferred**

1. **A shown picture is a new object.** `showPicture` makes
   `new Game_Picture()` and puts it in the slot, so a tint, a rotation and any
   movement are gone with the old one. A reader that changed the existing
   picture in place would keep what the engine has just discarded.
2. **A picture id is routed through `realPictureId`, which is not the
   identity.** In a battle a map picture and a battle picture share the
   editor's number. **This game's `System.json` sets `picturesUpperLimit` to
   110**, not the hundred `maxPictures` falls back on, and a reader using the
   hundred would put a battle picture on top of a map one at the wrong offset.
3. **The fourth parameter says where the fifth and sixth are read from.**
   `picturePoint` reads them as numbers when it is zero and out of variables
   when it is not, and a reader that read them as numbers either way would
   place a variable-positioned picture at the variable's own number.
4. **A move sets a target, not a value.** `updateMove` only moves while
   `_duration > 0`, so **a move of zero frames changes nothing at all** and asks
   for no wait even when the game asked for one.
5. **A move on an empty slot does nothing**, and is recorded rather than
   dropped — a game that moves a picture it never showed has a reason a log
   should hold.
6. **Only a move that asks to wait holds the list up.** `if (params[11]) {
   this.wait(params[10]); }` and there is no second one. This game asks for
   the wait on **two of its four** moves and not on the other two.

**A real fault this card found in reading, not in testing.**
`MzCommandEntry.From` handled a Number and took `item.Text` for everything
else, so a JSON **boolean** became the empty string. A 232 carries its wait in
the eleventh slot as a real `true`/`false`, and this game's four moves came
back as four that never ask to wait. No test had noticed, because no test had
read a boolean out of an event list. It is a lost value in a file this
repository claims to read, and it is fixed in the reader rather than worked
around in the test.

**A second one, of my own.** `if (params[11])` is a truth value, and a first
draft called `int.Parse` on it — which throws on the empty string a game may
leave in that slot. Reading it the way the engine reads it is now its own
named method.

**A third, and the worst of the three: a waiting move never arrived.**
`ExecuteOne` did not step the index when a command left the interpreter in
`Waiting`, and a `MovePicture` that asked to wait did `return false`, which
means the same thing. So the next frame read the same 232 again, set the same
twenty frames again, and **a picture that had to move across the screen
waited for ever and never got there.**

The engine has none of this trouble: `command232` ends in `return true`
whatever it asked for, and the wait it set lives in `_waitCount` where the
next command cannot reach it. The index moves and the run stops in two
separate steps now, which is what the engine's frame does — the command is
done, the frame is not. `MzInterpreter` runs 18 and `MzEventRunner` 16 tests
and both are unchanged after it, so this was a fault in a rule nothing had
exercised rather than a change to a rule something had.

**And a fourth, of my own again.** A test that claims a picture is at
`2000, 2000` because the scale is `2000, 2000` is reading the wrong line. The
event says `1, "UI/Status_HelpCollision", 0, 0, 0, 0, 2000, 2000, 255, 0`:
**a place of nothing and a picture two thousand times its own size**, and a
reader that put the scale into the place would have shown it off the bottom
left of the screen. The claim was corrected to the measured value, not
adjusted until it passed.

**A test that counted is not a test that ran.** The first draft's ninth test
was called "every picture command in this game runs" and it counted: three
shows, four moves, two erases, read out of the file without an interpreter in
sight. Four mutation rules escaped because of it. The test that replaced it
builds an interpreter, hands it the frames the two waiting moves ask for,
and checks what the screen holds when the list is through — and it is the
test that found the waiting-move fault.

**And an equivalent mutant that was not equivalent at all, twice.** Removing
`pInterpreter.Wait(frames)` entirely passed the suite, because the first draft
of the walk-through drove the screen's frames from the test's own loop — so a
reader that never waited still moved the picture and ended in the same place.
**It is equivalent for the picture and wrong for the page:** without the wait
the four commands after the move run in the same frame, and a game that fades
a picture out over twenty frames would run the rest of the event while it is
still at full opacity. The test that killed it claims frames and not an end
state: the interpreter is held for exactly the movement's length, the index is
already past the move, the command after it has not run, and it is released
when the frames are counted off.

**A fifth mistake of my own, in the same test.** A 122 written as four
parameters — `1, 0, 0, 5` — has nowhere to read a value from, because
`command122` is `startId, endId, operationType, operandType, operand` and
**the operand is the fifth**. The page was not held by the move failing; it
was held by a command that could not do what the test meant.

**Two more test gaps, found the same way.** A move that does *not* ask to
wait had no test of its own, so replacing the wait condition with `true`
passed — the rule was only ever checked from the side where it says yes. And
`if (params[11])` had no test with a parameter the game wrote as something
other than a boolean, so reading it as `written != ""` passed too. **A rule
checked from one side is half a rule**, and both halves are now tests of their
own: one that the page runs on in the same frame and the picture still moves,
and one that `true` and `1` ask while `false`, `""`, `0` and `no` do not.

**Test evidence** 11 tests in `project/tests/core/test_mz_screen.cs`.
**Total 935/935**, validator passed, build 0 errors.

**Mutations** Twenty-two rules over four runs, and the shape of the escape is
the same one this repository keeps meeting: **every rule that survived was a
rule no test had asked about from the side it fails on.** Run one caught 7 of
11. Run two caught 3 of 7. Run three caught 2 of 4. Run four is the full set
on the finished suite. The three escapes in run two were the waiting-move
fault, the boolean parameter and a direct-call gap; each one turned out to be
a real fault in the reader or in the index, not a weak test.

**What is deliberately not here.** A picture is a name, a place and some
numbers; it is not a texture, and nothing here loads one. The blend mode and
the scale are kept as the numbers the game wrote rather than resolved to a
rendering, because a reader with no renderer must not pretend to have one. The
easing is stored and not applied: `PassFrame` lands the last frame exactly on
the target, as the engine's easing is built to do, and does not walk the
straight line in between — which is stated rather than faked. 233 (rotate),
234 (tint), 236 (weather) and 224 (fade) are the next pictures and are not
here.


### K-128 Measure what this game actually needs from MZ before modelling more of it
`DONE` — measurement, P1, no dependencies

**Why this card exists.** K-127 asked which picture commands come next, and the
answer was: **none of them.** This map uses no 224, no 233, no 234 and no 236.
Modelling them would have been rules no data in this repository can check —
the mistake K-127 already refused to make once.

**So what is left in the one map that is here?** All twenty-two codes in it
are real MZ 1.9.1 commands, measured against the 114 `commandNNN` methods in
`rmmz_objects.js`. Every one of them is now either modelled or refused:

| Code | What it is | State |
|---:|---|---|
| 0, 401, 412, 655, 657 | steps over, as the engine does | K-124 |
| 101, 111, 112, 113, 117, 121, 122, 413, 601-603 | text, branches, control flow, waits | K-123 to K-126 |
| 126, 230, 231, 232, 235 | party, wait, pictures | K-125 to K-127 |
| **351** | **Open Menu** | **the next one** |
| **355** | **Script** | refused, and stays refused |
| **357** | **Plugin Command** | refused, and has to be |

**The finding, and it is about this game rather than about MZ.**
This game ships **52 plugins and all 52 are enabled.** Its eleven `357`
commands call `ItemCombinationMZ`, `DTextPicture` and `HyoujouSelect`, and
their parameters carry the plugins' own options. `command357` is
`PluginManager.callCommand(this, pluginName, params[1], params[3])` — so a
`357` in this game is nine times a request to run somebody else's JavaScript.

**That is refused, permanently and for the same reason `355` is.** Not
because a plugin call is harder, but because executing foreign JavaScript is
the one thing this repository does not do. A reader that implemented `357`
faithfully would be the thing the security contract forbids, and it would
have been faithful to this game and useless to everyone else.

**What a reader can honestly say about a `357`.** The plugin's name, the
command name inside it, the author's own description, and the parameters as
data — all four are readable without running a line of it. What the plugin
*does* is not answerable, and is not guessed. The same shape as `355`: the
text is kept, the running is declined, and the decline is structured rather
than a silent step over, because a step over would make a game look as if it
worked.

**The two commands, and they mean opposite things.**

`351` is run. The engine's `command351` is `if (!$gameParty.inBattle()) {
SceneManager.push(Scene_Menu); Window_MenuCommand.initCommandPosition(); }
return true;` — **one condition, and it returns true either way.** A menu in
a battle is not this command with another scene, it is nothing at all, and a
reader that stopped the run there would leave the commands after it unrun in a
way the engine never does. `MzMenuState` exists so a 351 is not
indistinguishable from a command with no effect: a reader with no screen still
has to be able to answer "did the game open a menu here", and without somewhere
to write the answer down it could only be silent.

`357` is refused, and **the refusal is structured rather than a step over.**
All nine of this map's 357 commands are answered, each naming the plugin and
the command inside it, and each landing on `MzBranchFacts.Notices` where a
caller looking for what went wrong will find it. A silent step would leave a
game that looks as if it works while its crafting menu and its floating text
never appear.

**Test evidence** 4 tests in
`project/tests/core/test_mz_menu_and_plugins.cs`. **Total 939/939**, validator
passed, build 0 errors. The expectations were all measured out of the game's
own files before they were written — nine plugin commands, three plugins, two
351s, three scripts — so no number in this card is a shape this card chose.

**Mutations** Nine rules, **nine caught**, and one of them had to be written
twice: the first attempt replaced a fragment inside an escaped string and left
the file unparseable, so it came back `BROKE` — which counts as caught and
proves nothing. The second attempt replaced the whole notice with a constant
that still compiles, and it failed three named tests, one for each of the
three plugins this map calls. **A mutation that does not compile is not
evidence**, and this project has now been bitten by that three times.

**And the honest limit this puts on the card.** A game with 52 plugins can
have its own logic in them: `ItemCombinationMZ` is a crafting system, and
this game's `355` scripts read `$gameVariables.value(180)` to work out what
was crafted. **UniversalRPG will run this game's MZ event code and none of
its plugin code**, and no bounded slice can change that. What a card can do is
say so where a caller will see it, once, with the numbers, instead of leaving
a reader to discover it by playing.


### K-129 A second MZ fixture, from a game with no plugins
`DONE` — fixture, P1, depends on K-121

**Why a second fixture was needed.** The first, `mz/` (*Stranded with You*),
carries **52 enabled plugins** and **nine plugin commands** on its one map. A
reader checked against it is mostly checked on its refusals, and barely at all
on the event code. That is not a fault in the reader — it is what that game
is. It needs a second game to be a statement about MZ.

**The game.** `CamelliaCoronation-Win`, in `E:/RPGMakerGames`, a free MZ game
put there by the user to work with. Engine **RPG Maker MZ 1.9.1**, measured and
not assumed: both games' `rmmz_objects.js` carry **the same 114
`commandNNN` methods**, with none only in one or only in the other.

**One plugin, and it is in no command.** `extra_party_member`, enabled, with an
empty parameter list. **No `355` and no `357` on any of the nineteen maps** —
counted over the files, and that negative claim is the reason the fixture
exists. A reader that refused nothing would run this game completely, and
there would be nothing to hide.

**What it measures, all of it counted rather than quoted:**

- **2 432 Befehle** over nineteen maps, **1 772 of them run today** and **660
  not**. Every one of the 660 is a real MZ command, not a plugin call.
- The most-used is **401, the line of text, at 938**. Then **101, the dialogue
  block, at 414**, then **505, the move route, at 348**. A first draft called
  the move route the most-used, on the grounds that a game is "mostly made of"
  it, and was wrong by two places.
- **Fifteen variables, numbered 0 to 15, and none above.** **Eight items.**
  One class, one animation, **no switches, no common-event calls, no actor
  references.** A reader that has read this fixture has read everything this
  game refers to — and the first fixture says the opposite, so between them
  they say how far a bounded slice can honestly go.
- **`CommonEvents.json` is 376 bytes and present.** The first fixture had none
  because the original was 4,5 MB, and the runner had to refuse a 117 by
  naming a common event it could not read. That was honest for a gap. **Here
  there is no gap**, and a rule only ever tested against a gap is a rule never
  tested.

**And the codes that are MZ's own and not a plugin call.** 0, 401, 404, 405,
412 and 505 have no `commandNNN` method and are not plugins: the block end, the
line of text, the end of processing, the choice, the end of a branch, the move
route. The engine reads them by position, not by dispatch, and this reader
models them for the same reason. **"It is a number MZ knows" is not the same as
"MZ does it"**, and a 357 shows up in a list of known numbers only because MZ
reserves a slot for plugins.

**The fixture is 537 KB over thirty files**, with no `js/`, no executable, no
image, no audio and no `Tilesets.json` — the reader loads no texture, so a
texture in a fixture is a claim about something nothing reads. **`Skills.json`
is the one file that is not the original**: 104 525 bytes become 1 181, because
**no command on any of the nineteen maps references a skill and the reader
reads none.** Everything else is bytewise identical and the SHA-256 values are
in `project/tests/fixtures/mz_plain/MZ_PLAIN_FIXTURES.md`.

**Test evidence** 6 tests in `project/tests/core/test_mz_plain_fixture.cs`.
**Total 945/945**, validator passed, build 0 errors.

**Mutations** Ten rules, **ten caught, first run, none escaped** — and the way
they were written is the point. **These are claims about data, so the data was
mutated and not the reader**: a 505 turned into a branch, a 401 into a choice, a
101 into something else, a 357 appended to a map, a 355 appended to a map, the
common event list emptied, the common event file deleted, and three rules
against the test's own arithmetic. Every one fell.

**That is the first card in four where nothing escaped**, and the reason is
that the previous three escaped a rule no test had asked from the side it
fails on. A claim about a number is only as good as the test that notices when
the number changes, and a fixture's claim is a claim about a number.

**What this card is for, in one line.** The first fixture says what a reader
must not do; this one says what it can. **1772 of 2432 already run**, and the
660 that do not are the map of the work that is left — led by 505 at 348, 205
at 96, 123 at 42, 213 at 36 and 405 at 36.



### K-131 Walk a character, one step a frame
`DONE` — runtime, P1, depends on K-130

**Why this one, and what it corrected.** K-129's list called `505` the
biggest thing left, at 348. **`command505` does not exist.** `505` is a
nested move-route entry that the editor writes, and a move route reaches the
runtime through **`205 Move Route` — 96 of them**, over fourteen character
ids, of which **60 say `wait` and 36 do not**. The 348 were never event
commands at all.

**The seventeen route codes this game uses, measured over its own
ninety-six routes:** END 96, MOVE_LEFT 74, MOVE_RIGHT 59, MOVE_DOWN 50,
MOVE_UP 45, JUMP 31, CHANGE_SPEED 26, TURN_UP 11, MOVE_BACKWARD 10,
TURN_DOWN 10, TURN_RIGHT 9, TURN_LEFT 7, MOVE_FORWARD 4, WAIT 4,
TRANSPARENT_ON 4, STEP_ANIME_ON 2, STEP_ANIME_OFF 2. **MOVE_LEFT leads and
MOVE_DOWN follows** — the opposite of what "a game mostly walks about"
would guess. MOVE_RANDOM, MOVE_TOWARD, MOVE_AWAY and all eight diagonal
codes appear **zero** times and are named rather than guessed at.

**Six rules, and every one of them is a place a first reading goes wrong:**

1. **The API is `isMapPassable` and `canPass`, not `isPassable` and
   `checkPassage`.** Those two names come from other RPG Maker engines;
   `checkPassage` has **zero** occurrences in 1.9.1. A reader built from
   memory would have compiled and tested nothing real.
2. **MZ has two coordinates.** `_x`/`_y` is the tile, `_realX`/`_realY` is
   where the character is drawn, and on a successful step the drawing
   position is set to **one tile behind** —
   `this._realX = $gameMap.xWithDirection(this._x, this.reverseDir(d))`. A
   reader with one coordinate snaps, and a snapped character teleports.
3. **`reverseDir` is `10 - d`, not `(d + 4) % 4`.** The first is right for a
   0..3 numbering and wrong for MZ's 2/4/6/8: `reverseDir(2) = 8` is **Up**,
   not Down. A first draft placed every "one tile behind" position **in
   front** of the character, so every character walked away from where it
   was going.
4. **`isStopping` is `!isMoving() && !isJumping()` — two terms, and a first
   draft added a third.** It wrote `... && !Waiting` and **every route with a
   `ROUTE_WAIT` in it ran backwards**, re-issuing one step for ever. A
   character waiting is a character that has arrived.
5. **A refused step still turns.** `moveStraight` turns in both branches, and
   on failure it calls `checkEventTriggerTouchFront`. A character that bumps
   a wall faces the wall, and that facing is what triggers the action
   button.
6. **A route is a queue of single steps, not a batch.**
   `updateRoutineMove` hands a command over only when the character has
   arrived, so **five steps into open floor is five frames**. A reader that
   ran the list in one call would teleport the character five tiles.

**And a product fault with a wider reach than this card.** A 205's second
parameter is a **nested object**, and `MzCommandEntry.From` turns every
parameter into a string — anything that is not a number or a boolean became
`item.Text`, which for an object is `""`. **Every move route in every game
came back empty, and the reader could not have said why.** `MzJson.Write`
now writes a value back out, because **a reader that cannot write a shape
back has already half-lost it.**

**Two more faults, found by the same tests:** `Truth` read a boolean out of
`Text` where the parser puts it in `Boolean`, so **all three flags of all
ninety-six routes came back false** and not one page was ever held by its
route; and `From` read a route's `code` out of `Text`, which is empty for a
number, so **every route code came back 0 — which is END** and all
ninety-six routes did nothing while looking perfectly plausible.

**Test evidence** 7 tests in `project/tests/core/test_mz_move_route.cs`.
**Total 957/957**, validator passed, build 0 errors.

**Mutations** Fourteen rules, thirteen caught in the main run. The one the
run reported as escaped — "a route that is not forced hands out no steps" —
**was not escaped**: an isolated second run killed it, three of seven tests
down, with the tree bytewise unchanged. It is recorded here as
**fourteen of fourteen**, because a number that was not checked is not a
number that was counted.

### K-130 Send the player somewhere, and hold the page until they arrive
`DONE` — runtime, P1, depends on K-124

**Why this one and not the biggest.** The 660 commands that do not run yet
are led by `505` at 348 and `205` at 96, and both are movement — both need
`Game_Character`, a move route decoder and a passability model, which is
three cards before the first of them can be tested. **201 is 33 commands and
needs none of that**: it changes where the player is, not how they got there,
and it is on sixteen of the nineteen maps.

**And it is the first command in this reader that is neither a change nor a
number of frames.** K-125 made a run wait for frames, K-127 for a picture's
movement, and a 201 for **a condition**:

```
Game_Interpreter.prototype.command201 = function(params) {
    if ($gameParty.inBattle() || $gameMessage.isBusy()) { return false; }
    …
    $gamePlayer.reserveTransfer(mapId, x, y, params[4], params[5]);
    this.setWaitMode("transfer");
    return true;
};
```

and `updateWaitMode` answers `waiting = $gamePlayer.isTransferring()`. **A
condition wait has no length** — a caller passing frames cannot end it, and a
reader that counted them would let the page on with the player still on the
old map. `MzWaitMode` is a third shape next to a 230's frames and a 232's
movement, and the engine's own modes are `message`, `transfer`, `scroll`,
`route` and `until`.

**Four rules, each a place a first reading goes wrong:**

1. **A transfer is reserved, not carried out.** `reserveTransfer` writes
   `_transferring = true` and the new map and position and **changes nothing
   the player can see**; `performTransfer` is what moves them. Applying it
   while reading the command would move the player before the commands after
   it had run — the difference between a game that leads the player and one
   that teleports them mid-sentence.
2. **The engine returns false and transfers nobody** in a battle or with a
   message on the screen. That is neither a wait nor a finish: the index
   stays, and the transfer happens in the frame in which the message closes.
   `MzStep.Refused` says exactly that and is not dressed up as either of the
   other two.
3. **The direction is set on the way, not on the reservation**, because
   `performTransfer` is what calls `setDirection`. A player that turned one
   frame early would face a map they are not on yet.
4. **A map this reader has not read is named and the player stays put.**
   `command201` does not check and `$gameMap.setup` fails further on where
   nobody is looking. **Half-applying it is worse than not moving** — the
   caller would see a position and no file behind it.

**And the numbers are this game's: 33 transfers over sixteen maps, all with
the first parameter at zero**, so the place is written out rather than read
from a variable. A reader that always looked in the variables would send
every player in this game to variable four.

**A C# trap this card walked into and measured.** `$"Map{i:03}.json"` with
`i = 1` produces **`Map13.json`**. In an interpolated string `i:03` is read as
a fill character of `0` and a **precision** of `3`, and a whole number with a
precision is padded on the **right**: 1 becomes "13", 2 becomes "23". Every
file was missing and the only thing that said so was the reader's own error
about a file ending mid-value. `ToString("000")` is the right spelling, and the
reason is in the test so the next card does not walk into it again.

**Test evidence** 5 tests in `project/tests/core/test_mz_player_transfer.cs`.
**Total 950/950**, validator passed, build 0 errors.

**Mutations** Nine rules, **nine caught, first run, none escaped.** Each of the
four rules above was broken in the place it actually fails: a reservation that
moves the player, a turn that happens one frame early, a message that no longer
refuses, a run that carries on past a refusal, a condition wait counted down in
frames, a transfer that holds its page for twenty of them, a condition that
never stops being met, a missing map that is carried out anyway, and a place
always read from the variables.

**What is not here.** No map is loaded and no tile is drawn: a transfer is a
position, not a picture of one, and the direction and fade type are kept as
the numbers the game wrote. The other four wait modes need a scrolling map, a
moving character and a plugin callback, and none of them is modellable here.


### K-132 Read a line of text, and every code in it
`DONE` — runtime, P1, depends on K-129

**The biggest thing left in this fixture: 938 lines, and every one carries
exactly one parameter.** But nothing about it is a rendering detail, and
that is the finding: **a line of dialogue is mostly not words.**

**Two passes, two rule sets.** Pass one, `convertEscapeCharacters`, rewrites
in three steps — every backslash becomes the escape character; **two escape
characters put one backslash back**; and the variable, actor, party and
currency codes are filled in, **the variable one in a loop**. Pass two, the
drawing loop, treats **every character below 0x20 as a control character**
and never puts it in the output. A reader that does them in one shows a
different line than the game does.

**Three classes, and only one of them is text:**

- **In the text:** `\V[n]`, `\N[n]`, `\P[n]`, `\G`.
- **Not in the text, and never shown:** `\|`, `^`, `!`, `>`, `<`, `$`.
  **A reader that emitted them would put a `|` in the middle of a
  sentence.**
- **Neither text nor pen, and this reader names them:** `\C[n]`, `\I[n]`,
  `\PX[n]`, `\PY[n]`, `\FS[n]`, `\{`, `\}`. **A reader with no
  renderer cannot draw them, and it says so rather than dropping them in
  silence.**

**This game's own numbers, measured over the files — and two of my own
measurements were wrong before they were right.**

| | zuerst behauptet | gemessen |
|---|---:|---:|
| Zeilen mit `\C[n]` | 0 | **19** |
| Undrawable insgesamt | 0 | **57** |
| Leere Zeilen | — | **14** |
| Code-Klassen | 2 | **5** |

`\C[3]` 19×, `\C[0]` 19×, `\I[177]` 19×, `\!` 3×, `\|` 3× — **19 Zeilen
mal drei Codes, das sind die 57.** Eine Zeile wartet **dreimal**: `\|.|\|.|\|.`
sind drei Entscheidungen und nicht eine.

**Der Fehler, der zweimal passierte.** Ein Scan dieser Zeilen fand den
Buchstaben `C` 53-mal, `N` 38-mal, `V` 22-mal und `P` 11-mal, und eine erste
Lesart hielt sie für Auszeichnungen. **Es sind Wörter**: „SEND **C**OUT!!",
„\* **N** om\*", „Valuable **V**egetables". **Ein Code ist zuerst ein
Backslash und dann ein Buchstabe** — wer nach einem nackten Großbuchstaben
sucht, liest Englisch. **Genau dieser Fehler ließ mich zuerst „keine Farben"
behaupten, und die echten Dateien sagten neunzehn.** Der Test, der es
bemerkte, las dieselben Dateien und riet nicht.

**Was nicht hierher gehört.** Eine Zeile wird **gelesen und behalten**, nicht
gezeichnet: `MzBranchFacts.Message` hält Wortlaut, Wartungszahl, und alles,
was dieser Leser nicht zeichnen kann. **Kein Textfenster, kein Renderer.**

**Und eine Aussage, die älter ist als diese Karte.** Ein Test aus K-124
behauptete, ein 401 werde *übergangen*, weil die Engine keine Methode dafür
hat — und das stimmt und stimmt weiter. **Der Leser liest es trotzdem**, weil
er nach einer anderen Frage gefragt wird: *was hat das Spiel geschrieben?*
**„Hat die Engine eine Methode" und „was steht in den Daten" sind zwei
Fragen mit zwei Antworten**, und sie zu vermischen bringt entweder ein
laufendes Spiel zum Stehen oder behauptet, ein Spiel habe keinen Text.

**Test evidence** 6 tests in `project/tests/core/test_mz_message.cs`, and one
K-124 test rewritten to say both answers.
**Total 963/963**, validator passed, build 0 errors.

**Mutations** Nine rules. The first run caught seven and reported two
escaped — **and both were a fault in the rules, not in the reader.** One
mutated a code's handling into an equivalent that changed nothing, and one
mutated a list entry that the test did not actually reach. Isolated and
rewritten, **nine of nine**. The second is the better story:

> **Die Liste der Zahlen ohne `commandNNN` war geraten, und sie war falsch.**
> Sie behauptete, `601`, `602` und `603` hätten keine Methode. **Sie haben
> eine** — `command601`, `command602` und `command603` sind drei der 114.
> Und sie behauptete „178 reservierte Nummern", wo die Liste in K-122 in
> Wahrheit **die 114 Methoden** ist. **Neun Zahlen haben keine Methode, und
> alle neun liegen außerhalb dieser 114** — `0`, `401`, `404`, `405`, `412`,
> `505`, `604`, `605`, `657`. Der Test sagt es jetzt ausdrücklich.

**Das ist der vierte Name in vier Karten, der aus dem Gedächtnis kam und in
der Engine nicht existierte** — nach `checkPassage`, `isPassable` und der
`reverseDir`-Form. **Gemessen wird, nicht erinnert.**

### K-133 A 101, and everything it swallows
`DONE` — runtime, P1, depends on K-132

**The first command in this reader that eats other commands.** And that one
fact reorganises K-132: `command101` is

```
if ($gameMessage.isBusy()) { return false; }
$gameMessage.setFaceImage(params[0], params[1]);
$gameMessage.setBackground(params[2]);
$gameMessage.setPositionType(params[3]);
$gameMessage.setSpeakerName(params[4]);
while (this.nextEventCode() === 401) { this._index++; add(…); }
switch (this.nextEventCode()) {
    case 102: this._index++; this.setupChoices(…); break;
    case 103: this._index++; this.setupNumInput(…); break;
    case 104: this._index++; this.setupItemChoice(…); break;
}
this.setWaitMode("message");
return true;
```

**So a line of dialogue is never dispatched.** There is no `command401` to
dispatch it to — `nextEventCode()` looks one ahead and the 101 steps the index
over each line itself. **Every one of this game's 938 lines belongs to a 101
and to nothing else**, and a reader that ran a 401 as a command of its own
would be running 938 commands the engine never runs.

**This game's numbers, measured over the files:** 414 dialogues, one to four
lines each — **118 with one, 130 with two, 104 with three, 62 with four** —
and the total is exactly 938. **Eight are followed by a 102**, six under a
one-line dialogue and two under a two-line one; there is no 103, no 104, no
403 anywhere in nineteen maps. Commands eaten: **112, 134, 106, 62** — 1360
rather than 414 + 938, because the eight choices are inside it.

**Three rules, and a fourth that is only visible in this game.** A dialogue
that is already up is refused — and `isBusy()` is **text or choice or number
or item**, so a 101 behind an unanswered choice is refused as firmly as one
behind a line. Exactly **one** of 102, 103 and 104 is taken, and it is the one
directly after the last line: the `switch` runs once, so a 102 that is not
right there is reached later as a command of its own. **And it always ends in
a wait**, `setWaitMode` being outside the `switch`, so a dialogue with no
choice holds its page all the same.

**268 of the 414 name somebody and 146 name nobody** — Camellia 32 times,
Mary 27, and `???` 45 times, which is the editor's placeholder for a person
not yet named. All 414 have five parameters. **Not one asks for a face**, so
this game has a name box that is filled in and no portrait beside it.

**`102` is not `405`, and that is the fifth name in five cards that had to be
measured.** `ShowChoices` has meant 405 since K-132 — the choices as data —
and the follower was compared against it, so **not one of this game's eight
choices was ever found**: 1352 commands instead of 1360, and a dialogue that
ended on a choice the engine would have taken. **Four runs**, because the
tests that failed were the ones checking a sum.

**`params[0]` is an array, not a bar-separated string.** A first draft wrote
`params[0].split("|")` — the shape an older RPG Maker used — and would have
read one option that reads `["Yes", "No"]`, brackets and comma included, and
compared the cancel number against the wrong length. **And
`cancelType = params[1] < choices.length ? params[1] : -2`**: a cancel number
that is not below the number of choices becomes "no cancel". This game's eight
are all two options with a cancel of 0 or 1, **so the rule never fires in the
real data** — which is why it had to be built by hand.

**`params[1] || 2` is 2, and a written zero is 2 as well** — 0 is falsy in
JavaScript. A 104 with no category and a 104 with `0` both get the whole
party, and a reader that defaulted to 0 would offer the player nothing.

**And the index moves by what was eaten, not by one.** `command101` steps the
index once per line and once for the 102 its switch took, and then
`executeCommand`'s own `this._index++` steps it once more — so a 101 that is
the last thing in a list leaves the index **one past the end**, and no other
command in this reader can, because every other one moves it by one.

**The off-by-one that cost the most.** Three times, in three different files,
an index that was one out was blamed on the nearest thing rather than
measured. The first draft's `nextEventCode(pCommands, i)` with `i` already one
past the 101 **started the read at the second line** — 524 lines instead of
938. The test helper's `k += eaten` was then "fixed" to step one further, on
the strength of a distribution that was one bucket out, and **the numbers got
worse** — 88 and 102 where the files say 118 and 130. **The fault was never
in the test.**

**And a guard with no test.** `ExecuteOne` had a bounds check that a mutation
switched off and every test passed, because `IsRunning` is
`Index < _commands.Count` and the guard was **never asked**. The repair was
not a test for it but **its removal** — the case is handled one level up, in
`Run`, which now checks before it enters its loop and says where the index was.
**A second check that can never fire is a claim a reader will believe and
nobody can prove.**

**Test evidence** 9 tests in `project/tests/core/test_mz_dialogue.cs`, plus
three rewritten in K-124's and K-132's files.
**Total 972/972**, validator passed, build 0 errors.
**Mutations** Twelve rules over four runs. Every escaped rule turned out to be
either a broken rule or a test that could not reach the thing it mutated; two
of them found real product faults — the 102 read from the wrong command, and
a 103/104 read as a list of options.
