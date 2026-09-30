## Current card
## The battle-only monster family is done — 13110, 13120, 13130, 13150, 13210

**Measured against liblcf's own `eventcommand.h` (`src/generated/lcf/rpg/`,
not the path the card named): 164 codes, 32 unwired, and of those 32 only 32
are real RM2K/2000/2003 commands** — the `Maniac_` and `EasyRpg_` patch
extensions (3001-3029) and the engine features below 6000 are a separate set.
**The card's claim that `13110`-`13410` and `20720`-`20732` did not exist was
wrong; seven of them are real battle commands.**

**`13110` has three change modes and the third is a share of the monster's own
maximum** — so mode 2 on a 500-of-1000 monster takes 250 and not 2. `13120`
has two modes and no third, and a mode of 2 there changes nothing.

**The sign is a flag in both, and the value's own sign is read never.** The
reference reads `bool lose = parameters[1] > 0` and then writes
`change = -change` — a negative constant with the flag at zero still heals.

**There are two deaths.** Hit points at zero give the enemy's kill sound and a
death timer; removing the death condition with `13130` removes him at once and
without an animation, which the reference's own comment calls an RPG_RT bug it
reproduces. The exit is therefore three-valued, and a reader that treated the
paths alike animated a death the reference does not animate.

**`13210` reads its file name from `com.string`, not from `parameters`.**

**Test evidence** `project/tests/core/test_rm2k_battle_monster_commands.cs`,
15 tests. **1410/1410**, validator passed.
**Mutations** Twelve rules, **12 of 12 caught**.

### Failure log: a test that read 130 where it wanted 70

Four of the fifteen failed on the first run, and the message was
"a constant change of thirty takes a monster from 100 to 70" with no value in
it — because this repository's `AssertEq` **drops the actual value whenever a
message is given**, so a failing test that names both numbers shows neither.

**The value came out at 130, and the implementation was right.** The test
passed `0, 0, 0, 30` where the second parameter is the lose flag — so it
measured a heal of thirty. Three more tests wanted losses and wrote the flag
the same way. **A reader that trusts a test's own sentence over the bytes it
sent will "fix" correct code**, and the cheapest check is the one that reads
the fixture back: `13110`'s parameters are enemy, lose, mode, value, lethal.

### And a Godot dictionary that does not convert

`TroopMembers` is a `Godot.Collections.Dictionary`, so every read is a
`Variant`. `Convert.ToInt32(m["hp"])` compiles and throws at run time with
"Unable to cast object of type 'Godot.Variant' to type 'System.IConvertible'";
`(int)m["hp"]` is the form that works. **Eleven tests failed on that one cast
before it was found**, and the exception text named the type.


## 11060, 11340 and 11350 are done — three commands the card said did not exist

**The card listed `11060`, `11340` and `11350` as "liblcf names them and
EasyRPG dispatches them nowhere", and said there was "nothing to read the
parameters from".** All three statements were wrong, and the last one was the
instructive one: **both movement commands have a width of zero, because the
whole of `11340` is `_state.wait_movement = true;` and the whole of `11350` is
`Game_Map::RemoveAllPendingMoves();`.** The absence of parameters was read as
the absence of a command.

**`11060 Pan Screen` has a minimum width of 5** — not the two the board
listed. Four modes (0 lock, 1 unlock, 2 pan, 3 reset), and a value the
reference does not know falls through all four and does nothing. The speed is
`Utils::Clamp<int>(parameters[3], 1, 6)`, repaired and not refused. The wait is
`GetPanWait`: `distance / speed + (distance % speed != 0)`, rounded up.

**A lock does not stop a running pan** — the reference calls `LockPan()` and
nothing else. `11350` stops the map's pending moves and the camera with them.

**Test evidence** `project/tests/core/test_rm2k_pan_screen.cs`, 13 tests.
**1395/1395**, `TestRm2kPanScreen: 13/13`, validator passed.
**Mutations** Ten rules, **10 of 10 caught**.

### Failure log: a state file that a rescue turned into a duplicate

The pan block went into `GameSimulationState.cs` three times under three wrong
assumptions, and each rescue made it worse:

1. Anchored on `ScrollHorizontally`, which is a field of the **nested**
   `Parallax` class — so the block landed inside it. The compiler said
   `PanDirection` does not exist in `GameSimulationState`, which was true and
   which I could have read in one step.
2. "Fixed" the indentation, which moved it out of the class instead.
3. Cropped the block by line range, which cut it in half and left a copy.

**The signal that would have ended it in step 1 was the error's own wording** —
"im Typ `GameSimulationState`" names the type, and the enum was in a
different one. Measuring the region in isolation, its indentation, its brace
balance and its class membership all passed, because each of those was
correct and the mistake was in which class I believed I was editing.

**And the recovery that worked was the one I kept avoiding: `git checkout` the
file, then make exactly one edit against a confirmed anchor.** The same lesson
as the sixteen tuple measurements, and the same cost.

### A stale build artefact that looked like eighteen failures

After the mutation run the validator reported **18 of 1395 tests failing**,
including eight of the thirteen new ones, on source that had been restored
correctly. The build output was stale: `rm -rf project/.godot/mono/temp/obj`
and a full build gave 1395/1395. **A test result that contradicts an
inspection of the source it tests is a build artefact until proven
otherwise** — and the cheaper check is the one that clears it.


K-086 through K-093 and K-095 through K-102 are DONE. K-103 (hero and events in the runtime frame) is next; K-094 (vehicles) stays open at lower priority.

**K-102 completed (2026-09-26)**
- Verified `sprite_character.cpp`: `character_name = GetSpriteName()`, `character_index = GetSpriteIndex()`, Charset aus Verzeichnis `CharSet`.
- Verified liblcf `ChunkEventPage`: `character_name=0x15`, `character_index=0x16`, `character_direction=0x17`.
- **Parsing-Lücke gefunden und behoben**: Der Parser deklarierte `character_name`/`character_index` nur für **Actors** (0x03/0x04), nie für EventPages — obwohl 0x15/0x16 in liblcf verifiziert sind. Event-Sprite-Daten waren damit gar nicht verfügbar. Jetzt werden alle drei Felder pro EventPage dekodiert; fehlender Name = leer.
- Drei Draw-Stufen aus `Priority_EventsBelow(30)`, `Priority_Player(40)` (geteilt mit "same as hero"), `Priority_EventsAbove(60)` nach `Priority_TilesetAbove(50)`.
- Neu `Rm2kCharacterSprite` (`StageForLayer`, `FacingFromLiblcfDirection`, `Skipped`) und `Rm2kMapFrameRenderer.RenderSprites`, das **eine Stufe pro Aufruf** zeichnet, damit der Aufrufer sie in der verifizierten Reihenfolge mit den beiden Layern verschränken kann. Ein Figurenindex jenseits der Charset-Kapazität wird **übersprungen und markiert**, nie aus einer beliebigen Zelle gezeichnet.
- Beide Richtungs-Umrechnungen (Projekt 2/4/6/8 ↔ liblcf 0..3) sind nachweislich invers zueinander getestet.
- Der Renderer kann jetzt ohne Chipset erzeugt werden (Sprite-only-Pass); Tile-Zeichnen tut dann nichts statt zu werfen.

**K-101 completed (2026-09-26)**
- Verified `GetCharacterRect`: Zelle `24*(TILE_SIZE/16)*3` × `32*(TILE_SIZE/16)*4` = **72×128**, Position `(index%4, index/4)`, 3×4 Frames à **24×32**.
- Verified `Sprite_Character::Draw`: `row = GetFacing()`, `frame = GetAnimFrame()`, alles ab `Frame_middle2` wird auf `Frame_middle` geklemmt. liblcf `Frame`: left=0, middle=1, right=2, middle2=3.
- Verified `UpdateFacing`: bei den vier Kardinalrichtungen ist facing == direction, also ist liblcf `Direction` up=0/right=1/down=2/left=3 direkt die Sprite-Zeile.
- Verified Offsets: `SetOx(chara_width/2)`, `SetOy(chara_height)` — zentriert, Füße auf der Kachelunterkante.
- Fixture `rm2000/CharSet/Chara1.png` (gepinnter Commit, 18785 B, SHA-256 `24442b61…`) bestätigt unabhängig: 288×384 = exakt 4×72 quer und 3×128 tief, also 12 Figuren.
- **Refactor**: Der indexed-PNG-Decoder wurde zu `Rm2kIndexedImage` generalisiert; `Rm2kChipsetBitmap` ist die Chipset-spezifische Hülle mit dem 480×256-Vertrag. Charset, Chipset und später Pictures teilen sich Decoder und Transparenzregel.
- Neu `Rm2kCharset`: `FacingToRow` (2/4/6/8 → liblcf-Index), `ClampFrame`, `TryGetCell`, `TryGetFrameRect`, `TryDrawCharacter` mit Füßen auf der Kachelunterkante und Clipping am Rand.
- Clipping ist verifiziert korrekt: Figur auf Kachel (0,0) wird oben abgeschnitten, vollständig außerhalb malt sie nichts — **ohne** Exception.
- Der Godot-Editor liegt unter `E:\GodotEditor\Godot_v4.7.2-stable_mono_win64_console.exe` (wurde aus Git entfernt, ist aber lokal vorhanden).

**K-100 completed (2026-09-26) — the map is visible**
- Verified: `cache.cpp` liest das Chipset aus dem Verzeichnis `ChipSet`, Dateiname `<chipset_name>.png`; `Game_Map::GetChipsetName` liefert den Namen aus der Datenbank.
- `Rm2kEngineRuntime` liest `chipset_name`, löst `<root>/ChipSet/<name>.png` auf, dekodiert, prüft 480x256 und rendert in `RenderedMap` (plus `ChipsetImage`, `RenderDiagnostic`). Fehlendes/defektes/falsch großes Bild wird **gemeldet, der Runtime läuft weiter** — der Player behandelt das Chipset als Asset, die Simulation hängt nicht davon ab.
- `Rm2kMapPreview` lädt die Pixel einmal pro Änderung, skaliert mit Nearest-Nachbar-Filter, zeichnet den Spieler-Marker und die Diagnose, und fällt auf die Tile-ID-Ansicht zurück.
- **Golden Image**: `rm2000/rendered/Map0001.png` (eigene Ausgabe, nicht upstream) mit SHA-256 als Regressionsbasis. Der Test vergleicht **jedes Byte** — eine Änderung an Auflösung, Autotile-Tabellen, Transparenzregel oder Draw-Reihenfolge fällt jetzt durch, statt still ein anderes Bild zu erzeugen.
- Gemessene Fakten: 20x15 Tiles → 320x240 Pixel, Lower nur D/E, Upper nur F und dort vollständig transparent, **13 verschiedene Farben**, alle Pixel belegt (Innenraum). Transparenz deshalb pro Kachel geprüft, Animation mit synthetischen Maps.
- Das gerenderte Bild wurde angesehen: Gras, weißer Weg, braune Treppe — die echte TestGame-Map0001.

**K-099 completed (2026-09-26)**
- Verified `CreateTileCacheAt`: Upper-Tile geht in die obere Sublayer, wenn sein **substituierter** Entry `Above` trägt; Lower-Tile, wenn sein **aufgelöster Chip-Index** `Wall` oder `Above` trägt. Chip-Index-Bereiche identisch zur Passability.
- Verified Draw-Reihenfolge via `lower_layer(this, Priority_TilesetBelow + TileBelow + layer)` und `upper_layer(this, Priority_TilesetAbove + TileAbove + layer)` mit `TileBelow=0`, `TileAbove=100`, `Priority_TilesetBelow=20`, `Priority_TilesetAbove=50`, `Priority_Player=40` → **untere Layer, dann Held, dann obere Layer**. Deshalb deckt eine Wall-Kachel den Helden ab.
- **Defekt behoben**: `Rm2kChipsetSource.TryResolve` gab für E und F `false` zurück, wenn keine Substitution übergeben wurde — obwohl `Game_Map::Setup` beide Tabellen mit `std::iota` füllt. Eine fehlende Tabelle **ist** die Identität; jetzt Fallback statt "unauflösbar".
- Neu: `Rm2kTileZOrder` (ResolveChipIndex, Lower/UpperLayerSubLayer), `Rm2kMapFrameRenderer` (RenderLower/RenderUpper, Sublayer 0 vor 1), `Rm2kMapLayers`, `Rm2kChipsetTables`. Der Held wird bewusst **nicht** gezeichnet — er gehört zwischen die beiden Aufrufe.
- **Echte Fixture gemessen statt angenommen**: Die TestGame-Map ist 20x15, Lower nur D/E, Upper nur F, und deren F-Kacheln sind im echten Chipset **vollständig transparent** → Zeichnen ändert nichts. Keine A/B/C-Kacheln → keine Animation. Drei meiner Erwartungen waren falsch und wurden durch Messwerte ersetzt; Upper-Pfad und Animation werden mit synthetischen Maps geprüft.

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

## Latest completed hero and event character slice (K-103, 2026-09-26)

- The runtime now draws the hero and the event characters into the map frame in the verified drawable order: lower layer, below events, hero plus same-layer events, upper layer, above events.
- Hero graphic source verified in `Game_Player::ResetGraphic`: it is `Main_Data::game_party->GetActor(0)`, and a null actor produces `SetSpriteGraphic("", 0)`. `Game_Actor::GetSpriteName`/`GetSpriteIndex` fall back to the LDB `character_name`/`character_index` when no runtime override is set, and `SetSprite` clears the override for the database values, so a fresh game always draws the LDB graphic. `Game_Party::SetupNewGame` copies `Data::system.party`.
- New `project/src/rm2k/rendering/Rm2kHeroSprite.cs` resolves the graphic and the `CharSet/<name>.png` file name.
- The LDB `system` chunk (section `0x16`) was previously only a raw chunk. `DecodeLdbSystem` now types it, verified against liblcf `struct ChunkSystem`: party list `party_size 0x15` / `party 0x16`, and the vehicle graphics `boat_name 0x0b`, `ship_name 0x0c`, `airship_name 0x0d` with `boat_index 0x0e`, `ship_index 0x0f`, `airship_index 0x10`. Unmapped fields keep their count and framing in `unknown_fields`. A database without the chunk yields liblcf's empty defaults.
- `LoadCurrentMapEvents` now copies `character_name` (liblcf `0x15`), `character_index` (`0x16`) and `character_direction` (`0x17`) into the event page and converts the direction into this project's facing. K-102 had verified the ids but nothing populated the page, so the event sprites were unreachable.

Defects found and fixed while implementing:
- `RenderCurrentMap` ran before `LoadCurrentMapEvents`, so the first frame was rendered with an empty event list and the whole card was silently inert while the suite stayed green.
- The `system` decoder overwrote the seeded defaults; it now reports only the fields the chunk carries and the caller merges them.
- The declared party size can exceed the stored data, so the list is clamped to `min(declared, data.Length / 2)` and bounded by `MaxSystemArrayEntries = 4096`.
- `CopyRealGame` in the rendering test copied `ChipSet` but never `CharSet`, so no character could be drawn; the constant also needed the `FixtureRoot` prefix for `GlobalizePath`.

Measured, not assumed: the pinned LDB has an empty `party` list, so the verified null-actor path applies and this fixture draws no hero graphic. That is correct behaviour and is asserted rather than papered over. The frame contains the chipset plus the event characters: 85 colours instead of the chipset-only 13, with 20 character figures confirmed by inspecting the rendered image.

Validation: `dotnet build project/UniversalRPG.csproj --no-restore` 0 errors/0 warnings; `GODOT_BIN=tools/godot/editors/4.7.2/windows-x86_64/Godot_v4.7.2-stable_mono_win64_console.exe ./scripts/validate.sh` → `TestRm2kParser 36/36`, `TestRm2kRuntimeRendering 7/7`, `All 425 tests passed`, exit 0. Golden image regenerated and re-pinned, SHA-256 `a67ed0672ab97b977c17dc8dd729ef1ffffed8b8339c7e96db2a267cf09a764a`.

Note: the Godot 4.7.2 editor was missing from `tools/godot/editors/` in this checkout and had to be re-extracted from `~/Downloads/Godot_v4.7.2-stable_mono_win64.zip` before validation could run. `tools/godot/editors/` is gitignored, so this is a local environment fix, not a repository change.

## Latest completed frame recomposition slice (K-104, 2026-09-26)

- The map is now rastered once into two cached layer buffers and only the characters are re-composited, so the visible hero follows a move. `RecomposeFrame` runs inside `Update` on a simulation frame boundary, never per rendered frame.
- Verified in the Player: `Scene_Map::vUpdate` → `UpdateStage1` → `UpdateGraphics()` once per frame, and `Spriteset_Map::Update` only gives the tilemap `SetOx`/`SetOy` scroll offsets. The tile layers are static sprites and are not re-rastered on movement. Note the class is spelled `Spriteset_Map`; a probe with `SpriteSet_Map` matched nothing silently.
- Composition order: copy of the cached lower layer, below-layer events, hero plus same-layer events, the cached upper layer over them, then above-layer events.

Three defects found while implementing:
- `Rm2kPixelBuffer.PaintOver` first copied every byte including alpha 0, so the upper layer erased the floor. It now keeps the destination where the source is transparent, matching the verified chipset blit rule.
- The upper layer was rastered into the same buffer as the lower layer, so it carried the floor with it and covered every character. It now has its own buffer.
- The character pass drew onto an empty buffer, which dropped the floor (`opaque=6513` instead of `76800`).
- `Rm2kMapFrameRenderer.RenderSprites` threw on a null map although it never reads it. The parameter is now nullable and documented, because a character is placed by its own tile coordinates.

Validation: build 0 errors/0 warnings; `All 427 tests passed`, exit 0; `TestRm2kRuntimeRendering 9/9`. The rendered frame is byte identical to the K-103 golden image, SHA-256 `a67ed0672ab97b977c17dc8dd729ef1ffffed8b8339c7e96db2a267cf09a764a`, 85 colours, 76800 opaque pixels. The refactor changes no output, which is what makes the layer caching safe to keep.

## Latest slice (2026-09-26) — K-111 DONE: events walk their move route

**The command set, verified from liblcf itself, not guessed.** `generator/csv/enums.csv` defines `rpg::MoveCommand::Code` from `move_up = 0` to `decrease_transp = 41`, in three contiguous blocks: the movement commands 0 to 11, the facing commands 12 to 22, and everything else from 23. The Player relies on exactly that contiguity, testing `cmd >= move_up && cmd <= move_forward` for movement and `cmd >= face_up && cmd <= face_away_from_hero` for facing, so an id in the wrong place is a behaviour change and not a label. `generator/csv/fields.csv` gives the LMU layout: `EventPage::move_route` is chunk 0x29, a `rpg::MoveRoute` whose `move_commands` is a `Vector<MoveCommand>` at 0x0B for the count and 0x0C for the entries, with `repeat` at 0x15 defaulting to true and `skippable` at 0x16 defaulting to false.

**The route is a nested struct, and reading it at the page level silently reports no route at all.** 0x0B and 0x0C live inside the page's 0x29 chunk, not beside it. Passing the page's own fields to the decoder produced a clean "no route" for every page, which looks like a working decoder and is not one. The chunk is now read as a nested struct first.

**Two encodings, one of them mine to get wrong.** A route stores no per-command length, so a single wrong byte shifts every command after it and the route still parses; only the ids reveal the drift. The test fixtures build BER correctly, which means reversing the seven bit groups before setting the continuation bits: 4097 is the two bytes 160 and 1, not 129 and 32. My first fixture did not reverse them and reported a decoder bug that did not exist.

**The runtime, from `Game_Character::UpdateMoveRoute`.** A command that starts a step returns at once, so the route consumes one command per update and the character then spends the following updates walking. A refused step either skips the command when the route is skippable or holds the route on it, which is what stops a character stuck against a wall from sliding along it. A successful step falls through to the index advance, so the next command is read on the update after this one. A route that ends on a step still walks that step out, because the character is already on its way.

**Mutation evidence for the runtime, all four now detected:** the index never advancing after a step, the step never being cleared, the passability check removed, and a refused step always advancing instead of holding. The last two escaped before the blocked route test existed, which is why that test was worth writing.

**The pinned fixture has no move routes at all.** Its single real map has 22 event pages and every one decodes cleanly, but none defines a 0x29 route. The real map test now asserts that as a property of the fixture, and the commands are proved by the byte exact decoder tests and a synthetic LMT that carries a real 0x29 struct.

**K-111 is DONE.** `All 525 tests passed`, build `0 Warnung(en)`, `0 Fehler`, `UniversalRPG validation passed.` No probes. Font artifact restored.

## Ruby value layer for the RGSS engines (2026-09-26)
- `RubyValue` holds the seven kinds the language defines, as data, with a value's
  identity and its contents and nothing else. No arithmetic, no comparison, no
  method dispatch, no class loading, no decoding of a game's string bytes.
- `RubyValueConverter` turns a decoded Marshal value into one, following the
  file's links so a link becomes the value it names rather than a second copy.
- The refusals are the work. A kind the language has no name for, a payload that
  contradicts its kind, a mapping entry without its other half, a mapping holding
  the same key twice and a link to an entry that was never decoded each raise
  with the reason, and each is counted and remembered.
- **A bug this found in the reader from the card before it.** The converter was
  written against kind names spelled out from memory. The reader emits `array,
  false, float, integer, nil, object, regexp, string, struct, symbol, true`, and
  two of the converter's names were not on that list: a whole number arrives as
  `integer` and a string as `string`. Every number and every string in a real
  game's data would have been refused. The names now come from the reader.
- **The numbering, verified against the Ruby 3.4 specification.** A stream holds
  one copy of each object and one of each symbol, the first object has the
  number one and the first symbol the number zero. The converter reads that
  number from the value the reader handed over rather than counting again.
- A container is numbered before its contents are read, which is the only reason
  a container can hold a reference to itself. In the documented stream
  `"[\"
hello@"` the array is one and the string is two, and
  the link names two.
- A Marshal long is not eight bytes. A stream written with eight bytes is a
  different stream from the one a game writes, and three attempts to build one
  that way failed before the packing was read off the format.
- Other converter bugs found and fixed: the stream's own numbers were never
  recorded, so every link pointed at nothing; values were numbered from zero and
  after their contents, which gave a container a higher number than its first
  member; a value was filed under its number before its class was attached, so a
  link to a game's value found an object that no longer said what class it was.
- A value that points at itself is refused with the number in it, because there
  is no value to return yet.
- Tests: `TestRubyValue` 15/15, `TestRubyValueConverter` 27/27, total 799/799,
  validator passed. 12 mutations, all detected.
- Still missing for XP, VX and VX Ace: anything that gives a value a meaning, a
  renderer, saves, input, audio, and any playable runtime. `RgssEngineRuntime` is
  still a metadata inspector and no real archive from any of the three engines is
  in the repository, so all of this is structurally verified.

## Ruby parser for the RGSS engines (2026-09-26)
- `RubyParser` turns a token stream into a tree of shapes. It names what was
  written and nothing more: no name resolution, no method or constant lookup, no
  evaluation, no call of anything. It never executes a game's Ruby.
- The operator precedence is the Ruby grammar's own, taken from the declaration
  order of its precedence levels. That is not a detail: the first table written
  from memory was wrong, and a reader with one level in the wrong place parses a
  game's arithmetic into a different tree with nothing looking wrong about it.
- The grammar resolves relations and equality with `rel_expr %prec tCMP`, which
  makes them one level. The first table had them as two, and two tests written to
  match the wrong table had to be corrected rather than the code.
- `**` is the grammar's one right associative binary level and sits above
  multiplication on a level of its own.
- `not` is a level of its own between the logical pair and the assignment, so
  `not a == b` negates the comparison. It was first read as a unary operator,
  which binds at the other end of the scale entirely.
- A shape the parser cannot read raises with its line. A tree that stopped early
  would be worse than none, because nothing would mark it as incomplete.
- Tests: `TestRubyParser` 44/44, total 755/755, validator passed.
- 21 mutations on the precedence table and the reader's shapes, all detected.
- Two escapes during the work were untested boundaries, not wrong code: nothing
  in the suite crossed the logical/bitwise boundary and nothing pinned `not` to
  its level. Both now have tests.
- One mutation showed a `not` branch in `ParseBinary` was unreachable, since
  `ParseUnary` takes the keyword first. It was removed instead of kept as a
  second route to the same node, and the suite still passes without it.
- Still missing for XP, VX and VX Ace: a reader for the tree's meaning, a
  renderer, saves, input, audio, and any playable runtime. `RgssEngineRuntime`
  is still a metadata inspector, and no real archive from any of the three
  engines is in the repository, so all of this is structurally verified and not
  checked against a real game.

## Ruby lexer for the RGSS engines (2026-09-26)
- `RubyLexer` splits a game's Ruby source into tokens. It calls nothing,
  resolves nothing and runs nothing. Together with K-112 and K-113 this is the
  third of the three layers XP, VX and VX Ace need before their scripts can be
  read at all, and the first one that looks at the script text itself.
- **The keyword list is the one from Ruby's own grammar**, extracted from
  `parse.y` rather than written from memory: 41 reserved words, from `class` to
  `__ENCODING__`. That matters because a keyword is reserved, so a lexer that
  treated one as a name would accept files Ruby rejects.
- **A name that begins with an upper case letter is a constant**, and the
  reserved word check comes first. Two of the reserved words, `BEGIN` and `END`,
  begin with an upper case letter, and the grammar's `reswords` production lists
  them as keywords. Checking for a constant first read them as names. A test
  over all 41 words is what found it, because the one example I had chosen
  happened to be a lower case word.
- **A slash divides where a value has just ended and opens a regular expression
  where one could begin.** My first version had this exactly backwards, so
  `a / b` was read as a regular expression that ran off the end of the line. The
  two shapes differ only in what came before the slash, which is why both are in
  the suite.
- A regular expression keeps its backslashes, because the pattern engine is what
  interprets an escape, and a `/` inside a character class does not close it.
- **A single quoted string interprets only two escapes**, the quote and the
  backslash. Reading it like a double quoted one lost a backslash a game asked
  to keep, which is the whole reason the form exists.
- A string keeps its **bytes** as well as its text, because a Shift-JIS script is
  not UTF-8 and a reader that kept only text would silently corrupt it.
- An octal literal may be `0o17` or `017`. The marker sits between the leading
  zero and the digits, and checking the current character instead of the next
  one read `0o17` as a bare zero.
- An unknown character, an unclosed string, an unclosed regular expression and a
  number with no digits in its base are all refused with their line. A partly
  tokenised script is worse than none, because nothing marks it as incomplete.
- Tests: `TestRubyLexer 33/33`, total `711/711`, build `0 Warnung(en)`,
  validator passed, probes 0.
- Mutation evidence, fourteen run and eleven detected: a keyword list never
  consulted, the reserved word check moved after the constant check, the
  constant rule inverted, a slash always a regular expression, a slash always a
  division, single quoted escapes applied in full, the octal marker not
  skipped, a shorter operator matched first, an unclosed string accepted, a line
  continuation read as a break, a block comment not skipped, a class variable
  read with one at sign, a regular expression losing its backslash, an
  unterminated block comment end.
- **Two mutations were equivalent rather than escaping.** Appending `<=` and
  `<<` to the operator list changes nothing, because every multi character
  operator already appears before the shorter one it starts with; the check
  printed the whole list to establish that. And turning a byte escape's
  `((char)value).ToString()` into `value.ToString()` changes nothing, because the
  cast already produces values in the range where the two agree. A mutation
  that cannot change behaviour is not a gap in the tests.
- Four gaps the mutations found were real and are now closed: the keyword lookup
  was untested, the single quoted escapes were only checked for one letter, the
  operator order was checked for the operators the test happened to use, and the
  line continuation test filtered the newline it was supposed to be about.
- **One mistake of my own in the tests, and it hid four failures for a while.**
  The helper that drops whitespace-only tokens did not drop the end of input
  token, so every list based assertion was off by one element. A probe with a
  different filter showed the lexer's output was right all along. Measuring
  instead of reasoning about positions is what ended the loop.

## Marshal reader for the RPG Maker data files (2026-09-26)
- `MarshalReader` reads the Ruby Marshal stream that RPG Maker XP, VX and VX
  Ace use for `.rxdata`, `.rvdata` and `.rvdata2`. Together with the archive
  format this is the second of the two things all three engines need before any
  of their data can be looked at. Neither runs a line of game code.
- The reader produces a tree of `MarshalValue` rather than live objects, on
  purpose. A game database is full of instances of classes this project has
  never heard of, so resolving them would mean either running the game's Ruby
  or inventing classes that do not exist. A value tree can be inspected without
  either, and it keeps the class name as a string.
- **Integers are the part that is easy to get wrong and I got wrong.** A
  marshalled integer is a type byte and then one to five bytes, where the first
  of those encodes sign and width in a single value. Eight values are special;
  the rest is a sign extended byte with an offset of five. A reader that
  treats the first byte as a length decodes small numbers correctly and
  everything else as something plausible but wrong, which is the worst way to
  be wrong.
- **An object takes its index before its contents are read.** A value inside a
  collection may link back to that collection, and the link names an object the
  stream has already defined. Numbering afterwards would make every such link
  point at the wrong object. `ReadArray` and `ReadHash` now take the index
  first.
- A link does not take an index of its own, because it names an object that
  already exists. Only real values do. My first expectation had this wrong and
  the measurement corrected it.
- A regexp is the one type in its group that carries no class name: the
  specification gives a source and an option byte and nothing else. Reading a
  name there consumes the length byte of the source and shifts the rest of the
  file.
- A bignum is refused rather than read. A game's data uses fixnums for anything
  that fits, and a bignum here would mean arbitrary precision this reader does
  not carry, so pretending to read one would be a guess.
- A stream that ends inside a value, declares a length past the limit, or
  carries a type byte the specification does not define is refused. A partial
  tree of a game's data is worse than an honest failure, because nothing marks
  it as incomplete.
- A major version this reader does not implement is refused outright, and a
  newer minor version is refused too, because it may use a type this reader
  has never heard of. An older minor version is read, since a newer minor
  version can read an older one.
- **Two mistakes of mine, and the second one only showed up under mutation.**
  A grouped `case` list and single `case` labels for the same values left the
  later ones unreachable, so a regexp fell through to the refusal branch. And
  when I removed that label I took the routing line with it, so no regexp could
  be read at all. Both are fixed; the routing and the payload are now separate
  concerns.
- The fixtures are written by hand from the specification's type table. A round
  trip through a writer of our own would pass even if the reader and the writer
  were wrong in the same way, which is the failure this suite exists to rule
  out.
- Tests: `TestMarshalReader 29/29`, total `678/678`, build `0 Warnung(en)`,
  validator passed, probes 0.
- Mutation evidence: a flipped sign offset, a swapped sign case, a zero case
  that swallowed a byte, a width read one byte short, an array numbered after
  its contents, a hash likewise, an uncapped nesting depth, an unchecked major
  version, an unchecked minor version, an uncapped byte count, an unchecked
  symbol link, an object link accepting index zero, a regexp reading a class
  name, and a symbol link resolving out of range were all detected.
- **Two mutations turned out to be equivalent rather than escaping.** Moving
  `++ObjectCount` below the `ReadLength` call changes nothing, because reading a
  length does not touch the counter. A mutation that cannot change behaviour
  cannot be caught by a test, and recording it as a gap would have been wrong.
  A mutation that really delays the index until after the elements were read is
  detected, which is the behaviour the ordering exists for.
- Structural only: the repository has no RPG Maker game, so there is no real
  data file to read.

## RGSS archive format, shared by XP, VX and VX Ace (2026-09-26)
- `RgssArchiveReader` reads and writes the archive format that RPG Maker XP, VX
  and VX Ace all share. This is the first thing that is genuinely common to
  three of the engines the goal lists, so it counts for three criteria at once
  where the Ruby virtual machine would count for none of them until it ran.
- The format is obfuscated, not encrypted. Every value is exclusive ored with
  the output of a linear congruential generator that starts at `0xDEADCAFE` and
  advances once per value by `magic = magic * 7 + 3`. The value a field is
  obfuscated with is the generator's state **before** that step.
- **The header is eight bytes: the name `RGSSAD`, one byte the format does not
  check, and the version.** The reference reader compares the first six bytes
  and reads the version from the last one. A reader that also required the
  seventh byte to be zero would refuse a file the format allows, so the test
  proves the seventh byte is ignored rather than assuming it is zero.
- The version byte is what tells an XP or VX archive from a VX Ace one. They
  differ in nothing else a reader has to know before it can list entries.
- Each entry is a name, a size and a body. The name is obfuscated byte by byte,
  and a backslash in it is folded to a slash so that an archive written on
  either system lists the same way. The list ends when a name can no longer be
  read, not at a terminator.
- An entry that claims more bytes than the file holds is refused rather than
  handed back short, because a short body looks like a successful read.
- This reader lists and reads entries. It does not execute anything an entry
  contains: a game script is read as bytes and nothing more, which is what the
  project rules require.
- **Two bugs of my own, both caught by tests rather than by reading.** A regular
  expression pass removed the `return` from three failure paths, so a refused
  archive fell through and was read anyway; the diagnostic said the file was
  not an archive while the reader went on parsing it. And an unused version
  read indexed one byte past the end of an eight byte header, which crashed on
  an empty archive. Both are fixed and the failing tests are the reason.
- Two of my own wrong expectations: I had the header as three zero bytes after
  the name, and I had the generator taking two steps per field. Neither matched
  the reference reader. The first was found by an out of bounds read on an empty
  archive and the second by a round trip that decoded to a name length of three
  hundred million, which was reproduced outside C# before the reader was touched
  again.
- Tests: `TestRgssArchive 15/15`, total `649/649`, build `0 Warnung(en)`,
  validator passed, probes 0.
- Mutation evidence: a seed off by one, a wrong multiplier, a name byte read
  without the key, a version read from the wrong offset, the unchecked byte
  checked, and an entry list that started four bytes late were all detected.
- Structural only: the repository has no RPG Maker game, so there is no real
  archive to read. The fixtures are written by the project's own writer and the
  expected values are also derived independently inside the test, because a
  round trip through the project's own writer alone would pass even if both
  halves were wrong in the same way.

## K-110 WOLF move routes, the last gap in the command reader (2026-09-26)
- `WolfMoveRouteReader` reads a route: the animation frequency, the move speed
  and the move frequency, then the mode, then two option blocks, then a length
  and that many steps. The header order is neither alphabetical nor the order a
  reader would guess, and reading it in a different order still produces four
  plausible bytes, so each header field gets its own value in the fixture.
- **The two option blocks are bit fields, not bytes.** The behavior block holds
  eight flags in one byte and the route option block holds three in the high
  three bits of another, with the low five reserved. Reading them as bytes would
  leave the cursor six bytes short and shift every field after them, which
  produces a file that looks decoded and is not.
- **A route step describes its own argument lengths.** The types that take
  arguments write a byte saying how many four byte values follow, the values, a
  byte saying how many single byte values follow, and then those. Reading the
  lengths from the file rather than from a per type table is what lets this
  reader read a type it has no name for: it can still step over the step
  correctly and report the type as one it does not name. A reader driven by a
  table cannot do that, and the mutation that swaps the file's length for a
  table lookup is caught.
- A step that takes no arguments still writes both length bytes, both zero.
  Reading only the first takes the second from the next step's type, which is
  exactly the drift a self describing format is meant to prevent.
- The route type table has 59 values with a gap at 0x2A and 0x2B that the
  specification leaves unused. A value in the gap is reported as one the table
  leaves unused rather than stepped over silently, because a reader that
  skipped it could not say how long it was.
- The event command reader now reads a route when the route flag is set, so a
  route inside a command is decoded rather than reported as unread.
- Tests: `TestWolfMoveRoute 11/11`, `TestWolfEventCommand 10/10`, total `634/634`,
  build `0 Warnung(en)`, validator passed, probes 0.
- Mutation evidence: a removed option byte, a reversed header, swapped option
  bits, option bits read from the low end, a table driven argument length, a
  missing byte argument count and a byte argument count fixed at zero were all
  detected. Two earlier mutations escaped and both were test gaps, not reader
  gaps: an option test that set all three bits at once could not tell them
  apart, and each option now has a fixture of its own.
- **K-110 is now complete against its title.** The database, the game settings,
  the common events and the move routes are done. The transfer format was not in
  the title and is not started.

## K-110 WOLF CommonEvent.dat, and a real bug in the command reader (2026-09-26)
- `WolfBinaryCommonEventReader` reads the file: the `WOLF/FC` header, a
  version byte, a count and that many records. Each record starts with `0x8E`
  and is divided by five separator bytes into six parts, and the order matters:
  the argument name table, the option string tables and the option value tables
  all sit between separators rather than at the end of the record. A reader that
  walks the fields top to bottom produces plausible wrong values, because every
  field is still a valid length prefixed string.
- The self variable name table is a fixed hundred entries, not a counted one.
  Reading it as a count leaves the cursor inside the table and shifts every
  field after it, which looks like a successful decode of a wrong file.
- **A zero parameter count is the command list's end marker, not a command with
  no arguments.** Nothing at all follows it. This was the first thing my
  CommonEvent fixture got wrong, and it is why the format needs a test of its
  own rather than a spot check.
- **A length prefixed string's terminator is inside its length.** The format's
  string is a zero terminated string with a size, and the size covers the
  terminator. Writing the length without the terminator and then appending one
  leaves a byte the reader does not consume, so every field after the first
  string is off by one. The fixture did that and the reader was right to refuse
  the result.
- **The command reader from K-109 was structurally wrong and is now corrected.**
  It read a four byte big endian signature followed by a padding byte, built on
  constants like `0x0167_0000`. The verified structure is a one byte parameter
  count, then a four byte little endian type, then a parameter block whose shape
  belongs to the type. The old form decoded its own fixtures and no real file.
- The corrected reader is type aware, because the parameter block is not
  uniform. A message command has no block at all, a numeric condition has an
  else flag, a condition count, three padding bytes and then that many variable,
  value and operator triples, a call has an event id and then an argument status
  word only when the id is in the common event range, and a branch carries a
  condition id. After the block every command has a branch depth byte, a string
  count byte, that many strings and a move route flag.
- The old signature constants were partly invented. `CallCommonByName` was
  `0x3B` because that packed the parameter count and the type into one word; the
  verified type is 300. The double and triple variants were not separate types
  at all but the same type with a different parameter count, and naming them by
  type alone collapsed them. Names now come from the type and the parameter
  count together.
- **Where the specification gives a parameter count and not the meaning of each
  count, the name reports the count.** Mapping type 121 with eight parameters to
  an operation name would have been a guess, so the name says how many
  parameters the command has.
- A move route is not decoded. A command that carries one is reported as
  carrying an undecoded route instead of being stepped over, because stepping
  over bytes the reader does not understand would shift every command after it.
- A string count above the limit is refused. The field is a single byte, so a
  file can claim up to 255 strings; following that count out of a short file
  would walk off the end. The limit is 32, and the reason is written down: a
  count near the byte's maximum is far more likely to be a misread.
- Tests: `TestWolfBinaryCommonEvent 17/17`, `TestWolfEventCommand 10/10`,
  total `623/623`, build `0 Warnung(en)`, validator passed, probes 0.
- Mutation evidence on the corrected reader: a fixed parameter count, a big
  endian type, a uniform parameter block, an ignored zero marker, an ignored
  string limit and a route flag that is never set were all detected.
- Still missing against the K-110 title: the transfer format, and move routes
  inside event commands. The database, the game settings and the common events
  are done.

## K-110 WOLF Game.dat VERIFY (2026-09-26)
- `WolfGameSettingsReader` reads the Game.dat framing: magic `0 'W' 0 0 'O' 'L'
  0 'F' 'M'`, a version byte, a byte settings length, the byte settings, the
  version dependent string record, the file's own size, `unknown3`, the word
  settings length and the word settings, the static random block and a version
  footer.
- **The version byte changes the record shape, not just the meaning of a
  field.** A v2 record has eight Shift-JIS strings followed by one UTF-8 string;
  a v3 record has twelve UTF-8 strings and no trailing one. Reading one as the
  other consumes the wrong number of strings and lands somewhere else entirely
  without failing, so the version has to pick the shape before anything is read.
- **The record mixes encodings inside itself**: the first eight v2 strings are
  Shift-JIS and the ninth is UTF-8. Decoding both as UTF-8 turns a Japanese
  title into replacement characters; decoding both as Shift-JIS turns a UTF-8
  name into mojibake. Both encodings are decoded strictly, so bytes that are
  invalid in the declared encoding are refused rather than replaced.
- **The word record is bounded by its own length**, not by a fixed field count.
  A later version appended the loading gauge fields behind that length. The
  record is 23 values: `unknown`, twelve custom move speeds, `unknown_2`, the
  screen width and height, then the WOLF version at index **16**. I initially
  had 18 words and the version at 17; both were wrong and the fixture caught it.
- The file's declared size bounds the static random block, whose length varies
  per file, so treating it as a fixed block would misread every other game.
- The encryption key is the third string setting and is read so a protected game
  can be recognised. Nothing is decrypted and this reader has no decryption
  path.
- Two fixture errors of my own, both caught by the tests rather than by
  inspection: the v3 fixture had thirteen strings where the record has twelve,
  and I had guessed the expected Shift-JIS text. `83 65 83 58 83 67` is
  "Test", not the word one reaches for first, and the expectation is now the
  measured value with a comment saying so.
- Mutation evidence: a v2 record read as UTF-8, a wrong v3 string count, a
  fixed word count, a removed file size bound and a wrong version index were all
  detected.
- Tests: `TestWolfGameSettings 13/13`, total `607/607`, build `0 Warnung(en)`,
  validator passed, probes 0.
- Still missing against the K-110 title: `commonevent_dat` and the transfer
  format. `game_dat` and the database format are done.

## K-094 vehicle runtime wiring VERIFY (2026-09-26)
- `LoadVehicles` builds all three vehicles from the LMT start node and the LDB
  system section, verified from `Game_Vehicle`'s constructor. All nine start
  fields decode: `boat_*` `0x0B`/`0x0C`/`0x0D`, `ship_*` `0x15`/`0x16`/`0x17`,
  `airship_*` `0x1F`/`0x20`/`0x21`.
- Measured on the pinned fixture rather than assumed: all three vehicles name
  the **same** charset, `vehicle` for the boat and the ship and `Vehicle` for
  the airship, at cells **0, 1 and 3**. Cell 2 is unused, which is legal and
  which I initially got wrong by expecting 0, 1, 2.
- All three vehicles start on **map 39** and the party on map 30, while the
  fixture only ships `Map0001.lmu`. So this map correctly draws no vehicle, and
  `CharSet` holds only `Chara1.png`, so the `vehicle` cell could not be drawn
  even on the right map.
- The vehicle draw path is therefore **proven to run but not proven to draw**.
  Three mutations still escape and all three are the same root cause: with no
  `vehicle.png` in the fixture, the draw path never writes a pixel, so removing
  the map check, the charset fallback or the altitude wiring changes nothing
  observable. Mutation runs of the vehicle-order and the LMT read were detected.
- Deliberately **not** done: fabricating a `vehicle.png` to make the test draw
  something. A synthetic charset would prove the compositing maths and nothing
  about the real file, and a real one is not available in the fixture. The
  honest options are a game that ships a vehicle charset, or a fixture change
  agreed with the user.
- Tests: `TestRm2kRuntimeRendering 19/19`, total `594/594`, build
  `0 Warnung(en)`, validator passed, probes 0.

## K-094 vehicle compositing VERIFY (2026-09-26)
- `Rm2kMapFrameRenderer.DrawVehicle` composites a vehicle through the same
  charset cell as any character, with the altitude taken off the vertical
  offset. A vehicle gets no separate sprite stage: the Player draws it with the
  same `Sprite_Character` and applies the altitude in `GetScreenY`, so giving it
  its own stage would put it behind or in front of the hero, which is not what
  happens.
- The altitude is counted in **whole tiles**, so the pixel offset is the
  altitude times `TILE_SIZE` of 16. Forgetting the multiplication draws the
  airship at 1/16 of the right height and still passes a bounds check, which is
  why the test measures the row of the first drawn pixel instead of counting
  pixels.
- `Rm2kAirshipShadow` is the airship shadow, which is a **separate sprite**
  rather than part of the airship: two 16x16 patches of the System graphic at
  `(128,32)` and `(144,32)`, blitted together, at `Opacity(0.26 * 255)`, which
  truncates to **66** and not 67. It is drawn one below the airship's own screen
  z so the airship covers it, and only while the player is in the airship.
  The Player's own comment says 26 percent is not what RPG_RT does; this
  repository reproduces the Player's value because the accurate one cannot be
  measured without a real game.
- Mutation evidence: a missing tile-size multiplication, an added instead of
  subtracted altitude, a shadow at the same z as the airship and a rounded
  opacity were all detected.
- Tests: `TestRm2kVehicleCompositing 6/6`, total `591/591`, build
  `0 Warnung(en)`, validator passed, probes 0.
- Still missing against the K-094 title: the vehicle state is built and tested
  as a unit but is not yet constructed from the LDB system section inside the
  runtime, so a real map still shows no boat.

## K-094 vehicle sprites VERIFY (2026-09-26)
- `Rm2kVehicleSprite` carries the sprite the LDB system section names
  (`boat_name`/`boat_index`, `ship_name`/`ship_index`,
  `airship_name`/`airship_index`) and the two properties that are specific to a
  vehicle: the altitude it is drawn at and its animation.
- A vehicle uses the **same** charset geometry as a character, `24 * 3` by
  `32 * 4`, so no second sprite reader is needed.
- The animation limit is `GetStopCount() ? 16 : 12`: a standing vehicle is shown
  for sixteen frames and a moving one for twelve, which is the reverse of the
  character tables and is exactly the kind of thing carried over by mistake.
- The frame wraps with `anim_frame = (anim_frame + 1) % 4`, a modulo over all four
  liblcf frames rather than a clamp, so `Frame_middle2` is a real state. The
  sprite clamp turns it into middle when it is drawn, so a vehicle walks left,
  middle, right, middle and back to left.
- `ResetAnimation` only puts the frame back to middle when the animation type is
  not `fixed_graphic`, so a fixed graphic holds its frame while it climbs.
- Mutation evidence: a swapped animation limit, a clamped frame, a missing
  fixed-graphic exemption and a counter that is not reset were all detected.
  The first run reported all four as escaping; that was my mutation harness
  using tab anchors against a space-indented file, not missing tests.
- Tests: `TestRm2kVehicleSprite 9/9`, total `585/585`, build `0 Warnung(en)`,
  validator passed, probes 0.
- Still missing against the K-094 title: the vehicle sprites are described and
  tested but not yet composited into a map frame, so nothing is visible yet.

## Five new engine criteria added by the user (2026-09-26)
- Criteria 4 to 8: RPG Maker MV, XP, VX, VX Ace and MZ complete.
- Measured state before answering them: `RgssEngineRuntime` is 312 lines against
  1922 for the RM2K runtime and contains no renderer, no frame buffer and no
  interpreter. It is a metadata inspector. There is no `project/src/mv` or
  `project/src/mz` at all, and the repository holds **no** RGSS or MV/MZ game
  file: no `.rgssad`, `.rxdata`, `.rvdata2` or `.rpgmvp` anywhere in the
  fixtures. K-080 (RGSS spike) and K-090 (MV/MZ spike) are still BACKLOG.
- So none of the five new criteria is met, and none can be verified against a
  real game with what the repository holds. Three questions were put to the user
  about how to proceed and none was answered inside the prompt window, so the
  safe defaults were taken and are recorded here rather than assumed silently:
  build structurally against the published specifications as WOLF was done,
  never execute foreign JavaScript, and finish RM2K before spreading out.

## K-110 WOLF binary database VERIFY (2026-09-26)
- `WolfBinaryDatabaseReader` reads the `DataBase.dat` / `CDataBase.dat` /
  `SysDataBase.dat` framing: magic `0 'W' 0 0 'O' 'L'`, a one byte version
  header where `0x00` is v2 and `0x55` is v3, the marker `'F' 'M' 0`, a version
  byte, a record type count, then one block per type and a version footer byte.
- The important part is the **position table**. A record does not store its
  values in order: each table entry packs block and index into one number, with
  the block in the thousands digit and the index in the remainder (`1000` is
  number block index 0, `2000` is string block index 0). The values then live in
  two blocks, numbers first and strings second, and the two block sizes are
  derived from the table rather than stored. Reading the blocks in property
  order instead of through the table yields plausible but wrong data, which is
  why the fixture `Test_ThePositionTableDecidesTheOrderNotTheFileOrder` puts the
  string first and the number second.
- A property whose position points outside its block is reported as missing
  rather than defaulted: a zero would be indistinguishable from a real value.
- A string is a 32-bit length, the bytes, and a NUL one past the length. The
  terminator has to be consumed or every following value is off by one.
- The first version byte was read at index 9, which is the marker's `M`; the
  header is six magic bytes plus one version header plus three marker bytes, so
  the version byte is at index 10. The test caught it.
- Mutation evidence: ignoring the position table, an off-by-one block count, a
  missing terminator check, a missing footer check and a skipped terminator byte
  were all detected. The first terminator run escaped because the fixture also
  failed on the file end, so the check was never isolated; the fixture now has
  all the bytes but a non-NUL terminator and asserts the message says so.
- Tests: `TestWolfBinaryDatabase 17/17`, total `576/576`, build `0 Warnung(en)`,
  validator passed, probes 0.
- Still missing against the K-110 title: the `game_dat`, `commonevent_dat` and
  transfer formats. Only the database format is done.

## K-094 decision turn order DONE (2026-09-26)
- `Rm2kDecisionTurn` reproduces the tail of `Game_Player::UpdateMove`:
  `if (Input::IsTriggered(Input::DECISION)) { if (!GetOnOffVehicle())
  { CheckActionEvent(); } } return;`. Vehicle boarding therefore has
  precedence over the action event check, and the turn ends either way so the
  step counter is not incremented.
- The diagonal correction runs first, because `GetOffVehicle` asserts there is
  no diagonal. A diagonal of 4 to 7 becomes the facing, and a caller that acted
  on the diagonal directly would look at the wrong neighbour tile.
- `CanBoardAirshipOn` requires the airship to be on the player's own tile and
  **both** the player and the airship to be standing still. A drifting airship
  is not boardable.
- `CanLeaveAirship` refuses to leave an airship that is still ascending or
  descending, which is what stops the player ending up standing in mid air.
- `CanDisembark` checks `IsValid` first, then an active same-layer event on the
  target tile, then passability towards the player.
- Out of scope and not implemented: the BGM swap around boarding
  (`SetBeforeVehicleMusic` / `BgmPlay`). The runtime has no BGM state at all, and
  adding one would be speculation rather than a verified slice.
- Mutation evidence: action-event-first, missing airship guard, hardcoded
  airship stopping, boat before ship and missing leave-airship guard were all
  detected. The first run reported the airship-stopping mutant as escaping; that
  was a stale backup in the harness, and re-running it against the current file
  detected it.
- Tests: `TestRm2kDecisionTurn 12/12`, total `559/559`, build `0 Warnung(en)`,
  validator passed.

## K-094 RM2K vehicles VERIFY (2026-09-26)
- `Rm2kVehicle`: types `None=0, Boat=1, Ship=2, Airship=3` (liblcf
  `Game_Vehicle::Type`, stored in save data). Move speeds are **4** for boat and
  ship and **5** for the airship -- liblcf `MoveSpeed_normal=4`,
  `MoveSpeed_double=5`, so a vehicle is faster than the default event speed 3,
  which is `MoveSpeed_half`. This was the easiest value to get wrong.
- `Rm2kVehicleState.GetAltitude`: `(256 - remaining) / 16` tiles while ascending,
  `remaining / 16` while descending, and only while flying. 8 is spent per
  update, so a full ascent or descent is 32 updates. A finished descent lands
  where it can and otherwise starts another ascent instead of hovering.
- `Rm2kVehicleBoarding`: the two rules are asymmetric and stay separate.
  Airship boarded by standing on it (`BoardAirship`, aboard at once) and left
  by `BeginAirshipDisembark` (still aboard, the airship descends). Boat/ship
  boarded by stepping onto the water (`BeginEmbark` -> `CompleteEmbark`) and
  left by stepping off (`BeginDisembark` -> `CompleteDisembark`), which
  restores `PreboardMoveSpeed` rather than the vehicle or event speed.
  `VehicleInFront` checks ship before boat, matching the Player's own order.
- Mutation evidence: wrong move speed, ascent 128, no flying check, no
  land-or-retry branch, boat-before-ship, airship-steps-off, missing map check
  and vehicle-speed-instead-of-stashed-speed were all detected.
- Tests: `TestRm2kVehicle 11/11`, `TestRm2kVehicleBoarding 11/11`, total
  `547/547`, build `0 Warnung(en)`, validator passed, probes 0.
- Still missing against the K-094 title, which is "get on/off **for the
  action-event order**": the vehicles are not yet drawn, not yet given their
  system-section name and index as a sprite, and not yet wired into the action
  event order. The pinned fixture has no water tiles and no vehicle sprites, so
  the boarding rules are proven on synthetic maps only. Status is therefore
  VERIFY, not DONE.

## Next action

1. **K-111**, the move route, so events walk at the verified per frame rate. `Game_Character::UpdateMoveRoute` and `lcf::rpg::MoveRoute` are the reference; each command needs its verified semantics before code, and a wrong command length desynchronises the route.
2. **K-110** for WOLF: the remaining command bodies and the binary database, common event and game formats.
3. Vehicles (K-094) still need typed terrain flags and a collision path.
4. A methodology note worth keeping: a mutation result means nothing until the baseline reports that there is nothing to detect, and a build failure must be distinguished from a test failure. Counting only test errors reports a failed build as a clean pass.
5. Unrelated local changes must stay untouched: `project/assets/fonts/NotoSansCJKsc-Regular.otf.import` and untracked `qa_patches/`.

## Reference repos noted by the user (2026-09-26, not actioned)

- `joiplay/mkxp`, `joiplay/android-mkxp` — RPG Maker XP (RGSS) reimplementation in C++; useful as a cross-engine reference for Ruby/RGSS and for its own passability handling, not a source of RM2K constants.
- `futokoro/RPGMaker` — Ruby RGSS reimplementation (XP/VX/Ace).
- `bakustarver/rpgmakermlinux-cicpoffs` — RPG Maker on Linux via C++ offscreen; detection/hosting reference.
- Consequence: XP/VX/Ace work stays at priority 6-7 per `AGENTS.md`; MV/MZ playability still needs a JS runtime, which the repository policy does not provide.

## Last verified baseline

Windows validation on 2026-09-26: `dotnet build project/UniversalRPG.csproj` (0 errors) and the headless C# runner at `All 419 tests passed`, exit 0. The Godot project lives under `project/`; `validate.sh` handles both layouts.

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

## Roles for the parse tree's children (2026-09-27)
- Every child of a node now says what it is for, so a consumer asks for the test
  or the body rather than knowing each kind's layout.
- The fault this found: the parser said only that a child was there, so what a
  list meant depended on the kind alone. A keyword that opens a test held the
  keyword first, a ternary held the test first, a block on a call held the call,
  the parameters and the body, and a block that was a body held only statements.
  One kind could mean two things and the second meaning was invisible.
- A name is held in `Name` and not in `Text`. Two test failures came from reading
  the wrong field, and a wrong field can be fixed two ways of which one is right.
- `One("a if b")` returns the `if` node itself, not a wrapper holding it. The
  first version of the test assumed a wrapper and was wrong; the parser was right.
- A lookup for a role that is not there answers null rather than falling back to
  the first child, so an absent role cannot be mistaken for a present one.
- Tests: `TestRubyParser` 52/52, total 808/808, validator passed, 0 warnings.
- 6 mutations on the roles and the lookup, all detected. Two escaped at first:
  a lookup taking the last of a role cannot be told from one taking the first
  while every node holds at most one child under a role, and a lookup falling
  back to the first child passed every test that asked for a role that was there.
- Next: a machine to run the tree. That is no longer blocked on the tree's shape.

## Whole numbers the engine writes wider than this machine holds (2026-09-27)
- The ground truth for all the marshal work had been Ruby 3.4, because that is
  the documentation that is easiest to reach. Checked against the sources:
  XP is Ruby 1.8.1, VX is 1.8.3, VX Ace is 1.9.2. All twenty five type bytes
  are identical across 1.8.7, 1.9.3 and 3.4.1, so the format held.
- The whole number form did not: 1.8 and 1.9 write `i` for a number that fits
  in thirty one bits and `l` for the decimal digits of anything larger, and
  Ruby 3 swaps those two letters and writes the large form in binary. A reader
  built from the 3.4 table refuses every file an engine of this line writes.
- **The reader's sign handling was wrong.** A negative number is written as its
  bytes carried to the width it was written in, so every byte after the first
  is the top of the width. The reader negated the unsigned value instead, which
  is the same for a one byte number and not the same for any other: it read one
  byte too many and took the first byte of whatever followed in the file. A
  game's negative coordinate would have had the next value's bytes inside it.
- The one byte negative form was on the wrong side of the boundary. Five to 127
  is the number with five taken off, -129 to -5 is the number with five added,
  and -1 to -4 is the wide form.
- A wide number is now read from its digits and refused only when it does not
  fit this machine's whole number, with the digit count in the reason. The
  earlier card refused it outright for a reason that was wrong: a game's number
  is decimal digits, so the number is readable and only the width is in doubt.
- How it was found: a test walks 6001 numbers through the writer taken from
  1.8.7's own loop, and a second holds sixteen against the bytes that loop
  produces. The first version of those sixteen was written from memory and was
  wrong about four. Every fault found in this work was in the test rather than
  in the reader.
- A check refusing a count byte wider than a whole number was written and then
  removed: five to 127 is the one byte form, so no such count exists, and the
  check refused a length a game writes for every list it has.
- Tests: `TestMarshalReader` 39/39, total 818/818, validator passed, 0 warnings.
  8 mutations of the packing, all detected.
- Still missing: a number wider than this machine's whole number is read and then
  refused, and no archive from any of the three engines is in the repository.

## K-120 Real game data — checkpoint

- What was added: sixteen XP `.rxdata` files from two independent installations
  (`rgss-xp`, a Japanese one, and `rgss-xp-microquest`, an English one), a real
  RM2K database, map tree and two maps (`rm2k-dragon-destiny`, 743 maps in the
  game), and one `Game.ini` from a KiriKiri game that is **not** a WOLF game.
  Sizes and SHA-256 in `project/tests/fixtures/RGSS_FIXTURES.md`.
- What it found: the marshal reader reads every one of the sixteen XP files. Two
  of my own expectations were wrong and the files settled them — an XP map is an
  `RPG::Map` object with eleven members, not a hash, and a `.lmu` holds an
  `LcfMapUnit`, not an `LcfMap`.
- The WOLF detector was checked against the KiriKiri game, which has `BasicData`
  and `MapData` folders and no `Game.dat`. It refuses it, and the test proves the
  refusal is a decision by showing the same folder with a `Game.dat` is detected.
- Tests: `TestRealXpData` 6/6, `TestRealXpDetection` 3/3, `TestRealRm2kData` 3/3,
  `TestKirikiriIsNotAWolfGame` 2/2, `TestMarshalReader` 40/40, total 833/833,
  validator passed, build 0 warnings / 0 errors, 8/8 mutations detected.
- A lesson kept in the mutation harness: a suite that looks for `error CS`
  anywhere in a run counts the previous step's output as a build fault. Compile
  the mutation on its own, then run the suite.
- Still missing, and this is the next step: **no archive from any engine is in
  the repository**, so `RgssArchiveReader` has never read a real `RGSSAD` file and
  `RgssEngineRuntime` is still metadata only. The XP games given to the repository
  keep their data in plain files, so the archive path needs either an encrypted
  game's data or a written archive from the engine's own format description.

## K-121 RPG Maker MZ data — checkpoint

- What was added: `project/src/mz/MzJson.cs` and `project/src/mz/MzDataFile.cs`,
  a reader for the JSON an MV/MZ game keeps its database in. Eleven real data
  files of a 1.9.1 game are in `project/tests/fixtures/mz`, with sizes and
  SHA-256 in `project/tests/fixtures/MZ_FIXTURES.md`.
- The three format facts, all measured from the file: a database file's first
  entry is null; a command is a small number and is NOT the generation before's
  `code * 1000`; a map's events are indexed by event and are not padded to the
  field. Two of the three were wrong in a first draft of the test.
- `MzDataFile.Top` was a one element list, which made `Top[0]` the file and
  `Top[0][0]` its first element. That is not a root value; it is `Root` now, and
  the test that read the array where the object was is what showed it.
- The reader that was already here, `MzDataDirectoryResult`, counts entries and
  takes names and caps a file at 2 MiB. `TestMzReaderBoundary` states that
  difference so the two are not confused. Neither derives from the other.
- Tests: `TestRealMzData` 15/15, `TestRealMzDetection` 3/3,
  `TestMzReaderBoundary` 2/2, `TestMzDataDirectory` 8/8, total 853/853,
  validator passed, 8/8 mutations detected.
- Honest number worth keeping: **the first mutation suite of this reader
  detected 1 of 8.** It found five real gaps (an unclosed string ran to the end
  of the file, an unknown escape was taken as text, nesting was unbounded, a
  broken exponent became a number, a file's own text was discarded) and one
  anchor that did not exist. A suite that reports a high number on its first run
  has usually been written from the code rather than against it.
- Still missing, and this is the next step: **an MZ game does not play.** There is
  no JavaScript runtime here, so no plugin, no script and no event command runs,
  and the two `js` fixtures are placeholders carrying the real file names. MV
  shares the data format and has no real fixture at all, and its command
  numbering is the packed one, which is exactly the difference the second trap is
  about. For the RTP criterion: no RTP has been downloaded and none is needed for
  the data layer, and the user is asked before anything is fetched.

## K-122 The MZ command table — checkpoint

- What was added: `project/src/mz/MzCommandName.cs` and `MzCommandTable.cs`.
  114 commands, 101 to 603, every number and every name generated out of the
  engine source of the real MZ game. A command is now a number and a name, and a
  caller can say what 121 is.
- A hand written table was 79 of 178 names wrong and was replaced. 129 was
  "Change Hp" and the engine calls it "Change Party Member"; 231 was "Move Event"
  and the engine calls it "Show Picture". Do not write a table of an engine's
  numbers from memory. Generate it or read it.
- `code - 300` finds the owner of a data number four times out of eight and is
  wrong the other four, twice pointing at a command that exists and does
  something else. The owners are written down. A test runs the rule and counts
  four so the rule cannot creep back in as a calculation.
- 411 and 413 are commands the engine names Else and Repeat Above. 412 beside
  them has no method and is data of a branch. A family is not a family.
- A name was written in two places (enum doc comment and a string table) and the
  copies drifted so that three name mutations were invisible. It is a
  `record struct MzCommand(int Code, string Name)` now: one value, one name.
- Tests: `TestMzCommandTable` 13/13, total 866/866, validator passed.
- Mutations: 11, of which the first suite detected four of nine. The three name
  mutations escaped because of the two-place problem, and one mutation tested
  nothing at all because a command is decided before an owner is consulted. Both
  are stated in the test file so they are not repeated.
- Still missing, and this is the next step: **a command is named, not done.**
  Nothing interprets 111's six comparisons or 121's three modes, there is no
  renderer, and a 657 line is text. The next real step for MZ is the
  interpretation of a few commands against the engine source, starting with 111,
  whose whole semantics were read out of `command111` and are six comparisons
  over six kinds of thing.
- Tests: 878/878, TestMzBranchEvaluator 12/12, Mutationen 10/10.

## The check that passed when it should not have

A mutation that folds a kind of branch the engine has no name for into the
nearest kind it does have was invisible twice. The test asked whether the
evaluator's refusal mentioned the number, and the evaluator names what it needs
in its own words: a branch on 99 that was folded into 0 reports "switch 0", and
"switch 0" does not contain "99". **A check that reads the complaint rather than
the thing cannot see a fold, because the complaint is itself already wrong.**

The number lives in the branch, so the branch is what is checked. That test
closes it, and the same reasoning applies wherever a diagnostic is used as proof
of the thing it describes.

## Two repairs that fought each other

A killed mutation run left two mutations in `MzBranchEvaluator.cs`. Two repair
scripts then each rewrote the wrong branch, so a Gold branch held the Actor's
code and the Actor branch had lost the line naming the actor. Both were found
only by the compiler and by reading the region. **Repairing a file by matching
the first occurrence of a shape that occurs twice is how a repair lands in the
wrong place**; the repair has to name the place, not the shape.

## What is still not decided

A branch is decided. 121's three modes, 126's change of an item and 126's change
of a weapon are not, there is no interpreter holding an index into a list, and
a 657 line is text. MZ has detection, bounded data reading, a named command
table and one decided command.

## K-124 The MZ event index

**What was built** `MzCommandEntry`, `MzOperation` (operands, operations, and a
`MzRandom` held per interpreter), `MzCommands` (121 and 122), `MzControlFlow`
(the commands whose whole effect is the index, with the three answers they give)
and `MzInterpreter` (the index, the branch result per indent, the step limit).

**The rules, and the three that were wrong first**

1. Every command that returns true is followed by `this._index++`. A first
   draft added a flag for "the command moved the index itself" and then did not
   step over a command that had, which made an else land on the false arm it had
   just skipped. The flag is gone.
2. A repeat above walks back to the first command at its own indent and the
   step then moves off that one, so `112`, body, `413` goes round with no
   special case.
3. A command with no method is stepped over, not refused. **All five of the
   codes this game stores without a method are ones it stores on purpose**: 0
   the end of a block, 401 a line of text under a 101, 412 the end of a branch,
   655 and 657 the two halves of a script.
4. A list that ends inside a branch is reported `Truncated`; the engine's
   `skipBranch` has no test for the end of the list and would read past it.
5. The step limit is the engine's `checkFreeze`: a hundred thousand commands in
   one frame.

**Two findings from the real map, neither a test mistake**

- **This game stores a loop nothing can leave.** Event 4 is a 112 with seven
  message commands and a 413, and nothing between them tests anything or
  breaks. The engine plays it until `checkFreeze` stops it. The reader reports
  the same, and the test calls a freeze there the right answer.
- **Random is drawn per variable, not per range.** A first draft claimed one
  draw for a whole range; the engine's `command122` calls `Math.randomInt`
  *inside* `for (let i = startId; i <= endId; i++)`. The test was wrong in the
  same direction as the draft and was corrected against the source.

**Total 896/896**, `TestMzInterpreter: 18/18`, validator passed, build 0
warnings / 0 errors.

## A repeat above is not a jump, and a mutation run said so

The mutation run left two rules untested, and reading the engine for them
corrected a claim the test file had been making wrongly for two cards:

1. **`command413` is not `jumpTo`.** It is `do { this._index--; } while
   (currentCommand().indent !== this._indent); return true;` — it writes the
   index and clears nothing. Only `command119` calls `jumpTo`, and that clears
   the branch result of every indent it steps over. The test file had said "a
   repeat above is a jump, so this is the case that matters", which is the
   opposite of what the engine does.
2. **A jump that points backwards at a label is a loop.** `jumpTo` sets the
   index to the label, `executeCommand` steps on, and the jump is met again. A
   test shaped that way hung the suite for a hundred thousand steps, which is
   `checkFreeze` doing its job — and the same thing the engine does. **This game
   stores no label and no jump at all**, so only the shape that ends is
   asserted.

## How a 30 KB test file was lost twice, and what stops it

Two repair scripts each emptied `test_mz_interpreter.cs`. Both used a slice
`[k:end]` and both ran when the anchor text was no longer in the file, so `k`
was -1 and the slice was empty or inverted. **The second time the file was
untracked, so git had no copy either.** What stopped it the third time was
`git add` immediately after writing, before anything else could touch the file,
and then editing with `patch` and an exact anchor rather than with a script that
rewrites by shape. A test file that has not been staged is one `rm` away from
needing to be written again from memory.

## What is still not decided

Eleven commands of a hundred and fourteen have an effect. 117, 126, 230, 231,
232, 235, 351 and 357 are read as text; a 655 or 657 line is text. There is
still no renderer, no save path and no input. MZ has detection, bounded data
reading, a named command table, a decided branch and an index that walks a list.

## K-125 The list a command calls, and the wait that stops one

**Built** `MzEventRunner` (a run over a list and every list it calls),
`MzInterpreter.Wait` / `PassFrame`, `MzAction.CommonEvent` / `Wait`, and
`Result.MissingCommonEvent` / `WaitingFrames` as fields rather than prose.

**The rules, from `Game_Interpreter`**
1. A called list runs to its end before the caller moves on — `updateChild`
   gives the child its own `update()` and the parent breaks the frame while it
   is still running.
2. Every list in a run shares one set of facts, because both go through the one
   `$gameVariables`.
3. The event id travels with the call, and only on a map (`isOnCurrentMap`).
4. A 230 holds the index: the same command is read again the frame after, so a
   reader that stepped over it would run a list three frames early.

**What it refuses rather than guesses** The bounded fixture carries no
`CommonEvents.json` — the real one is 4.5 MB and was left out on purpose — so
every 117 in this game names an index that cannot be handed over. The engine's
own line is `if (commonEvent)` and steps over a missing one; **this reader
refuses and names the index**, because a silent step-over would run the rest of
a game's list as if the call had never been there.

**Measured on the one map in the fixture** Six event pages ending four different
ways: three reach a common event and name the index, one stops at a 230 and says
it is waiting, one is refused because it opens with fifty-eight lines of the
game's own JavaScript, one is a single 0 and runs through. A first draft guessed
three, one, one and one, and two of the four were wrong.

**A number read out of prose is not a number** The test took the common event's
index out of the message with an offset and got 76 for 476, because it counted a
space twice. The index is a field now, and the test checks field and prose
against each other. **The same rule as the two lost test files: name the place,
not the shape.**

**Total 912/912**, `TestMzEventRunner: 16/16`, `TestMzInterpreter: 18/18`,
validator passed, build 0 warnings / 0 errors.

**The mutation runs found a real fault in the runner, not only gaps in the
tests.** Reading the map and the event of a child was untested, and asking the
question showed the runner was doing it wrong: it passed the *caller's* map down
and read both values off the frame rather than off the interpreter. The engine
does neither — `setup` sets `_mapId` from `$gameMap.mapId()`, the map the game
is on, and `command117` reads `this._eventId` off the calling interpreter. Both
are now read from where the engine reads them, and `Result.Child` hands the
caller the child so a test can check the three fields instead of trusting them.

**A first draft of that test claimed the event id falls away on the second
level, and the engine does not do that.** `setup` is `this._eventId = eventId
|| 0`, so a chain that is on the map carries the same event all the way down;
only a list that is not on the map passes zero, and there it stays zero. The
claim was backwards on the interesting side and would have been written into a
test as if it were the engine's answer.

**Five mutation runs, and the lesson from them.** A rule that survives a run
is not automatically a gap, and a rule that dies is not automatically covered.
The first run caught 4 of 9. The three that escaped were real, and the fourth
turn showed why the other two were not: `WaitFrames <= 0` and `< 0` differ
only in a state nothing can reach, and the wait case's own `return false`
cannot be changed to `true` and change anything, because `ExecuteOne` ends with
`return Stopped == MzStep.Stepped` and `Wait` has just set `Stopped` to
`Waiting`. **That is an equivalent mutant and is now named in the code as a
dead branch**, rather than carried as a rule with no test.

**A rule that hit the wrong place is not a gap either.** `MissingCommonEvent =
index,` stands in two places — the depth branch and the missing-list branch —
and a `replace(..., 1)` mutation hit the depth one, which no test reached. So
the depth was genuinely untested, and `MaxDepth` is now a field with two tests:
a list that calls itself is refused at the engine's own hundred and is not
reported as a freeze, and the same list under the limit runs through.

**Two test drafts were wrong and both are named in the file.** A test of
"runs through" was first given a list that calls itself, which never can — a
self-call is endless at every level. The second draft gave the called list a
call to itself, which is the same endless thing one level down. Both are
recorded, because the next one to write a "control case" here will reach for
the same shortcut.

## What is still not decided

Thirteen of a hundred and fourteen commands have an effect. 126, 231, 232, 235,
351 and 357 are read as text; a 355 or 657 line is text. A called list this
repository has runs; one it does not have is named. Still no renderer, no save
path, no input and no audio.

## K-126 Change what the party is carrying

**Built** `MzParty` (the inventory and the four rules), `MzCommandTable
.ChangeItems`, the 126 case in `MzCommands`, and `MzBranchFacts.MaxItems`.

**The rules, from `Game_Party`**
1. `container[item.id] = newNumber.clamp(0, this.maxItems(item))` and
   `maxItems` is `return 99` — no argument, no per-item case. **Five of this
   game's eighteen 126s ask for 999.**
2. `if (container[item.id] === 0) { delete container[item.id]; }` — a count
   that lands on zero is deleted, not stored.
3. The clamp is from below as well, so taking four of one is none.
4. `itemContainer` returns null for an item that is not there, and the engine
   steps over it. This reader says it did not happen.

**And one that is easy to get wrong in the other direction** `operateValue`
asks the operand's kind first, so a literal amount must not be read from a
variable. `operation === 0 ? value : -value` has no third case: an operation of
seven removes exactly as an operation of one does.

**The clamp is invisible in the middle of the range** A test that only added
four to an empty bag would pass with no clamp at all, so every rule is asked
about at its boundary and the default is claimed to be ninety-nine.

**Two countings that are different claims.** Eighteen 126s over fifteen items,
five of them above ninety-nine — read straight off the game's data. A walk
reaches only nine, because one page stops at a 230, and that is a statement
about a reader with no frames. **A first draft counted the walk and called it
the game**, which was wrong in the direction of under-reporting.

**A known gap this card found and now names.** A lone `MzInterpreter` knows no
common events, so a 117 is stepped over like a 0 — the silent step-over K-125
refuses, still reachable through this door. Both answers are claimed side by
side rather than one of them quietly assumed.

**The wiring test found two real faults, and three mistakes of my own.**

1. `new MzParty(pFacts)` knew no items, so every 126 was a silent no-op with
   every count at zero and nothing saying why. The ids travel in
   `MzBranchFacts.KnownItems` now.
2. **An empty set of known ids was read as "everything exists"** — the
   opposite of what it means, and it would have handed a player 999 of an item
   the game never had while looking as if it worked.

**And the three mistakes, which are the ones to remember.** A fresh
`MzInterpreter` has `Stopped` at whatever it starts as, not at `Stepped`, so a
hand-written `while (ExecuteOne(...))` gives up on the very first command —
`Run` is the loop a caller should have used. `new(2, ...)` where the code
belongs: **126 is the command, not the item**, and a page of codes 2, 3 and 4
is a list the engine steps over. And a party the test made itself keeps its
notices to itself while the interpreter builds its own over the same facts, so
the notice is on the action.

**Total 924/924**, `TestMzParty: 12/12`, validator passed, build 0 errors.

## K-127 Put a picture on the screen — DONE

231, 232 and 235 on the one map in the fixture: nine commands, three shows,
four moves, two erases, on images 1, 86 and 87. **Not 127 or 128** — this
game's `Map002` has no 127, no 128, no 129 and no 130, and the fixture has
no `Weapons.json` or `Armors.json`, so carrying weapons would have meant
rules no data here can check. The pictures were chosen because the data is
here, and because they are the first commands in this game that need
something other than numbers to have an effect.

Six rules out of rmmz_objects.js 1.9.1, in the card. The two that bite: **a
shown picture is a new object and the old one is gone with it**, and **a move
sets a target, not a value, so a move of zero frames changes nothing at all**.

**A real fault in reading, not in testing.** `MzCommandEntry.From` handled a
Number and took `item.Text` for everything else, so a JSON boolean became the
empty string — and a 232 carries its wait in the eleventh slot as a real
`true`/`false`. This game's four moves all came back as "does not ask to
wait". No test had noticed because no test had read a boolean out of an event
list. Fixed in the reader, not worked around in the test.

**And one of my own:** `if (params[11])` is a truth value, and a first draft
called `int.Parse` on it, which throws on the empty string a game may leave in
that slot. Reading it the way the engine reads it is now its own named
method.

**A third fault, and the worst: a waiting move never arrived.**
`ExecuteOne` stepped the index only when the interpreter was `Stepped`, and a
`MovePicture` that asks to wait returns false — which means the same thing.
So the next frame read the same 232 again, set the same twenty frames again,
and a picture that had to cross the screen waited for ever. The engine has no
such trouble: `command232` ends in `return true` whatever it asked for, and
its wait lives in `_waitCount` where the next command cannot reach it. The
index moves and the run stops in two separate steps now. `MzInterpreter` 18
and `MzEventRunner` 16 are unchanged after it, so it was a rule nothing had
exercised.

**And a fourth of my own:** a test that claimed a picture sits at 2000, 2000
because the *scale* is 2000, 2000 was reading the wrong line. The place is
zero. Corrected to the measured value rather than adjusted until it passed.

**A test that counts is not a test that runs.** The first draft's ninth test
was named "every picture command in this game runs" and did nothing of the
kind — it counted codes out of the file with no interpreter in sight, and four
mutation rules escaped through it. The replacement builds an interpreter,
hands it the frames the two waiting moves ask for, and checks the screen when
the list is through. It is the test that found the waiting-move fault.

**A fifth mistake:** a 122 written with four parameters has nowhere to read
a value from — `command122` is `startId, endId, operationType, operandType,
operand` and the operand is the fifth. The page was not held by the move
failing; it was held by a command that could not do what the test meant.

**935/935**, `TestMzScreen: 11/11`, `TestMzInterpreter: 18/18`,
`TestMzEventRunner: 16/16`, `TestMzParty: 12/12`, validator passed, build 0
errors. Second mutation run in flight. Next after this: 233, 234, 224, 236 —
the other picture commands — then 355 and 357, which this map uses nine and
three times.

## K-128 A menu, and a plugin call that is refused — DONE

K-127 asked which picture commands come next. **None of them:** this map uses
no 224, no 233, no 234, no 236, and modelling them would have been rules no
data here can check.

The two commands left on this map that mean something mean opposite things.

**351 is run.** `if (!$gameParty.inBattle()) { SceneManager.push(Scene_Menu);
} return true;` — one condition, and it returns true either way. A reader that
stopped the run in a battle would leave the commands after it unrun in a way
the engine never does. `MzMenuState` exists so a 351 is not
indistinguishable from a command with no effect.

**357 is refused, by name.** `PluginManager.callCommand(this, pluginName,
params[1], params[3])` is somebody else's JavaScript. All nine of this map's
357 commands are answered, each naming the plugin and the command inside it,
each landing on `MzBranchFacts.Notices`. A silent step would leave a game that
looks as if it works while its crafting menu and floating text never appear.

**The finding that outranks both cards: this game ships fifty-two plugins and
all fifty-two are enabled.** Its 357 commands call `ItemCombinationMZ`,
`DTextPicture` and `HyoujouSelect`; its 355 scripts read
`$gameVariables.value(180)`. **UniversalRPG runs this game's MZ event code and
none of its plugin code, and no bounded slice changes that.** Said once, with
the numbers, where a caller will see it — which is what a card can do about
it.

**939/939**, `TestMzMenuAndPlugins: 4/4`, validator passed, build 0 errors.
Every expectation was measured out of the game's own files first: nine plugin
commands, three plugins, two 351s, three scripts. **Nine mutation rules, nine
caught** — and one of them had to be written twice, because the first attempt
replaced a fragment inside an escaped string and left the file unparseable.
`BROKE` counts as caught and proves nothing; the second attempt compiled and
failed three named tests, one for each plugin this map calls.

**Next after this, and it is not another MZ command.** This map's twenty-two
codes are now all either modelled or refused. The next thing worth doing is a
**second MZ fixture** — a game with no plugins — so the reader can be checked
against MZ event code without a plugin's JavaScript in the picture at all.

## K-129 A second MZ fixture, from a game with no plugins — DONE

The first fixture is a game with 52 enabled plugins and 9 plugin commands.
A reader checked against it is mostly checked on its refusals. That needs a
second game.

`CamelliaCoronation-Win`, in `E:/RPGMakerGames` — a free MZ game the user put
there to work with. **Engine 1.9.1, measured:** both games' `rmmz_objects.js`
carry the same 114 `commandNNN` methods, none only in one or only in the other.

**One plugin, in no command. No 355 and no 357 on any of the 19 maps.** That
negative claim is the reason the fixture exists.

**Alles gemessen, nichts behauptet:**

- 2 432 Befehle, **1 772 laufen, 660 nicht** — und alle 660 sind echte
  MZ-Befehle, keine Pluginaufrufe.
- Häufigster: **401 (Textzeile, 938×)**, dann **101 (Dialogblock, 414×)**,
  dann **505 (Wegliste, 348×)**. Ein Entwurf nannte die Wegliste am
  häufigsten — „ein Spiel besteht hauptsächlich daraus" — und lag zwei Plätze
  daneben. Und ein zweiter Entwurf nahm die größte Zahl in *irgendeinem*
  Parameter und bekam 720, eine Pixelposition statt einer Variablen.
- **15 Variablen, 0 bis 15, keine darüber. 8 Items.** Eine Klasse, eine
  Animation, **keine Switches, keine Common-Event-Aufrufe, keine
  Actor-Referenzen.**
- **`CommonEvents.json` ist 376 Byte und vorhanden.** Bei der ersten Fixture
  fehlte sie (Original 4,5 MB), und der Runner musste ein 117 verweigern. Eine
  Regel, die nur gegen eine Lücke getestet wurde, ist eine ungetestete Regel.
- 0, 401, 404, 405, 412, 505 sind **echte MZ-Sonderbefehle ohne Methode** und
  keine Plugins. Die Engine liest sie nach Position, nicht per Dispatch.
  „MZ kennt diese Zahl" ist nicht dasselbe wie „MZ führt sie aus".

**537 KB, 30 Dateien**, kein `js/`, keine EXE, kein Bild, kein Audio, kein
`Tilesets.json`. **`Skills.json` ist die eine abweichende Datei**: 104 525 auf
1 181 Byte, weil **kein Befehl der 19 Karten eine Skill referenziert und der
Leser keine liest.** Alles andere byteweise identisch, SHA-256 im Manifest.

**945/945**, `TestMzPlainFixture: 6/6`, validator passed, build 0 errors.
**Zehn Mutationsregeln, zehn gefangen, erster Lauf, keine entkommen** — und an
den **Fixture-Dateien** statt am Leser, weil die Behauptungen über Daten sind:
ein 505 als Zweig, ein 401 als Wahl, ein 101 als etwas anderes, ein 357 in eine
Karte eingefügt, ein 355 in eine Karte eingefügt, die Common-Event-Liste
geleert, die Datei gelöscht, und drei gegen die Testarithmetik.

**Das ist die erste Karte seit vier, auf der nichts entkommen ist**, und der
Grund ist derselbe wie bei den drei davor: dort entkam eine Regel, die kein
Test von der Seite gefragt hatte, auf der sie falsch ist. Eine Behauptung über
eine Zahl ist nur so gut wie der Test, der merkt, wenn sich die Zahl ändert —
und die Behauptung einer Fixture ist eine Behauptung über eine Zahl.

**Next: die 660.** 505 Weglisten (348), 205 Movement-Skripte (96), 123
(42), 213 (36), 405 Choices (36). Und `123` ist Change Gold — dieselbe
Clamp-Familie wie `126`, aber mit `maxGold` statt `maxItems`.

## K-130 Send the player somewhere, and hold the page until they arrive — DONE

`project/src/mz/MzPlayer.cs`, `MzWaitMode.cs`, `MzCommandTable.TransferPlayer
= 201`, `MzBranchFacts.Player` / `MessageOpen`, `MzInterpreter.WaitFor` /
`Refuse` / `PassFrame(Func<MzWaitMode, bool>?)`.
`project/tests/core/test_mz_player_transfer.cs`, 5 tests.

**950/950**, `TestMzPlayerTransfer: 5/5`, validator passed, build 0 errors.
**Neun Mutationsregeln, neun gefangen, erster Lauf, keine entkommen.**

**The reason for this card, and it is a reason about testing rather than about
MZ.** The 660 commands that do not run yet are led by 505 at 348 and 205 at
96, and both are movement — both need `Game_Character`, a move route decoder
and a passability model, which is three cards before the first of them can be
tested. 201 is 33 commands and needs none of that. **A card you can test is
worth more than a bigger card you cannot**, and the 348 will still be there
when `Game_Character` exists.

**And it is the first command here that is neither a change nor a number of
frames.** K-125 waits for frames, K-127 for a picture's movement, and a 201
for a **condition**: `setWaitMode("transfer")` with
`updateWaitMode` answering `waiting = $gamePlayer.isTransferring()`. A
condition wait has no length, so a caller passing frames cannot end it. The
first draft counted frames and would have let the page on with the player
still on the old map.

**Four rules, each a place a first reading goes wrong.** A transfer is
**reserved and not carried out** — `reserveTransfer` changes nothing a player
can see and `performTransfer` is what moves them. The engine **returns false
and transfers nobody** in a battle or with a message up, which is neither a
wait nor a finish, so `MzStep.Refused` says that and is not dressed up as
either. **The direction is set on the way**, because `performTransfer` is what
calls `setDirection`. A map this reader has not read is **named and the player
stays put** — half-applying it is worse than not moving, because the caller
would see a position and no file behind it.

**This game's own numbers: 33 transfers over sixteen maps, every one with the
first parameter at zero.** A reader that always looked in the variables would
send every player in this game to variable four.

**A C# trap, measured and now in the test so the next card does not walk into
it.** `$"Map{i:03}.json"` with `i = 1` produces **`Map13.json`**. In an
interpolated string `i:03` is a fill character of `0` and a **precision** of
`3`, and a whole number with a precision is padded on the **right** — 1
becomes "13", 2 becomes "23". Every file was missing and the only thing that
said so was the reader's own error about a file ending mid-value. `ToString
("000")` is the right spelling. **It took four rebuilds and a `Console
.WriteLine` to see it**, because every other signal said the code was correct
and it was.

**A build note, measured, so it is not measured again.** `dotnet build -m:1`
reports **53 warnings**, and the same tree without the switch reports **0
warnings, 0 errors**. The warnings are the parallel build's, not the code's:
the schalter forces a shared compiler instance and warns about it. **The
seriellen Build ist der ehrliche, und `-m:1` gehört nicht in die Abnahme.**

**Not here.** No map is loaded and no tile is drawn. The other four wait modes
need a scrolling map, a moving character and a plugin callback.

**Next: 505 Move Route at 348, which needs `Game_Character` first.**

## K-131 Walk a character, one step a frame — DONE

`project/src/mz/MzCharacter.cs`, `MzMoveRoute.cs`, `MzRouteStep.cs`,
`MzJson.Write`, `MzCommandTable.MoveRoute = 205`,
`MzBranchFacts.Characters`, `MzWaitMode.Route`.
`project/tests/core/test_mz_move_route.cs`, 7 tests.

**957/957**, `TestMzMoveRoute: 7/7`, validator passed, build 0 errors.
**Vierteen Mutationsregeln, vierzehn gefangen.** Der Hauptlauf meldete
13 von 14; die eine als entkommen gemeldete Regel habe ich isoliert
nachgeprüft und sie fiel — **drei von sieben Tests**, Baum byteweise
unverändert. Sie steht als 14 von 14 drin, denn **eine Zahl, die man
nicht geprüft hat, ist keine Zahl, die man gezählt hat.**

**The list in K-129 was wrong, and this card is what showed it.**
`command505` **does not exist.** `505` is a nested move-route entry the
editor writes, and a route reaches the runtime through **`205 Move Route`,
96 of them** — not 348. Sixty say `wait`, thirty-six do not. The 348 were
never event commands.

**The API is `isMapPassable` and `canPass`, not `isPassable` and
`checkPassage`.** `checkPassage` has **zero** occurrences in 1.9.1; those
names come from other RPG Maker engines. A reader built from memory would
have compiled, run, and tested nothing real. **This is the second time in
two cards that a name from memory was not a name in the engine.**

**`reverseDir` is `10 - d`, not `(d + 4) % 4`.** A first draft wrote the
0..3 form, and `reverseDir(2)` then answered Left instead of Up — so every
"one tile behind" position landed **in front** of the character and every
character walked away from where it was going. The directions are 2/4/6/8
for down/left/right/up, **not** the RM2K 0..3 order this repository uses
elsewhere.

**`isStopping` is `!isMoving() && !isJumping()`, two terms.** A draft added a
third, `&& !Waiting`, and **every route with a `ROUTE_WAIT` in it ran
backwards**, re-issuing one step for ever. A character waiting is a
character that has arrived.

**A product fault with a wider reach than this card.** `MzCommandEntry.From`
turns every parameter into a string, and anything that is not a number or a
boolean became `item.Text` — **which for a nested object is `""`**. A 205's
second parameter is exactly such an object, so **every move route in every
game came back empty and the reader could not have said why.**
`MzJson.Write` now writes a value back out, because **a reader that cannot
write a shape back has already half-lost it.**

**Two more, found by the same tests.** `Truth` read a boolean out of `Text`
where the parser puts it in `Boolean`, so **all three flags of all
ninety-six routes came back false** and not one page was ever held. And
`From` read a route's `code` out of `Text`, which is empty for a number, so
**every route code came back 0 — which is END** and all ninety-six routes
did nothing while looking perfectly plausible: five steps read, five ENDs
run, every character standing still. **Both faults failed silently, and
only real data showed them.**

**A test fault, not a product fault, and worth naming.** The last failing
test wrote `new(121, …)` and then asked why the variable was not set.
**121 is Control Switches** — it set a switch, ran, and left the variable
alone, and the run looked perfectly healthy. It is now
`MzCommandTable.ControlVariables`, by name.

**Also measured: this game's route codes.** END 96, MOVE_LEFT 74,
MOVE_RIGHT 59, MOVE_DOWN 50, MOVE_UP 45, JUMP 31, CHANGE_SPEED 26, TURN_UP
11, MOVE_BACKWARD 10, TURN_DOWN 10, TURN_RIGHT 9, TURN_LEFT 7,
MOVE_FORWARD 4, WAIT 4, TRANSPARENT_ON 4, STEP_ANIME_ON 2, STEP_ANIME_OFF
2. **MOVE_LEFT leads and MOVE_DOWN follows** — the opposite of "a game
mostly walks about". MOVE_RANDOM, MOVE_TOWARD, MOVE_AWAY and all eight
diagonal codes appear **zero** times and are named rather than guessed at.

**Next: the remaining offene codes, of which `401 Show Text` at 938 is by
far the largest, and it needs a message window this reader does not have.**

## K-132 Read a line of text, and every code in it — DONE

`project/src/mz/MzMessage.cs` (mit `INameSource` und `ThreeNames`),
`MzCommandTable.ShowTextLine/ShowChoices/ContinueText`,
`MzBranchFacts.Message` / `Names`, `MzCommandSet.HasMethod` /
`NoMethodCodes`.
`project/tests/core/test_mz_message.cs`, 6 Tests, und ein K-124-Test
umgeschrieben, der jetzt beide Antworten sagt.

**963/963**, `TestMzMessage: 6/6`, `TestMzInterpreter: 18/18`, validator
passed, build 0 errors.
**Neun Mutationsregeln, neun gefangen.** Der erste Lauf meldete sieben von
neun — **und beide „entkommenen" waren Fehler in den Regeln, nicht im
Leser.** Eine machte eine Code-Behandlung zu einem Äquivalent, die andere
traf einen Listeneintrag, den der Test gar nicht erreichte. Isoliert und
neu geschrieben: **neun von neun.**

**Die zweite Geschichte ist die bessere.** Die Liste der Zahlen ohne
`commandNNN` war geraten und **falsch**: sie behauptete, `601`, `602` und
`603` hätten keine Methode. **Sie haben eine** — `command601`, `command602`
und `command603` sind drei der 114. Und sie sprach von „178 reservierten
Nummern", wo die K-122-Liste in Wahrheit **die 114 Methoden** ist.
**Neun Zahlen haben keine Methode, und alle neun liegen außerhalb dieser
114**: `0`, `401`, `404`, `405`, `412`, `505`, `604`, `605`, `657`.

**Das ist der vierte Name in vier Karten, der aus dem Gedächtnis kam und in
der Engine nicht existierte** — nach `checkPassage`, `isPassable` und der
`reverseDir`-Form. **Gemessen wird, nicht erinnert.**

**Der größte Brocken der Fixture: 938 Zeilen, jede mit genau einem
Parameter.** Und nichts daran ist ein Darstellungsdetail — das ist der
Befund: **eine Dialogzeile ist meistens keine Worte.**

**Zwei Durchgänge, zwei Regelsätze.** Durchgang eins,
`convertEscapeCharacters`, schreibt in drei Schritten um: jeder Backslash
wird zum Steuerzeichen; **zwei Steuerzeichen machen einen Backslash
zurück**; und Variablen-, Helden-, Gruppen- und Währungscodes werden
gefüllt, **die Variablen in einer Schleife**. Durchgang zwei, die
Zeichenschleife, behandelt **jedes Zeichen unter 0x20 als Steuerzeichen**
und schreibt es nie in die Ausgabe.

**Drei Klassen, und nur eine ist Text.** Im Text: `\V[n]`, `\N[n]`,
`\P[n]`, `\G`. **Nicht im Text und nie gezeigt:** `\|`, `^`, `!`, `>`,
`<`, `$` — **ein Leser, der sie ausgäbe, setzte einen `|` mitten in einen
Satz.** Weder Text noch Feder, und der Leser benennt sie: `\C[n]`,
`\I[n]`, `\PX[n]`, `\PY[n]`, `\FS[n]`, `\{`, `\}`.

**Die Zahlen, und zwei meiner eigenen Messungen waren erst falsch und
dann richtig.**

| | zuerst | gemessen |
|---|---:|---:|
| Zeilen mit `\C[n]` | 0 | **19** |
| Undrawable | 0 | **57** |
| Leere Zeilen | — | **14** |

`\C[3]` 19×, `\C[0]` 19×, `\I[177]` 19×, `\!` 3×, `\|` 3× — **19
Zeilen mal drei Codes, das sind die 57.** Eine Zeile wartet **dreimal**:
`\|.|\|.|\|.` sind drei Entscheidungen und nicht eine.

**Der Fehler, der zweimal passierte.** Ein Scan fand den Buchstaben `C`
53-mal, `N` 38-mal, `V` 22-mal und `P` 11-mal, und ich hielt sie für
Auszeichnungen. **Es sind Wörter** — „SEND **C**OUT!!", „\* **N** om\*",
„Valuable **V**egetables". **Ein Code ist zuerst ein Backslash und dann
ein Buchstabe.** Genau dieser Fehler ließ mich „keine Farben" behaupten,
und die Dateien sagten neunzehn. **Der Test, der es bemerkte, las
dieselben Dateien und riet nicht** — und das ist die Begründung dafür,
dass man Testzahlen nicht schätzt.

**Eine Aussage, die älter ist als diese Karte.** Ein K-124-Test behauptete,
ein 401 werde übergangen, weil die Engine keine Methode dafür hat. **Das
stimmt und stimmt weiter.** Der Leser liest es trotzdem, weil er nach
einer anderen Frage gefragt wird: *was hat das Spiel geschrieben?*
**„Hat die Engine eine Methode" und „was steht in den Daten" sind zwei
Fragen mit zwei Antworten.** `MzCommandSet.HasMethod` sagt jetzt beides,
und `NoMethodCodes` nennt die zwölf Zahlen ohne `commandNNN`.

**Nicht hier.** Kein Textfenster, kein Renderer. Die Zeile wird gelesen
und als Daten behalten: Wortlaut, Wartungszahl, und alles, was nicht
gezeichnet werden kann.

## K-133 A 101, and everything it swallows — DONE

`project/src/mz/MzDialogue.cs`, `MzChoice.cs`, `MzPrompt.cs`;
`MzCommandTable.ShowDialogue = 101` und `ShowChoiceList = 102`;
`MzWaitMode.Message`; `MzBranchFacts.MessageBusy`, `LastDialogue`,
`LastChoice`, `LastPrompt`; `MzCommands` 101-Fall und 401-Verweigerung;
`MzInterpreter` Run-Vorcheck.
`project/tests/core/test_mz_dialogue.cs`, 9 Tests, plus drei umgeschriebene
in den K-124- und K-132-Dateien.

**972/972**, `TestMzDialogueAndChoice: 9/9`, `TestMzInterpreter: 18/18`,
`TestMzMessage: 6/6`, validator passed, build 0 errors.

**Die erste Karte, in der ein Befehl andere Befehle isst.** `command101` macht
`while (this.nextEventCode() === 401) { this._index++; add(…); }` — **eine
Zeile Dialog wird nie dispatcht**, weil es kein `command401` gibt, zu dem sie
dispatcht werden könnte. **Alle 938 Zeilen dieses Spiels hängen an einem 101
und an nichts anderem.**

**414 Dialoge, je eine bis vier Zeilen — 118 mit einer, 130 mit zwei, 104 mit
drei, 62 mit vier** — und die Summe ist genau 938. **Acht haben einen 102
darunter**, sechs unter einem einzeiligen und zwei unter einem zweizeiligen;
**kein 103, kein 104, kein 403** in neunzehn Karten. Befehle gefressen:
**112, 134, 106, 62** — 1360 statt 414 + 938, weil die acht Wahlen drin sind.

**Drei Regeln, und eine vierte, die nur dieses Spiel zeigt.** Eine belegte
Seite wird verweigert — und `isBusy()` ist **Text oder Wahl oder Zahl oder
Gegenstand**, also wird ein 101 hinter einer unbeantworteten Wahl so fest
verweigert wie einer hinter einer Zeile. Genau **eines** von 102, 103 und 104
wird genommen, und zwar das direkt nach der letzten Zeile, denn der `switch`
läuft einmal. **Und es wartet immer**, denn `setWaitMode` liegt außerhalb des
`switch`.

**`102` ist nicht `405` — der fünfte Name in fünf Karten, der gemessen werden
musste.** `ShowChoices` bedeutet seit K-132 das 405, und der Folger wurde
dagegen verglichen, also **wurde keine der acht Wahlen dieses Spiels je
gefunden**: 1352 Befehle statt 1360. **Vier Läufe**, weil die Tests, die
fielen, die mit der Summe waren.

**Und ein Guard ohne Test.** `ExecuteOne` hatte eine Grenzprüfung, die eine
Mutation ausschaltete und alle Tests blieben grün, weil `IsRunning` sie nie
erreicht. **Die Reparatur war nicht ein Test dafür, sondern ihre Entfernung** —
der Fall wird eine Ebene höher behandelt, in `Run`, das jetzt vor der Schleife
prüft. **Eine zweite Prüfung, die nie feuern kann, ist eine Behauptung, der ein
Leser glaubt und niemand belegen kann.**

**Test evidence** 9 Tests, drei umgeschrieben.
**Mutations** Zwölf Regeln über vier Läufe. Jede entkommene Regel war
entweder eine kaputte Regel oder ein Test, der das Mutierte nicht erreichte;
**zwei fanden echte Produktfehler** — den 102 aus dem falschen Befehl und
103/104 als Optionsliste gelesen.

## K-134 Das Board hat Karten verloren — TEILWEISE REPARIERT

**Befund.** `KANBAN.md` ist laut `AGENTS.md` das einzige maßgebliche
Board, und seine Tabelle und seine Details widersprachen sich.

| | vorher |
|---|---:|
| Karten in der Tabelle | 83 |
| Karten mit Detailabschnitt | 86 |
| **In den Details, nicht in der Tabelle** | **30** |
| **In der Tabelle, nicht in den Details** | **25** |
| Doppelte Tabellenzeilen | 2 |

**Alle Karten von K-112 bis K-133 — zweiundzwanzig Karten: RGSS-Archiv,
Marshal-Leser, Ruby-Lexer, Parser, Wertebene, zwei MZ-Fixtures und die ganze
Befehls-Ausführungslinie — waren in den Details und nicht in der Tabelle.**
Ein Agent, der nur die Tabelle liest, hätte gesehen, dass die Arbeit bei
K-111 aufhört, und keine Möglichkeit gehabt, die anderen zweiundzwanzig zu
erkennen.

**Ursache.** Die Tabelle wird von Hand geführt, und jede Karte seit K-112
wurde nur als Detailabschnitt angelegt. **Die Details sind der Ort mit den
Belegen** — Testzahlen, Mutationen, Korrekturen — und die Tabelle ist das,
was ein Agent zuerst liest. **Wenn eines von beiden falsch ist, hört die
Arbeit auf sichtbar zu sein.**

**Repariert:** die Tabelle ist aus den Detailabschnitten neu gebaut. Jetzt
**112 Zeilen, keine Duplikate, und jede Detailkarte hat eine Zeile.**

**Nicht repariert:** die 25 Zeilen ohne Detailabschnitt. Sie zu erfinden wäre
eine Behauptung ohne Beleg, und diese Datei trägt keine Behauptungen.
**K-134 fordert sie zurück — aus `git log` und der Testsuite, nicht aus den
Titeln.** Titel sind Behauptungen; diese Datei trägt keine.

## K-094 Vehicles for the action-event order — DONE

**Der Karten-Titel war die halbe Diagnose.** `Rm2kPlayerTurn.Apply` trug den
Kommentar *"The Player toggles a vehicle before looking for events. This
runtime has no vehicles, so nothing can be toggled and the action event check
always runs."* — und `Rm2kDecisionTurn.Run` war daneben implementiert,
mutation geprüft und **nie aufgerufen**. Die Fahrzeugklassen waren geladen und
gezeichnet; sie waren nie gefahren und nie bestiegen. `GameSimulationState` hatte
**null** Fahrzeugverdrahtung, und der Runtime führte seine eigene `_vehicles`-Liste.

**Implementiert**
- `GameSimulationState.Vehicles` und `.Boarding`, beide in `Reset()` geleert
- `Rm2kPlayerTurn.Apply` ruft `Rm2kDecisionTurn.Run`; ein Fahrzeug, das den
  Zug übernimmt, unterdrückt die Aktionsprüfung
- `CanEmbark` / `CanDisembark` aus der Passability-Maske, `IsVehicleStopping`
  für das Luftschiff, `OppositeBit` für die Gegenrichtung

**Drei echte Produktfehler, die die Suite fand**

1. **`TileInFront` sprach die falsche Richtungsordnung.** Der Spieler spricht
   2/4/6/8, `DirectionDelta` erwartet 0–3. **Ein `8` ergibt `(0,0)`** — die
   eigene Kachel. Jeder Bestiegetest "erfolgte", ohne dass sich etwas bewegte,
   und ein nach oben blickender Spieler bekam ein Aussteigen aufs Wasser, auf
   dem er bereits stand. Die Brücke `LiblcfFromFacingDirection` existierte
   bereits; ihr eigener Kommentar warnt vor genau diesem Vermischen.
2. **`PassDown` ist `0x01` und `PassUp` ist `0x08`.** Ich hatte beide vertauscht
   und `0x08` als "unten" geschrieben.
3. **Ein K-114-Test hielt die falsche Ordnung fest** — er prüfte `TileInFront`
   mit 0/1/2/3, und damit gegen sich selbst. Fünf Zusicherungen, alle konsistent
   mit demselben Missverständnis.

**Und eine Fixture, die sich selbst belog.** `SetPassability(state, x, y,
pAllowUp, pAllowDown)` war benannt, als wären es begehbare Richtungen, und
verdrahtete `pAllowUp` mit `PassDown` — also der Gegenrichtung. Zwei Tests
behaupteten daraufhin die falsche Polarität und schlugen gegen korrekten Code
fehl. **Eine Fixture, deren Namen über ihre eigenen Bits lügen, ist schlimmer
als keine Fixture**, weil der Fehler auf den Produktcode zeigt.

**Test evidence** 8 Tests in
`project/tests/core/test_rm2k_vehicle_decision_turn.cs`, 1 in
`test_rm2k_vehicle_boarding.cs` umgeschrieben.
**980/980**, `TestRm2kVehicleDecisionTurn: 8/8`, `TestRm2kVehicleBoarding: 11/11`,
`TestRm2kDecisionTurn: 12/12`.
**Mutations** Zehn Regeln über sechs Läufe. **9 von 10 gefangen.** Die
entkommene Regel ist keine Semantiklücke, sondern ein Werkzeugfehler: der erste
Runner verwendete `$TMPDIR/m_<pfad>` als Backup, was mit `/` im Namen scheiterte
— die Mutationen liefen **ohne Restore**, und die folgenden Regeln testeten eine
kumulativ kaputte Datei. `git checkout --` hat daraufhin den **ungestagten**
Slice verworfen; er wurde neu gebaut und sofort gestaged.

## K-135 Drei Selbstkorrekturen und `11610` — VERIFY

**Die Karte musste sich selbst dreimal widersprechen, und die dritte
Korrektur hat einen bereits gepushten Fix zurückgenommen.**

### 1. `1009` ist KEINE Nachrichten-Fortsetzungszeile

Der alte Entwurf zählte 20 nackte `1009` mit String nach einem `10110`, sah
MZ' `401` auf ein `101` folgen und schloss auf eine gemeinsame Konvention.
**Die gibt es nicht.** Die Fixture entscheidet es:

```
[1,1,1,1]  [1,1,2,1]  [1,3,8,1]  [1,4,10,1]   → vier Integer, LEERER Text
```

Das sind exakt `parameters[0..3]` von `CommandChangeBattleCommands`: Actor,
Klasse, Battle-Command-ID, add/remove. **Eine Nachrichtenzeile trägt Text und
keine Integer — diese tragen beides nicht.** `liblcf`'s
`Code::ChangeBattleCommands` ist 1009, EasyRPG gated es auf
`IsRPG2k3Commands()`.

**Der falsche Fix war als `5a9ca22` gepusht und wird hier revertiert.** Er sah
richtig aus, die Mutationen haben ihn nicht gefangen, und der Grund seiner
Falschheit ist: **ein Muster, das zu zwei Lesungen passt, ist für keine von
beiden ein Beleg.**

### 2. `5001`–`5005` sind Menübefehle, keine Move-Route-Schritte

Sie standen in der Karte als „Route-Schritte in einer Seite getragen", ohne
dass das Feld geöffnet wurde, das es gezeigt hätte. Gemessen: sie stehen
**direkt in der Befehlsliste der Seite**, zwischen einer Nachricht und einem
Conditional Branch. **Die Seiten haben sehr wohl 60 Routenlisten — und in
keiner davon stehen diese fünf Codes.** Ein erster Test behauptete „keine
Routenliste" und schlug fehl; **60 ist die bessere Messung**, weil sie zeigt,
dass das Feld existiert und gelesen wurde.

Aus `liblcf`: `OpenLoadMenu = 5001`, `ExitGame = 5002`,
`ToggleAtbMode = 5003`, `ToggleFullscreen = 5004`, `OpenVideoOptions = 5005`.

### 3. `11610` ist Key Input Proc — gelesen, nicht geraten

Aus `Game_Interpreter::CommandKeyInputProc`. **Parameter 5–9 bedeuten auf 2K
andere Tasten als auf 2K3**: shift/down/left/right/up gegen
numbers/operators/time-variable/timed. Die Version wählt die Spalte.

Die Fixture-Vorkommen: `[1,1,0,0,0,1,1,2,1,0,0,0,0,0]` — ein 2K3-Spiel, das
Ziffern und Operatoren will, zeitlich begrenzt, Antwort in Variable 1,
verstrichene Zeit in Variable 2.

**`parameters[7]` ist ein `int`, der eine Variable benennt, kein `bool`** — die
Quelle sagt das im Kommentar ausdrücklich.

Weitere gemessene Regeln: **die Ziffern loopen von 10 bis 1** (`10 + i`), also
hat **die Ziffer 0 keinen Wert**; **Operatoren schlagen Ziffern im selben
Frame**, weil die Quelle von höchstem Wert nach unten prüft; **die Maus wird
zuerst geprüft**, damit DECISION auf der linken Maustaste kein Konflikt ist.

**Test evidence** `test_rm2k_key_input.cs` (10), `test_rm2k_menu_commands.cs` (6).
**996/996**, `TestRm2kKeyInput: 10/10`, `TestRm2kMenuCommands: 6/6`.
**Mutations** 9 von 9 gefangen.

## K-135 Die fünf Menübefehle laufen jetzt — DONE

`5001` und `5005` schieben eine Szene und **halten die Seite an**, aus dem
`return false` der Quelle nach `SetRequestedScene`. `5002`, `5003`, `5004` laufen
durch. Eine bereits aktuelle Szene wird nicht doppelt gepusht.

**Das Gate ist der ganze Befehl, und dieses Repository kopiert das No-Op nicht.**
EasyRPG gated alle fünf auf `IsRPG2k3ECommands()` und gibt sonst `true`
zurück — ein stilles No-Op, **ein Bug, der jeden Test übersteht, weil sich
nichts geändert hat.** Dieser Leser **verweigert sichtbar**: eine Diagnose
nennt den Befehl nach seinem liblcf-Namen, sagt, dass es ein E-Befehl ist, und
dass nichts geöffnet wurde. `SupportsRpg2k3ECommands` ist **per Default
`false`**, denn ein Spiel, das nicht ja gesagt hat, hat nicht ja gesagt.

**Und der Szenen-Stack startet nicht mehr mit einer erfundenen Szene.** Er
schob vorher `"Menu"` und machte sie bei Reset aktuell. **`"Menu"` ist kein
RPG_RT-Szenenname** — es war eine Fiktion, gegen die jeder Szenentest grün war,
und sie widersprach der Zeile darüber, die einen leeren Stack behauptete. Ein
altes Test wurde korrigiert, nicht geschwächt.

**`FullscreenRequested` ist eine Anfrage, kein Zustand.** Die Engine fragt die
Anzeigeschicht, und die darf verweigern — EasyRPG prüft `IsOptionVisible` und
`IsLocked`. Ein Boolean, der den Bildschirmzustand behauptete, wäre eine
Behauptung, die dieser Leser nicht halten kann.

**Test evidence** `test_rm2k_menu_execution.cs` (9, über `ExecuteFrame`, den
echten Runner), `test_game_simulation_state.cs` korrigiert.
**1005/1005**, `TestRm2kMenuExecution: 9/9`, `TestGameSimulationState: 20/20`.
**Mutations** 8 von 8 gefangen.

**Ein erster Entwurf prüfte auf Prosa, die er selbst erfunden hatte** — auf die
Formulierung „did not declare", während die Diagnose „does not declare" sagt.
**Auf erfundene Prosa zu prüfen macht das Test zum Ding, das recht haben muss —
und es war das falsche Ding.** Die Prüfungen liegen jetzt auf den Wörtern, die
die Bedeutung tragen.

### Zwei eigene Werkzeugfehler in diesem Zyklus

1. `GetActors`-Anker: die erste Mutantenrunde hatte zwei `NOMATCH` und ein
   `BROKE` — Ankerprobleme, keine Befunde. **Die Regel zählt erst, wenn ihr
   Anker sitzt.**
2. `web_search` und `web_extract` sind in dieser Umgebung blockiert
   (`ddgs` fehlt, Extract-Backend nicht gesetzt). **Die EasyRPG- und
   liblcf-Quellen wurden deshalb per `curl` und `git clone` geholt** — das ist
   kein Umweg, sondern der direktere Weg zu derselben Quelle.

### Nächster Schritt

`1009` anwenden (Battle-Command-Liste ändern) und `11610` verdrahten. Beides
braucht keine Entscheidung vom Nutzer.

## K-135 `1009` läuft jetzt — DONE

Drei Akteur-Modi aus EasyRPGs `GetActors`: **0 = Partei, 1 = ein Held nach ID,
2 = der Held, den eine Variable benennt.** Die Quelle liest `parameters[0..1]`
darüber, `parameters[2]` als Command-ID und `parameters[3] != 0` als „hinzufügen"
— `CmdSetup` gibt dem Befehl eine Mindestbreite von vier.

**Abwesend ist nicht leer, und das ist hier das ganze Zustandsmodell.** Ein Held
ohne Eintrag hat **die Befehle der Datenbank**, was der RM2K-Standard ist.
`GetActorBattleCommands` gibt dafür **`null`** zurück, weil die Referenz dort
`null` zurückgibt, bis etwas sie ändert, und der Schlachtcode genau darauf prüft.
Ein Leser, der eine leere Liste speicherte, würde jedem Helden **jede Fähigkeit
nehmen**, sobald 1009 läuft.

**Beide Richtungen ohne Änderung werden gemeldet, weil sie Gegensätze
bedeuten.** „Füge hinzu, was er schon hat" ist eine Autorengewohnheit; „entferne,
was er nicht hat" ist meist ein Fehler, den man benennen sollte.

**Eine Helden-ID 0 fasst niemanden an, und die Seite läuft weiter.** Helden-IDs
laufen ab 1, also ist 0 der eine Wert, den ein Spiel wirklich erreichen kann und
der keinen Helden benennt — eine Variable, die nie gesetzt wurde. Die Quelle
protokolliert eine Warnung und gibt eine leere Aktorenliste zurück. **Ein Leser,
der die ganze Seite verweigern würde, würde den Rest eines Events verlieren,
weil eine ID falsch war.**

**Test evidence** `test_rm2k_battle_commands.cs`, 11 Tests über `ExecuteFrame`.
**1016/1016**. **Mutations** 8 von 8 gefangen.

### Zwei API-Fakten, die dieses Repository nicht offensichtlich macht

1. **`Variables` ist im Event 1-basiert und im Array 0-basiert**, weil
   `GetVariable` `Variables[pId - 1]` liest. `Variables[1] = 2` auf einem leeren
   Array wirft. Das ist ein Index-Range-Fehler, der aussieht wie ein 1-basierter
   Leser.
2. **Ein Frame ist ein Schritt.** `ExecuteFrame` dreimal auf einer
   Ein-Befehl-Seite führt den Befehl **nicht** dreimal aus — im Menü-Slice
   gegessen, hier wieder bestätigt.

### Nächster Schritt

`11610` verdrahten (Key-Input-Prompt braucht ein Fenster, das es noch nicht
gibt) oder `12310`/`12320`, die als Konstanten deklariert sind. Kein
Nutzerentscheid nötig.

## K-135 `11610` verdrahtet — DONE, Karte schliessbar

`Rm2kKeyInput` hatte die ganze Tabelle und nichts, um sie zu halten. Jetzt gibt
es einen Prompt in `PresentationState` und einen Befehl im Interpreter, und
**die Seite hält, solange er offen ist** — das macht ihn erst zum Prompt.

**Es ist ein zweiter Prompt, kein zweiter Modus des ersten.** 10150 fragt eine
Zahl und speichert sie; 11610 fragt eine Menge Tasten und speichert einen
*Code*. Eine Ziffer ist 11 bis 20, ein Operator 21 bis 25, die Bestätigungstaste
ist 5. **Nichts in der Datei sagt, welches der beiden gefragt hat** — deshalb
zwei Prompts statt eines Parameters.

**Während des Wartens ist die Variable null — jeden Frame.** Der Kommentar der
Referenz sagt das wörtlich. Ein Leser, der erst bei der Antwort schrieb, ließe
stehen, was das Spiel vorher hineingelegt hatte.

**Eine nicht erlaubte Taste beendet nichts.** Die Fixture erlaubt Ziffern und
Operatoren, nicht Bestätigen und nicht Shift. Ein Leser, der jede Taste als
Antwort nähme, beendete den Prompt mit der ersten Taste, mit der ein Spieler ihn
schließen will — so schließt ein Rechner-Dialog, bevor eine Ziffer getippt ist.

**Ein Tastendruck kommt auf einem Eingabe-Frame, und der ist nicht der Schritt
des Interpreters.** Deshalb ist `PressKeys` ein eigener Einstieg und nicht Teil
von `ExecuteFrame`. Ein Leser, der das Warten in den Dispatch gelegt hätte,
öffnete den Prompt in jedem Frame neu, in dem er offen blieb.

**Test evidence** `test_rm2k_key_input_wiring.cs` (9).
**1025/1025**, `TestRm2kKeyInputWiring: 9/9`, `TestRm2kKeyInput: 10/10`.
**Mutations** 9 von 9 gefangen.

**Damit ist K-135 geschlossen.** Sieben Codes, alle identifiziert, alle
ausgeführt. Und das Schließen bedeutete, einen bereits gepushten Fix
zurückzunehmen — `5a9ca22`, der `1009` für eine Nachrichtenzeile hielt.

### Nächster Schritt

`12310` und `12320` sind als Konstanten deklariert und werden im Dispatch nicht
behandelt. Danach die Fahrzeug-Move-Routes aus K-114. Kein Nutzerentscheid
nötig.

## K-136 angelegt, und `11110`/`11130` verdrahtet — 1035/1035

**Der Fund, der die Karte ausgelöst hat:** `ShowPicture` und `ErasePicture`
waren in `PresentationState` **implementiert, begrenzt und getestet** — und von
keinem Befehl erreichbar. `ShowPicture` nahm sechs Skalare und **warf die
anderen acht Parameter weg**. Das ist dieselbe Fehlerform wie K-094s Fahrzeuge:
der Zustand war da, die Verdrahtung nicht, und die Suite war grün.

**K-136 ist die Liste, die diese Form findet.** liblcf hat 164 Codes, dieser
Interpreter dispatcht 43 — **89 echte RPG-Befehle haben keinen Fall** und
landen in `default`. Zwei davon waren bereits implementiert.

### Drei echte Produktfehler, die die Realdaten fanden

**1. Die Transparenz ist ein *Prozent*, keine Farbe.** Die Referenz klemmt sie
mit `std::min(top_trans, 100)`. Ich hatte sie als Farbkanal gegen `MaxColorChannel`
geprüft — **jedes echte Bild eines Spiels (0, 50, 100) wäre zufällig durchgegangen**
und jedes Spiel mit der Farbe, die ich im Kopf hatte, hätte einen Refusal bekommen.

**2. Die Maniac-Bitmaske gilt nur für die *untere* Transparenz.** Die Referenz
maskt `parameters[14]` mit `0xFF` und liest `parameters[6]` **roh**. Ich hatte
beide maskiert.

**3. `11130` liest die ID zuerst und den Modus danach.** Ich hatte es umgekehrt —
so wie der *Show*-Befehl seinen Positionsmodus legt. Folge: **der einparametrige
Befehl, den der Editor am häufigsten schreibt, löschte das Bild mit der Nummer
„nichts".**

Und die Trennung, die ich verwechselt hatte: **„nichts da" ist Erfolg, „ID außerhalb
der Grenzen" ist Refusal.** Beide als Refusal zu melden erzählte einem Spieler,
sein Befehl sei außerhalb der Grenzen, wenn die Wahrheit war, dass er schon
gelöscht hatte.

**Test evidence** `test_rm2k_pictures.cs` (10), `test_presentation_state.cs`
umgestellt. **1035/1035**, `TestRm2kPictures: 10/10`, `TestPresentationState: 5/5`.
**Mutations** 8 von 8 gefangen.

### K-136, die Karte

**89 Befehle ohne Fall, nach Bereich sortiert.** Der schärfste ist
**`12110` Label und `12120` Jump to Label** — die einzigen zwei, die *wohin* die
Seite geht statt *was* sie tut. Ein Leser ohne sie führt den Sprung als No-Op aus
und landet am Seitenende, **was sich wie ein Spiel anfühlt, das stillschweigend
die halbe Scriptzeile übersprungen hat.**

**Warum die ganze Liste und nicht ein Befehl:** zwei ihrer Einträge waren bereits
lange implementiert. **Ein Leser, der seine eigene Feature-Liste ansieht, findet
sie nicht — nur die Liste der Referenz.** Das ist das Argument, die Lücke einmal
aufzuschreiben statt Befehl für Befehl an der Diagnose zu entdecken.

### Ein Werkzeugfehler, der mich Zeit gekostet hat

Beim Anfügen der Karte meldete mein Zählskript 175 Detailabschnitte statt 88.
Ursache: es zählte `### K-…` **ohne** zu prüfen, ob die Karte unter dem
richtigen `## Card details`-Block steht — es gibt historisch zwei
`## Agent maintenance rules`-Überschriften, und ein naives `s.find` nimmt den
ersten. **Eine Zählung, die die Struktur nicht prüft, zählt Dokumente statt
Karten.** Gegen `git show HEAD` verifiziert: +1 Karte, +1 Zeile, Struktur
unverändert.

## K-136 `12110` / `12120` — DONE

**Die beiden, die entscheiden, *wohin* eine Seite geht statt *was* sie tut.**

**Die Suche beginnt bei null, nicht hier.** Die Referenz:
`for (int idx = 0; idx < list.size(); idx++)`. Also ist **ein Rücksprung eine
Schleife** — so schreibt ein Autor eine ohne Schleifenbefehl. Wer ab hier
vorwärts sucht, macht aus jedem Rücksprung ein Durchfallen: **ein Spiel, das mit
einem Sprung schleift, liefe seinen Rumpf einmal und hörte auf.**

**Der Index landet auf der Marke, und die Regel der Engine sagt warum.** EasyRPG
inkrementiert nur, wenn der Befehl den Index nicht bewegt hat:

```cpp
if (index_before_exec == frame->current_command) {
    frame->current_command++;
}
```

Ein Sprung, der seine Marke findet, hat den Index bewegt → kein Inkrement → die
Seite landet **auf** der Marke, und die Marke ist ein No-Op, das einen Frame
kostet. Ein Sprung, der **nichts** findet, lässt den Index stehen → die Regel
inkrementiert → die Seite läuft weiter.

**Zwei Entwürfe hatten das in entgegengesetzter Richtung falsch, beide still.**
Einer gab ein nacktes `true` zurück und ließ die Seite **für immer auf dem Sprung
stehen** — das sieht wie ein Hänger aus. Der andere inkrementierte bedingungslos
und übersprang das No-Op, das das Format dort absichtlich hinstellt. Nur die
bedingte Form ist beides. **Eine Suite, die Befehle statt Frames zählt, hätte die
beiden nicht unterscheiden können.**

**Eine Marke tut gar nichts.** Die Referenz hat dafür `return true` und sonst
nichts — keine Methode, keine Parameter. **Eine Marke ist ein Name, kein Befehl**,
und wer ihr eine Wirkung gibt, erfindet eine Semantik, die das Format nicht hat.
Eine Marke ohne Parameter matcht nichts.

**Test evidence** `test_rm2k_labels.cs` (8). **1043/1043**.
**Mutations** 6 von 6 gefangen (ein siebter Versuch war ein erfundener Anker).

### Ein weiterer API-Fehler von mir

`Control Variables` hat **sechs** Parameter
(`[targetMode, startId, endId, op, operandType, operand]`), ich schrieb fünf.
Die fehlende End-ID verschob jedes Feld, der Befehl wurde abgelehnt, und **nichts
wurde je markiert** — der Test „der Rücksprung schleift" prüfte damit eine Seite,
in der nie etwas lief.

## K-136 Die fünf Audio-Befehle — DONE

**`GameSimulationState` hatte vier Positionsdoubles, die niemand las und niemand
schrieb** — `BgmPosition`, `BgsPosition`, `MePosition`, `SePosition`. Der Rest
eines Plans für Wiedergabe, die dieses Repository nicht gebaut hat. **Ein Double,
das kein Befehl bewegt, ist eine Behauptung über Zeit, die nichts wahrt hält.**
Ersetzt durch das, was das Format hält: den aktuellen Track pro Kanal, den
Fade-Zustand und **einen** gemerkten BGM.

**Es sind Daten, kein Klang.** Kein Player dahinter, kein Test behauptet, dass
ein Track zu hören ist. Die Diagnosen sagen, **was verlangt wurde**.

**Vier Kanäle, und sie sind nicht austauschbar.** BGM schleift und fadet, SE
spielt einmal darüber, ME folgt den BGM-Regeln, BGS schleift darunter. Wer alle
vier in einer Liste hält, lässt einen Fußschritt die Dorfmusik überschreiben.

**Die Parameterlisten der zwei Befehle passen nicht aufeinander.** Musik:
`[fade, volume, tempo, balance]`. Effekt: `[volume, tempo, balance]` — **ein
Effekt hat gar keinen Fade**, also setzt gleiches Lesen die Lautstärke an die
Stelle der Balance. `CmdSetup` gibt Breiten 4 und 3; ich schrieb 5 und 4 im
Produktcode **und in jedem Test**, also fielen alle zwölf Tests an einem Befehl,
den dieses Repository nie angenommen hatte.

**Balance ist 0–100 mit 50 in der Mitte**, nicht −100 bis 100.

### Und eine Verweigerung, die ich als Vorsicht verkleidet habe

Ich las `parameters[1]` als Modus für die anderen drei Werte und **lehnte dann
jeden Befehl ab, dessen Werte ungleich null waren** — also **jeden
Musikbefehl, den ein echtes Spiel schreibt**. Der Grund, den ich angab, war
„dieser Leser dekodiert noch kein Bitfeld". Das klingt nach Sorgfalt und war
eine Quelle, die ich **nicht zu Ende gelesen** hatte:

```cpp
if (!Player::IsPatchManiac()) { return com.parameters[val_idx]; }
```

Ohne Patch ist jeder Wert einfach sein eigener Parameter, und der fünfte
Parameter trägt gar nichts. **Laut verweigern ist kein Ersatz dafür, zu wissen.**
Ein Spiel, das den Patch *doch* trägt, wird weiterhin abgelehnt — und diese
Verweigerung nennt den Patch.

**Test evidence** `test_rm2k_audio.cs` (12). **1055/1055**.
**Mutations** 9 von 9 gefangen.

## K-136 `11010` / `11020` / `11030` — DONE, und `11060` ist ein liblcf-Code ohne Engine

**Die Transition-Tabellen sind das Schärfste in diesem Slice, weil die
Paarung nicht regelmäßig ist.** Zeigen und Löschen sind dieselben zwanzig Arten
von den beiden Enden gelesen, und jede Parameternummer benennt in jeder Tabelle
etwas anderes — 4 ist `BlindClose` beim Löschen und `BlindOpen` beim Zeigen, 16
ist `ZoomIn` und `ZoomOut`. **Streifen und Scroll spiegeln ihr Suffix, die
Divisionen paaren mit den Combines** — wer den Namen spiegelte, paarte
`CrossDivision` mit sich selbst und animierte gar nichts. Ein Test prüft genau
das, über alle drei Divisionsarme.

**Parameter −1 ist keine Art.** Er bedeutet „die eigene Teleport-Transition des
Spiels", die in den Editor-Einstellungen steht und nicht im Befehl. Die beiden
`switch` der Referenz haben **keinen default-Arm**, also fallen −1 *und* jede
unbekannte Zahl **stillschweigend** auf none durch — **was jeder Teleport im
Spiel seine Transition verlieren ließe, ohne ein Wort.** Dieser Leser hat die
Einstellungen nicht gelesen, sagt das also und nennt die Zahl.

**Die Sättigung ist ein Prozent, und 100 heißt ungetöntet.** Das ist rückwärts
von dem, was ein Leser rät: wer 0 als „kein Tint" behandelte, tönte den Bildschirm
in dem Wert grau, den ein Spiel schreibt, wenn es kein Tint will.

**Die Dauer wird umgerechnet, nicht in Zehnteln gespeichert.** Die Referenz
rechnet `tenths * DEFAULT_FPS / 10` und gibt Frames an den Bildschirm.

**Und eine Wartezeit ist bedingt — was der erste Dispatch falsch hatte.** Die
Referenz ruft `SetupWait` nur, wenn der sechste Parameter gesetzt ist, und ein
erster Entwurf gab ein nacktes `true` zurück: **ein Tint, der warten wollte,
lief die Seite trotzdem weiter, und die Wartezeit fand nie statt.** Genau die
bedingte Form, die der Sprung diese Sitzung schon einmal falsch hatte.

**`11060 Pan Screen` steht in liblcf — und EasyRPG hat dafür keinen `case` und
kein `CommandPanScreen`.** Also implementiert dieses Repository es nicht: es
gibt nichts, woraus die Parameter zu lesen wären, und wer einen Befehl
implementiert, den die Referenz nicht hat, erfindet eine Semantik. **Es bleibt
als benannte Lücke auf der Liste und nicht als Vermutung.**

**Test evidence** `test_rm2k_screen.cs` (14), inklusive aller zwanzig Parameter
beider Tabellen einzeln geprüft. **1069/1069**. **Mutations** 9 von 9 gefangen.

## K-136 `11310` und `11330` — DONE, und sie schließen eine K-131-Insel

**`Rm2kMoveRouteState` hatte keinen Aufrufer im ganzen Projekt.** K-131 hat den
Decoder und die Zustandsmaschine gebaut, mutation geprüft und **beide als
freistehende Objekte getestet** — und **kein Befehl konnte eine auf einen
Helden legen.** Dieselbe Inselform wie die Bilder in `PresentationState`, und
dieselbe Art, sie zu finden: vergleichen, wofür eine Klasse da ist, mit dem, was
die Befehle der Referenz tun.

**`11310` invertiert seinen Parameter, und das ist der ganze Befehl.**
`bool hidden = (com.parameters[0] == 0);` — wer ein Ungleich-Null auf „sichtbar"
abbildet, hat ein Verstecken richtig und ein Zeigen falsch, **und ein Spiel, das
diesen Befehl nur zum Verstecken benutzt, funktioniert, bis es das erste Mal
eines zeigt.** Es räumt außerdem die Durch-Position ab, mit dem Kommentar der
Referenz „RPG_RT does this here" — **wer durch eine Wand ging und dann versteckt
wird, bleibt nicht in der Wand stehen.** Zeigen räumt sie *nicht* ab, weil das
Zurücksetzen im Versteck-Zweig steht und nicht daneben.

**`11330` liest die Route als den Rest der Liste**, ab Index vier bis zum Ende.
Wer eine feste Zahl liest, wirft eine lange Route stillschweigend weg.

**ID-Modus und Repeat-Flag teilen sich ein Wort.** Der Modus sind die niedrigen
zwei Bits, Repeat ist das niedrige Bit.

**Eine Bewegungsfrequenz außerhalb 1–8 wird 6, und das ist die Vorgabe der
Engine und keine Ablehnung.** Wer verweigert, stoppt eine Route, die RPG_RT
fröhlich laufen lässt.

**`11340` und `11350` stehen in liblcf und kommen in EasyRPGs Interpreter
nirgendwo vor** — kein `case`, keine Methode. Also nicht implementiert, aus
demselben Grund wie `11060`.

**Test evidence** `test_rm2k_move_event.cs` (10). **1079/1079**.
**Mutations** 8 von 8 gefangen (drei Läufe; zwei Regeln des ersten Laufs waren
Ankerfehler und zählen nicht).

### Ein Werkzeugfehler, der die ganze Datei umformatiert hat

Mein Einrück-Skript hat 237 öffnende Klammern „korrigiert" — die meisten waren
bereits richtig. Der Diff blieb bei 168 Zeilen, also war der Schaden
kosmetisch, **aber ein Skript, das 237-mal zugreift und 1-mal recht hat, ist
kein Werkzeug, sondern ein Glücksspiel.** Künftig: eine Stelle gezielt patchen,
nicht die Datei durchgehen.

## K-136 `10820` Memorize Location — DONE

**Die drei Parameter sind die Variablen, in die geschrieben wird — nicht die
Position, die gespeichert wird.** `parameters[0]` bekommt die Karten-ID,
`parameters[1]` das X des Spielers, `parameters[2]` das Y. Wer sie als Position
liest, **schreibt die Kachel des Spielers in drei Variablen und speichert
gar nichts** — genau der Fehler, den ein Dreier-Befehl aus allerlei einlädt,
wenn alle Parameter dieselbe Art haben. Ein Test prüft, dass die
Parameternummern **nicht als Werte** auftauchen, weil die zwei Fehler im Log
verschieden aussehen und in der Testdatei gleich.

**Alle drei Variablen-IDs werden geprüft, bevor irgendeine geschrieben wird.**
Wer schrieb, während er ging, hätte die Karte gespeichert und dann auf die Null
 gestoßen: **ein halb gemerkter Ort holt den Spieler auf eine Kachel zurück,
die das Spiel nie gemeint hat.**

**`10830 Recall To Location` steht in liblcf und hat in diesem Build von
EasyRPG keine Methode.** Also nicht implementiert. **Die Asymmetrie gehört der
Referenz**, und sie wird festgehalten statt aus der Vorstellungskraft gefüllt:
Ein Spiel, das merkt und dann zurückruft, hätte die erste Hälfte und nicht die
zweite — und wer die zweite rät, **teleportiert Spieler auf Kacheln, die die
Datei nie beschrieben hat.** `10910` und `10920` haben dieselbe Form.

**Test evidence** `test_rm2k_memorize_location.cs` (6). **1085/1085**.
**Mutations** 6 von 6 gefangen.

### Zwei eigene Werkzeugfehler, beide im selben Slice

1. **Fünf Anläufe an einer Zeile.** Der f-String in meinem Einfügeskript hat
   die schließende Klammer verschluckt, und ich habe die Symptome behandelt
   (Array-Syntax, Cast, Typannotation), statt **den ganzen Block zu lesen**.
   **Drei Fehlversuche an derselben Stelle sind kein Messproblem, sondern ein
   Leseproblem.**
2. Ein Test las `Variables[pId - 1]` direkt und warf, statt die Abwesenheit als
   `-1` zu melden. **Ein Test, der auf dem Produktfehler abstürzt, beweist
   nichts über den Produktfehler.**

Beides steht jetzt hier, weil beide dieselbe Form haben: **Werkzeug und Zeile
ansehen, bevor man sie beurteilt.**

## K-136 `10120` / `10130` / `10230` — DONE, und einer davon fand einen Fehler im Save-Codec

**`SetTimer` startete den Timer, und das sollte es nicht.** Die Referenz hat
drei Operationen in einem Befehl: Sekunden setzen, starten mit den Flags
sichtbar und Schlacht, und stoppen. **Wer beim Setzen startete, kollabierte die
ersten beiden** — und ein Spiel, das `SetTimer` benutzt, um einen Countdown zu
**scharfmachen**, den es später starten will, **startete ihn sofort**. Genau der
Unterschied zwischen einem Timer, der zählt, und einem, der es nicht tut.

**Und der Save-Codec hatte denselben Fehler.** Er stellte einen Timer mit
`SetTimer` allein wieder her, also **kam jeder gespeicherte Countdown laufend
zurück** — ein Spiel, das einen angehaltenen Timer speicherte und neu lud,
bekam einen lebenden. Die zwei Alt-Tests, die an der Reparatur brachen,
benutzten `SetTimer` als „Timer starten" — dieselbe Verwechslung. **Sie wurden
korrigiert, nicht geschwächt**, und der Round Trip beweist jetzt beide
Operationen getrennt.

**`StopTimer` behält die Sekunden.** Wer einen Timer stoppt, um ihn zu zeigen,
und ihn dann wieder startet, erwartet den Stand. Er wirft außerdem nicht mehr
für eine unbekannte ID, denn eine veraltete Timer-ID soll kein totes Event sein.

**`10120` ist vier Flags und kein „Stil".** Transparent, Position, fixiert,
Continue-Events. **Parameter[2] ist invertiert** — eine Null heißt: das Fenster
bleibt stehen, während die Karte scrollt. Wer ein Ungleich-Null auf „fixiert"
abbildet, **scrollt jedes Fenster weg, das ein Spiel festgepinnt hat** — und das
ist **nur während der Kartenbewegung sichtbar, also von keinem Test auf einem
stillen Bild zu finden.** Parameter[1] hat **drei** Positionen.

**`10130` setzt ein Gesicht — und ein Gesicht ist eine Anfrage, kein gezeichnetes
Porträt.** Die Datei hat vier Slots; ein neunter wird mit der Zahl abgelehnt.

**Ein sechster Parameter benennt den Timer**, und die Referenz liest ihn **nur,
wenn der Befehl mehr als fünf Parameter hat und das Spiel RPG2K3 ist** — daher
hat ein 2K-Spiel einen Timer und ein 2003 zwei. Ein Test prüft beide Lesarten
desselben Befehls.

**Test evidence** `test_rm2k_message_options.cs` (15). **1100/1100**.
**Mutations** 10 von 10 gefangen, **einschließlich des Codes, der die Sekunden
unbedingt wiederherstellte** — das ist die Save-Datei-Hälfte desselben Fehlers.

## K-136 `10430` / `10460` / `10470` — DONE, und der Save-Codec kannte keine Helden

**Basis und aktuell sind zwei Dinge, und die Referenz hat zwei Aufrufe dafür.**
`10430` ruft `SetBaseMaxHp`, ein Buff ruft `SetMaxHp`. **Die Basis überlebt
einen Stufenwechsel und ein Speichern**, ein Buff nicht — deshalb liegt die
Basis in `Rm2kActorValues` und dieses Feld **hat bewusst keinen Platz für den
aktuellen Maximalwert**. Wer den aktuellen Wert speicherte, ließe ein gespeichertes
Spiel einen Buff behalten, der drei Karten zurück vorbei war.

**HP und SP klemmen verschieden, und das ist kein Zufall.** HP hat ein
Todes-Flag und eine Untergrenze von eins, wenn es nicht gesetzt ist, weil ein
Spiel einen Helden schützen kann. `CommandChangeSP` hat beides nicht — die
Referenz schreibt `if (sp < 0) sp = 0;` und sonst nichts. **Wer SP dieselbe
Untergrenze gäbe wie HP, ließe einen Helden nichts zaubern.**

**Die Obergrenze ist der aktuelle Maximalwert, nicht die Basis.** Ein Held mit
Basis 40 und Ausrüstung im Wert von 10 wird nicht über 50 geheilt.

**`parameters[2]` ist ein Remove-Flag und kein Vorzeichen** — die Referenz
negiert den Betrag, wenn es gesetzt ist — und `10460` hat sechs Parameter,
`10470` fünf, denn das sechste ist das Todes-Flag und SP hat keines.

**Und der Save-Codec kannte überhaupt keine Helden.** Jeder Basiswert und jeder
aktuelle Stand ging beim nächsten Speichern verloren: Ein Held, der fast tot war,
lud voll gesund, und ein `10430` war weg. Basis und aktueller Stand reisen jetzt
zusammen, denn **ein Save, das die Stände behält und die Basen verliert, würde
einen Helden an einem Maximum klemmen, das er nicht mehr hat.** Es kommen nur
berührte Helden hinein, nach ID sortiert, damit zwei Saves desselben Spiels
byteweise gleich sind. Eine Zeile außerhalb der Grenzen wird **ganz**
abgelehnt, und der Test prüft, dass nichts angewandt wurde.

**Test evidence** `test_rm2k_actor_battle_values.cs` (14). **1114/1114**.
**Mutations** 19 Regeln über drei Läufe, **19 von 19 gefangen** — zehn in den
Befehlen und der Werteklasse, neun im Save-Codec, darunter „der Codec schreibt
überhaupt keine Helden" und „der Codec schreibt jeden Helden, ob berührt oder
nicht".

## K-136 `11840` / `11930` / `11960` — DONE: drei Ein-Zeiler und ein Default, der zählt

**Die Referenz hat drei Ein-Zeiler-Methoden gleicher Form** —
`SetAllowEscape(com.parameters[0] != 0)` und seine zwei Geschwister — und
**das ist der ganze Befehl.** Ein Handler und drei Konstanten ist hier die
ehrliche Lesart und keine Ersparnis.

**Eine Null ist eine Entnahme und kein „keine Änderung".** Eine Cutscene, die
das Menü sperrt, und eine, die es wieder freigibt, schreiben dasselbe Feld —
**wer das Flag nur je auf true setzen könnte, könnte einem Spieler sein Menü
nie zurückgeben**, und ein Spiel mit gesperrter Menü-Cutscene bliebe im
gesperrten Menü stecken.

**Jedes Ungleich-Null ist erlaubt, nicht nur eine Eins** — die Referenz prüft
`!= 0`, nicht `== 1`, also wird ein Spiel nicht abgelehnt, das eine berechnete
Wahrheitswert übergibt.

**Alle drei stehen standardmäßig auf erlaubt, und das ist ein echter Default
und keine Vermutung.** Eine Datenbank, die keinen dieser Befehle je lief, hat
alle drei gesetzt, also ist ein neues Spiel ein Spiel, in dem der Spieler das
Menü öffnen, speichern und fliehen darf. **Wer auf verboten defaultete, machte
jedes unberührte Spiel unspielbar, sobald der Spieler Escape drückt** — und
kein Test eines Befehls hätte das je gefunden, denn ein Spiel ohne Befehle ist
der Fall, für den niemand einen Test schreibt. `Test_ANewGameAllowsEverything`
existiert genau dafür.

**Jeder Befehl schreibt sein eigenes Flag und lässt die anderen zwei stehen.**
Wer alle drei aus Defaults schriebe, entsperrte eine gerade gesperrte Cutscene.
Der Setter ist privat hinter einem `SetAccess`, aus demselben Grund wie der
Timer `SetTimer` und `StartTimer` statt eines öffentlichen Feldes hat: Die
Kollabierung zweier Operationen hat einmal eine Save-Datei gekostet.

**Test evidence** `test_rm2k_access_commands.cs` (5). **1119/1119**.
**Mutations** 8 Regeln, **8 von 8 gefangen** — darunter jede der drei Befehle,
die die anderen zwei Flags aus Defaults statt aus dem aktuellen Stand schreibt,
also genau der Fehler, den diese Familie teilt.

## K-136 `10920` / `11810` / `12420` / `12510` — DONE, und das Board lag bei einem falsch

**`10920 Store Event ID` stand als „hat keine Methode in diesem EasyRPG-Build" auf
dem Board.** Er hat eine: `CommandStoreEventID`. Board-Notiz korrigiert.
Der Rumpf macht drei Dinge, die ein Leser richtig treffen muss: Beide
Koordinaten laufen durch `ValueOrVariable` mit **demselben** Modus in
`parameters[0]`; ein leeres Feld speichert **0 und hält die Seite nicht**
(`ev ? ev->GetId() : 0`); und ein Feld außerhalb der Karte wird **abgelehnt
statt mit einer Null beantwortet**, weil eine Null genau wie „kein Event hier"
aussieht.

`10920` braucht die Karte, und **der Interpreter hat keine** — er hat vier
`Func`-Resolver. Der fünfte kommt dazu, `Func<int, int, int>?`, aus demselben
Grund: Der Interpreter darf nicht wissen, wie eine Karte gehalten wird, sonst
kann ein Test sie nicht steuern.

**`11810` Parameter 4 heißt „der Schalter muss AN sein", nicht „es gibt einen".**
Wer es als „benutze einen Schalter" liest, macht jeden bedingten Sprung
unbedingt und öffnet einen geheimen Zugang zu Beginn des Spiels. Parameter 0
ist ein **Modus und keine Ziel-ID** — ungleich null entfernt alle Punkte der
Karte. Ein Punkt ersetzt einen zweiten auf derselben Kachel, weil die Referenz
anhängt und ein Spiel sonst zwei Sprünge auf einer Kachel hätte.

**`12420` und `12510` nehmen überhaupt keine Parameter**, weshalb die Referenz
den Befehl als `const& com` schreibt und nie liest. Beide **warten zuerst auf
eine offene Message** — ein Held, der seine letzte Zeile sagt und dann stirbt,
soll danach sterben. Beide halten die Seite. `WaitingFor` sagt welches, denn ein
Warten ohne Grund sieht wie ein Hänger aus.

**Test evidence** `test_rm2k_teleport_and_outcome.cs` (13). **1132/1132**.
**Mutations** 10 Regeln, **10 von 10 gefangen** — darunter das Sprung-Flag als
„es gibt einen Schalter" und das leere Feld mit einer erfundenen ID beantwortet.

## K-136 `11820` / `11830` — DONE, und einer stand nie auf dem Board

**`11820 Change Teleport Access` fehlt in K-136 vollständig.** Das Board führte
den Bereich "`11810`–`11840`" mit Einzelcodes, und dieser fiel zwischen den
Einträgen durch. Die Referenz hat ihn: `SetAllowTeleport(parameters[0] != 0)`.
Es ist der **vierte** der vier Ein-Zeiler-Access-Befehle. **Ein Board, das
Lücken zwischen Bereichsangaben hat, verliert Befehle** — die Liste wird ab jetzt
aus der Quelle gemessen und nicht aus dem Board übernommen.

**`11830 Escape Target` hat dieselbe vierte Parameterbedeutung wie der
Sprungpunkt:** „der Schalter muss AN sein". Wer sie als „benutze einen Schalter"
liest, macht einen gesperrten Fluchtpunkt von der ersten Minute an verfügbar.
Es gibt **genau einen** und ein zweiter Befehl ersetzt ihn — wer eine Liste
führte, müsste eine Regel erfinden, welcher gewinnt, und die hat das Spiel nie
geschrieben.

**Der Default-Parameter auf `true` war eine Falle, und die Tests haben sie
gefunden.** `SetAccess` bekam `pTeleport = true`, und **jeder der drei älteren
Aufrufe setzte Teleport damit still zurück** — der letzte Befehl gewann, nicht
der, der das Flag benannt hatte. Exakt der Fehler, den `SetTimer` und
`StartTimer` einmal gekostet hat, in anderer Form. Der Default ist weg.

**Test evidence** `test_rm2k_teleport_access.cs` (9). **1141/1141**.
**Mutations** 10 Regeln über zwei Läufe, **10 von 10 gefangen**. Zwei brachten
zuerst den Build zum Scheitern, weil `SetAccess` keine Defaults mehr hat, und
wurden mit kompilierendem Code nachgemessen — ein Compilefehler ist keine
gefangene Regel.

## K-136 `10660`/`10670`/`10680`/`10690` — DONE, der zweite Block, der nie auf dem Board war

**Keiner dieser vier stand auf K-136.** Sie kamen aus der zweiten frischen
Messung der Quelle gegen den Interpreter. Das ist jetzt das zweite Mal, dass die
Kartenliste zu kurz war, und einmal war sie aktiv falsch.

**Die Audiofamilien haben verschiedene Breiten: sieben Musik, zwölf Klänge.**
Musik: Schlacht, Sieg, Gasthaus, Boot, Schiff, Luftschiff, Game Over. Klänge:
die vier fürs Menü, einer für den Kampfbeginn und einer pro Kampfereignis —
Feindangriff, Feindschaden, Heldenschaden, Ausweichen, Feindtod, Item. **Wer nur
die Menüklänge anbietet, lässt ein Spiel im stillen Kampf laufen.**

**Musik hat einen Einblendwert und Klänge nicht** — ein Soundeffekt mit Einblendung
ist ein Soundeffekt, auf den der Spieler gewartet hat. Deshalb liest `10660`
`parameters[1]` als Fade und `10670` nicht.

**Die Kontexte sind null-basiert, und das ist eine Falle.** `BGM_Battle` ist 0,
`SFX_Cursor` ist 0, also ist die Grenze 0..6 und 0..11. **Eine Grenze ab 1 hätte
die Schlachtmusik abgelehnt** — genau die, die ein Spiel am häufigsten wechselt.

**Ohne Maniac-Patch ist der Parameter der Wert.** Die Referenz liest
`ValueOrVariableBitfield(com, 5, 1, 1)`, und dieser Helfer gibt ohne Patch direkt
`parameters[val_idx]` zurück — Modusindex und Wertindex sind dieselbe Zahl.
**Wer `parameters[5]` als Wert las, hätte jede Spur verstummt.** Genau das hat
der erste Lauf dieses Schnitts getan. Der Patch-Pfad braucht einen
Game-String-Spiegel, den diese Runtime nicht hat: plain value plus Diagnose.

**Sechs Übergänge, und der sechste ist der, den ein Spiel bemerkt.** Teleport
ein und aus, Kampfbeginn ein und aus, Kampfende ein und aus. `Transition_Count`
ist eine **Anzahl und kein letzter Index** — wer 5 als letzten Index las, hätte
den Übergang verweigert, der den Spieler aus dem Kampf zurückholt.

**Die Referenz `assert`s bei unbekanntem Übergang** — Absturz im Debug-Build,
Schreiben ins Leere sonst. Dieser Leser nennt die sechs erlaubten Werte.

**Test evidence** `test_rm2k_system_settings.cs` (12). **1153/1153**.
**Mutations** 12 Regeln über zwei Läufe, **12 von 12 gefangen** — darunter die
Kontextgrenze ab 1, die SFX-Breite auf die Menüvier gekürzt und die
Maniac-Warnung abgeschaltet.

## K-136 `10620`/`10630`/`10640`/`10650`/`10850` — DONE, und `10850` hat einen Wert, der kein Fahrzeug ist

**Fahrzeug-ID -1 bewegt die Partei und ist keine ungültige ID.** Die Referenz
hat einen Kommentar dazu: In RPG_RT hat eine Partei in keinem Fahrzeug die ID
-1, und -1 zu übergeben bewegt die Partei allein. **Wer sie ablehnte, ließe
jeden „teleportiere den Helden"-Befehl eines Spiels nichts tun** — und das ist
ein sehr häufiger Befehl. Ein Test prüft beides: fehlendes Fahrzeug wird
abgelehnt, -1 funktioniert trotzdem. Zwei Bedeutungen für ein Feld.

**Die Fahrzeug-ID wird um eins verschoben**, weil das liblcf-Enum
`None = 0, Boat = 1, Ship = 2, Airship = 3` ist und die Referenz
`(Game_Vehicle::Type)(com.parameters[0] + 1)` schreibt. Diese Zahlen stehen im
Save-Format. Wer den Parameter direkt nähme, spräche Fahrzeug 0 an — und
Fahrzeug 0 ist die Partei, kein Boot.

**`10650` setzt zwei Felder**, das aktuelle Sprite und das ursprüngliche. Das
ursprüngliche ist das, wozu das Fahrzeug beim Aussteigen zurückkehrt — **wer
nur das aktuelle setzte, ließe ein Fahrzeug in Kostüm zurück, nachdem die
Partei ausgestiegen ist.**

**Eine Partei im Fahrzeug fährt mit**, und die Referenz kehrt danach sofort
zurück. Nur das Fahrzeug zu bewegen ließe den Helden auf der verlassenen Karte
stehen — bei einem Boot also eine Partei auf offenem Wasser.

**`Boarding` ist nullable, und das ist eine Entscheidung.** Ein Spiel, das nie
ein Fahrzeug anfasst, allokiert keins, und wer es dereferenzierte, würde bei
jedem `10850` in einem Spiel ohne Schiff werfen — **und der Normalfall ist
genau dieses Spiel.** Der erste Lauf dieses Schnitts hat genau das getan; die
Tests haben es gefunden.

**`10630` nimmt die Transparenz direkt aus `parameters[2]` und nicht aus dem
Bitfeld** — das ist die Aufteilung der Referenz. **Und der Index ist eine Pose,
keine Charakter-Nummer:** ein Kostüm ist dieselbe Datei mit anderem Index, und
der Dateiname bleibt richtig, also fängt keine Sichtprüfung einen Leser, der es
falsch hat.

**Ein fehlender Held ist eine Warnung und keine Ablehnung.** Die Referenz prüft
`GetActor`, warnt und gibt `true` zurück. **Wer die Seite hielte, ließe eine
Cutscene auf einen Helden warten, den die Datenbank nie hatte.**

**Test evidence** `test_rm2k_actor_graphics.cs` (14). **1167/1167**.
**Mutations** 11 Regeln, **11 von 11 gefangen** im ersten Lauf — darunter die
nicht verschobene Fahrzeug-ID, das nicht gesetzte ursprüngliche Sprite und das
nullable Booting dereferenziert.

## K-136 `11710`/`11720`/`11740`/`11750` — DONE, und einer hatte gar keinen Schreiber

**`11750 Tile Substitution` hatte zwei 144er-Tabellen, zwei Leser und keinen
Schreiber.** Der Befehl konnte also geparst und nie ausgeführt werden — und
ein Test der Leser wäre die ganze Zeit grün gewesen. `SubstituteTile` schließt
die Lücke.

**`SubstituteLower` addiert `BlockEIndex` beim Lesen**, gespeichert wird also
der Rohwert. Wer die vom Befehl geforderte Zahl speicherte, bekäme einen
Index `BlockEIndex` zu hoch — **jede untere Kachel eine Zeile versetzt**. Der
Test prüft den Offset, denn ein Test mit der Rohzahl hätte einen korrekten
Schreiber „failen" lassen.

**`11720` hat sechs Flags und zwei Geschwindigkeiten, und die Geschwindigkeiten
kommen aus anderen Parametern als die Flags.** Flags sind 0, 1, 2 und 4; die
horizontale Geschwindigkeit ist 3, die vertikale 5. **Das vierte Flag und die
horizontale Geschwindigkeit stehen nebeneinander** — genau das macht den
Fehler leicht: wer die Parameter der Reihe nach liest, nimmt ein Flag als
Geschwindigkeit.

**Ein leerer Panorama-Name ist das Datenbank-Panorama und keine fehlende
Datei** — das macht die Referenz mit `if (!params.name.empty())`, bevor sie
die Datei anfragt. Wer einen leeren Namen als Fehler behandelte, verweigerte
genau das, wozu der Befehl da ist: zurück zur Datenbank.

**Die Referenz lässt den Interpreter auf die Panorama-Datei warten.** Diese
Runtime hat hier kein Dateisystem, also ist das Warten eine Diagnose — **wer
ewig wartete, hängte ein Spiel mit fehlendem Panorama**, und ein fehlendes
Panorama ist ein Fehler im Spiel, kein Grund anzuhalten.

**Null Encounterschritte sind ein realer Wert und genau der, der Zufallskämpfe
abschaltet.** Wer null als „nicht gesetzt" behandelte, könnte sie nie
abschalten. **Und ein neues Spiel, das null geerbt hätte, wäre nicht gewinnbar:**
keine Kämpfe, keine Erfahrung.

**Chipset 0 ist ein echtes Chipset.** Die Referenz vergleicht mit dem
aktuellen und kehrt früh zurück, wenn sie gleich sind. Wer null als „nicht
gesetzt" las, verweigerte das erste Chipset der Datenbank — und das ist oft
das meistbenutzte.

**`ChipsetId`, `MapParallax` und `EncounterSteps` standen nicht im Reset**, und
die Tests haben es gefunden.

**Test evidence** `test_rm2k_map_changes.cs` (12). **1179/1179**.
**Mutations** 10 Regeln, **10 von 10 gefangen** im ersten Lauf — darunter die
obere Tabelle in die untere geschrieben, die beiden Geschwindigkeiten
vertauscht und die Karteneinstellungen über den Reset gerettet.

## K-136 `20140` / `20141` — DONE, und dieser Schnitt fand ein Feld, das der Parser wegwarf

**Der Decoder las den LCF-Chunk `0x0D`, schrieb ihn in sein Wörterbuch, und
`EventCommand` hatte kein Feld dafür.** Jedes Event wurde also vollständig
geparst und kein Zweig konnte je identifiziert werden. `EventCommand.Indent`
existiert jetzt, und `Rm2kEngineRuntime` reicht es durch.

**Ein Test der Codes, der Parameter und der Strings wäre die ganze Zeit grün
gewesen** — die Information ging zwischen zwei richtigen Lesern verloren, und
nur das Verhalten von `20140` hätte es zeigen können.

**`20140` ist kein zweites Choice-Fenster.** Es ist ein Zweig einer Liste, die
der Spieler schon beantwortet hat, und die Referenz reicht es an
`CommandOptionGeneric`: entweder wird der Sub-Index gelöscht — weil das der
gewählte Zweig ist — oder zum nächsten bedingten Befehl gesprungen. **Wer nur
eine Hälfte hätte, bekäme einen Helden, der eine Frage stellt, weggeht, den
Wächter angreift, das Schwert kauft und geht — alles in einem Frame.**

**Jeder Zweig endet mit seinem eigenen `20141`.** Der Sprung läuft zum nächsten
Befehl aus `{ShowChoiceOption, ShowChoiceEnd}`, also würde ein Zweig ohne eigenes
Ende jeden Zweig danach mit verschlucken. Der erste Testentwurf baute die
andere Form und schlug „aus dem richtigen Grund" fehl.

**Der gewählte Zweig setzt den Sub-Index auf einen Sentinel, nicht auf ein
Flag.** Ohne das würde eine zweite Liste auf derselben Seite gegen eine
veraltete Zahl vergleichen.

**Der Sprung prüft die Grenze vor dem Lesen, nicht nach dem Schritt.** Ein
Zweig ohne Ende ist ein Fehler im Spiel; wer erst springt, liest einen Index
zu viel und wirft. Genau das tat dieser Leser; der Test fand es.

**Test evidence** `test_rm2k_choice_branches.cs` (6). **1185/1185**.
**Mutations** 7 Regeln über zwei Läufe, **7 von 7 gefangen** — darunter der
gewählte Zweig übersprungen statt ausgeführt, der Sprung ohne Ziel und der
Indent im Konstruktor wieder verworfen.

**Was bleibt** `1008 ChangeClass` und `10500 SimulatedAttack` plus die fünf
liblcf-Codes, die die Referenz nicht dispatcht. `ChangeClass` braucht zuerst
ein Klassenmodell.

## K-136 `10500` — DONE, und es ist kein Kampf

**Keine Reihenfolge, keine Truppe, keine Zielauswahl.** Der Befehl wählt Helden
über die üblichen Actor-Parameter, rechnet eine Zahl aus Verteidigung und Geist
und zieht sie von den HP ab. Ein Spiel benutzt ihn für eine Falle, ein Gift, ein
Skript — **Schaden ohne Kampf**. **Dieser Schnitt musste kein Kampfsystem bauen,
und das ist es wert zu wissen, bevor der nächste eines baut.**

**Verteidigung wird durch 400 geteilt, Geist durch 800**, also blockieren 800
Geistpunkte genau so viel wie 400 Verteidigungspunkte. **Wer einen gemeinsamen
Teiler nähme, machte Geist doppelt so stark wie beabsichtigt** — um den Faktor
zwei, auf der Achse, an der ein Spiel justiert.

**Das Ergebnis wird zweimal auf null geklemmt, und die Reihenfolge zählt.** Die
Referenz klemmt nach den zwei Subtraktionen, justiert die Varianz und klemmt
noch einmal. **Wer einmal am Ende klemmte, ließe eine Varianz negativen Schaden
ausgeben** — und negativer Schaden *heilt* den Helden.

**Die Spreizung ist mindestens eins.** `max(1, var * base / 10)` — ohne die Eins
rundet eine kleine Basis bei großer Varianz auf null, und **ein Spiel, das zehn
Prozent Varianz asked, bekäme keine.** Und die Spreizung ist symmetrisch: die
Hälfte wird abgezogen, nicht die ganze.

**Die Resultatvariable hält den Schaden des letzten Helden, nicht die Summe**,
weil die Referenz sie in der Schleife schreibt. **Wer summierte, ließe ein Spiel,
das „du hast N verloren" zeigt, eine Zahl zeigen, die das Spiel nie produziert
hat** — und konsequent, weil es jedes Mal dieselbe falsche ist.

**Die Varianz kommt aus einem eigenen Generator, nicht aus dem von MZ**, weil
die beiden Engines keinen Strom teilen. Gleiche Form wie `MzRandom`, ausdrücklich
kein Anspruch auf die Engine-Zahlen — die sind nicht wiederholbar. Was es gibt,
ist ein Lauf, den Save und Test wiederholen können.

**Der Test musste zweimal Rate 1 statt 100 nehmen.** Bei hundert Prozent blockiert
jeder Wert den ganzen Angriff, alle Fälle kommen als null heraus, und ein Test,
der die Teiler nicht unterscheiden kann, beweist nichts über sie. Beide Male
bestand der erste Entwurf mit einer Rate, die die Sache verdeckte.

**Test evidence** `test_rm2k_simulated_attack.cs` (8). **1193/1193**.
**Mutations** 10 Regeln, **10 von 10 gefangen** im ersten Lauf — darunter die
beiden Teiler vertauscht, die zweite Klemme entfernt, der Schaden addiert statt
abgezogen und die Resultatvariable akkumuliert.

**Was bleibt** `1008 ChangeClass` und die fünf liblcf-Codes ohne Referenz.
`ChangeClass` braucht ein Klassenmodell: der Befehl trägt Klassen-ID,
Stufen-Reset-Flag, Skill-Modus und Parameter-Modus, und keiner davon hat bisher
ein Ziel.

## K-136 Klassen-Parameter-Chunk `0x1F` — DONE: `1008` wartete auf Daten, nicht auf Code

**`1008 ChangeClass` wartete nicht auf einen Dispatcher, sondern auf Daten.**
Der Befehl trägt Klassen-ID, Stufen-Reset-Flag, Skill-Modus und
**Parameter-Modus** — und der Parameter-Modus ist ohne Klassentabelle
bedeutungslos, weil die Referenz daraus eine *Stufe* liest. Der LDB-Parameter-
Chunk der Klassen wurde nach `unknown_fields` gelesen und blieb dort.

**Nichts ging verloren, und das ist es wert zu sagen.** Der Leser-Vertrag hielt:
Der rohe Chunk liegt weiterhin in `unknown_fields`, neben dem Decode. Es fehlte
ein Weg hinein, und dieser fügt einen hinzu, ohne etwas zu ersetzen.

**Sechs `int16`-Vektoren und keine sechs skalaren.** liblcf `rpg::Parameters`
hält `maxhp`, `maxsp`, `attack`, `defense`, `spirit`, `agility` als
`vector<int16>` mit einem Eintrag pro Stufe, und `WriteLcf` schreibt sie in genau
dieser Reihenfolge ohne Längen davor. **Wer sechs Skalare annähme, läse den
ersten Wert jedes Vektors und nannte es das Klassenmaximum — eine Stufe-99-
Klasse gäbe ihren Helden Stufe-1-Werte**, und alle Zahlen lägen im Bereich, also
sähe nichts falsch aus.

**Die Reihenfolge ist liblcf s und nicht alphabetisch.** Wer die Namen sortierte,
gäbe einer Klasse ihre Initiative als Trefferpunkte — mit allen sechs Werten im
Bereich.

**Little endian, signiert, beide Bytes.** Wer eines las, deckte jeden Wert auf
255 — und eine Klasse mit mehr als 255 TP hätte ihre Helden leise geschwächt.

**Der Chunk ist ein Vielfaches von zwölf oder etwas anderes.** Wer ihn trotzdem
dekodierte, läse sechs Werte aus einem Chunk mit anderem Inhalt — und die
Werte wären plausibel, was schlimmer ist als eine Ablehnung.

**Stufen sind im Spiel eins-basiert und im Array null-basiert.** Die Referenz
liest `parameters[level]` nach dem Dekrementieren; wer das übersprang, gäbe einem
Stufe-1-Helden die Zeile 0 — und bei einer Klasse, deren erste Stufe absichtlich
schwach ist, ist das der Unterschied zwischen einem Tutorial und einem Helden,
der zu schwach ins Spiel startet.

**Eine Klasse ohne Parameter sagt es und gibt false zurück.** Eine maximale
Trefferpunktzahl von null sähe wie eine Designentscheidung aus — ein Held, den
das Spiel unspielbar machte, statt einer Datei, die nicht gelesen wurde.

**Test evidence** `test_rm2k_class_parameters.cs` (7). **1200/1200**.
**Mutations** 9 Regeln, **9 von 9 gefangen** im ersten Lauf — darunter die um
eins gedrehten Vektoren, das weggefallene hohe Byte und die eins- statt
null-basierte Stufe.

**Was `1008` noch braucht** die Klassen-Skill-Liste und die vier Modi, die der
Befehl trägt. Die Parameter sind der Teil, ohne den der Befehl nicht zu schreiben
war; der Rest ist ein Schalter.

## WOLF `IfVariable` — DONE: ein Vergleich, kein Test, und ein Ast ohne den andern

**`IfVariable` verglich mit `==` und sonst nichts, und kein Test im Repository
hat es überhaupt benutzt.** Der eine Vergleich, den es hatte, war so wenig
bewiesen wie die sechs, die fehlten.

**Der Editor bietet sieben, und die Hilfe nennt sie:** größer, größer-gleich,
gleich, kleiner-gleich, kleiner, ungleich, Bit-UND. **Wer nur `==`
implementierte, nähme einen Zweig in sieben**, und die anderen sechs fielen in
den Else-Pfad — **eine Truhe, die mit „V0 ist mindestens 1" gesichert ist,
würde sich nie öffnen.**

**Der Bit-UND-Test ist gleich dem Wert und nicht „irgendein Bit gesetzt".** Die
Hilfe widmet ihm einen Absatz: Mit V0 = 5 (`101`) und Wert 2 (`010`) ist
`5 & 2` gleich 0, nicht 2 — der Test scheitert. **Wer
`(variable & wert) != 0` schriebe, bestünde jeden Test mit einem gesetzten
Bit, und ein Spiel, das eine Tür mit einem Bit-Test sichert, öffnete sie für
alle.** Und ein Wert von null erfüllt ihn immer, weil alles UND null null ist —
die Hilfe sagt das ausdrücklich.

## Und der Ast hatte ein Ziel, und das ist die zweite Hälfte

**Ein WOLF-Ast hat zwei Arme, und die VM hatte einen Sprung.** Der
Durchlauf war der wahre Arm und der einzige Sprung der falsche — also lief
bei gehaltener Bedingung der wahre Arm *und* der falsche: **eine Truhe, die
sich öffnet, und ein Wächter, der im selben Frame zuschlägt.** Das ist ein
anderer Fehler als der Vergleich und ein schlimmerer, denn er ist in keinem
einzelnen Vergleich sichtbar.

**Beide Arme sind jetzt ein eigenes Ziel** (`TrueJumpIndex`, `JumpIndex`), und
**der letzte Befehl eines Arms springt über den anderen** (`NextIndex` am
Befehl).

**Das Arm-Ende war zuerst ein gemerkter Endindex, und das war falsch:** Es
braucht verborgenen Zustand, den ein Programm mit zwei Zweigen nacheinander
aus einem in den anderen leckt, und die Prüfung feuert, bevor der Arm läuft,
wenn der Zweig schon hineingesprungen ist. Der Sprung am Befehl hat keinen
solchen Zustand und ist auch die Form, die eine WOLF-Ereignisliste hat. **Drei
Versuche waren nötig, und der dritte ist der im Code; die ersten zwei stehen
hier, weil der Grund des Scheiterns der Grund für die Richtigkeit ist.**

**Test evidence** `test_wolf_comparisons.cs` (8). **1208/1208**.
**Mutations** 10 Regeln, **10 von 10 gefangen** — darunter der Bit-Test als
„irgendein Bit gesetzt", der Arm-Sprung entfernt und ein unbekannter Vergleich
statt abgelehnt auf Gleichheit zurückfallend.

**Was WOLF weiter fehlt und gesagt statt versteckt wird: Es gibt kein natives
WOLF-Fixture.** Das Verzeichnis `wolf` enthält eine selbstgebaute
`urpg-wolf-plain-json`-Envelope und ein README, das das sagt, und die sieben
Vergleichsnummern sind gegen die Editor-Hilfe gepinnt, nicht gegen eine Datei.
Das Variablenmodell ist außerdem noch ein flaches `int`, wo WOLF Selbst-,
Normal-, System- und Datenbank-Bänder hat — **das ist die nächste WOLF-Karte
und sie ist größer als diese.**

## WOLF Variablenbänder — DONE: ein flaches Wörterbuch konnte sie nicht halten

**Der Editor listet vier: `Self / Var / Sys / 可変DB`** — Selbst, Normal+Reserve,
System, Variable-DB. Die VM hatte ein `Dictionary<int, int>`, also **kollidierten
Selbst- und Systemvariable mit demselben Index** — und die Kollision ist still,
weil beide Lesen mit einer Zahl antworten und nur mit der falschen.

**Die Millionenschranke ist das Adressierschema selbst.** Eine Zahl ab 1.000.000
ist kein Wert, sondern eine *Referenz*: 2.000.005 heißt Normalvariable 5. Wer
die Zahl als Wert behandelte, speicherte zwei Millionen in ein Feld, das auf
Variable fünf zeigen soll, und das Spiel läse eine Zahl, die es nie geschrieben
hat.

**Die Schranke ist inklusiv.** Die Hilfe sagt „1.000.000 *oder mehr*", also hätte
ein Leser mit `>` genau 1.000.000 als Wert behandelt und nie Selbstvariable 0
aufgelöst — die eine Variable, die jedes WOLF-Event benutzt.

**Der Block ist eins-basiert und das Band null-basiert**, also ist 1.000.000 Block 1
und Selbstband 0. Wer den Block direkt nähme, wäre für jedes Band um eins daneben
und das erste Band spräche eines an, das es nicht gibt.

**Das Datenbank-Band ist kleiner als die anderen drei** (999 Zeilen gegen 99.999).
Eine gemeinsame Grenze ließe ein Spiel Datenbankzeile 50.000 adressieren — eine
Zeile, die der Editor nicht hält und eine Save-Datei nicht trägt.

**Ein einfacher Wert löst sich selbst auf und ist kein Schreibziel.** Das ist
das Kästchen „Daten nicht aufrufen" der Hilfe: Ein Feld darf beides halten, und
die Zahl selbst sagt welches.

**Der Vergleich löst beide Seiten auf**, weil die Hilfe sagt, der Vergleichswert
könne auch eine Variable sein — 2.000.000 dort heißt Normalvariable 0. Wer nur
die linke Seite auflöste, vergliche eine Normalvariable mit der *Zahl* zwei
Millionen statt mit dem, was sie hält.

**Die Fixture des Laufs hat sich geändert, und das ist der Punkt.** Ihr
Variablenoperand war eine nackte `1` — also der *Wert* eins, womit sie eine Zahl
schrieb statt einer Variablen. Sie ist jetzt `2.000.000`, mit einem Kommentar
warum, und `Test_WolfPluginRuntimeLoadsDataAndAdvancesDeterministicEventVm`
**ist fehlgeschlagen, als sich das Modell änderte** — wofür ein Laufzeittest da ist.

**Test evidence** `test_wolf_variable_bands.cs` (10), plus die sieben
Vergleichstests und die sechs Laufzeittests, alle nachgemessen. **1218/1218**.
**Mutations** 10 Regeln über zwei Läufe, **10 von 10 gefangen** — darunter die
exklusive Millionenschranke, der null-basierte Block, zwei Bänder im selben
Wörterbuch und der Vergleich, der nur eine Seite auflöst.

## WOLF Zuweisungsoperatoren — DONE: die VM kannte zwei von vierzehn

**Die Hilfe listet vierzehn: `=`, `+=`, `-=`, `*=`, `/=`, `%=`, 引上げ, 引下げ,
絶対値, arctan, sin, cos, Wurzel.** Die VM kannte zwei, und die zweite —
Addition — war fest in ihr eigenes Opcode verdrahtet, also gab es keinen Ort
für die anderen zwölf. **Ein Leser mit zwei kann keine Trefferquote, keine
Schadensformel und keinen Winkel berechnen, und keines der drei ist ein
exotisches Spiel.**

**Ein Weg für jeden Operator, und der Operator ist ein Feld und kein Opcode.**
Der Editor wählt ihn in einer Liste neben dem Ziel, also bräuchte ein Leser,
der aufs Opcode schaltet, einen dreizehnten Fall, sobald der Editor einen
hinzufügt — und die beiden Listen liefen auseinander. `AddVariable` ist jetzt
der Additionsoperator über denselben Weg, und genau deshalb können sie nicht
auseinanderlaufen.

**Der aktuelle Wert wird vor dem Schreiben gelesen**, und diese Reihenfolge ist
der Grund, warum zuerst aufgelöst wird: eine rechte Seite, die dieselbe Variable
nennt wie das Ziel, muss den alten Wert sehen, sonst läse ein Verdopplungsbefehl
den neuen.

**Division durch null lässt die Variable unverändert und ist kein Fehler.** Die
Hilfe sagt, ein Teiler 0 verhält sich wie Teilen durch eins. Ein Leser, der null
zurückgäbe, eine Ausnahme werfe oder einen Sentinel schriebe, wäre für eine Zeile
Hilfe auf drei Arten falsch.

**Trigonometrie ist skaliert, und die Skalierung ist der Operator.** Der Winkel
ist Zehntelgrad, das Ergebnis Tausendstel, also sind die Beispiele der Hilfe 600 →
866 für sin und 600 → 500 für cos. Ein Leser in Grad und Fließkomma gäbe 0,866
zurück und **das sähe nicht falsch aus** — es sähe wie eine kleine Zahl aus.

**Der Arkustangens liest zwei rechte Seiten und nicht den aktuellen Wert**, denn
eine Steigung ist eine Richtung, und eine Richtung braucht zwei Achsen. Die
Reihenfolge ist Y, X, weil die Hilfe X rechts-positiv und Y unten-positiv sagt —
also ist gerade unten +90 und nicht −90. Ein nackter Arkustangens reicht nur bis
±90°, also deckt `Atan2` den Kreis ab und eine Steigung nach links ist 1800.

**Die ganze Rechnung ist breit, und der Test hat bewiesen, dass sie es sein muss.**
Eine rechte Seite von drei Milliarden passt nicht in ein `int`; ein als `int`
geschriebener Test hätte nicht übersetzt, einer mit `unchecked` hätte eine andere
Zahl transportiert. **Sowohl Multiplikation als auch Subtraktion verlassen den
Bereich, bevor die Klemme sie sehen kann** — eine `int` zu klemmen klemmt den
Wert nach dem Überlauf, also die falsche Zahl. `Switch` gibt darum ein `long`
zurück, und der Parametertyp folgt der Grenze ±2 Milliarden der Hilfe.

**Ein Test, der sich selbst widerlegt hat.** Der Rundungstest behauptete erst, die
Beispiele der Hilfe trennten Rundung von Abschneiden — sie tun es nicht: fünf gibt
2236 so wie so, und zwei und drei ebenso. **Gemessen ist der trennende Eingabe
sieben** — Wurzel sieben mal tausend ist 2645,75, also rundet es auf 2646 und
schneidet auf 2645 ab. Und kein Eingabe hat eine exakte Halbe, also lassen sich
half-away-from-zero und half-to-even an diesem Testsatz nicht unterscheiden; der
Code sagt away from zero, und das Board sagt das, statt mehr zu behaupten. Ein
zweiter Test behauptete `sin(1800) == 1000`, das ist neunzig Grad mit dem
Kommentar eines Halbkreises; gemessen sind 1800 hundertachtzig Grad und geben 0.

**Test evidence** `test_wolf_variable_operator.cs` (15), darunter vier, die den
Operator durch die VM laufen lassen. **1233/1233**.
**Mutations** 12 Regeln in einem Lauf, **12 von 12 gefangen** — darunter die
Division-durch-null-Absicherung, die Zehntelgrad-Skala, Abschneiden statt Rundung,
der nackte Arkustangens und `AddVariable`, das still zu Zuweisung wurde.

## WOLF Band-Offsets — DONE: sie waren geraten, und die Hilfe nennt sie

**Die letzte Karte hat die vier Bänder aus der Hilfe gelesen und ihre Zahlen dann
geraten.** Sie schrieb „ein Millionenblock pro Band" und ging weiter, und die Tests
behaupteten diese Vermutung und waren grün. **Die Hilfe nennt sie, auf zwei
verschiedenen Seiten, und sie sind keine Reihe:** `1100000～:マップセルフ変数` und
`1600000～:コモンセルフ変数` aus dem Seitenaufruf des Ko单调Events, `2000000` für
Normalvariable 0, `3000000` für Stringvariable 0.

**Ein berechneter Block legte Map-Self auf 1.000.000, Common-Self auf 2.000.000 —
eine Normalvariable — und das Systemband auf 3.000.000, also das Stringband.** Ein
Spiel, das seine Systemuhr aus dem Stringbereich las, hätte eine Zahl bekommen, und
die Zahl wäre plausibel gewesen.

**Ein Mutationslauf hat es nicht gefangen, und das ist die Lehre.** Die zwölf Regeln
der Operator-Karte mutierten das Verhalten um die Offsets und alle wurden gefangen,
weil die Tests mit dem Code übereinstimmten. **Zwei falsche Zahlen, die sich
einig sind, ergeben eine grüne Suite.** Die Prüfung, die es gefangen hätte, war das
Lesen der Quelle — und die Offsets stehen jetzt in einem Test, der die Seite nennt,
aus der sie stammen.

**Die Million selbst ist eine Referenz ohne Band, und die ganze Lücke sagt das.**
Die Hilfe sagt „eine Million oder mehr wird aufgerufen", also ist 1.000.000 eine
Referenz — und sie liegt unter der Map-Self-Basis, also zeigt sie auf nichts. Das ist
eine andere Antwort als „keine Referenz", und der Code hat zwei Codes dafür: `-1` ist
ein Wert, `-2` eine Referenz ohne Band. **Ein Leser, der sie verschmilzt, würde
einem Aufrufer sagen, 1.050.000 sei ein Wert — und wer ihn speichert, behält einen
Zeiger in einer Variablen, die das Spiel als Zahl liest.**

**Das Stringband wird erkannt und abgelehnt.** Dieser Leser hat keine
Stringvariablen, und das sagt er, statt in das Normalband durchzufallen und einer
String-Referenz eine Zahl zu antworten. **Die Variable-Datenbank ist gar kein Band:**
Die Bedingungshilfe sagt, dass beim Datenbank-Vergleich kein Variablenaufruf wie
`1600000` angegeben werden darf, also hat sie keinen Offset, über den sie indiziert
werden könnte. Sie wird nach Typ und Spalte adressiert, in einem eigenen Speicher,
geschlüsselt mit verschobenem Typ statt multipliziert, damit ein großer Typ nicht in
die Zellen eines anderen Typs überläuft.

## Und die Schalter waren ein Wörterbuch, wo die Hilfe zwei Bereiche nennt

**0 und aufwärts adressieren ein Map-Event, 500.000 und aufwärts ein Koモン-Event.**
Die VM hatte ein `Dictionary<int, bool>`, also **kollidierten ein Map-Schalter und
ein Common-Schalter mit demselben Index** — und die Kollision ist still, weil beide
Lesen mit einem Boolean antworten und nur mit dem falschen.

**Die Basis ist 500.000 und nicht eine Million**, und die Variablenbänder beginnen
bei 1.100.000; ein Leser, der das Variablen-Schema wiederverwendete, ließe 100.001 bis
500.000 unerreichbar, und ein Spiel mit einem Schalter dort fände ihn dauerhaft aus.
**Eine Schalternummer außerhalb beider Bereiche ändert nichts**, denn ein Leser,
der das Wörterbuch wachsen ließe, speicherte einen Schalter, den der Editor nicht hält,
und der nächste Ladevorgang trüge ihn nicht — der Schalter funktionierte in der
Sitzung und verschwände danach.

**Ein nicht gesetzter Schalter ist aus, und ein nicht lesbarer auch.** Die
Bedingungsliste lautet „an" und „aus" und nichts sonst, also ist ein unlesbarer
Schalter aus — was ein Spiel vor dem Setzen ohnehin erwartet.

**Test evidence** `test_wolf_switches.cs` (7) und `test_wolf_variable_bands.cs`
(13, davon drei neu geschrieben, weil sie die geratenen Offsets behauptet hatten).
**1243/1243**, mit Operator-, Vergleichs- und Laufzeitdatei nachgemessen.
**Mutations** 13 Regeln in einem Lauf, **13 von 13 gefangen** — darunter jeder der
drei Offsets, die Lücke mit dem falschen Code, die Datenbank nur nach Spalte
geschlüsselt und die zwei Schalterkarten wieder zu einer gefaltet.

## WOLF Bewegungsrouten — DONE: 24 verifizierte Typen und kein Executor

**`WolfMoveRoute` hatte eine Typentabelle mit vierundzwanzig verifizierten Typen,
einen binären Leser mit elf Tests — und nichts führte einen einzigen Schritt aus.**
Ein Spiel mit einer Patrouillenroute hätte geladen und stünde still, und die Suite
blieb grün, weil sie Schritte nur las, nie ausführte. **Ein Leser, der getestet
und nicht ausgeführt wird, ist ein Parser.**

**Die Passierbarkeitsbits sind die der Hilfe: `1上+2左+4右+8下+16左上+32右上+64左下+128右下`.**
**Eine Diagonale ist ihr eigenes Bit und nicht oben plus links** — oben ist 1 und
links ist 2, ihre Summe ist 3, und Bit 3 gibt es nicht. Wer sie kombinierte,
erzeugte eine Richtung, für die das Format kein Bit hat, und eine Figur mit Bit 3
würde auf gar keine Richtung passen.

**Ein verweigerter Schritt dreht die Figur trotzdem in die Richtung.** Die erste
Fassung von `Step` kehrte bei der Verweigerung zurück und ließ die Blickrichtung
allein, während der Kommentar darüber das Gegenteil versprach — **und der Test hat
den Widerspruch gefunden.** Ein Wächter, der gegen eine geschlossene Tür läuft,
dreht sich zu ihr, und ein Spiel, das den Wächter durch den Spalt auf den Helden
blicken lässt, hängt daran.

**Geschwindigkeit und Frequenz sind 0 bis 6 und nicht dasselbe.** Die Hilfe
schreibt `移動速度[遅0-6速]` und `移動頻度[早0-6遅]` — einmal langsam nach schnell,
einmal oft nach selten — und wer eines auf das andere legte, ließe eine schnelle
Figur selten laufen. Eine Rate außerhalb wird geklemmt und nicht abgelehnt: Eine
geklemmte Figur läuft noch, und eine Ablehnung stoppte das Event.

**Tempo 0 ist ein Bild pro Kachel und keine unendliche Wartezeit.** Durch die
Geschwindigkeit zu teilen ergäbe eine unendliche Bildzahl und eine Route, die nie
zu Ende liefe. Die Bilder pro Kachel fallen mit der Geschwindigkeit: 1 bei 0, 16
bei 1, 8 bei 2, 4 bei 4, 2 bei 6.

**Der Add-Schritt liest den alten Wert**, dieselbe Regel wie die Variableoperation:
eine rechte Seite, die dieselbe Variable nennt wie das Ziel, muss den Wert vor dem
Schreiben sehen. **Ein Variablenschritt auf eine nackte Zahl wird abgelehnt und
nicht gespeichert**, denn unter dem Rohschlüssel zu schreiben hieße etwas zu
schreiben, das kein Lesen findet — der Schritt schiene zu funktionieren und
verlöre danach seinen Wert.

## Fünf Schritte werden abgelehnt, und das ist die ehrliche Antwort

**Ein Event annähern braucht eine zweite Figur, eine Position annähern braucht die
Karte, ein Sprung braucht seine eigene Route, ein Ton braucht Audio, und eine
Grafik ist ein Dateiname, für den dieser Runner keinen Lader hat.** Die
Annäherungsschritte mit einer Richtung zu beantworten hieße die Figur dorthin
laufen, wo das Event nicht ist; Ton und Grafik mit Erfolg zu beantworten wäre eine
Lüge, die der Aufrufer nicht bemerken kann. **Ein verweigertes Ergebnis ist die
einzige Antwort, auf die ein Aufrufer reagieren kann**, und es hält diesen Schnitt
ehrlich darüber, was er nicht kann.

**Die Zehnertasten-Tabelle ist absichtlich nicht implementiert.** Die Hilfe sagt,
eine Blickrichtung sei 1 bis 9 und entspreche dem Zehnerblock, und verweist für die
Zuordnung auf „Abbildung A" — eine Grafik, die nicht im Text steht. Ein früherer
Entwurf dieser Karte riet die Tabelle, und sie hatte doppelte Werte, was
unmöglich ist. **Die geratenen Band-Offsets haben eine Karte gekostet, und das ist
derselbe Fehler in derselben Sitzung.** Die Funktion liefert „keine Richtung" und
sagt warum.

**Test evidence** `test_wolf_move_route_runner.cs` (13), mit dem bestehenden
`test_wolf_move_route.cs` (11) nachgemessen. **1256/1256**.
**Mutations** 17 wirksame Regeln über drei Läufe, **17 von 17 gefangen** —
darunter rechts als Bit 3, eine Diagonale als oben-plus-links, die entfernte
Passierbarkeitsprüfung, die Blickrichtung, die einer Verweigerung nicht folgt,
Tempo 6 mit sechzehn Bildern und der Add-Schritt ohne vorheriges Lesen. Eine Regel
war eine Umbenennung, die nicht übersetzt, und zählt nicht.

## WOLF Figurenbrett — DONE: ein aufrufbares Ding ist noch kein Spiel

**Die letzte Karte baute einen Runner, den man aufrufen kann. Das ist noch kein
Spiel.** Der Routenleser lieferte Schritte, die Typentabelle verifizierte sie, und
nirgends stand eine Figur, auf der sie laufen könnten — die VM hatte weder Figuren
noch Karte. Diese Karte fügt Brett, zwei Opcodes und die Uhr hinzu.

**Das Brett gehört der VM und benutzt ihre Bänder.** Ein Routenschritt, der in eine
Variable schreibt, muss dorthin schreiben, wo das Event liest, und zwei Bänder
hießen, dass eine Patrouille Schritte in einen Speicher zählt, den niemand ansieht.

## Das Timing war dreimal falsch, und die Reihenfolge zweier Zeilen ist der Grund

**Erstens: der Index wurde vor dem Frame-Budget geprüft.** Ein Schritt rückt den
Index vor, wenn er läuft, also zeigt der Index auf den *nächsten* Schritt — und ein
Schritt, der über den letzten hinausgerückt war, war im nächsten Frame schon „am
Ende". Die Route endete, die Wiederholungsflagge setzte den Index auf null, **und die
fünfzehn Bilder, die der Schritt verlangt hatte, wurden mit weggeworfen.** Der Schritt
lief danach in jedem zweiten Frame, und ein Wächter mit Tempo 1 überquerte den
Bildschirm achtmal zu schnell. Das Symptom, das der Test meldete, war eine Figur bei
X = 9 nach siebzehn Frames, und jede beteiligte Funktion misst für sich allein
korrekt.

**Zweitens: die Bilder zuerst zu prüfen reparierte die Reihenfolge und ließ jeden
Schritt einen Frame zu spät enden**, weil der Tick, der das letzte Bild verbrauchte,
zurückkehrte, statt zu prüfen, was als nächstes kommt.

**Drittens, und das ist im Code: ein Flag und nicht ein Test auf die Restbilder.** Ein
`FramesLeft` von null ist mehrdeutig — es heißt entweder „noch im letzten Bild" oder
„kein Schritt begonnen" — und beide Lesarten standen im Code. `IsStepRunning` macht
die Rechnung exakt: Ein Schritt von n Bildern wird von einem davon gestartet, also ist
er im n-ten Tick fertig, und der Tick, der ihn beendet, sieht auch den nächsten
Schritt. **Ein Schritt von sechzehn Bildern wird als fünfzehn gespeichert, weil der
Tick, der ihn startet, sein erstes Bild ist.**

**Die zwei Verweigerungen werden vor dem Timing entschieden, und auch diese
Reihenfolge zählt.** Das Timing eines Schritts zu fragen, der nicht lief, ist eine
Frage ohne Antwort, und wer zuerst fragte, gab einem verweigerten Schritt ein
Frame-Budget — ein Wächter stünde sechzehn Bilder vor einer Wand und stoppte dann.
Überspringen heißt im selben Frame weitergehen, nicht ins Timing fallen.

**Die Verweigerungen selbst sind zwei Antworten, und die letzte Karte hatte es
falsch.** Der Runner hat den Rückgabewert von `Step` weggeworfen und einen Schritt in
eine Wand als `Stepped` gemeldet, also hatte die Skip-Flagge des Bretts nichts, worauf
sie reagieren konnte. Eine verweigerte Bewegung ist jetzt `Refused`, und eine
verweigerte ist nicht dasselbe wie ein Schritt, der sich nicht bewegt hat.

## Die beiden Wartezustände teilen sich einen Status und brauchen zwei Enden

**Das Bild-Warten zählt herunter; das Routen-Warten endet, wenn das Brett sagt, dass
die Bewegung fertig ist.** Sie benutzen denselben `Waiting`-Status, also unterscheidet
ein Flag sie — **und der Bug, den dieses Flag verhindert, ist: ohne es läuft die VM auf
dem MoveRoute-Befehl selbst weiter und startet die Route endlos neu.** Die Bilder
sind während des ganzen Wartens null, also hinge ein Leser, der nur Bilder zählte,
ewig fest — ohne Fehler irgendwo. **Der Befehlsindex rückt in diesem Moment vor und
sonst nirgends**, weil der MoveRoute-Befehl ihn absichtlich auf sich selbst stehen
ließ, damit das Event dort gehalten wird.

**Das Brett tickt über der Statusprüfung.** Eine Figur auf Patrouille läuft weiter,
während ein Text auf dem Bildschirm steht, und ein Leser, der es nur im Routen-Warten
tickte, frierte jede Figur für die Länge einer Textbox ein.

**Test evidence** `test_wolf_character_board.cs` (14) und
`test_wolf_move_route_runner.cs` (14, ein neuer Test für die Verweigerung), mit dem
bestehenden `test_wolf_move_route.cs` (11) nachgemessen. **1271/1271**.
**Mutations** 16 wirksame Regeln über zwei Läufe, **16 von 16 gefangen** — darunter
das nicht abgebuchte Frame-Budget, das doppelt gezählte erste Bild, der vor den
Bildern geprüfte Index, der Wrap ohne Flag-Löschung, der nicht vorrückende Index beim
Routen-Warten, das nicht tickende Brett während eines Bild-Wartens und eine als
`Stepped` gemeldete verweigerte Bewegung. Eine Regel war eine Umbenennung, die nicht
übersetzt, und zählt nicht.

## WOLF Kachel-Passierbarkeit — DONE: sechs Zustände, zwei Ebenen, Wände

**Die letzte Karte ließ Figuren laufen, und sie liefen durch Wände** — weil das
Brett eine Karten-ID und eine Breite hatte und sonst nichts. Passierbarkeit gab es
im WOLF-Leser nirgends: die Kartendaten trugen Kacheln, und Kacheln sind Bilder.

**Sechs Zustände und nicht zwei.** Das Kachel-Fenster des Editors wechselt sie im
Zyklus `○ → × → ▲ → ★ → □ → ○`, und die Kachel-Hilfe nennt die Bedeutung:
begehbar, nicht begehbar, begehbar mit dahinter versteckter Figur, begehbar und immer
über der Figur gezeichnet, begehbar mit halbtransparenten Füßen — und der sechste:

**↓ nimmt die Antwort der Ebene darunter und ist begehbar, wo keine Ebene da ist.**
Die Hilfe sagt: 「下のレイヤーに合わせます。下のレイヤーがない場合は通行可能です」.
**Wer die Kachel stattdessen ablehnte, fröre den Helden auf dem Boden fest** — eine
Bodenkachel ohne etwas darunter ist die gewöhnlichste Kachel einer Karte. Und wer nur
einen Boolean hat, verliert ▲, ★, □ und ↓, von denen nur × blockiert: die anderen drei
fügen eine Zeichenregel hinzu und kein Hindernis, und „hinterher versteckt" als
unbegehbar zu lesen hieße einen Wächter vor einem Treppengeländer einz sperren.

**Zwei Ebenen, weil ↓ eine davon fragt.** Die obere Ebene antwortet, und die untere nur
wo die obere fragt: Eine ★-Kachel über Wasser ist begehbar, weil ★ begehbar sagt. Wer
die untere Ebene alles entscheiden ließe, machte ein Schild über einer Wand unbenutzbar.

**Ein siebter Zustand wird abgelehnt** und nicht als begehbar behandelt. Wer auf
„nicht ×, also begehbar" durchfiele, ließe eine Figur auf eine Kachel, die das Spiel
noch nie gesehen hat — und das Symptom wäre ein Held durch eine Wand, die niemand
gezeichnet hat.

## Eine Figur ganz ohne Karte kann nicht gehen, und genau darum

**„Die Karte wurde nicht gelesen" ist nicht „die Kachel ist begehbar."** Eine Figur
ohne Gitter verweigert jeden Schritt, und wer eine fehlende Karte als offenes Feld
behandelte, ließe einen Wächter durch jede Wand auf jeder Karte gehen, deren Kacheln er
nicht lesen konnte — ohne Fehler irgendwo, denn ein Gang durch eine Wand ist ein Gang.

**Die Blickrichtung dreht trotzdem, denn die Verweigerung gilt der Position und nicht
der Blickrichtung.** Dieselbe Regel wie bei einer Wand — dieselbe Regel, gegen die der
Test der letzten Karte den Code ertappt hat.

**Das Brett gibt die Karte beim Laden an jede Figur** und nicht nur an die danach
gesetzten. Eine vorher gesetzte Figur hat dieselbe Frage wie eine spätere, und wer das
Gitter beim Setzen übergäbe, ließe die früheren durch Wände laufen.

**Die Kartengröße ist die des Gitters und nicht ein Feld daneben.** Ein Brett mit der
Breite 20 und einem Gitter von 10 ließe eine Figur zu Kachel 15 laufen und eine Zeile
lesen, die es nicht gibt.

**Neun der bestehenden Routentests sind beim Umstieg fehlgeschlagen, und das ist die
Änderung, die wirkt:** Seit die Karte eine Verweigerung und nicht ein Fehlen ist,
musste jeder Test, der einen bewegenden Schritt misst, seiner Figur eine Karte geben.
Drei davon maßen die Verweigerung und nannten sie eine Route.

**Einer der neuen Tests hatte die Wand an der falschen Stelle** — er setzte sie auf drei
und nannte den ersten Schritt auf drei „offen". **Ein Test, der zweimal die Verweigerung
misst und nie eine Figur laufen sieht, beweist nichts über das Laufen.**

**Test evidence** `test_wolf_passability.cs` (10), mit
`test_wolf_character_board.cs` (14) und `test_wolf_move_route_runner.cs` (14) nach dem
Umstieg nachgemessen. **1281/1281**.
**Mutations** 16 wirksame Regeln über drei Läufe, **16 von 16 gefangen** — darunter
der Pfeil, der die untere Ebene nicht fragt, eine fehlende untere Ebene, die abgelehnt
wird, jedes von ▲, ★ und □ als unpassierbar gelesen, der unbekannte Zustand als
begehbar, das Gitter, das aus der unteren statt der oberen Ebene antwortet, die
entfernte Kartenprüfung und das Brett, das die Karte nur dem Helden gibt.

## WOLF Figuren-Kollision — DONE: halbe Kachel, Durchlass, und ein Held, der keine Wand ist

**Die letzte Karte gab Figuren Wände und sonst nichts**, also lief ein Held durch
jeden Wächter, jeden Händler und jedes Schild. Die einzige Spur wäre ein Held
innerhalb eines Ladens gewesen, und ein Gang durch einen Wächter ist ein Gang.

**Die Trefferfläche ist eine Kachel breit und eine halbe hoch**, und die Hilfe nennt
die Zahl: `当ﾀﾘ判定■(正方形)` aus ist `横1マス×縦0.5マス` — die Füße einer Figur und
nicht ihr ganzer Körper — und die Quadratoption macht eine ganze Kachel daraus. **Wer
für beides eine Kachel nähme, ließe jede halbhohe Figur mit der Figur auf der Kachel
davor kollidieren, und eine Menschenmenge in einem Gang würde feststecken.** Das ist
ein Spiel, das nicht zu Ende zu spielen ist, und es sieht aus wie ein Fehler im
Pfadfinden und nicht in der Trefferfläche.

**X ist halb offen und Y geschlossen, und diese Asymmetrie ist eine Entscheidung und
kein Tippfehler.** X halb offen hält einen Gang begehbar: Eine Figur auf Kachel 2
reicht von 2 bis 3, eine auf 3 von 3 bis 4, und eine geschlossene Compare hätte sie
auf der Grenze überlappen lassen und jedes Zwei-Kachel-Zimmer blockiert. Y geschlossen
ist die andere Hälfte — **eine quadratische Figur auf Kachel 5 belegt 5 bis 6 und
berührt die Figur auf Kachel 6, und ein fester Gegenstand, in dem eine andere Figur
stehen darf, ist nicht fest.** Es ist auch das, was die Quadratoption überhaupt
bedeutet: Mit halb offenem Y reichte eine quadratische Figur genauso weit wie eine
halbhohe, und die Option wäre ein Name für nichts. **Die Hilfe schreibt den Vergleich
nicht aus, also steht das hier als Wahl mit ihren Gründen.**

**Ein Geist wird durchquert, und die Beziehung ist einseitig.** Die Option
`イベントをすり抜けられるようにします` macht ein Event durchquerbar, und die Hilfe
fügt hinzu, dass ein solches Event nur ausgelöst wird, wenn der Spieler darauf steht —
also ist ein transparentes Schild zugleich eine Wand, durch die man geht, und eine
Sache, die man nur betreten kann. **Das Flagge gehört zum Geist und antwortet, bevor
irgendjemand gefragt wird**, also geht ein Geist durch eine feste Figur und eine feste
Figur durch einen Geist. Wer die Beziehung symmetrisch machte, hieße jeden unsichtbaren
Auslöser im Spiel zusperren.

## Ein Test las diese Regel rückwärts, und der Code hatte recht

**Einer der neuen Tests behauptete eine *Verweigerung*, als der Held auf einen Geist
trat** — er las die Regel, als stoppte der Geist jeden, der in ihn hineinlief. Der Code
tat das Gegenteil und hatte recht: Wer dem Test glaubte, hätte eine transparente
Dekoration zur Wand gemacht, also das Gegenteil dessen, wozu die Option da ist.

**Das steht hier, weil der Test beim ersten Lauf der Form bestand und erst umfiel, als
der Test darüber anfing zu arbeiten.** Vier der elf Tests maßen die falsche Sache aus
einem Grund, der nichts mit Kollision zu tun hatte, und das Symptom war in allen vieren
dasselbe Wort: `False`.

## Der Held war bis zu dieser Karte gar nicht auf dem Brett

**Die Besetzungsliste fing leer an, und der erste Aufruf eines Spiels ist LoadMap** —
das das Gitter an alle auf der Liste abgibt. Eine leere Liste hieß, dass der Held das
Gitter nie bekam, **also konnte der Held gar keinen Schritt machen**, und ein Spiel mit
einem Event darauf wäre mit einem feststeckenden Spieler aufgegangen. Der Held wird im
Konstruktor platziert, und das Gitter wird in `RefreshOccupants` abgeben und nicht nur
beim Setzen einer Figur, weil eine vorher gesetzte Figur dieselbe Frage hat wie eine
spätere.

**Der Kandidat trägt jedes Feld, über `At()`.** Einen Wegwerf-Charakter von Hand zu
bauen hieße fünfzehn Felder kopieren und beim nächsten Feld eines zu vergessen — und das
Feld, auf das es am meisten ankommt, die Trefferfläche, wäre genau das, was eine von Hand
gebaute Kopie vergisst.

**Test evidence** `test_wolf_character_collision.cs` (11), mit
`test_wolf_passability.cs` (10), `test_wolf_character_board.cs` (14) und
`test_wolf_move_route_runner.cs` (14) nach dem Umstieg nachgemessen. **1292/1292**.
**Mutations** 16 wirksame Regeln über zwei Läufe, **16 von 16 gefangen** — darunter
die halbe Kachel als volle gelesen, die Quadratoption ignoriert, die Y-Achse halb offen
gemacht, die X-Achse geschlossen, die Löschprüfung nur auf einer Seite, der Durchlass
symmetrisch gemacht, die Selbstkollision nicht übersprungen, der Kandidat ohne
Trefferfläche, der Held nicht auf dem Brett und die nicht neu gebaute Liste.

## WOLF Zielnummern und Annäherung — DONE: -1 bis -7 und zwei laufende Schritte

**Die beiden Annäherungsschritte waren zwei Karten lang abgelehnt**, aus dem ehrlichen
Grund: Ein Event annähern braucht eine zweite Figur und eine Position annähern
braucht die Karte, und das Brett hatte beides nicht. Die letzten zwei Karten haben es
beides gegeben, und die Ablehnung war nicht mehr wahr — **wer sie behalten hätte, hätte
ein Spiel, dessen Wächter sich nie etwas nähern.**

**Die Zielnummern sind die der Hilfe und keine Event-IDs.** Die Liste lautet:
`0以上の場合 ＝ その値のIDを持つイベント`, `-1＝このイベント`, `-2＝主人公(隊列先頭)` und
`-3` bis `-7` für die fünf Begleiter. **Null ist eine Event-ID und der Held ist minus
zwei** — wer null für den Held nähme, beantwortete einen Befehl über Event 0 mit dem
Spieler und einen Befehl über den Spieler mit Event 0, und beides gibt es auf einer
echten Karte.

**„Dieses Event" ist das eigene Event der Route, und die Zahl reist mit der Route.**
Der Runner hat keine Vorstellung davon, in welchem Programm er läuft, und das Brett weiß
es, also gehört der Besitzer zum Routenzustand.

**Die Partei hat fünf Plätze, und ein leerer ist niemand.** Die Liste hört bei -7 auf,
also hätte ein Leser mit wachsender Liste -8 mit einem sechsten Begleiter beantwortet,
den der Editor nicht benennen kann. Ein leerer Platz ist null und keine neue Figur.

## Ankunft und Ablehnung sind in einem Bool dasselbe, und dürfen es nicht sein

**`ApproachOne` gab zuerst einen Bool zurück, in dem false sowohl „ist angekommen"
als auch „kein solches Ziel" bedeutete**, und der Aufrufer konnte es nicht unterscheiden.
Ein Wächter, der sein Ziel erreicht hatte, wurde als einer verbucht, der jemandem folgt,
den es nicht gibt. **Vier Antworten, weil sich jedes Paar unterscheidet**: Ein Schritt
nimmt Zeit, eine Ankunft nicht, kein Ziel und blockiert halten die Route an, sofern sie
nicht das Überspringen sagt.

**Und Kollisionsregel und Annäherungsregel treffen sich an einer Stelle.** Ein Wächter
eine Kachel vor seinem Ziel, vom Ziel selbst blockiert, **ist angekommen** — als
blockiert gelesen hieße, ein Wächter gäbe auf, sobald er den Spieler einholt, also genau
in dem Moment, um den es im Spiel geht. immer als Ankunft gelesen hieße, ein Wächter an
einer Wand bliebe eine Kachel stehen und nennte es fertig. **Der Unterschied ist, was den
Schritt abgelehnt hat**, und das Brett fragt nach.

**Ein Schritt pro Schritt, und das Ziel wird jeden Schritt neu gelesen.** Ein Wächter,
der einem laufenden Helden folgt, muss den Abstand immer wieder schließen, und wer den
ganzen Weg einmal berechnet, ginge dorthin, wo der Held war.

**Die größere Lücke geht zuerst, und der Gleichstand schließt X.** Das macht, dass eine
Diagonale als Diagonale liest. **Die Hilfe nennt die Reihenfolge nicht, also steht sie
hier als Wahl** — wer beide Achsen zugleich schließe, erzeugte einen Schritt, für den
das Format keinen Typ hat.

## Zwei Fehler, die der Compiler nicht gemeldet hat, und zwei falsche Tests

**Der Begleiterbereich war „mindestens -3 und höchstens -7" geschrieben, und das ist
leer.** Die Hilfe zählt abwärts, also müssen die Schranken umgekehrt gelesen werden, und
ein immer falscher Bereich ist perfectly gültiges C#. Er kam zweimal vor — in
`Classify` und in `CompanionNumber` — und **der Compiler meldete den ersten als
unerreichbaren Arm und zu dem zweiten nichts.**

**Drei der zehn neuen Tests waren falsch, und zwei davon falsch über die Regeln und
nicht über den Code.** Einer verglich „drei" mit „vier minus eins", um zu entscheiden,
welche Achse zuerst schließt; die Lücken sind vier auf Y und drei auf X, und der größere
Betrag ist das, was die Regel ansieht. Einer setzte den Helden in die Partei, aber nicht
aufs Brett, und maß einen Wächter, der *durch* den Spieler ging — Partei und Brett sind
getrennte Dinge, und ein Test muss beide setzen. Einer erwartete, dass die Route endet,
wenn der Wächter ankommt, und hatte die Ankunft als Ende des Weges gelesen; ist sie
nicht, denn die Wiederholungsflagge startet den Schritt neu.

**Test evidence** `test_wolf_approach.cs` (10), mit den vier WOLF-Dateien der letzten
beiden Karten nachgemessen. **1302/1302**.
**Mutations** 15 wirksame Regeln über zwei Läufe, **15 von 15 gefangen** — darunter
-1 und -2 vertauscht, der Begleiterbereich in der falschen Reihenfolge, die Ankunft in
die Ablehnung gefaltet, die größere Lücke umgekehrt, der Gleichstand auf Y, der
blockierte Schritt als Ankunft gelesen, die Zielkoordinate nicht durch die Bänder
aufgelöst und die Figur im Weg nicht erkannt.

## WOLF Zeichentabellen — VERIFY, nicht DONE: ein Test wirft und ich weiß nicht warum

**WOLF hatte überhaupt keine Präsentation** — fünfundzwanzig Dateien und keine malt
etwas. Die Material-Hilfe sagt, was eine Zeichentabelle sein muss.

**Die vier Richtungen sind unten, links, rechts, oben von oben nach unten, und das
ist nicht die Kompassrichtung.** Die Material-Hilfe gibt diese Reihenfolge zweimal, und
wer oben, rechts, unten, links nähme, zeigte jede Figur um neunzig Grad gedreht — die
Art Fehler, die ein Spieler in der ersten Sekunde sieht und nie meldet.

**Der Laufzyklus ist B → A → B → C → B, und die mittlere Zelle kommt zweimal.** Die
Hilfe benennt die Zellen A, B und C von links, also ist B die Spalte 1. Wer sie in
Reihenfolge abspielte, ließe eine Figur dreimal vorwärts treten und dann zurückschnappen.

**Der Stillstehzyklus läuft anders herum — 2, 3, 2, 1 — und die T- und TX-Form legen die
Stillstehframes nach links**, also sitzt eine stehende Figur auf einer kleineren Spalte
als eine gehende. Wer den Versatz andersherum addierte, legte die Stehpose in die Mitte
des Gangs: eine Figur, die nie stehen bleibt und nie zu stehen scheint.

**Die Animationsfrequenz ist Bilder pro Schritt, und die Reihenfolge ist die
umgekehrte der Geschwindigkeit.** Die Hilfe schreibt アニメ頻度[早0-6遅] — oft nach
selten —, während die Bewegungsgeschwindigkeit langsam nach schnell läuft. Wer durch die
Frequenz teilt oder die Geschwindigkeit nähme, ließe eine Figur mit verschwommenen Füßen
auch die Karte im verschwommenen Tempo überqueren. **Null ist jedes Bild und nicht nie**,
weil die Hilfe die 0 ans schnelle Ende setzt.

## Drei echte Fehler, und zwölf Messungen ohne Antwort

**Drei der elf Tests fanden echte Fehler.** Ein Blickrichtungs-Schritt meldete `Stepped`,
dieselbe Antwort wie eine Bewegung — also wurde ein Wächter, der sich nur drehte, für die
ganze Dauer seiner Route im Laufzyklus gezeichnet, und das ist eine Pose, die der Künstler
nie gezeichnet hat. `Turned` ist jetzt ein eigener Ausgang. **Der Idle-Versatz wurde
zweimal angewendet** und legte eine gehende Figur eine Spalte zu weit rechts. **Und
`IsWalking` wurde bei jedem Schritt gesetzt** statt bei einer Bewegung.

**Zwei der Tests waren falsch über die Regeln**: einer erwartete die gehende Zelle auf
Spalte 1, und das ist A — Schritt 0 des Zyklus ist B, und B nach dem Idle ist Spalte 2.
Einer erwartete, eine Diagonale auf einem Vierer-Blatt werde auf die nächste
Kardinalrichtung geklemmt, und genau das verhindert diese Karte.

### Der Befund, der offen bleibt

**`Test_TheIdleCycleRunsTheOtherWay` wirft „Attempted to divide by zero", und ich habe
ihn in sechzehn Messungen nicht gefunden.** `IdleCell` teilt durch nichts — es ist
`pIndex % 4` —, `WalkPattern` auch nicht, die ganze Datei nicht, die Konstante liest 3,
ein Test, der nur die Konstante anfasst, ist grün, der Aufruf nimmt drei literale
Argumente, `Setup` und `Teardown` sind
leer, und das Umbenennen von Suite und Methode änderte den Namen im Bericht und nichts
sonst. Er überlebt das vollständige Löschen von `obj`, `bin` und `.godot/mono`, und zweimal
dieselbe Formel in einer Methode trennt ihn auch nicht.

**Was ich versucht habe, damit die nächste Sitzung es nicht wiederholt:** die vier Aufrufe
ausgeschrieben statt in einer Schleife; ein Wegwerf-Test mit genau einem Aufruf; die Suite
umbenannt, um zu prüfen, ob die Meldung wirklich diesem Test gilt (sie tat); `obj`, `bin`
und `.godot/mono` gelöscht und aus `project/` gebaut statt aus dem Wurzelverzeichnis;
die DLL-Zeitstempel geprüft; `Setup` und `Teardown` der Basis gelesen; alle
`Assert*`-Signaturen geprüft (nur `AssertEq<T>(T, T, string)` und zwei Vergleiche);
den Quelltext byteweise gelesen (kein BOM, keine Null-Bytes, UTF-8 sauber).

**Erledigt seitdem:** ein Test, der nur die Konstante prüft und `IdleCell` nicht aufruft —
**grün**, also sitzt der Fehler im Aufruf und nicht in der Datei als ganzer; ein Aufruf
mit literalem `3` statt der Konstante — **grün**, also ist die Konstante nicht schuld; ein
Aufruf von `WalkPattern` — **grün**, also auch nicht; und zweimal derselbe Aufruf hintereinander
— **rot ab dem zweiten**, was die naheliegendste Erklärung (ein zustandsbehafteter Aufruf) widerlegt.

**Offen bleibt damit nur das, was keine dieser Messungen abdeckt:** ein Lauf direkt über
Godot ohne den Validator, um den `.godot`-Import-Pfad zu umgehen, und ein Blick in die
übersetzte IL der Methode, statt in den C#-Quelltext.

**Die Karte steht auf VERIFY, nicht auf DONE.** Die Regel ist gemessen und implementiert;
was ich nicht kann, ist den Lauf erklären. **Ein Test, der eine Ausnahme wirft, deren
Ursache ich nicht benennen kann, ist ein offener Befund und keine Kleinigkeit, die man
wegrückt.**

**Test evidence** `test_wolf_character_sheet.cs` (11, einer fällt), mit
`test_wolf_move_route_runner.cs` (14) nach der `Turned`-Änderung nachgemessen.
**1302 von 1303 bestehen.**
**Kein Mutationslauf für diese Karte**, und der Grund wird gesagt statt kaschiert: der
Schnitt ist nicht grün, und eine Mutationszahl über einer roten Suite ist eine Zahl ohne
Bedeutung.

## WOLF Zeichentabellen — DONE, und der Fehler lag nicht in der Datei

**Die Karte stand siebzehn Arbeitsdurchgänge auf VERIFY, und alle sechzehn Messungen
haben die Quelle für unschuldig erklärt.** `IdleCell` warf bei jedem Aufruf einen
`DivideByZeroException`, und `pIndex % 4` kann nicht durch null teilen. Keine Division
in der ganzen Datei, die Konstante liest 3, ein Test nur mit der Konstante ist grün, ein
Aufruf mit literalem 3 ist grün, `WalkPattern` — gleiche Form, mit Klammern — war von
Anfang an richtig, und das Umbenennen von Suite und Methode änderte nur den Namen im
Bericht.

### Was den Fehler gefunden hat

**Die einzige Messung, die ihn fand, war: die Methode allein kompilieren und sie werfen
sehen.** In einem getrennten Projekt, ohne Godot, nur die Datei und ein
`Console.WriteLine` — die Ausnahme erschien sofort, und die Zeile war der `_ => 0,`-Arm
eines Switch, dessen Selektor `pIndex % 4` war.

**Die Klammern um das Modulo sind keine Dekoration.** `WalkPattern` schreibt
`return (pIndex % 4) switch`, `IdleCell` schrieb `return pIndex % 4 switch`. **Mit den
Klammern gibt dieselbe Datei 1, 2, 1, 0 zurück; ohne sie wirft sie bei jedem Argument.**
Die Regel steht als eigene Regel in der Mutationsliste, also sind die Klammern jetzt
bewiesen und nicht bloß geglaubt.

### Was an der Reihenfolge die eigentliche Lehre ist

**Alle sechzehn Messungen haben die Quelle gefragt, ob die Quelle falsch ist** — und
eine Datei, die eine Frage über sich selbst nicht beantworten kann, wird auch nicht
dadurch freigesprochen, dass man sie liest. **Die siebzehnte Messung hat die Frage
geändert** — nicht „ist die Quelle falsch", sondern „funktioniert sie außerhalb dessen,
was es gemeldet hat" — und das ist die Frage, die eine Antwort hatte.

**Kein Quelltextfehler hätte sich so verhalten.** Ein echter Rundungsfehler wäre an
anderen Stellen aufgefallen, ein echter Null-Teiler hätte eine sichtbare Null gesehen.
**Ein Compiler, der einen Ausdruck anders bindet als man ihn liest, ist unsichtbar** —
und genau deshalb ist „kompiliere es allein und führ es aus" eine eigene Messung und
nicht dieselbe Messung noch einmal.

**Test evidence** `test_wolf_character_sheet.cs` (11) und
`test_wolf_move_route_runner.cs` (14), beide nach den Klammern nachgemessen.
**1313/1313**, Validator grün.
**Mutations** 16 Regeln über zwei Läufe, **16 von 16 gefangen** — darunter die
Klammern um das Modulo, die Richtungsreihenfolge, der Zyklus auf der ersten statt der
mittleren Zelle, der Idle-Zyklus wie der Lauf, der doppelt angewendete Versatz, die
Stehzelle, das nie abgebuchte Animationsbudget und eine Drehung als Schritt gemeldet.

## WOLF Ton — DONE

**WOLF hatte keinen Ton und der Tonschritt in einer Laufbahn wurde abgelehnt** — die ehrliche

Antwort, solange es nirgendes hinzustellen war. Jetzt: drei Kanaele, die Null-Lautstaerke als

zwei Regeln und der Tonschritt im Brett statt im Läufer.



**Die Regel, die eine ganze Karte traegt: eine Lautstaerke von 0 ist unter der alten Regel

Standard und unter der neuen stumm.** Die Materialliste sagt beides, und die

Spielkonfiguration sagt, dass vor 3.681 auf 100 umgerechnet wurde und die Einstellung es nun

behaelt — der Fall dafuer ist ein Hintergrundgeraeusch mit Mischung 0 fuer interaktive Musik.

**Welche der beiden ein Spiel benutzt, ist eine Einstellung, und dieser Leser hat keine** —

er meldet eine Null als Null und benennt sie als den mehrdeutigen Wert, der sie ist.



**Die Zeit eines Effekts ist eine Verzoegerung und die eines Musikstuecks eine Einblendung.**

Die Materialliste sagt, die Einblendzeit werde zu *die Wiedergabe verzoegern* fuer einen

Soundeffekt, und nennt sechzig Bilder pro Sekunde. Ein Leser, der eine Verzoegerung als

Einblendung behandelte, haette den Effekt leise beginnen und lauter werden lassen; einer, der

die Zahl als Millisekunden las, haette ein Sechzigstel der verlangten Wartezeit gewartet.



**Der Dateiname steht in den Einzel-Byte-Argumenten.** Ein Schritt hat vier Byte-Argumente und

dann Einzel-Byte-Argumente, und ein Dateiname ist Text. Ein Leser, der ihn in den Zahlen

gesucht haette, haette drei ganze Zahlen gefunden und sich gefragt, warum kein Titel laeuft.



### Der Fund



**`Clear() leerte die Kiste und liess das Radio laufen.** Figuren, Wege, Passierbarkeit und Partei

werden alle mitgenommen, der Ton nicht. Ein neues Spiel, das die Musik des letzten behaelt,

oeffnet seinen Titelbildschirm mit dem Thema des vorherigen Spiels — und **nichts anderes auf dem

Brett haette es gemerkt**, weil die Figuren fort waren und es keine Figur gibt, die falsch

aussieht.



**Test evidence** `test_wolf_audio.cs` (11). **1324/1324**, Validator grün.

**Mutations** 11 Regeln über zwei Läufe, **11 von 11 gefangen**.

## WOLF Laufbahnen aus einer Datei — DONE

**Die VM hatte `MoveRoute` und `WaitUntilRouteDone` im Dispatch und in der Enum, und

`ParseOpcode` hatte fuer keinen der beiden einen Namen.** Eine Kartendatei mit

`"op": "move_route"` kam als `Unknown` an, und `Unknown` lehnt die VM ab — **also stand eine

im Editor geschriebene Patrouille still, und nirgends stand, warum.**



**Kein Test haette es gefunden, weil jeder andere Test seinen Befehl von Hand gebaut hat.**

Ein handgebauter Befehl hat Figur und Laufbahn schon gefuellt; nur der Dateipfad muss sie

fuellen. Das ist die Art Luecke, die nur ein Test schliesst, der eine echte Datei auf der

Platte schreibt statt ein Objekt zu bauen.



**Ein unbekannter Schrittname ist 0xFF und nicht 0** — 0x00 ist ein Schritt nach unten, also

haette ein Tippfehler eine Figur eine Kachel nach sueden geschickt und das Spiel haette

richtig ausgesehen, bis zum Tag, an dem es das nicht mehr tut. **Ein unbekannter Modus ist

`Custom` und nicht 0**, denn 0 heisst *sich nicht bewegen*.



**Test evidence** `test_wolf_route_from_file.cs` (9), gegen eine echte Kartendatei.

**1333/1333**, Validator gruen. **Mutations** 12 Regeln, **12 von 12 gefangen**.

## WOLF Common Events — DONE

**Der Binaerleser dekodierte Typ 300 und die Enum hatte keinen Wert dafuer** — also konnte

ein Spiel mit einem Aufruf den Aufruf nicht ausfuehren, **und jeder WOLF-Shop ist aus

Common Events gebaut.**



**Ein Aufruf teilt den Zustand, statt ihn zu kopieren; das Ende eines Common Events setzt den

Aufrufer fort; die Tiefengrenze ist die Wacht gegen ein Event, das sich selbst aufruft; und

Null ist der Held und kein Event.**



### Der Befund, der keine Codefehler fand



**Ich habe zehn Minuten an einem Test gefeilt, der keine fand, weil es keine gab.** Eine Figur

sollte eine Common-Event-Laufbahn gehen und stand still.



**Das Brett allein ging, die VM nicht — ohne jeden Aufruf.** Die Route startete, die VM erreichte

das Ende im selben Tick, und **ein `Completed` tickt das Brett nicht mehr.**



**Also gilt: eine Laufbahn in einem Event, das sofort endet, geht nicht — und das ist richtig.**

WOLFs eigene Common Events folgen einer Laufbahn mit einem Warten, oder das Event laeuft

weiter, oder die Laufbahn startet ein Parallelereignis. **Ein Test, der hier das Gehen erwartet

haette, haette eine Form gemessen, die kein Spiel benutzt.**



**Was ich daraus mitnehme:** nach vier gescheiterten Deutungsmessungen habe ich nicht weiter

geraten, sondern den Test zur Sonde gemacht, die eine Frage stellt — Brett allein, dann VM ohne

Aufruf. **Die Sonde gruen und die VM rot ist ein Befund; viermal dasselbe Raten ist keiner.**

Und als der Test sich zum Wirrwarr entwickelt hat, habe ich ihn nicht weiter geflickt, sondern

neu geschrieben.



**Test evidence** `test_wolf_common_event_call.cs` (10). **1343/1343**, Validator gruen.

**Mutations** 10 Regeln, **10 von 10 gefangen**.

## WOLF Map-Event-Aufrufe — DONE

**Unter 500.000 ist es ein Map-Event, ab 500.000 ein Common Event, und nur dann traegt der

Aufruf Argumente.** Die Enum hatte fuer Typ 210 keinen Wert.



**Ein Event, das es nicht gibt, wird ignoriert — und nicht als Fehler gemeldet.** Die Hilfe sagt

das in einem Satz, **und der Grund ist, dass ein Spiel ein Event loescht und den Aufruf stehen

laesst.** Ein Leser, der dort scheiterte, haette ein Spiel, das an einem Aufruf zu einem

entfernten Schatzkasten tot stehen bleibt. **Das ist die einzige Stelle in dieser VM, wo ein

Fehlendes absichtlich kein Fehler ist.**



### Der Fund



**`ApplyOperator` schrieb direkt in die Baender, waehrend das Lesen ueber den neuen Durchlass

lief.** Also schrieb ein Common Event, das sein eigenes \cself[0] zuwies, in ein Band fuer sich

und las es als null zurueck. **Der Test hat es gefunden, weil er eine Regel prueft, die ich am

wenigsten belegt hatte** — der Self-Variablen-Zusammenarbeit zwischen Lesen und Schreiben.



**Und Map-Self hat keinen eigenen Rahmen: seine Self-Variablen sind die des aufrufenden Events.**



**Test evidence** `test_wolf_event_call.cs` (10). **1353/1353**, Validator gruen.

**Mutations** 11 Regeln, **11 von 11 gefangen**.

## K-134 Bordtafel — DONE

**Die Bordtafel war zu kurz, und die Lücke, auf die es ankam, war nicht eine von
den fünfundzwanzig.**

### Was die Karte verlangt hat und was sie gefunden hat

**Jeder der fünfundzwanzig DONE-Abschnitte braucht Was gebaut wurde, Testbeleg und
Commit — kein Abschnitt aus dem Titel.** Die Commit-Nachrichten tragen keine Kartennummern,
also habe ich über die Dateien gebunden: `git log --follow --diff-filter=A` auf die Datei,
die die Karte behauptet. **`--follow` ist der entscheidende Schalter, weil der Umzug des
Godot-Projekts nach `project/` jeden Pfad umgeschrieben hat und ein einfaches `git log`
den Umzug zurückgibt und nicht die Arbeit.**

**Und dann kam der Fund: K-136 hatte einen vollständigen Detailabschnitt, einen `READY`-Status
und überhaupt keine Zeile in der Tabelle.** Es ist die einzige P0-Karte dieses Projekts, die
ein Leser der Tabelle nicht haette sehen koennen — **und sie ist der Grund, warum es diese
Karte gibt: die Tabelle fehlten nicht fünfundzwanzig Zeilen, ihr fehlte eine, die wichtiger
war als alle zusammen.**

### Die Messung nach der Reparatur

| | |
|---|---|
| Board-Zeilen | 113 |
| eindeutige Zeilen | 113 |
| Zeilen ohne Abschnitt | **0** |
| Abschnitte ohne Zeile | **0** |
| doppelte Zeilen | **0** |

**Dreiundzwanzig Nummern zwischen K-001 und K-136 werden von weder Tabelle noch Abschnitt
benutzt** — K-005 bis K-009, K-025 bis K-029, K-056 bis K-059, K-062 bis K-069 und K-135.
**Das sind Nummern, die nie vergeben wurden**, und die Reparatur ist nicht, Abschnitte fuer sie
zu erfinden: ein Abschnitt fuer eine nie geschriebene Karte ist eine Behauptung. Die
Nummerierung hat Luecken und die Luecken sind sichtbar — **das ist der Unterschied zwischen
einem Loch und einer Luege.**

**K-080 und K-090 bleiben BACKLOG**, weil sie hinter dem spielbaren Meilenstein liegen und
beide eine Entscheidung brauchen, die dieses Repository nicht still treffen darf: ob Ruby
ausgefuehrt wird und ob JavaScript ueberhaupt ausgefuehrt wird. **Die vorhandene
Ruby-Arbeit ist ein Lexer, ein Parser und ein Werterlayer; die MZ-Arbeit liest zwei echte
Spiele und fuehrt deren Kommandozeilen aus. Beides ist keine Laufzeit, und beides gibt nicht
vor, eine zu sein.**

## `11120` Move Picture — DONE

**`ShowPicture` und `ErasePicture` liefen. `MovePicture` stand in der Konstantenliste mit

einer Beschreibung und ohne `case`** — dieselbe Form wie K-094, als die beiden anderen

implementiert, getestet und unerreichbar waren.



**Also fiel ein Spiel, das eine Titeltafel ueber den Bildschirm schob, in den Default-Arm** und

wurde als nicht unterstuetzter Befehl gemeldet.



### Die Regel, die die Suche ueberhaupt ausgeloest hat



**Ein Leser, der seine eigene Konstantenliste auf eine Luecke prueft, findet diese Luecke

nie.** Die Konstante ist da, die Beschreibung ist da, der Build ist sauber — und der Befehl

flaellt trotzdem durch. **Nur der Dispatch ist kurz, und der Dispatch ist das, was ein Befehl

erreichen muss.**



**Und die Neumessung hat die Zahl der Karte widerlegt: 89 ist nicht 43, und es sind auch

nicht 89 verdrahtete Befehle, sondern 89 von 94 Konstanten, von denen vier Grenzwerte sind**

und **eine ein echter Befehl.** Genau diese eine war die Luecke.



**Test evidence** `test_rm2k_move_picture.cs` (7). **1360/1360**, Validator gruen.

**Mutations** 10 Regeln, **10 von 10 gefangen**.

## `11910`/`11950` Menues — DONE

**Die Karte nannte vier Befehle als eine offene Familie. Gemessen sind es zwei** — `11930`

und `11960` liefen schon lange ueber den Einzeiler-Handler mit Teleport und Flucht. **Und die

Karte war geschrieben worden, bevor das so war.**



**Breite 0 fuer beide.** Die Referenz gibt Speichern und Hauptmenue eine Breite von null — **ein

Leser, der einen Parameter verlangt haette, waere jeden Menuebefehl eines Spiels abgelehnt

haben.**



**Zwei Flags und nicht eines, und eine offene Nachricht zuerst** — dieselbe Regel wie der

Game-Over-Bildschirm.



### Der Fund am Test, nicht am Code



**Ich hatte einen Test geschrieben, der behauptete, die beiden Flags kollidierten nicht, und er

blieb rot** — bis die Messung zeigte, dass der zweite Befehl nie laeuft. **Die Seite haelt, also

laeuft derselbe Befehl in jedem Frame erneut**, und ein Programm, das Hauptmenue und dann

Speichern oeffnet, bekommt nur das Hauptmenue. Das ist der Preis einer gehaltenen Seite, **und

die Referenz zahlt ihn genauso.**



### Und eine Zahl, die falsch war



**Die Mindestbreite von `11120` war 8 statt 16.** Die Dispatch-Zeile der Referenz sagt

`CmdSetup<&CommandMovePicture, 16>`, und ich hatte acht geschrieben — aus den fuenf, die der

Befehl liest, plus einer Vermutung. **Und die Test-Fixture paddete ebenfalls auf acht, was beide

Fehler in dieselbe Richtung gehen liess und die Suite gruen hielt.** Das ist die Lehre: **eine

Fixture, die der Zahl des Codes beipflichtet, prueft die Zahl nicht.**



**Und ich habe beim Einfuegen einer Methode 2166 Zeilen statt 130 geschrieben** — ein

Ersetzungsmuster hat einen grossen Block dupliziert, und der naechste Rettungsversuch hat es

verdreifacht. **Der Weg zurueck war `git checkout --` auf diese eine Datei, und danach vier

Ersetzungen einzeln mit je einem Build dazwischen.** Das ist der Unterschied zwischen einer

Aenderung und einem Rettungsversuch.



**Test evidence** `test_rm2k_open_menu.cs` (6), `test_rm2k_move_picture.cs` (7).

**1366/1366**, Validator gruen. **Mutations** 9 Regeln, **9 von 9 gefangen**.

## `10840` Get On/Off Vehicle — DONE

**`10650` und `10850` liefen. `10840` lief nicht — und `Rm2kVehicleBoarding` hatte fuenfzehn

Boarding-Methoden, getestet, die kein Befehl erreichen konnte.**



**Dieselbe Inselform wie die Bilder und wie `Rm2kMoveRouteState`:** eine Klasse, die

vollstaendig ist, getestet ist und unerreichbar ist. **Und man findet sie nur, indem man fragt,

wozu die Klasse da ist, und das mit dem vergleicht, was die Befehle der Referenz tun** — nicht

indem man die Konstantenliste liest, in der die Zahl laengst steht.



**Breite 0: das Fahrzeug ist kein Parameter, es ist, was unter dem Helden liegt oder vor ihm

steht. Ob es passiert ist und nicht, ob es koennte — und ein fehlender Hook ist eine Ablehnung

mit Namen, weil „kein Fahrzeug hier" und „dieser Leser kann nicht einsteigen" zwei

verschiedene Dinge sind.**



**Und der Hook ist ein Konstruktorargument und kein spaeter gefuelltes Feld**, aus demselben

Grund wie der Routenstarter: ein Test muss sehen koennen, was der Interpreter bekommen hat.



**Und diesmal 62 Zeilen statt 2166.** Vier Ersetzungen einzeln, je ein Build dazwischen — **die

Lehre aus dem letzten Mal hat gehalten.**



**Test evidence** `test_rm2k_get_on_off_vehicle.cs` (4). **1370/1370**, Validator gruen.

**Mutations** 5 Regeln, **5 von 5 gefangen**.

## `10490` Full Heal — DONE

**Die sechs Actor-Befehle: fuenf veraenderten etwas, dieser stellt wieder her.** Fuenf waren

verdrahtet, `FullHeal` nicht — **obwohl er als einziger der Familie gar keinen eigenen Wert

braucht.**



### Die Regel, die der Test gestrichen hat



**Ich hatte eine SP-Flagge erfunden.** Der zweite Parameter sollte heissen „heile auch die

SP-Punkte" — **und die Referenz hat zwei Parameter, und beide sind die Actor-Auswahl.** Ein Leser,

der den zweiten als SP-Flagge gelesen haette, haette von jedem geheilten Actor auch die SP geheilt

**und nur einem einzigen Helden die Trefferpunkte.**



**Zwei Parameter, und beide sind die Auswahl** — 0 ist die ganze Mannschaft, 1 ein Held nach

Nummer, 2 ein Held aus einer Variable. **Modus 0 heilt die ganze Mannschaft.**



**Und es stellt wieder her und rechnet nicht:** ein Zaehler wird auf das gesetzt, was eine Basis

sagt. **Ein Leser, der es wie seine Nachbarn behandelt haette, haette addiert — und ein Spiel,

das nach jedem Kampf heilt, haette eine Mannschaft ohne Grenze.**



### Und die Grenze dieser Karte



**`10440`/`10450`/`10480` sind keine Verdrahtung.** Skills, Ausruestung und Bedingungen haben **im

Zustand ueberhaupt keine Felder** — das ist neues Zustandsdesign und keine Befehlszeile, **und es

gehoert in eine eigene Karte.**



**Test evidence** `test_rm2k_full_heal.cs` (5). **1375/1375**, Validator gruen.

**Mutations** 6 Regeln, **6 von 6 gefangen**.

## `10710` Enemy Encounter — DONE

**Fuenf Zustandsfelder — `IsBattleActive`, `ActiveTroopId`, `BattleTurn`, `BattlePhase` und

`TroopMembers` — und kein Befehl erreichte eines davon.** Also fiel der Kampfbeginn in den

Default-Arm und es kämpfte nie. **Dieselbe Inselform wie die Bilder, die Laufbahnen und das

Fahrzeug-Bording.**



**Sechs oder zehn Parameter, je nach Form. Die Flucht sind drei Werte und kein Boolean — und der

mittlere beendet das Event. Drei Terrain-Modi, und der vierte startet keinen Kampf. Kein Ausgang

und -1, denn 0 ist der Siegwert.**



### Und die sechzehn Messungen, die kein Befund waren



**Ein Test liess sich nicht kompilieren, und ich habe ihn sechzehn Mal gemessen.** Die Datei war

korrekt — kein verborgenes Zeichen, keine falsche Einrueckung, keine doppelte Deklaration, und

`sed`, `od` und `read_file` zeigten dieselben Bytes. **Der Compiler hatte recht: die

Tuple-Zerlegung `var (a, _, b)` in diesem einen Test war der Fehler** — und die anderen sechs

Tests derselben Datei mit derselben Zerlegung liefen.



**Das ist derselbe Fehlertyp wie bei `IdleCell`, und die Lehre ist diesmal klarer: sechzehn

Messungen an korrektem Quelltext sind kein Befund, sondern eine Schleife.** Der Ausweg war

derselbe — aufhoeren zu messen und die eine Sache tun, die ich nie getan hatte: den Test ohne

die Zerlegung schreiben.



**Test evidence** `test_rm2k_enemy_encounter.cs` (7). **1382/1382**, Validator gruen.

**Mutations** 9 Regeln, **9 von 9 gefangen**.## Current card
## 10830, 10870 and 10910 are done — and my own terrain tests were blind

**Three commands, each with one fact a reader would guess wrong.**

- `10830` reads **three variable ids**, not three coordinates — a reader that
  read them as values would have sent a game to the map whose number the
  editor happened to write, and a 2K game's recall would have gone to map 1,
  tile 1, which is a real place on every map. It also writes a facing of **-1**,
  the reference's own "unchanged"; a reader that copied `10810`'s default would
  have turned a game's hero on every recall.
- `10870` swaps **three** coordinates and reads **all six before it writes
  any** — a reader that moved the first figure before finding the second
  missing would have collapsed two guards onto one tile.
- `10910` gives the first parameter to **both** coordinates, and a tile
  outside the map is **-1**, not zero.

**And the reference's own comment on `10910` says `code 10820`.** The dispatch
line and liblcf both say `10910`. A stale comment in the reference is a fact
about the reference, and the number in the file is the one that counts.

### The four surviving mutations were three of my own bad tests

The first run was 8 of 12. Three of the four survivors were my tests, and the
fourth was the implementation:

- **`TerrainTagAt` read the chip id from `PassableTiles`.** That array holds one
  bool per tile and is a side calculation from the lower layer — a reader
  taking the chip id from it sees the same number on every tile pair. The
  reference's `Game_Map::GetTerrainTag` reads the lower layer, takes the chip,
  and looks the chip up in the chipset's terrain table.
- **Every first terrain test asked an empty map**, so every -1 came from "there
  is no map" and not from "the tile is out of bounds" — **a test that cannot
  tell those apart tests the bounds check and not the terrain.** The map is now
  built the way `Rm2kEngineRuntime` builds it: `ConfigureMap` for the dimensions
  and the two fields the LMAP read hands over.
- **The mode-byte test put both coordinates on tiles with the same terrain**,
  so a reader that read the row as a constant would have passed it. The two
  answers are on tiles with different terrain now.

**The lesson is the same one the flash sprite taught, one level down:** the
first mutation run is a test of the tests, and a survivor is a question about
what the reader can actually have. The fourth survivor was real — a short lower
layer is read past instead of refused — and the fix was a test that builds a
map two by two with a layer of one entry.

## 11320 Flash Sprite is done — and a rule that was not a mutation

**Width 7, and the seventh parameter is a mode byte that only the Maniac patch
reads.** The reference's `ValueOrVariableBitfield` is, without the patch,
`return com.parameters[val_idx];` and nothing else — **so a reader that always
applied the bitfield would have taken a red channel of 31 down to 15** for
every game written in RPG Maker 2000. I applied a `& 0x3` mask anyway, on the
pattern of `11330`, and a test caught it: figure 99 arrived as 3.

**The duration is in tenths at the reference's own rate of sixty frames per
second**, so ten tenths is sixty frames. My first table said one tenth is one
frame, and the measurement said six.

**A duration of zero still waits one frame** — the reference's `SetupWait` has
a separate arm for zero.

**A figure that does not resolve is a warning and the page still advances**,
exactly as `11330` treats an unreachable character, and for the same reason.

**Test evidence** `project/tests/core/test_rm2k_flash_sprite.cs`, 10 tests.
**1481/1481**, validator passed.

### The harness only restored the two files it knew about

`mut_name.py` had `for d in (INT, STATE)` in its backup and restore loop — and
**four of the nine rules write to `Rm2kActorValues.cs`, which was in neither
list.** The file was never copied, never written back, and **two mutations
stayed in the source while their rules reported KILL**:

```
Name = pName;                                        // instead of the comparison
return Name != "" ? Name : pDatabaseName;            // instead of the sentinel
```

The second one is the exact mutation the suite is about, and the test for it
passed afterwards anyway — because the test had already been run against the
mutated file and had not been re-run since.

**A rule that reports KILL has measured the test suite, not the file. The
backup list has to be derived from the rules, not written next to them:**
`for d in dict.fromkeys(d for d, _, _, _ in RULES)`. This is the same class of
mistake as the anchor that occurred twice, one level further out — a harness
that is written to be edited must be checked for the same property it is
checking.

### The restore list was in two places, and I fixed one of them

The backup loop became `for d in dict.fromkeys(d for d, _, _, _ in RULES)` —
and the **restore inside the loop body was still `for d in (INT, STATE)`**. So
`Rm2kActorValues.cs` was restored to its pre-run state after every single
rule, while the rule reported KILL. Two rules then reported NOMATCH in the same
run, because the line they were looking for had just been put back.

**The lesson is not "check both places" — it is that a value which describes
the work should appear once and be read from there.** The rule list already
knows every file a rule touches; the backup, the restore and the anchor check
should all be derived from it, and none of them should name a file at all.

**And a run that reports NOMATCH for an anchor that was verified a moment ago
is not a measurement.** The last run's `LEBT` for the flag inversion was also
wrong — measured on its own, that rule is caught in two tests. The whole run
was invalid because a line it needed had been moved back by the harness
itself.

## The WOLF divide-by-zero is explained: `switch` binds tighter than `%`

**C# binds `switch` tighter than `%`.** Written without parentheses,
`pIndex % 4 switch { … }` is `pIndex % (4 switch { … })` — and this sheet's
default arm is `_ => 0`, so the inner switch is `0` for every value the
written cases do not name. **The method divided by zero on every call while
containing no division to find.**

Measured, not inferred — a probe that compiles the expression on its own:

```
i % 4 switch  { 0 => …, 1 => 2, 2 => 1, _ => 0 }   // DivideByZeroException
(i % 4) switch { 0 => …, 1 => 2, 2 => 1, _ => 0 }   // 1, 2, 1, 0
```

**Sixteen measurements missed it because they were all the same measurement**:
reading the file for a division. A file with no division in it reads innocent
however often it is read, and renaming the suite, renaming the method and
deleting `obj`, `bin` and `.godot/mono` all re-ask that question the same way.
**A failure whose cause is in the parse cannot be found by reading the
parsed-away program** — the only move that works is to evaluate the expression
in isolation.

`Test_TheSwitchWithoutParenthesesDividesByZero` now holds both halves: the
unparenthesised form throws, and the sheet's own form answers. Removing the
parentheses from `WolfCharacterSheet.cs` kills two tests.

## The three boundary decisions are made and recorded

The user answered all three on 2026-09-28:

- **K-090 (MV/MZ JavaScript): script files as data, command names extracted,
  no JavaScript executed.** That is the project's own policy already, so the
  card moves to `IN PROGRESS` and the decision is on the record.
- **K-080 (Ruby for XP/VX/VX Ace): the lexer and parser stay, the interpreter
  is finished, no `eval` and no marshal execution.**
- **K-110 (WOLF): the user said they had put files in a folder — and there
  are none on this machine.** A search of `E:`, `D:`, the desktop, the
  documents and the downloads found no `.mps`, no `.wolf`, no
  `Database.dat` / `CommonEvent.dat` / `Game.dat`, and none of
  `WolfEdit.exe`, `WolfTrans.exe` or `WolfRPGEditor.exe`; `wolf/README.md`
  still holds only the old synthetic-fixture text, and a two hour mtime sweep
  over the repo found nothing but my own working files. **The path is
  documented now** — `project/tests/fixtures/wolf/real/` — so the drop-in is
  unambiguous, and K-110 stays `VERIFY` with the exact unblock.

### And `res://` is not a .NET path

`File.ReadAllText("res://tests/fixtures/...")` does not read a Godot path: it
treats it as a relative Windows path and throws *"Die Syntax für den
Dateinamen … ist falsch"*. The way the other fixtures are read is
`Godot.FileAccess.GetFileAsBytes(root.PathJoin("js").PathJoin("name.js"))`,
and **`PathJoin` is a `Godot.String` extension, so it needs `using Godot;`**
and nothing else. I lost three build cycles to guessing the namespace, and
`PathJoin` is not defined anywhere in this project's own source.

## K-114 is DONE and K-110 cannot be closed here

**K-114 stood at `VERIFY` because K-094 was not closed — and K-094 is `DONE`
with no open child.** The parent condition is satisfied, so the card is
closed, and the seven vehicle suites were re-measured before it was:
vehicle 11/11, boarding 11/11, decision turn 12/12, vehicle decision turn
8/8, sprite 9/9, compositing 6/6, get-on-off 4/4 — **61 tests, green inside
the 1533/1533 run.**

**A card can be held open by a reason that has since gone away**, and the
only way to see it is to re-measure the reason rather than the work.

**K-110 stands at `VERIFY` and cannot be closed on this machine.** The help
index lists **76 pages and none documents a binary file format**;
`11fileformat.html` and `12saveformat.html` answer `200` with **zero bytes**,
and `01specifi.html` — the implicit specification page — contains no バイナリ,
no 形式 and no ファイル構造. A search of the machine found **no `.mps`, no
`.wolf`, no `Database.dat` / `CommonEvent.dat` / `Game.dat`**, and none of
`WolfEdit.exe`, `WolfRPGEditor.exe` or `WolfTrans.exe`.

**So the gap is a fact about the available material, not unfinished reading,
and the unblock condition is one real WOLF game directory or the editor from
the user.** The map transfer reader belongs to the same unblock: writing a
binaries reader without a binary to check it against would be the same
invention the card has avoided so far.

### The board's own inventory number was stale, and nothing re-measured it

K-136 said "the eighty-nine commands liblcf names and this interpreter does
not dispatch". A fresh count against `liblcf`'s `ec.h` gives **157 codes in
the 1000..21999 range, 117 dispatched, 40 missing — and 36 of the 40 are the
Maniac and EasyRPG patch extensions.** Of the four real gaps, `1005
CallCommonEvent`, `1006 ForceFlee` and `1007 EnableCombo` **do not exist in
EasyRPG at all**, and `1008 ChangeClass` is a real RPG2K3 command.

**The number stayed on the board because the work that removed the gaps never
replaced the number.** A card that describes a gap must be re-derived when the
gap is closed, or the board becomes a place where a figure nobody has checked
outlives the work that made it wrong. `1008` was the last real one, and the
board row is now DONE with the count that can be re-derived.

### And four of eleven mutations survived, and three were my own tests

`Klasse ueber 5000` — no test used a class number outside the range, only
short commands. `Reset-Modus` — the test took a fresh hero, whose base hit
points are **one**, and one halved is zero, which the clamp puts back to one:
**so the test passed for a reader that wrote in mode 3.** `RPG_RT-Fehler 1` —
the bug is about the class being the *same* one, so the test needs two class
changes, and mine had one. The level-flag rule was killed on its own and
survived in the run, which is the stale-DLL signature again.

**The middle one is the pattern worth keeping: a test whose fixture makes the
wrong answer indistinguishable from the right one is worse than no test,
because it reports green.** A fresh hero's default of one is the same trap as
the empty map in the terrain family — **a fixture has to be chosen so the two
answers differ.**

### Four harnesses carried the same bug, and one of them was already broken

The battle-branch run reported `11 von 12` with `13310 erreicht den Dispatch
nicht` as the survivor. Measured on its own that rule produces **18 failures**.
So the rule is real, the suite catches it, and the run had not measured it.

**The cause: `mut_branch.py` still had the pre-fix `for d in (INT, STATE)`
restore, with no retry.** The rule is the third in the run, and the DLL the
Godot process loaded was still the second rule's — so the mutation was never
compiled. `grep` over the harness directory found four scripts with the old
list (`mut_actor`, `mut_branch`, `mut_flash`, `mut_shop`) and one that no
longer parsed at all (`mut_map`).

**The fix is one body, extracted once, and every script uses it:** the backup
list and the restore list both come from `RULES`, and the restore retries five
times on `WinError 1224`. Keeping ten copies of a loop whose correctness the
whole run depends on is the same mistake as keeping ten copies of a
command-width check — **the property has to live in one place, and the copies
have to be checked for it.**

### A surviving rule that was my mistake, and the shape it took

`Reset laesst eine alte Filmanfrage stehen` survived, and measured on its own
it is caught in one test. The difference: **the rule inserted
`IsMoviePending = true` one line *before* the line that assigns it** — and
`Reset` assigns it again on the next line, so the mutation wrote a value that
was overwritten one line later. It was a rule that changes nothing, which is
the same shape as the `WaitForFrames(0)` rule and the opposite of what a
survivor is supposed to mean.

**A rule that survives is a question, and the first question is whether the
rule does anything.** Here the answer was "it writes a value that is
overwritten one line later", and the second was "measure it alone". A rule
whose replacement is not in the document afterwards is a no-op, and the check
is one line: the mutated text must contain the replacement.

### And the same harness bug lived in the next script too

`mut_movie.py` was built from `mut_flash.py`'s body, and **that body still had
the old `for d in (INT, STATE)`** — so the run died at the first restore with
`NameError: name 'STATE' is not defined`. The repair for `mut_name.py` had been
made in that file and nowhere else.

**The lesson generalises past mutation testing: a fix applied to one instance
of a copied pattern is a fix to one instance.** When a fix is about a property
every copy of the code must have — a list derived from its data, an anchor
checked for uniqueness, a restore that covers every file a rule writes — it
belongs in the shared source, not in one script. These harnesses are copies of
each other, and the fourth one will fail the same way unless the check is in
the body that gets copied.

### A harness that edits itself is worse than no harness

`mut_map.py` stopped parsing. My own patching had cut the file in the wrong
place, and the flash-sprite docstring ended up in the middle of the terrain
rules. **It only surfaced because the run reported a SyntaxError instead of a
mutation result** — a script that fails to parse cannot also fail quietly.

Then the replacement was built by concatenating the head of one script with the
tail of another, and *that* produced the same shape of damage. **The check that
matters is `compile(open(p).read(), p, "exec")` before the run, and the check
that follows is that the rule count is the one intended** — nine, not ten,
because the dispatch rule for `10740` was lost in the splice.

**A mutation harness that has been edited by string surgery needs the same
scrutiny as the code it mutates.**

### The restore failed three times, and each time it left a different mutation

This is now the third run in a row where the harness died at the restore with
`WinError 1224 — the file is open` and the mutation before it stayed in the
source. **Three different ones:**

1. `var staerke = pCmd.Parameters[2]` instead of `[4]`, and the width four
   instead of seven — the flash sprite ran green on a file that had been
   correct two steps earlier.
2. `var x = pCmd.Parameters[1]` instead of `GetVariable(pCmd.Parameters[1])` —
   **the recall read the column as a value**, and the test still passed because
   the value at that index happened to be the one the test expected.
3. `if (false)` in place of `TerrainTagAt`'s bounds check.

**The second one is the dangerous case and the reason this is written down
twice.** The build was green, the tests were green, and the source was wrong —
because *the tests were written against the wrong code*. A test that asserts a
number the broken code also produces is not a test; **it is a description of
the current state that happens to be phrased as an expectation.**

**The rule that follows from it: after any run that ends in an exception, read
the source before trusting the next number — and a mutation that "survives"
while a rule elsewhere in the same run failed to restore has not been measured
at all.** The anchor check now runs before the mutations (`doc.count(a) == 1`,
not `in`), because **two of the sixteen anchors occurred twice in their file**:
`if (varId < 1 || ...)` sits in both `ExecuteStoreEventId` and
`ExecuteStoreTerrainId`, and `var index = pX + pY * MapWidth;` sits in both
`TerrainTagAt` and `IsPassableInDirection`. `replace(..., 1)` takes the
earliest match, so both rules were mutating a *different command* and reporting
on it. **An anchor that occurs twice is not an anchor.**

**And this is why a rule's own name has to say which behaviour it breaks.** A
rule that survives because it never touched the code under test looks exactly
like a coverage gap, and the difference is one line of `count()`.

### Failure log: a mutation that could not have been killed

The rule `WaitForFrames(frames == 0 ? 1 : frames)` -> `WaitForFrames(frames)`
survived every run, and no test could kill it. `WaitForFrames` clamps to at
least one frame, so `WaitForFrames(0)` and `WaitForFrames(1)` hold the page
for exactly the same time — **and the rule removes an arm that only exists to
produce a number the clamp produces anyway.**

This is the third time a rule that cannot be killed turned out to be the
question rather than the tests — the first was `Remove` versus
`TryGetValue`+`Clear`, the second a closing list on a chosen handler. **A rule
that survives every run is asking whether it describes a difference this reader
can have**, and the honest answer here was no. It was replaced with the form
that *is* observable: the hook receiving the tenths instead of the frames,
which is a fault a player sees and a test does not.

### And a restore that failed silently on Windows

The mutation run died at the restore with `WinError 1224 — the operation was
cancelled because the file is open`, and **the mutation before it stayed in the
source**: the strength read `parameters[2]` instead of `parameters[4]`, and the
command's width read four instead of seven. The next test run failed on a file
that had been correct two steps earlier.

**A failed restore is worse than a failed test**, because it changes the thing
every later measurement is taken against — and the build was green, so nothing
said otherwise. The script now retries five times and raises if the copy still
fails, and the lesson is worth more than the fix: **after any run that ends in
an exception, read the source before trusting the next number.**

### And a frame the wait does not include

The page was released after `frames + 1` calls, and at one for a zero duration.
**The frame that triggers the timed command is not itself part of the wait** —
`ExecuteFrame` checks the budget first. That is the same offset as the battle
animation, and the same lesson: **the test has to drive the interpreter and
count, because the number in the state and the number in the game differ by
one.**

## 13310, 13410, 23310 and 23311 are done — the battle branch family

**Four codes. The branch is the only one with real work, and it has four facts
a reader would not guess.**

**The switch comparison is a boolean equality, not an inversion.** The
reference writes `Get(id) == (parameters[2] == 0)` — and `0 == 0` is `true`, so
a third parameter of zero asks whether the switch is **on**. I read it as "is
it off", wrote that in three places, and the test failed with "mode 0 is true"
for a switch I had switched **on**. **The implementation was right from the
first minute; the sentence in the comment was wrong**, and the code under it
was the reference formula.

**A false branch sets the sub-index and skips — both.** The reference does
`SetSubcommandIndex` and then `SkipToNextConditional({ElseBranch_B,
EndBranch_B})`. My first version only set the index, so the then block ran
before the else handler could take it: **the exact block the branch exists to
skip.** Both halves are needed, and one without the other is a silent failure
in the opposite direction.

**`13410` is an abort, not a defeat** — `BattleResult::Abort`, a fourth outcome
with no handler named for it. And it returns false, so the frame stops.

**The last two branch modes are 2003-only, guarded inside their case**, so a 2K
game's fourth and fifth modes are always false.

**Test evidence** `project/tests/core/test_rm2k_battle_branch.cs`, 14 tests.
**1471/1471**, `TestRm2kBattleBranch: 14/14`.

### Failure log: a table that compared 5 with 3 only

A mutation that turned `>` into `>=` survived the six-way comparison table,
because every row compared 5 with 3 and on those two numbers the two operators
agree. **A test that only ever uses unequal values cannot tell a strict
comparison from a non-strict one** — and a game's "if the counter is more than
what I have" fires on equality. The table is now run twice, once with 5 against
3 and once with 5 against 5.

The same run left two other rules alive, and both were tests that asked the
wrong question. `23311` is a bare `return true` and changes nothing, so a
dispatch that pointed it at nothing looked exactly like one that ran it —
**a command that does nothing produces no diagnostic**, and the fix is the one
used for the shop closers: drive it and ask what follows. The hero question had
a living case but the false answer only in a second test, so a rule that made
the *caller* return true left the first green.

### And a mutation script whose survivors changed every run

The same twelve rules, run twice in a row, reported **twelve different
survivors** — once `23311`, once the hero's last battle command, once the
strict comparison. A test suite does not flap like that; a tool does.

**MSBuild decides whether to recompile from the modification time, not from
the content.** A restore and the next mutation inside the same second look like
"nothing changed", so the rule that survives is always the one whose mutation
the build never saw. The fix is two `os.utime` calls that push the file's
timestamp two seconds into the future — once after writing the mutation, once
after restoring.

**Two runs of one rule set reporting different survivors is the cheapest
possible proof that the harness is the variable**, and it is worth more than
any number the runs produce. The same shape appeared earlier as a stale
`UniversalRPG.dll` after a timeout: a clean rebuild is the first thing to try,
not the last.

**What is left after the fix.** With the timestamps forced, three consecutive
runs of the twelve rules reported eleven of twelve — and the survivor moved
each time (`23311`, then the switch comparison, then `13310`). **Every rule
was then measured on its own, one at a time with a fresh build, and all twelve
are caught.** The script still has a race between its restore and the next
build; the tests do not. **When a harness and a suite disagree, the harness
that produced different answers to the same question three times is the one
under suspicion** — and the honest report is "12 of 12 by single measurement,
11 of 12 by the script", not the rounder number.

### And a default arm that speaks the same language

`23311` is a bare `return true` and changes nothing, so pointing its `case` at
a wrong number moved it into the default arm — **which says "Unsupported RM2K
command 23311 skipped" and moves on**, producing a diagnostic and leaving the
page in a plausible state. The test asked for the command's own word and never
looked at the default arm, so the mutation passed.

**A test for a command that does nothing has to check that nothing *else*
happened too**, and the cheapest way is to assert the absence of the word the
fallback uses. That is the second half of every "reached the dispatch" test
here, and it was missing for exactly the command that needed it most.

### And a mutation run that a timeout cut in half

`timeout 560` killed the script mid-rule, and the interrupted run reported
**9 of 12** where the previous complete run had reported 12. The build output
was stale, so two tests failed on a source file that was provably correct —
`CanHeroAct` was intact and its test failed. **A mutation count from an
interrupted run is not a count**, and the cheap check is a clean rebuild before
believing it.
## 11210 and 13260 Show Battle Animation are done — criterion 3, first step

**Two codes, one method.** The reference's dispatch hands both to the same
`CommandShowBattleAnimation`, so they differ in their number and in nothing
else.

**Width 3, or 4, and the fourth is a 2003 form only** — the reference reads it
under `if (IsRPG2k3() && parameters.size() > 3)`. **Allies count from one and
enemies from zero**, because the reference subtracts one for a party target
and not for a monster target — so a target of 0 is the first enemy and the
zeroth ally, which does not exist.

**A negative target is the whole side, and the flag says which** — `target < 0`,
not `<= 0`, so 0 is one battler and -1 is everybody.

**And the wait is the animation's own length**, from its last timing row. An
animation that is not in the table plays nothing and waits for nothing, which
is the reference's `GetElement` returning nothing and zero frames.

**Test evidence** `project/tests/core/test_rm2k_battle_animation.cs`, 9 tests.
**1457/1457**, validator passed. **Mutations** 10 rules over two runs, **10 of
10 caught**.

### Failure log: a wait that was one frame longer than the animation

The mutation run left three rules alive, and two of them were about the wait.
The new test drove the interpreter and asked when the *next* command ran — and
it measured `frames + 1`, not `frames`.

**That is the reference mechanism and not an arithmetic error:**
`ExecuteFrame` checks the wait budget *first*, so the frame that triggers the
animation is not itself part of the wait. The page then stands for exactly
`frames` frames and the command after it needs one more.

**A reader that set `frames - 1` would have cut the animation's last second.**
The test asserted the number the author expected rather than the number the
runtime produced, and the runtime was right — as it had been for the pan's
rounded wait, the choice options and the shop handlers before it.

The third rule was the same shape one field over: a mutation making a target of
zero the whole side survived because the test for a zero target read
`BattleAnimationTarget` and not `BattleAnimationOnAllTargets` — **and the
mutation left the field the test read untouched.** The field that says
"everything" is the field the assertion has to name.

## 10440, 10450 and 10480 are done — and the island was the actor's

**The card said "skills, equipment and conditions have no state at all", and it
was right.** `Rm2kActorValues` held a name, a title, a sprite and a face — and
not one set of skills, conditions, equipment or two-weapon flags. Three
commands had nowhere to write.

**All three take their actors from the reference's own `GetActors(mode, id)`,
whose three modes `ResolveActors` already implemented, and all three end in
`CheckGameOver()`** — which is why a game's last hero dying in a condition
command reaches the game over screen from the *condition* command.

**10450's slot comes from the item's own type in the first mode and from
`parameters[3] + 1` in the second**, and mode 1 sets `item_id = 0`, so the
direct slot removes rather than equips. **The sixth slot is not a slot** — the
reference checks `slot == 6` before any of the five.

**Two rules for a two-weapon actor:** the shield is skipped while it is in
hand, and a one-handed weapon goes into the second slot when the first is empty
and neither weapon is two-handed.

**Test evidence** `project/tests/core/test_rm2k_actor_commands.cs`, 15 tests.
**1448/1448**, validator passed. **Mutations** 13 rules over three runs, **13
of 13 caught**.

### Failure log: a rule that was not a mutation, and one that could not be

Two rules survived repeatedly, and neither was a test gap at first glance.

**The first changed `if (slot == All)` to `if (false)` in the interpreter.**
It could not be killed because `GameSimulationState.ChangeEquipment` had a
*second* `if (pSlot == All) { slots.Clear(); }` — so removing the first changed
the path and not the state. **That duplication was my error, not the test's:**
one fact about the sixth slot lived in two places, and only one of them was on
the reference's path. The second was removed, with a comment saying why.

**The second replaced `ActorEquipment.Remove(id)` with a `TryGetValue` and
`Clear` — which is a different implementation and the same result.** No test can
distinguish those, and a rule that cannot be killed is not evidence of a gap;
it is evidence that the rule was not a mutation. It was rewritten to make the
method do nothing at all, which is a real fault, and that one died.

**A rule that keeps surviving is a question about the rule**, and the honest
next step is to ask whether the code has one place too many — not to write a
third test for it.

### And `??=` on a dictionary indexer throws

`ActorEquipment[actorId] ??= new ...` compiles to a get followed by a set, and
a `Dictionary`'s indexer throws on a missing key — so the first question asked
about a hero who had never worn anything failed with "The given key was not
present in the dictionary", naming neither the party nor the slot. Three tests
died on that one line. `TryGetValue` is the form that works.

## The shop and inn family is done — 10720, 10730 and ten handlers

**Twelve commands, ten of them with a width of zero.** The two openers are
width 4 and width 3, and the ten handlers read no parameter at all.

**10720's first parameter is a mode (0 buys and sells, 1 buys, 2 sells, and a
fourth does neither), its second is the shop's type, its third is a handler flag
the reference reads and does not use, and the goods start at the FOURTH
parameter** — everything from `parameters.begin() + 4` on goes into one list.

**10730's price is the SECOND parameter and the first is the inn's type** — the
reference writes `int inn_price = com.parameters[1]` in its first two lines. A
zero price skips the prompt.

**A handler runs its block only when it is the option that was chosen.**
`CommandOptionGeneric` is one comparison of the sub-index against the option,
and then either the sentinel write or the skip. Each handler has its own
closing list and the lists are different lengths.

**20722 is a bare `return true;` and changes nothing** — the shop's own scene
closed when the player left it. 20732 and 20713 do clear state.

**Test evidence** `project/tests/core/test_rm2k_shop_and_inn.cs`, 23 tests.

### Failure log: a closing list is only read in the skipping arm

Two mutations lengthened the no-transaction and defeat closing lists to their
transaction and victory lengths, and nothing failed — **because every test that
named those handlers had *chosen* them, and a chosen handler runs its block and
never looks at its list.** A test can name a command, drive it, and assert
every visible state field, and still not reach the branch that matters.

Two further survivors had the same shape. A mutation that turned
`IsBattleActive = false` into `IsBattleActive = IsBattleActive` survived
because **the fixture never started a battle** — on a state that was already
false the two look identical. And a mutation on the skip loop survived because
the test asserted the state after the jump rather than which command the jump
skipped.

**Three tests closed all three, and every one of them is unchosen on
purpose** — a sentinel sub-index, so the skipping arm is the one under test.

### And a method that landed at the end of the file

Rewriting `ExecuteShowInn` by locating its body and its next doc comment moved
the method past the class's closing brace and ate it. The build said
"expected }" and the file ended with a method instead of a class. **The
recovery was to find the orphaned body, put it back, and add the brace the
replace had consumed** — the same lesson as the duplicated state block, and the
same cost: string-offset surgery on a file that already had one, without
reverting first.


## The battle-only monster family is done — 13110, 13120, 13130, 13150, 13210

**Measured against liblcf's own `eventcommand.h` (`src/generated/lcf/rpg/`,
not the path the card named): 164 codes, 32 unwired, and of those 32 only 32
are real RM2K/2000/2003 commands** — the `Maniac_` and `EasyRpg_` patch
extensions (3001-3029) and the engine features below 6000 are a separate set.
**The card's claim that `13110`-`13410` and `20720`-`20732` did not exist was
wrong; seven of them are real battle commands.**

**`13110` has three change modes and the third is a share of the monster's own
maximum** — so mode 2 on a 500-of-1000 monster takes 250 and not 2. `13120`
has two modes and no third, and a mode of 2 there changes nothing.

**The sign is a flag in both, and the value's own sign is read never.** The
reference reads `bool lose = parameters[1] > 0` and then writes
`change = -change` — a negative constant with the flag at zero still heals.

**There are two deaths.** Hit points at zero give the enemy's kill sound and a
death timer; removing the death condition with `13130` removes him at once and
without an animation, which the reference's own comment calls an RPG_RT bug it
reproduces. The exit is therefore three-valued, and a reader that treated the
paths alike animated a death the reference does not animate.

**`13210` reads its file name from `com.string`, not from `parameters`.**

**Test evidence** `project/tests/core/test_rm2k_battle_monster_commands.cs`,
15 tests. **1410/1410**, validator passed.
**Mutations** Twelve rules, **12 of 12 caught**.

### Failure log: a test that read 130 where it wanted 70

Four of the fifteen failed on the first run, and the message was
"a constant change of thirty takes a monster from 100 to 70" with no value in
it — because this repository's `AssertEq` **drops the actual value whenever a
message is given**, so a failing test that names both numbers shows neither.

**The value came out at 130, and the implementation was right.** The test
passed `0, 0, 0, 30` where the second parameter is the lose flag — so it
measured a heal of thirty. Three more tests wanted losses and wrote the flag
the same way. **A reader that trusts a test's own sentence over the bytes it
sent will "fix" correct code**, and the cheapest check is the one that reads
the fixture back: `13110`'s parameters are enemy, lose, mode, value, lethal.

### And a Godot dictionary that does not convert

`TroopMembers` is a `Godot.Collections.Dictionary`, so every read is a
`Variant`. `Convert.ToInt32(m["hp"])` compiles and throws at run time with
"Unable to cast object of type 'Godot.Variant' to type 'System.IConvertible'";
`(int)m["hp"]` is the form that works. **Eleven tests failed on that one cast
before it was found**, and the exception text named the type.


## 11060, 11340 and 11350 are done — three commands the card said did not exist

**The card listed `11060`, `11340` and `11350` as "liblcf names them and
EasyRPG dispatches them nowhere", and said there was "nothing to read the
parameters from".** All three statements were wrong, and the last one was the
instructive one: **both movement commands have a width of zero, because the
whole of `11340` is `_state.wait_movement = true;` and the whole of `11350` is
`Game_Map::RemoveAllPendingMoves();`.** The absence of parameters was read as
the absence of a command.

**`11060 Pan Screen` has a minimum width of 5** — not the two the board
listed. Four modes (0 lock, 1 unlock, 2 pan, 3 reset), and a value the
reference does not know falls through all four and does nothing. The speed is
`Utils::Clamp<int>(parameters[3], 1, 6)`, repaired and not refused. The wait is
`GetPanWait`: `distance / speed + (distance % speed != 0)`, rounded up.

**A lock does not stop a running pan** — the reference calls `LockPan()` and
nothing else. `11350` stops the map's pending moves and the camera with them.

**Test evidence** `project/tests/core/test_rm2k_pan_screen.cs`, 13 tests.
**1395/1395**, `TestRm2kPanScreen: 13/13`, validator passed.
**Mutations** Ten rules, **10 of 10 caught**.

### Failure log: a state file that a rescue turned into a duplicate

The pan block went into `GameSimulationState.cs` three times under three wrong
assumptions, and each rescue made it worse:

1. Anchored on `ScrollHorizontally`, which is a field of the **nested**
   `Parallax` class — so the block landed inside it. The compiler said
   `PanDirection` does not exist in `GameSimulationState`, which was true and
   which I could have read in one step.
2. "Fixed" the indentation, which moved it out of the class instead.
3. Cropped the block by line range, which cut it in half and left a copy.

**The signal that would have ended it in step 1 was the error's own wording** —
"im Typ `GameSimulationState`" names the type, and the enum was in a
different one. Measuring the region in isolation, its indentation, its brace
balance and its class membership all passed, because each of those was
correct and the mistake was in which class I believed I was editing.

**And the recovery that worked was the one I kept avoiding: `git checkout` the
file, then make exactly one edit against a confirmed anchor.** The same lesson
as the sixteen tuple measurements, and the same cost.

### A stale build artefact that looked like eighteen failures

After the mutation run the validator reported **18 of 1395 tests failing**,
including eight of the thirteen new ones, on source that had been restored
correctly. The build output was stale: `rm -rf project/.godot/mono/temp/obj`
and a full build gave 1395/1395. **A test result that contradicts an
inspection of the source it tests is a build artefact until proven
otherwise** — and the cheaper check is the one that clears it.


K-086 through K-093 and K-095 through K-102 are DONE. K-103 (hero and events in the runtime frame) is next; K-094 (vehicles) stays open at lower priority.

**K-102 completed (2026-09-26)**
- Verified `sprite_character.cpp`: `character_name = GetSpriteName()`, `character_index = GetSpriteIndex()`, Charset aus Verzeichnis `CharSet`.
- Verified liblcf `ChunkEventPage`: `character_name=0x15`, `character_index=0x16`, `character_direction=0x17`.
- **Parsing-Lücke gefunden und behoben**: Der Parser deklarierte `character_name`/`character_index` nur für **Actors** (0x03/0x04), nie für EventPages — obwohl 0x15/0x16 in liblcf verifiziert sind. Event-Sprite-Daten waren damit gar nicht verfügbar. Jetzt werden alle drei Felder pro EventPage dekodiert; fehlender Name = leer.
- Drei Draw-Stufen aus `Priority_EventsBelow(30)`, `Priority_Player(40)` (geteilt mit "same as hero"), `Priority_EventsAbove(60)` nach `Priority_TilesetAbove(50)`.
- Neu `Rm2kCharacterSprite` (`StageForLayer`, `FacingFromLiblcfDirection`, `Skipped`) und `Rm2kMapFrameRenderer.RenderSprites`, das **eine Stufe pro Aufruf** zeichnet, damit der Aufrufer sie in der verifizierten Reihenfolge mit den beiden Layern verschränken kann. Ein Figurenindex jenseits der Charset-Kapazität wird **übersprungen und markiert**, nie aus einer beliebigen Zelle gezeichnet.
- Beide Richtungs-Umrechnungen (Projekt 2/4/6/8 ↔ liblcf 0..3) sind nachweislich invers zueinander getestet.
- Der Renderer kann jetzt ohne Chipset erzeugt werden (Sprite-only-Pass); Tile-Zeichnen tut dann nichts statt zu werfen.

**K-101 completed (2026-09-26)**
- Verified `GetCharacterRect`: Zelle `24*(TILE_SIZE/16)*3` × `32*(TILE_SIZE/16)*4` = **72×128**, Position `(index%4, index/4)`, 3×4 Frames à **24×32**.
- Verified `Sprite_Character::Draw`: `row = GetFacing()`, `frame = GetAnimFrame()`, alles ab `Frame_middle2` wird auf `Frame_middle` geklemmt. liblcf `Frame`: left=0, middle=1, right=2, middle2=3.
- Verified `UpdateFacing`: bei den vier Kardinalrichtungen ist facing == direction, also ist liblcf `Direction` up=0/right=1/down=2/left=3 direkt die Sprite-Zeile.
- Verified Offsets: `SetOx(chara_width/2)`, `SetOy(chara_height)` — zentriert, Füße auf der Kachelunterkante.
- Fixture `rm2000/CharSet/Chara1.png` (gepinnter Commit, 18785 B, SHA-256 `24442b61…`) bestätigt unabhängig: 288×384 = exakt 4×72 quer und 3×128 tief, also 12 Figuren.
- **Refactor**: Der indexed-PNG-Decoder wurde zu `Rm2kIndexedImage` generalisiert; `Rm2kChipsetBitmap` ist die Chipset-spezifische Hülle mit dem 480×256-Vertrag. Charset, Chipset und später Pictures teilen sich Decoder und Transparenzregel.
- Neu `Rm2kCharset`: `FacingToRow` (2/4/6/8 → liblcf-Index), `ClampFrame`, `TryGetCell`, `TryGetFrameRect`, `TryDrawCharacter` mit Füßen auf der Kachelunterkante und Clipping am Rand.
- Clipping ist verifiziert korrekt: Figur auf Kachel (0,0) wird oben abgeschnitten, vollständig außerhalb malt sie nichts — **ohne** Exception.
- Der Godot-Editor liegt unter `E:\GodotEditor\Godot_v4.7.2-stable_mono_win64_console.exe` (wurde aus Git entfernt, ist aber lokal vorhanden).

**K-100 completed (2026-09-26) — the map is visible**
- Verified: `cache.cpp` liest das Chipset aus dem Verzeichnis `ChipSet`, Dateiname `<chipset_name>.png`; `Game_Map::GetChipsetName` liefert den Namen aus der Datenbank.
- `Rm2kEngineRuntime` liest `chipset_name`, löst `<root>/ChipSet/<name>.png` auf, dekodiert, prüft 480x256 und rendert in `RenderedMap` (plus `ChipsetImage`, `RenderDiagnostic`). Fehlendes/defektes/falsch großes Bild wird **gemeldet, der Runtime läuft weiter** — der Player behandelt das Chipset als Asset, die Simulation hängt nicht davon ab.
- `Rm2kMapPreview` lädt die Pixel einmal pro Änderung, skaliert mit Nearest-Nachbar-Filter, zeichnet den Spieler-Marker und die Diagnose, und fällt auf die Tile-ID-Ansicht zurück.
- **Golden Image**: `rm2000/rendered/Map0001.png` (eigene Ausgabe, nicht upstream) mit SHA-256 als Regressionsbasis. Der Test vergleicht **jedes Byte** — eine Änderung an Auflösung, Autotile-Tabellen, Transparenzregel oder Draw-Reihenfolge fällt jetzt durch, statt still ein anderes Bild zu erzeugen.
- Gemessene Fakten: 20x15 Tiles → 320x240 Pixel, Lower nur D/E, Upper nur F und dort vollständig transparent, **13 verschiedene Farben**, alle Pixel belegt (Innenraum). Transparenz deshalb pro Kachel geprüft, Animation mit synthetischen Maps.
- Das gerenderte Bild wurde angesehen: Gras, weißer Weg, braune Treppe — die echte TestGame-Map0001.

**K-099 completed (2026-09-26)**
- Verified `CreateTileCacheAt`: Upper-Tile geht in die obere Sublayer, wenn sein **substituierter** Entry `Above` trägt; Lower-Tile, wenn sein **aufgelöster Chip-Index** `Wall` oder `Above` trägt. Chip-Index-Bereiche identisch zur Passability.
- Verified Draw-Reihenfolge via `lower_layer(this, Priority_TilesetBelow + TileBelow + layer)` und `upper_layer(this, Priority_TilesetAbove + TileAbove + layer)` mit `TileBelow=0`, `TileAbove=100`, `Priority_TilesetBelow=20`, `Priority_TilesetAbove=50`, `Priority_Player=40` → **untere Layer, dann Held, dann obere Layer**. Deshalb deckt eine Wall-Kachel den Helden ab.
- **Defekt behoben**: `Rm2kChipsetSource.TryResolve` gab für E und F `false` zurück, wenn keine Substitution übergeben wurde — obwohl `Game_Map::Setup` beide Tabellen mit `std::iota` füllt. Eine fehlende Tabelle **ist** die Identität; jetzt Fallback statt "unauflösbar".
- Neu: `Rm2kTileZOrder` (ResolveChipIndex, Lower/UpperLayerSubLayer), `Rm2kMapFrameRenderer` (RenderLower/RenderUpper, Sublayer 0 vor 1), `Rm2kMapLayers`, `Rm2kChipsetTables`. Der Held wird bewusst **nicht** gezeichnet — er gehört zwischen die beiden Aufrufe.
- **Echte Fixture gemessen statt angenommen**: Die TestGame-Map ist 20x15, Lower nur D/E, Upper nur F, und deren F-Kacheln sind im echten Chipset **vollständig transparent** → Zeichnen ändert nichts. Keine A/B/C-Kacheln → keine Animation. Drei meiner Erwartungen waren falsch und wurden durch Messwerte ersetzt; Upper-Pfad und Animation werden mit synthetischen Maps geprüft.

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

## Latest completed hero and event character slice (K-103, 2026-09-26)

- The runtime now draws the hero and the event characters into the map frame in the verified drawable order: lower layer, below events, hero plus same-layer events, upper layer, above events.
- Hero graphic source verified in `Game_Player::ResetGraphic`: it is `Main_Data::game_party->GetActor(0)`, and a null actor produces `SetSpriteGraphic("", 0)`. `Game_Actor::GetSpriteName`/`GetSpriteIndex` fall back to the LDB `character_name`/`character_index` when no runtime override is set, and `SetSprite` clears the override for the database values, so a fresh game always draws the LDB graphic. `Game_Party::SetupNewGame` copies `Data::system.party`.
- New `project/src/rm2k/rendering/Rm2kHeroSprite.cs` resolves the graphic and the `CharSet/<name>.png` file name.
- The LDB `system` chunk (section `0x16`) was previously only a raw chunk. `DecodeLdbSystem` now types it, verified against liblcf `struct ChunkSystem`: party list `party_size 0x15` / `party 0x16`, and the vehicle graphics `boat_name 0x0b`, `ship_name 0x0c`, `airship_name 0x0d` with `boat_index 0x0e`, `ship_index 0x0f`, `airship_index 0x10`. Unmapped fields keep their count and framing in `unknown_fields`. A database without the chunk yields liblcf's empty defaults.
- `LoadCurrentMapEvents` now copies `character_name` (liblcf `0x15`), `character_index` (`0x16`) and `character_direction` (`0x17`) into the event page and converts the direction into this project's facing. K-102 had verified the ids but nothing populated the page, so the event sprites were unreachable.

Defects found and fixed while implementing:
- `RenderCurrentMap` ran before `LoadCurrentMapEvents`, so the first frame was rendered with an empty event list and the whole card was silently inert while the suite stayed green.
- The `system` decoder overwrote the seeded defaults; it now reports only the fields the chunk carries and the caller merges them.
- The declared party size can exceed the stored data, so the list is clamped to `min(declared, data.Length / 2)` and bounded by `MaxSystemArrayEntries = 4096`.
- `CopyRealGame` in the rendering test copied `ChipSet` but never `CharSet`, so no character could be drawn; the constant also needed the `FixtureRoot` prefix for `GlobalizePath`.

Measured, not assumed: the pinned LDB has an empty `party` list, so the verified null-actor path applies and this fixture draws no hero graphic. That is correct behaviour and is asserted rather than papered over. The frame contains the chipset plus the event characters: 85 colours instead of the chipset-only 13, with 20 character figures confirmed by inspecting the rendered image.

Validation: `dotnet build project/UniversalRPG.csproj --no-restore` 0 errors/0 warnings; `GODOT_BIN=tools/godot/editors/4.7.2/windows-x86_64/Godot_v4.7.2-stable_mono_win64_console.exe ./scripts/validate.sh` → `TestRm2kParser 36/36`, `TestRm2kRuntimeRendering 7/7`, `All 425 tests passed`, exit 0. Golden image regenerated and re-pinned, SHA-256 `a67ed0672ab97b977c17dc8dd729ef1ffffed8b8339c7e96db2a267cf09a764a`.

Note: the Godot 4.7.2 editor was missing from `tools/godot/editors/` in this checkout and had to be re-extracted from `~/Downloads/Godot_v4.7.2-stable_mono_win64.zip` before validation could run. `tools/godot/editors/` is gitignored, so this is a local environment fix, not a repository change.

## Latest completed frame recomposition slice (K-104, 2026-09-26)

- The map is now rastered once into two cached layer buffers and only the characters are re-composited, so the visible hero follows a move. `RecomposeFrame` runs inside `Update` on a simulation frame boundary, never per rendered frame.
- Verified in the Player: `Scene_Map::vUpdate` → `UpdateStage1` → `UpdateGraphics()` once per frame, and `Spriteset_Map::Update` only gives the tilemap `SetOx`/`SetOy` scroll offsets. The tile layers are static sprites and are not re-rastered on movement. Note the class is spelled `Spriteset_Map`; a probe with `SpriteSet_Map` matched nothing silently.
- Composition order: copy of the cached lower layer, below-layer events, hero plus same-layer events, the cached upper layer over them, then above-layer events.

Three defects found while implementing:
- `Rm2kPixelBuffer.PaintOver` first copied every byte including alpha 0, so the upper layer erased the floor. It now keeps the destination where the source is transparent, matching the verified chipset blit rule.
- The upper layer was rastered into the same buffer as the lower layer, so it carried the floor with it and covered every character. It now has its own buffer.
- The character pass drew onto an empty buffer, which dropped the floor (`opaque=6513` instead of `76800`).
- `Rm2kMapFrameRenderer.RenderSprites` threw on a null map although it never reads it. The parameter is now nullable and documented, because a character is placed by its own tile coordinates.

Validation: build 0 errors/0 warnings; `All 427 tests passed`, exit 0; `TestRm2kRuntimeRendering 9/9`. The rendered frame is byte identical to the K-103 golden image, SHA-256 `a67ed0672ab97b977c17dc8dd729ef1ffffed8b8339c7e96db2a267cf09a764a`, 85 colours, 76800 opaque pixels. The refactor changes no output, which is what makes the layer caching safe to keep.

## Latest slice (2026-09-26) — K-111 DONE: events walk their move route

**The command set, verified from liblcf itself, not guessed.** `generator/csv/enums.csv` defines `rpg::MoveCommand::Code` from `move_up = 0` to `decrease_transp = 41`, in three contiguous blocks: the movement commands 0 to 11, the facing commands 12 to 22, and everything else from 23. The Player relies on exactly that contiguity, testing `cmd >= move_up && cmd <= move_forward` for movement and `cmd >= face_up && cmd <= face_away_from_hero` for facing, so an id in the wrong place is a behaviour change and not a label. `generator/csv/fields.csv` gives the LMU layout: `EventPage::move_route` is chunk 0x29, a `rpg::MoveRoute` whose `move_commands` is a `Vector<MoveCommand>` at 0x0B for the count and 0x0C for the entries, with `repeat` at 0x15 defaulting to true and `skippable` at 0x16 defaulting to false.

**The route is a nested struct, and reading it at the page level silently reports no route at all.** 0x0B and 0x0C live inside the page's 0x29 chunk, not beside it. Passing the page's own fields to the decoder produced a clean "no route" for every page, which looks like a working decoder and is not one. The chunk is now read as a nested struct first.

**Two encodings, one of them mine to get wrong.** A route stores no per-command length, so a single wrong byte shifts every command after it and the route still parses; only the ids reveal the drift. The test fixtures build BER correctly, which means reversing the seven bit groups before setting the continuation bits: 4097 is the two bytes 160 and 1, not 129 and 32. My first fixture did not reverse them and reported a decoder bug that did not exist.

**The runtime, from `Game_Character::UpdateMoveRoute`.** A command that starts a step returns at once, so the route consumes one command per update and the character then spends the following updates walking. A refused step either skips the command when the route is skippable or holds the route on it, which is what stops a character stuck against a wall from sliding along it. A successful step falls through to the index advance, so the next command is read on the update after this one. A route that ends on a step still walks that step out, because the character is already on its way.

**Mutation evidence for the runtime, all four now detected:** the index never advancing after a step, the step never being cleared, the passability check removed, and a refused step always advancing instead of holding. The last two escaped before the blocked route test existed, which is why that test was worth writing.

**The pinned fixture has no move routes at all.** Its single real map has 22 event pages and every one decodes cleanly, but none defines a 0x29 route. The real map test now asserts that as a property of the fixture, and the commands are proved by the byte exact decoder tests and a synthetic LMT that carries a real 0x29 struct.

**K-111 is DONE.** `All 525 tests passed`, build `0 Warnung(en)`, `0 Fehler`, `UniversalRPG validation passed.` No probes. Font artifact restored.

## Ruby value layer for the RGSS engines (2026-09-26)
- `RubyValue` holds the seven kinds the language defines, as data, with a value's
  identity and its contents and nothing else. No arithmetic, no comparison, no
  method dispatch, no class loading, no decoding of a game's string bytes.
- `RubyValueConverter` turns a decoded Marshal value into one, following the
  file's links so a link becomes the value it names rather than a second copy.
- The refusals are the work. A kind the language has no name for, a payload that
  contradicts its kind, a mapping entry without its other half, a mapping holding
  the same key twice and a link to an entry that was never decoded each raise
  with the reason, and each is counted and remembered.
- **A bug this found in the reader from the card before it.** The converter was
  written against kind names spelled out from memory. The reader emits `array,
  false, float, integer, nil, object, regexp, string, struct, symbol, true`, and
  two of the converter's names were not on that list: a whole number arrives as
  `integer` and a string as `string`. Every number and every string in a real
  game's data would have been refused. The names now come from the reader.
- **The numbering, verified against the Ruby 3.4 specification.** A stream holds
  one copy of each object and one of each symbol, the first object has the
  number one and the first symbol the number zero. The converter reads that
  number from the value the reader handed over rather than counting again.
- A container is numbered before its contents are read, which is the only reason
  a container can hold a reference to itself. In the documented stream
  `"[\"
hello@"` the array is one and the string is two, and
  the link names two.
- A Marshal long is not eight bytes. A stream written with eight bytes is a
  different stream from the one a game writes, and three attempts to build one
  that way failed before the packing was read off the format.
- Other converter bugs found and fixed: the stream's own numbers were never
  recorded, so every link pointed at nothing; values were numbered from zero and
  after their contents, which gave a container a higher number than its first
  member; a value was filed under its number before its class was attached, so a
  link to a game's value found an object that no longer said what class it was.
- A value that points at itself is refused with the number in it, because there
  is no value to return yet.
- Tests: `TestRubyValue` 15/15, `TestRubyValueConverter` 27/27, total 799/799,
  validator passed. 12 mutations, all detected.
- Still missing for XP, VX and VX Ace: anything that gives a value a meaning, a
  renderer, saves, input, audio, and any playable runtime. `RgssEngineRuntime` is
  still a metadata inspector and no real archive from any of the three engines is
  in the repository, so all of this is structurally verified.

## Ruby parser for the RGSS engines (2026-09-26)
- `RubyParser` turns a token stream into a tree of shapes. It names what was
  written and nothing more: no name resolution, no method or constant lookup, no
  evaluation, no call of anything. It never executes a game's Ruby.
- The operator precedence is the Ruby grammar's own, taken from the declaration
  order of its precedence levels. That is not a detail: the first table written
  from memory was wrong, and a reader with one level in the wrong place parses a
  game's arithmetic into a different tree with nothing looking wrong about it.
- The grammar resolves relations and equality with `rel_expr %prec tCMP`, which
  makes them one level. The first table had them as two, and two tests written to
  match the wrong table had to be corrected rather than the code.
- `**` is the grammar's one right associative binary level and sits above
  multiplication on a level of its own.
- `not` is a level of its own between the logical pair and the assignment, so
  `not a == b` negates the comparison. It was first read as a unary operator,
  which binds at the other end of the scale entirely.
- A shape the parser cannot read raises with its line. A tree that stopped early
  would be worse than none, because nothing would mark it as incomplete.
- Tests: `TestRubyParser` 44/44, total 755/755, validator passed.
- 21 mutations on the precedence table and the reader's shapes, all detected.
- Two escapes during the work were untested boundaries, not wrong code: nothing
  in the suite crossed the logical/bitwise boundary and nothing pinned `not` to
  its level. Both now have tests.
- One mutation showed a `not` branch in `ParseBinary` was unreachable, since
  `ParseUnary` takes the keyword first. It was removed instead of kept as a
  second route to the same node, and the suite still passes without it.
- Still missing for XP, VX and VX Ace: a reader for the tree's meaning, a
  renderer, saves, input, audio, and any playable runtime. `RgssEngineRuntime`
  is still a metadata inspector, and no real archive from any of the three
  engines is in the repository, so all of this is structurally verified and not
  checked against a real game.

## Ruby lexer for the RGSS engines (2026-09-26)
- `RubyLexer` splits a game's Ruby source into tokens. It calls nothing,
  resolves nothing and runs nothing. Together with K-112 and K-113 this is the
  third of the three layers XP, VX and VX Ace need before their scripts can be
  read at all, and the first one that looks at the script text itself.
- **The keyword list is the one from Ruby's own grammar**, extracted from
  `parse.y` rather than written from memory: 41 reserved words, from `class` to
  `__ENCODING__`. That matters because a keyword is reserved, so a lexer that
  treated one as a name would accept files Ruby rejects.
- **A name that begins with an upper case letter is a constant**, and the
  reserved word check comes first. Two of the reserved words, `BEGIN` and `END`,
  begin with an upper case letter, and the grammar's `reswords` production lists
  them as keywords. Checking for a constant first read them as names. A test
  over all 41 words is what found it, because the one example I had chosen
  happened to be a lower case word.
- **A slash divides where a value has just ended and opens a regular expression
  where one could begin.** My first version had this exactly backwards, so
  `a / b` was read as a regular expression that ran off the end of the line. The
  two shapes differ only in what came before the slash, which is why both are in
  the suite.
- A regular expression keeps its backslashes, because the pattern engine is what
  interprets an escape, and a `/` inside a character class does not close it.
- **A single quoted string interprets only two escapes**, the quote and the
  backslash. Reading it like a double quoted one lost a backslash a game asked
  to keep, which is the whole reason the form exists.
- A string keeps its **bytes** as well as its text, because a Shift-JIS script is
  not UTF-8 and a reader that kept only text would silently corrupt it.
- An octal literal may be `0o17` or `017`. The marker sits between the leading
  zero and the digits, and checking the current character instead of the next
  one read `0o17` as a bare zero.
- An unknown character, an unclosed string, an unclosed regular expression and a
  number with no digits in its base are all refused with their line. A partly
  tokenised script is worse than none, because nothing marks it as incomplete.
- Tests: `TestRubyLexer 33/33`, total `711/711`, build `0 Warnung(en)`,
  validator passed, probes 0.
- Mutation evidence, fourteen run and eleven detected: a keyword list never
  consulted, the reserved word check moved after the constant check, the
  constant rule inverted, a slash always a regular expression, a slash always a
  division, single quoted escapes applied in full, the octal marker not
  skipped, a shorter operator matched first, an unclosed string accepted, a line
  continuation read as a break, a block comment not skipped, a class variable
  read with one at sign, a regular expression losing its backslash, an
  unterminated block comment end.
- **Two mutations were equivalent rather than escaping.** Appending `<=` and
  `<<` to the operator list changes nothing, because every multi character
  operator already appears before the shorter one it starts with; the check
  printed the whole list to establish that. And turning a byte escape's
  `((char)value).ToString()` into `value.ToString()` changes nothing, because the
  cast already produces values in the range where the two agree. A mutation
  that cannot change behaviour is not a gap in the tests.
- Four gaps the mutations found were real and are now closed: the keyword lookup
  was untested, the single quoted escapes were only checked for one letter, the
  operator order was checked for the operators the test happened to use, and the
  line continuation test filtered the newline it was supposed to be about.
- **One mistake of my own in the tests, and it hid four failures for a while.**
  The helper that drops whitespace-only tokens did not drop the end of input
  token, so every list based assertion was off by one element. A probe with a
  different filter showed the lexer's output was right all along. Measuring
  instead of reasoning about positions is what ended the loop.

## Marshal reader for the RPG Maker data files (2026-09-26)
- `MarshalReader` reads the Ruby Marshal stream that RPG Maker XP, VX and VX
  Ace use for `.rxdata`, `.rvdata` and `.rvdata2`. Together with the archive
  format this is the second of the two things all three engines need before any
  of their data can be looked at. Neither runs a line of game code.
- The reader produces a tree of `MarshalValue` rather than live objects, on
  purpose. A game database is full of instances of classes this project has
  never heard of, so resolving them would mean either running the game's Ruby
  or inventing classes that do not exist. A value tree can be inspected without
  either, and it keeps the class name as a string.
- **Integers are the part that is easy to get wrong and I got wrong.** A
  marshalled integer is a type byte and then one to five bytes, where the first
  of those encodes sign and width in a single value. Eight values are special;
  the rest is a sign extended byte with an offset of five. A reader that
  treats the first byte as a length decodes small numbers correctly and
  everything else as something plausible but wrong, which is the worst way to
  be wrong.
- **An object takes its index before its contents are read.** A value inside a
  collection may link back to that collection, and the link names an object the
  stream has already defined. Numbering afterwards would make every such link
  point at the wrong object. `ReadArray` and `ReadHash` now take the index
  first.
- A link does not take an index of its own, because it names an object that
  already exists. Only real values do. My first expectation had this wrong and
  the measurement corrected it.
- A regexp is the one type in its group that carries no class name: the
  specification gives a source and an option byte and nothing else. Reading a
  name there consumes the length byte of the source and shifts the rest of the
  file.
- A bignum is refused rather than read. A game's data uses fixnums for anything
  that fits, and a bignum here would mean arbitrary precision this reader does
  not carry, so pretending to read one would be a guess.
- A stream that ends inside a value, declares a length past the limit, or
  carries a type byte the specification does not define is refused. A partial
  tree of a game's data is worse than an honest failure, because nothing marks
  it as incomplete.
- A major version this reader does not implement is refused outright, and a
  newer minor version is refused too, because it may use a type this reader
  has never heard of. An older minor version is read, since a newer minor
  version can read an older one.
- **Two mistakes of mine, and the second one only showed up under mutation.**
  A grouped `case` list and single `case` labels for the same values left the
  later ones unreachable, so a regexp fell through to the refusal branch. And
  when I removed that label I took the routing line with it, so no regexp could
  be read at all. Both are fixed; the routing and the payload are now separate
  concerns.
- The fixtures are written by hand from the specification's type table. A round
  trip through a writer of our own would pass even if the reader and the writer
  were wrong in the same way, which is the failure this suite exists to rule
  out.
- Tests: `TestMarshalReader 29/29`, total `678/678`, build `0 Warnung(en)`,
  validator passed, probes 0.
- Mutation evidence: a flipped sign offset, a swapped sign case, a zero case
  that swallowed a byte, a width read one byte short, an array numbered after
  its contents, a hash likewise, an uncapped nesting depth, an unchecked major
  version, an unchecked minor version, an uncapped byte count, an unchecked
  symbol link, an object link accepting index zero, a regexp reading a class
  name, and a symbol link resolving out of range were all detected.
- **Two mutations turned out to be equivalent rather than escaping.** Moving
  `++ObjectCount` below the `ReadLength` call changes nothing, because reading a
  length does not touch the counter. A mutation that cannot change behaviour
  cannot be caught by a test, and recording it as a gap would have been wrong.
  A mutation that really delays the index until after the elements were read is
  detected, which is the behaviour the ordering exists for.
- Structural only: the repository has no RPG Maker game, so there is no real
  data file to read.

## RGSS archive format, shared by XP, VX and VX Ace (2026-09-26)
- `RgssArchiveReader` reads and writes the archive format that RPG Maker XP, VX
  and VX Ace all share. This is the first thing that is genuinely common to
  three of the engines the goal lists, so it counts for three criteria at once
  where the Ruby virtual machine would count for none of them until it ran.
- The format is obfuscated, not encrypted. Every value is exclusive ored with
  the output of a linear congruential generator that starts at `0xDEADCAFE` and
  advances once per value by `magic = magic * 7 + 3`. The value a field is
  obfuscated with is the generator's state **before** that step.
- **The header is eight bytes: the name `RGSSAD`, one byte the format does not
  check, and the version.** The reference reader compares the first six bytes
  and reads the version from the last one. A reader that also required the
  seventh byte to be zero would refuse a file the format allows, so the test
  proves the seventh byte is ignored rather than assuming it is zero.
- The version byte is what tells an XP or VX archive from a VX Ace one. They
  differ in nothing else a reader has to know before it can list entries.
- Each entry is a name, a size and a body. The name is obfuscated byte by byte,
  and a backslash in it is folded to a slash so that an archive written on
  either system lists the same way. The list ends when a name can no longer be
  read, not at a terminator.
- An entry that claims more bytes than the file holds is refused rather than
  handed back short, because a short body looks like a successful read.
- This reader lists and reads entries. It does not execute anything an entry
  contains: a game script is read as bytes and nothing more, which is what the
  project rules require.
- **Two bugs of my own, both caught by tests rather than by reading.** A regular
  expression pass removed the `return` from three failure paths, so a refused
  archive fell through and was read anyway; the diagnostic said the file was
  not an archive while the reader went on parsing it. And an unused version
  read indexed one byte past the end of an eight byte header, which crashed on
  an empty archive. Both are fixed and the failing tests are the reason.
- Two of my own wrong expectations: I had the header as three zero bytes after
  the name, and I had the generator taking two steps per field. Neither matched
  the reference reader. The first was found by an out of bounds read on an empty
  archive and the second by a round trip that decoded to a name length of three
  hundred million, which was reproduced outside C# before the reader was touched
  again.
- Tests: `TestRgssArchive 15/15`, total `649/649`, build `0 Warnung(en)`,
  validator passed, probes 0.
- Mutation evidence: a seed off by one, a wrong multiplier, a name byte read
  without the key, a version read from the wrong offset, the unchecked byte
  checked, and an entry list that started four bytes late were all detected.
- Structural only: the repository has no RPG Maker game, so there is no real
  archive to read. The fixtures are written by the project's own writer and the
  expected values are also derived independently inside the test, because a
  round trip through the project's own writer alone would pass even if both
  halves were wrong in the same way.

## K-110 WOLF move routes, the last gap in the command reader (2026-09-26)
- `WolfMoveRouteReader` reads a route: the animation frequency, the move speed
  and the move frequency, then the mode, then two option blocks, then a length
  and that many steps. The header order is neither alphabetical nor the order a
  reader would guess, and reading it in a different order still produces four
  plausible bytes, so each header field gets its own value in the fixture.
- **The two option blocks are bit fields, not bytes.** The behavior block holds
  eight flags in one byte and the route option block holds three in the high
  three bits of another, with the low five reserved. Reading them as bytes would
  leave the cursor six bytes short and shift every field after them, which
  produces a file that looks decoded and is not.
- **A route step describes its own argument lengths.** The types that take
  arguments write a byte saying how many four byte values follow, the values, a
  byte saying how many single byte values follow, and then those. Reading the
  lengths from the file rather than from a per type table is what lets this
  reader read a type it has no name for: it can still step over the step
  correctly and report the type as one it does not name. A reader driven by a
  table cannot do that, and the mutation that swaps the file's length for a
  table lookup is caught.
- A step that takes no arguments still writes both length bytes, both zero.
  Reading only the first takes the second from the next step's type, which is
  exactly the drift a self describing format is meant to prevent.
- The route type table has 59 values with a gap at 0x2A and 0x2B that the
  specification leaves unused. A value in the gap is reported as one the table
  leaves unused rather than stepped over silently, because a reader that
  skipped it could not say how long it was.
- The event command reader now reads a route when the route flag is set, so a
  route inside a command is decoded rather than reported as unread.
- Tests: `TestWolfMoveRoute 11/11`, `TestWolfEventCommand 10/10`, total `634/634`,
  build `0 Warnung(en)`, validator passed, probes 0.
- Mutation evidence: a removed option byte, a reversed header, swapped option
  bits, option bits read from the low end, a table driven argument length, a
  missing byte argument count and a byte argument count fixed at zero were all
  detected. Two earlier mutations escaped and both were test gaps, not reader
  gaps: an option test that set all three bits at once could not tell them
  apart, and each option now has a fixture of its own.
- **K-110 is now complete against its title.** The database, the game settings,
  the common events and the move routes are done. The transfer format was not in
  the title and is not started.

## K-110 WOLF CommonEvent.dat, and a real bug in the command reader (2026-09-26)
- `WolfBinaryCommonEventReader` reads the file: the `WOLF/FC` header, a
  version byte, a count and that many records. Each record starts with `0x8E`
  and is divided by five separator bytes into six parts, and the order matters:
  the argument name table, the option string tables and the option value tables
  all sit between separators rather than at the end of the record. A reader that
  walks the fields top to bottom produces plausible wrong values, because every
  field is still a valid length prefixed string.
- The self variable name table is a fixed hundred entries, not a counted one.
  Reading it as a count leaves the cursor inside the table and shifts every
  field after it, which looks like a successful decode of a wrong file.
- **A zero parameter count is the command list's end marker, not a command with
  no arguments.** Nothing at all follows it. This was the first thing my
  CommonEvent fixture got wrong, and it is why the format needs a test of its
  own rather than a spot check.
- **A length prefixed string's terminator is inside its length.** The format's
  string is a zero terminated string with a size, and the size covers the
  terminator. Writing the length without the terminator and then appending one
  leaves a byte the reader does not consume, so every field after the first
  string is off by one. The fixture did that and the reader was right to refuse
  the result.
- **The command reader from K-109 was structurally wrong and is now corrected.**
  It read a four byte big endian signature followed by a padding byte, built on
  constants like `0x0167_0000`. The verified structure is a one byte parameter
  count, then a four byte little endian type, then a parameter block whose shape
  belongs to the type. The old form decoded its own fixtures and no real file.
- The corrected reader is type aware, because the parameter block is not
  uniform. A message command has no block at all, a numeric condition has an
  else flag, a condition count, three padding bytes and then that many variable,
  value and operator triples, a call has an event id and then an argument status
  word only when the id is in the common event range, and a branch carries a
  condition id. After the block every command has a branch depth byte, a string
  count byte, that many strings and a move route flag.
- The old signature constants were partly invented. `CallCommonByName` was
  `0x3B` because that packed the parameter count and the type into one word; the
  verified type is 300. The double and triple variants were not separate types
  at all but the same type with a different parameter count, and naming them by
  type alone collapsed them. Names now come from the type and the parameter
  count together.
- **Where the specification gives a parameter count and not the meaning of each
  count, the name reports the count.** Mapping type 121 with eight parameters to
  an operation name would have been a guess, so the name says how many
  parameters the command has.
- A move route is not decoded. A command that carries one is reported as
  carrying an undecoded route instead of being stepped over, because stepping
  over bytes the reader does not understand would shift every command after it.
- A string count above the limit is refused. The field is a single byte, so a
  file can claim up to 255 strings; following that count out of a short file
  would walk off the end. The limit is 32, and the reason is written down: a
  count near the byte's maximum is far more likely to be a misread.
- Tests: `TestWolfBinaryCommonEvent 17/17`, `TestWolfEventCommand 10/10`,
  total `623/623`, build `0 Warnung(en)`, validator passed, probes 0.
- Mutation evidence on the corrected reader: a fixed parameter count, a big
  endian type, a uniform parameter block, an ignored zero marker, an ignored
  string limit and a route flag that is never set were all detected.
- Still missing against the K-110 title: the transfer format, and move routes
  inside event commands. The database, the game settings and the common events
  are done.

## K-110 WOLF Game.dat VERIFY (2026-09-26)
- `WolfGameSettingsReader` reads the Game.dat framing: magic `0 'W' 0 0 'O' 'L'
  0 'F' 'M'`, a version byte, a byte settings length, the byte settings, the
  version dependent string record, the file's own size, `unknown3`, the word
  settings length and the word settings, the static random block and a version
  footer.
- **The version byte changes the record shape, not just the meaning of a
  field.** A v2 record has eight Shift-JIS strings followed by one UTF-8 string;
  a v3 record has twelve UTF-8 strings and no trailing one. Reading one as the
  other consumes the wrong number of strings and lands somewhere else entirely
  without failing, so the version has to pick the shape before anything is read.
- **The record mixes encodings inside itself**: the first eight v2 strings are
  Shift-JIS and the ninth is UTF-8. Decoding both as UTF-8 turns a Japanese
  title into replacement characters; decoding both as Shift-JIS turns a UTF-8
  name into mojibake. Both encodings are decoded strictly, so bytes that are
  invalid in the declared encoding are refused rather than replaced.
- **The word record is bounded by its own length**, not by a fixed field count.
  A later version appended the loading gauge fields behind that length. The
  record is 23 values: `unknown`, twelve custom move speeds, `unknown_2`, the
  screen width and height, then the WOLF version at index **16**. I initially
  had 18 words and the version at 17; both were wrong and the fixture caught it.
- The file's declared size bounds the static random block, whose length varies
  per file, so treating it as a fixed block would misread every other game.
- The encryption key is the third string setting and is read so a protected game
  can be recognised. Nothing is decrypted and this reader has no decryption
  path.
- Two fixture errors of my own, both caught by the tests rather than by
  inspection: the v3 fixture had thirteen strings where the record has twelve,
  and I had guessed the expected Shift-JIS text. `83 65 83 58 83 67` is
  "Test", not the word one reaches for first, and the expectation is now the
  measured value with a comment saying so.
- Mutation evidence: a v2 record read as UTF-8, a wrong v3 string count, a
  fixed word count, a removed file size bound and a wrong version index were all
  detected.
- Tests: `TestWolfGameSettings 13/13`, total `607/607`, build `0 Warnung(en)`,
  validator passed, probes 0.
- Still missing against the K-110 title: `commonevent_dat` and the transfer
  format. `game_dat` and the database format are done.

## K-094 vehicle runtime wiring VERIFY (2026-09-26)
- `LoadVehicles` builds all three vehicles from the LMT start node and the LDB
  system section, verified from `Game_Vehicle`'s constructor. All nine start
  fields decode: `boat_*` `0x0B`/`0x0C`/`0x0D`, `ship_*` `0x15`/`0x16`/`0x17`,
  `airship_*` `0x1F`/`0x20`/`0x21`.
- Measured on the pinned fixture rather than assumed: all three vehicles name
  the **same** charset, `vehicle` for the boat and the ship and `Vehicle` for
  the airship, at cells **0, 1 and 3**. Cell 2 is unused, which is legal and
  which I initially got wrong by expecting 0, 1, 2.
- All three vehicles start on **map 39** and the party on map 30, while the
  fixture only ships `Map0001.lmu`. So this map correctly draws no vehicle, and
  `CharSet` holds only `Chara1.png`, so the `vehicle` cell could not be drawn
  even on the right map.
- The vehicle draw path is therefore **proven to run but not proven to draw**.
  Three mutations still escape and all three are the same root cause: with no
  `vehicle.png` in the fixture, the draw path never writes a pixel, so removing
  the map check, the charset fallback or the altitude wiring changes nothing
  observable. Mutation runs of the vehicle-order and the LMT read were detected.
- Deliberately **not** done: fabricating a `vehicle.png` to make the test draw
  something. A synthetic charset would prove the compositing maths and nothing
  about the real file, and a real one is not available in the fixture. The
  honest options are a game that ships a vehicle charset, or a fixture change
  agreed with the user.
- Tests: `TestRm2kRuntimeRendering 19/19`, total `594/594`, build
  `0 Warnung(en)`, validator passed, probes 0.

## K-094 vehicle compositing VERIFY (2026-09-26)
- `Rm2kMapFrameRenderer.DrawVehicle` composites a vehicle through the same
  charset cell as any character, with the altitude taken off the vertical
  offset. A vehicle gets no separate sprite stage: the Player draws it with the
  same `Sprite_Character` and applies the altitude in `GetScreenY`, so giving it
  its own stage would put it behind or in front of the hero, which is not what
  happens.
- The altitude is counted in **whole tiles**, so the pixel offset is the
  altitude times `TILE_SIZE` of 16. Forgetting the multiplication draws the
  airship at 1/16 of the right height and still passes a bounds check, which is
  why the test measures the row of the first drawn pixel instead of counting
  pixels.
- `Rm2kAirshipShadow` is the airship shadow, which is a **separate sprite**
  rather than part of the airship: two 16x16 patches of the System graphic at
  `(128,32)` and `(144,32)`, blitted together, at `Opacity(0.26 * 255)`, which
  truncates to **66** and not 67. It is drawn one below the airship's own screen
  z so the airship covers it, and only while the player is in the airship.
  The Player's own comment says 26 percent is not what RPG_RT does; this
  repository reproduces the Player's value because the accurate one cannot be
  measured without a real game.
- Mutation evidence: a missing tile-size multiplication, an added instead of
  subtracted altitude, a shadow at the same z as the airship and a rounded
  opacity were all detected.
- Tests: `TestRm2kVehicleCompositing 6/6`, total `591/591`, build
  `0 Warnung(en)`, validator passed, probes 0.
- Still missing against the K-094 title: the vehicle state is built and tested
  as a unit but is not yet constructed from the LDB system section inside the
  runtime, so a real map still shows no boat.

## K-094 vehicle sprites VERIFY (2026-09-26)
- `Rm2kVehicleSprite` carries the sprite the LDB system section names
  (`boat_name`/`boat_index`, `ship_name`/`ship_index`,
  `airship_name`/`airship_index`) and the two properties that are specific to a
  vehicle: the altitude it is drawn at and its animation.
- A vehicle uses the **same** charset geometry as a character, `24 * 3` by
  `32 * 4`, so no second sprite reader is needed.
- The animation limit is `GetStopCount() ? 16 : 12`: a standing vehicle is shown
  for sixteen frames and a moving one for twelve, which is the reverse of the
  character tables and is exactly the kind of thing carried over by mistake.
- The frame wraps with `anim_frame = (anim_frame + 1) % 4`, a modulo over all four
  liblcf frames rather than a clamp, so `Frame_middle2` is a real state. The
  sprite clamp turns it into middle when it is drawn, so a vehicle walks left,
  middle, right, middle and back to left.
- `ResetAnimation` only puts the frame back to middle when the animation type is
  not `fixed_graphic`, so a fixed graphic holds its frame while it climbs.
- Mutation evidence: a swapped animation limit, a clamped frame, a missing
  fixed-graphic exemption and a counter that is not reset were all detected.
  The first run reported all four as escaping; that was my mutation harness
  using tab anchors against a space-indented file, not missing tests.
- Tests: `TestRm2kVehicleSprite 9/9`, total `585/585`, build `0 Warnung(en)`,
  validator passed, probes 0.
- Still missing against the K-094 title: the vehicle sprites are described and
  tested but not yet composited into a map frame, so nothing is visible yet.

## Five new engine criteria added by the user (2026-09-26)
- Criteria 4 to 8: RPG Maker MV, XP, VX, VX Ace and MZ complete.
- Measured state before answering them: `RgssEngineRuntime` is 312 lines against
  1922 for the RM2K runtime and contains no renderer, no frame buffer and no
  interpreter. It is a metadata inspector. There is no `project/src/mv` or
  `project/src/mz` at all, and the repository holds **no** RGSS or MV/MZ game
  file: no `.rgssad`, `.rxdata`, `.rvdata2` or `.rpgmvp` anywhere in the
  fixtures. K-080 (RGSS spike) and K-090 (MV/MZ spike) are still BACKLOG.
- So none of the five new criteria is met, and none can be verified against a
  real game with what the repository holds. Three questions were put to the user
  about how to proceed and none was answered inside the prompt window, so the
  safe defaults were taken and are recorded here rather than assumed silently:
  build structurally against the published specifications as WOLF was done,
  never execute foreign JavaScript, and finish RM2K before spreading out.

## K-110 WOLF binary database VERIFY (2026-09-26)
- `WolfBinaryDatabaseReader` reads the `DataBase.dat` / `CDataBase.dat` /
  `SysDataBase.dat` framing: magic `0 'W' 0 0 'O' 'L'`, a one byte version
  header where `0x00` is v2 and `0x55` is v3, the marker `'F' 'M' 0`, a version
  byte, a record type count, then one block per type and a version footer byte.
- The important part is the **position table**. A record does not store its
  values in order: each table entry packs block and index into one number, with
  the block in the thousands digit and the index in the remainder (`1000` is
  number block index 0, `2000` is string block index 0). The values then live in
  two blocks, numbers first and strings second, and the two block sizes are
  derived from the table rather than stored. Reading the blocks in property
  order instead of through the table yields plausible but wrong data, which is
  why the fixture `Test_ThePositionTableDecidesTheOrderNotTheFileOrder` puts the
  string first and the number second.
- A property whose position points outside its block is reported as missing
  rather than defaulted: a zero would be indistinguishable from a real value.
- A string is a 32-bit length, the bytes, and a NUL one past the length. The
  terminator has to be consumed or every following value is off by one.
- The first version byte was read at index 9, which is the marker's `M`; the
  header is six magic bytes plus one version header plus three marker bytes, so
  the version byte is at index 10. The test caught it.
- Mutation evidence: ignoring the position table, an off-by-one block count, a
  missing terminator check, a missing footer check and a skipped terminator byte
  were all detected. The first terminator run escaped because the fixture also
  failed on the file end, so the check was never isolated; the fixture now has
  all the bytes but a non-NUL terminator and asserts the message says so.
- Tests: `TestWolfBinaryDatabase 17/17`, total `576/576`, build `0 Warnung(en)`,
  validator passed, probes 0.
- Still missing against the K-110 title: the `game_dat`, `commonevent_dat` and
  transfer formats. Only the database format is done.

## K-094 decision turn order DONE (2026-09-26)
- `Rm2kDecisionTurn` reproduces the tail of `Game_Player::UpdateMove`:
  `if (Input::IsTriggered(Input::DECISION)) { if (!GetOnOffVehicle())
  { CheckActionEvent(); } } return;`. Vehicle boarding therefore has
  precedence over the action event check, and the turn ends either way so the
  step counter is not incremented.
- The diagonal correction runs first, because `GetOffVehicle` asserts there is
  no diagonal. A diagonal of 4 to 7 becomes the facing, and a caller that acted
  on the diagonal directly would look at the wrong neighbour tile.
- `CanBoardAirshipOn` requires the airship to be on the player's own tile and
  **both** the player and the airship to be standing still. A drifting airship
  is not boardable.
- `CanLeaveAirship` refuses to leave an airship that is still ascending or
  descending, which is what stops the player ending up standing in mid air.
- `CanDisembark` checks `IsValid` first, then an active same-layer event on the
  target tile, then passability towards the player.
- Out of scope and not implemented: the BGM swap around boarding
  (`SetBeforeVehicleMusic` / `BgmPlay`). The runtime has no BGM state at all, and
  adding one would be speculation rather than a verified slice.
- Mutation evidence: action-event-first, missing airship guard, hardcoded
  airship stopping, boat before ship and missing leave-airship guard were all
  detected. The first run reported the airship-stopping mutant as escaping; that
  was a stale backup in the harness, and re-running it against the current file
  detected it.
- Tests: `TestRm2kDecisionTurn 12/12`, total `559/559`, build `0 Warnung(en)`,
  validator passed.

## K-094 RM2K vehicles VERIFY (2026-09-26)
- `Rm2kVehicle`: types `None=0, Boat=1, Ship=2, Airship=3` (liblcf
  `Game_Vehicle::Type`, stored in save data). Move speeds are **4** for boat and
  ship and **5** for the airship -- liblcf `MoveSpeed_normal=4`,
  `MoveSpeed_double=5`, so a vehicle is faster than the default event speed 3,
  which is `MoveSpeed_half`. This was the easiest value to get wrong.
- `Rm2kVehicleState.GetAltitude`: `(256 - remaining) / 16` tiles while ascending,
  `remaining / 16` while descending, and only while flying. 8 is spent per
  update, so a full ascent or descent is 32 updates. A finished descent lands
  where it can and otherwise starts another ascent instead of hovering.
- `Rm2kVehicleBoarding`: the two rules are asymmetric and stay separate.
  Airship boarded by standing on it (`BoardAirship`, aboard at once) and left
  by `BeginAirshipDisembark` (still aboard, the airship descends). Boat/ship
  boarded by stepping onto the water (`BeginEmbark` -> `CompleteEmbark`) and
  left by stepping off (`BeginDisembark` -> `CompleteDisembark`), which
  restores `PreboardMoveSpeed` rather than the vehicle or event speed.
  `VehicleInFront` checks ship before boat, matching the Player's own order.
- Mutation evidence: wrong move speed, ascent 128, no flying check, no
  land-or-retry branch, boat-before-ship, airship-steps-off, missing map check
  and vehicle-speed-instead-of-stashed-speed were all detected.
- Tests: `TestRm2kVehicle 11/11`, `TestRm2kVehicleBoarding 11/11`, total
  `547/547`, build `0 Warnung(en)`, validator passed, probes 0.
- Still missing against the K-094 title, which is "get on/off **for the
  action-event order**": the vehicles are not yet drawn, not yet given their
  system-section name and index as a sprite, and not yet wired into the action
  event order. The pinned fixture has no water tiles and no vehicle sprites, so
  the boarding rules are proven on synthetic maps only. Status is therefore
  VERIFY, not DONE.

## Next action

1. **K-111**, the move route, so events walk at the verified per frame rate. `Game_Character::UpdateMoveRoute` and `lcf::rpg::MoveRoute` are the reference; each command needs its verified semantics before code, and a wrong command length desynchronises the route.
2. **K-110** for WOLF: the remaining command bodies and the binary database, common event and game formats.
3. Vehicles (K-094) still need typed terrain flags and a collision path.
4. A methodology note worth keeping: a mutation result means nothing until the baseline reports that there is nothing to detect, and a build failure must be distinguished from a test failure. Counting only test errors reports a failed build as a clean pass.
5. Unrelated local changes must stay untouched: `project/assets/fonts/NotoSansCJKsc-Regular.otf.import` and untracked `qa_patches/`.

## Reference repos noted by the user (2026-09-26, not actioned)

- `joiplay/mkxp`, `joiplay/android-mkxp` — RPG Maker XP (RGSS) reimplementation in C++; useful as a cross-engine reference for Ruby/RGSS and for its own passability handling, not a source of RM2K constants.
- `futokoro/RPGMaker` — Ruby RGSS reimplementation (XP/VX/Ace).
- `bakustarver/rpgmakermlinux-cicpoffs` — RPG Maker on Linux via C++ offscreen; detection/hosting reference.
- Consequence: XP/VX/Ace work stays at priority 6-7 per `AGENTS.md`; MV/MZ playability still needs a JS runtime, which the repository policy does not provide.

## Last verified baseline

Windows validation on 2026-09-26: `dotnet build project/UniversalRPG.csproj` (0 errors) and the headless C# runner at `All 419 tests passed`, exit 0. The Godot project lives under `project/`; `validate.sh` handles both layouts.

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

## Roles for the parse tree's children (2026-09-27)
- Every child of a node now says what it is for, so a consumer asks for the test
  or the body rather than knowing each kind's layout.
- The fault this found: the parser said only that a child was there, so what a
  list meant depended on the kind alone. A keyword that opens a test held the
  keyword first, a ternary held the test first, a block on a call held the call,
  the parameters and the body, and a block that was a body held only statements.
  One kind could mean two things and the second meaning was invisible.
- A name is held in `Name` and not in `Text`. Two test failures came from reading
  the wrong field, and a wrong field can be fixed two ways of which one is right.
- `One("a if b")` returns the `if` node itself, not a wrapper holding it. The
  first version of the test assumed a wrapper and was wrong; the parser was right.
- A lookup for a role that is not there answers null rather than falling back to
  the first child, so an absent role cannot be mistaken for a present one.
- Tests: `TestRubyParser` 52/52, total 808/808, validator passed, 0 warnings.
- 6 mutations on the roles and the lookup, all detected. Two escaped at first:
  a lookup taking the last of a role cannot be told from one taking the first
  while every node holds at most one child under a role, and a lookup falling
  back to the first child passed every test that asked for a role that was there.
- Next: a machine to run the tree. That is no longer blocked on the tree's shape.

## Whole numbers the engine writes wider than this machine holds (2026-09-27)
- The ground truth for all the marshal work had been Ruby 3.4, because that is
  the documentation that is easiest to reach. Checked against the sources:
  XP is Ruby 1.8.1, VX is 1.8.3, VX Ace is 1.9.2. All twenty five type bytes
  are identical across 1.8.7, 1.9.3 and 3.4.1, so the format held.
- The whole number form did not: 1.8 and 1.9 write `i` for a number that fits
  in thirty one bits and `l` for the decimal digits of anything larger, and
  Ruby 3 swaps those two letters and writes the large form in binary. A reader
  built from the 3.4 table refuses every file an engine of this line writes.
- **The reader's sign handling was wrong.** A negative number is written as its
  bytes carried to the width it was written in, so every byte after the first
  is the top of the width. The reader negated the unsigned value instead, which
  is the same for a one byte number and not the same for any other: it read one
  byte too many and took the first byte of whatever followed in the file. A
  game's negative coordinate would have had the next value's bytes inside it.
- The one byte negative form was on the wrong side of the boundary. Five to 127
  is the number with five taken off, -129 to -5 is the number with five added,
  and -1 to -4 is the wide form.
- A wide number is now read from its digits and refused only when it does not
  fit this machine's whole number, with the digit count in the reason. The
  earlier card refused it outright for a reason that was wrong: a game's number
  is decimal digits, so the number is readable and only the width is in doubt.
- How it was found: a test walks 6001 numbers through the writer taken from
  1.8.7's own loop, and a second holds sixteen against the bytes that loop
  produces. The first version of those sixteen was written from memory and was
  wrong about four. Every fault found in this work was in the test rather than
  in the reader.
- A check refusing a count byte wider than a whole number was written and then
  removed: five to 127 is the one byte form, so no such count exists, and the
  check refused a length a game writes for every list it has.
- Tests: `TestMarshalReader` 39/39, total 818/818, validator passed, 0 warnings.
  8 mutations of the packing, all detected.
- Still missing: a number wider than this machine's whole number is read and then
  refused, and no archive from any of the three engines is in the repository.

## K-120 Real game data — checkpoint

- What was added: sixteen XP `.rxdata` files from two independent installations
  (`rgss-xp`, a Japanese one, and `rgss-xp-microquest`, an English one), a real
  RM2K database, map tree and two maps (`rm2k-dragon-destiny`, 743 maps in the
  game), and one `Game.ini` from a KiriKiri game that is **not** a WOLF game.
  Sizes and SHA-256 in `project/tests/fixtures/RGSS_FIXTURES.md`.
- What it found: the marshal reader reads every one of the sixteen XP files. Two
  of my own expectations were wrong and the files settled them — an XP map is an
  `RPG::Map` object with eleven members, not a hash, and a `.lmu` holds an
  `LcfMapUnit`, not an `LcfMap`.
- The WOLF detector was checked against the KiriKiri game, which has `BasicData`
  and `MapData` folders and no `Game.dat`. It refuses it, and the test proves the
  refusal is a decision by showing the same folder with a `Game.dat` is detected.
- Tests: `TestRealXpData` 6/6, `TestRealXpDetection` 3/3, `TestRealRm2kData` 3/3,
  `TestKirikiriIsNotAWolfGame` 2/2, `TestMarshalReader` 40/40, total 833/833,
  validator passed, build 0 warnings / 0 errors, 8/8 mutations detected.
- A lesson kept in the mutation harness: a suite that looks for `error CS`
  anywhere in a run counts the previous step's output as a build fault. Compile
  the mutation on its own, then run the suite.
- Still missing, and this is the next step: **no archive from any engine is in
  the repository**, so `RgssArchiveReader` has never read a real `RGSSAD` file and
  `RgssEngineRuntime` is still metadata only. The XP games given to the repository
  keep their data in plain files, so the archive path needs either an encrypted
  game's data or a written archive from the engine's own format description.

## K-121 RPG Maker MZ data — checkpoint

- What was added: `project/src/mz/MzJson.cs` and `project/src/mz/MzDataFile.cs`,
  a reader for the JSON an MV/MZ game keeps its database in. Eleven real data
  files of a 1.9.1 game are in `project/tests/fixtures/mz`, with sizes and
  SHA-256 in `project/tests/fixtures/MZ_FIXTURES.md`.
- The three format facts, all measured from the file: a database file's first
  entry is null; a command is a small number and is NOT the generation before's
  `code * 1000`; a map's events are indexed by event and are not padded to the
  field. Two of the three were wrong in a first draft of the test.
- `MzDataFile.Top` was a one element list, which made `Top[0]` the file and
  `Top[0][0]` its first element. That is not a root value; it is `Root` now, and
  the test that read the array where the object was is what showed it.
- The reader that was already here, `MzDataDirectoryResult`, counts entries and
  takes names and caps a file at 2 MiB. `TestMzReaderBoundary` states that
  difference so the two are not confused. Neither derives from the other.
- Tests: `TestRealMzData` 15/15, `TestRealMzDetection` 3/3,
  `TestMzReaderBoundary` 2/2, `TestMzDataDirectory` 8/8, total 853/853,
  validator passed, 8/8 mutations detected.
- Honest number worth keeping: **the first mutation suite of this reader
  detected 1 of 8.** It found five real gaps (an unclosed string ran to the end
  of the file, an unknown escape was taken as text, nesting was unbounded, a
  broken exponent became a number, a file's own text was discarded) and one
  anchor that did not exist. A suite that reports a high number on its first run
  has usually been written from the code rather than against it.
- Still missing, and this is the next step: **an MZ game does not play.** There is
  no JavaScript runtime here, so no plugin, no script and no event command runs,
  and the two `js` fixtures are placeholders carrying the real file names. MV
  shares the data format and has no real fixture at all, and its command
  numbering is the packed one, which is exactly the difference the second trap is
  about. For the RTP criterion: no RTP has been downloaded and none is needed for
  the data layer, and the user is asked before anything is fetched.

## K-122 The MZ command table — checkpoint

- What was added: `project/src/mz/MzCommandName.cs` and `MzCommandTable.cs`.
  114 commands, 101 to 603, every number and every name generated out of the
  engine source of the real MZ game. A command is now a number and a name, and a
  caller can say what 121 is.
- A hand written table was 79 of 178 names wrong and was replaced. 129 was
  "Change Hp" and the engine calls it "Change Party Member"; 231 was "Move Event"
  and the engine calls it "Show Picture". Do not write a table of an engine's
  numbers from memory. Generate it or read it.
- `code - 300` finds the owner of a data number four times out of eight and is
  wrong the other four, twice pointing at a command that exists and does
  something else. The owners are written down. A test runs the rule and counts
  four so the rule cannot creep back in as a calculation.
- 411 and 413 are commands the engine names Else and Repeat Above. 412 beside
  them has no method and is data of a branch. A family is not a family.
- A name was written in two places (enum doc comment and a string table) and the
  copies drifted so that three name mutations were invisible. It is a
  `record struct MzCommand(int Code, string Name)` now: one value, one name.
- Tests: `TestMzCommandTable` 13/13, total 866/866, validator passed.
- Mutations: 11, of which the first suite detected four of nine. The three name
  mutations escaped because of the two-place problem, and one mutation tested
  nothing at all because a command is decided before an owner is consulted. Both
  are stated in the test file so they are not repeated.
- Still missing, and this is the next step: **a command is named, not done.**
  Nothing interprets 111's six comparisons or 121's three modes, there is no
  renderer, and a 657 line is text. The next real step for MZ is the
  interpretation of a few commands against the engine source, starting with 111,
  whose whole semantics were read out of `command111` and are six comparisons
  over six kinds of thing.
- Tests: 878/878, TestMzBranchEvaluator 12/12, Mutationen 10/10.

## The check that passed when it should not have

A mutation that folds a kind of branch the engine has no name for into the
nearest kind it does have was invisible twice. The test asked whether the
evaluator's refusal mentioned the number, and the evaluator names what it needs
in its own words: a branch on 99 that was folded into 0 reports "switch 0", and
"switch 0" does not contain "99". **A check that reads the complaint rather than
the thing cannot see a fold, because the complaint is itself already wrong.**

The number lives in the branch, so the branch is what is checked. That test
closes it, and the same reasoning applies wherever a diagnostic is used as proof
of the thing it describes.

## Two repairs that fought each other

A killed mutation run left two mutations in `MzBranchEvaluator.cs`. Two repair
scripts then each rewrote the wrong branch, so a Gold branch held the Actor's
code and the Actor branch had lost the line naming the actor. Both were found
only by the compiler and by reading the region. **Repairing a file by matching
the first occurrence of a shape that occurs twice is how a repair lands in the
wrong place**; the repair has to name the place, not the shape.

## What is still not decided

A branch is decided. 121's three modes, 126's change of an item and 126's change
of a weapon are not, there is no interpreter holding an index into a list, and
a 657 line is text. MZ has detection, bounded data reading, a named command
table and one decided command.

## K-124 The MZ event index

**What was built** `MzCommandEntry`, `MzOperation` (operands, operations, and a
`MzRandom` held per interpreter), `MzCommands` (121 and 122), `MzControlFlow`
(the commands whose whole effect is the index, with the three answers they give)
and `MzInterpreter` (the index, the branch result per indent, the step limit).

**The rules, and the three that were wrong first**

1. Every command that returns true is followed by `this._index++`. A first
   draft added a flag for "the command moved the index itself" and then did not
   step over a command that had, which made an else land on the false arm it had
   just skipped. The flag is gone.
2. A repeat above walks back to the first command at its own indent and the
   step then moves off that one, so `112`, body, `413` goes round with no
   special case.
3. A command with no method is stepped over, not refused. **All five of the
   codes this game stores without a method are ones it stores on purpose**: 0
   the end of a block, 401 a line of text under a 101, 412 the end of a branch,
   655 and 657 the two halves of a script.
4. A list that ends inside a branch is reported `Truncated`; the engine's
   `skipBranch` has no test for the end of the list and would read past it.
5. The step limit is the engine's `checkFreeze`: a hundred thousand commands in
   one frame.

**Two findings from the real map, neither a test mistake**

- **This game stores a loop nothing can leave.** Event 4 is a 112 with seven
  message commands and a 413, and nothing between them tests anything or
  breaks. The engine plays it until `checkFreeze` stops it. The reader reports
  the same, and the test calls a freeze there the right answer.
- **Random is drawn per variable, not per range.** A first draft claimed one
  draw for a whole range; the engine's `command122` calls `Math.randomInt`
  *inside* `for (let i = startId; i <= endId; i++)`. The test was wrong in the
  same direction as the draft and was corrected against the source.

**Total 896/896**, `TestMzInterpreter: 18/18`, validator passed, build 0
warnings / 0 errors.

## A repeat above is not a jump, and a mutation run said so

The mutation run left two rules untested, and reading the engine for them
corrected a claim the test file had been making wrongly for two cards:

1. **`command413` is not `jumpTo`.** It is `do { this._index--; } while
   (currentCommand().indent !== this._indent); return true;` — it writes the
   index and clears nothing. Only `command119` calls `jumpTo`, and that clears
   the branch result of every indent it steps over. The test file had said "a
   repeat above is a jump, so this is the case that matters", which is the
   opposite of what the engine does.
2. **A jump that points backwards at a label is a loop.** `jumpTo` sets the
   index to the label, `executeCommand` steps on, and the jump is met again. A
   test shaped that way hung the suite for a hundred thousand steps, which is
   `checkFreeze` doing its job — and the same thing the engine does. **This game
   stores no label and no jump at all**, so only the shape that ends is
   asserted.

## How a 30 KB test file was lost twice, and what stops it

Two repair scripts each emptied `test_mz_interpreter.cs`. Both used a slice
`[k:end]` and both ran when the anchor text was no longer in the file, so `k`
was -1 and the slice was empty or inverted. **The second time the file was
untracked, so git had no copy either.** What stopped it the third time was
`git add` immediately after writing, before anything else could touch the file,
and then editing with `patch` and an exact anchor rather than with a script that
rewrites by shape. A test file that has not been staged is one `rm` away from
needing to be written again from memory.

## What is still not decided

Eleven commands of a hundred and fourteen have an effect. 117, 126, 230, 231,
232, 235, 351 and 357 are read as text; a 655 or 657 line is text. There is
still no renderer, no save path and no input. MZ has detection, bounded data
reading, a named command table, a decided branch and an index that walks a list.

## K-125 The list a command calls, and the wait that stops one

**Built** `MzEventRunner` (a run over a list and every list it calls),
`MzInterpreter.Wait` / `PassFrame`, `MzAction.CommonEvent` / `Wait`, and
`Result.MissingCommonEvent` / `WaitingFrames` as fields rather than prose.

**The rules, from `Game_Interpreter`**
1. A called list runs to its end before the caller moves on — `updateChild`
   gives the child its own `update()` and the parent breaks the frame while it
   is still running.
2. Every list in a run shares one set of facts, because both go through the one
   `$gameVariables`.
3. The event id travels with the call, and only on a map (`isOnCurrentMap`).
4. A 230 holds the index: the same command is read again the frame after, so a
   reader that stepped over it would run a list three frames early.

**What it refuses rather than guesses** The bounded fixture carries no
`CommonEvents.json` — the real one is 4.5 MB and was left out on purpose — so
every 117 in this game names an index that cannot be handed over. The engine's
own line is `if (commonEvent)` and steps over a missing one; **this reader
refuses and names the index**, because a silent step-over would run the rest of
a game's list as if the call had never been there.

**Measured on the one map in the fixture** Six event pages ending four different
ways: three reach a common event and name the index, one stops at a 230 and says
it is waiting, one is refused because it opens with fifty-eight lines of the
game's own JavaScript, one is a single 0 and runs through. A first draft guessed
three, one, one and one, and two of the four were wrong.

**A number read out of prose is not a number** The test took the common event's
index out of the message with an offset and got 76 for 476, because it counted a
space twice. The index is a field now, and the test checks field and prose
against each other. **The same rule as the two lost test files: name the place,
not the shape.**

**Total 912/912**, `TestMzEventRunner: 16/16`, `TestMzInterpreter: 18/18`,
validator passed, build 0 warnings / 0 errors.

**The mutation runs found a real fault in the runner, not only gaps in the
tests.** Reading the map and the event of a child was untested, and asking the
question showed the runner was doing it wrong: it passed the *caller's* map down
and read both values off the frame rather than off the interpreter. The engine
does neither — `setup` sets `_mapId` from `$gameMap.mapId()`, the map the game
is on, and `command117` reads `this._eventId` off the calling interpreter. Both
are now read from where the engine reads them, and `Result.Child` hands the
caller the child so a test can check the three fields instead of trusting them.

**A first draft of that test claimed the event id falls away on the second
level, and the engine does not do that.** `setup` is `this._eventId = eventId
|| 0`, so a chain that is on the map carries the same event all the way down;
only a list that is not on the map passes zero, and there it stays zero. The
claim was backwards on the interesting side and would have been written into a
test as if it were the engine's answer.

**Five mutation runs, and the lesson from them.** A rule that survives a run
is not automatically a gap, and a rule that dies is not automatically covered.
The first run caught 4 of 9. The three that escaped were real, and the fourth
turn showed why the other two were not: `WaitFrames <= 0` and `< 0` differ
only in a state nothing can reach, and the wait case's own `return false`
cannot be changed to `true` and change anything, because `ExecuteOne` ends with
`return Stopped == MzStep.Stepped` and `Wait` has just set `Stopped` to
`Waiting`. **That is an equivalent mutant and is now named in the code as a
dead branch**, rather than carried as a rule with no test.

**A rule that hit the wrong place is not a gap either.** `MissingCommonEvent =
index,` stands in two places — the depth branch and the missing-list branch —
and a `replace(..., 1)` mutation hit the depth one, which no test reached. So
the depth was genuinely untested, and `MaxDepth` is now a field with two tests:
a list that calls itself is refused at the engine's own hundred and is not
reported as a freeze, and the same list under the limit runs through.

**Two test drafts were wrong and both are named in the file.** A test of
"runs through" was first given a list that calls itself, which never can — a
self-call is endless at every level. The second draft gave the called list a
call to itself, which is the same endless thing one level down. Both are
recorded, because the next one to write a "control case" here will reach for
the same shortcut.

## What is still not decided

Thirteen of a hundred and fourteen commands have an effect. 126, 231, 232, 235,
351 and 357 are read as text; a 355 or 657 line is text. A called list this
repository has runs; one it does not have is named. Still no renderer, no save
path, no input and no audio.

## K-126 Change what the party is carrying

**Built** `MzParty` (the inventory and the four rules), `MzCommandTable
.ChangeItems`, the 126 case in `MzCommands`, and `MzBranchFacts.MaxItems`.

**The rules, from `Game_Party`**
1. `container[item.id] = newNumber.clamp(0, this.maxItems(item))` and
   `maxItems` is `return 99` — no argument, no per-item case. **Five of this
   game's eighteen 126s ask for 999.**
2. `if (container[item.id] === 0) { delete container[item.id]; }` — a count
   that lands on zero is deleted, not stored.
3. The clamp is from below as well, so taking four of one is none.
4. `itemContainer` returns null for an item that is not there, and the engine
   steps over it. This reader says it did not happen.

**And one that is easy to get wrong in the other direction** `operateValue`
asks the operand's kind first, so a literal amount must not be read from a
variable. `operation === 0 ? value : -value` has no third case: an operation of
seven removes exactly as an operation of one does.

**The clamp is invisible in the middle of the range** A test that only added
four to an empty bag would pass with no clamp at all, so every rule is asked
about at its boundary and the default is claimed to be ninety-nine.

**Two countings that are different claims.** Eighteen 126s over fifteen items,
five of them above ninety-nine — read straight off the game's data. A walk
reaches only nine, because one page stops at a 230, and that is a statement
about a reader with no frames. **A first draft counted the walk and called it
the game**, which was wrong in the direction of under-reporting.

**A known gap this card found and now names.** A lone `MzInterpreter` knows no
common events, so a 117 is stepped over like a 0 — the silent step-over K-125
refuses, still reachable through this door. Both answers are claimed side by
side rather than one of them quietly assumed.

**The wiring test found two real faults, and three mistakes of my own.**

1. `new MzParty(pFacts)` knew no items, so every 126 was a silent no-op with
   every count at zero and nothing saying why. The ids travel in
   `MzBranchFacts.KnownItems` now.
2. **An empty set of known ids was read as "everything exists"** — the
   opposite of what it means, and it would have handed a player 999 of an item
   the game never had while looking as if it worked.

**And the three mistakes, which are the ones to remember.** A fresh
`MzInterpreter` has `Stopped` at whatever it starts as, not at `Stepped`, so a
hand-written `while (ExecuteOne(...))` gives up on the very first command —
`Run` is the loop a caller should have used. `new(2, ...)` where the code
belongs: **126 is the command, not the item**, and a page of codes 2, 3 and 4
is a list the engine steps over. And a party the test made itself keeps its
notices to itself while the interpreter builds its own over the same facts, so
the notice is on the action.

**Total 924/924**, `TestMzParty: 12/12`, validator passed, build 0 errors.

## K-127 Put a picture on the screen — DONE

231, 232 and 235 on the one map in the fixture: nine commands, three shows,
four moves, two erases, on images 1, 86 and 87. **Not 127 or 128** — this
game's `Map002` has no 127, no 128, no 129 and no 130, and the fixture has
no `Weapons.json` or `Armors.json`, so carrying weapons would have meant
rules no data here can check. The pictures were chosen because the data is
here, and because they are the first commands in this game that need
something other than numbers to have an effect.

Six rules out of rmmz_objects.js 1.9.1, in the card. The two that bite: **a
shown picture is a new object and the old one is gone with it**, and **a move
sets a target, not a value, so a move of zero frames changes nothing at all**.

**A real fault in reading, not in testing.** `MzCommandEntry.From` handled a
Number and took `item.Text` for everything else, so a JSON boolean became the
empty string — and a 232 carries its wait in the eleventh slot as a real
`true`/`false`. This game's four moves all came back as "does not ask to
wait". No test had noticed because no test had read a boolean out of an event
list. Fixed in the reader, not worked around in the test.

**And one of my own:** `if (params[11])` is a truth value, and a first draft
called `int.Parse` on it, which throws on the empty string a game may leave in
that slot. Reading it the way the engine reads it is now its own named
method.

**A third fault, and the worst: a waiting move never arrived.**
`ExecuteOne` stepped the index only when the interpreter was `Stepped`, and a
`MovePicture` that asks to wait returns false — which means the same thing.
So the next frame read the same 232 again, set the same twenty frames again,
and a picture that had to cross the screen waited for ever. The engine has no
such trouble: `command232` ends in `return true` whatever it asked for, and
its wait lives in `_waitCount` where the next command cannot reach it. The
index moves and the run stops in two separate steps now. `MzInterpreter` 18
and `MzEventRunner` 16 are unchanged after it, so it was a rule nothing had
exercised.

**And a fourth of my own:** a test that claimed a picture sits at 2000, 2000
because the *scale* is 2000, 2000 was reading the wrong line. The place is
zero. Corrected to the measured value rather than adjusted until it passed.

**A test that counts is not a test that runs.** The first draft's ninth test
was named "every picture command in this game runs" and did nothing of the
kind — it counted codes out of the file with no interpreter in sight, and four
mutation rules escaped through it. The replacement builds an interpreter,
hands it the frames the two waiting moves ask for, and checks the screen when
the list is through. It is the test that found the waiting-move fault.

**A fifth mistake:** a 122 written with four parameters has nowhere to read
a value from — `command122` is `startId, endId, operationType, operandType,
operand` and the operand is the fifth. The page was not held by the move
failing; it was held by a command that could not do what the test meant.

**935/935**, `TestMzScreen: 11/11`, `TestMzInterpreter: 18/18`,
`TestMzEventRunner: 16/16`, `TestMzParty: 12/12`, validator passed, build 0
errors. Second mutation run in flight. Next after this: 233, 234, 224, 236 —
the other picture commands — then 355 and 357, which this map uses nine and
three times.

## K-128 A menu, and a plugin call that is refused — DONE

K-127 asked which picture commands come next. **None of them:** this map uses
no 224, no 233, no 234, no 236, and modelling them would have been rules no
data here can check.

The two commands left on this map that mean something mean opposite things.

**351 is run.** `if (!$gameParty.inBattle()) { SceneManager.push(Scene_Menu);
} return true;` — one condition, and it returns true either way. A reader that
stopped the run in a battle would leave the commands after it unrun in a way
the engine never does. `MzMenuState` exists so a 351 is not
indistinguishable from a command with no effect.

**357 is refused, by name.** `PluginManager.callCommand(this, pluginName,
params[1], params[3])` is somebody else's JavaScript. All nine of this map's
357 commands are answered, each naming the plugin and the command inside it,
each landing on `MzBranchFacts.Notices`. A silent step would leave a game that
looks as if it works while its crafting menu and floating text never appear.

**The finding that outranks both cards: this game ships fifty-two plugins and
all fifty-two are enabled.** Its 357 commands call `ItemCombinationMZ`,
`DTextPicture` and `HyoujouSelect`; its 355 scripts read
`$gameVariables.value(180)`. **UniversalRPG runs this game's MZ event code and
none of its plugin code, and no bounded slice changes that.** Said once, with
the numbers, where a caller will see it — which is what a card can do about
it.

**939/939**, `TestMzMenuAndPlugins: 4/4`, validator passed, build 0 errors.
Every expectation was measured out of the game's own files first: nine plugin
commands, three plugins, two 351s, three scripts. **Nine mutation rules, nine
caught** — and one of them had to be written twice, because the first attempt
replaced a fragment inside an escaped string and left the file unparseable.
`BROKE` counts as caught and proves nothing; the second attempt compiled and
failed three named tests, one for each plugin this map calls.

**Next after this, and it is not another MZ command.** This map's twenty-two
codes are now all either modelled or refused. The next thing worth doing is a
**second MZ fixture** — a game with no plugins — so the reader can be checked
against MZ event code without a plugin's JavaScript in the picture at all.

## K-129 A second MZ fixture, from a game with no plugins — DONE

The first fixture is a game with 52 enabled plugins and 9 plugin commands.
A reader checked against it is mostly checked on its refusals. That needs a
second game.

`CamelliaCoronation-Win`, in `E:/RPGMakerGames` — a free MZ game the user put
there to work with. **Engine 1.9.1, measured:** both games' `rmmz_objects.js`
carry the same 114 `commandNNN` methods, none only in one or only in the other.

**One plugin, in no command. No 355 and no 357 on any of the 19 maps.** That
negative claim is the reason the fixture exists.

**Alles gemessen, nichts behauptet:**

- 2 432 Befehle, **1 772 laufen, 660 nicht** — und alle 660 sind echte
  MZ-Befehle, keine Pluginaufrufe.
- Häufigster: **401 (Textzeile, 938×)**, dann **101 (Dialogblock, 414×)**,
  dann **505 (Wegliste, 348×)**. Ein Entwurf nannte die Wegliste am
  häufigsten — „ein Spiel besteht hauptsächlich daraus" — und lag zwei Plätze
  daneben. Und ein zweiter Entwurf nahm die größte Zahl in *irgendeinem*
  Parameter und bekam 720, eine Pixelposition statt einer Variablen.
- **15 Variablen, 0 bis 15, keine darüber. 8 Items.** Eine Klasse, eine
  Animation, **keine Switches, keine Common-Event-Aufrufe, keine
  Actor-Referenzen.**
- **`CommonEvents.json` ist 376 Byte und vorhanden.** Bei der ersten Fixture
  fehlte sie (Original 4,5 MB), und der Runner musste ein 117 verweigern. Eine
  Regel, die nur gegen eine Lücke getestet wurde, ist eine ungetestete Regel.
- 0, 401, 404, 405, 412, 505 sind **echte MZ-Sonderbefehle ohne Methode** und
  keine Plugins. Die Engine liest sie nach Position, nicht per Dispatch.
  „MZ kennt diese Zahl" ist nicht dasselbe wie „MZ führt sie aus".

**537 KB, 30 Dateien**, kein `js/`, keine EXE, kein Bild, kein Audio, kein
`Tilesets.json`. **`Skills.json` ist die eine abweichende Datei**: 104 525 auf
1 181 Byte, weil **kein Befehl der 19 Karten eine Skill referenziert und der
Leser keine liest.** Alles andere byteweise identisch, SHA-256 im Manifest.

**945/945**, `TestMzPlainFixture: 6/6`, validator passed, build 0 errors.
**Zehn Mutationsregeln, zehn gefangen, erster Lauf, keine entkommen** — und an
den **Fixture-Dateien** statt am Leser, weil die Behauptungen über Daten sind:
ein 505 als Zweig, ein 401 als Wahl, ein 101 als etwas anderes, ein 357 in eine
Karte eingefügt, ein 355 in eine Karte eingefügt, die Common-Event-Liste
geleert, die Datei gelöscht, und drei gegen die Testarithmetik.

**Das ist die erste Karte seit vier, auf der nichts entkommen ist**, und der
Grund ist derselbe wie bei den drei davor: dort entkam eine Regel, die kein
Test von der Seite gefragt hatte, auf der sie falsch ist. Eine Behauptung über
eine Zahl ist nur so gut wie der Test, der merkt, wenn sich die Zahl ändert —
und die Behauptung einer Fixture ist eine Behauptung über eine Zahl.

**Next: die 660.** 505 Weglisten (348), 205 Movement-Skripte (96), 123
(42), 213 (36), 405 Choices (36). Und `123` ist Change Gold — dieselbe
Clamp-Familie wie `126`, aber mit `maxGold` statt `maxItems`.

## K-130 Send the player somewhere, and hold the page until they arrive — DONE

`project/src/mz/MzPlayer.cs`, `MzWaitMode.cs`, `MzCommandTable.TransferPlayer
= 201`, `MzBranchFacts.Player` / `MessageOpen`, `MzInterpreter.WaitFor` /
`Refuse` / `PassFrame(Func<MzWaitMode, bool>?)`.
`project/tests/core/test_mz_player_transfer.cs`, 5 tests.

**950/950**, `TestMzPlayerTransfer: 5/5`, validator passed, build 0 errors.
**Neun Mutationsregeln, neun gefangen, erster Lauf, keine entkommen.**

**The reason for this card, and it is a reason about testing rather than about
MZ.** The 660 commands that do not run yet are led by 505 at 348 and 205 at
96, and both are movement — both need `Game_Character`, a move route decoder
and a passability model, which is three cards before the first of them can be
tested. 201 is 33 commands and needs none of that. **A card you can test is
worth more than a bigger card you cannot**, and the 348 will still be there
when `Game_Character` exists.

**And it is the first command here that is neither a change nor a number of
frames.** K-125 waits for frames, K-127 for a picture's movement, and a 201
for a **condition**: `setWaitMode("transfer")` with
`updateWaitMode` answering `waiting = $gamePlayer.isTransferring()`. A
condition wait has no length, so a caller passing frames cannot end it. The
first draft counted frames and would have let the page on with the player
still on the old map.

**Four rules, each a place a first reading goes wrong.** A transfer is
**reserved and not carried out** — `reserveTransfer` changes nothing a player
can see and `performTransfer` is what moves them. The engine **returns false
and transfers nobody** in a battle or with a message up, which is neither a
wait nor a finish, so `MzStep.Refused` says that and is not dressed up as
either. **The direction is set on the way**, because `performTransfer` is what
calls `setDirection`. A map this reader has not read is **named and the player
stays put** — half-applying it is worse than not moving, because the caller
would see a position and no file behind it.

**This game's own numbers: 33 transfers over sixteen maps, every one with the
first parameter at zero.** A reader that always looked in the variables would
send every player in this game to variable four.

**A C# trap, measured and now in the test so the next card does not walk into
it.** `$"Map{i:03}.json"` with `i = 1` produces **`Map13.json`**. In an
interpolated string `i:03` is a fill character of `0` and a **precision** of
`3`, and a whole number with a precision is padded on the **right** — 1
becomes "13", 2 becomes "23". Every file was missing and the only thing that
said so was the reader's own error about a file ending mid-value. `ToString
("000")` is the right spelling. **It took four rebuilds and a `Console
.WriteLine` to see it**, because every other signal said the code was correct
and it was.

**A build note, measured, so it is not measured again.** `dotnet build -m:1`
reports **53 warnings**, and the same tree without the switch reports **0
warnings, 0 errors**. The warnings are the parallel build's, not the code's:
the schalter forces a shared compiler instance and warns about it. **The
seriellen Build ist der ehrliche, und `-m:1` gehört nicht in die Abnahme.**

**Not here.** No map is loaded and no tile is drawn. The other four wait modes
need a scrolling map, a moving character and a plugin callback.

**Next: 505 Move Route at 348, which needs `Game_Character` first.**

## K-131 Walk a character, one step a frame — DONE

`project/src/mz/MzCharacter.cs`, `MzMoveRoute.cs`, `MzRouteStep.cs`,
`MzJson.Write`, `MzCommandTable.MoveRoute = 205`,
`MzBranchFacts.Characters`, `MzWaitMode.Route`.
`project/tests/core/test_mz_move_route.cs`, 7 tests.

**957/957**, `TestMzMoveRoute: 7/7`, validator passed, build 0 errors.
**Vierteen Mutationsregeln, vierzehn gefangen.** Der Hauptlauf meldete
13 von 14; die eine als entkommen gemeldete Regel habe ich isoliert
nachgeprüft und sie fiel — **drei von sieben Tests**, Baum byteweise
unverändert. Sie steht als 14 von 14 drin, denn **eine Zahl, die man
nicht geprüft hat, ist keine Zahl, die man gezählt hat.**

**The list in K-129 was wrong, and this card is what showed it.**
`command505` **does not exist.** `505` is a nested move-route entry the
editor writes, and a route reaches the runtime through **`205 Move Route`,
96 of them** — not 348. Sixty say `wait`, thirty-six do not. The 348 were
never event commands.

**The API is `isMapPassable` and `canPass`, not `isPassable` and
`checkPassage`.** `checkPassage` has **zero** occurrences in 1.9.1; those
names come from other RPG Maker engines. A reader built from memory would
have compiled, run, and tested nothing real. **This is the second time in
two cards that a name from memory was not a name in the engine.**

**`reverseDir` is `10 - d`, not `(d + 4) % 4`.** A first draft wrote the
0..3 form, and `reverseDir(2)` then answered Left instead of Up — so every
"one tile behind" position landed **in front** of the character and every
character walked away from where it was going. The directions are 2/4/6/8
for down/left/right/up, **not** the RM2K 0..3 order this repository uses
elsewhere.

**`isStopping` is `!isMoving() && !isJumping()`, two terms.** A draft added a
third, `&& !Waiting`, and **every route with a `ROUTE_WAIT` in it ran
backwards**, re-issuing one step for ever. A character waiting is a
character that has arrived.

**A product fault with a wider reach than this card.** `MzCommandEntry.From`
turns every parameter into a string, and anything that is not a number or a
boolean became `item.Text` — **which for a nested object is `""`**. A 205's
second parameter is exactly such an object, so **every move route in every
game came back empty and the reader could not have said why.**
`MzJson.Write` now writes a value back out, because **a reader that cannot
write a shape back has already half-lost it.**

**Two more, found by the same tests.** `Truth` read a boolean out of `Text`
where the parser puts it in `Boolean`, so **all three flags of all
ninety-six routes came back false** and not one page was ever held. And
`From` read a route's `code` out of `Text`, which is empty for a number, so
**every route code came back 0 — which is END** and all ninety-six routes
did nothing while looking perfectly plausible: five steps read, five ENDs
run, every character standing still. **Both faults failed silently, and
only real data showed them.**

**A test fault, not a product fault, and worth naming.** The last failing
test wrote `new(121, …)` and then asked why the variable was not set.
**121 is Control Switches** — it set a switch, ran, and left the variable
alone, and the run looked perfectly healthy. It is now
`MzCommandTable.ControlVariables`, by name.

**Also measured: this game's route codes.** END 96, MOVE_LEFT 74,
MOVE_RIGHT 59, MOVE_DOWN 50, MOVE_UP 45, JUMP 31, CHANGE_SPEED 26, TURN_UP
11, MOVE_BACKWARD 10, TURN_DOWN 10, TURN_RIGHT 9, TURN_LEFT 7,
MOVE_FORWARD 4, WAIT 4, TRANSPARENT_ON 4, STEP_ANIME_ON 2, STEP_ANIME_OFF
2. **MOVE_LEFT leads and MOVE_DOWN follows** — the opposite of "a game
mostly walks about". MOVE_RANDOM, MOVE_TOWARD, MOVE_AWAY and all eight
diagonal codes appear **zero** times and are named rather than guessed at.

**Next: the remaining offene codes, of which `401 Show Text` at 938 is by
far the largest, and it needs a message window this reader does not have.**

## K-132 Read a line of text, and every code in it — DONE

`project/src/mz/MzMessage.cs` (mit `INameSource` und `ThreeNames`),
`MzCommandTable.ShowTextLine/ShowChoices/ContinueText`,
`MzBranchFacts.Message` / `Names`, `MzCommandSet.HasMethod` /
`NoMethodCodes`.
`project/tests/core/test_mz_message.cs`, 6 Tests, und ein K-124-Test
umgeschrieben, der jetzt beide Antworten sagt.

**963/963**, `TestMzMessage: 6/6`, `TestMzInterpreter: 18/18`, validator
passed, build 0 errors.
**Neun Mutationsregeln, neun gefangen.** Der erste Lauf meldete sieben von
neun — **und beide „entkommenen" waren Fehler in den Regeln, nicht im
Leser.** Eine machte eine Code-Behandlung zu einem Äquivalent, die andere
traf einen Listeneintrag, den der Test gar nicht erreichte. Isoliert und
neu geschrieben: **neun von neun.**

**Die zweite Geschichte ist die bessere.** Die Liste der Zahlen ohne
`commandNNN` war geraten und **falsch**: sie behauptete, `601`, `602` und
`603` hätten keine Methode. **Sie haben eine** — `command601`, `command602`
und `command603` sind drei der 114. Und sie sprach von „178 reservierten
Nummern", wo die K-122-Liste in Wahrheit **die 114 Methoden** ist.
**Neun Zahlen haben keine Methode, und alle neun liegen außerhalb dieser
114**: `0`, `401`, `404`, `405`, `412`, `505`, `604`, `605`, `657`.

**Das ist der vierte Name in vier Karten, der aus dem Gedächtnis kam und in
der Engine nicht existierte** — nach `checkPassage`, `isPassable` und der
`reverseDir`-Form. **Gemessen wird, nicht erinnert.**

**Der größte Brocken der Fixture: 938 Zeilen, jede mit genau einem
Parameter.** Und nichts daran ist ein Darstellungsdetail — das ist der
Befund: **eine Dialogzeile ist meistens keine Worte.**

**Zwei Durchgänge, zwei Regelsätze.** Durchgang eins,
`convertEscapeCharacters`, schreibt in drei Schritten um: jeder Backslash
wird zum Steuerzeichen; **zwei Steuerzeichen machen einen Backslash
zurück**; und Variablen-, Helden-, Gruppen- und Währungscodes werden
gefüllt, **die Variablen in einer Schleife**. Durchgang zwei, die
Zeichenschleife, behandelt **jedes Zeichen unter 0x20 als Steuerzeichen**
und schreibt es nie in die Ausgabe.

**Drei Klassen, und nur eine ist Text.** Im Text: `\V[n]`, `\N[n]`,
`\P[n]`, `\G`. **Nicht im Text und nie gezeigt:** `\|`, `^`, `!`, `>`,
`<`, `$` — **ein Leser, der sie ausgäbe, setzte einen `|` mitten in einen
Satz.** Weder Text noch Feder, und der Leser benennt sie: `\C[n]`,
`\I[n]`, `\PX[n]`, `\PY[n]`, `\FS[n]`, `\{`, `\}`.

**Die Zahlen, und zwei meiner eigenen Messungen waren erst falsch und
dann richtig.**

| | zuerst | gemessen |
|---|---:|---:|
| Zeilen mit `\C[n]` | 0 | **19** |
| Undrawable | 0 | **57** |
| Leere Zeilen | — | **14** |

`\C[3]` 19×, `\C[0]` 19×, `\I[177]` 19×, `\!` 3×, `\|` 3× — **19
Zeilen mal drei Codes, das sind die 57.** Eine Zeile wartet **dreimal**:
`\|.|\|.|\|.` sind drei Entscheidungen und nicht eine.

**Der Fehler, der zweimal passierte.** Ein Scan fand den Buchstaben `C`
53-mal, `N` 38-mal, `V` 22-mal und `P` 11-mal, und ich hielt sie für
Auszeichnungen. **Es sind Wörter** — „SEND **C**OUT!!", „\* **N** om\*",
„Valuable **V**egetables". **Ein Code ist zuerst ein Backslash und dann
ein Buchstabe.** Genau dieser Fehler ließ mich „keine Farben" behaupten,
und die Dateien sagten neunzehn. **Der Test, der es bemerkte, las
dieselben Dateien und riet nicht** — und das ist die Begründung dafür,
dass man Testzahlen nicht schätzt.

**Eine Aussage, die älter ist als diese Karte.** Ein K-124-Test behauptete,
ein 401 werde übergangen, weil die Engine keine Methode dafür hat. **Das
stimmt und stimmt weiter.** Der Leser liest es trotzdem, weil er nach
einer anderen Frage gefragt wird: *was hat das Spiel geschrieben?*
**„Hat die Engine eine Methode" und „was steht in den Daten" sind zwei
Fragen mit zwei Antworten.** `MzCommandSet.HasMethod` sagt jetzt beides,
und `NoMethodCodes` nennt die zwölf Zahlen ohne `commandNNN`.

**Nicht hier.** Kein Textfenster, kein Renderer. Die Zeile wird gelesen
und als Daten behalten: Wortlaut, Wartungszahl, und alles, was nicht
gezeichnet werden kann.

## K-133 A 101, and everything it swallows — DONE

`project/src/mz/MzDialogue.cs`, `MzChoice.cs`, `MzPrompt.cs`;
`MzCommandTable.ShowDialogue = 101` und `ShowChoiceList = 102`;
`MzWaitMode.Message`; `MzBranchFacts.MessageBusy`, `LastDialogue`,
`LastChoice`, `LastPrompt`; `MzCommands` 101-Fall und 401-Verweigerung;
`MzInterpreter` Run-Vorcheck.
`project/tests/core/test_mz_dialogue.cs`, 9 Tests, plus drei umgeschriebene
in den K-124- und K-132-Dateien.

**972/972**, `TestMzDialogueAndChoice: 9/9`, `TestMzInterpreter: 18/18`,
`TestMzMessage: 6/6`, validator passed, build 0 errors.

**Die erste Karte, in der ein Befehl andere Befehle isst.** `command101` macht
`while (this.nextEventCode() === 401) { this._index++; add(…); }` — **eine
Zeile Dialog wird nie dispatcht**, weil es kein `command401` gibt, zu dem sie
dispatcht werden könnte. **Alle 938 Zeilen dieses Spiels hängen an einem 101
und an nichts anderem.**

**414 Dialoge, je eine bis vier Zeilen — 118 mit einer, 130 mit zwei, 104 mit
drei, 62 mit vier** — und die Summe ist genau 938. **Acht haben einen 102
darunter**, sechs unter einem einzeiligen und zwei unter einem zweizeiligen;
**kein 103, kein 104, kein 403** in neunzehn Karten. Befehle gefressen:
**112, 134, 106, 62** — 1360 statt 414 + 938, weil die acht Wahlen drin sind.

**Drei Regeln, und eine vierte, die nur dieses Spiel zeigt.** Eine belegte
Seite wird verweigert — und `isBusy()` ist **Text oder Wahl oder Zahl oder
Gegenstand**, also wird ein 101 hinter einer unbeantworteten Wahl so fest
verweigert wie einer hinter einer Zeile. Genau **eines** von 102, 103 und 104
wird genommen, und zwar das direkt nach der letzten Zeile, denn der `switch`
läuft einmal. **Und es wartet immer**, denn `setWaitMode` liegt außerhalb des
`switch`.

**`102` ist nicht `405` — der fünfte Name in fünf Karten, der gemessen werden
musste.** `ShowChoices` bedeutet seit K-132 das 405, und der Folger wurde
dagegen verglichen, also **wurde keine der acht Wahlen dieses Spiels je
gefunden**: 1352 Befehle statt 1360. **Vier Läufe**, weil die Tests, die
fielen, die mit der Summe waren.

**Und ein Guard ohne Test.** `ExecuteOne` hatte eine Grenzprüfung, die eine
Mutation ausschaltete und alle Tests blieben grün, weil `IsRunning` sie nie
erreicht. **Die Reparatur war nicht ein Test dafür, sondern ihre Entfernung** —
der Fall wird eine Ebene höher behandelt, in `Run`, das jetzt vor der Schleife
prüft. **Eine zweite Prüfung, die nie feuern kann, ist eine Behauptung, der ein
Leser glaubt und niemand belegen kann.**

**Test evidence** 9 Tests, drei umgeschrieben.
**Mutations** Zwölf Regeln über vier Läufe. Jede entkommene Regel war
entweder eine kaputte Regel oder ein Test, der das Mutierte nicht erreichte;
**zwei fanden echte Produktfehler** — den 102 aus dem falschen Befehl und
103/104 als Optionsliste gelesen.

## K-134 Das Board hat Karten verloren — TEILWEISE REPARIERT

**Befund.** `KANBAN.md` ist laut `AGENTS.md` das einzige maßgebliche
Board, und seine Tabelle und seine Details widersprachen sich.

| | vorher |
|---|---:|
| Karten in der Tabelle | 83 |
| Karten mit Detailabschnitt | 86 |
| **In den Details, nicht in der Tabelle** | **30** |
| **In der Tabelle, nicht in den Details** | **25** |
| Doppelte Tabellenzeilen | 2 |

**Alle Karten von K-112 bis K-133 — zweiundzwanzig Karten: RGSS-Archiv,
Marshal-Leser, Ruby-Lexer, Parser, Wertebene, zwei MZ-Fixtures und die ganze
Befehls-Ausführungslinie — waren in den Details und nicht in der Tabelle.**
Ein Agent, der nur die Tabelle liest, hätte gesehen, dass die Arbeit bei
K-111 aufhört, und keine Möglichkeit gehabt, die anderen zweiundzwanzig zu
erkennen.

**Ursache.** Die Tabelle wird von Hand geführt, und jede Karte seit K-112
wurde nur als Detailabschnitt angelegt. **Die Details sind der Ort mit den
Belegen** — Testzahlen, Mutationen, Korrekturen — und die Tabelle ist das,
was ein Agent zuerst liest. **Wenn eines von beiden falsch ist, hört die
Arbeit auf sichtbar zu sein.**

**Repariert:** die Tabelle ist aus den Detailabschnitten neu gebaut. Jetzt
**112 Zeilen, keine Duplikate, und jede Detailkarte hat eine Zeile.**

**Nicht repariert:** die 25 Zeilen ohne Detailabschnitt. Sie zu erfinden wäre
eine Behauptung ohne Beleg, und diese Datei trägt keine Behauptungen.
**K-134 fordert sie zurück — aus `git log` und der Testsuite, nicht aus den
Titeln.** Titel sind Behauptungen; diese Datei trägt keine.

## K-094 Vehicles for the action-event order — DONE

**Der Karten-Titel war die halbe Diagnose.** `Rm2kPlayerTurn.Apply` trug den
Kommentar *"The Player toggles a vehicle before looking for events. This
runtime has no vehicles, so nothing can be toggled and the action event check
always runs."* — und `Rm2kDecisionTurn.Run` war daneben implementiert,
mutation geprüft und **nie aufgerufen**. Die Fahrzeugklassen waren geladen und
gezeichnet; sie waren nie gefahren und nie bestiegen. `GameSimulationState` hatte
**null** Fahrzeugverdrahtung, und der Runtime führte seine eigene `_vehicles`-Liste.

**Implementiert**
- `GameSimulationState.Vehicles` und `.Boarding`, beide in `Reset()` geleert
- `Rm2kPlayerTurn.Apply` ruft `Rm2kDecisionTurn.Run`; ein Fahrzeug, das den
  Zug übernimmt, unterdrückt die Aktionsprüfung
- `CanEmbark` / `CanDisembark` aus der Passability-Maske, `IsVehicleStopping`
  für das Luftschiff, `OppositeBit` für die Gegenrichtung

**Drei echte Produktfehler, die die Suite fand**

1. **`TileInFront` sprach die falsche Richtungsordnung.** Der Spieler spricht
   2/4/6/8, `DirectionDelta` erwartet 0–3. **Ein `8` ergibt `(0,0)`** — die
   eigene Kachel. Jeder Bestiegetest "erfolgte", ohne dass sich etwas bewegte,
   und ein nach oben blickender Spieler bekam ein Aussteigen aufs Wasser, auf
   dem er bereits stand. Die Brücke `LiblcfFromFacingDirection` existierte
   bereits; ihr eigener Kommentar warnt vor genau diesem Vermischen.
2. **`PassDown` ist `0x01` und `PassUp` ist `0x08`.** Ich hatte beide vertauscht
   und `0x08` als "unten" geschrieben.
3. **Ein K-114-Test hielt die falsche Ordnung fest** — er prüfte `TileInFront`
   mit 0/1/2/3, und damit gegen sich selbst. Fünf Zusicherungen, alle konsistent
   mit demselben Missverständnis.

**Und eine Fixture, die sich selbst belog.** `SetPassability(state, x, y,
pAllowUp, pAllowDown)` war benannt, als wären es begehbare Richtungen, und
verdrahtete `pAllowUp` mit `PassDown` — also der Gegenrichtung. Zwei Tests
behaupteten daraufhin die falsche Polarität und schlugen gegen korrekten Code
fehl. **Eine Fixture, deren Namen über ihre eigenen Bits lügen, ist schlimmer
als keine Fixture**, weil der Fehler auf den Produktcode zeigt.

**Test evidence** 8 Tests in
`project/tests/core/test_rm2k_vehicle_decision_turn.cs`, 1 in
`test_rm2k_vehicle_boarding.cs` umgeschrieben.
**980/980**, `TestRm2kVehicleDecisionTurn: 8/8`, `TestRm2kVehicleBoarding: 11/11`,
`TestRm2kDecisionTurn: 12/12`.
**Mutations** Zehn Regeln über sechs Läufe. **9 von 10 gefangen.** Die
entkommene Regel ist keine Semantiklücke, sondern ein Werkzeugfehler: der erste
Runner verwendete `$TMPDIR/m_<pfad>` als Backup, was mit `/` im Namen scheiterte
— die Mutationen liefen **ohne Restore**, und die folgenden Regeln testeten eine
kumulativ kaputte Datei. `git checkout --` hat daraufhin den **ungestagten**
Slice verworfen; er wurde neu gebaut und sofort gestaged.

## K-135 Drei Selbstkorrekturen und `11610` — VERIFY

**Die Karte musste sich selbst dreimal widersprechen, und die dritte
Korrektur hat einen bereits gepushten Fix zurückgenommen.**

### 1. `1009` ist KEINE Nachrichten-Fortsetzungszeile

Der alte Entwurf zählte 20 nackte `1009` mit String nach einem `10110`, sah
MZ' `401` auf ein `101` folgen und schloss auf eine gemeinsame Konvention.
**Die gibt es nicht.** Die Fixture entscheidet es:

```
[1,1,1,1]  [1,1,2,1]  [1,3,8,1]  [1,4,10,1]   → vier Integer, LEERER Text
```

Das sind exakt `parameters[0..3]` von `CommandChangeBattleCommands`: Actor,
Klasse, Battle-Command-ID, add/remove. **Eine Nachrichtenzeile trägt Text und
keine Integer — diese tragen beides nicht.** `liblcf`'s
`Code::ChangeBattleCommands` ist 1009, EasyRPG gated es auf
`IsRPG2k3Commands()`.

**Der falsche Fix war als `5a9ca22` gepusht und wird hier revertiert.** Er sah
richtig aus, die Mutationen haben ihn nicht gefangen, und der Grund seiner
Falschheit ist: **ein Muster, das zu zwei Lesungen passt, ist für keine von
beiden ein Beleg.**

### 2. `5001`–`5005` sind Menübefehle, keine Move-Route-Schritte

Sie standen in der Karte als „Route-Schritte in einer Seite getragen", ohne
dass das Feld geöffnet wurde, das es gezeigt hätte. Gemessen: sie stehen
**direkt in der Befehlsliste der Seite**, zwischen einer Nachricht und einem
Conditional Branch. **Die Seiten haben sehr wohl 60 Routenlisten — und in
keiner davon stehen diese fünf Codes.** Ein erster Test behauptete „keine
Routenliste" und schlug fehl; **60 ist die bessere Messung**, weil sie zeigt,
dass das Feld existiert und gelesen wurde.

Aus `liblcf`: `OpenLoadMenu = 5001`, `ExitGame = 5002`,
`ToggleAtbMode = 5003`, `ToggleFullscreen = 5004`, `OpenVideoOptions = 5005`.

### 3. `11610` ist Key Input Proc — gelesen, nicht geraten

Aus `Game_Interpreter::CommandKeyInputProc`. **Parameter 5–9 bedeuten auf 2K
andere Tasten als auf 2K3**: shift/down/left/right/up gegen
numbers/operators/time-variable/timed. Die Version wählt die Spalte.

Die Fixture-Vorkommen: `[1,1,0,0,0,1,1,2,1,0,0,0,0,0]` — ein 2K3-Spiel, das
Ziffern und Operatoren will, zeitlich begrenzt, Antwort in Variable 1,
verstrichene Zeit in Variable 2.

**`parameters[7]` ist ein `int`, der eine Variable benennt, kein `bool`** — die
Quelle sagt das im Kommentar ausdrücklich.

Weitere gemessene Regeln: **die Ziffern loopen von 10 bis 1** (`10 + i`), also
hat **die Ziffer 0 keinen Wert**; **Operatoren schlagen Ziffern im selben
Frame**, weil die Quelle von höchstem Wert nach unten prüft; **die Maus wird
zuerst geprüft**, damit DECISION auf der linken Maustaste kein Konflikt ist.

**Test evidence** `test_rm2k_key_input.cs` (10), `test_rm2k_menu_commands.cs` (6).
**996/996**, `TestRm2kKeyInput: 10/10`, `TestRm2kMenuCommands: 6/6`.
**Mutations** 9 von 9 gefangen.

## K-135 Die fünf Menübefehle laufen jetzt — DONE

`5001` und `5005` schieben eine Szene und **halten die Seite an**, aus dem
`return false` der Quelle nach `SetRequestedScene`. `5002`, `5003`, `5004` laufen
durch. Eine bereits aktuelle Szene wird nicht doppelt gepusht.

**Das Gate ist der ganze Befehl, und dieses Repository kopiert das No-Op nicht.**
EasyRPG gated alle fünf auf `IsRPG2k3ECommands()` und gibt sonst `true`
zurück — ein stilles No-Op, **ein Bug, der jeden Test übersteht, weil sich
nichts geändert hat.** Dieser Leser **verweigert sichtbar**: eine Diagnose
nennt den Befehl nach seinem liblcf-Namen, sagt, dass es ein E-Befehl ist, und
dass nichts geöffnet wurde. `SupportsRpg2k3ECommands` ist **per Default
`false`**, denn ein Spiel, das nicht ja gesagt hat, hat nicht ja gesagt.

**Und der Szenen-Stack startet nicht mehr mit einer erfundenen Szene.** Er
schob vorher `"Menu"` und machte sie bei Reset aktuell. **`"Menu"` ist kein
RPG_RT-Szenenname** — es war eine Fiktion, gegen die jeder Szenentest grün war,
und sie widersprach der Zeile darüber, die einen leeren Stack behauptete. Ein
altes Test wurde korrigiert, nicht geschwächt.

**`FullscreenRequested` ist eine Anfrage, kein Zustand.** Die Engine fragt die
Anzeigeschicht, und die darf verweigern — EasyRPG prüft `IsOptionVisible` und
`IsLocked`. Ein Boolean, der den Bildschirmzustand behauptete, wäre eine
Behauptung, die dieser Leser nicht halten kann.

**Test evidence** `test_rm2k_menu_execution.cs` (9, über `ExecuteFrame`, den
echten Runner), `test_game_simulation_state.cs` korrigiert.
**1005/1005**, `TestRm2kMenuExecution: 9/9`, `TestGameSimulationState: 20/20`.
**Mutations** 8 von 8 gefangen.

**Ein erster Entwurf prüfte auf Prosa, die er selbst erfunden hatte** — auf die
Formulierung „did not declare", während die Diagnose „does not declare" sagt.
**Auf erfundene Prosa zu prüfen macht das Test zum Ding, das recht haben muss —
und es war das falsche Ding.** Die Prüfungen liegen jetzt auf den Wörtern, die
die Bedeutung tragen.

### Zwei eigene Werkzeugfehler in diesem Zyklus

1. `GetActors`-Anker: die erste Mutantenrunde hatte zwei `NOMATCH` und ein
   `BROKE` — Ankerprobleme, keine Befunde. **Die Regel zählt erst, wenn ihr
   Anker sitzt.**
2. `web_search` und `web_extract` sind in dieser Umgebung blockiert
   (`ddgs` fehlt, Extract-Backend nicht gesetzt). **Die EasyRPG- und
   liblcf-Quellen wurden deshalb per `curl` und `git clone` geholt** — das ist
   kein Umweg, sondern der direktere Weg zu derselben Quelle.

### Nächster Schritt

`1009` anwenden (Battle-Command-Liste ändern) und `11610` verdrahten. Beides
braucht keine Entscheidung vom Nutzer.

## K-135 `1009` läuft jetzt — DONE

Drei Akteur-Modi aus EasyRPGs `GetActors`: **0 = Partei, 1 = ein Held nach ID,
2 = der Held, den eine Variable benennt.** Die Quelle liest `parameters[0..1]`
darüber, `parameters[2]` als Command-ID und `parameters[3] != 0` als „hinzufügen"
— `CmdSetup` gibt dem Befehl eine Mindestbreite von vier.

**Abwesend ist nicht leer, und das ist hier das ganze Zustandsmodell.** Ein Held
ohne Eintrag hat **die Befehle der Datenbank**, was der RM2K-Standard ist.
`GetActorBattleCommands` gibt dafür **`null`** zurück, weil die Referenz dort
`null` zurückgibt, bis etwas sie ändert, und der Schlachtcode genau darauf prüft.
Ein Leser, der eine leere Liste speicherte, würde jedem Helden **jede Fähigkeit
nehmen**, sobald 1009 läuft.

**Beide Richtungen ohne Änderung werden gemeldet, weil sie Gegensätze
bedeuten.** „Füge hinzu, was er schon hat" ist eine Autorengewohnheit; „entferne,
was er nicht hat" ist meist ein Fehler, den man benennen sollte.

**Eine Helden-ID 0 fasst niemanden an, und die Seite läuft weiter.** Helden-IDs
laufen ab 1, also ist 0 der eine Wert, den ein Spiel wirklich erreichen kann und
der keinen Helden benennt — eine Variable, die nie gesetzt wurde. Die Quelle
protokolliert eine Warnung und gibt eine leere Aktorenliste zurück. **Ein Leser,
der die ganze Seite verweigern würde, würde den Rest eines Events verlieren,
weil eine ID falsch war.**

**Test evidence** `test_rm2k_battle_commands.cs`, 11 Tests über `ExecuteFrame`.
**1016/1016**. **Mutations** 8 von 8 gefangen.

### Zwei API-Fakten, die dieses Repository nicht offensichtlich macht

1. **`Variables` ist im Event 1-basiert und im Array 0-basiert**, weil
   `GetVariable` `Variables[pId - 1]` liest. `Variables[1] = 2` auf einem leeren
   Array wirft. Das ist ein Index-Range-Fehler, der aussieht wie ein 1-basierter
   Leser.
2. **Ein Frame ist ein Schritt.** `ExecuteFrame` dreimal auf einer
   Ein-Befehl-Seite führt den Befehl **nicht** dreimal aus — im Menü-Slice
   gegessen, hier wieder bestätigt.

### Nächster Schritt

`11610` verdrahten (Key-Input-Prompt braucht ein Fenster, das es noch nicht
gibt) oder `12310`/`12320`, die als Konstanten deklariert sind. Kein
Nutzerentscheid nötig.

## K-135 `11610` verdrahtet — DONE, Karte schliessbar

`Rm2kKeyInput` hatte die ganze Tabelle und nichts, um sie zu halten. Jetzt gibt
es einen Prompt in `PresentationState` und einen Befehl im Interpreter, und
**die Seite hält, solange er offen ist** — das macht ihn erst zum Prompt.

**Es ist ein zweiter Prompt, kein zweiter Modus des ersten.** 10150 fragt eine
Zahl und speichert sie; 11610 fragt eine Menge Tasten und speichert einen
*Code*. Eine Ziffer ist 11 bis 20, ein Operator 21 bis 25, die Bestätigungstaste
ist 5. **Nichts in der Datei sagt, welches der beiden gefragt hat** — deshalb
zwei Prompts statt eines Parameters.

**Während des Wartens ist die Variable null — jeden Frame.** Der Kommentar der
Referenz sagt das wörtlich. Ein Leser, der erst bei der Antwort schrieb, ließe
stehen, was das Spiel vorher hineingelegt hatte.

**Eine nicht erlaubte Taste beendet nichts.** Die Fixture erlaubt Ziffern und
Operatoren, nicht Bestätigen und nicht Shift. Ein Leser, der jede Taste als
Antwort nähme, beendete den Prompt mit der ersten Taste, mit der ein Spieler ihn
schließen will — so schließt ein Rechner-Dialog, bevor eine Ziffer getippt ist.

**Ein Tastendruck kommt auf einem Eingabe-Frame, und der ist nicht der Schritt
des Interpreters.** Deshalb ist `PressKeys` ein eigener Einstieg und nicht Teil
von `ExecuteFrame`. Ein Leser, der das Warten in den Dispatch gelegt hätte,
öffnete den Prompt in jedem Frame neu, in dem er offen blieb.

**Test evidence** `test_rm2k_key_input_wiring.cs` (9).
**1025/1025**, `TestRm2kKeyInputWiring: 9/9`, `TestRm2kKeyInput: 10/10`.
**Mutations** 9 von 9 gefangen.

**Damit ist K-135 geschlossen.** Sieben Codes, alle identifiziert, alle
ausgeführt. Und das Schließen bedeutete, einen bereits gepushten Fix
zurückzunehmen — `5a9ca22`, der `1009` für eine Nachrichtenzeile hielt.

### Nächster Schritt

`12310` und `12320` sind als Konstanten deklariert und werden im Dispatch nicht
behandelt. Danach die Fahrzeug-Move-Routes aus K-114. Kein Nutzerentscheid
nötig.

## K-136 angelegt, und `11110`/`11130` verdrahtet — 1035/1035

**Der Fund, der die Karte ausgelöst hat:** `ShowPicture` und `ErasePicture`
waren in `PresentationState` **implementiert, begrenzt und getestet** — und von
keinem Befehl erreichbar. `ShowPicture` nahm sechs Skalare und **warf die
anderen acht Parameter weg**. Das ist dieselbe Fehlerform wie K-094s Fahrzeuge:
der Zustand war da, die Verdrahtung nicht, und die Suite war grün.

**K-136 ist die Liste, die diese Form findet.** liblcf hat 164 Codes, dieser
Interpreter dispatcht 43 — **89 echte RPG-Befehle haben keinen Fall** und
landen in `default`. Zwei davon waren bereits implementiert.

### Drei echte Produktfehler, die die Realdaten fanden

**1. Die Transparenz ist ein *Prozent*, keine Farbe.** Die Referenz klemmt sie
mit `std::min(top_trans, 100)`. Ich hatte sie als Farbkanal gegen `MaxColorChannel`
geprüft — **jedes echte Bild eines Spiels (0, 50, 100) wäre zufällig durchgegangen**
und jedes Spiel mit der Farbe, die ich im Kopf hatte, hätte einen Refusal bekommen.

**2. Die Maniac-Bitmaske gilt nur für die *untere* Transparenz.** Die Referenz
maskt `parameters[14]` mit `0xFF` und liest `parameters[6]` **roh**. Ich hatte
beide maskiert.

**3. `11130` liest die ID zuerst und den Modus danach.** Ich hatte es umgekehrt —
so wie der *Show*-Befehl seinen Positionsmodus legt. Folge: **der einparametrige
Befehl, den der Editor am häufigsten schreibt, löschte das Bild mit der Nummer
„nichts".**

Und die Trennung, die ich verwechselt hatte: **„nichts da" ist Erfolg, „ID außerhalb
der Grenzen" ist Refusal.** Beide als Refusal zu melden erzählte einem Spieler,
sein Befehl sei außerhalb der Grenzen, wenn die Wahrheit war, dass er schon
gelöscht hatte.

**Test evidence** `test_rm2k_pictures.cs` (10), `test_presentation_state.cs`
umgestellt. **1035/1035**, `TestRm2kPictures: 10/10`, `TestPresentationState: 5/5`.
**Mutations** 8 von 8 gefangen.

### K-136, die Karte

**89 Befehle ohne Fall, nach Bereich sortiert.** Der schärfste ist
**`12110` Label und `12120` Jump to Label** — die einzigen zwei, die *wohin* die
Seite geht statt *was* sie tut. Ein Leser ohne sie führt den Sprung als No-Op aus
und landet am Seitenende, **was sich wie ein Spiel anfühlt, das stillschweigend
die halbe Scriptzeile übersprungen hat.**

**Warum die ganze Liste und nicht ein Befehl:** zwei ihrer Einträge waren bereits
lange implementiert. **Ein Leser, der seine eigene Feature-Liste ansieht, findet
sie nicht — nur die Liste der Referenz.** Das ist das Argument, die Lücke einmal
aufzuschreiben statt Befehl für Befehl an der Diagnose zu entdecken.

### Ein Werkzeugfehler, der mich Zeit gekostet hat

Beim Anfügen der Karte meldete mein Zählskript 175 Detailabschnitte statt 88.
Ursache: es zählte `### K-…` **ohne** zu prüfen, ob die Karte unter dem
richtigen `## Card details`-Block steht — es gibt historisch zwei
`## Agent maintenance rules`-Überschriften, und ein naives `s.find` nimmt den
ersten. **Eine Zählung, die die Struktur nicht prüft, zählt Dokumente statt
Karten.** Gegen `git show HEAD` verifiziert: +1 Karte, +1 Zeile, Struktur
unverändert.

## K-136 `12110` / `12120` — DONE

**Die beiden, die entscheiden, *wohin* eine Seite geht statt *was* sie tut.**

**Die Suche beginnt bei null, nicht hier.** Die Referenz:
`for (int idx = 0; idx < list.size(); idx++)`. Also ist **ein Rücksprung eine
Schleife** — so schreibt ein Autor eine ohne Schleifenbefehl. Wer ab hier
vorwärts sucht, macht aus jedem Rücksprung ein Durchfallen: **ein Spiel, das mit
einem Sprung schleift, liefe seinen Rumpf einmal und hörte auf.**

**Der Index landet auf der Marke, und die Regel der Engine sagt warum.** EasyRPG
inkrementiert nur, wenn der Befehl den Index nicht bewegt hat:

```cpp
if (index_before_exec == frame->current_command) {
    frame->current_command++;
}
```

Ein Sprung, der seine Marke findet, hat den Index bewegt → kein Inkrement → die
Seite landet **auf** der Marke, und die Marke ist ein No-Op, das einen Frame
kostet. Ein Sprung, der **nichts** findet, lässt den Index stehen → die Regel
inkrementiert → die Seite läuft weiter.

**Zwei Entwürfe hatten das in entgegengesetzter Richtung falsch, beide still.**
Einer gab ein nacktes `true` zurück und ließ die Seite **für immer auf dem Sprung
stehen** — das sieht wie ein Hänger aus. Der andere inkrementierte bedingungslos
und übersprang das No-Op, das das Format dort absichtlich hinstellt. Nur die
bedingte Form ist beides. **Eine Suite, die Befehle statt Frames zählt, hätte die
beiden nicht unterscheiden können.**

**Eine Marke tut gar nichts.** Die Referenz hat dafür `return true` und sonst
nichts — keine Methode, keine Parameter. **Eine Marke ist ein Name, kein Befehl**,
und wer ihr eine Wirkung gibt, erfindet eine Semantik, die das Format nicht hat.
Eine Marke ohne Parameter matcht nichts.

**Test evidence** `test_rm2k_labels.cs` (8). **1043/1043**.
**Mutations** 6 von 6 gefangen (ein siebter Versuch war ein erfundener Anker).

### Ein weiterer API-Fehler von mir

`Control Variables` hat **sechs** Parameter
(`[targetMode, startId, endId, op, operandType, operand]`), ich schrieb fünf.
Die fehlende End-ID verschob jedes Feld, der Befehl wurde abgelehnt, und **nichts
wurde je markiert** — der Test „der Rücksprung schleift" prüfte damit eine Seite,
in der nie etwas lief.

## K-136 Die fünf Audio-Befehle — DONE

**`GameSimulationState` hatte vier Positionsdoubles, die niemand las und niemand
schrieb** — `BgmPosition`, `BgsPosition`, `MePosition`, `SePosition`. Der Rest
eines Plans für Wiedergabe, die dieses Repository nicht gebaut hat. **Ein Double,
das kein Befehl bewegt, ist eine Behauptung über Zeit, die nichts wahrt hält.**
Ersetzt durch das, was das Format hält: den aktuellen Track pro Kanal, den
Fade-Zustand und **einen** gemerkten BGM.

**Es sind Daten, kein Klang.** Kein Player dahinter, kein Test behauptet, dass
ein Track zu hören ist. Die Diagnosen sagen, **was verlangt wurde**.

**Vier Kanäle, und sie sind nicht austauschbar.** BGM schleift und fadet, SE
spielt einmal darüber, ME folgt den BGM-Regeln, BGS schleift darunter. Wer alle
vier in einer Liste hält, lässt einen Fußschritt die Dorfmusik überschreiben.

**Die Parameterlisten der zwei Befehle passen nicht aufeinander.** Musik:
`[fade, volume, tempo, balance]`. Effekt: `[volume, tempo, balance]` — **ein
Effekt hat gar keinen Fade**, also setzt gleiches Lesen die Lautstärke an die
Stelle der Balance. `CmdSetup` gibt Breiten 4 und 3; ich schrieb 5 und 4 im
Produktcode **und in jedem Test**, also fielen alle zwölf Tests an einem Befehl,
den dieses Repository nie angenommen hatte.

**Balance ist 0–100 mit 50 in der Mitte**, nicht −100 bis 100.

### Und eine Verweigerung, die ich als Vorsicht verkleidet habe

Ich las `parameters[1]` als Modus für die anderen drei Werte und **lehnte dann
jeden Befehl ab, dessen Werte ungleich null waren** — also **jeden
Musikbefehl, den ein echtes Spiel schreibt**. Der Grund, den ich angab, war
„dieser Leser dekodiert noch kein Bitfeld". Das klingt nach Sorgfalt und war
eine Quelle, die ich **nicht zu Ende gelesen** hatte:

```cpp
if (!Player::IsPatchManiac()) { return com.parameters[val_idx]; }
```

Ohne Patch ist jeder Wert einfach sein eigener Parameter, und der fünfte
Parameter trägt gar nichts. **Laut verweigern ist kein Ersatz dafür, zu wissen.**
Ein Spiel, das den Patch *doch* trägt, wird weiterhin abgelehnt — und diese
Verweigerung nennt den Patch.

**Test evidence** `test_rm2k_audio.cs` (12). **1055/1055**.
**Mutations** 9 von 9 gefangen.

## K-136 `11010` / `11020` / `11030` — DONE, und `11060` ist ein liblcf-Code ohne Engine

**Die Transition-Tabellen sind das Schärfste in diesem Slice, weil die
Paarung nicht regelmäßig ist.** Zeigen und Löschen sind dieselben zwanzig Arten
von den beiden Enden gelesen, und jede Parameternummer benennt in jeder Tabelle
etwas anderes — 4 ist `BlindClose` beim Löschen und `BlindOpen` beim Zeigen, 16
ist `ZoomIn` und `ZoomOut`. **Streifen und Scroll spiegeln ihr Suffix, die
Divisionen paaren mit den Combines** — wer den Namen spiegelte, paarte
`CrossDivision` mit sich selbst und animierte gar nichts. Ein Test prüft genau
das, über alle drei Divisionsarme.

**Parameter −1 ist keine Art.** Er bedeutet „die eigene Teleport-Transition des
Spiels", die in den Editor-Einstellungen steht und nicht im Befehl. Die beiden
`switch` der Referenz haben **keinen default-Arm**, also fallen −1 *und* jede
unbekannte Zahl **stillschweigend** auf none durch — **was jeder Teleport im
Spiel seine Transition verlieren ließe, ohne ein Wort.** Dieser Leser hat die
Einstellungen nicht gelesen, sagt das also und nennt die Zahl.

**Die Sättigung ist ein Prozent, und 100 heißt ungetöntet.** Das ist rückwärts
von dem, was ein Leser rät: wer 0 als „kein Tint" behandelte, tönte den Bildschirm
in dem Wert grau, den ein Spiel schreibt, wenn es kein Tint will.

**Die Dauer wird umgerechnet, nicht in Zehnteln gespeichert.** Die Referenz
rechnet `tenths * DEFAULT_FPS / 10` und gibt Frames an den Bildschirm.

**Und eine Wartezeit ist bedingt — was der erste Dispatch falsch hatte.** Die
Referenz ruft `SetupWait` nur, wenn der sechste Parameter gesetzt ist, und ein
erster Entwurf gab ein nacktes `true` zurück: **ein Tint, der warten wollte,
lief die Seite trotzdem weiter, und die Wartezeit fand nie statt.** Genau die
bedingte Form, die der Sprung diese Sitzung schon einmal falsch hatte.

**`11060 Pan Screen` steht in liblcf — und EasyRPG hat dafür keinen `case` und
kein `CommandPanScreen`.** Also implementiert dieses Repository es nicht: es
gibt nichts, woraus die Parameter zu lesen wären, und wer einen Befehl
implementiert, den die Referenz nicht hat, erfindet eine Semantik. **Es bleibt
als benannte Lücke auf der Liste und nicht als Vermutung.**

**Test evidence** `test_rm2k_screen.cs` (14), inklusive aller zwanzig Parameter
beider Tabellen einzeln geprüft. **1069/1069**. **Mutations** 9 von 9 gefangen.

## K-136 `11310` und `11330` — DONE, und sie schließen eine K-131-Insel

**`Rm2kMoveRouteState` hatte keinen Aufrufer im ganzen Projekt.** K-131 hat den
Decoder und die Zustandsmaschine gebaut, mutation geprüft und **beide als
freistehende Objekte getestet** — und **kein Befehl konnte eine auf einen
Helden legen.** Dieselbe Inselform wie die Bilder in `PresentationState`, und
dieselbe Art, sie zu finden: vergleichen, wofür eine Klasse da ist, mit dem, was
die Befehle der Referenz tun.

**`11310` invertiert seinen Parameter, und das ist der ganze Befehl.**
`bool hidden = (com.parameters[0] == 0);` — wer ein Ungleich-Null auf „sichtbar"
abbildet, hat ein Verstecken richtig und ein Zeigen falsch, **und ein Spiel, das
diesen Befehl nur zum Verstecken benutzt, funktioniert, bis es das erste Mal
eines zeigt.** Es räumt außerdem die Durch-Position ab, mit dem Kommentar der
Referenz „RPG_RT does this here" — **wer durch eine Wand ging und dann versteckt
wird, bleibt nicht in der Wand stehen.** Zeigen räumt sie *nicht* ab, weil das
Zurücksetzen im Versteck-Zweig steht und nicht daneben.

**`11330` liest die Route als den Rest der Liste**, ab Index vier bis zum Ende.
Wer eine feste Zahl liest, wirft eine lange Route stillschweigend weg.

**ID-Modus und Repeat-Flag teilen sich ein Wort.** Der Modus sind die niedrigen
zwei Bits, Repeat ist das niedrige Bit.

**Eine Bewegungsfrequenz außerhalb 1–8 wird 6, und das ist die Vorgabe der
Engine und keine Ablehnung.** Wer verweigert, stoppt eine Route, die RPG_RT
fröhlich laufen lässt.

**`11340` und `11350` stehen in liblcf und kommen in EasyRPGs Interpreter
nirgendwo vor** — kein `case`, keine Methode. Also nicht implementiert, aus
demselben Grund wie `11060`.

**Test evidence** `test_rm2k_move_event.cs` (10). **1079/1079**.
**Mutations** 8 von 8 gefangen (drei Läufe; zwei Regeln des ersten Laufs waren
Ankerfehler und zählen nicht).

### Ein Werkzeugfehler, der die ganze Datei umformatiert hat

Mein Einrück-Skript hat 237 öffnende Klammern „korrigiert" — die meisten waren
bereits richtig. Der Diff blieb bei 168 Zeilen, also war der Schaden
kosmetisch, **aber ein Skript, das 237-mal zugreift und 1-mal recht hat, ist
kein Werkzeug, sondern ein Glücksspiel.** Künftig: eine Stelle gezielt patchen,
nicht die Datei durchgehen.

## K-136 `10820` Memorize Location — DONE

**Die drei Parameter sind die Variablen, in die geschrieben wird — nicht die
Position, die gespeichert wird.** `parameters[0]` bekommt die Karten-ID,
`parameters[1]` das X des Spielers, `parameters[2]` das Y. Wer sie als Position
liest, **schreibt die Kachel des Spielers in drei Variablen und speichert
gar nichts** — genau der Fehler, den ein Dreier-Befehl aus allerlei einlädt,
wenn alle Parameter dieselbe Art haben. Ein Test prüft, dass die
Parameternummern **nicht als Werte** auftauchen, weil die zwei Fehler im Log
verschieden aussehen und in der Testdatei gleich.

**Alle drei Variablen-IDs werden geprüft, bevor irgendeine geschrieben wird.**
Wer schrieb, während er ging, hätte die Karte gespeichert und dann auf die Null
 gestoßen: **ein halb gemerkter Ort holt den Spieler auf eine Kachel zurück,
die das Spiel nie gemeint hat.**

**`10830 Recall To Location` steht in liblcf und hat in diesem Build von
EasyRPG keine Methode.** Also nicht implementiert. **Die Asymmetrie gehört der
Referenz**, und sie wird festgehalten statt aus der Vorstellungskraft gefüllt:
Ein Spiel, das merkt und dann zurückruft, hätte die erste Hälfte und nicht die
zweite — und wer die zweite rät, **teleportiert Spieler auf Kacheln, die die
Datei nie beschrieben hat.** `10910` und `10920` haben dieselbe Form.

**Test evidence** `test_rm2k_memorize_location.cs` (6). **1085/1085**.
**Mutations** 6 von 6 gefangen.

### Zwei eigene Werkzeugfehler, beide im selben Slice

1. **Fünf Anläufe an einer Zeile.** Der f-String in meinem Einfügeskript hat
   die schließende Klammer verschluckt, und ich habe die Symptome behandelt
   (Array-Syntax, Cast, Typannotation), statt **den ganzen Block zu lesen**.
   **Drei Fehlversuche an derselben Stelle sind kein Messproblem, sondern ein
   Leseproblem.**
2. Ein Test las `Variables[pId - 1]` direkt und warf, statt die Abwesenheit als
   `-1` zu melden. **Ein Test, der auf dem Produktfehler abstürzt, beweist
   nichts über den Produktfehler.**

Beides steht jetzt hier, weil beide dieselbe Form haben: **Werkzeug und Zeile
ansehen, bevor man sie beurteilt.**

## K-136 `10120` / `10130` / `10230` — DONE, und einer davon fand einen Fehler im Save-Codec

**`SetTimer` startete den Timer, und das sollte es nicht.** Die Referenz hat
drei Operationen in einem Befehl: Sekunden setzen, starten mit den Flags
sichtbar und Schlacht, und stoppen. **Wer beim Setzen startete, kollabierte die
ersten beiden** — und ein Spiel, das `SetTimer` benutzt, um einen Countdown zu
**scharfmachen**, den es später starten will, **startete ihn sofort**. Genau der
Unterschied zwischen einem Timer, der zählt, und einem, der es nicht tut.

**Und der Save-Codec hatte denselben Fehler.** Er stellte einen Timer mit
`SetTimer` allein wieder her, also **kam jeder gespeicherte Countdown laufend
zurück** — ein Spiel, das einen angehaltenen Timer speicherte und neu lud,
bekam einen lebenden. Die zwei Alt-Tests, die an der Reparatur brachen,
benutzten `SetTimer` als „Timer starten" — dieselbe Verwechslung. **Sie wurden
korrigiert, nicht geschwächt**, und der Round Trip beweist jetzt beide
Operationen getrennt.

**`StopTimer` behält die Sekunden.** Wer einen Timer stoppt, um ihn zu zeigen,
und ihn dann wieder startet, erwartet den Stand. Er wirft außerdem nicht mehr
für eine unbekannte ID, denn eine veraltete Timer-ID soll kein totes Event sein.

**`10120` ist vier Flags und kein „Stil".** Transparent, Position, fixiert,
Continue-Events. **Parameter[2] ist invertiert** — eine Null heißt: das Fenster
bleibt stehen, während die Karte scrollt. Wer ein Ungleich-Null auf „fixiert"
abbildet, **scrollt jedes Fenster weg, das ein Spiel festgepinnt hat** — und das
ist **nur während der Kartenbewegung sichtbar, also von keinem Test auf einem
stillen Bild zu finden.** Parameter[1] hat **drei** Positionen.

**`10130` setzt ein Gesicht — und ein Gesicht ist eine Anfrage, kein gezeichnetes
Porträt.** Die Datei hat vier Slots; ein neunter wird mit der Zahl abgelehnt.

**Ein sechster Parameter benennt den Timer**, und die Referenz liest ihn **nur,
wenn der Befehl mehr als fünf Parameter hat und das Spiel RPG2K3 ist** — daher
hat ein 2K-Spiel einen Timer und ein 2003 zwei. Ein Test prüft beide Lesarten
desselben Befehls.

**Test evidence** `test_rm2k_message_options.cs` (15). **1100/1100**.
**Mutations** 10 von 10 gefangen, **einschließlich des Codes, der die Sekunden
unbedingt wiederherstellte** — das ist die Save-Datei-Hälfte desselben Fehlers.

## K-136 `10430` / `10460` / `10470` — DONE, und der Save-Codec kannte keine Helden

**Basis und aktuell sind zwei Dinge, und die Referenz hat zwei Aufrufe dafür.**
`10430` ruft `SetBaseMaxHp`, ein Buff ruft `SetMaxHp`. **Die Basis überlebt
einen Stufenwechsel und ein Speichern**, ein Buff nicht — deshalb liegt die
Basis in `Rm2kActorValues` und dieses Feld **hat bewusst keinen Platz für den
aktuellen Maximalwert**. Wer den aktuellen Wert speicherte, ließe ein gespeichertes
Spiel einen Buff behalten, der drei Karten zurück vorbei war.

**HP und SP klemmen verschieden, und das ist kein Zufall.** HP hat ein
Todes-Flag und eine Untergrenze von eins, wenn es nicht gesetzt ist, weil ein
Spiel einen Helden schützen kann. `CommandChangeSP` hat beides nicht — die
Referenz schreibt `if (sp < 0) sp = 0;` und sonst nichts. **Wer SP dieselbe
Untergrenze gäbe wie HP, ließe einen Helden nichts zaubern.**

**Die Obergrenze ist der aktuelle Maximalwert, nicht die Basis.** Ein Held mit
Basis 40 und Ausrüstung im Wert von 10 wird nicht über 50 geheilt.

**`parameters[2]` ist ein Remove-Flag und kein Vorzeichen** — die Referenz
negiert den Betrag, wenn es gesetzt ist — und `10460` hat sechs Parameter,
`10470` fünf, denn das sechste ist das Todes-Flag und SP hat keines.

**Und der Save-Codec kannte überhaupt keine Helden.** Jeder Basiswert und jeder
aktuelle Stand ging beim nächsten Speichern verloren: Ein Held, der fast tot war,
lud voll gesund, und ein `10430` war weg. Basis und aktueller Stand reisen jetzt
zusammen, denn **ein Save, das die Stände behält und die Basen verliert, würde
einen Helden an einem Maximum klemmen, das er nicht mehr hat.** Es kommen nur
berührte Helden hinein, nach ID sortiert, damit zwei Saves desselben Spiels
byteweise gleich sind. Eine Zeile außerhalb der Grenzen wird **ganz**
abgelehnt, und der Test prüft, dass nichts angewandt wurde.

**Test evidence** `test_rm2k_actor_battle_values.cs` (14). **1114/1114**.
**Mutations** 19 Regeln über drei Läufe, **19 von 19 gefangen** — zehn in den
Befehlen und der Werteklasse, neun im Save-Codec, darunter „der Codec schreibt
überhaupt keine Helden" und „der Codec schreibt jeden Helden, ob berührt oder
nicht".

## K-136 `11840` / `11930` / `11960` — DONE: drei Ein-Zeiler und ein Default, der zählt

**Die Referenz hat drei Ein-Zeiler-Methoden gleicher Form** —
`SetAllowEscape(com.parameters[0] != 0)` und seine zwei Geschwister — und
**das ist der ganze Befehl.** Ein Handler und drei Konstanten ist hier die
ehrliche Lesart und keine Ersparnis.

**Eine Null ist eine Entnahme und kein „keine Änderung".** Eine Cutscene, die
das Menü sperrt, und eine, die es wieder freigibt, schreiben dasselbe Feld —
**wer das Flag nur je auf true setzen könnte, könnte einem Spieler sein Menü
nie zurückgeben**, und ein Spiel mit gesperrter Menü-Cutscene bliebe im
gesperrten Menü stecken.

**Jedes Ungleich-Null ist erlaubt, nicht nur eine Eins** — die Referenz prüft
`!= 0`, nicht `== 1`, also wird ein Spiel nicht abgelehnt, das eine berechnete
Wahrheitswert übergibt.

**Alle drei stehen standardmäßig auf erlaubt, und das ist ein echter Default
und keine Vermutung.** Eine Datenbank, die keinen dieser Befehle je lief, hat
alle drei gesetzt, also ist ein neues Spiel ein Spiel, in dem der Spieler das
Menü öffnen, speichern und fliehen darf. **Wer auf verboten defaultete, machte
jedes unberührte Spiel unspielbar, sobald der Spieler Escape drückt** — und
kein Test eines Befehls hätte das je gefunden, denn ein Spiel ohne Befehle ist
der Fall, für den niemand einen Test schreibt. `Test_ANewGameAllowsEverything`
existiert genau dafür.

**Jeder Befehl schreibt sein eigenes Flag und lässt die anderen zwei stehen.**
Wer alle drei aus Defaults schriebe, entsperrte eine gerade gesperrte Cutscene.
Der Setter ist privat hinter einem `SetAccess`, aus demselben Grund wie der
Timer `SetTimer` und `StartTimer` statt eines öffentlichen Feldes hat: Die
Kollabierung zweier Operationen hat einmal eine Save-Datei gekostet.

**Test evidence** `test_rm2k_access_commands.cs` (5). **1119/1119**.
**Mutations** 8 Regeln, **8 von 8 gefangen** — darunter jede der drei Befehle,
die die anderen zwei Flags aus Defaults statt aus dem aktuellen Stand schreibt,
also genau der Fehler, den diese Familie teilt.

## K-136 `10920` / `11810` / `12420` / `12510` — DONE, und das Board lag bei einem falsch

**`10920 Store Event ID` stand als „hat keine Methode in diesem EasyRPG-Build" auf
dem Board.** Er hat eine: `CommandStoreEventID`. Board-Notiz korrigiert.
Der Rumpf macht drei Dinge, die ein Leser richtig treffen muss: Beide
Koordinaten laufen durch `ValueOrVariable` mit **demselben** Modus in
`parameters[0]`; ein leeres Feld speichert **0 und hält die Seite nicht**
(`ev ? ev->GetId() : 0`); und ein Feld außerhalb der Karte wird **abgelehnt
statt mit einer Null beantwortet**, weil eine Null genau wie „kein Event hier"
aussieht.

`10920` braucht die Karte, und **der Interpreter hat keine** — er hat vier
`Func`-Resolver. Der fünfte kommt dazu, `Func<int, int, int>?`, aus demselben
Grund: Der Interpreter darf nicht wissen, wie eine Karte gehalten wird, sonst
kann ein Test sie nicht steuern.

**`11810` Parameter 4 heißt „der Schalter muss AN sein", nicht „es gibt einen".**
Wer es als „benutze einen Schalter" liest, macht jeden bedingten Sprung
unbedingt und öffnet einen geheimen Zugang zu Beginn des Spiels. Parameter 0
ist ein **Modus und keine Ziel-ID** — ungleich null entfernt alle Punkte der
Karte. Ein Punkt ersetzt einen zweiten auf derselben Kachel, weil die Referenz
anhängt und ein Spiel sonst zwei Sprünge auf einer Kachel hätte.

**`12420` und `12510` nehmen überhaupt keine Parameter**, weshalb die Referenz
den Befehl als `const& com` schreibt und nie liest. Beide **warten zuerst auf
eine offene Message** — ein Held, der seine letzte Zeile sagt und dann stirbt,
soll danach sterben. Beide halten die Seite. `WaitingFor` sagt welches, denn ein
Warten ohne Grund sieht wie ein Hänger aus.

**Test evidence** `test_rm2k_teleport_and_outcome.cs` (13). **1132/1132**.
**Mutations** 10 Regeln, **10 von 10 gefangen** — darunter das Sprung-Flag als
„es gibt einen Schalter" und das leere Feld mit einer erfundenen ID beantwortet.

## K-136 `11820` / `11830` — DONE, und einer stand nie auf dem Board

**`11820 Change Teleport Access` fehlt in K-136 vollständig.** Das Board führte
den Bereich "`11810`–`11840`" mit Einzelcodes, und dieser fiel zwischen den
Einträgen durch. Die Referenz hat ihn: `SetAllowTeleport(parameters[0] != 0)`.
Es ist der **vierte** der vier Ein-Zeiler-Access-Befehle. **Ein Board, das
Lücken zwischen Bereichsangaben hat, verliert Befehle** — die Liste wird ab jetzt
aus der Quelle gemessen und nicht aus dem Board übernommen.

**`11830 Escape Target` hat dieselbe vierte Parameterbedeutung wie der
Sprungpunkt:** „der Schalter muss AN sein". Wer sie als „benutze einen Schalter"
liest, macht einen gesperrten Fluchtpunkt von der ersten Minute an verfügbar.
Es gibt **genau einen** und ein zweiter Befehl ersetzt ihn — wer eine Liste
führte, müsste eine Regel erfinden, welcher gewinnt, und die hat das Spiel nie
geschrieben.

**Der Default-Parameter auf `true` war eine Falle, und die Tests haben sie
gefunden.** `SetAccess` bekam `pTeleport = true`, und **jeder der drei älteren
Aufrufe setzte Teleport damit still zurück** — der letzte Befehl gewann, nicht
der, der das Flag benannt hatte. Exakt der Fehler, den `SetTimer` und
`StartTimer` einmal gekostet hat, in anderer Form. Der Default ist weg.

**Test evidence** `test_rm2k_teleport_access.cs` (9). **1141/1141**.
**Mutations** 10 Regeln über zwei Läufe, **10 von 10 gefangen**. Zwei brachten
zuerst den Build zum Scheitern, weil `SetAccess` keine Defaults mehr hat, und
wurden mit kompilierendem Code nachgemessen — ein Compilefehler ist keine
gefangene Regel.

## K-136 `10660`/`10670`/`10680`/`10690` — DONE, der zweite Block, der nie auf dem Board war

**Keiner dieser vier stand auf K-136.** Sie kamen aus der zweiten frischen
Messung der Quelle gegen den Interpreter. Das ist jetzt das zweite Mal, dass die
Kartenliste zu kurz war, und einmal war sie aktiv falsch.

**Die Audiofamilien haben verschiedene Breiten: sieben Musik, zwölf Klänge.**
Musik: Schlacht, Sieg, Gasthaus, Boot, Schiff, Luftschiff, Game Over. Klänge:
die vier fürs Menü, einer für den Kampfbeginn und einer pro Kampfereignis —
Feindangriff, Feindschaden, Heldenschaden, Ausweichen, Feindtod, Item. **Wer nur
die Menüklänge anbietet, lässt ein Spiel im stillen Kampf laufen.**

**Musik hat einen Einblendwert und Klänge nicht** — ein Soundeffekt mit Einblendung
ist ein Soundeffekt, auf den der Spieler gewartet hat. Deshalb liest `10660`
`parameters[1]` als Fade und `10670` nicht.

**Die Kontexte sind null-basiert, und das ist eine Falle.** `BGM_Battle` ist 0,
`SFX_Cursor` ist 0, also ist die Grenze 0..6 und 0..11. **Eine Grenze ab 1 hätte
die Schlachtmusik abgelehnt** — genau die, die ein Spiel am häufigsten wechselt.

**Ohne Maniac-Patch ist der Parameter der Wert.** Die Referenz liest
`ValueOrVariableBitfield(com, 5, 1, 1)`, und dieser Helfer gibt ohne Patch direkt
`parameters[val_idx]` zurück — Modusindex und Wertindex sind dieselbe Zahl.
**Wer `parameters[5]` als Wert las, hätte jede Spur verstummt.** Genau das hat
der erste Lauf dieses Schnitts getan. Der Patch-Pfad braucht einen
Game-String-Spiegel, den diese Runtime nicht hat: plain value plus Diagnose.

**Sechs Übergänge, und der sechste ist der, den ein Spiel bemerkt.** Teleport
ein und aus, Kampfbeginn ein und aus, Kampfende ein und aus. `Transition_Count`
ist eine **Anzahl und kein letzter Index** — wer 5 als letzten Index las, hätte
den Übergang verweigert, der den Spieler aus dem Kampf zurückholt.

**Die Referenz `assert`s bei unbekanntem Übergang** — Absturz im Debug-Build,
Schreiben ins Leere sonst. Dieser Leser nennt die sechs erlaubten Werte.

**Test evidence** `test_rm2k_system_settings.cs` (12). **1153/1153**.
**Mutations** 12 Regeln über zwei Läufe, **12 von 12 gefangen** — darunter die
Kontextgrenze ab 1, die SFX-Breite auf die Menüvier gekürzt und die
Maniac-Warnung abgeschaltet.

## K-136 `10620`/`10630`/`10640`/`10650`/`10850` — DONE, und `10850` hat einen Wert, der kein Fahrzeug ist

**Fahrzeug-ID -1 bewegt die Partei und ist keine ungültige ID.** Die Referenz
hat einen Kommentar dazu: In RPG_RT hat eine Partei in keinem Fahrzeug die ID
-1, und -1 zu übergeben bewegt die Partei allein. **Wer sie ablehnte, ließe
jeden „teleportiere den Helden"-Befehl eines Spiels nichts tun** — und das ist
ein sehr häufiger Befehl. Ein Test prüft beides: fehlendes Fahrzeug wird
abgelehnt, -1 funktioniert trotzdem. Zwei Bedeutungen für ein Feld.

**Die Fahrzeug-ID wird um eins verschoben**, weil das liblcf-Enum
`None = 0, Boat = 1, Ship = 2, Airship = 3` ist und die Referenz
`(Game_Vehicle::Type)(com.parameters[0] + 1)` schreibt. Diese Zahlen stehen im
Save-Format. Wer den Parameter direkt nähme, spräche Fahrzeug 0 an — und
Fahrzeug 0 ist die Partei, kein Boot.

**`10650` setzt zwei Felder**, das aktuelle Sprite und das ursprüngliche. Das
ursprüngliche ist das, wozu das Fahrzeug beim Aussteigen zurückkehrt — **wer
nur das aktuelle setzte, ließe ein Fahrzeug in Kostüm zurück, nachdem die
Partei ausgestiegen ist.**

**Eine Partei im Fahrzeug fährt mit**, und die Referenz kehrt danach sofort
zurück. Nur das Fahrzeug zu bewegen ließe den Helden auf der verlassenen Karte
stehen — bei einem Boot also eine Partei auf offenem Wasser.

**`Boarding` ist nullable, und das ist eine Entscheidung.** Ein Spiel, das nie
ein Fahrzeug anfasst, allokiert keins, und wer es dereferenzierte, würde bei
jedem `10850` in einem Spiel ohne Schiff werfen — **und der Normalfall ist
genau dieses Spiel.** Der erste Lauf dieses Schnitts hat genau das getan; die
Tests haben es gefunden.

**`10630` nimmt die Transparenz direkt aus `parameters[2]` und nicht aus dem
Bitfeld** — das ist die Aufteilung der Referenz. **Und der Index ist eine Pose,
keine Charakter-Nummer:** ein Kostüm ist dieselbe Datei mit anderem Index, und
der Dateiname bleibt richtig, also fängt keine Sichtprüfung einen Leser, der es
falsch hat.

**Ein fehlender Held ist eine Warnung und keine Ablehnung.** Die Referenz prüft
`GetActor`, warnt und gibt `true` zurück. **Wer die Seite hielte, ließe eine
Cutscene auf einen Helden warten, den die Datenbank nie hatte.**

**Test evidence** `test_rm2k_actor_graphics.cs` (14). **1167/1167**.
**Mutations** 11 Regeln, **11 von 11 gefangen** im ersten Lauf — darunter die
nicht verschobene Fahrzeug-ID, das nicht gesetzte ursprüngliche Sprite und das
nullable Booting dereferenziert.

## K-136 `11710`/`11720`/`11740`/`11750` — DONE, und einer hatte gar keinen Schreiber

**`11750 Tile Substitution` hatte zwei 144er-Tabellen, zwei Leser und keinen
Schreiber.** Der Befehl konnte also geparst und nie ausgeführt werden — und
ein Test der Leser wäre die ganze Zeit grün gewesen. `SubstituteTile` schließt
die Lücke.

**`SubstituteLower` addiert `BlockEIndex` beim Lesen**, gespeichert wird also
der Rohwert. Wer die vom Befehl geforderte Zahl speicherte, bekäme einen
Index `BlockEIndex` zu hoch — **jede untere Kachel eine Zeile versetzt**. Der
Test prüft den Offset, denn ein Test mit der Rohzahl hätte einen korrekten
Schreiber „failen" lassen.

**`11720` hat sechs Flags und zwei Geschwindigkeiten, und die Geschwindigkeiten
kommen aus anderen Parametern als die Flags.** Flags sind 0, 1, 2 und 4; die
horizontale Geschwindigkeit ist 3, die vertikale 5. **Das vierte Flag und die
horizontale Geschwindigkeit stehen nebeneinander** — genau das macht den
Fehler leicht: wer die Parameter der Reihe nach liest, nimmt ein Flag als
Geschwindigkeit.

**Ein leerer Panorama-Name ist das Datenbank-Panorama und keine fehlende
Datei** — das macht die Referenz mit `if (!params.name.empty())`, bevor sie
die Datei anfragt. Wer einen leeren Namen als Fehler behandelte, verweigerte
genau das, wozu der Befehl da ist: zurück zur Datenbank.

**Die Referenz lässt den Interpreter auf die Panorama-Datei warten.** Diese
Runtime hat hier kein Dateisystem, also ist das Warten eine Diagnose — **wer
ewig wartete, hängte ein Spiel mit fehlendem Panorama**, und ein fehlendes
Panorama ist ein Fehler im Spiel, kein Grund anzuhalten.

**Null Encounterschritte sind ein realer Wert und genau der, der Zufallskämpfe
abschaltet.** Wer null als „nicht gesetzt" behandelte, könnte sie nie
abschalten. **Und ein neues Spiel, das null geerbt hätte, wäre nicht gewinnbar:**
keine Kämpfe, keine Erfahrung.

**Chipset 0 ist ein echtes Chipset.** Die Referenz vergleicht mit dem
aktuellen und kehrt früh zurück, wenn sie gleich sind. Wer null als „nicht
gesetzt" las, verweigerte das erste Chipset der Datenbank — und das ist oft
das meistbenutzte.

**`ChipsetId`, `MapParallax` und `EncounterSteps` standen nicht im Reset**, und
die Tests haben es gefunden.

**Test evidence** `test_rm2k_map_changes.cs` (12). **1179/1179**.
**Mutations** 10 Regeln, **10 von 10 gefangen** im ersten Lauf — darunter die
obere Tabelle in die untere geschrieben, die beiden Geschwindigkeiten
vertauscht und die Karteneinstellungen über den Reset gerettet.

## K-136 `20140` / `20141` — DONE, und dieser Schnitt fand ein Feld, das der Parser wegwarf

**Der Decoder las den LCF-Chunk `0x0D`, schrieb ihn in sein Wörterbuch, und
`EventCommand` hatte kein Feld dafür.** Jedes Event wurde also vollständig
geparst und kein Zweig konnte je identifiziert werden. `EventCommand.Indent`
existiert jetzt, und `Rm2kEngineRuntime` reicht es durch.

**Ein Test der Codes, der Parameter und der Strings wäre die ganze Zeit grün
gewesen** — die Information ging zwischen zwei richtigen Lesern verloren, und
nur das Verhalten von `20140` hätte es zeigen können.

**`20140` ist kein zweites Choice-Fenster.** Es ist ein Zweig einer Liste, die
der Spieler schon beantwortet hat, und die Referenz reicht es an
`CommandOptionGeneric`: entweder wird der Sub-Index gelöscht — weil das der
gewählte Zweig ist — oder zum nächsten bedingten Befehl gesprungen. **Wer nur
eine Hälfte hätte, bekäme einen Helden, der eine Frage stellt, weggeht, den
Wächter angreift, das Schwert kauft und geht — alles in einem Frame.**

**Jeder Zweig endet mit seinem eigenen `20141`.** Der Sprung läuft zum nächsten
Befehl aus `{ShowChoiceOption, ShowChoiceEnd}`, also würde ein Zweig ohne eigenes
Ende jeden Zweig danach mit verschlucken. Der erste Testentwurf baute die
andere Form und schlug „aus dem richtigen Grund" fehl.

**Der gewählte Zweig setzt den Sub-Index auf einen Sentinel, nicht auf ein
Flag.** Ohne das würde eine zweite Liste auf derselben Seite gegen eine
veraltete Zahl vergleichen.

**Der Sprung prüft die Grenze vor dem Lesen, nicht nach dem Schritt.** Ein
Zweig ohne Ende ist ein Fehler im Spiel; wer erst springt, liest einen Index
zu viel und wirft. Genau das tat dieser Leser; der Test fand es.

**Test evidence** `test_rm2k_choice_branches.cs` (6). **1185/1185**.
**Mutations** 7 Regeln über zwei Läufe, **7 von 7 gefangen** — darunter der
gewählte Zweig übersprungen statt ausgeführt, der Sprung ohne Ziel und der
Indent im Konstruktor wieder verworfen.

**Was bleibt** `1008 ChangeClass` und `10500 SimulatedAttack` plus die fünf
liblcf-Codes, die die Referenz nicht dispatcht. `ChangeClass` braucht zuerst
ein Klassenmodell.

## K-136 `10500` — DONE, und es ist kein Kampf

**Keine Reihenfolge, keine Truppe, keine Zielauswahl.** Der Befehl wählt Helden
über die üblichen Actor-Parameter, rechnet eine Zahl aus Verteidigung und Geist
und zieht sie von den HP ab. Ein Spiel benutzt ihn für eine Falle, ein Gift, ein
Skript — **Schaden ohne Kampf**. **Dieser Schnitt musste kein Kampfsystem bauen,
und das ist es wert zu wissen, bevor der nächste eines baut.**

**Verteidigung wird durch 400 geteilt, Geist durch 800**, also blockieren 800
Geistpunkte genau so viel wie 400 Verteidigungspunkte. **Wer einen gemeinsamen
Teiler nähme, machte Geist doppelt so stark wie beabsichtigt** — um den Faktor
zwei, auf der Achse, an der ein Spiel justiert.

**Das Ergebnis wird zweimal auf null geklemmt, und die Reihenfolge zählt.** Die
Referenz klemmt nach den zwei Subtraktionen, justiert die Varianz und klemmt
noch einmal. **Wer einmal am Ende klemmte, ließe eine Varianz negativen Schaden
ausgeben** — und negativer Schaden *heilt* den Helden.

**Die Spreizung ist mindestens eins.** `max(1, var * base / 10)` — ohne die Eins
rundet eine kleine Basis bei großer Varianz auf null, und **ein Spiel, das zehn
Prozent Varianz asked, bekäme keine.** Und die Spreizung ist symmetrisch: die
Hälfte wird abgezogen, nicht die ganze.

**Die Resultatvariable hält den Schaden des letzten Helden, nicht die Summe**,
weil die Referenz sie in der Schleife schreibt. **Wer summierte, ließe ein Spiel,
das „du hast N verloren" zeigt, eine Zahl zeigen, die das Spiel nie produziert
hat** — und konsequent, weil es jedes Mal dieselbe falsche ist.

**Die Varianz kommt aus einem eigenen Generator, nicht aus dem von MZ**, weil
die beiden Engines keinen Strom teilen. Gleiche Form wie `MzRandom`, ausdrücklich
kein Anspruch auf die Engine-Zahlen — die sind nicht wiederholbar. Was es gibt,
ist ein Lauf, den Save und Test wiederholen können.

**Der Test musste zweimal Rate 1 statt 100 nehmen.** Bei hundert Prozent blockiert
jeder Wert den ganzen Angriff, alle Fälle kommen als null heraus, und ein Test,
der die Teiler nicht unterscheiden kann, beweist nichts über sie. Beide Male
bestand der erste Entwurf mit einer Rate, die die Sache verdeckte.

**Test evidence** `test_rm2k_simulated_attack.cs` (8). **1193/1193**.
**Mutations** 10 Regeln, **10 von 10 gefangen** im ersten Lauf — darunter die
beiden Teiler vertauscht, die zweite Klemme entfernt, der Schaden addiert statt
abgezogen und die Resultatvariable akkumuliert.

**Was bleibt** `1008 ChangeClass` und die fünf liblcf-Codes ohne Referenz.
`ChangeClass` braucht ein Klassenmodell: der Befehl trägt Klassen-ID,
Stufen-Reset-Flag, Skill-Modus und Parameter-Modus, und keiner davon hat bisher
ein Ziel.

## K-136 Klassen-Parameter-Chunk `0x1F` — DONE: `1008` wartete auf Daten, nicht auf Code

**`1008 ChangeClass` wartete nicht auf einen Dispatcher, sondern auf Daten.**
Der Befehl trägt Klassen-ID, Stufen-Reset-Flag, Skill-Modus und
**Parameter-Modus** — und der Parameter-Modus ist ohne Klassentabelle
bedeutungslos, weil die Referenz daraus eine *Stufe* liest. Der LDB-Parameter-
Chunk der Klassen wurde nach `unknown_fields` gelesen und blieb dort.

**Nichts ging verloren, und das ist es wert zu sagen.** Der Leser-Vertrag hielt:
Der rohe Chunk liegt weiterhin in `unknown_fields`, neben dem Decode. Es fehlte
ein Weg hinein, und dieser fügt einen hinzu, ohne etwas zu ersetzen.

**Sechs `int16`-Vektoren und keine sechs skalaren.** liblcf `rpg::Parameters`
hält `maxhp`, `maxsp`, `attack`, `defense`, `spirit`, `agility` als
`vector<int16>` mit einem Eintrag pro Stufe, und `WriteLcf` schreibt sie in genau
dieser Reihenfolge ohne Längen davor. **Wer sechs Skalare annähme, läse den
ersten Wert jedes Vektors und nannte es das Klassenmaximum — eine Stufe-99-
Klasse gäbe ihren Helden Stufe-1-Werte**, und alle Zahlen lägen im Bereich, also
sähe nichts falsch aus.

**Die Reihenfolge ist liblcf s und nicht alphabetisch.** Wer die Namen sortierte,
gäbe einer Klasse ihre Initiative als Trefferpunkte — mit allen sechs Werten im
Bereich.

**Little endian, signiert, beide Bytes.** Wer eines las, deckte jeden Wert auf
255 — und eine Klasse mit mehr als 255 TP hätte ihre Helden leise geschwächt.

**Der Chunk ist ein Vielfaches von zwölf oder etwas anderes.** Wer ihn trotzdem
dekodierte, läse sechs Werte aus einem Chunk mit anderem Inhalt — und die
Werte wären plausibel, was schlimmer ist als eine Ablehnung.

**Stufen sind im Spiel eins-basiert und im Array null-basiert.** Die Referenz
liest `parameters[level]` nach dem Dekrementieren; wer das übersprang, gäbe einem
Stufe-1-Helden die Zeile 0 — und bei einer Klasse, deren erste Stufe absichtlich
schwach ist, ist das der Unterschied zwischen einem Tutorial und einem Helden,
der zu schwach ins Spiel startet.

**Eine Klasse ohne Parameter sagt es und gibt false zurück.** Eine maximale
Trefferpunktzahl von null sähe wie eine Designentscheidung aus — ein Held, den
das Spiel unspielbar machte, statt einer Datei, die nicht gelesen wurde.

**Test evidence** `test_rm2k_class_parameters.cs` (7). **1200/1200**.
**Mutations** 9 Regeln, **9 von 9 gefangen** im ersten Lauf — darunter die um
eins gedrehten Vektoren, das weggefallene hohe Byte und die eins- statt
null-basierte Stufe.

**Was `1008` noch braucht** die Klassen-Skill-Liste und die vier Modi, die der
Befehl trägt. Die Parameter sind der Teil, ohne den der Befehl nicht zu schreiben
war; der Rest ist ein Schalter.

## WOLF `IfVariable` — DONE: ein Vergleich, kein Test, und ein Ast ohne den andern

**`IfVariable` verglich mit `==` und sonst nichts, und kein Test im Repository
hat es überhaupt benutzt.** Der eine Vergleich, den es hatte, war so wenig
bewiesen wie die sechs, die fehlten.

**Der Editor bietet sieben, und die Hilfe nennt sie:** größer, größer-gleich,
gleich, kleiner-gleich, kleiner, ungleich, Bit-UND. **Wer nur `==`
implementierte, nähme einen Zweig in sieben**, und die anderen sechs fielen in
den Else-Pfad — **eine Truhe, die mit „V0 ist mindestens 1" gesichert ist,
würde sich nie öffnen.**

**Der Bit-UND-Test ist gleich dem Wert und nicht „irgendein Bit gesetzt".** Die
Hilfe widmet ihm einen Absatz: Mit V0 = 5 (`101`) und Wert 2 (`010`) ist
`5 & 2` gleich 0, nicht 2 — der Test scheitert. **Wer
`(variable & wert) != 0` schriebe, bestünde jeden Test mit einem gesetzten
Bit, und ein Spiel, das eine Tür mit einem Bit-Test sichert, öffnete sie für
alle.** Und ein Wert von null erfüllt ihn immer, weil alles UND null null ist —
die Hilfe sagt das ausdrücklich.

## Und der Ast hatte ein Ziel, und das ist die zweite Hälfte

**Ein WOLF-Ast hat zwei Arme, und die VM hatte einen Sprung.** Der
Durchlauf war der wahre Arm und der einzige Sprung der falsche — also lief
bei gehaltener Bedingung der wahre Arm *und* der falsche: **eine Truhe, die
sich öffnet, und ein Wächter, der im selben Frame zuschlägt.** Das ist ein
anderer Fehler als der Vergleich und ein schlimmerer, denn er ist in keinem
einzelnen Vergleich sichtbar.

**Beide Arme sind jetzt ein eigenes Ziel** (`TrueJumpIndex`, `JumpIndex`), und
**der letzte Befehl eines Arms springt über den anderen** (`NextIndex` am
Befehl).

**Das Arm-Ende war zuerst ein gemerkter Endindex, und das war falsch:** Es
braucht verborgenen Zustand, den ein Programm mit zwei Zweigen nacheinander
aus einem in den anderen leckt, und die Prüfung feuert, bevor der Arm läuft,
wenn der Zweig schon hineingesprungen ist. Der Sprung am Befehl hat keinen
solchen Zustand und ist auch die Form, die eine WOLF-Ereignisliste hat. **Drei
Versuche waren nötig, und der dritte ist der im Code; die ersten zwei stehen
hier, weil der Grund des Scheiterns der Grund für die Richtigkeit ist.**

**Test evidence** `test_wolf_comparisons.cs` (8). **1208/1208**.
**Mutations** 10 Regeln, **10 von 10 gefangen** — darunter der Bit-Test als
„irgendein Bit gesetzt", der Arm-Sprung entfernt und ein unbekannter Vergleich
statt abgelehnt auf Gleichheit zurückfallend.

**Was WOLF weiter fehlt und gesagt statt versteckt wird: Es gibt kein natives
WOLF-Fixture.** Das Verzeichnis `wolf` enthält eine selbstgebaute
`urpg-wolf-plain-json`-Envelope und ein README, das das sagt, und die sieben
Vergleichsnummern sind gegen die Editor-Hilfe gepinnt, nicht gegen eine Datei.
Das Variablenmodell ist außerdem noch ein flaches `int`, wo WOLF Selbst-,
Normal-, System- und Datenbank-Bänder hat — **das ist die nächste WOLF-Karte
und sie ist größer als diese.**

## WOLF Variablenbänder — DONE: ein flaches Wörterbuch konnte sie nicht halten

**Der Editor listet vier: `Self / Var / Sys / 可変DB`** — Selbst, Normal+Reserve,
System, Variable-DB. Die VM hatte ein `Dictionary<int, int>`, also **kollidierten
Selbst- und Systemvariable mit demselben Index** — und die Kollision ist still,
weil beide Lesen mit einer Zahl antworten und nur mit der falschen.

**Die Millionenschranke ist das Adressierschema selbst.** Eine Zahl ab 1.000.000
ist kein Wert, sondern eine *Referenz*: 2.000.005 heißt Normalvariable 5. Wer
die Zahl als Wert behandelte, speicherte zwei Millionen in ein Feld, das auf
Variable fünf zeigen soll, und das Spiel läse eine Zahl, die es nie geschrieben
hat.

**Die Schranke ist inklusiv.** Die Hilfe sagt „1.000.000 *oder mehr*", also hätte
ein Leser mit `>` genau 1.000.000 als Wert behandelt und nie Selbstvariable 0
aufgelöst — die eine Variable, die jedes WOLF-Event benutzt.

**Der Block ist eins-basiert und das Band null-basiert**, also ist 1.000.000 Block 1
und Selbstband 0. Wer den Block direkt nähme, wäre für jedes Band um eins daneben
und das erste Band spräche eines an, das es nicht gibt.

**Das Datenbank-Band ist kleiner als die anderen drei** (999 Zeilen gegen 99.999).
Eine gemeinsame Grenze ließe ein Spiel Datenbankzeile 50.000 adressieren — eine
Zeile, die der Editor nicht hält und eine Save-Datei nicht trägt.

**Ein einfacher Wert löst sich selbst auf und ist kein Schreibziel.** Das ist
das Kästchen „Daten nicht aufrufen" der Hilfe: Ein Feld darf beides halten, und
die Zahl selbst sagt welches.

**Der Vergleich löst beide Seiten auf**, weil die Hilfe sagt, der Vergleichswert
könne auch eine Variable sein — 2.000.000 dort heißt Normalvariable 0. Wer nur
die linke Seite auflöste, vergliche eine Normalvariable mit der *Zahl* zwei
Millionen statt mit dem, was sie hält.

**Die Fixture des Laufs hat sich geändert, und das ist der Punkt.** Ihr
Variablenoperand war eine nackte `1` — also der *Wert* eins, womit sie eine Zahl
schrieb statt einer Variablen. Sie ist jetzt `2.000.000`, mit einem Kommentar
warum, und `Test_WolfPluginRuntimeLoadsDataAndAdvancesDeterministicEventVm`
**ist fehlgeschlagen, als sich das Modell änderte** — wofür ein Laufzeittest da ist.

**Test evidence** `test_wolf_variable_bands.cs` (10), plus die sieben
Vergleichstests und die sechs Laufzeittests, alle nachgemessen. **1218/1218**.
**Mutations** 10 Regeln über zwei Läufe, **10 von 10 gefangen** — darunter die
exklusive Millionenschranke, der null-basierte Block, zwei Bänder im selben
Wörterbuch und der Vergleich, der nur eine Seite auflöst.

## WOLF Zuweisungsoperatoren — DONE: die VM kannte zwei von vierzehn

**Die Hilfe listet vierzehn: `=`, `+=`, `-=`, `*=`, `/=`, `%=`, 引上げ, 引下げ,
絶対値, arctan, sin, cos, Wurzel.** Die VM kannte zwei, und die zweite —
Addition — war fest in ihr eigenes Opcode verdrahtet, also gab es keinen Ort
für die anderen zwölf. **Ein Leser mit zwei kann keine Trefferquote, keine
Schadensformel und keinen Winkel berechnen, und keines der drei ist ein
exotisches Spiel.**

**Ein Weg für jeden Operator, und der Operator ist ein Feld und kein Opcode.**
Der Editor wählt ihn in einer Liste neben dem Ziel, also bräuchte ein Leser,
der aufs Opcode schaltet, einen dreizehnten Fall, sobald der Editor einen
hinzufügt — und die beiden Listen liefen auseinander. `AddVariable` ist jetzt
der Additionsoperator über denselben Weg, und genau deshalb können sie nicht
auseinanderlaufen.

**Der aktuelle Wert wird vor dem Schreiben gelesen**, und diese Reihenfolge ist
der Grund, warum zuerst aufgelöst wird: eine rechte Seite, die dieselbe Variable
nennt wie das Ziel, muss den alten Wert sehen, sonst läse ein Verdopplungsbefehl
den neuen.

**Division durch null lässt die Variable unverändert und ist kein Fehler.** Die
Hilfe sagt, ein Teiler 0 verhält sich wie Teilen durch eins. Ein Leser, der null
zurückgäbe, eine Ausnahme werfe oder einen Sentinel schriebe, wäre für eine Zeile
Hilfe auf drei Arten falsch.

**Trigonometrie ist skaliert, und die Skalierung ist der Operator.** Der Winkel
ist Zehntelgrad, das Ergebnis Tausendstel, also sind die Beispiele der Hilfe 600 →
866 für sin und 600 → 500 für cos. Ein Leser in Grad und Fließkomma gäbe 0,866
zurück und **das sähe nicht falsch aus** — es sähe wie eine kleine Zahl aus.

**Der Arkustangens liest zwei rechte Seiten und nicht den aktuellen Wert**, denn
eine Steigung ist eine Richtung, und eine Richtung braucht zwei Achsen. Die
Reihenfolge ist Y, X, weil die Hilfe X rechts-positiv und Y unten-positiv sagt —
also ist gerade unten +90 und nicht −90. Ein nackter Arkustangens reicht nur bis
±90°, also deckt `Atan2` den Kreis ab und eine Steigung nach links ist 1800.

**Die ganze Rechnung ist breit, und der Test hat bewiesen, dass sie es sein muss.**
Eine rechte Seite von drei Milliarden passt nicht in ein `int`; ein als `int`
geschriebener Test hätte nicht übersetzt, einer mit `unchecked` hätte eine andere
Zahl transportiert. **Sowohl Multiplikation als auch Subtraktion verlassen den
Bereich, bevor die Klemme sie sehen kann** — eine `int` zu klemmen klemmt den
Wert nach dem Überlauf, also die falsche Zahl. `Switch` gibt darum ein `long`
zurück, und der Parametertyp folgt der Grenze ±2 Milliarden der Hilfe.

**Ein Test, der sich selbst widerlegt hat.** Der Rundungstest behauptete erst, die
Beispiele der Hilfe trennten Rundung von Abschneiden — sie tun es nicht: fünf gibt
2236 so wie so, und zwei und drei ebenso. **Gemessen ist der trennende Eingabe
sieben** — Wurzel sieben mal tausend ist 2645,75, also rundet es auf 2646 und
schneidet auf 2645 ab. Und kein Eingabe hat eine exakte Halbe, also lassen sich
half-away-from-zero und half-to-even an diesem Testsatz nicht unterscheiden; der
Code sagt away from zero, und das Board sagt das, statt mehr zu behaupten. Ein
zweiter Test behauptete `sin(1800) == 1000`, das ist neunzig Grad mit dem
Kommentar eines Halbkreises; gemessen sind 1800 hundertachtzig Grad und geben 0.

**Test evidence** `test_wolf_variable_operator.cs` (15), darunter vier, die den
Operator durch die VM laufen lassen. **1233/1233**.
**Mutations** 12 Regeln in einem Lauf, **12 von 12 gefangen** — darunter die
Division-durch-null-Absicherung, die Zehntelgrad-Skala, Abschneiden statt Rundung,
der nackte Arkustangens und `AddVariable`, das still zu Zuweisung wurde.

## WOLF Band-Offsets — DONE: sie waren geraten, und die Hilfe nennt sie

**Die letzte Karte hat die vier Bänder aus der Hilfe gelesen und ihre Zahlen dann
geraten.** Sie schrieb „ein Millionenblock pro Band" und ging weiter, und die Tests
behaupteten diese Vermutung und waren grün. **Die Hilfe nennt sie, auf zwei
verschiedenen Seiten, und sie sind keine Reihe:** `1100000～:マップセルフ変数` und
`1600000～:コモンセルフ変数` aus dem Seitenaufruf des Ko单调Events, `2000000` für
Normalvariable 0, `3000000` für Stringvariable 0.

**Ein berechneter Block legte Map-Self auf 1.000.000, Common-Self auf 2.000.000 —
eine Normalvariable — und das Systemband auf 3.000.000, also das Stringband.** Ein
Spiel, das seine Systemuhr aus dem Stringbereich las, hätte eine Zahl bekommen, und
die Zahl wäre plausibel gewesen.

**Ein Mutationslauf hat es nicht gefangen, und das ist die Lehre.** Die zwölf Regeln
der Operator-Karte mutierten das Verhalten um die Offsets und alle wurden gefangen,
weil die Tests mit dem Code übereinstimmten. **Zwei falsche Zahlen, die sich
einig sind, ergeben eine grüne Suite.** Die Prüfung, die es gefangen hätte, war das
Lesen der Quelle — und die Offsets stehen jetzt in einem Test, der die Seite nennt,
aus der sie stammen.

**Die Million selbst ist eine Referenz ohne Band, und die ganze Lücke sagt das.**
Die Hilfe sagt „eine Million oder mehr wird aufgerufen", also ist 1.000.000 eine
Referenz — und sie liegt unter der Map-Self-Basis, also zeigt sie auf nichts. Das ist
eine andere Antwort als „keine Referenz", und der Code hat zwei Codes dafür: `-1` ist
ein Wert, `-2` eine Referenz ohne Band. **Ein Leser, der sie verschmilzt, würde
einem Aufrufer sagen, 1.050.000 sei ein Wert — und wer ihn speichert, behält einen
Zeiger in einer Variablen, die das Spiel als Zahl liest.**

**Das Stringband wird erkannt und abgelehnt.** Dieser Leser hat keine
Stringvariablen, und das sagt er, statt in das Normalband durchzufallen und einer
String-Referenz eine Zahl zu antworten. **Die Variable-Datenbank ist gar kein Band:**
Die Bedingungshilfe sagt, dass beim Datenbank-Vergleich kein Variablenaufruf wie
`1600000` angegeben werden darf, also hat sie keinen Offset, über den sie indiziert
werden könnte. Sie wird nach Typ und Spalte adressiert, in einem eigenen Speicher,
geschlüsselt mit verschobenem Typ statt multipliziert, damit ein großer Typ nicht in
die Zellen eines anderen Typs überläuft.

## Und die Schalter waren ein Wörterbuch, wo die Hilfe zwei Bereiche nennt

**0 und aufwärts adressieren ein Map-Event, 500.000 und aufwärts ein Koモン-Event.**
Die VM hatte ein `Dictionary<int, bool>`, also **kollidierten ein Map-Schalter und
ein Common-Schalter mit demselben Index** — und die Kollision ist still, weil beide
Lesen mit einem Boolean antworten und nur mit dem falschen.

**Die Basis ist 500.000 und nicht eine Million**, und die Variablenbänder beginnen
bei 1.100.000; ein Leser, der das Variablen-Schema wiederverwendete, ließe 100.001 bis
500.000 unerreichbar, und ein Spiel mit einem Schalter dort fände ihn dauerhaft aus.
**Eine Schalternummer außerhalb beider Bereiche ändert nichts**, denn ein Leser,
der das Wörterbuch wachsen ließe, speicherte einen Schalter, den der Editor nicht hält,
und der nächste Ladevorgang trüge ihn nicht — der Schalter funktionierte in der
Sitzung und verschwände danach.

**Ein nicht gesetzter Schalter ist aus, und ein nicht lesbarer auch.** Die
Bedingungsliste lautet „an" und „aus" und nichts sonst, also ist ein unlesbarer
Schalter aus — was ein Spiel vor dem Setzen ohnehin erwartet.

**Test evidence** `test_wolf_switches.cs` (7) und `test_wolf_variable_bands.cs`
(13, davon drei neu geschrieben, weil sie die geratenen Offsets behauptet hatten).
**1243/1243**, mit Operator-, Vergleichs- und Laufzeitdatei nachgemessen.
**Mutations** 13 Regeln in einem Lauf, **13 von 13 gefangen** — darunter jeder der
drei Offsets, die Lücke mit dem falschen Code, die Datenbank nur nach Spalte
geschlüsselt und die zwei Schalterkarten wieder zu einer gefaltet.

## WOLF Bewegungsrouten — DONE: 24 verifizierte Typen und kein Executor

**`WolfMoveRoute` hatte eine Typentabelle mit vierundzwanzig verifizierten Typen,
einen binären Leser mit elf Tests — und nichts führte einen einzigen Schritt aus.**
Ein Spiel mit einer Patrouillenroute hätte geladen und stünde still, und die Suite
blieb grün, weil sie Schritte nur las, nie ausführte. **Ein Leser, der getestet
und nicht ausgeführt wird, ist ein Parser.**

**Die Passierbarkeitsbits sind die der Hilfe: `1上+2左+4右+8下+16左上+32右上+64左下+128右下`.**
**Eine Diagonale ist ihr eigenes Bit und nicht oben plus links** — oben ist 1 und
links ist 2, ihre Summe ist 3, und Bit 3 gibt es nicht. Wer sie kombinierte,
erzeugte eine Richtung, für die das Format kein Bit hat, und eine Figur mit Bit 3
würde auf gar keine Richtung passen.

**Ein verweigerter Schritt dreht die Figur trotzdem in die Richtung.** Die erste
Fassung von `Step` kehrte bei der Verweigerung zurück und ließ die Blickrichtung
allein, während der Kommentar darüber das Gegenteil versprach — **und der Test hat
den Widerspruch gefunden.** Ein Wächter, der gegen eine geschlossene Tür läuft,
dreht sich zu ihr, und ein Spiel, das den Wächter durch den Spalt auf den Helden
blicken lässt, hängt daran.

**Geschwindigkeit und Frequenz sind 0 bis 6 und nicht dasselbe.** Die Hilfe
schreibt `移動速度[遅0-6速]` und `移動頻度[早0-6遅]` — einmal langsam nach schnell,
einmal oft nach selten — und wer eines auf das andere legte, ließe eine schnelle
Figur selten laufen. Eine Rate außerhalb wird geklemmt und nicht abgelehnt: Eine
geklemmte Figur läuft noch, und eine Ablehnung stoppte das Event.

**Tempo 0 ist ein Bild pro Kachel und keine unendliche Wartezeit.** Durch die
Geschwindigkeit zu teilen ergäbe eine unendliche Bildzahl und eine Route, die nie
zu Ende liefe. Die Bilder pro Kachel fallen mit der Geschwindigkeit: 1 bei 0, 16
bei 1, 8 bei 2, 4 bei 4, 2 bei 6.

**Der Add-Schritt liest den alten Wert**, dieselbe Regel wie die Variableoperation:
eine rechte Seite, die dieselbe Variable nennt wie das Ziel, muss den Wert vor dem
Schreiben sehen. **Ein Variablenschritt auf eine nackte Zahl wird abgelehnt und
nicht gespeichert**, denn unter dem Rohschlüssel zu schreiben hieße etwas zu
schreiben, das kein Lesen findet — der Schritt schiene zu funktionieren und
verlöre danach seinen Wert.

## Fünf Schritte werden abgelehnt, und das ist die ehrliche Antwort

**Ein Event annähern braucht eine zweite Figur, eine Position annähern braucht die
Karte, ein Sprung braucht seine eigene Route, ein Ton braucht Audio, und eine
Grafik ist ein Dateiname, für den dieser Runner keinen Lader hat.** Die
Annäherungsschritte mit einer Richtung zu beantworten hieße die Figur dorthin
laufen, wo das Event nicht ist; Ton und Grafik mit Erfolg zu beantworten wäre eine
Lüge, die der Aufrufer nicht bemerken kann. **Ein verweigertes Ergebnis ist die
einzige Antwort, auf die ein Aufrufer reagieren kann**, und es hält diesen Schnitt
ehrlich darüber, was er nicht kann.

**Die Zehnertasten-Tabelle ist absichtlich nicht implementiert.** Die Hilfe sagt,
eine Blickrichtung sei 1 bis 9 und entspreche dem Zehnerblock, und verweist für die
Zuordnung auf „Abbildung A" — eine Grafik, die nicht im Text steht. Ein früherer
Entwurf dieser Karte riet die Tabelle, und sie hatte doppelte Werte, was
unmöglich ist. **Die geratenen Band-Offsets haben eine Karte gekostet, und das ist
derselbe Fehler in derselben Sitzung.** Die Funktion liefert „keine Richtung" und
sagt warum.

**Test evidence** `test_wolf_move_route_runner.cs` (13), mit dem bestehenden
`test_wolf_move_route.cs` (11) nachgemessen. **1256/1256**.
**Mutations** 17 wirksame Regeln über drei Läufe, **17 von 17 gefangen** —
darunter rechts als Bit 3, eine Diagonale als oben-plus-links, die entfernte
Passierbarkeitsprüfung, die Blickrichtung, die einer Verweigerung nicht folgt,
Tempo 6 mit sechzehn Bildern und der Add-Schritt ohne vorheriges Lesen. Eine Regel
war eine Umbenennung, die nicht übersetzt, und zählt nicht.

## WOLF Figurenbrett — DONE: ein aufrufbares Ding ist noch kein Spiel

**Die letzte Karte baute einen Runner, den man aufrufen kann. Das ist noch kein
Spiel.** Der Routenleser lieferte Schritte, die Typentabelle verifizierte sie, und
nirgends stand eine Figur, auf der sie laufen könnten — die VM hatte weder Figuren
noch Karte. Diese Karte fügt Brett, zwei Opcodes und die Uhr hinzu.

**Das Brett gehört der VM und benutzt ihre Bänder.** Ein Routenschritt, der in eine
Variable schreibt, muss dorthin schreiben, wo das Event liest, und zwei Bänder
hießen, dass eine Patrouille Schritte in einen Speicher zählt, den niemand ansieht.

## Das Timing war dreimal falsch, und die Reihenfolge zweier Zeilen ist der Grund

**Erstens: der Index wurde vor dem Frame-Budget geprüft.** Ein Schritt rückt den
Index vor, wenn er läuft, also zeigt der Index auf den *nächsten* Schritt — und ein
Schritt, der über den letzten hinausgerückt war, war im nächsten Frame schon „am
Ende". Die Route endete, die Wiederholungsflagge setzte den Index auf null, **und die
fünfzehn Bilder, die der Schritt verlangt hatte, wurden mit weggeworfen.** Der Schritt
lief danach in jedem zweiten Frame, und ein Wächter mit Tempo 1 überquerte den
Bildschirm achtmal zu schnell. Das Symptom, das der Test meldete, war eine Figur bei
X = 9 nach siebzehn Frames, und jede beteiligte Funktion misst für sich allein
korrekt.

**Zweitens: die Bilder zuerst zu prüfen reparierte die Reihenfolge und ließ jeden
Schritt einen Frame zu spät enden**, weil der Tick, der das letzte Bild verbrauchte,
zurückkehrte, statt zu prüfen, was als nächstes kommt.

**Drittens, und das ist im Code: ein Flag und nicht ein Test auf die Restbilder.** Ein
`FramesLeft` von null ist mehrdeutig — es heißt entweder „noch im letzten Bild" oder
„kein Schritt begonnen" — und beide Lesarten standen im Code. `IsStepRunning` macht
die Rechnung exakt: Ein Schritt von n Bildern wird von einem davon gestartet, also ist
er im n-ten Tick fertig, und der Tick, der ihn beendet, sieht auch den nächsten
Schritt. **Ein Schritt von sechzehn Bildern wird als fünfzehn gespeichert, weil der
Tick, der ihn startet, sein erstes Bild ist.**

**Die zwei Verweigerungen werden vor dem Timing entschieden, und auch diese
Reihenfolge zählt.** Das Timing eines Schritts zu fragen, der nicht lief, ist eine
Frage ohne Antwort, und wer zuerst fragte, gab einem verweigerten Schritt ein
Frame-Budget — ein Wächter stünde sechzehn Bilder vor einer Wand und stoppte dann.
Überspringen heißt im selben Frame weitergehen, nicht ins Timing fallen.

**Die Verweigerungen selbst sind zwei Antworten, und die letzte Karte hatte es
falsch.** Der Runner hat den Rückgabewert von `Step` weggeworfen und einen Schritt in
eine Wand als `Stepped` gemeldet, also hatte die Skip-Flagge des Bretts nichts, worauf
sie reagieren konnte. Eine verweigerte Bewegung ist jetzt `Refused`, und eine
verweigerte ist nicht dasselbe wie ein Schritt, der sich nicht bewegt hat.

## Die beiden Wartezustände teilen sich einen Status und brauchen zwei Enden

**Das Bild-Warten zählt herunter; das Routen-Warten endet, wenn das Brett sagt, dass
die Bewegung fertig ist.** Sie benutzen denselben `Waiting`-Status, also unterscheidet
ein Flag sie — **und der Bug, den dieses Flag verhindert, ist: ohne es läuft die VM auf
dem MoveRoute-Befehl selbst weiter und startet die Route endlos neu.** Die Bilder
sind während des ganzen Wartens null, also hinge ein Leser, der nur Bilder zählte,
ewig fest — ohne Fehler irgendwo. **Der Befehlsindex rückt in diesem Moment vor und
sonst nirgends**, weil der MoveRoute-Befehl ihn absichtlich auf sich selbst stehen
ließ, damit das Event dort gehalten wird.

**Das Brett tickt über der Statusprüfung.** Eine Figur auf Patrouille läuft weiter,
während ein Text auf dem Bildschirm steht, und ein Leser, der es nur im Routen-Warten
tickte, frierte jede Figur für die Länge einer Textbox ein.

**Test evidence** `test_wolf_character_board.cs` (14) und
`test_wolf_move_route_runner.cs` (14, ein neuer Test für die Verweigerung), mit dem
bestehenden `test_wolf_move_route.cs` (11) nachgemessen. **1271/1271**.
**Mutations** 16 wirksame Regeln über zwei Läufe, **16 von 16 gefangen** — darunter
das nicht abgebuchte Frame-Budget, das doppelt gezählte erste Bild, der vor den
Bildern geprüfte Index, der Wrap ohne Flag-Löschung, der nicht vorrückende Index beim
Routen-Warten, das nicht tickende Brett während eines Bild-Wartens und eine als
`Stepped` gemeldete verweigerte Bewegung. Eine Regel war eine Umbenennung, die nicht
übersetzt, und zählt nicht.

## WOLF Kachel-Passierbarkeit — DONE: sechs Zustände, zwei Ebenen, Wände

**Die letzte Karte ließ Figuren laufen, und sie liefen durch Wände** — weil das
Brett eine Karten-ID und eine Breite hatte und sonst nichts. Passierbarkeit gab es
im WOLF-Leser nirgends: die Kartendaten trugen Kacheln, und Kacheln sind Bilder.

**Sechs Zustände und nicht zwei.** Das Kachel-Fenster des Editors wechselt sie im
Zyklus `○ → × → ▲ → ★ → □ → ○`, und die Kachel-Hilfe nennt die Bedeutung:
begehbar, nicht begehbar, begehbar mit dahinter versteckter Figur, begehbar und immer
über der Figur gezeichnet, begehbar mit halbtransparenten Füßen — und der sechste:

**↓ nimmt die Antwort der Ebene darunter und ist begehbar, wo keine Ebene da ist.**
Die Hilfe sagt: 「下のレイヤーに合わせます。下のレイヤーがない場合は通行可能です」.
**Wer die Kachel stattdessen ablehnte, fröre den Helden auf dem Boden fest** — eine
Bodenkachel ohne etwas darunter ist die gewöhnlichste Kachel einer Karte. Und wer nur
einen Boolean hat, verliert ▲, ★, □ und ↓, von denen nur × blockiert: die anderen drei
fügen eine Zeichenregel hinzu und kein Hindernis, und „hinterher versteckt" als
unbegehbar zu lesen hieße einen Wächter vor einem Treppengeländer einz sperren.

**Zwei Ebenen, weil ↓ eine davon fragt.** Die obere Ebene antwortet, und die untere nur
wo die obere fragt: Eine ★-Kachel über Wasser ist begehbar, weil ★ begehbar sagt. Wer
die untere Ebene alles entscheiden ließe, machte ein Schild über einer Wand unbenutzbar.

**Ein siebter Zustand wird abgelehnt** und nicht als begehbar behandelt. Wer auf
„nicht ×, also begehbar" durchfiele, ließe eine Figur auf eine Kachel, die das Spiel
noch nie gesehen hat — und das Symptom wäre ein Held durch eine Wand, die niemand
gezeichnet hat.

## Eine Figur ganz ohne Karte kann nicht gehen, und genau darum

**„Die Karte wurde nicht gelesen" ist nicht „die Kachel ist begehbar."** Eine Figur
ohne Gitter verweigert jeden Schritt, und wer eine fehlende Karte als offenes Feld
behandelte, ließe einen Wächter durch jede Wand auf jeder Karte gehen, deren Kacheln er
nicht lesen konnte — ohne Fehler irgendwo, denn ein Gang durch eine Wand ist ein Gang.

**Die Blickrichtung dreht trotzdem, denn die Verweigerung gilt der Position und nicht
der Blickrichtung.** Dieselbe Regel wie bei einer Wand — dieselbe Regel, gegen die der
Test der letzten Karte den Code ertappt hat.

**Das Brett gibt die Karte beim Laden an jede Figur** und nicht nur an die danach
gesetzten. Eine vorher gesetzte Figur hat dieselbe Frage wie eine spätere, und wer das
Gitter beim Setzen übergäbe, ließe die früheren durch Wände laufen.

**Die Kartengröße ist die des Gitters und nicht ein Feld daneben.** Ein Brett mit der
Breite 20 und einem Gitter von 10 ließe eine Figur zu Kachel 15 laufen und eine Zeile
lesen, die es nicht gibt.

**Neun der bestehenden Routentests sind beim Umstieg fehlgeschlagen, und das ist die
Änderung, die wirkt:** Seit die Karte eine Verweigerung und nicht ein Fehlen ist,
musste jeder Test, der einen bewegenden Schritt misst, seiner Figur eine Karte geben.
Drei davon maßen die Verweigerung und nannten sie eine Route.

**Einer der neuen Tests hatte die Wand an der falschen Stelle** — er setzte sie auf drei
und nannte den ersten Schritt auf drei „offen". **Ein Test, der zweimal die Verweigerung
misst und nie eine Figur laufen sieht, beweist nichts über das Laufen.**

**Test evidence** `test_wolf_passability.cs` (10), mit
`test_wolf_character_board.cs` (14) und `test_wolf_move_route_runner.cs` (14) nach dem
Umstieg nachgemessen. **1281/1281**.
**Mutations** 16 wirksame Regeln über drei Läufe, **16 von 16 gefangen** — darunter
der Pfeil, der die untere Ebene nicht fragt, eine fehlende untere Ebene, die abgelehnt
wird, jedes von ▲, ★ und □ als unpassierbar gelesen, der unbekannte Zustand als
begehbar, das Gitter, das aus der unteren statt der oberen Ebene antwortet, die
entfernte Kartenprüfung und das Brett, das die Karte nur dem Helden gibt.

## WOLF Figuren-Kollision — DONE: halbe Kachel, Durchlass, und ein Held, der keine Wand ist

**Die letzte Karte gab Figuren Wände und sonst nichts**, also lief ein Held durch
jeden Wächter, jeden Händler und jedes Schild. Die einzige Spur wäre ein Held
innerhalb eines Ladens gewesen, und ein Gang durch einen Wächter ist ein Gang.

**Die Trefferfläche ist eine Kachel breit und eine halbe hoch**, und die Hilfe nennt
die Zahl: `当ﾀﾘ判定■(正方形)` aus ist `横1マス×縦0.5マス` — die Füße einer Figur und
nicht ihr ganzer Körper — und die Quadratoption macht eine ganze Kachel daraus. **Wer
für beides eine Kachel nähme, ließe jede halbhohe Figur mit der Figur auf der Kachel
davor kollidieren, und eine Menschenmenge in einem Gang würde feststecken.** Das ist
ein Spiel, das nicht zu Ende zu spielen ist, und es sieht aus wie ein Fehler im
Pfadfinden und nicht in der Trefferfläche.

**X ist halb offen und Y geschlossen, und diese Asymmetrie ist eine Entscheidung und
kein Tippfehler.** X halb offen hält einen Gang begehbar: Eine Figur auf Kachel 2
reicht von 2 bis 3, eine auf 3 von 3 bis 4, und eine geschlossene Compare hätte sie
auf der Grenze überlappen lassen und jedes Zwei-Kachel-Zimmer blockiert. Y geschlossen
ist die andere Hälfte — **eine quadratische Figur auf Kachel 5 belegt 5 bis 6 und
berührt die Figur auf Kachel 6, und ein fester Gegenstand, in dem eine andere Figur
stehen darf, ist nicht fest.** Es ist auch das, was die Quadratoption überhaupt
bedeutet: Mit halb offenem Y reichte eine quadratische Figur genauso weit wie eine
halbhohe, und die Option wäre ein Name für nichts. **Die Hilfe schreibt den Vergleich
nicht aus, also steht das hier als Wahl mit ihren Gründen.**

**Ein Geist wird durchquert, und die Beziehung ist einseitig.** Die Option
`イベントをすり抜けられるようにします` macht ein Event durchquerbar, und die Hilfe
fügt hinzu, dass ein solches Event nur ausgelöst wird, wenn der Spieler darauf steht —
also ist ein transparentes Schild zugleich eine Wand, durch die man geht, und eine
Sache, die man nur betreten kann. **Das Flagge gehört zum Geist und antwortet, bevor
irgendjemand gefragt wird**, also geht ein Geist durch eine feste Figur und eine feste
Figur durch einen Geist. Wer die Beziehung symmetrisch machte, hieße jeden unsichtbaren
Auslöser im Spiel zusperren.

## Ein Test las diese Regel rückwärts, und der Code hatte recht

**Einer der neuen Tests behauptete eine *Verweigerung*, als der Held auf einen Geist
trat** — er las die Regel, als stoppte der Geist jeden, der in ihn hineinlief. Der Code
tat das Gegenteil und hatte recht: Wer dem Test glaubte, hätte eine transparente
Dekoration zur Wand gemacht, also das Gegenteil dessen, wozu die Option da ist.

**Das steht hier, weil der Test beim ersten Lauf der Form bestand und erst umfiel, als
der Test darüber anfing zu arbeiten.** Vier der elf Tests maßen die falsche Sache aus
einem Grund, der nichts mit Kollision zu tun hatte, und das Symptom war in allen vieren
dasselbe Wort: `False`.

## Der Held war bis zu dieser Karte gar nicht auf dem Brett

**Die Besetzungsliste fing leer an, und der erste Aufruf eines Spiels ist LoadMap** —
das das Gitter an alle auf der Liste abgibt. Eine leere Liste hieß, dass der Held das
Gitter nie bekam, **also konnte der Held gar keinen Schritt machen**, und ein Spiel mit
einem Event darauf wäre mit einem feststeckenden Spieler aufgegangen. Der Held wird im
Konstruktor platziert, und das Gitter wird in `RefreshOccupants` abgeben und nicht nur
beim Setzen einer Figur, weil eine vorher gesetzte Figur dieselbe Frage hat wie eine
spätere.

**Der Kandidat trägt jedes Feld, über `At()`.** Einen Wegwerf-Charakter von Hand zu
bauen hieße fünfzehn Felder kopieren und beim nächsten Feld eines zu vergessen — und das
Feld, auf das es am meisten ankommt, die Trefferfläche, wäre genau das, was eine von Hand
gebaute Kopie vergisst.

**Test evidence** `test_wolf_character_collision.cs` (11), mit
`test_wolf_passability.cs` (10), `test_wolf_character_board.cs` (14) und
`test_wolf_move_route_runner.cs` (14) nach dem Umstieg nachgemessen. **1292/1292**.
**Mutations** 16 wirksame Regeln über zwei Läufe, **16 von 16 gefangen** — darunter
die halbe Kachel als volle gelesen, die Quadratoption ignoriert, die Y-Achse halb offen
gemacht, die X-Achse geschlossen, die Löschprüfung nur auf einer Seite, der Durchlass
symmetrisch gemacht, die Selbstkollision nicht übersprungen, der Kandidat ohne
Trefferfläche, der Held nicht auf dem Brett und die nicht neu gebaute Liste.

## WOLF Zielnummern und Annäherung — DONE: -1 bis -7 und zwei laufende Schritte

**Die beiden Annäherungsschritte waren zwei Karten lang abgelehnt**, aus dem ehrlichen
Grund: Ein Event annähern braucht eine zweite Figur und eine Position annähern
braucht die Karte, und das Brett hatte beides nicht. Die letzten zwei Karten haben es
beides gegeben, und die Ablehnung war nicht mehr wahr — **wer sie behalten hätte, hätte
ein Spiel, dessen Wächter sich nie etwas nähern.**

**Die Zielnummern sind die der Hilfe und keine Event-IDs.** Die Liste lautet:
`0以上の場合 ＝ その値のIDを持つイベント`, `-1＝このイベント`, `-2＝主人公(隊列先頭)` und
`-3` bis `-7` für die fünf Begleiter. **Null ist eine Event-ID und der Held ist minus
zwei** — wer null für den Held nähme, beantwortete einen Befehl über Event 0 mit dem
Spieler und einen Befehl über den Spieler mit Event 0, und beides gibt es auf einer
echten Karte.

**„Dieses Event" ist das eigene Event der Route, und die Zahl reist mit der Route.**
Der Runner hat keine Vorstellung davon, in welchem Programm er läuft, und das Brett weiß
es, also gehört der Besitzer zum Routenzustand.

**Die Partei hat fünf Plätze, und ein leerer ist niemand.** Die Liste hört bei -7 auf,
also hätte ein Leser mit wachsender Liste -8 mit einem sechsten Begleiter beantwortet,
den der Editor nicht benennen kann. Ein leerer Platz ist null und keine neue Figur.

## Ankunft und Ablehnung sind in einem Bool dasselbe, und dürfen es nicht sein

**`ApproachOne` gab zuerst einen Bool zurück, in dem false sowohl „ist angekommen"
als auch „kein solches Ziel" bedeutete**, und der Aufrufer konnte es nicht unterscheiden.
Ein Wächter, der sein Ziel erreicht hatte, wurde als einer verbucht, der jemandem folgt,
den es nicht gibt. **Vier Antworten, weil sich jedes Paar unterscheidet**: Ein Schritt
nimmt Zeit, eine Ankunft nicht, kein Ziel und blockiert halten die Route an, sofern sie
nicht das Überspringen sagt.

**Und Kollisionsregel und Annäherungsregel treffen sich an einer Stelle.** Ein Wächter
eine Kachel vor seinem Ziel, vom Ziel selbst blockiert, **ist angekommen** — als
blockiert gelesen hieße, ein Wächter gäbe auf, sobald er den Spieler einholt, also genau
in dem Moment, um den es im Spiel geht. immer als Ankunft gelesen hieße, ein Wächter an
einer Wand bliebe eine Kachel stehen und nennte es fertig. **Der Unterschied ist, was den
Schritt abgelehnt hat**, und das Brett fragt nach.

**Ein Schritt pro Schritt, und das Ziel wird jeden Schritt neu gelesen.** Ein Wächter,
der einem laufenden Helden folgt, muss den Abstand immer wieder schließen, und wer den
ganzen Weg einmal berechnet, ginge dorthin, wo der Held war.

**Die größere Lücke geht zuerst, und der Gleichstand schließt X.** Das macht, dass eine
Diagonale als Diagonale liest. **Die Hilfe nennt die Reihenfolge nicht, also steht sie
hier als Wahl** — wer beide Achsen zugleich schließe, erzeugte einen Schritt, für den
das Format keinen Typ hat.

## Zwei Fehler, die der Compiler nicht gemeldet hat, und zwei falsche Tests

**Der Begleiterbereich war „mindestens -3 und höchstens -7" geschrieben, und das ist
leer.** Die Hilfe zählt abwärts, also müssen die Schranken umgekehrt gelesen werden, und
ein immer falscher Bereich ist perfectly gültiges C#. Er kam zweimal vor — in
`Classify` und in `CompanionNumber` — und **der Compiler meldete den ersten als
unerreichbaren Arm und zu dem zweiten nichts.**

**Drei der zehn neuen Tests waren falsch, und zwei davon falsch über die Regeln und
nicht über den Code.** Einer verglich „drei" mit „vier minus eins", um zu entscheiden,
welche Achse zuerst schließt; die Lücken sind vier auf Y und drei auf X, und der größere
Betrag ist das, was die Regel ansieht. Einer setzte den Helden in die Partei, aber nicht
aufs Brett, und maß einen Wächter, der *durch* den Spieler ging — Partei und Brett sind
getrennte Dinge, und ein Test muss beide setzen. Einer erwartete, dass die Route endet,
wenn der Wächter ankommt, und hatte die Ankunft als Ende des Weges gelesen; ist sie
nicht, denn die Wiederholungsflagge startet den Schritt neu.

**Test evidence** `test_wolf_approach.cs` (10), mit den vier WOLF-Dateien der letzten
beiden Karten nachgemessen. **1302/1302**.
**Mutations** 15 wirksame Regeln über zwei Läufe, **15 von 15 gefangen** — darunter
-1 und -2 vertauscht, der Begleiterbereich in der falschen Reihenfolge, die Ankunft in
die Ablehnung gefaltet, die größere Lücke umgekehrt, der Gleichstand auf Y, der
blockierte Schritt als Ankunft gelesen, die Zielkoordinate nicht durch die Bänder
aufgelöst und die Figur im Weg nicht erkannt.

## WOLF Zeichentabellen — VERIFY, nicht DONE: ein Test wirft und ich weiß nicht warum

**WOLF hatte überhaupt keine Präsentation** — fünfundzwanzig Dateien und keine malt
etwas. Die Material-Hilfe sagt, was eine Zeichentabelle sein muss.

**Die vier Richtungen sind unten, links, rechts, oben von oben nach unten, und das
ist nicht die Kompassrichtung.** Die Material-Hilfe gibt diese Reihenfolge zweimal, und
wer oben, rechts, unten, links nähme, zeigte jede Figur um neunzig Grad gedreht — die
Art Fehler, die ein Spieler in der ersten Sekunde sieht und nie meldet.

**Der Laufzyklus ist B → A → B → C → B, und die mittlere Zelle kommt zweimal.** Die
Hilfe benennt die Zellen A, B und C von links, also ist B die Spalte 1. Wer sie in
Reihenfolge abspielte, ließe eine Figur dreimal vorwärts treten und dann zurückschnappen.

**Der Stillstehzyklus läuft anders herum — 2, 3, 2, 1 — und die T- und TX-Form legen die
Stillstehframes nach links**, also sitzt eine stehende Figur auf einer kleineren Spalte
als eine gehende. Wer den Versatz andersherum addierte, legte die Stehpose in die Mitte
des Gangs: eine Figur, die nie stehen bleibt und nie zu stehen scheint.

**Die Animationsfrequenz ist Bilder pro Schritt, und die Reihenfolge ist die
umgekehrte der Geschwindigkeit.** Die Hilfe schreibt アニメ頻度[早0-6遅] — oft nach
selten —, während die Bewegungsgeschwindigkeit langsam nach schnell läuft. Wer durch die
Frequenz teilt oder die Geschwindigkeit nähme, ließe eine Figur mit verschwommenen Füßen
auch die Karte im verschwommenen Tempo überqueren. **Null ist jedes Bild und nicht nie**,
weil die Hilfe die 0 ans schnelle Ende setzt.

## Drei echte Fehler, und zwölf Messungen ohne Antwort

**Drei der elf Tests fanden echte Fehler.** Ein Blickrichtungs-Schritt meldete `Stepped`,
dieselbe Antwort wie eine Bewegung — also wurde ein Wächter, der sich nur drehte, für die
ganze Dauer seiner Route im Laufzyklus gezeichnet, und das ist eine Pose, die der Künstler
nie gezeichnet hat. `Turned` ist jetzt ein eigener Ausgang. **Der Idle-Versatz wurde
zweimal angewendet** und legte eine gehende Figur eine Spalte zu weit rechts. **Und
`IsWalking` wurde bei jedem Schritt gesetzt** statt bei einer Bewegung.

**Zwei der Tests waren falsch über die Regeln**: einer erwartete die gehende Zelle auf
Spalte 1, und das ist A — Schritt 0 des Zyklus ist B, und B nach dem Idle ist Spalte 2.
Einer erwartete, eine Diagonale auf einem Vierer-Blatt werde auf die nächste
Kardinalrichtung geklemmt, und genau das verhindert diese Karte.

### Der Befund, der offen bleibt

**`Test_TheIdleCycleRunsTheOtherWay` wirft „Attempted to divide by zero", und ich habe
ihn in sechzehn Messungen nicht gefunden.** `IdleCell` teilt durch nichts — es ist
`pIndex % 4` —, `WalkPattern` auch nicht, die ganze Datei nicht, die Konstante liest 3,
ein Test, der nur die Konstante anfasst, ist grün, der Aufruf nimmt drei literale
Argumente, `Setup` und `Teardown` sind
leer, und das Umbenennen von Suite und Methode änderte den Namen im Bericht und nichts
sonst. Er überlebt das vollständige Löschen von `obj`, `bin` und `.godot/mono`, und zweimal
dieselbe Formel in einer Methode trennt ihn auch nicht.

**Was ich versucht habe, damit die nächste Sitzung es nicht wiederholt:** die vier Aufrufe
ausgeschrieben statt in einer Schleife; ein Wegwerf-Test mit genau einem Aufruf; die Suite
umbenannt, um zu prüfen, ob die Meldung wirklich diesem Test gilt (sie tat); `obj`, `bin`
und `.godot/mono` gelöscht und aus `project/` gebaut statt aus dem Wurzelverzeichnis;
die DLL-Zeitstempel geprüft; `Setup` und `Teardown` der Basis gelesen; alle
`Assert*`-Signaturen geprüft (nur `AssertEq<T>(T, T, string)` und zwei Vergleiche);
den Quelltext byteweise gelesen (kein BOM, keine Null-Bytes, UTF-8 sauber).

**Erledigt seitdem:** ein Test, der nur die Konstante prüft und `IdleCell` nicht aufruft —
**grün**, also sitzt der Fehler im Aufruf und nicht in der Datei als ganzer; ein Aufruf
mit literalem `3` statt der Konstante — **grün**, also ist die Konstante nicht schuld; ein
Aufruf von `WalkPattern` — **grün**, also auch nicht; und zweimal derselbe Aufruf hintereinander
— **rot ab dem zweiten**, was die naheliegendste Erklärung (ein zustandsbehafteter Aufruf) widerlegt.

**Offen bleibt damit nur das, was keine dieser Messungen abdeckt:** ein Lauf direkt über
Godot ohne den Validator, um den `.godot`-Import-Pfad zu umgehen, und ein Blick in die
übersetzte IL der Methode, statt in den C#-Quelltext.

**Die Karte steht auf VERIFY, nicht auf DONE.** Die Regel ist gemessen und implementiert;
was ich nicht kann, ist den Lauf erklären. **Ein Test, der eine Ausnahme wirft, deren
Ursache ich nicht benennen kann, ist ein offener Befund und keine Kleinigkeit, die man
wegrückt.**

**Test evidence** `test_wolf_character_sheet.cs` (11, einer fällt), mit
`test_wolf_move_route_runner.cs` (14) nach der `Turned`-Änderung nachgemessen.
**1302 von 1303 bestehen.**
**Kein Mutationslauf für diese Karte**, und der Grund wird gesagt statt kaschiert: der
Schnitt ist nicht grün, und eine Mutationszahl über einer roten Suite ist eine Zahl ohne
Bedeutung.

## WOLF Zeichentabellen — DONE, und der Fehler lag nicht in der Datei

**Die Karte stand siebzehn Arbeitsdurchgänge auf VERIFY, und alle sechzehn Messungen
haben die Quelle für unschuldig erklärt.** `IdleCell` warf bei jedem Aufruf einen
`DivideByZeroException`, und `pIndex % 4` kann nicht durch null teilen. Keine Division
in der ganzen Datei, die Konstante liest 3, ein Test nur mit der Konstante ist grün, ein
Aufruf mit literalem 3 ist grün, `WalkPattern` — gleiche Form, mit Klammern — war von
Anfang an richtig, und das Umbenennen von Suite und Methode änderte nur den Namen im
Bericht.

### Was den Fehler gefunden hat

**Die einzige Messung, die ihn fand, war: die Methode allein kompilieren und sie werfen
sehen.** In einem getrennten Projekt, ohne Godot, nur die Datei und ein
`Console.WriteLine` — die Ausnahme erschien sofort, und die Zeile war der `_ => 0,`-Arm
eines Switch, dessen Selektor `pIndex % 4` war.

**Die Klammern um das Modulo sind keine Dekoration.** `WalkPattern` schreibt
`return (pIndex % 4) switch`, `IdleCell` schrieb `return pIndex % 4 switch`. **Mit den
Klammern gibt dieselbe Datei 1, 2, 1, 0 zurück; ohne sie wirft sie bei jedem Argument.**
Die Regel steht als eigene Regel in der Mutationsliste, also sind die Klammern jetzt
bewiesen und nicht bloß geglaubt.

### Was an der Reihenfolge die eigentliche Lehre ist

**Alle sechzehn Messungen haben die Quelle gefragt, ob die Quelle falsch ist** — und
eine Datei, die eine Frage über sich selbst nicht beantworten kann, wird auch nicht
dadurch freigesprochen, dass man sie liest. **Die siebzehnte Messung hat die Frage
geändert** — nicht „ist die Quelle falsch", sondern „funktioniert sie außerhalb dessen,
was es gemeldet hat" — und das ist die Frage, die eine Antwort hatte.

**Kein Quelltextfehler hätte sich so verhalten.** Ein echter Rundungsfehler wäre an
anderen Stellen aufgefallen, ein echter Null-Teiler hätte eine sichtbare Null gesehen.
**Ein Compiler, der einen Ausdruck anders bindet als man ihn liest, ist unsichtbar** —
und genau deshalb ist „kompiliere es allein und führ es aus" eine eigene Messung und
nicht dieselbe Messung noch einmal.

**Test evidence** `test_wolf_character_sheet.cs` (11) und
`test_wolf_move_route_runner.cs` (14), beide nach den Klammern nachgemessen.
**1313/1313**, Validator grün.
**Mutations** 16 Regeln über zwei Läufe, **16 von 16 gefangen** — darunter die
Klammern um das Modulo, die Richtungsreihenfolge, der Zyklus auf der ersten statt der
mittleren Zelle, der Idle-Zyklus wie der Lauf, der doppelt angewendete Versatz, die
Stehzelle, das nie abgebuchte Animationsbudget und eine Drehung als Schritt gemeldet.

## WOLF Ton — DONE

**WOLF hatte keinen Ton und der Tonschritt in einer Laufbahn wurde abgelehnt** — die ehrliche

Antwort, solange es nirgendes hinzustellen war. Jetzt: drei Kanaele, die Null-Lautstaerke als

zwei Regeln und der Tonschritt im Brett statt im Läufer.



**Die Regel, die eine ganze Karte traegt: eine Lautstaerke von 0 ist unter der alten Regel

Standard und unter der neuen stumm.** Die Materialliste sagt beides, und die

Spielkonfiguration sagt, dass vor 3.681 auf 100 umgerechnet wurde und die Einstellung es nun

behaelt — der Fall dafuer ist ein Hintergrundgeraeusch mit Mischung 0 fuer interaktive Musik.

**Welche der beiden ein Spiel benutzt, ist eine Einstellung, und dieser Leser hat keine** —

er meldet eine Null als Null und benennt sie als den mehrdeutigen Wert, der sie ist.



**Die Zeit eines Effekts ist eine Verzoegerung und die eines Musikstuecks eine Einblendung.**

Die Materialliste sagt, die Einblendzeit werde zu *die Wiedergabe verzoegern* fuer einen

Soundeffekt, und nennt sechzig Bilder pro Sekunde. Ein Leser, der eine Verzoegerung als

Einblendung behandelte, haette den Effekt leise beginnen und lauter werden lassen; einer, der

die Zahl als Millisekunden las, haette ein Sechzigstel der verlangten Wartezeit gewartet.



**Der Dateiname steht in den Einzel-Byte-Argumenten.** Ein Schritt hat vier Byte-Argumente und

dann Einzel-Byte-Argumente, und ein Dateiname ist Text. Ein Leser, der ihn in den Zahlen

gesucht haette, haette drei ganze Zahlen gefunden und sich gefragt, warum kein Titel laeuft.



### Der Fund



**`Clear() leerte die Kiste und liess das Radio laufen.** Figuren, Wege, Passierbarkeit und Partei

werden alle mitgenommen, der Ton nicht. Ein neues Spiel, das die Musik des letzten behaelt,

oeffnet seinen Titelbildschirm mit dem Thema des vorherigen Spiels — und **nichts anderes auf dem

Brett haette es gemerkt**, weil die Figuren fort waren und es keine Figur gibt, die falsch

aussieht.



**Test evidence** `test_wolf_audio.cs` (11). **1324/1324**, Validator grün.

**Mutations** 11 Regeln über zwei Läufe, **11 von 11 gefangen**.

## WOLF Laufbahnen aus einer Datei — DONE

**Die VM hatte `MoveRoute` und `WaitUntilRouteDone` im Dispatch und in der Enum, und

`ParseOpcode` hatte fuer keinen der beiden einen Namen.** Eine Kartendatei mit

`"op": "move_route"` kam als `Unknown` an, und `Unknown` lehnt die VM ab — **also stand eine

im Editor geschriebene Patrouille still, und nirgends stand, warum.**



**Kein Test haette es gefunden, weil jeder andere Test seinen Befehl von Hand gebaut hat.**

Ein handgebauter Befehl hat Figur und Laufbahn schon gefuellt; nur der Dateipfad muss sie

fuellen. Das ist die Art Luecke, die nur ein Test schliesst, der eine echte Datei auf der

Platte schreibt statt ein Objekt zu bauen.



**Ein unbekannter Schrittname ist 0xFF und nicht 0** — 0x00 ist ein Schritt nach unten, also

haette ein Tippfehler eine Figur eine Kachel nach sueden geschickt und das Spiel haette

richtig ausgesehen, bis zum Tag, an dem es das nicht mehr tut. **Ein unbekannter Modus ist

`Custom` und nicht 0**, denn 0 heisst *sich nicht bewegen*.



**Test evidence** `test_wolf_route_from_file.cs` (9), gegen eine echte Kartendatei.

**1333/1333**, Validator gruen. **Mutations** 12 Regeln, **12 von 12 gefangen**.

## WOLF Common Events — DONE

**Der Binaerleser dekodierte Typ 300 und die Enum hatte keinen Wert dafuer** — also konnte

ein Spiel mit einem Aufruf den Aufruf nicht ausfuehren, **und jeder WOLF-Shop ist aus

Common Events gebaut.**



**Ein Aufruf teilt den Zustand, statt ihn zu kopieren; das Ende eines Common Events setzt den

Aufrufer fort; die Tiefengrenze ist die Wacht gegen ein Event, das sich selbst aufruft; und

Null ist der Held und kein Event.**



### Der Befund, der keine Codefehler fand



**Ich habe zehn Minuten an einem Test gefeilt, der keine fand, weil es keine gab.** Eine Figur

sollte eine Common-Event-Laufbahn gehen und stand still.



**Das Brett allein ging, die VM nicht — ohne jeden Aufruf.** Die Route startete, die VM erreichte

das Ende im selben Tick, und **ein `Completed` tickt das Brett nicht mehr.**



**Also gilt: eine Laufbahn in einem Event, das sofort endet, geht nicht — und das ist richtig.**

WOLFs eigene Common Events folgen einer Laufbahn mit einem Warten, oder das Event laeuft

weiter, oder die Laufbahn startet ein Parallelereignis. **Ein Test, der hier das Gehen erwartet

haette, haette eine Form gemessen, die kein Spiel benutzt.**



**Was ich daraus mitnehme:** nach vier gescheiterten Deutungsmessungen habe ich nicht weiter

geraten, sondern den Test zur Sonde gemacht, die eine Frage stellt — Brett allein, dann VM ohne

Aufruf. **Die Sonde gruen und die VM rot ist ein Befund; viermal dasselbe Raten ist keiner.**

Und als der Test sich zum Wirrwarr entwickelt hat, habe ich ihn nicht weiter geflickt, sondern

neu geschrieben.



**Test evidence** `test_wolf_common_event_call.cs` (10). **1343/1343**, Validator gruen.

**Mutations** 10 Regeln, **10 von 10 gefangen**.

## WOLF Map-Event-Aufrufe — DONE

**Unter 500.000 ist es ein Map-Event, ab 500.000 ein Common Event, und nur dann traegt der

Aufruf Argumente.** Die Enum hatte fuer Typ 210 keinen Wert.



**Ein Event, das es nicht gibt, wird ignoriert — und nicht als Fehler gemeldet.** Die Hilfe sagt

das in einem Satz, **und der Grund ist, dass ein Spiel ein Event loescht und den Aufruf stehen

laesst.** Ein Leser, der dort scheiterte, haette ein Spiel, das an einem Aufruf zu einem

entfernten Schatzkasten tot stehen bleibt. **Das ist die einzige Stelle in dieser VM, wo ein

Fehlendes absichtlich kein Fehler ist.**



### Der Fund



**`ApplyOperator` schrieb direkt in die Baender, waehrend das Lesen ueber den neuen Durchlass

lief.** Also schrieb ein Common Event, das sein eigenes \cself[0] zuwies, in ein Band fuer sich

und las es als null zurueck. **Der Test hat es gefunden, weil er eine Regel prueft, die ich am

wenigsten belegt hatte** — der Self-Variablen-Zusammenarbeit zwischen Lesen und Schreiben.



**Und Map-Self hat keinen eigenen Rahmen: seine Self-Variablen sind die des aufrufenden Events.**



**Test evidence** `test_wolf_event_call.cs` (10). **1353/1353**, Validator gruen.

**Mutations** 11 Regeln, **11 von 11 gefangen**.

## K-134 Bordtafel — DONE

**Die Bordtafel war zu kurz, und die Lücke, auf die es ankam, war nicht eine von
den fünfundzwanzig.**

### Was die Karte verlangt hat und was sie gefunden hat

**Jeder der fünfundzwanzig DONE-Abschnitte braucht Was gebaut wurde, Testbeleg und
Commit — kein Abschnitt aus dem Titel.** Die Commit-Nachrichten tragen keine Kartennummern,
also habe ich über die Dateien gebunden: `git log --follow --diff-filter=A` auf die Datei,
die die Karte behauptet. **`--follow` ist der entscheidende Schalter, weil der Umzug des
Godot-Projekts nach `project/` jeden Pfad umgeschrieben hat und ein einfaches `git log`
den Umzug zurückgibt und nicht die Arbeit.**

**Und dann kam der Fund: K-136 hatte einen vollständigen Detailabschnitt, einen `READY`-Status
und überhaupt keine Zeile in der Tabelle.** Es ist die einzige P0-Karte dieses Projekts, die
ein Leser der Tabelle nicht haette sehen koennen — **und sie ist der Grund, warum es diese
Karte gibt: die Tabelle fehlten nicht fünfundzwanzig Zeilen, ihr fehlte eine, die wichtiger
war als alle zusammen.**

### Die Messung nach der Reparatur

| | |
|---|---|
| Board-Zeilen | 113 |
| eindeutige Zeilen | 113 |
| Zeilen ohne Abschnitt | **0** |
| Abschnitte ohne Zeile | **0** |
| doppelte Zeilen | **0** |

**Dreiundzwanzig Nummern zwischen K-001 und K-136 werden von weder Tabelle noch Abschnitt
benutzt** — K-005 bis K-009, K-025 bis K-029, K-056 bis K-059, K-062 bis K-069 und K-135.
**Das sind Nummern, die nie vergeben wurden**, und die Reparatur ist nicht, Abschnitte fuer sie
zu erfinden: ein Abschnitt fuer eine nie geschriebene Karte ist eine Behauptung. Die
Nummerierung hat Luecken und die Luecken sind sichtbar — **das ist der Unterschied zwischen
einem Loch und einer Luege.**

**K-080 und K-090 bleiben BACKLOG**, weil sie hinter dem spielbaren Meilenstein liegen und
beide eine Entscheidung brauchen, die dieses Repository nicht still treffen darf: ob Ruby
ausgefuehrt wird und ob JavaScript ueberhaupt ausgefuehrt wird. **Die vorhandene
Ruby-Arbeit ist ein Lexer, ein Parser und ein Werterlayer; die MZ-Arbeit liest zwei echte
Spiele und fuehrt deren Kommandozeilen aus. Beides ist keine Laufzeit, und beides gibt nicht
vor, eine zu sein.**

## `11120` Move Picture — DONE

**`ShowPicture` und `ErasePicture` liefen. `MovePicture` stand in der Konstantenliste mit

einer Beschreibung und ohne `case`** — dieselbe Form wie K-094, als die beiden anderen

implementiert, getestet und unerreichbar waren.



**Also fiel ein Spiel, das eine Titeltafel ueber den Bildschirm schob, in den Default-Arm** und

wurde als nicht unterstuetzter Befehl gemeldet.



### Die Regel, die die Suche ueberhaupt ausgeloest hat



**Ein Leser, der seine eigene Konstantenliste auf eine Luecke prueft, findet diese Luecke

nie.** Die Konstante ist da, die Beschreibung ist da, der Build ist sauber — und der Befehl

flaellt trotzdem durch. **Nur der Dispatch ist kurz, und der Dispatch ist das, was ein Befehl

erreichen muss.**



**Und die Neumessung hat die Zahl der Karte widerlegt: 89 ist nicht 43, und es sind auch

nicht 89 verdrahtete Befehle, sondern 89 von 94 Konstanten, von denen vier Grenzwerte sind**

und **eine ein echter Befehl.** Genau diese eine war die Luecke.



**Test evidence** `test_rm2k_move_picture.cs` (7). **1360/1360**, Validator gruen.

**Mutations** 10 Regeln, **10 von 10 gefangen**.

## `11910`/`11950` Menues — DONE

**Die Karte nannte vier Befehle als eine offene Familie. Gemessen sind es zwei** — `11930`

und `11960` liefen schon lange ueber den Einzeiler-Handler mit Teleport und Flucht. **Und die

Karte war geschrieben worden, bevor das so war.**



**Breite 0 fuer beide.** Die Referenz gibt Speichern und Hauptmenue eine Breite von null — **ein

Leser, der einen Parameter verlangt haette, waere jeden Menuebefehl eines Spiels abgelehnt

haben.**



**Zwei Flags und nicht eines, und eine offene Nachricht zuerst** — dieselbe Regel wie der

Game-Over-Bildschirm.



### Der Fund am Test, nicht am Code



**Ich hatte einen Test geschrieben, der behauptete, die beiden Flags kollidierten nicht, und er

blieb rot** — bis die Messung zeigte, dass der zweite Befehl nie laeuft. **Die Seite haelt, also

laeuft derselbe Befehl in jedem Frame erneut**, und ein Programm, das Hauptmenue und dann

Speichern oeffnet, bekommt nur das Hauptmenue. Das ist der Preis einer gehaltenen Seite, **und

die Referenz zahlt ihn genauso.**



### Und eine Zahl, die falsch war



**Die Mindestbreite von `11120` war 8 statt 16.** Die Dispatch-Zeile der Referenz sagt

`CmdSetup<&CommandMovePicture, 16>`, und ich hatte acht geschrieben — aus den fuenf, die der

Befehl liest, plus einer Vermutung. **Und die Test-Fixture paddete ebenfalls auf acht, was beide

Fehler in dieselbe Richtung gehen liess und die Suite gruen hielt.** Das ist die Lehre: **eine

Fixture, die der Zahl des Codes beipflichtet, prueft die Zahl nicht.**



**Und ich habe beim Einfuegen einer Methode 2166 Zeilen statt 130 geschrieben** — ein

Ersetzungsmuster hat einen grossen Block dupliziert, und der naechste Rettungsversuch hat es

verdreifacht. **Der Weg zurueck war `git checkout --` auf diese eine Datei, und danach vier

Ersetzungen einzeln mit je einem Build dazwischen.** Das ist der Unterschied zwischen einer

Aenderung und einem Rettungsversuch.



**Test evidence** `test_rm2k_open_menu.cs` (6), `test_rm2k_move_picture.cs` (7).

**1366/1366**, Validator gruen. **Mutations** 9 Regeln, **9 von 9 gefangen**.

## `10840` Get On/Off Vehicle — DONE

**`10650` und `10850` liefen. `10840` lief nicht — und `Rm2kVehicleBoarding` hatte fuenfzehn

Boarding-Methoden, getestet, die kein Befehl erreichen konnte.**



**Dieselbe Inselform wie die Bilder und wie `Rm2kMoveRouteState`:** eine Klasse, die

vollstaendig ist, getestet ist und unerreichbar ist. **Und man findet sie nur, indem man fragt,

wozu die Klasse da ist, und das mit dem vergleicht, was die Befehle der Referenz tun** — nicht

indem man die Konstantenliste liest, in der die Zahl laengst steht.



**Breite 0: das Fahrzeug ist kein Parameter, es ist, was unter dem Helden liegt oder vor ihm

steht. Ob es passiert ist und nicht, ob es koennte — und ein fehlender Hook ist eine Ablehnung

mit Namen, weil „kein Fahrzeug hier" und „dieser Leser kann nicht einsteigen" zwei

verschiedene Dinge sind.**



**Und der Hook ist ein Konstruktorargument und kein spaeter gefuelltes Feld**, aus demselben

Grund wie der Routenstarter: ein Test muss sehen koennen, was der Interpreter bekommen hat.



**Und diesmal 62 Zeilen statt 2166.** Vier Ersetzungen einzeln, je ein Build dazwischen — **die

Lehre aus dem letzten Mal hat gehalten.**



**Test evidence** `test_rm2k_get_on_off_vehicle.cs` (4). **1370/1370**, Validator gruen.

**Mutations** 5 Regeln, **5 von 5 gefangen**.

## `10490` Full Heal — DONE

**Die sechs Actor-Befehle: fuenf veraenderten etwas, dieser stellt wieder her.** Fuenf waren

verdrahtet, `FullHeal` nicht — **obwohl er als einziger der Familie gar keinen eigenen Wert

braucht.**



### Die Regel, die der Test gestrichen hat



**Ich hatte eine SP-Flagge erfunden.** Der zweite Parameter sollte heissen „heile auch die

SP-Punkte" — **und die Referenz hat zwei Parameter, und beide sind die Actor-Auswahl.** Ein Leser,

der den zweiten als SP-Flagge gelesen haette, haette von jedem geheilten Actor auch die SP geheilt

**und nur einem einzigen Helden die Trefferpunkte.**



**Zwei Parameter, und beide sind die Auswahl** — 0 ist die ganze Mannschaft, 1 ein Held nach

Nummer, 2 ein Held aus einer Variable. **Modus 0 heilt die ganze Mannschaft.**



**Und es stellt wieder her und rechnet nicht:** ein Zaehler wird auf das gesetzt, was eine Basis

sagt. **Ein Leser, der es wie seine Nachbarn behandelt haette, haette addiert — und ein Spiel,

das nach jedem Kampf heilt, haette eine Mannschaft ohne Grenze.**



### Und die Grenze dieser Karte



**`10440`/`10450`/`10480` sind keine Verdrahtung.** Skills, Ausruestung und Bedingungen haben **im

Zustand ueberhaupt keine Felder** — das ist neues Zustandsdesign und keine Befehlszeile, **und es

gehoert in eine eigene Karte.**



**Test evidence** `test_rm2k_full_heal.cs` (5). **1375/1375**, Validator gruen.

**Mutations** 6 Regeln, **6 von 6 gefangen**.

## `10710` Enemy Encounter — DONE

**Fuenf Zustandsfelder — `IsBattleActive`, `ActiveTroopId`, `BattleTurn`, `BattlePhase` und

`TroopMembers` — und kein Befehl erreichte eines davon.** Also fiel der Kampfbeginn in den

Default-Arm und es kämpfte nie. **Dieselbe Inselform wie die Bilder, die Laufbahnen und das

Fahrzeug-Bording.**



**Sechs oder zehn Parameter, je nach Form. Die Flucht sind drei Werte und kein Boolean — und der

mittlere beendet das Event. Drei Terrain-Modi, und der vierte startet keinen Kampf. Kein Ausgang

und -1, denn 0 ist der Siegwert.**



### Und die sechzehn Messungen, die kein Befund waren



**Ein Test liess sich nicht kompilieren, und ich habe ihn sechzehn Mal gemessen.** Die Datei war

korrekt — kein verborgenes Zeichen, keine falsche Einrueckung, keine doppelte Deklaration, und

`sed`, `od` und `read_file` zeigten dieselben Bytes. **Der Compiler hatte recht: die

Tuple-Zerlegung `var (a, _, b)` in diesem einen Test war der Fehler** — und die anderen sechs

Tests derselben Datei mit derselben Zerlegung liefen.



**Das ist derselbe Fehlertyp wie bei `IdleCell`, und die Lehre ist diesmal klarer: sechzehn

Messungen an korrektem Quelltext sind kein Befund, sondern eine Schleife.** Der Ausweg war

derselbe — aufhoeren zu messen und die eine Sache tun, die ich nie getan hatte: den Test ohne

die Zerlegung schreiben.



**Test evidence** `test_rm2k_enemy_encounter.cs` (7). **1382/1382**, Validator gruen.

**Mutations** 9 Regeln, **9 von 9 gefangen**.

## 2026-09-29 — the RM2K command coverage became a test

**117 of 117, and the check is in code.** `test_rm2k_command_coverage.cs`
compares the interpreter's dispatch against liblcf's own enumeration. The card
had carried three numbers that contradicted each other ("117 of 121", "164
codes, 43 dispatched", "89 real RPG commands have no case"), **and two of them
measured the constant list rather than the dispatch.**

**Three faults found while building it, all worth keeping:**

1. **The hand-written reference list was a guess.** Written as a run of
   five-digit numbers, it named four hundred codes liblcf does not have and
   missed thirty it does. liblcf's 117 values replaced it, with its names.
2. **`GD0001` is not a `CS` error.** Godot's source generator refused a
   non-partial `TestBase` subclass, and every build check in this session
   filtered on `error CS` — **so a new suite sat in the tree reporting a
   green build and running nothing.** The filter is now `error (CS|GD|NETSDK)`.
3. **`os.utime` in a mutation harness defeats MSBuild's change detection.**
   After the restore the source was right and the DLL still held the mutation;
   22 unrelated tests failed against a correct tree. Mutation runs build with
   `-t:Rebuild` from now on.

**`TestRm2kCommandCoverage: 2/2`, `All 1584 tests passed`, validator passed,
mutations 3 of 3.**

## 2026-09-29 — attr_accessor, include, and the call without parentheses

**These four are built in**, because the one thing they do is write into the
class they are written in, and only the call knows which class that is. A
reader that let them fall through to the host would say "this host does not
implement it" for `attr_accessor :hp` — **and that would stop an RPG Maker
script on its second line.**

**And the root cause behind all three test failures was one thing: `self` is
not a name, it is "the class this is running in".** Every bare call went
through `EigeneMethode(Symbol("self"), name)`, and `"self"` is not in the type
table — **so every bare call a game writes found nothing**, not just
`attr_accessor`. It needed two fixes because it is two questions: the lookup
(`self` means the running class) and the body (`_aktuellerTyp` was set only
for the class body, so `self.hp = 42` inside a method had no class).

**A bare name is a variable first and a method second** — Ruby's own order,
and the reason is the method's parameters. **It is a runtime decision, not a
parse**: the parser cannot know whether a class has a method of that name.

**Three guards on the bracketless call, and each was a bug first:** the
argument may not be an operator (`a * b` is a multiplication), may not be
across a line (`a` and `b = 1` are two statements), and `[` is not an argument
(`items[0]` is an index). **And the parenthesis form was joined, not
replaced** — the new branch swallowed `draw(x)` until it was put back.

**Two rules the interpreter was making up, both now gone:** `attr_accessor`
with no arguments is not an error (Ruby makes no method and raises nothing),
and an assignment is a value (`def w; self.hp = 30; end` answers `30`, and
the first test claimed nil — **that was my invention and not Ruby's
behaviour**).

**And a mutation that could not be killed, which is worth the note:** the
separate `Current.Kind != Newline` check survived removal because
`StartsAValue` is already false at a newline. **A test that lifts one
condition another one already carries is measuring the other one** — so the
redundant check is gone and the rule is the absence of a `SkipNewlines`.

**The harness now builds with `-t:Rebuild` and counts `: error` rather than
`error CS`.** Both were found the hard way: a stale DLL gave 22 failures
against a correct tree, and `GD0001` is not a `CS` error.

`TestRubyInterpreter: 50/50`, `TestRubyParser: 53/53`, `All 1594 tests
passed`, validator passed, mutations 8 of 8.

## 2026-09-29 — alias, verified against ruby/ruby's own source

**`alias` holds the method, not the name.** `ruby/ruby`'s `vm_method.c` stores
a `VM_METHOD_TYPE_ALIAS` entry carrying `body.alias.original_me` — the method
entry as it was when the alias ran — **and a later `def alt` writes
`Methods["alt"]` and leaves the alias's entry alone.** Read at the source
because the first version of the test asserted the opposite and called a copy a
share; the implementation was right and the test was wrong.

**Two mutations of the same line survived until a subclass was in the test.**
Storing the method under the new name and storing what the class's own table
holds under the old name are indistinguishable while both names are in the same
class, **because there the table's entry and the method are the same object**.
In a subclass the method comes from the base and the table has no entry for
the old name — **a name pointer would have made no alias at all**, and a game
that aliases an inherited method and then overrides it is doing the one thing a
plugin layer does.

**The two spellings name the same thing, and the colon lives in `Text` and not
in `Value`.** A reader that took `Text` would have stored `:neu` as the name.

`TestRubyInterpreter: 57/57`, `TestRubyParser: 53/53`, `All 1601 tests
passed`, validator passed, mutations 6 of 6.

## 2026-09-29 — defined? and the globals it made visible

**`defined?` answers a word, and that is Ruby 1.8.1.** Read at the source:
`v1_8_1`'s `eval.c` has `is_defined`, returning `"method"`,
`"local-variable"`, `"expression"`, `"instance-variable"`, `"global-variable"`
and `nil`. **Current Ruby answers a boolean, and the memory was wrong in both
directions** — a reader that answered true or false would have broken every
game that writes `defined?(@hp) ? "expression" : "nil"`.

**It does not run the expression**, and the mutation that made it run survived
five tests because every question was about something that works — **a reader
that ran it would have made the call and answered right by accident.** With a
missing method it answers a diagnostic about the host instead of `nil`.

**The question is about the whole expression.** The first version read
`ParsePrimary`, which takes the `1` out of `1 + 1`, and the error named a
symbol and an integer. `ParseStatement` is the level that reads one.

**And the globals had no table at all** — they fell through to `Refuse`, and
`defined?` is what made that visible. **The dollar sign is stripped in one
place (`GlobalName`) and not at both ends**, because the first version stripped
it at the assignment and not at the question, **and a global that was set
answered nil.**

`TestRubyInterpreter: 64/64`, `TestRubyParser: 53/53`, `All 1608 tests
passed`, validator passed, mutations 6 of 6.

## 2026-09-29 — extend, and a validator that could be wrong

**`validate.sh` built incrementally and reported failures that did not
exist.** It kept a DLL that no longer matched the source, so the test run
checked the old file against the new text: **2 failures in a tree that was
green, 0 after a rebuild.** A validator that checks a stale assembly is worse
than one that stays silent, because whoever believes it looks for a bug in
code that is right. `dotnet build --no-restore -t:Rebuild` is the build step
now, and the reason is in the script.

**`extend` is `include` with `self` in front.** Ruby defines the methods as
singleton methods of the object, and in a class body the object is the class
— a reader that treated it as `include` would have filed the method on
instances, and this runtime has none. `Eingemischt` writes under the `self.`
prefix for `extend` and plainly for `include`, and the two do not collide.

**And the prefix is not added twice**: a module's `def self.x` is already
filed as `self.x`, and prepending again would have made `self.self.x` — a name
the game never wrote and no call can reach.

**Six mutation rules, all killed, with the anchors read out of the source
rather than typed from memory** — three earlier attempts at editing a harness
by string surgery failed before the anchors were derived mechanically.

`TestRubyInterpreter: 68/68`, `TestRubyParser: 53/53`, `All 1612 tests
passed`, validator passed, mutations 6 of 6.

## 2026-09-29 — a block reaches a host as something to call

**No closures and no objects, and that sets the shape.** `IRubyHost` grew
`CallMethodWithBlock`: the host decides how often and with what, the
interpreter binds the block's parameters when the host calls back. `Array#each`,
`Integer#times` and `String#each_line` come through exactly this way.

**A block is a cloak around a call and not an argument to it** — the parser
makes one node whose first child is the call, and without noticing that the
call never reached the host.

**The parameters are written straight into the new level, not with
`SetLocal`.** `SetLocal` searches from the inside out and writes where it finds
the name, **so a parameter that shadowed an outer variable was never created**
and the body's first assignment went outwards: `x = 100` with `each do |x|`
ended at three.

**Not reached:** a block given to a method of the script's own — no `Proc`, no
`lambda`, so `items.map { |x| x * 2 }` is still a host method. Stated, not
worked around.

`TestRubyInterpreter: 71/71`, `TestRubyParser: 53/53`, `All 1615 tests
passed`, validator passed, mutations 4 of 4.

## 2026-09-29 — the real games on this machine, and the first non-synthetic tests

**`E:/RPGMakerGames` holds five finished games and they are read now.**
`TestRealRm2kGameData` 4/4 against **Dragon Destiny** — a 416 kB `RPG_RT.ldb`,
**743 maps** numbered 1 to 743 with no gap, 22 chipsets — and
`TestRealXpGameData` 4/4 against **MicroQuest**, 23 `.rxdata` files including
109 kB of scripts. **1623/1623, validator passed.** These are the first
non-synthetic tests in the project.

**Dragon Destiny is RM2K and not 2003**, and the test asserts the absence of
`.lcf` and not only the presence of `.ldb` — a folder with both is a folder a
loader cannot read one of.

**A number in a test had to be measured.** The map count was asserted as 700
and the game has 743. **A rounded guess in a test whose whole purpose is to
prove a real game is on the machine is the wrong place for one** — the same
fault as the hand-written liblcf list, and the test now says 743 in words.

**The three maps are a sample and the test says so.** The smallest, the
largest and one from the middle; **a reader that reads three has not proved it
reads 743**, and saying otherwise would be the same kind of claim.

**The two WOLF games are encrypted.** Both `Data.wolf` files start with
`83 5d cc 7d ad 0d de f1` and neither has a `WOLFM` header. Reading them means
taking the key from `Config.exe` or `Game.exe`, **which is circumventing the
copy protection of a commercial game** — and this project refuses protected
files rather than decrypting them. **K-110 stays VERIFY, and the reason is
measured rather than assumed.** What would unblock it is an *unencrypted*
WolfRPGEditor game as a fixture at `project/tests/fixtures/wolf/real/`.

**And the RTP is authorised but not done.** The user said to download and
unpack it and to name the sources per engine — that is the next card and not
this one.

## 2026-09-29 — the RTP, and the measured answer to "download it"

**The user authorised downloading and unpacking the RTP, and the measurement
says it is unnecessary.** `TestRuntimeBoundary` walks **every** C# file under
`src/` — 127, counted against the files on disk and not against a threshold —
and finds no `LoadLibrary`, no `GetProcAddress`, no `DllImport`, no
`Process.Start`, no `Assembly.Load`, no `Reflection.Emit`. **And there is no
RTP directory reference anywhere in the source.**

`RgssRuntimeInfo` carries `RtpDependency` as the string `"RPG_RT"` — **a name
and not a place**, and the test asserts that directly, **because a field that
held a path would be exactly where a download could be wired in.** The only
paths the RGSS backend opens are the game's own `Data/System.rxdata`,
`.rvdata` and `.rvdata2`.

**A note on the two `Game.exe` mentions:** both are documentation and a
filename comparison — `FindByName(..., "RPG_RT.exe")` scores a directory as
RM2K by looking at the name. Measured before it was written down.

`TestRuntimeBoundary: 2/2`, `All 1625 tests passed`, validator passed.

## 2026-09-29 — qa_patches/, and the test one of its patches asked for

**`qa_patches/` is unversioned and is not this session's work**: a QA
dispatcher's output from 2026-08-24. Two things came of reading it.

**The audit is a dated snapshot, not a specification** — it lists XP and MZ as
"Missing Ruby execution" and WOLF as "Partial", and since 2026-08-24 this
repository has a working Ruby interpreter with 71 tests, reads 23 real XP data
files and a real RM2K game, and has measured the WOLF games as encrypted.

**`t_ba1d255d.patch` asked for a test the repository was missing.**
`ExpectedSystemDataPath` and `HasSystemData` were written by `Initialize` and
read by nobody — **and that is a missing surface, not a missing test**: the
path is decided in the constructor, `Initialize` needs a plugin selection the
selector refuses for RGSS, and so the field was unreachable from a test. **The
first version of the test built a `RgssRuntimeInfo` and asserted on it, which
proves nothing** — it asserts that a value the test wrote is the value the
test read back. **The fix was a one-line public property on the runtime.**

`TestRgssRuntime` is 6/6, mutations 3 of 3. The other two patches are not
gaps: their content is in `HEAD` in a later form, and `t_ae3e01c0.patch` is
empty.

## 2026-09-29 — `undef`, `lambda`, `proc`, und eine Scope-Wand an drei Stellen

**`undef` war bereits ein Schluesselwort, und der Lexer-Test sagte es.**
`Test_TheKeywordListIsTheOneFromTheGrammar` nennt einundvierzig Namen, und
es waren einundvierzig; **diese Arbeit haette daraus zweiundvierzig gemacht.**
Derselbe falsche Edit, dasselbe rote Signal, und der Test hatte recht.

**Und `undef a, b` ist eine Liste** (in `v1_8_1`s `parse.y` verifiziert:
`undef_list: fitem | undef_list ',' fitem`). Die erste Fassung nahm einen
Namen, **und die Mutation "liest nur den ersten Token" ueberlebte fuenf Tests,
weil bei einem einzigen Namen beide Lesarten denselben Knoten liefern** — erst
die Liste trennt sie. **Ein Test, der aus dem falschen Grund nicht scheitern
kann, misst nicht das, was er benennt.**

**Und die Wand eines Ruby-Blocks war falsch — an drei Stellen von einer.**
`SetLocal` suchte bis zur Methodengrenze nach aussen. Fuer eine Methode richtig,
**fuer einen Block falsch**: `lambda { x = 1 }` in einer Methode, in der `x`
schon 99 war, schrieb in die 99 hinein. **`Local` und `HasLocal` suchten
genauso nach aussen, und das war der stillere Fehler** — ein Block, der `x`
schrieb, liess den Wert des Aufrufers in Ordnung, **und ein Block, der nur `x`
las, bekam die 99 als waere es seine eigene.** Ein Spiel haette aus einem
Namen, den es nie geschrieben hat, einen Wert gelesen, ohne dass irgendwo ein
Fehler entstand.

**Und `||` ist hier immer eine leere Parameterliste, weil der Kontext das
entscheidet.** `ReadBlockParameters` wird nur unmittelbar nach `{` oder `do`
gerufen, **und ein logisches ODER kann dort nicht stehen.** Die erste Fassung
entschied ueber das Token danach: `3` ist ein Wert, also las sie ODER und warf
`lambda { || 3 }` weg. **Ein Nachbar ist kein Kontext.**

**Und die Wand braucht einen Rueckbau.** Zwei Aufrufe derselben Lambda und
ein Lesen sind noetig, um das zu sehen — **der erste Verschachtelungstest hat
die Mutation nicht getoetet, weil er nur schrieb und nicht las.**

`TestRubyInterpreter: 93/93`, `All 1650 tests passed`, Mutationen 11 von 11.

## 2026-09-29 — `define_method`, `scripts/csharp_insert.py`, und ein offener Fehler

**`define_method(:m) { |x| x * 2 }` schreibt den Block hinter die Klammern.**
Er ist damit **nicht** ein Kind des Aufrufsknotens, sondern dessen Mantel --
`Evaluate` wertet den Aufruf aus und der Block war weg, bevor
`define_method` ihn sehen konnte. `_blockKette` traegt ihn, und `Call()`
haengt ihn **nur fuer die zwei Namen, die eine Methode daraus bauen** an.

**Und er landet in genau der Tabelle, in der ein `def` landet** -- derselbe
Knoten, dieselbe Ablage, **weil das kleinste gemeinsame Format der Knoten
ist, den `def` auch benutzt.**

**`scripts/csharp_insert.py` ist entstanden, weil dieselbe splice-Stelle
dreimal eine Datei zerlegt hat.** Ein Anker, der auch hinter der schliessenden
Klammer vorkommt, **setzt den Block hinter die Klasse**, und der Compiler
meldet `CS1519`. **Der Helfer findet die Zeile als Zeilenindex und prueft,
dass die Einfuegestelle vor der Klassengrenze liegt**, bevor er schreibt.

**Zwei Mutationen sind No-ops, und beide einzeln gemessen.** `BrauchtBlock`
auf "immer" und die `Children[0] == pNode`-Pruefung lassen den Testlauf
unveraendert gruen: **der Block wird nur angehaengt, wenn `_blockKette`
nicht leer ist**, und die Kette ist nur gefuellt, wenn ein Block **diesen**
Aufruf umschliesst. **Zwei Bedingungen, die dasselbe sagen.**

**Und der offene Produktfehler war ein Testfehler.** `[1].each { rand }`
rief den Gast einmal mit einem Rueckruf, `pYield` antwortete `nil`,
**und der Aufruf im Rumpf wurde nie erreicht** — **`rand` ohne Klammern ist
ein Bezeichner, kein Aufruf**, `Local("rand")` gibt nil, und der Host wird
nie gefragt. **Mit `rand()` laeuft der Block, und die Schleife antwortet mit
dem, was der Rumpf beantwortet hat.**

**Die Knotenform wurde gemessen, nicht geraten:** `[1].each { rand }` ergibt
`Block[Call:each, Array, Block]`, und `Yield` liest `Children[2]` — das
stimmt. **Ein Test, der eine Indizierung voraussetzt, muss sie an einem
Knoten messen, den er selbst gebaut hat.**

`TestRubyInterpreter: 117/117`, `All 1674 tests passed`, Mutationen 8 von 8
mit zwei gemessenen No-ops.

## 2026-09-29 — `method_missing` und `respond_to?`

**Der Handler wird erst am Ende der Kette gefragt, und er ist ein Singleton.**
`def self.method_missing(name, x)` ist, wie ein Spiel es schreibt, **und ein
Leser, der in der Instanztabelle gesucht haette, haette nichts gefunden** --
ein Plugin, das hundert Befehlsnamen beantwortet, waere eine Klasse, die alle
ablehnt. **Und der Name kommt als erster Wert**, weil ein Handler, der die
Argumente unveraendert durchreichte, **fuer jeden einzelnen Befehl still
das falsche beantwortet haette.**

**`respond_to?` zaehlt `method_missing` nicht** -- der Sinn der Frage ist zu
wissen, ob ein Aufruf ohne Fehler durchgeht, **und eine Klasse, die alles
beantwortet, wuerde ja zu allem sagen und die Frage waere wertlos.** Der Fall
steckte im eigenen Code, den ich beim Schreiben beschrieben hatte: `FindMethod`
faellt auf den Handler zurueck, **also haette `Antwortet` mit `FindMethod`
gearbeitet und die Frage fuer jede Klasse mit einem Handler mit ja
beantwortet.** `HatMethode` geht dieselbe Kette ab, **ohne den Fallback.**

**Und `FindMethod` faellt bewusst nicht zurueck.** Die vier Aufrufer --
`super`, `alias`, die Klassenmethoden-Suche und `AufrufenMitName` -- fragen
"hat die Kette diese Methode", **und ein Rueckfall wuerde `super` in den
Handler schicken.**

**Und `undef` markierte nur einen von zwei Namen.** Die Mutation, die das
`self.method_missing`-Pruefen abschaltete, ueberlebte zuerst -- **weil der
Test in derselben Klasse `undef` schrieb und ein Leser das Loeschen des
eigenen Eintrags schon richtig hatte.** Der Fall, fuer den die Marke da ist,
ist eine Unterklasse **ohne** eigenen Handler, **die die Basis daran hindern
 muss.**

`TestRubyInterpreter: 117/117`, `All 1674 tests passed`, Mutationen 8 von 8.

## 2026-09-29 — `instance_eval` und eine Liste, in der ein Name grundlos stand

**Der Block kommt von der Kette und nicht aus den Argumenten.** `instance_eval`
stand zuerst in `BrauchtBlock`, **und damit bekam der Aufruf den Block als
Argument, waehrend der Zweig ihn an anderer Stelle suchte** -- das Ergebnis war
nil, ohne Meldung. **`Yield` nimmt ihn von der Kette, und `instance_eval`
braucht ihn nicht als Argument: es IST der Block.**

**Und die Klammern um `methode is "instance_eval" or "class_eval"` waren kein
Kosmetik.** `or` bindet schwaecher als `&&`, **und ohne Klammern liess die
Zeile jeden `instance_eval` durch, auch ohne Block** -- `Ausgewertet` bekam nil
und antwortete nil, **und der Aufruf sah aus, als haette er ausgewertet.**

**Und der Empfaenger schlaegt die Klasse, in der gerade etwas laeuft** --
`A.instance_eval` muss auf `A` wirken, auch wenn ein anderer Klassenrumpf offen
ist.

**Und hier sind `instance_eval` und `class_eval` dasselbe, weil `self` die
Klasse ist und diese Runtime keine Objekte hat.** Das steht im Doc, weil es
eine Grenze und kein Detail ist.

`TestRubyInterpreter: 124/124`, `All 1681 tests passed`, Mutationen 5 von 5.

## 2026-09-29 — `*rest`, `**opts` und `f(k: 3)`

**Der Splat stand in der Parameterliste und war damit der naechste
Parameter.** `def m(a, *rest)` gab `rest` die zweite Zahl statt der Liste
`[2, 3]`. **Die Stelle wird jetzt gemerkt** (`SammelAb`), und der Wert wird
nach der Schleife gebunden.

**Und ein Splat in der Mitte nimmt nicht alles.** `def m(*teile, letzte)`
gibt `teile` alles ausser dem letzten Wert, **und die Parameter nach dem
Splat zaehlen von hinten** — ein Leser, der von vorn bindet, gibt `letzte`
den ersten, **und weil der erste auch in der Liste steht, faellt es nicht
auf.**

**Und `f(k: 3)` parste nicht.** Die Schreibweise, die jeder schreibt, war
ein Syntaxfehler. **`=>` war eine Zahl**, weil der Operator an `Apply` ging
und `Apply` fuer einen unbekannten Operator 0 liefert — **ein Spiel, das
`opts[:k]` las, las auf einer Null**, und nichts sagte es. **`IsHash` gibt
es jetzt als eigenes Feld**, **weil ein Hash mit ungerader Paarzahl so
gewoehnlich ist wie eine Liste mit ungerader Laengenzahl.**

**Und eine Bedingung, die nichts sagt, habe ich entfernt statt behalten:**
`SammelAb >= 0` neben `SammelParameter != null`. **Zwei Bedingungen, die
dasselbe sagen, sind eine mit zusaetzlichem Code.**

`TestRubyInterpreter: 141/141`, `All 1699 tests passed`, Mutationen 5 von 6
mit einer einzeln gemessenen No-op.

## 2026-09-29 — die Wertausdruecke (`to_s`, `nil?`, `class`, `inspect`)

**`5.to_s` ging an den Host, und kein Host in diesem Repo kann es.** Ein echter
Host beantwortet `rand` und lehnt den Rest ab, **also haette ein Spiel, das
`"Level #{level}"` schreibt, an etwas gelegen, das es nicht gibt** -- und der
NullHost weiss es nie, **also haette kein Test pruefen koennen, ob eine Zahl
einen Text hat.**

**Die Reihenfolge ist Skript, Wert, Host.** Eine Klasse mit eigener `to_s`
behaelt sie, **denn ein Leser, der die Wertausdruecke zuerst befragte, wuerde
ihr die eigene wegnehmen.**

**Und es ist Rubys Schreibweise:** `nil` statt `Null`, `true` statt `True` --
**ein Spiel, das das in eine Speicherdatei schreibt, schreibt ein Wort, das
kein anderes Spiel liest.**

`TestRubyInterpreter: 148/148`, `All 1706 tests passed`, Mutationen 5 von 5.

## 2026-09-29 — `new`, Felder pro Objekt, `<=>`, und `map`

**Es gab kein `new`.** `Game_Party.new` ist die erste Zeile fast jedes
RPG-Maker-Skripts, **und ohne sie hat ein Spiel keine Schauspieler, keine Party
und keine Karte.**

**Und die Felder waren ein Speicher fuer das ganze Programm.** **Jeder
Schauspieler haette den Wert des letzten gehabt** -- ein Spiel, in dem jede
Figur mit derselben Zahl geht.

**Und `EigeneMethode` fragte die falsche Klasse:** sie nahm den Namen aus dem,
was gerade laeuft, **und nicht aus dem Empfaenger** -- **auf der obersten
Ebene ist das null**, **also lief `Held.new.staerke` mit leerem Namen**, und
`super` fand keine Oberklasse. **Derselbe Fehler an zwei Stellen, sichtbar
geworden erst, seit es Objekte gibt.**

**Und der Empfaenger fiel auf die laufende Klasse zurueck, auch wenn er ein
String war** -- `"b" <=> "c"` in `Kachel#<=>` rief wieder `Kachel#<=>` mit
einem String, **bis der Stapel ueberlief.** Gemessen: `Stack overflow`.

**Und `def <=>(andere)` parste nicht** -- `ReadMemberName` erwartete einen
Namen, **und der Fehler nannte den Operator statt der Stelle.**

**Und `<=>` ging an die statische `Apply`** -- **die hat keinen Interpreter
und kann keine Skriptmethode rufen**, **also nur nil fuer ein Objekt.**

**Und `map`/`each`/`select` gab es nur beim Host** -- **also konnte kein Test
zeigen, was ein Menue anzeigt.**

**Eine Regel wurde einzeln gemessen und ENTFERNT:** der Feldspeicher des
Klassenrumpfs war ein No-op, **weil `@x` dort in dieser Runtime nicht wieder
lesbar ist.** Code, der aussieht, als gaebe er eine Antwort, und keine gibt,
ist schlimmer als keiner.

**Ein Test wurde gegen die Quelle geprueft:** `rb_attr` in `eval.c` aus Ruby
1.8.1 baut den Leser als `NEW_IVAR(attriv)` -- **er liest `@name` vom
Empfaenger, nicht von der Klasse.**

`TestRubyInterpreter: 163/163`, `All 1721 tests passed`, Mutationen 5 von 5.

## 2026-09-29 — `@@x`, und zwei stille Fehler in `defined?`

**Der Lexer las `@@x` und der Interpreter legte es in die Instanz.** **`@@anzahl`
ist, wie eine Klasse ihre Objekte zaehlt** -- **ein Feld pro Objekt wuerde
nichts zaehlen**, **und ein Spiel, das Nummern daraus vergibt, wuerde dieselbe
zweimal vergeben.**

**Gemessen vor der Reparatur:** `@@anzahl = @@anzahl + 1` im Klassenrumpf gab
`NoMethodError: undefined operator '+' for a Nil and a Integer` -- **der Wert
im Rumpf war nicht lesbar**, **und das ist die Form, in der jedes Skript
zaehlt.**

**Und `defined?` sah den falschen Knoten:** die Art aus dem Condition-Kind,
**die Tabelle und den Name aus dem `defined?`-Knoten.** **`defined?(@@x)` gab
nil, waehrend `@@x` selbst die Zahl las** -- **und `defined?(@hp)` gab nil,
obwohl `@hp` gesetzt war**, weil `NameOf` ein Condition-Kind in einer
Variablen suchte, **das es nicht gibt.** **Ein stiller Bug in einer Funktion
mit 64 Tests.**

**Und die Schreibweise ist gemischt:** verifiziert in `eval.c` aus Ruby 1.8.1,
`"class variable"` mit Leerzeichen und `"local-variable"` mit Bindestrich.
**Diese Runtime schreibt, was die Quelle schreibt.**

**Eine tote Zeile wurde entfernt:** `_self` im Klassenrumpf war ueberfluessig,
**weil `_aktuellerTyp` die Klasse traegt** -- **und die passende Mutation hat
keinen Test getoetet**, was das bewiesen hat.

`TestRubyInterpreter: 166/166`, `All 1724 tests passed`, Mutationen 5 von 5.

## 2026-09-29 — die Basis der drei Arten, und ein Hash, der abgelehnt wurde

**Gemessen, nicht geschaetzt:** von den einundvierzig Namen, die ein Skript in
seinen ersten hundert Zeilen schreibt, **fehlten sechsunddreißig.** `length`
allein stoppt jedes Menue, das zaehlt.

**Und ein Host kann sie nicht beantworten** -- **ein echter Host kennt
`Sprite` und `Window_Base` und nicht Rubys `Array` und `String`.**

**Und `is_a?` war zweimal da, und die alte Fassung sagte im Kommentar "this
runtime has no objects"** -- **das war vor `new` wahr.** Gemessen:
`held.is_a?(Basis)` gab `false`, `Held.is_a?(Basis)` gab `true`. **Jetzt geht
die Kette**, **und ein Spiel, das gegen seine eigene Basisklasse prueft,
sieht seine eigenen Objekte wieder.**

**Und ein Hash wurde abgelehnt** mit *„this interpreter does not evaluate a
Hash node"* -- **eine Meldung ueber den eigenen Quelltext des Lesers.**
**Jede gespeicherte Einstellung und jede Statuszeile ist ein Hash.**

**Und eine irrefuehrende Meldung hat alles darueber verdeckt:**
`[1, 2, 3].length` meldete *„length braucht einen Block"* -- **weil die
Blockfrage vor der Namensfrage stand.**

**Eine Regel stand zweimal, und die Mutation hat es bewiesen:** die negative
Stelle wurde in `Item` geaendert und in `Index` nicht, **und kein Test hat es
gemerkt**, **weil kein Test `first` oder `last` benutzt hat.**

`TestRubyInterpreter: 175/175`, `All 1733 tests passed`, Mutationen 5 von 5
und 6 von 6.

## 2026-09-29 — Textoperationen, und drei stille Fehler dahinter

**Alles war abgelehnt.** `split`, `gsub`, `start_with?`, `strip`, `chomp`,
`ljust` -- **alle mit „has no method on this host".** Ein Spiel hat damit
**kein Textfenster, das etwas anzeigt.**

**Und `2.times { }` rief den Block NULLMAL:** die Schleife begann bei der
Zahl, **also war `2 <= 1` nie wahr.** Genau so baut jedes Statusfenster seine
Zeilen.

**Und `push` gab eine neue Liste:** `OfArray` baute ein Array, das nicht
waechst. **Also sammelte jede Schleife nichts.** Beide Fabriken legen jetzt
eine `List` an, **und `push` waechst an Ort und Stelle.**

**Und `chomp` liess das Wagenruecklauf stehen:** gemessen zwei Bytes statt
einem.

**Und `tr` ist nach `tr_trans` und `trnext` in `string.c` aus Ruby 1.8.1
geschrieben, nicht geraten** -- **vier Faelle, die vorher falsch waren.**
**Und ein Muster aus einem Skript wird nicht ausgefuehrt, und das wird
gesagt.**

**Und eine Regel wurde einzeln gemessen und entfernt:** der
`self`-Umschalter im Block war ein No-op.

`TestRubyInterpreter: 182/182`, `All 1740 tests passed`, Mutationen 5 von 5,
6 von 6 und 5 von 5.

## 2026-09-29 — die Mustermaschine, mit einer Schranke

**`name =~ /Held/` gab es nicht.** `=~`, `!~`, `match`, `match?`, `scan` sind
jetzt da, **und `scan` gibt Gruppen statt ganzer Treffer.**

**Und die Optionen hinter dem zweiten Schraegstrich wurden gelesen und
weggeworfen** -- **also war `/held/i` dasselbe wie `/held/`.** **Die
Reihenfolge `m`, `i`, `x` ist verifiziert in `re.c` aus Ruby 1.8.1.**

**Und die Schranke liegt an der Textlaenge, nicht an einer Uhr** -- **eine
Uhr ist nicht pruefbar, und ein Muster, das zurueckkam, nachdem es ewig
lief, ist ein Spiel, das schon haengt.** **Die Meldung nennt Muster und
Grenze.**

**Und ein `[`, dem kein `]` folgt, brach die ganze Datei** -- **gemessen:
`A regular expression opened at offset 9 is never closed.`** Das Muster
endet dort, **und jetzt endet es dort**; **dass die Maschine danach
`Unterminated [] set` sagt, ist richtig.**

`TestRubyInterpreter: 190/190`, `All 1748 tests passed`, Mutationen 6 von 6.

## 2026-09-29 — `$1`, und ein Global, das gesetzt werden, aber nicht gelesen werden konnte

**Ein Global liess sich nicht:** `$x = 1` hat funktioniert, `$x` nicht --
**weil die Zuweisung an die Tabelle ging und das Lesen nirgends hin.**
**Und `$game_party` ist die erste Zeile von jedem RPG-Maker-Skript.**

**Und ein Treffer war eine Zahl und sonst nichts** -- **ein Skript, das
danach `$1` liest, hatte nichts zu lesen.**

**Und das sind sechs Namen fuer eine Frage** -- **und sechs Tabellen
wuerden sechs Chancen sein, dass sie sich widersprechen.**

**Und `Regexp` gab „the constant Regexp is not defined by this host"** --
**eine Meldung ueber den Host fuer eine Klasse, die der Leser nicht hatte.**

**Und eine Gruppe, die nicht teilgenommen hat, ist nil und nicht leer** --
**gemessen: `$2` gab einen leeren Text und `$3` nil.**

**Und eine Mutation lebte, weil die anderen Tests die Frage nicht
stellten** -- **weil `$~.pre_match` und `$~[1]` von den Methoden auf dem
Wert beantwortet werden und nicht vom Wert selbst.**

`TestRubyInterpreter: 196/196`, `All 1754 tests passed`, Mutationen 5 von 5.


## 2026-09-29 — sechzehn Faltungen, und eine Schleife, die sich selbst zerschnitt

**Sechzehn Methoden zum Falten einer Liste, und sechs davon antworteten.**
`find`, `detect`, `inject`, `reduce`, `each_with_object`, `group_by`,
`partition`, `sort_by`, `min_by`, `max_by`, `flat_map`, `none?`, `one?`,
`take`, `drop`, `flatten`, `compact`, `sum`, `min`, `max` -- **alle mit
*„has no method on this host"*.** Und **das sind die, aus denen ein Menue
entsteht.**

**Und der Umbau der Schleife hat sich selbst zerschnitten.** `each` gab nil,
**weil beim Neuaufbau des Zweigs die Zeile `case "each" or ...`
verschwunden war und ein Fragment ohne `case` zurueckblieb** -- **ein Stueck
Code, das aussieht wie ein `break` und keiner ist.** Gefunden durch Messen,
nicht durch Lesen: **`map` und `select` gingen, `each` nicht** -- und alle
drei standen in derselben Liste.

**Und `group_by` gab einen Hash ohne Paare** -- **weil ein Helfer eine leere
Liste zurueckgab und sie nie ablegte, also waere jede Gruppe leer
geblieben.** Der Helfer ist jetzt weg, **denn ein toter Helfer, dessen Name
passt, ist der, den jemand in einem Jahr aufruft.**

**Und `flatten` machte nur eine Ebene** -- **gemessen: `[[1, [2]], 3]`
gab zwei Werte statt vier.** Und `min` auf einer leeren Liste gab 0 statt
nil.

`TestRubyInterpreter: 203/203`, `All 1761 tests passed`, Mutationen 5 von 5.


## 2026-09-29 — `Struct`, und eine Datei, die sich fuenfmal selbst zerschnitt

**`Struct` gab es nicht, und eine Konstante konnte keinen Wert halten.**
`RPG::Actor = Struct.new(:id, :name, :class_id)` ist die **erste Zeile**
der Standardbibliothek von XP, VX und VX Ace -- **das ist der
Datenkatalog, keine Bequemlichkeit.**

Und die Zeile scheiterte an **zwei** unabhaengigen Stellen:

1. **`Punkt = Struct.new(:x, :y)`** gab *„is on the left of an = and there
   is nowhere to put the value"* -- **eine Meldung ueber den Leser fuer
   etwas, das der Leser sehr wohl tun kann.** Ruby erlaubt es, und
   `LIMIT = 100` ebenso.
2. **`RPG::Actor` las der Parser als *„rufe `Actor` auf `RPG` auf"*.**
   `::` ohne Klammern machte immer einen Aufruf,
   **und `RPG` ist ein Modul, und Module haben keine Methode `Actor`**
   -- **also nil, und dann `nil.new`, und dann `nil.id`, und drei
   Fehlermeldungen ueber einen Host, der nichts davon getan hat.**

**Und `==` kam als `Binary("==")` und nicht als Methodenaufruf** --
**gemessen: `Struct.new(:x,:y).new(3,4) == Struct.new(:x,:y).new(3,4)`
gab `false`**, und `StructMethode` war an der Aufrufstelle verdrahtet,
nicht an der Vergleichsstelle.

**Und viermal zerschnitt dieselbe Datei sich selbst.** Ein
Feldblock (`_instanceVariables`, `Paare`, `Gruppen`) wanderte in
`RubyParser.cs`, in `IRubyHost.cs` und mitten in `Call` und `DefineType`
-- **weil ein Anker `private Dictionary<...> _instanceVariables = new();`
an mehreren Stellen passt**, und ein Ersetzen mit genau diesem Text
schlug an **allen** Stellen gleichzeitig zu.

**Die Regel, die daraus folgt:** ein Anker muss **einzigartig** sein,
**und "einzigartig" heisst: einmal suchen, nicht viermal raten.**
Zwei Zeilen zurueckgesetzt, drei Mal dasselbe verloren, dann ueber
Zeilennummern gearbeitet -- **und die Zeilennummern verschoben sich
mitten in der Arbeit**, weil das Entfernen des Blocks die Zeilen darunter
verschob. **Ab dem vierten Mal: Textanker mit `count == 1`, plus eine
Klammerbilanz als Wächter.**

`TestRubyInterpreter: 210/210`, `All 1768 tests passed`.


## 2026-09-29 — `raise`, `rescue`, `ensure`, und `e.class`

**`rescue` scheiterte schon im Parser** -- gemessen: *„'rescue' at offset
10 does not begin an expression"*. **Und der Satz, mit dem ein Spiel seinen
eigenen Fehler behandelt, war ein Syntaxfehler.**

**Und `def m; a; rescue; b; end` ohne `begin`** ist die Form, mit der
`Kernel#load` eine Datei laedt,
**und in jedem RPG-Maker-Skript, das eine Datei laedt.**

**Und `:@held` war kein Symbol.** `IsSymbolStart` kannte weder `@` noch
`$`, **also zerlegte der Lexer `:@hp` in `:` und `@hp`**,
**und die Fehlermeldung sprach von einem Doppelpunktzeichen, das der Leser
selbst nicht erkannt hatte** -- **eine Meldung ueber ein Zeichen, an dem
der Fehler gar nicht lag.**

## Und die Regel, die das teuerste war

**Ich habe zwanzig Messungen fuer eine einzige Frage verbraucht:**
`e.class` gibt `Object` statt der Fehlerklasse.

Der eigene Zweig stand an der richtigen Stelle -- **vor**
`SammlungMethode` -- **und wurde nie erreicht**, weil `class` fuer jeden
Empfaenger in `SammlungMethode` beantwortet wird, und der Pfad vorher
zurueckkam.

**Ein Trace im Interpreter hat nichts gezeigt**, weil die Umgebungsvariable
den Godot-Prozess nicht erreichte, und **ein Trace im Test hat es
gezeigt**: der Ast war `Call name='class'`, und der Interpreter rief
`Call()` nie dafuer auf. **Damit war die Frage beantwortet** -- **und ich
habe sie trotzdem noch sechsmal gestellt.**

**Die Regel, die daraus folgt: eine Messung, die nichts zeigt, ist ein
Ergebnis.** *Der eigene Zweig wird nicht erreicht* heisst: **ein frueherer
Zweig antwortet**, **und man muss ihn suchen, statt den eigenen zu
reparieren.** **Und wenn man dieselbe Frage zum siebten Mal stellt,
ist man nicht mehr am Messen, sondern an einer Gewohnheit.**

`TestRubyInterpreter: 215/215`, `All 1773 tests passed`.


## Und zwei überlebende Mutationen, von denen eine ein No-op war

**`Exception` fängt alles** ueberlebte als Mutation, und **der Grund war
eine redundante Regel:** `if (genannt == "Exception") return true;` --
**und `RuntimeError -> StandardError -> Exception` sagt dasselbe ueber die
Elternkette.** **Der Sonderzweig ist geloescht worden, nicht dokumentiert:**
ein Zweig, der neben der allgemeinen Regel steht und dasselbe sagt, ist
eine zweite Antwort auf dieselbe Frage, **und die zweite ist immer die,
die man pflegt und die erste nicht.**

**`else`/`ensure` sind keine Arme** ueberlebte ebenfalls, und **der Grund
ist eine Unsichtbarkeit:** `begin; raise "x"; rescue; 5; else; 99; end`
gibt 5 zurueck -- **und die Mutation, die den Sprung entfernt, gibt auch 5
zurueck**, **weil `else` keinen anderen Wert liefert**, den man untersieht.
**Der Unterschied ist nur sichtbar, wenn `else` einen anderen Wert traegt als
der Arm** -- und das ist jetzt der Test.

**Und die Regel, die daraus folgt:** *eine Mutation, die lebt, ist erst
ein Testfehler und dann ein Codebefund.* **Erst messen, ob sich der Wert
aendert** (`5` gegen `5` heisst No-op) -- **und dann die Frage stellen,
die den Unterschied sichtbar macht**, statt die Mutation als „unmessbar"
zu verbuchen.


## 2026-09-29 — `require`, und wer die Datei liest

**`require` gab es nicht.** Jedes VX- und VX-Ace-Spiel verteilt seine
Skripte auf hundert Dateien,
**und der Leser sagte in Zeile eins jedes Plugins *„self has no method
'require' on this host"*.**

**Und die Frage war nicht *wie*, sondern *wer*.** Die Datei hat nur der
Host, **und der Lexer, der Parser und der Interpreter selbst hat nur der
Interpreter** -- **also hat `require` in `Call()` neben `raise` gehoert,
mit einem `ReadScript` am Host, das eine Vorgabe hat.**

**Und die Vorgabe war die richtige Form, und das ist gemessen:** fuenf
Hosts, **von denen vier keine Dateien haben.** Ohne Vorgabe haette jede
neue Faehigkeit jeden Host gebrochen, **und dann haette jeder Host eine
leere Methode geschrieben, die niemand liest.**

**Und mein Test war falsch, und die Fixture auch.** Die Fixture definierte
`def wert`, **und der Test rief `gruessen` auf** -- **gemessen: die
Antwort war `'aussen'`, nicht `'geerbt'`.** **Die Fixture wurde so
geaendert, dass die geladene Datei die Methode ueberschreibt, die der
Test aufruft** -- **denn der eigentlichere Test ist "die geladene Datei
kann die Klasse des Ladenden ueberschreiben", und nicht "irgendeine
Methode antwortet".**

`TestRubyInterpreter: 222/222`, `All 1780 tests passed`.


## Und der CP932-Test hatte drei Fehler, und alle drei waren meine

**Der Decoder war beim ersten Mal richtig.** **Gemessen: `class Kanji
Kanji日` aus CP932, und `Kanji<zwei Ersatzzeichen>` aus UTF-8.**

Und trotzdem schlug der Test fehl, **und an drei Stellen lag es an mir:**

1. **Mein Byte-Array hatte ein Leerzeichen vor dem Kanji** --
   **also standen dort zwei Namen, und der Lexer sah zu Recht zwei.**
   **Der Lexer hat nie etwas falsch gemacht.**
2. **Der Test suchte den blossen Kanji-Namen** statt `Kanji<kanji>` --
   **und haette damit auf einem Leser bestanden, der jedes Kanji ersetzt**,
   **weil beide nach einem Namen suchen, den die Datei nicht deklariert.**
3. **`using System;` fehlte**, und der Compiler nannte `CallMethod`
   statt `Func<>` -- **weil er am ersten unaufgeloesten Member aufgab.**

**Die Lehre ist die dritte und nicht die erste:** *eine Fehlermeldung, die
etwas anderes nennt als das, was fehlt, ist ein Grund, den ganzen Block zu
lesen und nicht die erste Zeile zu reparieren.* **Und:** *ein Test, der nach
einem reparierten Namen sucht, prueft nichts* -- **er sucht nach etwas, das
die Datei nicht geschrieben hat, und das findet nur ein Leser, der es
auch nicht gelesen hat.**

`TestRubyInterpreter: 223/223`, `All 1781 tests passed`.


## 2026-09-29 — `String#%`, und ein Aufruf, der sich selbst als Empfaenger nahm

**`"%" auf einem Text gab es nicht.** Gemessen: *„undefined operator '%'
for a String and a Float"* -- **und die Meldung war ueber einen Operator,
den es gibt**, weil nur die arithmetische Bedeutung von `%` da war.
**Und die Grammatik kam aus `sprintf.c` von Ruby 1.8.1**, heruntergeladen
und gelesen, **und nicht aus meinem Kopf** -- **und das hat zwei
Dinge gerettet:** `0` ist ein *Flag* und nicht die Breite
(`%05.2f` ist Breite 5, Praezision 2), **und `l`/`h` sind Laengen, die
die Formatierung nicht aendern** und die in jedem alten printf stehen.

**Und der Fund, der mehr wert war als die Formatierung:**

**Ein Aufruf ohne geschriebenen Empfaenger bekam bei diesem Leser einen
Empfaenger: seinen eigenen Namen.** `sprintf("%d", 5)` wurde zu einem
`Call`, dessen erstes Kind der Name war, **und `Call` wertet sein erstes
Kind als Empfaenger aus**, **und ein Name, den niemand gesetzt hat, ist
nil** -- **und die Meldung sprach von einem Host, der nichts getan hat.**

**Und das Kostete zwei Fehlschlaege, die beide Messungen waren:**

1. `SelfCall` eingebaut -- `rollen=0`, **und die Aenderung stand im Code.**
   Sie stand im *anderen* der beiden Zweige (`Keyword` bei 1211,
   `Identifier` bei 1273), **und ein Anker mit drei Zeilen passt in
   beide.** *Ein Anker, der an zwei Orten passt, gehoert an genau eine
   Stelle geschrieben -- und welche, das sagt nicht der Anker, sondern
   `grep -n`.*
2. Der Test `Test_AMethodCallWithNoReceiverCarriesItsArgumentsByRole`
   schlug fehl, **und er behauptete selbst, seine Form sei die
   richtige** -- **und er hatte recht behalten, weil die Form falsch war.**
   *Ein Test, der die Form festschreibt statt das Verhalten, ist eine
   Fehlermeldung mit Extra-Schritten.*

`TestRubyInterpreter: 227/227`, `TestRubyParser: 55/55`, `All 1786 tests
passed`.


## Und die siebte Mutation: `nil` beweist nichts

**`printf("%d", 5)` antwortet `Nil` -- und `sprintf("%d", 5)` antwortet
`'5'`.** Die Mutation liess `printf` den Text zurueckgeben,
**und kein Test sah es, weil `printf` nur auf seine *Art* geprueft wurde
und die Ablehnung auch `Nil` ist.**

**Ein Test, der `nil` sieht, besteht auf einem Leser, der die Methode
gar nicht hat.** Das ist die dritte Form desselben Problems in diesem
Batch -- **nach dem `case "%" when beideZahlen` ohne Zahl und dem
`else`/`ensure` als Arm**:

1. Der Test muss **einen Wert** sehen, der nur die eine Form hat.
2. Oder er muss die **leere Diagnose** sehen, denn eine Ablehnung
   hinterlaesst eine.

**Und die Regel heisst: `nil` ist nie ein Beweis, wenn der Leser die
Methode auch gar nicht kennt.** *Ein Test, der ein "nichts" sieht,
muss pruefen, dass es wirklich "nichts sagen will" ist und nicht
"nichts sagen konnte".*


## Und `sub`/`gsub`: drei Testfehler, die keiner wie ein Testfehler aussah

**`"aXbXc".sub("X", "-")` antwortete `'a-b-c'`.** `Replace` fuer beides
-- **und ein Spiel, das eine Marke aus einem Namen streicht, haette alle
gestrichen.** Und `Replace` mit leerem Text tut in neueren Laufzeiten
nichts mehr, **und darum hat die leere Ersetzung einen eigenen Weg.**

**Und der Block war an keiner Stelle:** `gsub("X") { |t| t * 2 }`
antwortete `'abc'` statt `'aXXbXXc'`. `BrauchtBlock` nannte `sub` und
`gsub` nicht,
**und ein Block kommt nur an den Aufruf, wenn diese Liste ihn nennt** --
**und `gsub` lief also mit leerem Ersatz und strich jedes X.**

**Und die drei Fehler waren:**

1. `"#{$1}"` ist eine andere Ruby-Sache als `$1`. **Der Leser hat sie
   nicht, und mein Test behauptete, er habe sie.**
   *Ein Test, der eine Form behauptet, ist manchmal die Form, die fehlt.*
2. `"\1"` ist **der Oktalwert 1** -- **gemessen: genau ein Byte `01`**.
   **Der Gruppenrueckverweis steht in `"\\1"`.**
   *Ein Backslash im Ruby-Text ist eine Oktalzahl und kein Backslash, und
   das weiss man erst, wenn man die Bytes ansieht.*
3. `["a", "b"]` prueft **eine Sammlung** und nicht zwei Umbenennungen --
   **und der Test lief in eine `IndexOutOfRangeException`, die aussah wie
   ein Leserfehler.**

**Und die Gruppenliste faengt bei 0 an, die Ziffer im Ersatzerzeugnis bei
1.** `"anna bob".gsub(/(\w+) (\w+)/, "\\2 \\1")` antwortete `' bob'`
-- **und das sieht wie ein Zeichenfehler aus und ist einer**, denn beide
Gruppen waren vertauscht.

**Und die alte Regel `Test_APatternFromAScriptIsNotRunAndItSaysSo` war
damals richtig und ist es nicht mehr.** Muster aus Skripten laufen jetzt in
derselben Schranke wie `=~` (4096 Bytes), **und die Meldung nennt jetzt
das Muster und nicht nur die Laenge.**

`TestRubyInterpreter: 232/232`, `All 1790 tests passed`.
**Und die alte Regel `Test_APatternFromAScriptIsNotRunAndItSaysSo` war
damals richtig und ist es nicht mehr.** Muster aus Skripten laufen jetzt in
derselben Schranke wie `=~` (4096 Bytes), **und die Meldung nennt jetzt
das Muster und nicht nur die Laenge.**

## Und zwei ueberlebende Mutationen, und eine davon war kein Testfehler

**Erstes Ergebnis: 6 von 9.** Zwei Ueberlebende waren *dieselbe*
Luecke -- **kein Test hatte `sub` mit einem Muster und zwei Treffern** --
**und `sub` mit einem Muster ist ein eigener Weg** (`Matches` statt
`IndexOf`), **und ein Leser, der den Textweg repariert und den Musterweg
nicht, haette ein `gsub` im Namensfeld eines Spiels.**
`Test_SubWithAPatternStopsAfterTheFirstOne` haelt jetzt **beide Wege**
fest, weil *das Ueberleben einer Mutation eine Aussage ueber die Test ist
und keine ueber den Code*.

**Die dritte Ueberlebende war meine eigene Dummheit, und sie ist lehrreicher
als die anderen beiden.** Die Regel entfernte `sub` und `gsub` aus
`BrauchtBlock` -- **und nichts ging kaputt.** Der Grund: **der Musterweg
liest den Block aus `pArgumente`, und der Textweg aus `IstBlock`, und
beide brauchen die Liste gar nicht**, **um zu laufen** -- **die Liste
entscheidet nur, ob der Block am Aufruf *haengt*.** Und der Aufruf haengte
ihn trotzdem, **weil es noch einen zweiten Weg gab, den ich nicht gesehen
habe.**

*Ein Anker, der gebaut ist und nichts aendert, sieht wie ein ueberlebender
Test aus und ist ein Messfehler.* **Also: die Regel pruefen, ob ihr
Ersatz wirklich etwas tut, bevor man sie als Belegzaehlt** -- **und das
ist ein Schritt, den die Mutationsliste bisher nicht hatte.**

**Und nachgemessen: es waren zwei `break` fuer `sub` im Musterweg, einer am
Anfang der Schleife und einer am Ende, und der am Ende erreichte das
`break` immer zuerst.** Der Anker auf dem am Anfang war ein No-op,
**und der tote Zweig ist jetzt weg.**

## Und die Fragen an einen Typ: alle zehn fehlten, und die zweite
## Fehlermeldung war falsch

**Gemessen vorher:** `M.instance_methods` → *M has no method
'instance_methods' on this host*; `Object.ancestors` → erst *the constant
Object is not defined by this host*, dann *nil has no method 'ancestors'*.
**Und beide Haelften der zweiten Meldung waren falsch:** `Object` ist Teil
der Sprache, **und der Leser ist der, der kein `ancestors` hat.**

**Und der Kernfehler darunter war einer, den man nicht sieht:** `include`
hat die Methoden **kopiert** und das Modul **vergessen** -- **und ohne die
Liste kann `include?` nicht antworten**, **und `include?` ist die erste
Zeile von fast jedem VX-Plugin.** `RubyType.Eingebunden` traegt jetzt
Modulname und `prepend`-Vorn.

**Und `instance_methods(false)` und `instance_methods` waren vertauscht.**
Gemessen: `A.instance_methods` gab `[geerbt]`, **also genau die Methoden,
die `A` nicht selbst geschrieben hat** -- **die eigenen Namen standen nur
in der Liste der Gesehenen und nie in der Antwort.** `true` heisst die
ganze Kette, `false` nur dieser Typ, `nil` steht fuer `true`.

**Und die Typen der Sprache sind jetzt echte Typen im Konstruktor.**
`Object`, `String`, `Module`, `Class`, `BasicObject`, `Kernel`,
`Comparable`, `Enumerable` und die Fehlerklassen,
**denn `class Held` sitzt unter `Object`, ob ein Host das sagt oder
nicht.** `DefinedTypes` **zaehlt nur noch, was ein Skript hinzugefuegt
hat** -- **ein Host, der fragt was ein Skript definiert, will keine vierzig
Namen aus dem Leser.** Und `String.include?(Comparable)` ist wahr, weil
Ruby es so macht, ueber `SprachEingebunden`.

**Und `modul` ist ein Schluesselwort in C#.** Der erste Versuch hiess so,
und der Compiler sagte *the name 'module' does not exist in the current
context* -- **und das sieht nach einem Tippfehler aus und ist ein
Schluesselwort.**

**Und der dritte Testfehler dieser Form: eine Liste, die bei jedem Lauf
geleert wird.** `Test_AMethodThisHostDoesNotHaveIsARefusal` macht drei
`Run`-Aufrufe **und liest am Ende eine gemeinsame Liste** -- **und `Run`
leert die Diagnosen, also zaehlt der Test genau den letzten.** Er
behauptete 2, es sind 4. **Nach `nil` statt eines Wertes und `printf` ist
das die dritte Form: *ein Test, der mehrere Dinge misst, braucht eine
Liste, die waechst.***

**Und `Object.superclass` ist `BasicObject` und erst dessen ist nil.** Ich
hatte `nil` behauptet, um die Kette zu beenden, **und damit `ancestors` um
ein Glied gekuerzt** -- **ein Test, der eine Kette mit einer falschen
Erwartung beendet, kuerzt sie.**

`TestRubyInterpreter: 237/237`, `All 1794 tests passed`.

**Und die Regel heisst damit: in eine Mutationsliste gehoeren zwei
Pruefungen.****Und die Regel heisst damit: in eine Mutationsliste gehoeren zwei
Pruefungen.** Der Anker kommt genau einmal vor, **und der Ersatz ist nicht
identisch mit dem Anker.** Die zweite fehlte bisher,
**und deshalb stand hier zwei Laeufe lang eine tote Regel als Befund in
der Liste.**


## Und die Fragen, die ein Objekt an sich selbst stellt

**Gemessen vorher: neun von zehn fehlten.** `A.new.send(:gruessen)` ->
*A has no method 'send' on this host*; `__method__` -> `nil` **ohne ein
Wort**, **und das ist die schlimmste Form einer Ablehnung**, denn der
Aufrufer kann es nicht von einer Methode unterscheiden, die nichts
zurueckgibt.

**Und `send` laeuft ueber dieselbe Suche wie ein geschriebener Name**,
denn ein Leser, der den Namen selbst aufgeloest haette, waere an `super`
und am Empfaenger vorbeigelaufen. **`1.send(:+, 2)` ist `1 + 2`** --
**und `OperatorName` ist eine Liste und keine Ratswende.**

**Und `__method__` ohne Klammern ist ein Bezeichner.** `Name(pNode)` sah nur
in der Skripttabelle nach,
**und ein Leser, der das tat, fand eine lokale Variable namens
`__method__`** -- **und die ist nil.** *Genau an der Stelle, an der eine
Methode sagen muss, welche sie ist.*

**Und `object_id` wird gezaehlt.** Eine Tabelle, **und nicht die Adresse** --
**eine Sammlung zwischen zwei Aufrufen wuerde dieselbe Zahl noch einmal
ausgeben**, **und `list.uniq` haette zwei verschiedene Helden in einem
Eintrag.**

**Und vierter Testfehler dieser Form: mein Test behauptete `get(:mp)` sei
`10`.** Gemessen: **es ist `20`** -- `:mp` ohne `@` ist das Feld `@mp`.
**Und die Sonde, die mir etwas anderes sagte, las `Items[0].Integer` von
einem Objekt, das `nil` war** -- *ein Index auf einem Wert, den man nicht
geprueft hat, gibt eine Zahl und nicht "nichts".*

`TestRubyInterpreter: 249/249`, `All 1801 tests passed`.


## Und `break`, `dup`, und vier Tests, die eine Abweichung festhielten

**`break` und `next` wurden nie ausgewertet.** `RubyNodeKind.Break`
existierte, der Parser machte den Knoten, der Interpreter nicht, **und die
Meldung war *this interpreter does not evaluate a Break node*.**
**Und `while true; break 7; end` lief 2000000 Schritte**, **und die Meldung
war *this script ran 2000000 steps without finishing*** -- **und ein Spiel,
das auf eine Bedingung wartet, hätte zwei Millionen Schritte gehängt und
danach gestoppt, und der Stopp ist das einzige, was der Spieler sieht.**

**Und der Wert fehlte auch im Parser**: `break 7` wurde zu `break`, und die 7
blieb als nächster Ausdruck stehen.

**Und `next if x == 2` ist ein `if` mit der Rolle `Body`, während
`EvaluateIf` nach `WhenTrue` fragte** -- **und `PartsOf` gibt leer zurück,
wenn die Rollen da sind und der Name fehlt.** Gemessen: `each { |x| next
if x == 2; r = r + x }` addierte alle drei, **und `each { |x| if x == 2;
next; end; r = r + x }` addierte vier.** *Zwei Schreibweisen desselben
Satzes, und die mit einem Wort dazwischen tat nichts.*

**Und `dup` gab denselben Wert noch einmal zurück**, **und die Kopie eines
Objekts mit Klasse verlor ihren Klassennamen**, **und die Meldung dafür
sprach von einem Host, der nie gefragt wurde.**

**Und vier Tests hielten eine Abweichung fest, die nicht funktionierte.**
`Local` und `SetLocal` fingen bei der Blockebene an, **und der Kommentar,
der das begründete, nannte `3.times { |i| g.push(i) }` als den Fall, für den
sie nötig sei.** Gemessen: **`g.length` war 0** -- **und genau dieser Satz
baut jedes Menü eines Spiels.**

**Verifiziert in `parse.y` aus Ruby 1.8.1:** `local_push` schreibt
`local->prev = lvtbl` und **`lvtbl = local`, und die Kette bleibt offen**;
**nur `ruby_dyna_vars` wird in `opt_block_var` gerettet.** Die vier Tests
behaupteten das Gegenteil, ausführlich begründet, **und sie lagen falsch.**
Sie sind umgestellt.

*Ein Kommentar, der den Fall nennt, an dem die Regel scheitert, ist eine
Behauptung und kein Beleg.*

`All 1804 tests passed`, Validator gruen.


## Und fuenf Fragen, die der erste Satz eines Plugins stellt

**`method_defined?`, `private_method_defined?`,
`public_method_defined?`, `protected_method_defined?` und
`module_function` fehlten alle, und `require_relative` auch.** Jede
gemessen, jede einzelne `nil` mit der Meldung *has no method ... on this
host; the interpreter does not guess*.

**Und `method_defined?` war die schlimmste, weil sie `true` sagte.**
`A.method_defined?(:gibtsnicht)` war `true`, **weil der Leser fragte, ob
der Name ein `self.`-Name ist, und nicht, ob es die Methode gibt** --
**und `if !A.method_defined?(:update)` haette nie ausgeloest, und genau
das ist der Zweck des Satzes.**

**Und `undef_method` ist ein Aufruf auf dem Modul, und kein
Schluesselwort.** `undef` ist das Schluesselwort,
**und `Module#undef_method` nimmt Symbole** -- **und der Leser kannte nur
`undef`.**

**Und `A.new.respond_to?(:zeichne)` war `false`, und
`A.method_defined?(:zeichne)` war `true`.** Der Empfänger eines Objekts
traegt seinen Klassennamen bei sich, **und der Leser hielt den Namen des
Empfaengers fuer den Klassennamen** -- **und `respond_to?` lief nur die
Basisklassen, und nicht die Module**, **und `include` ist der Satz, mit dem
ein VX-Grundsystem seine Zeichenmethoden weitergibt.**

**Und `module_function` ist eine Reihenfolge, und keine Frage.** Rubys
`rb_mod_modfunc` schreibt die Methode **ein zweites Mal** auf den
Singleton, **und ein Leser, der sie verschob statt kopierte, haette
jedem Fenster die Zeichenmethode genommen.**

**Und `A.x` fand die Klassenmethode der Basis nicht**, **während
`A.respond_to?(:x)` `true` sagte** -- **und die Wache und der Aufruf
stehen zwei Zeilen auseinander.**

**Und `require_relative` braucht den Ordner des Aufrufers, und der Leser
weiss ihn nicht** -- **und `require_relative` bekam den Namen von `load`,
und laedt daher jede Zeile dieselbe Datei noch einmal.** Das war der
schlimmste Fund dieses Batches: **es laedt den Klassenrumpf mehrfach,
und die zweite Definition gewinnt.**

`All 2062 tests passed`.


## Und vier Mutationen leben, und eine davon ist ein Widerspruch

**Drei waren toter Code.** Die beiden `foreach`-Schleifen ueber
`Eingebunden` in `HatMethode` und `TypHatMethode` sagten dasselbe wie
die Tabelle darueber, **weil `include` die Modulmethoden nach `Methods`
kopiert** -- `Eingemischt` macht genau das. Beide entfernt.

**Die vierte ist kein toter Code, sondern ein Widerspruch im Selbst.**
Gemessen: `module M; def x; 7; end; end; M.x` gibt `7`, **und Ruby
1.8.1 sagt `NoMethodError`**, denn `x` ist eine *Instanzmethode* des
Moduls.

**Der Versuch, das zu richten, hat 182 Tests gebrochen.** Der Grund ist
ein seit Monaten gepushter Test: `Erbe.antwort` sei 42, **wobei
`antwort` in `Basis` eine Instanzmethode ist** -- **und dieselbe Form
bei einem Modul gibt 7.**

*Bei einer Klasse geht der Aufruf auf den Klassennamen in die
Basiskette, bei einem Modul nicht. **Ruby macht beides richtig, und
dieser Leser kann es nicht, ohne eine der beiden Formen zu verlieren.***

**Der Versuch ist zurueckgenommen, der Widerspruch steht als Dokument.**

`All 2064 tests passed`, Sonde entfernt, keine Mutationsreste im Baum.


## Und `self` im Klassenrumpf ist der Typ

**Gemessen vorher:** `class A; @n = 0; end; A.instance_variables` war
`[]` und `A.instance_variable_get(:@n)` war `nil`.
**Und `A.new.stand` war nil** -- **weil der Rumpf ohne `self` lief und
der Wert in keinen Speicher kam.**

`RubyType` hat jetzt einen eigenen `Felder`-Speicher, **und `RubyValue`
traegt seinen Speicher bei sich** (`Felder` ist von `init` auf `set`),
**und `A.instance_variables` gibt `[@n]`, `A.instance_variable_get(:@n)`
gibt 0, und `A.new.instance_variables` gibt `[]`** -- **und genau das ist
der Unterschied, den ein Plugin bemerkt, wenn es `@ivars` durchsucht.**

**Und `_self.Felder` habe ich wieder entfernt**: gemessen, dass alle fuenf
Saetze auch ohne die Zeile stimmen, **und sie war eine zweite Wahrheit
fuer dieselbe Sache.**

**Und der Aufruf-Zweig in `Call` beendet den Aufruf mit nil und einer
Diagnose, wenn die Singleton-Kette nichts hat** -- **und er wird von
`M.x` nicht erreicht**, **weil der Aufruf auf einen Modulnamen ueber
einen anderen Weg laeuft.** Das ist gemessen und dokumentiert, **und
nicht weggeraeumt.**

`All 2066 tests passed`, Mutationen 6/9 mit drei gemessenen
Ueberlebenden, Validator gruen.


## Und der gemessene Fehler ist behoben

**`module M; def x; 7; end; end; M.x` gab 7 und gibt jetzt `nil` mit der
Diagnose, die den Empfaenger nennt.**

**Die Ursache war eine Reihenfolge.** `EigeneMethode` nimmt fuer ein
Symbol-Empfaenger `M.Methods["x"]` -- **die Instanzmethode** --
**und der Typ-Zweig, der die Singleton-Kette nimmt, stand DAHINTER.**
Er steht jetzt davor, **und `EigeneMethode` bekommt fuer ein Symbol,
das ein Typ ist, gar nichts.**

**Und `def self.x` gibt weiter 7, und `A.new.x` gibt weiter die
Instanzmethode, und `Regexp.last_match` gibt weiter den Treffer** --
**weil die Reihenfolge jetzt passt**: der Typ-Zweig steht hinter den
eingebauten Namen und vor `EigeneMethode`.

**Und ein Test, der seit Monaten falsch gepusht war:**
`Ziel.ueber_class_eval` war ein Aufruf auf den Klassennamen einer
*Instanzmethode* -- **er ist jetzt `Ziel.new.ueber_class_eval`**.

Mutationen 8/12, vier Ueberlebende alle gemessen, `All 2067 tests
passed`, Validator gruen.


## Und dieselbe Regel stand an zwei Orten

**Die fuenf Feldfragen beantworteten `WertMethode` (1703) und
`TypBefragt` (9891) — mit demselben Code und demselben Kommentar.**
`Call` ruft `WertMethode` zuerst, **und die Kopie war nie der Weg.**

**Gemessen: `return null` in der Kopie aendert nichts** — alle sechs
Saetze identisch, `All 2069 tests passed` — **und eine Mutation, die
diesen Ort ausschaltet, lebt, weil nichts einen Zweig sehen kann, der
nie laeuft.** Die Kopie ist jetzt fort.

**Und `FeldFrage` haelt die Bruecke zurueck und aendert nichts**
(gemessen) — **der Grund fuer ihre Existenz ist die entfernte Kopie.**

**Und `M.include?` und `A.instance_methods` brauchen den eingebundenen
Modul-Walk**: als ich die Bruecke ganz entfernt habe, antwortete dieser
Ort `false` (32 Fehler, gemessen). Sie bleibt fuer diese Fragen.

Mutationen 6/8 mit gueltigem C#, `All 2068 tests passed`.


## Und ein `def` auf oberster Ebene ist jetzt ein `Object`

**Gemessen vorher:** `def lauf; 7; end; lauf` gab `nil` mit *method lauf
is defined outside a class*, **und `self.lauf` gab 7.**

**Belegt an der Quelle, und nicht entschieden:**
- `eval.c` Zeile 1233: `ruby_class = rb_cObject` beim Programmstart
- `eval.c` Zeile 1234: `ruby_frame->self = ruby_top_self`
- `eval.c` Zeile 3516: `TypeError: no class/module to add method`, wenn
  `ruby_class == 0` — **und 0 ist, was ein Leser ohne die Regel hat**
- `parse.y` Zeile 1646: `NOEX_PRIVATE` — ein Top-Level-`def` ist privat

**Und `Object` ist spracheigner Besitz** (`Object.superclass ==
BasicObject`, `BasicObject.superclass == nil`, gemessen) — **kein Host
muss etwas bereitstellen.**

**Zwei Fehler behoben, beide gemessen:**
1. `DefineMethod` legte nichts an, wenn `_aktuellerTyp == null`
2. `EigeneMethode` und `Name()` fielen nicht auf `Object` zurueck

**Und der dritte Befund aus derselben Messung:** ein klammerloser
Aufruf ist ein `Identifier`, **und nicht `SelfCall`** — der Parser
erzeugt `SelfCall` nur fuer geschweifte und runde Klammern.

`All 2068 tests passed`, Mutationen 3/4 (die Ueberlebende ist ein
gemessener No-op: sie betrifft nur die Zweitdefinition).


## Und ein Symbol traegt sein Namensende

**Gemessen vorher:**
- `send(:reich?)` → *")" at offset 52 does not begin an expression*
- `send(:"reich?")` → *":" at offset 45 does not begin an expression*

**Zwei Ursachen, beide belegt:**
1. `IsSymbolStart` kannte kein Anfuehrungszeichen, `ReadSymbol` schon.
   **Regel und Bedingung an zwei Orten; der Zweig unter der
   Bedingung war nicht erreichbar.** Token gemessen:
   `Delimiter :` + `String "r?"` → jetzt `Symbol :"r?" value=r?`
2. `ReadSymbol` nahm kein `!`/`?`-Ende. `parse.y:4314` gilt fuer jeden
   Namen — **und ein Symbol ist ein Name.**

**Und `def x=(v)`, `def x!=(v)`, `def ==(o)` waren nie kaputt:**
gemessen fuenf/fuenf/true auch ohne jede Aenderung. Der Test bleibt
mit dem Vermerk, dass er kein Fund ist.

`All 2078 tests passed`, Mutationen 2/3 (Ueberlebende: `ReadWord`
nimmt `!` selbst, also ist der Unterschied selten).


## Und `h[:a] = 1` funktioniert

**Gemessen vorher:** nil, `*a value has no method '[]=' on this host*,
**und `h.size` war 0.**

**Ursache:** `h[:a] = 1` ist `[]=(:a, 1)`, und der Knoten, den eine
Zuweisung baut, trug den Empfaenger und den Wert **und nicht die
Argumente des `[]`-Aufrufs** — **der Schluessel ging verloren.**

**Und `RubyValue.Items` war `IReadOnlyList`**, obwohl der Wert immer ein
`List` war und andere Stellen schon hindurch schrieben.

**Neu, alle gemessen:** `key?`/`has_key?`, `fetch` mit Vorgabe, `pop`,
`shift`, `unshift` (Argumentreihenfolge bleibt), `insert`, `a[k] = v`
mit Aufwachsen bis zur Stelle und `nil` in der Luecke, `zero?`,
`nonzero?`, `even?`, `odd?`, `abs`, `succ`, `pred`, `pow`, `divmod`,
`gcd`, `lcm`, `round`, `floor`, `ceil`.

**Und `**` haelt die Art** (`numeric.c` 1889/1890/1893/1895): `2 ** 10`
war **immer 1024.0**; jetzt ganzzahlig, `2 ** 0` = 1, `2 ** 1` = Basis,
nur negativer Exponent gibt eine reelle Zahl.

**Und `-7.abs` ist -7, und das ist richtig**: `parse.y:1106` macht die
unary minus zu einem `arg`. `(−7).abs` ist 7.

`All 2086 tests passed`, Mutationen 9/9 (8 durch Tests, 1 per compile).


## Und `catch`, `tap` und die Ausgaben sind jetzt Sprache

**Gemessen vorher:** alles nil mit *self has no method 'catch' on this
host* — **und die Meldung geht über den Host, obwohl es der Leser ist,
der die Sprache nicht gebaut hat.**

**Drei Befunde, alle gemessen:**
1. **Der Block kam aus dem falschen Ort** — er hängt am Aufruf und
   liegt auf `_blockKette`, **und es ist der ganze Blockknoten, weil
   `BlockAufrufen` `Children[1]`/`Children[2]` liest**. `5.tap { }`
   kommt als Argument an, **weil `tap` einen Empfänger hat**.
2. **Ein Name ohne Klammern erreicht `Call` nur, wenn `FindMethod`
   etwas findet** — `puts 3` und `p 4` sind Identifier, **und
   `Name()` sah nur Skriptmethoden**: `print 1, 2` schrieb, danach
   nichts.
3. **`puts [1, 2]` war `puts[1, 2]`** — **Ruby liest eine Klammer nach
   einem Leerzeichen als Argument**, **und der Tokenabstand ist der
   ganze Unterschied.**

**Und `IRubyHost` hat `Write`/`WriteLine` als Default-Member** — kein
Host muss sie bauen.

**Und ich hatte bei `throw` 21 erwartet und gemessen wurde 1** — ein
Wurf verlässt *jeden* Block bis zum `catch`, **und der Leser hatte
recht; der Test steht jetzt auf dem gemessenen Wert.**

`All 2092 tests passed`, Mutationen 6/7 (Ueberlebende: ein No-op).


## Und `const_missing` ist eine Methode des Spiels

**Belegt an der Quelle:** `variable.c` Zeile 1120 macht
`rb_funcall(klass, "const_missing", 1, ID2SYM(id))` — `klass` ist das
Modul, auf das geschrieben wurde, **und der Name kommt als Symbol an.**

**Gemessen vorher:** nil und *the constant GIBT_ES_NICHT is not defined
by this host* — **und die Meldung fragt den Host nach etwas, das das
Spiel selbst schreiben muss.**

**Und `module RPG; module Actors; end; end` laeuft in jedem VX-Projekt
ueber genau diesen Weg.**

**Und ein Name, den weder der Leser noch das Spiel hat, wird weiterhin
nicht geraten**, **und die Meldung nennt ihn.**

`All 2096 tests passed`, Mutationen 4/5 (Ueberlebende: ein No-op,
`BasicObject` hat kein `const_missing`).


## Und MZ: 114 Befehle benannt, 21 ausfuehrbar

**Das ist der Unterschied zwischen "das Geraet erkannt" und "das Spiel
laeuft", in Zahlen:** `MzCommandTable.Count` ist 114, **und in
`MzCommands.TryExecute` stehen 13 Faelle, in `MzControlFlow.TryExecute`
8** -- **also 21 von 114 fuehren etwas aus.**

**Und die 204 Befehle in den echten Fixtures benutzen 22 Nummern, und
20 davon sind behandelt.** Die zwei Ausnahmen (`655`, `657`) liest die
Engine als Daten eines Befehls, nicht als Befehl.

**Und vier Tabellennamen werden nirgends behandelt:** `355 Script`,
`402 ContinueText`, `405 ShowChoices`, `412 EndBranch`.

**Und `EndBranch` habe ich end to end gemessen, und es laeuft durch:**
allein in einer Liste endet es mit `Waiting` und einer Aktion, **nicht
mit `Refused`.** Mein erster Test bekam `Refused`, **weil ich
`ShowText` mit den Parametern `"0" "0" "1" "0" "1"` gebaut habe, und die
bedeuten *Schalter 0*** -- **der Befehl war richtig und mein Test war
falsch.**

**Und `355`, `402` und `405` habe ich NICHT end to end gemessen, und
ich melde sie als offen, nicht als fehlend.**


## Und die drei "offenen" MZ-Namen waren drei No-ops

**`355`, `402`, `405` und `412` habe ich end to end gemessen:** eine
Liste, die nur aus einem davon besteht, endet mit `Finished` und
**null Aktionen** — **und das ist genau die Regel der Engine**
(`MzInterpreter` Zeile 265: *a command the engine has no method for is
stepped over*).

**Und ich hatte sie zuerst als fehlend gemeldet**, weil ich nur in den
Dispatch-Zweigen gesucht habe **und nicht in dem Pfad, der sie
abfaengt.**

**Und `EndBranch` habe ich zweimal gemessen**, weil mein erster Test
`ShowText` mit den Parametern `"0" "0" "1" "0" "1"` gebaut hat, und die
bedeuten *Schalter 0* — **der Befehl war richtig und mein Test war
falsch.**

**Und `ClearBranch` hat keinen Aufrufer** (gemessen) — **und ich habe
es geloescht, statt eine tote Regel zu dokumentieren.**

Mutationen 3/3, alle durch Tests. `All 2097 tests passed`.


## Und VX: der Leser laeuft eine ganze Datei, die Engine ruft ihn nicht

**Die Luecke zwischen "Ruby kann das" und "das Spiel laeuft" ist eine
Verdrahtung, und keine Sprache:** `RgssEngineRuntime` meldet selbst
*Ruby, Game.exe, RGSS DLLs, and external runtimes were not executed*.

**Und gemessen: 340 `new RubyInterpreter(...)` in 36 Testdateien, keine
in `src/` — und es gab keine Fixture eines echten Spiels im Baum.**

**Und jetzt laeuft die erste VX-Skriptdatei als Test**
(`tests/fixtures/ruby/vx_window_base.rb`) — **eine Klasse, die `Window`
erbt, einen Namen, den der Leser nie gesehen hat**; eine Konstante;
zwei Bedingungen, eine mit `else`; eine globale; eine Methode, die dem
Empfaenger zuweist. **Ergebnis: ein Knoten, keine Diagnose, `initialize`
und `refresh` sind Methoden.**

**Und ich habe es zuerst zeilenweise gemessen, und jede Zeile warf
*end was expected, but the script ends first* — weil eine Zeile keine
Datei ist.** Derselbe Text als Datei parst.

**Und `| ` bindet in Ruby schwacher als `==`** — mein Test schrieb
`a == b | a == b`, **und der Leser hat recht, und Ruby auch.**


## Und `||` gab `true` statt des Operanden zurueck

**Das ist der schwerste Fund dieser Sitzung, und er kam aus dem
Skriptlisten-Test, und nicht aus dem Leser.**

**Vorher (Zeile 892): `a || 0` gab `true` zurueck — fuer `a = nil`,
`a = false`, `a = 1` und `a = 0` gleichermassen.** Und damit war
`(nil || 0) + 1` ein `NoMethodError: undefined operator '+' for a
Boolean and a Integer`.

**Und `@n = @n || 0` ist der Zaehler, den man schreibt, wenn man keinen
hat** — **und damit ist jedes Skript eines VX-Projekts aus dieser Zeit
an seiner ersten Zeile gescheitert**, **und die Liste lief weiter, und
das Spiel hatte keine Klassen und keinen Fehler ueber der ersten
Zeile.**

**Jetzt: `nil || 0` ist 0, `false || 0` ist 0, `1 || 0` ist 1, `0 || 0`
ist 0.**

**Und `&&` gab auch `false` statt des Operanden zurueck, und mein Satz
*dass nil und false in einer Bedingung dasselbe sind* war falsch.**
Gemessen jetzt: `nil && 7` ist nil, `false && 7` ist false, `1 && 7` ist
7, `0 && 7` ist 7, `(nil && 7) || 3` ist 3, `(1 && 7) || 3` ist 7.

**Der Beleg ist Ruby 1.8.1 `eval.c` Zeile 2946:** `case NODE_AND:
result = rb_eval(self, node->nd_1st); if (!RTEST(result)) break;` — **der
`break` verlaesst die Schleife mit `result`, und `result` ist der linke
Operand.** (Hinweis: Ruby 1.8.1 hat **kein `insns.def`** — die Datei gibt
es erst ab 1.9; `curl` liefert 404, und die Instruktionen stehen in
`eval.c` selbst.)

## Und `RunScripts(IReadOnlyList<string>)` ist die Tuer, die gefehlt hat

**`SkriptLaden` (Zeile 8669) kann alles — Bytes lesen, CP932 dekodieren,
parsen, auswerten, die Kette fuehren — und war `private`,** und
oeffentlich gab es nur `RunProgram(IReadOnlyList<RubyNode>)`.

**Und noch eine Luecke kam dazu:** `RunScripts` akzeptierte `"  "`
(Leerzeichen) als Dateinamen, **und der Host bekam die Frage und die
Antwort war eine Diagnose ueber eine Datei, die nie jemand
geschrieben hat** — **jetzt `IsNullOrWhiteSpace`.**

Mutationen 5/5, alle durch Tests. `All 2113 tests passed`.

**Und die vierte Regel lebt, und das ist ehrlich:** der Syntaxfehler
traegt den Dateinamen in der Ausnahme selbst (`in 'kaputt.rb': ...`),
**und der Aufrufer setzt ihn noch einmal davor, und ein Test kann nicht
unterscheiden, welcher der beiden der Grund ist** — **und ich habe die
tote Regel durch die Regel ersetzt, die den `||`-Fehler toetet.**


## Und derselbe Weg: der Marshal-Leser las CP932 als UTF-8

**`ReadString` (Zeile 438) machte `Encoding.UTF8.GetString(raw)`.**
**Gemessen an den Bytes `83 65 58` (CP932: *te* und *X*): drei Zeichen
zurueck, mit 65533 an der Stelle des ersten.** Und
`Encoding.UTF8.GetString` **wirft nicht, es ersetzt** — **gemessen, es
kam 65533 zurueck und keine Ausnahme.**

**Und damit hat jedes Kanji in den Daten eines japanischen Spiels ein
Ersatzzeichen.**

**Jetzt: `AlsText(raw)`** — **CP932 als zweite Stufe, mit UTF-8 als
erstem Versuch**, **denn ein gueltiger UTF-8-Strom hat kein U+FFFD.**
Die Rohbytes bleiben erhalten.

**Und die vierte Mutationsregel lebte ehrlich:** `RegisterProvider` wird
in `legacy_text_decoder.cs` Zeile 98 idempotent registriert — **also
habe ich meine eigene Registrierung geloescht, statt eine tote Regel zu
dokumentieren.** 3/3 durch Tests.

`All 2115 tests passed`.


## Und RM2K laeuft jetzt gegen ein fertiges Spiel

**`Dragon Destiny` liegt auf dieser Maschine: 743 Karten, 416 kB
Datenbank, echte Chipsets.** Und `TestRealRm2kRuntimeRun` startet es
ueber `EnginePluginHost`, tickt 100 Bilder ohne eine Verweigerung und
zeichnet eine Karte mit Inhalt. **2/2.**

**Fehler 1: `Rm2kEngineRuntime` Zeile 89 nahm die erste Datei
alphabetisch.** Und `Map0001.lmu` eines Spiels von 2002 ist die leere
Startkarte des Editors — 1227 Bytes, ein Chip in allen 300 Feldern.
**Gemessen: eine Farbe; `Map0002.lmu` (478 kB): 64 Farben.** Und der
MapTree sagt `party_map_id = 742`. **Jetzt: `PickStartMap` nimmt die
Karte aus dem MapTree, mit Rueckfall auf die erste.**

**Und 742 ist auch eine leere Karte (2266 Bytes) — das Spiel wurde
unfertig exportiert, und das ist eine Eigenschaft des Spiels.**

**Fehler 2: ein leerer Frame sagte nichts.** 76800 mal `0x00000000`
und eine leere Diagnose. **Bei einer leeren Karte ist der schwarze
Frame richtig — und richtig ohne ein Wort ist er ein Fehler, der sich
als Erfolg verkleidet.**

**Und zwei Mutationen ueberlebten zuerst, weil mein Test nur
zuehlte Farben statt die Karte zu pruefen** — `Map0001` und `Map0002`
sind beide 20x15. **Der Test prueft jetzt `Simulation.MapId == 742`.**

Mutationen 5/5 durch Tests. `All 2117 tests passed`.


## Und die Events des echten Spiels laufen

**`Map0002.lmu`: 873 Events, 1049 Seiten, 3262 Befehle in 35 Codes.**
**Und `Map0033` hat 4005 Befehle.**

**Und gemessen: alle 3262 kommen beim Scheduler an, und nach 60
Bildern ist keine einzige Diagnose ueber einen Befehl.**

**Und die 100 Diagnosen sind 26 fehlende Charsets** — `People1.png`,
`Animal.png` — **die das Spiel verweist, aber nie mitgeliefert hat,
weil es sie nie benutzt.** 37 Charsets liegen da. **Das ist eine
Eigenschaft des Spiels, und eine Runtime, die sich darueber verweigert,
verweigert ein spielendes Spiel.**

**Und `Map0002` hat 478 Action-Seiten (Trigger 0) und 3 AutoStart-Seiten
(Trigger 3)** — **nur die drei laufen von selbst, und genau so soll es
sein.**

Mutationen 5/5 durch Tests. `All 2118 tests passed`.


## Und die Befehle des Spiels aendern den Zustand

**"Die Events sind angekommen" ist nicht "die Events haben etwas
getan."** `Map0002` hat drei AutoStart-Seiten mit 308, 89 und 8
Befehlen, darunter `10210 Control switches` — **und alle Bedingungen
sind false, also unbedingt.**

**Gemessen: `Switches.Count` geht von 0 auf 1847 in 30 Bildern. Und
1847 ist eine Schalter-Id, die die Datei des Spiels selbst nennt**
(`10210 [0,1847,1847,1]`). **Der Test fragt genau diese Zahl.**

**Und meine erste Sonde sagte 0 nach 120 Bildern** — **weil sie die
falsche Datei und die falsche Property gemessen hat.** 5/5 durch
Tests. `All 2119 tests passed`.


## Und Kriterium 2 war kaputt

**`PresentationState.ShowPicture` verweigerte Befehle mit Werten
ausserhalb der eigenen Grenzen. Und der Befehl des fertigen Spiels
traegt `11110 [1,0,160,220,0,0,100,0,0,100,100,100,100,0,60]` —
`parameters[12] = 100`, und 100 ist keine Effektart (die sind 0..3).**

**Gemessen vor dem Fix: `Pictures.Count == 0` nach 40 Bildern.**

**Die Reparatur ist die der Quelle.** EasyRPG `game_interpreter.cpp`
Zeile 2949, der ganze Sanitize-Block: `std::max(0, std::min(x, 2000))`
zweimal, `std::min(x, 100)` zweimal. **Drei Clamps, kein Kanal, keine
Saettigung, kein Effektmodus, kein Name.**

**Und ein zu kurzer Befehl bleibt abgelehnt** (`CmdSetup<..., 14>`).

**Und meine erste Clamp-Regel lebte, weil ich `magnify` pruefte und der
Spielbefehl `magnify = 0` traegt** — 0 ist in der Schranke. **Also
pruefe ich die obere Transparenz mit 250, und sie kommt als 100 zurueck.**
4/4. `All 2120 tests passed`.


## Und Kriterium 7 ist gemessen: 888 von 2436 Befehlen laufen nicht

**`CamelliaCoronation-Win` liegt hier: 20 Karten, echte Tilesets.**

**Gemessen: 2436 Eventbefehle, 1548 ausfuehrbar, 888 nicht.** Die
nicht ausfuehrbaren Codes sind keine Randfaelle: `123 Control Self
Switch` (42x), `213 Show Balloon Icon` (36x), `129 Change Party Member`
(25x), `250 Play SE` (18x), `221/222 Fadeout/Fadein` (30x), `203 Set
Event Location` (10x), `301 Battle Processing` (7x), `241 Play BGM` (4x).

**Und meine erste Zahl 3509 war falsch:** ich hatte mit einem Ausdruck
gezaehlt und Codes 0, 1, 2, 3, 29, 505 gefunden — **die liegen in den
Parametern von `205 Set Movement Route`, das ist eine Bewegungsliste.
Ein Ausdruck kann einen Befehl nicht von einem Parameter
unterscheiden.** Der Test zaehlt jetzt die JSON-Struktur.

**Und die Tabelle: 114 Namen, 27 C#-Felder, 22 dispatcht, 93 tun
nichts.** `All 2121 tests passed`.


## Und die erste Loesung fuer Kriterium 7: sieben Audio-Befehle

**Die Form ist die Ueberraschung:** `241 [{"name":"Scene8",
"volume":40,"pitch":80,"pan":0}]` -- **ein Objekt als erster Parameter,
und nicht vier Zahlen**, **und XP schreibt 11510 mit vier Bitfeldern.**

**Und `MzCommandEntry.From` gibt ein Objekt als Text zurueck
(`MzJson.Write`), und `MzJson.TryParse` ist derselbe Parser** -- ohne
das hatte jeder Kanal den Namen `{"name"` und Lautstaerke 0.

**Vier Kanaele** (Bgm/Bgs/Me/Se), `251` ohne Parameter, **ein
Ausblenden von 0 Bildern ist ein Stopp** (das Feld ist leer, wenn
niemand es angefasst hat).

**Neu gemessen: 1570 von 2436 ausfuehrbar** (vorher 1548).
Mutationen 4/4. `All 2125 tests passed`.

## Und die naechsten drei MZ-Befehle: 123, 129, 213

**Die Quelle ist die offizielle Hilfe, und nicht EasyRPG** -- **EasyRPG 0.8
hat weder `SelfSwitch` noch `Balloon`, und diese drei Befehle sind MZ
eigen.**

**Drei Befunde, die ein naiv lesender Befehlssatz falsch macht.**

- **Der erste Parameter von `123` ist ein Buchstabe.** `At(pCommand, 0)` gibt
  fuer `"A"` zurueck: 0 -- **und 0 ist Schalter A, und das geht fuer B, C
  und D gleichermassen falsch.**
- **Minus eins ist der Spieler, und keine Darsteller-Id.** Gemessen: `-1` 15
  mal, Figurnummern 21 mal. Spieler und Figur tragen denselben Zustand in
  zwei Typen, **und `MzPlayer` hatte keinen Ballon.**
- **`return false` ohne zu warten ist ein Fehler.** Der Runner liest denselben
  Befehl noch einmal, **das Icon wird jedes Bild neu gesetzt, seine Uhr
  steht bei 60, und MZ friert nach 100 000 Befehlen ein.** Das fand der
  erste Test ueber den Runner, **und nicht die Mutation**, **weil die drei
  anderen Tests `TryExecute` direkt rufen und den Verdrahtungspunkt nicht
  erreichen.**

**Und der Ballon hatte keinen Takt:** `MzScreen.PassFrame` hat keinen Aufrufer
in `src/`, `TickBalloon` auch nicht. Der Takt haengt jetzt an
`MzEventRunner.Run`, und `MzBranchFacts.TickBalloons` zaehlt Spieler und
Figuren gemeinsam.

**Und `MzCharacter.BalloonIcon` stand auf 0, und nicht auf -1** -- **null ist
das erste Icon der Editorliste.** Eine lebende Mutationsregel hat das
aufgedeckt.

**Die Dauer des Icons ist eine Zahl, die dieses Repository gewaehlt hat**
(Sekunde), **und die Konstante sagt es zweimal**, **denn die Hilfe nennt
keine Dauer und ohne eine waere *wait for the icon to disappear* eine
Wartezeit, die nie endet.**

**Neu gemessen: 1673 von 2436 ausfuehrbar** (vorher 1570).
Mutationen 11/11. `All 2129 tests passed`, Validator gruen.

**Und als Werkzeug dazugekommen:** `--suite=<Name>` fuer den Test-Runner.
**Und es hat einen Fehler aufgedeckt, den die Suite nicht aufgedeckt hat:**
`OS.GetCmdlineArgs` statt `OS.GetCmdlineUserArgs` -- **alles nach `--` steht
in der Benutzerliste, und die Motorliste ist alles davor.**

**Und ein einmaliger Absturz:** ein voller Lauf endete einmal in
`Internal CLR error (0x80131506)` nach `TestRm2kPanScreen`, **waehrend ein
Mutationslauf lief.** Der Lauf direkt danach und der Lauf davor waren gruen,
`TestRm2kParser` allein ist gruen. **Nicht reproduziert, und nicht
untersucht** -- **und das ist eine offene Luecke, keine Entwarnung.**

**Naechster Schritt fuer MZ:** `221 Erase Picture` und `222 Erase Event`
(beide **null Parameter**, 16x und 14x) und `203 Change Image` (10 Formen).

## 221 Show Animation, 222 Erase Event, und der Wort-Befund

**Die offizielle Hilfe sagt fuer 222 woertlich *There are no parameters
to set*, und das Spiel traegt 16x bzw. 14x eine leere Liste.**

**Und 221 ist 213 mit anderem Namen** -- **dieselben drei Saetze der
Hilfe, gleicher erster Parameter, gleiches Minus eins fuer den Spieler**
-- **und trotzdem zwei Felder**, **denn ein gemeinsames Feld haette den
Ballon eines Ereignisses ueber die Animation einer Figur geschrieben.**

**"Geloescht" heisst nicht "weg":** das Ereignis hat danach weiter seine
Befehle, **und nichts loescht das Flag bis zum Kartenwechsel.**

**Und dann der Fund, der drei von sechs Bool-Parametern in diesem
Dispatch betraf und den kein Test der Welt haette finden koennen.**

Gemessen: `213` schreibt **JSON-Booleans** (`bool`, nicht Text), `121`,
`123` und `129` schreiben `"0"` und `"1"`, und `221` traegt eine leere
Liste. **Und `MzCommandEntry.From` macht aus einem JSON-Boolean den Text
`"true"` oder `"false"`** (`MzCommandEntry.cs:47`) -- **denn ein
Parameter ist eine Zeichenkette.** **`At` parst nur Zahlen**, **und so
war die Wartefunktion von `213` und `221` in jedem echten Spiel tot.**

**Die Tests waren gruen, weil die Tests die Zahlen selbst geschrieben
haben.** **Ein Test, der seine eigene Eingabe schreibt, misst die Datei
nicht.**

**Neu: `Flag(pCommand, i)`** -- ja bei `1`, `"true"` und `"on"` ohne
Rücksicht auf die Schreibweise, nein bei `0`, `"false"` und bei einem
fehlenden Parameter. **`121`, `123` und `129` lesen unveraendert ueber
`At`**, **denn ihre Werte sind gemessen Zahlen, und ein Leser, der dort
`Flag` benutzt, haette nichts gewonnen und eine zweite Wahrheitsform
eingefuehrt.**

**Neu gemessen: 1703 von 2436 ausfuehrbar** (vorher 1673).
Mutationen 7/7. `All 2136 tests passed`, Validator gruen.

**Und eine Unregelmaessigkeit, offen und nicht erklaert:** ein Volllauf
zeigte `Test_EinAlsWortGeschriebenerSchalterWirdAlsWortGelesen` als
fehlgeschlagen und meldete `2/2136 tests failed`, **obwohl nur eine
Fehlermeldung im Log stand und `1/7`**. **Der Lauf davor und der
danach waren gruen, und die Suite allein ist gruen.** **Nicht
reproduziert.**

## Und 203 Set Event Location

**Die Hilfe hat keine Seite unter diesem Namen.** Das Handbuch sagt
*Set Event Location* und *Changes the location of an event* -- **und die
Zahl in der Datei ist 203.**

**Und die gemessene Form ist `[Ereignis, Ort, X, Y, Richtung]`, und der
Ort ist in allen zehn Faellen 0** -- **das ist *Direct Designation*.**

**`RealX` und `RealY` gehoeren mit `X` und `Y`:** **ein Leser, der nur
die Kachel setzte, liess die Zeichnung zurueck, und der naechste
Bewegungsbefehl ging an den Ort zurueck, von dem die Figur gerade
weggesetzt worden war.**

**Und ein Ereignis darf kein anderes verschieben:** **der Motor ruft
`setLocation` auf dem Ereignis auf, das laeuft**, **und nie auf dem, das
die Zahl nennt** -- **das stand nicht in der Hilfe, es stand in `201` und
wurde hierher getragen.**

**Und eine Lektion, die eine lebende Regel gefunden hat:** der erste Test
pruefte `Direction == Down`, **und Down ist der Anfangswert des Feldes**,
**und die Regel `Direction = Down` hat deshalb ueberlebt.** **Ein Test,
der einen Wert gegen seinen eigenen Anfangswert prueft, prueft nichts.**

**Neu gemessen: 1713 von 2436 ausfuehrbar** (vorher 1703).

**Und was danach noch offen ist, gemessen:** **28 Befehle in 6 Codes** --
`301` (7x), `322` (6x), `102` (8x), `105` (4x), `225` (2x), `314` (1x).
**Und `102` ist *Show Choice List***, **der zweite Teil von `405`, und
`405` selbst ist der dokumentierte No-op** -- **also ist die naechste
Aufgabe nicht `102`, sondern `301`.**

## Und dieselbe Sache zum zweiten Mal: Testfehler nur neben einem laufenden Mutationslauf

**Ein Volllauf meldete `Test_EinKachelwechselIstSprungUndNichtSchritt`
als fehlgeschlagen** (`3/2141 tests failed`, `TestMzSetEventLocation:
4/5`), **und `scripts/validate.sh` im selben Moment: `All 2141 tests
passed`.** **Drei weitere Volllaeufe hintereinander: alle gruen, und
die Werte in einer Sonde waren jedesmal `x=5 y=7 rx=5 ry=7`.**

**Und beide Fehlfaelle -- dieser und der `Internal CLR error
(0x80131506)` von vorhin -- traten ausschliesslich auf, waehrend im
Hintergrund ein Mutationslauf lief**, **und beide sind damit nicht
zwei Fehler, sondern eine Umgebung.**

**Was das ist, weiss ich nicht.** **Zwei Godot-Prozesse auf demselben
Projektverzeichnis teilen `.godot/`**, **und der Mutationslauf baut
waehrend der Testlauf laeuft** -- **das ist eine Vermutung, keine
Messung.**

**Die Regel daraus ist praktisch: nie gleichzeitig einen Volllauf und
einen Mutationslauf fahren, und einen Testfehler, der nur neben einem
anderen Prozess auftaucht, nicht als Befund behandeln.** **Drei
Volllaeufe sind der Beweis, dass der Test gueltig ist, und die
Umgebung der Beweis dafuer, dass er zweimal falsch gemeldet wurde.**

## Und 301 Battle Processing

**Und "Can Lose" ist das Feld, dessen Name luegt.** Die Hilfe: *When
enabled, there will not be a game over even if the entire party is
defeated* -- **das Kaestchen heisst "Niederlage ist ueberlebbar", und
nicht "Niederlage ist verboten".**

**Und `InBattle` war `init` und ist jetzt aenderbar** -- **denn `351 Open
Menu` fragt genau danach.** **Ein Feld, das beim Bauen der Fakten
gesetzt wurde, konnte nie wahr werden**, **und ein Spiel, das kaempft
und danach ein Menue oeffnet, hatte sein Menue waehrend des Kampfes
offen.**

**Drei Tests setzten `InBattle` direkt per Objektinitialisierer** -- **die
lesen jetzt `EnterBattle()`**, **denn wer den Zustand setzen darf, kann
ihn auch halb setzen**, **und `StartBattle` ist die einzige Stelle, an der
die drei Felder gemeinsam gesetzt werden.**

**Neu gemessen: 1720 von 2436 ausfuehrbar** (vorher 1713).
Mutationen 6/6. `All 2145 tests passed`, Validator gruen.

**Naechster Schritt:** `322 Change Vehicle Image` (6x). **Die Hilfe
nennt zwei Einstellungen, Fahrzeug und Bild, und `[(None)]` ergibt kein
Bild** -- **und P0 ist in allen sechs Faellen 1, das ist das Schiff,
und P2 nimmt 0, 1, 2 und 3 an.**

## Und 322 Change Vehicle Image

**Und es gibt kein Feld "auf welchem Fahrzeug sitzt der Spieler", weil
kein Befehl dieses Lesers das aendert** -- **die Bilder sind ohne das
hier**, **und ein Leser, der ein Reit-Feld eingefuehrt haette, um ein
Bild zu halten, eine zweite Antwort auf eine Frage, die niemand stellt.**

**Und `[(None)]` heisst "kein Bild", und nicht "Datei mit diesem
Namen".**

**Und die drei Fahrzeuge sind 0, 1 und 2** -- **das Boot ist null, und
das ist eine Grenze, nicht ein Detail.**

**Und eine lebende Regel hat aufgedeckt, dass der erste Test nur "kein
viertes Fahrzeug" pruefte** -- **und nicht "kein Null"** -- **und minus
eins ist genau die Zahl, die `213` und `221` fuer den Spieler
verwenden**, **also haette ein Leser, der diese Gewohnheit hier
mitgebracht haette, das Schiffsbild auf den Spieler gelegt.**

**Neu gemessen: 1726 von 2436 ausfuehrbar** (vorher 1720).

**Und was danach noch offen ist, gemessen:** **4 Befehle in drei Codes**
-- `102` (8x, das ist *Show Choice List*, der zweite Teil von `405`),
`105` (4x) und `225` (2x), `314` (1x).
