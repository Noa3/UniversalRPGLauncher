# UniversalRPG — Development Roadmap

> **Last reviewed:** 2026-09-11  
> **Primary implementation track:** RM2000/2003 faithful runtime  
> **Architecture:** shared core + engine-specific compiled plugins

This roadmap describes the long-term product direction. It is intentionally not a granular task board. `SESSION_STATE.md` records only the current objective/next action, while `docs/PROJECT_STATUS.md` lists immediate priorities. Agents should choose the next coherent implementation slice directly from repository evidence rather than maintaining a Kanban.

## Runtime Milestone Vocabulary

Use the following levels independently per engine:

| Level | Meaning |
|---|---|
| L0 | Detection |
| L1 | Bounded metadata/data parsing |
| L2 | Core data structures load |
| L3 | Map/presentation foundation |
| L4 | Basic event/script execution |
| L5 | Exploration/simple game path playable |
| L6 | Menus/system services |
| L7 | Save/load compatibility |
| L8 | Battle/major game systems |
| L9 | Broad compatibility with authorized fixtures |
| L10 | Optional enhanced features |

Do not describe an engine as “supported” without stating the milestone/capability level.

---

## Track A — Shared Application and Safety Foundation

**Status:** Implemented foundation; ongoing hardening

Implemented:

- Godot 4.7.2 C#/.NET project
- persistent game library
- trusted compiled engine plugin contracts
- bounded folder/ZIP inspection
- ranked detection and ambiguity handling
- partial-vs-malformed inspection distinction
- library persistence
- localization
- compatibility profiles/reporting
- deterministic runtime clock
- validation script and CI workflow
- RTP registry/diagnostic primitives
- runtime selection gated by explicit capabilities
- defensive runtime-creation capability checks

Ongoing:

- runtime-wide VFS containment
- platform export validation
- Android/iOS import integration
- stronger fuzz/malformed-input coverage
- permission model required before script/native execution
- continued simplification/removal of dead pseudo-runtime abstractions

---

## Track B — RM2000 / RM2003

**Status:** Active; strongest runtime foundation

### Completed foundation

- [x] LCF framing / BER reader
- [x] LMT parser
- [x] bounded LDB typed slices
- [x] LMU map/event/page/command metadata slices
- [x] unknown-field retention for implemented structures
- [x] read-only LSD framing model
- [x] parser-backed runtime lifecycle
- [x] deterministic simulation state/clock
- [x] player movement/transfer state
- [x] event scheduler
- [x] expanding verified event-command interpreter
- [x] renderer-neutral framebuffer/sprite/presentation state
- [x] RTP registry/diagnostic foundation
- [x] runtime-owned bounded save codec
- [x] input/presentation handoff slices

### Near-term goals

- [ ] verify and implement chipset/passability semantics from authoritative field evidence
- [ ] complete enough tile/sprite/window presentation for representative real maps
- [ ] expand event command coverage from verified semantics
- [ ] separate RM2000/RM2003 behavior where it diverges
- [ ] implement audio path
- [ ] implement basic menus/system flow
- [ ] establish original save semantic compatibility
- [ ] reach an authorized end-to-end exploration milestone
- [ ] expand toward battle/system parity

### Completion gate

RM2K/RM2K3 should not be called broadly playable until real authorized fixtures can launch, move, interact, transfer maps, save/load appropriately, and exercise representative menus/events without external `RPG_RT.exe`.

---

## Track C — WOLF RPG Editor

**Status:** Experimental plain-data runtime slice

WOLF is an independent engine family.

Implemented foundation:

- [x] conservative detection
- [x] protected-data refusal boundary
- [x] experimental unencrypted/plain-data readers
- [x] database/map/event model foundation
- [x] bounded deterministic event VM slice
- [x] runtime lifecycle regression tests

Next milestones:

- [ ] define supported WOLF versions
- [ ] pin authorized native format documentation/fixtures
- [ ] replace synthetic/plain envelopes with native readers where justified
- [ ] expand opcode/event semantics with per-opcode tests
- [ ] add renderer/input/audio/UI/save layers
- [ ] validate representative authorized real games

Protected/encrypted data is not a prerequisite for initial WOLF support and must not be bypassed merely to increase compatibility.

---

## Track D — RGSS Core / RPG Maker XP, VX, VX Ace

**Status:** Detection/parsing boundary; runtime not implemented

The previous metadata-only RGSS pseudo-runtime has been removed. Future RGSS work should start from a real embedded Ruby boundary rather than resurrecting a lifecycle placeholder.

Shared architecture:

```text
XP / VX / VX Ace plugin
        |
        v
IRubyVm
        |
        v
RGSS compatibility core
 ├── RGSS1 profile
 ├── RGSS2 profile
 └── RGSS3 profile
        |
        v
URPG platform/runtime services
```

Tasks:

- [ ] select embeddable Ruby implementation with license/version/platform review
- [ ] implement `IRubyVm` boundary
- [ ] implement serialized data/archive readers per generation
- [ ] implement common RGSS value/data types
- [ ] implement Graphics / Bitmap / Sprite / Viewport / Window / Tilemap / Plane
- [ ] implement Input / Audio / Font / Rect / Color / Tone / Table
- [ ] implement RGSS1 profile and XP boot path
- [ ] implement RGSS2 profile and VX boot path
- [ ] implement RGSS3 profile and VX Ace boot path
- [ ] define Win32API compatibility policy
- [ ] add default-script and representative third-party-script fixtures
- [ ] add save/load compatibility per generation

Do not require a separately installed Ruby runtime.

---

## Track E — JavaScript Core / RPG Maker MV and MZ

**Status:** Detection + bounded metadata; runtime not implemented

Shared architecture:

```text
MV / MZ plugin
      |
      v
IJavaScriptVm
      |
      v
browser/RPG Maker compatibility layer
      |
      v
URPG services
```

Tasks:

- [ ] select embeddable JavaScript VM with license/platform/sandbox review
- [ ] implement `IJavaScriptVm`
- [ ] implement bounded script loading and error diagnostics
- [ ] implement required `window`, timers, `performance`, `requestAnimationFrame`
- [ ] implement Canvas/WebGL presentation bridge
- [ ] implement WebAudio/audio bridge
- [ ] implement Image/fetch/XMLHttpRequest/storage subset
- [ ] implement input/gamepad/pointer APIs
- [ ] implement MV profile
- [ ] implement MZ profile
- [ ] implement plugin compatibility reporting
- [ ] define limited Node/NW.js compatibility policy
- [ ] implement save/load and representative project fixtures

No imported JavaScript should run outside the explicit sandbox/runtime boundary.

---

## Track F — RPG Maker 95

**Status:** Detection/research only

- [x] conservative detection boundary
- [ ] pin legal/verified file-format references
- [ ] create representative authorized fixtures
- [ ] implement bounded project/map/database/event readers
- [ ] define deterministic simulation/runtime
- [ ] implement renderer/input/audio/UI/save
- [ ] validate representative real games

RM95 must not be treated as RM2000 merely because both are legacy engines.

---

## Track G — RPG Tsukūru Dante 98

**Status:** Detection/research only

- [x] explicit research-marker detector
- [ ] determine practical/legal source-media scope
- [ ] document file/media format evidence
- [ ] create safe fixtures
- [ ] implement parser only after format confidence exists
- [ ] keep PC-98/runtime concerns separate from RM95

Do not alias Dante 98 data to RM95 without evidence.

---

## Track H — RPG Maker Unite

**Status:** Research/detection only

RPG Maker Unite is Unity-based. Generic Unity exports are not enough to prove Unite provenance and a general Unity compatibility runtime is outside the current product scope.

- [ ] decide whether Unite should remain detection-only permanently
- [ ] obtain authorized project/export fixtures if deeper support is considered
- [ ] investigate conversion/import possibilities rather than promising arbitrary Unity-runtime compatibility

This track must not block the primary engines.

---

## Track I — Native Plugins / DLL / Win32 Compatibility

**Status:** Long-term research

Sequence:

1. metadata-only PE/native inspection
2. compatibility database and hashes
3. high-level replacements for known plugins
4. constrained Win32 API shims
5. only then investigate controlled native execution
6. ARM64/x86 translation is a separate advanced problem

Imported native code must never be executed automatically just because it exists.

---

## Track J — Enhanced Mode

**Status:** Partial foundations; broad polish deferred until faithful runtime milestones

Potential capabilities:

- integer/pixel-perfect scaling
- high-resolution presentation
- high-refresh presentation without changing simulation Hz
- shaders
- controller profiles/remapping
- Android touch controls
- fast-forward / slow-motion
- screenshots
- asset overrides
- translation packs
- accessibility
- save states
- rewind
- experimental widescreen

Every enhancement must be capability-gated and independently disableable.

---

## Track K — Platform and Release Readiness

Targets:

- Windows x86-64
- Linux x86-64
- Android ARM64
- macOS
- iOS

Required before release claims:

- clean reproducible build
- platform-specific import/storage flow
- export templates/toolchains documented
- signed package where platform requires it
- smoke test on actual target
- save path verification
- controller/input verification
- performance/memory profiling
- third-party license audit

---

## Cross-Cutting Requirements

### Testing

- parser unit tests
- malformed/fuzz regression cases
- event/script VM tests
- runtime lifecycle tests
- runtime-capability boundary tests
- golden rendering tests when presentation becomes stable
- save/load tests
- engine-specific authorized fixtures
- end-to-end representative play paths

### Security

- bounded parsing
- no detector-time execution
- VFS containment
- dangerous host APIs denied by default
- explicit capabilities for network/clipboard/native behavior
- sandbox/watchdog limits before Ruby/JavaScript/native execution
- detection/parsing/runtime capabilities remain separate and fail closed

### Documentation

Living:

- `README.md`
- `SESSION_STATE.md`
- `AGENTS.md`
- `HERMES_AUTONOMOUS_PROMPT.md`
- `docs/PROJECT_STATUS.md`
- `docs/ARCHITECTURE.md`
- `docs/ROADMAP.md`
- `docs/ENGINE_DETECTION.md`
- `docs/ENGINE_PLUGINS.md`
- `docs/COMPATIBILITY.md`
- `docs/IMPORT_SECURITY.md`

Historical dated handoffs/reports should remain snapshots rather than being continuously rewritten.

## Recommended Development Order

1. keep shared build/test/security foundation green
2. reach a meaningful RM2K/RM2K3 playable milestone
3. improve WOLF native-format fidelity in parallel where verified fixtures exist
4. build RGSS core, then XP → VX → VX Ace
5. build shared JavaScript core, then MV → MZ
6. deepen RM95/Dante only when format evidence/fixtures justify it
7. treat Unite as research
8. expand native DLL/Win32 compatibility only after normal runtimes are stable
9. grow Enhanced Mode alongside stable engine capabilities, never ahead of correctness
