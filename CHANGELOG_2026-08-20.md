# UniversalRPG Stabilization Changes — 2026-08-20

This file describes the continuation pass performed on the supplied `URPG.zip`.

## Code fixes

- Fixed `VirtualClock` repeating callback cadence. Repeating events now reschedule by their requested interval instead of firing on every subsequent tick.
- Changed slow-motion semantics to a true speed factor (`0.5` means half speed), while fast-forward uses factors above `1.0`.
- Replaced fragile scheduled-callback array indices with stable event IDs.
- Switched timing/FPS sampling to a monotonic clock and fixed simulation-FPS baselining.
- Fixed compatibility-profile precedence: game-specific flags now actually override global defaults.
- Fixed compatibility-profile matching so an unknown/empty game hash no longer matches every hash-specific profile for an engine. Empty hash fields on a profile remain deliberate engine-wide wildcards.
- Repaired `RM2KDatabase`, which previously had invalid declarations and missing `from_dict()` implementations.
- Expanded RM2K database serialization and made ID lookups work for sparse/non-contiguous database IDs.
- Hardened `GameDetector` so known subdirectories reached through symlinks/junctions are not followed during detection.

## Tests

Added regression coverage for:

- normal/fast/slow/paused virtual-clock timing;
- one-shot and repeating callback scheduling;
- stable callback handles and reset behavior;
- compatibility override precedence;
- hash-specific compatibility-profile isolation;
- engine-wide profile behavior;
- RM2K database serialization and sparse IDs.

The core test suite now contains **91 `test_*` methods**. The supplied project's previous handoff reported **77/77 passing** before this pass.

This packaging environment did not include a Godot executable, therefore the new 91-test state is intentionally marked `VERIFY`. The first required command on a development machine or in CI is:

```bash
./scripts/validate.sh
```

## Autonomous development support

Added:

- `KANBAN.md` — active prioritized autonomous work queue;
- `AGENTS.md` — repository-wide agent execution, validation, recovery and anti-loop rules;
- `SESSION_STATE.md` — durable interruption/context-restart checkpoint;
- `HERMES_AUTONOMOUS_PROMPT.md` — ready-to-paste master prompt for Hermes;
- `scripts/validate.sh` — one-command Godot import/core/smoke validation;
- `.github/workflows/validate.yml` — GitHub Actions validation using Godot 4.7.2 stable.

The anti-loop protocol limits the same normalized failure to three materially different repair attempts. After that Hermes must preserve evidence, mark only that card blocked, checkpoint its state, and continue with another independent ready card rather than repeating the same action indefinitely.

## C# migration

The partial C# port was preserved. GDScript remains canonical until a dedicated migration card proves the C# implementation with a compatible Godot .NET editor, `dotnet build`, ported tests, and behavioral parity. Working GDScript files must not be deleted merely because a partial `.cs` equivalent exists.
