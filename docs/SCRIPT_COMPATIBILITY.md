# Game-Authored Script and Plugin Compatibility

> **Last reviewed:** 2026-09-11  
> **Status:** architecture and inventory foundations; executable Ruby/JavaScript VMs not yet implemented

Running game-authored scripts is a core UniversalRPG compatibility requirement, not an optional enhancement.

Many RPG Maker games depend on custom scripts/plugins as heavily as they depend on maps and database files. A replacement runtime that ignores those scripts is not broadly compatible.

## Compatibility Principle

UniversalRPG should execute the scripting model expected by each engine generation through an internal compatible runtime.

The end user should not normally have to install Ruby, Node.js, NW.js, Wine, or the original RPG Maker executable separately.

Custom game scripts must run with the same ordering and engine-facing APIs expected by the original engine as far as practical.

## Engine Families

### RPG Maker 2000 / 2003

These engines do not have RGSS/JavaScript-style built-in game scripting.

Game-authored logic primarily comes from:

- event commands
- common events
- move routes
- variables/switches/database-driven systems
- patched-runtime extensions
- DynRPG / Maniacs / executable patches in some games

UniversalRPG's RM2K/3 event interpreter is therefore the normal script-like compatibility layer.

Patch/native plugin compatibility is a separate advanced track and should prefer known High-Level Emulation over arbitrary native execution.

### RPG Maker XP — RGSS1

Required long-term architecture:

```text
XP game
  |
  v
Scripts.rxdata
  |
  v
RGSS1 script archive reader
  |
  v
embedded Ruby VM
  |
  v
RGSS1 compatibility API
  |
  v
UniversalRPG services
```

Custom Ruby scripts must be loaded in the original project-defined order.

Important API families include:

- Graphics
- Input
- Audio
- Bitmap
- Sprite
- Viewport
- Window
- Tilemap
- Plane
- Font
- Rect
- Color
- Tone
- Table
- RPG data classes
- Win32API compatibility where required

### RPG Maker VX — RGSS2

Reuse the same embedded Ruby VM and shared RGSS core, with a dedicated RGSS2 compatibility profile.

Do not fork the complete XP runtime.

### RPG Maker VX Ace — RGSS3

Reuse the same architecture with an RGSS3 profile.

Custom scripts, aliases, monkey patches, class reopenings, and script load order are part of expected compatibility.

Using a modern Ruby interpreter is not by itself sufficient: historical Ruby/RGSS behavior needs compatibility tests.

### RPG Maker MV

Custom logic is commonly delivered through JavaScript plugins configured by:

```text
js/plugins.js
js/plugins/*.js
```

UniversalRPG now has a bounded, non-executing `WebScriptInventory` that:

- reads plugin ordering/enabled state from `plugins.js`
- inventories plugin files
- hashes complete inspected plugin sources
- identifies unlisted plugins without silently enabling them
- classifies obvious Node/NW.js/process/native-addon requirements
- never evaluates JavaScript during inspection

Future execution architecture:

```text
MV project
   |
   v
plugin inventory/order
   |
   v
embedded JavaScript VM
   |
   v
MV browser/RPG Maker API profile
   |
   v
UniversalRPG services
```

### RPG Maker MZ

MZ uses the same broad architecture as MV with a separate MZ compatibility profile.

MV and MZ should share the JavaScript VM and web compatibility infrastructure while preserving engine-specific API differences.

### WOLF RPG Editor

WOLF should be treated as its own event/database runtime rather than forced into Ruby or JavaScript abstractions.

Its common events and database-driven systems are the primary programmable layer.

## Public SDK Contracts

The Godot-free SDK defines:

```text
IEngineScriptingRuntime
IEmbeddedScriptVm
EngineScriptDescriptor
ScriptModule
ScriptExecutionPolicy
IScriptLibraryProvider
```

These are implementation-independent contracts.

A future RGSS backend might wrap CRuby; an MV/MZ backend might wrap QuickJS, V8, or another suitable JavaScript engine. The public API should not expose the chosen VM directly.

## VM Requirements

Any embedded Ruby/JavaScript runtime must support:

- deterministic engine-controlled lifecycle where practical
- engine-defined script load order
- bounded memory
- watchdog/time limits
- controlled call depth
- VFS-backed file access
- explicit save/cache write roots
- diagnostics with script identity and stack information
- controlled host callbacks
- clean runtime reset/disposal

The VM must not automatically inherit unrestricted application permissions.

## Safe Default Policy

`ScriptExecutionPolicy.SafeDefault` allows normal game-data access but denies host-impacting capabilities by default:

- arbitrary host filesystem — denied
- network — denied
- clipboard — denied
- process execution — denied
- native interop — denied

Compatibility exceptions should be explicit and visible.

## MV/MZ Plugin Classification

Current static classifications are advisory and intentionally conservative:

### StandardBrowserApi

No obvious Node/process/native requirement was found in the bounded inspected source.

This does **not** guarantee runtime compatibility.

### RequiresNodeShim

The plugin references Node/NW.js-style APIs such as `require`, `process`, `Buffer`, `fs`, or `path`.

UniversalRPG should implement only the required safe subset rather than embedding an unrestricted Node host by default.

### RequiresProcessExecution

The plugin appears to use APIs such as `child_process`, `spawn`, or `exec`.

These should remain denied by default and require a deliberate compatibility/security decision.

### RequiresNativeAddon

The plugin references `.node` addons or native loading.

This is a substantially harder compatibility class and may require a platform-specific replacement/HLE implementation rather than executing the original addon.

### Truncated / MissingFile

The inventory cannot safely make a complete compatibility assessment.

## External Compatibility Libraries

Trusted embedding applications may provide compatible libraries/shims through the SDK.

Examples:

- reimplementation of a popular RGSS utility library
- known MV/MZ plugin API shim
- compatibility replacement for a common native dependency

These trusted host libraries are different from imported game code.

UniversalRPG must never allow an imported game to silently register arbitrary host-level .NET modules.

## Near-Term Implementation Order

1. keep RM2K/3 event execution accurate
2. maintain safe MV/MZ plugin inventory and compatibility diagnostics
3. define serialized RGSS script/archive readers for XP/VX/VX Ace
4. evaluate and select an embedded Ruby implementation
5. implement `IEmbeddedScriptVm` adapter for Ruby
6. implement shared RGSS API core + RGSS1 profile
7. boot XP default/custom scripts
8. extend to RGSS2 and RGSS3
9. evaluate/select embedded JavaScript VM
10. implement sandboxed MV/MZ browser runtime
11. load plugins in `plugins.js` order
12. add Node/NW.js shims only from real compatibility requirements
13. address native addons/plugins with HLE first

## Definition of Script Compatibility

An engine should not advertise scripting support merely because script files can be found or parsed.

Scripting support requires at minimum:

- actual executable VM/runtime
- correct engine API profile
- correct script load order
- failures surfaced with useful diagnostics
- security policy enforced
- representative custom-script/plugin fixtures passing

Until that exists, the SDK/session `Scripting` property must remain unavailable for that engine.
