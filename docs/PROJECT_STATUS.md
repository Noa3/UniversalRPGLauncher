# UniversalRPG — Project Status

> **Last reviewed:** 2026-09-11  
> **Reviewed main baseline:** `782ea66141e494d32929a9cc41056523177888eb`  
> **Primary focus:** RM2000/2003 playable runtime + reusable SDK/script-runtime foundations

## Executive Summary

UniversalRPG now has three deliberately separated product layers:

1. a Godot 4.7.2 C#/.NET launcher/application;
2. internal engine detection/runtime plugins;
3. a new Godot-free `UniversalRPG.Sdk` contract assembly for external embedding and future script VM backends.

It is **not yet a general playable replacement runtime**.

RM2000/2003 remain the most complete RPG Maker runtime path. WOLF has an experimental unencrypted/plain-data runtime. XP/VX/VX Ace and MV/MZ remain non-launchable, but their custom-script compatibility path now has real foundations: script inventory/archive decoding, ordered bootstrap abstractions, security policy, VM factory profiles, and public SDK exposure.

The last recorded canonical validation on `main` remains **296/296 headless tests passed** with a clean build. The current branch contains substantial runtime/SDK/script changes and requires fresh validation before merge.

## Current Engine Status

| Engine | Detection | Parsing / script inventory | Runtime | Script execution |
|---|---:|---:|---:|---:|
| Dante 98 | Yes | No | No | No |
| RPG Maker 95 | Yes | No native parser | No | No |
| RPG Maker 2000 | Yes | Partial LCF | Partial runtime | Native event interpreter |
| RPG Maker 2003 | Yes | Partial shared LCF | Partial runtime | Native event interpreter |
| RPG Maker XP | Yes | Metadata + bounded `Scripts.rxdata` reader | No | VM pipeline exists; no embedded Ruby backend |
| RPG Maker VX | Yes | Metadata + bounded `Scripts.rvdata` reader | No | VM pipeline exists; no embedded Ruby backend |
| RPG Maker VX Ace | Yes | Metadata + bounded `Scripts.rvdata2` reader | No | VM pipeline exists; no embedded Ruby backend |
| RPG Maker MV | Yes | Metadata + custom plugin inventory | No | VM pipeline exists; no embedded JS backend |
| RPG Maker MZ | Yes | Metadata/database + custom plugin inventory | No | VM pipeline exists; no embedded JS backend |
| WOLF RPG Editor | Yes | Experimental plain-data readers | Experimental runtime | Native WOLF event VM slice |
| RPG Maker Unite | Research candidate | No | No | No |

A script archive/plugin inventory does **not** imply executable script compatibility. XP/VX/VX Ace and MV/MZ still intentionally advertise no Runtime capability.

## Public SDK / External Library

A Godot-free public contract layer now exists under:

```text
project/src/sdk/
sdk/UniversalRPG.Sdk/UniversalRPG.Sdk.csproj
```

The standalone SDK project targets plain `.NET 8` and compiles the exact same contract sources as the main application.

Implemented public contracts include:

- `IUniversalRpgLibrary`
- `GameAnalysis`
- `EngineSupportDescriptor`
- `IUniversalRpgSession`
- `IEngineScriptingRuntime`
- `IEmbeddedScriptVm`
- `IEmbeddedScriptVmFactory`
- `EngineScriptDescriptor`
- `ScriptExecutionPolicy`
- trusted `IUniversalRpgExtension`
- trusted script-library/shim provider contracts

The in-app implementation is currently provided by:

```text
project/src/sdk_host/UniversalRpgLibraryAdapter.cs
```

It bridges the current detector/plugin host to the public SDK and can already:

- analyze a game;
- report truthful engine support level;
- expose discovered RGSS/MV/MZ script descriptors;
- create real sessions for runtime-capable engines;
- refuse sessions for parsing-only engines.

NuGet packaging remains disabled until the repository chooses an explicit source license and package/versioning policy.

See `docs/SDK.md`.

## Script Compatibility Foundation

Running game-authored scripts/plugins is now an explicit core requirement.

### Shared security/runtime contracts

`ScriptExecutionPolicy.SafeDefault` allows bounded game/save/cache access but denies by default:

- arbitrary host filesystem
- network
- clipboard
- process execution
- native interop

`IEmbeddedScriptVm` keeps concrete Ruby/JavaScript implementations replaceable.

`IEmbeddedScriptVmFactory` receives a generation-specific compatibility request instead of allowing one VM to silently substitute different historical semantics.

### RGSS / XP, VX, VX Ace

Implemented:

- bounded Ruby Marshal 4.8 subset reader for RPG Maker script archives;
- zlib script-source decompression with per-script and total limits;
- preservation of archive/script-editor order;
- SHA-256 source identity;
- Ruby-1.9-style IVAR/encoding metadata handling needed by later RGSS archives;
- `RgssScriptRuntime` ordered load/bootstrap pipeline;
- `RgssVmProfiles` separating:
  - RGSS1 -> Ruby-1.8-compatible profile
  - RGSS2 -> Ruby-1.8-compatible profile
  - RGSS3 -> Ruby-1.9.2-compatible profile
- fake-VM regression coverage proving order, language-profile enforcement, explicit bootstrap and failure propagation.

Still missing:

- actual embedded Ruby backend;
- RGSS1/2/3 host APIs;
- serialized RPG data object compatibility beyond the script archive path;
- Win32API compatibility;
- executable engine runtime registration.

### MV / MZ

Implemented:

- bounded `plugins.js` parsing without JavaScript evaluation;
- `js/plugins/*.js` inventory;
- enabled/disabled and configured load order;
- SHA-256 source hashes;
- unlisted plugin discovery without silently enabling it;
- conservative classification of:
  - standard browser-style plugin
  - Node/NW.js shim requirement
  - process-execution requirement
  - native `.node` addon requirement
  - truncated/missing source
- `WebScriptRuntime` ordered enabled-plugin bootstrap over `IEmbeddedScriptVm`;
- explicit `IWebScriptSourceProvider` separating metadata inspection from executable VFS reads;
- policy gates before process/native-capability plugins are loaded;
- MV/MZ-specific VM compatibility profiles;
- public SDK exposure of plugin inventory.

Still missing:

- actual embedded JavaScript backend;
- browser/RPG Maker API environment (`window`, timers, Canvas/WebGL/WebAudio, storage, etc.);
- safe Node/NW.js compatibility shims;
- executable MV/MZ runtime registration.

Current first JS VM spike candidate is QuickJS/QuickJS-ng. The choice is not yet vendored/final.

See `docs/SCRIPT_COMPATIBILITY.md` and `docs/VM_EVALUATION.md`.

## RM2000/2003 Runtime

Current branch work materially advances the first playable path.

Implemented foundations include:

- bounded LCF framing/BER parsing;
- typed LDB/LMT/LMU slices;
- deterministic simulation;
- verified event-command subset;
- correct raw LMU trigger mapping;
- active page selection;
- serialized foreground interpreter and independent parallel interpreters;
- repeating autorun semantics while page conditions remain active;
- simulation-owned player-input lock during foreground events;
- event-layer collision;
- Player Touch on collision and successful step;
- runtime-owned Action interaction targeting;
- action events on current tile/front tile;
- RPG_RT-style traversal across up to three Counter tiles;
- verified directional chipset passability foundation;
- renderer-neutral framebuffer/sprites/presentation state;
- real `Teleport / Place Hero` map application rather than pending state only;
- target-map parse/coordinate validation/passability/framebuffer/event replacement;
- fail-closed missing/invalid map transfer behavior;
- RTP/save/debug foundations.

Important remaining gaps:

- promote chipset passage vectors fully into first-class typed parser output and remove legacy raw fallback after validation;
- runtime tile substitution / looping maps / vehicles / moving-event collision;
- much broader event command coverage;
- RM2000 vs RM2003 semantic differences;
- faithful visible rendering;
- audio;
- menus/system flow;
- original save compatibility;
- battles;
- authorized end-to-end real-game playthrough evidence.

## WOLF

Implemented:

- WOLF detection;
- protected-data refusal boundary;
- experimental unencrypted/plain-data readers;
- database/map/event foundations;
- bounded deterministic `WolfEventVm`;
- runtime lifecycle tests.

Still experimental and not native-format/broad-game complete.

## Runtime Capability Hardening

Current branch also separates recognition from execution structurally:

- plugin selection can require capabilities;
- `EnginePluginHost` explicitly requires Runtime;
- runtime creation defensively rechecks Runtime capability;
- generic bootstrap runtime fails closed;
- obsolete RGSS pseudo-runtime was removed;
- regression coverage ensures detection-only engines cannot masquerade as launchable.

## Validation

`scripts/validate.sh` now validates six stages:

1. standalone public SDK restore;
2. standalone public SDK build;
3. Godot .NET project restore;
4. Godot .NET project build;
5. Godot headless import;
6. C# core/SDK-adapter/smoke tests.

GitHub's connected API currently exposes no workflow/status run for the latest branch commits. Therefore the current branch is **not claimed green** even though extensive regression tests have been added.

The existing GitHub workflow is configured for .NET 8 + Godot 4.7.2 Mono/.NET.

## Immediate Priorities

1. obtain a fresh full build/test run for the current branch and repair any compile/test regression;
2. keep RM2000/2003 moving toward a representative end-to-end playable map;
3. finish typed chipset passage parser migration;
4. remove remaining RPG_RT-specific input/event decisions from launcher UI where runtime APIs now exist;
5. extend real two-map transfer fixtures and broader event semantics;
6. implement the first real `IEmbeddedScriptVm` spike:
   - CRuby-family adapter investigation for RGSS;
   - QuickJS/QuickJS-ng adapter investigation for MV/MZ;
7. implement RGSS host API skeleton only after VM lifecycle/exception/sandbox tests work;
8. implement minimal MV/MZ browser host APIs only after JS VM isolation works;
9. continue WOLF format/runtime work independently from RPG Maker script VM layers;
10. keep native DLL/addon compatibility late-stage and prefer HLE/shims.

## Living Project Control

Current sources of truth:

1. source and tests;
2. `SESSION_STATE.md`;
3. this file;
4. `docs/ARCHITECTURE.md`;
5. `docs/ROADMAP.md`;
6. SDK/script-specific docs.

There is intentionally no Kanban/work-board file.
