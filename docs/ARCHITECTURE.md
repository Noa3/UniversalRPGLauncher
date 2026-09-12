# UniversalRPG — Architecture

> **Status:** Active implementation  
> **Last reviewed:** 2026-09-11  
> **Primary milestone:** RM2000/2003 faithful runtime foundation  
> **Secondary maintained boundaries:** cross-engine detection and experimental WOLF plain-data runtime

## Design Principles

Priority order:

1. correct game behavior
2. compatibility
3. security
4. stability
5. maintainability
6. performance
7. optional enhancements

Engine recognition must never be presented as playable compatibility.

## Repository Boundary

The canonical implementation is C#/.NET under Godot 4.7.2 .NET.

```text
/
├── project/
│   ├── app/                  # launcher, library, UI
│   ├── src/
│   │   ├── core/             # shared deterministic/runtime services
│   │   ├── compatibility/    # profiles and compatibility rules
│   │   ├── game_detector/    # UI/library compatibility facade
│   │   ├── plugins/          # engine contracts, registries and runtimes
│   │   ├── rm2k/             # RM2000/2003 parser/simulation/presentation
│   │   └── wolf/             # experimental WOLF plain-data slice
│   └── tests/
├── docs/
├── scripts/
└── tools/
```

Future RGSS and MV/MZ runtimes should be added behind the existing plugin/runtime boundaries rather than coupled directly to launcher UI code.

## Layered Runtime Model

```text
Imported game folder / ZIP
        |
        v
SafeGameInspector
  bounded + read-only
        |
        v
EngineDetectionRegistry
        |
        v
EngineDetectionReport
        |
        v
EngineRuntimeSelector / EnginePluginHost
        |
        | requires PluginCapability.Runtime
        v
IEnginePlugin / IEngineRuntime
        |
        +-----------------------------+
        | engine-specific subsystem   |
        | parser / VM / simulation    |
        +-----------------------------+
        |
        v
Shared runtime services
 VFS / clock / diagnostics / saves / compatibility
        |
        v
Godot application/platform presentation
```

Imported executables, DLLs and scripts are **data during inspection**. Detection does not dynamically load user-provided detector plugins.

## Plugin Capability Model

Capabilities are explicit and must match real implementation.

Current high-level boundaries:

| Engine | Capabilities / boundary |
|---|---|
| Dante 98 | Detection only |
| RPG Maker 95 | Detection only |
| RPG Maker 2000 | Detection + parsing + partial runtime/save/debug foundation |
| RPG Maker 2003 | Detection + parsing + partial runtime/save/debug foundation |
| XP / VX / VX Ace | Detection + parsing only; no Ruby VM |
| MV / MZ | Detection + parsing/metadata only; no JavaScript VM |
| WOLF RPG | Detection + parsing + experimental unencrypted plain-data runtime |
| Unite | Detection/research only |

A generic lifecycle object is not proof of engine compatibility.

Runtime safety is enforced at multiple layers:

1. runtime-facing selection requests `PluginCapability.Runtime`
2. `EnginePluginHost` refuses plugins without Runtime before runtime creation
3. `EnginePluginRegistry.CreateRuntime()` re-checks Runtime capability defensively
4. the generic `EngineBootstrapRuntime` is deliberately non-launchable and fails closed
5. engine plugins that actually support runtime behavior provide concrete runtimes such as `Rm2kEngineRuntime` or `WolfEngineRuntime`

The old unregistered RGSS pseudo-runtime was removed so future RGSS execution work starts from a real embedded Ruby/compatibility design rather than a metadata lifecycle placeholder.

See [ENGINE_PLUGINS.md](ENGINE_PLUGINS.md).

## Detection

Default bounded inspection limits are defined by `GameInspectionLimits`:

- depth: 4
- entries: 4096
- metadata bytes per file: 1 MiB
- archive total uncompressed budget: 64 MiB
- archive entry metadata budget: 1 MiB
- bounded executable/data prefix reads

When the entry budget is reached on otherwise valid input, the snapshot is marked **partial**, not automatically malformed.

Detection order is deterministic by score, plugin priority and plugin ID. Ambiguous top candidates remain unresolved rather than being silently forced to a runtime.

## RM2000/2003 Runtime

The LCF runtime is currently the most developed engine path.

Implemented foundations include:

- bounded LCF framing/BER parsing
- LDB/LMT/LMU/LSD-related structured readers
- typed portions of the database model
- parser-backed runtime initialization
- deterministic `VirtualClock`
- `GameSimulationState`
- event scheduler and a growing verified command subset
- map movement/transfer state
- renderer-neutral framebuffer and sprite descriptors
- presentation state for messages/choices/pictures
- RTP registry/diagnostics primitives
- runtime-owned save codec and read-only original-LSD framing model
- compatibility diagnostics and regression tests

Important incomplete areas include:

- complete LDB/LMU semantics
- verified chipset/passability mapping
- complete RM2K/RM2K3 event command parity
- actual faithful Godot tile/sprite/window rendering
- audio
- menu/system scenes
- battle parity
- original save semantic compatibility
- broad real-game end-to-end testing

RM2000 and RM2003 share infrastructure but must gain version-specific tests where behavior diverges.

## WOLF Runtime

WOLF is a separate engine family, not an RPG Maker mode.

Current code contains a deliberately narrow, bounded, unencrypted plain-data reader/runtime/VM used to establish architecture and tests. It is **not** native-format-complete and does not support protected/encrypted game data.

Future WOLF work must keep its database/event semantics independent from RM2K assumptions.

## RGSS Runtime Boundary

XP, VX and VX Ace currently expose detection/parsing metadata only.

Future architecture:

```text
XP/VX/VX Ace plugin
        |
        v
IRubyVm
        |
        v
RGSS1 / RGSS2 / RGSS3 profile
        |
        v
URPG graphics/audio/input/filesystem services
```

No Ruby implementation has been selected or embedded yet. There is intentionally no metadata-only RGSS runtime class in the active code path.

## MV/MZ Runtime Boundary

MV and MZ currently perform bounded web-game detection and metadata inspection. MZ additionally has bounded database inventory helpers.

Future architecture:

```text
MV/MZ plugin
    |
    v
IJavaScriptVm
    |
    v
Browser/RPG Maker compatibility layer
    |
    v
URPG services
```

No imported JavaScript is currently executed.

## Compatibility Profiles

Game-specific fixes belong in centralized, validated compatibility profiles keyed by reliable identity such as hashes/signatures. Unknown hashes must not inherit unrelated same-engine fixes.

Compatibility data should describe:

- engine/generation
- game/plugin hash
- supported capability
- flags/workarounds
- diagnostics
- regression-test reference

## Security Boundary

Imported games are untrusted.

Required rules include:

- no arbitrary process execution
- no unrestricted host filesystem
- no automatic native-library loading
- no network/clipboard by default for future script runtimes
- bounded parser allocations/depth/counts
- VFS containment for runtime file access
- explicit permission/capability policy for dangerous functionality
- unsupported runtimes remain fail-closed rather than using generic success bootstraps

See [IMPORT_SECURITY.md](IMPORT_SECURITY.md).

## Validation

Canonical repository validation:

```bash
./scripts/validate.sh
```

Last recorded canonical result on the reviewed `main` baseline: **296/296** headless tests passed with clean build. The current branch contains runtime code changes and therefore needs fresh validation before merge.

## Sources of Truth

For current work, use in this order:

1. actual source and tests
2. `SESSION_STATE.md`
3. `docs/PROJECT_STATUS.md`
4. this document
5. roadmap/research documents

There is intentionally no Kanban/work-board source of truth. Historical session handoffs are evidence, not current architecture authority.
