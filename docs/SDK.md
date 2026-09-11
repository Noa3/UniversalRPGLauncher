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
        v
Analyze(game)
        |
        v
GameAnalysis
        |
        v
CreateSession(...)
        |
        v
IUniversalRpgSession
        |
        +--> Start / Update / Stop
        |
        +--> optional IEngineScriptingRuntime
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

## Scripting

A session may expose:

```csharp
IEngineScriptingRuntime? Scripting
```

This must remain `null` until that engine actually has a compatible script VM and API layer.

For example, RPG Maker XP being detected as RGSS1 does **not** mean Ruby scripts can currently run.

See [SCRIPT_COMPATIBILITY.md](SCRIPT_COMPATIBILITY.md).

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

Breaking public contract changes must increment `UniversalRpgSdkVersion.ApiVersion`.

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

`scripts/validate.sh` now builds the SDK independently before building the Godot application.

That ensures accidental Godot dependencies or invalid public contracts are caught separately from launcher/runtime compilation.
