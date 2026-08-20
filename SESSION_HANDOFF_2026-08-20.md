# Session Handoff — 2026-08-20

> Handover note for the next session. Written mid-flight during the LCF parser
> rework and the start of a full GDScript → C# port.

## 1. Context / Goal

Project: **UniversalRPG** — cross-platform RPG Maker compatibility runtime
(Godot 4.7, GL Compatibility).

Goal of this session: replace the existing RM2K parsers (which read invented
headers/offsets and were not LCF-compatible) with a real LCF container parser
(LDB database / LMU map / LSD save), connect the previously orphaned
`extends Test` suites to a working test runner, and begin porting the entire
codebase from GDScript to C#.

## 2. What was done

### 2.1 LCF parser (GDScript) — complete
- **New `src/rm2k/parser/lcf_binary_reader.gd`** — `LCFBinaryReader`.
  Real LCF container reading: BER-encoded length + header
  (`LcfDataBase` / `LcfMapUnit` / `LcfSaveData`), then BER-encoded
  `ID, Length, Payload` chunks; structures end with chunk ID 0.
  Hard limits: BER ≤ 5 bytes, chunk ≤ 32 MB, ≤ 100k chunks / array items,
  ≤ 10k struct fields. Tracks `error_message` / `error_offset`.
- **Rewritten `src/rm2k/parser/rm2k_parser.gd`** — `RM2KParser`.
  - `parse_game_ini` (`[RPG_RT]` or `[Game]` section)
  - `parse_database` (LDB section map 0x0b–0x21, array sections, version 0x1a,
    engine family 2000 vs 2003)
  - `parse_map` (chipset 0x01, width 0x02, height 0x03, lower 0x47, upper 0x48,
    events 0x51 with x/y/name/page_count; dimension/tile limits)
  - `parse_save` (top-level chunk list only)
  - Only the safe container layer + real map base fields are decoded; full
    event/battle content is intentionally not interpreted yet.

### 2.2 Test infrastructure — complete, all green
- **New `tests/framework/test_base.gd`** — minimal `Test` base class
  (`setup`/`teardown`, `test_*` discovery, `assert_true/false/eq/ne`).
- **New `tests/runner.gd`** — `SceneTree` runner; runs all suites under
  `tests/core/`, forces locale `en`, skips non-instantiable suites.
- **Extended `tests/smoke_runner.gd`** — added `_test_lcf_parser()` smoke test
  (minimal real-LCF LDB fixture, verifies version + actors count).
- **Rewritten `tests/core/test_rm2k_parser.gd`** — fixtures now use real LCF
  encoding (BER length + header + chunks, single `0x00` terminator); 19 tests
  incl. wrong header, truncated chunk, bad layer size, dimension limit.
- **Fixed pre-existing bugs** so all suites pass:
  - `compatibility_profile.gd` — two `var value := dict.get(...)` Variant
    inference errors (typed `Variant`).
  - `game_detector.gd` — `_has_native_libraries` now also counts `.exe`
    (RPG_RT.exe).
  - `virtual_filesystem.gd` — added missing `class_name`; fixed `"\0"` escape
    (→ `"\u0000"`); fixed nonexistent `left_back` (→ `trim_suffix("/")`);
    `resolve()` now respects mount priority (OVERRIDE > RTP > GAME > SAVE >
    CACHE) and maps empty path to the GAME mount.
  - `test_virtual_filesystem.gd` — fixed `\0` escape and a bool→String
    assignment.
- **Result:** `tests/runner.gd` → **77/77 tests passed (exit 0)**;
  `tests/smoke_runner.gd` → passed.

### 2.3 C# port — IN PROGRESS
- Installed **.NET 8 SDK** (local, `/tmp/opencode/dotnet8` — not system-wide).
- Created **`UniversalRPG.csproj`** (Godot.NET.Sdk/4.7.2, net8.0,
  RootNamespace `UniversalRPG`); `dotnet restore` verified against
  GodotSharp 4.7.2.
- Ported to C# so far (namespace `UniversalRPG.*`):
  - `src/core/legacy_text_decoder.cs` — `LegacyTextDecoder`
    (CP932/Shift_JIS via `CodePagesEncodingProvider`, UTF-8/UTF-16 BOM handling).
  - `src/rm2k/parser/lcf_binary_reader.cs` — `LcfBinaryReader`
  - `src/rm2k/parser/rm2k_parser.cs` — `Rm2kParser` (with inner
    `ParseError`/`ParseResult`)
- The Godot **Mono** editor and `dotnet build` have **not** been run yet for C#.

## 3. Current state

- GDScript codebase: all 12 source files + 4 test suites green (77/77).
- C# port: **4 of ~15 files done** (csproj + 3 classes); 11 files remain.
- Git: branch `main`, remote `origin` → `github.com/Noa3/UniversalRPGLauncher`.
  Pushed at end of this session.

## 4. Remaining work (next session)

1. Download Godot **Mono** 4.7.2 editor into `tools/godot/editors/4.7.2/`
   (currently empty; the plain non-mono editor was used for GDScript tests).
2. Port remaining sources to C#:
   - `src/core/virtual_clock.cs`, `src/core/virtual_filesystem.cs`
   - `src/game_detector/game_detector.cs`
   - `src/compatibility/compatibility_profile.cs`
   - `src/rm2k/rm2k_map.cs`, `src/rm2k/database/rm2k_database.cs`
   - `app/library/game_library.cs`, `app/launcher/runtime_launcher.cs`
   - `app/ui/main.cs` (scene script)
3. Update `scenes/main.tscn` to reference `res://app/ui/main.cs`;
   add `"C#"` to `project.godot` `config/features`.
4. Port the test harness + suites to C# (Test base, runner, smoke runner,
   the 4 `test_*` suites).
5. `dotnet build` + run tests under the Mono editor; fix compile errors.
6. Delete the `.gd` files once their C# equivalents compile and tests pass.
7. Validate the parser against real RPG Maker 2000/2003 LCF files.

## 5. Known issues / notes

- `src/rm2k/database/rm2k_database.gd` currently does **not** compile:
  missing `var` on `State.phases`, and `from_dict()` calls `from_dict` on
  `Skill`/`State`/`Class`/`Enemy`/`BattleAnimation`/`Trooper`, which lack that
  method. Fix during the C# port.
- The RM2K parser decodes only the container + map base fields; full
  event/battle parsing is future work.
- The test runner forces English locale; the app itself is localized via
  `locale/*.po`.
- The parser test uses a synthetic real-LCF fixture, not a real game file.
- Local tooling lives in `/tmp/opencode` (Godot binary, .NET SDK) and is not
  persisted — re-download/install on a fresh machine.

## 6. Commands

```bash
# GDScript tests
godot --headless --path . --script res://tests/runner.gd
# Smoke tests
godot --headless --path . --script res://tests/smoke_runner.gd
# C# restore/build (uses local SDK)
export DOTNET_ROOT=/tmp/opencode/dotnet8 && export PATH=$DOTNET_ROOT:$PATH
dotnet restore
dotnet build
```