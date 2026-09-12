# Native game-data pass — validation evidence

Date: 2026-09-12. Branch: docs/refresh-2026-09-09, PR #1.
Parent: 372d59aba4a0239cf232bed97e6f35c839b1cb0c.

## Scope

MV/MZ remains first, but installed-app execution now takes priority over general browser work/web delivery. This pass adds a read-only local JSON data adapter behind the XMLHttpRequest interface used by original game scripts. No HTTP, external runtime, game-code replacement or new Runtime-capability promotion is added.

## Executed

Environment: Node v22.16.0.

```sh
node scripts/test-native-game-data.mjs
```

Final result:

```text
Native game-data JavaScript: 27/27 passed (Node; mocked storage/timers, not .NET or full game).
```

The tests extract the actual production JavaScript constant from NativeDataRequestPrelude.cs. They use a controlled in-memory native transport and timer fixture, not the C# provider or production Jint engine. The initial test-fixture name collision with a scheduler variable was corrected without weakening the behavior assertions, then all 27 checks passed.

21 checks cover asynchronous completion, ready-state/event ordering, missing data, abort/reopen, stale response cancellation, receiver/arguments, response types, listeners, unsupported operations, pending bounds, private callback visibility and isolated realms.

Six checks execute the original MV DataManager loading/metadata methods from a pinned upstream excerpt. They load all 14 database files, preserve null-index arrays/note tags, read two maps lazily, allow a plugin to extend the database list/alias onLoad, and surface missing/malformed data.

**Important:** storage, timers, Utils.isOptionValid, Scene_Boot.loadSystemImages, Decrypter, ResourceHandler.createLoader and the formatting helpers are test doubles. No actual graphics, audio, scene boot, map traversal, encryption or game playthrough is tested. MZ's vendor DataManager is not included in these Node checks; shared MZ-language behavior has separate pending C# coverage.

## Added, not executed

TestNativeGameData contains **15 new C# methods** using the actual Jint adapter and SDK content contract: deferred reads, MZ language path, policy denial, unsafe paths, www isolation, Unicode/BOM, invalid UTF-8, oversize refusal, sanitized provider failure, abort, mount ownership, default-no-capability behavior and VM reentrancy.

These are not passing test counts. They require the actual .NET/Jint/Godot pipeline. No compiler result is claimed for the optional ClrFunction binding or the changed probe.

## Source/configuration checks

Materialized baselines matched their Git blobs before editing:

- JintEmbeddedScriptVm.cs: 3c32047d32af774277615a6f6bcf48a9ed925782
- LogicalGamePath.cs: 5f2004350495d0a41dbf69ae81cb1bd47a4832ea
- WebPluginProbe.cs: 6e939c1076f43646346b60cfc4e5853f1edde894
- validate.yml: 7cc185027760f54852f82e5cde9eba6e3a9235e9

The workflow received one additional Node test step; the previous workflow content remains unchanged. YAML parsing and job-dependency references were checked locally. This is not a GitHub Actions run.

LogicalGamePath's invalid StartsWith(char, StringComparison) invocation was corrected to its string overload. This is a source-level correction, not evidence of a successful complete build.

## Primary references and licensing

- Original MV DataManager: https://github.com/rpgtkoolmv/corescript/blob/9875c94cb92c655f4ff919458740bf1eb503ee0f/js/rpg_managers/DataManager.js
- Full upstream file blob: 9ddcc62f8324a29f202b376c77eaa63d871c1016
- Test fixture contains unchanged upstream lines 1–195 plus four provenance comments.
- MIT license/copyright retained in project/tests/fixtures/native-data/LICENSE.MV; upstream license blob 8365e056d2a2b297e7c6b95abbac671d0a860609.
- Jint native callback API checked against https://github.com/sebastienros/jint/blob/v4.16.2/Jint/Runtime/Interop/ClrFunction.cs (blob 839d13a4c51f2ed0a0c8e6a7969fc6cc81c57f5b).

The licensed excerpt is test-only and does not authorize copying proprietary games or RTP assets.

## Pending acceptance

```sh
./scripts/validate.sh
```

Require actual SDK/Jint/Godot builds and execution of the new C# suite, then run the developer probe with an authorized root-layout and www-layout project. Follow with real core boot/render/input integration and an actual game playthrough; the probe alone is not that milestone.

The current editing container has no dotnet or Godot. Toolchain retrieval was attempted and failed due network/DNS access. Full builds, actual probe execution, platform exports and user games remain unverified. Previous semantic reports and historical main 296/296 evidence must not be reused as acceptance of this branch.

Keep PR #1 unmerged until fresh complete validation and review. The installed native engine's full MV/MZ playability is not complete in this pass.
