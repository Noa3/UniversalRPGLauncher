# UniversalRPG — current implementation status

Reviewed: 2026-09-12. **MV and MZ now have first priority at the user's request.** They have more projects available to test. This supersedes older RM2K-first ordering without discarding other runtimes.

## Product boundary

URPG is an internal compatibility runtime/launcher, not a game remake. Preserve game-authored scripts, ordering, patches and engine semantics. A script VM or successful plugin subset is not a complete game engine.

The development branch remains unverified as a whole under .NET/Jint/Godot. Do not reuse historical main test counts or claim that Node semantic checks validate the C# integration. No merge or additional Runtime-capability promotion was made in this pass.

## Active MV/MZ work

The existing Jint backend and ordered plugin pipeline now have an opt-in, frame-pumped browser subset: window/self aliases, virtual performance.now, function timers and cancellation, animation callbacks, and currentScript metadata while a plugin file executes. Existing five-argument WebScriptRuntime construction remains supported; the new overload enables the host explicitly.

AdvanceFrame accepts seconds only after successful bootstrap and invokes the whole callback batch once through the VM. Invalid host deltas are rejected before touching execution state. Callback failures stop the session, requiring a fresh runtime/VM. Limits cover queue sizes, arguments and callbacks. This is frame-quantized scheduling, not full browser event-loop conformance; see MV_MZ_TESTING.md for deviations.

The new developer probe inspects an actual selected MV/MZ folder by default. It requires --execute-plugins for limited execution, uses the existing VFS provider, never modifies the game folder, and writes a scoped JSON report under user://web-plugin-probes. It reports plugin-subset results separately from full-game playability. Unimplemented SceneManager, PIXI, DOM, audio or Node calls are not faked to obtain success.

The VFS script provider now checks an available inventory SHA-256 against the bytes read for loading. Changed sources require fresh inspection. This checks content identity only, not code trust or origin authenticity.

## Engine status

| Family | Present source | Missing for game playability |
|---|---|---|
| MV / MZ — primary | Inventory, load plans, parameters/commands, Jint adapter, opt-in timing/metadata host and developer probe | Validated integrated builds, actual core/library boot, DOM/render/audio/input/data/storage integration and representative full games |
| RM2000 / RM2003 | Partial LCF/event/simulation runtime, passability, transfers and recent actual LMU event-page import fix | Automatic movement integration/timing, complete renderer/audio/menu/save/battle behavior |
| XP / VX / VX Ace | Script archives, configured paths, generation profiles and ordered fail-stop loader | Embedded Ruby and RGSS APIs |
| WOLF | Experimental understood plain-data parser/VM | Broader native formats, systems and real-game conformance |
| RM95 / Dante98 / Unite | Detection/research | Executable runtime |

The Godot-free SDK, content providers and other engine code are retained. No external original-engine, EasyRPG, mkxp, Wine or NW.js process is added as a normal runtime dependency. Node remains test tooling only.

## Validation of this pass

Executed locally on Node v22.16.0: 46 production browser-host semantic checks and both synthetic MV/MZ plugin fixtures passed. The fixtures use explicit fake core filename markers and do not test vendor engine code. Workflow YAML/dependencies and source resource paths were inspected.

Added but not executed: 15 C#/Jint frame-host methods and 4 content-identity methods. Full SDK/Jint/Godot builds, the actual developer scene, C# tests, platform exports and user games remain pending because this environment lacks .NET/Godot and direct network/toolchain retrieval fails.

Detailed evidence: VALIDATION_MV_MZ_HOST_2026-09-12.md. Prior dated reports remain historical snapshots.

## Next milestone

First validate the current branch and the developer probe under actual Godot/Jint. Then build a version-aware manifest for real MV/MZ core/library startup, followed by data and encrypted-asset access through VFS, real rendering/audio/input/storage and a simple default project path: title -> New Game -> map movement -> dialogue -> transfer -> save/load. Extend custom plugins against that core path with minimized failure fixtures.

Other engines receive regression maintenance, not priority takeover. The old RM2K-first roadmap is superseded by this explicit product decision. No Kanban is maintained.
