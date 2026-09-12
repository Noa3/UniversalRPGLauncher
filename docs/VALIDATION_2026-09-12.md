# Validation evidence — 2026-09-12 stabilization

## Scope

Development branch: `docs/refresh-2026-09-09`, PR #1.
Implementation baseline before this pass: `c1f3ad0d2df15155e5e2510b6e8cf43edfb64050`.

This report deliberately separates executed tests from tests merely added to source.

## Executed

Environment: Node **v22.16.0** in the editing container.

```sh
node scripts/test-script-invocation-bridge.mjs
node scripts/test-web-plugin-shim.mjs
```

Results:

```text
JavaScript invocation bridge: 12/12 passed (Node; not Jint/Godot).
PluginManager JavaScript: 8/8 passed (Node; not Jint/Godot).
```

The scripts extract the actual JavaScript constants from production C# files. They do not test an independently rewritten imitation of the bridge/shim.

Covered behavior:

- normal, strict and global receivers;
- getter resolution and exception propagation;
- literal target/member names rather than code evaluation;
- primitive argument values/order and Unicode;
- captured intrinsics and independent realms;
- case-insensitive plugin lookup with string-valued parameters;
- prototype-named keys as data;
- code-like parameter values not executed;
- MZ callback receiver, falsy arguments and registration replacement;
- missing command behavior and MV/MZ API separation.

Workflow YAML was parsed locally and job dependencies checked. This is configuration syntax checking, not an Actions execution result.

## Added or strengthened, not executed here

- `TestRm2kMoveRouteRunner`: **14 C# test methods**.
- `TestWebPluginShimRegression`: **4 C#/Jint integration methods**.
- Existing directional-passability test expanded to **256 source/destination mask pairs**, both travel directions.
- Standalone Jint smoke expanded to cover real VM invocation, rejected CLR objects, argument bounds, getter timeouts, fault/reset and aggregate stored-source limits.

These are pending tests, not passing test counts. Node tests do not prove that C# compiles, that Jint applies constraints correctly, or that Godot gameplay works.

## Pending validation commands

```sh
dotnet build sdk/UniversalRPG.Sdk/UniversalRPG.Sdk.csproj --configuration Release
dotnet build runtime/UniversalRPG.JavaScript.Jint/UniversalRPG.JavaScript.Jint.csproj --configuration Release
dotnet run --project runtime/UniversalRPG.JavaScript.Jint.Smoke/UniversalRPG.JavaScript.Jint.Smoke.csproj --configuration Release
./scripts/validate.sh
```

Also pending: Windows/Linux/Android exports and representative authorized real-game playthroughs.

## Tooling limitation

The editing container did not have .NET or Godot installed, and attempts to retrieve the toolchain failed because network access was unavailable. The connected GitHub query returned no PR-triggered workflow run for the pre-change head. That observation does not identify the cause or establish that every type of Actions run is disabled.

The historical main result of 296/296 tests belongs to `782ea66141e494d32929a9cc41056523177888eb`; it is not validation of this development branch.

## Acceptance status

JavaScript semantic regressions: passed locally.
C#/.NET/Jint/Godot stabilization: source changes and regression coverage added, **verification pending**.
Whole runtime and release: **not complete; do not merge solely on this report**.
