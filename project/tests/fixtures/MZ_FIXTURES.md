# Real RPG Maker MZ fixture

`Stranded with You`, an MZ 1.9.1 game, given to this repository to read. Nothing
in it was executed. **The JavaScript is not here**: `js/rmmz_core.js` is 175 KB
and `js/rmmz_managers.js` is 83 KB of a game's own code, and this repository does
not run a game's code. Two files stand in for them by name, because a name is
what the detector reads and a runtime is not an input to a data reader.

## What is here

Eleven data files, the two files that name the engine, and a package file: 15
files, 238,133 bytes. `CommonEvents.json` was taken and dropped again — 4.5 MB of
one game's text, and a test that needs it is not one that needed it.

**What that means for 117, and it is a known gap rather than a bug.** A 117 names
a common event by the index it has in that file, and the one map read here calls
eight different ones. `MzEventRunner` runs a called list when it has one and
**names the index and refuses when it has not**, because the engine's own line
is `if (commonEvent)` — a missing one is stepped over — and a silent step-over
would run the rest of a game's list as if the call had never been there. To
close the gap the real file would have to come in whole, with a decision about
whose text that is.

| Fixture | Bytes | SHA-256 |
|---|---:|---|
| `mz/data/Actors.json` | 1745 | `c8b04e1b5c95403d77bca447b14e320e56b4d3c663606892b464e43efd42c7ac` |
| `mz/data/Animations.json` | 69868 | `9c3ba404a24ac64bfec06eda57304910e7f9fc4c9613b7311e7585282a4663d6` |
| `mz/data/Classes.json` | 25503 | `adcff5b1c56403a877476909ada42a2e379f5d0b0e916c5a6fb8ae5c3b9dc342` |
| `mz/data/Enemies.json` | 2273 | `3a9c9d857716c4aad0c8f392a0021a7c5535152c5b0c9ae02931c6db520dc8d7` |
| `mz/data/Items.json` | 75061 | `f7d1c49202323864f52083e3d9acf2ea37431dcc71bc2aad5d640c1bbce315dd` |
| `mz/data/Map001.json` | 3143 | `643061bad5203d76788a42c7e1232d6e906b8ce913fe424391abc14756b5bc73` |
| `mz/data/Map002.json` | 20903 | `4124f8d6b213fa5625a3f0ebab39a08c4c8108502f3eb76c8d330bcc190fba20` |
| `mz/data/MapInfos.json` | 1799 | `53c2d47813ea0b362c173b28a01cf72e99b8abd0625ae3b62e37537c44f5071b` |
| `mz/data/Skills.json` | 4709 | `4d0dbc5f96274e7853343b05e27ee69af09c83c523d60db92afded339b33ac79` |
| `mz/data/States.json` | 3719 | `ca40420aeb24819679a5afd9f03b3abc00a1799f186c4367bb78b41115eb3272` |
| `mz/data/System.json` | 28890 | `2f6aff37d558f8e994a6501cd73bbd646ffc9cfba11b784b58266b6912e49045` |
| `mz/game.rmmzproject` | 11 | `f52f065cf5c322d61d36a87085a6f333df72612b5884f9a9ca45aa3622f5a47d` |
| `mz/js/rmmz_core.js` | 89 | `43673ba7c589f03e1268827cf6f1e55ffe61550cab571882afd7186279051cdb` |
| `mz/js/rmmz_managers.js` | 125 | `4e6b73a471a2141bbaabaf634c9cbea07d158768f140c2c799bd50ec6d5bae54` |
| `mz/package.json` | 295 | `a7c9f0419221040796f81452cf6f0324844ac8aff1934f32bbe0bb9ebd77bf46` |

The two `js` files are **placeholders carrying the real file names**, and their
hashes are the hashes of those placeholders. They are in the table so that what
is in the repository can be checked; they are not the game's runtime.

## The three things the format has that a reader written from a description gets wrong

All three were found by reading these files, not by reading about them, and two of
them were wrong in a first draft of the test that is now written down here so they
are not wrong again.

**One. A database file's first entry is null.** `Actors.json` is
`[\nnull,\n{"id":1,...}]`. The editor numbers its actors from one so that zero can
mean "no actor", and the same holds for items, enemies, classes, skills, states
and animations. A reader that treats the first element as the first entry of the
game reads a null and calls it a database.

**Two. A command is a small number and is not packed.** This game's event
commands are `121`, `231`, `357`, `657` and nothing above a thousand anywhere in
the file. In the generation before this a command's number is its own value times
a thousand and a reader divides by a thousand to learn what a command is. **A
reader written for MV and pointed at this file would divide every command to
zero.**

**Three. A map's events are an array indexed by event, not padded to the field.**
`Map002.json` is seventeen by thirteen and its `events` array holds seven entries,
the first of them null. A first draft of this test claimed the array ran over the
whole field. It does not, and the null is the editor writing from the first event
number.

## What is claimed and what is not

Claimed: these files are read, and the values in them come back as they were
written, including the fields the reader has no name for. The reader refuses a
file that is not JSON and says why, and it bounds how deep a file may nest.

Not claimed: that a game's data is understood. `MzDataFile` returns values.
Nothing here knows what a command 231 does, what a `battlerName` is for, or how
a page's conditions are evaluated. There is no JavaScript runtime here, no
renderer, and no save path; an MZ game does not run.

## The command numbering, read out of the engine

The engine gives each command a method and the editor names the command after
the method. Both the number and the name are in the engine's own source of this
game, and `project/src/mz/MzCommandName.cs` was generated from it: **114 commands,
from 101 to 603**, and every name is the one the engine carries.

A first draft of `MzCommandTable.cs` was written from memory and **79 of its 178
names were wrong** — 129 was written as "Change Hp" and the engine calls it
"Change Party Member", 231 was "Move Event" and the engine calls it "Show
Picture". A table built from memory is a table of plausible numbers, and a
plausible command that a game does not use is invisible until the game uses it.

### The rule that is not a rule

"Which command does a piece of data belong to" invites a rule: the data number is
three hundred above its command. Measured against this game, that rule is right
**four times out of eight**:

| Data number | Owner, measured | `code - 300` says | What that is |
|---:|---:|---:|---|
| 401 | 101 Show Text | 101 | right |
| 405 | 105 Show Scrolling Text | 105 | right |
| 408 | 108 Comment | 108 | right |
| 655 | 355 Script | 355 | right |
| 412 | 111 Conditional Branch | 112 | **Loop** |
| 501 | 102 Show Choices | 201 | **Transfer Player** |
| 605 | 302 Shop Processing | 305 | not a command here |
| 657 | 355 Script | 357 | **Plugin Command** |

Two of the four mistakes point at a command that exists in this generation and
does something else entirely. A reader that used the rule would read a branch's
else as a loop, a choice as a teleport, and a shop's purchases as a number that
means nothing. **The owners are written down because none of them can be
calculated.**

### 411, 412 and 413 are two commands and one piece of data

These three sit at an indent of their own under a branch, which makes all three
look like the branch's options. The engine disagrees for two of them:

- **411 is a command**, and the engine calls it `Else`.
- **413 is a command**, and the engine calls it `Repeat Above`.
- **412 has no method at all.** It is the one a branch reads.

A reader that treated the family as data would refuse two real commands. One that
treated it as commands would run a branch's structure as an instruction. Both
fail silently, which is why the test checks all three separately.

### A name written once

An earlier shape of this was an enum whose members carried a name in a doc
comment and a second table beside it carrying the same names as strings, because
a C# identifier cannot be `Show Text`. **The two copies drifted, and three
mutations of a name were invisible to the suite** — the reader handed out the
string while the enum carried the prose, so changing either one alone changed
nothing a test could see. It is a `readonly record struct MzCommand(int Code,
string Name)` now: the number and the name are one value and there is nowhere for
a second copy to live.

## What a conditional branch means, and the two numberings inside it

The engine decides a branch in one method. `project/src/mz/MzBranchEvaluator.cs`
was written from it, and four things in it are worth writing down because three
of them were got wrong first.

### The third parameter only says whether the right side is a variable

```
if (params[2] === 0) { value2 = params[3]; } else { value2 = $gameVariables.value(params[3]); }
```

`params[2]` is a yes or no and `params[3]` is the operand. **This reader read
`params[2]` as the variable**, so this game's own branch
`[1, 77, 1, 78, 1]` asked about variable 1 where the game asked about variable
78, and refused a branch it could have answered. The harness for the test made
the same mistake in the other direction and asked for a number where it meant a
variable, so the two errors hid each other for one run.

### Gold has a numbering of its own

| Number | Gold means | A variable means |
|---:|---|---|
| 0 | at least | equal to |
| 1 | at most | at least |
| 2 | less than | at most |
| 3 | — | greater than |
| 4 | — | less than |
| 5 | — | not equal to |

The first three are the same words in a **different order**. Read through the
variable's numbering, a branch that gates a purchase on a hundred gold opens at
ninety and shuts at a hundred and ten — and the reader is right about the
arithmetic and wrong about the question.

### A timer has no number in the parameters

```
if ($gameTimer.isWorking()) {
    const sec = $gameTimer.frames() / 60;
    if (params[2] === 0) { result = sec >= params[1]; } else { result = sec <= params[1]; }
}
```

The second parameter is a threshold in seconds and the third is the way. There is
no timer number anywhere, because the branch asks the one timer the event owns.
This reader treated the second as a number and asked for a timer called five on a
branch about five seconds.

### One branch is deliberately not answered

Kind 12 asks whether a line of the author's own JavaScript is true, and the
engine writes `result = !!eval(params[1])` for it. **This repository does not
evaluate a game's JavaScript.** A branch of that kind is reported as
`ScriptNotRun`, the author's text is kept so a caller can see what was declined,
and the answer is neither true nor false — because saying either would be a claim
about code that was not run.

### What is not known is not off

A branch that asks about a switch nobody supplied returns `Unknown` and names
the switch, rather than coming to false. A reader that treated what it does not
know as off would skip a game's content with nothing to show for it, and that is
the one failure in a branch evaluator that would be invisible.

## Pictures: 231, 232 and 235

The one map carries nine picture commands — three show, four move, two erase —
on editor numbers 1, 86 and 87. They are the first commands in this game that
need something other than numbers to have an effect, so K-127 gives them a
place to go.

### The game sets a hundred and ten, not the hundred

```
Game_Screen.prototype.maxPictures = function() {
    if ("picturesUpperLimit" in $dataSystem.advanced) {
        return $dataSystem.advanced.picturesUpperLimit;
    } else {
        return 100;
    }
};
```

**This game's `System.json` sets `picturesUpperLimit` to `110`.** The hundred is
the fallback for a game that does not set it, and this one does. A reader that
used the hundred would tell a battle picture from a map one by the wrong
amount, because `realPictureId` adds `maxPictures()` to separate them:

```
Game_Screen.prototype.realPictureId = function(pictureId) {
    return $gameScene.isBattle() ? pictureId + this.maxPictures() : pictureId;
};
```

So picture 86 is at slot 86 on the map and at slot 196 in a battle. The two
never meet, which is the whole of the rule.

### A shown picture is a new picture

```
Game_Screen.prototype.showPicture = function(pictureId, name, origin, x, y,
                                            scaleX, scaleY, opacity, blendMode) {
    const realPictureId = this.realPictureId(pictureId);
    const picture = new Game_Picture();
    picture.show(name, origin, x, y, scaleX, scaleY, opacity, blendMode);
    this._pictures[realPictureId] = picture;
};
```

**A new object, and the old one is gone with it** — its tint, its rotation and
any movement. A reader that changed the existing picture in place would keep
what the engine has just thrown away, and a game that shows the same slot twice
would have the first picture still moving.

### The fourth parameter says where the place is read from

```
Game_Interpreter.prototype.picturePoint = function(params) {
    const point = new Point();
    if (params[3] === 0) {
        point.x = params[4];
        point.y = params[5];
    } else {
        point.x = $gameVariables.value(params[4]);
        point.y = $gameVariables.value(params[5]);
    }
    return point;
};
```

**The kind decides, not the value.** Zero means the fifth and sixth are the
numbers; anything else means they are variables to look up. A reader that read
them as numbers either way would place a variable-positioned picture at the
variable's own number — here at 40 rather than at the 640 it holds.

### A move sets a target, and a move of no frames does nothing

```
Game_Picture.prototype.updateMove = function() {
    if (this._duration > 0) {
        this._x = ...;
        this._duration--;
    }
};
```

`move` writes the targets and the duration and **does not touch the current
values**. So a move of zero frames changes nothing at all: the picture is still
where it was, the target is never reached, and `this.wait(params[10])` becomes
a wait of no frames, which is over at once even when the game asked for it.

Only a move that asks to wait holds the list up:

```
if (params[11]) { this.wait(params[10]); }
```

and there is no second line like it. This game asks on **two of its four**
moves and not on the other two, so a reader that waited on every move would
stall a page that the engine runs straight through.

### A boolean parameter is a value, and a first reader lost it

`params[11]` is a real JSON boolean in this game's data — `true` and `false`,
not `"1"` and `""`. `MzCommandEntry.From` handled a Number and took the text of
everything else, so **both booleans became the empty string** and all four moves
came back as "does not wait". It is fixed in the reader, and the test reads a
boolean out of an event list rather than building one by hand, so the loss
cannot come back unnoticed.

### What is not here

A picture is a name, a place and some numbers. Nothing in this repository
loads a texture, and the blend mode and the scale are kept as the numbers the
game wrote rather than resolved to a rendering. The easing is stored and not
applied: `PassFrame` lands the last frame exactly on the target, as the
engine's easing is built to do, and does not walk the straight line in between
— which is stated rather than faked. 233 (rotate), 234 (tint), 224 (fade) and
236 (weather) are the next picture commands and are not modelled.
