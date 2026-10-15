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
| 1 random | 1,787 | Implemented (`d474a91`) |
| 2 vertical cycle | 15 | Implemented (`ec1ecdb`) |
| 3 horizontal cycle | 19 | Implemented (`ec1ecdb`) |
| 4 toward player | 272 | Implemented (`ec1ecdb`) |
| 5 away from player | 3 | Implemented (`ec1ecdb`) |
| 6 custom | 224 | Bounded supported command execution |

All six types now walk the engine's own rules; only a mode outside the format's
range still reports itself as unimplemented. The rules were read from EasyRPG
Player's `game_event.cpp` (`MoveTypeRandom`, `MoveTypeCycle` and its two
wrappers, `MoveTypeTowardsOrAwayPlayer`) and `game_character.cpp`
(`ReverseDir`, `GetDirectionToCharacter`, `GetDirectionAwayCharacter`,
`GetMaxStopCountForStep`). The engine's direction order comes from the Player's
own enum -- `Up = 0, Right, Down, Left, UpRight, ...` -- so the vertical cycle
starts down and the horizontal one right. The sight margin of the
player-seeking types is `TILE_SIZE * 2`, that is 32 pixels, and the event's
screen position is computed from the camera's own `PositionX`/`OffsetPixelsX`.

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

The movement-mode backlog is closed: all six types walk their rules, with
focused evidence and two caught mutations recorded in the KANBAN card. What
remains unsupported in this corpus is small and named exactly: route commands
**24 (begin jump, 3 instances), 25 (end jump, 3) and 26 (lock facing, 1)** --
seven command instances out of 2,733 in custom routes. The structural RM2000
gaps that are *not* command opcodes remain open as well: loop maps, dynamic
character collision, full forced-route parity, transparency, and the
route/interpreter lifecycle.

**Update (2026-10-15).** Two of those three are closed and the third is pinned:

* **Loop maps are done.** The LMU's `scroll_type` is parsed, and the camera, the
  passability lookups and every step (player and events) wrap through
  `RoundX`/`RoundY` -- the four looping maps of the collection now wrap instead of
  walling the player in (`K-PROD-RM2K-LOOP-MAPS`).
* **26 (lock facing) is implemented**, together with its twin 27 (unlock facing),
  which shares the page's own facing-lock flag the way the reference does.
* **And an unhandled route command no longer stops the route.** The reference's
  move-route switch ends in `default: break;` and then advances the index, so a
  command it does not implement is walked past and the rest of the route runs; the
  runtime used to cancel the whole route at that command, which stopped an event
  the engine keeps moving. The gap is reported by name, once per event and command.
* **What is still open: 24 (begin jump) and 25 (end jump)** -- a movement mode of
  their own (`BeginMoveRouteJump` scans ahead between the two commands, applies the
  turns, accumulates `jdx`/`jdy` and jumps at the end), pinned in
  `K-PROD-RM2K-ROUTE-JUMP` with six measured instances -- and the relative/random
  movement forms **8, 10 and 11** as *route* commands, which the autonomous movement
  types implement but the route dispatch refuses.

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

## Toward-player command9 development follow-up

The next bounded extension executes custom-route9 on the existing non-looping,
static-terrain movement surface. Pinned Player `0de2a9ab`
`Game_Character::GetDirectionToCharacter` selects the larger absolute distance
axis; equal distances select vertical, and coincident coordinates select down.
The route attempts exactly that direction, without the autonomous approach
mode's alternate-axis fallback. Existing loop-map movement, dynamic character
collision and production forced-route pacing remain unimplemented boundaries.

The shared step path now selects attempted direction/automatic facing before
passability. A non-skippable refusal retains it and invalidates changed visible
pixels; a skippable refusal restores the previous direction and facing. Fixed
facing remains independent. Completed production commands reset the failure
streak without adopting the prototype runner's immediate repeat wrap/finish.

Native command9 was observed44/45 RED before implementation; failed-facing
45/46 RED and failure-count54/55 RED separately discriminated shared defects.
The corrected candidate passes58/58 native runtime/pixel tests (14 added),
route-state12/12, legacy routes7/7 and renderer19/19. Horizontal-tie mutation
fails57/58; suppressed failed-turn repaint fails56/58, including real pixels.
Both mutations are restored and rebuilt. There are no new build diagnostics.

Reference auditdeleg_80c16c3d confirms source semantics and the existing loop/
collision boundary; its original code snapshot predates the implementation.
Precommit reviewdeleg_b0bfbe31 found no blocker in the updated implementation.
Full canonicalproc_a0dd4b70fd42 exited0 with **2886/2886** (2866 Core+20
Smoke) and the final validator pass marker. Supported production commands are
now0..3,9,12..18,23,28..31. Build142 existing warnings and known shutdown leaks
remain unchanged. Next bounded card: diagonal4..7, measured custom6(1)/7(3).
This is not a completed release or a full engine-compatibility claim.
