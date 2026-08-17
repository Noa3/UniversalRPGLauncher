# UniversalRPG — Project Status

> **Last Updated:** 2026-08-17
> **Current Phase:** Phase 1 — Runtime Foundation

## Executive Summary

This is a **fresh repository** at the start of Phase 1. The core runtime abstractions (VirtualFileSystem, VirtualClock, GameDetector, CompatibilityProfile) have been implemented as a foundation for all subsequent work.

## Phase Status Overview

| Phase | Description | Status | Notes |
|-------|-------------|--------|-------|
| 0 | Repository audit | ✅ Complete | Initial setup |
| 1 | Runtime foundation | ✅ Complete | Core abstractions implemented |
| 2 | RM2000/2003 parser | 📋 Planned | Next priority |
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

### 1. VirtualFileSystem (`src/core/virtual_filesystem.gd`)

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

### 2. VirtualClock (`src/core/virtual_clock.gd`)

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

**Test Coverage:** None yet

---

### 3. GameDetector (`src/game_detector/game_detector.gd`)

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

**Test Coverage:** None yet

---

### 4. CompatibilityProfile (`src/compatibility/compatibility_profile.gd`)

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

**Test Coverage:** None yet

## Technical Debt

| Item | Severity | Description |
|------|----------|-------------|
| No tests | High | Core systems lack test coverage |
| No CI | Medium | No automated build/testing |
| No export pipeline | Medium | No automated export for Windows/Linux/Android |
| Empty architecture docs | Low | Sections marked TODO in docs |
| No error reporting system | Medium | GameDetector._get_file_version() and _get_dll_imports() are stubs |

## Missing Core Components (Planned)

| Component | Phase | Priority |
|-----------|-------|----------|
| RM2K parser | 2 | Critical |
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
| Windows Export | ⏸️ Not tested | Needs Godot editor |
| Linux Export | ⏸️ Not tested | Needs Godot editor |
| Android Export | ⏸️ Not tested | Needs Godot editor |

## Next Immediate Tasks

1. **Write unit tests** for VirtualFileSystem, GameDetector, CompatibilityProfile
2. **Create synthetic test fixtures** for game detection
3. **Export the project** to verify it builds
4. **Start Phase 2** — RM2000/2003 data format research and parsing

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
