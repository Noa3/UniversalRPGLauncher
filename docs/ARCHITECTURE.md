# UniversalRPG - Architecture

> **Status:** Phase 1 — Runtime Foundation
> **Last Updated:** 2026-08-17

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

```
UniversalRPG/
├── app/                    # Application layer
│   ├── library/           # Game library management
│   ├── launcher/          # Game launch workflow
│   ├── settings/          # Configuration
│   └── ui/               # Godot UI scenes/scripts
│
├── runtime/               # Core RPG Maker runtime
│   ├── core/             # Platform-independent abstractions
│   │   ├── interfaces/   # IRenderer, IAudioBackend, etc.
│   │   ├── scheduler/    # Virtual clock, simulation loop
│   │   ├── filesystem/   # Virtual filesystem
│   │   ├── input/        # Input abstraction
│   │   ├── audio/        # Audio abstraction
│   │   ├── rendering/    # Rendering abstraction
│   │   ├── save/         # Save system
│   │   ├── serialization/
│   │   └── diagnostics/  # Logging, error reporting
│   │
│   ├── rm2k/             # RPG Maker 2000/2003 backend
│   │   ├── parser/       # Data file parsers
│   │   ├── interpreter/  # Event interpreter
│   │   ├── rendering/    # Map/sprite rendering
│   │   └── database/     # RM2K database structures
│   │
│   ├── rgss/             # RGSS runtime (XP/VX/VXAce)
│   │   ├── vm/           # Ruby VM adapter
│   │   ├── api/          # RGSS API implementations
│   │   └── compatibility/
│   │
│   ├── mv/               # RPG Maker MV backend
│   ├── mz/               # RPG Maker MZ backend
│   └── compatibility/    # Compatibility database
│
├── platform/             # Godot platform adapter
│   └── godot/           # Godot-specific implementations
│
├── native/               # C++ native code
│   ├── binary_inspector/ # PE/DLL inspection
│   ├── pe/              # PE format parser
│   └── win32/           # Win32 API compatibility
│
├── enhancement/          # Enhanced Mode features
├── tests/               # Test suite
│   ├── unit/
│   ├── fixtures/
│   ├── integration/
│   └── rendering/
│
├── docs/               # Documentation
└── tools/              # Build/dev tools
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
User selects game directory
        │
        ▼
  GameDetector.analyze()
        │
        ├── Multiple signal analysis
        │   ├── Game.ini parsing
        │   ├── Archive detection (.rvdata2, .dat, etc.)
        │   ├── Directory structure analysis
        │   ├── File signature inspection
        │   └── Metadata extraction
        │
        ├── Confidence scoring
        │
        └── Result:
            ├── Engine type
            ├── Confidence (High/Medium/Low)
            ├── Evidence list
            ├── RTP dependencies
            ├── Custom scripts/plugins
            ├── Native libraries
            └── Compatibility flags
```

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

### Current Phase: Phase 0 — Repository Audit

- Repository structure audit
- Architecture documentation
- Technical debt assessment

### Next Phase: Phase 1 — Runtime Foundation

- Platform abstractions
- Virtual filesystem
- Virtual clock
- Renderer/audio/input interfaces
- Game detector
- Compatibility profiles

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
