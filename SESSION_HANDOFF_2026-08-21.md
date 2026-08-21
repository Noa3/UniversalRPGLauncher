# Session Handoff — 2026-08-21

## Objective
- Continue **UniversalRPG** (`/home/noa3/Schreibtisch/URPG`, Godot 4.7.2 Mono, RPG-Maker compatibility runtime): complete the GDScript → pure C#/.NET migration, get the headless Mono-editor C# test suite running, then delete all superseded `.gd` scripts and commit/push.
- **Explicit user directive (this session):** "ich möchte kein godotscript sondern pure .net bitte replace die skripte wenn es möglich ist" — replacing `.gd` with `.cs` is sanctioned once the C# path works. This overrides the earlier "keep `.gd` canonical" policy.
- User communicates in German; assistant replies in German.

## Important Details
- Git: branch `main`, synced with origin/main through commit `c66a702` ("Initialize Godot .NET validation workflow"). This session's work is committed on top (see Work State).
- .NET SDK 8.0.412 lives at `/tmp/opencode/dotnet8` (**ephemeral** — reinstall if `/tmp` was cleared). Every `dotnet build` AND every Godot Mono-editor run needs:
  ```bash
  export DOTNET_ROOT=/tmp/opencode/dotnet8
  export PATH=/tmp/opencode/dotnet8:$PATH
  export DOTNET_CLI_TELEMETRY_OPTOUT=1
  ```
  Without `DOTNET_ROOT` the Mono editor dies with "Failed to load hostfxr".
- Mono editor binary: `tools/godot/editors/4.7.2/linux-x86_64/Godot_v4.7.2-stable_mono_linux_x86_64/Godot_v4.7.2-stable_mono_linux.x86_64`.
  - Import: `--headless --path . --import` (works; crashes cosmetically at exit: "Parameter singleton is null" — ignore).
  - Test run: `--headless --path . res://tests/csharp_runner.tscn` (currently blocked, see below).
- C# conventions established: PascalCase members; test discovery via reflection over methods prefixed `Test_` (`TestBase`); data models plain C# classes, services `partial … : RefCounted`; namespaces `UniversalRPG.Core/.GameDetectorNs/.Compatibility/.Rm2k*/.App.* /.Tests.Framework/.Tests.Core`.
- Binding substitutions settled (C# vs GDScript):
  - `String.Format("{key}", dict)` does NOT work → use `.Replace("{key}", value)` chains everywhere (`main.cs`, `game_detector.cs` done).
  - ItemList delegates pass `long` index → `SelectGame(long pIndex)`, selection resolved via `_library.Games[(int)pIndex]` (plain C# class cannot be stored via `SetItemMetadata` — Variant only).
  - All Godot-derived classes need the `partial` modifier (GD0001), including test classes and `TestBase`.
- Verified en.po catalog values used by detector tests: `CONFIDENCE_HIGH/MEDIUM/LOW` = "High"/"Medium"/"Low"; `DETECT_LCF_DATABASE` = "Found RPG_RT.ldb and RPG_RT.lmt"; `DETECT_MV_RUNTIME` contains "JavaScript".
- Real-fixture framing expectations encoded in tests: rm2000 `RPG_RT.ldb` = 16 chunks / 210227 bytes / no terminator; rm2003 `RPG_RT.ldb` = 22 / 416513 / none; rm2000 `Map0001.lmu` = 6 / 8544 / terminator; rm2003 `Map0001.lmu` = 11 / 8488 / terminator.
- LMT fixture expectations: rm2000 map_count 81, maps[0] id −1/"MAP-0001", maps[1] id 0/"RPG Maker 2000 Test suite", tree_order 81, active_node 50, start party_map_id 30/x 37/y 72; rm2003 map_count 22, maps[0] id 0/"RPG Maker 2003 Test suite", maps[2].parent_id 1, active_node 20, start party_map_id 1/x 4/y 8.

## Work State
### Completed
- Read all 9 GDScript test suites fully (porting references).
- `src/core/legacy_text_decoder.cs`: `JapaneseEncodings` → `public static readonly string[] { "SHIFT_JIS" }`; added `NormalizeEncoding()` mapping CP932/SJIS/SHIFTJIS/SHIFT_JIS → SHIFT_JIS (parity with `.gd`).
- `src/core/virtual_filesystem.cs`: added `public static bool ContainsNullByte(byte[])`.
- Wrote all 9 C# test suites in `tests/core/`: `test_virtual_filesystem.cs`, `test_virtual_clock.cs`, `test_legacy_text_decoder.cs`, `test_game_detector.cs` (uses `using static …GameDetector;`), `test_compatibility_profile.cs`, `test_rm2k_parser.cs` (internal static BER/Chunk/Lcf helpers), `test_rm2k_lmt_parser.cs` (signed BER builder, synthetic LMT fixtures incl. cycle/malicious-count/invalid-parent rejection), `test_rm2k_database.cs`, `test_rm2k_real_fixtures.cs` (framing assertions incl. exact byte sizes). Framework: `tests/framework/test_base.cs`; runner: `tests/csharp_runner.cs` + `tests/csharp_runner.tscn`.
- Build-error fixes applied: `runtime_launcher.cs` (+`using UniversalRPG.App.Library;`, `GameDetector.EngineType` qualification), `game_library.cs` (`GameDetector.DetectionResult`, `GameDetector.EngineType.Unknown`), `partial` added everywhere (GD0001), `csharp_runner.cs` (`TestBase.SuiteResult`, enum qualification, `byte[]` Append → `List<byte>.AddRange` in SmokeLcfDetection/SmokeLcfParser), `main.cs` (`SelectGame(long)`, metadata-free selection, `.Format` → `.Replace` chains), `game_detector.cs` (same `.Replace` fix).
- `dotnet build UniversalRPG.csproj` is GREEN: "Der Buildvorgang wurde erfolgreich ausgeführt. 0 Fehler".
- Diagnosed headless-run failure "Cannot instantiate C# script because the associated class could not be found. Script: 'res://tests/csharp_runner.cs'":
  - Initial builds silently skipped source generators (stale incremental Up2Date marker).
  - Forced `-t:Rebuild -p:EmitCompilerGeneratedFiles=true` → generators DID run: `GodotPlugins.Game`, ScriptMethods/ScriptProperties/ScriptPropertyDefVal/ScriptSerialization/ScriptSignals outputs exist for ALL classes under `.godot/mono/temp/obj/Debug/generated/Godot.SourceGenerators/`.
  - BUT `Godot.SourceGenerators.ScriptPathAttributeGenerator` produces NO output (no generated dir, zero `[ScriptPath]` attributes in `UniversalRPG.dll`; `strings -el` shows only 5 unrelated `res://` literals).
  - Verified: `GodotSharp.dll` 4.7.2 DOES define `ScriptPathAttribute` (+ `TryGetScriptPath`); `Godot.NET.Sdk` targets disable no generator; the generator class exists inside `Godot.SourceGenerators.dll`. Root cause unknown.

### Blocked
- Headless C# test run: scene loads but `can_instantiate` fails because the assembly carries no `ScriptPathAttribute`s, so `ScriptManagerBridge.LookupScriptsInAssembly` finds nothing. Two evidence-gathering attempts spent; next attempt must change strategy (see Next Move).

## Next Move
1. **Workaround first (materially different attempt #3):** manually annotate the two scene-referenced classes with the attribute the generator should emit:
   - `app/ui/main.cs`: `[ScriptPathAttribute("res://app/ui/main.cs")]` above `partial class Main` (confirm exact ext_resource path in `scenes/main.tscn` first).
   - `tests/csharp_runner.cs`: `[ScriptPathAttribute("res://tests/csharp_runner.cs")]`.
   Then `dotnet build` + rerun `res://tests/csharp_runner.tscn`. If instantiation works, decide whether to annotate remaining Godot-derived classes or keep minimal.
2. If manual annotation fails too: decompile the generator (`dotnet tool install -g ilspycmd`, target `/home/noa3/.nuget/packages/godot.sourcegenerators/4.7.2/analyzers/dotnet/cs/Godot.SourceGenerators.dll`) to find its emission condition (suspects: project-dir/path heuristics, an MSBuild property gate, or editor-only gating). Also try re-running `--headless --path . --import` AFTER a successful attribute-bearing build.
3. Iterate until the runner prints "All N tests passed" and exits 0; fix surfaced test failures.
4. After green: delete superseded `.gd` files (all `src/**/*.gd`, `app/**/*.gd`, `tests/core/*.gd`, `tests/framework/test_base.gd`, `tests/runner.gd`, `tests/smoke_runner.gd`), remove leftover references (project.godot autoloads/scenes, docs), verify build+run again.
5. Update KANBAN/docs (migration ran on explicit user directive; consider formalizing as a Kanban card), commit, push.

## Relevant Files
- `SESSION_HANDOFF_2026-08-20.md`: previous handoff (original work plan steps 1–7).
- `UniversalRPG.csproj` (`Godot.NET.Sdk/4.7.2`, net8.0), `project.godot`, `scenes/main.tscn` (points to `app/ui/main.cs`): build/config entry points.
- Modified this session: `src/core/legacy_text_decoder.cs`, `src/core/virtual_filesystem.cs`, `src/game_detector/game_detector.cs`, `app/library/game_library.cs`, `app/launcher/runtime_launcher.cs`, `app/ui/main.cs`, `tests/csharp_runner.cs`.
- New test suites: `tests/core/test_{virtual_filesystem,virtual_clock,legacy_text_decoder,game_detector,compatibility_profile,rm2k_parser,rm2k_lmt_parser,rm2k_database,rm2k_real_fixtures}.cs`; framework `tests/framework/test_base.cs`; runner `tests/csharp_runner.cs` + `tests/csharp_runner.tscn`.
- Pre-existing C# sources: `src/core/virtual_clock.cs`, `src/compatibility/compatibility_profile.cs`, `src/rm2k/rm2k_map.cs`, `src/rm2k/database/rm2k_database.cs`, `src/rm2k/parser/{lcf_binary_reader,rm2k_parser}.cs`.
- GDScript originals pending deletion after green: `src/**/**.gd`, `app/**/**.gd`, `tests/core/*.gd`, `tests/framework/test_base.gd`, `tests/runner.gd`, `tests/smoke_runner.gd`.
- Fixtures: `tests/fixtures/easyrpg-testgame/{rm2000,rm2003}/RPG_RT.ldb|lmt, Map0001.lmu`.
- Generator outputs (evidence): `.godot/mono/temp/obj/Debug/generated/Godot.SourceGenerators/` (gitignored).
- Generator package: `/home/noa3/.nuget/packages/godot.sourcegenerators/4.7.2/analyzers/dotnet/cs/Godot.SourceGenerators.dll`.
