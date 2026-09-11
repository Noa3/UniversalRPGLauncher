# Game-authored script and plugin compatibility

Last reviewed: 2026-09-12.

Custom scripts are a core compatibility requirement, not an optional graphical enhancement. Script inventory, an executable language adapter, and a playable engine are nevertheless different milestones.

## Current boundary

| Family | Present source | Not yet established |
|---|---|---|
| RM2000/2003 | Partial event interpreter, movement-route decoder/runner and simulation | Full event/system parity and end-to-end playability |
| XP / VX / VX Ace | RGSS script-archive reader, configured Game.ini script paths, generation-specific ordered pipeline | Embedded Ruby and RGSS1/2/3 engine APIs |
| MV / MZ | Plugin inventory, load plan, PluginManager shim, ordered pipeline, Jint 4.16.2 adapter | Validated complete browser/render/audio/storage host and playable engine registration |
| WOLF | Experimental understood plain-data event/database VM | Broad native-format and gameplay compatibility |

The JavaScript adapter is no longer merely a proposed dependency. Its source exists, but the current branch still requires a complete .NET/Jint/Godot build and test run. Do not advertise full MV/MZ support because a standalone plugin probe can execute.

## Shared public interfaces

The Godot-free SDK defines `IEngineScriptingRuntime`, `IEmbeddedScriptVm`, `IEmbeddedScriptVmFactory`, `EngineScriptDescriptor`, `ScriptModule`, `ScriptExecutionPolicy` and trusted compatibility-library contracts.

Concrete engine pipelines reproduce original ordering above the interchangeable VM. The application host still owns rendering, input and other engine services. End users are not expected to install Ruby, Node.js, NW.js, Wine or an original engine executable separately.

## Fail-stop startup

Both `WebScriptRuntime` and `RgssScriptRuntime` now enforce:

```text
metadata/policy checks -> load -> explicit bootstrap -> host hooks
                            |            |                 |
                            +------------+-----------------+
                                         |
                                    failure/exception
                                         |
                              refuse further execution
```

Loading alone does not execute game code. Host hooks require successful bootstrap, not merely loaded source.

A provider or VM may have mutated state before returning an error. Once an external load, bootstrap or hook operation fails, that pipeline is unusable for further execution. A repeated call returns `web.session-faulted` or `rgss.session-faulted` without configuring, loading or executing earlier scripts again.

Recover by disposing the pipeline and creating a **fresh runtime and fresh VM**. Do not silently reset the same VM and replay initialization. This prevents additional duplicate startup side effects; it is not a transaction that undoes writes or other effects already performed by a script.

Metadata/policy rejection before the first external load call does not consume the untouched session. Ordinary provider/VM exceptions are returned as diagnostics with phase and script identity. Fatal memory/stack exceptions are not presented as successful recovery.

Use one host thread per pipeline. Reentrant load/bootstrap/hook calls return `*.operation-in-progress`; this guard is not a claim of general thread safety. Reentrant disposal is refused rather than destroying a VM while it is executing.

## MV/MZ executable plugin selection

`WebPluginLoadPlan.SelectEnabled` is shared by the loader and shim builder:

1. Bound the input enumeration before sorting.
2. Select enabled entries in stable configured order.
3. Keep only the first enabled occurrence of an **exact, case-sensitive plugin name**.
4. Preserve actual whitespace in names.
5. Snapshot selected parameter dictionaries with per-field and aggregate limits.

This matches the inspected MV `PluginManager.setup` rule. De-duplicating names is distinct from parameter-key normalization: parameter access is lowercased, so distinct selected names differing only by case can share a parameter key. Duplicate SDK script IDs are still diagnosed rather than guessed away.

`GetPluginParameters` returns the configured snapshot. It is not a readback of later mutations inside the live JavaScript realm.

### PluginManager surface

The VM-neutral shim supports:

- `parameters(name)` and `setParameters(name, values)`;
- `_scripts` containing the selected/scheduled plugin names, in order;
- MZ `registerCommand` / `callCommand`, retaining the supplied receiver and argument values.

`_scripts` is scheduled-load metadata, not proof that every plugin initialized successfully. Configuration uses JSON parsing so special names such as `__proto__` remain data. JavaScript parameter objects can be changed by plugins through the setter as expected, without mutating the host's original configuration dictionary.

Dynamic `loadScript`, DOM setup, Canvas, WebGL, WebAudio, browser timers and full RPG Maker globals are not provided by this parameter shim. Missing functionality must not be replaced with successful no-ops.

### Reference

Behavioral reference inspected: the official/community MV CoreScript repository's `js/rpg_managers/PluginManager.js`:

`https://github.com/rpgtkoolmv/corescript/blob/master/js/rpg_managers/PluginManager.js`

Inspected blob: `491e9fa141ccfc6422dd03de865a6dc91bbf49ce`.

This pass does not import that implementation or add a runtime dependency on it. MZ-specific and full browser conformance still require their own fixtures.

## RGSS ordering and compatibility

Scripts execute in archive order so aliases, reopened classes and patches retain their intended sequence. The loader rejects unknown generation values, duplicate script IDs, and negative/duplicate archive indices before VM configuration. It bounds caller-supplied entry enumeration.

RGSS1/2/3 retain separate profiles. No actual Ruby backend is installed by `RgssScriptRuntime`; tests using a recording VM verify orchestration only. A future Ruby backend must address historical language semantics, RGSS APIs, cooperative execution and optional Win32API compatibility rather than simply executing modern Ruby syntax.

## Security and external libraries

Static plugin classification is advisory. It does not prove that a source is harmless or that every API call has been found. Host filesystem, network, clipboard, process and native access remain explicit policy/host capabilities.

Jint's restricted host bindings and execution constraints are not an operating-system sandbox or a hard cap on total process memory. Dynamic string compilation remains disabled in the current adapter; plugins requiring `eval` or `Function` need an explicit future compatibility/security decision.

Trusted embedding applications may provide prelude modules and compatibility libraries. Imported games cannot silently register arbitrary host-level .NET assemblies. Windows DLLs, native Node addons and runtime-patching plugins remain a separate compatibility class; prefer narrowly specified replacements where feasible.

Archive/VFS access and engine-managed asset decoding do not imply script or engine playability. Inspecting or loading metadata must not execute the game.

## Validation

See [the startup validation report](VALIDATION_SCRIPT_STARTUP_2026-09-12.md) for executed versus pending checks.

The new JavaScript semantic suite runs the actual production shim constant under Node. The validation-driver tests run the actual shell driver with isolated simulated tools. Neither is proof of a .NET/Jint/Godot build or a playable game.

The C# session and load-plan tests must pass through `./scripts/validate.sh` before this development slice is treated as fully verified. Do not promote any engine capability based solely on these source changes.
