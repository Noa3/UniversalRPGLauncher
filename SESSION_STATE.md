# UniversalRPG Autonomous Session State

> **Updated:** 2026-09-09  
> **Purpose:** concise durable checkpoint for Hermes/other autonomous agents. Historical details belong in commits, KANBAN evidence and dated handoffs.

## Repository Baseline

Reviewed `main` commit:

`782ea66141e494d32929a9cc41056523177888eb`

Last recorded canonical validation on that implementation baseline:

- `scripts/validate.sh`: passed
- .NET build: clean
- headless suite: **296/296 passed**

A documentation-only branch does not create a newer runtime validation result.

## Current Architecture State

- Canonical implementation: **C#/.NET**
- Host engine: **Godot 4.7.2 stable .NET**
- Godot project root: `project/`
- Authoritative work queue: `KANBAN.md`
- Primary runtime track: RM2000/2003
- Secondary experimental track: WOLF unencrypted/plain-data runtime
- RGSS XP/VX/VX Ace: detection + parsing only
- MV/MZ: detection + metadata parsing only
- RM95/Dante/Unite: research/detection boundaries

## Important Current Facts

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

Major unresolved area:

- verified chipset/passability semantics and fixtures
- full presentation/audio/menu/battle/save parity

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

## Next Automatic Development Priority

Unless a P0/P1 regression appears:

1. select the highest-priority READY RM2K/2003 card in `KANBAN.md`
2. prefer verified passability/rendering/event/runtime work that advances a real playable milestone
3. keep WOLF work independent and evidence-driven
4. do not jump to broad RGSS/MV/MZ/native execution while primary runtime cards are actionable
5. create future-engine cards only as dependencies become actionable; keep the board from becoming speculative noise

## Documentation Recovery Rule

At session start read:

1. `AGENTS.md`
2. `KANBAN.md`
3. this file
4. `docs/PROJECT_STATUS.md`
5. `docs/ARCHITECTURE.md`
6. relevant source/tests

Historical `SESSION_HANDOFF_*.md` and dated QA reports are snapshots, not current authority.

## Failure / Anti-Loop Rule

For the same normalized failure signature:

- at most 3 materially different repair strategies
- do not rerun the exact same failed command more than twice without new evidence/change
- after threshold: preserve useful work, document evidence, mark the card BLOCKED and continue with an independent READY card

Never delete/disable correct tests to make validation green.

## Documentation Audit Note — 2026-09-09

Living documentation was reviewed against current source because several files had drifted:

- Dante 98 existed in source but an old coverage audit called it absent
- RM95/RGSS/MV/MZ were incorrectly described in some docs as runtime bootstraps
- WOLF's experimental runtime slice was understated/contradictory
- scanner depth was documented as 2 while current default is 4
- old docs claimed no CI although a validation workflow exists
- Hermes instructions still described GDScript as canonical after the completed C# migration

The documentation refresh branch corrects these statements without changing runtime code.
