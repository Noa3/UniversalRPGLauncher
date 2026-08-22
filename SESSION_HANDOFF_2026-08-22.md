# Session Handoff — 2026-08-22

## Objective
- Continue **UniversalRPG** (`/home/noa3/Schreibtisch/URPG`, Godot 4.7.2 Mono, C#/.NET): user initially requested RPG Maker MZ integration/plugin work.
- **Scope decision (user confirmed via question prompt):** MZ work violates repo scope policy (`AGENTS.md` priority list puts MV/MZ at P7; K-090 depends on the RM2K playable milestone). User chose **"Weiter K-012"** — finish the running P0 card instead. MZ stays detection-only until its milestone.
- Completed K-012 as a scoped slice, prepared handoff + git upload.

## Important Details
- Git: branch `main`, remote `origin https://github.com/Noa3/UniversalRPGLauncher.git`, previous head `15e6a97` ("Tighten RM95 research detection boundaries").
- .NET SDK 8.0.424 now installed at `~/.dotnet` (**persistent**, replaces the ephemeral `/tmp/opencode/dotnet8` from the 2026-08-21 handoff). Every `dotnet`/Godot-Mono run needs:
  ```bash
  export DOTNET_ROOT="$HOME/.dotnet"
  export PATH="$HOME/.dotnet:$PATH"
  ```
- Godot binary: `tools/godot/editors/4.7.2/linux-x86_64/Godot_v4.7.2-stable_mono_linux_x86_64/Godot_v4.7.2-stable_mono_linux.x86_64`.
- Validation command on this box: `bash scripts/validate.sh` (the file lost its executable bit; `./scripts/validate.sh` fails with EACCES). Full run passed this session: import + headless suite `165/165`.
- Field-ID source of truth used for LDB decoding: EasyRPG **liblcf** `src/generated/lcf/ldb/chunks.h` (repo `EasyRPG/liblcf`; old `EasyRPG/Reader` URLs are dead — Reader was merged into liblcf). Key verified IDs:
  - `ChunkActor`: name=0x01, title=0x02, character_name=0x03, character_index=0x04, transparent=0x05, initial_level=0x07, final_level=0x08, critical_hit=0x09, critical_hit_chance=0x0A, face_name=0x0F, face_index=0x10.
  - `ChunkSwitch` / `ChunkVariable`: name=0x01 only.
- LDB integer fields decode with **signed** BER (`DecodeLdbIntegerField` → `ReadSignedBer`), matching liblcf's Integer reading; empty payload ⇒ 0.
- `ParseDatabase` result gained additive keys: `actors`, `switches`, `variables` (each `Array<Dictionary>` with per-entry `unknown_fields`). Existing keys unchanged; `Rm2kEngineRuntime` consumers unaffected.
- Test helpers added in `TestRm2kParser`: internal static `Struct(params byte[][])` (field chunks + terminator) and `StructArray(params byte[][])` (BER count + sequential ids starting at 1).

## Work State
### Completed (K-012 — typed LDB slice)
- `src/rm2k/parser/rm2k_parser.cs`:
  - New constants/maps: `MaxLdbStringBytes`, `LdbActorFieldNames`, `LdbNamedEntryFieldNames`.
  - `ParseDatabase` collects fields for typed sections (actors/switches/variables), decodes them via new `DecodeTypedLdbSection` → `DecodeLdbActorEntries` / `DecodeLdbNamedEntries`; string fields via `DecodeLdbString`, ints via `DecodeLdbIntegerField`. Defaults mirror liblcf `rpg::Actor` initializers (e.g. initial_level 1, final_level −1, critical_hit 1, critical_hit_chance 30).
  - Duplicate structure IDs rejected; unknown actor/entry fields retained per entry for diagnostics.
- Tests (`tests/core/test_rm2k_parser.cs`): typed actor entries incl. defaults + unknown-field retention; switch/variable name decoding; duplicate-ID rejection; missing-terminator rejection.
- Tests (`tests/core/test_rm2k_real_fixtures.cs`): `Test_RealDatabaseTypedSectionsMatchSectionCounts` asserts actors/switches/variables typed counts equal `section_counts` on both pinned TestGame LDBs.
- Suite went 159 → **165/165**; `bash scripts/validate.sh` fully green (Godot `4.7.2.stable.mono`).
- Bookkeeping: KANBAN K-012 → DONE with evidence; successor card **K-015** created (remaining LDB array sections); SESSION_STATE checkpointed; PROJECT_STATUS/ARCHITECTURE updated to 165/165 and current parser status.

### Not done / deferred
- MZ/MV integration (K-090) — blocked by policy until RM2K playable milestone; user approved deferral this session.
- Remaining LDB array sections typing → K-015. LMU event/page metadata → K-013. Unknown-field preservation hardening → K-014.

## Next Move
1. Select next `READY` card: **K-015 recommended** (parser continuity; same patterns as K-012: verify IDs against liblcf `chunks.h` first — skills/items/enemies/troops/terrains/attributes/states/animations/chipsets/classes/battle_commands), or K-013/K-014.
2. Per-section batches: synthetic fixtures + real-fixture count comparisons, keep unknown-field retention.
3. Re-run `bash scripts/validate.sh` before moving any card to DONE; update KANBAN/SESSION_STATE/docs counts.

## Relevant Files
- Modified this session: `src/rm2k/parser/rm2k_parser.cs`, `tests/core/test_rm2k_parser.cs`, `tests/core/test_rm2k_real_fixtures.cs`, `KANBAN.md`, `SESSION_STATE.md`, `docs/PROJECT_STATUS.md`, `docs/ARCHITECTURE.md`.
- Fixtures (unchanged, pinned): `tests/fixtures/easyrpg-testgame/{rm2000,rm2003}/RPG_RT.ldb`.
- Reference: `SESSION_HANDOFF_2026-08-21.md` (C# conventions, binding substitutions, framing/LMT fixture expectations — still valid).
