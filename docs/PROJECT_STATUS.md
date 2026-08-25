# UniversalRPG — Project Status

> **Last Updated:** 2026-08-24
> **Current Phase:** Phase 2 — RM2000/2003 Parser in progress

## Executive Summary

The project has a Godot 4.7.2 application foundation, localized game-library UI, bounded folder/ZIP inspection, registry-driven engine detection, persisted import metadata, legacy metadata decoding, a real bounded LCF container parser, and a minimal parser-backed RM2000/2003 runtime bootstrap validated against pinned EasyRPG TestGame fixtures. Full gameplay is not playable yet; the immediate critical path is expanding faithful RM2000/2003 parsing before event execution and rendering.

The repository uses pure C#/.NET through the Godot 4.7.2 .NET editor. The Godot project (including `project.godot`, `UniversalRPG.csproj` and `UniversalRPG.sln`) lives under `project/`; development docs, `scripts/validate.sh`, and the pinned Godot runtime under `tools/godot/` stay at the repository root. `scripts/validate.sh` runs restore, build, Godot import, and the C# core/smoke suite. The headless runner passed `261/261` tests.

## Phase Status Overview

| Phase | Description | Status | Notes |
|-------|-------------|--------|-------|
| 0 | Repository audit | ✅ Complete | Initial setup |
| 1 | Runtime foundation | ✅ Complete | Core abstractions implemented |
| 1.5 | Application foundation | ✅ Complete | Library, plugin detection/selection wiring, persistence, localization, import safety |
| 2 | RM2000/2003 parser | 🚧 In progress | Real LCF reader + initial LDB/LMU/LSD decoding |
| 3 | RM2000/2003 rendering | 🚧 In progress | Renderer-neutral framebuffer adapter implemented; Godot presentation, sprites, and camera remain |
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
- Persisted candidate/selection/evidence/compatibility records in `user://library.cfg`
- RTP dependency, custom script/plugin, and native library metadata
- Explicit user-provided RTP registry/resolution is bounded, deterministic, and data-only; no proprietary RTP data is bundled or auto-discovered
- Runtime selection through exact plugin IDs, capability checks, platform checks, and no-fallback errors

**Limitations:**
- All built-in targets except Unite have a safe bounded bootstrap; RM2K/RM2K3 additionally parse LDB/LMT/LMU; full gameplay runtime is not implemented
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
- Incomplete gameplay runtime | High | RM2K/RM2K3 bootstrap loads data and ticks deterministically; movement state is bounded and tested, while renderer/events/presentation/systems remain planned |

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
| Input abstraction | 1 | Low (planned) |
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
