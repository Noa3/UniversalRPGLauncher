# UniversalRPG — current implementation status

Reviewed: 2026-09-12. **MV/MZ first, installed application first.** The user has deferred general browser work and wants actual playable games inside the engine. Existing other-engine implementations remain intact.

## Product and milestone

URPG is a compatibility runtime, not a game-content remake. Preserve original scripts, custom plugins and game logic. Implement only the browser-shaped interfaces actually needed by that code, backed by native engine services. A full browser, web server, NW.js or original-game executable is not the normal runtime path.

No complete MV/MZ game boot or playable runtime is established yet. The branch must not be described as green without fresh .NET/Jint/Godot verification. The next playable milestone remains title -> New Game -> map movement -> dialogue -> transfer -> save/load in an authorized default project, followed by representative plugins.

## New native game-data path

The Jint adapter now optionally accepts an IGameContentSource. A private native function serves bounded local data/*.json requests, including nested plugin JSON files. The JavaScript side preserves the XMLHttpRequest shape used by original DataManager code, but there is no HTTP or arbitrary host-path lookup.

Requests complete through the existing frame pump, preserve callbacks/state and cancellation, and report missing files as errors instead of supplying empty success data. Original code still parses JSON and extracts note metadata, so plugins can extend the database list or alias onLoad rather than being bypassed by a replacement loader.

The source restricts file/read/aggregate budgets, UTF-8 decoding and path prefixes; enforces AllowReadGameFiles per read; and does not own/dispose the shared VFS mount. Native provider exceptions are sanitized. Reentrant VM reset/disposal/execution is refused while a callback is active. These controls are not an OS sandbox or a complete process-memory limit.

The developer probe enables this narrow read capability only for explicitly requested trusted plugin execution. It resolves one root or www project and rejects ambiguous mixtures. Inspection remains non-executing. A passed probe remains a script subset, not a full game result.

## Engine boundaries

| Family | Present implementation | Remaining game-runtime gap |
|---|---|---|
| MV/MZ — primary | Plugin inventory/order/parameters, Jint, timing/metadata subset, optional native local JSON transport, developer probe | Verified original core/library startup; real rendering/audio/input/storage; complete game execution |
| RM2000/2003 | Partial LCF/events/simulation/passability/transfers and corrected actual LMU page parsing | Movement integration/timing, rendering/audio/menu/save/battle completeness |
| XP/VX/VX Ace | Script parsing, configured paths, generation-specific ordered pipelines | Embedded Ruby and RGSS APIs |
| WOLF | Experimental understood plain-data parser/VM | Broader formats, systems and real-game conformance |
| RM95/Dante98/Unite | Detection/research | Executable runtime |

The shared SDK stays Godot-free. There is no new package/runtime dependency and no new Runtime capability flag in this pass.

## Validation

Executed: **27/27 Node semantic checks** of production data-adapter JavaScript. Six use pinned, MIT-licensed original MV DataManager methods to load 14 databases, preserve note metadata, load successive map data, handle a plugin-added database and surface missing/malformed data. Storage/timers and unrelated engine dependencies are explicit test doubles. No rendering or full game is tested.

Added but not executed: **15 C# methods** using actual Jint/SDK interfaces. Full SDK/Jint/Godot builds, the probe scene, exports and user projects remain pending. Node results do not prove native adapter compilation, policy enforcement or disk/archive integration. The current environment has no dotnet/Godot and toolchain retrieval failed.

Evidence: [VALIDATION_NATIVE_DATA_2026-09-12.md](VALIDATION_NATIVE_DATA_2026-09-12.md). Interface/scope: [NATIVE_MV_MZ.md](NATIVE_MV_MZ.md). Prior dated reports remain historical.

## Immediate priorities

First establish actual full validation, then implement the real core/library boot sequence and native rendering/input/audio/save services. Preserve custom-script behavior and use real projects to guide integration. Do not grow a general browser or substitute isolated semantic tests for a playable game. Other engines receive regression maintenance. No Kanban is maintained.
