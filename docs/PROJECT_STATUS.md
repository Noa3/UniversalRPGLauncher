# UniversalRPG — Project Status

> **Last Updated:** 2026-08-31
> **Current Phase:** Phase 2 — RM2000/2003 Parser in progress

## Executive Summary

The project has a Godot 4.7.2 application foundation, localized game-library UI, bounded folder/ZIP inspection, registry-driven engine detection, persisted import metadata, legacy metadata decoding, a real bounded LCF container parser, and a minimal parser-backed RM2000/2003 runtime bootstrap validated against pinned EasyRPG TestGame fixtures. Full gameplay is not playable yet; the immediate critical path is expanding faithful RM2000/2003 parsing, renderer/system coverage, event counters, and walk animation beyond the bounded native event path, the working chipset passability, and the verified autotile animation steps.

The repository uses pure C#/.NET through the Godot 4.7.2 .NET editor. The Godot project (including `project.godot`, `UniversalRPG.csproj` and `UniversalRPG.sln`) lives under `project/`; development docs, `scripts/validate.sh`, and the pinned Godot runtime under `tools/godot/` stay at the repository root. `scripts/validate.sh` runs restore, build, Godot import, and the C# core/smoke suite. The latest headless runner passed `1302 of 1303 tests (one open finding). `project/tests/fixtures/` also holds data three real games wrote: sixteen XP `.rxdata` files from two independent installations, a real RM2K database, map tree and two maps from a 743 map game, and one `Game.ini` from a KiriKiri game that is not a WOLF game. Sizes and SHA-256 are in `project/tests/fixtures/RGSS_FIXTURES.md`. No executable, DLL, save, image, audio or script is imported. An RPG Maker MZ game is read in `project/src/mz/`: its database, its map list and its maps come back as values, with the file own text kept so a caller can hash what was read. Eleven real data files are in `project/tests/fixtures/mz` with their sizes and SHA-256 in `project/tests/fixtures/MZ_FIXTURES.md`. **No JavaScript of a game is read or run, so an MZ game does not play.** Every one of its 114 commands is named the way the engine names it, with the number and the name read out of the engine source of a real game; a conditional branch is decided from the facts a caller has; and an interpreter holds an index into an event list and walks it the way the engine moves that index — branches, else, loops, break, repeat above, labels and jumps — under a step limit that is the engine's own `checkFreeze`. A script line is held as the text the author wrote and is never run, and a branch or an operand that would need `eval` is refused and named. **Eleven of the 114 commands have an effect**; the rest are read as text, and there is still no renderer, no save path, no input and no audio.

Real LMU event pages now decode: the pinned RM2000/RM2003 fixtures yield 22 and 38 event pages with verified liblcf field ids (`condition 0x02`, `move_frequency 0x20`, `trigger 0x21`, `layer 0x22`, `move_route 0x29`, `event_commands_size 0x33`, `event_commands 0x34`). A command vector that cannot be decoded is contained per page with a diagnostic and its raw payload size instead of making the whole map unloadable, and such pages are skipped by the runtime instead of running empty. Page trigger ids follow liblcf `EventPage::Trigger` (`action=0`, `touched=1`, `collision=2`, `auto_start=3`, `parallel=4`). `ControlSwitches` and `Control Variables` follow the verified EasyRPG parameter layout (`[targetMode, start, end, …]`), which real games use, and a regression test executes a real fixture action page end to end through the RM2K runtime.

The bounded RM2K interpreter now supports verified `ChangeGold` command `10310`, `ChangeItems` command `10320`, `ChangePartyMembers` command `10330`, `ChangeExp` `10410`, `ChangeLevel` `10420`, `ChangeHeroName` `10610`, screen effects `11040`/`11050`/`11070`, `ChangeEventLocation` `10860`, `EraseEvent` `12320`, and nested `CallEvent` `12330` for map events. Gold and item counts are bounded to `0..999999`; levels clamp to `1..99`; party mutations support constant/variable actor IDs, add/remove operations, duplicate and capacity protection, and fail-closed diagnostics. Bounded MV and MZ metadata extraction reads top-level `System.json` properties with a real JSON parser (MV `versionId`, MZ `systemVersion`, plus locale, start position and party); both engines share one bounded `data/` inventory reader that requires the matching runtime signature, and no foreign JavaScript is executed. Chipset passability is now resolved from verified data: the LDB chipset section supplies the 162 lower and 144 upper passability entries, and the EasyRPG Player `map_data.h` block constants plus the `Game_Map` upper-then-lower rule turn LMU `lower_layer`/`upper_layer` tile ids into per-tile direction masks. The simulation moves a character only when the target tile's mask permits that direction, and a real-fixture test proves an RM2000 and an RM2003 map both reject steps into impassable tiles and accept steps onto walkable ones.


**K-127, pictures.** 231, 232 and 235 — the first commands in this game that
need something other than numbers to have an effect. Nine on the one map in
the fixture, on images 1, 86 and 87. **This game's `System.json` sets
`picturesUpperLimit` to 110** and not to the engine's fallback of 100, and a
move of zero frames changes nothing at all rather than snapping a picture to
its target. **Reading this card found a lost value in the reader itself**:
`MzCommandEntry.From` turned a JSON boolean into an empty string, and a 232
carries its wait as a real `true`/`false` — so all four of this game's moves
came back as "does not wait". Fixed in the reader, with a test that reads a
boolean out of an event list.

**K-127 did not do 127 or 128.** Those are Change Weapons and Change Armors,
and this game's `Map002` has no 127, no 128, no 129 and no 130, and the
fixture has no `Weapons.json` or `Armors.json`. Rules no data here can check
are not rules worth writing down, so the cards went to what the game actually
uses.



**K-128, and what this game actually needs.** K-127 asked which picture
commands come next and the answer was **none of them** — this map uses no 224,
no 233, no 234 and no 236. The two commands it still had that mean something
mean opposite things. `351` opens a menu and is run: the engine's one
condition is `$gameParty.inBattle()`, and it returns true either way. `357` is
`PluginManager.callCommand`, and it is **refused and named**.

**The finding that matters more than either card: this game ships fifty-two
plugins and all fifty-two are enabled.** Its eleven `357` commands call
`ItemCombinationMZ`, `DTextPicture` and `HyoujouSelect`, and its `355` scripts
read `$gameVariables.value(180)` to work out what was crafted. **UniversalRPG
runs this game's MZ event code and none of its plugin code**, and no bounded
slice changes that. The nine plugin commands are refused by name and land on
`MzBranchFacts.Notices` rather than being stepped over, because a silent step
would leave a game that looks as if it works while its crafting menu never
appears.



**K-129, a second MZ fixture from a game with no plugins.** The first
fixture is a game with fifty-two enabled plugins, so a reader checked against
it is mostly checked on its refusals. `CamelliaCoronation-Win` is the opposite:
**one plugin with an empty parameter list that no command uses, nineteen maps,
and no `355` and no `357` anywhere** — counted over the files. Both games run
**RPG Maker MZ 1.9.1**, measured: the same 114 `commandNNN` methods, none only
in one or only in the other.

**2 432 Befehle, 1 772 davon laufen, 660 nicht** — and every one of the 660 is
a real MZ command rather than a plugin call. The most-used is **401, the line
of text, at 938**; then **101, the dialogue block, at 414**; then **505, the
move route, at 348**. **Fifteen variables, numbered 0 to 15, and none above;
eight items; no switches, no common-event calls, no actor references.**
`CommonEvents.json` is 376 bytes and present, so the common-event rule is
checked against a file rather than against a gap.

**The two fixtures together say how far a bounded slice can go.** The first
game's event code runs and its plugin code never does; this game's event code
runs and there is no plugin code to refuse.



**K-130, and the first command that is neither a change nor a number of
frames.** A 201 does not move the player; it **reserves** the move —
`reserveTransfer` writes `_transferring = true` and the new map and position
and changes nothing a player can see — and then sets
`setWaitMode("transfer")`. `updateWaitMode` asks
`$gamePlayer.isTransferring()` every frame, so **the wait is a condition and
not a count**: a caller passing frames cannot end it. That is a third shape
beside a 230's frames and a 232's movement, and the engine's own modes are
`message`, `transfer`, `scroll`, `route` and `until`.

Four rules, each a place a first reading goes wrong: **a reservation is not a
move**; **the engine returns false and transfers nobody in a battle or with a
message on screen**, which is neither a wait nor a finish and so gets its own
answer; **the direction is set when the transfer happens**, because
`performTransfer` is what calls `setDirection`; and **a map this reader has
not read is named and the player stays put**, because half-applying a transfer
would leave a position with no file behind it.

This game's own numbers: **33 transfers over sixteen maps, every one with the
first parameter at zero**, so the place is written out rather than read from a
variable. A reader that always looked in the variables would send every player
in this game to variable four.

**And one trap that would have come back on the next card.**
`$"Map{i:03}.json"` with `i = 1` produces **`Map13.json`**, not `Map001.json`:
in an interpolated string `i:03` is a fill character and a *precision*, and a
whole number with a precision is padded on the right. The reason is in the test
now. Nine mutation rules, nine caught, first run.



**K-131, and the list was wrong before the code was.** K-129 called `505`
the biggest thing left, at 348 commands. **`command505` does not exist** —
`505` is a nested move-route entry the editor writes, and a route reaches
the runtime through `205 Move Route`: **96 of them**, sixty of which ask to
hold the page and thirty-six of which do not. **The 348 were never event
commands.**

**And the API is not the one a reader would guess.** The passability methods
are `isMapPassable` and `canPass`; `checkPassage` and `isPassable` have
**zero** occurrences in 1.9.1 and come from other RPG Maker engines.
`reverseDir` is `10 - d`, not the `0..3` form — so **down's opposite is up**,
and a first draft that got it wrong placed every character one tile *in
front* of where it had arrived.

**Three rules the engine states plainly and a first reading gets wrong
anyway.** A refused step **still turns the character** — a character that
bumps a wall faces the wall, which is what triggers the action button. A
jump of `3,1` goes **three right and not one down**, because only the
bigger axis moves. And `isStopping` is `!isMoving() && !isJumping()`, two
terms: a draft added `&& !Waiting`, and **every route with a wait in it ran
backwards**, re-issuing one step for ever.

**A product fault with a wider reach than move routes.** A 205's second
parameter is a nested object, and the command reader turned every parameter
into a string — anything that was not a number or a boolean became `""`.
**Every move route in every game came back empty, and the reader could not
have said why.** The reader can now write a value back out.

**This game's own route codes, measured:** MOVE_LEFT 74 leads MOVE_DOWN 50 —
**not the other way round** — then MOVE_RIGHT 59, MOVE_UP 45 and JUMP 31.
MOVE_RANDOM, MOVE_TOWARD, MOVE_AWAY and all eight diagonal codes appear
**zero** times and are named rather than guessed at. Fourteen mutation
rules, fourteen caught.



**K-132, the biggest thing left, and it is mostly not words.** 938 lines of
text, one parameter each — and a line of dialogue is mostly escape codes. The
reading is `Window_Base`'s and it happens in **two passes with two rule
sets**: `convertEscapeCharacters` turns every backslash into an escape
character, **two escape characters put one backslash back**, and the
variable, actor, party and currency codes are filled in — the variable one in
a loop. Then the drawing loop treats **every character below 0x20 as a
control character** and never puts it in the output.

**Three classes, and only one is text.** In the text: `\V[n]`, `\N[n]`,
`\P[n]`, `\G`. **Never shown at all:** `\|`, `^`, `!`, `>`, `<`, `$` — a
reader that emitted them would put a `|` in the middle of a sentence. And
neither text nor pen, **named rather than dropped**: `\C[n]`, `\I[n]`,
`\PX[n]`, `\PY[n]`, `\FS[n]`, `\{`, `\}`.

**This game's own numbers, measured over the files:** nineteen lines ask for
a colour and the same nineteen for icon 177 — **fifty-seven undrawable things
between them** — three lines ask the player to decide, and one line waits
**three times**, because `\|.|\|.|\|.` is three decisions and not one.
Fourteen lines are empty, and an empty line is a blank in a conversation
rather than a fault.

**A measurement that was wrong twice.** A scan of these lines found the
letter `C` 53 times, `N` 38, `V` 22 and `P` 11, and a first reading took them
for escape codes. They are words: "SEND **C**OUT!!", "Valuable **V**egetables".
**A code is a backslash first and a letter second** — which is why I first
claimed this game had no colours, and the files said nineteen.

**And a list of numbers that was wrong.** A list of the codes with no
`commandNNN` method claimed that 601, 602 and 603 have none. They do — they
are three of the 114. It also said "178 reserved numbers" where the K-122
list is the 114 methods. **Nine numbers have no method and all nine lie
outside those 114**: 0, 401, 404, 405, 412, 505, 604, 605, 657. **That is
the fourth name in four cards that came from memory and does not exist in the
engine**, after `checkPassage`, `isPassable` and the `reverseDir` form.


## Phase Status Overview

| Phase | Description | Status | Notes |
|-------|-------------|--------|-------|
| 0 | Repository audit | ✅ Complete | Initial setup |
| 1 | Runtime foundation | ✅ Complete | Core abstractions implemented |
| 1.5 | Application foundation | ✅ Complete | Library, plugin detection/selection wiring, persistence, localization, import safety |
| 2 | RM2000/2003 parser | 🚧 In progress | Real LCF reader + initial LDB/LMU/LSD decoding |
| 3 | RM2000/2003 rendering | 🚧 In progress | Real map and character frames render into pixels and follow a move; the frame is a screen-sized viewport that follows the verified camera, while walk animation and event move routes are absent |
| 4 | Event interpreter | 📋 Planned | Depends on Phase 2 |
| 5 | Full RM2000/2003 systems | 📋 Planned | Depends on Phase 4 |
| 6 | Compatibility work | 📋 Planned | Real-world testing |
| 7 | Enhanced Mode | 📋 Planned | After Faithful Mode stable |
| 8 | RGSS runtime | 📋 Planned | After Phase 5 |
| 9 | MV/MZ runtime | 📋 Planned | After Phase 8 |
| 10 | Native plugin compat | 🔬 Research | Long-term |
| 11 | Android compat | 🔬 Research | Long-term |

## Implemented Systems

### 1. VirtualFileSystem (`project/src/core/virtual_filesystem.cs`)

**Status:** Implemented

**Features:**
- Multi-mount merging (game, override, RTP, save, cache)
- Case-insensitive path resolution
- Path traversal protection
- Path normalization
- Archive access preparation

**Limitations:**
- No archive (ZIP/RVData2) support yet
- No symlink resolution
- Case map rebuild on every mount change (O(n))

**Test Coverage:** Partial (see tests below)

---

### 2. VirtualClock (`project/src/core/virtual_clock.cs`)

**Status:** Implemented

**Features:**
- Deterministic simulation timing (60 Hz base)
- Speed control (0.5x–10x, pause)
- Scheduled callbacks
- Frame-rate decoupling
- Single-step debugging

**Limitations:**
- No save-state serialization yet
- No rewind support
- No deterministic RNG seeding

**Test Coverage:** 8 deterministic regression tests

---

### 3. GameDetector (`project/src/game_detector/game_detector.cs`)

**Status:** Implemented

**Features:**
- Compatibility facade over registered, deterministic detection plugins
- Bounded folder/ZIP inspection with ranked candidates and confidence scoring
- Version, evidence, ambiguity, malformed-input, and structured diagnostics
- Compatibility profile schema validation (legacy schema 0, current schema 1, future-schema rejection) and bounded profile sizes
- Deterministic Markdown compatibility-report export for GitHub issues without upload or external execution
- Explicit Faithful/Enhanced render policy with bounded integer scaling controls
- Persisted candidate/selection/evidence/compatibility records in `user://library.cfg`
- RTP dependency, custom script/plugin, and native library metadata
- Explicit user-provided RTP registry/resolution is bounded, deterministic, and data-only; no proprietary RTP data is bundled or auto-discovered
- Runtime selection through exact plugin IDs, capability checks, platform checks, and no-fallback errors

**Limitations:**
- RM2K/RM2K3 have a safe bounded bootstrap that loads LDB/LMT/LMU into simulation and scheduler state; full gameplay runtime is not implemented. RGSS/XP/VX/VX Ace, RM95, MV/MZ, and Unite remain detection-only.
- Bounded inspection now distinguishes *partial* scans (entry budget reached on a well-formed tree, advisory Info) from truly malformed input (Error); large real games no longer hard-fail runtime initialization for exceeding the 4096-entry budget
- MV metadata extraction now reads bounded `data/System.json` title data and reports `.rpgmvp`/`.rpgmvo`/`.rpgmvm` encrypted assets without executing JavaScript; MV remains detection/metadata-only
- Executables and libraries are inspected as bounded data only, never loaded or executed
- Archive import is read-only inspection; safe extraction/staging for future runtime assets remains separate work
- Missing-asset diagnostics and bounded per-game RTP profile metadata are implemented in K-041; original RM2K/RM2K3 `LSD` saves now have a read-only bounded framing model from K-050, while semantic field mapping, save mutation, and UI integration remain separate work

**Test Coverage:** 15 deterministic detection tests

---

### 4. CompatibilityProfile (`project/src/compatibility/compatibility_profile.cs`)

**Status:** Implemented

**Features:**
- Extensible JSON-based profiles
- SHA-256 hash matching
- Engine-specific profiles
- Per-game flags with global override
- Profile loading from directory

**Limitations:**
- No versioned schema migration
- No profile validation
- No profile signing

**Test Coverage:** 19 deterministic profile tests

## Technical Debt

| Item | Severity | Description |
|------|----------|-------------|
| Headless Godot editor diagnostic | Low | Godot 4.7.2 emits an internal `EditorSettings` message during `--headless --editor --quit`; validation still exits successfully |
| No CI | Medium | No automated build/testing |
| No export pipeline | Medium | Presets exist; signed/release exports are not automated |
| Legacy encoding varies by platform | High | CP932 decoder must be tested on every target, especially Android/iOS |
| No safe archive importer | High | Folder scans are bounded, but archive staging is not implemented |
- Incomplete gameplay runtime | High | RM2K/RM2K3 renders the real pinned map into a golden-regression frame through the verified draw order, with the hero and the event characters placed from the LDB actor and charset, the two tile layers cached exactly as the Player keeps them, the screen tile camera applied by dividing by 16, and the frame recomposited on a simulation boundary after a move. Movement now uses the verified step budget, so a character spends `1 << (1 + move_speed)` per update and its sprite pixels interpolate while its logical position updates immediately, and event move routes decode from the real `EventPage` chunk and execute with the verified order: a facing command takes effect at once, a movement command advances the index only after the move completes, and a blocked move advances only when the route is skippable. Vehicles are modelled, can be boarded and left with the asymmetric rules, drive with the verified speeds, animate, climb and descend with the verified altitude budget, and draw with the airship shadow. What is still missing: picture and other presentation effects, audio, the battle system, a full save and load, and input beyond the turn order. A synthetic floor still does not render, because the LDB tile substitution tables are never populated, so the wide map test asserts applied offsets rather than pixel differences. The RGSS engines XP, VX and VX Ace have an archive reader, a Marshal reader, a lexer, a parser and a value layer, all data only, and no runtime: `RgssEngineRuntime` is still a metadata inspector. MV and MZ have no runtime source tree at all |

## Missing Core Components (Planned)

| Component | Phase | Priority |
|-----------|-------|----------|
| Expand/validate RM2K parser | 2 | Critical |
| RM2K interpreter | 4 | Critical |
| RM2K renderer | 3 | High |
| RGSS runtime | 8 | High |
| JavaScript runtime | 9 | High |
| Win32 API shim | 10 | Medium |
| Save state system | 5 | High |
| Asset override system | 7 | Medium |
| Input abstraction | 1 | Implemented for RM2K keyboard/controller/touch mapping; broader platform abstraction remains open |
| Audio abstraction | 1 | Low (planned) |
| Renderer interface | 1 | Low (planned) |

## Current Build Status

| Target | Status | Notes |
|--------|--------|-------|
| Godot Editor | ✅ Runs | project.godot created |
| Linux headless | ✅ Tested | Godot 4.7.2 import and all UI locales start |
| Windows Export | ⏸️ Not tested | Preset present; templates/toolchain needed |
| Linux Export | ⏸️ Not tested | Preset present; templates needed |
| macOS/iOS Export | ⏸️ Not tested | Requires macOS, Xcode, signing, templates |
| Android Export | ⏸️ Not tested | Preset present; Android SDK/templates needed |

## Next Immediate Tasks

1. K-016: typed bounded RPG Maker MZ metadata inspection and explicit encrypted-asset diagnostics; keep MZ detection-only until a safe JavaScript runtime boundary is separately verified
2. Expand typed LMU decoding incrementally; do not guess undocumented offsets/fields
3. Test CP932/Shift-JIS behavior on target platforms and add malicious-input fixtures
4. Implement LMU event/page metadata decoding without executing commands

## Open Questions

1. Which Ruby VM to embed for RGSS? (mruby, rbx, custom?)
2. Which JavaScript engine for MV/MZ? (V8, QuickJS, Duktape?)
3. Should we use C++ for the RM2K parser (performance)?
4. How to handle large game databases efficiently?
5. What is the target minimum hardware spec?

## Risk Assessment

| Risk | Likelihood | Impact | Mitigation |
|------|-----------|--------|------------|
| Ruby VM embedding complexity | High | High | Start with mruby, evaluate later |
| JavaScript engine size | Medium | Medium | Use QuickJS (small footprint) |
| RM2K format reverse-engineering | Medium | High | Public documentation exists |
| Performance on mobile | Medium | High | Profile early, optimize hot paths |
| Legal issues with RTP | Low | High | Never bundle RTP, user provides |
| Win32 compatibility scope creep | High | Medium | Strict scope control, phase gates |


## 2026-08-20 Stabilization Pass

Changes prepared in this pass:

- fixed repeating `VirtualClock` callbacks so they keep their requested interval instead of firing every tick after first expiry;
- corrected slow-motion to use a real speed factor (`0.5 == half speed`), added stable callback IDs and monotonic FPS sampling;
- fixed compatibility-profile precedence so per-game flags actually override global defaults;
- repaired the previously non-compiling/incomplete `RM2KDatabase` data model and added round-trip regression tests;
- added `scripts/validate.sh`, GitHub validation workflow, `KANBAN.md`, `AGENTS.md`, `SESSION_STATE.md`, and the Hermes autonomous-work prompt;
- added provenance-pinned EasyRPG TestGame RM2000/RM2003 LDB/LMT/LMU fixtures and real-framing regression tests;
- accepted valid zero-length LDB struct-array sections while preserving bounded malformed-input rejection.

## 2026-08-22 Typed LDB Slice (K-012/K-015)

- `ParseDatabase` now decodes the actors section into typed entries: name/title/character_name/face_name strings plus character_index, transparent, initial_level, final_level, critical_hit, critical_hit_chance, face_index integers; defaults mirror liblcf `rpg::Actor` initializers.
- Switches and variables sections decode to id/name entries; duplicate structure IDs are rejected.
- K-015 now additionally decodes scalar metadata for skills/items/states/classes/enemies/terrains/attributes/troops/animations/chipsets/battle_commands using field IDs verified against EasyRPG liblcf; nested arrays remain data-only and are retained as unknown fields.
- Field IDs are verified against EasyRPG liblcf `src/generated/lcf/ldb/chunks.h`; unknown actor/entry fields remain preserved per entry for diagnostics.
- Synthetic fixtures cover defaults, unknown-field retention, duplicate IDs, missing terminators, and battle-command trailing data; real-fixture tests assert typed entry counts equal section counts on both pinned TestGame LDBs.
- Validation: `GODOT_BIN=E:/URPG/Godot_v4.7.2/Godot_v4.7.2-stable_mono_win64_console.exe ./scripts/validate.sh` passed with Godot `4.7.2.stable.mono.official.ed1daf0bf`; headless suite `170/170`.

The C# migration and plugin application wiring have been validated with the local Godot 4.7.2 stable .NET editor on Windows: `dotnet build` passed, script registration succeeded after PascalCase file renames, and the headless C# core/smoke runner passed `159/159` at that time (now `171/171`).

**RPG Maker MZ, in bounded slices, and no more than that.** A real MZ 1.9.1
game is detected from its own files and its data is read as a value tree
(`Actors.json`, `System.json`, `MapInfos.json`, maps and events). Every one of
the 114 commands an MZ game stores is named from the engine's own
`Game_Interpreter` methods, a conditional branch is decided from the facts a
caller has, and an interpreter holds an index into an event list and walks it
the way the engine moves that index — branches, else, loops, break, repeat
above, labels, jumps, and a step limit that is the engine's own `checkFreeze`.
Eleven of those 114 commands have an effect; the rest are read as text. **There
is still no renderer, no save path, no input and no audio, and no line of a
game's JavaScript is executed** — a branch or an operand that would need `eval`
is refused and named.

**K-133, and the first command in this reader that eats other commands.**
`command101` runs `while (this.nextEventCode() === 401) { this._index++;
$gameMessage.add(…); }` — so **a line of dialogue is never dispatched**, because
there is no `command401` for it to be dispatched to. Every one of this game's
938 lines belongs to a 101 and to nothing else.

**Measured over the files:** 414 dialogues, one to four lines each — 118 with
one, 130 with two, 104 with three, 62 with four — totalling exactly 938. Eight
are followed by a 102; there is no 103, no 104 and no 403 in nineteen maps.
**And a dialogue that is already up is refused**, because `isBusy()` is text
*or* a choice *or* a number *or* an item to choose — so a 101 behind an
unanswered choice is refused as firmly as one behind a line. It always ends in
a wait, `setWaitMode` being outside the `switch`, so a dialogue with no choice
holds its page all the same.

**Two names that had to be measured.** `102` is not `405`: `ShowChoices` has
meant 405 since K-132, and comparing the follower against it meant **not one
of this game's eight choices was ever found** — 1352 commands instead of 1360,
across four runs, because the tests that failed were the ones checking a sum.
And `params[0]` is an **array**, not a bar-separated string, so a first draft's
`split("|")` would have read one option reading `["Yes", "No"]` with its
brackets in it.

**`params[1] || 2` is 2, and a written zero is 2 as well** — 0 is falsy in
JavaScript, so a 104 with no category and a 104 with `0` both get the whole
party.

**And a guard with no test.** `ExecuteOne` had a bounds check that a mutation
switched off without a single test noticing, because `IsRunning` meant the
guard was never asked. The repair was not a test for it but **its removal**:
the case is handled one level up. **A second check that can never fire is a
claim a reader will believe and nobody can prove.**

**K-094, and the sentence that outlived the feature it described.**
`Rm2kPlayerTurn.Apply` said *"this runtime has no vehicles, so nothing can be
toggled and the action event check always runs"* — while `Rm2kDecisionTurn.Run`
sat next to it, implemented, mutation checked, and never called. The vehicles
were loaded and drawn, never driven and never boarded.

**And `TileInFront` was speaking the wrong direction order the whole time.** The
player speaks 2/4/6/8; `DirectionDelta` expects 0–3, so an 8 produced `(0, 0)` —
the character's own tile. Every boarding test passed without anything moving, and
a player facing up was handed a disembark onto the water they were already
standing on. The conversion between the two orders already existed in the
project, under a comment warning that mixing them silently turns a right step
into a left one.

**A K-114 test held the wrong order in place**, checking `TileInFront` with the
same 0/1/2/3 it should have refused — five assertions, all consistent with one
misreading. **When a test and the code share a misreading, the suite is a
change detector, not a correctness proof.**

**K-135 is closed, and closing it meant taking back a pushed fix.** Seven
command codes that two real games use and this reader skipped: `1009` was
recorded as a message continuation line and is actually `ChangeBattleCommands`
— four integers and no text, which is a battle command change and not a
sentence. The wrong fix was pushed as `5a9ca22` and reverted. The mistake was
pattern-matching: 20 bare `1009` after a `10110`, and MZ's `401` after a `101`,
and a conclusion the fixture settles against.

**All seven now run.** The five menu commands refuse visibly where EasyRPG
returns a silent no-op, `1009` treats "absent" as "the database's commands"
rather than as "nothing", and `11610` is a second prompt — it stores a key
*code*, not a key, and nothing in the file says which of the two asked.

**1025/1025, nine effective mutation rules, nine caught.**

**And two picture commands were implemented, bounded, tested — and unreachable
by any command.** `ShowPicture` took six scalars and threw the other eight
parameters away. That is the same shape of fault as K-094's vehicles, and it is
why K-136 exists: liblcf names 164 command codes, this interpreter dispatches 43,
and **89 real commands fall into the default branch.** Two of those 89 were
already written.

**Reading the reference found three faults in the first hour of that work.** The
transparency is a percentage clamped to 100, not a colour channel — so every
real picture in a game, carrying 0, 50 or 100, passed by accident. The Maniac
bitmask applies to the lower transparency only, and the erase command reads the
id *before* the mode, not after — so the one-parameter command the editor writes
most often erased the picture numbered "nothing".

**1035/1035, eight effective mutation rules, eight caught.**

**And the two commands that decide where a page goes.** `12110 Label` and
`12120 Jump to Label` were the sharpest entry on the 89, because they change
*where* the page goes rather than what it does. The search starts at zero, so a
backward jump is a loop — that is how an author writes one without a loop command
— and the engine increments the index only when a command left it alone, so the
page lands *on* the label, which is a no-op that costs a frame of its own.

**Two drafts got that conditional wrong in opposite directions, and both were
silent.** One left the page on the jump forever, which looks like a hang; the
other advanced unconditionally and skipped the no-op the format puts there on
purpose. A suite that counted commands rather than frames could not have told
the two apart.

**1043/1043, six effective mutation rules, six caught.**

**And four position doubles that nothing read and nothing wrote.** `BgmPosition`,
`BgsPosition`, `MePosition` and `SePosition` were the residue of a plan for
playback this repository has not built. A double no command moves is a claim
about time that nothing keeps. They are replaced by the five audio commands and
what the format actually holds: the current track per channel, the fade state,
and one memorised BGM. It is data, not sound — there is no player behind it and
no test claims a track can be heard.

**The two commands' parameter lists do not line up**, and a first draft read
both from the same offsets, which put the sound effect's volume where its balance
belongs. It then refused every command whose values were non-zero — every music
command a real game writes — and gave the reason "this reader does not decode a
bitfield yet". That read as care. It was a source not read to the end:
`ValueOrVariableBitfield` opens with `if (!IsPatchManiac()) return
com.parameters[val_idx]`. **Refusing loudly is not a substitute for knowing.**

**1055/1055, nine effective mutation rules, nine caught.**

**And the two screen transition commands, whose tables are not regular.** Show
and erase are the same twenty kinds read from opposite ends, and each parameter
number names a different one in each table. The stripes and scrolls mirror their
suffix; the divisions pair with the combines, so a reader that mirrored the name
would pair `CrossDivision` with itself and animate nothing. There is a test for
exactly that, over all three division arms.

**Parameter -1 is not a kind** — it is the game's own teleport transition, from
the editor's settings and not from the command. The reference's two `switch`es
have no default arm, so -1 and every number it does not know fall through to
none in silence, which is what would make every teleport in a game lose its
transition without a word.

**`11060 Pan Screen` is in liblcf's enumeration and EasyRPG dispatches it
nowhere.** This repository does not implement it: there is nothing to read the
parameters from, and a reader that implemented a command the reference does not
would be inventing a semantic.

**1069/1069, nine effective mutation rules, nine caught.**

**And a move-route state machine with no caller anywhere in the project.** K-131
built the decoder and the state machine, mutation checked them and tested both
as free-standing objects, and no event could put one on a character — the same
island shape as the pictures in `PresentationState`. Two commands close it, and
`11310` inverts its parameter, which is the whole command: a reader that mapped a
non-zero to visible gets a hide right and a show wrong, and a game whose only use
is to hide a sprite works until the first time it shows one.

`11340` and `11350` are in liblcf's enumeration and appear nowhere in EasyRPG's
interpreter, so they are not implemented here either: there is nothing to read
the parameters from.

**1079/1079, eight effective mutation rules, eight caught.**

**And a three-parameter command whose parameters are the variables to write into,
not the position to store.** A reader that read them as a position would write
the player's tile into three variables and store nothing at all — which is
exactly the failure a three-integer command invites when every parameter is the
same kind. All three ids are checked before any of them is written, because a
reader that wrote as it went would store the map and then hit a zero, leaving a
game half-memorized.

`10830 Recall To Location` is in liblcf and has no method in this build of
EasyRPG, so it is not implemented: the asymmetry is the reference's, and a
reader that guessed the second half would teleport players to tiles the file
never described.

**1085/1085, six effective mutation rules, six caught.**

**And a timer that started when it was only set.** The reference has three
operations in one command — set the seconds, start with the visible and battle
flags, stop — and a reader that started on set collapsed the first two. A game
that wrote SetTimer to arm a countdown it would start later started it
immediately.

**The save codec had the same fault.** It restored a timer with SetTimer alone,
so every saved countdown came back running: a game that saved a paused timer and
reloaded it got a live one. The two old tests that broke on the repair were using
SetTimer as "start the timer" — the same misreading. They were corrected, not
weakened, and the round trip now proves both operations separately.

**Message options are four flags and not one style**, and parameters[2] is
inverted: a zero means the window holds its position while the map scrolls. A
reader that mapped a non-zero to fixed would scroll every window a game had
pinned, and that is visible only while the map moves, so no test of a still map
could have caught it.

**1100/1100, ten effective mutation rules, ten caught.**
**And a save codec that had never heard of an actor.** Every base value and
every current count was lost at the next save: a hero who was nearly dead
reloaded at full health, and a 10430 a game did was gone. Bases and current
counts now travel together, because a save that kept the counts and dropped
the bases would reload a hero clamped to a maximum he no longer has.

**Base and current are different things, and the reference has two calls for
them.** 10430 calls SetBaseMaxHp; a buff calls SetMaxHp. The base survives a
level change and a save, and this reader keeps it in `Rm2kActorValues` with
deliberately no field for the current maximum.

**HP and SP clamp differently.** HP has a lethal flag and a floor of one when
it is not set. CommandChangeSP has neither — the reference writes
`if (sp < 0) sp = 0;` and nothing else, and a reader that gave SP the same
floor as HP would leave a hero unable to cast anything.

**1114/1114, nineteen effective mutation rules, nineteen caught.**
**And three one-line commands whose default matters more than their body.**
11840, 11930 and 11960 are `SetAllowEscape(parameters[0] != 0)` and its two
siblings in the reference — that is the whole command. A zero is a removal
and not "no change", so a cutscene that locks the menu and one that unlocks it
again write the same field; a reader that only ever set the flag to true could
never give a player their menu back.

All three default to allowed, because a database that never ran one of these
commands has all three set. A reader that defaulted to forbidden would make
every untouched game unplayable the moment the player pressed Escape — and no
test of a command would ever have found it, because a command-free game is the
case nobody writes a test for.

**1119/1119, eight effective mutation rules, eight caught.**
**And a board note that was wrong.** 10920 Store Event ID was listed as
having no method in this EasyRPG build. It has one — CommandStoreEventID —
and the note is corrected. The method sends both coordinates through
ValueOrVariable with the same mode, stores 0 for an empty tile without
holding the page, and this reader refuses a tile outside the map rather than
answering with a zero that reads exactly like "no event here".

**Teleport parameter 4 says the switch must be ON, not that there is one.** A
reader that read it as "use a switch" would make every conditional warp
unconditional and open a secret entrance at the start of the game.

**Game over and return to title take no parameters and both wait for an open
message first** — a hero who says their last line and then dies should die
after the line is read — and both hold the page.

**1132/1132, ten effective mutation rules, ten caught.**
**And a board that silently lost a command.** 11820 Change Teleport Access was
never on K-136: the board listed the range "11810–11840" with individual codes
and this one fell between the entries. The reference has it —
SetAllowTeleport(parameters[0] != 0) — and it is the fourth of the four
one-line access commands. **A board with gaps between range entries loses
commands**, so the open list is now measured against the source and no longer
copied from the board.

**A default parameter on the state value was a trap, and the tests found it.**
SetAccess gained a fourth parameter pTeleport = true, and every one of the
three older calls silently reset teleport through it — the last command won,
not the one that named the flag. The default is gone, because a default on the
state value has no business being there.

**1141/1141, ten effective mutation rules, ten caught.**
**And a second block that was never on the board.** 10660, 10670, 10680 and
10690 — system BGM, system SFX, system graphics and screen transitions — came
out of a fresh measurement of the reference against the interpreter. The card
list has now been short twice and actively wrong once.

**The audio families have different widths: seven music and twelve sounds.** A
reader that offered only the menu sounds would leave a game with a silent
battle. Music has a fade-in and sounds do not, because a sound effect with a
fade is one the player waited for.

**The contexts are zero based.** BGM_Battle is 0, so a guard that started at one
would refuse the battle theme — the one a game changes most. And without the
Maniac patch the parameter is the value: a reader that read parameters[5] as
the value would silence every track, which is what the first run of this slice
did.

**Transition_Count is a count and not a last index**, so a guard that read five
as the last would refuse the transition that brings the player back from a
battle. The reference asserts on an unknown one; this reader names the six.

**1153/1153, twelve effective mutation rules, twelve caught.**
**And a vehicle id that is not a vehicle.** 10850 Set Vehicle Location takes -1
to mean the party, and the reference has a comment saying why: RPG_RT stores -1
for a party in no vehicle. A reader that refused it would make every "teleport
the hero" command in a game do nothing, and that is a very common command.

The vehicle id is shifted by one because the liblcf enum is None 0, Boat 1,
Ship 2, Airship 3, and those numbers are in the save format. 10650 sets two
fields — the current sprite and the original — because the original is what a
vehicle returns to when a board ends, so a reader that set only one would leave
a boat in a costume after the party got out.

**Boarding is nullable and that is a design**: a game that never touches a
vehicle never allocates one, so a reader that dereferenced it would throw on
every 10850 in a game with no ship. The first run of this slice did exactly
that and the tests found it.

**1167/1167, eleven effective mutation rules, eleven caught.**
**And a command with no writer at all.** 11750 Tile Substitution had two
144-entry tables, two readers and no setter, so it could be parsed and never
run — and a test of the readers would have been green the whole time.

**11720 has six flags and two speeds, and the speeds come from different
parameters than the flags**: the flags are 0, 1, 2 and 4, the horizontal speed
is 3 and the vertical is 5. The fourth flag and the horizontal speed are
adjacent, which is what makes the mistake easy.

**Zero encounter steps is a real value and it is the one that turns random
encounters off** — and a new game that inherited zero would be unwinnable,
with no fights and no experience.

**1179/1179, ten effective mutation rules, ten caught.**
**And a field the parser was throwing away.** 20140 Show Choice Option and 20141
Show Choice End are a sub-command pair, and the number that says which branch is
the chosen one comes from the LCF 0x0D indent chunk. The decoder read that chunk
and wrote it into its dictionary, and EventCommand had no field for it — so
every event parsed completely and no branch could ever be identified. A test of
the codes, the parameters and the strings would have been green the whole time.

A reader with only one half of the command would have a hero who asks a
question, walks away, fights the guard, buys the sword and leaves, all in one
frame. Each branch ends with its own 20141, because the skip walks to the next
command from {ShowChoiceOption, ShowChoiceEnd} and a branch without an end of
its own would swallow every branch after it.

**1185/1185, seven effective mutation rules, seven caught.**
**And a damage command that is not a battle.** 10500 Simulated Attack has no
turn order, no troop and no target selection: it picks heroes, computes one
number from their defence and spirit, and subtracts it. This slice did not
have to build a combat system first.

Defence is divided by 400 and spirit by 800, so 800 points of spirit block
exactly as much as 400 points of defence — a reader with one divisor would make
spirit twice as strong as the game meant it, by a factor of two on the axis a
game tunes.

The result is floored at zero twice, because a variance draw can push a small
result below zero, and negative damage heals the hero the command was aimed
at. The result variable holds the last actor damage and not the sum, because
the reference writes it inside the loop.

**1193/1193, ten effective mutation rules, ten caught.**
**And a command that was waiting for data, not for code.** 1008 ChangeClass
carries a class id, a level reset flag, a skill mode and a parameter mode, and
the parameter mode is meaningless without the class table — the reference reads
a level out of it. The LDB class parameter chunk was being read into
unknown_fields and staying there. Nothing was lost: the raw chunk is still
there, beside the decode. What was missing was a way in.

**Six int16 vectors and not six scalars.** liblcf rpg::Parameters holds one
vector per stat with one entry per level, written in liblcf order with no
lengths in front. A reader that assumed six scalars would read the first value of
each vector — a level 99 class would give its heroes level 1 stats, and every
number would be in range, so nothing would look wrong.

**1200/1200, nine effective mutation rules, nine caught.**
**And a WOLF branch with one comparison and no test.** IfVariable compared
with == and nothing else, and no test in the repository exercised it at all —
so the one comparison it had was as unproven as the six it was missing. The
editor offers seven: greater, greater or equal, equal, less or equal, less,
not equal, and bit and. A reader that implemented only == would take one
branch in seven, and a chest guarded by "V0 is at least 1" would never open.

**The bit-and test is equal to the value and not "any bit set".** With V0 = 5
(101) and value 2 (010), 5 & 2 is 0, not 2, so the test fails. A reader that
wrote (variable & value) != 0 would pass every test with any bit set, and a
game that guards a door with a bit test would open it for everyone.

**A branch has two arms and the VM had one jump**, so when the condition held
the true arm ran and the false arm ran too — a chest that opened and a guard
that attacked in the same frame. Both arms are now their own target, and the
last command of an arm jumps over the other.

**Stated rather than hidden: there is no native WOLF fixture.** The wolf
fixture directory holds a synthetic envelope and a README that says so, and
the seven comparison numbers are pinned against the editor help rather than a
file on disk.

**1208/1208, ten effective mutation rules, ten caught.**
**And a flat dictionary that could not hold the WOLF variable bands.** The
editor lists four — self, normal and reserve, system, and the variable
database — and the VM had one Dictionary<int, int>, so a self variable and a
system variable with the same index collided. The collision is silent: both
reads answer with a number and only the wrong one.

The million boundary is the addressing scheme itself: a number at or above
1,000,000 is a reference, not a value. The boundary is inclusive, so a reader
that tested > would treat exactly 1,000,000 as a value and never resolve self
variable 0 — the one variable every WOLF event uses. The block is one based
and the band zero based, and the database band is smaller than the other three.

**The runtime fixture changed and that is the point.** Its variable operand was
a bare 1, which is the value one, so it was writing a value rather than a
variable. It is 2,000,000 now, and the runtime test failed when the model
changed — which is what a runtime test is for.

**1218/1218, ten effective mutation rules, ten caught.**
**And two assignment operators where the editor offers fourteen.** The help
tabulates them as =, +=, -=, *=, /=, %=, pull up, pull down, absolute value,
arc tangent, sine, cosine, and square root. The VM knew two, and the second —
addition — was hard coded into its own opcode, so there was no place to put the
other twelve. A reader with two cannot compute a hit rate, a damage formula, or
an angle.

The operator is a field and not an opcode, and AddVariable is the addition
operator over the same path, so the two lists cannot drift apart. The current
value is read before the write, because a right hand side that names the same
variable as the destination has to see the old value. Division by zero leaves
the variable alone and is not an error, which is what the help says. Trigonometry
is scaled — tenths of a degree in, thousandths out — and a reader in degrees and
floating point would return 0.866 and it would not look wrong.

**The whole computation is wide, and a test proved it had to be.** A right hand
side of three billion does not fit an int, and both the multiply and the subtract
leave the range before the clamp can see them, so clamping an int clamps the
value after the wrap.

**Two tests proved themselves wrong and are recorded as such.** The rounding
test first claimed the help's examples separate rounding from truncation, and
they do not; measured, the separating input is seven, whose root times a thousand
is 2645.75. And a test asserted sin(1800) equals 1000, which is ninety degrees
wearing a half turn's comment; measured, 1800 is a hundred and eighty degrees
and gives 0.

**1233/1233, twelve effective mutation rules, twelve caught.**
**And band offsets that were guessed where the help names them.** The previous
card read the four bands out of the help and then guessed their numbers, writing
"a million block per band" — and the tests asserted that guess and passed. The
help names them on two pages: 1,100,000 for map self variables, 1,600,000 for
common self variables, 2,000,000 for normal variable 0, and 3,000,000 for string
variable 0. A computed block put common self on a normal variable and the system
band on the string band, and a game reading its system clock out of the string
range would have got a plausible number.

**A mutation run did not catch it, and that is the lesson.** Twelve rules around
the offsets were all caught, because the tests agreed with the code. Two wrong
numbers that agree produce a green suite. The check that would have caught it is
reading the source, and the offsets are now pinned in a test that names the page.

The million itself is a reference naming no band, and the whole gap says so — a
different answer from "not a reference", with -1 for a value and -2 for a
reference naming nothing. The string band is recognised and refused rather than
answered with a number, and the variable database is not a band at all: the help
says a variable call may not be given when it is the source, so it is addressed by
type and column in a store of its own.

**And the switches were one dictionary where the help names two ranges.** 0 and
above address a map event, 500,000 and above a common event, so a map switch and a
common switch with the same index collided. The base is 500,000 and not a million,
and a switch number outside both ranges changes nothing, because a reader that grew
a dictionary would store a switch the next load would not carry.

**1243/1243, thirteen effective mutation rules, thirteen caught.**
**And a move route that was read, tested, and never run.** The type table had
twenty four verified types and a binary reader with eleven tests, and nothing
executed a single step. A game with a patrol route would load and stand still,
and the suite stayed green because it only ever read steps. A reader that is
tested and not executed is a parser.

The passability bits are the help's own — 1 up, 2 left, 4 right, 8 down, 16 up-left,
32 up-right, 64 down-left, 128 down-right — and a diagonal is its own bit, not up
plus left, because their sum is 3 and there is no bit 3.

**A refused step still turns the character to face the direction.** The first
version returned on the refusal and left the facing alone while the comment above
it promised the opposite, and the test is what caught the two disagreeing.

Speed and frequency are 0 to 6 and are not the same thing — the help writes one
slow to fast and the other often to rarely. Speed 0 is one frame per tile and not
an infinite wait, because dividing by the speed would mean a route that never
finishes.

**Five steps are refused, and that is the honest answer.** Approaching an event
needs a second character, approaching a position needs the map, a jump needs its
own route, a sound needs audio, and a graphic is a file name this runner has no
loader for. A refused outcome is the one answer a caller can act on.

**The ten key facing table is deliberately not implemented** — the help points at a
figure that is not in the text, and an earlier draft guessed the table and produced
duplicate values, which is impossible. Guessing the band offsets cost a card, and
this is the same mistake in the same session.

**1256/1256, seventeen effective mutation rules, seventeen caught.**
**And a runner that can be called, which is not yet a game.** The previous card
built a step runner; nothing held a figure to run the steps on, and the VM had no
characters and no map. This card adds the board, two opcodes, and the clock — and
the board is the VM's own, over the VM's own variable bands, so a route step that
stores to a variable writes where the event reads it.

**The timing was wrong three times, and the order of two lines is why.** First the
index was checked before the frame budget: a step advances the index when it runs, so
a step that had advanced past the last one was already "at the end" on the next frame
— the route ended, the repeat flag reset the index, and the fifteen frames the step
had asked for were thrown away with it. The step ran every second frame and a guard
crossed the screen eight times too fast. The symptom was a figure at X = 9 after
seventeen frames, while every function measured correct in isolation.

**Then a frames-left of zero proved ambiguous** — "still on the last frame" or "no
step has started" — and both readings were in the code. `IsStepRunning` makes it
exact: a step of n frames is started by one of them, so it is finished on the nth
tick, and the tick that finishes it also looks at the next step.

**The two waits share one state and need two endings.** The frame wait counts down;
the route wait ends when the board says the movement is finished. Without a flag to
tell them apart, the VM resumes on the move route command and starts the route over
forever — the frames are zero for the whole wait, so the event hangs with no error
anywhere.

**1271/1271, sixteen effective mutation rules, sixteen caught.**
**And figures that moved through walls.** The previous card made characters move,
and they moved through walls, because the board had a map id and a width and nothing
else. There was no passability anywhere in the WOLF reader: the map data carried
tiles, and tiles are pictures.

**Six states and not two.** The editor cycles them ○ → × → ▲ → ★ → □ → ○, and only ×
blocks — the other three add a drawing rule and not an obstacle. The sixth takes the
layer below's answer and is passable where there is no layer, which the help says
outright: a reader that refused it would freeze the hero on the floor, and a floor tile
with nothing under it is the most ordinary tile in a map.

**Two layers, because the arrow asks one of them.** The upper layer answers and the
lower only where the upper asks, so a ★ chip over water is passable.

**A figure with no map at all cannot step**, and that is the point: "the map was not
read" is not "the tile is passable", and a reader that treated a missing map as open
ground would let a guard walk through every wall on every map whose chips it could
not read — with no error anywhere.

**Nine of the existing route tests failed when this landed**, which is the change
working: since the map became a refusal rather than an absence, every test that
measured a moving step had to give its figure a map, and three of them had been
measuring the refusal and calling it a route.

**1281/1281, sixteen effective mutation rules, sixteen caught.**
**And figures that walked through every guard.** The previous card gave figures
walls and nothing else, so a hero walked through every guard, shopkeeper and sign
in the game — and a walk through a guard is a walk, so the only trace would have
been a hero standing inside a shop.

**The hitbox is a tile wide and half a tile high**, and the help gives the number:
off it is one tile by half, and the square option makes it a full tile. A reader
that used one tile for both would make every half-height figure collide with the
figure on the tile above, and a crowd in a corridor would lock solid — a game that
cannot be finished, and it looks like a pathfinding bug rather than a hitbox one.

**X is half open and Y is closed, and that asymmetry is a decision.** X half open
keeps a corridor walkable; Y closed is what makes a square figure solid and what
gives the square option any meaning at all. The help does not spell the comparison
out, so it is stated as a choice with its reasons.

**A ghost is walked through, and one sided.** The option belongs to the ghost and
answers before anybody is asked, so a ghost walks through a solid figure and a solid
figure walks through a ghost. A symmetric reader would wall off every invisible
trigger in the game.

**One test read that rule backwards and the code was right** — it asserted a refusal
when the hero stepped onto a ghost, which would have made a transparent decoration a
wall. Four of the eleven tests were measuring the wrong thing for a reason that had
nothing to do with collision.

**The hero was not on the board until this card**, because the occupant list started
empty and the first call a game makes is the one that hands out the map — so the hero
could not step at all, and a game with an event on it would have opened with a player
stuck.

**1292/1292, sixteen effective mutation rules, sixteen caught.**
**And two approach steps refused for two cards.** The honest reason was that
approaching an event needs a second figure and approaching a position needs the map,
and the board had neither. The last two cards gave it both, so the refusal was no
longer true — a reader that kept it would have a game whose guards never approach
anything.

**The target numbers are the help's and are not event ids:** 0 and above is the event
with that id, -1 is this event, -2 is the hero, and -3 to -7 are the five companions.
Zero is a real event id, so a reader that used it for the hero would answer a command
about event 0 with the player and about the player with event 0 — and both exist in a
real map.

**Arrival and refusal cannot share one bool**, and an earlier version did: false meant
both "already arrived" and "no such target", so a guard that had reached its target was
recorded as chasing somebody who is not there. Four answers, because each pair differs.

**The collision rule and the approach rule meet in one place.** A guard one tile short,
blocked by its target, has arrived — reading it as blocked would have a guard give up
the moment it caught the player.

**Two bugs the compiler said nothing about:** the companion range was written "at least
-3 and at most -7", which is empty, and an always-false range is valid C#. It appeared
twice, and the compiler flagged the first as an unreachable arm and said nothing about
the second. **Three of the ten new tests were wrong too, two of them about the rules
rather than the code.**

**1302/1302, fifteen effective mutation rules, fifteen caught.**
**And a character sheet layer that did not exist.** WOLF had twenty five files and
not one of them drew anything. The material specification gives what a sheet has to
be: the four directions are down, left, right, up — not the compass order — the walk
cycle is B → A → B → C → B with the middle cell twice, and the idle cycle runs the
other way with the idle cells to the left.

**The animation frequency is frames per step and the order is the opposite of the
speed** — the help writes アニメ頻度[早0-6遅] while the move speed runs slow to fast.
Zero is every frame and not never.

**Three of the eleven tests found real defects**: a facing step reported the same
outcome as a movement, so a guard that turned in place was drawn with the walk cycle;
the idle offset was applied twice; and IsWalking was set on every step rather than on a
movement. Turned is now its own outcome.

**One test throws "Attempted to divide by zero" and the cause is not known** after
sixteen measurements: IdleCell has no division, neither does WalkPattern, the file has
none, the constant reads 3, a test touching only the constant passes, a call with a
literal passes, and calling the same expression twice does not separate it. Renaming
the suite moved the name in the report and nothing else, and deleting obj, bin and
.godot/mono does not change it. The card is **VERIFY** rather than DONE, the test
stays as a failure, and no mutation run was made over a red suite.
