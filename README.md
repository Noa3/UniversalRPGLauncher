# UniversalRPG

Self-contained, cross-platform RPG Maker compatibility runtime built with Godot.

## Current State

The Godot application now starts, lets the user choose a games directory, scans its subdirectories, and identifies RPG Maker 2000/2003, XP, VX, VX Ace, MV, and MZ projects without executing imported files.

Runtime backends are not playable yet. Detection, library UI, localization, security boundaries, a bounded LCF container reader, and initial RM2000/2003 LDB/LMU/LSD parsing exist. Event interpretation, rendering, audio, and complete database/map decoding remain under development.

| Capability | Status |
|---|---|
| Cross-platform game library and folder selection | Working |
| RPG Maker generation detection | Working, heuristic |
| English, German, Spanish, French, Japanese, Korean, Simplified Chinese UI | Working |
| UTF-8, BOM, CP932/Shift-JIS metadata decoding | Initial implementation |
| RM2000/2003 parser | Real LCF container layer + initial LDB/LMU/LSD decoding; not format-complete |
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

### Editor/tooling

The repository is pinned to **Godot 4.7.2 stable**. Editor binaries are intentionally not included in the repository/ZIP. Optional local editor locations are documented in [`tools/godot/README.md`](tools/godot/README.md).

The validated/canonical implementation is pure C#/.NET under the Godot 4.7.2 .NET editor. The migration is covered by `dotnet build` and the headless C# suite (`128/128` tests passed).

### Run

Open `project.godot`, then press F6/F5, or on Linux run:

```bash
tools/godot/editors/4.7.2/linux-x86_64/Godot_v4.7.2-stable_mono_linux_x86_64/Godot_v4.7.2-stable_mono_linux.x86_64 --path .
```

### Headless Validation

Preferred command:

```bash
./scripts/validate.sh
```

Set `GODOT_BIN=/absolute/path/to/Godot` if Godot is not on `PATH`. The script performs .NET restore/build, editor import validation, and the C# core/smoke suite.

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

- [`KANBAN.md`](KANBAN.md): active autonomous work queue
- [`SESSION_STATE.md`](SESSION_STATE.md): interruption/restart checkpoint
- [`HERMES_AUTONOMOUS_PROMPT.md`](HERMES_AUTONOMOUS_PROMPT.md): ready-to-paste Hermes instructions
- [`AGENTS.md`](AGENTS.md): repository rules for coding agents
- [`idea.md`](idea.md): product and engineering brief
- [`docs/ROADMAP.md`](docs/ROADMAP.md): long-term implementation phases
- [`docs/ARCHITECTURE.md`](docs/ARCHITECTURE.md): runtime architecture
- [`docs/COMPATIBILITY.md`](docs/COMPATIBILITY.md): compatibility policy

## Legal

UniversalRPG contains no proprietary RPG Maker engine code, runtime binaries, games, or RTP assets. Users must provide legally obtained games and RTP packages. Third-party components are listed in [`THIRD_PARTY_LICENSES.md`](THIRD_PARTY_LICENSES.md).

License for UniversalRPG itself is still to be selected.
