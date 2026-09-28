# Synthetic WOLF plain-data fixture contract

These tests do not contain a commercial WOLF game. `TestWolfRuntime` creates a
small temporary project using the internal `urpg-wolf-plain-json` version 1
conformance envelope:

- `Data/Game.dat` — project metadata and title;
- `Data/BasicData/*.db` — schema-free system/user/variable records;
- `Data/MapData/*.mps` — bounded map tiles and event command data.

The envelope is deliberately explicit and unencrypted. It is a test/runtime
foundation, not a claim that arbitrary proprietary WOLF files are JSON. Native
format adapters require independently sourced documentation and legal,
reproducible fixtures. Protected/encrypted markers must be rejected and are
never decrypted by the tests or runtime.

## Where a real game goes

The user was asked on 2026-09-28 for a real WOLF game and did not have one
placeable on this machine yet. **Drop a whole game directory here** as
`real/` — the common event file, the database file, the game file and the map
files, unencrypted — and the reader gets its first non-synthetic fixture:

```
wolf/real/
  Data/Game.dat
  Data/BasicData/Database.dat
  Data/BasicData/CommonEvent.dat
  Data/MapData/Map0001.mps
```

**Nothing in this project executes a WOLF file, and nothing decrypts one.** A
file that arrives protected is refused with a diagnostic, and the tests record
that refusal as the correct outcome.
