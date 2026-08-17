# UniversalRPG — Compatibility Guide

> **Last Updated:** 2026-08-17

## Compatibility Philosophy

The priority order is:

1. **Correct game behavior** — Original behavior takes absolute priority
2. **Compatibility** — Support as many games as possible
3. **Stability** — Never crash on malformed input
4. **Security** — Treat imported games as untrusted
5. **Performance** — Run efficiently on mobile hardware
6. **Enhancements** — Graphics/UI improvements are secondary

## Compatibility Modes

### Faithful Mode

Default mode for all games. Reproduces original RPG Maker behavior as accurately as practical.

- Disables potentially incompatible improvements
- Uses original timing where possible
- Preserves original rendering behavior
- No enhanced scaling by default
- No shader effects by default

### Enhanced Mode

Optional mode that adds modern improvements without altering gameplay semantics.

- Integer scaling
- High-resolution presentation
- Shader effects (CRT, scanlines, etc.)
- Modern controller support
- Touch controls (Android)
- Fast-forward/slow-motion
- Save states
- Asset overrides

Every enhancement is individually disableable.

## Compatibility Database

The compatibility database is data-driven. Game-specific behavior uses centralized flags:

```json
{
  "id": "game.title",
  "sha256": "hash_of_game_files",
  "engine": "RPGMaker2003",
  "type": "game_profile",
  "compatibility": "full",
  "flags": [
    {"name": "PreserveLegacyPictureTiming", "type": "BOOLEAN", "value": true},
    {"name": "LegacyTextEncoding", "type": "STRING", "value": "CP932"},
    {"name": "DisableEnhancedRenderer", "type": "BOOLEAN", "value": true}
  ],
  "notes": "Known quirks and workarounds"
}
```

### Compatibility Flags

| Flag | Type | Description |
|------|------|-------------|
| PreserveLegacyPictureTiming | BOOLEAN | Keep original picture update timing |
| LegacyTextEncoding | STRING | Force legacy encoding (CP932, Shift_JIS, etc.) |
| DisableEnhancedRenderer | BOOLEAN | Disable enhanced rendering for this game |
| AlternateBattleAnimationTiming | BOOLEAN | Fix battle animation timing quirks |
| MaxMapCount | INTEGER | Override maximum map count |
| ForceWindowedMode | BOOLEAN | Force windowed mode |
| DisableFastForward | BOOLEAN | Prevent fast-forward for this game |

## Common Compatibility Issues

### RM2000/2003

- **Text encoding**: CP932/Shift_JIS handling
- **Picture timing**: Original games update pictures on specific frames
- **Battle transitions**: Some games use custom transition timing
- **Random number generation**: Not all games use standard RNG

### RMXP/VX/VXAce (RGSS)

- **Ruby version differences**: Each RGSS version targets different Ruby
- **Win32API calls**: Some games call Windows APIs directly
- **DLL dependencies**: Some games use native DLLs
- **Script errors**: RGSS script errors must be captured and reported

### RMV/MZ

- **JavaScript engine**: Different games may expect different JS features
- **Plugin compatibility**: Plugins may use unsupported browser APIs
- **Node.js dependencies**: Some games rely on Node-specific APIs
- **Canvas/WebGL**: Rendering compatibility varies by game

## Diagnostic Mode

Built-in debugging overlays show:

- FPS / simulation FPS
- Frame time
- Current map and event
- Switch/variable changes
- Script errors
- Missing assets
- Audio state
- Draw calls
- Memory usage
- Runtime warnings

## Compatibility Reports

Users can export compatibility reports without distributing copyrighted files:

```
Runtime version: 0.2.0
Platform: Windows x86_64
Game hash: abc123...
Engine: RPG Maker VX Ace
Plugin hashes: [def456..., ghi789...]
DLL metadata: RGSS302A.dll v3.02
Script errors: [list]
Unsupported APIs: [list]
Missing assets: [list]
Runtime warnings: [list]
Crash information: [if applicable]
```

## Reporting Compatibility Issues

When reporting issues, include:

1. Game name and version
2. Engine type (auto-detected or manual)
3. Compatibility mode (Faithful/Enhanced)
4. Exported compatibility report
5. Reproduction steps
6. Expected vs. actual behavior

## Adding New Compatibility Entries

1. Test the game in Faithful Mode
2. Document quirks and workarounds
3. Create a compatibility profile entry
4. Add regression tests if possible
5. Update this document

## Legal Notes

- Never bundle proprietary RPG Maker code or RTP assets
- Implement behavior independently
- Document all third-party components
- Respect game licenses and redistribution rights
