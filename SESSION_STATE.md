# UniversalRPG Autonomous Session State

> **Updated:** 2026-09-11  
> **Purpose:** concise durable checkpoint for Hermes/other autonomous agents. Historical details belong in commits and dated handoffs.

## Current Objective

Harden the plugin/runtime boundary so engine detection can never be mistaken for executable runtime support, then continue the RM2000/2003 path toward a real playable milestone.

Current branch work:

- project Kanban workflow removed
- `EnginePluginRegistry` can select by required capabilities
- `EnginePluginHost` explicitly requires `PluginCapability.Runtime`
- runtime creation defensively refuses plugins without Runtime capability
- generic `EngineBootstrapRuntime` changed to fail closed instead of simulating a successful engine lifecycle
- unused `RgssEngineRuntime.cs` pseudo-runtime removed
- regression coverage added for detection-only plugins being unable to create/start runtimes

## Repository Baseline

Reviewed `main` commit before this branch:

`782ea66141e494d32929a9cc41056523177888eb`

Last recorded canonical validation on that baseline:

- `scripts/validate.sh`: passed
- .NET build: clean
- headless suite: **296/296 passed**

The current branch now contains runtime code changes and therefore requires a fresh validation before merge. Do not reuse 296/296 as proof that the branch is green.

## Current Architecture State

- Canonical implementation: **C#/.NET**
- Host engine: **Godot 4.7.2 stable .NET**
- Godot project root: `project/`
- Primary runtime track: RM2000/2003
- Secondary experimental track: WOLF unencrypted/plain-data runtime
- RGSS XP/VX/VX Ace: detection + parsing only
- MV/MZ: detection + metadata parsing only
- RM95/Dante/Unite: research/detection boundaries
- No Kanban/work-board file is used

## Important Current Facts

### Runtime capability boundary

Detection, parsing and runtime execution are deliberately separate.

- a plugin may be recognized without being launchable
- `EnginePluginHost` now requests Runtime capability during selection
- `EnginePluginRegistry.CreateRuntime` re-checks Runtime capability defensively
- the generic bootstrap path is non-launchable
- dead RGSS pseudo-runtime code has been removed so future RGSS work starts from a real embedded Ruby/compatibility design

### RM2000/2003

Implemented foundations include:

- bounded LCF parsing
- LMT + typed LDB/LMU slices
- deterministic simulation clock/state
- event scheduler
- growing verified event-command subset
- runtime restart/reset behavior
- movement/transfer/presentation state
- renderer-neutral framebuffer/sprite structures
- RTP resolver/diagnostics
- runtime-owned save tooling
- read-only original-LSD framing model

Major unresolved areas:

- verified chipset/passability semantics and fixtures
- full faithful presentation/audio/menu/battle/save parity
- broader real-game end-to-end validation

### WOLF

A bounded experimental unencrypted/plain-data reader, event VM and runtime exist. This is not complete native WOLF compatibility.

### Other engines

Do not describe experimental source files as runtime support when the plugin does not advertise `PluginCapability.Runtime`.

Specifically:

- XP/VX/VX Ace: no Runtime capability
- MV/MZ: no Runtime capability
- RM95: no Runtime capability
- Dante 98: no Runtime capability
- Unite: no Runtime capability

## Next Automatic Action

1. run the narrow plugin-contract/runtime-selection tests
2. run `./scripts/validate.sh`
3. if green, update the verified baseline in this file and `docs/PROJECT_STATUS.md`
4. if validation fails, repair the regression using the anti-loop policy
5. after the branch is stable, return to RM2000/2003 and select the next coherent slice that most directly advances verified passability/rendering toward an end-to-end playable map

## Documentation Recovery Rule

At session start read:

1. `AGENTS.md`
2. this file
3. `docs/PROJECT_STATUS.md`
4. `docs/ARCHITECTURE.md`
5. relevant source/tests
6. `docs/ROADMAP.md` when choosing a new area

Historical `SESSION_HANDOFF_*.md` and dated QA reports are snapshots, not current authority.

## Failure / Anti-Loop Rule

For the same normalized failure signature:

- at most 3 materially different repair strategies
- do not rerun the exact same failed command more than twice without new evidence/change
- after threshold: preserve useful work, document the blocker here, and continue with an independent useful area when possible

Never delete/disable correct tests to make validation green.
