# UniversalRPG - Architecture

> **Status:** Phase 2 — RM2000/2003 Parser
> **Last Updated:** 2026-08-20

## Design Philosophy

UniversalRPG is a self-contained cross-platform RPG Maker compatibility runtime.
The goal is to interpret RPG Maker games natively, preserve original behavior,
and optionally enhance presentation on modern hardware.

### Core Principles

1. **Correct game behavior** — Original behavior takes priority
2. **Compatibility** — Support as many games as possible
3. **Stability** — Never crash on malformed input
4. **Security** — Treat imported games as untrusted
5. **Performance** — Run efficiently on mobile hardware
6. **Enhancements** — Graphics/UI improvements are secondary

## Architecture Overview

The repository currently uses this concrete layout. Planned interfaces should be
added only when implementation reaches them; this document must not pretend empty
future directories already exist.

```text
UniversalRPG/
├── app/
│   ├── launcher/          # Runtime availability/launch workflow
│   ├── library/           # Game library scan/settings
│   └── ui/                # Godot application UI
├── src/
│   ├── core/              # VFS, clock, legacy text decoding
│   ├── compatibility/     # Compatibility profiles/database
│   ├── game_detector/     # Compatibility facade over plugin detection
│   ├── plugins/            # Trusted engine contracts, inspection, registry
│   ├── rm2k/
│   │   ├── parser/        # LCF reader + LDB/LMU/LSD parser
│   │   ├── database/      # Serializable RM2K/2003 models
│   │   ├── interpreter/   # Future event interpreter
│   │   └── rendering/     # Future faithful renderer
│   ├── rgss/              # Future XP/VX/VX Ace runtime
│   ├── mv/                # Future MV runtime
│   └── mz/                # Future MZ runtime
├── platform/godot/        # Future explicit Godot adapter boundary
├── enhancement/           # Future optional Enhanced Mode features
├── plugins/               # Optional integration/plugin surfaces
├── tests/                 # Core, fixtures, integration, rendering
├── scripts/               # Validation/development automation
├── docs/                  # Architecture, roadmap, compatibility/security docs
└── tools/                 # Local development-tool metadata (binaries ignored)
```

## Runtime Abstractions

The RPG Maker runtime core must be independent of Godot. All platform-specific
code flows through clear interfaces:

```
┌─────────────────────────────────────────────────┐
│              RPG Maker Runtime Core              │
│  (platform-independent, no Godot dependencies)   │
├─────────────────────────────────────────────────┤
│  IRenderer  │  IAudioBackend  │  IInputBackend  │
│  IFileSystem│  IClock         │  INetworkBackend│
└──────────┬──────────────────────────────────────┘
           │ implements
           ▼
┌─────────────────────────────────────────────────┐
│           Godot Platform Adapter                 │
│  (Godot-specific implementations of interfaces)  │
└─────────────────────────────────────────────────┘
```

## Game Detection Flow

```
User selects a game directory or ZIP archive
        │
        ▼
  SafeGameInspector
        │ bounded, read-only snapshot
        ▼
  EngineDetectionRegistry
        │ ranked plugin candidates
        ▼
  GameDetector compatibility facade
        │ DetectionResult + full report
        ▼
  EngineRuntimeSelector
        │ exact plugin/capability/platform checks
        ▼
  EnginePluginRegistry -> IEnginePlugin -> IEngineRuntime
```

Detection and runtime selection are implemented as trusted, compiled in-process
plugins. Imported EXE, DLL, Ruby, JavaScript, and native plugin files are data
only; they are never executed during inspection. See
[ENGINE_DETECTION.md](ENGINE_DETECTION.md) and
[ENGINE_PLUGINS.md](ENGINE_PLUGINS.md).

## Compatibility Database

The compatibility database is extensible and data-driven:

```json
{
  "id": "profile.identifier",
  "sha256": "game_or_plugin_hash",
  "engine": "RPGMaker2003",
  "type": "game_profile",
  "compatibility": "full",
  "flags": ["PreserveLegacyPictureTiming", "LegacyTextEncoding"],
  "notes": "Known quirks and workarounds"
}
```

Game-specific behavior uses centralized flags, not scattered conditionals.

## Development Phases

See [ROADMAP.md](ROADMAP.md) for the complete phase breakdown.

### Current Phase: Phase 2 — RM2000/2003 Parser

- real bounded LCF container/BER parsing exists;
- initial LDB/LMU/LSD decoding exists;
- synthetic and provenance-pinned real parser regression fixtures exist;
- registry-driven engine plugin detection and safe runtime-selection boundaries exist;
- built-in engine entries cover RM95, RM2K, RM2K3, XP, VX, VX Ace, MV, MZ, WOLF, and Unite research detection;
- RM2K/RM2K3 have a parser-backed bootstrap runtime that loads validated data and advances the shared deterministic clock;
- LMT is fully parsed; LDB actors/switches/variables plus scalar skills/items/states/classes/enemies/terrains/attributes metadata decode into typed models with verified liblcf field IDs and per-entry unknown-field retention;
- remaining LDB array sections and LMU event/page metadata typing are next.

### Language boundary

The tested implementation is pure C#/.NET under the Godot .NET editor. Migration and plugin-wiring validation passed `dotnet build` and the headless C# regression suite at `167/167`. Performance-critical components may later move behind GDExtension/native interfaces without forcing the whole application into one language.

## Security Model

Imported games are treated as untrusted:

- Virtual filesystem sandbox
- No arbitrary process execution
- No system directory access
- Network access configurable per-game
- Clipboard access configurable per-game
- Plugin loading requires explicit compatibility policy

## Legal Considerations

- No proprietary RPG Maker code included
- No RTP assets bundled without redistribution rights
- Independent implementation of behavior
- All third-party components documented in THIRD_PARTY_LICENSES.md

## Error Philosophy

Errors must be actionable:

**Bad:** `Failed to load game`

**Good:**
```
Unable to initialize RPG Maker VX Ace runtime.

Reason:
RGSS script requested unsupported Win32 API function.

Library:
user32.dll

Function:
GetKeyboardLayout

Script:
InputExtension

Compatibility report saved.
```
