# UniversalRPG Brief for Hermes

## Mission

Build one self-contained application that safely discovers and eventually runs legally obtained RPG Maker games on Windows, Linux, macOS, Android, and iOS. Preserve original game behavior first; add optional modern features without mutating original game files.

## Non-Negotiable Requirements

1. Targets: Windows x86-64, Linux x86-64, macOS universal, Android ARM64, and iOS ARM64.
2. App starts in a game library, scans a user-selected root, and shows every recognized game before any runtime starts.
3. Supported RPG Maker families: 2000, 2003, XP, VX, VX Ace, MV, and MZ.
4. Imported games are untrusted. Detection must never execute game binaries or scripts.
5. Runtime is an independent compatibility implementation. Do not bundle Enterbrain/Kadokawa runtime code, RTP, or game assets.
6. English is the default/fallback UI language. Ship German, Spanish, French, Japanese, Korean, and Simplified Chinese. Adding a PO catalog and one locale registry entry must be enough to add another language.
7. Preserve raw source bytes where round-tripping matters. Decode into Unicode only at parser boundaries.
8. Original game directories stay read-only. Saves, cache, patches, translations, and settings live in app-owned directories.

## Current Baseline (2026-08-20)

- Godot 4.7.2 project and start scene load successfully.
- Responsive game library chooses and persists a directory.
- Scanner checks two subdirectory levels, skips hidden folders and filesystem links, and limits metadata reads to 1 MiB.
- Detector recognizes real LCF, RGSS, MV, and MZ file signals without executing them.
- UTF-8, UTF BOM, CP932, and Shift-JIS metadata decoding exists through Godot's multibyte decoder.
- Seven UI locales and bundled Noto Sans CJK glyph coverage exist.
- Export presets exist for all target platforms.
- Runtime launch button accurately remains disabled because no gameplay backend is complete.
- Existing RM2K parser is a prototype and must not be treated as a correct LCF implementation.

## Runtime Priority

1. Complete RM2000/2003 LCF reader for `RPG_RT.ldb`, `RPG_RT.lmt`, and `MapXXXX.lmu` using real fixtures and public format documentation.
2. Build map renderer, input/audio adapters, event interpreter, save system, menus, and battles.
3. Validate real games and encode quirks in data-driven compatibility profiles.
4. Add RGSS1/2/3 using a sandboxed Ruby VM and explicit RGSS/Win32API compatibility APIs.
5. Add MV/MZ using a sandboxed JavaScript VM plus only the browser/Node APIs games need.

Do not jump to broad engine support before one runtime is playable end to end.

## Legacy Text and Paths

Japanese games frequently use CP932/Windows-31J/Shift-JIS in INI files, LCF strings, scripts, and filenames. Korean and Chinese translations may introduce CP949/EUC-KR, GBK/GB18030, or Big5. Western games may use Windows-1252.

Required design:

- `ITextDecoder`-style boundary with strict UTF-8 first, BOM handling, engine/profile hint, then legacy fallback.
- Encoding selection per file and, where formats require it, per field.
- Preserve undecodable raw bytes and emit diagnostics; never silently replace data needed for identifiers.
- Internal text is Unicode. Do not normalize game identifiers unless the original engine did.
- Test CP932 half-width kana, combining marks, invalid byte sequences, and mixed encodings.
- Resolve game paths case-insensitively while preserving original spelling.
- Detect case collisions and Unicode-normalization collisions instead of choosing nondeterministically.

## Import and Runtime Security

- Canonicalize every path and prove it remains below its mount root.
- Reject `..`, absolute archive paths, drive/UNC prefixes, NUL/control characters, reserved device names, and filesystem links during import.
- Set limits for directory entries, recursion, individual file size, total bytes, archive expansion ratio, image dimensions, parser nesting, and script execution.
- Inspect archives before extraction; extract only into a fresh app-owned staging directory.
- Hash source files and bind profiles, patches, saves, and translations to game/version identity.
- Never call host process execution for `Game.exe`, DLLs, scripts, or plugin helpers.
- Ruby and JavaScript VMs get virtual filesystem, input, audio, rendering, clock, and optional network capabilities only.
- Network, clipboard, external links, and native plugin behavior require per-game policy and visible user consent.
- Crash containment, watchdogs, deterministic limits, and actionable compatibility reports are required.

## Game Translation Feature

Plan a non-destructive translation overlay after the first runtime is playable:

1. Extract translatable database, dialogue, choice, item, skill, map, and plugin strings with stable context IDs.
2. Export/import PO and XLIFF; support translation memory and glossary metadata.
3. Bind a translation pack to engine, game hash, and source version.
4. Apply translated text through the VFS override layer. Never rewrite original archives.
5. Support per-pack fonts, fallback chains, line-breaking rules, vertical metrics, and text-box overflow diagnostics.
6. Allow image/audio replacement only as explicit patch assets with provenance.
7. Machine translation is optional and opt-in. It must disclose external network use and never upload whole games.
8. Translation packs must not redistribute copyrighted source text or assets without permission.

## JoiPlay-Parity Product Goals

Use JoiPlay as a capability reference, not as a code source or branding dependency. UniversalRPG remains RPG-Maker-focused.

- Friendly library with manual add, directory scan, cover/icon, favorites, search, sorting, and recent play time.
- Modular runtime/backend updates with explicit compatibility versions.
- Per-game profiles for renderer, engine quirks, locale/encoding, input, audio, and performance.
- Configurable touch gamepad: move/resize/hide buttons, opacity, dead zones, layouts, and per-game presets.
- Physical keyboard/gamepad mapping, mouse/touch emulation, vibration, and accessibility controls.
- Save discovery, backup, import/export, conflict-safe sync, and desktop/mobile transfer.
- Integer scaling, aspect modes, filters/shaders, orientation, FPS limits, fast-forward, and screenshot controls.
- Patch/translation overlays, compatibility fixes, and rollback without modifying the game.
- Diagnostics, script console for developers, logs, compatibility reports, and optional safe cheat/debug tools.
- Archive/folder import through Android Storage Access Framework and iOS document picker.

Ren'Py, TyranoBuilder, Construct, Flash, and generic HTML5 support are outside current scope. Architecture may stay extensible, but RPG Maker runtime quality wins over engine count.

## Near-Term Acceptance Criteria

1. App starts without parser/runtime errors in all seven UI locales.
2. Real sample folders for each RPG Maker generation are detected with expected title, engine, evidence, and warnings.
3. CP932 Japanese titles decode identically on Windows, Linux, macOS, Android, and iOS.
4. A malicious corpus cannot escape selected roots, follow links, over-expand archives, execute code, or overwrite game files.
5. RM2000/2003 parser reads real LCF database/map fixtures and rejects malformed input with bounded work.
6. First playable milestone renders a map, moves the player, displays Japanese dialogue, runs basic events, and saves/loads without external runtimes.
