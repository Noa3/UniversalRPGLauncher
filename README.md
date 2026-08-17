# UniversalRPG

> **Self-contained cross-platform RPG Maker compatibility runtime**

## Overview

UniversalRPG is a standalone application that detects, analyzes, and runs RPG Maker games natively on modern hardware — without requiring EasyRPG, mkxp, Wine, or any external runtimes.

### Key Features

- **Universal Detection**: Automatically identifies RPG Maker 2000/2003/XP/VX/VXAce/MV/MZ games
- **Native Interpretation**: Interprets game data directly — no compatibility layers
- **Cross-Platform**: Windows x86-64, Linux x86-64, Android ARM64
- **Faithful Mode**: Preserve original game behavior exactly
- **Enhanced Mode**: Modern improvements (scaling, shaders, controller support)
- **Self-Contained**: No external dependencies to install
- **Security First**: Imported games are treated as untrusted

## Architecture

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
│   ├── rm2k/             # RPG Maker 2000/2003 backend
│   ├── rgss/             # RGSS runtime (XP/VX/VXAce)
│   ├── mv/               # RPG Maker MV backend
│   ├── mz/               # RPG Maker MZ backend
│   └── compatibility/    # Compatibility database
│
├── platform/             # Godot platform adapter
├── native/               # C++ native code
├── enhancement/          # Enhanced Mode features
├── tests/               # Test suite
├── docs/               # Documentation
└── tools/              # Build/dev tools
```

See [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md) for detailed architecture.

## Development Phases

See [docs/ROADMAP.md](docs/ROADMAP.md) for the complete phase breakdown.

### Current Phase: Phase 1 — Runtime Foundation ✅

- ✅ VirtualFileSystem with case-insensitive resolution
- ✅ VirtualClock with deterministic timing
- ✅ GameDetector with multi-signal analysis
- ✅ CompatibilityProfile system with extensible database

### Next Phase: Phase 2 — RM2000/2003 Parser

- [ ] RM2K data file parsers
- [ ] Map/event/database structures
- [ ] Save/load format support

## Getting Started

### Prerequisites

- Godot 4.7+ (Mono or standard)
- Git

### Setup

```bash
# Clone the repository
git clone https://github.com/Noa3/UniversalRPG.git
cd UniversalRPG

# Open in Godot editor
# File → Open → select project.godot
```

### Running

```bash
# Run from Godot editor (F5)
# Or export and run the built executable
```

## Contributing

Contributions are welcome! Please read [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md) before submitting changes.

1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Submit a pull request

## Legal

This project implements RPG Maker behavior independently. It does not include:

- Proprietary RPG Maker engine code
- Original runtime binaries
- RTP assets

See [THIRD_PARTY_LICENSES.md](THIRD_PARTY_LICENSES.md) for third-party component documentation.

Users must provide their own legally obtained RPG Maker games and RTP packages.

## License

TODO: Choose an appropriate open-source license.

## Credits

- Built with [Godot Engine](https://godotengine.org/)
- Inspired by the RPG Maker community
