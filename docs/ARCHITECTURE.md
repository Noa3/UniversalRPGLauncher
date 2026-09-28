# UniversalRPG - Architecture

> **Status:** Phase 2 — RM2000/2003 Parser
> **Last Updated:** 2026-08-20

## Design Philosophy

UniversalRPG is a self-contained cross-platform RPG Maker compatibility runtime.
The goal is to interpret RPG Maker games natively, preserve original behavior,
and optionally enhance presentation on modern hardware.

### Core Principles

1. **Correct game behavior** — Original behavior takes priority
2. **Compatibility** — Support as many games as possible
3. **Stability** — Never crash on malformed input
4. **Security** — Treat imported games as untrusted
5. **Performance** — Run efficiently on mobile hardware
6. **Enhancements** — Graphics/UI improvements are secondary

## Architecture Overview

The repository currently uses this concrete layout. Planned interfaces should be
added only when implementation reaches them; this document must not pretend empty
future directories already exist.

```text
UniversalRPG/
├── project/                 # Godot project root (project.godot, csproj/sln)
│   ├── app/
│   │   ├── launcher/        # Runtime availability/launch workflow
│   │   ├── library/         # Game library scan/settings
│   │   └── ui/              # Godot application UI
│   ├── src/
│   │   ├── core/            # VFS, clock, legacy text decoding
│   │   ├── compatibility/   # Compatibility profiles/database
│   │   ├── game_detector/   # Compatibility facade over plugin detection
│   │   ├── plugins/          # Trusted engine contracts, inspection, registry
│   │   ├── rm2k/
│   │   │   ├── parser/      # LCF reader + LDB/LMU/LSD parser
│   │   │   ├── database/    # Serializable RM2K/2003 models
│   │   │   ├── interpreter/ # Event interpreter (first slice done)
│   │   │   └── rendering/   # Future faithful renderer
│   │   ├── rgss/            # Future XP/VX/VX Ace runtime
│   │   ├── mz/              # MZ data as values, command table, branch
│   │   │                   #   decision, and an event list index
│   │   │                   #   (no JS run)
│   │   └── mv/              # Future MV runtime
│   ├── platform/godot/      # Future explicit Godot adapter boundary
│   ├── enhancement/         # Future optional Enhanced Mode features
│   ├── plugins/             # Optional integration/plugin surfaces
│   └── tests/               # Core, fixtures, integration, rendering
├── scripts/                 # Validation/development automation
├── docs/                    # Architecture, roadmap, compatibility/security docs
└── tools/                   # Pinned Godot editor binaries (runtime stays at root)
```

## Runtime Abstractions

The RPG Maker runtime core must be independent of Godot. All platform-specific
code flows through clear interfaces:

```
┌─────────────────────────────────────────────────┐
│              RPG Maker Runtime Core              │
│  (platform-independent, no Godot dependencies)   │
├─────────────────────────────────────────────────┤
│  IRenderer  │  IAudioBackend  │  IInputBackend  │
│  IFileSystem│  IClock         │  INetworkBackend│
└──────────┬──────────────────────────────────────┘
           │ implements
           ▼
┌─────────────────────────────────────────────────┐
│           Godot Platform Adapter                 │
│  (Godot-specific implementations of interfaces)  │
└─────────────────────────────────────────────────┘
```

## Game Detection Flow

```
User selects a game directory or ZIP archive
        │
        ▼
  SafeGameInspector
        │ bounded, read-only snapshot
        ▼
  EngineDetectionRegistry
        │ ranked plugin candidates
        ▼
  GameDetector compatibility facade
        │ DetectionResult + full report
        ▼
  EngineRuntimeSelector
        │ exact plugin/capability/platform checks
        ▼
  EnginePluginRegistry -> IEnginePlugin -> IEngineRuntime
```

Detection and runtime selection are implemented as trusted, compiled in-process
plugins. Imported EXE, DLL, Ruby, JavaScript, and native plugin files are data
only; they are never executed during inspection. See
[ENGINE_DETECTION.md](ENGINE_DETECTION.md) and
[ENGINE_PLUGINS.md](ENGINE_PLUGINS.md).

## MZ event reading, and the index that walks a list

An MZ game is read as data at three levels, and none of them runs anything:

```
MapNNN.json / Actors.json / System.json
        │  MzJson      strict JSON decoder, no execution, no coercion
        ▼
MzDataFile             a value tree: objects, arrays, text, numbers, flags
        │  MzCommandTable  the 114 commands, named from the engine's own
        │                  Game_Interpreter methods in a real 1.9.1 game
        ▼
MzBranchEvaluator      decides a 111 from the facts a caller has; a branch or an
        │                operand that would need eval is refused and named
        ▼
MzInterpreter          an index into one event list, moved the way the engine
                       moves it: branches, else, loops, break, repeat above,
                       labels, jumps, a wait that holds the index, and a step
                       limit equal to the engine's checkFreeze
        ▼
MzEventRunner          a run over a list and every list it calls. A 117 makes a
                       child the way the engine's setupChild does, and the caller
                       waits for it, so the called list runs to its end first.
                       Every list in a run shares one set of facts, because the
                       engine's $gameVariables is one object. A common event the
                       repository has no list for is named and refused, not
                       stepped over.
        ▼
MzParty                what a 126 changes. The count is clamped to maxItems,
                       which is the engine's own ninety-nine and not the number
                       the event asked for; a count that lands on zero is
                       deleted rather than stored; and an id with no item behind
                       it changes nothing and says which id it was.
MzScreen                what a 231, 232 and 235 change. Every value is a
                       pair, because a picture is nearly always between two
                       places: where it is and where it is going, and the
                       frames left to get there. Three rules a first reading
                       gets wrong: a shown picture is a NEW object and the old
                       one is gone with it; a picture id is routed through
                       realPictureId, so a battle picture lives a
                       picturesUpperLimit above the map one — this game sets
                       110, not the engine's fallback of 100; and a move sets
                       a TARGET, so a move of zero frames changes nothing at
                       all. PassFrame lands the last frame exactly on the
                       target and does not walk the straight line in between:
                       the easing is stored, not applied, and that is said
                       rather than faked.
```

`MzCommands` is where the commands that change numbers live (121 and 122, 126,
231, 232 and 235, and 230, which is a wait), and `MzControlFlow` is where the commands whose whole
effect is the index live. A command the engine has no method for — 0, 401, 412,
655 and 657, every one of which this game stores on purpose — is stepped over
rather than refused, because the engine steps over it too.

**A run ends four ways, and all four are said out loud.** `Finished` when the
list and everything it called ran out. `Waiting` when a 230 held the index, with
the frame count, because the engine counts frames down one per frame and a
reader that stepped over the wait would run the rest of a list early. `Frozen`
when a run passed the engine's own freeze, which counts the whole run and not
one list. `Refused` when it met something it will not guess at — a common event
the repository has no list for, or a branch whose operand would need `eval`.
A caller that read any of these as "the list ended" would be wrong about the
other three, so each carries what it was.

**The party is part of the facts, not a second owner of them.** `MzParty`
reads and writes `MzBranchFacts.Items`, because the engine has one
`$gameParty` and a reader that kept a count beside the facts would let a
branch asking whether the party has a potion disagree with the event that gave
it one. The members of the party, their equipment and the weapons and armors
containers are **not** modelled; a caller asking about a member gets an empty
one rather than a guess.

**Nothing here is a JavaScript runtime.** A 355 or 655 line is held as the text
its author wrote, a 357 plugin call is not made, and a branch of kind 12 or an
operand of kind 4 returns `ScriptNotRun` with the author's text kept. The engine
evaluates those with `eval`; this repository does not evaluate a game's code.

### The player, and the wait that is a condition

`MzPlayer` holds where the player is and, separately, whether a transfer is
**on its way**. The split is the engine's own: `reserveTransfer` records a
destination and changes nothing a player can see, `performTransfer` is what
applies it, and only then does `isTransferring()` answer false. **A reader
that applied a transfer while reading the command would move the player before
the commands after it had run.**

`MzWaitMode` is the third kind of waiting, next to a 230's frame count and a
232's picture movement. `command201` returns **true** — it does not hold the
index — and sets `setWaitMode("transfer")`; `updateWaitMode` then asks
`$gamePlayer.isTransferring()` every frame. **A condition has no length**, so
`PassFrame` takes a predicate as well as counting:

```
interpreter.PassFrame(_ => player.IsTransferring);
```

Only `Transfer` is modelled. The engine's other modes — `message`, `scroll`,
`route`, `until` — need a scrolling map, a moving character or a plugin
callback, and this repository runs none of those.

Two refusals, both the engine's own. A transfer in a battle or with a message
on the screen returns false and transfers nobody, which is **neither a wait nor
a finish**: the index stays and the transfer happens in the frame in which the
message closes, so `MzStep.Refused` says that rather than borrowing one of the
other two answers. And a map this reader has not read leaves the player where
they were, with the reservation still standing — the map may be read later, and
throwing the request away would lose the game's own intent.


### A character, and a route it walks

`MzCharacter` keeps **two positions**, because MZ does: `X`/`Y` is the tile
every rule asks about, and `RealX`/`RealY` is where the character is drawn,
part-way across. A successful step sets the tile first and then puts the
drawing position **one tile behind** —
`_realX = xWithDirection(_x, reverseDir(d))` — which is what makes a walk
look like a walk. A reader with one coordinate snaps, and a snapped
character teleports once per step.

Directions are **2, 4, 6, 8** for down, left, right, up. Not `0..3`, and not
the RM2K order used elsewhere in this repository. `ReverseDir` is
`10 - d`, so the pairs are 2↔8 and 4↔6.

`canPass` refuses in the engine's order, and **the order matters**: off the
map first, then `isThrough()`, then the map, then other characters. A
through character therefore walks over a wall **but not off the edge**.
`isMapPassable` asks twice — the tile being left *and* the tile being
entered looking back.

`MzMoveRoute` is the queue. `Force` memorises, takes the route and starts at
zero; `Step` hands out **one** entry, and only when the character has
arrived, so five steps into open floor is five frames. A refused step still
advances the index — otherwise a character facing a wall would hold that
entry for ever.

`IMzMapPassable` is deliberately two questions. The engine asks more, and a
reader with no renderer cannot answer `bushDepth`, `terrainTag` or
`regionId` honestly.


### A line of text, read as data

`MzMessage.Read` is `Window_Base`'s two passes and nothing more. The first
turns every backslash into the escape character, **puts one backslash back
for every two**, and fills in the variable, actor, party and currency codes —
the variable one in a loop, because a line can name the same variable twice.
The second treats **every character below 0x20 as a control character** and
never lets it reach the output.

A `Line` keeps the words, the number of times it waits, the number of
choices, whether it ends early, **and a list of everything it asked for that
this repository cannot draw** — colours, icons, positions, font sizes. The
waiting is not a rendering detail: a 402 follows a 401, and a line with three
`\|` needs three decisions.

**`MzCommandSet.HasMethod` answers a different question from "what did the
game write".** The engine asks `typeof this["command401"] === "function"`,
and the answer is no — a 401 is read by position inside a 101's block. A
reader is asked what the game wrote, and the answer is a line of text. **Two
questions, two answers**, and `NoMethodCodes` names the nine that have no
method so the difference can be stated rather than assumed.


## Compatibility Database

The compatibility database is extensible and data-driven:

```json
{
  "id": "profile.identifier",
  "sha256": "game_or_plugin_hash",
  "engine": "RPGMaker2003",
  "type": "game_profile",
  "compatibility": "full",
  "flags": ["PreserveLegacyPictureTiming", "LegacyTextEncoding"],
  "notes": "Known quirks and workarounds"
}
```

Game-specific behavior uses centralized flags, not scattered conditionals.

## Development Phases

See [ROADMAP.md](ROADMAP.md) for the complete phase breakdown.

### Current Phase: Phase 2 — RM2000/2003 Parser

- real bounded LCF container/BER parsing exists;
- initial LDB/LMU/LSD decoding exists;
- synthetic and provenance-pinned real parser regression fixtures exist;
- registry-driven engine plugin detection and safe runtime-selection boundaries exist;
- Built-in detection covers RM95, Dante98, RM2K, RM2K3, XP, VX, VX Ace, MV, MZ, WOLF, and Unite. Library scans use a cheap direct-entry preflight, search nested collection folders to a bounded depth of 4, cap visited directories at 4096, and skip common bulk asset/runtime folders. MV/MZ runtime versions are extracted from bounded package/runtime metadata when available; missing versions remain explicitly unknown.
- RM2K/RM2K3 have a parser-backed bootstrap runtime that loads validated data and advances the shared deterministic clock;
- LMT is fully parsed; LDB actors/switches/variables plus scalar skills/items/states/classes/enemies/terrains/attributes/troops/animations/chipsets/battle_commands metadata decode into typed models with verified liblcf field IDs and per-entry unknown-field retention;
- remaining nested LDB content and chipset passability decoding are next; LMU event/page metadata is decoded into the bounded native scheduler path, while K-015's accepted scalar/battle-command slice is complete.

### Language boundary

The tested implementation is pure C#/.NET under the Godot .NET editor. Migration and plugin-wiring validation passed `dotnet build` and the headless C# regression suite at `171/171`. Performance-critical components may later move behind GDExtension/native interfaces without forcing the whole application into one language.

## Security Model

Imported games are treated as untrusted:

- Virtual filesystem sandbox
- No arbitrary process execution
- No system directory access
- Network access configurable per-game
- Clipboard access configurable per-game
- Plugin loading requires explicit compatibility policy

## Legal Considerations

- No proprietary RPG Maker code included
- No RTP assets bundled without redistribution rights
- Independent implementation of behavior
- All third-party components documented in THIRD_PARTY_LICENSES.md

## Error Philosophy

Errors must be actionable:

**Bad:** `Failed to load game`

**Good:**
```
Unable to initialize RPG Maker VX Ace runtime.

Reason:
RGSS script requested unsupported Win32 API function.

Library:
user32.dll

Function:
GetKeyboardLayout

Script:
InputExtension

Compatibility report saved.
```

### A 101, and the commands it swallows

`command101` is the first command in this reader that moves the index over
other commands: `while (this.nextEventCode() === 401) { this._index++;
$gameMessage.add(…); }`, then one `switch` that takes the 102, 103 or 104
directly after the last line, then `setWaitMode("message")` **outside** the
switch — so a dialogue with no choice holds its page all the same.

`MzDialogue.Read` returns the block **and how many commands it ate**, because
the index moves by that and not by one: the 101 steps the index once per line
and once for the 102, and `executeCommand`'s own `this._index++` steps it once
more. **A 101 that is the last thing in a list therefore leaves the index one
past the end**, and no other command here can, because every other one moves
it by one.

`MzChoice` is a separate type from `MzPrompt`, and that is the point: a 102
carries a list of options, a 103 a digit count and a 104 an item id, and
reading the last two through the first would count a `4` as four options.
**This game has neither command in nineteen maps**, so everything about them
comes from the engine and not from data.

`MzInterpreter.Run` checks `IsRunning` **before** its loop, so a list that was
cut off in the middle of a command is said rather than silently left at
`Stepped` — which is the answer for "I have not run yet".
