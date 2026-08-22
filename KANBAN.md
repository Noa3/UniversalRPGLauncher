# UniversalRPG Autonomous Kanban

> Updated: 2026-08-22
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
| K-013 | 0 | BACKLOG | Expand LMU event/page metadata decoding without executing commands | K-010 |
| K-014 | 0 | BACKLOG | Preserve unknown LCF fields/chunks for diagnostics and forward compatibility | K-010 |
| K-015 | 0 | IN PROGRESS | Decode remaining LDB array sections into typed models | K-012 |
| K-020 | 1 | BACKLOG | Define faithful RM2K/2003 simulation state model | K-011,K-012,K-013 |
| K-021 | 1 | BACKLOG | Implement first event-interpreter slice: message/switch/variable/branch/wait/transfer | K-020 |
| K-022 | 1 | BACKLOG | Implement map/player movement and passability simulation | K-020 |
| K-030 | 1 | BACKLOG | Godot renderer adapter: virtual framebuffer + lower/upper tile layers | K-020 |
| K-031 | 1 | BACKLOG | Character/event sprite renderer and camera | K-030 |
| K-032 | 1 | BACKLOG | Message/window/picture presentation layer | K-030,K-021 |
| K-040 | 1 | BACKLOG | RTP registry/resolver without bundled proprietary RTP data | K-012 |
| K-041 | 1 | BACKLOG | Missing-asset diagnostics and per-game RTP profile | K-040 |
| K-050 | 2 | BACKLOG | Original-format save model and safe save directory integration | K-020 |
| K-060 | 2 | BACKLOG | Game compatibility profile schema versioning/validation | K-002 |
| K-061 | 2 | BACKLOG | Compatibility report export for GitHub issues | K-060 |
| K-070 | 3 | BACKLOG | Faithful-vs-Enhanced profile and integer scaling controls | K-030 |
| K-071 | 3 | BACKLOG | Controller/touch remapping layer | K-020 |
| K-080 | 4 | BACKLOG | RGSS architecture spike after RM2K/2003 playable milestone | RM2K playable milestone |
| K-090 | 4 | BACKLOG | MV/MZ JavaScript runtime architecture spike | RM2K playable milestone |
| K-100 | 5 | BACKLOG | PE/DLL inspector research and safe metadata-only parser | Stable primary runtimes |

## Card details

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
- Built-in descriptors cover RM95, RM2K, RM2K3, XP, VX, VX Ace, MV, MZ, WOLF, and Unite research detection. RM95/MV/MZ/Unite remain detection-only; RGSS has a bounded metadata lifecycle slice, WOLF has an explicitly unencrypted plain-data slice, and RM2K/RM2K3 additionally parse LDB/LMT/LMU data.
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
- K-015 remains `IN PROGRESS`; next batch is the battle-command/system metadata boundary before nested arrays and event-command decoding.

### K-013 — LMU events/pages

**Acceptance criteria**
- Decode event/page structures as data only.
- Do not execute event commands while parsing.
- Bound page/command counts and payload sizes.
- Preserve raw/unknown commands for later interpreter work.

## Agent maintenance rules

- Hermes may split a card when implementation reveals genuinely independent work, but must preserve traceability to the parent ID.
- New defects found during a card become `P0`/`P1` bug cards when they threaten correctness/security; otherwise add them to backlog.
- Do not create hundreds of speculative cards for distant phases. Expand the next 1–2 milestones in detail and keep later phases coarse.
- At the end of a work session update this board and `SESSION_STATE.md` with exactly what is next.
