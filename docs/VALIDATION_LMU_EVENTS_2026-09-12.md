# LMU event import correction — evidence and pending validation

Date: 2026-09-12. Development branch: `docs/refresh-2026-09-09`, PR #1.
Parent: `7f5b805b792ff4f16a7f4cbe3f71127b1311fd7e`.

## Why this took priority over automatic movement

In the reviewed source, `ParseMap` called `ParseStructArray(pageBytes, false)`. That mode counts structures but does not collect their objects/fields. The subsequent page loop was therefore empty even for an LMU that declared pages. This blocked actual map events before the scheduler or route runner could use them.

The unreachable page-condition code then expected a `fields` property on a raw LCF chunk, although chunks contain byte `data`. Merely changing the collect flag would have exposed that second fault.

The fix delegates page decoding to a focused partial implementation. The large parser retains its other LDB/LMT/LSD logic unchanged. No replacement executable or new dependency is introduced.

## Format decisions and primary references

Format reference revision: EasyRPG/liblcf `6854310c3432e553fd4ae672ce861899c80c3bd0`.
These sources were consulted for format semantics; they are not runtime dependencies.

- [EventPage fields and defaults](https://github.com/EasyRPG/liblcf/blob/6854310c3432e553fd4ae672ce861899c80c3bd0/src/generated/lcf/rpg/eventpage.h)
- [LMU chunk IDs](https://github.com/EasyRPG/liblcf/blob/6854310c3432e553fd4ae672ce861899c80c3bd0/src/generated/lcf/lmu/chunks.h)
- [Generated MoveRoute fields](https://github.com/EasyRPG/liblcf/blob/6854310c3432e553fd4ae672ce861899c80c3bd0/src/generated/lmu_moveroute.cpp)
- [SizeField versus CountField](https://github.com/EasyRPG/liblcf/blob/6854310c3432e553fd4ae672ce861899c80c3bd0/src/reader_struct.h)
- [Event-command operands and vector encoding](https://github.com/EasyRPG/liblcf/blob/6854310c3432e553fd4ae672ce861899c80c3bd0/src/ldb_eventcommand.cpp)
- [LCF integer reader](https://github.com/EasyRPG/liblcf/blob/6854310c3432e553fd4ae672ce861899c80c3bd0/src/reader_lcf.cpp)

Notable distinctions:

- EventPage `0x02` is a nested condition structure; `0x29` is a nested movement route; `0x34` is the event-command vector.
- Page `0x33` and route `0x0B` are serialized-byte size hints, not command counts. liblcf reads them as advisory. URPG never allocates or iterates from these hints; the real payload and independent command cap govern decoding.
- Condition values and command operands are signed 32-bit compressed integers. Lengths/counts remain nonnegative and bounded.
- Raw LMU direction values (up/right/down/left = 0/1/2/3) are retained as metadata, not confused with runtime numpad directions.
- Unknown patch fields are retained as raw data. Guessed aliases that could hide malformed canonical fields were removed.

## Tests added/changed, NOT executed here

`TestRm2kLmuEventPages`: **19 methods**. Most construct a synthetic `.lmu`, write it to a temporary file, and call the actual `Rm2kParser.ParseMap` entry point. Coverage includes retained pages, IDs/order, nested switches, signed thresholds/operands, empty scalar/default behavior, graphics/movement metadata, parameterized routes, advisory sizes, malformed/trailing/duplicate data, unknown fields and the event-command cap boundary.

`TestRm2kMoveRouteDecoder`: **11 methods**, previously 7. The parameterized test now exercises the containing serialized route, not only the raw instruction decoder. Tests distinguish an explicit caller-supplied command-count assertion from the on-disk byte-size hint, and retain malformed-input/count bounds.

The existing reflection-based C# runner discovers these TestBase-derived suites. No proprietary game or RTP assets are included in the fixtures.

## Checks actually performed

- Materialized original `rm2k_parser.cs` matched its inspected Git blob SHA `b5162ac17c798d8f34a1ca518bc63c25c288cc49` exactly.
- The modified main parser has one intended event-page hunk; the other parser sections were not replaced or regenerated.
- Materialized original `Rm2kMoveRouteDecoder.cs` matched Git blob `7524d92be71708c7e817586c887b9d8e6c20fc43`.
- Edited C# files passed a lexical balanced-delimiter check. This is NOT a compiler, type checker, or behavioral test.
- The code/fixtures were reviewed against the primary format contracts above.

**No C# test pass is claimed.** The local environment lacks .NET and Godot, and attempts to retrieve the toolchain failed due network/DNS access. No fresh canonical GitHub validator result is used as acceptance evidence for this pass.

## Required acceptance command

```sh
GODOT_BIN=/absolute/path/to/Godot-mono ./scripts/validate.sh
```

Inspect the retained logs. Specifically require the LMU and route suites to run and pass; a successful import process alone is insufficient. Follow with an actual small project exercising page selection, dialogue/conditions, interaction and map transfers.

Earlier dated JavaScript/driver reports and the historical main 296/296 result do not validate this commit.

## Deliberately unfinished

Automatic movement-route execution from active LMU pages, faithful timing, graphical sprite synchronization, complete command coverage, sound, menus, saves and battle compatibility are not completed by this parser correction. Parsing graphic/route metadata does not imply that the renderer or scheduler consumes all of it yet.

PR #1 must remain unmerged until the complete branch has fresh build/test evidence. No engine capability was promoted in this pass.
