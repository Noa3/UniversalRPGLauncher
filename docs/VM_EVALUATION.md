# Embedded Script VM Evaluation

> **Last reviewed:** 2026-09-11  
> **Decision state:** architecture selected; concrete native dependencies not yet vendored

UniversalRPG needs embedded script engines because custom RPG Maker scripts/plugins are part of game compatibility, not an optional addon.

The public/runtime architecture deliberately targets `IEmbeddedScriptVm` and `IEmbeddedScriptVmFactory` instead of exposing one third-party VM directly.

## Requirements

Any selected VM backend must be evaluated for:

- Windows x86-64
- Linux x86-64
- Android ARM64
- deterministic/restartable lifecycle where practical
- memory/resource controls
- interruption/watchdog support
- controlled host callbacks
- no mandatory external runtime installation
- build reproducibility
- acceptable redistribution license
- ability to deny arbitrary filesystem/process/network/native access
- useful error/stack diagnostics

Compatibility with the language alone is insufficient. The engine-facing RGSS/MV/MZ APIs are separate compatibility layers above the VM.

---

## Ruby / RGSS

### Current architecture decision

Use a CRuby-compatible native embedding backend behind `IEmbeddedScriptVmFactory`, but keep RGSS generations separated by explicit compatibility profiles:

```text
RGSS1 -> ruby-rgss1 -> rgss1-ruby18
RGSS2 -> ruby-rgss2 -> rgss2-ruby18
RGSS3 -> ruby-rgss3 -> rgss3-ruby192
```

A factory is not allowed to silently return a different language/profile implementation.

### Why CRuby-family embedding

Ruby exposes a native embedding/extension API suitable for defining classes/modules/methods and invoking Ruby code from C/C++ host code. UniversalRPG needs exactly this kind of boundary to implement `Graphics`, `Bitmap`, `Sprite`, `Input`, `Audio`, `Window`, `Table`, and other RGSS objects through host callbacks.

Ruby's license allows redistribution under the Ruby license or 2-clause BSD option, subject to the distribution's included legal notices/files. Exact vendored versions still require a per-version LEGAL/COPYING audit before shipping.

### Compatibility warning

Do **not** assume a current CRuby release can transparently replace every historical RGSS Ruby runtime.

The compatibility target is behavioral:

- RGSS1 / RGSS2: Ruby-1.8-era semantics
- RGSS3: Ruby-1.9.2-era semantics

Games may rely on:

- argument/block semantics
- String/Encoding differences
- syntax accepted by the historical parser
- aliasing and class reopenings
- removed/deprecated core methods
- exception behavior
- Marshal behavior
- Win32API assumptions

Therefore the implementation spike should compare these strategies rather than prematurely locking one:

1. embed historical-compatible CRuby builds per generation where build/platform maintenance is viable;
2. embed a newer CRuby with a strict compatibility layer only where conformance tests show sufficient fidelity;
3. allow optional modern-Ruby profiles later for games specifically written for replacement runtimes, but never use that profile silently for original RGSS games.

### Next Ruby spike

The next native spike should implement only enough of `IEmbeddedScriptVm` to run synthetic Ruby scripts:

- create/dispose VM
- configure safe policy
- load named source module
- execute in order
- report syntax/runtime exception with script identity
- define one host module + callback
- prove repeated sessions do not leak state
- prove denied host operations stay denied

Then run the same script-conformance fixtures across candidate Ruby profiles.

---

## JavaScript / RPG Maker MV/MZ

### Preferred first spike candidate: QuickJS / QuickJS-ng

QuickJS is small and designed to be embedded. Current QuickJS documentation describes a compact C implementation with low startup overhead and broad ECMAScript support. QuickJS-ng continues the model under the MIT license.

This makes it a strong first candidate for UniversalRPG because the VM can remain an implementation detail behind `IEmbeddedScriptVm`.

This is **not** yet a final dependency choice.

### Important limitation

A JavaScript VM is only the language engine.

RPG Maker MV/MZ games expect a substantial browser/RPG Maker environment such as:

- `window`
- `document`
- timers
- `performance`
- `requestAnimationFrame`
- Canvas
- WebGL
- WebAudio
- image loading
- XHR/fetch
- localStorage
- keyboard/pointer/gamepad input
- RPG Maker engine globals/classes

Some plugins additionally expect NW.js/Node APIs.

UniversalRPG should provide these incrementally as controlled host shims rather than embedding unrestricted Node.js by default.

### Existing repository foundation

`WebScriptInventory` already safely inventories MV/MZ plugins without executing them and classifies obvious requirements:

- standard browser API
- Node/NW.js shim required
- process execution required
- native addon required
- truncated source
- missing source

`WebScriptRuntime` then applies `ScriptExecutionPolicy` before loading enabled plugins in `plugins.js` order.

### Next JavaScript spike

Implement a QuickJS-family `IEmbeddedScriptVm` proof-of-concept with:

1. per-session runtime/context
2. memory limit
3. interrupt/watchdog handler
4. named module/source execution
5. exception/stack conversion
6. deterministic host-call bridge
7. no default `std`/OS/process exposure
8. synthetic MV/MZ plugin ordering test
9. Windows/Linux/Android build investigation

Only after the VM boundary works should browser/RPG Maker APIs be added.

---

## Node / NW.js Compatibility

Do not embed unrestricted Node.js merely to maximize plugin compatibility.

Preferred order:

1. static inventory detects requirement
2. implement safe high-level shim (`path`, bounded game-VFS `fs`, Buffer, limited process metadata)
3. gate dangerous behavior by explicit policy
4. process execution remains denied by default
5. native `.node` addons require a separate compatibility/HLE strategy

---

## Native Script Extensions

Native Ruby extensions, Node `.node` addons, Win32API/DLL dependencies and RPG_RT patches belong to a harder compatibility class.

Preferred strategy:

```text
inspect as data
 -> identify by hash/API usage
 -> known high-level replacement
 -> portable compatibility shim
 -> controlled native execution only as a late fallback
```

Never automatically load a native binary because an imported game references it.

---

## Licensing Gate

Before adding a VM implementation to distributed builds:

- pin exact upstream repository/version/commit
- record license and required notices in `THIRD_PARTY_LICENSES.md`
- confirm static/dynamic linking implications
- confirm Android redistribution/build path
- confirm transitive third-party source licenses
- add reproducible build instructions
- add CI build coverage

The repository itself still needs an explicit source license before publishing the standalone SDK as NuGet.

---

## Current Recommendation

- **Ruby:** proceed with a CRuby-family embedding spike, but keep RGSS1/2 and RGSS3 profiles behaviorally separate.
- **JavaScript:** proceed with QuickJS/QuickJS-ng as the first embedded VM spike.
- **Public API:** keep both behind `IEmbeddedScriptVm` / `IEmbeddedScriptVmFactory` so the project can replace a backend without breaking external consumers.
- **Security:** keep game-authored code denied access to arbitrary host APIs unless a compatibility policy explicitly grants a bounded capability.
