# Session Handoff — 2026-08-23

## What happened this session

1. **Pulled remote work** from the other agent (K-013/K-014/K-015/K-016/K-020/K-021-first-slice). HEAD became `c37dac2`; the build was broken by two `CS0039` Variant-cast errors in `GetCmdParams`.
2. **Fixed the build** (`project/src/rm2k/interpreter/EventInterpreter.cs`): `pCmd["parameters"].Obj` cast, removed dead `_shouldBreak`, removed the placeholder move-route case whose literal `0x69` (=105) collided with the new `ControlSwitches = 105` constant (`CS0152`).
3. **Completed K-021**: dispatch + bounded executors for `ControlSwitches` (range on/off), `ControlVariables` (set/add/sub/mul/div/mod, division-by-zero diagnostic), `TransferPlayer` (pending-transfer state). All malformed/out-of-range payloads produce a diagnostic and are skipped. 6 new tests. Suite **199/199 green**; commit `bfa99a8`.
4. **Repository layout split (user request)**: Godot project moved into `project/` via `git mv`. Root keeps docs/notes (`*.md`, `docs/`), dev tooling (`scripts/`), and the pinned Godot runtime under `tools/godot/`. `.godot` cache regenerated inside `project/`. `scripts/validate.sh` now builds and runs Godot with `--path "$ROOT_DIR/project"`; binary discovery still uses root `tools/godot/editors/4.7.2/`. Full validation re-run from the new layout: **green (199/199 + import check)**.

## Toolchain notes (Linux box)

- .NET SDK lives in `~/.dotnet`: run `export DOTNET_ROOT="$HOME/.dotnet"; export PATH="$HOME/.dotnet:$PATH"` before any dotnet command.
- Godot binary: `tools/godot/editors/4.7.2/linux-x86_64/Godot_v4.7.2-stable_mono_linux_x86_64/Godot_v4.7.2-stable_mono_linux.x86_64`.
- Run validation as `bash scripts/validate.sh` (no executable bit).
- After editing C# sources, rebuild before running the Godot test runner — it loads the last built DLL (stale-DLL runs can look like phantom failures).

## Known debt / follow-ups

- **K-023 (READY)**: interpreter opcode constants are placeholders (101–118 block, plus 105–107). No verified RM2K/2003 numeric command table was found yet (EasyRPG Player has no `command_codes.h`; web search inconclusive). Migrate to verified codes and keep tests parameterized.
- K-016 table row said DONE while its detail text said IN PROGRESS — resolved to DONE (typed bounded metadata landed).
- KANBAN markdown glitches (`||` row prefixes) fixed.
- `docs/PROJECT_STATUS.md` component paths updated to `project/src/...`; test count updated to 199/199.

## Next actions

1. Work K-023 (verified command codes) or continue with K-022 (movement) per Kanban priority order — check the board first.
2. Push is pending user go-ahead; local commits: `bfa99a8` (K-021) + layout/docs commits after it.
