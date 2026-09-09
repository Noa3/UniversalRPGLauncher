# Import and Runtime Security

> **Last reviewed:** 2026-09-09

Every imported game is untrusted.

Security requirements apply independently from compatibility goals. Supporting more engines must not mean executing arbitrary imported code with host permissions.

## Implemented Inspection Controls

Current `GameInspectionLimits` defaults:

- maximum directory depth: **4**
- maximum entries: **4096**
- maximum metadata read per file: **1 MiB**
- maximum archive uncompressed inspection budget: **64 MiB**
- maximum archive entry metadata read: **1 MiB**
- maximum bounded prefix read: **4096 bytes**

Current scanner behavior:

- imported EXE/DLL/SO/Ruby/JavaScript/shell/native plugin content is not executed during detection
- reparse points/symlinks/junctions are skipped
- ZIP inspection is read-only
- absolute/traversal-style unsafe archive paths are rejected
- original game files are not modified
- exceeding the entry budget on otherwise valid input produces a **partial/advisory** snapshot rather than automatically marking the game malformed

These controls protect inspection. They are not a complete runtime sandbox.

## Runtime Capability Principle

A future engine runtime may only expose host functionality through explicit URPG capabilities.

Default posture for imported script/native code:

| Host capability | Default |
|---|---|
| read mounted game data | allow through bounded VFS |
| write save/cache/temp roots | allow through bounded VFS |
| arbitrary host filesystem | deny |
| process/shell execution | deny |
| native library loading | deny |
| network | deny unless explicitly permitted |
| clipboard | deny unless explicitly permitted |
| external URL/app launch | deny unless explicitly permitted |

## Required Before Ruby/JavaScript Execution

- embedded VM; no host-process fallback
- VFS-backed filesystem
- read-only game mount plus separate writable save/cache/temp mounts
- memory limits
- recursion/stack limits
- instruction/time/watchdog limits where practical
- bounded callback/output queues
- cancellation and fault containment
- explicit network/clipboard/process/native policy
- deterministic/virtual clock integration where engine semantics permit it
- actionable script error reporting

Do not advertise RGSS or MV/MZ Runtime capability before these boundaries exist.

## Required Before Native Plugin/DLL Execution

Native binary inspection must remain data-only first.

Before any controlled execution:

- verify architecture/ABI
- parse imports/exports without loading the library
- identify known hash/signature
- prefer high-level compatible replacement where possible
- define allowed Win32/host API surface
- isolate crashes
- prevent arbitrary filesystem/process/network access
- require explicit compatibility policy

Never load an arbitrary DLL simply because a game directory contains it.

## Archive Import / Staging

Current ZIP inspection does not imply safe extraction.

Before writable archive staging/import:

- canonical destination-root check for every entry
- reject absolute, drive-qualified, UNC, traversal, NUL, control-character and reserved-device paths
- reject symlinks/reparse metadata
- reject case/Unicode-normalization collisions
- bound entry count, compressed bytes, expanded bytes, per-file size, nesting and expansion ratio
- extract into a new app-owned staging directory
- validate before registration
- register atomically
- never overwrite original source or unrelated app/user data

## Parser Rules

Every parser must:

- validate lengths before allocation
- validate integer arithmetic before offset calculations
- cap collection counts, map dimensions, strings and payload sizes
- cap recursion/nesting
- preserve unknown fields where safe
- reject structurally unsafe data
- return actionable engine/file/offset/field diagnostics
- gain regression tests for discovered malformed-input bugs

Binary parser fuzzing should use legal/synthetic minimized inputs rather than copyrighted game data.

## WOLF Protected Data

The current WOLF runtime is intentionally limited to understood unencrypted/plain-data inputs.

Do not add protection/encryption bypass as a shortcut to compatibility. Protected formats require a clear legal/technical support policy and authorized test material.

## Saves, Patches and Overrides

Runtime-owned mutable data belongs under app-controlled storage:

- saves
- cache
- settings
- compatibility profiles
- translations
- asset overrides

Prefer VFS overlays and stable game/version identity so rollback/uninstall is deterministic.

Original game files should remain read-only unless the user explicitly invokes a separate export/patch workflow.

## Logging and Reports

Compatibility reports should avoid exposing:

- unrelated absolute user paths
- secrets/tokens
- arbitrary file contents
- personal data unrelated to engine diagnosis

Prefer relative game paths, hashes, engine IDs, bounded diagnostics and explicitly requested metadata.
