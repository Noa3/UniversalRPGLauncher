# UniversalRPG

Self-contained, cross-platform compatibility runtime for classic RPG Maker generations and WOLF RPG Editor, hosted by Godot 4.7.2 and implemented primarily in C#/.NET.

UniversalRPG is intended to interpret supported game data itself. It does **not** use original RPG Maker executables, EasyRPG, mkxp, NW.js, Wine, or other external launchers as the normal runtime path.

The core is also being designed as an embeddable .NET library so external frontends and tools can reuse engine detection, script/plugin analysis, logical game-content access, and eventually runtime sessions without depending on the Godot UI.

## Current State

> **Documentation snapshot:** 2026-09-11  
> **Latest recorded canonical validation on `main`:** 296/296 headless tests passed, plus clean .NET build and `scripts/validate.sh`.  
> **Current branch:** major runtime/SDK/content refactor; fresh validation is required before merge.

The application foundation is working: users can select a game collection, import folders or inspect ZIP archives, persist library metadata, and run bounded engine detection without executing imported binaries or scripts.

The project is **not yet a general playable RPG Maker replacement**. RM2000/2003 currently have the strongest runtime foundation. WOLF has a deliberately narrow experimental unencrypted plain-data runtime slice. Other engine families are currently detection/parsing or research boundaries.

| Engine family | Current repository status |
|---|---|
| RPG Tsukūru Dante 98 | Detection-only research marker; no parser/runtime |
| RPG Maker 95 | Detection-only research boundary |
| RPG Maker 2000 | Partial parser-backed runtime foundation |
| RPG Maker 2003 | Partial parser-backed runtime foundation sharing the LCF core |
| RPG Maker XP | Detection + bounded RGSS/script/archive parsing; no Ruby/RGSS execution yet |
| RPG Maker VX | Detection + bounded RGSS/script/archive parsing; no Ruby/RGSS execution yet |
| RPG Maker VX Ace | Detection + bounded RGSS/script/archive parsing; no Ruby/RGSS execution yet |
| RPG Maker MV | Detection + metadata/plugin inventory + encrypted-asset content access; no JavaScript runtime yet |
| RPG Maker MZ | Detection + metadata/plugin inventory + encrypted-asset content access; no JavaScript runtime yet |
| WOLF RPG Editor | Experimental unencrypted plain-data parser/runtime/VM slice; protected archives are reported but not bypassed |
| RPG Maker Unite | Research/detection only; generic Unity exports are not considered proof of Unite provenance |

RM2000/2003 currently include bounded LCF parsing, deterministic simulation infrastructure, an expanding event-command interpreter, directional chipset passability, event collision/interaction, real map transfers, renderer-neutral map/sprite/presentation state, RTP resolution primitives, and bounded save-related codecs. Full faithful rendering, audio, menus, battle parity, original save compatibility, and complete command coverage remain incomplete.

Runtime selection is fail-closed: engine recognition alone is never sufficient to start a game. A plugin must explicitly advertise `PluginCapability.Runtime`, and the runtime host/registry enforce that capability before runtime creation.

## Architecture

UniversalRPG uses trusted, compiled engine plugins behind a shared host and a Godot-free public SDK/content boundary:

```text
Game directory / ZIP / supported protected content
        |
        v
Safe inspection + logical content sources
        |
        +--> Directory / ZIP / layered overrides
        +--> trusted protected-content providers
        |
        v
Engine detection / GameAnalysis
        |
        +--> scripts/plugins inventory
        +--> protected-content status
        |
        v
Engine plugin / runtime
        |
        +--> parser / VM / simulation
        |
        v
Shared runtime services
        |
        +--> UniversalRPG.Sdk for external hosts
        |
        v
Godot application/platform layer
```

Engine detection is intentionally separate from runtime capability. A game being recognized does not mean it is playable.

See:

- [Architecture](docs/ARCHITECTURE.md)
- [Public SDK](docs/SDK.md)
- [Script compatibility](docs/SCRIPT_COMPATIBILITY.md)
- [Protected/packed content](docs/PROTECTED_CONTENT.md)
- [Engine detection](docs/ENGINE_DETECTION.md)
- [Engine plugins](docs/ENGINE_PLUGINS.md)
- [Compatibility policy](docs/COMPATIBILITY.md)
- [Engine coverage audit](docs/ENGINE_COVERAGE_AUDIT.md)

## Game Content and Packaging

Runtime-facing content is moving to a common read-only logical API rather than requiring permanent extraction.

Current content sources include:

- normal directories with Windows-style case-insensitive lookup on every platform
- bounded read-only ZIP mounts
- ordered override/translation/game/RTP layers
- transparent in-memory MV/MZ built-in encrypted image/audio assets

The content layer rejects traversal paths, ambiguous case collisions, reparse/symlink escape paths where applicable, oversized reads, and unsafe ZIP entries.

XP/VX/VX Ace encrypted archive families are detected and represented through the protected-content API, but no built-in archive provider is enabled yet. WOLF protected archives are detected and fail closed rather than being silently bypassed.

See [docs/PROTECTED_CONTENT.md](docs/PROTECTED_CONTENT.md).

## Game Library

The default library is `user://games`. The exact native path depends on the operating system and is displayed by the application.

Place games in their own directories, for example:

```text
games/
├── Game A/
│   ├── Game.ini
│   └── Data/
└── Game B/
    ├── index.html
    ├── data/
    └── js/
```

Current bounded inspection defaults are:

- maximum directory depth: 4
- maximum inspected entries: 4096
- maximum metadata read per file: 1 MiB
- reparse points/symlinks/junctions are skipped
- ZIP inspection is read-only and bounded
- reaching the entry budget produces a **partial/advisory** scan rather than automatically classifying a well-formed game as malformed

Imported EXE, DLL, SO, Ruby, JavaScript, shell, and native plugin files are never executed during detection.

## Custom Scripts and Plugins

Supporting game-authored scripts is a core compatibility requirement, not an optional add-on.

Current foundations include:

- XP/VX/VX Ace `Scripts.rxdata/.rvdata/.rvdata2` Ruby-Marshal/zlib inventory with original load order
- MV/MZ `plugins.js` + `js/plugins/*.js` inventory with enabled state, load order, hashes, and static compatibility classification
- shared `IEmbeddedScriptVm` / VM-factory contracts
- generation-specific RGSS VM profiles
- MV/MZ VM profiles and explicit security policy
- engine-level ordered script loaders that do not execute code until explicit bootstrap

Actual Ruby and JavaScript execution is still pending an embedded VM implementation and faithful RGSS/browser/RPG Maker API layers.

See [docs/SCRIPT_COMPATIBILITY.md](docs/SCRIPT_COMPATIBILITY.md) and [docs/VM_EVALUATION.md](docs/VM_EVALUATION.md).

## Development Setup

### Requirements

- Godot **4.7.2 stable .NET/Mono**
- compatible .NET 8 SDK
- platform SDKs/export templates only when building platform packages

The Godot project lives under `project/`.

### Run

Open:

```text
project/project.godot
```

or run a Godot .NET binary with:

```bash
godot --path project
```

A repository-local editor may also be placed under `tools/godot/editors/4.7.2/`; editor binaries are intentionally not committed as project source.

### Validation

From the repository root:

```bash
./scripts/validate.sh
```

Set `GODOT_BIN=/absolute/path/to/Godot` when Godot is not available on `PATH`.

The validation path independently builds `UniversalRPG.Sdk`, restores/builds the Godot .NET project, performs a Godot headless import, and runs the C# test suite. Historical test counts are evidence for the commit that produced them; they are not a substitute for a fresh validation after changes.

## Targets

Export presets are stored in `project/export_presets.cfg`.

Intended targets:

- Windows x86-64
- Linux x86-64
- Android ARM64
- macOS
- iOS

Presets existing in the repository does not mean release packages have been validated on every platform. Android/iOS/macOS still require their platform toolchains and signing where applicable.

## Localization

The launcher currently contains catalogs for:

- English
- German
- Spanish
- French
- Japanese
- Korean
- Simplified Chinese

See [docs/LOCALIZATION.md](docs/LOCALIZATION.md).

## Security

Games are untrusted input.

Detection and parsing remain bounded and non-executing. Future Ruby, JavaScript, WOLF, or native-plugin execution must use explicit capability/security boundaries rather than granting imported game code unrestricted host access.

Protected-content providers are trusted application/host components. Imported games are never allowed to register arbitrary executable decryptor/provider code themselves.

See [docs/IMPORT_SECURITY.md](docs/IMPORT_SECURITY.md) and [docs/PROTECTED_CONTENT.md](docs/PROTECTED_CONTENT.md).

## Project Guidance

- [SESSION_STATE.md](SESSION_STATE.md) — concise interruption/restart checkpoint and current next action
- [AGENTS.md](AGENTS.md) — coding-agent policy and autonomous work rules
- [HERMES_AUTONOMOUS_PROMPT.md](HERMES_AUTONOMOUS_PROMPT.md) — Hermes autonomous workflow
- [docs/PROJECT_STATUS.md](docs/PROJECT_STATUS.md) — current implementation status and immediate priorities
- [docs/ROADMAP.md](docs/ROADMAP.md) — long-term multi-engine direction

There is intentionally no project Kanban. Agents should inspect the current source/tests, use `SESSION_STATE.md` only as a compact checkpoint, and choose the next coherent implementation slice from the immediate priorities and roadmap.

Historical `SESSION_HANDOFF_*.md` files are snapshots of past work and should not be treated as the current source of truth.

## Legal

UniversalRPG does not include proprietary RPG Maker/WOLF runtime binaries, games, or RTP assets. Users must provide legally obtained game data and required RTP packages.

Third-party components are tracked in [THIRD_PARTY_LICENSES.md](THIRD_PARTY_LICENSES.md).

The license for UniversalRPG itself is still to be selected.
