# UniversalRPG session checkpoint

Updated: 2026-09-12. Branch: docs/refresh-2026-09-09, PR #1.
Parent implementation: 372d59aba4a0239cf232bed97e6f35c839b1cb0c.
No Kanban. Preserve unrelated work.

## Current priority

MV/MZ remains first, now explicitly focused on the installed application. General browser work/web delivery is deferred. Browser-shaped APIs are only compatibility adapters for original scripts; no actual browser, HTTP server or external runtime is required by this pass.

## Latest implementation

- NativeGameDataSource routes relative data/*.json reads through an explicit read-only VFS mount with optional www prefix, UTF-8/BOM handling, path and per-execution resource bounds.
- Local XMLHttpRequest subset supports the original asynchronous DataManager read pattern, callbacks, abort/reopen and error behavior without networking or writes.
- Jint gets an optional content-source constructor and a private primitive-only native function. The original constructor/factory still grants no data capability. Read policy is checked for every native call; the shared content source remains host-owned.
- VM reentrancy guards prevent reset/dispose/recursive execute while a native callback is active.
- Developer probe uses one unambiguous root/www project for scripts/data and allows local JSON reads only during explicitly requested trusted plugin execution.
- Fixed the invalid StartsWith(char, StringComparison) call in LogicalGamePath to its string overload.
- Added 15 C# integration methods, 27 executable Node checks and a pinned MIT-licensed test-only original MV DataManager excerpt. No production core replacement or runtime-capability promotion.

## Actual validation

Ran node scripts/test-native-game-data.mjs on Node v22.16.0: **27/27 passed**. This executes the production JavaScript adapter and original MV loading methods with mocked transport/timers and explicit unrelated engine test doubles. It is not a complete MV/MZ or native C# test.

Workflow YAML/job references checked; the original workflow baseline matched its Git blob before the single added test step. Full .NET/Jint/Godot builds, all 15 new C# methods, the actual probe, exports and user games remain unexecuted. The editing environment lacks dotnet/Godot and toolchain retrieval failed with network/DNS errors. No historical main count is accepted for this branch.

Details: docs/VALIDATION_NATIVE_DATA_2026-09-12.md.
Native scope/configuration: docs/NATIVE_MV_MZ.md.

## Next useful work

1. Run the complete ./scripts/validate.sh under actual pinned tools and repair measured failures. No merge before fresh verification/review.
2. Build the real original MV/MZ core/library startup sequence and version profiles, using the native data adapter instead of empty engine globals.
3. Connect actual Godot rendering, input, audio and safe save storage. Target a simple title/New Game/map/dialogue/transfer/save path, not more generic browser features.
4. Test user-owned projects with original custom plugins and minimize failures. Plugin-only/data-only passes are not proof of playability.
5. Preserve RM2K/RGSS/WOLF and shared SDK work; maintain the no-external-executable path.

After three materially different failed strategies for one signature, checkpoint evidence/unblock conditions and move to an independent useful slice. Do not replay failed initialization, weaken assertions, invent completed features or create a work board.
