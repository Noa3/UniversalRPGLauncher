# UniversalRPG — Development Roadmap

> **Last Updated:** 2026-08-20
> **Current Phase:** Phase 2 — RM2000/2003 Parser

## Phase 0 — Repository Audit ✅

**Status:** Complete (initial repository setup)

- [x] Create repository structure
- [x] Document architecture
- [x] Set up project configuration
- [x] Add Godot .NET project and solution metadata
- [x] Add reproducible restore/build/import/test validation entrypoint
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

## Phase 1.5 — Application Foundation ✅

**Status:** Complete for desktop MVP; mobile import integration remains

- [x] Valid Godot 4.7.2 project and responsive start scene
- [x] Persistent user-selected games directory
- [x] Bounded library scan with link/junction avoidance
- [x] Real LCF, RGSS, MV, and MZ detection signals
- [x] Honest runtime-support state and disabled launch action (RGSS/XP/VX/VX Ace, RM95, MV/MZ, and Unite remain detection-only)
- [x] English-default localized UI with German, Spanish, French, Japanese, Korean, and Simplified Chinese
- [x] Bundled CJK-capable font
- [x] Initial UTF-8/CP932/Shift-JIS metadata decoder
- [x] Trusted in-process engine plugin contracts and deterministic built-in catalog
- [x] Bounded folder/ZIP detection with persisted candidates and safe runtime selection
- [x] Windows, Linux, macOS, Android, and iOS export presets
- [ ] Android Storage Access Framework import
- [ ] iOS document picker import
- [ ] Cover art, favorites, search, sorting, and recent play time

---

## Phase 2 — RM2000/2003 Parser

**Status:** In progress

### Goals

Load an RM2000/2003 project and inspect:
- Maps
- Events
- Database
- Resources

### Tasks

- [x] Real LCF container framing/BER reader with hard limits
- [x] RM2KParser — core parser class
- [x] RM2KDatabase — serializable database data model (field decoding still incomplete)
- [x] Initial LMU base fields: chipset, dimensions, tile layers, event metadata
- [x] Initial LSD top-level container/chunk parsing
- [x] Parser error handling for truncation/invalid BER/oversized data/dimensions
- [x] Parser unit tests with synthetic real-LCF encodings
- [x] Validate parser against legal/reproducible real-world fixtures
- [x] Minimal RM2K/RM2K3 parser-backed runtime bootstrap with deterministic ticking
- [ ] LMT map-tree parser
- [ ] Full typed LDB section decoding
- [ ] Full LMU event/page/command data decoding
- [ ] Preserve/report unknown fields consistently

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
- [ ] Configurable virtual touch gamepad and per-game layouts
- [ ] Physical controller remapping and mouse/touch emulation
- [ ] Save backup/import/export and conflict-safe device transfer
- [ ] Non-destructive translation/patch packs (PO/XLIFF)
- [ ] Translation memory, glossary, font packs, and overflow diagnostics
- [ ] Optional compatibility/debug/cheat tools with explicit safety policy

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

- [x] Detection never executes imported files
- [x] Scanner depth, link, hidden-directory, and metadata-size limits
- [ ] Canonical root containment on every runtime access
- [ ] Safe archive staging and zip-bomb limits
- [ ] Virtual filesystem runtime sandbox
- [ ] Plugin loading policy
- [ ] Network access control
- [ ] Clipboard access control
- [ ] Per-game permission UI
- [ ] Crash isolation

### Testing

- [x] Plugin contract, detection, archive, persistence, and lifecycle regression tests
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
- [x] docs/IMPORT_SECURITY.md
- [x] docs/LOCALIZATION.md
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
| 0.2.1 | Phase 1.5 | Application foundation | 2026-08-20 |
