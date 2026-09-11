# UniversalRPG — Project Status

Last reviewed: 2026-09-12. This describes source present on the development branch, not a released compatibility guarantee.

## Product boundary

UniversalRPG is a Godot-hosted C# application with engine-specific interpreters and a separate .NET SDK. The project is **not yet a generally playable replacement for every supported detector**.

The current branch has extensive changes beyond the historical main baseline. Fresh .NET/Jint/Godot validation remains required. Executed JavaScript-semantic evidence is recorded separately in [VALIDATION_2026-09-12.md](VALIDATION_2026-09-12.md); it must not be used as proof that the complete application builds.

## Engine matrix

| Engine family | Present implementation | Important missing boundary |
|---|---|---|
| RM2000 / RM2003 | Partial LCF parser, event scheduler/interpreter, passability, runtime state, transfers and presentation models | Full rendering/audio/menu/save/battle parity and representative end-to-end playability |
| XP / VX / VX Ace | Metadata, bounded script-archive reader, Game.ini script-path handling, ordered VM pipeline | Embedded Ruby and RGSS1/2/3 APIs; no executable engine registration |
| MV / MZ | Metadata, plugin inventory/parameters, ordered loader, concrete Jint adapter and PluginManager shim source | Validated integrated VM plus browser/render/audio/storage host; no playable MV/MZ runtime registration |
| WOLF RPG Editor | Experimental understood unencrypted/plain-data parser and event VM | Broader native formats, systems and real-game conformance |
| RM95 / Dante 98 / Unite | Detection/research | Runtime implementation |

Detection, parsing, an isolated language interpreter, and a playable engine are distinct milestones. A capability must not be promoted simply because a class or a synthetic test exists.

## SDK, scripts and content

The application references the shared `sdk/UniversalRPG.Sdk` assembly rather than providing competing copies of public SDK types. The SDK itself remains Godot-free; its current application adapter is still Godot-hosted.

Existing foundations include:

- library analysis/session/extension contracts and truthful engine support descriptors;
- ordered custom-script metadata and replaceable embedded VM interfaces;
- RGSS Marshal/zlib script decoding, generation profiles and configured script paths;
- MV/MZ enabled-plugin order, parameters, source identity and compatibility diagnostics;
- read-only directory/ZIP/layered/prefixed content sources;
- protected-content provider contracts and in-memory MV/MZ engine-asset reading.

Packed data access does not imply that the corresponding game engine can execute. No general third-party DRM bypass, arbitrary DLL execution or external original-runtime fallback is provided.

## Latest correctness work

### JavaScript invocation

The prior host-side GetValue/Invoke sequence detached methods from their receiver and passed arbitrary CLR argument values into Jint. The new private invocation bridge resolves the target and member inside one constrained call, preserves `this`, and converts only bounded JSON primitive arguments. Strings used for target/member names are literal keys, never evaluated as code.

The bridge captures its JavaScript intrinsics before game code runs. Getter failures and execution constraints fault the session; invalid host arguments return errors before executing game code. Stored module text receives an aggregate UTF-16 source budget, separate from Jint's per-entry allocation limit.

### PluginManager

Configuration is parsed as JSON rather than injected as a JavaScript object literal. Special names such as `__proto__` remain data. MZ callbacks retain their supplied receiver and argument values, including null/false/zero/empty strings. The builder bounds input enumeration and does not silently trim real plugin filenames.

### RM2000/2003 routes

The runner now uses the real simulation coordinate fields, grows valid lazy switch storage, snapshots input commands, and validates coordinates before arithmetic. Fourteen new regression methods cover movement, collision, skip/repeat, facing locks, through movement, switches, invalid input and preservation of parsed event positions.

The directional-passability fixture previously expected reverse movement through an edge whose opposite flag was blocked. It now independently varies source and destination masks and checks both directions without weakening the geometry implementation.

**Not completed:** automatic route extraction/scheduling from LMU pages, faithful move-speed/frequency timing, full runtime sprite synchronization, unsupported route operations and a complete gameplay loop. The route runner still executes a bounded supported subset per explicit Step call.

## Security and compatibility limitations

Jint is pinned to 4.16.2 behind an interchangeable adapter. CLR namespace/reflection access is disabled. Arbitrary CLR object arguments are refused. Time, statement, recursion and allocation constraints are configured, but an in-process interpreter is **not an OS sandbox**, and its allocation/source budgets are **not a hard total process-heap limit**.

Dynamic string compilation remains disabled; plugins requiring eval or Function are not automatically compatible. Node/NW.js/process/native requirements remain separate host capabilities, not implicit grants. Static source classification is diagnostic and cannot replace enforcement at runtime.

No imported game code executes during detection. No Runtime capability was newly enabled in this stabilization pass.

## Validation and immediate priorities

The workflow separates JavaScript-semantic tests from the portable .NET/Jint smoke and Godot checks. Node is used only for development regression tests and is not part of the user installation.

Required next steps:

1. Run the complete SDK/Jint/Godot validation and repair measured failures.
2. Connect supported movement routes to LMU pages, timing, active-page refresh and render descriptors with integration fixtures.
3. Complete representative RM2K traversal, interaction, transfer and save paths before claiming playability.
4. After real VM validation, add minimal browser/RPG Maker services with explicit capability tests rather than empty stubs.
5. Continue Ruby/RGSS and WOLF as independent, evidence-driven runtime tracks.
6. Audit dependency notices, exports and actual target devices before release.

Use source/tests, `SESSION_STATE.md` and the relevant engine documents as the operational baseline. There is intentionally no Kanban file. Historical handoffs remain dated evidence, not current acceptance results.
