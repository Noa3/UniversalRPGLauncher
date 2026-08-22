# Session Handoff — 2026-08-23

## What happened this session

1. **Pulled remote work** from the other agent (K-013/K-014/K-015/K-016/K-020/K-021-first-slice). HEAD became `c37dac2`; the build was broken by two `CS0039` Variant-cast errors in `GetCmdParams`.
2. **Fixed the build** (`project/src/rm2k/interpreter/EventInterpreter.cs`): `pCmd["parameters"].Obj` cast, removed dead `_shouldBreak`, removed the placeholder move-route case whose literal `0x69` (=105) collided with the new `ControlSwitches = 105` constant (`CS0152`).
3. **Completed K-021**: dispatch + bounded executors for `ControlSwitches`, `ControlVariables`, `TransferPlayer`. 6 new tests. Commit `bfa99a8`.
4. **Repository layout split (user request)**: Godot project moved into `project/` via `git mv`. Root keeps docs/notes, `scripts/`, and the pinned Godot runtime under `tools/godot/`. `scripts/validate.sh` runs with `--path "$ROOT_DIR/project"`. Validation green from the new layout.
5. **Completed K-023 — verified command codes**: authoritative table found in liblcf `src/generated/lcf/rpg/eventcommand.h` (`lcf::rpg::Cmd` enum, machine-readable); parameter semantics cross-checked against EasyRPG Player `src/game_interpreter.cpp` and `src/game_interpreter_map.cpp`. Interpreter rewritten on the typed `Rm2kMap.EventCommand` model (code/int params/text): ShowMessage 10110 + continuation 20110, ControlSwitches 10210 (mode 0=ON/1=OFF/2=flip), ControlVars 10220 ([target][op][operandType][value], const/var operands, div/mod-by-zero diagnostic), Teleport 10810 (map/x/y), Wait 11410 (tenths → frames, clamp 600), ConditionalBranch/ElseBranch/EndBranch 12010/22010/22011, Loop/BreakLoop/EndLoop 12210/12220/22210 with bounded stack, jump-back and jump-past semantics. Suite now **198/198 green**.

**Key discovery for future agents:** the verified RM2K/2003 event-command numeric codes live in liblcf's *generated* header `eventcommand.h` (also cached at `/tmp/opencode/eventcommand.h` this session). EasyRPG Player dispatches map commands in `game_interpreter_map.cpp` (e.g. Teleport 10810), not only in `game_interpreter.cpp`.
6. **Completed K-017 (user-directed MZ slice)**: `MzDataDirectoryResult.Extract(snapshot)` in `project/src/plugins/BuiltInEnginePlugins.cs` decodes bounded `data/Actors.json` + `data/MapInfos.json` metadata (counts + first 32 names, 256 KiB/file cap) with Godot's JSON parser; detects MZ encrypted assets (`.rpgmvp/.rpgmvo/.rpgmvm`); refuses snapshots without the rmmz runtime signature. New `TestMzDataDirectory` suite (5 tests). Suite total **203/203 green**. Note: malformed-JSON fixtures log a Godot engine-side "Parse JSON failed" error line by design of `Json.ParseString`; behavior is deterministic and asserted via diagnostics.

## Toolchain notes (Linux box)

- .NET SDK lives in `~/.dotnet`: run `export DOTNET_ROOT="$HOME/.dotnet"; export PATH="$HOME/.dotnet:$PATH"` before any dotnet command.
- Godot binary: `tools/godot/editors/4.7.2/linux-x86_64/Godot_v4.7.2-stable_mono_linux_x86_64/Godot_v4.7.2-stable_mono_linux.x86_64`.
- Run validation as `bash scripts/validate.sh` (no executable bit).
- After editing C# sources, rebuild before running the Godot test runner — it loads the last built DLL (stale-DLL runs can look like phantom failures).

## Known debt / follow-ups

- Conditional branch (12010) always takes the true branch; condition decoding (switch/variable/actor/timer) is the natural next interpreter card.
- ShowChoice 10140 / InputNumber 10150 are skipped pending presentation/input slices (K-032).
- ControlVars target modes >0 (indirect ranges) and operand types >1 (random/items/hero…) produce diagnostics and skip; extend when party/item models land.
- K-016 table row said DONE while its detail text said IN PROGRESS — resolved to DONE.
- `docs/PROJECT_STATUS.md` component paths updated to `project/src/...`; test count updated to 198/198.

## Next actions

1. New card: decode ConditionalBranch conditions against `GameSimulationState` (switch/var checks first), then K-022 movement per Kanban priority.
2. Push is pending user go-ahead; local commits: `bfa99a8` (K-021), layout commit, K-023 commit.
