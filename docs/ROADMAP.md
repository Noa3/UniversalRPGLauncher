# UniversalRPG — Development Roadmap

> **Last Updated:** 2026-08-17
> **Current Phase:** Phase 1 — Runtime Foundation (IN PROGRESS)

## Phase 0 — Repository Audit ✅

**Status:** Complete (initial repository setup)

- [x] Create repository structure
- [x] Document architecture
- [x] Set up project configuration
- [ ] Audit existing codebase (N/A — fresh repository)

**Deliverables:**
- `docs/ARCHITECTURE.md` — Complete architecture documentation
- `docs/ROADMAP.md` — This file
- `project.godot` — Godot project configuration
- Directory structure matching the design

---

## Phase 1 — Runtime Foundation ✅

**Status:** Complete (core abstractions implemented)

### Implemented

- [x] VirtualFileSystem
  - Multi-mount merging (game, override, RTP, save)
  - Case-insensitive path resolution
  - Path traversal protection
  - Archive access preparation

- [x] VirtualClock
  - Deterministic simulation timing
  - Speed control (0.5x–10x, pause)
  - Scheduled callbacks
  - Frame-rate decoupling

- [x] GameDetector
  - Multi-signal detection (Game.ini, RGSS DLLs, archives, directory structure)
  - Confidence scoring (Low/Medium/High)
  - Evidence collection
  - RTP dependency detection
  - Custom script/plugin detection
  - Native library detection
  - Unknown runtime warnings

- [x] CompatibilityProfile
  - Extensible JSON-based profiles
  - SHA-256 hash matching
  - Engine-specific profiles
  - Per-game flags
  - Global flags with per-game override

### Next Steps (Phase 1 completion)

- [ ] Add unit tests for VirtualFileSystem
- [ ] Add unit tests for GameDetector
- [ ] Add unit tests for CompatibilityProfile
- [ ] Create synthetic test fixtures for detection
- [ ] Export and test Godot project

---

## Phase 2 — RM2000/2003 Parser

**Status:** Planned

### Goals

Load an RM2000/2003 project and inspect:
- Maps
- Events
- Database
- Resources

### Tasks

- [ ] RM2K binary format specification (research)
- [ ] RM2KParser — core parser class
- [ ] RM2KDatabase — database structures
- [ ] RM2KMap — map tree parsing
- [ ] RM2KEvent — event data structures
- [ ] RM2KSave — save format parsing
- [ ] Parser error handling (malformed data)
- [ ] Parser unit tests with synthetic fixtures

### Key Design Decisions

- Parse into structured data, not directly into Godot nodes
- Unknown fields preserved/skipped safely
- Separate parsing from interpretation logic

---

## Phase 3 — RM2000/2003 Rendering

**Status:** Planned

### Goals

Open a map and render it accurately.

### Tasks

- [ ] Map tile rendering
- [ ] Character sprite rendering
- [ ] Player movement
- [ ] Camera system
- [ ] Picture layer
- [ ] Text/window rendering
- [ ] Render order preservation
- [ ] Golden image tests

---

## Phase 4 — Event Interpreter

**Status:** Planned

### Goals

Implement enough event commands for basic games.

### Priority Commands

1. Movement commands
2. Message display
3. Switch/variable operations
4. Conditional branches
5. Event calls/jumps
6. Map transfers
7. Wait/delay commands
8. Picture manipulation
9. Audio commands
10. Labels/jumps/loops

### Tasks

- [ ] EventCommand data structure
- [ ] EventContext — execution context
- [ ] EventStack — call stack management
- [ ] EventFrame — frame scheduling
- [ ] EventWaitState — non-blocking waits
- [ ] ParallelEvent support
- [ ] AutorunEvent support
- [ ] CommonEvent support
- [ ] Nested event call support
- [ ] Event trace/debug system
- [ ] Interpreter unit tests

---

## Phase 5 — Full RM2000/2003 Systems

**Status:** Planned

### Goals

Broad RM2000/2003 game compatibility.

### Tasks

- [ ] Menu system (title, game over, save, load)
- [ ] Inventory system
- [ ] Party management
- [ ] Equipment system
- [ ] Skills database
- [ ] States/status effects
- [ ] Battle system
- [ ] Save/load system
- [ ] Screen transitions
- [ ] Animation system

---

## Phase 6 — Compatibility Work

**Status:** Planned

### Goals

Use real-world test cases to refine compatibility.

### Tasks

- [ ] Encoding quirks (CP932, Shift_JIS, EUC-JP)
- [ ] Runtime quirks (version-specific bugs)
- [ ] Common patches (community fixes)
- [ ] Known plugin HLE implementations
- [ ] Compatibility database expansion
- [ ] Regression test suite

---

## Phase 7 — Enhanced Mode

**Status:** Planned (only after Faithful Mode is dependable)

### Goals

Modern improvements without altering gameplay semantics.

### Tasks

- [ ] Integer scaling
- [ ] High-resolution presentation
- [ ] Shader system (nearest, bilinear, CRT, scanlines)
- [ ] Controller support (Xbox, PlayStation, Steam Deck)
- [ ] Touch UI (Android)
- [ ] Fast-forward/slow-motion
- [ ] Screenshot system
- [ ] Asset override system
- [ ] Per-game enhancement profiles

---

## Phase 8 — RGSS Runtime

**Status:** Planned (after Phase 5)

### Goals

Support RPG Maker XP, VX, and VX Ace.

### Tasks

- [ ] RGSSRuntime — core runtime abstraction
- [ ] RubyVMAdapter — embedded Ruby VM
- [ ] RGSS1 API (XP)
- [ ] RGSS2 API (VX)
- [ ] RGSS3 API (VXAce)
- [ ] Win32API compatibility dispatcher
- [ ] Native DLL inspector
- [ ] Compatibility profiles per RGSS version
- [ ] Automated compatibility tests

---

## Phase 9 — MV/MZ Runtime

**Status:** Planned (after Phase 8)

### Goals

Support RPG Maker MV and MZ games.

### Tasks

- [ ] JavaScriptRuntime — IJavaScriptVM interface
- [ ] Browser compatibility API (window, document, etc.)
- [ ] Canvas rendering compatibility
- [ ] WebAudio compatibility
- [ ] WebGL compatibility
- [ ] DOM compatibility layer
- [ ] Storage compatibility (localStorage, IndexedDB)
- [ ] Plugin system with compatibility reporting
- [ ] Node compatibility layer (progressive)

---

## Phase 10 — Native Plugin Compatibility

**Status:** Research (long-term)

### Goals

Support Windows DLL plugins on Linux/Android.

### Tasks

- [ ] PE parser (binary inspection)
- [ ] Win32 API shim layer
- [ ] Known native plugin replacements (HLE)
- [ ] Controlled Windows plugin loading
- [ ] Architecture verification
- [ ] Sandbox model
- [ ] Crash containment

---

## Phase 11 — Advanced Android Compatibility

**Status:** Research (long-term)

### Goals

Support x86 Windows plugins on ARM64 Android.

### Tasks

- [ ] x86 execution layer research
- [ ] ARM64 translation strategies
- [ ] Windows ABI compatibility
- [ ] Native plugin isolation
- [ ] Android touch control layouts
- [ ] Android storage permissions

---

## Cross-Cutting Concerns

### Security

- [ ] Virtual filesystem sandbox
- [ ] Plugin loading policy
- [ ] Network access control
- [ ] Clipboard access control
- [ ] Per-game permission UI
- [ ] Crash isolation

### Testing

- [ ] Unit tests for all parsers
- [ ] Interpreter tests
- [ ] Integration tests
- [ ] Golden image rendering tests
- [ ] Save/load tests
- [ ] Timing tests
- [ ] Compatibility regression tests

### Documentation

- [ ] docs/ARCHITECTURE.md (maintained)
- [ ] docs/COMPATIBILITY.md
- [ ] docs/RUNTIME_RM2K.md
- [ ] docs/RUNTIME_RGSS.md
- [ ] docs/RUNTIME_MV_MZ.md
- [ ] docs/NATIVE_PLUGINS.md
- [ ] docs/SECURITY.md
- [ ] THIRD_PARTY_LICENSES.md

### Performance

- [ ] Profiling infrastructure
- [ ] Memory allocation analysis
- [ ] Rendering batch optimization
- [ ] Startup speed optimization
- [ ] Mobile performance targets

---

## Version History

| Version | Phase | Status | Date |
|---------|-------|--------|------|
| 0.1.0 | Phase 0 | Complete | 2026-08-17 |
| 0.2.0 | Phase 1 | In Progress | 2026-08-17 |
