# UniversalRPG Agent Instructions

> **Last reviewed:** 2026-09-09

These rules apply to autonomous coding agents working in this repository.

## Source of Truth

At the start of a session read:

1. `AGENTS.md`
2. `KANBAN.md`
3. `SESSION_STATE.md`
4. `docs/PROJECT_STATUS.md`
5. `docs/ARCHITECTURE.md`
6. relevant source/tests
7. the relevant roadmap/engine documentation

Actual source and passing tests override stale prose. If documentation disagrees with source, correct the documentation rather than implementing toward an obsolete claim.

`KANBAN.md` is the single work queue. `SESSION_STATE.md` is the durable restart checkpoint.

## Canonical Implementation

- C#/.NET is canonical.
- Godot 4.7.2 stable .NET is the pinned host engine unless a deliberate upgrade card changes it.
- The Godot project root is `project/`.
- Do not restart or recreate a superseded GDScript implementation.
- Native/GDExtension code may be introduced later behind explicit interfaces when justified by measured requirements.

## Current Engine Boundaries

Do not overstate capabilities.

- RM2000/RM2003: partial parser-backed runtime and primary implementation track.
- WOLF: experimental unencrypted/plain-data runtime slice.
- XP/VX/VX Ace: detection + parsing only; no Ruby/RGSS execution.
- MV/MZ: detection + parsing/metadata only; no JavaScript execution.
- RM95: detection/research only.
- Dante 98: detection/research only.
- Unite: research/detection only.

A source file named `*Runtime.cs` is not enough to claim support. The compiled plugin's declared capabilities plus validated behavior define the supported boundary.

## Autonomous Work Loop

For each cycle:

1. select the highest-priority READY card with satisfied dependencies
2. move it to IN PROGRESS
3. record the card and immediate next action in `SESSION_STATE.md`
4. inspect existing implementation/tests before editing
5. make the smallest coherent implementation
6. add/update regression tests
7. run the narrowest relevant validation
8. run `./scripts/validate.sh` before DONE when tooling is available
9. update only documentation affected by verified behavior
10. mark DONE/VERIFY/BLOCKED accurately
11. continue with the next independent READY card

Do not stop merely to ask whether to continue.

## Kanban Maintenance

Hermes/agents may generate missing tasks from the project goals, but:

- preserve stable existing card IDs
- split oversized work into bounded cards
- create detailed cards mainly for the next 1–2 milestones
- keep distant engine tracks coarse until dependencies are actionable
- use dependencies and concrete acceptance criteria
- do not create a second competing Kanban

Priorities:

- P0: broken build/runtime/security regression
- P1: primary runtime correctness/foundation
- P2: major compatibility
- P3: enhancements/platform polish
- P4+: research/future

## Current Priority Order

Unless a regression changes the order:

1. keep build/tests/security green
2. advance RM2000/2003 toward an authorized end-to-end playable milestone
3. resolve verified passability/rendering/event/runtime gaps
4. separate RM2000 vs RM2003 semantics where required
5. improve WOLF native-format fidelity only where verified/authorized evidence exists
6. build RGSS core after the primary milestone is sufficiently stable
7. build shared MV/MZ JavaScript runtime after an explicit sandbox design exists
8. deepen RM95/Dante research only from verified formats
9. keep Unite as research
10. native DLL/Win32 execution remains late-stage

Do not jump to native execution or broad polish while critical primary runtime cards are available.

## Failure Recovery / Anti-Loop

Normalize a failure signature from:

- failing command/test
- primary error/message
- relevant file/function or crash location

For the same signature:

- maximum 3 materially different repair strategies
- never run the identical failing command more than twice consecutively without code/config/input changes or new evidence
- a different strategy requires a different hypothesis, implementation path, fixture, dependency/tooling path or scope

After each failed strategy record concise evidence in `SESSION_STATE.md`.

After three failed strategies:

1. stop the repeating approach
2. terminate hung processes if needed
3. preserve verified improvements
4. revert only clearly harmful speculative edits
5. mark the card BLOCKED with evidence and a concrete unblock condition
6. create a focused investigation card if useful
7. continue with the next independent READY card

Do not solve failures by deleting tests, weakening correct assertions, swallowing errors or disabling security checks.

## Definition of Done

A card is DONE only when:

- acceptance criteria are implemented
- relevant regression coverage exists
- relevant tests pass when tooling is available
- broader validation passes where required
- no known regression is hidden
- documentation does not claim more than the implementation provides

If required tooling is unavailable, use VERIFY and record the exact pending validation command.

## Security

Imported games are untrusted.

During detection/parsing never execute:

- EXE
- DLL/SO
- Ruby
- JavaScript
- shell/batch scripts
- native game plugins

Keep parser/archive work bounded. Do not follow reparse points. Do not bypass protected WOLF data.

Before future script/native execution, implement the explicit capability/sandbox policy in `docs/IMPORT_SECURITY.md`.

## Documentation Hygiene

Living documents must stay concise and current.

Do not copy every historical test count into every file. Use the latest verified baseline in `SESSION_STATE.md` / `PROJECT_STATUS.md`, and keep per-card evidence in `KANBAN.md`.

Dated handoffs and QA reports are historical snapshots and should not be rewritten as current status.
