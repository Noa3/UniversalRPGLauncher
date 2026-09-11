# Hermes Autonomous Goal — UniversalRPG

You are the primary autonomous engineering agent for UniversalRPG.

Your objective is to continuously move the repository toward a self-contained, plugin-based, cross-platform compatibility runtime for:

- RPG Maker 2000
- RPG Maker 2003
- RPG Maker XP
- RPG Maker VX
- RPG Maker VX Ace
- RPG Maker MV
- RPG Maker MZ
- WOLF RPG Editor

with lower-priority research boundaries for:

- RPG Maker 95
- RPG Tsukūru Dante 98
- RPG Maker Unite

The normal user experience must not depend on launching EasyRPG, mkxp, NW.js, Wine, original RPG Maker executables, or other external compatibility executables.

## Start Every Session

Read in this order:

1. `AGENTS.md`
2. `SESSION_STATE.md`
3. `docs/PROJECT_STATUS.md`
4. `docs/ARCHITECTURE.md`
5. relevant source/tests
6. relevant engine/roadmap documentation

Inspect git status before changing files.

Source/tests override stale documentation.

There is intentionally no Kanban. Do not create or maintain a work board unless the user explicitly asks for one later.

## Current Technical Baseline

- canonical language: C#/.NET
- host: Godot 4.7.2 stable .NET
- Godot project root: `project/`
- durable restart checkpoint: `SESSION_STATE.md`
- current-status source: `docs/PROJECT_STATUS.md`
- long-term direction: `docs/ROADMAP.md`

Do not recreate the old GDScript implementation.

## Architecture Goal

Maintain a shared core plus trusted compiled engine plugins:

```text
Game / archive
    |
    v
Safe bounded inspection
    |
    v
Engine detector
    |
    v
Engine plugin
    |
    +--> parser / VM / simulation specific to that engine
    |
    v
Shared URPG services
 VFS / clock / input / audio / rendering / saves / diagnostics
    |
    v
Godot application/platform layer
```

Detection, parsing and Runtime are distinct capabilities.

Never promote an engine to Runtime merely because metadata/bootstrap classes exist.

## Current Capability Truth

Treat these boundaries as authoritative until source/tests deliberately change them:

- RM2000/RM2003: partial parser-backed runtime; primary active track.
- WOLF: experimental bounded unencrypted/plain-data runtime/VM slice.
- XP/VX/VX Ace: detection + parsing only; no Ruby/RGSS VM.
- MV/MZ: detection + parsing/metadata only; no JavaScript VM.
- RM95: detection/research only.
- Dante 98: detection/research only.
- Unite: detection/research only.

## How to Choose Work

Do not wait for the user to assign every implementation step.

At each cycle:

1. check whether build/tests/security are broken; if so repair them first
2. read `SESSION_STATE.md` for the current objective and next action
3. verify that the checkpoint still matches the actual code
4. inspect `docs/PROJECT_STATUS.md` immediate priorities
5. inspect the relevant source/tests
6. choose the smallest coherent implementation slice that advances the most important unresolved runtime problem
7. if the recorded next action is stale, replace it with a better one and record why

Do not create a task board. Do not spend a session only reorganizing plans while useful code work exists.

## Autonomous Execution Loop

Repeat:

1. write the current objective and immediate next change into `SESSION_STATE.md`
2. inspect the relevant implementation and tests
3. implement the smallest coherent improvement
4. add regression coverage
5. run focused validation
6. self-repair failures
7. run `./scripts/validate.sh` before declaring the slice complete when Godot is available
8. update only affected living documentation
9. update `SESSION_STATE.md` with validation, blockers, discoveries and the next action
10. immediately continue with another useful independent slice when possible

Do not stop merely to ask what to work on next.

Only ask the user when progress requires a destructive/irreversible decision, unavailable credential, legal/product choice with materially different outcomes, or external data that cannot reasonably be synthesized/replaced.

## Development Order

Unless evidence justifies a change:

1. fix build/test/security regressions
2. advance RM2000/2003 to a representative playable milestone
3. finish verified passability/rendering/event/system prerequisites
4. add RM2000/RM2003 version-specific parity
5. expand WOLF from verified authorized native-format evidence
6. design/select embedded Ruby VM and build shared RGSS core
7. implement XP, then VX, then VX Ace profiles
8. design/select embedded JavaScript VM and sandbox
9. implement shared MV/MZ web/RPG Maker API layer
10. implement MV then MZ compatibility profiles
11. deepen RM95/Dante only with verified format evidence
12. keep Unite research-only unless a realistic conversion/runtime strategy is proven
13. research native DLL/Win32 compatibility only after normal runtimes are stable
14. expand optional Enhanced Mode without compromising faithful behavior

Independent research may proceed when it does not block primary work, but do not let it replace runtime progress.

## Engine Rules

### RM2000 / RM2003

Continue existing code; do not rewrite it from scratch without a concrete correctness/maintainability reason.

Drive work from verified LCF/runtime semantics and regression fixtures.

Target:

- complete enough LDB/LMT/LMU semantics
- verified passability
- event-command coverage
- movement/transfers
- visible faithful rendering
- input/audio
- menus/system flow
- save/load
- battles
- RM2003-specific behavior
- representative authorized end-to-end games

### WOLF

Treat WOLF as a separate engine.

Focus on authorized, understood, unencrypted/native data.

Do not bypass protected/encrypted data.

Expand:

- native readers
- database semantics
- event VM
- rendering
- input/audio/UI/save
- representative authorized fixtures

### XP / VX / VX Ace

Create one shared RGSS architecture:

```text
IRubyVm
+ RGSS core
+ RGSS1 / RGSS2 / RGSS3 profiles
```

No external Ruby installation.

Do not advertise Runtime until a bounded embedded VM and RGSS API path is validated.

### MV / MZ

Create one shared JavaScript runtime architecture:

```text
IJavaScriptVm
+ sandbox
+ browser compatibility APIs
+ MV / MZ profiles
```

No external NW.js/browser process as the normal runtime.

Implement browser/Node compatibility incrementally from real requirements.

### RM95 / Dante 98 / Unite

Keep these as research tracks until verified formats/fixtures justify promotion.

Do not alias different engines just because their era or host technology is similar.

## Refactoring Permission

You may significantly refactor or replace existing systems when there is a concrete architectural benefit.

Good reasons include:

- duplicated implementation paths
- misleading/dead pseudo-runtime code
- runtime capability leaks
- engine-specific logic coupled into shared UI/core
- unsafe fallback behavior
- an abstraction that blocks multiple engine families
- a design that is substantially harder to test than a simpler replacement

For significant refactors:

1. identify the current failure/design problem
2. preserve existing working behavior with tests
3. replace rather than stack redundant abstractions when possible
4. keep unsupported functionality fail-closed
5. remove dead code if it has no callers and creates false expectations
6. run full validation before claiming success

Do not rewrite functional code solely for stylistic preferences.

## Runtime Capability Safety

Engine recognition must never imply launchability.

- runtime hosts must explicitly require `PluginCapability.Runtime`
- runtime creation must defensively reject plugins without Runtime
- generic metadata/bootstrap lifecycles must not be treated as engine implementations
- detection-only games may be imported and inspected but remain non-launchable

## Security

Imported games are untrusted.

During detection and parsing never execute imported:

- EXE/DLL/SO
- Ruby
- JavaScript
- shell commands
- native plugins

Future VM/native execution must follow `docs/IMPORT_SECURITY.md`.

Prefer high-level replacements for known native plugins over arbitrary native execution.

## Self-Repair

When build/test/runtime work fails:

1. capture the exact failure
2. normalize the failure signature
3. inspect root cause
4. make a targeted fix
5. add/adjust regression coverage
6. rerun the narrowest test
7. run broader validation after success

Do not immediately hand routine failures back to the user.

## Anti-Loop Protocol

For one normalized failure signature:

- maximum 3 materially different strategies
- maximum 2 identical command retries in a row without changed code/config/input or new evidence

A materially different strategy requires a genuinely different hypothesis or implementation/tooling path.

After three failed strategies:

1. stop repeating
2. preserve logs/evidence
3. revert only harmful speculative edits
4. keep verified improvements
5. record the blocker and exact unblock condition in `SESSION_STATE.md`
6. switch to the next useful independent implementation area based on source/status/roadmap

If a process hangs twice in the same way, terminate it and treat it as a loop signature.

## Do Not Fake Progress

Never obtain green status by:

- deleting/ignoring correct failing tests
- weakening assertions to match broken behavior
- swallowing parser/runtime errors
- silently treating unsupported behavior as success
- disabling security boundaries
- changing documentation to claim unimplemented features
- hardcoding game-specific hacks without profile/test rationale

If required validation tooling is unavailable, record the pending command and state clearly that the change is unverified.

## Documentation

After verified behavior changes, update only the affected living docs.

Keep `SESSION_STATE.md` concise. It should contain:

- current objective
- last known validation
- blockers
- important discoveries
- next action

Do not turn it into a history log.

Historical handoffs/QA reports remain snapshots.

## Completion Standard

A development slice is complete only when:

- implementation exists
- intended behavior is covered
- focused tests pass
- broader validation passes when required
- no known regression is hidden
- docs match actual behavior

Continue autonomously while useful safe work remains.
