# Engine Coverage Audit

Date: 2026-08-24
Scope: RPG Tsukūru Dante 98, RPG Maker 95, RPG Maker 2000/2003, XP, VX, VX Ace, MV/MZ, WOLF RPG Editor, and RPG Maker Unite.

## Method and status vocabulary

This audit distinguishes engine identification from playable compatibility. A registered plugin, a metadata parser, or a deterministic lifecycle bootstrap is not full gameplay support. Classifications are based on repository source, tests, and documentation. `Implemented` means the named subsystem exists and is exercised; `Partial` means bounded or synthetic behavior; `Detection-only` means identification/inspection without launchable runtime; `Missing` means no repository implementation was found; `Broken` means the canonical validation path fails.

Imported games remain untrusted input. Detection and inspection must not execute game EXEs, DLLs, Ruby, JavaScript, shell commands, or native plugins.

## Current coverage matrix

| Engine | Detection | Plugin/runtime selection | Data loading | Event/script execution | Simulation | Rendering/presentation | Input/audio/UI/save/battle | Tests | Overall |
|---|---|---|---|---|---|---|---|---|---|
| Dante 98 | Missing; no Dante symbol or detector/plugin found | Missing | Missing | Missing | Missing | Missing | Missing | Missing | Unsupported; independent research track required |
| RPG Maker 95 | Partial detection/research boundary via `rpg-maker-95` | Detection plugin exists, but no playable runtime evidence | Missing parser/runtime | Missing | Missing | Missing | Missing | Positive/weak-boundary detection tests only | Detection/research-only |
| RM2K | Implemented detection | Implemented parser-backed runtime selection | Partial: bounded LDB/LMT/first-LMU loading | Partial: `EventInterpreter` supports a bounded command subset and skips unsupported commands diagnostically | Partial: `GameSimulationState`, movement, switches/variables, timers, actors/items/battle state | Partial: renderer-neutral `VirtualFramebuffer` and sprite/presentation adapters; no complete Godot renderer | Partial data/state fields and save codec; no complete audio/input/menu/battle parity | Strongest focused C# coverage, but fixture/runtime parity remains incomplete | Partial runtime foundation |
| RM2K3 | Implemented detection | Shared RM2K runtime/plugin path | Partial shared LCF parser; version-specific completeness not demonstrated | Partial shared interpreter; RM2K3-specific command/behavior parity not demonstrated | Partial shared simulation | Partial shared renderer/presentation foundation | Partial shared state/save foundation | Shared RM2K tests; no complete RM2K3 parity suite found | Partial, shared implementation |
| XP | Implemented detection | Bounded RGSS bootstrap | Partial metadata/source inspection; no Ruby object/data runtime | Missing RGSS/Ruby execution | Missing gameplay simulation | Missing full renderer | Missing full platform services | `TestRgssRuntime` covers detection/bootstrap boundaries | Bootstrap-only |
| VX | Implemented detection | Bounded RGSS bootstrap | Partial metadata/source inspection | Missing RGSS/Ruby execution | Missing gameplay simulation | Missing full renderer | Missing full platform services | `TestRgssRuntime` covers shared RGSS boundary | Bootstrap-only |
| VX Ace | Implemented detection | Bounded RGSS bootstrap | Partial metadata/source inspection | Missing RGSS/Ruby execution | Missing gameplay simulation | Missing full renderer | Missing full platform services | `TestRgssRuntime` covers shared RGSS boundary | Bootstrap-only |
| MV | Implemented detection | Bounded bootstrap; no JavaScript VM | Partial bounded metadata/data inspection | Missing JavaScript execution and plugin behavior | Missing gameplay simulation | Missing full renderer | Missing full platform services | Detection and bounded web fixtures in `CSharpRunner` | Bootstrap-only |
| MZ | Implemented stricter detection and bounded database inventory | Bounded bootstrap; intentionally non-launchable in-process | Partial: `System.json`, Actors/MapInfos and inventory/encrypted-asset diagnostics | Missing JavaScript execution and plugin behavior | Missing gameplay simulation | Missing full renderer | Missing full platform services | Positive, missing-manager, malformed, oversized, inventory, encrypted-asset fixtures | Bootstrap/metadata-only |
| WOLF RPG | Implemented bounded signature detection | Bounded plain-data runtime | Partial: explicit unencrypted JSON test envelope, database/map/event readers | Partial synthetic `WolfEventVm`; unknown operations fault; native/protected format not supported | Partial deterministic VM state | Missing full WOLF renderer | Missing full audio/input/UI/save/battle | `TestWolfRuntime` covers plain, protected, limits, choice, unknown op | Partial/experimental plain-data slice |
| RPG Maker Unite | Candidate detection only | No runtime capability; selection refuses launch | Missing Unite project/data parser | Missing | Missing | Missing | Missing | Detection-only negative/non-provable Unity export coverage | Detection-only |

## Shared infrastructure and evidence

- Engine identity and display mapping: `project/src/game_detector/game_detector.cs` (`EngineType`, `DetectableEngines`, display-name mapping).
- Built-in registration: `project/src/plugins/BuiltInEnginePlugins.cs` (`CreatePlugins`, `CreateDetectionRegistry`, `CreateRuntimeRegistry`). The catalog contains RM95, RM2K, RM2K3, XP, VX, VX Ace, MV, MZ, WOLF, and Unite; it contains no Dante 98 entry.
- Contracts and safety boundary: `project/src/plugins/EnginePluginContract.cs`; runtime selection validates exact plugin ID, capabilities, platform, engine range, and compatibility probe without external fallback.
- Bounded inspection and archive handling: `project/src/core/virtual_filesystem.cs` and plugin inspection contracts. ZIP inspection is read-only and bounded; executable/library contents are data only.
- RM2K runtime: `project/src/plugins/Rm2kEngineRuntime.cs`; parser `project/src/rm2k/parser/rm2k_parser.cs`; map/database models under `project/src/rm2k/`.
- RM2K interpreter: `project/src/rm2k/interpreter/EventInterpreter.cs`. It explicitly documents verified command constants, deterministic execution, wait/branch/loop/choice/input handling, and diagnostic skipping of unsupported commands.
- RM2K state/save: `project/src/rm2k/simulation/GameSimulationState.cs` and `project/src/rm2k/simulation/Rm2kSimulationSaveCodec.cs`. These are in-memory deterministic state tools, not compatibility with original save files.
- RM2K presentation/rendering: `project/src/rm2k/rendering/VirtualFramebuffer.cs`, `Rm2kSpriteRenderer.cs`, and `project/src/rm2k/presentation/PresentationState.cs`. They provide renderer-neutral bounded state, not a complete engine renderer.
- RGSS boundary: `project/src/plugins/RgssEngineRuntime.cs` and `project/tests/core/TestRgssRuntime.cs`; XP/VX/VX Ace are distinct RGSS generations but currently share a bounded bootstrap and do not execute Ruby/RGSS scripts.
- WOLF boundary: `project/src/plugins/WolfEngineRuntime.cs`, `project/src/plugins/WolfPlugin.cs`, `project/src/wolf/WolfDataReader.cs`, `WolfDatabaseReader.cs`, `WolfMapReader.cs`, and `WolfEventVm.cs`. The source contains a synthetic bounded VM; `docs/ENGINE_PLUGINS.md` should be read as “full WOLF VM not implemented,” not as denying that this bounded VM exists.
- MZ inspection: `BuiltInEnginePlugins.cs` and `project/tests/CSharpRunner.cs`; requires MZ runtime signatures and bounded valid data. JavaScript, HTML, native binaries, and external runtimes are not executed.
- Validation: `scripts/validate.sh` performs restore, .NET build, Godot headless import, and the C# runner scene.

## Engine-specific completion criteria

“Complete” below means complete for the declared supported version range, not merely able to identify a folder.

### Dante 98

1. Decide scope and legal/format sources independently from RM95.
2. Add positive, negative, malformed, and ambiguous detection fixtures with a stable engine ID.
3. Pin an independently verified format specification or document the reverse-engineering boundary.
4. Implement bounded project/database/map/event parsing before any runtime work.
5. Add deterministic interpreter, map simulation, renderer, input/audio, UI, save/load, and real-project launch fixtures.
6. Do not alias Dante 98 to RM95 without format evidence.

### RPG Maker 95

1. Preserve the current conservative detector boundary and add a parser only from verified format evidence.
2. Decode representative project, map, event, database, graphics/audio references, and legacy encodings with malformed-input limits.
3. Implement the event model and deterministic simulation, then rendering and platform services.
4. Validate against authorized real RM95 game fixtures and a representative gameplay path.

### RM2K and RM2K3

1. Complete LCF conformance for LDB/LMT/LMU and version-specific sections; unknown fields must be preserved or rejected explicitly.
2. Expand `EventInterpreter` against the complete supported command set, with explicit diagnostics for unsupported commands and recursion/wait limits.
3. Connect parser output to map/player/event simulation, renderer/presentation, input, audio, menus, inventory, battle, and transitions.
4. Define and test original-save compatibility separately from the current in-memory `Rm2kSimulationSaveCodec` round-trip.
5. Add real RM2K and RM2K3 fixtures and end-to-end launch tests; do not mark full support from synthetic fixtures alone.

### XP, VX, and VX Ace

1. Select and license/document the Ruby implementation and embedding boundary.
2. Decode each generation’s serialized data and archive variants (`.rxdata`, `.rvdata`, `.rvdata2`, `.rgssad/.rgss2a/.rgss3a`) with version-specific tests.
3. Implement RGSS1/2/3 APIs, script loading, graphics/audio/input/window APIs, and a compatibility policy for `Win32API`.
4. Test default scripts and representative third-party scripts per generation, including encrypted archive diagnostics and save/load.
5. Require real-game fixture launch and representative map/event/battle paths before calling the generation playable.

### MV and MZ

1. Select an embeddable JavaScript engine and record license, version, sandbox, memory, filesystem, and API policy.
2. Load JSON databases/maps and `js/plugins.js` through the in-process runtime; never execute imported code outside the sandbox.
3. Implement the engine API surface used by default scripts and a documented plugin compatibility subset.
4. Implement rendering, input, audio, scenes/menus, save/load, encrypted-asset policy, and Node/NW/Electron compatibility shims as explicit capabilities.
5. Add distinct MV/MZ real-project fixtures, plugin execution tests, and launch/save/load/transition paths.
6. MZ’s current metadata inventory and Electron launch verification are evidence for packaging/inspection only, not in-process runtime support.

### WOLF RPG Editor

1. Pin authorized native format documentation/fixtures and define the supported WOLF version range.
2. Replace the synthetic plain-JSON envelope with bounded native readers where legally and technically justified.
3. Expand VM opcode coverage with per-opcode tests, deterministic state semantics, and explicit unsupported-operation diagnostics.
4. Decide whether protected/encrypted data is permanently unsupported; never treat rejection diagnostics as compatibility.
5. Add map rendering, input/audio/UI/save/battle and authorized real-project gameplay fixtures.

### RPG Maker Unite

1. Decide whether Unite is in product scope. Generic Unity exports cannot prove Unite provenance.
2. If in scope, obtain authorized Unite project/export fixtures and identify the Unity/RPG Maker data/runtime contract.
3. Implement a Unity asset/data reader and runtime boundary only after provenance and licensing are established.
4. Add a real Unite fixture; until then retain candidate detection-only status.

## Recommended implementation order and dependencies

1. Stabilize the canonical build/test path and shared diagnostics, bounded virtual filesystem, archive handling, encoding, and fixture harness. Every engine depends on this.
2. Finish RM2K/RM2K3 because parser, interpreter, simulation, save, rendering-neutral, and presentation foundations already exist.
3. Separate RM2K and RM2K3 capability/version tests where behavior diverges.
4. Resolve RGSS Ruby engine selection and implement XP/VX/VX Ace generation-specific compatibility.
5. Expand WOLF only with authorized native fixtures and an explicit protected-data policy.
6. Select and sandbox a JavaScript engine, then implement MV/MZ as a separate runtime project.
7. Treat RM95 and Dante 98 as independent legacy reverse-engineering tracks.
8. Resolve Unite scope and provenance last; do not promote generic Unity detection to runtime support.

## Risks and known gaps

- Canonical validation may expose Godot/.NET source-generator or script-loading failures; fresh validation is required before relying on historical pass counts.
- `project/src/game_detector/game_detector.cs` still has legacy PE metadata helper limitations (`_get_file_version()` / `_get_dll_imports()` in the older compatibility path) documented in project status.
- RM2K parser completeness and undocumented/unknown-field behavior remain compatibility risks.
- RGSS requires Ruby version/API compatibility and a `Win32API` policy; third-party scripts are a much larger surface than default scripts.
- MV/MZ JavaScript runtime choice affects licensing, sandboxing, plugin compatibility, memory, and platform exports.
- WOLF protected/native formats are not equivalent to the repository’s explicit plain-data test envelope.
- RM95 and Dante 98 have incomplete/independent format knowledge; conflating them would create false positives and unsafe parser assumptions.
- Documentation drift exists around WOLF: source has a bounded synthetic VM while catalog wording says the full VM is not implemented.
- Full support requires real authorized fixtures; synthetic tests validate contracts and limits, not engine fidelity.

## Validation plan

Canonical command from repository policy:

```bash
GODOT_BIN=/absolute/path/to/Godot_v4.7.2-stable_mono_win64_console.exe ./scripts/validate.sh
```

Fresh run on 2026-08-24: `bash scripts/validate.sh` completed restore and `.NET build --no-restore` successfully (`0` warnings, `0` errors), then stopped with exit `127` because Godot 4.7.2 was not found. The script requested `GODOT_BIN=/absolute/path/to/Godot` or installation under `tools/godot/editors/4.7.2/`. Therefore no fresh Godot import or C# test count is claimed in this audit. Focused tests should be exercised through the same C# runner and include:

- `TestPluginDetection` for all detector boundaries and malformed/ambiguous cases;
- `TestRgssRuntime` for XP/VX/VX Ace shared bootstrap constraints;
- `TestWolfRuntime` for plain-data load, limits, protected rejection, choices, and unknown operations;
- RM2K parser/interpreter/simulation/rendering/presentation/save tests;
- MZ data-directory and encrypted-asset diagnostics in the C# runner.

A passing validation script proves repository build/import/test health only. It does not prove playable compatibility for any engine.

## External format references

These are background references for future implementation criteria, not proof of repository support:

- EasyRPG/liblcf and its LCF data-structure reference: https://github.com/EasyRPG/liblcf and https://wiki.easyrpg.org/development/data-structure-reference
- RPG Maker 95 reverse-engineering notes: https://github.com/Ghabry/rpg95-fileformat
- RPG Maker MV plugin specifications: https://rpgmakerofficial.com/product/MV_Help/page/01_11_03.html
- RGSS reference material: https://www.rpg-maker.fr/dl/monos/aide/vx/source/rgss/index.html

## Audit conclusion

The repository has a credible detection, bounded inspection, plugin-contract, and deterministic-runtime foundation. RM2K/RM2K3 are the only engines with a meaningful parser-backed gameplay foundation; WOLF has a deliberately narrow plain-data/synthetic VM slice. XP/VX/VX Ace and MV/MZ are safe bootstraps/metadata paths, RM95 is research-boundary detection, Unite is non-provable candidate detection, and Dante 98 is absent. No target should be described as feature-complete until the engine-specific real-fixture and gameplay criteria above are met.

STATUS: PARTIALLY INVESTIGATED

Next action: run the canonical validation command and attach its fresh result to the Kanban handoff; if it fails, track that failure before expanding engine scope.

## Sources

Repository evidence is cited by path and symbol throughout. External background sources are listed above and were not used as proof of current implementation.

Sources:
- https://github.com/EasyRPG/liblcf
- https://wiki.easyrpg.org/development/data-structure-reference
- https://github.com/Ghabry/rpg95-fileformat
- https://rpgmakerofficial.com/product/MV_Help/page/01_11_03.html
- https://www.rpg-maker.fr/dl/monos/aide/vx/source/rgss/index.html
