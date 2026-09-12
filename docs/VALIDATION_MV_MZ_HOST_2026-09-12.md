# MV/MZ browser-host pass — validation evidence

Date: 2026-09-12. Branch: docs/refresh-2026-09-09, PR #1.
Parent: 7a538b8df494a077cbd3922387d086a3fea2d7c3.

## Product decision

MV/MZ takes priority over the earlier RM2K-first order because the user has more MV/MZ projects to test. Existing engine implementations are preserved. No work board is created.

## Executed in the editing container

Node v22.16.0:

```sh
node scripts/test-web-browser-host.mjs
node scripts/test-web-probe-fixtures.mjs
```

Results:

```text
Browser host JavaScript: 46/46 passed (Node; not C#/Jint/Godot).
MV/MZ synthetic probe fixtures: 2/2 passed (Node; not Godot probe or full games).
```

The host suite extracts the actual JavaScript constant from WebBrowserHostPrelude.cs. It covers virtual clock units, deferred callbacks, ordering, cancellation, shared timer IDs, arguments/receivers, interval coalescing, nesting clamp, frame timestamps, queue/callback limits, errors, reentrancy, metadata scope and isolated realms. It also checks captured intrinsics and delay-coercion reentrancy.

The fixture runner extracts the unchanged production PluginManager JavaScript and the new browser host, then evaluates the committed synthetic MV/MZ plugin files with those actual constants. It does not test the C# load plan, CLI parser, content scanner, actual Jint or any vendor RPG Maker core.

## Source/configuration checks

- Materialized WebScriptRuntime baseline matched Git blob dc3e39eef318eaa8d6b43abcbcbff68941f54a44. Integration changes are isolated to a partial-class boundary, an additional constructor, prelude composition and plugin source scopes.
- The original five-argument WebScriptRuntime constructor is retained, delegating to the new overload with no host enabled.
- Unchanged PluginManager baseline matched Git blob 5775912fd8c8088c72225d8c2f55c0e597692182.
- The workflow baseline (before inserted steps) matched Git blob 18102ad43f71ad79fcd4acb8aad8b7f417f498e2. Existing checks/toolchain pins were preserved.
- Workflow YAML was parsed and dependency references inspected. The new probe scene's script resource path exists.

These are source/config checks, not compilation.

## Added but not executed

- TestWebBrowserFrameHost: 15 C# methods using real Jint, including actual timer constraints, lifecycle and MZ callbacks.
- TestWebScriptSourceIdentity: 4 C# methods for hash match/change/invalid/absent identity.
- The developer Godot scene, its JSON-report flow and both synthetic Godot/Jint probe runs.
- The new CI steps invoking those checks.

## Pending acceptance

```sh
./scripts/validate.sh
godot --headless --path project res://tools/web_plugin_probe.tscn -- --game "project/tests/fixtures/web-probe/mv" --engine mv --execute-plugins --frames 3
godot --headless --path project res://tools/web_plugin_probe.tscn -- --game "project/tests/fixtures/web-probe/mz" --engine mz --execute-plugins --frames 3
```

Use the pinned .NET-enabled Godot binary. Successful subset reports are not full-game acceptance. Windows/Linux/Android exports and actual authorized user projects remain untested.

The editing environment has no dotnet or Godot executable. Current direct attempts to access GitHub and the official dotnet installer failed DNS resolution. Connected GitHub reading/writing remains available, but no full build result is inferred from successful source commits.

## Deliberate limitations

The new host is opt-in and provides frame-pumped timing plus script metadata, not complete browser/event-loop fidelity. New callbacks wait for the next pump, missed interval periods coalesce, and callback failures stop the session. Promise/microtask checkpoints, rendering, audio, input, storage, dynamic script loading and original core boot remain unimplemented here. String timers remain unsupported. The realm-local control object is not an inaccessible security boundary; Jint is in-process, not an OS sandbox.

No engine was promoted to Runtime and no merge should be based on Node checks or historical 296/296 main evidence. See MV_MZ_TESTING.md for behavior references and how to interpret probe reports.
