# Hermes autonomous goal — UniversalRPG

## Current user decision: MV/MZ first

As of 2026-09-12 prioritize **RPG Maker MV and MZ**, because the user has more projects available for direct testing. This supersedes earlier RM2K-first prompts and roadmaps. Keep all existing RM2000/2003, RGSS and WOLF work and regression tests; do not delete them or wait for those engines to be complete before advancing MV/MZ.

URPG remains a self-contained, plugin-based compatibility runtime using C#/.NET and Godot. It is not a game-content remake. Execute the original games' expected behavior, custom scripts and plugin order; do not redesign their worlds or replace their game logic. Preserve the Godot-free SDK and interchangeability of embedded VM implementations.

Read and follow `AGENTS.md`, `SESSION_STATE.md`, `docs/PROJECT_STATUS.md`, `docs/MV_MZ_TESTING.md`, then relevant source and tests. There is no Kanban. Do not create one or spend a session merely reorganizing plans.

## Immediate working sequence

1. Repair measured build/test/security regressions; establish the complete SDK/Jint/Godot validation baseline.
2. Validate the new developer probe and synthetic MV/MZ fixtures under actual Godot/Jint, not only Node.
3. Use authorized user MV/MZ projects to collect precise missing-API and boot errors. Keep `inspection`, `plugin-subset execution`, `engine boot` and `playable game` distinct in reports/UI.
4. Build a version-aware manifest for the actual core/library files and their correct order. Plugins must execute against real supported engine-facing APIs, not fake global classes.
5. Implement the required shared browser services incrementally: data/asset loading through VFS, DOM elements needed for boot, real rendering, audio, input and storage. Keep MV/MZ differences in profiles.
6. Reach title screen, New Game, map movement, dialogue/event flow, transfers and save/load in a simple authorized project.
7. Then expand custom-plugin compatibility using minimized regressions, not a hard-coded list of game names.
8. Keep other engine tracks maintained; native DLL/Win32 compatibility and legacy research remain later work.

## Present boundaries

A concrete Jint adapter and PluginManager shim exist. The opt-in browser frame host adds window/self, a virtual performance clock, function timers, animation callbacks and scoped currentScript metadata. It does NOT implement a complete browser, renderer, audio engine, Node or the whole RPG Maker core.

`WebScriptRuntime.AdvanceFrame` takes seconds and pumps one bounded VM call after successful bootstrap. New callbacks are deferred to the next pump; missed interval periods are coalesced. These are explicit subset semantics. Full compatibility claims must await real engine integration.

The developer scene `res://tools/web_plugin_probe.tscn` inspects by default. `--execute-plugins` explicitly runs trusted game scripts in the limited host. Its `subset-passed` result is not proof of a playable game. An in-process Jint VM is not an operating-system sandbox.

## Development rules

Work directly from source/tests and the concise checkpoint. Preserve working behavior and public signatures. Implement one coherent improvement, add tests, run focused checks, self-repair, run `./scripts/validate.sh` when possible, document the verified scope, checkpoint and continue.

Refactors are permitted for concrete correctness/maintainability reasons, not style alone. Avoid duplicate runtime implementations and success-returning placeholders. Unsupported functionality must produce useful failures. Do not promote Runtime merely because a detector or scripting helper exists.

Use VFS access and read-only game mounts. No execution during inspection. Do not grant game code arbitrary CLR/filesystem/process/native/network access or bypass DRM. Do not require end users to install external original-engine/EasyRPG/mkxp/Wine/NW.js/browser processes. Internally embedded, properly licensed components are allowed.

Custom scripts remain first-class. Preserve ordering and method receivers; keep source identity, parameters, callback state and version profiles accurate. Failed sessions require fresh runtimes/VMs rather than replaying partial initialization. Record diagnostics instead of swallowing exceptions.

## Anti-loop and truthful acceptance

At most three materially different failed strategies for one error signature without new evidence. Preserve logs and useful work, revert only harmful experiments, record the blocker and an actionable unblock condition, then continue an independent useful slice. Do not repeatedly run the same command, delete correct tests or disable safety checks to claim progress.

Distinguish code written, tests added, tests actually executed and real-game evidence. Node checks of embedded JavaScript do not prove C# compilation or Jint/Godot correctness. Simulated-tool tests do not prove real builds. Historical main test counts are not branch validation. Keep the PR unmerged until it has fresh full validation and review.

The user should not need to choose every ordinary next task. Continue toward a genuinely testable MV/MZ runtime while retaining the broader engine architecture.
