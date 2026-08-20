# UniversalRPG

Self-contained, cross-platform RPG Maker compatibility runtime built with Godot.

## Current State

The Godot application now starts, lets the user choose a games directory, scans its subdirectories, and identifies RPG Maker 2000/2003, XP, VX, VX Ace, MV, and MZ projects without executing imported files.

Runtime backends are not playable yet. Detection, library UI, parser foundations, localization, and security boundaries exist; interpreters, rendering, audio, and complete data parsers remain under development.

| Capability | Status |
|---|---|
| Cross-platform game library and folder selection | Working |
| RPG Maker generation detection | Working, heuristic |
| English, German, Spanish, French, Japanese, Korean, Simplified Chinese UI | Working |
| UTF-8, BOM, CP932/Shift-JIS metadata decoding | Initial implementation |
| RM2000/2003 parser | Prototype, not format-complete |
| RM2000/2003 gameplay | Not implemented |
| XP/VX/VX Ace gameplay | Not implemented |
| MV/MZ gameplay | Not implemented |
| Windows/Linux/macOS/Android/iOS exports | Presets present, not release-tested |

## Game Library

The default library is `user://games`. Its native location depends on the operating system. The exact path is displayed in the app and can be changed with the folder picker.

Place each game in its own directory:

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

The scanner checks the selected directory and up to two subdirectory levels. Symlinks, junctions, hidden directories, and oversized metadata are not followed or loaded.

## Development Setup

### Included Editors

Godot 4.7.2 stable .NET editors are locally installed at:

- Linux x86-64: `tools/godot/editors/4.7.2/linux-x86_64/`
- Windows x86-64: `tools/godot/editors/4.7.2/windows-x86_64/`

Downloaded binaries are intentionally ignored by Git. Checksums and source URLs are documented in [`tools/godot/README.md`](tools/godot/README.md).

The .NET editor needs a separate 64-bit .NET SDK. Godot 4.7 requires .NET 8 or newer; Android C# exports require .NET 9 or newer. UniversalRPG currently uses GDScript, so the standard Godot editor can also build it.

### Run

Open `project.godot`, then press F6/F5, or on Linux run:

```bash
tools/godot/editors/4.7.2/linux-x86_64/Godot_v4.7.2-stable_mono_linux_x86_64/Godot_v4.7.2-stable_mono_linux.x86_64 --path .
```

### Headless Validation

```bash
godot --headless --editor --quit --path .
godot --headless --path . --quit-after 5
```

## Targets

- Windows x86-64
- Linux x86-64
- macOS universal
- Android ARM64
- iOS ARM64

Export presets live in `export_presets.cfg`. Apple exports still require macOS/Xcode and signing. Android/iOS need their platform SDKs and export templates.

## Localization

English is the default and fallback language. Current catalogs:

- English (`en`)
- German (`de`)
- Spanish (`es`)
- French (`fr`)
- Japanese (`ja`)
- Korean (`ko`)
- Simplified Chinese (`zh_CN`)

See [`docs/LOCALIZATION.md`](docs/LOCALIZATION.md) to add languages. Noto Sans CJK is bundled so desktop and mobile exports do not depend on system CJK fonts.

## Security

Games are untrusted input. Detection never starts `EXE`, `DLL`, `SO`, Ruby, or JavaScript files. Future runtimes must use capability-based APIs and the virtual filesystem rather than host filesystem/process APIs.

See [`docs/IMPORT_SECURITY.md`](docs/IMPORT_SECURITY.md) for required path, archive, parser, script, network, and save-data controls.

## Direction

- [`idea.md`](idea.md): product and engineering brief for Hermes
- [`docs/ROADMAP.md`](docs/ROADMAP.md): implementation phases
- [`docs/ARCHITECTURE.md`](docs/ARCHITECTURE.md): runtime architecture
- [`docs/COMPATIBILITY.md`](docs/COMPATIBILITY.md): compatibility policy

## Legal

UniversalRPG contains no proprietary RPG Maker engine code, runtime binaries, games, or RTP assets. Users must provide legally obtained games and RTP packages. Third-party components are listed in [`THIRD_PARTY_LICENSES.md`](THIRD_PARTY_LICENSES.md).

License for UniversalRPG itself is still to be selected.
