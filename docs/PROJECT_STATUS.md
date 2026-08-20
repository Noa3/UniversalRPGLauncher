# UniversalRPG — Project Status

> **Last Updated:** 2026-08-20
> **Current Phase:** Phase 1.5 — Application Foundation complete; Phase 2 next

## Executive Summary

The project now has a validated Godot 4.7.2 application, localized game-library UI, bounded folder scanner, improved generation detector, legacy metadata decoding, and export presets. No runtime backend is playable yet. RM2000/2003 format correctness remains the next critical path.

## Phase Status Overview

| Phase | Description | Status | Notes |
|-------|-------------|--------|-------|
| 0 | Repository audit | ✅ Complete | Initial setup |
| 1 | Runtime foundation | ✅ Complete | Core abstractions implemented |
| 1.5 | Application foundation | ✅ Complete | Library, detection UI, localization, initial import safety |
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
| Legacy test harness missing | High | Existing `extends Test` suites are not connected to a test runner |
| No CI | Medium | No automated build/testing |
| No export pipeline | Medium | Presets exist; signed/release exports are not automated |
| Legacy encoding varies by platform | High | CP932 decoder must be tested on every target, especially Android/iOS |
| No safe archive importer | High | Folder scans are bounded, but archive staging is not implemented |
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
| Linux headless | ✅ Tested | Godot 4.7.2 import and all UI locales start |
| Windows Export | ⏸️ Not tested | Preset present; templates/toolchain needed |
| Linux Export | ⏸️ Not tested | Preset present; templates needed |
| macOS/iOS Export | ⏸️ Not tested | Requires macOS, Xcode, signing, templates |
| Android Export | ⏸️ Not tested | Preset present; Android SDK/templates needed |

## Next Immediate Tasks

1. Connect or replace the legacy test harness and add malicious import fixtures
2. Test CP932/Shift-JIS decoding on every target platform
3. Implement real LCF parsing from documented, legal fixtures
4. Add Android/iOS document-provider imports
5. Produce unsigned desktop test exports once templates are installed

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
