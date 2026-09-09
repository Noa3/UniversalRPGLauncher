# Engine Plugin Catalog and Lifecycle

> **Last reviewed:** 2026-09-09

UniversalRPG uses a deterministic catalog of trusted, compiled, in-process engine plugins. This is an internal architecture boundary, not a user-provided dynamic plugin loader.

Imported game executables, DLLs and scripts are never loaded merely to detect an engine.

## Built-in Catalog

The following table reflects the capabilities currently declared by source code in `project/src/plugins/BuiltInEnginePlugins.cs`.

| Stable ID | Engine | Declared capability boundary | Current meaning |
|---|---|---|---|
| `rpg-tsukuru-dante-98` | RPG Tsukūru Dante 98 | Detection | explicit research marker only |
| `rpg-maker-95` | RPG Maker 95 | Detection | conservative research detector |
| `rpg-maker-2000` | RPG Maker 2000 | Detection, Parsing, Runtime, SaveLoad, Debugging | partial parser-backed runtime foundation; not full compatibility |
| `rpg-maker-2003` | RPG Maker 2003 | Detection, Parsing, Runtime, SaveLoad, Debugging | partial shared LCF runtime foundation; RM2K3-specific parity incomplete |
| `rpg-maker-xp` | RPG Maker XP | Detection, Parsing | no Ruby/RGSS execution |
| `rpg-maker-vx` | RPG Maker VX | Detection, Parsing | no Ruby/RGSS execution |
| `rpg-maker-vx-ace` | RPG Maker VX Ace | Detection, Parsing | no Ruby/RGSS execution |
| `rpg-maker-mv` | RPG Maker MV | Detection, Parsing | bounded metadata only; no JavaScript runtime |
| `rpg-maker-mz` | RPG Maker MZ | Detection, Parsing | bounded metadata/database inventory only; no JavaScript runtime |
| `wolf-rpg` | WOLF RPG Editor | Detection, Parsing, Runtime | experimental unencrypted plain-data runtime/VM slice |
| `rpg-maker-unite` | RPG Maker Unite / Unity candidate | Detection | research-only candidate detection |

A capability flag describes an implemented URPG boundary, **not** feature completeness. For example, RM2K/3 `SaveLoad` currently includes runtime-owned save tooling and read-only original LSD framing work; it does not imply complete original save compatibility.

## Detection vs Runtime

Detection and runtime selection use separate registries built from the same compiled catalog:

```csharp
var detectionRegistry = BuiltInEnginePluginCatalog.CreateDetectionRegistry();
var runtimeRegistry = BuiltInEnginePluginCatalog.CreateRuntimeRegistry();

var detector = new GameDetector(detectionRegistry);
var selector = new EngineRuntimeSelector(runtimeRegistry);
```

A plugin that lacks `PluginCapability.Runtime` must be rejected by runtime selection even if detection succeeded.

That currently applies to:

- Dante 98
- RPG Maker 95
- RPG Maker XP
- RPG Maker VX
- RPG Maker VX Ace
- RPG Maker MV
- RPG Maker MZ
- RPG Maker Unite

WOLF and RM2K/3 advertise a runtime boundary, but neither should be described as broadly compatible gameplay yet.

## Runtime Families

### RM2000 / RM2003

`Rm2kEngineRuntime` loads bounded LCF data into deterministic simulation/scheduler/presentation structures. This is the primary active runtime.

### WOLF

`WolfEngineRuntime` is a deliberately narrow experimental runtime for the repository's unencrypted/plain-data slice. It is not proof of native-format-complete WOLF support.

### RGSS

`RgssEngineRuntime.cs` exists as experimental/research code, but XP/VX/VX Ace plugins do **not** currently advertise Runtime. Until an embedded Ruby VM and RGSS compatibility layer are implemented, UI/runtime selection must continue to refuse execution.

### MV/MZ

No JavaScript runtime is currently advertised. Bounded JSON/web metadata inspection is not runtime execution.

### RM95 / Dante / Unite

Research/detection only.

## Plugin Lifecycle

For plugins that advertise Runtime, `EnginePluginHost` owns lifecycle transitions and typed failure handling.

Conceptual lifecycle:

```text
NotStarted
   |
   v
Created -> Initialized -> Running -> Stopped
              |             |
              +-> Faulted <-+
                   |
                   v
                Disposed
```

A stopped runtime may be replaced with a fresh runtime instance for restart; stale runtime state must not be silently reused.

## Adding an Engine Plugin

1. Create a stable engine/plugin ID only when the engine is an intentional catalog target.
2. Implement bounded detection from `EngineInspectionContext.Snapshot`.
3. Add positive, negative, malformed, partial and ambiguous fixtures as appropriate.
4. Advertise only capabilities backed by real code/tests.
5. Keep parsers/runtime code engine-specific behind shared core interfaces.
6. Add `Runtime` only when `CreateRuntime` returns a meaningful, safe runtime boundary.
7. Add runtime-selection and lifecycle tests.
8. Update detection, coverage and status docs.
9. Run `./scripts/validate.sh`.

## Security Rules

Detection plugins may inspect bounded data but must not:

- execute imported EXE/DLL/SO files
- evaluate Ruby/JavaScript
- spawn the original game
- load arbitrary native libraries
- follow untrusted reparse points
- bypass protected/encrypted game formats

Future script/native runtime plugins require explicit sandbox/capability policies before execution is allowed.

## Documentation Rule

When source and documentation disagree, source/tests win and the documentation must be corrected. Do not preserve a stale “bootstrap runtime” claim merely because an old handoff used that terminology.
