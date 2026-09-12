# Validation evidence — script startup and failure gates

Date: 2026-09-12. Branch: `docs/refresh-2026-09-09`, PR #1.
Parent implementation: `e038cfb04d7ffc4cf4ff5e2d3eb8a4bfc5a7a880`.

## Executed in this pass

| Command/check | Result | What it establishes |
|---|---|---|
| `node scripts/test-plugin-manager-contract.mjs` | 18/18 passed | Production shim parameter setters, scheduled names, callback and data semantics |
| `node scripts/test-web-plugin-shim.mjs` | 8/8 passed | Existing production-shim regressions remain satisfied |
| `python3 scripts/test-validation-driver.py` | 18 test methods passed | Actual shell-driver behavior with simulated dotnet/Godot processes |
| `bash -n scripts/validate.sh` | Passed | Bash syntax only |
| Workflow YAML parsing/dependency-key check | Passed | YAML structure/job references only |

Node version: v22.16.0. Both Node suites extract the JavaScript constant from the modified production C# file rather than testing a rewritten mock.

The existing eight-check suite was materialized unchanged; its Git blob hash is `ad830349a602807b0d36bc94a07e3bc396b88a7d`, matching the inspected repository file.

### Validation driver cases

The isolated driver tests cover normal completion, CRLF output, failed SDK/adapter builds, missing or zero-check Jint runs, contradictory success/failure text, a non-.NET or wrong-version Godot binary, an invalid explicit binary override, import/script errors despite exit 0, missing/empty Godot test runs, a positive failure count despite exit 0, nonzero exit despite a success marker, and stale log reuse.

These tools are **simulated processes** emitting controlled output. Passing the tests proves the driver's failure gates, not the real engines or compilers. Python and Node are development/CI tools, not end-user runtime dependencies.

## C# tests added, not executed here

- `TestScriptSessionSafety`: **16 methods**, with shared cases covering MV, MZ and RGSS1/2/3 orchestration through a recording VM.
- `TestWebPluginLoadPlan`: **10 methods** for stable selection, exact duplicates, disabled entries, parameter snapshots and input bounds.
- Existing RGSS hook test updated to perform explicit bootstrap before invoking its hook.

These **26 new C# methods are pending**, not a passing test count. In particular, a recording VM is not an embedded Ruby implementation.

## Full validation is still pending

No local .NET or Godot executable was available. Toolchain retrieval was attempted, including the official .NET download path, but failed in this environment; direct network attempts reported DNS resolution failures. No successful toolchain installation or current branch build is claimed.

The connected Actions query returned only an older Copilot review run for the branch, not a fresh canonical validator result. This observation does not establish why validator runs are absent.

Required full command:

```sh
GODOT_BIN=/absolute/path/to/the/pinned/Godot-mono ./scripts/validate.sh
```

The driver now requires the pinned `4.7.2.stable.mono` version and explicit non-empty completion summaries from Jint and the Godot test runner. Logs are retained in a fresh temporary directory, or at `URPG_VALIDATION_LOG_DIR` when explicitly configured. Each stage truncates its own prior log before running.

The prior main result of 296/296 tests is historical and does not validate this branch. The earlier dated validation report is left unchanged as a snapshot.

## Release status

JavaScript semantics and validation-driver regression checks passed locally.
C# compilation, actual Jint/Godot execution, platform exports and real-game playthroughs remain unverified. No engine was promoted to full scripting/runtime compatibility and PR #1 was not merged in this pass.
