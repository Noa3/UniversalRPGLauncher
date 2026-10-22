# Drive RGSS scenes through real host frame boundaries

This execution plan is subordinate to K-PROD-RGSS-RENDERING in KANBAN.md and the engine acceptance gates in execplan/production-readiness.md. It does not replace the work queue. Maintain Progress, Surprises & Discoveries, Decision Log and Outcomes & Retrospective as implementation proceeds.

## Purpose / Big Picture

XP, VX and VX Ace must execute the game's own scene setup and loop, present the resulting images, and accept input without freezing the Godot window. Merely defining Bitmap, Sprite and Audio in Ruby is not presentation. The first acceptance target is a bounded scene that creates a bitmap and sprite, changes pixels and sprite position over several frames, and produces different captured output through the real runtime boundary. Keep these engines Experimental until the actual presentation and input paths are proven.

## Progress

- [x] Inspected production runtime bootstrap, Ruby block dispatch and RgssGrafikHost. Existing host has a frame callback and budget but the Ruby Graphics.update implementation bypasses it.
- [x] Local regression coverage for loop forwarding, break values, nested break/next, empty-loop execution budget, script overrides and ordinary method bodies passes (5/5). Bitmap font ownership passes across all three bootstraps (1/1) and detects mutation back to nil. Final canonical verification passed: 3003 tests, 381 suite records, exit 0. Build148 warnings and known shutdown leaks remain.
- [x] Route Graphics.update and wait through the host. Frame-bridge regressions pass 3/3, including all three bootstraps, terminal measured lifecycle and timing reset. The real XP scene reaches 5 host boundaries with 0 script problems; stopped updates are rejected rather than restarting it.
- [x] Preserve the running scene across frame handoffs; prevent duplicate updates, repeated setup, premature self comparison exits, and interpreter-state corruption on cancellation. Implemented locally with an owned worker rendezvous: focused13/13 and real XP ordinary Update10 frames/0 script problems. Review-driven lifecycle, measured-budget and diagnostic-retention corrections were reproduced RED then fixed GREEN. Corrected canonical run passed3017 tests,382 suite records,exit0 with final validator marker. Independent follow-up review found no new actionable regression; this is static review, not execution evidence.
- [ ] Implement and integrate bounded pixel surfaces, image loading and sprite composition; validate actual captured pixels and disposal.
- [ ] Wire a frame consumer and input into the Godot game view, then verify rendered runtime behavior.

## Surprises & Discoveries

The parser does not drop the loop block. It stores a block wrapper around a bare Identifier. EvaluateBlock did not preserve this wrapper when Name synthesized a call, and BrauchtBlock omitted loop. A call node with zero children was therefore not evidence of a missing parsed block.

An empty loop body performed no Evaluate calls and bypassed StepLimit. A dedicated budget charge per iteration is required even when no child statement exists. The regression timed out after 15 seconds before the fix and passed afterwards.

Initially RgssExtraRubySources implemented Graphics.update only by incrementing $graphics_frames, bypassing RgssGrafikHost. The explicit rgss_present_frame() helper now crosses the trusted host boundary and invokes its callback and budget. Real XP now reaches 5 host boundaries. These are not rendered images. There are no rendered surfaces behind the Ruby Bitmap stubs. Audio only stores state and does not play sound.

A first canonical run failed in four RGSS suites because extending the wrapper heuristic to all Identifiers misclassified ordinary method bodies. Parser block wrappers now carry explicit Receiver, Parameter and BlockBody roles; bare-Identifier dispatch requires that role. All four previously failing suites pass after the fix.

The runtime also invokes per-frame global updates and scene.update directly in RunGameLoop. This must not run concurrently with the game's own main loop, or gameplay advances twice.

## Decision Log

- Decision: prioritize the shared execution/presentation boundary over adding more API-name stubs. Rationale: it blocks all three RGSS engine targets and lowering diagnostic counts does not create playable output. Author: Hermes Code, following the user's explicit hardest-blockers-first request.
- Decision: retain Runtime-only metadata during this work. Rationale: an offscreen surface or synthetic frame callback alone does not prove playable game presentation, input, audio or saves.
- Decision: use the existing managed interpreter and trusted host boundary, with no native game DLL/EXE execution and no added interpreter dependency. Detection and parsing remain non-executing.
- Decision: never read live mutable Ruby state concurrently from the Godot thread. A worker, if needed to preserve the recursive interpreter stack, must publish immutable frame data at a controlled boundary and support cancellation, timeout and joining. Godot resources remain on the owning thread.

## Outcomes & Retrospective

The bounded host-frame bridge is implemented and locally tested, including instance/module self identity. Continuous scene-stack execution is implemented and validated by focused13/13 plus canonical3017 tests/exit0. Independent follow-up review found no new actionable regression. Rendering and input are not implemented. The broad engine criteria remain open. Record actual commands and final results below as milestones land; do not infer success from silence or compiler output filters.

## Context and Orientation

The project is E:/URPG/project, targets net8.0 and Godot.NET.Sdk 4.7.2, and already depends on Jint for unrelated MV/MZ work. No additional library is needed for the first RGSS frame boundary.

project/src/ruby/RubyInterpreter.cs recursively evaluates Ruby syntax. Call dispatches through IRubyHost; EvaluateBlock maintains _blockKette, the stack of attached blocks. Its current implementation is synchronous and not a resumable bytecode machine. An exception that unwinds a scene cannot be treated as a reusable continuation.

project/src/rgss/RgssGrafikHost.cs receives host graphics calls, counts Frames, invokes Rahmen and throws RgssFrameBudgetErreicht after Budget frames for a bounded measurement. project/src/plugins/RgssExtraRubySources.cs supplies common Ruby classes and explicitly forwards graphics updates through rgss_present_frame(). XpEngineRuntime.cs, VxEngineRuntime.cs and VxAceEngineRuntime.cs load these sources before builtins, load project scripts, inject RgssLaufzeit state, and expose runtime Update. XP's SpielSchleifeStarten is a bounded diagnostic entry, not a complete scheduler.

project/app/ui/Rm2kGameScreen.cs already accepts an RGBA pixel buffer through SetGameState. Reuse its display/scaling behavior where suitable, without assuming its RM2K map or input semantics apply to RGSS. Rendering metadata is defined in project/src/plugins/EnginePluginContract.cs.

## Plan of Work

First finish the current language/font slice and checkpoint canonical results without including unrelated RM2K battle work. Add a regression using each real built-in bootstrap and the graphics host, then invoke a synthetic scene's own main method. Require setup once, several host frames, and an observed budget exit. Any Ruby helper used to cross the host boundary must be explicit, bounded, and unavailable to detection/parsing.

Next make the scene's execution ownership explicit. Inspect instance self identity and method-call state preservation with minimal scene-transition tests. Choose a controlled worker rendezvous if preserving the existing recursive stack; do not restart main each Update or use exception unwinding as a resume mechanism. Bound unresponsive code independently of Graphics.update. Stop/Dispose must cancel and join only the owned worker; no Godot API calls from that worker.

Then implement a platform-independent pixel surface and a bounded live sprite registry. Bitmap operations must modify actual pixels, enforce dimensions and memory limits, and reject disposed or malformed resources with named diagnostics. Compose visible sprites in deterministic z/creation order with source rectangle, position, origin and opacity. Load images through a validated asset resolver rather than trusting unvalidated PNG-header dimensions. Transparent images and disposed objects must not leak old frame data.

Finally consume immutable RGBA frames on the Godot thread, route actual input snapshots at frame boundaries, and exercise the production launcher/game view path. Window text, tilemaps, viewports, tone/color, audio, save/load and full compatibility stay separate explicit gates unless implemented and verified.

## Concrete Steps

Run from E:/URPG:

    dotnet build project/UniversalRPG.csproj -v quiet
    tools/godot/editors/4.7.2/windows-x86_64/Godot_v4.7.2-stable_mono_win64_console.exe --headless --audio-driver Dummy --path E:/URPG/project res://tests/csharp_runner.tscn -- --suite=TestRubyKernelLoop
    tools/godot/editors/4.7.2/windows-x86_64/Godot_v4.7.2-stable_mono_win64_console.exe --headless --audio-driver Dummy --path E:/URPG/project res://tests/csharp_runner.tscn -- --suite=TestRgssBitmapFont
    bash scripts/validate.sh

The first suites currently report 5/5 and 1/1 respectively; the runner also executes its separate 20 smoke checks. Store logs under C:/Users/noa3/AppData/Local/hermes/profiles/code/cache/scratch. A killed or interrupted canonical run is not green.

## Validation and Acceptance

A frame-boundary test must observe the actual host callback several times and the configured budget exit, with setup called once and mutable scene state preserved between calls. Cover all three production bootstraps, own-script method overrides, a scene transition, Stop during a wait and a script that never yields. A font or frame counter is not pixel evidence.

The rendering milestone must assert exact non-background pixels from actual Bitmap writes and Sprite composition across successive frames. Mutating the sprite's position must change the expected region; disposing it must remove it. The final native capture must use the production consumer and cannot be a generated reference picture. Prove that invalid sizes, resource exhaustion and cancellation fail boundedly without freezing the UI or touching original files.

## Idempotence and Recovery

Preserve the unrelated uncommitted Rm2kBattleRunner.cs and rm2k_troop_page_decoder.cs changes. Do not use whole-file checkout to discard mixed edits. Tests generate only owned fixtures and managed scratch evidence, never modify imported games or original saves. After any killed run, recheck owned process status and exact log before restarting. A failed or unavailable reviewer is not a completed review; the recent read-only delegation failed with HTTP 401 and supplied no code findings.

## Artifacts and Notes

Observed focused behavior: TestRubyKernelLoop 5/5; TestRgssBitmapFont 1/1 across XP/VX/VX Ace; TestRgssFrameBridge 3/3; TestRubySelfIdentity 2/2. Font mutation to nil was rejected in each generation. Real XP: 90 parsed/executed scripts, 5 host frame boundaries, 0 script problems, 4 diagnostics over 1 distinct message (missing image), no rendered pixels. The old vacuous measurement assertion was replaced by actual frame-budget and stopped-lifecycle assertions. These are measured execution limits, not game completeness.

## Interfaces and Dependencies

Keep IEngineRuntime lifecycle contracts and existing runtime metadata stable until the corresponding behavior is ready. RgssGrafikHost remains the trusted per-game graphics boundary. Its frame publication must eventually carry owned immutable pixel data, while its measurement budget stays independently testable. Ruby methods may be overridden by game scripts; avoid overriding all same-named methods globally in RubyInterpreter merely to force host routing.

Revision note: established the shared RGSS execution/presentation work as the next large integration milestone, grounded in source and observed zero host frames rather than speculative API stubs.

Final verification note: the source and behavioral diff was reviewed by the parent after a provider HTTP401 prevented independent review. The final canonical run passed after the documented wrapper regression fix. Continuous scheduling and real pixel presentation remain the next uncompleted milestones.
