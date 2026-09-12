# Original MV/MZ core-script initialization

Updated: 2026-09-12. Installed-app MV/MZ execution remains the priority.

## Scope

The new core stage reads and evaluates the project's own libraries, engine core files and `plugins.js` before its enabled plugins. It does not replace the game's core with mock SceneManager/PIXI classes or with a bundled vendor runtime. All file access uses the supplied read-only content source. There is no HTTP server, browser executable or external original-runtime fallback.

**This is not a complete game boot.** `js/main.js` is read and hashed but deliberately not executed. Window-load callbacks, Effekseer initialization, Scene_Boot and actual rendering/input/audio/save behavior are not implemented by this stage. No engine Runtime capability is promoted.

## Dependency discovery

`NativeBootManifestParser` inspects text without evaluating imported JavaScript. A constrained Jint instance evaluates only URPG's own manifest parser. MV uses classic script tags from `index.html` in their original order. MZ additionally reads a leading literal `scriptUrls` array from `main.js`. The six required core files and `plugins.js` must occur in a valid generation-specific order. Extra declared project libraries are preserved, not replaced by a fixed default list.

This is a conservative static format, not a general HTML parser or JavaScript interpreter: inline code/handlers, dynamic list expressions, async/module scripts, base URLs, ambiguous contexts, unsafe URLs, missing/duplicate/mixed-generation cores and oversized inputs return explicit errors. An arbitrary custom main.js may mutate its list or do additional work later; that behavior is NOT validated or executed by manifest extraction.

`NativeCoreScriptSet` reads the declared files from one selected root or `www` prefix. It never borrows a missing file from a second project. Original bytes are snapshotted and hashed; invalid UTF-8, excessive per-file/aggregate source size and missing sources fail before partial modules are returned. The content provider remains caller-owned and must bound allocations before returning data.

## Original PluginManager

The core path does not install the standalone replacement PluginManager shim used by the isolated plugin probe. Instead, it requires the PluginManager and `$plugins` from the actual core/config scripts, calls their original setup method, and compares scheduled names with the existing enabled-plugin load plan.

Only the initial synchronous `loadScript` scheduling operation is temporarily captured (MV passes `name.js`, MZ passes `name`). The original property is restored in `finally`. Parameter methods, custom setParameters aliases, command registration and subsequent plugin monkey patches remain in the original realm.

This does not implement arbitrary patched `loadScript` side effects or dynamic loading later in gameplay. Such behavior is not certified by this stage. Schedule mismatches and earlier initialization fail rather than resetting/replaying a partially mutated realm. Core/library scripts receive scoped currentScript metadata; compatibility shims do not impersonate imported files.

## Developer entry point

After actual SDK/Jint/Godot validation succeeds, run from the repository root with the pinned .NET-enabled Godot binary:

```sh
godot --headless --path project res://tools/web_plugin_probe.tscn -- --game "/path/to/game" --engine mz --inspect-core
godot --headless --path project res://tools/web_plugin_probe.tscn -- --game "/path/to/game" --engine mz --execute-core --frames 3
```

Use `--engine mv` for MV. Default inspection and `--execute-plugins` retain their earlier meaning. Execution modes are mutually exclusive and require an explicit flag. An in-process VM is not an OS sandbox; execute only trusted projects.

Reports contain core-file order/path/hash/size, startup-file hashes and the first failure. `core-manifest-blocked` means startup dependencies were not accepted. `core-scripts-failed` means load/initialization/frame work failed. `core-scripts-passed` means only the declared scripts and requested limited frames completed; it does NOT mean a title screen or game ran. `originalEntryPointExecuted` and `fullGameRuntimeExecuted` remain false; playability remains not-tested.

The C# integration and actual scene have not yet been executed in the editing environment. Most real exports still require missing native graphics/DOM/audio services and are expected to fail at the first genuine dependency rather than report a fabricated success.

## Next acceptance milestone

Run the full branch validation, then use authorized default MV/MZ exports to identify the first failing original library/core API. Implement the necessary real native presentation/input/audio/storage services and the original entry-point lifecycle. Do not bypass main.js permanently or equate isolated script success with title -> New Game -> map -> dialogue -> transfer -> save/load.

Evidence: [VALIDATION_CORE_STARTUP_2026-09-12.md](VALIDATION_CORE_STARTUP_2026-09-12.md).
