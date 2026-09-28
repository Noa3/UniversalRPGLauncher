# UniversalRPG — Project Status

> **Last Updated:** 2026-08-31
> **Current Phase:** Phase 2 — RM2000/2003 Parser in progress

## Executive Summary

The project has a Godot 4.7.2 application foundation, localized game-library UI, bounded folder/ZIP inspection, registry-driven engine detection, persisted import metadata, legacy metadata decoding, a real bounded LCF container parser, and a minimal parser-backed RM2000/2003 runtime bootstrap validated against pinned EasyRPG TestGame fixtures. Full gameplay is not playable yet; the immediate critical path is expanding faithful RM2000/2003 parsing, renderer/system coverage, event counters, and walk animation beyond the bounded native event path, the working chipset passability, and the verified autotile animation steps.

The repository uses pure C#/.NET through the Godot 4.7.2 .NET editor. The Godot project (including `project.godot`, `UniversalRPG.csproj` and `UniversalRPG.sln`) lives under `project/`; development docs, `scripts/validate.sh`, and the pinned Godot runtime under `tools/godot/` stay at the repository root. `scripts/validate.sh` runs restore, build, Godot import, and the C# core/smoke suite. The latest headless runner passed `1035/1035` tests. `project/tests/fixtures/` also holds data three real games wrote: sixteen XP `.rxdata` files from two independent installations, a real RM2K database, map tree and two maps from a 743 map game, and one `Game.ini` from a KiriKiri game that is not a WOLF game. Sizes and SHA-256 are in `project/tests/fixtures/RGSS_FIXTURES.md`. No executable, DLL, save, image, audio or script is imported. An RPG Maker MZ game is read in `project/src/mz/`: its database, its map list and its maps come back as values, with the file own text kept so a caller can hash what was read. Eleven real data files are in `project/tests/fixtures/mz` with their sizes and SHA-256 in `project/tests/fixtures/MZ_FIXTURES.md`. **No JavaScript of a game is read or run, so an MZ game does not play.** Every one of its 114 commands is named the way the engine names it, with the number and the name read out of the engine source of a real game; a conditional branch is decided from the facts a caller has; and an interpreter holds an index into an event list and walks it the way the engine moves that index — branches, else, loops, break, repeat above, labels and jumps — under a step limit that is the engine's own `checkFreeze`. A script line is held as the text the author wrote and is never run, and a branch or an operand that would need `eval` is refused and named. **Eleven of the 114 commands have an effect**; the rest are read as text, and there is still no renderer, no save path, no input and no audio.

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
