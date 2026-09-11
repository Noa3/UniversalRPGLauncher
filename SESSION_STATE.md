# UniversalRPG Autonomous Session State

> **Updated:** 2026-09-11  
> **Purpose:** concise durable checkpoint for Hermes/other autonomous agents. Historical details belong in commits and dated handoffs.

## Current Objective

Continue RM2000/2003 toward a representative playable map while keeping the shared plugin/runtime boundary fail-closed and engine-agnostic.

## Repository Baseline

Reviewed `main` commit before this branch:

`782ea66141e494d32929a9cc41056523177888eb`

Last recorded canonical validation on that baseline:

- `scripts/validate.sh`: passed
- .NET build: clean
- headless suite: **296/296 passed**

The current branch contains runtime code changes and requires fresh validation before merge. Do not reuse 296/296 as proof that the branch is green.

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

### RM2000/2003 passability cleanup

- verified chipset passage field IDs and defaults against liblcf/EasyRPG
- hardened `Rm2kPassabilityMap` with bounded map dimensions/tile counts
- typed `passable_data_lower` / `passable_data_upper` fields are preferred when present
- legacy raw `unknown_fields` remain supported temporarily during parser migration
- oversized or malformed passage vectors fail closed
- obsolete byte-based `Rm2kMap.TileLayer` / whole-map container state removed; real RM2K tile IDs are not constrained to 8-bit
- dedicated passability regression tests added

Remaining passability work:

- promote chipset passage vectors to first-class typed fields in `rm2k_parser.cs`
- remove legacy raw-field fallback after parser/real-fixture validation
- add runtime tile substitution, looping-map and vehicle/event-specific passage behavior

### RM2000/2003 event/runtime correctness

Verified liblcf LMU trigger encoding:

- Action = 0
- Player Touch = 1
- Collision/Event Touch = 2
- Autorun = 3
- Parallel = 4

Changes:

- added `Rm2kEventTriggerCodec` so raw LMU values map correctly to the existing internal scheduler semantics
- invalid trigger values are diagnosed and skipped
- runtime now imports verified event-page layer and move-frequency metadata
- EventPage model now stores layer and move-frequency explicitly
- scheduler now searches all events sharing a coordinate instead of allowing the first non-matching event to mask later matches
- active same-layer events are exposed as blocking collision geometry
- player movement now stops on active same-layer events; a matching Player Touch page is queued without moving the player into the event tile
- below/above-layer events remain non-blocking in the current geometry model
- autorun pages restart after completion while their conditions remain active
- foreground execution is serialized: at most one autorun/action/touch/collision interpreter runs at once
- parallel pages remain independently concurrent
- scheduler exposes `ForegroundBusy` for the next input-lock integration step

New regression suites cover:

- LMU trigger mapping and invalid trigger rejection
- multiple events sharing one coordinate
- active-page layer collision queries
- repeating autorun behavior
- serialized foreground autoruns
- same-layer Player Touch collision through the real RM2K fixture
- same-layer Action events blocking without being incorrectly started as Player Touch

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

`.github/workflows/validate.yml` on this branch is already configured for:

- .NET 8
- Godot 4.7.2 Mono/.NET Linux build
- `./scripts/validate.sh`

GitHub currently reports no workflow/status run for the latest branch commits through the connected API. Local container network access also cannot clone the repository. Therefore the new branch changes are **not yet claimed as validated**.

Required next validation when a runner is available:

1. `dotnet build project/UniversalRPG.csproj --no-restore`
2. focused C# test runner, especially:
   - `TestEnginePluginContract`
   - `TestRm2kEventTriggerCodec`
   - `TestRm2kEventSchedulerBehavior`
   - `TestRm2kRuntimeInteraction`
   - existing RM2K parser/passability/plugin tests
3. `./scripts/validate.sh`
4. update this baseline only after all results are green

## Next Automatic Development Priorities

1. lock player movement/action input while `Rm2kEventScheduler.ForegroundBusy` is true, while allowing parallel events to coexist with player input
2. move action/touch interaction decisions out of `Main.cs` into the RM2K runtime so keyboard/controller/touch frontends share identical engine semantics
3. promote chipset passage arrays to typed parser output and validate them against the pinned real fixtures
4. implement target-map application for pending transfers instead of only storing transfer requests
5. continue map presentation/audio/menu work after movement/event flow is stable

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
