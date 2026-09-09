# UniversalRPG — Compatibility Policy

> **Last reviewed:** 2026-09-09

## Principle

Compatibility claims must describe what the repository can actually execute, not merely what it can recognize.

Priority order:

1. correct engine/game behavior
2. compatibility
3. security
4. stability
5. maintainability
6. performance
7. optional enhancements

## Capability Levels

Use explicit language:

- **Detection-only** — identifies the engine; cannot run it.
- **Parsing-only** — safely reads a bounded subset of data; cannot execute the engine.
- **Experimental runtime** — executes a deliberately narrow subset.
- **Partial runtime** — meaningful runtime exists, but representative compatibility is incomplete.
- **Playable** — a representative authorized end-to-end game path works.
- **Broad compatibility** — requires substantial real-world conformance evidence.

Current state:

| Engine | Compatibility level |
|---|---|
| Dante 98 | Detection-only research |
| RPG Maker 95 | Detection-only research |
| RPG Maker 2000 | Partial runtime |
| RPG Maker 2003 | Partial runtime |
| RPG Maker XP | Parsing-only |
| RPG Maker VX | Parsing-only |
| RPG Maker VX Ace | Parsing-only |
| RPG Maker MV | Parsing-only |
| RPG Maker MZ | Parsing-only |
| WOLF RPG Editor | Experimental unencrypted/plain-data runtime |
| RPG Maker Unite | Detection-only research |

## Faithful and Enhanced Behavior

### Faithful Mode

Faithful behavior is the compatibility baseline.

It should preserve, where known:

- logical timing
- map/event semantics
- coordinate system
- rendering order
- input semantics
- save/game state behavior
- engine quirks required by real games

A feature is not “faithful” merely because it looks similar.

### Enhanced Mode

Enhanced behavior is optional and capability-gated.

Examples:

- integer/pixel-perfect scaling
- high-resolution presentation
- higher display refresh without changing simulation speed
- shaders
- controller/touch improvements
- fast-forward/slow-motion
- asset/translation overrides
- accessibility features
- save states/rewind when technically safe

Each enhancement must be individually disableable and must not silently change game logic.

## Compatibility Profiles

Game-specific behavior belongs in centralized validated profiles rather than scattered hard-coded checks.

Profiles should be keyed by reliable identity such as:

- game hash/signature
- engine/generation
- plugin/native dependency hash
- verified version metadata

Unknown hashes must **not** inherit another game's profile merely because the engine matches.

A profile may contain:

- compatibility flags
- encoding overrides
- disabled enhancements
- known unsupported dependencies
- plugin HLE selection
- notes and regression-test references

Profile schemas must remain bounded and versioned.

## RM2000 / RM2003

Current compatibility strengths:

- bounded LCF parsing
- deterministic simulation infrastructure
- growing verified event-command coverage
- presentation/framebuffer/sprite state
- RTP/save/debug foundations

Current major risks:

- chipset/passability semantics
- incomplete command coverage
- RM2003-specific differences
- incomplete visible renderer/audio/menu/battle/save parity
- insufficient end-to-end real-game evidence

Compatibility fixes should be backed by verified format/runtime semantics and a regression fixture.

## XP / VX / VX Ace

Current plugins do not advertise Runtime.

Future compatibility concerns include:

- Ruby version differences
- RGSS1/2/3 API differences
- serialized data/archive formats
- Win32API calls
- native DLL dependencies
- default and third-party script assumptions

A Ruby VM choice alone is not enough; the RGSS API surface must be reproduced.

## MV / MZ

Current plugins do not advertise Runtime.

Future compatibility concerns include:

- JavaScript language/runtime behavior
- browser API expectations
- Canvas/WebGL/WebAudio
- RPG Maker engine scripts
- third-party plugins
- Node/NW.js assumptions
- native Node modules
- encrypted-asset policy

Imported JavaScript must only execute inside the future explicit sandbox/runtime boundary.

## WOLF RPG Editor

Current WOLF compatibility is an experimental unencrypted/plain-data slice.

Do not equate the synthetic/plain-data VM with complete WOLF format compatibility.

Future compatibility requires:

- supported version definition
- authorized native-format fixtures
- native database/map/event parsing
- broader VM semantics
- renderer/input/audio/UI/save/game-system layers

Do not bypass protected/encrypted data merely to increase a compatibility percentage.

## Native Plugins / DLLs

Prefer this order:

1. inspect as data
2. identify by hash/signature
3. implement high-level compatible replacement when possible
4. implement narrow portable API shims
5. consider controlled native execution only after a security boundary exists

A native dependency that patches original executable memory may be fundamentally incompatible with a replacement runtime and should be reported honestly.

## Diagnostics

Compatibility failures should be actionable.

Prefer:

```text
Engine: RPG Maker VX Ace
Status: Unsupported runtime
Reason: RGSS script execution is not implemented
Detected runtime: RGSS3
Native dependencies: 2
Next supported boundary: metadata inspection only
```

over generic errors such as:

```text
Failed to load game
```

## Compatibility Reports

Reports should contain only bounded diagnostic metadata needed for troubleshooting:

- URPG/runtime version
- platform
- engine/plugin ID
- game hash/signature where available
- detected dependencies
- compatibility flags
- unsupported APIs/commands
- missing assets
- bounded errors/warnings

Do not include unrelated private filesystem/user data.

## Regression Policy

For a compatibility bug:

1. reproduce it
2. identify the real semantic/root cause
3. add a legal/synthetic/minimized regression fixture where practical
4. implement the narrow correct fix
5. run focused validation
6. run the canonical suite
7. update compatibility/profile documentation only after validation

Do not weaken assertions or silently skip unsupported behavior to make a test green.

## Legal Boundary

- no proprietary engine binaries in URPG
- no bundled RTP unless redistribution rights explicitly permit it
- no copied proprietary game assets
- third-party code/assets must have documented licenses
- users supply legally obtained games and RTP resources
