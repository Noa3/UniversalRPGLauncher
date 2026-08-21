# Engine Detection and Runtime Selection

## Status

Implemented as a bounded, non-executing inspection pipeline. Detection is available for the built-in engine signatures below; playable runtime backends remain separate work and are not implied by detection.

## Flow

```text
folder or ZIP archive
        |
        v
SafeGameInspector
  bounded entries, metadata bytes, archive budget
        |
        v
EngineDetectionRegistry
  registered IEngineDetectionPlugin instances
        |
        v
ranked EngineDetectionReport
  score, confidence, evidence, version, diagnostics
        |
        v
EngineRuntimeSelector
  explicit ambiguity/unknown/malformed/capability/platform checks
        |
        v
EnginePluginRegistry -> IEnginePlugin -> runtime
```

`GameDetector` remains as a compatibility facade for the launcher and library. It does not contain engine-specific scoring branches; it converts the plugin report into the existing `DetectionResult` model and exposes the full ranked report through `Report`/`Candidates`.

`GameLibrary.Import()` and the normal library scan persist a versioned import record in `user://library.cfg`. The record contains the source path and ID, all ranked candidates, scores/confidence, evidence, diagnostics, the selected plugin ID when detection is unambiguous, and the current compatibility status. On relaunch the source is inspected again; stale persisted selections are not trusted when the current candidate list no longer contains them.

## Built-in detection plugins

| Plugin ID | Engine | Runtime status |
|---|---|---|
| `rpg-maker-95` | RPG Maker 95 | Detection plus safe runtime bootstrap |
| `rpg-maker-2000` | RPG Maker 2000 | Detection/parsing plus minimal LDB/LMT/LMU runtime bootstrap |
| `rpg-maker-2003` | RPG Maker 2003 | Detection/parsing plus minimal LDB/LMT/LMU runtime bootstrap |
| `rpg-maker-xp` | RPG Maker XP / RGSS1 | Detection/parsing plus safe runtime bootstrap |
| `rpg-maker-vx` | RPG Maker VX / RGSS2 | Detection/parsing plus safe runtime bootstrap |
| `rpg-maker-vx-ace` | RPG Maker VX Ace / RGSS3 | Detection/parsing plus safe runtime bootstrap |
| `rpg-maker-mv` | RPG Maker MV | Detection/parsing plus safe runtime bootstrap |
| `rpg-maker-mz` | RPG Maker MZ | Detection/parsing plus safe runtime bootstrap |
| `wolf-rpg` | WOLF RPG Editor | Detection/parsing plus safe runtime bootstrap |
| `rpg-maker-unite` | Unity/RPG Maker Unite candidate | Detection only; arbitrary Unity exports are not treated as Unite games |

Detection-only means the project can be identified and reported, but `EngineRuntimeSelector` refuses to launch it until a plugin advertises the required `Runtime` capability and passes its compatibility probe. All built-in targets except the research-only Unite candidate now have a safe bootstrap lifecycle; RM2K/RM2K3 additionally parse LDB/LMT/LMU data. None of these bootstraps provides full event, RGSS/JavaScript/WOLF VM, rendering, audio, menu, save, or battle behavior yet.

## Extending detection

Implement `IEngineDetectionPlugin` in the application, provide stable `EnginePluginMetadata` with the `Detection` capability and supported engine ranges, and register the compiled instance with `EngineDetectionRegistry`. A plugin must inspect `EngineInspectionContext.Snapshot` only. User-provided DLLs, executables, scripts, and native plugins are never loaded as detector extensions.

For a playable engine, implement `IEnginePlugin` as well, advertise `Runtime` and any additional capabilities, constrain `SupportedPlatforms` when needed, and register it with `EnginePluginRegistry`. Runtime selection uses the exact detected plugin ID; it does not silently select another engine or fall back to an external executable.

## Inspection and security limits

- Folder traversal is bounded by depth and entry count.
- Reparse points (symlinks/junctions) are skipped.
- Metadata reads are capped at 1 MiB per file by default.
- ZIP inspection is read-only, does not extract entries, and enforces entry count, per-entry, and total uncompressed-size limits.
- Absolute and `..` archive paths are rejected.
- Executable/DLL names and bounded prefixes may be inspected as data; no discovered file is executed.
- Malformed or over-budget input produces diagnostics and a safe unknown result where no trustworthy candidate can be returned.

## Ambiguity and diagnostics

Candidates are ordered deterministically by probe score, metadata priority, and ordinal plugin ID. Equal top scores for different engine IDs set `IsAmbiguous` and leave `SelectedCandidate` empty. Unknown, ambiguous, malformed, missing, unsupported, and platform-incompatible cases return typed errors from `EngineRuntimeSelector` with diagnostics suitable for the UI/import report.

The launcher displays these diagnostics inside the same application. It does not start external game executables. A detection-only candidate stays visible as an imported game but its launch action remains unavailable until a compiled plugin advertises `PluginCapability.Runtime` and passes the required checks.
