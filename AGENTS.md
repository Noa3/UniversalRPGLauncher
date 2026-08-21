# UniversalRPG Agent Instructions

These instructions apply to autonomous coding agents working in this repository.

## Source of truth

Read these files at the start of every session, in this order:

1. `AGENTS.md`
2. `KANBAN.md`
3. `SESSION_STATE.md`
4. `docs/PROJECT_STATUS.md`
5. `docs/ARCHITECTURE.md`
6. `docs/ROADMAP.md`
7. relevant code/tests for the selected card

`KANBAN.md` is the work queue. `SESSION_STATE.md` is the crash/restart checkpoint.
Do not invent a parallel private roadmap and do not spend a session only rewriting plans when a ready implementation card exists.

## Current implementation policy

- GDScript is the **validated/canonical implementation** until a complete C# migration passes both `dotnet build` and the Godot test suite under a .NET editor.
- Existing `.cs` files are an experimental port. Do not delete working `.gd` equivalents just because a C# file exists.
- Do not continue a broad language migration while it blocks RM2000/2003 runtime progress. Port only behind an explicit Kanban card and acceptance tests.
- Godot 4.7.2 stable is the pinned engine line for this repository unless a deliberate upgrade card changes it.
- Imported games are untrusted input. Never execute game EXEs, DLLs, Ruby, JavaScript, shell commands, or native plugins during detection/parsing tests.

## Autonomous work loop

For each cycle:

1. Select the highest-priority `READY` card whose dependencies are satisfied.
2. Move it to `IN PROGRESS` and record it in `SESSION_STATE.md`.
3. Inspect existing implementation before editing.
4. Make the smallest coherent implementation that satisfies the acceptance criteria.
5. Add or update regression tests.
6. Run the narrowest relevant tests, then `./scripts/validate.sh` before declaring the card done when Godot is available.
7. If validation passes, move the card to `DONE`, update docs/status only where behavior actually changed, checkpoint `SESSION_STATE.md`, and select the next card.
8. Continue without asking for permission unless a destructive action, missing credential, legal decision, or genuinely ambiguous product choice makes progress unsafe.

## Failure recovery and anti-loop rules

A failure is identified by its normalized signature: failing command/test + primary error type/message + relevant file/function.

For one signature:

- Attempt at most **3 materially different fixes** before declaring the card blocked.
- Never run the identical failing command more than **2 times in a row** without changing code/config/input or gathering new evidence.
- A materially different attempt must change the hypothesis, implementation strategy, fixture, dependency/tooling path, or scope. Cosmetic edits do not count.
- After each failed attempt, write a short entry under `SESSION_STATE.md -> Failure log` with hypothesis, change, and result.
- If the same signature appears after 3 different attempts, stop modifying that subsystem. Revert only the speculative changes that made the state worse, keep verified improvements, mark the card `BLOCKED`, record exact evidence and a concrete unblock condition, then continue with the next independent `READY` card.
- If a command hangs or makes no useful progress, terminate it, record the command and last output, and do not immediately rerun it unchanged.
- If an agent notices it is repeating the same reasoning/action sequence, treat that as a loop even without an explicit error. Checkpoint state, mark the current approach exhausted, choose a different approach or another card.
- Do not solve a local build failure by deleting tests, weakening assertions, swallowing exceptions, disabling validation, or silently changing compatibility requirements.

## Context/restart recovery

Update `SESSION_STATE.md` after every completed card and before risky/refactor-heavy work.
If the agent/process restarts, do not begin from memory. Read the checkpoint and Kanban, verify the working tree, rerun the last relevant validation if possible, and continue from the recorded next action.

## Definition of done

A card may be `DONE` only when:

- acceptance criteria are implemented,
- relevant regression tests exist,
- relevant tests pass when tooling is available,
- no known regression was hidden or ignored,
- documentation/status is not claiming functionality beyond what exists.

If tooling required for validation is unavailable, use `VERIFY` rather than `DONE` and state the exact command that still must run.

## Scope control

Priority order remains:

1. RM2000/2003 faithful parsing/runtime correctness
2. deterministic core/runtime infrastructure
3. renderer/event interpreter
4. broader compatibility and RTP handling
5. enhancements
6. RGSS
7. MV/MZ
8. native DLL/Win32 compatibility

Do not jump to DLL emulation, Ruby VM, JavaScript VM, AI translation, HD rendering, or broad UI polish while higher-priority runtime cards are ready.
