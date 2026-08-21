# Engine Plugin Catalog and Lifecycle

UniversalRPG uses a deterministic catalog of trusted, compiled, in-process engine plugins. The catalog is detection and runtime-selection infrastructure; it is not a dynamic loader and it never executes files from an imported game directory.

## Built-in entries

| Stable ID | Display name | Capabilities | Launch status |
|---|---|---|---|
| `rpg-maker-95` | RPG Maker 95 | Detection, runtime bootstrap | Safe bounded bootstrap; no gameplay |
| `rpg-maker-2000` | RPG Maker 2000 | Detection, parsing metadata, runtime bootstrap | Minimal bootstrap: LDB/LMT/LMU load and deterministic tick |
| `rpg-maker-2003` | RPG Maker 2003 | Detection, parsing metadata, runtime bootstrap | Minimal bootstrap: LDB/LMT/LMU load and deterministic tick |
| `rpg-maker-xp` | RPG Maker XP | Detection, parsing metadata, runtime bootstrap | Safe bounded bootstrap; RGSS execution not implemented |
| `rpg-maker-vx` | RPG Maker VX | Detection, parsing metadata, runtime bootstrap | Safe bounded bootstrap; RGSS execution not implemented |
| `rpg-maker-vx-ace` | RPG Maker VX Ace | Detection, parsing metadata, runtime bootstrap | Safe bounded bootstrap; RGSS execution not implemented |
| `rpg-maker-mv` | RPG Maker MV | Detection, parsing metadata, runtime bootstrap | Safe bounded bootstrap; JavaScript execution not implemented |
| `rpg-maker-mz` | RPG Maker MZ | Detection, parsing metadata, runtime bootstrap | Safe bounded bootstrap; JavaScript execution not implemented |
| `wolf-rpg` | WOLF RPG Editor | Detection, parsing metadata, runtime bootstrap | Safe bounded bootstrap; WOLF VM not implemented |
| `rpg-maker-unite` | RPG Maker Unite / Unity candidate | Detection | Detection only; generic Unity exports are not treated as Unite games |

RM2K and RM2K3 currently advertise a parser-backed `PluginCapability.Runtime` bootstrap. `Rm2kEngineRuntime` loads the bounded LDB/LMT/first-LMU data through the canonical parser. The other target entries use `EngineBootstrapRuntime`, which re-inspects the bounded source and provides the same safe lifecycle/clock boundary without executing engine scripts or binaries. These are integration bootstraps, not full gameplay: event interpretation, RGSS/JavaScript/WOLF VMs, rendering, audio, menus, saves, and battles are not implemented yet. Unite remains detection-only because generic Unity exports cannot prove RPG Maker Unite provenance. Console-only RPG Maker products are not part of this catalog.

## Application composition

Create separate registries for detection and runtime selection from the same deterministic catalog:

```csharp
using UniversalRPG.Plugins;

var detectionRegistry = BuiltInEnginePluginCatalog.CreateDetectionRegistry();
var runtimeRegistry = BuiltInEnginePluginCatalog.CreateRuntimeRegistry();
var detector = new GameDetector(detectionRegistry);
var selector = new EngineRuntimeSelector(runtimeRegistry);
```

`EnginePluginRegistry.Register()` validates metadata and rejects duplicate IDs with `PluginErrorCode.DuplicatePluginId`. Registry enumeration and runtime selection are deterministic: probe score, declared priority, then ordinal plugin ID. `EngineRuntimeSelector` additionally validates the exact detected plugin ID, required capabilities, platform, engine range, and the plugin compatibility probe. It never falls back to a different engine or an external executable.

`GameLibrary.Import()` and `Scan()` persist the detection report under `user://library.cfg`. Each entry records the schema version, candidate IDs and scores, evidence, selected plugin ID when unambiguous, confidence, diagnostics, and compatibility status. On relaunch the source is inspected again; persisted selections are reused only when the current bounded detection still reports that candidate.

## Lifecycle

A playable plugin must provide an `IEngineRuntime`. `EnginePluginHost` owns the selected runtime and enforces:

```text
NotStarted -> Created -> Initialized -> Running -> Stopped -> Disposed
                                      \-> Faulted -> disposed
```

Initialization, start, update, stop, and disposal failures become typed `PluginError` values. Failed partial runtimes are disposed and cannot be reused accidentally.

## Adding a future plugin

1. Add a stable lowercase identifier to `EnginePluginIds` only when it is a public catalog target.
2. Implement `IEngineDetectionPlugin` and declare `EnginePluginMetadata` with the real engine range, generation, priority, and `PluginCapability.Detection`.
3. Inspect only `EngineInspectionContext.Snapshot`; keep entry count, depth, file, and archive limits in force. Never execute or load an EXE, DLL, script, shell command, or native plugin from the game.
4. Register the compiled plugin in the detection registry. Add deterministic fixtures for positive, negative, ambiguous, malformed, and unknown cases.
5. For a playable backend, implement `IEnginePlugin`, advertise `PluginCapability.Runtime`, declare platform/capability constraints, implement `IEngineRuntime`, and make the compatibility probe reject unsupported games.
6. Add import persistence and runtime-selection tests for missing registry entries, missing capabilities, platform mismatch, probe failure, and lifecycle failure.
7. Update `docs/ENGINE_DETECTION.md`, this catalog, and the user-facing status documentation; run `./scripts/validate.sh`.

Imported games remain read-only untrusted input throughout detection and selection.
