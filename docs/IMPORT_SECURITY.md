# Import and Runtime Security

Every imported game is untrusted.

## Implemented Scanner Controls

- Detection reads data only; it never starts executables, libraries, Ruby, or JavaScript.
- Library recursion is limited to two levels.
- Hidden directories and filesystem links/junctions are skipped.
- Recognized metadata reads are capped at 1 MiB.
- Original game files are not modified.

These controls protect the current detector. They are not yet a complete runtime sandbox.

## Required Before Archive Import

- Canonical destination-root check for every entry.
- Reject absolute, drive-qualified, UNC, traversal, NUL, control-character, and reserved-device paths.
- Reject links and path collisions, including case and Unicode-normalization collisions.
- Bound entry count, compressed bytes, expanded bytes, per-file size, nesting, and expansion ratio.
- Extract into a new app-owned staging directory, validate, then register atomically.
- Never overwrite an existing game, app data, or original source.

## Required Before Script Execution

- Embedded VM only; no host process execution fallback.
- Virtual filesystem mounts with read-only game data and separate writable save/cache roots.
- Deny host filesystem, dynamic library loading, network, clipboard, shell, and process APIs by default.
- Explicit per-game capability policy for network/external links.
- CPU/instruction, memory, recursion, callback, and output limits.
- Watchdog, cancellation, crash reporting, and deterministic clock options.

## Parser Rules

- Validate lengths before allocation and arithmetic before offset use.
- Cap maps, events, commands, strings, image dimensions, audio duration, and decompressed payloads.
- Preserve unknown fields where possible; reject structurally unsafe data.
- Return actionable errors with file, offset/field, engine, and violated limit.
- Fuzz every binary/archive parser and keep regression inputs stripped of copyrighted content.

## Saves and Patches

Saves, caches, settings, compatibility fixes, and translations belong under app-owned storage. Bind them to a stable game/version hash. Apply patches through VFS overlays so uninstall and rollback are deterministic.
