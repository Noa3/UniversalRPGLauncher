# Core-script initialization pass — validation evidence

Date: 2026-09-12. Branch: `docs/refresh-2026-09-09`, PR #1.
Parent: `f3d31dcf57d053bc78ab65025c9e3bb1e089b1c4`.

## Actually executed

Environment: Node v22.16.0.

```sh
node scripts/test-native-boot-manifest.mjs
node scripts/test-original-plugin-setup.mjs
node --check scripts/test-native-boot-manifest.mjs
node --check scripts/test-original-plugin-setup.mjs
```

Results: **36/36 manifest checks** and **18/18 original-plugin-setup checks passed**. Both suites extract the actual JavaScript constants from their production C# files. JavaScript syntax checks also passed.

Manifest coverage includes static MV order, leading MZ literal declaration, custom library preservation, HTML comments/attributes, data-only inspection, URI paths, ambiguity/mixed-engine refusal, size/count limits and explicit rejection of unsupported startup forms. No imported main.js is evaluated.

Plugin setup coverage uses the unchanged original MIT-licensed MV PluginManager fixture (Git blob `491e9fa141ccfc6422dd03de865a6dc91bbf49ce`). Tests include parameter identity/aliases, enabled/duplicate ordering, synthetic MZ filename scheduling, callback errors and restoring the original loadScript method. The Array.contains helper and MZ scheduling variant are test doubles, not full engine implementations. The initial synthetic MZ setup string missed a closing brace; the test fixture was corrected, then all 18 checks passed without weakening assertions.

## Source/configuration checks

Materialized baseline hashes were verified before editing:

- WebScriptRuntime.cs: `9c3222b4406847f7352314869caee594dbc2298d`.
- WebPluginProbe.cs: `8b8bd06d1d44f03f8d31128dd0a9e9538bf14618`.
- validate.yml: `e1e9eca35f6012567b3cfe11622aa7ecd37b6e73`.
- THIRD_PARTY_LICENSES.md: `a2e3ed4dec11f6c0720d35f26a7c7e2deabbca7b`.

The main WebScriptRuntime file has only the intended prelude-execution/diagnostic hunk; the helper lives in a partial. The workflow adds two Node test steps and preserves the previous jobs/toolchain settings. YAML syntax and job dependency references were checked; this is not an Actions run.

## Added, not executed

- TestNativeCoreScriptSet: **14 C# methods** covering actual script-set loading/snapshotting, root isolation, source limits and failures.
- TestNativeCoreInitialization: **4 C#/Jint methods** for original setup before plugins, scoped source metadata, core failures and configuration mismatches.
- Actual `--inspect-core` / `--execute-core` Godot scene and native VFS/Jint integration.

These are **18 pending C# methods**, not passed tests. No .NET compiler result, Jint policy result, renderer output, platform export or full-game playthrough is claimed.

## Tooling and required acceptance

No dotnet/Godot toolchain was available in this editing environment. Direct .NET toolchain retrieval failed DNS/network access. The GitHub query for PR-triggered runs on the parent returned an empty list; it does not explain the absence of validator runs.

```sh
./scripts/validate.sh
```

After full validation, run the developer core modes against an authorized standard MV project and an MZ project, including one www deployment. Keep exact first-failure diagnostics. Node uses synthetic manifests/storage and does not execute the C# wrapper or a complete game. Do not reuse earlier main's 296/296 count or other dated reports as acceptance of this pass.

## Honest runtime boundary

The stage loads original project scripts, not an invented native game loop, but deliberately stops short of executing main.js and full scene boot. Conservative manifest extraction is not proof that arbitrary custom main.js behavior is supported. Capturing initial loadScript scheduling does not implement custom loader side effects or later dynamic loads. Real native rendering/input/audio/storage and original startup callbacks remain necessary.

No Runtime flag was enabled. PR #1 remains unmerged pending fresh complete validation and review. See CORE_STARTUP.md for user-facing report semantics.
