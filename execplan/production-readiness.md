# Production readiness for UniversalRPG

This is a living execution plan. Progress, discoveries, decisions and outcomes must remain current. KANBAN.md remains the work queue; this document defines release acceptance, not a second backlog.

## Purpose / Big Picture

A player must be able to run a supported game without losing progress, overwriting original game files, or being told unsupported behavior succeeded. The current application is a bounded compatibility runtime, not a complete replacement for every RPG Maker generation. Production completion requires verified persistence, player-facing integration, explicit compatibility boundaries and a tested release artifact. Passing helper tests alone does not establish those properties.

## Progress

- [x] Folder-scan responsiveness: isolated worker, immediate localized activity/path/counts/elapsed feedback, cooperative cancellation, guarded owner-thread publication and preserved prior library. Scan8/8, UI20/20, integration11/11 and plugins25/25 pass; final canonical validator exited0 with2822/2822. Both audits resolved; selected-root junction compatibility and saved explicit choices are regression-tested. Real rendered collection probe: 11games,1010ms,28frames while scanning. No release export or complete GUI/engine claim.

- [x] (2026-10-09 13:28 UTC+8) Inspected repository instructions, checkpoint, status, architecture, roadmap and current save storage implementation on main 264af64.
- [x] Storage IO reviewed, validated and published as 985e366. Safety suite 13/13; three runs of eight independent processes / 256 writes passed. Final mutex-bearing canonical run passed 2787/2787, and both IO reviews are resolved. This does not complete durable game-state persistence.
- [ ] K-PROD-SAVE-STATE: preserve all authoritative durable state; reject unsupported or malformed saves before touching live state. Existing saves omit screen, timer, actors and party.
- [ ] K-PROD-SAVE-UI: managed per-game storage and usable save/load/Continue integration through the launcher; no writes into imported game directories by default.
- [ ] Verify game command execution and plugin-to-native state synchronization; stop reporting blocked scripts as Finished.
- [ ] Verify supported engine claims against actual complete gameplay paths, including battle, inventory, audio, event execution and save resumption.
- [x] (2026-10-09 14:03 UTC+8) K-PROD-RM2K-WAIT corrected: full representable timed waits, zero-frame compatibility, invalid-duration diagnostics; new suite 4/4 and existing interpreter suite 85/85. Canonical validator passed 2782/2782. This is one completed RM2000/2003 slice, not engine completion.
- [x] RM2003 decision-key Wait reviewed and published as c35d65a; title identity published as ce19f4c. Decision-host 6/6, timed waits 4/4, existing interpreter 85/85 and vehicle turns 8/8 pass. Canonical run proc_c9550df807a6 passed 2793/2793; bounded read-only review found no blocker for the Wait slice.
- [x] Main/parallel player input isolation verified with native integration 8/8, adjacent suites green, canonical validator proc_9bf97bab7452 exit 0 with 2801/2801, and read-only review deleg_3a9b690a finding no introduced blocker. The first three behavioral tests were observed red before their fixes. This is not full RM2000/2003 parity.
- [x] Input review coverage added for nested calls under both execution roles, simultaneous main/parallel decisions and message-release recovery. Expanded native suite 11/11; canonical log confirms 2804/2804 and final validator pass marker. No additional production change was needed.
- [x] Active-page activation corrected: highest eligible page owns trigger and layer, never a hidden lower page. Native suite 8/8 and adjacent suites pass; canonical proc_5542a98625a3 exited 0 with 2812/2812, and review deleg_c44d04b3 found no introduced blocker. First trigger/layer regressions were observed red before fixes. Running-page refresh/cancellation, autorun repetition and single-main scheduling remain separate gaps.
- [ ] Active event graphics: use the current eligible page's charset/index/layer on the repaint path, never the first graphic-bearing page; preserve movement/animation and prove pixels using real assets. Source-confirmed successor K-PROD-RM2K-ACTIVE-GRAPHIC is READY.
- [ ] K-PROD-RM2K-RUNTIME: complete RM2000/2003 gameplay paths, including faithful timing, battle/event execution, menu actions, original-game asset handling and restart/resume coverage.
- [ ] K-PROD-VX-RUNTIME: complete RPG Maker VX runtime execution and presentation, not detection/parsing only. VX is a separate acceptance target from VX Ace.
- [ ] K-PROD-MVMZ-RUNTIME: complete MV and MZ runtime and plugin integration within the authorized sandbox, with verified durable state and save/load UI.
- [ ] K-PROD-GUI: rework both the game-library launcher and in-game interface; verify keyboard/controller/mouse navigation, scaling, accessibility and game-visible windows on supported engines.
- [x] Localized title dispatch preserves command symbols. New regressions 2/2, existing title/game suite 12/12 and plugin-host suite 4/4 pass; completed full validators passed 2784/2784 and then 2787/2787 before the later mutex change.
- [ ] Run canonical validation and then verify a Windows release artifact; do not call the application production complete until all four explicit user criteria and remaining release gates are satisfied.

## Surprises & Discoveries

Current MzSaveStore.Unzip calls CopyTo without an output bound, and TryLoad calls File.ReadAllBytes without a file size bound. PathOf joins arbitrary names into a writable directory. Save uses fixed temporary and backup names, so concurrent writers can interfere, and its catch deletes the backup. Current tests cover only this writer reading itself and failure before committing, not malformed input, competing writes or real-engine interoperability.

The historical statement that saving works end to end is narrower than a production-ready save system: runtime saves omit four durable engine objects and are not wired into the player-facing menus.

## Decision Log

- Decision: fix data safety before enabling save UI. Rationale: exposing incomplete save behavior would encourage players to trust a format that loses durable state. Date/Author: 2026-10-09, Hermes Code.
- Decision: keep the existing public storage API and use .NET 8 standard-library primitives. Rationale: no new dependency is needed for bounded streams, strict UTF-8 or atomic file replacement.
- Decision: execute no imported game code during this investigation. Reference JavaScript is inspected only as text. Existing authorization remains limited to the running sandbox.

## Outcomes & Retrospective

Storage IO is complete for the bounded-snapshot scope and published as 985e366, with two read-only reviews, 13/13 safety cases, and three independent-process concurrency runs passing. Decision waits and title identity are published as c35d65a and ce19f4c. Main/parallel input isolation is published as 1985d0f, with expanded coverage 11/11. Highest-active-page activation is reviewed and published as 9ae27c1 with native suite 8/8 and canonical validator exit 0, 2812/2812. Active event graphics still select the wrong page and have a READY successor card; runtime lifecycle, full durable-state persistence, native-save interoperability, the four complete engine/GUI criteria and a tested distributable remain open. No release export was produced.

## Context and Orientation

The Godot .NET project lives in project/ and targets net8.0 with Godot.NET.Sdk 4.7.2. project/src/mz/MzSaveStore.cs writes and reads compressed JSON; project/src/mz/MzSaveContents.cs projects part of the engine state into a value tree; project/src/plugins/MzEngineRuntime.Save.cs applies that tree to a running game. MzJson is the existing internal JSON parser/writer and MzValue is its mutable tree. project/tests/core contains automatically discovered TestBase subclasses. scripts/validate.sh performs restore, rebuild, editor import and the complete Godot test runner.

## Plan of Work

First harden MzSaveStore independently of live game state. Create failure-first tests in project/tests/core/test_mz_save_store_safety.cs. Exercise traversal names, bad compressed data, invalid UTF-8, excessive file and expanded sizes, excessive tree depth or cycles, overwrite and concurrent writers. Use unique sibling temporary files, flush data before replacement, and clean up only files created by this operation. Preserve the prior destination on every rejected write.

Next repair state persistence from the actual runtime ownership and call flow, not assumptions about field names. Validate the complete snapshot and target map before applying it. A failed load must leave the running game unchanged. Confirm the reference engine's file encoding before claiming native save interoperability. Separate launcher-owned snapshots from original-engine saves if the complete serialization contract differs.

Then route the verified API through actual launcher save/load UI, assign a managed game-specific directory, and enable Continue only for loadable compatible slots. Finally run real fixtures and release-level tests, documenting unsupported engines and plugin features rather than presenting them as complete.

## Concrete Steps

From E:/URPG run:

    dotnet build project/UniversalRPG.csproj -t:Rebuild -v minimal
    tools/godot/editors/4.7.2/windows-x86_64/Godot_v4.7.2-stable_mono_win64_console.exe --headless --audio-driver Dummy --path E:/URPG/project res://tests/csharp_runner.tscn -- --suite=TestMzSaveStoreSafety
    bash scripts/validate.sh

The build must actually return exit 0. A filtered test must name its selected suite and report no failures; the full validator must end with UniversalRPG validation passed. Preserve complete logs in the Hermes profile scratch directory, not /tmp. Do not filter error text and infer build success from zero matches.

## Validation and Acceptance

A malformed, oversized, path-escaping or unsupported save produces a diagnostic, no loaded value, and no mutation of files or live state outside its authorized directory. A successful overwrite leaves a complete readable payload, and racing writers never produce a mixed or truncated file. Invalid writes preserve the last good save. New tests must fail against the old implementation and pass against the fix. All existing tests and scripts/validate.sh must pass before a card is DONE.

Full production acceptance additionally requires preservation of all player progress, restart/resume tests, actual menu navigation to save/load/Continue, original-save preservation, bounded untrusted input and resource cleanup, truthful capability reporting, and a tested distributable. Those gates are not waived by finishing the storage card.

## Idempotence and Recovery

Tests use unique directories under BH_AGENT scratch/TMPDIR and clean only their own files. Preserve qa_patches/. Do not reset entire files to HEAD to undo individual edits. Do not migrate or overwrite original game saves. Commit verified slices only when authorized by the standing commit/push request; verify remote refs after pushes. No release packaging is necessary until the code and release gates are ready.

## Artifacts and Notes

Baseline git inspection:

    main
    264af64 docs(mz): saving works end to end, and the next step is its wiring
    ?? qa_patches/

Historical counts in SESSION_STATE.md are not new evidence. Fresh commands and exact results will be recorded here.

## Interfaces and Dependencies

Keep MzSaveStore.Save, TryLoad, Zip, Unzip, PathOf, Exists, Remove and Slots callable with their existing signatures. Add public resource limits so regression tests can exercise exact boundaries. Use System.IO, System.IO.Compression, System.Text, System.Text.Json and bounded validation of MzValue. Do not add CLR/native access to Jint or execute imported JavaScript as part of parsing.
