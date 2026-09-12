# Engine Detection and Runtime Selection

> **Status:** Implemented bounded detection pipeline  
> **Last reviewed:** 2026-09-09

Detection identifies a probable engine from untrusted game data. It does **not** imply that a playable runtime exists.

## Flow

```text
folder or ZIP archive
        |
        v
SafeGameInspector
 bounded + read-only
        |
        v
EngineDetectionRegistry
        |
        v
ranked EngineDetectionReport
 score / confidence / evidence / diagnostics
        |
        v
EngineRuntimeSelector
 capability / platform / compatibility checks
        |
        v
IEnginePlugin -> IEngineRuntime
```

`GameDetector` remains the launcher/library-facing facade over the plugin report.

## Default Inspection Limits

`GameInspectionLimits` currently defaults to:

- maximum depth: **4**
- maximum entries: **4096**
- maximum metadata bytes per file: **1 MiB**
- maximum archive uncompressed inspection budget: **64 MiB**
- maximum archive entry metadata bytes: **1 MiB**
- maximum bounded prefix read: **4096 bytes**

Reparse points are skipped. Unsafe archive paths are rejected.

When a well-formed tree reaches the entry budget, the inspection is marked **partial** and receives an advisory diagnostic. Partial is distinct from malformed.

## Built-in Detection Status

| Plugin ID | Engine | Current runtime status |
|---|---|---|
| `rpg-tsukuru-dante-98` | RPG Tsukūru Dante 98 | detection-only explicit research boundary |
| `rpg-maker-95` | RPG Maker 95 | detection-only research boundary |
| `rpg-maker-2000` | RPG Maker 2000 | partial parser-backed runtime |
| `rpg-maker-2003` | RPG Maker 2003 | partial shared parser-backed runtime |
| `rpg-maker-xp` | RPG Maker XP / RGSS1 | detection + parsing only; runtime selector must refuse |
| `rpg-maker-vx` | RPG Maker VX / RGSS2 | detection + parsing only; runtime selector must refuse |
| `rpg-maker-vx-ace` | RPG Maker VX Ace / RGSS3 | detection + parsing only; runtime selector must refuse |
| `rpg-maker-mv` | RPG Maker MV | detection + metadata parsing only |
| `rpg-maker-mz` | RPG Maker MZ | detection + bounded metadata/database inventory only |
| `wolf-rpg` | WOLF RPG Editor | experimental unencrypted plain-data runtime slice |
| `rpg-maker-unite` | RPG Maker Unite / Unity candidate | detection/research only |

## Important Engine Boundaries

### Dante 98

The current detector only accepts an explicit research marker. It does not classify arbitrary PC-98 media.

### RPG Maker 95

Detection requires a root `*.RPG` descriptor plus documented companion data such as `*.ATR`, `EVT*.DAT`, `STRINGS.DAT`, or `SWNAME.DAT`. An executable name alone is not enough.

### RM2000 / RM2003

LCF detection requires the RPG_RT database/map-tree pair and uses bounded metadata/evidence to distinguish generations when possible. These engines have the current primary runtime.

### XP / VX / VX Ace

RGSS signatures can be detected and bounded metadata inspected, but the plugins intentionally advertise no Runtime capability until an embedded Ruby/RGSS implementation exists.

### MV / MZ

Detection requires the expected web-game layout/runtime signatures and bounded valid metadata. Imported HTML/JavaScript is not executed.

MZ detection includes stricter runtime signature checks and bounded `System.json`/database inventory support.

### WOLF

Detection identifies WOLF data signatures. Runtime support is intentionally limited to explicit unencrypted/plain-data structures currently understood by URPG. Protected data is not decrypted/bypassed.

### Unite

Generic Unity export markers do not prove that a game came from RPG Maker Unite. The result therefore remains research-only.

## Ranking and Ambiguity

Candidates are ordered deterministically by:

1. probe score
2. plugin priority
3. ordinal plugin ID

Equal highest-confidence candidates from different engine IDs remain ambiguous; URPG must not silently choose one.

## Runtime Selection

Runtime selection validates:

- exact detected plugin ID
- declared Runtime capability
- engine range/generation
- supported platform
- compatibility probe

There is no external-executable fallback.

A recognized XP/VX/VX Ace/MV/MZ/RM95/Dante/Unite game should remain visible in the library while launch is refused with an actionable unsupported-runtime diagnostic.

## Persistence

Library records persist:

- source
- ranked candidates
- confidence/scores
- evidence
- diagnostics
- selected plugin ID when unambiguous
- compatibility state

Persisted selection is revalidated against a fresh bounded inspection rather than blindly trusted.

## Security

During detection:

- no EXE/DLL/SO execution
- no Ruby/JavaScript evaluation
- no shell/process launch
- no arbitrary native plugin load
- no archive extraction into game/system directories
- no following reparse points

See [IMPORT_SECURITY.md](IMPORT_SECURITY.md) for runtime prerequisites.
