# UniversalRPG Autonomous Session State

> **Updated:** 2026-09-11  
> **Purpose:** concise durable checkpoint for Hermes/other autonomous agents. Historical details belong in commits and dated handoffs.

## Current Objective

Continue RM2000/2003 toward a representative playable map while keeping engine detection/parsing/runtime boundaries explicit and fail-closed.

## Repository Baseline

Reviewed `main` commit before this branch:

`782ea66141e494d32929a9cc41056523177888eb`

Last recorded canonical validation on that baseline:

- `scripts/validate.sh`: passed
- .NET build: clean
- headless suite: **296/296 passed**

The current branch contains substantial runtime changes and still requires fresh validation before merge. Do not reuse 296/296 as proof that the branch is green.

## Branch Work Completed So Far

### Project/workflow cleanup

- removed `KANBAN.md` and all active Kanban dependencies
- removed obsolete undated `SESSION_REPORT.md`
- reduced this file to a restart checkpoint
- agents/Hermes choose coherent work directly from source/tests, project status and roadmap
- larger refactors are allowed when they materially improve correctness, safety or maintainability
- living documentation was refreshed against actual source/test capability boundaries

### Runtime capability hardening

- `EnginePluginRegistry` can select by required capabilities
- `EnginePluginHost` explicitly requires `PluginCapability.Runtime`
- runtime creation defensively refuses plugins without Runtime capability
- generic `EngineBootstrapRuntime` fails closed instead of masquerading as engine support
- unused `RgssEngineRuntime.cs` pseudo-runtime removed
- regression coverage added proving detection-only plugins cannot create/start runtimes

### RM2000/2003 passability and interaction

- verified chipset passage IDs/defaults against liblcf/EasyRPG
- hardened `Rm2kPassabilityMap` bounds and malformed vector handling
- typed passage fields are preferred when available, with legacy raw-field fallback during parser migration
- obsolete byte-based `Rm2kMap.TileLayer`/whole-map representation removed
- directional map passage behavior is modeled separately from event collision
- upper-layer Counter flag (`0x40`) is now exposed per map coordinate
- runtime action interaction supports the RPG_RT sequence:
  - action event on player's own coordinate
  - action event directly in front
  - up to three consecutive counter tiles, then event behind the counter

Remaining passability work:

- promote chipset passage vectors to first-class typed fields in `rm2k_parser.cs`
- remove legacy `unknown_fields` fallback after parser/fixture validation
- implement runtime tile substitution, loop-map behavior and vehicle-specific passage rules

### RM2000/2003 event/runtime correctness

Verified raw LMU trigger encoding:

- Action = 0
- Player Touch = 1
- Collision/Event Touch = 2
- Autorun = 3
- Parallel = 4

Implemented:

- `Rm2kEventTriggerCodec` maps raw LMU values to runtime trigger semantics
- invalid raw trigger values are diagnosed and skipped
- event page layer and move-frequency metadata are retained
- active-page selection is used for collision
- multiple events sharing one coordinate are searched correctly
- same-layer active events block player movement
- Player Touch can start on blocked same-layer collision
- Player Touch can start after a successful step onto a non-blocking event
- autorun pages restart while their conditions remain active
- one serialized foreground interpreter is used for autorun/action/touch/collision
- parallel event interpreters remain independently concurrent
- foreground execution owns `GameSimulationState.PlayerInputLocked`
- parallel execution does not lock player movement
- runtime rejects movement/interact input before facing/position mutation while locked
- `Rm2kEngineRuntime.TryMove()` owns movement/touch semantics
- `Rm2kEngineRuntime.TryInteract()` owns decision-key map targeting

### RM2000/2003 map transfer

`Teleport`/Place Hero is no longer only a pending-state placeholder.

Runtime update now:

1. observes `IsTransferPending`
2. resolves the requested LMU with `Rm2kMapLocator.SelectMapById`
3. parses the destination map
4. validates target coordinates
5. prepares destination framebuffer/sprite descriptors
6. configures destination passability/simulation state
7. preserves requested facing
8. replaces current map/event scheduler/presentation state
9. clears pending transfer fields only after successful application

Missing/invalid destination maps fail closed and leave the transfer pending rather than pretending completion.

Regression coverage now includes successful same-map transfer and a missing-target failure case.

## Current Architecture State

- Canonical implementation: **C#/.NET**
- Host engine: **Godot 4.7.2 stable .NET**
- Godot project root: `project/`
- Primary runtime track: RM2000/2003
- Secondary experimental track: WOLF unencrypted/plain-data runtime
- RGSS XP/VX/VX Ace: detection + parsing only
- MV/MZ: detection + metadata parsing only
- RM95/Dante/Unite: research/detection boundaries
- no project Kanban/work-board file is used

## CI / Validation State

`.github/workflows/validate.yml` on this branch is configured for:

- .NET 8
- Godot 4.7.2 Mono/.NET Linux build
- `./scripts/validate.sh`

The connected GitHub API still reports no workflow/status run for the current branch commits. Local container network access cannot clone GitHub either. Therefore the branch changes are **not yet claimed as validated**.

Required next validation when a runner is available:

1. `dotnet build project/UniversalRPG.csproj --no-restore`
2. focused C# suites, especially:
   - `TestEnginePluginContract`
   - `TestRm2kEventTriggerCodec`
   - `TestRm2kEventSchedulerBehavior`
   - `TestRm2kRuntimeInteraction`
   - `TestPluginDetection` passability/counter tests
   - existing RM2K parser/runtime tests
3. `./scripts/validate.sh`
4. update this baseline only after all results are green

## Next Automatic Development Priorities

1. promote chipset `terrain_data`, `passable_data_lower`, and `passable_data_upper` to typed LDB parser output and remove runtime dependence on `unknown_fields`
2. simplify `Main.cs` so normal map movement/action calls only `Rm2kEngineRuntime.TryMove/TryInteract`; presentation controls stay frontend-side
3. add a fixture with a real second LMU and validate cross-map transfer, target event loading and passability changes
4. implement Event Touch/Collision semantics for moving events separately from Player Touch
5. add runtime tile substitution and looping-map passage behavior
6. continue faithful map presentation/audio/menu work once movement/event/transfer flow is validated

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
