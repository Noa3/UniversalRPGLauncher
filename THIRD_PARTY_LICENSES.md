# Third-Party Components and Licenses

Last reviewed: 2026-09-12.

This inventory distinguishes components present in source/build configuration from future candidates. A declared package dependency is not proof that release packaging or transitive notices have been fully audited.

## Present components

### Noto Sans CJK SC

- Asset: `project/assets/fonts/NotoSansCJKsc-Regular.otf`
- Purpose: launcher glyph coverage.
- Upstream: `notofonts/noto-cjk`.
- License: SIL Open Font License 1.1.
- Local license text: `project/assets/fonts/OFL.txt`.

### Jint

- Declared package: **Jint 4.16.2**.
- Used by: `runtime/UniversalRPG.JavaScript.Jint/UniversalRPG.JavaScript.Jint.csproj`.
- Purpose: experimental embedded JavaScript adapter behind `IEmbeddedScriptVm`.
- Upstream: `https://github.com/sebastienros/jint`.
- Version reference: `v4.16.2`, commit `730db51d99d3bede8072ca55b0437c3206b83599`.
- License: BSD 2-Clause.
- Local notice: `third_party/Jint/LICENSE.txt`.
- Status: adapter source and package reference exist; current branch build/smoke validation is pending.

Do not describe QuickJS as the only current VM prototype or Jint as an unselected candidate. Jint is already a declared dependency. It remains interchangeable and is not a commitment to a full MV/MZ browser runtime.

Before release, restore the pinned dependency graph, inventory transitive dependencies, include their required notices, and verify the packaged assemblies. This document does not claim that unresolved transitive packages have already been reviewed.

### Godot and .NET

The host project references Godot.NET.Sdk/4.7.2 and targets .NET 8. Release packaging must include the notices required by the actual engine/runtime binaries and their dependencies. Export presets or SDK references alone do not demonstrate completed platform packaging or notice compliance.

## Development-only tools

The Node-based regression scripts use built-in `node:` modules, with no npm package dependencies. Node is a development/CI tool for checking embedded JavaScript semantics. It is not required by the end user's URPG installation and is not launched as a game runtime.

GitHub Actions referenced in workflows are CI tooling, not shipped game content.

## Prospective dependencies

### Ruby / RGSS

No concrete Ruby backend is embedded yet. CRuby-family embedding remains a research direction behind the existing VM factory and generation-specific profiles.

Before adoption:

- select the exact source/version;
- review that version's COPYING/LEGAL and bundled dependencies;
- verify historical RGSS1/2/3 language behavior rather than assuming current Ruby is compatible;
- validate Windows/Linux/Android integration and resource/host-access restrictions;
- include required notices in release packages.

### Alternative JavaScript backends

QuickJS/QuickJS-ng and other engines remain optional future alternatives behind the SDK VM interface. Evaluate their exact licenses, dependencies, platform support and interruption/memory behavior before integration. A JavaScript interpreter alone does not provide Canvas, WebGL, WebAudio, DOM or RPG Maker APIs.

### Native libraries / PE inspection

Metadata inspection and native execution are separate concerns. Selecting a PE parser does not authorize loading arbitrary game DLLs. Any future dependency requires a version/platform/license review and explicit runtime capability policy.

## External asset provenance

For each new third-party texture, sound, font, UI image or other asset, record the original author/source URL, exact license/version, local path, modifications and required attribution. Verify permission for the intended use, modification and redistribution before importing it.

A downloadable asset with no clear license is not approved for inclusion. Do not import ripped game assets, unofficial reposts or proprietary RTP content without applicable rights. Preserve existing approved notices; record uncertain assets separately rather than silently treating them as production content.

## Release gates

- Select an explicit license for UniversalRPG itself before publishing a reusable package under assumed open-source terms.
- Keep actual dependencies separate from research candidates.
- Pin versions and audit the restored transitive graph.
- Include required license texts and credits in binary distributions.
- Recheck obligations when linking/packaging mode changes.
- Do not include proprietary RPG Maker/WOLF runtime binaries, user games or RTP assets without redistribution permission.

Users supply legally obtained games and RTP resources. URPG implements compatibility behavior without bundling those proprietary contents.
