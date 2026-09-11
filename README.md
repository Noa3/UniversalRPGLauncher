# UniversalRPG

Self-contained, cross-platform compatibility runtime for classic RPG Maker generations and WOLF RPG Editor, hosted by Godot 4.7.2 and implemented primarily in C#/.NET.

UniversalRPG is intended to interpret supported game data itself. It does **not** use original RPG Maker executables, EasyRPG, mkxp, NW.js, Wine, or other external launchers as the normal runtime path.

## Current State

> **Documentation snapshot:** 2026-09-11  
> **Latest recorded canonical validation on `main`:** 296/296 headless tests passed, plus clean .NET build and `scripts/validate.sh`.  
> **Current branch:** documentation refresh plus runtime-capability safety hardening; fresh validation is required before merge.

The application foundation is working: users can select a game collection, import folders or inspect ZIP archives, persist library metadata, and run bounded engine detection without executing imported binaries or scripts.

The project is **not yet a general playable RPG Maker replacement**. RM2000/2003 currently have the strongest runtime foundation. WOLF has a deliberately narrow experimental unencrypted plain-data runtime slice. Other engine families are currently detection/parsing or research boundaries.

| Engine family | Current repository status |
|---|---|
| RPG Tsukūru Dante 98 | Detection-only research marker; no parser/runtime |
| RPG Maker 95 | Detection-only research boundary |
| RPG Maker 2000 | Partial parser-backed runtime foundation |
| RPG Maker 2003 | Partial parser-backed runtime foundation sharing the LCF core |
| RPG Maker XP | Detection + bounded metadata/parsing; no Ruby/RGSS execution |
| RPG Maker VX | Detection + bounded metadata/parsing; no Ruby/RGSS execution |
| RPG Maker VX Ace | Detection + bounded metadata/parsing; no Ruby/RGSS execution |
| RPG Maker MV | Detection + bounded metadata inspection; no JavaScript runtime |
| RPG Maker MZ | Detection + bounded metadata/database inventory; no JavaScript runtime |
| WOLF RPG Editor | Experimental unencrypted plain-data parser/runtime/VM slice; not native-format complete |
| RPG Maker Unite | Research/detection only; generic Unity exports are not considered proof of Unite provenance |

RM2000/2003 currently include bounded LCF parsing, deterministic simulation infrastructure, an expanding event-command interpreter, renderer-neutral map/sprite/presentation state, RTP resolution primitives, and bounded save-related codecs. Full map rendering, chipset/passability fidelity, audio, menus, battle parity, original save compatibility, and complete command coverage remain incomplete.

Runtime selection is fail-closed: engine recognition alone is never sufficient to start a game. A plugin must explicitly advertise `PluginCapability.Runtime`, and the runtime host/registry enforce that capability before runtime creation.

## Architecture

UniversalRPG uses trusted, compiled engine plugins behind a shared host:

```text
Game / archive
    |
    v
SafeGameInspector
    |
    v
EngineDetectionRegistry
    |
    v
Engine plugin
    |
    +--> engine-specific parser / VM / simulation
    |
    v
Shared runtime services
    |
    v
Godot platform/application layer
```

Engine detection is intentionally separate from runtime capability. A game being recognized does not mean it is playable.

See:

- [Architecture](docs/ARCHITECTURE.md)
- [Engine detection](docs/ENGINE_DETECTION.md)
- [Engine plugins](docs/ENGINE_PLUGINS.md)
- [Compatibility policy](docs/COMPATIBILITY.md)
- [Engine coverage audit](docs/ENGINE_COVERAGE_AUDIT.md)

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

## Development Setup

### Requirements

- Godot **4.7.2 stable .NET/Mono**
- compatible .NET SDK
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

The validation path performs restore/build, Godot headless import, and the C# test runner. Historical test counts are evidence for the commit that produced them; they are not a substitute for a fresh validation after changes.

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

Detection and parsing must remain bounded and non-executing. Future Ruby, JavaScript, WOLF, or native-plugin runtime work must use explicit capability/security boundaries rather than granting imported game code unrestricted host access.

See [docs/IMPORT_SECURITY.md](docs/IMPORT_SECURITY.md).

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
