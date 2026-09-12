# UniversalRPG Public SDK

> **Status:** early contract boundary (`0.1.0-alpha`)  
> **Last reviewed:** 2026-09-11

UniversalRPG is intended to be usable both as an application and as a library embedded by other tools, launchers, frontends, debuggers, compatibility projects, and game-management software.

The public API lives under the Godot-free namespace:

```text
UniversalRPG.Sdk
```

The same source files are compiled by the main Godot application and by:

```text
sdk/UniversalRPG.Sdk/UniversalRPG.Sdk.csproj
```

This is intentional: there is only one source of truth for public SDK contracts.

## Current Build Boundary

The SDK project targets plain `.NET 8` and must not depend on:

- Godot
- launcher UI classes
- game library UI state
- platform-specific Godot objects
- original RPG Maker executables
- embedded Ruby/JavaScript implementation details

The main application provides an adapter in:

```text
project/src/sdk_host/UniversalRpgLibraryAdapter.cs
```

that bridges the existing detector/plugin host to `IUniversalRpgLibrary` and `IUniversalRpgSession`.

## Public Embedding Model

Conceptually:

```text
External application
        |
        v
UniversalRPG.Sdk
 IUniversalRpgLibrary
        |
        +--> Analyze(game)
        |       |
        |       v
        |   GameAnalysis
        |   - engine/support
        |   - scripts/plugins
        |   - protected content
        |   - diagnostics
        |
        +--> OpenProtectedContent(...)
        |       |
        |       v
        |   IGameContentSource
        |
        +--> CreateSession(...)
                |
                v
        IUniversalRpgSession
        - Start / Update / Stop
        - optional IEngineScriptingRuntime
```

External consumers should not depend on `Main.cs`, Godot scene nodes, or launcher-specific classes.

## Capability Truth

`EngineSupportLevel` distinguishes:

- `DetectionOnly`
- `ParsingOnly`
- `ExperimentalRuntime`
- `PartialRuntime`
- `Playable`
- `BroadCompatibility`

A recognized game must not be reported as executable merely because UniversalRPG can inspect it.

The current implementation adapter therefore reports, at a high level:

- RM2000/2003 — partial runtime
- WOLF — experimental runtime
- XP/VX/VX Ace — parsing only
- MV/MZ — parsing only
- RM95/Dante98/Unite — detection/research only

## Script and Plugin Inventory

`GameAnalysis.Scripts` exposes bounded script/plugin metadata where available.

Current inventory paths include:

- XP `Data/Scripts.rxdata`
- VX `Data/Scripts.rvdata`
- VX Ace `Data/Scripts.rvdata2`
- MV/MZ `js/plugins.js` + `js/plugins/*.js`

The inventory contains stable script IDs, names, load order, logical source path, language ID, origin, and SHA-256 where available.

Inventory does not imply execution support. XP/VX/VX Ace and MV/MZ still remain parsing-only until their embedded VMs and compatibility APIs are validated.

## Scripting

A session may expose:

```csharp
IEngineScriptingRuntime? Scripting
```

This must remain `null` until that engine actually has a compatible script VM and API layer.

For example, RPG Maker XP being detected as RGSS1 does **not** mean Ruby scripts can currently run.

Shared public script contracts include:

- `IEmbeddedScriptVm`
- `IEmbeddedScriptVmFactory`
- `EngineScriptDescriptor`
- `ScriptModule`
- `ScriptExecutionPolicy`
- `IEngineScriptingRuntime`

This keeps engine boot/load semantics separate from the concrete Ruby/JavaScript VM implementation.

See [SCRIPT_COMPATIBILITY.md](SCRIPT_COMPATIBILITY.md) and [VM_EVALUATION.md](VM_EVALUATION.md).

## Logical Game Content

The SDK now exposes a Godot-free read-only content abstraction:

```text
IGameContentSource
```

Implementations currently include:

- `DirectoryGameContentSource`
- `ZipGameContentSource`
- `LayeredGameContentSource`
- the host-side MV/MZ encrypted asset source

This layer exists so engine/runtime code can ask for a normal logical path without caring whether the bytes come from:

```text
override/translation layer
        ↓
engine-protected game content
        ↓
plain game directory or ZIP
        ↓
RTP/fallback layer
```

`DirectoryGameContentSource` provides Windows-style case-insensitive path resolution on all platforms while rejecting traversal and reparse/symlink paths.

`ZipGameContentSource` is read-only and does not extract archives. It validates entry count, per-entry size, total uncompressed size, expansion ratio, traversal paths, and case-colliding logical names.

`LayeredGameContentSource` is ordered highest-priority first. If a higher layer claims a file but fails to read it, the error is returned rather than silently hiding a malformed override/archive with lower-priority data.

## Protected / Encrypted Content

`GameAnalysis.ProtectedContent` distinguishes recognized protected content from ordinary files and reports whether the current trusted runtime providers can read it.

Public contracts include:

- `ProtectedContentDescriptor`
- `ProtectedContentStatus`
- `IProtectedContentProvider`
- `ProtectedContentRegistry`
- `IUniversalRpgLibrary.OpenProtectedContent(...)`

Current behavior:

- MV/MZ built-in encrypted image/audio deployment assets: transparent read-only in-memory support implemented
- XP/VX/VX Ace encrypted archives: detected and represented, provider not enabled yet
- WOLF protected `.wolf`: detected/reported, no protection-bypass provider
- third-party DRM: no generic bypass

External clients can take a readable descriptor from `GameAnalysis.ProtectedContent`, call `OpenProtectedContent(...)`, and read logical files through `IGameContentSource` without implementing engine-specific decryption themselves.

See [PROTECTED_CONTENT.md](PROTECTED_CONTENT.md).

## Trusted Host Extensions vs Game Scripts

These are deliberately different security domains.

### Game-authored scripts

Examples:

- RGSS Ruby scripts
- MV/MZ JavaScript plugins
- native plugin dependencies

These are untrusted imported game content and must run under engine/runtime security policy.

### Host extensions

`IUniversalRpgExtension` is for code deliberately loaded by an embedding application or trusted UniversalRPG installation.

The launcher must **not** scan an imported game directory for arbitrary .NET assemblies and automatically load them as host extensions.

## Script Library Providers

Trusted embedding applications may eventually add compatibility libraries/shims through:

```text
IScriptLibraryProvider
IScriptLibraryRegistry
```

This can support use cases such as:

- a compatible implementation of a common RGSS library
- an MV/MZ plugin API shim
- debugging instrumentation
- translation/mod integration
- known-plugin High-Level Emulation

Imported game code itself must never receive unrestricted access to this registration API.

## Versioning

Current SDK API version:

```text
1
```

Breaking public contract changes must increment `UniversalRpgSdkVersion.ApiVersion` once the API leaves its current alpha design phase. During `0.1.0-alpha`, contracts are still expected to evolve together with the in-tree runtime and are validated from the same source files.

The contract assembly is pre-release and does not imply that all engines are playable.

## Packaging

`UniversalRPG.Sdk.csproj` currently has `IsPackable=false`.

This is intentional because the repository has not yet selected an explicit source-code license and public package release policy.

Before publishing a NuGet package:

1. choose and add the project license
2. define semantic versioning policy
3. add package metadata/license expression
4. add a package build in CI
5. test a small external consumer project against the produced package
6. document supported compatibility guarantees

Until then, the SDK is an internal/public-contract boundary available from source.

## Validation

`scripts/validate.sh` builds the SDK independently before building the Godot application.

That ensures accidental Godot dependencies or invalid public contracts are caught separately from launcher/runtime compilation.

New SDK/content/script work should always be covered by tests in the main C# suite in addition to the standalone SDK build.
