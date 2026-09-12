# Protected, Packed, and Encrypted Game Content

> Last reviewed: 2026-09-11

UniversalRPG distinguishes **engine-managed packaging/encryption required by the original game runtime** from **author protection, third-party DRM, and arbitrary access-control bypass**.

The goal is to run legally obtained games through compatible runtime behavior, not to provide a general-purpose extraction or decryption utility.

## Runtime Principle

Supported protected content is exposed through the same logical read-only content layer as ordinary files:

```text
logical game path
      |
      v
trusted content provider
      |
      +-- plain file
      +-- engine archive
      +-- engine-managed encrypted asset
      |
      v
bytes supplied to runtime in memory
```

UniversalRPG should not require a game to be permanently unpacked or rewritten before play.

Where practical:

- original files remain untouched;
- plaintext is not written to an extraction directory;
- archive/encryption details stay behind `IGameContentSource`;
- the renderer, audio system, script runtime, and game logic request normal logical paths;
- bounds and canonical-path checks still apply;
- external tools can use the same read-only SDK contracts.

## Public SDK Contracts

The Godot-free SDK now exposes:

- `IGameContentSource`
- `IProtectedContentProvider`
- `ProtectedContentRegistry`
- `ProtectedContentDescriptor`
- `ProtectedContentStatus`
- `IUniversalRpgLibrary.OpenProtectedContent(...)`

Providers are trusted host/application components. Imported games do **not** get to load arbitrary assemblies/native code and register their own decryptors.

Stable scheme IDs currently include:

- `rpg-maker-mv-assets`
- `rpg-maker-mz-assets`
- `rpg-maker-rgss1-archive`
- `rpg-maker-rgss2-archive`
- `rpg-maker-rgss3-archive`
- `wolf-protected-archive`

## Current Support Matrix

| Engine/content type | Detection | Transparent runtime read | Policy |
|---|---:|---:|---|
| RM2000/2003 normal LCF files | yes | yes/partial runtime | normal game data |
| XP `.rgssad` | yes | not enabled yet | provider architecture ready; legal/compatibility implementation review required |
| VX `.rgss2a` | yes | not enabled yet | same as above |
| VX Ace `.rgss3a` | yes | not enabled yet | same as above |
| MV `.rpgmvp/.rpgmvo/.rpgmvm` | yes | **implemented** | engine-managed deployment encryption, read-only/in-memory |
| MZ `.png_/.ogg_/.m4a_` encrypted deployment assets | yes | **implemented** | engine-managed deployment encryption, read-only/in-memory |
| WOLF protected `.wolf` content | yes | no built-in provider | detect/report; no author-protection bypass |
| third-party DRM/copy protection | limited | no generic support | fail closed |

This table describes the content layer only. For example, MV/MZ encrypted assets can now be read transparently, but the complete MV/MZ JavaScript gameplay runtime is still under development.

## RPG Maker MV / MZ

MV/MZ deployment can encrypt image/audio assets. The game itself stores the metadata/key needed by the original runtime in `data/System.json` and accesses encrypted files through the engine runtime.

UniversalRPG implements the same kind of runtime-facing behavior:

1. read bounded `System.json` metadata;
2. validate the 16-byte encryption key;
3. resolve the logical requested asset;
4. prefer a normal plaintext asset when present;
5. otherwise resolve the engine's encrypted extension;
6. validate the RPG Maker encrypted-asset header;
7. decrypt the required payload bytes in memory;
8. return the logical file bytes to the caller;
9. never create a plaintext extraction copy.

Implemented encrypted extension mapping includes:

- `.png` -> `.rpgmvp` / `.png_`
- `.ogg` -> `.rpgmvo` / `.ogg_`
- `.m4a` -> `.rpgmvm` / `.m4a_`

Path traversal, absolute paths, malformed keys, malformed encrypted headers, and oversized reads fail closed.

## RPG Maker XP / VX / VX Ace Archives

RGSS games may place game data inside:

- XP: `.rgssad`
- VX: `.rgss2a`
- VX Ace: `.rgss3a`

UniversalRPG already detects these archive families and reports them through `GameAnalysis.ProtectedContent`.

The architecture intentionally supports a future read-only archive mount, but the repository does **not** currently ship a built-in archive provider. Do not work around this by unpacking the game into a permanent directory or calling an external decryptor process.

If/when an RGSS archive provider is approved, it should:

- implement `IProtectedContentProvider`;
- expose only logical game files through `IGameContentSource`;
- be bounded against corrupt archives;
- normalize and validate archive paths;
- never overwrite the source archive;
- avoid writing plaintext extraction trees;
- support the exact format variants separately;
- gain synthetic/legal fixture coverage;
- document provenance and compatibility evidence.

## WOLF RPG Editor

WOLF is treated separately from RPG Maker.

The current runtime targets understood unencrypted/plain data. Protected `.wolf` content is detected and reported as protected content, but the built-in runtime does not attempt to bypass game-author protection.

A future trusted provider may only be considered if the specific format/version has a justified compatibility path and appropriate legal/technical basis. It must not become a generic protection-bypass feature.

## Third-Party DRM and Native Launchers

Some games may add protection outside the base engine format, for example:

- custom packers;
- launchers requiring online activation;
- executable integrity checks;
- commercial DRM;
- native plugins that decrypt data after startup.

UniversalRPG should report these dependencies accurately. It should not silently execute the original launcher or arbitrary native code merely to gain access to protected content.

Preferred order:

1. support normal engine-managed format directly;
2. use a documented/authorized compatibility provider;
3. implement a high-level compatible replacement where possible;
4. report unsupported protection clearly;
5. never claim success by bypassing security boundaries.

## Analysis and Session Gating

`GameAnalysis.ProtectedContent` reports every recognized protected source with:

- scheme ID;
- source path;
- engine ID;
- protection category;
- whether the currently registered trusted providers can read it;
- provider ID when available;
- an explanatory note.

If an engine otherwise has a runtime but required protected content is not runtime-readable, `CreateSession()` fails with `session.protected-content-unavailable` rather than launching a predictably broken session.

## Testing Requirements

Each supported provider should cover at minimum:

- known-good content;
- malformed header/index;
- wrong/missing key;
- truncation;
- path traversal;
- absolute paths;
- duplicate/case-colliding entries where applicable;
- entry/file size limits;
- deterministic provider selection;
- no plaintext extraction side effects;
- plain-file override/fallback behavior;
- external SDK access through the same provider.

## Non-Goal

UniversalRPG is not intended to become a standalone asset-ripping/decryption application. The protected-content API exists so the compatibility runtime can consume legally obtained game data in the same way the original engine was expected to consume it.
