# UniversalRPG Autonomous Session State

> **Updated:** 2026-09-11  
> **Purpose:** concise durable checkpoint for Hermes/other autonomous agents. Historical details belong in commits and dated handoffs.

## Current Objective

Advance two first-class goals in parallel without overstating support:

1. RM2000/2003 toward a representative playable runtime path.
2. Custom RPG Maker script/plugin compatibility plus a Godot-free external SDK/library.

## Repository Baseline

Reviewed `main` baseline before this branch:

`782ea66141e494d32929a9cc41056523177888eb`

Last recorded canonical validation on that baseline:

- `scripts/validate.sh`: passed
- .NET build: clean
- headless suite: **296/296 passed**

The current branch contains substantial runtime/SDK/script changes and requires fresh validation before merge. Never reuse 296/296 as proof that this branch is green.

## Public SDK / External Library

Implemented:

- Godot-free `sdk/UniversalRPG.Sdk/UniversalRPG.Sdk.csproj` targeting .NET 8
- shared contract sources under `project/src/sdk/` compile into both main app and standalone SDK
- `IUniversalRpgLibrary` / `GameAnalysis`
- `IUniversalRpgSession`
- support-level descriptors separating detection/parsing/runtime truth
- `IEngineScriptingRuntime`
- `IEmbeddedScriptVm` / `IEmbeddedScriptVmFactory`
- `ScriptExecutionPolicy.SafeDefault`
- `EngineScriptDescriptor` / `ScriptModule`
- trusted host extension and script-library-provider contracts
- `UniversalRpgLibraryAdapter` bridges current detector/plugin host to SDK
- SDK analysis exposes discovered RGSS and MV/MZ script/plugin descriptors
- parsing-only engines are still refused executable sessions
- `scripts/validate.sh` now builds standalone SDK before Godot project/tests

Packaging remains disabled until the repository chooses an explicit source license/versioning policy.

## Custom Script / Plugin Compatibility

Custom game-authored scripts are a core compatibility requirement.

### Security baseline

Safe script policy allows normal game/save/cache access but denies by default:

- arbitrary host filesystem
- network
- clipboard
- process execution
- native interop

Metadata inventory never executes game code.

### RGSS — XP / VX / VX Ace

Implemented foundations:

- bounded Ruby Marshal 4.8 subset reader for `Scripts.rxdata`, `Scripts.rvdata`, `Scripts.rvdata2`
- array/fixnum/string/symbol/link/IVAR support sufficient for script archive shape
- bounded zlib decompression
- archive/script-editor load order preservation
- SHA-256 source identity
- public SDK script descriptors
- `RgssScriptRuntime` loads/executes modules in archive order over `IEmbeddedScriptVm`
- explicit bootstrap; loading alone does not execute code
- first script failure stops bootstrap with script identity
- generation-specific VM profiles:
  - RGSS1 -> `rgss1-ruby18`
  - RGSS2 -> `rgss2-ruby18`
  - RGSS3 -> `rgss3-ruby192`
- VM factory cannot silently substitute another language/profile
- synthetic fake-VM regression coverage

Not yet implemented:

- actual embedded Ruby VM
- RGSS1/2/3 host APIs
- full RPG serialized object/runtime support
- Win32API compatibility
- runtime registration for XP/VX/VXA

Current Ruby direction: CRuby-family embedding behind the VM factory, with historical compatibility profiles kept distinct. Do not assume a current Ruby release is automatically RGSS-compatible.

### MV / MZ

Implemented foundations:

- bounded non-executing `plugins.js` inventory
- enabled/disabled state and plugin order
- `js/plugins/*.js` discovery and SHA-256 identity
- unlisted plugin discovery without silent enablement
- conservative requirements classification:
  - browser-style
  - Node/NW.js shim
  - process execution
  - native `.node` addon
  - truncated/missing source
- `WebScriptRuntime` loads only enabled plugins in configured order
- executable source comes through explicit `IWebScriptSourceProvider`, not metadata inspector
- safe policy blocks process/native requirements before VM load
- MV/MZ-specific VM compatibility profiles
- public SDK analysis exposes plugin descriptors and compatibility diagnostics
- `GameDetector.HasCustomScripts` is now engine-aware; engine-core JS no longer counts as a custom plugin
- synthetic fake-VM and detector regression coverage

Not yet implemented:

- actual embedded JavaScript VM
- browser/RPG Maker API host environment
- Node/NW.js safe shims
- runtime registration for MV/MZ

Current first JS VM spike candidate: QuickJS / QuickJS-ng behind `IEmbeddedScriptVm`.

See:

- `docs/SDK.md`
- `docs/SCRIPT_COMPATIBILITY.md`
- `docs/VM_EVALUATION.md`

## RM2000/2003 Runtime Progress

Implemented on this branch:

- plugin/runtime capability hardening and fail-closed selection
- verified directional chipset passability foundation
- Counter tile metadata
- correct LMU trigger conversion
- active-page selection and same-layer event collision
- serialized foreground interpreter; parallel interpreters remain concurrent
- repeating autoruns while conditions remain active
- simulation-owned player-input lock for foreground events
- runtime-owned `TryMove()` / `TryInteract()` interaction semantics
- Player Touch on collision and successful movement
- action events on current tile/front tile/across up to three Counter tiles
- real pending-transfer application to target LMU with validation, passability/framebuffer/event replacement and fail-closed errors
- extensive new regression suites for these paths

Important remaining RM2K/3 work:

- promote chipset passage vectors fully to typed parser output and remove legacy raw-field fallback after fixture validation
- moving-event collision / Event Touch
- loop maps, tile substitution and vehicles
- broader event commands and RM2K/RM2K3 semantic differences
- faithful rendering/audio/menu/save/battle coverage
- authorized end-to-end real-game validation

## Engine Capability Truth

- RM2000/2003: partial runtime
- WOLF: experimental unencrypted/plain-data runtime
- XP/VX/VX Ace: detection/parsing + script inventory/pipeline, **no executable Ruby runtime**
- MV/MZ: detection/parsing + plugin inventory/pipeline, **no executable JavaScript runtime**
- RM95/Dante98/Unite: research/detection only

Do not set Runtime or scripting-execution support merely because scripts can be inventoried/decoded.

## CI / Validation State

`.github/workflows/validate.yml` is configured for .NET 8 + Godot 4.7.2 Mono/.NET, but the connected GitHub API still reports no workflow/status run for the current head.

Current `scripts/validate.sh` sequence:

1. standalone SDK restore/build
2. Godot .NET restore/build
3. Godot headless import
4. C# core/SDK/script/runtime/smoke tests

Fresh validation is mandatory before merge.

Focused suites now include at least:

- `TestEnginePluginContract`
- `TestPublicSdkContracts`
- `TestWebScriptInventory`
- `TestWebScriptRuntime`
- `TestRgssScriptArchiveReader`
- `TestRgssScriptRuntime`
- `TestScriptVmProfiles`
- `TestGameDetectorScriptContent`
- RM2K trigger/scheduler/runtime/passability/transfer suites

## Next Automatic Development Priorities

1. obtain a fresh SDK + Godot build/test run; fix compile/test regressions first
2. keep RM2K/2003 playable-path work advancing independently
3. implement first concrete native JS VM spike behind `IEmbeddedScriptVm` (QuickJS-family candidate), proving lifecycle/memory/interrupt/errors before browser APIs
4. perform CRuby-family embedding/build spike for Windows/Linux/Android and historical RGSS compatibility profiles
5. add RGSS host API skeleton only after a real Ruby VM session works
6. add minimal MV/MZ browser/RPG Maker APIs only after the embedded JS VM is isolated and tested
7. resolve custom `Game.ini` script archive paths for RGSS rather than assuming only default `Data/Scripts.*`
8. keep Node/process/native addon support policy-gated and late-stage; prefer HLE/shims
9. select an explicit source license before enabling NuGet packaging

## Recovery Rule

At session start read:

1. `AGENTS.md`
2. this file
3. `docs/PROJECT_STATUS.md`
4. `docs/ARCHITECTURE.md`
5. `docs/SDK.md`
6. `docs/SCRIPT_COMPATIBILITY.md`
7. relevant source/tests
8. `docs/ROADMAP.md` when choosing a new area

Historical handoffs/dated QA reports are snapshots, not current authority.

## Anti-Loop Rule

For the same normalized failure signature:

- at most 3 materially different repair strategies
- do not rerun the identical failure without changed evidence/input
- after threshold preserve evidence, isolate/block that path, and continue with an independent useful area

Never delete/disable correct tests or weaken security checks to obtain a green result.
