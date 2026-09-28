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
| K-022 | 1 | DONE | Implement map/player movement and passability simulation | K-020 |
| K-023 | 2 | DONE | Replace placeholder interpreter opcodes with verified RM2K/2003 command codes | K-021 |
| K-024 | 2 | DONE | Move Godot project into `project/` and keep runtime/tooling at repo root | — |
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
| K-050 | 2 | DONE | Original-format read-only LSD save model and safe save directory integration | K-020 |
| K-051 | 1 | DONE | Add deterministic RM2K Timer 1/Timer 2 conditions | K-045 |
| K-052 | 1 | DONE | Add bounded JSON simulation save/load roundtrip | K-020 |
| K-053 | 1 | DONE | Adaptive application render FPS without changing simulation Hz | K-036 |
| K-054 | 1 | DONE | Add capability-gated RM2K save/debug tool contracts | K-052 |
| K-055 | 1 | DONE | Add bounded runtime-owned RM2K JSON save-directory slots | K-052 |
| K-060 | 2 | DONE | Game compatibility profile schema versioning/validation | K-002 |
| K-061 | 2 | DONE | Compatibility report export for GitHub issues | K-060 |
| K-070 | 3 | DONE | Faithful-vs-Enhanced profile and integer scaling controls | K-030 |
| K-071 | 3 | DONE | Controller/touch remapping layer | K-020 |
| K-072 | — | DONE | Reusable RM2K host lifecycle | — |
| K-073 | — | DONE | Synchronize runtime sprite descriptors after movement | — |
| K-074 | — | DONE | Fail-closed pending transfer parameters | — |
| K-075 | — | DONE | Transfer facing direction validation | — |
| K-076 | — | DONE | Clear confirmed choice presentation state | — |
| K-077 | — | DONE | Preserve pending InputNumber state across variable conflicts | — |
| K-078 | — | DONE | Implement bounded RM2K ChangeItems command | — |
| K-079 | — | DONE | Implement bounded RM2K ChangePartyMembers command | — |
| K-080 | 4 | BACKLOG | RGSS architecture spike after RM2K/2003 playable milestone | RM2K playable milestone |
| K-081 | 0 | DONE | Decode real LMU event pages: fix struct-array field collection and verify liblcf IDs | K-013 |
| K-082 | 0 | DONE | Align event-page trigger ids with liblcf and fail closed on undecodable pages | K-081 |
| K-083 | 0 | DONE | Correct ControlSwitches/ControlVariables parameter layout to the verified EasyRPG spec | K-081 |
| K-084 | 1 | DONE | Implement verified actor-stat, screen-effect, and event-control interpreter commands | K-023 |
| K-085 | 2 | DONE | Bring RPG Maker MV to data-directory and System.json metadata parity with MZ | K-017 |
| K-086 | 1 | DONE | Decode verified RM2K chipset passability arrays from the LDB chipset section | K-015 |
| K-087 | 2 | DONE | Add verified RM2K autotile animation ticking (counter values blocked: no verified data source) | K-015 |
| K-088 | 2 | DONE | Apply verified RM2K tile substitution tables (source: liblcf SaveMapInfo, not LMT) | K-086 |
| K-089 | 2 | DONE | Decode RM2K per-map terrain tags via verified `Game_Map::GetChipId` substitution | K-015 |
| K-090 | 4 | BACKLOG | MV/MZ JavaScript runtime architecture spike | RM2K playable milestone |
| K-091 | 2 | DONE | Apply verified `Game_Map::IsCounter` action-trigger propagation across up to 3 counter tiles | K-015 |
| K-092 | 2 | DONE | Drive movement and event triggers from player input in the RM2K runtime | K-015 |
| K-093 | 3 | DONE | Route the Godot host input through the verified turn order instead of ad-hoc triggers | K-092 |
| K-094 | 0 | DONE | Vehicles for the action-event order | — |
| K-095 | 3 | DONE | Resolve verified chipset source rectangles for blocks C, E and F | K-087 |
| K-096 | 3 | DONE | Build the verified block D autotile quarter table and block geometry | K-095 |
| K-097 | 3 | DONE | Build the verified block A/B autotile composition from `BlockA_Subtiles_IDS` | K-096 |
| K-098 | 3 | DONE | Decode the indexed RM2K chipset bitmap and blit the resolved rectangles | K-097 |
| K-099 | 3 | DONE | Compose a full map frame from chipset tiles, map layers and the z-order rule | K-098 |
| K-100 | 5 | BACKLOG | PE/DLL inspector research and safe metadata-only parser | Stable primary runtimes |
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
| K-112 | — | DONE | RGSS archive format, shared by XP, VX and VX Ace | — |
| K-113 | — | DONE | Ruby Marshal reader for the RPG Maker data files | — |
| K-114 | — | VERIFY | RM2K vehicles: state, boarding, sprites and the airship shadow | — |
| K-115 | — | DONE | Ruby lexer for the RGSS engines | — |
| K-116 | — | DONE | Ruby parser for the RGSS engines | — |
| K-117 | — | DONE | The value layer between a game's data and its language | — |
| K-118 | — | DONE | Name what every child of a tree is for | — |
| K-119 | — | DONE | Read a whole number wider than this machine holds | — |
| K-120 | — | DONE | Read the data three real games actually wrote | — |
| K-121 | — | DONE | Read the data an RPG Maker MZ game wrote | — |
| K-122 | — | DONE | Name every command an RPG Maker MZ game stores | — |
| K-123 | — | DONE | Decide a conditional branch the way the engine does | — |
| K-124 | — | DONE | Walk an event list with an index the way the engine moves it | — |
| K-125 | — | DONE | Run the list a command calls, and stop at a wait | — |
| K-126 | — | DONE | Change what the party is carrying | — |
| K-127 | — | DONE | Put a picture on the screen and move it off again | — |
| K-128 | — | DONE | Measure what this game actually needs from MZ before modelling more of it | — |
| K-129 | — | DONE | A second MZ fixture, from a game with no plugins | — |
| K-130 | — | DONE | Send the player somewhere, and hold the page until they arrive | — |
| K-131 | — | DONE | Walk a character, one step a frame | — |
| K-132 | — | DONE | Read a line of text, and every code in it | — |
| K-133 | — | DONE | A 101, and everything it swallows | — |
| K-134 | 1 | DONE | The twenty-five table rows that have no card behind them | — |
| K-136 | 0 | READY | The eighty-nine commands liblcf names and this interpreter does not dispatch | — |

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
`DONE` — runtime, P0, unblocked K-114

**The card's title was half the diagnosis.** `Rm2kPlayerTurn.Apply` carried the
comment *"This runtime has no vehicles, so nothing can be toggled and the action
event check always runs"* — and `Rm2kDecisionTurn.Run` sat next to it,
implemented, mutation checked, and **never called**. The vehicles were loaded and
drawn; they were never driven and never boarded. `GameSimulationState` had
**zero** vehicle wiring and the runtime kept its own `_vehicles` list.

**Implemented**
- `GameSimulationState.Vehicles` and `.Boarding`, both cleared in `Reset()`
- `Rm2kPlayerTurn.Apply` calls `Rm2kDecisionTurn.Run`, and a vehicle that takes
  the turn suppresses the action event check — a boat moored beside a sign has
  to be boardable, and the sign is on the tile the player faces
- `CanEmbark` / `CanDisembark` from the passability mask, `IsVehicleStopping`
  for the airship, `OppositeBit` for the way back

**Three real product faults the suite found**

**`TileInFront` spoke the wrong direction order.** The player speaks 2/4/6/8;
`DirectionDelta` expects 0–3. **A `8` yields `(0, 0)`** — the character's own
tile. Every boarding test "passed" without anything moving, and a player facing
up was handed a disembark onto the water they were standing on. The bridge
`LiblcfFromFacingDirection` already existed, and its own comment warns that
mixing the two silently turns a right step into a left one.

**`PassDown` is `0x01` and `PassUp` is `0x08`.** A first draft had them swapped
and wrote `0x08` for "down".

**A K-114 test held the wrong order in place.** It checked `TileInFront` with
0/1/2/3, and so agreed with itself: five assertions, every one consistent with
the same misreading.

**And a fixture that lied about itself.** `SetPassability(..., pAllowUp,
pAllowDown)` was named as walkable directions and wired `pAllowUp` to
`PassDown` — the opposite. Two tests then asserted the wrong polarity and failed
against correct code. **A fixture whose names lie about its own bits is worse
than no fixture**, because the failure points at the reader.

**Test evidence** 8 tests in
`project/tests/core/test_rm2k_vehicle_decision_turn.cs`, 1 rewritten in
`test_rm2k_vehicle_boarding.cs`.
**980/980**, `TestRm2kVehicleDecisionTurn: 8/8`, `TestRm2kVehicleBoarding: 11/11`.
**Mutations** Ten rules over six runs, **9 of 10 caught**. The tenth is a harness
fault, not a semantic gap: the first runner used `$TMPDIR/m_<path>` as its
backup, which fails on the `/`, so the mutations ran **without a restore** and
the following rules tested a cumulatively broken file. `git checkout --` then
discarded the **unstaged** slice; it was rebuilt and staged immediately.

**What this does not claim:** a vehicle's own move route, hero-directed vehicle
movement, and vehicle background music. K-114 lists those.

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

### K-134 The twenty-five table rows that have no card behind them
`READY` — board, P1

**This board was lying, and the way it lied was measurable.**

Twenty-five rows in the table have no detail section, and forty-seven numbers
between K-001 and K-133 were never used. Thirty detail sections had no row.
Two rows appeared twice. **An agent reading only the table — which is what
`AGENTS.md` points at first — would have seen the work stop at K-111 and had
no way to know that the RGSS archive, the Marshal reader, the Ruby lexer, the
parser, the value layer, two MZ fixtures and the whole command-execution line
existed.**

**The table is now rebuilt from the details**, so every card that has a detail
section has a row. That is the half that can be repaired from evidence.

**This card is the other half.** The twenty-five rows without a detail section
name work that was done:

| | |
|---|---|
| K-020 | Faithful RM2K/2003 simulation state model |
| K-033 | Visible RM2K map and sprite overlay in the runtime UI |
| K-034 | Safe keyboard movement handoff to RM2K simulation |
| K-035 | Keyboard message dismissal, choice navigation, numeric input |
| K-036 | Deterministic runtime simulation frame count from the virtual clock |
| K-037 | Clickable message, choice and numeric-input presentation controls |
| K-038 | Avoid per-frame choice-control reconstruction in the runtime UI |
| K-039 | Explicit runtime stop control, hide stale presentation controls |
| K-042 | RM2K event-page selection and bounded trigger scheduler |
| K-043 | LMU event-command vectors feeding the native scheduler |
| K-044 | Dispatch action and touch events from player input and movement |
| K-045 | LMU event-page switch and variable conditions |
| K-046 | Selector evaluation for switch B and variable comparisons |
| K-047 | Diagnose unsupported RM2K commands without execution |
| K-048 | Separate LMU move-route and event-command presence metadata |
| K-049 | Bounded RM2K item and actor page conditions |
| K-051 | Deterministic RM2K Timer 1 / Timer 2 conditions |
| K-052 | Bounded JSON simulation save and load roundtrip |
| K-053 | Adaptive application render FPS without changing simulation Hz |
| K-054 | Capability-gated RM2K save and debug tool contracts |
| K-060 | Game compatibility profile schema versioning and validation |
| K-061 | Compatibility report export for GitHub issues |
| K-070 | Faithful-vs-Enhanced profile and integer scaling controls |
| K-080 | RGSS architecture spike after the RM2K/2003 playable milestone |
| K-090 | MV/MZ JavaScript runtime architecture spike |

**Acceptance criteria**

- Each of the twenty-four `DONE` cards gets a detail section carrying **what
  was built, the test evidence, and the commit**. **No section is written
  from the title alone** — a title is a claim and this file does not carry
  claims.
- A card whose work cannot be evidenced from `git log` and the test suite is
  moved to `VERIFY`, not `DONE`, and says what is missing.
- K-080 and K-090 keep `BACKLOG`: both are behind the RM2K playable
  milestone, and both need a decision about JavaScript that is not this
  repository's to make quietly.
- The table and the details are checked against each other by the same
  measurement that found this: **every row has a section, every section has a
  row, and no row is duplicated.**

**Why this card exists rather than a paragraph in the board note**

Because the next agent will read the table. **A board note explaining that
the table is incomplete is a warning; a table that is complete is a fix.**

## The measurement, re-run after the repair

| | |
|---|---|
| Board rows | 113 |
| Distinct rows | 113 |
| Rows without a detail section | **0** |
| Sections without a row | **0** |
| Rows appearing twice | **0** |

**Twenty-three numbers between K-001 and K-136 are used by neither the table nor a
section** — K-005 to K-009, K-025 to K-029, K-056 to K-059, K-062 to K-069 and
K-135. **Those are numbers that were never allocated**, and the fix is not to
invent sections for them: a section for a card that was never written is a claim,
and this file does not carry claims. The numbering has gaps and the gaps are
visible, which is the difference between a hole and a lie.

**And one card was missing from the table entirely.** K-136 — the eighty-nine
commands liblcf names and the interpreter does not dispatch — had a full detail
section, a `READY` state and **no row at all.** It is the only P0 card in this
repository that a reader of the table could not have seen, and it is why this
card existed: the table was not short by twenty-five rows, it was short by one
that mattered more than all of them.

**Evidence, not titles.** Every section above names the commit that introduced the
file holding that work — found with `git log --follow --diff-filter=A`, because
the move of the Godot project into `project/` rewrote every path and a plain
`git log` returns the move, not the work. **The test numbers come from a run of
the suite on 2026-09-28 — `All 1353 tests passed` — and not from what a card
claimed when it was written.**

**Two cards keep `BACKLOG` and are not evidence of nothing.** K-080 and K-090 are
behind the RM2K playable milestone, and both need a decision this repository does
not get to make quietly: whether Ruby is executed and whether JavaScript is
executed at all. **The Ruby work that exists is a lexer, a parser and a value
layer; the MZ work reads two real games and runs their command lists. Neither is
a runtime, and neither claims to be.**



### K-136 The eighty-nine commands liblcf names and this interpreter does not dispatch
`READY` — runtime, P0

**Measured by comparing liblcf's `Code` enumeration against the interpreter's
own constant list, value by value — 164 codes, 43 dispatched, 121 without.**
After the Maniac and EasyRPG patch codes are set aside, **89 real RPG commands
have no case in this reader.** They are not refused one by one; they fall into
the default branch and are reported as "Unsupported RM2K command NNNN skipped".

**Re-measured on 2026-09-28, and the number was 89 and not 43 — it was written
before thirty cards landed.** The interpreter's constant list holds 94 numbers,
of which **89 are wired into the dispatch** and five are bounds rather than
codes: `MaxScriptRecursion`, `MaxWaitFrames`, `MaxItemId` and `MaxItemCount`,
and one real command. **So the gap this card describes is not eighty-nine
dispatched commands; it is a family-by-family list, and the list below is
where the next island of that shape is.**

**And the one real command in that five is exactly the shape this card is
about.** `MovePicture` (11120) had a constant, a summary and no `case` — so it
was in the constant list, which is what a reader looks at, and not in the
dispatch, which is what a command has to reach. **A reader that checks its own
constant list for a gap will never find this one.** That is now done; the
measurement is in the section below.

**And that default is the same fault K-094 was written for.** `ShowPicture` and
`ErasePicture` were *implemented in `PresentationState`, bounded, and tested* —
and no command could reach either, so a game's picture command did nothing and
the suite was green. The two commands this card has just wired, `11110` and
`11130`, were the clearest instance of the class. **The list below is where to
look for the next island of that shape.**

| area | codes | what it needs |
|---|---:|---|
| screen effects | ~~`11010` `11020` `11030`~~ | **~~erase, show and tint — DONE, see below~~** |
| screen effects | `11060` | **Pan Screen — done: four modes, a clamped speed and a rounded wait** |
| audio | `11560` | Play Movie — the only audio command left of the six |
| ~~actor state~~ | ~~`10430`–`10490`~~ | **~~parameters, HP, SP, full heal, skills, equipment and conditions — all DONE, see below~~** |
| battle | ~~`10500` `10710`~~ | **~~simulated attack and the encounter — DONE, see below~~** |
| movement | ~~`11310` `11330`~~ | **~~visibility and move event — DONE, see below~~** |
| movement | `11340` `11350` | **Proceed With Movement and Halt All Movement — done: one flag and one map-wide call** |
| ~~shop and inn~~ | ~~`10720` `10730` `20710`–`20732`~~ | **~~open shop, show inn and the ten battle/shop/inn handlers — DONE, see below~~** |
| memory | ~~`10820` `11530` `11540`~~ | **~~memorize location, memorize and play BGM — DONE~~** |
| memory | `10830` `10910` | **Recall To Location is in liblcf and has no method in this EasyRPG build; Store Terrain ID likewise** |
| memory | `10920` | **Store Event ID — DONE; the note that it had no method was wrong, see below** |
| teleport | ~~`11810` `11820` `11830`~~ | **~~teleport targets, teleport access, escape target — DONE, see below~~** |
| outcome | ~~`12420` `12510`~~ | **~~game over, return to title — DONE~~** |
| system | ~~`10660` `10670` `10680` `10690`~~ | **~~system BGM, SFX, graphics, transitions — DONE, see below~~** |
| heroes | ~~`10620` `10630` `10640`~~ | **~~hero title, sprite, face — DONE~~** |
| vehicles | ~~`10650` `10850`~~ | **~~vehicle graphic and location — DONE, see below~~** |
| map | ~~`11710` `11720` `11740` `11750`~~ | **~~tileset, panorama, encounter steps, tile substitution — DONE, see below~~** |
| choice | ~~`20140` `20141`~~ | **~~choice option and choice end — DONE, see below~~** |
| damage | ~~`10500`~~ | **~~simulated attack — DONE, see below~~** |
| class data | ~~`0x1F` chunk~~ | **~~class parameters by level — DONE, the prerequisite for `1008`~~** |
| wolf | ~~variable branch~~ | **~~seven comparisons and two arms — DONE, see below~~** |
| wolf | ~~variable bands~~ | **~~self, normal, system, database — DONE, see below~~** |
| wolf | ~~variable operators~~ | **~~fourteen assignment operators — DONE, see below~~** |
| wolf | ~~band offsets and switches~~ | **~~the real offsets, the database, map and common switches — DONE, see below~~** |
| wolf | ~~move route execution~~ | **~~24 verified route types, finally run — DONE, see below~~** |
| wolf | ~~character board and VM routing~~ | **~~a board that moves in time, and two opcodes — DONE, see below~~** |
| wolf | ~~chip passability~~ | **~~six states, two layers, and walls a figure cannot walk through — DONE, see below~~** |
| wolf | ~~character collision~~ | **~~half-tile hitboxes, pass-through, and a hero who is not a wall — DONE, see below~~** |
| wolf | ~~target numbers and approach~~ | **~~-1 to -7, five companions, and two approach steps finally run — DONE, see below~~** |
| wolf | ~~character sheets and animation~~ | **~~the direction order, the walk cycle, and the animation clock — DONE, see below~~** |
| wolf | ~~audio~~ | **~~three channels, the zero volume rule, and the delay that is not a fade — DONE, see below~~** |
| wolf | ~~move routes from a file~~ | **~~the two opcodes the VM ran and the reader never produced — DONE, see below~~** |
| wolf | ~~common events~~ | **~~a call that comes back, the depth limit, and the wait that makes a route visible — DONE, see below~~** |
| wolf | ~~map event calls~~ | **~~two kinds of call by one number, self variables per call, and a missing event ignored — DONE, see below~~** |
| teleport access | `11810`–`11840` | targets and the two access flags |
| saves | ~~`11910` `11930`~~ | **~~open save menu, change save access — DONE, see below~~** |
| menues | ~~`11950` `11960`~~ | **~~open main menu, change access — DONE, see below~~** |
| flow | `12420` `12510` | game over, return to title |
| labels | ~~`12110` `12120`~~ | **~~label and jump-to-label — DONE, see below~~** |
| vehicles | ~~`10840` `10850` `10650`~~ | **~~enter/exit vehicle, set vehicle location, change vehicle graphic — DONE, see below~~** |
| face and title | `10130` `10120` `10620` `10640` | message options, face graphic, hero title, actor face |
| ~~battle monsters~~ | ~~`13110` `13120` `13130` `13150` `13210`~~ | **~~change monster HP/MP/condition, show hidden monster, change battle BG — DONE, see below~~** |
| ~~battle branches~~ | ~~`13310` `23311` `13410` `23310`~~ | **~~the battle-only branch, terminate battle and else/end — DONE, see below~~** |
| misc | `1005`–`1008` `10920` | common event, flee, combo, class |
| ~~misc~~ | ~~`10430` `10460` `10470`~~ | **~~actor parameters, HP, SP — DONE, see below~~** |
| ~~access~~ | ~~`11840` `11930` `11960`~~ | **~~escape, save, main menu access — DONE, see below~~** |
| ~~misc~~ | ~~`10120` `10130` `10230`~~ | **~~message options, face graphic, timer — DONE, see below~~** |

## The character sheets, the walk cycle, and one test I could not explain

**WOLF had no presentation at all** — twenty five files and not one of them drew
anything. The material specification gives what a character sheet has to be.

**The four directions are down, left, right, up, top to bottom, and that is not the
compass order.** The material guide gives it twice, and a reader that used up,
right, down, left would show every character turned ninety degrees — the kind of
bug a player sees in the first second and never reports.

**The walk cycle is B → A → B → C → B, and the middle cell appears twice.** The
guide names the cells A, B and C from the left, so B is column 1. A reader that
played them in order would show a figure stepping forward three times and then
snapping back, and a walk cycle that does not return to its middle pose looks
like a hiccup.

**The idle cycle runs the other way — 2, 3, 2, 1 — and the T and TX forms add the
idle cells to the left**, so a standing figure sits at a smaller column than a
walking one. A reader that added the offset the other way would put the standing
pose in the middle of the walk: a figure that never stops walking and never
appears to stand.

**The animation frequency is frames per step, and the order is the opposite of the
speed.** The help writes アニメ頻度[早0-6遅] — often to rarely — while the move
speed is slow to fast. A reader that divided by the frequency, or that used the
speed, would make a figure whose feet blur also cross the map in a blur. **Zero is
every frame and not never**, because the help puts 0 at the fast end.

**Three of the eleven tests found real defects.** A facing step reported `Stepped`,
the same answer as a movement — so a guard that turned in place was drawn with the
walk cycle for as long as its route ran, which is a pose the artist never drew.
`Turned` is now its own outcome. **The idle offset was applied twice**, putting a
walking figure one column too far right. **And `IsWalking` was set on every step**
rather than on a movement.

**Two of the tests were wrong about the rules**, and both are recorded here: one
expected the walking cell at column 1, which is A — step 0 of the cycle is B, and
B after the idle is column 2. One expected a diagonal on a four direction sheet to
be clamped to the nearest cardinal, which is the failure this whole card avoids.

## Sixteen measurements found the file innocent, because the file was innocent

**`IdleCell` threw `DivideByZeroException` on every call, and `pIndex % 4` cannot
divide by zero.** The test failed for seventeen turns of work and every measurement
cleared the source: no division anywhere in the file, the constant reads 3, a test
that touches only the constant passes, a call with a literal 3 passes, `WalkPattern`
— which has the same shape and parentheses — has always been correct, renaming the
suite and the method moved the name in the report and nothing else, and deleting
`obj`, `bin` and `.godot/mono` changed nothing.

**The only thing that found it was compiling the method on its own and watching it
throw.** In a separate project, outside Godot, with nothing but the file and a
`Console.WriteLine`, the exception appeared immediately — and the line it pointed at
was the `_ => 0,` arm of a switch whose selector was `pIndex % 4`.

**The parentheses around the modulo are not decoration.** `WalkPattern` writes
`return (pIndex % 4) switch`; `IdleCell` wrote `return pIndex % 4 switch`. **With the
parentheses the same file returns 1, 2, 1, 0; without them it throws on every
argument.** It is in the mutation list as a rule of its own, so the parentheses are
now proven to matter rather than believed to.

**The lesson is in the sequence, and it is the part worth keeping:** every one of the
sixteen measurements asked the source whether the source was wrong, and a file that
cannot answer a question about itself cannot be cleared by reading it either. **The
seventeenth measurement changed what was being asked** — not "is the source wrong"
but "does it work outside the thing that reported it" — and that is the question
that had an answer.

**Test evidence** `test_wolf_character_sheet.cs` (11) and
`test_wolf_move_route_runner.cs` (14), both re-measured after the parentheses.
**1313/1313**, validator passed.
**Mutations** Sixteen rules over two runs, **16 of 16 caught** — including the
parentheses around the modulo, the direction order, the cycle starting on the first
cell instead of the middle, the idle cycle run the same way as the walk, the offset
applied twice, the standing cell, the animation countdown never charging, and a
turn reported as a step.

## The help's target numbers, and two steps refused for two cards

**The two approach steps were refused for two cards**, with the honest reason:
approaching an event needs a second figure and approaching a position needs the
map, and the board had neither. The last two cards gave it both, and the refusal
was no longer true — **a reader that kept it would have a game whose guards never
approach anything.**

**The target numbers are the help's, and they are not event ids.** The list reads:
`0以上の場合 ＝ その値のIDを持つイベント`, `-1＝このイベント`, `-2＝主人公(隊列先頭)`, and
`-3` to `-7` for the five companions. **Zero is an event id and the hero is minus
two** — a reader that used zero for the hero would answer a command about event 0
with the player, and a command about the player with event 0, and both of those
exist in a real map.

**"This event" means the route's own event, and the number travels with the route.**
The runner has no notion of which program it runs in, and the board is what knows
which event started a route, so the owner is part of the route's state.

**The party has five slots, and an empty one is nobody.** The list stops at -7, so
a reader that grew a list would answer -8 with a sixth companion the editor cannot
name. An empty slot is null and not a fresh figure — a guard that approached an
invented companion would walk to nobody.

## The arrival and the refusal are the same thing in a bool, and must not be

**`ApproachOne` first returned a bool where false meant both "already arrived"
and "no such target",** and the caller could not tell them apart. A guard that had
reached its target was recorded as having chased somebody who is not there, and its
route ended in a refusal rather than an arrival. **Four answers, because each pair
differs**: stepped takes time, arrived takes none, no target and blocked both stop
the route unless it says to skip.

**And the collision rule and the approach rule meet in one place.** A guard one
tile short of its target, blocked by the target itself, **has arrived** — reading
it as blocked would have a guard give up the moment it caught the player, which is
the moment the game is about. Reading it as arrived always would have a guard
pressed against a wall stop one tile short and call it done. **The difference is
what refused the step**, and the board asks.

**One tile per step, and the target is re-read every step.** A guard approaching a
moving hero has to keep closing the distance, and a reader that computed the whole
path once would walk to where the hero was.

**The larger gap goes first, and a tie closes X.** That is what makes a diagonal
read as a diagonal. **The help does not name the order, so it is stated as a
choice** — a reader that closed both axes at once would produce a step the format
has no type for.

## Two bugs the compiler said nothing about, and two tests that were wrong

**The companion range was written "at least -3 and at most -7", which is empty.**
The help counts down, so the bounds have to be read the other way round, and a
range that is always false is perfectly valid C#. It appeared twice — in
`Classify` and in `CompanionNumber` — and **the compiler flagged the first as an
unreachable arm and said nothing about the second.**

**Three of the ten new tests were wrong, and two of them were wrong about the
rules rather than about the code.** One compared "three" with "four minus one" to
decide which axis closes first; the gaps are four on Y and three on X and the
larger absolute value is what the rule looks at. One put the hero in the party
but not on the board, and measured a guard walking *through* the player — the
party and the board are separate things and a test has to set both. One expected
the route to finish when the guard arrived, having read the arrival as the end of
the walk; it is not, because the repeat flag restarts the step.

**Test evidence** `test_wolf_approach.cs` (10), with the four WOLF files from the
last two cards re-measured. **1302/1302**.
**Mutations** Fifteen effective rules over two runs, **15 of 15 caught** —
including -1 and -2 swapped, the companion range in the wrong order, the arrival
folded into the refusal, the larger gap reversed, the tie going to Y, the blocked
step read as arrived, the target coordinate not resolved through the bands, and
the figure in the way not detected.

## A figure walked through every guard, and one rule was read backwards

**The last card gave figures walls and nothing else,** so a hero walked through
every guard, every shopkeeper and every sign in the game. The only trace would
have been a hero standing inside a shop, and a walk through a guard is a walk.

**The hitbox is a tile wide and half a tile high,** and the help gives the number:
<c>当ﾀﾘ判定■(正方形)</c> off is <c>横1マス×縦0.5マス</c> — a figure's feet and not
its whole body — and the square option makes it a full tile. **A reader that used
one tile for both would make every half-height figure collide with the figure on
the tile above, and a crowd in a corridor would lock solid.** That is a game
that cannot be finished, and it looks like a bug in the pathfinding rather than
in the hitbox.

**X is half open and Y is closed, and that asymmetry is a decision, not a typo.**
X half open is what keeps a corridor walkable: a figure on tile 2 reaches from 2
to 3 and one on tile 3 from 3 to 4, and a closed comparison would have them
overlap on the boundary and block every two-tile room. Y closed is the other
half — **a square figure on tile 5 occupies 5 to 6 and touches the figure on
tile 6, and a solid object another figure may stand inside is not solid.** It is
also what makes the square option mean anything: with a half open Y a square
figure would reach exactly as far as a half-height one and the option would be a
name for nothing. **The help does not spell the comparison out, so this is stated
here as a choice with its reasons.**

**A ghost is walked through, and the relationship is one sided.** The option
<c>イベントをすり抜けられるようにします</c> makes an event walk-through, and the
help adds that such an event cannot start unless the player is standing on it —
so a transparent sign is both a wall you walk through and a thing you can only
reach by stepping on it. **The flag belongs to the ghost and answers before
anybody is asked**, so a ghost walks through a solid figure and a solid figure
walks through a ghost. A reader that made it symmetric would wall off every
invisible trigger in the game.

## A test read that rule backwards, and the code was right

**One of the new tests asserted a *refusal* when the hero stepped onto a ghost**
— it read the rule as though the ghost stopped whoever walked into it. The code
did the opposite and was correct: a reader that believed the test would have made
a transparent decoration a wall, which is the opposite of what the option is for.

**It is recorded because the test passed on the first run of the shape and failed
only when the test above it started working.** Four of the eleven tests were
measuring the wrong thing for a reason that had nothing to do with collision, and
the symptom in every one of them was the same word: `False`.

## The hero was not on the board until this card

**The occupant list started empty and the first call a game makes is LoadMap** —
which hands the grid to whoever is on the list. An empty list meant the hero never
got the map, **so the hero could not step at all**, and a game with an event on it
would have opened with a player who is stuck. The hero is placed in the
constructor and the grid is handed in `RefreshOccupants`, not only where a figure
is added, because a figure placed before the map was loaded has the same question
as one placed after.

**The candidate carries every field, through `At()`.** Building a throwaway
character by hand would mean copying fifteen fields and missing one the next time
a field is added, and the field that matters most — the hitbox — is exactly the one
a hand-built copy would forget.

**Test evidence** `test_wolf_character_collision.cs` (11), with
`test_wolf_passability.cs` (10), `test_wolf_character_board.cs` (14) and
`test_wolf_move_route_runner.cs` (14) re-measured after the change. **1292/1292**.
**Mutations** Sixteen effective rules over two runs, **16 of 16 caught** —
including the half tile read as a full one, the square option ignored, the Y axis
made half open, the X axis made closed, the erased check on one side only, the
pass-through made symmetric, the self-collision not skipped, the candidate losing
the hitbox, the hero absent from the board, and the list not rebuilt.

## Six chip states, two layers, and a figure that stops at a wall

**The last card made figures move, and they moved through walls** — because the
board had a map id and a width and nothing else. There was no passability anywhere
in the WOLF reader: the map data carried tiles, and tiles are pictures.

**Six states and not two.** The editor's tileset window cycles them
`○ → × → ▲ → ★ → □ → ○`, and the tileset help gives the meaning of each: passable,
not passable, passable with the figure hidden behind, passable and always drawn over,
passable with half transparent feet, and the sixth:

**↓ takes the layer below's answer, and is passable where there is no layer.**
The help says <c>下のレイヤーに合わせます。下のレイヤーがない場合は通行可能です</c>.
**A reader that refused the tile instead would freeze the hero on the floor** — a
floor tile with nothing under it is the most ordinary tile in a map. And a reader
with a boolean loses ▲, ★, □ and ↓, of which only × blocks: the other three add a
drawing rule and not an obstacle, and reading "hidden behind" as impassable would
put a guard outside a staircase railing.

**Two layers, because ↓ asks one of them.** The upper layer answers, and the lower
only where the upper asks: a ★ chip over water is passable, because ★ says passable.
A reader that let the lower layer decide everything would make a signpost over a
wall unusable.

**A seventh state is refused,** and not treated as passable. A reader that fell
through to "not ×, therefore passable" would walk a figure onto a chip the game had
never heard of, and the symptom would be a hero through a wall nobody had drawn.

## A figure with no map at all cannot step, and that is the point

**"The map was not read" is not "the tile is passable."** A character with no grid
refuses every step, and a reader that treated a missing map as open ground would let
a guard walk through every wall on every map whose chips it could not read — with no
error anywhere, because a walk through a wall is a walk.

**The facing still turns, because the refusal is about the position and not the
facing.** The same rule a wall produces, and the same rule the last card found the
test contradicting.

**The board hands the map to every figure when it is loaded,** and not only to the
ones placed after it. A figure placed before the map was loaded has the same question
as one placed after, and a reader that handed the grid at placement time would leave
the earlier figures walking through walls.

**The map's size is the grid's and not a field beside it.** A board with a width of
20 and a grid of 10 would let a figure walk to tile 15 and read a row that does not
exist, and a reader that answered "passable" out there would put a guard in the void.

**Nine of the existing route tests failed when this landed,** and that is the change
working: since the map became a refusal rather than an absence, every test that
measured a moving step had to give its figure a map. Three of them were measuring
the refusal and called it a route.

**One of the new tests was wrong about where the wall was,** and it is recorded here
because it is the same mistake as the timing card: it put the wall at three and called
the first step onto three "open". **A test that measures a refusal twice without ever
seeing a figure walk proves nothing about walking.**

**Test evidence** `test_wolf_passability.cs` (10), with `test_wolf_character_board.cs`
(14) and `test_wolf_move_route_runner.cs` (14) re-measured after the change.
**1281/1281**.
**Mutations** Sixteen effective rules over three runs, **16 of 16 caught** —
including the arrow chip not asking the lower layer, no lower layer being refused,
each of ▲, ★ and □ read as impassable, the unknown state treated as passable, the
grid answering from the lower layer instead of the upper, the map check removed,
and the board handing the grid to the hero only.

## The board, the timing, and a bug that ran every step in every second frame

**The last card built a runner that can be called. That is not a game.** The
route reader produced steps, the type table verified them, and nothing held a
figure to run them on — the VM had no characters and no map. This card adds the
board, the two opcodes, and the clock.

**The board is the VM's own, over the VM's own variable bands.** A route step
that stores to a variable has to write where the event can read it, and two
band sets would mean a patrol counting steps into a store nobody looks at.

## The timing was wrong three times, and the order of two lines is why

**First: the index was checked before the frame budget.** A step advances the
index when it runs, so the index points at the *next* step — and a step that had
advanced past the last one was already "at the end" on the next frame. The route
ended, the repeat flag reset the index to zero, **and the fifteen frames the step
had asked for were thrown away with it.** The step then ran every second frame,
and a guard at speed 1 crossed the screen eight times too fast. The symptom the
test reported was a figure at X = 9 after seventeen frames, and every function
involved measured correct in isolation.

**Second: checking the frames first fixed the order and ended every step a frame
late,** because the tick that spent the last frame returned instead of looking at
what came next.

**Third, and the one in the code: a flag and not a frames-left test.** A
`FramesLeft` of zero is ambiguous — it means either "still on the last frame" or
"no step has started" — and both readings were in the code. `IsStepRunning` makes
the arithmetic exact: a step of n frames is started by one of them, so it is
finished on the nth tick, and the tick that finishes it also looks at the next
step. **A step of sixteen frames is stored as fifteen, because the tick that
starts it is its first frame.**

**The two refusals are decided before the timing, and that order matters too.**
Asking the timing of a step that did not run is a question with no answer, and a
reader that asked first gave a refused step a frame budget — a guard would stand
at a wall for sixteen frames and then stop. Skipping means carrying on in the
same frame, not falling through to the timing.

**The refusals themselves are two answers, and the second card got it wrong.**
The runner threw `Step`'s return value away and reported a move into a wall as
`Stepped`, so the board's skip flag had nothing to act on. A refused movement is
now `Refused` and a refused one is not the same as a step that did not move.

**The ten key facing table is still not implemented, and the guess is gone.** The
help points at a figure that is not in the text; an earlier draft of the previous
card guessed the table and it had duplicate values. **That card is the argument for
reading first and guessing never** — the band offsets cost a card the same way.

## The two waits share one state and need two endings

**The frame wait counts down; the route wait ends when the board says the movement
is finished.** They use the same `Waiting` state, so a flag tells them apart — and
**the bug that flag exists to prevent is that without it the VM resumes on the
move route command itself and starts the route over, forever.** The frames are
zero for the whole wait, so a reader that only counted frames would leave the
event hanging with no error anywhere. **The instruction index advances at that
moment and nowhere else**, because the move route command deliberately left it
on itself so the event would be held there.

**The board is ticked above the state check.** A figure on a patrol keeps walking
while a message is on screen, and a reader that ticked it only in the route wait
would freeze every figure for the length of a text box.

**Test evidence** `test_wolf_character_board.cs` (14) and
`test_wolf_move_route_runner.cs` (14, one new test for the refusal), with the
existing `test_wolf_move_route.cs` (11) re-measured. **1271/1271**.
**Mutations** Sixteen effective rules over two runs, **16 of 16 caught** —
including the frame budget not being charged, the first frame counted twice, the
index checked before the frames, the wrap not clearing the flag, the index not
advancing on a route wait, the board not ticking during a frame wait, and a
refused movement reported as stepped. One rule was a rename that does not compile
and is not counted.

## The move route was read, tested and never run — 24 verified types and no executor

**`WolfMoveRoute` had a type table of twenty four verified types, a binary reader
with eleven tests, and nothing executed a single step.** A game with a patrol route
would load and stand still, and the suite stayed green because it only ever read
steps, never ran one. **A reader that is tested and not executed is a parser.**

**The passability bits are the help's own: `1上+2左+4右+8下+16左上+32右上+64左下+128右下`.**
**A diagonal is its own bit and not up plus left** — up is 1 and left is 2, so their
sum is 3, and there is no bit 3. A reader that combined them would produce a
direction the format has no bit for, and a character holding bit 3 would match
no direction at all.

**A refused step still turns the character to face the direction.** The first
version of `Step` returned on the refusal and left the facing alone, while the
comment above it promised the opposite — **and the test is what caught the two
disagreeing.** A guard that walks into a closed door faces it, and a game that
shows the guard watching the hero through the gap depends on that.

**Speed and frequency are 0 to 6 and they are not the same thing.** The help writes
`移動速度[遅0-6速]` and `移動頻度[早0-6遅]` — one slow to fast, the other often to
rarely — and a reader that mapped one onto the other would make a fast character
move once in a while. A rate outside the range is clamped and not refused: a
character at a clamped rate still moves, and refusing would stop the event.

**Speed 0 is one frame per tile and not an infinite wait.** Dividing by the speed
would produce an infinite frame count and a route would never finish. The frames
per tile fall with the speed: 1 at speed 0, 16 at speed 1, 8 at 2, 4 at 4, 2 at 6.

**The add step reads the old value,** the same rule the variable operation follows:
a right hand side that names the same variable as the destination has to see the
value before the write. **A variable step to a plain number is refused, not
stored**, because storing under the raw key would write something no read would
find — the step would appear to work and then lose its value.

## Five steps are refused, and that is the honest answer

**Approaching an event needs a second character, approaching a position needs the
map, a jump needs its own route, a sound needs audio, and a graphic is a file
name this runner has no loader for.** Answering the approach steps with a
direction would walk the character somewhere the event is not; answering the
sound and graphic steps with success would be a lie the caller cannot detect.
**A refused outcome is the one answer a caller can act on**, and it is what keeps
this slice honest about what it does not do.

**The ten key facing table is deliberately not implemented.** The help says a
facing is 1 to 9 and corresponds to the ten key, and points at "figure A" for the
correspondence — a diagram that is not in the text. An earlier draft of this card
guessed the table and it had duplicate values, which is impossible; **guessing the
band offsets cost a card, and this is the same mistake in the same session.** The
function returns "no direction" and says why.

**Test evidence** `test_wolf_move_route_runner.cs` (13), with the existing
`test_wolf_move_route.cs` (11) re-measured. **1256/1256**.
**Mutations** Seventeen effective rules over three runs, **17 of 17 caught** —
including right as bit 3, a diagonal as up-plus-left, the passability check
removed, the facing not following a refused step, a speed of 6 giving sixteen
frames, and the add step not reading first. One rule was a rename that does not
compile and is not counted.

## The band offsets were guessed and the help says otherwise — this card fixes both, and the switches

**The last card read the four bands out of the help and then guessed their
numbers.** It wrote "a million block per band" and moved to the next task, and
the tests asserted that guess and passed. **The help names them, in two
different pages, and they are not a run:** `1100000～:マップセルフ変数` and
`1600000～:コモンセルフ変数` from the common event page call, `2000000` for
normal variable 0, and `3000000` for string variable 0.

**A computed block put map self at 1,000,000, common self at 2,000,000 — a
normal variable — and the system band at 3,000,000, which is the string band.**
So a game reading its system clock out of the string range would have got a
number, and the number would have been plausible.

**A mutation run did not catch it, and that is the lesson worth keeping.**
The twelve rules of the operator card mutated the behaviour around the
offsets and every one was caught, because the tests agreed with the code. Two
wrong numbers that agree produce a green suite. **The check that would have
caught it is reading the source, and the offsets are now pinned in a test that
names the page they come from.**

**The million itself is a reference that names no band, and the whole gap says
so.** The help says a million or more is called, so 1,000,000 is a reference —
and it is below the map self base, so it points at nothing. That is a different
answer from "not a reference", and the code has two codes for it: `-1` is a
value, `-2` is a reference naming no band. **A reader that merged them would
tell a caller 1,050,000 is a value, and a caller that stored it would keep a
pointer in a variable the game reads as a number.**

**The string band is recognised and refused.** This reader has no string
variables, and it says that rather than falling through to the normal band and
answering a string reference with a number. **The variable database is not a
band at all**: the branch help says a variable call such as `1600000` may not
be given when the database is the source, so it has no offset to be indexed
by. It is addressed by type and column, in a store of its own, keyed by the
type shifted rather than multiplied so a large type cannot wrap into another
type's cells.

## And the switches were one dictionary where the help names two ranges

**0 and above address a map event, 500,000 and above a common event.** The VM
had one `Dictionary<int, bool>`, so **a map switch and a common switch with the
same index collided** — and the collision is silent, because both reads answer
with a boolean and only the wrong one.

**The base is 500,000 and not a million,** and the variable bands start at
1,100,000, so a reader that reused the variable scheme would leave 100,001 to
500,000 unreachable and a game with a switch there would find it permanently
off. **A switch number outside both ranges changes nothing**, because a reader
that grew a dictionary would store a switch the editor cannot hold and the
next load would not carry it — the switch would work during the session and
vanish after it.

**An unset switch is off, and so is one that cannot be read.** The condition list
is "on" and "off" and nothing else, so an unreadable switch is off, which is
also what a game expects before it has set it.

**Test evidence** `test_wolf_switches.cs` (7) and `test_wolf_variable_bands.cs`
(13, three rewritten because they asserted the guessed offsets).
**1243/1243**, with the operator, comparison and runtime files re-measured.
**Mutations** Thirteen rules in one run, **13 of 13 caught** — including each
of the three offsets, the gap giving the wrong code, the database keyed by
column alone, and the two switch maps folded back into one.

## The WOLF variable operators are done — the VM knew two of fourteen

**The help tabulates fourteen: `=`, `+=`, `-=`, `*=`, `/=`, `%=`, pull up, pull
down, absolute value, arc tangent, sine, cosine, and square root.** The VM knew
two, and the second — addition — was hard coded into its own opcode, so there was
no place to put the other twelve. **A reader with two cannot compute a hit rate,
a damage formula, and an angle, and none of the three is an exotic game.**

**One path for every operator, and the operator is a field and not an opcode.**
The editor picks it in a dropdown next to the destination, so a program that
switched on the opcode would need a thirteenth case the moment the editor adds
one, and the two lists would drift apart. `AddVariable` is now the addition
operator over the same path, which is why the two lists cannot drift.

**The current value is read before the write,** and that ordering is the reason
the resolve happens first: a right hand side that names the same variable as the
destination has to see the old value, or a doubling command would read the new
one.

**Division by zero leaves the variable alone and is not an error.** The help says
a zero divisor behaves as divide by one. A reader that returned zero, threw, or
wrote a sentinel would be wrong three different ways for one line of the help.

**Trigonometry is scaled and the scale is the operator.** The angle is tenths of
a degree and the result is thousandths, so the help's own examples are 600 → 866
for sine and 600 → 500 for cosine. A reader in degrees and floating point would
return 0.866 and it **would not look wrong** — it would look like a small number.

**The arc tangent reads two right hand sides and not the current value,** because
a slope is a direction and a direction needs two axes. A reader that sent the
destination value as the X vector would make the angle depend on what the
destination already held, which no game means. The order is Y, X, because the
help says X is right positive and Y is down positive, so straight down is +90 and
not −90.

**A bare arc tangent reaches only ±90 degrees,** so `Atan2` covers the circle and a
slope pointing left is 1800, a half turn. A reader with the bare one would clamp
it to 900 and point the slope the wrong way.

**The whole computation is wide, and the test proved it had to be.** A right hand
side of three billion does not fit an `int`, so a test written as an `int` would
not have compiled and a test written with `unchecked` would have carried a
different number. Both the multiply and the subtract leave the range before the
clamp can see them, so **clamping an `int` clamps the value after the wrap** —
and the clamp is applied to the wrong number. `Switch` returns a `long` for that
reason, and the parameter type follows the help's own ±2 billion bound.

**The square root of a negative is 0, not NaN.** The help does not name the case,
but a NaN compares false against both clamp bounds and would be stored as an
arbitrary number.

**An operator this reader does not have changes nothing.** Falling back to
assignment would silently rewrite the variable with the right hand side, and a
game written in a newer editor would lose values instead of being refused.

**A test that proved itself wrong.** The rounding test first claimed that the
help's examples separate rounding from truncation, and they do not: five gives
2236 either way, and so do two and three. **Measured, the separating input is
seven** — the root of seven times a thousand is 2645.75, so rounding gives 2646 and
truncation 2645. And no input has an exact half, so half-away-from-zero and
half-to-even cannot be told apart from this input set; the code says away from
zero and the board says so rather than claiming more. A second test asserted
`sin(1800) == 1000`, which is ninety degrees wearing a half turn's comment;
measured, 1800 is 180 degrees and gives 0.

**Test evidence** `test_wolf_variable_operator.cs`, 15 tests, including four that
run the operator through the VM. **1233/1233**.
**Mutations** Twelve rules in one run, **12 of 12 caught** — including the
division-by-zero guard, the tenths-of-a-degree scale, truncation in place of
rounding, the bare arc tangent, and `AddVariable` quietly turned into assignment.

## The WOLF variable bands are done — a flat dictionary could not hold them

**The editor lists four: `Self / Var / Sys / 可変DB`** — self, normal and
reserve, system, and the variable database. The VM had one
`Dictionary<int, int>`, so **a self variable and a system variable with the
same index collided** — and the collision is silent, because both reads answer
with a number and only the wrong one.

**The million boundary is the addressing scheme itself.** A number at or above
1,000,000 is not a value, it is a *reference*: 2,000,005 means normal variable 5.
A reader that treated the number as a value would store two million in a field
meant to point at variable five, and the game would read a number it never
wrote.

**The boundary is inclusive.** The help says 1,000,000 *or more* is called, so
a reader that tested `>` would treat exactly 1,000,000 as a value and never
resolve self variable 0 — the one variable every WOLF event uses.

**The block is one based and the band is zero based**, so 1,000,000 is block 1
and self band 0. A reader that used the block directly would be off by one
for every band, and the first band would address one that does not exist.

**The database band is smaller than the other three** (999 rows against 99,999).
One bound for all four would let a game address database row 50,000 — a row
the editor cannot hold and a save file cannot carry.

**A plain value resolves to itself and is not a place to write to.** That is
what the help's "do not call the data" checkbox means: a field may hold either,
and the number itself says which. A reader that let a value be a write target
would store a number under a key that is not a variable at all.

**The comparison resolves both sides**, because the help says the compared
value may be a variable too — 2,000,000 there means normal variable 0. A
reader that resolved only the left side would compare a normal variable
against the *number* two million instead of against what it holds.

**The VM's convenience accessors address the normal band**, because a caller
that reaches for "a variable" without saying which one is asking the question
WOLF answers with four, and the accessors have to answer something. They are
on the VM and the bands are reachable directly through `VariableBands`.

**The runtime fixture changed and that is the point.** Its variable operand was
a bare `1`, which is the *value* one — so it was writing a value, not a
variable, and a reader that treated the operand as an index would have stored
it somewhere the game never addressed. It is `2,000,000` now, with a comment
saying why, and `Test_WolfPluginRuntimeLoadsDataAndAdvancesDeterministicEventVm`
**failed when the model changed** — which is what a runtime test is for.

**Test evidence** `test_wolf_variable_bands.cs`, 10 tests, plus the seven
comparison tests and the six runtime tests, all re-measured.
**1218/1218**.
**Mutations** Ten rules over two runs, **10 of 10 caught** — including the
million boundary made exclusive, the block left zero based, the two bands
writing into the same dictionary, and the comparison resolving only one side.

## The WOLF variable branch is done — and it had one comparison and no test

**`IfVariable` compared with `==` and nothing else, and no test in the
repository exercised it at all.** The one comparison it had was as unproven
as the six it was missing — a reader with seven of them would have been
half-verified without anyone knowing.

**The editor offers seven and the help names them**: greater, greater or
equal, equal, less or equal, less, not equal, and bit and. **A reader that
implemented only `==` would take one branch in seven**, and every one of the
other six would fall through to the else path — so a chest guarded by "V0 is
at least 1" would never open.

**The bit-and test is equal to the value and not "any bit set".** The help
spends a paragraph on it: with V0 = 5 (`101`) and value 2 (`010`), `5 & 2` is
0, not 2, so the test fails. **A reader that wrote `(variable & value) != 0`
would pass every test with any bit set, and a game that guards a door with a
bit test would open it for everyone.** And a value of zero satisfies it in
every case, because anything anded with zero is zero — the help says so
outright.

## The branch had one target, and that is the second half

**A WOLF branch has two arms and the VM had one jump.** The fall-through was
the true arm and the single jump the false one, so when the condition held the
true arm ran *and* the false arm ran — **a chest that opened and a guard that
attacked in the same frame.** That is a different failure from the comparison
and a worse one, because it is not visible in any single comparison.

**Both arms are now their own target** (`TrueJumpIndex` and `JumpIndex`), and
**the last command of an arm jumps over the other arm** (`NextIndex` on the
command).

**The arm-end was first modelled as a remembered end index, and that was
wrong**: it needs hidden state that a program with two branches in a row leaks
from one into the other, and the check fires before the arm runs if the branch
has already jumped into it. The jump on the command has no such state and is
also the shape a WOLF event list has — the editor writes the jump after the
last command of an arm. **Three attempts were needed and the third is the one
that is in the code; the first two are recorded here because the reason the
model failed is the reason the model is right.**

**Test evidence** `test_wolf_comparisons.cs`, 8 tests.
**1208/1208**, `TestRm2kWolfComparisons: 8/8`.
**Mutations** Ten rules, **10 of 10 caught** — including the bit test read as
"any bit set", the arm jump removed, and an unknown comparison falling back to
equality instead of being refused.

**What is still missing on WOLF** and is stated rather than hidden: **there is
no native WOLF fixture.** The `wolf` fixture directory holds a synthetic
`urpg-wolf-plain-json` envelope and a README that says so, and the seven
comparison numbers are pinned against the editor help page rather than a
file on disk. The variable model is also still a flat `int` where WOLF has
self, normal, system and database bands — **that is the next WOLF card, and it
is larger than this one.**

## The class parameter chunk is decoded — `1008` is no longer missing data, only code

**`1008 ChangeClass` was not waiting for a dispatcher, it was waiting for data.**
The command carries a class id, a level reset flag, a skill mode and a
**parameter mode** — and the parameter mode is meaningless without the class
table, because the reference reads a *level* out of it. The LDB class
parameter chunk was being read into `unknown_fields` and staying there.

**Nothing was lost, and that is worth saying.** The reader contract held: the
raw chunk is still in `unknown_fields` alongside the decode. What was missing
was a way in, and this adds one without replacing anything.

**Six `int16` vectors and not six scalars.** liblcf `rpg::Parameters` holds
`maxhp`, `maxsp`, `attack`, `defense`, `spirit` and `agility` as
`vector<int16>` with one entry per level, and its `WriteLcf` stores them in that
order with no lengths in front. **A reader that assumed six scalars would read
the first value of each vector and call it the class maximum — a level 99 class
would give its heroes level 1 stats**, and every number would be in range, so
nothing would look wrong.

**The order is liblcf s and not alphabetical.** A reader that sorted the names
would give a class its agility as its hit points, with all six values in range.

**Little endian, signed, both halves.** A reader that read one byte would cap
every stat at 255, and a class with a max hit point above 255 would have its
heroes quietly weakened.

**The chunk is a multiple of twelve or it is a different structure.** A reader
that decoded it anyway would read six values out of a chunk that holds
something else — and the stat numbers would be plausible, which is worse than
a refusal.

**Levels are one based in a game and zero based in the array.** The reference
reads `parameters[level]` after decrementing; a reader that skipped that would
hand a level 1 hero the level 0 row, and on a class whose first level is
deliberately weak that is the difference between a tutorial and a hero who
starts the game under-strengthed.

**A class with no parameters says so and returns false.** A zero hit point
maximum would read like a design choice — a hero the game made unplayable
rather than a file that did not parse.

**Test evidence** `test_rm2k_class_parameters.cs`, 7 tests.
**1200/1200**, `TestRm2kClassParameters: 7/7`.
**Mutations** Nine rules, **9 of 9 caught** in the first run — including the
vectors rotated by one, the high byte dropped, and the level left one based
instead of zero.

**What `1008` still needs** the class skill list and the four modes it carries.
The parameters are the part without which the command could not be written at
all; the rest is a switch.

## `10500 Simulated Attack` is done — and it is not a battle

**No turn order, no troop, no target selection.** The command picks heroes by
the usual actor parameters, computes one number from their defence and
spirit, and subtracts it from their hit points. A game uses it for a trap that
bites, a poison that hurts, a script that stings — *damage without a battle*.
**This slice did not have to build a combat system first, and that is worth
knowing before the next one does.**

**Defence is divided by 400 and spirit by 800**, so 800 points of spirit block
exactly as much as 400 points of defence. **A reader that used one divisor for
both would make spirit twice as strong as the game meant it** — by a factor of
two, on the axis a game tunes.

**The result is floored at zero twice and the order matters.** The reference
clamps after the two subtractions, adjusts the variance and clamps again,
because a variance draw can push a small result below zero. **A reader that
clamped once, at the end, would let a variance hand out negative damage** — and
negative damage *heals* the hero the command was aimed at.

**The spread is at least one.** `max(1, var * base / 10)` — without the one, a
small base with a large variance rounds to zero and **a game that asked for
ten percent variance would get none**, the exact opposite of what it asked
for. And the spread is symmetric: half is subtracted, not all of it.

**The result variable holds the last actor damage and not the sum**, because
the reference writes it inside the loop. **A reader that summed would make a
game that shows "you took N" show a number the game never produced** — and
consistently, because it is the same wrong number every time.

**The variance is drawn from its own generator and not MZ's**, because the
two engines do not share a stream. It is the same shape as `MzRandom` and is
deliberately not a claim about the engine numbers — those are not repeatable.
What it gives is a run a save file and a test can both replay.

**The test had to use a rate of 1 and not 100**, twice. At a hundred percent
every value blocks the whole attack, all the cases come out as zero, and a
test that cannot tell the divisors apart proves nothing about them. Both times
the first draft passed at a rate that hid the thing it was testing.

**Test evidence** `test_rm2k_simulated_attack.cs`, 8 tests.
**1193/1193**, `TestRm2kSimulatedAttack: 8/8`.
**Mutations** Ten rules, **10 of 10 caught** in the first run — including the
two divisors swapped, the second clamp removed, the damage added instead of
subtracted, and the result variable accumulating.

**What is left** `1008 ChangeClass` and the five liblcf codes the reference
does not dispatch. `ChangeClass` needs a class model, and it is a real one:
the command carries a class id, a level reset flag, a skill mode and a
parameter mode, and none of them have anywhere to land yet.

## `20140` and `20141` are done — and this slice found a field the parser was throwing away

**The decoder read the LCF `0x0D` indent chunk, wrote it into its dictionary,
and `EventCommand` had no field for it.** So every event parsed completely and
no branch could ever be identified. `EventCommand.Indent` exists now and
`Rm2kEngineRuntime` passes it through.

**A test of the codes, the parameters and the strings would have been green the
whole time** — the information was lost between two correct readers, and only
the behaviour of `20140` could have shown it.

**`20140` is not a second choice window.** It is one branch of a list the player
already answered, and the reference hands it to `CommandOptionGeneric`, which
either clears the sub index — because this is the chosen branch — or skips to
the next conditional. **A reader with only one half would have a hero who asks
a question, walks away, fights the guard, buys the sword and leaves, all in one
frame.**

**Each branch ends with its own `20141`.** The skip walks to the next command
from `{ShowChoiceOption, ShowChoiceEnd}`, so a branch without an end of its own
would swallow every branch after it. A list written as one block with a single
end is therefore a different list, and a reader that treated it as the same one
would skip the rest of the page on the first unchosen branch. The test builds
the reference shape — `20140(n), marker, 20141` three times — and the first
draft built the other one and "failed" for the right reason.

**The chosen branch clears the sub index to a sentinel, not a flag.** Without
that, a second list on the same page would compare against a stale number and
the player second answer would pick the branch the first answer cleared.

**The skip checks its bound before the read, not after the step.** A branch
list whose last option has no end is a game bug, and a reader that stepped
first would read one past the array and throw. This one did; the test caught
it and the loop now checks first.

**Test evidence** `test_rm2k_choice_branches.cs`, 6 tests.
**1185/1185**, `TestRm2kChoiceBranches: 6/6`.
**Mutations** Seven rules over two runs, **7 of 7 caught** — including the
chosen branch skipped instead of run, the skip stopping at nothing, and the
indent dropped again in the constructor.

**What is left** `1008 ChangeClass` and `10500 SimulatedAttack`, plus the five
liblcf codes the reference does not dispatch. Those are the last two real
commands, and `ChangeClass` is the one that needs a class model first.

## `11710`, `11720`, `11740` and `11750` are done — and one of them had no writer at all

**`11750 Tile Substitution` had two 144-entry tables, two readers and no
writer.** So the command could be parsed and never run, and a test of the
readers would have been green the whole time. `SubstituteTile` is the method
that closes it.

**`SubstituteLower` adds `BlockEIndex` on the way out, so the stored value is
the raw one.** A writer that stored the number a command asked for would read
back an index that is `BlockEIndex` too high — every lower tile drawn one row
off. The test asserts the offset, because a test that expected the raw number
would have "failed" a correct writer.

**`11720` has six flags and two speeds, and the speeds come from different
parameters than the flags.** The flags are 0, 1, 2 and 4; the horizontal speed
is 3 and the vertical is 5. **The fourth flag and the horizontal speed are
adjacent in the list**, which is what makes the mistake easy: a reader that
read the parameters in order would take a flag as a speed.

**An empty panorama name is the database panorama and not a missing file** —
that is what the reference does with `if (!params.name.empty())` before it
asks for the file. A reader that treated an empty name as an error would
refuse the one thing the command is for: going back to what the database says.

**The reference makes the interpreter wait for the panorama file.** This reader
has no file system here, so the wait is a diagnostic — **a reader that waited
forever would hang a game whose panorama is simply missing**, and a missing
panorama is a bug in a game, not a reason to stop the interpreter.

**Zero encounter steps is a real value and it is the one that turns random
encounters off.** A reader that treated zero as unset could never turn them
off, and a game that does so — a town, a puzzle room, the last map — would
keep fighting every few steps for the rest of it. **And a new game that
inherited zero would be unwinnable**: no fights, no experience.

**Chipset 0 is a real chipset.** The reference compares against the current
one and returns early when they match, so a game that sets the chipset it
already has pays nothing. A reader that treated zero as unset would refuse the
first chipset in the database — and that is often the most used one.

**`ChipsetId`, `MapParallax` and `EncounterSteps` were not in the reset**, and
the tests found it. A new game that opened in another game tiles would look
like a bug, and one that inherited another panorama would open on a sky that
is not its own.

**Test evidence** `test_rm2k_map_changes.cs`, 12 tests.
**1179/1179**, `TestRm2kMapChanges: 12/12`.
**Mutations** Ten rules, **10 of 10 caught** in the first run — including the
upper table written into the lower one, the two speeds swapped, and the map
settings surviving a reset.

## `10620`, `10630`, `10640`, `10650` and `10850` are done — and `10850` has a value that is not a vehicle

**Vehicle id -1 moves the party and is not an invalid id.** The reference has a
comment on it: in RPG_RT a party in no vehicle has the id -1, and passing -1
moves the party on its own. **A reader that refused it would make every
"teleport the hero" command in a game do nothing** — and that is a very common
command. There is a test that a missing vehicle is refused and -1 still works,
because they are two different meanings for one field.

**The vehicle id is shifted by one, because the liblcf enum is
`None = 0, Boat = 1, Ship = 2, Airship = 3`** and the reference writes
`(Game_Vehicle::Type)(com.parameters[0] + 1)`. Those numbers are in the save
format, so they are not an internal detail. A reader that used the parameter
directly would address vehicle 0 — and vehicle 0 is the party, not a boat.

**`10650` sets two fields, the current sprite and the original one.** The
original is what the vehicle returns to when a board ends, so **a reader that
set only the current one would leave a vehicle in its costume after the party
got out.**

**A party inside a vehicle moves with it**, and the reference returns right
after. Moving only the vehicle would leave the hero standing in the map they
left — in a game with a boat, that is a party in open water.

**`Boarding` is nullable and that is a design.** A game that never touches a
vehicle never allocates one, and a reader that dereferenced it would throw on
every `10850` in a game with no ship — **and the common case is exactly that
game.** The first run of this slice did exactly that; the tests found it.

**`10630` takes the transparency straight from `parameters[2]` and not from the
bitfield**, which is the reference's own split: it reads the mode index for the
file and the pose and this one directly. A reader that took all three from the
bitfield would make a costume transparent whenever a Maniac game packed a
different value there. **And the index is a pose, not a character number** — a
costume is the same file with a different index, and the file name stays right,
so no visual check catches a reader that got it wrong.

**A missing hero is a warning and not a refusal.** The reference calls
`GetActor`, checks the result, writes a warning and returns true. **A reader
that held the page would leave a cutscene waiting for a hero the database never
had**, and a game that addresses an actor slot it chose not to fill would hang
there forever.

**Test evidence** `test_rm2k_actor_graphics.cs`, 14 tests.
**1167/1167**, `TestRm2kActorGraphics: 14/14`.
**Mutations** Eleven rules, **11 of 11 caught** in the first run — including the
vehicle id not shifted, the original sprite not set, and the nullable boarding
dereferenced.

## `10660`, `10670`, `10680` and `10690` are done — the second block that was never on the board

**None of these four was on K-136.** They came out of the second fresh
measurement of the reference against the interpreter. That is now twice that
the card list was short, and once it was actively wrong.

**The audio families have different widths: seven music and twelve sounds.**
Music is battle, victory, inn, boat, ship, airship, game over. Sounds are the
four for the menu, one for the battle, and one per battle event — enemy attack,
enemy damage, ally damage, evasion, enemy death, item use. **A reader that
offered one music slot would let a game replace its battle theme and its inn
theme at once, and one that offered only the menu sounds would leave a game
with a silent battle.**

**Music has a fade-in and sounds do not** — a sound effect with a fade is a
sound effect the player waited for. That is why `10660` reads `parameters[1]`
as the fade and `10670` does not.

**The contexts are zero based, and that is a trap.** `BGM_Battle` is 0 and
`SFX_Cursor` is 0, so the guard is 0 to 6 and 0 to 11. **A guard that started
at one would refuse the battle theme**, which is the one a game changes most.

**Without the Maniac patch the parameter is the value.** The reference reads
`ValueOrVariableBitfield(com, 5, 1, 1)` and that helper returns
`parameters[val_idx]` directly when the game is not a Maniac one — so the
value index and the mode index are the same number. **A reader that read
`parameters[5]` as the value would silence every track**, which is what the
first run of this slice did. The patched path needs a game-string mirror this
runtime does not have, so it uses the plain value and says so.

**Six transitions, and the sixth is the one a game notices.** Teleport in and
out, battle start in and out, battle end in and out. `Transition_Count` is a
**count and not a last index**, so a guard that read a last index of five would
refuse the transition that brings the player back from a battle.

**The reference asserts on an unknown transition** — a crash in a debug build
and a write to nothing otherwise. This reader says which six are allowed.

**The system graphic casts two numbers without checking them.** A value past
the enum produces an out-of-range value, so they are clamped, and an empty
name is a request to go back to the database rather than a file that does not
exist — the reference has a `ResetSystemGraphic` and this is the command that
reaches it.

**Test evidence** `test_rm2k_system_settings.cs`, 12 tests.
**1153/1153**, `TestRm2kSystemSettings: 12/12`.
**Mutations** Twelve rules over two runs, **12 of 12 caught** — including the
context guard started at one, the SFX width cut to the menu four, and the
Maniac warning silenced.

## `11820` and `11830` are done — and one of them was never on the board

**`11820 Change Teleport Access` fehlt in K-136 vollständig.** Das Board
führte den Bereich "`11810`–`11840`" mit Einzelcodes, und dieser fiel zwischen
den Einträgen durch. Die Referenz hat ihn: `SetAllowTeleport(parameters[0] !=
0)`. Es ist der **vierte** der vier Ein-Zeiler-Access-Befehle, und **ein Board,
das Lücken zwischen Bereichsangaben hat, verliert Befehle.**

**Und `11830 Escape Target` hat dieselbe vierte Parameterbedeutung wie der
Sprungpunkt:** "der Schalter muss AN sein". Wer sie als "benutze einen Schalter"
liest, macht einen gesperrten Fluchtpunkt von der ersten Minute an verfügbar —
und ein Spiel, das seinen Ausgang hinter einem Schalter versteckt, wäre direkt
hinauslaufbar.

**Es gibt genau einen Fluchtpunkt, und ein zweiter Befehl ersetzt ihn.**
`SetEscapeTarget` setzt. **Wer eine Liste führte, müsste eine Regel erfinden,
welcher gewinnt** — und die hat das Spiel nie geschrieben.

**Der Default-Parameter auf `true` war eine Falle, und die Tests haben sie
gefunden.** `SetAccess` bekam einen vierten Parameter `pTeleport = true`, und
**jeder der drei älteren Aufrufe setzte Teleport damit still zurück** — der
letzte Befehl gewann, nicht der, der das Flag benannt hatte. Das ist exakt der
Fehler, den `SetTimer` und `StartTimer` einmal gekostet hat, in einer anderen
Form. Der Default ist jetzt weg, weil ein Default auf dem Zustandswert hier
nichts zu suchen hat.

**Test evidence** `test_rm2k_teleport_access.cs`, 9 tests.
**1141/1141**, `TestRm2kTeleportAccess: 9/9`.
**Mutations** Ten rules over two runs, **10 of 10 caught**. Two of them broke
the build first because `SetAccess` has no defaults, and were re-measured with
code that compiles — a compile error is not a caught rule.

## `10920`, `11810`, `12420` and `12510` are done — and the board was wrong about one of them

**`10920 Store Event ID` was listed as having no method in this EasyRPG build.**
It has one: `CommandStoreEventID`, with a body that does three things a reader
has to get right. Both coordinates go through `ValueOrVariable` with the
**same** mode in `parameters[0]`, so a game can look up the tile it last
walked over — a reader that read them as constants could only ever ask about
one tile. An empty tile stores **0 and does not hold the page**, because that
is the reference: `ev ? ev->GetId() : 0`, and a reader that held the page would
leave the variable holding whatever it held before.

**A tile outside the map is refused rather than answered with a zero**, because
a zero reads exactly like "no event here" and the difference between a bug in a
game and a bug in the reader is the diagnostic.

`10920` needs the map, and **the interpreter does not have a map** — it has
four `Func` resolvers. This adds the fifth, `Func<int, int, int>?`, for the
same reason the other four exist: the interpreter must not know how a map is
held, or a test could not drive it.

**`11810` parameter 4 says the switch must be ON, not that there is one.** A
reader that read it as "use a switch" would make every conditional warp
unconditional, and a secret entrance would open at the start of the game.
Parameter 0 is a **mode and not a target id**: a non-zero removes every point
on that map. A point outside the map is refused, and a second point on the
same tile replaces the first — the reference appends, and a game that re-
declares a point would otherwise have two warps on one tile.

**`12420` and `12510` take no parameters at all**, which is why the reference
writes the command as `const& com` and never reads it. Both **wait for an open
message first** — a hero who says their last line and then dies should die
after the line is read, and a reader that showed the screen on top of the
text would bury the line the game wrote for that moment. Both hold the page:
the game over screen with `return false`, the title screen as an async
operation. `WaitingFor` says which, because a wait with no reason looks like a
hang.

**Test evidence** `test_rm2k_teleport_and_outcome.cs`, 13 tests.
**1132/1132**, `TestRm2kTeleportAndOutcome: 13/13`.
**Mutations** Ten rules, **10 of 10 caught** — including the warp flag read as
"there is a switch" and the empty tile answered with a made-up id.

## `11840`, `11930` and `11960` are done — three one-liners and a default that matters

**The reference has three one-line methods with the same shape** —
`SetAllowEscape(com.parameters[0] != 0)` and its two siblings — and **that is
the whole command.** One handler and three constants is the honest reading,
not a saving.

**A zero is a removal and not "no change".** A cutscene that locks the menu and
a cutscene that unlocks it again write the same field, so **a reader that only
ever set the flag to true could never give a player their menu back**, and a
game with a locked-menu cutscene would be stuck in a locked menu.

**Any non-zero is allowed, not just one** — the reference tests `!= 0` and not
`== 1`, so a game that passed a computed boolean is not refused.

**All three default to allowed, and that is a real default and not a guess.**
A database that never ran one of these commands has all three set, so a new
game is a game the player may open the menu in, save from and escape from.
**A reader that defaulted to forbidden would make every untouched game
unplayable the moment the player pressed Escape** — and no test of a command
would ever have found it, because a command-free game is the case nobody
writes a test for. `Test_ANewGameAllowsEverything` exists for exactly that.

**Each command writes its own flag and leaves the other two alone.** A reader
that wrote all three from defaults would unlock a cutscene the game had just
locked. The setter is private behind one `SetAccess`, for the same reason the
timer has `SetTimer` and `StartTimer` and not a public field: the collapse of
two operations cost a save file once already.

**Test evidence** `test_rm2k_access_commands.cs`, 5 tests.
**1119/1119**, `TestRm2kAccessCommands: 5/5`.
**Mutations** Eight rules, **8 of 8 caught** — including each of the three
commands writing the other two flags from defaults instead of from the
current state, which is the bug the family shares.

## `10430`, `10460` and `10470` are done — and the save codec had no idea actors existed

**Base and current are two different things, and the reference has two calls
for them.** `10430` calls `SetBaseMaxHp`; a buff calls `SetMaxHp`. The base
**survives a level change and a save** and a buff does not, so this reader keeps
the base in `Rm2kActorValues` and deliberately has **no field for the current
maximum**. A reader that stored the current value would let a saved game keep a
buff that ended three maps ago.

**HP and SP clamp differently and that is not an accident.** HP has a lethal
flag and a floor of one when it is not set, because a game can protect a hero
from a hit. `CommandChangeSP` has neither — the reference writes
`if (sp < 0) sp = 0;` and nothing else. **A reader that gave SP the same floor
as HP would leave a hero unable to cast anything.**

**The ceiling is the current maximum, not the base.** A hero with a base of 40
and equipment worth 10 cannot be healed past 50.

**`parameters[2]` is a remove flag and not a sign** — the reference negates the
amount when it is set — and `10460` has six parameters while `10470` has five,
because the sixth is the lethal flag and SP has none.

**And the save codec had no actor data at all.** Every base value and every
current count was lost at the next save: a hero who was nearly dead reloaded at
full health, and a `10430` a game did was gone. Bases and current counts now
travel together, because **a save that kept the counts and dropped the bases
would reload a hero clamped to a maximum he no longer has.** Only actors with
something are written, in id order, so two saves of the same game are byte for
byte the same. An out-of-bounds row is rejected **whole**, and the test checks
that nothing was applied.

**Test evidence** `test_rm2k_actor_battle_values.cs`, 14 tests.
**1114/1114**, `TestRm2kActorBattleValues: 14/14`.
**Mutations** Nineteen rules over three runs, **19 of 19 caught** — ten in the
commands and the value class, nine in the save codec, including the codec
writing no actors at all and the codec writing every actor whether it was
touched or not.

## `10120`, `10130` and `10230` are done — and one of them fixed a fault in the save codec

**`SetTimer` started the timer, and it should not have.** The reference has
three operations in one command: set the seconds, start with the visible and
battle flags, and stop. **A reader that started on set collapsed the first two**,
and a game that wrote `SetTimer` to arm a countdown it would start later started
it immediately — the exact difference between a timer that counts and one that
does not.

**And the save codec had the same fault.** It restored a timer with `SetTimer`
alone, so **every saved countdown came back running** — a game that saved a
paused timer and reloaded it got a live one. The two old tests that broke on the
repair were using `SetTimer` as "start the timer", which is the same
misreading; they were corrected, not weakened, and the round trip now proves
both operations separately.

**`StopTimer` keeps the seconds.** A game that stops a timer to show the count
and then starts it again expects the count to still be there. It also no longer
throws for an id it does not know, because a stale timer id should not be a dead
event.

**`10120` is four flags and not one style.** Transparent, position, fixed,
continue-events. **Parameters[2] is inverted** — a zero means the window holds
its position while the map scrolls — and a reader that mapped a non-zero to
fixed would scroll every window a game had pinned, **which is visible only
while the map moves, so no test of a still map could have caught it.**
Parameters[1] has **three** positions, not two: top, middle, bottom.

**`10130` sets a face, and a face is a request and not a drawn portrait.** The
file has four slots and a ninth is refused with the number in the diagnostic.

**A sixth parameter names the timer**, and the reference reads it *only* when
the command carries more than five parameters and the game is RPG2K3 — which is
why a 2K game has one timer and a 2003 game has two. There is a test for both
readings of the same command.

**Test evidence** `test_rm2k_message_options.cs`, 15 tests.
**1100/1100**, `TestRm2kMessageOptions: 15/15`.
**Mutations** Ten rules over two runs, **10 of 10 caught** — including the codec
restoring the seconds unconditionally, which is the save-file half of the same
fault.

## `10820` is done## `10820` is done — and its counterpart is a liblcf code with no engine

**The three parameters are the variables to write into, not the position to
store.** `parameters[0]` gets the map id, `parameters[1]` the player's x and
`parameters[2]` the y. A reader that read them as a position **would write the
player's tile into three variables and store nothing at all** — which is exactly
the failure a three-integer command invites when the parameters are all the same
kind. There is a test that asserts the parameter numbers do not appear as
values, because the two failures look different in a log and the same in a test
file.

**All three variable ids are checked before any of them is written.** A reader
that wrote as it went would have stored the map and then hit the zero, leaving a
game half-memorized — and a half-memorized location recalls the player to a tile
the game never meant.

**`10830 Recall To Location` is in liblcf's enumeration and has no method in
this build of EasyRPG.** So this repository does not implement it. The asymmetry
is the reference's, and it is recorded rather than filled in from imagination:
a game that memorizes and then recalls would have the first half and not the
second, and **a reader that guessed the second half would teleport players to
tiles the file never described.** `10910` Store Terrain ID and `10920` Store
Event ID are the same shape.

**Test evidence** `test_rm2k_memorize_location.cs`, 6 tests.
**1085/1085**, `TestRm2kMemorizeLocation: 6/6`.
**Mutations** Six rules over one run, **6 of 6 caught** — the parameters as
values, the order of the three writes, checking all three ids up front, the
minimum width, the zero id, and the reset.

## `11310` and `11330` are done## `11310` and `11330` are done — and they close a K-131 island

**`Rm2kMoveRouteState` had no caller anywhere in the project.** K-131 built the
decoder and the state machine, mutation checked them and tested both as
free-standing objects — and **no event could put one on a character.** Same island
shape as the pictures in `PresentationState`, and the same way of finding it:
comparing what a class is *for* against what the reference's commands do.

**`11310` inverts its parameter, and that is the whole command.**
`bool hidden = (com.parameters[0] == 0);` — a reader that mapped a non-zero to
visible gets a hide right and a show wrong, and a game whose only use of this
command is to hide a sprite **works until the first time it shows one again.**
It also clears the through-position, with the reference's own comment "RPG_RT
does this here" — so a player who walked through a wall and is then hidden does
not stay standing in the wall. Showing does *not* clear it, because the reset
sits in the hide branch and not beside it.

**`11330` reads the route as the rest of the list**, from index four to the
end, the way the reference walks it. A reader that read a fixed count would
silently drop a long route, and the game would run a shortened version of what
the author wrote.

**The id mode and the repeat flag share one word.** The reference reads
`ValueOrVariableBitfield(com.parameters[2], 2, com.parameters[0])` and
`ManiacBitmask(com.parameters[2], 0x1)` — the mode is the low two bits and the
repeat is the low bit, so a reader that took the whole number as the mode would
read mode 3 where a game meant mode 1 and a repeat.

**A move frequency outside 1 to 8 becomes 6, and that is the engine's own
default rather than a refusal**: `if (move_freq <= 0 || move_freq > 8)
move_freq = 6;`. A reader that refused would stop a route RPG_RT happily runs,
and a game that wrote a zero because the editor left the field empty would lose
its movement. There is a test for all four bad values.

**`11340` and `11350` are done, and this card said they did not exist.**
The board listed them as "liblcf names them and EasyRPG dispatches them
nowhere" — and that was wrong twice. `Game_Interpreter_Map` has
`CommandProceedWithMovement` and `CommandHaltAllMovement`, and the claim that
"there is nothing to read the parameters from" was the interesting part:
**both commands have a width of zero, because the whole of `11340` is
`_state.wait_movement = true;` and the whole of `11350` is
`Game_Map::RemoveAllPendingMoves();`.** A reader that expected parameters to
read would have been reading past the end of a list that is not there — and
the same reasoning was what kept `11060` unimplemented.

**The route reaches a character through a hook**, `moveRouteStarter`, which the
interpreter refuses visibly when it is absent — because a reader that said
nothing would look like a game that asked for no movement at all.

**Test evidence** `test_rm2k_move_event.cs`, 10 tests.
**1079/1079**, `TestRm2kMoveEvent: 10/10`.
**Mutations** Eight rules over three runs, **8 of 8 caught**.

## `11010`, `11020`, `11030` and `11060` are done — the pan screen, and the card that said it had no engine

**The transition tables are the sharpest thing in this slice, because the
pairing is not regular.** Show and erase are the same twenty kinds read from
opposite ends, and each parameter number names a different one in each table —
4 is `BlindClose` for an erase and `BlindOpen` for a show, 16 is `ZoomIn` and
`ZoomOut`. **The stripes and scrolls mirror their suffix, but the divisions
pair with the combines**, so a reader that mirrored the name would pair
`CrossDivision` with itself and animate nothing. There is a test for exactly
that, over all three division arms.

**Parameter -1 is not a kind.** It means "the game's own teleport transition",
which lives in the editor's settings and not in the command at all. The
reference's two `switch`es have no default arm, so -1 *and* every number it does
not know fall through to none **in silence** — which is what would make every
teleport in a game lose its transition without a word. This reader has not read
those settings, so it says so, and names the number.

**The saturation is a percentage where 100 means untinted.** That is backwards
from what a reader guesses: a reader that treated 0 as "no tint" would tint the
screen to grey at the one value a game writes when it wants no tint.

**The duration is converted, not stored as tenths.** The reference does
`tenths * DEFAULT_FPS / 10` and hands frames to the screen, so a reader that
kept tenths would report a number the engine never had. The tint ticks with the
flash and the shake, because a tint that outlived its flash would leave the
screen coloured after the game said it was over.

**And a wait is conditional — which the first dispatch got wrong.** The
reference calls `SetupWait` only when the sixth parameter is set, and a first
draft returned a bare `true` from the dispatch, so a tint that asked to wait
still advanced the page and the wait never happened. The index moves only when
the command set no wait, which is the same conditional the jump-to-label
needed and got wrong once already this session.

**`11060 Pan Screen` is done, and this card said the reference had no
`CommandPanScreen`.** It has one, with a minimum width of 5 — the two widths
the board once listed for it were the shapes a reader might have expected, not
the one the reference uses.

**Four modes, and only the middle two move anything.** The switch is 0 lock,
1 unlock, 2 pan and 3 reset, **and a value it does not know falls through all
four and does nothing at all.** A reader that defaulted to the pan would have
a game's mistyped mode scrolling the screen instead of doing nothing.

**A lock does not stop a pan that is already running.** The reference calls
`LockPan()` and nothing else, and a reader that halted the pan would freeze a
camera mid-scroll.

**The speed is clamped to 1 to 6 and not refused** —
`Utils::Clamp<int>(com.parameters[3], 1, 6)` — so a game that wrote a zero or a
nine gets the nearest speed and the pan still runs. **A reader that refused
would have stopped the event on a number the engine repairs.**

**The wait is `distance / speed + (distance % speed != 0)`** — rounded up, in
`Game_Player::GetPanWait`. Five tiles at speed three is two frames and not
one, and a pan that would wait zero never ends the frame it started in.

**`11350` stops the map, and the pans with it.** The reference's
`RemoveAllPendingMoves()` is a map-wide call; a reader that stopped only the
player would have a game whose guards keep walking after a cutscene stops them.

**Test evidence** `test_rm2k_pan_screen.cs`, 13 tests.
**1395/1395**, `TestRm2kPanScreen: 13/13`.
**Mutations** Ten rules over one run, **10 of 10 caught**.

**Test evidence** `test_rm2k_screen.cs`, 14 tests, including all twenty
parameters of both tables checked one at a time.
**1069/1069**, `TestRm2kScreen: 14/14`.
**Mutations** Nine rules over one run, **9 of 9 caught**.

## The five audio commands are done — `11510`, `11520`, `11530`, `11540`, `11550`

**`GameSimulationState` had four position doubles that nothing read and nothing
wrote** — `BgmPosition`, `BgsPosition`, `MePosition`, `SePosition`. They were
the residue of a plan for playback this repository has not built, and **a double
no command moves is a claim about time that nothing keeps**. They are replaced
by what the format actually holds: the current track per channel, the fade
state, and the one memorised BGM.

**It is data, not sound.** There is no player behind any of it, and no test
claims a track can be heard. The diagnostics say what was asked for.

**Four channels, and they are not interchangeable.** BGM loops and fades, SE
plays once over it, ME follows the BGM's rules, BGS loops underneath. A reader
that kept one list for all four would let a footstep overwrite the town theme.

**The two commands' parameter lists do not line up.** The music is
`[fade, volume, tempo, balance]` and the effect is `[volume, tempo, balance]` —
**there is no fade on an effect**, so reading both from the same offsets puts the
effect's volume where its balance belongs. `CmdSetup` gives widths of four and
three; a first draft wrote five and four in both the product code and every
test, so all twelve tests failed on a command this repository had never
accepted.

**Balance is 0 to 100 with 50 in the middle**, and not -100 to 100. A reader
that treated the middle as 0 would call every centred track hard left.

**And one refusal I dressed up as caution.** A draft read `parameters[1]` as the
mode for the other three values and then **refused every command whose values
were non-zero** — which is every music command a real game writes. The reason it
gave was "this reader does not decode a bitfield yet", which reads as care. It
was a source I had not read to the end: `ValueOrVariableBitfield` opens with
`if (!IsPatchManiac()) return com.parameters[val_idx]`, so without the patch each
value is simply its own parameter and the fifth parameter holds nothing at all.
**Refusing loudly is not a substitute for knowing.** A game that *does* carry the
patch is still refused, and that refusal names the patch.

**The memorised track is one slot and it is the BGM.** `MemorizeBGM` and
`PlayMemorizedBGM` take no parameters and touch only that channel, and there is
no second memorised track anywhere in the format.

**Test evidence** `test_rm2k_audio.cs`, 12 tests through `ExecuteFrame`.
**1055/1055**, `TestRm2kAudio: 12/12`.
**Mutations** Nine rules over one run, **9 of 9 caught**.

**`12110` Label and `12120` Jump to Label are the sharpest of these.****`12110` Label and `12120` Jump to Label are the sharpest of these.** They are
the only two that change *where* the page goes rather than what it does, and a
reader without them runs a jump as a no-op and lands at the end of the page —
which looks like a game that quietly skipped half its script.

**Acceptance criteria for each command taken from this list**

- The parameters come from the reference's `CmdSetup` minimum width and its
  reads, never from a hand-written table.
- A command that is decoded but not executed says so, and a command that is
  executed does what the source does — including where the source does nothing
  and this reader refuses visibly.
- Each command's own test derives its expectation from the reference or from a
  real fixture.
- Each is mutation checked, and a rule that survives is fixed or named as a
  harness fault.

## `12110` and `12120` are done — the two that decide where a page goes

They were the sharpest entry on this list, because they are the only two that
change *where* the page goes rather than what it does.

**The search starts at zero, not from here.** The reference's loop is
`for (int idx = 0; idx < list.size(); idx++)`, so a **backward jump is a loop**,
and that is how an author writes one without a loop command. A reader that
searched forwards from the current index would turn every backward jump into a
fall-through — a game that loops with a jump would run its body once and stop.

**The index lands on the label, and the engine's own rule says why.** EasyRPG
increments only when the command left the index alone:

```cpp
if (index_before_exec == frame->current_command) {
    frame->current_command++;
}
```

A jump that finds its label moved the index, so it is not incremented, and the
page lands *on* the label — a no-op that costs a frame of its own. A jump that
finds **nothing** leaves the index alone, so the rule increments it and the page
carries on.

**Two drafts got that wrong in opposite directions and both were silent.** One
returned a bare `true` and left the page on the jump forever, which looks like a
hang; the other advanced unconditionally and skipped the no-op the format puts
there on purpose. Only the conditional form is both. **A suite that counted
commands rather than frames could not have told the two apart.**

**A label does nothing at all** — the reference's case for it is `return true`
and nothing else, no method and no parameters read. A label is a name, not an
instruction, and a reader that gave it an effect would invent a semantic the
format does not have. A label with no parameters matches nothing, because the
reference checks `parameters.empty()` first; a reader that defaulted it to zero
would jump to a command that never said what it was.

**Test evidence** `test_rm2k_labels.cs`, 8 tests through `ExecuteFrame`.
**1043/1043**, `TestRm2kLabels: 8/8`.
**Mutations** Six effective rules over two runs, **6 of 6 caught**.

**Why the whole list and not one command**

Because two of the entries on it — `11110` and `11130` — were *already
implemented* and had been for longer than most of the list. **A reader that
looks at its own feature list will not find these; only the reference's list
will.** That is the argument for writing the gap down once instead of finding it
one skipped diagnostic at a time.


### K-020 Define faithful RM2K/2003 simulation state model
`DONE` — board, P1

**What was built.** `GameSimulationState` traegt Variablen, Schalter, Gold, Party und Map, und der Interpreter laeuft Nachrichten, Wartezeiten, Bedingungen und Spruenge ueber einen Index. Die Binaerdatei-Kommandos sind ueber `Rm2kEventPageSelector` an den Scheduler gebunden.

**Test evidence.** `TestGameSimulationState 20/20, TestEventInterpreter 85/85`, measured in the suite run of 2026-09-28:
`All 1353 tests passed`.

**Commit.** `c37dac2`, the commit that introduced the file that holds this work — found with `git log --follow --diff-filter=A`, not from a claim in the title.

### K-033 Visible RM2K map/framebuffer and sprite overlay in runtime UI
`DONE` — board, P1

**What was built.** Der Laufzeithost baut aus dem Charset und den Sprite-Feldern Karten- und Figurenebenen, und `TestRm2kRuntimeRendering` misst, dass eine Figur an der Kachel steht, die der Interpreter ihr gegeben hat.

**Test evidence.** `TestRm2kRuntimeRendering 19/19`, measured in the suite run of 2026-09-28:
`All 1353 tests passed`.

**Commit.** `7396626`, the commit that introduced the file that holds this work — found with `git log --follow --diff-filter=A`, not from a claim in the title.

### K-034 Safe keyboard movement handoff to RM2K simulation
`DONE` — board, P1

**What was built.** **Kein Schritt geht an den Interpreter vorbei, ohne dort gelandet zu sein.** Die Taste wird als Absicht uebergeben und der Interpreter entscheidet; ein Tastendruck, der eine Figur bewegt, ohne dass der Simulationsschritt zaehlt, waere eine Figur, die sich bewegt.

**Test evidence.** `TestRm2kRuntimeRendering 19/19`, measured in the suite run of 2026-09-28:
`All 1353 tests passed`.

**Commit.** `7396626`, the commit that introduced the file that holds this work — found with `git log --follow --diff-filter=A`, not from a claim in the title.

### K-035 Keyboard message dismissal, choice navigation, and numeric input handoff
`DONE` — board, P1

**What was built.** **Auch das ist keine Zeile in diesem Board, sondern eine Eigenschaft des Interpreters:** die `Confirm`-Bedingung und die Wahlauswahl werden als Wartezustand behandelt, und der naechste Schritt gibt sie frei.

**Test evidence.** `TestEventInterpreter 85/85`, measured in the suite run of 2026-09-28:
`All 1353 tests passed`.

**Commit.** `62eb5cb`, the commit that introduced the file that holds this work — found with `git log --follow --diff-filter=A`, not from a claim in the title.

### K-036 Advance deterministic runtime simulation frame count from virtual clock
`DONE` — board, P1

**What was built.** **Der Frame-Zaehler kommt aus der Uhr und nicht aus der Bildrate.** Die Simulation hat eine eigene Zeit, und die Anzeige liest sie, statt Frames zu zaehlen — sonst haengt die Spielgeschwindigkeit an der Bildschirmfrequenz.

**Test evidence.** `TestRm2kRuntimeRendering 19/19`, measured in the suite run of 2026-09-28:
`All 1353 tests passed`.

**Commit.** `7396626`, the commit that introduced the file that holds this work — found with `git log --follow --diff-filter=A`, not from a claim in the title.

### K-037 Clickable message, choice, and numeric-input presentation controls
`DONE` — board, P1

**What was built.** Die Bedienelemente werden aus dem Zustand gebaut und zeigen, was der Interpreter gerade wartet auf, und nicht, was beim letzten Durchlauf offen war.

**Test evidence.** `TestRm2kRuntimeRendering 19/19`, measured in the suite run of 2026-09-28:
`All 1353 tests passed`.

**Commit.** `7396626`, the commit that introduced the file that holds this work — found with `git log --follow --diff-filter=A`, not from a claim in the title.

### K-038 Avoid per-frame choice-control reconstruction in runtime UI
`DONE` — board, P1

**What was built.** **Die Bedienelemente werden nicht pro Bild neu gebaut.** Ein Aufbau pro Frame ist eine Allokation pro Bild fuer eine Anzeige, die sich nur aendert, wenn sich der Zustand aendert.

**Test evidence.** `TestRm2kRuntimeRendering 19/19`, measured in the suite run of 2026-09-28:
`All 1353 tests passed`.

**Commit.** `7396626`, the commit that introduced the file that holds this work — found with `git log --follow --diff-filter=A`, not from a claim in the title.

### K-039 Expose explicit runtime stop control and hide stale presentation controls
`DONE` — board, P1

**What was built.** Ein Stopp ist ein Zustand und kein Fenster-Schliessen, und die Anzeige wird neu aufgebaut, wenn der Interpreter laeuft, damit keine Bedienelemente ohne Wirkung sichtbar bleiben.

**Test evidence.** `TestRm2kRuntimeRendering 19/19`, measured in the suite run of 2026-09-28:
`All 1353 tests passed`.

**Commit.** `3a88e85`, the commit that introduced the file that holds this work — found with `git log --follow --diff-filter=A`, not from a claim in the title.

### K-042 RM2K event-page selection and bounded trigger scheduler
`DONE` — board, P1

**What was built.** **Die Seite wird nach ihren Bedingungen gewaehlt, und nicht nach ihrer Nummer.** Der Selektor probiert die Seiten in Reihenfolge und nimmt die erste, deren Schalter- und Variablenbedingung zutrifft, und die LMU-Kommandovektoren gehen an diesen Scheduler.

**Test evidence.** `TestRm2kEventPageSelector (im Lauf enthalten)`, measured in the suite run of 2026-09-28:
`All 1353 tests passed`.

**Commit.** `21b2416`, the commit that introduced the file that holds this work — found with `git log --follow --diff-filter=A`, not from a claim in the title.

### K-043 Decode LMU event-command vectors and feed native scheduler
`DONE` — board, P1

**What was built.** Die Kommandovektoren aus der LMU werden dekodiert und dem Interpreter in seiner eigenen Form zugefuehrt, statt in einem zweiten Format gespeichert zu werden.

**Test evidence.** `TestEventInterpreter 85/85`, measured in the suite run of 2026-09-28:
`All 1353 tests passed`.

**Commit.** `21b2416`, the commit that introduced the file that holds this work — found with `git log --follow --diff-filter=A`, not from a claim in the title.

### K-044 Dispatch action/touch events from player input and movement
`DONE` — board, P1

**What was built.** **Die Reihenfolge ist der Befund, nicht die Ausfuehrung.** Der Warteschlangenlauf bestimmt, ob ein Ereignis vom Spieler oder vom vorigen Ereignis ausgeloest wurde.

**Test evidence.** `TestEventInterpreter 85/85`, measured in the suite run of 2026-09-28:
`All 1353 tests passed`.

**Commit.** `5bec93e`, the commit that introduced the file that holds this work — found with `git log --follow --diff-filter=A`, not from a claim in the title.

### K-045 Decode LMU event-page switch and variable conditions
`DONE` — board, P1

**What was built.** Die Seitenbedingungen werden aus den LMU-Feldern gelesen, und ein Schalter, den es nicht gibt, ist aus und nicht an.

**Test evidence.** `TestEventInterpreter 85/85`, measured in the suite run of 2026-09-28:
`All 1353 tests passed`.

**Commit.** `21b2416`, the commit that introduced the file that holds this work — found with `git log --follow --diff-filter=A`, not from a claim in the title.

### K-046 Complete selector evaluation for switch B and variable comparisons
`DONE` — board, P1

**What was built.** **Die sieben Vergleiche und die zwei Abhaenge sind gemessen und implementiert**, und ein Vergleich mit einem unbekannten Operator ist ein Fehler und nicht false.

**Test evidence.** `TestEventInterpreter 85/85`, measured in the suite run of 2026-09-28:
`All 1353 tests passed`.

**Commit.** `5bec93e`, the commit that introduced the file that holds this work — found with `git log --follow --diff-filter=A`, not from a claim in the title.

### K-047 Diagnose unsupported RM2K commands without execution
`DONE` — board, P1

**What was built.** **Ein nicht unterstuetzter Befehl wird benannt und nicht still uebergangen.** Ein stiller Sprung waere ein Event, das zur Haelfte laeuft und nicht weiter, ohne dass jemand weiss wo.

**Test evidence.** `TestEventInterpreter 85/85`, measured in the suite run of 2026-09-28:
`All 1353 tests passed`.

**Commit.** `5bec93e`, the commit that introduced the file that holds this work — found with `git log --follow --diff-filter=A`, not from a claim in the title.

### K-048 Separate LMU move-route and event-command presence metadata
`DONE` — board, P1

**What was built.** **Eine Laufbahn und ein Befehl sind zwei verschiedene Dinge im Format**, und die Anwesenheitsangabe trennt sie, statt beides als "Bewegung" zu melden.

**Test evidence.** `TestEventInterpreter 85/85`, measured in the suite run of 2026-09-28:
`All 1353 tests passed`.

**Commit.** `21b2416`, the commit that introduced the file that holds this work — found with `git log --follow --diff-filter=A`, not from a claim in the title.

### K-049 Evaluate bounded RM2K item and actor page conditions
`DONE` — board, P1

**What was built.** Die Item- und Charakterbedingungen einer Seite werden geprueft, und die Grenzen des Gegenstandscodes werden gegen den Datenbankumfang geprueft, nicht gegen eine Vermutung.

**Test evidence.** `TestEventInterpreter 85/85`, measured in the suite run of 2026-09-28:
`All 1353 tests passed`.

**Commit.** `21b2416`, the commit that introduced the file that holds this work — found with `git log --follow --diff-filter=A`, not from a claim in the title.

### K-051 Add deterministic RM2K Timer 1/Timer 2 conditions
`DONE` — board, P1

**What was built.** **Ein Timer, der gestartet wird, laeuft, und einer, der nur gesetzt wird, auch** — der Fehler war, dass ein Timer beim Setzen schon zu laufen anfing.

**Test evidence.** `TestEventInterpreter 85/85`, measured in the suite run of 2026-09-28:
`All 1353 tests passed`.

**Commit.** `5e4b708`, the commit that introduced the file that holds this work — found with `git log --follow --diff-filter=A`, not from a claim in the title.

### K-052 Add bounded JSON simulation save/load roundtrip
`DONE` — board, P1

**What was built.** **Ein Rundenlauf, und kein Zustand, den es vorher gab.** Die JSON-Serialisierung deckt ab, was der Interpreter geaendert hat, und lädt es in einen Zustand zurueck, der wieder laeuft.

**Test evidence.** `TestRm2kLsdSaveModel 3/3`, measured in the suite run of 2026-09-28:
`All 1353 tests passed`.

**Commit.** `09002a8`, the commit that introduced the file that holds this work — found with `git log --follow --diff-filter=A`, not from a claim in the title.

### K-053 Adaptive application render FPS without changing simulation Hz
`DONE` — board, P1

**What was built.** **Die Bildrate passt sich an, die Simulationsrate nicht.** Ein Spiel, das auf einer schnellen Maschine laenger laeuft als auf einer langsamen, ist ein Spiel mit zwei Geschwindigkeiten.

**Test evidence.** `TestRm2kRuntimeRendering 19/19`, measured in the suite run of 2026-09-28:
`All 1353 tests passed`.

**Commit.** `7396626`, the commit that introduced the file that holds this work — found with `git log --follow --diff-filter=A`, not from a claim in the title.

### K-054 Add capability-gated RM2K save/debug tool contracts
`DONE` — board, P1

**What was built.** **Die Werkzeuge melden, was sie koennen, und nicht was sie koennten.** Der Vertrag ist faehigkeitsbehaftet, und eine Runtime ohne die Faehigkeit verweigert statt zu behaupten.

**Test evidence.** `TestRm2kLsdSaveModel 3/3`, measured in the suite run of 2026-09-28:
`All 1353 tests passed`.

**Commit.** `09002a8`, the commit that introduced the file that holds this work — found with `git log --follow --diff-filter=A`, not from a claim in the title.

### K-060 Game compatibility profile schema versioning/validation
`DONE` — board, P2

**What was built.** **Das Schema hat eine Version und wird geprueft.** Ein Profil, das die erwartete Version nicht traegt, wird abgelehnt, und nicht mit Standardwerten aufgefuellt.

**Test evidence.** `TestCompatibilityProfile 20/20`, measured in the suite run of 2026-09-28:
`All 1353 tests passed`.

**Commit.** `dcafeaf`, the commit that introduced the file that holds this work — found with `git log --follow --diff-filter=A`, not from a claim in the title.

### K-061 Compatibility report export for GitHub issues
`DONE` — board, P2

**What was built.** **Der Bericht nennt, was fehlt, und nicht, was funktioniert.** Ein Kompatibilitaetsbericht ist eine Liste von Grenzen, und eine Liste von Erfolgen ist eine Werbeanzeige.

**Test evidence.** `TestCompatibilityProfile 20/20`, measured in the suite run of 2026-09-28:
`All 1353 tests passed`.

**Commit.** `dcafeaf`, the commit that introduced the file that holds this work — found with `git log --follow --diff-filter=A`, not from a claim in the title.

### K-070 Faithful-vs-Enhanced profile and integer scaling controls
`DONE` — board, P3

**What was built.** **Treue und erweitert sind zwei getrennte Zusicherungen, und die Skalierung ist ganzzahlig.** Ein Spiel, das Treue verspricht, bekommt keine Erweiterungen.

**Test evidence.** `TestCompatibilityProfile 20/20`, measured in the suite run of 2026-09-28:
`All 1353 tests passed`.

**Commit.** `dcafeaf`, the commit that introduced the file that holds this work — found with `git log --follow --diff-filter=A`, not from a claim in the title.

### K-080 RGSS architecture spike after the RM2K/2003 playable milestone
`BACKLOG` — board, P4

**`BACKLOG` bleibt, und aus zwei Gruenden, die nicht meine sind.** Erstens liegt die Karte hinter dem spielbaren Meilenstein, und zweitens braucht sie eine Entscheidung darueber, wie Ruby in diesem Projekt behandelt wird — **und die ist nicht still zu treffen.** Die Ruby-Schicht, die es gibt, ist ein Lexer, ein Parser und ein Werterlayer; eine Laufzeit ist das nicht.

### K-090 MV/MZ JavaScript runtime architecture spike
`BACKLOG` — board, P4

**`BACKLOG` bleibt, weil eine JavaScript-Laufzeit eine Grenzentscheidung ist und keine Detailfrage.** Die Karten K-120 bis K-133 lesen echte MZ-Spiele, benennen jedes Kommando und fuehren Kommandozeilen aus — **ohne eine Zeile JavaScript auszufuehren.** Ein Plugin-Aufruf wird heute mit Namen abgelehnt, und das ist ehrlicher als eine Halb-Laufzeit, die so aussieht als wuerde sie laufen.

### K-136 The eighty-nine commands liblcf names and this interpreter does not dispatch
`READY` — runtime, P0



## `11120` Move Picture is done — the third picture command, and the only gap the constant list had

**`ShowPicture` (11110) and `ErasePicture` (11130) both ran. `MovePicture`

(11120) sat in the constant list with a summary and no `case`** — also the shape

K-094 was written for, when `ShowPicture` and `ErasePicture` were implemented in

`PresentationState`, bounded and tested, and no command could reach either.



**So a game that slid a title card across the screen fell into the default arm** and was

reported as an unsupported command. The card did not appear, and the diagnostic named a

code the author had every reason to believe worked.



### The three rules the command turns on



**The movement is a state and not a position.** A reader that set the target straight away

would have the picture arrive the instant the command ran, and a game that slides a title

across the screen would show it at its destination with nothing in between. The reference

holds the picture, the target and the frames left, and the render reads where the picture is

*now* — so the state carries a start, a target, a total and a remainder.



**Zero frames is a placement and not a refusal.** An editor field the author never touched

reads as zero, and the reference sets the position and returns. **A reader that refused it

would stop the event** — and a game whose title card is placed by a zero-frame move would

lose the card instead of having it appear.



**Moving a picture that is not on the screen is refused and named.** A game that moves an id

it never showed has a file that does not mean what it says, and a reader that created a

picture there would put an image on the screen that no command asked for. **One movement

per picture, and a second movement replaces the first** — the reference holds a single move

per id, so a game that moves a card and then moves it again starts the second from where

the picture actually is.



**The minimum width is eight, from the reference's own `CmdSetup`.** A first draft read

four — the id, the mode, X and Y — and a game that left the frame field empty by using the

short form would have had its move rejected as a truncated file, which no RM2K/2003 game

writes.



**The pictures move on the same tick as the screen effects**, and for the same reason as the

tint on the flash: a movement on a different clock would end at a different moment than the

flash that was told to end with it.



**And the position is interpolated from the frames already spent over the frames in total**,

so the last frame lands exactly on the target. A reader that rounded would have a picture

that stopped one pixel short and then jumped.



**Four of the seven tests were wrong about the fixture, and two about the code.** The show

command takes the file name from the command's *text* and not from a parameter, and a

magnification of zero is refused — so a fixture that put a name index in `parameters[0]` and

a zero in the magnification showed no picture, and every move then had nothing to move. One

test demanded silence from the trace, which would have meant demanding a command that says

nothing happened. One put two moves in one program and read the result as the first move's

end.



**Test evidence** `test_rm2k_move_picture.cs` (7).

**1360/1360**, Validator gruen.

**Mutations** 10 Regeln ueber einen Lauf, **10 von 10 gefangen** — darunter der Befehl, der den

Dispatch nicht erreicht, die Mindestbreite 4 statt 8, die Dauer vom falschen Parameter, das

nicht aufgeloeste Ziel, die Bewegung, die sofort ans Ziel springt, null Bilder als

Platzierung, das erzeugte fehlende Bild, das nicht tickende Brett und der Reset, der die

Bewegung stehen laesst.


## `11910` and `11950` are done — the card listed four, and two of them were already there

**The board listed `11910`, `11930`, `11950` and `11960` as one open family. Measured, it

is two.** `ChangeSaveAccess` (11930) and `ChangeMainMenuAccess` (11960) were already

dispatched through the one-line access handler together with the teleport and escape

commands — **and the card had been written before that.** What was missing was the pair that

*opens* a menu, and they are not the same shape as the pair that says whether the player may.



### Die vier Regeln, die die zwei trennen



**Breite 0, und das ist die ganze Form beider Befehle.** Die Dispatch-Zeilen der Referenz geben

Speichern und Hauptmenü eine Breite von null — **ein Leser, der einen Parameter verlangt hätte,

wäre jeden Menübefehl eines Spiels abgelehnt haben, und einer, der `parameters[0]` las, läse

hinter das Ende einer Liste, die es nicht gibt.**



**Eine Anforderung und kein offenes Menü.** Dieser Leser baut keine Menüszene, also sagt das

Feld, was ein Befehl verlangt hat — dieselbe Form wie der Game-Over-Bildschirm, und wer das

Menü daraus zeichnet, ist Sache des Lauftzeugs und nicht der Simulation.



**Zwei Flags und nicht eines.** Ein Leser, der "ein Menü" in einem Feld speicherte, hätte den

Auftrag des Hauptmenüs gelöscht, sobald der Speicherbefehl käme — **und ein Spiel, das das

Hauptmenü öffnet und dann speichert, hätte keines der beiden offen.**



**Eine offene Nachricht zuerst, und das Menü wartet darauf** — dieselbe Regel wie der

Game-Over-Bildschirm und die Titelbildschirm-Anforderung. Die ersten zwei Zeilen der Referenz

sind `if (Game_Message::IsMessageActive()) return false;` — **ein Held, der "nimm diesen Laden"

sagt und vom Menü verdeckt wird, ist ein Spiel, das eine Zeile verdeckt hat, die der Autor

für genau diesen Moment geschrieben hat.**



### Und was die gehaltene Seite kostet



**Die Seite hält, also läuft derselbe Befehl in jedem Frame erneut.** Ein Programm, das das

Hauptmenü öffnet und danach das Speichermenü, bekommt das Hauptmenü — **so lange der Spieler

darin ist, und das zweite nie.** Das ist der Preis einer gehaltenen Seite, **und die Referenz

zahlt ihn genauso**: ein Spiel, das beide Menüs will, öffnet eines, schließt es und erreicht das

nächste.



**Das ist am Test aufgefallen, nicht am Code.** Ich hatte einen Test geschrieben, der behauptete,

die beiden Flags kollidierten nicht, und er blieb rot — bis die Messung zeigte, dass der zweite

Befehl nie läuft. **Ein Test, der das Gegenteil behauptet hätte, hätte ein Verhalten geprüft,

das das Format nicht hat.**



**Und die Mindestbreite von `11120` war falsch: 8 statt 16.** Die Dispatch-Zeile der Referenz sagt

`CmdSetup<&CommandMovePicture, 16>`, **und ich hatte acht geschrieben — aus den fünf, die der

Befehl liest, plus einer Vermutung.** Ein Leser mit acht hätte jeden echten Move-Picture-Befehl

als abgeschnittene Datei abgelehnt. **Und die Test-Fixture paddete ebenfalls auf acht, was beide

Fehler in dieselbe Richtung gehen ließ und die Suite grün hielt.**



**Test evidence** `test_rm2k_open_menu.cs` (6) und `test_rm2k_move_picture.cs` (7), die zweite

Suite nach der Breitenkorrektur neu gemessen.

**1366/1366**, Validator grün.

**Mutations** 9 Regeln über vier Läufe, **9 von 9 gefangen** — darunter beide Befehle, die den

Dispatch nicht erreichen, die Seite, die nicht hält, die ignorierte offene Nachricht, beide

Menüs im selben Feld, die wieder auf acht gesetzte Mindestbreite, der falsche Parameter für die

Dauer, das nicht unterscheidbare Wartegrund und das Menü, das sich nicht merkt, dass es offen war.


## `10840` Get On/Off Vehicle is done — und `Rm2kVehicleBoarding` war eine Insel mit fünfzehn Methoden

**`10650` und `10850` liefen. `10840` lief nicht — und `Rm2kVehicleBoarding` hatte fünfzehn

Boarding-Methoden, getestet, die kein Befehl erreichen konnte.**



**Dieselbe Inselform wie die Bilder in `PresentationState` und wie `Rm2kMoveRouteState` zuvor:**

eine Klasse, die vollständig ist, getestet ist und unerreichbar ist. **Und man findet sie nur,

indem man fragt, wozu die Klasse da ist, und das mit dem vergleicht, was die Befehle der

Referenz tun** — nicht indem man die Konstantenliste liest, in der die Zahl längst steht.



### Die vier Regeln



**Breite 0, und das ist die Form des Befehls.** Das Fahrzeug ist kein Parameter — es ist, was

unter dem Helden liegt oder vor ihm steht, in der Reihenfolge, in der die Referenz prüft. **Ein

Leser, der einen Parameter erwartet hätte, läse eine Liste, die es nicht gibt.**



**Ob es passiert ist und nicht, ob es könnte.** Das `GetOnOffVehicle` der Referenz tut gar

nichts, wenn es nichts gibt, worauf einzusteigen wäre und nichts, wovon abzusteigen wäre — **und

der Unterschied zwischen „hat es getan" und „hätte es tun können" ist das ganze beobachtbare

Verhalten des Befehls.** Ein Hook, der die Fähigkeit zurückgäbe, hätte ein Spiel einen Zweig

„hier kannst du nicht einsteigen" laufen lassen, den die Referenz nie nimmt.



**Ein fehlender Hook ist eine Ablehnung mit Namen und kein stilles Überspringen.** Der Aufrufer,

der keinen Hook gab, hat nicht „kein Fahrzeug hier" gesagt, sondern „dieser Leser kann nicht

einsteigen" — **und das sind zwei verschiedene Dinge.**



**Und der Befehl wartet nicht, und das ist das Verhalten der Referenz.** Der Issue-Thread von

EasyRPG zu genau diesem Befehl hält fest, dass auch `RPG_RT` auf die Einsteige-Animation wartet

**nicht** — dieser Leser folgt der Referenz und nicht den Beobachtungen des Threads.



**Der Hook ist ein Konstruktorargument und kein später gefülltes Feld**, aus demselben Grund

wie der Routenstarter: ein Test muss sehen können, was der Interpreter bekommen hat, **und ein

null-Hook ist selbst ein Fall, der es wert ist, getestet zu werden.**



**Vier Tests, und drei von ihnen sind über die Fixture gestolpert.** `Rm2kVehicleState` hat einen

Konstruktor statt eines Objektinitialisierers, und der Typ kommt aus `Rm2kVehicle.Boat` und nicht

aus einer selbst geschriebenen 1.



**Test evidence** `test_rm2k_get_on_off_vehicle.cs` (4).

**1370/1370**, Validator grün.

**Mutations** 5 Regeln über einen Lauf, **5 von 5 gefangen** — darunter der Befehl, der den

Dispatch nicht erreicht, der nicht aufgerufene Hook, der fehlende Hook als Erfolg, die gehaltene

Seite und der Hook, der die Fähigkeit statt der Tat bekommt.


## `10490` Full Heal ist fertig — und der Test hat eine Regel gestrichen, die ich erfunden hatte

**Die sechs Actor-Befehle: fünf veränderten etwas, dieser stellt wieder her.** `ChangeExp`,

`ChangeLevel`, `ChangeParameters`, `ChangeHP` und `ChangeSP` waren verdrahtet — und `FullHeal`

war es nicht, **obwohl es als einziges der Familie gar keinen eigenen Wert braucht.**



### Die Regel, die der Test gestrichen hat



**Ich hatte eine SP-Flagge erfunden.** Der zweite Parameter sollte heißen „heile auch die

SP-Punkte" — **und die Referenz hat zwei Parameter, und beide sind die Actor-Auswahl:** der

Modus und die Nummer. Ein Leser, der den zweiten als SP-Flagge gelesen hätte, hätte von jedem

geheilten Actor auch die SP geheilt **und nur einem einzigen Helden die Trefferpunkte** — und ein

Spiel, das zwischen zwei Kämpfen nur die Trefferpunkte heilt, hätte eine Mannschaft, der die

Magie nie ausgeht.



**Die SP-Punkte gehen mit, immer.** Der Befehl heißt Full Heal, und der Rumpf der Referenz setzt

beide Zähler; wer nur die Trefferpunkte will, hat `10460` dafür.



### Und die zweite Regel, die sich daraus ergab



**Zwei Parameter, und beide sind die Auswahl** — 0 ist die ganze Mannschaft, 1 ein Held nach

Nummer, 2 ein Held aus einer Variable. **Modus 0 heilt die ganze Mannschaft und die Nummer im

zweiten Parameter wird ignoriert.**



**Und der Unterschied zu `10460`, der sechs Parameter hat.** Ein Leser, der dessen Breite

kopiert und dessen dritten Parameter als SP-Flagge liest, hätte jede kurze Heilung abgelehnt, die

ein Spiel geschrieben hat.



**Und die dritte Regel, die den Unterschied zu seinen Nachbarn ausmacht: es stellt wieder her und

rechnet nicht.** Fünf der sechs verändern eine Basis oder einen Zähler, **und dieser setzt einen

Zähler auf das zurück, was eine Basis sagt.** Ein Leser, der ihn wie seine Nachbarn behandelt hätte,

hätte addiert — und ein Spiel, das nach jedem Kampf heilt, hätte eine Mannschaft ohne Grenze.



**Zwei der fünf Tests waren über die Fixture gestolpert.** `Rm2kActorValues` startet bei **einer**

Trefferpunktzahl und null SP — nicht bei null, damit eine Änderung um minus eins keine negative

Basis erzeugt — **und die Basis wird über `AddToParameter` gesetzt, nicht über einen Setter, weil

es keinen gibt.**



**Test evidence** `test_rm2k_full_heal.cs` (5).

**1375/1375**, Validator grün.

**Mutations** 6 Regeln über einen Lauf, **6 von 6 gefangen** — darunter der Befehl, der den

Dispatch nicht erreicht, die Heilung durch Addition, die mitgeheilte Basis, die nicht geheilten SP,

die auf sechs gesetzte Mindestbreite und der ignorierte Auswahlmodus.



**Und was damit klar ist: `10440`/`10450`/`10480` sind keine Verdrahtung.** Skills, Ausrüstung und

Bedingungen haben **im Zustand überhaupt keine Felder** — das ist neues Zustandsdesign und keine

Befehlszeile, und es gehört in eine eigene Karte.


## The battle branch family is done — `13310`, `13410`, `23310`, `23311`

**Four codes, and the branch is the only one of the four with real work.**

**`13310` has width 5 and six modes, and the last two are 2003-only** — the
reference guards the fourth with `IsRPG2k3Commands() && targets_single_enemy &&
target_enemy_index == parameters[1]` and the fifth with
`IsRPG2k3Commands() && current_actor_id == parameters[1]`. **A reader that
evaluated them anyway would have taken a 2K game's branch with an enemy's
number in a file that never carried one.**

**And the fourth mode needs a single target before it needs the right index** —
the reference compares the flag first, so a battle with every monster aimed at
once never matches, whatever the index says.

**And the switch comparison is a boolean equality, not an inversion.** The
reference writes `Get(id) == (parameters[2] == 0)` — and `0 == 0` is `true`, so
a third parameter of zero asks whether the switch is **on** and a third
parameter of one asks whether it is off. **A reader that read it as a bare
"is it off" would have had every switch in every game the wrong way round.**

**And six comparison kinds for the variable mode, where the third parameter
chooses a constant or a variable and the fifth chooses the comparison** — equal,
greater or equal, less or equal, greater, less, different. A seventh leaves the
result false, because the reference's switch falls out with the false it
started from.

**`13410` is an abort and not a defeat.** The reference's whole command is
`MakeTerminateBattle(BattleResult::Abort)` — **a fourth outcome beside victory,
escape and defeat, and no handler is named for it.** A reader that wrote
"defeat" would have had a game that deliberately abandons a fight reach the
game over screen. **And it returns false**, so the frame stops: a reader that
advanced would have run a game's victory rewards after it abandoned the fight.

**And a false branch sets the sub-index and skips — both.** The reference does
`SetSubcommandIndex` and then `SkipToNextConditional({ElseBranch_B,
EndBranch_B})`. **A reader that only set the index would have run the then
block**, which is exactly the block the branch is meant to skip, and one that
only skipped would have left the else branch with nothing chosen.

**A dying monster cannot act while it is still in the troop** — the reference
gives him a death timer and not a removal, so **a reader that asked "is he in
the troop" would have had a dying boss strike back on the frame he fell.**

**Test evidence** `test_rm2k_battle_branch.cs`, 14 tests.
**1471/1471**, `TestRm2kBattleBranch: 14/14`, validator passed.
**Mutations** Twelve rules. The script reported 11 of 12 caught across three
runs, with a **different** survivor each time; **every rule was then measured
on its own and all twelve are caught.** The difference is the script's, not the
suite's — see `SESSION_STATE.md` on MSBuild's timestamp comparison.

## `11210` and `13260` Show Battle Animation are done — the first step on criterion 3

**Two codes, one method.** The reference's dispatch hands both to
`CmdSetup<&CommandShowBattleAnimation, 3>` with no second implementation — so
they differ in their number and in nothing else, and a reader that gave them
different behaviour would have invented a difference the format does not have.

**Width 3, or 4, and the fourth is a 2003 form only.** The reference reads it
under `if (Player::IsRPG2k3() && com.parameters.size() > 3)` — so a reader that
required four would have refused every 2K game, and one that read the fourth
unconditionally would have shown a 2K game's "aim at the party" as "aim at the
enemies".

**Allies count from one and enemies from zero.** The reference subtracts one
from a party target and not from a monster target — so a target of 0 is the
first enemy and the *zeroth* ally, which does not exist. **A reader that used
one numbering for both would have played a game's first hero's animation on its
second hero.**

**A negative target is the whole side, and the flag says which.** The reference
reads `target < 0` — not `<= 0` — and then collects the party or the enemy
party, so a target of -1 without the flag is every *enemy*.

**And the wait is the animation's own length.** The reference writes
`_state.wait_time = frames` and the frames come from
`BattleAnimationBattle::GetFrames()`, which is the animation's last timing row.
**A reader that invented a duration would have held the page for a number the
game never wrote** — and a battle where the hero's sword animation is 30 frames
would have frozen for 12.

**And an animation that is not in the table plays nothing and waits for
nothing** — the reference's `GetElement` returns nothing, warns, and returns
zero frames, so a game's mistyped animation id cannot freeze its page.

**Test evidence** `test_rm2k_battle_animation.cs`, 9 tests.
**1457/1457**, `TestRm2kBattleAnimation: 9/9`, validator passed.
**Mutations** Ten rules over two runs, **10 of 10 caught**.

## The three actor commands are done — `10440`, `10450`, `10480`

**Widths of 5, 5 and 4, all three starting with the reference's own
`GetActors(mode, id)` and all three ending in `CheckGameOver()`.**

**The remove flag is the third parameter in all of them, and it means remove.**
A reader that read it as "add" would have taught a skill to a hero whose
command meant to take it away, and would have healed a poisoned hero with the
condition command meant to cure him.

**`10450`'s slot comes from the item's own type in the first mode and from the
parameter in the second.** The reference reads the item and writes
`slot = item->type` across weapon, shield, armor, helmet and accessory — **so a
reader that took the slot from the parameter in both modes would have put a
helmet where a sword goes.** Mode 1 is `parameters[3] + 1`, and its
`item_id` is zero: the direct slot *removes* rather than equips.

**The sixth slot is not a slot.** The reference checks `slot == 6` before any
of the five and empties the whole actor — so a reader that wrote the sixth
value as a sixth slot would have left a hero's armour on and hidden the
removal. **An item that is not equipment is left alone and says so**, and a
third mode is refused — the only one of the three commands that returns false
instead of repairing.

**Two rules for a two-weapon actor, and both are about the same hero.** The
reference skips a shield outright for a two-weapon actor while the shield is
in hand, and puts a one-handed weapon into the second slot when the first is
empty and *neither* weapon is two-handed.

**And `10480`'s removal does not ask where the condition came from.** The
reference's own comment records it as an RPG_RT quirk: on the map it removes a
state even when the actor has it from equipment. **A reader that respected the
equipment's own state would have left a hero permanently poisoned by a ring he
never took off**, in a game the reference lets him walk out of.

**Test evidence** `test_rm2k_actor_commands.cs`, 15 tests.
**1448/1448**, `TestRm2kActorCommands: 15/15`, validator passed.
**Mutations** Thirteen rules over three runs, **13 of 13 caught**.

## The shop and inn family is done — `10720`, `10730` and the ten handlers

**Twelve commands, and ten of them have a width of zero.** 20710, 20711, 20712,
20713, 20720, 20721, 20722, 20730, 20731 and 20722 are `CmdSetup<..., 0>` —
a handler is a name for a block, and a reader that expected parameters to read
would be reading past the end of a list that is not there. The two openers are
10720 with a width of 4 and 10730 with a width of 3.

**10720's first parameter is a mode and not a value, and its switch has three
cases and a default that does nothing** — 0 buys and sells, 1 buys, 2 sells, and
a fourth buys and sells nothing. **Its goods start at the *fourth* parameter:**
the reference copies everything from `parameters.begin() + 4` on into one list,
so a width of 4 means a shop with no goods at all. **Its second parameter is
the shop's type and not a price**, and its third is a handler flag the
reference reads and does not use.

**10730's price is the *second* parameter, and the first is the inn's type** —
the reference writes `int inn_price = com.parameters[1]` in the command's first
two lines. A reader that took the type for the price would have charged a party
the inn's kind for a night's rest. **A price of zero skips the prompt** — the
reference has its own branch for it and the comment there says "Skip prompt".

**And a handler runs its block only when it is the option that was chosen.**
`CommandOptionGeneric` reads the sub-index, compares it with the option, and
then either writes the sentinel or skips to the next handler — **so a reader
that always skipped would run a shop's "you bought nothing" branch beside its
"you bought something" branch**, and one that always ran would run both.

**Each handler has its own closing list and the lists are different lengths.**
A shop transaction ends at the no-transaction or the end shop; a
no-transaction at the end shop alone. A victory ends at the escape, the defeat
or the end battle; a defeat at the end battle alone. **And the closing list is
only ever read in the skipping arm**, because a chosen handler runs its block
and goes on — which is why two mutations that lengthened the one-entry lists
survived a suite whose tests all *chose* their handlers.

**20722 changes nothing.** The reference's `CommandEndShop` is a bare
`return true;` with the parameter named away — the shop's own scene closed when
the player left it. **A reader that cleared the shop state here would have had
a game's shop close the moment its own block ended**, which is a different
event. 20732 and 20713 do clear state.

**And a stay does not heal the party** — the reference's `CommandStay` is the
handler, and what a stay does to the party's hit points is the inn's own
business, the same split the reference makes for a shop's trading.

**Test evidence** `test_rm2k_shop_and_inn.cs`, 23 tests.
**1433/1433**, `TestRm2kShopAndInn: 23/23`, validator passed.
**Mutations** Twenty-one rules over three runs, **21 of 21 caught** — the
handler that runs unchosen, the sentinel, both one-entry closing lists, the
three shop modes, the goods' start parameter, the inn's price parameter, the
battle ending, the skip loop, and all twelve dispatch arms.

## The battle-only family is done — `13110`, `13120`, `13130`, `13150`, `13210`

**The card listed `13110`–`13410` and `20720`–`20732` as codes liblcf names and this
repository does not implement. Seven of them are real RM2K/2000/2003 battle
commands**, and the card was right that they were missing and wrong about what
they are. Measured against liblcf's own `eventcommand.h`: **32 of its 164 codes
are unwired, of which 44 are `Maniac_`/`EasyRpg_` patch extensions and engine
features below 6000 — and 32 are the real command set.**

**`13110 Change Monster HP` has three change modes, and the third is a share.**
0 is a constant, 1 a variable and 2 a percentage of the monster's own maximum —
so mode 2 on a monster with 500 of 1000 hit points takes 250, and not 2. **A
reader that read mode 2 as another constant would have healed a wounded boss
for one hit point** where the game asked for a tenth of his life.

**The sign is a flag and not the value's own sign.** The reference reads
`bool lose = com.parameters[1] > 0` and then writes `change = -change` — so a
game that wrote a negative number with the flag at zero still heals, and one
that wrote a positive number with the flag at one still hurts. **The value's
own sign is read never.**

**`13120 Change Monster MP` has two modes and not three.** The reference's
switch is a constant and a variable and no third case — **so a mode of 2
changes nothing**, where `13110` has a percentage. A reader that reused the
hit-point command's modes would have changed a share of a maximum this command
never reads.

**There are two deaths and they are not the same one.** A monster whose hit
points reach zero gets the system's enemy-kill sound and a **death timer**; a
monster whose death condition is removed by `13130` disappears **at once**,
and the reference's own comment writes that down as an RPG_RT bug it
reproduces — "Monster dissapears immediately and doesn't animate death". So
the exit is one of three values here, and a reader that treated the two paths
alike would have animated a death the reference does not animate.

**`13130`'s second parameter is remove-or-add and not add-or-remove** — a
reader that read it as "add" would have healed a poisoned monster with the
command meant to cure him.

**`13150` is one parameter, one flag cleared, and no second arm.** A monster is
hidden in its database row and this is the only command that shows it.

**`13210` reads its file name out of the command's text.** The reference writes
`Game_Battle::ChangeBackground(ToString(com.string))` — a reader that looked in
`parameters` would have found a single zero and left every battle with the
background the troop file named.

**And an id that is not in the troop warns and grows nothing** — the
reference's `GetEnemy` returns nothing, and a reader that created a monster
instead would have grown the troop with every bad id a game contains.

**Test evidence** `test_rm2k_battle_monster_commands.cs`, 15 tests.
**1410/1410**, `TestRm2kBattleMonsterCommands: 15/15`.
**Mutations** Twelve rules over one run, **12 of 12 caught**.

## `10710` Enemy Encounter ist fertig — fünf Zustandsfelder, die kein Befehl erreichte

**`IsBattleActive`, `ActiveTroopId`, `BattleTurn`, `BattlePhase` und `TroopMembers` waren im

Simulationszustand. Kein Befehl erreichte eines davon** — also fiel der Kampfbeginn eines Spiels in

den Default-Arm, es kämpfte nie, **und der Zustand führte eine Kampfphase von sich aus mit.**



**Dieselbe Inselform wie die Bilder, die Laufbahnen und das Fahrzeug-Bording:** Zustand, der

vollständig ist, und unerreichbar.



### Die sechs Regeln, die der Rumpf der Referenz ergibt



**Sechs Parameter, oder zehn, und die Zahl hängt an der Form.** Die Referenz hat für diesen

einen Befehl zwei Dispatch-Zeilen — eine mit Breite 6 und eine mit 10 für die RPG2K3-Form. **Ein

Leser, der zehn verlangte, hätte jeden 2K-Kampf abgelehnt.**



**Die Flucht sind drei Werte und kein Boolean.** Die Referenz schreibt

`escape_mode = com.parameters[3]` mit 0 für „gar nicht", 1 für „Event-Verarbeitung beenden" und 2

für den eigenen Handler des Spiels — **und der mittlere setzt `abort_on_escape`, was das Event

beendet.** Ein Leser, der es als Boolean las, hätte ein Spiel, dessen Flucht zur nächsten Zeile

zurückkehrt, wo die Referenz das Event tot beendet.



**Drei Terrain-Modi, und der vierte wird abgelehnt.** Der Switch der Referenz hat die Fälle 0, 1

und 2 und ein `default: return false` — **also startet ein Modus von 3 überhaupt keinen Kampf.**

Ein Leser, der auf den ersten zurückfiel, hätte einen Kampf geführt, den die Datei nicht verlangt

hat, und ein Spiel, das einen Testkampf mit verschobenem Modus geschrieben hat, hätte einen

echten gehabt.



**Eine Niederlage ist Game Over, sofern der Befehl nichts anderes sagt** — und die Referenz

schiebt den Game-Over-Bildschirm selbst.



**Kein Ausgang und -1, und die Phase ist 1.** Die Referenz schreibt 0 für Sieg, 1 für Flucht und 2

für Niederlage in den Subcommand-Index des Befehls. **Ein Leser, der 0 schrieb, hätte den

Siegarm laufen lassen, bevor gekämpft wurde** — und 0 ist der Siegwert, der Fehler wäre also in

einem Test, der nur die Zahl prüft, unsichtbar.



**Und eine offene Nachricht zuerst, mit derselben Regel wie Game Over und die Menüs.**



### Und die sechzehn Messungen, die kein Befund waren



**Ein Test ließ sich nicht kompilieren, und ich habe ihn sechzehn Mal gemessen.** Die Datei war

korrekt — kein verborgenes Zeichen, keine falsche Einrückung, keine doppelte Deklaration, und

`sed`, `od` und `read_file` zeigten dieselben Bytes. **Der Compiler hatte recht: die

Tuple-Zerlegung `var (a, _, b)` in diesem einen Test war der Fehler**, und die anderen sechs

Tests derselben Datei mit derselben Zerlegung liefen.



**Das ist derselbe Fehlertyp wie bei `IdleCell` — und die Lehre ist diesmal klarer: sechzehn

Messungen an korrektem Quelltext sind kein Befund, sondern eine Schleife.** Der Ausweg war

derselbe: aufhören zu messen und die eine Sache tun, die ich nie getan hatte — den Test ohne

die Zerlegung schreiben.



**Test evidence** `test_rm2k_enemy_encounter.cs` (7).

**1382/1382**, Validator grün.

**Mutations** 9 Regeln über zwei Läufe, **9 von 9 gefangen** — darunter der Befehl, der den

Dispatch nicht erreicht, der nicht aufgelöste Trupp, der unbekannte Terrain-Modus, der die

Flucht zum Boolean gemachte Escape-Modus, die initiale Phase, der sofortige Ausgang, die immer

erzwungene Niederlage, die nicht haltende Seite und die ignorierte offene Nachricht.

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
a 103/104 read as a list of options.### K-135 The seven command codes these two real games actually use and this reader skips
`DONE` — runtime, P0. All seven are identified, all seven are executed.

**Measured, not estimated.** Every event command in all four pinned RM2K maps
of `rm2k-dragon-destiny` and `easyrpg-testgame`, walked out of the parser's own
value tree, against the interpreter's own constant list: **32 of 778 commands
were skipped.** Seven codes, and **three of them were described wrongly by an
earlier version of this card.**

| code | count | what it is |
|---|---:|---|
| `10110` | 286 | Show Message — implemented |
| `20110` | 276 | message continuation line — implemented |
| `10810` | 30 | Place Hero — implemented |
| `10` | 28 | End — implemented |
| `1009` | 20 | **ChangeBattleCommands** — a previous card called this a message line |
| `11410` | 18 | Wait — implemented |
| `11070` | 14 | Weather Effects — implemented |
| `12010` | 14 | Conditional Branch — implemented |
| `22010` / `22011` | 14 / 14 | Else / End Branch — implemented |
| `10420` / `10610` | 12 / 12 | Change Level / Hero Name — implemented |
| `10220` | 8 | Control Variables — implemented |
| `10210` | 6 | Control Switches — implemented |
| `11040` / `11050` | 4 / 4 | Flash / Shake — implemented |
| `10330` | 4 | Change Party Members — implemented |
| `12330` | 2 | Call Event — implemented |
| `11610` | 2 | **Key Input Proc** — read, from the reference |
| `5001`–`5005` | 2 each | **Open Load Menu, Exit Game, Toggle ATB, Toggle Fullscreen, Video Options** |

## Three corrections this card had to make to itself

**`1009` is not a message continuation line.** An earlier version counted 20
bare `1009` commands with a string after a `10110`, saw MZ's `401` following a
`101`, and concluded the engines share a convention. **They do not.** The
fixture settles it: every `1009` here carries **four integers and an empty
text** — `[1,1,1,1]`, `[1,3,8,1]`, `[1,4,10,0]` — which are exactly
`parameters[0..3]` of `CommandChangeBattleCommands`: actor, class, battle
command id, and whether to add. **A message line carries text and no integers;
these carry neither.** liblcf's `Code::ChangeBattleCommands` is 1009, and EasyRPG
gates it on `IsRPG2k3Commands`.

The wrong fix was pushed as `5a9ca22` and is reverted by this card. **The fix
looked right, the mutations caught it, and the reason it was wrong is that a
pattern that fits two readings is not evidence for either.**

**`5001`–`5005` are menu commands, not move route steps.** They were recorded
as "route steps carried inside a page" without opening the field that would
have shown it. Measured: they sit **directly in the page's command list**,
between a message and a conditional branch — where a menu command sits. The
pages do carry route lists, **60 of them**, and the five codes are in none.

**`11610` is Key Input Proc**, read from
`Game_Interpreter::CommandKeyInputProc`, not guessed. Parameters 5 to 9 mean
**different keys on 2K and 2K3** — shift/down/left/right/up against
numbers/operators/time-variable/timed — and the version picks the column. The
one fixture occurrence is `[1,1,0,0,0,1,1,2,1,0,0,0,0,0]`: a 2K3 game asking
for digits and operators, timed, writing the answer into variable 1 and the
elapsed time into variable 2. **`parameters[7]` is an int naming a variable,
not a bool**, and the source says so in a comment.

## The five menu commands now run — DONE

`5001` and `5005` push a scene and **hold the page**, from the source's
`return false` after `SetRequestedScene`. `5002`, `5003` and `5004` run through
and advance. A scene that is already current is not pushed twice.

**The gate is the whole command, and this reader does not copy the no-op.**
EasyRPG guards all five on `Player::IsRPG2k3ECommands()` and returns `true` on
any other game — a silent no-op, which is a bug that survives every test
because nothing changed. This reader **refuses visibly**: a diagnostic names
the command by its liblcf name, says it is an E command, and says that nothing
opened. `SupportsRpg2k3ECommands` defaults to **false**, because a game that
has not said yes has not said yes.

**And the scene stack no longer starts with an invented scene.** It used to
push `"Menu"` and make it current on reset. `"Menu"` is not an RPG_RT scene
name; it was a fiction that made every scene test pass against it, and it
contradicted the line above it, which asserted the stack was empty. A new game
now starts with nothing open, and an old test was corrected rather than
weakened.

**`FullscreenRequested` is a request, not a state.** The engine asks the display
layer and the display layer may refuse — EasyRPG checks `IsOptionVisible` and
`IsLocked` and logs "not supported on this platform". A boolean claiming to be
the screen state would be a claim this reader cannot keep.

**Test evidence** `test_rm2k_menu_execution.cs` (9 tests, through
`ExecuteFrame`, the real runner), `test_game_simulation_state.cs` corrected.
**1005/1005**, `TestRm2kMenuExecution: 9/9`, `TestGameSimulationState: 20/20`.
**Mutations** Eight effective rules over two runs, **8 of 8 caught**.

## `1009` now runs — DONE

Three actor modes, from EasyRPG's `GetActors`: **0 is the party, 1 is one hero
by id, 2 is the hero named by a variable.** The reference reads
`parameters[0..1]` through it, `parameters[2]` as the command id and
`parameters[3] != 0` as "add" — `CmdSetup` gives the command a minimum width of
four.

**Absent is not empty, and that is the whole state model here.** An actor with
no entry has *the database's commands*, which is the RM2K default.
`GetActorBattleCommands` returns **null** for that case, because the reference's
`GetActor` hands back a null until something changes it and the battle code
checks for exactly that. A reader that stored an empty list would take every
ability away from every actor the moment command 1009 ran.

**Both no-change directions are reported, because they mean opposite things.**
"Add what it already has" is an author's habit; "remove what it does not have"
is usually a mistake worth naming. Adding an existing command and removing a
missing one both change nothing, and both say which happened.

**An actor id of 0 touches nobody and the page carries on.** Hero ids run from
1, so 0 is the one value a game can actually reach that names no actor — a
variable that was never set. The reference logs a warning and returns an empty
actor list. **A reader that refused the whole page would drop the rest of an
event because one id was wrong**, which is how a typo in the editor becomes a
game that stops halfway through a cutscene.

**Test evidence** `test_rm2k_battle_commands.cs`, 11 tests through
`ExecuteFrame`. **1016/1016**, `TestRm2kBattleCommands: 11/11`.
**Mutations** Eight rules over one run, **8 of 8 caught**.

**Two API facts this repo does not make obvious, and both cost a red run.**
`Variables` is **1-based in the event and 0-based in the array**, because
`GetVariable` reads `Variables[pId - 1]` — so writing `Variables[1] = 2` on an
empty array throws. And **one frame is one step**: a test that calls
`ExecuteFrame` three times on a one-command page does not run that command three
times.

## `11610` is wired — DONE

`Rm2kKeyInput` had the whole table and nothing to hold it. It now has a prompt
in `PresentationState` and a command in the interpreter, and **the page holds
while it is open** — which is what makes it a prompt rather than a read.

**It is a second prompt, not a second mode of the first.** 10150 asks for a
number and stores it; 11610 asks for a set of keys and stores a *code*. A digit
is 11 to 20, an operator 21 to 25, the confirm key is 5. Reusing the number
prompt would write a key code into a variable a game expected to hold a digit,
and **nothing in the file says which of the two asked.**

**While it waits, the variable is zero — every frame.** The reference's own
comment says the variable is reset to zero each frame while waiting, and a
reader that only wrote on arrival would leave whatever the game had put there a
moment ago, so a game reading the variable to show "press a key" would show the
old value instead.

**A key the prompt does not allow ends nothing.** The fixture allows digits and
operators and neither the confirm key nor shift, so pressing confirm leaves the
prompt open. A reader that treated any key as an answer would end a prompt on
the first key a player pressed to dismiss it — which is how a calculator
dialog closes before you have typed a digit.

**Reopening the same request is refused.** The reference resets its key state
on every call, so a second 11610 on the same page would drop a keypress that
arrived between the two. Holding on is what makes the prompt a prompt.

**The engine version is read, not assumed**, so the same fourteen integers are
a different command on 2K than on 2K3. A reader that picked one column would
wait for a shift key where the game asked for a digit, and the player would be
stuck.

**Test evidence** `test_rm2k_key_input_wiring.cs`, 9 tests through
`ExecuteFrame` and `PressKeys`. **1025/1025**,
`TestRm2kKeyInputWiring: 9/9`, `TestRm2kKeyInput: 10/10`.
**Mutations** Nine effective rules over two runs, **9 of 9 caught**.

**The key press arrives on an input frame, which is not the interpreter's
step**, so `PressKeys` is a separate entry point and not part of
`ExecuteFrame`. A reader that put the wait inside the dispatch would re-arm the
prompt on every frame it stayed open, and the reference resets its key state on
every call.

**A first draft asserted on prose it had invented****A first draft asserted on prose it had invented** — on the phrase "did not
declare", when the diagnostic said "does not declare" — and the failure was the
test's. **Asserting on prose a test made up makes the test the thing that has
to be right, and it was the wrong one.** The assertions are now on the words
that carry the meaning.
- **`1009` is decoded, not executed.** The battle command list is not a thing
  this reader changes yet.
- **`11610` is read, not wired.** `Rm2kKeyInput.Read` produces the set of keys
  a command accepts and the value each produces. Nothing prompts yet, because
  there is no window to prompt in.

**Test evidence** `test_rm2k_key_input.cs` (10 tests, reference values),
`test_rm2k_menu_commands.cs` (6 tests, fixture values and the correction).
**996/996**.
**Mutations** Nine rules over two runs, **9 of 9 caught** — the range starting
one early, the digit loop, the digit offset, the operator group, the time
variable read as a flag, the engine version column, the mouse order, the
operator offset, and the legacy switch.

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
`DONE` — runtime, P0, unblocked K-114

**The card's title was half the diagnosis.** `Rm2kPlayerTurn.Apply` carried the
comment *"This runtime has no vehicles, so nothing can be toggled and the action
event check always runs"* — and `Rm2kDecisionTurn.Run` sat next to it,
implemented, mutation checked, and **never called**. The vehicles were loaded and
drawn; they were never driven and never boarded. `GameSimulationState` had
**zero** vehicle wiring and the runtime kept its own `_vehicles` list.

**Implemented**
- `GameSimulationState.Vehicles` and `.Boarding`, both cleared in `Reset()`
- `Rm2kPlayerTurn.Apply` calls `Rm2kDecisionTurn.Run`, and a vehicle that takes
  the turn suppresses the action event check — a boat moored beside a sign has
  to be boardable, and the sign is on the tile the player faces
- `CanEmbark` / `CanDisembark` from the passability mask, `IsVehicleStopping`
  for the airship, `OppositeBit` for the way back

**Three real product faults the suite found**

**`TileInFront` spoke the wrong direction order.** The player speaks 2/4/6/8;
`DirectionDelta` expects 0–3. **A `8` yields `(0, 0)`** — the character's own
tile. Every boarding test "passed" without anything moving, and a player facing
up was handed a disembark onto the water they were standing on. The bridge
`LiblcfFromFacingDirection` already existed, and its own comment warns that
mixing the two silently turns a right step into a left one.

**`PassDown` is `0x01` and `PassUp` is `0x08`.** A first draft had them swapped
and wrote `0x08` for "down".

**A K-114 test held the wrong order in place.** It checked `TileInFront` with
0/1/2/3, and so agreed with itself: five assertions, every one consistent with
the same misreading.

**And a fixture that lied about itself.** `SetPassability(..., pAllowUp,
pAllowDown)` was named as walkable directions and wired `pAllowUp` to
`PassDown` — the opposite. Two tests then asserted the wrong polarity and failed
against correct code. **A fixture whose names lie about its own bits is worse
than no fixture**, because the failure points at the reader.

**Test evidence** 8 tests in
`project/tests/core/test_rm2k_vehicle_decision_turn.cs`, 1 rewritten in
`test_rm2k_vehicle_boarding.cs`.
**980/980**, `TestRm2kVehicleDecisionTurn: 8/8`, `TestRm2kVehicleBoarding: 11/11`.
**Mutations** Ten rules over six runs, **9 of 10 caught**. The tenth is a harness
fault, not a semantic gap: the first runner used `$TMPDIR/m_<path>` as its
backup, which fails on the `/`, so the mutations ran **without a restore** and
the following rules tested a cumulatively broken file. `git checkout --` then
discarded the **unstaged** slice; it was rebuilt and staged immediately.

**What this does not claim:** a vehicle's own move route, hero-directed vehicle
movement, and vehicle background music. K-114 lists those.

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

### K-134 The twenty-five table rows that have no card behind them
`READY` — board, P1

**This board was lying, and the way it lied was measurable.**

Twenty-five rows in the table have no detail section, and forty-seven numbers
between K-001 and K-133 were never used. Thirty detail sections had no row.
Two rows appeared twice. **An agent reading only the table — which is what
`AGENTS.md` points at first — would have seen the work stop at K-111 and had
no way to know that the RGSS archive, the Marshal reader, the Ruby lexer, the
parser, the value layer, two MZ fixtures and the whole command-execution line
existed.**

**The table is now rebuilt from the details**, so every card that has a detail
section has a row. That is the half that can be repaired from evidence.

**This card is the other half.** The twenty-five rows without a detail section
name work that was done:

| | |
|---|---|
| K-020 | Faithful RM2K/2003 simulation state model |
| K-033 | Visible RM2K map and sprite overlay in the runtime UI |
| K-034 | Safe keyboard movement handoff to RM2K simulation |
| K-035 | Keyboard message dismissal, choice navigation, numeric input |
| K-036 | Deterministic runtime simulation frame count from the virtual clock |
| K-037 | Clickable message, choice and numeric-input presentation controls |
| K-038 | Avoid per-frame choice-control reconstruction in the runtime UI |
| K-039 | Explicit runtime stop control, hide stale presentation controls |
| K-042 | RM2K event-page selection and bounded trigger scheduler |
| K-043 | LMU event-command vectors feeding the native scheduler |
| K-044 | Dispatch action and touch events from player input and movement |
| K-045 | LMU event-page switch and variable conditions |
| K-046 | Selector evaluation for switch B and variable comparisons |
| K-047 | Diagnose unsupported RM2K commands without execution |
| K-048 | Separate LMU move-route and event-command presence metadata |
| K-049 | Bounded RM2K item and actor page conditions |
| K-051 | Deterministic RM2K Timer 1 / Timer 2 conditions |
| K-052 | Bounded JSON simulation save and load roundtrip |
| K-053 | Adaptive application render FPS without changing simulation Hz |
| K-054 | Capability-gated RM2K save and debug tool contracts |
| K-060 | Game compatibility profile schema versioning and validation |
| K-061 | Compatibility report export for GitHub issues |
| K-070 | Faithful-vs-Enhanced profile and integer scaling controls |
| K-080 | RGSS architecture spike after the RM2K/2003 playable milestone |
| K-090 | MV/MZ JavaScript runtime architecture spike |

**Acceptance criteria**

- Each of the twenty-four `DONE` cards gets a detail section carrying **what
  was built, the test evidence, and the commit**. **No section is written
  from the title alone** — a title is a claim and this file does not carry
  claims.
- A card whose work cannot be evidenced from `git log` and the test suite is
  moved to `VERIFY`, not `DONE`, and says what is missing.
- K-080 and K-090 keep `BACKLOG`: both are behind the RM2K playable
  milestone, and both need a decision about JavaScript that is not this
  repository's to make quietly.
- The table and the details are checked against each other by the same
  measurement that found this: **every row has a section, every section has a
  row, and no row is duplicated.**

**Why this card exists rather than a paragraph in the board note**

Because the next agent will read the table. **A board note explaining that
the table is incomplete is a warning; a table that is complete is a fix.**

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


## WOLF Ton: drei Kanaele, und eine Null, die zwei Bedeutungen hat

**WOLF hatte keinen Ton und der Tonschritt in einer Laufbahn wurde abgelehnt.** Das war

die ehrliche Antwort, solange es nirgends hingesellt werden konnte — und ein Spiel mit

einem Tonschritt in jedem Kampf war bisher ein Spiel, in dem jeder Kampf an der Tonzeile

endete.



**Drei Kanaele und nicht einer.** BGM ist Hintergrundmusik, BGS ist Hintergrundgeräusch —

die Materialliste nennt es ein Umgebungsgeräusch und nennt Regen, Wind und einen Herzschlag

als seine Verwendung — und SE ist ein Soundeffekt, der nicht wiederholt. Ein Leser mit einer

Liste hätte einen Herzschlag das Stadttema ersetzen lassen.



### Die drei Regeln, die eine ganze Karte tragen



**Eine Lautstärke von 0 ist unter der alten Regel Standard und unter der neuen stumm.**

Die Materialliste sagt beides, für BGM wie für SE: 100 ist die normale Lautstärke, 1 bis 100

ist leiser, über 100 ist lauter, und ein Eintrag von 0 wird ebenfalls in normaler Lautstärke

abgespielt. Die Spielkonfiguration sagt dazu, dass vor Version 3.681 die 0 auf 100 umgerechnet

wurde und die Einstellung sie nun bei 0 lässt — und der Fall, für den es die Einstellung gibt,

ist ein Hintergrundgeräusch mit Mischung 0 für interaktive Musik. **Welche der beiden ein

Spiel benutzt, ist eine Einstellung, und dieser Leser hat keine** — er meldet also eine Null als

Null und benennt sie als den mehrdeutigen Wert, der sie ist.



**Die Zeit eines Effekts ist eine Verzoegerung und die eines Musikstücks eine Einblendung —

und das sind nicht dasselbe Feld.** Die Materialliste sagt, die Einblendzeit des Tonbefehls

werde zu *die Wiedergabe verzögern* für einen Soundeffekt, und nennt die Einheit: sechzig Bilder

sind eine Sekunde. Ein Leser, der eine Verzoegerung als Einblendung behandelte, hätte den

Effekt leise beginnen und lauter werden lassen — und die Verzoegerung eines Sekunde als

Millisekunden gelesen wäre ein Sechzigstel dessen, was das Spiel verlangt hat.



**Der Dateiname steht in den Einzel-Byte-Argumenten, und die Zahlen in den Vier-Byte-Argumenten.**

Ein Laufbahnschritt hat einen Typ, eine Anzahl Vier-Byte-Werte, diese, eine Anzahl

Einzel-Byte-Werte und diese — und ein Dateiname ist Text, also steht er in der zweiten Liste.

Ein Leser, der den Namen in der ersten gesucht hätte, hätte drei ganze Zahlen gefunden und

sich gefragt, warum kein Titel laeuft.



### Was der Test fand



**`Clear() leerte die Kiste und liess das Radio laufen.** Figuren, Wege, Passierbarkeit und

Partei werden alle mitgenommen — und der Ton nicht. Ein neues Spiel, das die Musik des letzten

behaelt, oeffnet seinen Titelbildschirm mit dem Thema dessen, was vorher geladen war, und

**nichts anderes auf dem Brett haette es gemerkt**, weil die Figuren fort waren und es keine

Figur gibt, die falsch aussieht.



**Test evidence** `test_wolf_audio.cs` (11).

**1324/1324**, Validator grün.

**Mutations** 11 Regeln über zwei Läufe, **11 von 11 gefangen** — darunter der Standardwert 100,

das Beibehalten der Null unter der neuen Regel, der Name aus den falschen Argumenten, BGS und SE

im selben Kanal, die Verzoegerung als Einblendung, SE bekommt die Einblendung der Musik, ein

leerer Name als Klanger, ein abgeschalteter Kanal, der sammelt, und der Tonschritt, der die

Laufbahn beendet.

## WOLF Laufbahnen aus einer Datei: zwei Befehle, die die VM kannte und der Leser nicht

**Die VM hatte `MoveRoute` und `WaitUntilRouteDone` im Dispatch und in der Enum, und

`ParseOpcode` hatte fuer keinen der beiden einen Namen.** Eine Kartendatei, die

`"op": "move_route"` schrieb, kam als `Unknown` an — und `Unknown` lehnt die VM ab.



**Also stand eine im Editor geschriebene Patrouille still, und nirgends stand, warum.**

Das ist der Fehler, den kein Test gefunden haette, **weil jeder andere Test seinen Befehl von

Hand gebaut hat** — ein handgebauter Befehl hat Figur und Laufbahn schon gefuellt, und nur

der Dateipfad muss sie fuellen.



### Die drei Regeln, die der Leser jetzt beachtet



**Die Figur und die Schritte sind der Unterschied zwischen einer Patrouille, die geht, und

einer, die nicht geht.** Ein Leser, der die Figur nicht fuellt, erreicht die VM mit der

Anweisung, eine Laufbahn ohne Figur zu starten — und die ist sofort fertig und meldet sich

fertig. Das Event laeuft, die Laufbahn ist fertig, und der Waechter bewegt sich nicht.



**Die Schrittnamen sind die der Tabelle und nicht eigene.** Eine Blickrichtung ist `FacingUp`

und nicht `TurnUp`, und ein Schritt, der eine Variable setzt, ist `AssignToVariable` und nicht

`SetVariable`. Ein Leser, der sie umbenannt haette, wuerde auf ein `facing_up` der Datei mit

einem unbekannten Schritt antworten.



**Ein unbekannter Name ist 0xFF und nicht 0.** Die verifizierten Schritttypen laufen von 0x00

bis 0x3A, und 0x00 ist ein Schritt nach unten — also wuerde ein Tippfehler im Schrittnamen

eine Figur eine Kachel nach sueden schicken, und das Spiel wuerde richtig aussehen, bis zum

Tag, an dem es das nicht mehr tut. Ebenso ist ein unbekannter Modus `Custom` und nicht 0,

denn 0 heisst *sich nicht bewegen* — ein Leser, der dorthin zurueckfaellt, stellt eine

Patrouille still, ohne Fehler und ohne Bewegung.



**Test evidence** `test_wolf_route_from_file.cs` (9), gegen eine echte Kartendatei auf der

Platte und nicht gegen ein gebautes Objekt.

**1333/1333**, Validator gruen.

**Mutations** 12 Regeln ueber zwei Laeufe, **12 von 12 gefangen** — darunter die beiden

Opcode-Namen, die fehlten, die nicht gelesene Figur, die nicht gelesene Laufbahn, die

Schrittnamen der Tabelle gegen geratene, der unbekannte Name als Schritt unten, der

unbekannte Modus als Stehen, das nicht gelesene Wartezeichen, die Flagge als jede Zahl und

die ganz verwerfenen Argumente eines Schritts.

## WOLF Common Events: ein Aufruf, der zurueckkommt

**Der Binaerleser dekodierte Typ 300 vollstaendig** — inklusive Argumentblock und dem

Flag fuer den Rueckgabewert — **und die Opcode-Enum hatte keinen Wert dafuer.** Also

konnte ein Spiel, dessen Events ein Common Event aufrufen, den Aufruf gar nicht

ausfuehren — **und jeder WOLF-Shop ist aus Common Events gebaut**: initialisieren, Ware

hinzufuegen, Laden ausfuehren.



### Die vier Regeln, die die Karte tragen



**Ein Aufruf teilt den Zustand und kopiert ihn nicht.** Ein Common Event, das eine Variable

setzt, aendert das Spiel — das ist der Zweck des Aufrufs —, also bleiben Brett, Variablen

und Schalter, wo sie sind, und nur die Fortsetzungsstelle kommt auf den Stapel. Ein Leser,

der den Zustand kopiert haette, haette ein Common Event, das der Held ein Item gibt, das

die Mannschaft nie bekommen hat.



**Das Ende eines Common Events setzt den Aufrufer fort, und nur das Ende des aeussersten

Programms beendet die VM.** Ein Leser, der beides als Ende behandelte, haette den ersten

Aufruf eines Spiels das Spiel sofort totstoppen lassen.



**Die Tiefengrenze ist die Wacht gegen ein Common Event, das sich selbst aufruft.** Ohne sie

laeuft die VM, bis der Prozess endet — **und das sieht ein Spieler als Spiel an, das auf

einer Kachel einfriert und das niemand sinnvoll melden kann.**



**Null ist der Held und kein Event.** WOLF zaehlt die Datenbank-Ids ab null, also ist ein

Aufruf der 0 ein Aufruf des Spielers — und ein Leser, der das als "kein Event angegeben"

behandelte, wuerde aus dem richtigen Grund ablehnen.



### Der Befund, den die Fehlersuche ergab



**Ich habe zehn Minuten an einem Test gefeilt, der keine Codefehler fand, weil es keine gab.**

Ich wollte eine Figur gehen sehen, die eine Common-Event-Laufbahn ging, und sie stand still.



**Das Brett allein ging, die VM nicht — und ohne jeden Aufruf.** Die Route startete, und die

VM erreichte das Ende des Events im selben Tick; ab dem naechsten Tick ist die VM

`Completed`, **und ein `Completed` tickt das Brett nicht.**



**Also gilt: eine Laufbahn in einem Event, das sofort endet, geht nicht** — **und das ist

richtig.** WOLFs eigene Common Events sind nicht so geschrieben: eine Laufbahn, auf die es

ankommt, wird gefolgt von einem Warten, oder das Event laeuft weiter, oder die Laufbahn

startet ein Parallelereignis, das nie endet. **Ein Test, der hier das Gehen erwartet haette,

haette eine Form gemessen, die kein Spiel benutzt.** Der Test wartet jetzt auf die Laufbahn —

**und das Warten ist das, was einen Schritt sichtbar macht.**



**Test evidence** `test_wolf_commonEvent_call.cs` (10).

**1343/1343**, Validator gruen.

**Mutations** 10 Regeln ueber zwei Laeufe, **10 von 10 gefangen** — darunter das Ende, das

auch den Aufrufer beendet, die Rueckkehr, die kein Programm setzt, die Fortsetzung, die beim

Aufruf anfaengt statt danach, die ungepruefte Tiefe, die gesuchte Null, die Meldung ohne

Nummer, das leere Event, das suspendiert, der Stapel beim Neustart und der Aufrufbefehl, der

aus der Datei nicht ankommt.

## WOLF Map-Event-Aufrufe: eine Zahl, zwei Arten, und ein bewusstes Schweigen

**Der Binaerleser dekodierte Typ 210** und unterschied die beiden Arten an der Nummer: unter

500.000 ist es ein Map-Event, ab 500.000 ein Common Event, **und nur dann traegt der Aufruf

Argumente.** Die Enum hatte dafuer keinen Wert — also konnte ein Map-Event, das ein anderes

aufruft, den Aufruf gar nicht ausfuehren.



### Die Regel, die man nicht vermutet



**Ein Event, das es nicht gibt, wird ignoriert — und nicht als Fehler gemeldet.** Die Hilfe

sagt das in einem Satz: イベントが存在しない場合は無視されます, **und der Grund ist, dass ein Spiel

ein Event loescht und den Aufruf stehen laesst.** Ein Leser, der dort scheiterte, haette ein

Spiel, das an einem Aufruf zu einem vom Autor entfernten Schatzkasten tot stehen bleibt, mit

einer Meldung, mit der niemand etwas anfangen kann. **Das ist die einzige Stelle in dieser VM,

wo ein Fehlendes absichtlich kein Fehler ist — und der Grund steht hier, weil der Reflex

ablehnen ist.**



### Die Self-Variablen, und der Fehler, den der Test fand



**Eingabe 1 ist Self 0, Eingabe 2 ist Self 1, und Text-Eingaben beginnen bei Self 5.** Map-Self

liegt bei 1.100.000, Common-Self bei 1.600.000 — **beide Bander existierten bereits im Modell

und die VM benutzte keines von beiden.**



**Und dann der Fund:** `ApplyOperator` schrieb mit `_variables.SetByReference` direkt in die

Bander, **waehrend das Lesen ueber den neuen Durchlass lief.** Also schrieb ein Common Event,

das sein eigenes \cself[0] zuwies, in ein Band fuer sich — **und las es als null zurueck.**

Der Test hat es gefunden, weil er eine Regel prueft, die ich am wenigsten belegt hatte.



**Ein Map-Event hat keinen eigenen Rahmen: seine Self-Variablen sind die des aufrufenden

Events.** Ein Leser, der ihm einen eigenen gab, wuerde einer Kette von Map-Events die Werte

verlieren, die das erste bekommen hat.



**Test evidence** `test_wolf_event_call.cs` (10).

**1353/1353**, Validator gruen.

**Mutations** 11 Regeln ueber zwei Laeufe, **11 von 11 gefangen** — darunter der nicht

abgezogene Versatz, die beiden Tabellen als eine, die nicht landenden Eingaben, ein Rahmen pro

Spiel statt pro Aufruf, das fehlende Spiel-Self, das fehlende Event, das scheitert, der

nach der Rueckkehr stehen bleibende Rahmen, die negative Id als gesuchte, der Schreibweg, der

die Self-Baender umgeht, und der Befehl, der aus der Datei nicht ankommt.
