# RM2000/2003 movement coverage

## Evidence and boundary

Measured with the repository's `Rm2kParser.ParseMap`, reading LMU files only.
No imported executable, script, plugin or game runtime was executed. The corpus
contains 806 owned collection maps and one pinned EasyRPG RM2000 map. This is
structural/data evidence, not proof of full gameplay or native visual parity.

Traversal excluded linked descendants/files and enforced directory, depth, map,
per-file and aggregate-byte budgets. The first post-fix probe wrote complete
results but aborted during native shutdown; a later probe with periodic managed
cleanup completed normally and is the accepted measurement. Temporary probes
are removed after collection. Private game paths are not published here.

## Measured pages

| Movement mode | Pages | Current production boundary |
|---|---:|---|
| 0 stationary | 25,172 | No autonomous route execution |
| 1 random | 1,787 | Explicitly unsupported |
| 2 vertical cycle | 15 | Explicitly unsupported |
| 3 horizontal cycle | 19 | Explicitly unsupported |
| 4 toward player | 272 | Explicitly unsupported |
| 5 away from player | 3 | Explicitly unsupported |
| 6 custom | 224 | Bounded supported command execution |

Total: **807 maps, 27,492 pages, 27,206 empty routes and 286 nonempty routes**.
Before correction, all 286 nonempty routes failed decoding. After correction,
all decoded, with no map or route parse errors and no skipped files.

## Root cause and reference

Pinned liblcf revision `6854310c`, `src/lmu_movecommand.cpp`:
`RawStruct<vector<MoveCommand>>::ReadLcf` consumes commands up to the payload
byte boundary. There is **no vector count prefix**. Ordinary commands contain
only an opcode. Switch32/33 carries A; graphic34 carries a length-prefixed string
and A; sound35 carries a string and A/B/C. `src/generated/lmu_moveroute.cpp`
uses a `SizeField` for 0x0B and a typed payload at 0x0C: 0x0B is byte-size metadata,
not a command count.

The old decoder and its hand-built fixtures agreed on an invented format.
Fixture migration corrects wire bytes and impossible expectations (for example,
a movement command cannot contain a string or three integer parameters).
Command-count, byte, string and truncation protection remain required.

## Route opcode distribution

Across all page routes: **3,356 opcode instances**.
In custom-mode routes: **2,733 opcode instances**.

All-route distribution, preserving IDs:

`0(305), 1(1022), 2(303), 3(987), 6(1), 7(3), 9(46), 12(78), 13(77), 14(78), 15(2), 23(250), 24(3), 25(3), 26(1), 28(93), 29(46), 30(54), 36(4)`

Custom-route distribution:

`0(212), 1(815), 2(212), 3(783), 6(1), 7(3), 9(38), 12(78), 13(77), 14(78), 15(2), 23(250), 24(3), 25(3), 26(1), 28(85), 29(38), 30(54)`

The production support baseline was derived from the actual
`UpdateEventPageRoute` branches and the named constants they reference, not
transcribed into an independent support table. Before this extension, unsupported
custom commands were:

`23(250), 9(38), 7(3), 24(3), 25(3), 6(1), 26(1)`

## Selected bounded extension

Wait23 is the highest-frequency unsupported custom-route command in this
corpus. Pinned EasyRPG Player `0de2a9ab`, `Game_Character::UpdateMoveRoute`, sets
`MaxStopCountForWait(frequency)` and resets stop count. The wait threshold is
20 plus the turn-frequency threshold, not an interpreter's timed Wait command.

The extension is tested through native file-authored routes at frequencies8/7,
with exact wait boundaries, the following real movement step and charset pixels.
The movement-mode backlog and other unsupported route commands remain open.

## Verification status

Native-format literal regression observed RED before reader correction.
Migrated decoder14/14, native runtime/pixel44/44, legacy routes7/7 and parser36/36
pass in focused runs. An ordinary-command string-field mutation was detected.
Reference audit is resolved: optional/advisory size, sparse payloads and int32
writer bit patterns verified; explicit flags survive absent vectors. A wrong
wait-duration mutation was also detected and restored. Final canonical
proc_df731ce4d9c1 exited0 with **2872/2872** (2852 Core+20 Smoke), and the
final validator pass marker. Precommit reviewdeleg_45231a32 found no blocker.
Build has142 existing warnings with no new normalized diagnostic. Shutdown
still leaks3 CanvasItem RIDs/6 ObjectDB instances. This is a completed bounded
development card, not a complete engine or release. The next measured custom
route gap is toward-player9(38); supported production branches remain cardinal
0..3, face/turn12..18, Wait23 and speed/frequency28..31 only.
