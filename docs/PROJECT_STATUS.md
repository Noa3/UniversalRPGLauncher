# UniversalRPG — Project Status

> **Last Updated:** 2026-08-31
> **Current Phase:** Phase 2 — RM2000/2003 Parser in progress

## Executive Summary

The project has a Godot 4.7.2 application foundation, localized game-library UI, bounded folder/ZIP inspection, registry-driven engine detection, persisted import metadata, legacy metadata decoding, a real bounded LCF container parser, and a minimal parser-backed RM2000/2003 runtime bootstrap validated against pinned EasyRPG TestGame fixtures. Full gameplay is not playable yet; the immediate critical path is expanding faithful RM2000/2003 parsing, renderer/system coverage, event counters, and walk animation beyond the bounded native event path, the working chipset passability, and the verified autotile animation steps.

The repository uses pure C#/.NET through the Godot 4.7.2 .NET editor. The Godot project (including `project.godot`, `UniversalRPG.csproj` and `UniversalRPG.sln`) lives under `project/`; development docs, `scripts/validate.sh`, and the pinned Godot runtime under `tools/godot/` stay at the repository root. `scripts/validate.sh` runs restore, build, Godot import, and the C# core/smoke suite. The latest headless runner passed `853/853` tests. `project/tests/fixtures/` also holds data three real games wrote: sixteen XP `.rxdata` files from two independent installations, a real RM2K database, map tree and two maps from a 743 map game, and one `Game.ini` from a KiriKiri game that is not a WOLF game. Sizes and SHA-256 are in `project/tests/fixtures/RGSS_FIXTURES.md`. No executable, DLL, save, image, audio or script is imported. An RPG Maker MZ game is read in `project/src/mz/`: its database, its map list and its maps come back as values, with the file own text kept so a caller can hash what was read. Eleven real data files are in `project/tests/fixtures/mz` with their sizes and SHA-256 in `project/tests/fixtures/MZ_FIXTURES.md`. **No JavaScript of a game is read or run, so an MZ game does not play.**

Real LMU event pages now decode: the pinned RM2000/RM2003 fixtures yield 22 and 38 event pages with verified liblcf field ids (`condition 0x02`, `move_frequency 0x20`, `trigger 0x21`, `layer 0x22`, `move_route 0x29`, `event_commands_size 0x33`, `event_commands 0x34`). A command vector that cannot be decoded is contained per page with a diagnostic and its raw payload size instead of making the whole map unloadable, and such pages are skipped by the runtime instead of running empty. Page trigger ids follow liblcf `EventPage::Trigger` (`action=0`, `touched=1`, `collision=2`, `auto_start=3`, `parallel=4`). `ControlSwitches` and `Control Variables` follow the verified EasyRPG parameter layout (`[targetMode, start, end, …]`), which real games use, and a regression test executes a real fixture action page end to end through the RM2K runtime.

The bounded RM2K interpreter now supports verified `ChangeGold` command `10310`, `ChangeItems` command `10320`, `ChangePartyMembers` command `10330`, `ChangeExp` `10410`, `ChangeLevel` `10420`, `ChangeHeroName` `10610`, screen effects `11040`/`11050`/`11070`, `ChangeEventLocation` `10860`, `EraseEvent` `12320`, and nested `CallEvent` `12330` for map events. Gold and item counts are bounded to `0..999999`; levels clamp to `1..99`; party mutations support constant/variable actor IDs, add/remove operations, duplicate and capacity protection, and fail-closed diagnostics. Bounded MV and MZ metadata extraction reads top-level `System.json` properties with a real JSON parser (MV `versionId`, MZ `systemVersion`, plus locale, start position and party); both engines share one bounded `data/` inventory reader that requires the matching runtime signature, and no foreign JavaScript is executed. Chipset passability is now resolved from verified data: the LDB chipset section supplies the 162 lower and 144 upper passability entries, and the EasyRPG Player `map_data.h` block constants plus the `Game_Map` upper-then-lower rule turn LMU `lower_layer`/`upper_layer` tile ids into per-tile direction masks. The simulation moves a character only when the target tile's mask permits that direction, and a real-fixture test proves an RM2000 and an RM2003 map both reject steps into impassable tiles and accept steps onto walkable ones.

## Phase Status Overview

| Phase | Description | Status | Notes |
|-------|-------------|--------|-------|
| 0 | Repository audit | ✅ Complete | Initial setup |
| 1 | Runtime foundation | ✅ Complete | Core abstractions implemented |
| 1.5 | Application foundation | ✅ Complete | Library, plugin detection/selection wiring, persistence, localization, import safety |
| 2 | RM2000/2003 parser | 🚧 In progress | Real LCF reader + initial LDB/LMU/LSD decoding |
| 3 | RM2000/2003 rendering | 🚧 In progress | Real map and character frames render into pixels and follow a move; the frame is a screen-sized viewport that follows the verified camera, while walk animation and event move routes are absent |
| 4 | Event interpreter | 📋 Planned | Depends on Phase 2 |
| 5 | Full RM2000/2003 systems | 📋 Planned | Depends on Phase 4 |
| 6 | Compatibility work | 📋 Planned | Real-world testing |
| 7 | Enhanced Mode | 📋 Planned | After Faithful Mode stable |
| 8 | RGSS runtime | 📋 Planned | After Phase 5 |
| 9 | MV/MZ runtime | 📋 Planned | After Phase 8 |
| 10 | Native plugin compat | 🔬 Research | Long-term |
| 11 | Android compat | 🔬 Research | Long-term |

## Implemented Systems

### 1. VirtualFileSystem (`project/src/core/virtual_filesystem.cs`)

**Status:** Implemented

**Features:**
- Multi-mount merging (game, override, RTP, save, cache)
- Case-insensitive path resolution
- Path traversal protection
- Path normalization
- Archive access preparation

**Limitations:**
- No archive (ZIP/RVData2) support yet
- No symlink resolution
- Case map rebuild on every mount change (O(n))

**Test Coverage:** Partial (see tests below)

---

### 2. VirtualClock (`project/src/core/virtual_clock.cs`)

**Status:** Implemented

**Features:**
- Deterministic simulation timing (60 Hz base)
- Speed control (0.5x–10x, pause)
- Scheduled callbacks
- Frame-rate decoupling
- Single-step debugging

**Limitations:**
- No save-state serialization yet
- No rewind support
- No deterministic RNG seeding

**Test Coverage:** 8 deterministic regression tests

---

### 3. GameDetector (`project/src/game_detector/game_detector.cs`)

**Status:** Implemented

**Features:**
- Compatibility facade over registered, deterministic detection plugins
- Bounded folder/ZIP inspection with ranked candidates and confidence scoring
- Version, evidence, ambiguity, malformed-input, and structured diagnostics
- Compatibility profile schema validation (legacy schema 0, current schema 1, future-schema rejection) and bounded profile sizes
- Deterministic Markdown compatibility-report export for GitHub issues without upload or external execution
- Explicit Faithful/Enhanced render policy with bounded integer scaling controls
- Persisted candidate/selection/evidence/compatibility records in `user://library.cfg`
- RTP dependency, custom script/plugin, and native library metadata
- Explicit user-provided RTP registry/resolution is bounded, deterministic, and data-only; no proprietary RTP data is bundled or auto-discovered
- Runtime selection through exact plugin IDs, capability checks, platform checks, and no-fallback errors

**Limitations:**
- RM2K/RM2K3 have a safe bounded bootstrap that loads LDB/LMT/LMU into simulation and scheduler state; full gameplay runtime is not implemented. RGSS/XP/VX/VX Ace, RM95, MV/MZ, and Unite remain detection-only.
- Bounded inspection now distinguishes *partial* scans (entry budget reached on a well-formed tree, advisory Info) from truly malformed input (Error); large real games no longer hard-fail runtime initialization for exceeding the 4096-entry budget
- MV metadata extraction now reads bounded `data/System.json` title data and reports `.rpgmvp`/`.rpgmvo`/`.rpgmvm` encrypted assets without executing JavaScript; MV remains detection/metadata-only
- Executables and libraries are inspected as bounded data only, never loaded or executed
- Archive import is read-only inspection; safe extraction/staging for future runtime assets remains separate work
- Missing-asset diagnostics and bounded per-game RTP profile metadata are implemented in K-041; original RM2K/RM2K3 `LSD` saves now have a read-only bounded framing model from K-050, while semantic field mapping, save mutation, and UI integration remain separate work

**Test Coverage:** 15 deterministic detection tests

---

### 4. CompatibilityProfile (`project/src/compatibility/compatibility_profile.cs`)

**Status:** Implemented

**Features:**
- Extensible JSON-based profiles
- SHA-256 hash matching
- Engine-specific profiles
- Per-game flags with global override
- Profile loading from directory

**Limitations:**
- No versioned schema migration
- No profile validation
- No profile signing

**Test Coverage:** 19 deterministic profile tests

## Technical Debt

| Item | Severity | Description |
|------|----------|-------------|
| Headless Godot editor diagnostic | Low | Godot 4.7.2 emits an internal `EditorSettings` message during `--headless --editor --quit`; validation still exits successfully |
| No CI | Medium | No automated build/testing |
| No export pipeline | Medium | Presets exist; signed/release exports are not automated |
| Legacy encoding varies by platform | High | CP932 decoder must be tested on every target, especially Android/iOS |
| No safe archive importer | High | Folder scans are bounded, but archive staging is not implemented |
- Incomplete gameplay runtime | High | RM2K/RM2K3 renders the real pinned map into a golden-regression frame through the verified draw order, with the hero and the event characters placed from the LDB actor and charset, the two tile layers cached exactly as the Player keeps them, the screen tile camera applied by dividing by 16, and the frame recomposited on a simulation boundary after a move. Movement now uses the verified step budget, so a character spends `1 << (1 + move_speed)` per update and its sprite pixels interpolate while its logical position updates immediately, and event move routes decode from the real `EventPage` chunk and execute with the verified order: a facing command takes effect at once, a movement command advances the index only after the move completes, and a blocked move advances only when the route is skippable. Vehicles are modelled, can be boarded and left with the asymmetric rules, drive with the verified speeds, animate, climb and descend with the verified altitude budget, and draw with the airship shadow. What is still missing: picture and other presentation effects, audio, the battle system, a full save and load, and input beyond the turn order. A synthetic floor still does not render, because the LDB tile substitution tables are never populated, so the wide map test asserts applied offsets rather than pixel differences. The RGSS engines XP, VX and VX Ace have an archive reader, a Marshal reader, a lexer, a parser and a value layer, all data only, and no runtime: `RgssEngineRuntime` is still a metadata inspector. MV and MZ have no runtime source tree at all |

## Missing Core Components (Planned)

| Component | Phase | Priority |
|-----------|-------|----------|
| Expand/validate RM2K parser | 2 | Critical |
| RM2K interpreter | 4 | Critical |
| RM2K renderer | 3 | High |
| RGSS runtime | 8 | High |
| JavaScript runtime | 9 | High |
| Win32 API shim | 10 | Medium |
| Save state system | 5 | High |
| Asset override system | 7 | Medium |
| Input abstraction | 1 | Implemented for RM2K keyboard/controller/touch mapping; broader platform abstraction remains open |
| Audio abstraction | 1 | Low (planned) |
| Renderer interface | 1 | Low (planned) |

## Current Build Status

| Target | Status | Notes |
|--------|--------|-------|
| Godot Editor | ✅ Runs | project.godot created |
| Linux headless | ✅ Tested | Godot 4.7.2 import and all UI locales start |
| Windows Export | ⏸️ Not tested | Preset present; templates/toolchain needed |
| Linux Export | ⏸️ Not tested | Preset present; templates needed |
| macOS/iOS Export | ⏸️ Not tested | Requires macOS, Xcode, signing, templates |
| Android Export | ⏸️ Not tested | Preset present; Android SDK/templates needed |

## Next Immediate Tasks

1. K-016: typed bounded RPG Maker MZ metadata inspection and explicit encrypted-asset diagnostics; keep MZ detection-only until a safe JavaScript runtime boundary is separately verified
2. Expand typed LMU decoding incrementally; do not guess undocumented offsets/fields
3. Test CP932/Shift-JIS behavior on target platforms and add malicious-input fixtures
4. Implement LMU event/page metadata decoding without executing commands

## Open Questions

1. Which Ruby VM to embed for RGSS? (mruby, rbx, custom?)
2. Which JavaScript engine for MV/MZ? (V8, QuickJS, Duktape?)
3. Should we use C++ for the RM2K parser (performance)?
4. How to handle large game databases efficiently?
5. What is the target minimum hardware spec?

## Risk Assessment

| Risk | Likelihood | Impact | Mitigation |
|------|-----------|--------|------------|
| Ruby VM embedding complexity | High | High | Start with mruby, evaluate later |
| JavaScript engine size | Medium | Medium | Use QuickJS (small footprint) |
| RM2K format reverse-engineering | Medium | High | Public documentation exists |
| Performance on mobile | Medium | High | Profile early, optimize hot paths |
| Legal issues with RTP | Low | High | Never bundle RTP, user provides |
| Win32 compatibility scope creep | High | Medium | Strict scope control, phase gates |


## 2026-08-20 Stabilization Pass

Changes prepared in this pass:

- fixed repeating `VirtualClock` callbacks so they keep their requested interval instead of firing every tick after first expiry;
- corrected slow-motion to use a real speed factor (`0.5 == half speed`), added stable callback IDs and monotonic FPS sampling;
- fixed compatibility-profile precedence so per-game flags actually override global defaults;
- repaired the previously non-compiling/incomplete `RM2KDatabase` data model and added round-trip regression tests;
- added `scripts/validate.sh`, GitHub validation workflow, `KANBAN.md`, `AGENTS.md`, `SESSION_STATE.md`, and the Hermes autonomous-work prompt;
- added provenance-pinned EasyRPG TestGame RM2000/RM2003 LDB/LMT/LMU fixtures and real-framing regression tests;
- accepted valid zero-length LDB struct-array sections while preserving bounded malformed-input rejection.

## 2026-08-22 Typed LDB Slice (K-012/K-015)

- `ParseDatabase` now decodes the actors section into typed entries: name/title/character_name/face_name strings plus character_index, transparent, initial_level, final_level, critical_hit, critical_hit_chance, face_index integers; defaults mirror liblcf `rpg::Actor` initializers.
- Switches and variables sections decode to id/name entries; duplicate structure IDs are rejected.
- K-015 now additionally decodes scalar metadata for skills/items/states/classes/enemies/terrains/attributes/troops/animations/chipsets/battle_commands using field IDs verified against EasyRPG liblcf; nested arrays remain data-only and are retained as unknown fields.
- Field IDs are verified against EasyRPG liblcf `src/generated/lcf/ldb/chunks.h`; unknown actor/entry fields remain preserved per entry for diagnostics.
- Synthetic fixtures cover defaults, unknown-field retention, duplicate IDs, missing terminators, and battle-command trailing data; real-fixture tests assert typed entry counts equal section counts on both pinned TestGame LDBs.
- Validation: `GODOT_BIN=E:/URPG/Godot_v4.7.2/Godot_v4.7.2-stable_mono_win64_console.exe ./scripts/validate.sh` passed with Godot `4.7.2.stable.mono.official.ed1daf0bf`; headless suite `170/170`.

The C# migration and plugin application wiring have been validated with the local Godot 4.7.2 stable .NET editor on Windows: `dotnet build` passed, script registration succeeded after PascalCase file renames, and the headless C# core/smoke runner passed `159/159` at that time (now `171/171`).
