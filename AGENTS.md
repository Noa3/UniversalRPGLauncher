# UniversalRPG Agent Instructions

> **Last reviewed:** 2026-09-11

These rules apply to autonomous coding agents working in this repository.

## Source of Truth

At the start of a session read:

1. `AGENTS.md`
2. `SESSION_STATE.md`
3. `docs/PROJECT_STATUS.md`
4. `docs/ARCHITECTURE.md`
5. relevant source/tests
6. the relevant roadmap/engine documentation

Actual source and passing tests override stale prose. If documentation disagrees with source, correct the documentation rather than implementing toward an obsolete claim.

There is intentionally no Kanban/work-board file. Do not create one unless the user explicitly asks for it later.

`SESSION_STATE.md` is only a concise restart checkpoint. `docs/PROJECT_STATUS.md` describes immediate priorities, while `docs/ROADMAP.md` describes long-term direction.

## Canonical Implementation

- C#/.NET is canonical.
- Godot 4.7.2 stable .NET is the pinned host engine unless a deliberate upgrade changes it.
- The Godot project root is `project/`.
- Do not restart or recreate the superseded GDScript implementation.
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

A source file named `*Runtime.cs` is not enough to claim support. Declared plugin capabilities plus validated behavior define the supported boundary.

## Autonomous Work Selection

Do not maintain a task board. Instead, select work directly from repository evidence.

At each work cycle:

1. repair any build/test/security regression first
2. read the current objective and next action from `SESSION_STATE.md`
3. compare that checkpoint with actual source/tests
4. inspect `docs/PROJECT_STATUS.md` immediate priorities
5. choose the smallest coherent implementation slice that advances the highest-value unresolved problem
6. if the previous next action is stale or no longer useful, replace it with a better one and explain why in `SESSION_STATE.md`

Do not spend a session only reorganizing planning files when useful code work is available.

## Autonomous Work Loop

For each cycle:

1. record the current objective and intended next change in `SESSION_STATE.md`
2. inspect existing implementation/tests before editing
3. make the smallest coherent implementation that improves the real runtime
4. add/update regression tests
5. run the narrowest relevant validation
6. self-repair failures when possible
7. run `./scripts/validate.sh` before declaring the slice complete when tooling is available
8. update only documentation affected by verified behavior
9. update `SESSION_STATE.md` with the new baseline, blockers and next action
10. continue with the next coherent slice without asking for permission

Only ask the user when progress requires a destructive/irreversible action, unavailable credential, legal/product decision with materially different outcomes, or external data that cannot reasonably be synthesized/replaced.

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

Do not jump to native execution or broad visual polish while core runtime work is still the limiting factor.

## Refactoring Policy

Larger refactors are allowed when they materially improve correctness, maintainability or engine separation.

Before a large refactor:

- identify the concrete problem in the existing design
- preserve working behavior with regression tests
- prefer replacing dead/duplicated abstractions rather than layering another abstraction over them
- keep runtime capabilities fail-closed
- remove dead experimental code when it creates false support expectations and has no active callers
- avoid rewriting functional engine subsystems merely to make the directory structure look cleaner

A refactor is successful only if the resulting architecture is easier to reason about and validation remains green.

## Runtime Capability Safety

Engine recognition, parsing and runtime execution are separate capabilities.

- runtime hosts must require `PluginCapability.Runtime`
- runtime creation must defensively reject plugins that do not advertise Runtime
- generic metadata/bootstrap lifecycles must not masquerade as playable engine runtimes
- detection-only plugins may remain visible in the library but cannot launch

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
5. record the blocker and exact unblock condition in `SESSION_STATE.md`
6. choose the next independent useful implementation area from source/status/roadmap

Do not solve failures by deleting tests, weakening correct assertions, swallowing errors or disabling security checks.

## Definition of Complete Work

A development slice is complete only when:

- implementation exists
- the intended behavior is covered by relevant regression tests
- relevant tests pass when tooling is available
- broader validation passes where required
- no known regression is hidden
- documentation does not claim more than the implementation provides

If required tooling is unavailable, record the exact pending validation command in `SESSION_STATE.md` rather than claiming success.

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

- `SESSION_STATE.md` is a checkpoint, not a history log.
- `PROJECT_STATUS.md` records current implementation and immediate priorities.
- `ROADMAP.md` records long-term direction, not granular tasks.
- dated handoffs and QA reports are historical snapshots and should not be rewritten as current status.

Do not duplicate every historical test count across files.
