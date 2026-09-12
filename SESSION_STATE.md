# UniversalRPG session checkpoint

Updated: 2026-09-12. Branch: `docs/refresh-2026-09-09`, PR #1.
No Kanban is maintained. Preserve unrelated work and choose coherent changes from source/tests.

## Latest work: actual LMU event-page import

Parent implementation: `7f5b805b792ff4f16a7f4cbe3f71127b1311fd7e`.

While preparing automatic movement integration, inspection found a more fundamental blocker: `ParseMap` counted event pages with `ParseStructArray(..., false)` and consequently iterated an empty object list. Actual LMU pages never reached the existing runtime event loader. The previously unreachable condition path also tried to access a nonexistent `fields` member on a raw chunk.

This pass:

- Materializes real pages through `Rm2kParser.EventPages.cs` and retains page IDs/order, commands, conditions, movement metadata and graphics metadata.
- Decodes nested condition bytes through bounded structure readers; reports duplicate/malformed/trailing data instead of dropping a page.
- Uses documented field IDs/defaults instead of guessed legacy aliases. Unknown page fields remain raw data.
- Decodes the embedded movement-route structure. Its 0x0B SizeField is a serialized-byte hint, not an instruction count. Stale hints are advisory; actual payload and decoded command limits remain enforced.
- Reads event operands and condition integer values as signed int32. Explicit empty condition integer payloads represent zero.
- Fixes the event-command cap boundary so exactly MaxCommands plus its terminator is accepted, while an extra command is rejected.
- Adds 19 C# LMU/event regression methods, primarily entering through generated file bytes and the actual ParseMap entry point. The route decoder suite now has 11 methods instead of 7, including parameterized structures and size-hint bounds.

## Validation truth

Performed in this pass: source/format review, exact Git-blob verification of the materialized original large parser, inspection of the single intended ParseMap diff hunk, and lexical delimiter checks on edited C# files.

**Not performed:** C# compilation or execution, the new LMU/route tests, actual Jint/Godot execution, exports, or real-game playthroughs. .NET/Godot are absent and toolchain download attempts failed. Do not describe the branch as green. Earlier Node and simulated-tool test results belong to their earlier dated reports, not this parser pass.

Evidence, primary format references and pending commands: `docs/VALIDATION_LMU_EVENTS_2026-09-12.md`.

## Preserved script work

MV/MZ and RGSS loaders retain the previous fail-stop startup/hook rules, requiring fresh sessions after external failures. `WebPluginLoadPlan`, PluginManager parameters/scheduled names, the Jint invocation boundary, VFS and validation-driver guards are unchanged by this parser pass.

## Remaining runtime boundary

- RM2000/2003: partial runtime. The parser now delivers real event-page data, but this pass does NOT implement automatic route scheduling, faithful speed/frequency timing, or full runtime sprite synchronization.
- Runtime sprite refresh still needs to consume current event positions/facing and active pages, not only original map coordinates.
- XP/VX/VX Ace: script archive/inventory/pipeline; no embedded Ruby backend.
- MV/MZ: experimental Jint adapter/shims; no complete browser/render/audio host or playable engine registration.
- WOLF: experimental understood plain-data subset. RM95/Dante98/Unite remain research/detection.

## Next useful work

1. Run `./scripts/validate.sh` with the pinned .NET/Godot toolchain and repair measured failures. No merge before fresh complete validation.
2. Validate real LMU -> runtime page selection -> action/parallel event execution with a small authorized/synthetic project; existing hand-built runtime fixtures alone cannot prove the import path.
3. Connect parsed movement routes to active-page lifecycle and explicit timing; publish current positions/facing to render descriptors without mutating parsed maps.
4. Add a two-map traversal/save regression before claiming a playable RM2K milestone.
5. Continue browser/RGSS services only behind truthful capabilities and verified VM behavior.

On recovery read AGENTS, this checkpoint and relevant source/tests. Never weaken correct tests, replay partial script initialization, recreate a task board or promote compatibility from class names alone.
