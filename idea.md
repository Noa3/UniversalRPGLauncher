# UniversalRPG Product and Engineering Brief

> **Last reviewed:** 2026-09-09

## Mission

Build one self-contained application that safely discovers and eventually runs legally obtained games from supported RPG Maker generations and WOLF RPG Editor on modern platforms.

UniversalRPG should interpret supported game data through its own compatibility runtimes instead of requiring the user to launch original engine executables or install external replacement runtimes.

Correct game behavior comes first. Modern enhancements are optional and must not silently break compatibility.

## Product Targets

Primary platforms:

- Windows x86-64
- Linux x86-64
- Android ARM64

Secondary/export targets:

- macOS
- iOS
- future Linux ARM64/handhelds where practical

## Engine Scope

Primary runtime targets:

- RPG Maker 2000
- RPG Maker 2003
- RPG Maker XP
- RPG Maker VX
- RPG Maker VX Ace
- RPG Maker MV
- RPG Maker MZ
- WOLF RPG Editor

Research targets:

- RPG Maker 95
- RPG Tsukūru Dante 98
- RPG Maker Unite

Console RPG Maker products and generic Unity/HTML engines are outside the normal runtime scope.

## User Experience Goal

The intended flow is:

```text
Install UniversalRPG
        |
        v
Choose/import game
        |
        v
Safe bounded inspection
        |
        v
Detect engine/version/dependencies
        |
        v
Resolve required RTP/user resources
        |
        v
Select internal engine plugin
        |
        v
Run in UniversalRPG
```

Users should not normally need to install:

- EasyRPG
- mkxp/mkxp-z
- JoiPlay
- NW.js
- Wine
- separate Ruby/JavaScript runtimes
- original RPG Maker runtime executables

Internal open-source libraries may be embedded when licensing/security/compatibility justify them.

## Architecture

Use a shared core and engine-specific compiled plugins.

Shared services should include:

- game library/import
- bounded detection
- virtual filesystem
- deterministic/virtual clock
- input
- audio
- rendering/presentation interfaces
- save/cache storage
- compatibility profiles
- diagnostics
- security/capability policy

Engine plugins own their format/runtime semantics.

Do not force WOLF, RGSS or MV/MZ into RM2K event/data models merely to share code.

## Current Baseline

Reviewed `main` baseline: commit `782ea66141e494d32929a9cc41056523177888eb`.

Recorded canonical validation:

- clean .NET build
- `scripts/validate.sh` passed
- 296/296 headless tests passed

Current implementation:

- Godot 4.7.2 stable .NET host
- C#/.NET canonical implementation
- Godot project under `project/`
- persistent localized game library
- bounded folder/ZIP inspection
- deterministic plugin-based engine detection
- depth 4 / 4096-entry default inspection limits
- partial-vs-malformed scan distinction
- compatibility profiles/reports
- RM2000/2003 partial parser-backed runtime
- WOLF experimental unencrypted/plain-data runtime slice
- RGSS XP/VX/VX Ace detection/parsing only
- MV/MZ detection/metadata parsing only
- RM95/Dante/Unite research detection boundaries

No engine is yet claimed as broadly compatible.

## Runtime Development Priority

1. Keep build, tests and import security green.
2. Reach a representative RM2000/2003 playable milestone.
3. Complete verified passability/rendering/event/audio/menu/save prerequisites.
4. Add RM2000 vs RM2003 version-specific parity where required.
5. Expand WOLF only from verified/authorized native-format evidence.
6. Build a shared embedded Ruby/RGSS core, then XP → VX → VX Ace.
7. Build a shared embedded JavaScript/browser compatibility core, then MV → MZ.
8. Deepen RM95/Dante only from verified file-format evidence.
9. Keep Unite as research unless a realistic conversion/runtime strategy is proven.
10. Investigate native DLL/Win32 execution only after normal runtimes are stable.

Do not substitute engine count for runtime quality.

## RM2000 / RM2003 Milestone

The first meaningful end-to-end milestone should demonstrate, without `RPG_RT.exe`:

- load legal/authorized real LCF data
- render a representative map
- resolve chipset/passability correctly
- move the player
- interact with events
- display dialogue/choices
- transfer between maps
- play basic audio
- use a minimal menu/system path
- save/load safely
- recover/report unsupported commands without hanging/crashing

## WOLF Milestone

WOLF is a separate runtime family.

Initial goal:

- explicit supported version range
- authorized unencrypted/native-format fixtures
- database/map/event readers
- deterministic event VM
- map presentation
- input/audio/UI/save path

Do not bypass protected/encrypted data as a compatibility shortcut.

## RGSS Milestone

XP/VX/VX Ace require:

```text
IRubyVm
+ shared RGSS core
+ RGSS1/RGSS2/RGSS3 profiles
```

The embedded VM must not grant unrestricted host filesystem/process/network/native access.

A modern Ruby interpreter is not automatically RGSS compatible; historical semantics/API behavior require tests.

## MV / MZ Milestone

MV/MZ require:

```text
IJavaScriptVm
+ sandbox
+ browser/RPG Maker compatibility APIs
+ MV/MZ profiles
```

Implement only the browser/Node APIs required by supported games/plugins, but report unsupported calls explicitly.

Do not use an external browser/NW.js process as the normal compatibility path.

## Legacy Text and Paths

Games may rely on:

- CP932/Windows-31J/Shift-JIS
- Windows-1252
- Korean/Chinese legacy encodings
- Windows-style separators
- case-insensitive paths

Required behavior:

- internal Unicode representation
- explicit decoder boundary
- preserve raw bytes where round-trip identity matters
- deterministic encoding/profile decisions
- case-insensitive compatibility without nondeterministic collisions
- detect unsafe/ambiguous case or Unicode-normalization collisions

## Import and Runtime Security

Imported games are untrusted.

Detection/parsing must never execute imported:

- EXE
- DLL/SO
- Ruby
- JavaScript
- shell/batch files
- native plugins

Runtime script/native support requires:

- VFS containment
- read-only game mount
- separate writable save/cache/temp roots
- bounded memory/time/recursion/output
- watchdog/cancellation
- process/native loading denied by default
- network/clipboard/external links denied or explicitly permissioned
- actionable diagnostics

Original game directories stay read-only by default.

## RTP and Proprietary Assets

Do not bundle proprietary RPG Maker/WOLF runtimes, game assets or RTP data unless explicit redistribution rights permit it.

Users may configure legally obtained RTP resources. URPG should resolve them through its own bounded resource layer rather than requiring registry-only Windows installation behavior.

## Translation / Mod Overlays

After runtime foundations are stable, support non-destructive overlays for:

- translated text
- fonts
- graphics
- audio
- compatibility patches
- optional HD assets

Bind overlays to a stable game/version identity and do not mutate original archives.

External assets may be sourced only when their license clearly permits the intended modification and redistribution. Track provenance/license/attribution.

## Enhanced Mode

Potential optional improvements:

- integer/pixel-perfect scaling
- high-resolution output
- high-refresh presentation without changing simulation Hz
- shaders
- controller/touch remapping
- fast-forward/slow-motion
- screenshots
- accessibility
- asset/translation overrides
- save states/rewind where safe
- experimental widescreen

Faithful Mode remains the compatibility baseline.

## Library / Launcher Product Goals

- manual add and bounded directory scan
- covers/icons
- favorites
- search/sorting
- recent play time
- per-game compatibility status
- engine/version/dependency report
- RTP status
- control profiles
- enhancement profiles
- diagnostics/exportable compatibility report

## Acceptance Philosophy

A feature is complete when it is implemented and validated, not when a class or document exists.

A runtime engine becomes “playable” only after an authorized representative end-to-end path succeeds.

A compatibility claim must identify its scope and limitations.

The project should favor one trustworthy playable runtime over many misleading detection-only “supported” badges.
