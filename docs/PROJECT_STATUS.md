# UniversalRPG — Project Status

> **Last Updated:** 2026-08-21
> **Current Phase:** Phase 2 — RM2000/2003 Parser in progress

## Executive Summary

The project has a Godot 4.7.2 application foundation, localized game-library UI, bounded folder scanner, generation detection, legacy metadata decoding, and a real bounded LCF container parser with initial RM2000/2003 LDB/LMU/LSD decoding validated against pinned EasyRPG TestGame fixtures. No runtime backend is playable yet. The immediate critical path is expanding faithful RM2000/2003 parsing before rendering or event execution.

The repository uses pure C#/.NET through the Godot 4.7.2 .NET editor. `project.godot` names the `UniversalRPG` assembly, `UniversalRPG.csproj` and `UniversalRPG.sln` describe the .NET project, and `scripts/validate.sh` runs restore, build, Godot import, and the C# core/smoke suite. The headless runner passed `128/128` tests.

## Phase Status Overview

| Phase | Description | Status | Notes |
|-------|-------------|--------|-------|
| 0 | Repository audit | ✅ Complete | Initial setup |
| 1 | Runtime foundation | ✅ Complete | Core abstractions implemented |
| 1.5 | Application foundation | ✅ Complete | Library, detection UI, localization, initial import safety |
| 2 | RM2000/2003 parser | 🚧 In progress | Real LCF reader + initial LDB/LMU/LSD decoding |
| 3 | RM2000/2003 rendering | 📋 Planned | Depends on Phase 2 |
| 4 | Event interpreter | 📋 Planned | Depends on Phase 2 |
| 5 | Full RM2000/2003 systems | 📋 Planned | Depends on Phase 4 |
| 6 | Compatibility work | 📋 Planned | Real-world testing |
| 7 | Enhanced Mode | 📋 Planned | After Faithful Mode stable |
| 8 | RGSS runtime | 📋 Planned | After Phase 5 |
| 9 | MV/MZ runtime | 📋 Planned | After Phase 8 |
| 10 | Native plugin compat | 🔬 Research | Long-term |
| 11 | Android compat | 🔬 Research | Long-term |

## Implemented Systems

### 1. VirtualFileSystem (`src/core/virtual_filesystem.cs`)

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

### 2. VirtualClock (`src/core/virtual_clock.cs`)

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

### 3. GameDetector (`src/game_detector/game_detector.cs`)

**Status:** Implemented

**Features:**
- Multi-signal detection (Game.ini, RGSS DLLs, archives, directory structure)
- Confidence scoring (Low/Medium/High)
- Evidence collection
- RTP dependency detection
- Custom script/plugin detection
- Native library detection
- Unknown runtime warnings

**Limitations:**
- No actual PE/DLL parsing (stubbed)
- No file signature verification
- No archive content inspection
- Detection relies on file/directory presence, not content analysis

**Test Coverage:** 15 deterministic detection tests

---

### 4. CompatibilityProfile (`src/compatibility/compatibility_profile.cs`)

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
| No error reporting system | Medium | GameDetector._get_file_version() and _get_dll_imports() are stubs |

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

1. Implement the LMT map-tree parser with bounded parsing and tests
2. Expand typed LDB/LMU decoding incrementally; do not guess undocumented offsets/fields
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

The C# migration has been validated with the local Godot 4.7.2 stable .NET editor on Windows: `dotnet build` passed, script registration succeeded after PascalCase file renames, and the headless C# core/smoke runner passed `128/128`. The only remaining known output is Godot's non-fatal internal `EditorSettings` message during headless editor shutdown.
