# UniversalRPG Autonomous Session State

> Updated: 2026-08-21
> Purpose: small durable checkpoint for Hermes/other autonomous agents.

## Current card

`K-012` — Expand LDB decoding into typed core database sections. C# migration is complete under `K-003`.

## Last verified baseline

C# is now canonical. `dotnet build UniversalRPG.csproj` passes with zero errors, Godot 4.7.2 Mono instantiates `tests/CSharpRunner.cs`, and the headless C# suite passes `151/151` with exit code `0`.

## Validated stabilization changes

- `VirtualClock` repeating callback cadence fixed; stable event IDs introduced; slow-motion speed factor corrected; monotonic FPS sampling added.
- Compatibility game-specific flags now truly override global defaults.
- `RM2KDatabase` compile/serialization defects repaired and round-trip tests added.
- New VirtualClock regression suite and RM2KDatabase regression suite.
- `GameDetector` now refuses symlink/junction directory matches for Data/www/js/Scripts discovery.
- `scripts/validate.sh` and GitHub validation workflow added.
- Kanban/agent recovery protocol added.
- `RM2KDatabase` array comprehensions were replaced with valid GDScript serialization loops; all database collections now have focused serialization coverage.
- `VirtualClock` uses GDScript `float`/`maxi()` types compatible with Godot 4.7.2 warning-as-error parsing.
- `scripts/validate.sh` discovers the local Windows Godot 4.7.2 editor without `GODOT_BIN`.

## Completed K-002

- Normalized CP932/SJIS aliases to Godot's supported `SHIFT_JIS` decoder name; added `test_legacy_text_decoder.gd`.
- Replaced the VFS `"\\u0000"` source literal with byte-level NUL detection and retained security regression coverage.
- Updated current test counts and validation status in project documentation.

## Completed K-010

- Added provenance-pinned EasyRPG/TestGame RM2000 and RM2003 LDB/LMT/LMU fixtures with SHA-256 notes.
- Added real-fixture parser/framing tests for both databases and maps.
- Accepted valid zero-length LDB struct-array sections and retained unknown top-level chunks.

## Completed engine plugin foundation

- Added trusted in-process plugin contracts, deterministic registries, typed probe/lifecycle errors, and runtime host cleanup under `src/plugins/`.
- Added bounded read-only folder/ZIP inspection and built-in detection plugins for RM95, RM2K, RM2K3, XP, VX, VX Ace, MV, MZ, WOLF, and Unite research detection.
- Added the first functional parser-backed RM2K/RM2K3 runtime bootstrap: validated LDB/LMT/LMU loading, deterministic clock updates, and safe lifecycle start/stop without `RPG_RT.exe`.
- Added safe bounded lifecycle bootstrap backends for RM95, XP, VX, VX Ace, MV, MZ, and WOLF; Unite remains explicitly research-only.
- Rewired `GameDetector`, `GameLibrary`, `RuntimeLauncher`, and the Godot UI to preserve ranked detection reports, persist import metadata, and refuse unsafe/unsupported runtime selection without external fallback.
- Added contract, detection, archive, persistence, ambiguity, platform, and lifecycle regression coverage.
- Validation passed with Godot 4.7.2 Mono: `151/151` tests.

## Current action

Plugin foundation and application wiring are complete. Continue K-012 typed LDB/database work in C# and keep parser regression coverage in `tests/core/`.

## Next action

Run full `./scripts/validate.sh` where Godot and .NET are available, then continue K-012. Commit/push only after final validation review.

## Failure log

- 2026-08-21 | C# migration | Signature: Godot Mono headless -> `Cannot instantiate C# script because the associated class could not be found. Script: 'res://tests/csharp_runner.cs'`. Hypothesis 1: stale incremental build skipped source generators. Evidence: forced `-t:Rebuild -p:EmitCompilerGeneratedFiles=true` ran ScriptMethods/Properties/Signals generators for all classes, but `ScriptPathAttributeGenerator` produced no output and `UniversalRPG.dll` contains zero `[ScriptPath]` attributes (only 5 unrelated `res://` strings). GodotSharp 4.7.2 defines `ScriptPathAttribute`; SDK targets disable nothing; generator class exists in the package. Attempt 1 (rebuild) did not resolve. Next attempt: manual `[ScriptPathAttribute]` annotation on scene-referenced classes; if that fails, decompile the generator for its emission condition.
- 2026-08-21 | C# migration | Manual `[ScriptPathAttribute]` annotations did not register scene scripts. Root cause: `ScriptPathAttributeGenerator` requires case-sensitive file/class name equality and emits `AssemblyHasScriptsAttribute`; `main.cs`/`Main` and `csharp_runner.cs`/`CSharpRunner` were skipped. Renamed files to `Main.cs` and `CSharpRunner.cs`, updated scenes, rebuilt, and verified generated script-path registry.
- 2026-08-21 | C# migration | C# runner initially failed 5 database assertions because `List<int>` and typed dictionary lists do not implement `IEnumerable<object>`, and test cast `List<Dictionary<...>>` to `List<object>`. Changed deserialization to non-generic `IEnumerable`; test now uses `ICollection`. Result: `128/128` passed, exit `0`.
- 2026-08-20 | K-001 | Signature: `./scripts/validate.sh` -> exit 127, `Godot 4.7.2 was not found`. Hypothesis: the wrapper only knows POSIX/editor-PATH locations while this Windows checkout has a local Godot binary. Evidence: `E:/URPG/Godot_v4.7.2/Godot_v4.7.2-stable_mono_win64_console.exe` exists and reports `4.7.2.stable.mono.official.ed1daf0bf`. Changed prerequisite: supplied `GODOT_BIN`; result: validation reached import/tests and exposed source failures. Next attempt will repair the source signatures, not retry discovery unchanged.
- 2026-08-20 | K-001 | Signature: Godot test runner -> `Parse Error: Expected closing "]" after array elements` at `src/rm2k/database/rm2k_database.gd:341`, preventing `RM2KDatabase` and `tests/core/test_rm2k_database.gd` from loading. Hypothesis: Python-style array comprehensions are not valid GDScript 4.7.2. Evidence: direct Godot load reports the exact parser location. Attempt 1: source inspection/direct load; confirmed. Next attempt will replace only the invalid serialization syntax and add focused coverage.
- 2026-08-20 | K-001 | Signature: Godot test runner -> `Could not find type "double"` at `src/core/virtual_clock.gd:54,232`, followed by Variant-inference warnings treated as errors at lines 150 and 158. Hypothesis: the stabilization patch used a non-GDScript type and generic `max()` where typed `float`/`maxi()` are required. Evidence: direct Godot load reproduces all locations. Attempt 1: source inspection/direct load; confirmed. Repair: changed the time values to `float`, made `now`/`elapsed` explicit floats, and replaced `max()` with `maxi()`. Result: targeted core suite and full validation passed.
- 2026-08-20 | K-001 | Signature: direct `godot --headless --path . --script res://src/rm2k/database/rm2k_database.gd` timed out after 120s with no further output. Cause: a pure `RefCounted` class script does not own a `SceneTree` exit path when invoked as the main script. Action: terminated by timeout and did not repeat unchanged; validation uses `tests/runner.gd`, which exits normally. Result: no source failure indicated; core suite passed.
- 2026-08-20 | K-002 | Signature: successful smoke run emitted `ERROR: Conversion failed: Unknown encoding` from `legacy_text_decoder.gd:25` on Windows for CP932 metadata. Repair: normalized CP932/SJIS aliases to the supported `SHIFT_JIS` name and added three decoder tests. Result: `95/95` core tests and the full validation pass without the diagnostic.
- 2026-08-20 | K-002 | Signature: successful VFS suite emitted six `Unexpected NUL character` parser diagnostics from the `"\\u0000"` literal in the VFS security check and its test. Repair: changed production code to byte-level NUL detection and tested the helper with `PackedByteArray` values, avoiding an engine warning while preserving the security assertion. Result: full validation pass has no NUL diagnostics.
- 2026-08-20 | K-010 | Signature: real RM2003 LDB parse rejected `class_duplicate` at offset `0x60D85` with EOF on an empty payload. Hypothesis: the valid fixture uses zero-length encoding for an empty struct array instead of BER count zero. Evidence: independent raw framing showed chunk `0x1f` length `0` and the next chunk begins exactly at `0x60D85`. Repair: accept empty struct-array payloads as count zero; keep non-empty BER/truncation checks unchanged. Result: both real LDBs/LMUs and `102/102` core tests pass.

## Recovery rule

If validation fails, keep the failure signature here. Use at most three materially different attempts for the same signature; after that mark the corresponding Kanban card blocked and continue with an independent ready card.
