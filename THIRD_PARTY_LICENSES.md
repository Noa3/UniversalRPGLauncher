# Third-Party Components and Licenses

> **Last reviewed:** 2026-09-09

This file records third-party components that are actually distributed with UniversalRPG and tracks prospective dependencies separately.

A candidate listed below is **not** an approved dependency until its version, license, platform support and integration strategy are reviewed.

## Currently Distributed

### Noto Sans CJK SC

- **Component:** `project/assets/fonts/NotoSansCJKsc-Regular.otf`
- **Purpose:** launcher Latin/Japanese/Korean/Simplified-Chinese glyph coverage
- **Upstream:** notofonts/noto-cjk
- **License:** SIL Open Font License 1.1
- **License text:** `project/assets/fonts/OFL.txt`

## Prospective Runtime Dependencies

No Ruby VM, JavaScript VM or PE/native execution library is currently selected as the canonical dependency.

### Embedded Ruby / RGSS

Requirements before selection:

- compatibility with the Ruby behavior required by RGSS1/2/3
- embeddable without a user-installed runtime
- Windows/Linux/Android feasibility
- acceptable license and redistribution terms
- controllable filesystem/process/network/native-extension boundaries
- testable resource limits

Candidates must be evaluated when the RGSS architecture card becomes actionable. Do not describe a candidate as “suitable for RGSS1/2/3” without compatibility evidence.

### Embedded JavaScript / MV/MZ

Requirements before selection:

- sufficient ECMAScript behavior for RPG Maker MV/MZ/default plugins
- Windows/Linux/Android support
- embeddable sandbox boundary
- memory/interrupt/watchdog control
- acceptable license and redistribution terms
- practical bindings for Canvas/WebGL/WebAudio and host APIs

Potential families to research include QuickJS, Duktape and V8, but no choice is currently approved.

### PE / Native Binary Inspection

Initial native work must remain metadata-only.

A future library or self-implementation may be evaluated for:

- PE headers
- architecture
- imports/exports
- version metadata
- hashes

Selecting a PE parser does not authorize native DLL execution.

## Asset Licensing

If future development sources textures, audio, fonts, UI graphics or other game assets externally, record at minimum:

- exact asset
- original author/source
- source URL
- license name/version
- commercial-use permission
- modification permission
- redistribution permission
- attribution requirement
- local modifications

Prefer assets with clear redistribution terms. “Free download” without a clear license is not sufficient.

Do not use ripped assets, unofficial reposts or proprietary RPG Maker/WOLF/game assets without permission.

A dedicated asset manifest may be added when external game-facing assets begin to be included.

## Licensing Principles

1. Prefer permissive or clearly compatible licenses.
2. Review copyleft obligations before introducing them; do not reject a license category solely by label without understanding its actual obligations.
3. Never bundle proprietary RPG Maker/WOLF runtime binaries or RTP assets without explicit redistribution rights.
4. Include required license text and attribution with distributed third-party components.
5. Pin source/version information for reproducibility.
6. Re-evaluate licensing when a dependency changes distribution mode (for example static vs dynamic linking).
7. Keep planned candidates separate from components actually shipped.

## Adding a Dependency

Before merging:

1. identify exact upstream project/version
2. verify license from the original source
3. confirm Windows/Linux/Android implications
4. assess security/sandbox impact
5. assess binary size/performance impact
6. add regression/build coverage
7. update this file and include required license texts
8. ensure release packaging contains required notices

## Proprietary Engine / RTP Data

Not distributed by UniversalRPG unless an explicit license permits it.

Users provide legally obtained games and required RTP resources.

The project aims to implement compatible behavior independently rather than redistributing original engine runtimes.
