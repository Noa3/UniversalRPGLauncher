# UniversalRPG — Project Status

> **Last reviewed:** 2026-09-09  
> **Repository baseline reviewed:** `main` at commit `782ea66141e494d32929a9cc41056523177888eb`  
> **Primary development focus:** RM2000/2003 faithful runtime foundation

## Executive Summary

UniversalRPG has a working Godot 4.7.2 C#/.NET application foundation, bounded game inspection, a registry-driven engine plugin architecture, persistent library metadata, compatibility diagnostics, and a substantial automated test harness.

It is **not yet a general playable replacement runtime**.

RM2000/2003 are the only RPG Maker generations with a meaningful parser-backed gameplay/runtime foundation. WOLF RPG Editor has a deliberately narrow experimental unencrypted plain-data runtime/VM slice. XP/VX/VX Ace and MV/MZ are currently detection/parsing/metadata boundaries only. RM95, Dante 98 and Unite are research-oriented detection boundaries.

The last recorded canonical validation on `main` is **296/296 headless tests passed**, with clean .NET build and `scripts/validate.sh`. That is historical evidence for the reviewed commit; run validation again after any code change.

## Engine Status Matrix

| Engine | Detection | Parsing/data | Runtime execution | Overall |
|---|---:|---:|---:|---|
| Dante 98 | Yes, explicit research marker | No | No | Research/detection-only |
| RPG Maker 95 | Yes, conservative signatures | No native parser | No | Research/detection-only |
| RPG Maker 2000 | Yes | Partial LCF | Partial deterministic runtime | **Primary active implementation** |
| RPG Maker 2003 | Yes | Partial shared LCF | Partial shared deterministic runtime | **Primary active implementation** |
| RPG Maker XP | Yes | Bounded metadata/signatures | No Ruby/RGSS execution | Detection/parsing-only |
| RPG Maker VX | Yes | Bounded metadata/signatures | No Ruby/RGSS execution | Detection/parsing-only |
| RPG Maker VX Ace | Yes | Bounded metadata/signatures | No Ruby/RGSS execution | Detection/parsing-only |
| RPG Maker MV | Yes | Bounded metadata | No JavaScript execution | Detection/parsing-only |
| RPG Maker MZ | Yes | Bounded metadata/database inventory | No JavaScript execution | Detection/parsing-only |
| WOLF RPG Editor | Yes | Experimental plain-data readers | Experimental bounded VM/runtime | Partial research runtime |
| RPG Maker Unite | Candidate detection | No | No | Research-only |

## Current Shared Infrastructure

### Application/library

Implemented:

- Godot launcher/application shell
- persistent game library
- bounded folder and ZIP inspection
- deterministic ranked engine detection
- ambiguous/unknown/malformed/partial diagnostics
- engine/runtime registry and selector
- persisted import metadata
- multilingual launcher UI
- compatibility report infrastructure

### Bounded inspection

Current defaults:

- maximum directory depth: 4
- maximum inspected entries: 4096
- maximum file metadata read: 1 MiB
- maximum archive uncompressed inspection budget: 64 MiB
- reparse points skipped
- unsafe archive paths rejected
- entry-budget exhaustion represented as a partial/advisory scan

### Core/runtime services

Implemented or partially implemented:

- `VirtualClock`
- virtual filesystem primitives
- compatibility profile/database primitives
- RTP registry and diagnostics
- runtime capability checks
- renderer-neutral RM2K framebuffer/sprite/presentation structures
- runtime-owned bounded save codec infrastructure
- structured diagnostics

## RM2000/2003 Status

### Implemented foundations

- real bounded LCF framing and BER decoding
- LDB parsing with typed portions and unknown-field retention
- LMT parsing
- LMU map/event metadata parsing
- read-only LSD framing model
- parser-backed runtime initialization
- deterministic simulation clock
- map/player simulation state
- event scheduling
- action/touch/autorun/parallel-related runtime infrastructure
- verified event-command slices including switches/variables, messages/choices/input, transfer validation, gold/items/party mutations and other bounded control flow
- renderer-neutral tile layers
- sprite descriptors/camera-related state
- presentation state
- RTP resolution/diagnostics primitives
- bounded regression fixtures

### Main blockers / gaps

- chipset/passability mapping still needs verified field semantics and distinguishing fixtures
- event-command coverage is far from complete
- RM2003-specific behavioral differences need dedicated parity tests
- no complete faithful Godot renderer
- audio path incomplete
- menus/system scenes incomplete
- battle system not compatible
- original save semantics incomplete
- no broad end-to-end real-game playable milestone yet

## WOLF Status

Implemented:

- WOLF detection
- explicit protected-data refusal boundary
- unencrypted synthetic/plain-data reader models
- database/map/event reader foundation
- bounded deterministic `WolfEventVm`
- runtime lifecycle tests

Not implemented:

- complete native WOLF file formats/version coverage
- protected/encrypted data support
- faithful renderer
- audio/input/UI
- save/battle parity
- broad authorized real-game fixtures

WOLF must remain a separate runtime family rather than being forced into RM2K data/event models.

## RGSS Status — XP / VX / VX Ace

Current plugins advertise **Detection + Parsing**, not Runtime.

Implemented:

- generation-specific signatures
- bounded metadata inspection
- archive/runtime identification
- runtime selection correctly refuses unsupported execution

Missing:

- embedded Ruby VM
- RGSS1/2/3 APIs
- serialized-data/archive decoding sufficient for gameplay
- script execution
- graphics/audio/input/window APIs
- Win32API policy
- save/gameplay compatibility

## MV / MZ Status

Current plugins advertise **Detection + Parsing**, not Runtime.

Implemented:

- bounded web-game signatures
- top-level JSON metadata parsing
- MV encrypted-asset diagnostics
- stricter MZ runtime signature validation
- bounded MZ database inventory helpers
- malformed/oversized metadata tests

Missing:

- embedded JavaScript VM
- RPG Maker browser API compatibility
- plugin execution
- rendering/audio/input scenes
- save/load
- Node/NW.js compatibility policy

## Legacy / Research Boundaries

### Dante 98

A compiled detection plugin exists and only recognizes an explicit research marker. No PC-98 media parser or runtime exists.

### RPG Maker 95

A conservative detector exists using documented companion data signatures. No runtime exists.

### RPG Maker Unite

Only candidate/research detection exists. Generic Unity exports do not prove RPG Maker Unite provenance.

## Validation and CI

- `scripts/validate.sh` is the canonical local validation command.
- A GitHub validation workflow exists; therefore old documentation that says “No CI” is obsolete.
- Export presets exist, but release exports are not yet validated across all target platforms.
- Historical counts in session handoffs should not be copied forward as current truth.

## Immediate Priorities

1. keep `main` build/test green and repair P0 regressions first
2. finish RM2K/2003 passability and map-rendering prerequisites using verified semantics
3. continue verified RM2K/2003 event/runtime slices toward a small real playable milestone
4. separate RM2K vs RM2K3 behavior where required
5. improve real authorized fixtures and end-to-end compatibility evidence
6. continue WOLF native-format work only from authorized/verified specifications and fixtures
7. select and prototype an embedded Ruby boundary for RGSS after the RM2K milestone is sufficiently stable
8. select and sandbox a JavaScript VM for MV/MZ
9. keep RM95/Dante/Unite as research tracks unless evidence justifies promotion

## Important Documentation Rule

Living documents:

- `README.md`
- `KANBAN.md`
- `SESSION_STATE.md`
- `docs/PROJECT_STATUS.md`
- `docs/ARCHITECTURE.md`
- `docs/ROADMAP.md`

Historical documents such as `SESSION_HANDOFF_YYYY-MM-DD.md` and dated QA reports describe the repository at a particular time and should not override current source/tests.

## Known Strategic Decisions Still Open

- Ruby VM choice and Ruby-version compatibility strategy for RGSS1/2/3
- JavaScript VM choice and browser/Node compatibility scope for MV/MZ
- exact supported WOLF versions and native-format coverage
- whether/how RM95 or Dante 98 move beyond research detection
- long-term native Windows DLL/Win32 compatibility scope
- release minimum hardware/platform requirements
