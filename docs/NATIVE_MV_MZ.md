# Native MV/MZ execution priority and game data

Updated: 2026-09-12. General browser development/web delivery is deferred at the user's request. The target is an installed URPG application that runs games through its own engine integration. MV/MZ remains the first engine family.

## Why a local XMLHttpRequest adapter exists

Original MV/MZ scripts use browser-shaped interfaces even in desktop exports. Preserving those calls does not require launching a browser or serving files over HTTP. URPG translates the relevant data requests into read-only VFS reads inside the application.

```text
Original DataManager / custom plugin
  -> XMLHttpRequest GET data/SomeFile.json
  -> embedded Jint native data adapter
  -> explicitly supplied IGameContentSource
  -> local game folder / configured archive mount
```

This pass implements that narrow data path, not a complete browser, general HTTP client or full engine boot. It does not replace the game's DataManager methods, database list, note-tag parsing or plugin aliases with a different gameplay implementation.

## Host configuration

The original one-argument JintEmbeddedScriptVm constructor and default factory remain unchanged in meaning: no content capability is installed. A trusted native host may opt in:

```csharp
using var vm = new JintEmbeddedScriptVm(languageId, contentSource, contentPrefix: "www");
```

Use an empty prefix for a root-layout project. The mount must implement safe read-only lookup and pre-allocation limits. It remains owned by the host, not the VM. The VM installs a private native function with primitive string input/output; arbitrary reflected CLR objects are not exposed to game code.

AllowReadGameFiles must be enabled in the explicit execution policy. The source checks it again for every read. The existing frame host must be installed before scripts call send(); completion is scheduled on the next frame pump. The whole pump retains the VM's outer execution budget. No background thread, browser window, localhost server or HTTP request is created.

## Supported data subset

- Relative GET requests under data/, including nested plugin JSON files.
- Optional leading ./, percent-encoded names, and ignored query/fragment cache metadata.
- UTF-8 including an initial BOM; JSON text is passed through, not rewritten.
- Asynchronous response callbacks, readyState, text/json responseType, abort and reopening a request.
- Defined loadstart/load/error/abort/loadend/readystatechange callbacks and bounded function listeners.
- A pending request that is cancelled/reopened does not deliver stale results.
- Original DataManager/custom code remains responsible for JSON parsing and note metadata.

Local read success uses status 200. A local failure has status 0 and invokes onerror with bounded diagnostic metadata, including urpgErrorCode. There is no simulated HTTP server returning actual HTTP status codes. JSON responseType returns null on malformed JSON; text responses preserve the text so the original engine can surface its own parsing error.

This is not complete XMLHttpRequest or browser conformance. Synchronous requests, network URLs, credentials, writes/bodies, general request headers, binary response types and arbitrary timeouts are explicitly unsupported. Callback exceptions propagate into the existing fail-stop session instead of being silently ignored.

## Bounds and containment

Current limits: 8 MiB per file, 32 MiB returned source bytes and 128 native reads per outer VM call, 128 pending requests and 64 listeners per request. The VFS must cap allocations before returning data; the adapter's post-read limits do not substitute for that. Decoded text and parsed JSON allocations still consume memory, and these controls are not a hard whole-process memory quota or OS sandbox.

Only logical data/*.json paths are accepted. Absolute paths, file/http/other schemes, traversal, alternate streams, double encoding and Windows device-name segments are refused. Native provider errors do not echo unrelated absolute host paths. File types outside the data contract remain unavailable through this adapter even if the mount contains them.

VM Configure/Load/Execute/Invoke/Reset refuse reentry during execution, and disposal during a callback throws. This prevents a native provider from resetting an actively executing engine. It does not make one VM thread-safe.

## Developer probe integration

The existing res://tools/web_plugin_probe.tscn is still inspection-only by default; --execute-plugins explicitly runs a trusted subset. The probe now selects one root/www content layout and rejects ambiguous mixtures. It passes that prefix to the native data adapter while preserving the existing inspected script paths.

No script execution occurs on import/inspection. Reports still set fullGameRuntimeExecuted=false and playability=not-tested. The actual full core, renderer, audio, input and persistence are not booted by this tool. A successful data request or subset report is not a working game.

## Evidence and next milestone

See [VALIDATION_NATIVE_DATA_2026-09-12.md](VALIDATION_NATIVE_DATA_2026-09-12.md). Node tests use the production adapter and a pinned original MV DataManager excerpt with explicitly mocked transport/timers and unrelated engine dependencies. Real C#/Jint integration, file/archive reads and full games require actual toolchain validation.

The next development target is the original core/library startup order and native rendering/input/audio/save services, culminating in a default project's title -> New Game -> map -> dialogue -> transfer -> save/load path. Preserve custom plugins against those real services. Do not spend that milestone on browser export or a generic browser API collection.
