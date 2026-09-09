# Engine Coverage Audit

> **Audit date:** 2026-09-09  
> **Repository baseline:** `main` commit `782ea66141e494d32929a9cc41056523177888eb`  
> **Method:** source/tests are authoritative; capability flags are not treated as proof of complete gameplay.

## Status Vocabulary

- **Detection-only** — engine can be identified/reported; no launchable runtime capability.
- **Parsing-only** — bounded metadata/data inspection exists but no engine execution.
- **Experimental runtime** — a real in-process runtime boundary exists for a deliberately limited subset.
- **Partial runtime** — meaningful parser/simulation/interpreter work exists, but representative game compatibility is incomplete.
- **Playable** — reserved for a representative authorized end-to-end gameplay milestone. No engine currently has this label.
- **Broad compatibility** — reserved for later real-world conformance evidence. No engine currently has this label.

## Current Matrix

| Engine | Detection | Data/parsing | Event/script execution | Runtime/presentation | Overall |
|---|---|---|---|---|---|
| RPG Tsukūru Dante 98 | Yes, explicit marker | No | No | No | Detection-only research |
| RPG Maker 95 | Yes, conservative signatures | No native parser | No | No | Detection-only research |
| RPG Maker 2000 | Yes | Partial LCF | Partial verified command subset | Partial simulation/framebuffer/presentation | Partial runtime |
| RPG Maker 2003 | Yes | Partial shared LCF | Partial shared interpreter | Partial shared runtime | Partial runtime; RM2K3 parity incomplete |
| RPG Maker XP | Yes | Bounded signatures/metadata | No Ruby | No runtime capability | Parsing-only |
| RPG Maker VX | Yes | Bounded signatures/metadata | No Ruby | No runtime capability | Parsing-only |
| RPG Maker VX Ace | Yes | Bounded signatures/metadata | No Ruby | No runtime capability | Parsing-only |
| RPG Maker MV | Yes | Bounded JSON/web metadata | No JavaScript | No runtime capability | Parsing-only |
| RPG Maker MZ | Yes | Bounded JSON/database inventory | No JavaScript | No runtime capability | Parsing-only |
| WOLF RPG Editor | Yes | Experimental unencrypted/plain-data readers | Experimental bounded `WolfEventVm` | Experimental `WolfEngineRuntime` | Experimental subset |
| RPG Maker Unite | Candidate/research detection | No | No | No | Detection-only research |

## Corrections from Earlier Audits

Earlier documentation drifted from source in several places. The current source establishes that:

1. **Dante 98 is present** in the built-in catalog as `Dante98Plugin`, detection-only.
2. **RM95 is detection-only**; it does not advertise Runtime.
3. **XP/VX/VX Ace advertise Detection + Parsing only**; the selector must refuse runtime execution.
4. **MV/MZ advertise Detection + Parsing only**; there is no JavaScript runtime capability.
5. **WOLF advertises Runtime**, but only for the repository's intentionally narrow unencrypted/plain-data slice.
6. RM2K/RM2K3 are the primary runtime path and advertise Runtime/SaveLoad/Debugging, but those capability flags do not mean full engine fidelity.

## Evidence by Subsystem

### Shared plugin/detection infrastructure

- `project/src/plugins/EnginePluginContract.cs`
- `project/src/plugins/EngineDetectionContract.cs`
- `project/src/plugins/EnginePluginRegistry.cs`
- `project/src/plugins/EngineRuntimeSelection.cs`
- `project/src/plugins/EnginePluginHost.cs`
- `project/src/plugins/BuiltInEnginePlugins.cs`

The built-in catalog currently registers:

- RPG Maker 95
- Dante 98
- RPG Maker 2000
- RPG Maker 2003
- RPG Maker XP
- RPG Maker VX
- RPG Maker VX Ace
- RPG Maker MV
- RPG Maker MZ
- WOLF RPG Editor
- RPG Maker Unite candidate detection

### RM2000/2003

Primary evidence:

- `project/src/plugins/Rm2kEngineRuntime.cs`
- `project/src/rm2k/parser/`
- `project/src/rm2k/interpreter/`
- `project/src/rm2k/simulation/`
- `project/src/rm2k/rendering/`
- `project/src/rm2k/presentation/`
- focused C# tests under `project/tests/core/`

The runtime includes meaningful deterministic state/interpreter/presentation foundations, but complete renderer, passability, audio, menus, battle and original save parity are not established.

### RGSS

Evidence:

- `RgssPlugin` in `BuiltInEnginePlugins.cs`
- `project/src/plugins/RgssEngineRuntime.cs` as research/experimental code
- `project/tests/core/TestRgssRuntime.cs`

The plugin metadata intentionally omits Runtime. Existing runtime experiments must not be interpreted as supported Ruby/RGSS execution.

### MV / MZ

Evidence:

- `WebRpgPlugin`, `RpgMakerMvPlugin`, `RpgMakerMzPlugin`
- plugin detection/metadata tests
- bounded `System.json` parsing and MZ database inventory

No imported JavaScript is executed.

### WOLF

Evidence:

- `project/src/plugins/WolfEngineRuntime.cs`
- `project/src/plugins/WolfPlugin.cs`
- `project/src/wolf/WolfDataReader.cs`
- `WolfDatabaseReader.cs`
- `WolfMapReader.cs`
- `WolfEventVm.cs`
- `project/tests/core/TestWolfRuntime.cs`

This is a bounded experimental plain-data slice, not complete native WOLF compatibility.

## Validation Evidence

The latest recorded canonical result on the reviewed `main` history is:

- clean .NET build
- `scripts/validate.sh` passed
- **296/296** headless tests passed

This documentation audit did not modify runtime code, so it does not invent a newer runtime test count. Any code change after the reviewed baseline requires fresh validation.

## Engine Completion Gates

### RM2000 / RM2003

Before “playable”:

- verified passability/chipset behavior
- faithful visible map/sprite/window presentation
- representative event-command path
- transfers/interactions
- audio
- basic menus/system flow
- save/load behavior
- authorized real game fixture path

### WOLF

Before “playable”:

- supported WOLF version range
- native-format readers backed by authorized fixtures
- substantially broader event VM
- renderer/input/audio/UI/save path
- representative real project

### XP / VX / VX Ace

Before Runtime capability:

- embedded Ruby boundary
- RGSS1/2/3 profile
- generation-specific serialized data/archive support
- graphics/audio/input APIs
- explicit Win32API policy
- default script boot tests

### MV / MZ

Before Runtime capability:

- embedded JavaScript boundary
- browser/RPG Maker compatibility layer
- rendering/audio/input/storage APIs
- plugin compatibility policy
- representative project boot tests

### RM95 / Dante

Remain research until verified format sources and authorized fixtures justify parser/runtime investment.

### Unite

Remain research unless there is a realistic project/data conversion strategy. Do not promise general Unity runtime compatibility.

## Conclusion

The repository has a credible shared plugin/detection/security foundation and a meaningful RM2K/RM2K3 runtime implementation in progress. WOLF is a useful experimental secondary runtime track. All other listed RPG Maker families are currently bounded detection/parsing or research tracks.

No documentation should use “all RPG Maker versions supported” without qualifying the engine-specific milestone/capability status.
