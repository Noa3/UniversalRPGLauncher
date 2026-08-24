# Cross-Engine QA Verification Report — t_1b2292d4

> Scope: independent verification pass over the four parent engine-plugin tracks
> merged into this worktree (base `1da7e2a`). Confirms detection + runtime per
> engine family, static review for cross-track regressions, defect fixes with
> regression tests, and a build/test matrix.

## Parent tracks integrated

| Track | Engine family | Source changes | Status in QA tree |
|-------|---------------|----------------|-------------------|
| t_ae3e01c0 | Metadata/docs only | none (docs) | merged (doc-only) |
| t_ba1d255d | RGSS XP / VX / Ace runtime + detection | yes | merged, verified |
| t_dbb7d1bd | Dante98 / RM95 | yes | merged, verified |
| t_a37367ee | WOLF RPG Editor | yes | merged, verified |

All four shared base `1da7e2a`; diffs captured as patches and applied in order.

## Build & test matrix (canonical gate)

Command:
`GODOT_BIN=E:/URPG/Godot_v4.7.2/Godot_v4.7.2-stable_mono_win64_console.exe bash scripts/validate.sh`

| Suite | Result |
|-------|--------|
| .NET build (`dotnet build`) | 0 errors, clean |
| Headless C# suite (all) | **248 / 248 passed** |
| validate.sh exit code | 0 — "UniversalRPG validation passed" |

Expected log noise: two `Parse JSON failed` lines from intentionally-malformed MZ
fixtures (`TestMzDataDirectory.Test_Malformed*`) and one VFS path-not-found probe.

## Per-engine verification matrix

Legend — Detection / Runtime = does the plugin detect it and expose a playable or
bounded runtime? Real fixture = signature-only inspection on disk (no game code executed).

| Engine | Detection | Runtime boundary | Synthetic test | Real fixture evidence |
|--------|:---------:|------------------|:--------------:|-----------------------|
| RM95 (Dante-era) | yes | detection-only | `Test_DetectRepresentative...` (`RM95`) | no GAME.RPG on disk; audit doc + plugin test |
| Dante 98 | yes | detection-only | `Dante98` + new `Test_Dante98FacadeEngineResolution` | DANTE98.MRK fixture; facade now resolves (defect fix) |
| RM2K / RM2003 | yes | parser-backed bootstrap | `RM2K`, `RM2K3` | TestGame LDB/LMT fixtures, real-fixture tests green |
| RM XP | yes | RGSS metadata lifecycle | `RMXP` (RGSS102A) | `IntheHamletofLoliBigtits_v103a` (RGSS104J.dll + Game.rxproj), ~7k files |
| RM VX / VX Ace | yes | RGSS metadata lifecycle | `RMVX`, `RMVXA` | same RGSS family; XP fixture representative |
| MV | yes | detection-only (+bounded System.json) | `RMMV`, `MvNestedWeb` (new nested-title test) | multiple `www/js/rpg_core.js` titles on disk |
| MZ | yes | detection-only (+bounded metadata) | `RMMZ`, `MZWeak/Malformed/Oversized` | 7+ `rmmz_core.js` trees; `SkiesInflateableAdventure` (7,039 files) |
| WOLF RPG Editor | yes | plain-data slice | `WOLF` (`Data/Game.dat`) | synthetic fixtures only on disk |
| Unite / Unity | yes | detection-only | `Unite` | metadata-only fixture |

## Defects found and fixed (with regression tests)

### D1 — Dante98 not resolvable through the legacy facade  (HIGH, CONFIRMED)
- Location: `src/game_detector/game_detector.cs` → `FromPluginId()`.
- Problem: the plugin system detected Dante98 correctly, but the legacy
  `GameDetector` facade mapped its engine id to `Unknown`, so UI-facing results
  lost the engine.
- Fix: added `EnginePluginIds.Dante98 => EngineType.Dante98` to the switch.
- Regression test: `Test_Dante98FacadeEngineResolution` (asserts `Engine ==
  Dante98` and display name "RPG Tsukūru Dante 98").

### D2 — Entry budget marked large, well-formed games as malformed  (HIGH, CONFIRMED)
- Location: `src/plugins/EngineDetectionContract.cs`, plus runtime initializers in
  `RgssEngineRuntime.cs` and `EngineBootstrapRuntime.cs`.
- Problem: bounded inspection capped at `MaxEntries=4096` / depth 4. Real games are
  7k–20k+ files, so the scan stopped early and set `malformed=true` + an Error
  diagnostic; runtime `Initialize()` then hard-failed on perfectly valid projects.
- Fix: introduced a distinct `partial` flag (well-formed but not fully covered).
  Directory + archive entry-budget paths now emit an Info advisory (`partial`)
  instead of Error+malformed; runtimes treat partial as an advisory Warning and only
  fail on true malformed input. Report level distinguishes "bounded-out (partial)"
  from genuinely malformed.
- Regression test: `Test_PartialEntryBudgetDoesNotRefuseDetection` (50-file MV tree,
  `MaxEntries=40`; asserts `IsPartial==true`, `IsMalformed==false`, MV still selected,
  `detection.partial-scan` diagnostic present, and runtime selector refuses MV for
  being detection-only — *not* for malformedness).

### D3 — MV title extraction used a naive first-match regex  (MEDIUM, CONFIRMED)
- Location: `src/plugins/BuiltInEnginePlugins.cs` → `RpgMakerMvPlugin.ExtractMetadata()`
  and the shared `JsonTitle()` helper.
- Problem: `Regex.Match(... "gameTitle" ...)` returned the *first* `"gameTitle"` in the
  document, so a nested key (e.g. inside an object) could shadow the real top-level title.
- Fix: parse with bounded `System.Text.Json` (`JsonDocument`, MaxDepth 64), reading only
  the root `gameTitle` string property; malformed JSON degrades to empty title (metadata-only).
- Regression test: `Test_MvMetadataTitleIgnoresNestedGameTitleKeys` (nested `"gameTitle"`
  placed *before* the top-level one so a naive regex would pick the trap value; asserts the
  top-level title wins, both via `ExtractMetadata` and end-to-end through real on-disk MV detection).

## Static review — cross-track regression check

- Shared contracts (`EngineDetectionContract`, `BuiltInEnginePlugins`, runtime base) reviewed
  across all four tracks; no duplicate engine-id ranges, no circular registration.
- `DetectableEngines` public array confirmed unused by any consumer (informational only).
- Partial/malformed change confined to inspection + the two runtime initializers; RM2K/LCF
  and WOLF paths unaffected (they do not read entry-budget state).
- No new per-frame allocations introduced on hot paths.

## Not verified / residual risk

- **RM95/Dante real fixtures**: no GAME.RPG on disk; Dante/RM95 evidence is audit-doc +
  plugin-level tests, not a live tree run.
- **WOLF**: synthetic fixtures only; no plain-data WOLF title present locally.
- **Real-fixture runtime launch** for XP/VX/Ace/MV/MZ was detection/metadata-only this pass
  (no game EXE/DLL/Ruby/JS executed, per import-security rule); full playable-runtime proof is
  out of scope and remains on the respective runtime cards.

## Status

STATUS: PASS WITH CONCERNS

- Blocking findings: none remaining after D1–D3 fixes (all regression-tested).
- Non-blocking / residual: real-fixture *runtime* launch for RGSS/MV/MZ not exercised this
  pass (detection-only); RM95/Dante/WOLF lack live on-disk fixtures.
