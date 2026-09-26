# UniversalRPG Autonomous Session State

> Updated: 2026-09-26
> Purpose: small durable checkpoint for Hermes/other autonomous agents.

## Current card

K-086 through K-093 and K-095 through K-098 are DONE. K-099 (compose a full map frame) is the next RM2K card; K-094 (vehicles) stays open at lower priority.

**K-098 completed (2026-09-26)**
- **Blocker solved by research**: the pinned fixtures come from the public `EasyRPG/TestGame` repo, which ships the chipset images. The right chipset was determined, not guessed: the map's `chipset_id` is `1` and that LDB entry's `chipset_name` is `World`, so `TestGame-2000/ChipSet/World.png` from the **same pinned commit** is the real chipset for the pinned LDB.
- The fixture README said "no image is imported". That was true while the project only parsed LCF data; it is now updated with the reason, the pinned URL and the SHA-256. The image is passive and never executed.
- Verified: `cache.cpp` `Material::Chipset` spec = directory `ChipSet`, `transparent` true, 480x256. `image_png.cpp` `ReadPalettedData` = palette index 0 is transparent, every other index opaque. The real file is 8 bit paletted, non interlaced, exactly 480x256, which independently confirms `Columns=30`/`Rows=16` from K-095.
- New `Rm2kChipsetBitmap.TryParse`/`TryLoad` (bounded paletted PNG decoding, keeping the **palette index** so transparency survives) with `TryBlitTile`/`TryBlitRectangle` implementing the verified transparency rule, plus a Godot free `Rm2kPixelBuffer`.
- The decoder refuses bad signature, non 8 bit depth, non paletted type, interlacing, oversized dimensions, missing/oversized palette, missing data and unknown scanline filters instead of reinterpreting them.
- The strongest test: every rectangle K-095..K-097 can produce for the real chipset, over 1000 of them, is blittable inside the real image.

**K-097 completed (2026-09-26)**
- **Defect in K-096 found while reading the source**: `GenerateAutotiles` packs the quarter pairs with the last quarter on top and unpacks `x` first, so the **second** value of a pair is the chipset column and the **first** is the row. K-096 had it transposed. Block D code and anchor expectations are corrected. The K-096 test did not catch it because it verified the table, not the axis order — lesson: an axis assumption needs its own test, which now exists.
- Verified `GenerateAutotileAB`: `block = ID/1000`, `b_subtile = (ID-block*1000)/50`, `a_subtile = ID-block*1000-b_subtile*50`, refusing `b_subtile >= TILE_SIZE` and `a_subtile >= 47`. `#define TILE_SIZE 16` is in `src/options.h`, so B is a four bit pattern.
- Three passes in order: B-supplied quarters, A-supplied quarters (row `animID + (block==1?3:0)`), then the combination pass which runs last and wins.
- `t ^= 3` swaps the two bits: a cleared bit 0 becomes **3**, a set bit 0 becomes 2. I initially claimed the B columns 4..6 only; all four (4..7) are reachable. The test now pins the reachable set.
- The Player de-duplicates quarters through a hash; that only affects generated cache layout, so it is not reproduced.
- New `Rm2kAutotileQuarters.TryResolveBlockAB`. **Every lower layer block now resolves**: A, B, D via the autotile tables, C, E, F straight from the chipset.
- Encoding note: a `for (var x in new[] { ... })` line would not compile in this project; declaring the array first works.

**K-096 completed (2026-09-26)**
- Verified in `tilemap_layer.cpp`: `BlockA_Subtiles_IDS[47][2][2]` (int8, `-1` = B liefert das Quartett) und `BlockD_Subtiles_IDS[50][2][2][2]` (uint8), Reihenfolge oben-links, oben-rechts, unten-links, unten-rechts.
- Verified `GenerateAutotileD`: `block = (ID-4000)/50`, `variant = ID-4000-block*50`, Ablehnung bei `block >= 12 || variant >= 50`. Blockursprung `(block%2)*3, 8+(block/2)*4` für `block < 4`, sonst `6+(block%2)*3, ((block-4)/2)*4`. Jedes Quartett = Blockursprung + Tabellenoffset.
- Wichtig: Der Player setzt Autotiles aus **vier** 16x16-Quartetten zusammen, eine Tile-ID löst also zu vier Chipset-Rechtecken auf, nicht zu einem.
- **Transkriptionsdisziplin**: Beide Tabellen wurden per Skript mechanisch aus der Player-Quelle extrahiert (188 Werte für A, 400 für D), mit Anzahl, Wertebereich und erster/letzter Zeile gegen die Quelle geprüft, bevor eine Zeile C# geschrieben wurde. Dasselbe Skript hat die Block-D-Anker-Erwartungen im Test erzeugt, damit der Test nicht von der Tabelle abweichen kann.
- Neu: `Rm2kAutotileQuarters.TryResolveBlockD` (vier `ChipsetRect`-Quartette, Range-Refusal) und `TryGetBlockAQuarters` (A-Tabelle für K-097 und für Regressionstests).
- Der erste Testlauf fand einen echten Defekt: Offset `variant * 4`, obwohl eine Variante acht Werte umfasst — jede Variante ab der zweiten las die falsche Zeile.
- Noch nicht: A/B-Zusammensetzung, Bitmap-Decoding, Blitting. Blöcke A, B, D lösen über `Rm2kChipsetSource` weiterhin **nicht** auf.

**K-095 completed (2026-09-26)**
- Verified in `tilemap_layer.cpp` (Draw): only blocks **C, E and F** are blitted straight from the chipset bitmap. Blocks A, B and D come from the generated caches `autotiles_ab_screen`/`autotiles_d_screen` and are refused instead of guessed.
- Formulas: block C `col = 3 + (id-3000)/50`, `row = 4 + animation_step_c` (columns 3-5, rows 4-7, because `BLOCK_C_TILES` is 3). Block E applies `lower_tiles` first, then `col = 12 + id%6, row = id/6` for `id < 96` else `col = 18 + (id-96)%6, row = (id-96)/6`. Block F applies `upper_tiles` first, then `col = 18 + id%6, row = 8 + id/6` for `id < 48` else `col = 24 + (id-48)%6, row = (id-48)/6`.
- The formulas need at least 30 x 16 tiles of 16 px, derived from the largest column (24+5) and row ((143-48)/6).
- Range detail kept on purpose and documented: the Player guards block C with `< BLOCK_D`, not with the end of block C, so 3150..3999 still resolve. Its passability lookup uses the same range, so renderer and simulation must agree.
- New `Rm2kChipsetSource.TryResolve` with `ChipsetRect`, an identity overload, and `Columns`/`Rows` bounds. Unknown ids and unresolvable substitutions fail closed.
- Still no pixels: `VirtualFramebuffer` stores tile ids only, and the pinned fixtures contain no `Chipset.png`.

**K-093 completed (2026-09-26)**
- Card correction: the host was **already** wired. `Main.cs` constructs `Rm2kInputMapper`, calls `SetTouchViewport` in `_Ready`, and handles `_UnhandledInput` with the verified key edge rules. The real defect was narrower: the host had an input path that bypassed everything K-091/K-092 verified.
- Before: `Confirm` used a local `GetFacingTarget` with no map wrap and then `TriggerAt(x, y, Action)` — no layer rule, no touched/collision in front, no counter walk. A direction called `TryMove` and then `TriggerAt(mapX, mapY, Touched)` on success only — no layer rule and no blocked-step in-front path.
- After: the map input branch calls `Rm2kEngineRuntime.SubmitInput(action)`, so the host inherits the verified `Here`/`There` choice, the layer rules, the counter walk, the pause/running-event guards and the map wrap. `GetFacingTarget` is deleted.
- Input is marked handled when consumed, including a blocked step that moved nothing; `None`/`Menu`/`Cancel` stay unhandled. The message/choice/numeric-input priority stays ahead of map input because that is the `IsMessageActive` gate.
- Not test-covered: the `Main.cs` branch is a Node override and cannot run headless without the scene; the runtime side is covered in `TestPluginDetection`, the host branch was verified by reading the code.

**K-092 completed (2026-09-26)**
- Card correction: a successful step does **not** trigger touched/collision in front. `Game_Player::UpdateNextMovementAction` calls `CheckEventTriggerThere` (in front, layer same) only when the step was **blocked**, while `Game_Player::UpdateMovement` calls `CheckEventTriggerHere` (own tile, layer **not** same) after a **successful** step. The layer rules are opposite in the two cases.
- Verified: on a stop plus decision key the vehicle toggle runs first and the action check only runs if no vehicle was toggled. `CheckActionEvent` unions touched/collision in front, action on the own tile, and the action chain over at most three counter tiles.
- New `Rm2kEventScheduler.TriggerTouchOrCollisionHere` and the complete `CheckActionEvent`.
- New `Rm2kPlayerTurn` (Godot-free, so the ordering is regression tested): refuses while paused, in a menu, or while an event page runs; a direction attempts `TryMove` then picks the `Here` or `There` path; `Confirm` runs `CheckActionEvent`.
- New `Rm2kEngineRuntime.SubmitInput(Rm2kInputAction)`, refused unless the runtime is running.
- Deliberate simplification, recorded not faked: no vehicles/airship exist, so the vehicle toggle cannot change anything and the action check always runs.
- Still not reachable from a game: nothing constructs `Rm2kInputMapper` or forwards input, so `SubmitInput` is only called by tests. That is K-093.

**K-091 completed (2026-09-26)**
- Verified: `Game_Map::IsCounter` = upper tile `>= BLOCK_F`, id through `upper_tiles`, entry carries `Counter` (`0x40`). `XwithDirection`/`YwithDirection` = the tile in front with the looping map wrap applied.
- Verified: the action search checks the tile in front, then steps over a counter tile and checks again, at most three times. Four counter tiles in a row stop the search.
- Verified layer rules (easy to get backwards): events **in front** of the player must have `Layers_same` (`1`), events **on the player's own tile** must **not** have it. Touch/collision while walking never walk counter tiles.
- **Defect fixed**: LMU field `0x22` was decoded and stored as `priority`. liblcf has no `priority` field — `0x22` is `layer` (`below=0, same=1, above=2`). The stored value was unusable, so the layer rules could not be implemented at all. Now `layer` in `Rm2kMap.EventPage.Layer`.
- New: `Rm2kChipset.IsCounterTile`, `GameSimulationState.IsCounterAt`/`FrontTile`/`Wrap`/`UpperLayer`/`UpperPassability`, `Rm2kEventScheduler.TriggerActionFacing`/`TriggerActionHere`/`TriggerTouchOrCollisionFacing` with `Rm2kTriggerLayerRule` and `MaxCounterTiles = 3`.
- Two of my own mistakes, both caught by the tests: the counter loop first checked the tile *before* stepping (the Player steps first), and the loop condition was inverted.
- Still not wired: nothing in the runtime calls the trigger API yet, so these entry points are implemented and tested but unreachable until K-092.

**K-089 completed (2026-09-26)**
- Verified: `terrain_data = 0x03` is 162 **shorts** (324 bytes), liblcf `int16_t`, all ones by default. RPG_RT omits an all-ones table and the Player returns terrain 1 for an empty table, so an absent table is normal data.
- Verified: only the **lower** layer decides the terrain, the upper layer is never consulted, and the order is raw id -> `ChipIdToIndex` -> substitution in `[18, 162)` -> `terrain_data[chip_index]`. Out-of-bounds uses chip index 0.
- Parser decodes `terrain_data` with a bounded length check per chipset entry (plus the section-level key for the first entry) and reports unverified lengths with an offset.
- `Rm2kTileSubstitution.GetTerrainTag` implements the lookup and falls back to `DefaultTerrainTag` (1) for an absent table or an uncovered chip index, instead of reading out of bounds like the Player's `assert` permits.
- `GameSimulationState` gained `TerrainData`, `LowerLayer`, `TileSubstitution`, `GetTerrainTagAt`; the runtime reads the table of the map's own chipset.
- The pinned RM2000 fixture carries a real 162-entry terrain table with valid tag ids.

**K-088 completed (2026-09-26)**
- Card correction: the substitution tables are **not** LMT data. `lcf::rpg::MapInfo` has no such fields and `ChunkMapInfo` has no field ids for them. They live in `lcf::rpg::SaveMapInfo` (`lower_tiles`, `upper_tiles`, 144 identity entries), so they are save-file data. Reading them belongs with the open K-050 save-game work.
- Verified order in Player `game_map.cpp`: upper layer reduces by `BLOCK_F` then substitutes through `upper_tiles`; lower block E reduces by `BLOCK_E` then substitutes and adds `BLOCK_E_INDEX`; blocks A/B/C/D are never substituted; `GetChipId` converts the raw id to a chip index **first** and then remaps indices in `[BLOCK_E_INDEX, NUM_LOWER_TILES)`.
- New `Rm2kTileSubstitution` with identity default, `SubstituteLower`, `SubstituteUpper`, `ResolveChipIndex`. Tables that do not fit the 144-entry range fall back to identity instead of clamping; out-of-range requests return -1 and fail closed.
- `Rm2kChipset.IsPassableLowerTile`, `IsPassableTile` and `BuildDirectionMasks` accept an optional substitution; the old overloads keep identity, so the runtime is unchanged until a save supplies a table.

**K-087 completed (2026-09-26)**
- Verified autotile animation in EasyRPG Player `src/tilemap_layer.cpp` (Draw), `src/game_map.cpp` (SetChipset, GetAnimationType/Speed) and liblcf `src/generated/lcf/ldb/chunks.h`.
- `animation_type = 0x0B`, `animation_speed = 0x0C`; the project's scalar field contract matches upstream.
- `GetAnimationSpeed()` = `animation_speed != 0 ? 12 : 24`. `animation_speed` is an animated/not flag, not a frame rate and not an on/off switch: even the zero default keeps AB autotiles cycling at half speed.
- AB (blocks A/B): `frames / speed`, cyclic `% 3`, reciprocating `% 4` with `3 → 1` (0,1,2,1). Block C: `(frames / 6) % 4`, independent of the chipset settings. Blocks D/E/F: never animate. `frames` is the RPG_RT frame counter, already ticked as `Simulation.FrameCount`.
- New API: `Rm2kChipset.AnimationSpeed/ReciprocatingStep/CyclicStep/CBlockStep/ChipAnimationStep`, plus `GameSimulationState.ChipsetAnimationType`, `ChipsetAnimationSpeed`, `GetChipAnimationStep`.
- Parser fix: the passability tables now live on the matching typed chipset entry (matched by `id`), and the section-level keys stay for the first entry. Runtime fix: the chipset is selected by the LMU `chipset_id`, like `Game_Map::SetChipset(map->chipset_id)`, instead of assuming the first chipset.
- Counter values are deliberately **not** implemented: liblcf `master` has no per-map counter array on `lcf::rpg::Map`/`MapInfo`, so there is no verified data source; inventing one is what K-086 forbids.
- Lesson recorded: passability and animation belong to one chipset entry, and the LMU `chipset_id` is the verified selector. `sections["chipsets"]` is the raw section, while the typed entries are the top-level `chipsets` array.

**K-086 completed (2026-09-26)**
- Verified EasyRPG Player constants in `src/map_data.h`: passability bits `Down=0x01`, `Left=0x02`, `Right=0x04`, `Up=0x08`, `Above=0x10`, `Wall=0x20`, `Counter=0x40`; tile blocks A-F with strides 1000/1000/50/50/1/1, indices 0/2/3/6/18/162, ends 2000/3000/3150/4600/5144/10144; `NUM_LOWER_TILES=162`, `NUM_UPPER_TILES=144`.
- Verified `Game_Map` rules: upper layer decides first and only falls through to the lower layer when the upper entry carries `Above`; `Wall` autotile exception covers ids 20-23, 33-37, 42, 43, 45, 46.
- New `project/src/rm2k/simulation/Rm2kChipset.cs` (chip-id conversion, direction bit, upper-then-lower resolution, `BuildDirectionMasks`) with fail-closed handling for unknown ids, missing tables, and mismatched layer lengths.
- `GameSimulationState` gained `PassabilityMasks` + `IsPassableInDirection`; `TryMove` checks the direction bit; the old `IEnumerable<bool>` `ConfigureMap` still works by mapping passable to all four directions.
- `Rm2kEngineRuntime` reads `passable_data_lower`/`passable_data_upper` from the LDB chipset section, verifies 162/144, builds masks from LMU `lower_layer`/`upper_layer`, and configures the simulation. The fail-closed "chipset passability is not decoded yet" diagnostic is gone.
- New `project/tests/core/test_rm2k_chipset.cs` (8 tests) pins the verified constants and rules; `Test_RealFixtureChipsetProducesBothPassableAndBlockedTiles` proves real RM2000/RM2003 maps mix walkable and impassable tiles and that real steps follow them; `TestPluginDetection` asserts the runtime decoded non-empty masks.
- Lesson recorded: passability flags are stored **per chipset chip id**, so a test that needs different behaviour for two map tiles must use two different tile ids. Truncated/mismatched layer arrays intentionally yield the shorter length, and the runtime separately requires `masks.Length == width * height`.

## Next action

1. Start K-099: re-read `CreateTileCacheAt` in `tilemap_layer.cpp` for the z-order rule (upper sublayer from the `Above` flag after substitution, lower from `Wall`/`Above` on the resolved chip index) and how the two layers plus three sublayers are drawn. Do not invent a draw order.
2. Unrelated local changes must stay untouched: `project/assets/fonts/NotoSansCJKsc-Regular.otf.import` (line endings) and untracked `qa_patches/`.
3. Reference Player/liblcf sources used this session are cached under `%TEMP%\opencode\rpgrefs` (`game_map.cpp`, `game_character.cpp`, `tilemap_layer.cpp`, `spriteset_map.cpp`, `chunks.h`, `lmt_chunks.h`, `liblcf_chipset.h`, `liblcf_map.h`, `mapinfo.h`, `savemapinfo.h`).

## Reference repos noted by the user (2026-09-26, not actioned)

- `joiplay/mkxp`, `joiplay/android-mkxp` — RPG Maker XP (RGSS) reimplementation in C++; useful as a cross-engine reference for Ruby/RGSS and for its own passability handling, not a source of RM2K constants.
- `futokoro/RPGMaker` — Ruby RGSS reimplementation (XP/VX/Ace).
- `bakustarver/rpgmakermlinux-cicpoffs` — RPG Maker on Linux via C++ offscreen; detection/hosting reference.
- Consequence: XP/VX/Ace work stays at priority 6-7 per `AGENTS.md`; MV/MZ playability still needs a JS runtime, which the repository policy does not provide.

## Last verified baseline

Windows validation on 2026-09-26: `dotnet build project/UniversalRPG.csproj` (0 errors) and the headless C# runner at `All 400 tests passed`, exit 0. The Godot project lives under `project/`; `validate.sh` handles both layouts.

|K-032, K-040, K-041, K-050, and K-055 are DONE; K-033 through K-039 and K-042 are also DONE — engine-neutral `IRuntimeSaveTools` and `IRuntimeDebugTools` gate in-memory save snapshots and local debug mutations. K-050 adds a read-only bounded original `LcfSaveData` framing model with unknown-chunk retention; semantic field mapping, save mutation, and UI integration remain separate. RM2K/RM2K3 explicitly declare `SaveLoad`/`Debugging`; debug tools are off by default.|
|midnightschool.exe (C:\Users\noa3\Desktop\Neuer Ordner (3)) analyzed detection-only: NSIS-3 Unicode installer wrapping `$PLUGINSDIR/app-64.7z` = Electron x64 distribution; `resources/app.asar` contains a complete unencrypted RPG Maker MZ 1.x game under `project/` (title: 深夜学校のパイズリ怪異, 858 files / ~238 MiB extracted to %TEMP%\midnight-extract\mzgame with standard layout index.html + js/rmmz_core.js + rmmz_managers.js + data/System.json). The extracted Electron host was externally launch-verified with process exit 0 and visually confirmed by the user. Static ASAR inspection shows `package.json` main=`src/main.js`; the host creates an Electron window and loads `project/index.html` from inside the ASAR. This proves the vendor launcher works, not a UniversalRPG runtime path; the existing MZ plugin remains detection-only and must not mark the installer EXE as directly startable.|

## Last verified baseline (previous session)

Windows validation on 2026-08-31: `dotnet restore`, `dotnet build project/UniversalRPG.csproj --no-restore` (0 warnings, 0 errors), Godot import (`--headless --editor --quit`), and the headless C# runner at `All 300 tests passed`, exit 0.

## Cross-engine QA pass (t_1b2292d4) — completed 2026-08-24

All four parent tracks (t_ae3e01c0 docs-only, t_ba1d255d RGSS XP/VX/Ace, t_dbb7d1bd Dante98/RM95, t_a37367ee WOLF) merged on base `1da7e2a`; full matrix + defect fixes in `docs/CROSS_ENGINE_QA_REPORT.md`. Defects fixed with regression tests (suite 245 → 248):
- D1: `GameDetector.FromPluginId()` was missing the Dante98 mapping — facade reported Unknown. Added case; test `Test_Dante98FacadeEngineResolution`.
- D2: bounded inspection flagged >4096-entry well-formed games as malformed, hard-failing runtime init (real XP/MZ trees are 7k+). New `partial` advisory flag distinct from malformed in `EngineDetectionContract.cs`; `RgssEngineRuntime`/`EngineBootstrapRuntime` accept partial with a Warning. Test `Test_PartialEntryBudgetDoesNotRefuseDetection`.
- D3: MV `ExtractMetadata` + shared `JsonTitle` used first-match regex for `"gameTitle"`, so nested keys could shadow the top-level title. Switched to bounded System.Text.Json root-property read (MaxDepth 64, malformed → empty). Test `Test_MvMetadataTitleIgnoresNestedGameTitleKeys`.
Residual: RM95/Dante/WOLF have no live on-disk fixtures (plugin tests + audit doc only); RGSS/MV/MZ real-fixture runs were detection/metadata-only this pass.

## Layout note (2026-08-23)

Godot project files (`project.godot`, csproj/sln, app/, src/, tests/, assets/, locale/, scenes/, plugins/) moved to `project/`. Root keeps docs/notes, `docs/`, `scripts/`, and the Godot runtime under `tools/godot/`. Build/test commands must target the project dir (validate.sh does this automatically).

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
- RM95, RGSS, MV, MZ, and Unite remain detection-only; WOLF exposes an explicitly unencrypted plain-data slice, and RM2K/RM2K3 retain the parser-backed runtime bootstrap.
- Rewired `GameDetector`, `GameLibrary`, `RuntimeLauncher`, and the Godot UI to preserve ranked detection reports, persist import metadata, and refuse unsafe/unsupported runtime selection without external fallback.
- Added contract, detection, archive, persistence, ambiguity, platform, and lifecycle regression coverage.
- Added RGSS and WOLF regression fixtures/tests; RGSS selector refusal is verified and validation passes with Godot 4.7.2 Mono: `279/279` tests.
- Nullable contracts were hardened across C# core/UI/test code; `.NET` build now reports `0` warnings and `0` errors.

## Current action

K-022, K-030, and K-031 are complete for their bounded slices; the runtime update/scheduler integration and lifecycle reset evidence were extended in the current slice. `GameSimulationState` supports bounded map configuration and movement. `Rm2kEngineRuntime` now bridges LMU geometry/map ID/start-map diagnostics into simulation, creates a bounded `VirtualFramebuffer` from validated lower/upper layers, builds bounded player/event sprite descriptors through `Rm2kSpriteAdapter`, and forwards decoded events to `Rm2kEventScheduler`; `Update()` drives native autorun commands through the deterministic clock; `Stop()` clears scheduler, clock, presentation, simulation, loaded map data, framebuffer, and sprite-descriptor state. `VirtualFramebuffer`/`Rm2kRendererAdapter` assemble validated lower/upper layers; sprite/camera adapters remain bounded and data-only.

Fixture reconnaissance: `D:\NextCloud\Games\PornGames\SkiesInflateableAdventure` is an unencrypted RPG Maker MZ tree (`index.html`, `js/rmmz_core.js`, `js/rmmz_managers.js`, `data/System.json`, title `Skie's Inflatable Adventures (v0.30.001)`, 7,039 files). `D:\NextCloud\Games\PornGames\IntheHamletofLoliBigtits_v103a` is not an MZ web tree at its root: no `index.html`, `js/rmmz_*`, or `data/System.json`; Japanese locale remains unconfirmed and no encrypted marker was found in the bounded filename scan. Both were inspected detection-only; no game code executed.

## Completed K-055

- Added bounded `TryWriteFile`/`TryReadFile` APIs to the JSON-only `Rm2kSimulationSaveCodec` for runtime-owned slot files.
- Slot paths are confined under the caller-supplied directory; invalid names and traversal are rejected before I/O.
- Writes serialize to a temporary file and replace the target; temporary cleanup is attempted after success/failure.
- Regression coverage verifies slot round-trip, gold preservation, traversal rejection, and cleanup.
- This does not parse or write original RM2K/RM2K3 `LSD` saves.

## Completed K-040

- Added explicit in-memory `RtpRegistry`/`RtpProfile` registration and deterministic asset resolution by engine, generation, dependency, and bounded relative path.
- Rejects invalid identifiers, missing or reparse-point roots, duplicate profile IDs, traversal/absolute/NUL paths, and reparse-point escapes.
- Resolution only checks file existence and returns a structured result; it never opens, parses, downloads, or executes RTP data.
- K-041 now provides the follow-up missing-asset diagnostics and bounded per-game metadata.

## Completed K-041

- Added bounded `RtpGameProfile` metadata and a JSON codec with payload/list/path limits.
- Added `RtpAssetDiagnostics` with distinct `Available`, `MissingAsset`, `NoMatchingProfile`, and `InvalidPath` statuses.
- Diagnostics use the explicit registry only and never open, parse, download, or execute RTP assets.
- Profile metadata is not yet persisted into `GameLibrary` records; that remains a separate integration decision.

## Completed K-050

- Added read-only `Rm2kLsdSaveCodec` and typed `Rm2kLsdSaveModel` over the existing bounded LCF reader.
- Preserves chunk IDs, lengths, offsets, payload bytes, and unknown-chunk count; rejects invalid paths, malformed/truncated framing, missing terminators, oversized files, and oversized chunks.
- No event commands, scripts, plugins, or native content are executed; original saves are never written.
- Validation: analyzer build clean and full headless suite `279/279` passed.

## Latest completed lifecycle slice (2026-08-28)

- `EnginePluginHost` now permits `Stopped → Start`.
- A stopped runtime is disposed exactly once before a fresh runtime is selected and initialized; stopped runtime objects are never re-initialized.
- `Test_Rm2kRuntimeCanRestartAfterStopWithFreshRuntimeState` verifies `Start → Update → Stop → Start`, fresh framebuffer/map state, clock reset, scheduler reload, and distinct runtime identity against the real RM2K fixture.
- Focused result: `TestPluginDetection 22/22`; canonical result: `All 280 tests passed`.

## Latest completed sprite synchronization slice (2026-08-28)

- `Main._UnhandledInput` now routes movement through `Rm2kEngineRuntime.TryMove()`.
- The runtime refreshes bounded player/event descriptors only after successful movement, preserving the parser map as the event-data source and avoiding direct UI mutation of simulation/render state.
- `Test_Rm2kRuntimeMovementSynchronizesPlayerSpriteDescriptor` covers movement and descriptor position synchronization against the real RM2K fixture.
- Focused result: `TestPluginDetection 23/23`; canonical result: `All 281 tests passed`.

## Latest completed pending-transfer validation slice (2026-08-28)

- `EventInterpreter` rejects map IDs outside `1..GameSimulationState.MaxMapId` and negative transfer coordinates before mutating pending state.
- Invalid requests preserve an existing pending transfer and emit bounded diagnostics.
- `Test_TeleportRejectsInvalidMapIdsWithoutOverwritingPendingState` covers the contract.
- Focused result: `TestEventInterpreter 34/34`; canonical result: `All 282 tests passed`.

## Latest completed transfer-facing validation slice (2026-08-28)

- `EventInterpreter` validates the optional transfer facing parameter against RM2K directions `2/4/6/8` before mutating state.
- Valid facing is applied; invalid facing preserves the existing direction and pending transfer atomically.
- `Test_TeleportAppliesValidFacingAndRejectsInvalidFacingAtomically` covers the contract.
- Focused result: `TestEventInterpreter 35/35`; canonical result: `All 283 tests passed`.

## Latest completed choice lifecycle slice (2026-08-28)

- `EventInterpreter` clears `PresentationState.ActiveChoice` after a valid selection is confirmed and logged.
- `Test_ShowChoicePausesUntilSelection` verifies that the interpreter advances without leaving stale choice UI state.
- Focused result: `TestEventInterpreter 35/35`; canonical result: `All 283 tests passed`.

## Latest completed InputNumber lifecycle slice (2026-08-29)

- `EventInterpreter` pauses an `InputNumber` command when a different variable already owns the pending presentation input.
- The existing pending variable/value remain unchanged; no conflicting variable is created or mutated.
- `Test_InputNumberDoesNotConsumePendingValueForDifferentVariable` covers the conflict contract.
- Focused result: `TestEventInterpreter 36/36`; canonical result: `All 284 tests passed`.

## Latest completed ChangeGold interpreter slice (2026-08-29)

- `EventInterpreter` now handles verified RM2K command `10310` (`ChangeGold`) with EasyRPG semantics: operation `0` adds and operation `1` subtracts; operands may be constants or bounded variables.
- Gold is clamped to the modeled RM2K range `0..999999`; malformed parameters, invalid operand variables, and unsupported operations fail closed with bounded diagnostics.
- `Test_ChangeGoldAddsConstantOperand`, `Test_ChangeGoldClampsToBoundedRange`, `Test_ChangeGoldSubtractsAndClampsBelowZero`, `Test_ChangeGoldReadsVariableOperand`, and `Test_ChangeGoldRejectsInvalidParametersFailClosed` cover the new command path and bounds.
- Focused result: `TestEventInterpreter 41/41`; canonical result: `All 289 tests passed`; build and `scripts/validate.sh` passed.
- Chipset passability remains intentionally fail-closed: `PassabilityLayer` has no verified LMU/Chipset parser source yet. Unblock requires a verified liblcf/EasyRPG field mapping plus a fixture distinguishing passable and impassable tiles.

## Latest completed ChangeItems interpreter slice (2026-08-29)

- `EventInterpreter` now handles verified RM2K command `10320` (`ChangeItems`) with EasyRPG semantics: operation `0` adds and operation `1` subtracts; item IDs and amounts may be constants or bounded variables.
- Item counts are clamped to the modeled range `0..999999`; negative amounts, invalid IDs/variables, unsupported operand types, malformed parameters, and unsupported operations fail closed with bounded diagnostics.
- Regression coverage includes constant addition, variable item/amount subtraction, lower-bound clamping, invalid-operation rejection, and verified opcode/mode constants.
- Focused result: `TestEventInterpreter 44/44`; canonical result: `All 292 tests passed`; build and `scripts/validate.sh` passed.
- Chipset passability remains intentionally fail-closed: `PassabilityLayer` has no verified LMU/Chipset parser source yet. Unblock requires a verified liblcf/EasyRPG field mapping plus a fixture distinguishing passable and impassable tiles.

## Latest completed ChangePartyMembers interpreter slice (2026-08-29)

- `EventInterpreter` now handles verified RM2K command `10330` (`ChangePartyMembers`) with EasyRPG semantics: parameter layout `[operation, actor_mode, actor_id]`; operation `0` adds and operation `1` removes; actor IDs may be constant or bounded variables.
- Party mutations are bounded by `GameSimulationState.MaxPartyMembers` (`4`) and `MaxActorId` (`50000`); duplicate additions, removal of absent actors, invalid operands, malformed parameters, and unsupported operations fail closed with diagnostics.
- Regression coverage includes constant addition, variable-ID removal, duplicate rejection, invalid actor rejection, and verified opcode/mode constants.
- EasyRPG source verification: `CommandChangePartyMember` uses command code `10330`, resolves `ValueOrVariable(com.parameters[1], com.parameters[2])`, then adds for operation `0` and removes otherwise.
- Focused result: `TestEventInterpreter 47/47`; canonical result: `All 296 tests passed`; build and `scripts/validate.sh` passed.
- Chipset passability remains intentionally fail-closed: `PassabilityLayer` has no verified LMU/Chipset parser source yet. Unblock requires a verified liblcf/EasyRPG field mapping plus a fixture distinguishing passable and impassable tiles.

## Latest completed simulation lifecycle slice (2026-08-31)

- `GameSimulationState.Reset()` now clears all mutable runtime collections: switches, variables, inventory, party members, actor state, troop members, common-event IDs, and passability data.
- This prevents stale gameplay state from surviving a runtime stop/restart boundary.
- Regression coverage: `Test_ResetClearsMutableRuntimeCollections` in `TestGameSimulationState`.
- Fresh canonical validation: `TestGameSimulationState 20/20`; `All 297 tests passed`; `dotnet build project/UniversalRPG.csproj --no-restore` passed with 0 warnings and 0 errors; `scripts/validate.sh` passed.
- The known non-fatal Godot `EditorSettings` headless diagnostic and intentional malformed-JSON fixture diagnostics remain unchanged.

## Latest completed real LMU event-page decoding slice (2026-08-31)

- Fixed a silent parser defect: `ParseStructArray`/`ReadStructFields` only materialized objects and fields when an internal collect flag was set, so nested `rpg::EventPage` arrays decoded to zero pages in every real LMU file. The event interpreter, scheduler, and page-condition paths had never executed against real data.
- Struct arrays/fields are now always materialized; the collect flag was removed.
- `EventInterpreter.End` corrected from `0` to the verified liblcf `END = 10`.
- Page field ids reduced to the verified liblcf set (condition `0x02`, move_frequency `0x20`, trigger `0x21`, layer `0x22`, move_route `0x29`, `event_commands_size` `0x33`, `event_commands` `0x34`); unverified fallbacks `0x09`/`0x08`/`0x06`/`0x0b` were removed.
- Nested `EventPageCondition` payloads are now decoded as struct fields; previously this threw `KeyNotFoundException` and faulted RM2K runtime initialization.
- A command vector that cannot be decoded is contained per page (`command_error`, `event_commands_bytes`) so one bad page no longer makes the whole map unloadable. One RM2003 page carries a 5-byte BER value above 31 bits; the encoding is left undecoded rather than guessed.
- Real-fixture coverage: RM2000 `Map0001.lmu` 22 pages, RM2003 38 pages; RM2000 decodes every command vector, and `event_commands_bytes` matches the declared `event_commands_size`.
- Regression coverage: `Test_RealMapEventPagesDecodeCommandCountsMatchingLiblcfSizes`, `Test_Rm2000RealMapPagesDecodeEveryCommandVector`, `Test_LiblcfEndCommandStopsInterpreterWithoutDiagnostic`.
- Fresh canonical validation: `All 300 tests passed`; `dotnet build project/UniversalRPG.csproj --no-restore` 0 warnings and 0 errors; Godot import exit `0`.

## Latest completed event-trigger alignment slice (2026-08-31)

- `Rm2kEventTrigger` mirrored invented values (`Autorun=0, Parallel=1, Action=2, Touch=3`); liblcf `lcf::rpg::EventPage::Trigger` defines `action=0, touched=1, collision=2, auto_start=3, parallel=4`, and EasyRPG Player compares those raw ids against decoded pages. The enum now matches liblcf exactly, so real auto-start, parallel, and action pages can finally match.
- `Rm2kEngineRuntime` now fails closed: a page with a non-empty `command_error` is skipped with a diagnostic instead of being bridged as an empty page that would run as if valid.
- Real-fixture trigger values are asserted to stay inside the verified liblcf set, and the pinned fixtures are confirmed to contain action-trigger pages.
- New end-to-end coverage: `Test_Rm2kRuntimeExecutesRealFixtureActionPages` starts the RM2K runtime on the pinned fixture, triggers a real action page, advances 20 frames, and requires interpreter diagnostics — the first proof that real fixture commands execute through the runtime.
- Condition semantics were cross-checked against EasyRPG Player `Game_Event::AreConditionsMet`: switch A and switch B both require ON, RM2000 uses `variable >= value` while RM2K3 uses the six compare operators, timers compare with `secs > limit`. The existing implementation already matches, so nothing was changed there.
- Fresh canonical validation: `All 304 tests passed`; `dotnet build project/UniversalRPG.csproj --no-restore` 0 warnings and 0 errors; Godot import exit `0`.

## Latest completed control-command parameter layout slice (2026-08-31)

- `ControlSwitches` and `ControlVars` read `parameters[0]` as the first id, but EasyRPG stores the lvalue form there (`Game_Interpreter_Shared::TargetEvalMode`), with the start id in `parameters[1]` and the range end in `parameters[2]`. Real payloads therefore started at id `0` and were always rejected as `invalid range 0-…`, so no real switch or variable command ever executed.
- Both commands now follow the verified layout: `ControlSwitches` `[targetMode, start, end, mode]`, `ControlVars` `[targetMode, start, end, operation, operandMode, operand, bitfield]`.
- `TargetEvalSingle` collapses the range end to the start id like `DecodeTargetEvaluationMode`; patch-only target modes stay fail-closed with diagnostics.
- Added verified `VarOperandVariableIndirect` (`v[v[x]]`, EasyRPG `ValueOrVariable` mode 2).
- Verified minimum widths from `Game_Interpreter::ExecuteCommand` are now asserted against the pinned fixtures: `ControlSwitches` 4, `ControlVars` 7, `ChangeLevel` 6, `ConditionalBranch` 6, `ChangeGold` 3, `ChangeItems` 5, `ChangePartyMembers` 3, `Teleport` 3, `Wait` 1.
- The real-fixture runtime test now fails if a real control command is rejected as an invalid range or a patch-only target mode, so this regression cannot silently return.
- Still diagnostic-only by design: `ChangeLevel` (10420, 6 occurrences), `ChangeHeroName` (10610), screen effects (11040/11050/11070), `CallEvent` (12330), `ChangeBattleCommands` (1009), and Maniac codes found in the fixtures.
- Fresh canonical validation: `All 309 tests passed`; `dotnet build project/UniversalRPG.csproj --no-restore` 0 warnings and 0 errors; Godot import exit `0`.

## Latest completed interpreter command batch (2026-08-31)

Implemented from the verified liblcf command table and EasyRPG `ExecuteCommand` dispatch widths:

- `ChangeLevel` (10420) and `ChangeExp` (10410), 6 parameters `[actorMode, actorId, operation, operandMode, operand, showMessage]`, using `GetActors` modes (party / hero / variable-held hero) and `OperateValue` add/subtract. Levels clamp to `1..99`, exp to `0..999999`. `GameSimulationState` gained bounded per-actor records (`GetOrCreateActorState`, level/exp/name accessors).
- `ChangeHeroName` (10610) stores the command string as the actor name, bounded to 64 characters.
- `EndEventProcessing` (12310) ends the current command frame rather than the whole interpreter.
- `FlashScreen` (11040), `ShakeScreen` (11050) and `WeatherEffects` (11070) drive new bounded screen-effect state on `PresentationState`; `Rm2kEngineRuntime.Update` ticks effects with elapsed simulation frames, and the wait flag reuses the tenths-to-frames conversion. Weather strength clamps to 2, unknown RM2K types fold to 0.
- `CallEvent` (12330) pushes a bounded nested frame for map events via an injected resolver (`Rm2kEventScheduler` answers from its own event list). Nested `END` returns to the caller, loop-stack depth is restored per frame, recursion is capped at `MaxScriptRecursion`, and common-event targets stay diagnostic-only because the LDB common-event section is not decoded yet.
- `ChangeEventLocation` (10860) and `EraseEvent` (12320) mutate event position and activity through scheduler-backed hooks, so `TriggerAt` observes the new position and the owning interpreter stops.
- Corrected the stale `Rm2kMap.EventPage.Trigger` comment to the liblcf enum.
- Fresh canonical validation: `All 330 tests passed`; `dotnet build project/UniversalRPG.csproj --no-restore` 0 warnings and 0 errors; Godot import exit `0`.
- Still diagnostic-only: `ChangeBattleCommands` (1009), menu/Maniac codes (5001-5005, 11610), `MoveEvent` (11330, needs move routes), `ChangeMapTileset` (11710), battle-dependent commands, and common-event calls.

## Latest completed MV/MZ parity slice (2026-08-31)

- Scope boundary: MV/MZ gameplay requires a JavaScript engine, which stays blocked behind card K-090 and the repository rule against executing imported JavaScript. This slice is data-only by design.
- The bounded `data/` inventory is now shared between both engines: `WebDataDirectoryResult` holds the reader, and `MzDataDirectoryResult`/`MvDataDirectoryResult` are thin wrappers that require their own runtime signature. An MV snapshot can no longer be read as MZ and vice versa.
- MV previously reported only `gameTitle`; `MvMetadataResult` now also reports `versionId`, `locale`, `currencyUnit`, `startMapId`, `startX`, `startY`, and bounded `partyMembers` (ids `1..50000`, capped at four). Verified against the public MV System data contract: MV uses `versionId` where MZ uses `systemVersion`, and all values are read from the top-level object only.
- RPG Maker AX was investigated and intentionally not added: no publicly verifiable file signature exists, and the repository forbids inventing format details.
- New `TestMvDataDirectory` suite covers the inventory, section counts, missing files, malformed and non-array JSON, malformed optional sections, encrypted assets, the verified System.json keys, and mutual signature refusal.
- Fresh canonical validation: `All 340 tests passed`; `dotnet build project/UniversalRPG.csproj --no-restore` 0 warnings and 0 errors; Godot import exit `0`.

## Latest completed chipset passability decoding slice (2026-08-31)

- This slice targeted the blocker that every previous note repeated: chipset passability was never decoded, so movement only used caller-supplied data.
- Verified against liblcf: `passable_data_lower` is LDB chunk `0x04` (162 bitflag entries), `passable_data_upper` is `0x05` (144 entries), and the liblcf defaults (15 lower / 31 upper) prove bits 0-3 are the four direction flags with bit 4 added on the upper layer.
- The parser now decodes both arrays from the LDB chipset section and reports an unexpected length as `<key>_unverified_length` with its offset instead of reinterpreting it.
- `Test_RealChipsetDecodesVerifiedPassabilityArrays` proves both pinned fixtures decode 162/144 entries and each contains both fully passable and fully blocked tiles, so the "distinguishing fixture" requirement is now satisfied.
- Still unverified and therefore not implemented: the per-direction bit mapping and the `BLOCK_B`..`BLOCK_F` tile-index constants/strides. The unblock condition is to read the `Passable` namespace and `BLOCK_*` values directly from the EasyRPG Player source, then implement the upper-then-lower resolution like `Game_Map::IsPassableTile`.
- Fresh canonical validation: `All 341 tests passed`; `dotnet build project/UniversalRPG.csproj --no-restore` 0 warnings and 0 errors; Godot import exit `0`.

## Next action

Continue with the next RM2K/2003 runtime slice only after its command/data semantics and regression oracle are verified. RGSS remains detection-only until a bounded Ruby implementation exists; its former metadata bootstrap is retained only as unregistered code and is not startable through the runtime selector.

## Audit note 2026-08-26

The completed-card audit found and corrected an unsafe RGSS capability claim: XP/VX/VX Ace were marked `Runtime` even though no bounded Ruby interpreter exists. `RgssPlugin` now exposes only `Detection | Parsing`, and `TestRgssRuntime` verifies selector refusal with `UnsupportedEngine`. Current canonical validation is `279/279`. The scheduler now stops source enumeration at its bounded event cap and diagnoses truncation.

## Completed K-012

- `ParseDatabase` decodes actors into typed entries (verified `ChunkActor` IDs; liblcf-default values for absent fields); switches/variables decode as id/name entries.
- Duplicate structure IDs rejected; unknown actor/entry fields retained per entry.
- Synthetic coverage: defaults, unknown retention, duplicate IDs, missing terminator. Real-fixture tests assert typed counts equal section counts on both TestGame LDBs.
- Validation evidence in `KANBAN.md`; suite now `165/165`.

## Failure log

- 2026-08-22 | K-015 | Signature: new typed-section regression -> `The given key was not present in the dictionary` in `Test_ParseDatabaseDecodesTypedSkillItemStateAndClassEntries`. Hypothesis: the test exposed that the parser only returned actors/switches/variables. Action: added verified scalar field contracts and typed result arrays for skills/items/states/classes. Result: focused behavior became green; full suite then exposed the separate dispatch-boundary regression below.
- 2026-08-22 | K-015 | Signature: full validation -> `No scalar field contract exists for LDB section 0xE/0x1F` in `ParseDatabase`, breaking real-fixture parsing and RM2K runtime initialization. Hypothesis: all LDB array sections were routed through the new scalar decoder. Action: restricted typed dispatch to sections with an implemented contract while retaining bounded framing/count parsing for the rest. Result: `166/166` tests and smoke validation passed.
- 2026-08-22 | K-015 | Signature: new combat-section regression -> `The given key was not present in the dictionary` in `Test_ParseDatabaseDecodesTypedEnemyTerrainAndAttributeEntries`. Hypothesis: parser output still exposed only the previous typed batches. Action: added verified scalar contracts and result arrays for enemies/terrains/attributes. Result: `167/167` tests and smoke validation passed.
- 2026-08-22 | K-015 | Signature: new presentation-section regression -> `The given key was not present in the dictionary` in `Test_ParseDatabaseDecodesTypedTroopAnimationAndChipsetEntries`. Hypothesis: parser output still exposed only the previous typed batches. Action: added verified scalar contracts and result arrays for troops/animations/chipsets. Result: `168/168` tests and smoke validation passed.

- 2026-08-22 | K-016 | Signature: existing `SmokeMzDetection` failed after MZ validation required `rmmz_managers.js`. Hypothesis: the new MZ boundary was correct but the legacy smoke fixture was incomplete. Action: added the manager signature to the synthetic smoke fixture. Result: full validation passed at `171/171`.
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
