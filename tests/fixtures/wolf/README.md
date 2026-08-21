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
